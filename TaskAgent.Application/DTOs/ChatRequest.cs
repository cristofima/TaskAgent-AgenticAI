namespace TaskAgent.Application.DTOs;

/// <summary>
/// Data Transfer Object for chat requests
/// </summary>
public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? ThreadId { get; set; }
}
