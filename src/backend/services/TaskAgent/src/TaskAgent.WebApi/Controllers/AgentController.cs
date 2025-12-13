using System.ClientModel;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using TaskAgent.Application.DTOs.Requests;
using TaskAgent.Infrastructure.Services;
using TaskAgent.WebApi.Constants;
using TaskAgent.WebApi.Services;

namespace TaskAgent.WebApi.Controllers;

/// <summary>
/// Custom AG-UI streaming endpoint with serialized state support for conversation continuity.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly SseStreamingService _streamingService;
    private readonly ILogger<AgentController> _logger;

    public AgentController(
        SseStreamingService streamingService,
        ILogger<AgentController> logger
    )
    {
        _streamingService = streamingService;
        _logger = logger;
    }

    /// <summary>
    /// Streams agent responses with Server-Sent Events (SSE), including thread state.
    /// </summary>
    [HttpPost("chat")]
    public async Task ChatAsync(
        [FromBody] AgentRequest request,
        CancellationToken cancellationToken
    )
    {
        // Configure SSE headers before any response is written
        ConfigureSseResponse();
        
        // Capture serializedState for conversation continuity
        string? serializedState = request.SerializedState;
        
        try
        {
            // Convert request messages to ChatMessage list
            List<ChatMessage> messages =
                request.Messages?.Select(MapToChatMessage).ToList() ?? [];

            // Stream response using the service
            await _streamingService.StreamToResponseAsync(
                Response,
                messages,
                serializedState,
                cancellationToken
            );
        }
        catch (ContentFilterException ex)
        {
            // Content filter exception - send as CONTENT_FILTER event (UI only, not persisted)
            _logger.LogWarning(ex, "Content filter triggered");
            await HandleContentFilterAsync(serializedState, cancellationToken);
        }
        catch (ClientResultException ex) when (IsContentFilterError(ex))
        {
            // Azure OpenAI content filter - send as CONTENT_FILTER event (UI only, not persisted)
            _logger.LogWarning(ex, "Azure OpenAI content filter triggered");
            await HandleContentFilterAsync(serializedState, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing agent request");
            await WriteErrorEventAsync(ex.Message, cancellationToken);
            await WriteDoneEventAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Handles content filter by sending SSE events for UI display.
    /// Note: Content-filtered messages are displayed in chat but NOT persisted to avoid
    /// state inconsistencies between AgentThread and PostgreSQL storage systems.
    /// This ensures conversation continuity for subsequent valid messages.
    /// </summary>
    private async Task HandleContentFilterAsync(
        string? serializedState,
        CancellationToken cancellationToken)
    {
        // Send content filter event (immediate UI feedback)
        await WriteContentFilterEventAsync(cancellationToken);
        
        // Return the original serializedState to maintain conversation continuity
        // The content filter message is displayed in frontend but not persisted
        // This avoids state inconsistency between AgentThread and DB storage
        await WriteThreadStateEventAsync(serializedState, cancellationToken);
        await WriteDoneEventAsync(cancellationToken);
    }

    private void ConfigureSseResponse()
    {
        Response.ContentType = AgentConstants.SSE_CONTENT_TYPE;
        Response.Headers.Append("Cache-Control", AgentConstants.SSE_CACHE_CONTROL);
        Response.Headers.Append("Connection", AgentConstants.SSE_CONNECTION);
    }

    private static ChatMessage MapToChatMessage(AgentMessage message)
    {
        ChatRole role =
            message.Role?.ToUpperInvariant() == AgentConstants.USER_ROLE
                ? ChatRole.User
                : ChatRole.Assistant;

        return new ChatMessage(role, new List<AIContent> { new TextContent(message.Content ?? "") });
    }

    private async Task WriteContentFilterEventAsync(CancellationToken cancellationToken)
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

        await Response.WriteAsync($"data: {contentFilterEvent}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    private async Task WriteThreadStateEventAsync(string? serializedState, CancellationToken cancellationToken)
    {
        // Only send thread state if we have one (conversation continuity)
        if (string.IsNullOrEmpty(serializedState))
        {
            return;
        }

        string threadStateEvent = JsonSerializer.Serialize(
            new
            {
                type = AgentConstants.EVENT_THREAD_STATE,
                serializedState
            }
        );

        await Response.WriteAsync($"data: {threadStateEvent}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    private async Task WriteErrorEventAsync(string errorMessage, CancellationToken cancellationToken)
    {
        string errorEvent = JsonSerializer.Serialize(
            new
            {
                type = AgentConstants.EVENT_RUN_ERROR,
                error = AgentConstants.ERROR_AGENT_ERROR,
                message = errorMessage,
            }
        );

        await Response.WriteAsync($"data: {errorEvent}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    private async Task WriteDoneEventAsync(CancellationToken cancellationToken)
    {
        await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if the exception is an Azure OpenAI content filter error.
    /// </summary>
    private static bool IsContentFilterError(ClientResultException ex)
    {
        return ex.Status == 400 && 
               (ex.Message.Contains("content_filter", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("content management policy", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("content filtering", StringComparison.OrdinalIgnoreCase));
    }
}
