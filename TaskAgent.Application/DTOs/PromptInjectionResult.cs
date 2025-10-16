namespace TaskAgent.Application.DTOs;

/// <summary>
/// Result of prompt injection detection from Azure Prompt Shield API
/// </summary>
public record PromptInjectionResult
{
    public bool IsSafe { get; set; }
    public bool InjectionDetected { get; set; }
    public string DetectedAttackType { get; set; } = "None";
    public string? Reason { get; set; }
}
