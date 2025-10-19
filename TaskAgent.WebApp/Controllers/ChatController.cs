using Microsoft.AspNetCore.Mvc;
using TaskAgent.Application.DTOs;
using TaskAgent.Application.Interfaces;
using TaskAgent.WebApp.Constants;
using TaskAgent.WebApp.Models;
using TaskAgent.WebApp.Services;

namespace TaskAgent.WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ITaskAgentService _taskAgent;
    private readonly ILogger<ChatController> _logger;

    public ChatController(ITaskAgentService taskAgent, ILogger<ChatController> logger)
    {
        _taskAgent = taskAgent ?? throw new ArgumentNullException(nameof(taskAgent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessageAsync([FromBody] ChatRequest? request)
    {
        // Try to get the request from HttpContext.Items first (set by middleware)
        if (
            HttpContext.Items.TryGetValue("ChatRequest", out object? storedRequest)
            && storedRequest is ChatRequestDto chatRequestDto
        )
        {
            request = new ChatRequest
            {
                Message = chatRequestDto.Message ?? string.Empty,
                ThreadId = chatRequestDto.ThreadId,
            };
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            return ErrorResponseFactory.CreateBadRequest(
                ErrorCodes.INVALID_INPUT,
                ErrorMessages.MESSAGE_EMPTY
            );
        }

        try
        {
            // Send message to agent - service handles thread management
            (string response, string activeThreadId) = await _taskAgent.SendMessageAsync(
                request.Message,
                request.ThreadId
            );

            return Ok(new ChatResponse { Message = response, ThreadId = activeThreadId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return ErrorResponseFactory.CreateInternalServerError(
                ErrorMessages.PROCESSING_ERROR,
                new { exception = ex.Message }
            );
        }
    }

    [HttpPost("new-thread")]
    public IActionResult NewThread()
    {
        try
        {
            string threadId = _taskAgent.GetNewThreadId();
            return Ok(new { threadId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating new thread");
            return ErrorResponseFactory.CreateInternalServerError(
                ErrorMessages.THREAD_CREATION_ERROR
            );
        }
    }
}
