namespace TaskAgent.Infrastructure.Models;

internal class ContentSafetyConfig
{
    public int HateSeverityThreshold { get; set; }
    public int ViolenceSeverityThreshold { get; set; }
    public int SexualSeverityThreshold { get; set; }
    public int SelfHarmSeverityThreshold { get; set; }
}
