using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskAgent.Application.DTOs;
using TaskAgent.Application.Interfaces;
using TaskAgent.Domain.Entities;
using TaskAgent.Infrastructure.Data;

namespace TaskAgent.Infrastructure.Services;

/// <summary>
/// PostgreSQL-backed implementation of thread persistence using JSONB blob storage
/// Each ConversationThread stores the complete serialized AgentThread as JSONB
/// </summary>
public class PostgresThreadPersistenceService : IThreadPersistenceService
{
    private readonly ConversationDbContext _context;
    private readonly ILogger<PostgresThreadPersistenceService> _logger;

    public PostgresThreadPersistenceService(
        ConversationDbContext context,
        ILogger<PostgresThreadPersistenceService> logger
    )
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SaveThreadAsync(string threadId, string serializedThread)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            throw new ArgumentException("Thread ID cannot be empty", nameof(threadId));
        }

        if (string.IsNullOrWhiteSpace(serializedThread))
        {
            throw new ArgumentException(
                "Serialized thread cannot be empty",
                nameof(serializedThread)
            );
        }

        // Check if thread already exists
        ConversationThread? existingThread = await _context.ConversationThreads.FirstOrDefaultAsync(
            t => t.ThreadId == threadId
        );

        // Extract metadata from JSON for search/filtering
        var (title, preview, messageCount) = ExtractMetadataFromJson(serializedThread);

        if (existingThread == null)
        {
            // Create new thread
            var newThread = ConversationThread.Create(
                threadId,
                serializedThread,
                title,
                preview,
                messageCount
            );

            _context.ConversationThreads.Add(newThread);
        }
        else
        {
            // Update existing thread with new serialized content
            existingThread.UpdateThread(serializedThread, title, preview, messageCount);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<string?> GetThreadAsync(string threadId)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            return null;
        }

        ConversationThread? thread = await _context
            .ConversationThreads.AsNoTracking()
            .FirstOrDefaultAsync(t => t.ThreadId == threadId);

        return thread?.SerializedThread;
    }

    public async Task DeleteThreadAsync(string threadId)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            throw new ArgumentException("Thread ID cannot be empty", nameof(threadId));
        }

        ConversationThread? thread = await _context.ConversationThreads.FirstOrDefaultAsync(t =>
            t.ThreadId == threadId
        );

        if (thread != null)
        {
            _context.ConversationThreads.Remove(thread);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<ListThreadsResponse> GetThreadsAsync(ListThreadsRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // Base query
        IQueryable<ConversationThread> query = _context.ConversationThreads.AsNoTracking();

        // Filter by IsActive if specified
        if (request.IsActive.HasValue)
        {
            query = query.Where(t => t.IsActive == request.IsActive.Value);
        }

        // Apply sorting
        query = request.SortBy switch
        {
            "CreatedAt" => string.Equals(
                request.SortOrder,
                "asc",
                StringComparison.OrdinalIgnoreCase
            )
                ? query.OrderBy(t => t.CreatedAt)
                : query.OrderByDescending(t => t.CreatedAt),
            "UpdatedAt" => string.Equals(
                request.SortOrder,
                "asc",
                StringComparison.OrdinalIgnoreCase
            )
                ? query.OrderBy(t => t.UpdatedAt)
                : query.OrderByDescending(t => t.UpdatedAt),
            _ => query.OrderByDescending(t => t.UpdatedAt),
        };

        // Get total count
        int totalCount = await query.CountAsync();

        // Apply pagination
        int skip = (request.Page - 1) * request.PageSize;
        List<ConversationThread> threads = await query
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync();

        // Map to DTOs
        var threadDtos = threads
            .Select(t => new ConversationThreadDTO
            {
                Id = t.ThreadId,
                Title = t.Title,
                Preview = t.Preview,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                MessageCount = t.MessageCount,
                IsActive = t.IsActive,
            })
            .ToList();

        return new ListThreadsResponse
        {
            Threads = threadDtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            HasMore = skip + request.PageSize < totalCount,
        };
    }

    #region Private Helper Methods

    /// <summary>
    /// Extracts metadata (title, preview, message count) from serialized JSON
    /// </summary>
    private (string? title, string? preview, int messageCount) ExtractMetadataFromJson(
        string serializedJson
    )
    {
        try
        {
            using var doc = JsonDocument.Parse(serializedJson);
            JsonElement root = doc.RootElement;

            // Log root structure for debugging
            _logger.LogDebug(
                "Thread JSON structure - Root properties: {Properties}",
                string.Join(", ", root.EnumerateObject().Select(p => p.Name))
            );

            // AgentThread structure: root.storeState.messages
            if (!root.TryGetProperty("storeState", out JsonElement storeStateElement))
            {
                _logger.LogWarning("storeState property not found in thread JSON");
                return (null, null, 0);
            }

            // Extract messages array from storeState
            if (
                !storeStateElement.TryGetProperty("messages", out JsonElement messagesElement)
                || messagesElement.ValueKind != JsonValueKind.Array
            )
            {
                _logger.LogWarning("messages property not found or not an array in storeState");
                return (null, null, 0);
            }

            int messageCount = messagesElement.GetArrayLength();
            _logger.LogDebug("Found {MessageCount} messages in thread", messageCount);

            // Log first message structure
            if (messageCount > 0)
            {
                JsonElement firstMessage = messagesElement[0];
                _logger.LogDebug(
                    "First message properties: {Properties}",
                    string.Join(", ", firstMessage.EnumerateObject().Select(p => p.Name))
                );
            }

            // Extract title from first user message
            string? title = ExtractTitleFromMessages(messagesElement);

            // Extract preview from last assistant message
            string? preview = ExtractPreviewFromMessages(messagesElement);

            _logger.LogDebug(
                "Extracted metadata - Title: {Title}, Preview: {Preview}, Count: {Count}",
                title ?? "(null)",
                preview ?? "(null)",
                messageCount
            );

            return (title, preview, messageCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract metadata from thread JSON");
            return (null, null, 0);
        }
    }

    /// <summary>
    /// Extracts title from first user message (max 50 chars)
    /// </summary>
    private static string? ExtractTitleFromMessages(JsonElement messagesElement)
    {
        foreach (JsonElement message in messagesElement.EnumerateArray())
        {
            if (!message.TryGetProperty("role", out JsonElement roleElement))
            {
                continue;
            }

            string? role = roleElement.GetString();
            if (!string.Equals(role, "user", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Message structure: { role: "user", contents: [{ text: "...", $type: "text" }] }
            if (
                !message.TryGetProperty("contents", out JsonElement contentsElement)
                || contentsElement.ValueKind != JsonValueKind.Array
                || contentsElement.GetArrayLength() <= 0
            )
            {
                continue;
            }

            JsonElement firstContent = contentsElement[0];
            if (!firstContent.TryGetProperty("text", out JsonElement textElement))
            {
                continue;
            }

            string? text = textElement.GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            const int maxLength = 50;
            return text.Length > maxLength ? string.Concat(text.AsSpan(0, maxLength), "...") : text;
        }

        return null;
    }

    /// <summary>
    /// Extracts preview from last assistant message (max 100 chars)
    /// </summary>
    private static string? ExtractPreviewFromMessages(JsonElement messagesElement)
    {
        JsonElement? lastAssistantMessage = null;

        foreach (JsonElement message in messagesElement.EnumerateArray())
        {
            if (!message.TryGetProperty("role", out JsonElement roleElement))
            {
                continue;
            }

            string? role = roleElement.GetString();
            if (string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase))
            {
                lastAssistantMessage = message;
            }
        }

        if (!lastAssistantMessage.HasValue)
        {
            return null;
        }

        // Message structure: { role: "assistant", contents: [{ text: "...", $type: "text" }] }
        if (
            !lastAssistantMessage.Value.TryGetProperty("contents", out JsonElement contentsElement)
            || contentsElement.ValueKind != JsonValueKind.Array
            || contentsElement.GetArrayLength() <= 0
        )
        {
            return null;
        }

        JsonElement firstContent = contentsElement[0];
        if (!firstContent.TryGetProperty("text", out JsonElement textElement))
        {
            return null;
        }

        string? text = textElement.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        const int maxLength = 100;
        return text.Length > maxLength ? string.Concat(text.AsSpan(0, maxLength), "...") : text;
    }

    #endregion
}
