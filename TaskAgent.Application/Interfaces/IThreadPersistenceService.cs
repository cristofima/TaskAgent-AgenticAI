namespace TaskAgent.Application.Interfaces;

/// <summary>
/// Service for persisting and retrieving AI Agent threads
/// </summary>
public interface IThreadPersistenceService
{
    /// <summary>
    /// Saves a serialized thread to storage
    /// </summary>
    /// <param name="threadId">Unique identifier for the thread</param>
    /// <param name="serializedThread">Serialized thread data</param>
    Task SaveThreadAsync(string threadId, string serializedThread);

    /// <summary>
    /// Retrieves a serialized thread from storage
    /// </summary>
    /// <param name="threadId">Unique identifier for the thread</param>
    /// <returns>Serialized thread data, or null if not found</returns>
    Task<string?> GetThreadAsync(string threadId);

    /// <summary>
    /// Deletes a thread from storage
    /// </summary>
    /// <param name="threadId">Unique identifier for the thread</param>
    Task DeleteThreadAsync(string threadId);
}
