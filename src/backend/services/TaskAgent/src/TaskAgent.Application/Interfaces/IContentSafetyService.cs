using TaskAgent.Application.DTOs;

namespace TaskAgent.Application.Interfaces;

/// <summary>
/// Interface for Azure AI Content Safety service
/// </summary>
public interface IContentSafetyService
{
    /// <summary>
    /// Analyzes text for harmful content using Azure Content Safety SDK
    /// </summary>
    Task<ContentSafetyResult> AnalyzeTextAsync(string text);

    /// <summary>
    /// Detects prompt injection attacks using Azure Content Safety Prompt Shield REST API
    /// </summary>
    Task<PromptInjectionResult> DetectPromptInjectionAsync(string userPrompt);
}
