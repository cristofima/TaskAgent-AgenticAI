namespace TaskAgent.Application.DTOs;

/// <summary>
/// Result of prompt injection detection from Azure Prompt Shield API
/// </summary>
public record PromptInjectionResult
{
    public bool IsSafe { get; init; }
    public bool InjectionDetected { get; init; }
    public string DetectedAttackType { get; init; } = "None";
    public string? Reason { get; set; }
}
