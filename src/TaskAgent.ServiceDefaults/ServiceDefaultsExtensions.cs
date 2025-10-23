using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace TaskAgent.ServiceDefaults
{
    /// <summary>
    /// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
    /// This project should be referenced by each service project in your solution.
    ///
    /// Telemetry Architecture:
    /// - DEVELOPMENT: Uses OTLP exporter → Aspire Dashboard (localhost:18888)
    /// - PRODUCTION: Uses Azure Monitor → Application Insights
    ///
    /// The appropriate exporter is selected based on configuration:
    /// - If OTEL_EXPORTER_OTLP_ENDPOINT is set → OTLP (Aspire Dashboard)
    /// - If APPLICATIONINSIGHTS_CONNECTION_STRING is set → Azure Monitor (Production)
    ///
    /// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
    /// </summary>
    public static class ServiceDefaultsExtensions
    {
        private const string HealthEndpointPath = "/health";
        private const string AlivenessEndpointPath = "/alive";

        public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
            where TBuilder : IHostApplicationBuilder
        {
            builder.ConfigureOpenTelemetry();

            builder.AddDefaultHealthChecks();

            builder.Services.AddServiceDiscovery();

            builder.Services.ConfigureHttpClientDefaults(http =>
            {
                // Turn on resilience by default
                http.AddStandardResilienceHandler();

                // Turn on service discovery by default
                http.AddServiceDiscovery();
            });

            // Security: Restrict service discovery to HTTPS in production only
            // Development allows both HTTP and HTTPS for local service-to-service communication
            if (!builder.Environment.IsDevelopment())
            {
                builder.Services.Configure<ServiceDiscoveryOptions>(options =>
                {
                    options.AllowedSchemes = ["https"];
                });
            }

            return builder;
        }

        private static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
            where TBuilder : IHostApplicationBuilder
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });

            builder
                .Services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        // Custom meters for AI Agent
                        .AddMeter("TaskAgent.Agent")
                        .AddMeter("TaskAgent.Functions");
                })
                .WithTracing(tracing =>
                {
                    tracing
                        .AddSource(builder.Environment.ApplicationName)
                        // Custom activity sources for AI Agent
                        .AddSource("TaskAgent.Agent")
                        .AddSource("TaskAgent.Functions")
                        .AddAspNetCoreInstrumentation(options =>
                            // Exclude health check requests from tracing
                            options.Filter = context =>
                                !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                                && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                        )
                        // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                        //.AddGrpcClientInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddEntityFrameworkCoreInstrumentation(efOptions =>
                        {
                            // Capture detailed SQL queries in development only (security concern in production)
                            bool isDevelopment = builder.Environment.IsDevelopment();
                            efOptions.SetDbStatementForText = isDevelopment;
                            efOptions.SetDbStatementForStoredProcedure = isDevelopment;
                            efOptions.EnrichWithIDbCommand = (activity, _) =>
                            {
                                // Add custom tags to EF activities
                                activity?.SetTag("db.system", "sqlserver");
                            };
                        });
                });

            builder.AddOpenTelemetryExporters();

            return builder;
        }

        private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
            where TBuilder : IHostApplicationBuilder
        {
            // Development: Use OTLP exporter for Aspire Dashboard (localhost)
            bool useOtlpExporter = !string.IsNullOrWhiteSpace(
                builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
            );

            if (useOtlpExporter)
            {
                builder.Services.AddOpenTelemetry().UseOtlpExporter();
            }

            // Production: Use Azure Monitor (Application Insights)
            string? applicationInsightsConnectionString = builder.Configuration[
                "APPLICATIONINSIGHTS_CONNECTION_STRING"
            ];

            if (!string.IsNullOrEmpty(applicationInsightsConnectionString))
            {
                builder
                    .Services.AddOpenTelemetry()
                    .UseAzureMonitor(options =>
                    {
                        options.ConnectionString = applicationInsightsConnectionString;
                    });
            }

            return builder;
        }

        private static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
            where TBuilder : IHostApplicationBuilder
        {
            builder
                .Services.AddHealthChecks()
                // Add a default liveness check to ensure app is responsive
                .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

            return builder;
        }

        public static WebApplication MapDefaultEndpoints(this WebApplication app)
        {
            // Adding health checks endpoints to applications in non-development environments has security implications.
            // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
            if (!app.Environment.IsDevelopment())
            {
                return app;
            }

            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks(HealthEndpointPath);

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(
                AlivenessEndpointPath,
                new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") }
            );

            return app;
        }
    }
}
