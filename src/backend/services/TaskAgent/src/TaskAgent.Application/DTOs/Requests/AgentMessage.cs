namespace TaskAgent.Application.DTOs.Requests;

/// <summary>
/// Message model within agent requests.
/// </summary>
public class AgentMessage
{
    public string? Role { get; set; }
    public string? Content { get; set; }
}
