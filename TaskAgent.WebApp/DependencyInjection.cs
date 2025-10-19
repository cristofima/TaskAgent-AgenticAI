using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
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
        string? azureOpenAiEndpoint = configuration["AzureOpenAI:Endpoint"];
        string? modelDeployment = configuration["AzureOpenAI:DeploymentName"];
        string? apiKey = configuration["AzureOpenAI:ApiKey"];

        if (
            string.IsNullOrWhiteSpace(azureOpenAiEndpoint)
            || string.IsNullOrWhiteSpace(modelDeployment)
            || string.IsNullOrWhiteSpace(apiKey)
        )
        {
            throw new InvalidOperationException(
                "Missing required Azure OpenAI configuration. Please check appsettings.json and appsettings.Development.json"
            );
        }

        var client = new AzureOpenAIClient(
            new Uri(azureOpenAiEndpoint),
            new AzureKeyCredential(apiKey)
        );

        services.AddScoped<ITaskAgentService>(sp =>
        {
            ITaskRepository taskRepository = sp.GetRequiredService<ITaskRepository>();
            ILogger<TaskAgentService> logger = sp.GetRequiredService<ILogger<TaskAgentService>>();
            IThreadPersistenceService threadPersistence = sp.GetRequiredService<IThreadPersistenceService>();
            AIAgent agent = TaskAgentService.CreateAgent(client, modelDeployment, taskRepository);
            return new TaskAgentService(agent, logger, threadPersistence);
        });
    }
}
