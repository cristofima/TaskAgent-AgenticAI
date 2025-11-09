using System.Diagnostics;

namespace TaskAgent.Application.Telemetry;

/// <summary>
/// Custom activity source for AI Agent distributed tracing
/// </summary>
public static class AgentActivitySource
{
    private const string ActivitySourceName = "TaskAgent.Agent";

    /// <summary>
    /// Activity source for creating custom spans
    /// </summary>
    private static readonly ActivitySource _source = new(ActivitySourceName, "1.0.0");

    /// <summary>
    /// Start a new activity for a function call
    /// </summary>
    public static Activity? StartFunctionActivity(
        string functionName,
        IDictionary<string, object?>? tags = null
    )
    {
        Activity? activity = _source.StartActivity($"Function.{functionName}");

        if (activity == null)
        {
            return activity;
        }

        activity.SetTag("function.name", functionName);
        activity.SetTag("agent.operation", "function_call");

        if (tags == null)
        {
            return activity;
        }

        foreach (KeyValuePair<string, object?> tag in tags)
        {
            activity.SetTag(tag.Key, tag.Value);
        }

        return activity;
    }

    /// <summary>
    /// Start a new activity for agent message processing
    /// </summary>
    public static Activity? StartMessageActivity(string threadId, string message)
    {
        Activity? activity = _source.StartActivity("Agent.ProcessMessage", ActivityKind.Server);

        if (activity == null)
        {
            return activity;
        }

        activity.SetTag("thread.id", threadId);
        activity.SetTag("message.length", message.Length);
        activity.SetTag("agent.operation", "process_message");

        return activity;
    }
}
