# TaskAgent.ServiceDefaults

Shared .NET Aspire service defaults library that provides consistent observability, resilience, and infrastructure patterns across all services in the TaskAgent solution.

## Overview

This project centralizes common Aspire configurations that should be applied to every service:

- **OpenTelemetry** - Metrics, traces, and logs instrumentation
- **Service Discovery** - Automatic service resolution with DNS refresh
- **Resilience** - HTTP client retry, circuit breaker, and timeout policies
- **Health Checks** - `/health` and `/alive` endpoints for orchestration
- **Serilog** - Centralized logging with console and file sinks

## Usage

Reference this project from your service's `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add Serilog (optional)
builder.Host.AddSerilogDefaults();

var app = builder.Build();

// Map health check endpoints (development only)
app.MapDefaultEndpoints();

app.Run();
```

## Telemetry Architecture

The telemetry exporter is automatically selected based on environment configuration:

| Environment | Configuration | Exporter | Destination |
|-------------|---------------|----------|-------------|
| **Development** | `OTEL_EXPORTER_OTLP_ENDPOINT` set | OTLP | Aspire Dashboard (https://localhost:17198) |
| **Production** | `APPLICATIONINSIGHTS_CONNECTION_STRING` set | Azure Monitor | Application Insights |

### OpenTelemetry Features

**Metrics:**
- .NET request metrics
- HTTP client metrics
- .NET Runtime metrics
- Custom meters: `TaskAgent.Agent`, `TaskAgent.Functions`

**Traces:**
- .NET request tracing
- HTTP client tracing
- Entity Framework Core SQL command tracing
- Custom activity sources: `TaskAgent.Agent`, `TaskAgent.Functions`
- Health check requests excluded from tracing

**Logs:**
- Integration with `Microsoft.Extensions.Logging`
- Structured logging support via OpenTelemetry
- Serilog enrichment with context properties

## Service Discovery

Configured for automatic service resolution with:

- Standard resilience handler (retry, circuit breaker, timeout)
- DNS refresh handling via `IHttpClientFactory`
- **Development**: Allows both HTTP and HTTPS schemes
- **Production**: Restricted to HTTPS only for security

## Health Checks

Two endpoints are exposed (development only):

- **`/health`** - All registered health checks must pass (readiness)
- **`/alive`** - Only "live" tagged checks must pass (liveness)

Default check: `self` check always returns healthy.

> ⚠️ **Security Note**: Health check endpoints are disabled in production by default. See [Aspire health checks documentation](https://aka.ms/dotnet/aspire/healthchecks) before enabling in non-development environments.

## Serilog Configuration

The `AddSerilogDefaults()` extension configures Serilog with:

### Sinks

1. **Console Sink**
   - Format: `[HH:mm:ss LEVEL] Message {Properties}`
   - Ideal for development and container logs

2. **File Sink**
   - Path: `logs/{assembly-name}-{date}.log`
   - Example: `logs/taskagent-webapi-20251127.log`
   - Rolling: Daily
   - Retention: 7 days

### Log Levels

| Namespace | Minimum Level |
|-----------|---------------|
| Default | `Information` |
| `Microsoft` | `Warning` |
| `Microsoft.Hosting.Lifetime` | `Information` |
| **Development only** | `Debug` |

### Enrichment

All log entries are enriched with:
- `Application` - Assembly name
- `Environment` - Environment name (Development, Production, etc.)
- Context properties via `FromLogContext()`

### OpenTelemetry Integration

Serilog integrates with OpenTelemetry when configured with `writeToProviders: true`. This enables:

- Structured logs in Aspire Dashboard
- Log correlation with traces and metrics
- Unified observability across all three pillars

## Extension Methods

### `AddServiceDefaults<TBuilder>()`

Configures all service defaults (OpenTelemetry, service discovery, resilience, health checks).

**Returns:** The builder for method chaining.

### `MapDefaultEndpoints()`

Maps health check endpoints (`/health`, `/alive`) in development environments only.

**Returns:** The `WebApplication` for method chaining.

### `AddSerilogDefaults()`

Configures Serilog with console and file sinks, integrated with OpenTelemetry.

**Returns:** The `IHostBuilder` for method chaining.

## Custom Telemetry

To add custom metrics or traces in your services:

```csharp
// Custom Meter (metrics)
var meter = new Meter("TaskAgent.Agent");
var counter = meter.CreateCounter<int>("agent.requests");
counter.Add(1);

// Custom ActivitySource (traces)
var activitySource = new ActivitySource("TaskAgent.Agent");
using var activity = activitySource.StartActivity("ProcessMessage");
activity?.SetTag("thread.id", threadId);
```

The service defaults automatically register these custom sources for export.

## References

- [.NET Aspire Service Defaults](https://aka.ms/dotnet/aspire/service-defaults)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)
- [Serilog Documentation](https://serilog.net/)
- [Azure Monitor OpenTelemetry](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore)
