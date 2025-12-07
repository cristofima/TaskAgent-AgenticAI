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
    private readonly FunctionDescriptionProvider _descriptionProvider;

    public SseStreamingService(
        IAgentStreamingService agentStreamingService,
        FunctionDescriptionProvider descriptionProvider)
    {
        _agentStreamingService = agentStreamingService;
        _descriptionProvider = descriptionProvider;
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

        // Send initial status - indicate if loading history or processing new request
        bool hasExistingConversation = !string.IsNullOrEmpty(serializedState);
        string initialStatus = hasExistingConversation 
            ? AgentConstants.STATUS_LOADING_HISTORY 
            : AgentConstants.STATUS_PROCESSING_REQUEST;
        await SendStatusUpdateAsync(response, initialStatus, cancellationToken);

        // Convert to object enumerable for interface
        IEnumerable<object> messageObjects = messages;

        // Track active function call for STEP_FINISHED event
        string? activeStepName = null;

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

            // Check for function calls and send STEP_STARTED + STATUS_UPDATE
            string? functionName = await SendFunctionStatusIfNeededAsync(response, update, cancellationToken);
            if (functionName != null)
            {
                activeStepName = functionName;
            }

            // Check for function result and send STEP_FINISHED
            if (activeStepName != null && HasFunctionResult(update))
            {
                await SendStepFinishedAsync(response, activeStepName, cancellationToken);
                activeStepName = null;
            }

            string eventJson = SerializeEvent(update);
            await WriteEventAsync(response, eventJson, cancellationToken);
        }

        // Check for content filter exception
        object? contentFilterEx = _agentStreamingService.GetContentFilterException();
        if (contentFilterEx != null)
        {
            await SendContentFilterEventAsync(response, cancellationToken);
            // Still send thread state for conversation continuity
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

    /// <summary>
    /// Checks if the update contains a function result.
    /// </summary>
    private static bool HasFunctionResult(AgentRunResponseUpdate update)
    {
        return update.Contents.OfType<FunctionResultContent>().Any();
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

    /// <summary>
    /// Sends a content filter event with a ChatGPT-like user-friendly message.
    /// </summary>
    private static async Task SendContentFilterEventAsync(
        HttpResponse response,
        CancellationToken cancellationToken
    )
    {
        string contentFilterEvent = JsonSerializer.Serialize(
            new
            {
                type = AgentConstants.EVENT_CONTENT_FILTER,
                error = AgentConstants.ERROR_CONTENT_FILTER,
                message = AgentConstants.CONTENT_FILTER_MESSAGE,
                messageId = $"cf-{Guid.NewGuid():N}",
            }
        );

        await WriteEventAsync(response, contentFilterEvent, cancellationToken);
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

    /// <summary>
    /// Sends a status update event to inform the client about current processing stage.
    /// </summary>
    private static async Task SendStatusUpdateAsync(
        HttpResponse response,
        string status,
        CancellationToken cancellationToken
    )
    {
        string statusEvent = JsonSerializer.Serialize(
            new { type = AgentConstants.EVENT_STATUS_UPDATE, status }
        );
        await WriteEventAsync(response, statusEvent, cancellationToken);
    }

    /// <summary>
    /// Sends a status update if the current update is a function call, with a message specific to the function being called.
    /// Uses AG-UI standard STEP_STARTED event and dynamic status messages from function descriptions.
    /// </summary>
    /// <returns>The function name if a function call was detected, null otherwise.</returns>
    private async Task<string?> SendFunctionStatusIfNeededAsync(
        HttpResponse response,
        AgentRunResponseUpdate update,
        CancellationToken cancellationToken
    )
    {
        FunctionCallContent? functionCall = update.Contents.OfType<FunctionCallContent>().FirstOrDefault();
        if (functionCall == null)
        {
            return null;
        }

        string functionName = functionCall.Name ?? "Unknown";

        // Send AG-UI standard STEP_STARTED event
        await SendStepStartedAsync(response, functionName, cancellationToken);

        // Send dynamic status message derived from [Description] attribute
        string status = _descriptionProvider.GetStatusMessage(functionName);
        await SendStatusUpdateAsync(response, status, cancellationToken);

        return functionName;
    }

    /// <summary>
    /// Sends an AG-UI standard STEP_STARTED event.
    /// </summary>
    private static async Task SendStepStartedAsync(
        HttpResponse response,
        string stepName,
        CancellationToken cancellationToken
    )
    {
        string stepEvent = JsonSerializer.Serialize(
            new { type = AgentConstants.EVENT_STEP_STARTED, stepName }
        );
        await WriteEventAsync(response, stepEvent, cancellationToken);
    }

    /// <summary>
    /// Sends an AG-UI standard STEP_FINISHED event.
    /// </summary>
    private static async Task SendStepFinishedAsync(
        HttpResponse response,
        string stepName,
        CancellationToken cancellationToken
    )
    {
        string stepEvent = JsonSerializer.Serialize(
            new { type = AgentConstants.EVENT_STEP_FINISHED, stepName }
        );
        await WriteEventAsync(response, stepEvent, cancellationToken);
    }
}
