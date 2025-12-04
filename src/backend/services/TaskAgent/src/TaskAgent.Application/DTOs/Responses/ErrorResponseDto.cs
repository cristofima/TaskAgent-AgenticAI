namespace TaskAgent.Application.DTOs.Responses;

/// <summary>
/// Generic error response for API endpoints
/// </summary>
public class ErrorResponseDto
{
    public required string Error { get; set; }
    public required string Message { get; set; }
    public object? Details { get; set; }
}
