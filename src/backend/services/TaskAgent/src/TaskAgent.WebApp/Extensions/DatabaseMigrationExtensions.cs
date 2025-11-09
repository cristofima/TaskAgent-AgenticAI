using TaskAgent.Infrastructure.Services;

namespace TaskAgent.WebApp.Extensions;

/// <summary>
/// Extension methods for database migrations in WebApp layer
/// </summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies pending database migrations automatically on application startup.
    /// This method applies migrations in all environments for simplified deployment.
    /// </summary>
    /// <param name="app">The WebApplication instance</param>
    /// <returns>The same WebApplication instance for method chaining</returns>
    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app) =>
        await DatabaseMigrationService.ApplyDatabaseMigrationsAsync(app.Services);
}
