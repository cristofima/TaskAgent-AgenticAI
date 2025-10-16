namespace TaskAgent.Infrastructure.Models;

internal record PromptShieldResponse
{
    public UserPromptAnalysis? UserPromptAnalysis { get; set; }
    public DocumentAnalysis[]? DocumentsAnalysis { get; set; }
}

internal record UserPromptAnalysis
{
    public bool AttackDetected { get; set; }
}

internal record DocumentAnalysis
{
    public bool AttackDetected { get; set; }
}
