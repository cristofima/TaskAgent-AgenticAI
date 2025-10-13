namespace TaskAgent.Application.Interfaces;

/// <summary>
/// Interface for AI Agent service operations
/// Part of Application Layer - defines business logic contracts
/// </summary>
public interface ITaskAgentService
{
    /// <summary>
    /// Send a message to the AI agent and get a response
    /// </summary>
    Task<string> SendMessageAsync(string message, string? threadId = null);

    /// <summary>
    /// Get a new thread ID for starting a new conversation
    /// </summary>
    string GetNewThreadId();
}
