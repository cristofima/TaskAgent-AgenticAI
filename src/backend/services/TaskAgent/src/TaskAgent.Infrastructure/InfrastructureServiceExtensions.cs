using Azure;
using Azure.AI.ContentSafety;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskAgent.Application.Interfaces;
using TaskAgent.Infrastructure.Data;
using TaskAgent.Infrastructure.Repositories;
using TaskAgent.Infrastructure.Services;

namespace TaskAgent.Infrastructure;

/// <summary>
/// Infrastructure layer dependency injection
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // SQL Server DbContext for Tasks
        string? tasksConnectionString = configuration.GetConnectionString("TasksConnection");
        if (string.IsNullOrWhiteSpace(tasksConnectionString))
        {
            throw new InvalidOperationException(
                "TasksConnection string is required. Please configure ConnectionStrings:TasksConnection in appsettings.json."
            );
        }

        services.AddDbContext<TaskDbContext>(options =>
            options.UseSqlServer(tasksConnectionString)
        );

        // Register repositories
        services.AddScoped<ITaskRepository, TaskRepository>();

        // PostgreSQL DbContext for Conversations (ChatMessageStore pattern)
        string? conversationsConnectionString = configuration.GetConnectionString(
            "ConversationsConnection"
        );
        if (string.IsNullOrWhiteSpace(conversationsConnectionString))
        {
            throw new InvalidOperationException(
                "ConversationsConnection string is required. Please configure ConnectionStrings:ConversationsConnection in appsettings.json."
            );
        }

        services.AddDbContext<ConversationDbContext>(options =>
            options.UseNpgsql(conversationsConnectionString)
        );

        // Register application services (implementations in Infrastructure)
        services.AddScoped<IAgentStreamingService, AgentStreamingService>();
        services.AddScoped<IConversationService, ConversationService>();

        RegisterContentSafety(services, configuration);

        return services;
    }

    /// <summary>
    /// Register Azure Content Safety infrastructure
    /// </summary>
    private static void RegisterContentSafety(
        IServiceCollection services,
        IConfiguration configuration
    )
    {
        string? contentSafetyEndpoint = configuration["ContentSafety:Endpoint"];
        string? contentSafetyApiKey = configuration["ContentSafety:ApiKey"];

        if (
            string.IsNullOrWhiteSpace(contentSafetyEndpoint)
            || string.IsNullOrWhiteSpace(contentSafetyApiKey)
        )
        {
            throw new InvalidOperationException(
                "Content Safety configuration is required. Please configure ContentSafety:Endpoint and ContentSafety:ApiKey."
            );
        }

        var contentSafetyClient = new ContentSafetyClient(
            new Uri(contentSafetyEndpoint),
            new AzureKeyCredential(contentSafetyApiKey)
        );
        services.AddSingleton(contentSafetyClient);

        services.AddHttpClient(
            "ContentSafety",
            client =>
            {
                client.BaseAddress = new Uri(contentSafetyEndpoint);
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", contentSafetyApiKey);
            }
        );

        services.AddScoped<IContentSafetyService, ContentSafetyService>();
    }
}
