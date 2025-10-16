using Azure;
using Azure.AI.OpenAI;
using TaskAgent.Application.Interfaces;
using TaskAgent.WebApp.Services;

namespace TaskAgent.WebApp;

/// <summary>
/// Presentation layer dependency injection
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddControllersWithViews();
        RegisterTaskAgent(services, configuration);
        return services;
    }

    /// <summary>
    /// Register Task Agent service
    /// </summary>
    private static void RegisterTaskAgent(IServiceCollection services, IConfiguration configuration)
    {
        var azureOpenAiEndpoint = configuration["AzureOpenAI:Endpoint"];
        var modelDeployment = configuration["AzureOpenAI:DeploymentName"];
        var apiKey = configuration["AzureOpenAI:ApiKey"];

        if (string.IsNullOrWhiteSpace(azureOpenAiEndpoint) || 
            string.IsNullOrWhiteSpace(modelDeployment) || 
            string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Missing required Azure OpenAI configuration. Please check appsettings.json and appsettings.Development.json"
            );
        }

        var client = new AzureOpenAIClient(new Uri(azureOpenAiEndpoint), new AzureKeyCredential(apiKey));

        services.AddScoped<ITaskAgentService>(sp =>
        {
            var taskRepository = sp.GetRequiredService<ITaskRepository>();
            var agent = TaskAgentService.CreateAgent(client, modelDeployment, taskRepository);
            return new TaskAgentService(agent);
        });
    }
}
