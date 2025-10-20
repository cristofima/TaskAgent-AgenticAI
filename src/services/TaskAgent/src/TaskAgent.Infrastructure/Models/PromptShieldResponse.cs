namespace TaskAgent.Infrastructure.Models;

internal record PromptShieldResponse
{
    public UserPromptAnalysis? UserPromptAnalysis { get; init; }
    public DocumentAnalysis[]? DocumentsAnalysis { get; init; }
}

internal record UserPromptAnalysis
{
    public UserPromptAnalysis(bool attackDetected)
    {
        AttackDetected = attackDetected;
    }

    public bool AttackDetected { get; }
}

internal record DocumentAnalysis
{
    public bool AttackDetected { get; set; }
}
