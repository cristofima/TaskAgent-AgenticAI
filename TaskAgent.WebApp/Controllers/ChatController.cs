using Microsoft.AspNetCore.Mvc;
using TaskAgent.Application.DTOs;
using TaskAgent.Application.Interfaces;
using TaskAgent.WebApp.Services;

namespace TaskAgent.WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ITaskAgentService _taskAgent;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        ITaskAgentService taskAgent,
        ILogger<ChatController> logger)
    {
        _taskAgent = taskAgent ?? throw new ArgumentNullException(nameof(taskAgent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return ErrorResponseFactory.CreateBadRequest("InvalidInput", "Message cannot be empty");
        }

        try
        {
            var response = await _taskAgent.SendMessageAsync(request.Message, request.ThreadId);
            var threadId = string.IsNullOrWhiteSpace(request.ThreadId)
                ? _taskAgent.GetNewThreadId()
                : request.ThreadId;

            return Ok(new ChatResponse { Message = response, ThreadId = threadId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return ErrorResponseFactory.CreateInternalServerError(
                "An error occurred while processing your message. Please try again.",
                new { exception = ex.Message }
            );
        }
    }

    [HttpPost("new-thread")]
    public IActionResult NewThread()
    {
        try
        {
            var threadId = _taskAgent.GetNewThreadId();
            return Ok(new { threadId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating new thread");
            return ErrorResponseFactory.CreateInternalServerError(
                "An error occurred while creating a new conversation thread."
            );
        }
    }
}
