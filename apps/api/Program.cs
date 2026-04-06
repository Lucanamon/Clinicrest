using System.Security.Claims;
using System.Text;
using api.Application.Abstractions;
using api.Application.Services;
using api.Domain;
using api.Domain.Entities;
using api.Infrastructure.Auth;
using api.Infrastructure.Persistence;
using api.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

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
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IBacklogRepository, BacklogRepository>();
builder.Services.AddScoped<IBacklogService, BacklogService>();
builder.Services.AddScoped<IGlobalSearchService, GlobalSearchService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

var app = builder.Build();

await ApplyDatabaseMigrationsAsync(app);
await SeedRootAdminAsync(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();

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
