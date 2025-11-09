using System.Diagnostics.Metrics;

namespace TaskAgent.Application.Telemetry;

/// <summary>
/// Custom metrics for AI Agent operations
/// </summary>
public sealed class AgentMetrics : IDisposable
{
    private const string MeterName = "TaskAgent.Agent";
    private readonly Meter _meter;
    private readonly Counter<long> _requestsCounter;
    private readonly Counter<long> _functionCallsCounter;
    private readonly Counter<long> _errorsCounter;
    private readonly Histogram<double> _responseDurationHistogram;
    private bool _disposed;

    public AgentMetrics()
    {
        _meter = new Meter(MeterName, "1.0.0");

        // Counter: Total requests to the agent
        _requestsCounter = _meter.CreateCounter<long>(
            name: "agent.requests",
            unit: "requests",
            description: "Total number of requests sent to the AI agent"
        );

        // Counter: Total function calls made by the agent
        _functionCallsCounter = _meter.CreateCounter<long>(
            name: "agent.function_calls",
            unit: "calls",
            description: "Total number of function tool calls made by the agent"
        );

        // Counter: Total errors
        _errorsCounter = _meter.CreateCounter<long>(
            name: "agent.errors",
            unit: "errors",
            description: "Total number of errors encountered by the agent"
        );

        // Histogram: Response duration
        _responseDurationHistogram = _meter.CreateHistogram<double>(
            name: "agent.response.duration",
            unit: "ms",
            description: "Response time for agent requests in milliseconds"
        );
    }

    /// <summary>
    /// Record a new agent request
    /// </summary>
    public void RecordRequest(string threadId, string status = "success")
    {
        _requestsCounter.Add(
            1,
            new KeyValuePair<string, object?>("thread_id", threadId),
            new KeyValuePair<string, object?>("status", status)
        );
    }

    /// <summary>
    /// Record a function call by the agent
    /// </summary>
    public void RecordFunctionCall(string functionName, string status = "success")
    {
        _functionCallsCounter.Add(
            1,
            new KeyValuePair<string, object?>("function_name", functionName),
            new KeyValuePair<string, object?>("status", status)
        );
    }

    /// <summary>
    /// Record an error
    /// </summary>
    public void RecordError(string errorType, string? message = null)
    {
        _errorsCounter.Add(
            1,
            new KeyValuePair<string, object?>("error_type", errorType),
            new KeyValuePair<string, object?>("message", message ?? "Unknown error")
        );
    }

    /// <summary>
    /// Record response duration
    /// </summary>
    public void RecordResponseDuration(double durationMs, string threadId, bool success = true)
    {
        _responseDurationHistogram.Record(
            durationMs,
            new KeyValuePair<string, object?>("thread_id", threadId),
            new KeyValuePair<string, object?>("success", success)
        );
    }

    /// <summary>
    /// Dispose the meter
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _meter?.Dispose();
        }

        _disposed = true;
    }
}
