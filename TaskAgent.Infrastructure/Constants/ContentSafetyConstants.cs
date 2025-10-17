namespace TaskAgent.Infrastructure.Constants;

/// <summary>
/// Constants for Content Safety service configuration
/// </summary>
public static class ContentSafetyConstants
{
    public const string PROMPT_SHIELD_API_PATH =
        "/contentsafety/text:shieldPrompt?api-version=2024-09-01";
    public const string HTTP_CLIENT_NAME = "ContentSafety";
    public const string CONTENT_TYPE_JSON = "application/json";

    // Default values
    public const int DEFAULT_SEVERITY_THRESHOLD = 2;

    // Error messages
    public const string API_ERROR_ATTACK_TYPE = "API Error";
    public const string API_ERROR_REASON = "Security check failed. Request blocked as precaution.";
}
