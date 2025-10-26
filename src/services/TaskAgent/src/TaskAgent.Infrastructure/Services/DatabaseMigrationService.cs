using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskAgent.Infrastructure.Data;

namespace TaskAgent.Infrastructure.Services;

/// <summary>
/// Database migration service for Infrastructure layer
/// </summary>
public static class DatabaseMigrationService
{
    /// <summary>
    /// Applies pending database migrations automatically.
    /// This method applies migrations in all environments for simplified deployment.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve dependencies</param>
    public static async Task ApplyDatabaseMigrationsAsync(IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        TaskDbContext dbContext = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
        ILogger<TaskDbContext> logger = scope.ServiceProvider.GetRequiredService<
            ILogger<TaskDbContext>
        >();

        try
        {
            IEnumerable<string> pendingMigrations =
                await dbContext.Database.GetPendingMigrationsAsync();

            if (pendingMigrations.Any())
            {
                logger.LogInformation(
                    "Applying {Count} pending migrations: {Migrations}",
                    pendingMigrations.Count(),
                    string.Join(", ", pendingMigrations)
                );

                await dbContext.Database.MigrateAsync();

                logger.LogInformation("Database migrations applied successfully");
            }
            else
            {
                logger.LogInformation("Database is up to date. No pending migrations");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "An error occurred while applying database migrations. "
                    + "Please ensure the database server is accessible and the connection string is correct"
            );
            throw;
        }
    }
}
