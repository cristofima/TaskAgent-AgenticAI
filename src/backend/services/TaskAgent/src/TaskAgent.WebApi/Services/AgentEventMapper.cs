using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using TaskAgent.WebApi.Constants;

namespace TaskAgent.WebApi.Services;

/// <summary>
/// Helper service for mapping agent response updates to SSE event types.
/// </summary>
public static class AgentEventMapper
{
    /// <summary>
    /// Determines the event type based on the agent response update.
    /// </summary>
    public static string GetEventType(AgentRunResponseUpdate update)
    {
        if (update.Contents.Any(c => c is TextContent))
        {
            return AgentConstants.EVENT_TEXT_MESSAGE_CONTENT;
        }

        if (update.Contents.Any(c => c is FunctionCallContent))
        {
            return AgentConstants.EVENT_TOOL_CALL_START;
        }

        if (update.Contents.Any(c => c is FunctionResultContent))
        {
            return AgentConstants.EVENT_TOOL_CALL_RESULT;
        }

        return AgentConstants.EVENT_RUN_UPDATE;
    }

    /// <summary>
    /// Extracts text content from the agent response update.
    /// </summary>
    public static string? GetTextContent(AgentRunResponseUpdate update)
    {
        TextContent? textContent = update.Contents.OfType<TextContent>().FirstOrDefault();
        return textContent?.Text;
    }
}
