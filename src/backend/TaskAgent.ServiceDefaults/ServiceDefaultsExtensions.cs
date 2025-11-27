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
using Serilog;
using Serilog.Events;

namespace TaskAgent.ServiceDefaults
{
    /// <summary>
    /// Provides extension methods for configuring .NET Aspire service defaults: OpenTelemetry, service discovery, resilience, health checks, and Serilog.
    /// </summary>
    /// <remarks>
    /// Reference this project from each service to apply consistent observability and infrastructure patterns.
    /// See <see href="https://aka.ms/dotnet/aspire/service-defaults">Aspire documentation</see> for details.
    /// </remarks>
    public static class ServiceDefaultsExtensions
    {
        private const string HealthEndpointPath = "/health";
        private const string AlivenessEndpointPath = "/alive";

        /// <summary>
        /// Configures service defaults: OpenTelemetry, service discovery, resilience, and health checks.
        /// </summary>
        /// <typeparam name="TBuilder">The host application builder type.</typeparam>
        /// <param name="builder">The host application builder.</param>
        /// <returns>The configured builder for method chaining.</returns>
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

        /// <summary>
        /// Maps health check endpoints (<c>/health</c> and <c>/alive</c>) in development environments only.
        /// </summary>
        /// <param name="app">The web application.</param>
        /// <returns>The configured application for method chaining.</returns>
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

        /// <summary>
        /// Configures Serilog with console and file sinks, integrating with OpenTelemetry for centralized logging.
        /// </summary>
        /// <param name="host">The host builder.</param>
        /// <returns>The configured host builder for method chaining.</returns>
        /// <remarks>
        /// Log files: <c>logs/{assembly-name}-{date}.log</c> with daily rolling and 7-day retention.
        /// See README for detailed configuration.
        /// </remarks>
        public static IHostBuilder AddSerilogDefaults(this IHostBuilder host)
        {
            return host.UseSerilog((context, configuration) =>
            {
                // Generate log path from assembly name
                string assemblyName = context.HostingEnvironment.ApplicationName;
#pragma warning disable CA1308 // Lowercase is appropriate for file paths
                string sanitizedName = assemblyName.ToLowerInvariant().Replace(".", "-", StringComparison.Ordinal);
#pragma warning restore CA1308
                string logPath = $"logs/{sanitizedName}-";

                configuration
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
                    .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                    .WriteTo.Console(
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                    .WriteTo.File(
                        path: $"{logPath}.log",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

                // Development: Include debug logs for better diagnostics
                if (context.HostingEnvironment.IsDevelopment())
                {
                    configuration.MinimumLevel.Debug();
                }
            }, writeToProviders: true); // CRITICAL: writeToProviders:true sends logs to Microsoft.Extensions.Logging (OpenTelemetry)
        }
    }
}
