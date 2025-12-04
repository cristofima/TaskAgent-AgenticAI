using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskAgent.Application.DTOs.Responses;
using TaskAgent.Application.Interfaces;
using TaskAgent.Domain.Entities;
using TaskAgent.Infrastructure.Data;

namespace TaskAgent.Infrastructure.Services;

/// <summary>
/// Service for managing conversation threads and messages.
/// </summary>
public class ConversationService : IConversationService
{
    private readonly ConversationDbContext _context;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        ConversationDbContext context,
        ILogger<ConversationService> logger
    )
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ListThreadsResponseDto> ListThreadsAsync(
        int page,
        int pageSize,
        string sortBy,
        string sortOrder,
        CancellationToken cancellationToken = default
    )
    {
        // Query from ConversationThreads metadata table
        IQueryable<ConversationThreadMetadata> threadsQuery = _context
            .ConversationThreads.Where(t => t.IsActive)
            .AsQueryable();

        // Apply sorting
        threadsQuery = ApplySorting(threadsQuery, sortBy, sortOrder);

        // Get total count
        int totalCount = await threadsQuery.CountAsync(cancellationToken);

        // Apply pagination
        List<ConversationThreadMetadata> threads = await threadsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Map to DTOs
        var threadDtos = threads
            .Select(t => new ConversationThreadDto
            {
                Id = t.ThreadId,
                Title = t.Title ?? "New conversation",
                Preview = t.Preview ?? "",
                MessageCount = t.MessageCount,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                IsActive = t.IsActive,
                SerializedState = t.SerializedState,
            })
            .ToList();

        return new ListThreadsResponseDto
        {
            Threads = threadDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        };
    }

    public async Task<GetConversationResponseDto?> GetConversationHistoryAsync(
        string threadId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default
    )
    {
        // Get thread metadata
        ConversationThreadMetadata? threadMetadata = await _context
            .ConversationThreads.FirstOrDefaultAsync(
                t => t.ThreadId == threadId && t.IsActive,
                cancellationToken
            );

        if (threadMetadata == null)
        {
            return null;
        }

        // Get total message count
        int totalCount = await _context
            .ConversationMessages.Where(m => m.ThreadId == threadId)
            .CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            // Return empty conversation with serializedState
            return new GetConversationResponseDto
            {
                ThreadId = threadId,
                SerializedState = threadMetadata.SerializedState,
                Messages = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize,
            };
        }

        // Get all messages
        List<ConversationMessage> allMessages = await _context
            .ConversationMessages.Where(m => m.ThreadId == threadId)
            .OrderBy(m => m.Timestamp)
            .ToListAsync(cancellationToken);

        // Filter to displayable messages (user + assistant text only)
        List<ConversationMessageDto> displayableMessages = allMessages
            .Where(m =>
                m.Role.Equals("user", StringComparison.OrdinalIgnoreCase)
                || m.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
            )
            .Select(m => new ConversationMessageDto
            {
                MessageId = m.MessageId,
                Role = m.Role,
                Content = ExtractTextFromContent(m.Content),
                Timestamp = m.Timestamp,
            })
            .ToList();

        // Apply pagination to displayable messages
        List<ConversationMessageDto> paginatedMessages = displayableMessages
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new GetConversationResponseDto
        {
            ThreadId = threadId,
            SerializedState = threadMetadata.SerializedState,
            Messages = paginatedMessages,
            TotalCount = displayableMessages.Count,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<bool> DeleteThreadAsync(
        string threadId,
        CancellationToken cancellationToken = default
    )
    {
        ConversationThreadMetadata? threadMetadata = await _context
            .ConversationThreads.FirstOrDefaultAsync(
                t => t.ThreadId == threadId && t.IsActive,
                cancellationToken
            );

        if (threadMetadata == null)
        {
            return false;
        }

        // Soft delete using domain method
        threadMetadata.Deactivate();

        // Also delete associated messages
        List<ConversationMessage> messages = await _context
            .ConversationMessages.Where(m => m.ThreadId == threadId)
            .ToListAsync(cancellationToken);

        _context.ConversationMessages.RemoveRange(messages);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted conversation thread {ThreadId} and {MessageCount} messages",
            threadId,
            messages.Count
        );

        return true;
    }

    private static IQueryable<ConversationThreadMetadata> ApplySorting(
        IQueryable<ConversationThreadMetadata> query,
        string sortBy,
        string sortOrder
    )
    {
        bool isAscending = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToUpperInvariant() switch
        {
            "CREATEDAT"
                => isAscending
                    ? query.OrderBy(t => t.CreatedAt)
                    : query.OrderByDescending(t => t.CreatedAt),
            "UPDATEDAT" or _
                => isAscending
                    ? query.OrderBy(t => t.UpdatedAt)
                    : query.OrderByDescending(t => t.UpdatedAt),
        };
    }

    private string ExtractTextFromContent(string contentJson)
    {
        try
        {
            JsonDocument? content = JsonSerializer.Deserialize<JsonDocument>(contentJson);
            if (content?.RootElement.ValueKind == JsonValueKind.Array)
            {
                JsonElement firstElement = content.RootElement.EnumerateArray().FirstOrDefault();
                if (firstElement.ValueKind != JsonValueKind.Undefined)
                {
                    if (firstElement.TryGetProperty("Text", out JsonElement textProp))
                    {
                        return textProp.GetString() ?? "";
                    }
                    if (firstElement.TryGetProperty("text", out JsonElement textPropLower))
                    {
                        return textPropLower.GetString() ?? "";
                    }
                    return firstElement.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract text from content JSON");
        }

        return "";
    }
}
