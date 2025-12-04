using Microsoft.AspNetCore.Mvc;
using TaskAgent.Application.DTOs.Responses;
using TaskAgent.Application.Interfaces;
using TaskAgent.Application.Validators;
using TaskAgent.WebApi.Services;

namespace TaskAgent.WebApi.Controllers;

/// <summary>
/// Provides REST API for conversation management: list, retrieve history, and delete threads.
/// </summary>
/// <remarks>
/// Message sending handled by AG-UI protocol endpoint (<c>/agui</c>).
/// </remarks>
[ApiController]
[Route("api/conversations")]
[Produces("application/json")]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(
        IConversationService conversationService,
        ILogger<ConversationsController> logger
    )
    {
        _conversationService = conversationService;
        _logger = logger;
    }

    /// <summary>
    /// Lists conversation threads with pagination and sorting.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ListThreadsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListThreadsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "UpdatedAt",
        [FromQuery] string sortOrder = "desc",
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // Validate pagination parameters
            string? validationError = PaginationValidator.ValidatePagination(page, pageSize);
            if (validationError != null)
            {
                return ErrorResponseFactory.CreateBadRequest("InvalidParameters", validationError);
            }

            ListThreadsResponseDto response = await _conversationService.ListThreadsAsync(
                page,
                pageSize,
                sortBy,
                sortOrder,
                cancellationToken
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing conversation threads");
            return ErrorResponseFactory.CreateInternalServerError(
                "Failed to list chats",
                ex.Message
            );
        }
    }

    /// <summary>
    /// Gets paginated conversation history for a specific thread.
    /// </summary>
    [HttpGet("{threadId}/messages")]
    [ProducesResponseType(typeof(GetConversationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetConversationHistoryAsync(
        [FromRoute] string threadId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // Validate thread ID
            string? threadIdError = PaginationValidator.ValidateThreadId(threadId);
            if (threadIdError != null)
            {
                return ErrorResponseFactory.CreateBadRequest("InvalidThreadId", threadIdError);
            }

            // Validate pagination
            string? paginationError = PaginationValidator.ValidatePagination(page, pageSize);
            if (paginationError != null)
            {
                return ErrorResponseFactory.CreateBadRequest("InvalidParameters", paginationError);
            }

            GetConversationResponseDto? response =
                await _conversationService.GetConversationHistoryAsync(
                    threadId,
                    page,
                    pageSize,
                    cancellationToken
                );

            if (response == null)
            {
                return NotFound(
                    new ErrorResponseDto
                    {
                        Error = "ThreadNotFound",
                        Message = $"Thread '{threadId}' not found or is inactive",
                    }
                );
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation history for thread {ThreadId}", threadId);
            return ErrorResponseFactory.CreateInternalServerError(
                "Failed to get conversation history",
                ex.Message
            );
        }
    }

    /// <summary>
    /// Deletes a conversation thread and all its messages.
    /// </summary>
    [HttpDelete("{threadId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteThreadAsync(
        [FromRoute] string threadId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // Validate thread ID
            string? threadIdError = PaginationValidator.ValidateThreadId(threadId);
            if (threadIdError != null)
            {
                return ErrorResponseFactory.CreateBadRequest("InvalidThreadId", threadIdError);
            }

            bool deleted = await _conversationService.DeleteThreadAsync(threadId, cancellationToken);

            if (!deleted)
            {
                return NotFound(
                    new ErrorResponseDto
                    {
                        Error = "ThreadNotFound",
                        Message = $"Thread '{threadId}' not found or is inactive",
                    }
                );
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting conversation thread {ThreadId}", threadId);
            return ErrorResponseFactory.CreateInternalServerError(
                "Failed to delete conversation",
                ex.Message
            );
        }
    }
}
