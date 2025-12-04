using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TaskAgent.Application.Interfaces;

namespace TaskAgent.Infrastructure.Services;

/// <summary>
/// Implementation of agent streaming service with state management.
/// </summary>
public class AgentStreamingService : IAgentStreamingService
{
    private readonly AIAgent _agent;
    private readonly ILogger<AgentStreamingService> _logger;
    private AgentThread? _currentThread;

    public AgentStreamingService(
        AIAgent agent,
        ILogger<AgentStreamingService> logger
    )
    {
        _agent = agent;
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

        // Convert to list for streaming - cast back to ChatMessage
        List<ChatMessage> messageList = messages.Cast<ChatMessage>().ToList();

        // Stream agent responses
        await foreach (AgentRunResponseUpdate update in _agent.RunStreamingAsync(
            messageList,
            _currentThread,
            cancellationToken: cancellationToken
        ))
        {
            yield return update;
        }
    }

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
            return _agent.GetNewThread();
        }

        try
        {
            JsonElement stateElement = JsonSerializer.Deserialize<JsonElement>(serializedState);
            AgentThread thread = _agent.DeserializeThread(stateElement);
            _logger.LogInformation("Successfully deserialized existing conversation thread");
            return thread;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize thread, creating new one");
            return _agent.GetNewThread();
        }
    }
}
