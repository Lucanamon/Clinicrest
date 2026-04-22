using System.Security.Claims;
using System.Text;
using api.Application.Abstractions;
using api.Application.Services;
using api.Domain;
using api.Domain.Entities;
using api.Infrastructure.Auth;
using api.Infrastructure.DependencyInjection;
using api.Infrastructure.Integrations;
using api.Infrastructure.Middleware;
using api.Infrastructure.Persistence;
using api.Hubs;
using api.Workers;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

try
{
    var builder = WebApplication.CreateBuilder(args);
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured.");
    var sanitizedConnectionString = SanitizeConnectionString(connectionString);
    Console.WriteLine($"[Startup] DefaultConnection={sanitizedConnectionString}");

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddHealthChecks();
    const string CorsPolicyName = "WebClientCors";
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(CorsPolicyName, policy =>
        {
            policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddClinicrestPersistence(connectionString);
    builder.Services.AddClinicrestRepositories();
    builder.Services.AddClinicrestApplicationServices();
    builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
    builder.Services.AddScoped<INotificationSender, SmtpNotificationSender>();
    builder.Services.AddHostedService<NotificationWorker>();
    builder.Services.AddSignalR();

    var jwtSection = builder.Configuration.GetSection("Jwt");
    var jwtIssuer = jwtSection["Issuer"] ?? "clinicrest-api";
    var jwtAudience = jwtSection["Audience"] ?? "clinicrest-web";
    var jwtSecret = jwtSection["Secret"] ?? throw new InvalidOperationException("Jwt:Secret must be configured.");
    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = signingKey,
                ClockSkew = TimeSpan.FromSeconds(30),
                RoleClaimType = ClaimTypes.Role
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        // Keep endpoints public by default; protect only explicitly attributed controllers/actions.
        options.FallbackPolicy = null;
    });

    var app = builder.Build();

    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var error = context.Features.Get<IExceptionHandlerFeature>();
            Console.WriteLine(error?.Error);
            await Task.CompletedTask;
        });
    });

    await LogDatabaseStateAsync(app);
    await ApplyDatabaseMigrationsAsync(app);
    await SeedRootAdminAsync(app);

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseCors(CorsPolicyName);
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<ActivityMiddleware>();
    app.MapControllers();
    app.MapHub<NotificationHub>("/hubs/notifications");
    app.MapHealthChecks("/health");
    app.Run();
}
catch (Exception ex)
{
    Console.Error.WriteLine("[Startup] Fatal exception:");
    Console.Error.WriteLine(ex);
    throw;
}

static async Task ApplyDatabaseMigrationsAsync(WebApplication app)
{
    const int maxRetries = 5;
    var delay = TimeSpan.FromSeconds(2);

    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            logger.LogInformation("Applying EF Core migrations. Attempt {Attempt}/{MaxRetries}", attempt, maxRetries);
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("EF Core migrations applied successfully.");
            return;
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            logger.LogWarning(ex, "Migration attempt {Attempt} failed. Retrying in {DelaySeconds}s.", attempt, delay.TotalSeconds);
            await Task.Delay(delay);
            delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2);
        }
    }

    // Final attempt surfaces full error to fail fast on unrecoverable startup issues.
    await dbContext.Database.MigrateAsync();
}

static async Task LogDatabaseStateAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    await using var connection = dbContext.Database.GetDbConnection();
    if (connection.State != System.Data.ConnectionState.Open)
    {
        await connection.OpenAsync();
    }

    await using var command = connection.CreateCommand();
    command.CommandText =
        """
        SELECT current_database(),
               EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'slots'),
               EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'bookings');
        """;

    await using var reader = await command.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        var databaseName = reader.GetString(0);
        var hasSlots = reader.GetBoolean(1);
        var hasBookings = reader.GetBoolean(2);

        logger.LogInformation(
            "Database verification: Database={DatabaseName}, slots={HasSlots}, bookings={HasBookings}",
            databaseName,
            hasSlots,
            hasBookings);
    }
}

static async Task SeedRootAdminAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
    var configuration = app.Configuration;
    var environment = app.Environment;

    var seedUsername = configuration["Seed:RootAdminUsername"] ?? "rootadmin";
    var seedPassword = configuration["Seed:RootAdminPassword"] ?? "guardianOP";

    var syncRequested = configuration.GetValue("Seed:SyncRootAdminPasswordOnStartup", false);
    if (environment.IsProduction() && syncRequested)
    {
        logger.LogWarning("Seed:SyncRootAdminPasswordOnStartup is ignored in Production.");
    }

    var syncOnStartup = syncRequested && !environment.IsProduction();
    var resetOnMismatch = configuration.GetValue("Seed:ResetRootAdminIfPasswordMismatch", false);

    await using var connection = dbContext.Database.GetDbConnection();
    if (connection.State != System.Data.ConnectionState.Open)
    {
        await connection.OpenAsync();
    }

    await using var command = connection.CreateCommand();
    command.CommandText =
        """
        SELECT COUNT(*)
        FROM information_schema.columns
        WHERE table_name = 'users'
          AND column_name = 'PasswordHash';
        """;

    var columnCount = Convert.ToInt32(await command.ExecuteScalarAsync());
    var hasPasswordHashColumn = columnCount > 0;

    if (!hasPasswordHashColumn)
    {
        logger.LogWarning("Skipping root admin seed because users.PasswordHash does not exist yet.");
        return;
    }

    var rootAdmin = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == seedUsername);
    if (rootAdmin is null)
    {
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Username = seedUsername,
            PasswordHash = string.Empty,
            Role = Roles.RootAdmin
        };

        newUser.PasswordHash = passwordHasher.HashPassword(newUser, seedPassword);
        dbContext.Users.Add(newUser);
        await dbContext.SaveChangesAsync();
        logger.LogInformation("Root admin created");
        return;
    }

    if (syncOnStartup)
    {
        rootAdmin.PasswordHash = passwordHasher.HashPassword(rootAdmin, seedPassword);
        await dbContext.SaveChangesAsync();
        logger.LogInformation("Root admin password synced");
        return;
    }

    if (resetOnMismatch)
    {
        if (AuthPasswordValidation.VerifyPassword(passwordHasher, rootAdmin, seedPassword))
        {
            logger.LogInformation("Root admin already valid");
            return;
        }

        rootAdmin.PasswordHash = passwordHasher.HashPassword(rootAdmin, seedPassword);
        await dbContext.SaveChangesAsync();
        logger.LogInformation("Root admin password synced");
        return;
    }

    if (AuthPasswordValidation.VerifyPassword(passwordHasher, rootAdmin, seedPassword))
    {
        logger.LogInformation("Root admin already valid");
    }
    else
    {
        logger.LogWarning("Root admin seed skipped: password does not match configured seed and no reset/sync flags enabled.");
    }
}

static string SanitizeConnectionString(string connectionString)
{
    var builder = new NpgsqlConnectionStringBuilder(connectionString);
    if (!string.IsNullOrWhiteSpace(builder.Password))
    {
        builder.Password = "***";
    }

    if (!string.IsNullOrWhiteSpace(builder.Username))
    {
        builder.Username = builder.Username;
    }

    return builder.ConnectionString;
}

public partial class Program;
