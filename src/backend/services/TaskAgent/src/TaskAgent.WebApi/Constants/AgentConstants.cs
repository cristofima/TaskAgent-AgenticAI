namespace TaskAgent.WebApi.Constants;

/// <summary>
/// Constants for Agent API and SSE event types
/// </summary>
public static class AgentConstants
{
    /// <summary>
    /// SSE Content-Type header value
    /// </summary>
    public const string SSE_CONTENT_TYPE = "text/event-stream";

    /// <summary>
    /// SSE Cache-Control header value
    /// </summary>
    public const string SSE_CACHE_CONTROL = "no-cache";

    /// <summary>
    /// SSE Connection header value
    /// </summary>
    public const string SSE_CONNECTION = "keep-alive";

    /// <summary>
    /// User role identifier for message parsing
    /// </summary>
    public const string USER_ROLE = "USER";

    /// <summary>
    /// Custom SSE event type for thread state serialization
    /// </summary>
    public const string EVENT_THREAD_STATE = "THREAD_STATE";

    /// <summary>
    /// SSE event type for run errors
    /// </summary>
    public const string EVENT_RUN_ERROR = "RUN_ERROR";

    /// <summary>
    /// Error type identifier for agent errors
    /// </summary>
    public const string ERROR_AGENT_ERROR = "AgentError";

    /// <summary>
    /// SSE event type for text message content
    /// </summary>
    public const string EVENT_TEXT_MESSAGE_CONTENT = "TEXT_MESSAGE_CONTENT";

    /// <summary>
    /// SSE event type for tool call start
    /// </summary>
    public const string EVENT_TOOL_CALL_START = "TOOL_CALL_START";

    /// <summary>
    /// SSE event type for tool call result
    /// </summary>
    public const string EVENT_TOOL_CALL_RESULT = "TOOL_CALL_RESULT";

    /// <summary>
    /// SSE event type for generic run updates
    /// </summary>
    public const string EVENT_RUN_UPDATE = "RUN_UPDATE";

    /// <summary>
    /// SSE event type for content filter violations
    /// </summary>
    public const string EVENT_CONTENT_FILTER = "CONTENT_FILTER";

    /// <summary>
    /// Error code for content filter violations
    /// </summary>
    public const string ERROR_CONTENT_FILTER = "content_filter";

    /// <summary>
    /// Default message for content filter violations (ChatGPT-like)
    /// </summary>
    public const string CONTENT_FILTER_MESSAGE = "I'm unable to assist with that request as it may violate content policies. Please try rephrasing your message or asking something else.";
}
