using Microsoft.AspNetCore.Mvc;
using TaskAgent.Application.DTOs;
using TaskAgent.Application.Interfaces;
using TaskAgent.WebApp.Constants;
using TaskAgent.WebApp.Models;
using TaskAgent.WebApp.Services;

namespace TaskAgent.WebApp.Controllers;

/// <summary>
/// Controller for chat interactions with the AI Agent
/// </summary>
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

    /// <summary>
    /// Send a message to the AI agent and receive a response
    /// </summary>
    /// <param name="request">Chat request with message and optional thread ID</param>
    /// <returns>Chat response with message, thread ID, and metadata</returns>
    [HttpPost("send")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
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
            // Send message to agent - service handles thread management and returns enriched response
            ChatResponse response = await _taskAgent.SendMessageAsync(
                request.Message,
                request.ThreadId
            );

            return Ok(response);
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

    /// <summary>
    /// Create a new conversation thread
    /// </summary>
    /// <returns>New thread ID</returns>
    [HttpPost("threads")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult CreateNewThread()
    {
        try
        {
            string threadId = _taskAgent.GetNewThreadId();
            return Ok(new { threadId, createdAt = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating new thread");
            return ErrorResponseFactory.CreateInternalServerError(
                ErrorMessages.THREAD_CREATION_ERROR
            );
        }
    }

    /// <summary>
    /// List all conversation threads with pagination
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of threads per page</param>
    /// <param name="sortBy">Sort by field (CreatedAt or UpdatedAt)</param>
    /// <param name="sortOrder">Sort order (asc or desc)</param>
    /// <param name="isActive">Filter by active status</param>
    /// <returns>Paginated list of conversation threads</returns>
    [HttpGet("threads")]
    [ProducesResponseType(typeof(ListThreadsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListThreadsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "UpdatedAt",
        [FromQuery] string sortOrder = "desc",
        [FromQuery] bool? isActive = null
    )
    {
        try
        {
            var request = new ListThreadsRequest
            {
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortOrder = sortOrder,
                IsActive = isActive,
            };

            ListThreadsResponse response = await _taskAgent.ListThreadsAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing threads");
            return ErrorResponseFactory.CreateInternalServerError("Error listing threads");
        }
    }

    /// <summary>
    /// Get conversation history for a specific thread
    /// </summary>
    /// <param name="threadId">Thread ID to retrieve messages from</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of messages per page</param>
    /// <returns>Paginated conversation history</returns>
    [HttpGet("threads/{threadId}/messages")]
    [ProducesResponseType(typeof(GetConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetConversationHistoryAsync(
        [FromRoute] string threadId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50
    )
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            return ErrorResponseFactory.CreateBadRequest(
                ErrorCodes.INVALID_INPUT,
                "Thread ID is required"
            );
        }

        try
        {
            var request = new GetConversationRequest
            {
                ThreadId = threadId,
                Page = page,
                PageSize = pageSize,
            };

            GetConversationResponse response = await _taskAgent.GetConversationHistoryAsync(
                request
            );

            if (response.TotalCount == 0)
            {
                return NotFound(
                    new ErrorResponse
                    {
                        Error = "Thread not found",
                        Message = $"Thread with ID {threadId} was not found",
                    }
                );
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving conversation history for thread {ThreadId}",
                threadId
            );
            return ErrorResponseFactory.CreateInternalServerError(
                "Error retrieving conversation history"
            );
        }
    }

    /// <summary>
    /// Delete a conversation thread
    /// </summary>
    /// <param name="threadId">Thread ID to delete</param>
    /// <returns>Success response</returns>
    [HttpDelete("threads/{threadId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteThreadAsync([FromRoute] string threadId)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            return ErrorResponseFactory.CreateBadRequest(
                ErrorCodes.INVALID_INPUT,
                "Thread ID is required"
            );
        }

        try
        {
            await _taskAgent.DeleteThreadAsync(threadId);
            _logger.LogInformation("Deleted thread {ThreadId}", threadId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting thread {ThreadId}", threadId);
            return ErrorResponseFactory.CreateInternalServerError(
                "Error deleting thread",
                new { threadId }
            );
        }
    }
}
