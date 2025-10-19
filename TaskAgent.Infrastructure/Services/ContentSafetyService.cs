using System.Text;
using System.Text.Json;
using Azure;
using Azure.AI.ContentSafety;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TaskAgent.Application.DTOs;
using TaskAgent.Application.Interfaces;
using TaskAgent.Infrastructure.Constants;
using TaskAgent.Infrastructure.Models;

namespace TaskAgent.Infrastructure.Services;

public class ContentSafetyService : IContentSafetyService
{
    private readonly ContentSafetyClient _contentSafetyClient;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ContentSafetyService> _logger;
    private readonly ContentSafetyConfig _config;

    public ContentSafetyService(
        ContentSafetyClient contentSafetyClient,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ContentSafetyService> logger
    )
    {
        _contentSafetyClient =
            contentSafetyClient ?? throw new ArgumentNullException(nameof(contentSafetyClient));
        _httpClient =
            httpClientFactory?.CreateClient(ContentSafetyConstants.HTTP_CLIENT_NAME)
            ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _config = LoadConfiguration(configuration);
    }

    public async Task<ContentSafetyResult> AnalyzeTextAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return CreateSafeResult();
        }

        try
        {
            _logger.LogInformation("Analyzing text for content safety");

            var request = new AnalyzeTextOptions(text);
            Response<AnalyzeTextResult>? response = await _contentSafetyClient.AnalyzeTextAsync(
                request
            );

            return BuildContentSafetyResult(response.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing text for content safety");
            return CreateSafeResult(); // Fail open for availability
        }
    }

    public async Task<PromptInjectionResult> DetectPromptInjectionAsync(string userPrompt)
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            return CreateSafeInjectionResult();
        }

        try
        {
            _logger.LogInformation("Checking prompt injection");

            const string apiUrl = ContentSafetyConstants.PROMPT_SHIELD_API_PATH;

            var requestBody = new { userPrompt };

            string jsonContent = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(
                jsonContent,
                Encoding.UTF8,
                ContentSafetyConstants.CONTENT_TYPE_JSON
            );

            HttpResponseMessage httpResponse = await _httpClient.PostAsync(apiUrl, httpContent);

            if (!httpResponse.IsSuccessStatusCode)
            {
                await httpResponse.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Prompt Shield API error. Status: {Status}",
                    httpResponse.StatusCode
                );

                return new PromptInjectionResult
                {
                    IsSafe = false,
                    InjectionDetected = true,
                    DetectedAttackType = ContentSafetyConstants.API_ERROR_ATTACK_TYPE,
                    Reason = ContentSafetyConstants.API_ERROR_REASON,
                };
            }

            string responseContent = await httpResponse.Content.ReadAsStringAsync();
            PromptShieldResponse? apiResponse = JsonSerializer.Deserialize<PromptShieldResponse>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (apiResponse == null)
            {
                _logger.LogWarning("Failed to parse Prompt Shield response");
                return new PromptInjectionResult
                {
                    IsSafe = false,
                    InjectionDetected = true,
                    DetectedAttackType = "Parse Error",
                    Reason = "Could not validate prompt safety. Request blocked.",
                };
            }

            bool attackDetected = apiResponse.UserPromptAnalysis?.AttackDetected ?? false;

            var result = new PromptInjectionResult
            {
                IsSafe = !attackDetected,
                InjectionDetected = attackDetected,
                DetectedAttackType = attackDetected ? "Prompt Injection Attack" : "None",
                Reason = attackDetected
                    ? "Azure Prompt Shield detected a potential prompt injection or jailbreak attempt"
                    : "Prompt passed security validation",
            };

            _logger.LogInformation("Prompt Shield result: AttackDetected={Attack}", attackDetected);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Prompt Shield API - blocking request");
            return new PromptInjectionResult
            {
                IsSafe = false,
                InjectionDetected = true,
                DetectedAttackType = "System Error",
                Reason = "Security check failed. Request blocked as precaution.",
            };
        }
    }

    private static ContentSafetyConfig LoadConfiguration(IConfiguration configuration)
    {
        const int defaultThreshold = ContentSafetyConstants.DEFAULT_SEVERITY_THRESHOLD;

        return new ContentSafetyConfig
        {
            HateSeverityThreshold = configuration.GetValue(
                "ContentSafety:HateThreshold",
                defaultThreshold
            ),
            ViolenceSeverityThreshold = configuration.GetValue(
                "ContentSafety:ViolenceThreshold",
                defaultThreshold
            ),
            SexualSeverityThreshold = configuration.GetValue(
                "ContentSafety:SexualThreshold",
                defaultThreshold
            ),
            SelfHarmSeverityThreshold = configuration.GetValue(
                "ContentSafety:SelfHarmThreshold",
                defaultThreshold
            ),
        };
    }

    private static ContentSafetyResult CreateSafeResult() => new() { IsSafe = true };

    private static PromptInjectionResult CreateSafeInjectionResult() =>
        new() { IsSafe = true, InjectionDetected = false };

    private ContentSafetyResult BuildContentSafetyResult(AnalyzeTextResult azureResult)
    {
        Dictionary<string, int> categoryScores = ExtractCategoryScores(azureResult);
        List<string> violations = FindViolations(categoryScores);

        return new ContentSafetyResult
        {
            IsSafe = violations.Count == 0,
            CategoryScores = categoryScores,
            ViolatedCategories = violations,
            BlockReason =
                violations.Count > 0
                    ? $"Content blocked due to: {string.Join(", ", violations)}"
                    : null,
        };
    }

    private static Dictionary<string, int> ExtractCategoryScores(AnalyzeTextResult result)
    {
        return new Dictionary<string, int>
        {
            {
                "Hate",
                result.CategoriesAnalysis.First(c => c.Category == TextCategory.Hate).Severity ?? 0
            },
            {
                "Violence",
                result.CategoriesAnalysis.First(c => c.Category == TextCategory.Violence).Severity
                    ?? 0
            },
            {
                "Sexual",
                result.CategoriesAnalysis.First(c => c.Category == TextCategory.Sexual).Severity
                    ?? 0
            },
            {
                "SelfHarm",
                result.CategoriesAnalysis.First(c => c.Category == TextCategory.SelfHarm).Severity
                    ?? 0
            },
        };
    }

    private List<string> FindViolations(Dictionary<string, int> scores)
    {
        var violations = new List<string>();

        if (scores["Hate"] >= _config.HateSeverityThreshold)
        {
            violations.Add("Hate");
        }

        if (scores["Violence"] >= _config.ViolenceSeverityThreshold)
        {
            violations.Add("Violence");
        }

        if (scores["Sexual"] >= _config.SexualSeverityThreshold)
        {
            violations.Add("Sexual");
        }

        if (scores["SelfHarm"] >= _config.SelfHarmSeverityThreshold)
        {
            violations.Add("SelfHarm");
        }

        return violations;
    }
}
