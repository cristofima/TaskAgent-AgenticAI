using Microsoft.Extensions.DependencyInjection;
using TaskAgent.Application.Telemetry;

namespace TaskAgent.Application;

/// <summary>
/// Extension methods for registering Application layer services.
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Registers Application layer services including telemetry.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register telemetry components
        services.AddSingleton<AgentMetrics>();

        return services;
    }
}
