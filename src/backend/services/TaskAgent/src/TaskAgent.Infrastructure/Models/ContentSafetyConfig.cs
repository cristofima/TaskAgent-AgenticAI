namespace TaskAgent.Infrastructure.Models;

internal class ContentSafetyConfig
{
    public int HateSeverityThreshold { get; init; }
    public int ViolenceSeverityThreshold { get; init; }
    public int SexualSeverityThreshold { get; init; }
    public int SelfHarmSeverityThreshold { get; init; }
}
