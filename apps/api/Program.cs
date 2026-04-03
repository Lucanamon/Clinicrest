using api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

var app = builder.Build();

await ApplyDatabaseMigrationsAsync(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
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
