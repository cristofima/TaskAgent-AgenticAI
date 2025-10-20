namespace TaskAgent.Application.DTOs;

/// <summary>
/// Data Transfer Object for chat requests
/// </summary>
public record ChatRequest
{
    public string Message { get; init; } = string.Empty;
    public string? ThreadId { get; init; }
}
