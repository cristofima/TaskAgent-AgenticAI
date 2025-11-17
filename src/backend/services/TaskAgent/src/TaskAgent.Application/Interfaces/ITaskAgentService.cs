using TaskAgent.Application.DTOs;

namespace TaskAgent.Application.Interfaces;

/// <summary>
/// Interface for AI Agent service operations
/// </summary>
public interface ITaskAgentService
{
    /// <summary>
    /// Send a message to the AI agent and get a response with full metadata
    /// Returns a ChatResponse with message, thread ID, and metadata
    /// If threadId is null, a new thread will be created automatically
    /// </summary>
    public Task<ChatResponse> SendMessageAsync(string message, string? threadId = null);

    /// <summary>
    /// Get a new thread ID for starting a new conversation
    /// </summary>
    public string GetNewThreadId();

    /// <summary>
    /// Get conversation history for a specific thread
    /// </summary>
    /// <param name="request">Request with thread ID and pagination options</param>
    /// <returns>Conversation history with messages</returns>
    public Task<GetConversationResponse> GetConversationHistoryAsync(
        GetConversationRequest request
    );

    /// <summary>
    /// Delete a conversation thread
    /// </summary>
    /// <param name="threadId">Thread ID to delete</param>
    public Task DeleteThreadAsync(string threadId);

    /// <summary>
    /// List all conversation threads
    /// </summary>
    /// <param name="request">Request with pagination and filter options</param>
    /// <returns>List of conversation threads</returns>
    public Task<ListThreadsResponse> ListThreadsAsync(ListThreadsRequest request);

    /// <summary>
    /// Creates or restores a thread for blocked conversation (ChatGPT-like behavior)
    /// Note: Does NOT persist the blocked message content (security measure)
    /// Creates a thread placeholder so the conversation can continue after the block
    /// </summary>
    /// <param name="threadId">Thread ID (will create new if null)</param>
    /// <returns>The thread ID (existing or newly created)</returns>
    public Task<string> SaveBlockedMessageAsync(string? threadId = null);
}
