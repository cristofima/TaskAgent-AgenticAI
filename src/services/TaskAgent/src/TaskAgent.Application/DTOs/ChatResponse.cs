namespace TaskAgent.Application.DTOs;

/// <summary>
/// Data Transfer Object for chat responses
/// </summary>
public record ChatResponse
{
    public string Message { get; set; } = string.Empty;
    public string ThreadId { get; set; } = string.Empty;
}
