using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskAgent.Infrastructure.Data;

namespace TaskAgent.Infrastructure.Services;

/// <summary>
/// Database migration service for Infrastructure layer
/// Handles migrations for both TaskDbContext (SQL Server) and ConversationDbContext (PostgreSQL)
/// </summary>
public static class DatabaseMigrationService
{
    /// <summary>
    /// Applies pending database migrations automatically for both databases.
    /// This method applies migrations in all environments for simplified deployment.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve dependencies</param>
    public static async Task ApplyDatabaseMigrationsAsync(IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();

        // Apply SQL Server migrations (Tasks)
        await ApplyMigrationsForContextAsync<TaskDbContext>(scope, "SQL Server (Tasks)");

        // Apply PostgreSQL migrations (Conversations)
        await ApplyMigrationsForContextAsync<ConversationDbContext>(
            scope,
            "PostgreSQL (Conversations)"
        );
    }

    private static async Task ApplyMigrationsForContextAsync<TContext>(
        IServiceScope scope,
        string databaseName
    )
        where TContext : DbContext
    {
        TContext dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        ILogger<TContext> logger = scope.ServiceProvider.GetRequiredService<ILogger<TContext>>();

        try
        {
            IEnumerable<string> pendingMigrations =
                await dbContext.Database.GetPendingMigrationsAsync();

            if (pendingMigrations.Any())
            {
                logger.LogInformation(
                    "[{DatabaseName}] Applying {Count} pending migrations: {Migrations}",
                    databaseName,
                    pendingMigrations.Count(),
                    string.Join(", ", pendingMigrations)
                );

                await dbContext.Database.MigrateAsync();

                logger.LogInformation(
                    "[{DatabaseName}] Database migrations applied successfully",
                    databaseName
                );
            }
            else
            {
                logger.LogInformation(
                    "[{DatabaseName}] Database is up to date. No pending migrations",
                    databaseName
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[{DatabaseName}] Failed to apply database migrations. "
                    + "The database server is not accessible. "
                    + "Application cannot start without a working database connection. "
                    + "Error: {ErrorMessage}",
                databaseName,
                ex.Message
            );
            // Re-throw exception to prevent application from starting with unavailable database
            throw;
        }
    }
}
