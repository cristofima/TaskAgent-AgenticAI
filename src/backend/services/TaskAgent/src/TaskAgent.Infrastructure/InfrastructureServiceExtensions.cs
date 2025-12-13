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
        // Register user context service (requires HTTP context from WebApi layer)
        services.AddScoped<IUserContext, HttpUserContext>();

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

        return services;
    }
}
