using System.Collections.Concurrent;
using TaskAgent.Application.Interfaces;

namespace TaskAgent.Infrastructure.Services;

/// <summary>
/// In-memory implementation of thread persistence
/// Simple implementation for single-server scenarios
/// </summary>
public class InMemoryThreadPersistenceService : IThreadPersistenceService
{
    private readonly ConcurrentDictionary<string, string> _threads = new();

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

        _threads[threadId] = serializedThread;

        return Task.CompletedTask;
    }

    public Task<string?> GetThreadAsync(string threadId)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            return Task.FromResult<string?>(null);
        }

        _threads.TryGetValue(threadId, out string? serializedThread);

        return Task.FromResult(serializedThread);
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
}
