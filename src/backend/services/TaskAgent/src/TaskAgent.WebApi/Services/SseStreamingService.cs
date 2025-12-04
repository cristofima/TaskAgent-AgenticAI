using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using TaskAgent.Application.Interfaces;
using TaskAgent.WebApi.Constants;

namespace TaskAgent.WebApi.Services;

/// <summary>
/// Service for streaming agent responses using Server-Sent Events (SSE).
/// </summary>
public class SseStreamingService
{
    private readonly IAgentStreamingService _agentStreamingService;

    public SseStreamingService(IAgentStreamingService agentStreamingService)
    {
        _agentStreamingService = agentStreamingService;
    }

    /// <summary>
    /// Streams agent responses to the HTTP response using SSE protocol.
    /// </summary>
    public async Task StreamToResponseAsync(
        HttpResponse response,
        IEnumerable<ChatMessage> messages,
        string? serializedState,
        CancellationToken cancellationToken
    )
    {
        // Configure response for SSE
        ConfigureSseResponse(response);

        // Convert to object enumerable for interface
        IEnumerable<object> messageObjects = messages;

        // Stream agent responses
        await foreach (
            object updateObj in _agentStreamingService.StreamResponseAsync(
                messageObjects,
                serializedState,
                cancellationToken
            )
        )
        {
            // Cast back to AgentRunResponseUpdate
            if (updateObj is not AgentRunResponseUpdate update)
            {
                continue;
            }

            string eventJson = SerializeEvent(update);
            await WriteEventAsync(response, eventJson, cancellationToken);
        }

        // Get the thread after streaming completes
        object? threadObj = _agentStreamingService.GetCurrentThread();
            
        if (threadObj != null)
        {
            // Send thread state event
            await SendThreadStateEventAsync(response, threadObj, cancellationToken);
        }

        // Send completion event
        await WriteDoneEventAsync(response, cancellationToken);
    }

    private static void ConfigureSseResponse(HttpResponse response)
    {
        response.ContentType = AgentConstants.SSE_CONTENT_TYPE;
        response.Headers.Append("Cache-Control", AgentConstants.SSE_CACHE_CONTROL);
        response.Headers.Append("Connection", AgentConstants.SSE_CONNECTION);
    }

    private static string SerializeEvent(AgentRunResponseUpdate update)
    {
        return JsonSerializer.Serialize(
            new
            {
                type = AgentEventMapper.GetEventType(update),
                role = update.Role?.ToString(),
                delta = AgentEventMapper.GetTextContent(update),
                messageId = update.MessageId,
            }
        );
    }

    private async Task SendThreadStateEventAsync(
        HttpResponse response,
        object thread,
        CancellationToken cancellationToken
    )
    {
        string serializedState = _agentStreamingService.GetSerializedState(thread);

        string threadStateEvent = JsonSerializer.Serialize(
            new { type = AgentConstants.EVENT_THREAD_STATE, serializedState }
        );

        await WriteEventAsync(response, threadStateEvent, cancellationToken);
    }

    private static async Task WriteEventAsync(
        HttpResponse response,
        string eventJson,
        CancellationToken cancellationToken
    )
    {
        await response.WriteAsync($"data: {eventJson}\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }

    private static async Task WriteDoneEventAsync(
        HttpResponse response,
        CancellationToken cancellationToken
    )
    {
        await response.WriteAsync("data: [DONE]\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}
