using System.ClientModel;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TaskAgent.Application.Interfaces;
using TaskAgent.Domain.Entities;
using TaskAgent.Infrastructure.Data;

namespace TaskAgent.Infrastructure.Services;

/// <summary>
/// Implementation of agent streaming service with state management.
/// Handles both full AgentThread JSON and simple ThreadDbKey (GUID) formats.
/// </summary>
public class AgentStreamingService : IAgentStreamingService
{
    private readonly AIAgent _agent;
    private readonly ConversationDbContext _conversationContext;
    private readonly ILogger<AgentStreamingService> _logger;
    private AgentThread? _currentThread;
    private ContentFilterException? _contentFilterException;
    private string? _pendingThreadId; // ThreadDbKey for loading history from DB

    public AgentStreamingService(
        AIAgent agent,
        ConversationDbContext conversationContext,
        ILogger<AgentStreamingService> logger
    )
    {
        _agent = agent;
        _conversationContext = conversationContext;
        _logger = logger;
    }

    public async IAsyncEnumerable<object> StreamResponseAsync(
        IEnumerable<object> messages,
        string? serializedState,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default
    )
    {
        // Deserialize or create new thread
        _currentThread = (AgentThread)DeserializeThread(serializedState);
        _contentFilterException = null;

        // Convert to list for streaming - cast back to ChatMessage
        List<ChatMessage> messageList = messages.Cast<ChatMessage>().ToList();
        
        // If we received a simple ThreadDbKey (from loadConversation), prepend history from database
        if (!string.IsNullOrEmpty(_pendingThreadId))
        {
            List<ChatMessage> historyMessages = await LoadMessagesFromDatabaseAsync(_pendingThreadId, cancellationToken);
            if (historyMessages.Count > 0)
            {
                _logger.LogInformation("Loaded {Count} history messages from database for thread {ThreadId}", 
                    historyMessages.Count, _pendingThreadId);
                // Prepend history before current message
                messageList = historyMessages.Concat(messageList).ToList();
            }
            _pendingThreadId = null; // Clear after use
        }

        // Stream agent responses with content filter detection
        IAsyncEnumerable<AgentRunResponseUpdate> responseStream;
        
        try
        {
            responseStream = _agent.RunStreamingAsync(
                messageList,
                _currentThread,
                cancellationToken: cancellationToken
            );
        }
        catch (ClientResultException ex) when (IsContentFilterError(ex))
        {
            _logger.LogWarning(ex, "Content filter triggered on prompt (initialization)");
            _contentFilterException = new ContentFilterException(ex.Message, ex);
            // Thread remains empty but valid - frontend will handle UI state
            yield break;
        }

        // Iterate with content filter detection - exception can occur during streaming
        IAsyncEnumerator<AgentRunResponseUpdate> enumerator = responseStream.GetAsyncEnumerator(cancellationToken);
        try
        {
            while (true)
            {
                AgentRunResponseUpdate update;
                try
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        break;
                    }
                    update = enumerator.Current;
                }
                catch (ClientResultException ex) when (IsContentFilterError(ex))
                {
                    _logger.LogWarning(ex, "Content filter triggered during streaming");
                    _contentFilterException = new ContentFilterException(ex.Message, ex);
                    yield break;
                }
                
                yield return update;
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }

    /// <summary>
    /// Gets the content filter exception if one occurred during streaming.
    /// </summary>
    public object? GetContentFilterException() => _contentFilterException;

    public object? GetCurrentThread()
    {
        return _currentThread;
    }

    public string GetSerializedState(object thread)
    {
        if (thread is not AgentThread agentThread)
        {
            throw new ArgumentException("Thread must be of type AgentThread", nameof(thread));
        }

        JsonElement serializedThread = agentThread.Serialize();
        return serializedThread.GetRawText();
    }

    public object DeserializeThread(string? serializedState)
    {
        if (string.IsNullOrEmpty(serializedState))
        {
            _pendingThreadId = null;
            return _agent.GetNewThread();
        }

        try
        {
            JsonElement stateElement = JsonSerializer.Deserialize<JsonElement>(serializedState);
            
            // Check if it's a simple ThreadDbKey string (GUID format from loadConversation)
            // vs a full AgentThread JSON object (from normal message flow)
            if (stateElement.ValueKind == JsonValueKind.String)
            {
                string? threadId = stateElement.GetString();
                _logger.LogInformation("Received simple ThreadDbKey format: {ThreadId}, will load history from database", threadId);
                
                // Store the threadId for loading history in StreamResponseAsync
                _pendingThreadId = threadId;
                
                // Create a new thread - history will be prepended in StreamResponseAsync
                return _agent.GetNewThread();
            }
            
            // It's a full AgentThread JSON - deserialize normally
            _pendingThreadId = null;
            AgentThread thread = _agent.DeserializeThread(stateElement);
            _logger.LogInformation("Successfully deserialized existing AgentThread");
            return thread;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize thread, creating new one");
            _pendingThreadId = null;
            return _agent.GetNewThread();
        }
    }
    
    /// <summary>
    /// Loads messages from PostgreSQL database and converts them to ChatMessage format.
    /// Used when the serializedState is a simple ThreadDbKey (from loadConversation flow).
    /// </summary>
    private async Task<List<ChatMessage>> LoadMessagesFromDatabaseAsync(
        string threadId,
        CancellationToken cancellationToken)
    {
        try
        {
            List<ConversationMessage> dbMessages = await _conversationContext.ConversationMessages
                .Where(m => m.ThreadId == threadId)
                .OrderBy(m => m.Timestamp)
                .ToListAsync(cancellationToken);

            if (dbMessages.Count == 0)
            {
                _logger.LogWarning("No messages found in database for thread {ThreadId}", threadId);
                return [];
            }

            var chatMessages = new List<ChatMessage>();
            foreach (ConversationMessage dbMsg in dbMessages)
            {
                ChatRole role = string.Equals(dbMsg.Role, "user", StringComparison.OrdinalIgnoreCase) 
                    ? ChatRole.User 
                    : ChatRole.Assistant;
                string textContent = ExtractTextFromContent(dbMsg.Content);
                
                chatMessages.Add(new ChatMessage(role, textContent));
            }

            return chatMessages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load messages from database for thread {ThreadId}", threadId);
            return [];
        }
    }
    
    /// <summary>
    /// Extracts text content from JSON content array.
    /// </summary>
    private static string ExtractTextFromContent(string contentJson)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(contentJson);
            JsonElement root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                JsonElement firstContent = root[0];
                // Try both "Text" (PascalCase) and "text" (camelCase)
                if (firstContent.TryGetProperty("Text", out JsonElement textProp) ||
                    firstContent.TryGetProperty("text", out textProp))
                {
                    return textProp.GetString() ?? "";
                }
            }
        }
        catch
        {
            // If parsing fails, return raw content (might be plain text)
        }

        return contentJson;
    }

    /// <summary>
    /// Checks if the exception is an Azure OpenAI content filter error.
    /// </summary>
    private static bool IsContentFilterError(ClientResultException ex)
    {
        // Azure OpenAI returns 400 with "content_filter" code
        // Message typically contains: "The response was filtered due to the prompt triggering Azure OpenAI's content management policy"
        return ex.Status == 400 && 
               (ex.Message.Contains("content_filter", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("content management policy", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("content filtering", StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Exception representing a content filter violation from Azure OpenAI.
/// </summary>
public class ContentFilterException : Exception
{
    public ContentFilterException() : base() { }
    
    public ContentFilterException(string message) : base(message) { }
    
    public ContentFilterException(string message, Exception innerException) 
        : base(message, innerException) { }
}
