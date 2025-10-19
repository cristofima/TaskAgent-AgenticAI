namespace TaskAgent.Application.Interfaces;

/// <summary>
/// Interface for AI Agent service operations
/// </summary>
public interface ITaskAgentService
{
    /// <summary>
    /// Send a message to the AI agent and get a response
    /// Returns a tuple with the response message and the thread ID used
    /// If threadId is null, a new thread will be created automatically
    /// </summary>
    Task<(string response, string threadId)> SendMessageAsync(string message, string? threadId = null);

    /// <summary>
    /// Get a new thread ID for starting a new conversation
    /// </summary>
    string GetNewThreadId();
}
