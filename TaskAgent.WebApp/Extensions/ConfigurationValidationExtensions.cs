namespace TaskAgent.WebApp.Extensions;

/// <summary>
/// Extension methods for validating application configuration
/// </summary>
public static class ConfigurationValidationExtensions
{
    /// <summary>
    /// Validates critical configuration values on startup
    /// </summary>
    public static void ValidateConfiguration(this WebApplication app)
    {
        var configuration = app.Configuration;
        var logger = app.Logger;
        var errors = new List<string>();

        ValidateAzureOpenAIConfiguration(configuration, errors);
        ValidateContentSafetyConfiguration(configuration, logger);

        if (errors.Any())
        {
            ThrowConfigurationException(errors, logger);
        }

        logger.LogInformation("Configuration validation passed successfully");
    }

    private static void ValidateAzureOpenAIConfiguration(
        IConfiguration configuration,
        List<string> errors
    )
    {
        var azureOpenAiEndpoint = configuration["AzureOpenAI:Endpoint"];
        var azureOpenAiKey = configuration["AzureOpenAI:ApiKey"];
        var azureOpenAiDeployment = configuration["AzureOpenAI:DeploymentName"];

        if (string.IsNullOrWhiteSpace(azureOpenAiEndpoint))
            errors.Add("AzureOpenAI:Endpoint is not configured");

        if (string.IsNullOrWhiteSpace(azureOpenAiKey))
            errors.Add("AzureOpenAI:ApiKey is not configured");

        if (string.IsNullOrWhiteSpace(azureOpenAiDeployment))
            errors.Add("AzureOpenAI:DeploymentName is not configured");
    }

    private static void ValidateContentSafetyConfiguration(
        IConfiguration configuration,
        ILogger logger
    )
    {
        var contentSafetyEndpoint = configuration["ContentSafety:Endpoint"];
        var contentSafetyKey = configuration["ContentSafety:ApiKey"];

        if (
            string.IsNullOrWhiteSpace(contentSafetyEndpoint)
            || string.IsNullOrWhiteSpace(contentSafetyKey)
        )
        {
            logger.LogWarning(
                "Content Safety is not configured. The application will run without content moderation. "
                    + "To enable Content Safety, configure ContentSafety:Endpoint and ContentSafety:ApiKey in appsettings.json"
            );
        }
    }

    private static void ThrowConfigurationException(List<string> errors, ILogger logger)
    {
        var errorMessage = string.Join(Environment.NewLine, errors);
        logger.LogCritical(
            "Application cannot start due to missing configuration:{NewLine}{Errors}",
            Environment.NewLine,
            errorMessage
        );
        throw new InvalidOperationException(
            $"Critical configuration is missing. Please check appsettings.json:{Environment.NewLine}{errorMessage}"
        );
    }
}
