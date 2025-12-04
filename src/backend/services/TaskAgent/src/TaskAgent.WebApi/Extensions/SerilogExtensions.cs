using Serilog;

namespace TaskAgent.WebApi.Extensions;

/// <summary>
/// Extension methods for configuring Serilog logging.
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    /// Configures Serilog with default settings for the application.
    /// </summary>
    public static void AddSerilogDefaults(this ConfigureHostBuilder hostBuilder)
    {
        hostBuilder.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console();
        });
    }
}
