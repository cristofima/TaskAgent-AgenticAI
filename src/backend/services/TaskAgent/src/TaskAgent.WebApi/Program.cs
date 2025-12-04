using Serilog;
using TaskAgent.Application;
using TaskAgent.Infrastructure;
using TaskAgent.ServiceDefaults;
using TaskAgent.WebApi;
using TaskAgent.WebApi.Extensions;

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    // Configure ServiceDefaults: OpenTelemetry (Tracing + Metrics) + Health Checks + Resilience
    builder.AddServiceDefaults();

    // Configure Serilog: Logging (completes the 3 pillars of observability)
    builder.Host.AddSerilogDefaults();

    Log.Information("🚀 Starting {ApplicationName}...", "TaskAgent WebApi");

    // Register application layers
    builder
        .Services.AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddPresentation(builder.Configuration) // Keep for now (parallel deployment)
        .AddAgentServices(builder.Configuration); // AG-UI integration with agent registration

    WebApplication app = builder.Build();

    // Apply database migrations automatically on startup (all environments)
    await app.ApplyDatabaseMigrationsAsync();

    // Configure middleware pipeline
    app.ConfigureMiddlewarePipeline();

    await app.RunAsync();
}
catch (Exception ex)
{
    LogFatalError(ex);
    throw;
}
finally
{
    await CloseAndFlushLoggerAsync();
}

return;

static void LogFatalError(Exception ex)
{
    Log.Fatal(ex, "❌ Application terminated unexpectedly");
}

static async Task CloseAndFlushLoggerAsync()
{
    Log.Information("🛑 Shutting down {ApplicationName}...", "TaskAgent WebApi");
    await Log.CloseAndFlushAsync();
}
