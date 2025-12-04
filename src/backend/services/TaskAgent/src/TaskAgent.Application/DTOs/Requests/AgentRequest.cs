namespace TaskAgent.Application.DTOs.Requests;

/// <summary>
/// Request model for agent chat endpoint.
/// </summary>
public class AgentRequest
{
    public List<AgentMessage>? Messages { get; set; }
    public string? SerializedState { get; set; }
}
