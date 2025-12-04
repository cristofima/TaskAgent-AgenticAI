namespace TaskAgent.Application.Interfaces;

/// <summary>
/// Service for streaming AI agent responses with state management.
/// </summary>
/// <remarks>
/// This interface uses object types to avoid dependencies on Microsoft.Agents.AI in Application layer.
/// Concrete implementations in Infrastructure layer handle the actual agent types.
/// </remarks>
public interface IAgentStreamingService
{
    /// <summary>
    /// Streams agent responses and returns the serialized thread state.
    /// </summary>
    /// <param name="messages">User messages to process (ChatMessage enumerable).</param>
    /// <param name="serializedState">Existing thread state (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async stream of agent response updates.</returns>
    IAsyncEnumerable<object> StreamResponseAsync(
        IEnumerable<object> messages,
        string? serializedState,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets the current thread after streaming operations.
    /// </summary>
    /// <returns>The current agent thread or null if not initialized.</returns>
    object? GetCurrentThread();

    /// <summary>
    /// Gets the serialized state of the current thread after streaming.
    /// </summary>
    /// <param name="thread">The agent thread to serialize.</param>
    /// <returns>Serialized thread state as JSON string.</returns>
    string GetSerializedState(object thread);

    /// <summary>
    /// Deserializes a thread from serialized state.
    /// </summary>
    /// <param name="serializedState">Serialized thread state.</param>
    /// <returns>Deserialized agent thread or new thread if deserialization fails.</returns>
    object DeserializeThread(string? serializedState);
}
