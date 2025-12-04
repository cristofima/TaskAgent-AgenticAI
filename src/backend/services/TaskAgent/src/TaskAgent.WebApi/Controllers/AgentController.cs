using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using TaskAgent.Application.DTOs.Requests;
using TaskAgent.WebApi.Constants;
using TaskAgent.WebApi.Services;

namespace TaskAgent.WebApi.Controllers;

/// <summary>
/// Custom AG-UI streaming endpoint with serialized state support for conversation continuity.
/// </summary>
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
        try
        {
            // Convert request messages to ChatMessage list
            List<ChatMessage> messages =
                request.Messages?.Select(MapToChatMessage).ToList() ?? [];

            // Stream response using the service
            await _streamingService.StreamToResponseAsync(
                Response,
                messages,
                request.SerializedState,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing agent request");
            await WriteErrorEventAsync(ex.Message, cancellationToken);
        }
    }

    private static ChatMessage MapToChatMessage(AgentMessage message)
    {
        ChatRole role =
            message.Role?.ToUpperInvariant() == AgentConstants.USER_ROLE
                ? ChatRole.User
                : ChatRole.Assistant;

        return new ChatMessage(role, new List<AIContent> { new TextContent(message.Content ?? "") });
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
    }
}
