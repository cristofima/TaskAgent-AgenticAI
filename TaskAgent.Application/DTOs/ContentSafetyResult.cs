namespace TaskAgent.Application.DTOs;

/// <summary>
/// Result of content safety analysis
/// </summary>
public record ContentSafetyResult
{
    public bool IsSafe { get; set; }
    public Dictionary<string, int> CategoryScores { get; set; } = new();
    public List<string> ViolatedCategories { get; set; } = new();
    public string? BlockReason { get; set; }
}
