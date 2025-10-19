namespace TaskAgent.Application.DTOs;

/// <summary>
/// Result of content safety analysis
/// </summary>
public record ContentSafetyResult
{
    public bool IsSafe { get; init; }
    public Dictionary<string, int> CategoryScores { get; init; } = new();
    public List<string> ViolatedCategories { get; init; } = [];
    public string? BlockReason { get; init; }
}
