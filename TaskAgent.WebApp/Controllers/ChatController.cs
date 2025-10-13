using Microsoft.AspNetCore.Mvc;
using TaskAgent.Application.DTOs;
using TaskAgent.Application.Interfaces;

namespace TaskAgent.WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ITaskAgentService _taskAgent;

    public ChatController(ITaskAgentService taskAgent)
    {
        _taskAgent = taskAgent;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Message cannot be empty" });
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
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("new-thread")]
    public IActionResult NewThread()
    {
        var threadId = _taskAgent.GetNewThreadId();
        return Ok(new { threadId });
    }
}
