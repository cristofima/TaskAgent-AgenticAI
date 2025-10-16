namespace TaskAgent.Application.DTOs;

/// <summary>
/// Standardized error response for API
/// </summary>
public record ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Details { get; set; }
}
