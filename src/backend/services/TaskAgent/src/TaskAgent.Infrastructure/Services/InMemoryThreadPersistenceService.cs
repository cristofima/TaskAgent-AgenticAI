using System.Collections.Concurrent;
using TaskAgent.Application.DTOs;
using TaskAgent.Application.Interfaces;

namespace TaskAgent.Infrastructure.Services;

/// <summary>
/// In-memory implementation of thread persistence with metadata tracking
/// Simple implementation for single-server scenarios
/// For production, replace with database-backed implementation
/// </summary>
public class InMemoryThreadPersistenceService : IThreadPersistenceService
{
    private readonly ConcurrentDictionary<string, ThreadStorage> _threads = new();

    /// <summary>
    /// Internal storage model for threads with metadata
    /// </summary>
    private sealed class ThreadStorage
    {
        public string SerializedThread { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int MessageCount { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public Task SaveThreadAsync(string threadId, string serializedThread)
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

        DateTime now = DateTime.UtcNow;

        _threads.AddOrUpdate(
            threadId,
            // Add new thread
            _ => new ThreadStorage
            {
                SerializedThread = serializedThread,
                CreatedAt = now,
                UpdatedAt = now,
                MessageCount = 1,
                IsActive = true,
            },
            // Update existing thread
            (_, existing) =>
                new ThreadStorage
                {
                    SerializedThread = serializedThread,
                    CreatedAt = existing.CreatedAt,
                    UpdatedAt = now,
                    MessageCount = existing.MessageCount + 1,
                    IsActive = existing.IsActive,
                }
        );

        return Task.CompletedTask;
    }

    public Task<string?> GetThreadAsync(string threadId)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            return Task.FromResult<string?>(null);
        }

        if (_threads.TryGetValue(threadId, out ThreadStorage? storage))
        {
            return Task.FromResult<string?>(storage.SerializedThread);
        }

        return Task.FromResult<string?>(null);
    }

    public Task DeleteThreadAsync(string threadId)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            return Task.CompletedTask;
        }

        _threads.TryRemove(threadId, out _);

        return Task.CompletedTask;
    }

    public Task<ListThreadsResponse> GetThreadsAsync(ListThreadsRequest request)
    {
        // Get all threads
        var allThreads = _threads
            .Select(kvp => new ConversationThread
            {
                Id = kvp.Key,
                Title = null, // TODO: Extract title from first message in future
                Preview = null, // TODO: Extract preview from messages in future
                CreatedAt = kvp.Value.CreatedAt,
                UpdatedAt = kvp.Value.UpdatedAt,
                MessageCount = kvp.Value.MessageCount,
                IsActive = kvp.Value.IsActive,
            })
            .ToList();

        // Apply filters
        if (request.IsActive.HasValue)
        {
            allThreads = allThreads.Where(t => t.IsActive == request.IsActive.Value).ToList();
        }

        // Apply sorting
        allThreads = request.SortBy switch
        {
            "CreatedAt" => string.Equals(
                request.SortOrder,
                "asc",
                StringComparison.OrdinalIgnoreCase
            )
                ? allThreads.OrderBy(t => t.CreatedAt).ToList()
                : allThreads.OrderByDescending(t => t.CreatedAt).ToList(),
            "UpdatedAt" => string.Equals(
                request.SortOrder,
                "asc",
                StringComparison.OrdinalIgnoreCase
            )
                ? allThreads.OrderBy(t => t.UpdatedAt).ToList()
                : allThreads.OrderByDescending(t => t.UpdatedAt).ToList(),
            _ => allThreads.OrderByDescending(t => t.UpdatedAt).ToList(),
        };

        // Apply pagination
        int totalCount = allThreads.Count;
        int skip = (request.Page - 1) * request.PageSize;
        var pagedThreads = allThreads.Skip(skip).Take(request.PageSize).ToList();

        return Task.FromResult(
            new ListThreadsResponse
            {
                Threads = pagedThreads,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                HasMore = skip + pagedThreads.Count < totalCount,
            }
        );
    }

    public Task<ConversationThread?> GetThreadMetadataAsync(string threadId)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            return Task.FromResult<ConversationThread?>(null);
        }

        if (_threads.TryGetValue(threadId, out ThreadStorage? storage))
        {
            return Task.FromResult<ConversationThread?>(
                new ConversationThread
                {
                    Id = threadId,
                    Title = null, // TODO: Extract from messages
                    Preview = null, // TODO: Extract from messages
                    CreatedAt = storage.CreatedAt,
                    UpdatedAt = storage.UpdatedAt,
                    MessageCount = storage.MessageCount,
                    IsActive = storage.IsActive,
                }
            );
        }

        return Task.FromResult<ConversationThread?>(null);
    }

    public Task<bool> ThreadExistsAsync(string threadId)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(_threads.ContainsKey(threadId));
    }
}
