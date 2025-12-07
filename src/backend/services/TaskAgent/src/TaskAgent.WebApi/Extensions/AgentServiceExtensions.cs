using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using TaskAgent.Application.Functions;
using TaskAgent.Application.Telemetry;
using TaskAgent.Infrastructure.Data;
using TaskAgent.Infrastructure.MessageStores;
using TaskAgent.WebApi.Constants;
using TaskAgent.WebApi.Services;

namespace TaskAgent.WebApi.Extensions;

/// <summary>
/// Provides AG-UI integration setup with Agent Framework and <c>ChatMessageStore</c> pattern.
/// </summary>
public static class AgentServiceExtensions
{
    /// <summary>
    /// Registers AG-UI services, HttpClient, and AIAgent with automatic message persistence.
    /// </summary>
    public static IServiceCollection AddAgentServices(this IServiceCollection services, IConfiguration configuration)
    {
        // AG-UI requires HttpClient for protocol communication
        services.AddHttpClient();

        // Register AG-UI protocol services
        services.AddAGUI();

        // Register FunctionDescriptionProvider as singleton for dynamic status messages
        services.AddSingleton<FunctionDescriptionProvider>(sp =>
        {
            var provider = new FunctionDescriptionProvider();
            // Register TaskFunctions type to extract [Description] attributes
            provider.RegisterFunctionType(typeof(TaskFunctions));
            return provider;
        });

        // Register AIAgent as singleton for DI (used by AgentController)
        services.AddSingleton<AIAgent>(serviceProvider =>
        {
            // Validate Azure OpenAI configuration
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
                    "Missing required Azure OpenAI configuration (Endpoint, DeploymentName, ApiKey). "
                        + "Please check appsettings.json and appsettings.Development.json"
                );
            }

            // Create Azure OpenAI client
            var azureClient = new AzureOpenAIClient(
                new Uri(azureOpenAiEndpoint),
                new AzureKeyCredential(apiKey)
            );

            ChatClient openAIChatClient = azureClient.GetChatClient(modelDeployment);
            IChatClient chatClient = openAIChatClient.AsIChatClient();

            // Create agent with function tools and message store factory
            return CreateAgentWithToolsAndStore(serviceProvider, chatClient);
        });

        return services;
    }

    /// <summary>
    /// Maps the AG-UI protocol endpoint.
    /// </summary>
    public static WebApplication MapAgentEndpoint(
        this WebApplication app,
        string endpoint = ApiRoutes.AGUI
    )
    {
        // Get agent from DI
        AIAgent agent = app.Services.GetRequiredService<AIAgent>();

        // Map AG-UI protocol endpoint
        app.MapAGUI(endpoint, agent);

        return app;
    }

    /// <summary>
    /// Creates an AIAgent with function tools and <c>ChatMessageStore</c> factory for automatic persistence.
    /// </summary>
    /// <remarks>
    /// Tools use <see cref="IServiceProvider"/> to resolve scoped dependencies per-call.
    /// </remarks>
    private static ChatClientAgent CreateAgentWithToolsAndStore(
        IServiceProvider serviceProvider,
        IChatClient chatClient
    )
    {
        // Resolve singleton services (safe to use across requests)
        AgentMetrics metrics = serviceProvider.GetRequiredService<AgentMetrics>();
        ILogger<TaskFunctions> functionsLogger =
            serviceProvider.GetRequiredService<ILogger<TaskFunctions>>();

        // Create TaskFunctions with IServiceProvider for per-call scoped dependency resolution
        // This ensures each function call creates a new scope and resolves fresh DbContext
        var taskFunctions = new TaskFunctions(serviceProvider, metrics, functionsLogger);

        // Create function tools using AIFunctionFactory
        AIFunction createTaskTool = AIFunctionFactory.Create(taskFunctions.CreateTaskAsync);
        AIFunction listTasksTool = AIFunctionFactory.Create(taskFunctions.ListTasksAsync);
        AIFunction getTaskDetailsTool = AIFunctionFactory.Create(
            taskFunctions.GetTaskDetailsAsync
        );
        AIFunction updateTaskTool = AIFunctionFactory.Create(taskFunctions.UpdateTaskAsync);
        AIFunction deleteTaskTool = AIFunctionFactory.Create(taskFunctions.DeleteTaskAsync);
        AIFunction getTaskSummaryTool = AIFunctionFactory.Create(
            taskFunctions.GetTaskSummaryAsync
        );

        // Create ChatMessageStore factory for automatic persistence
        // Factory creates new store instance for each thread
        // Follows Microsoft best practices: https://learn.microsoft.com/en-us/agent-framework/tutorials/agents/third-party-chat-history-storage
        ChatMessageStore MessageStoreFactory(ChatClientAgentOptions.ChatMessageStoreFactoryContext ctx)
        {
            // Create a new scope for the message store
            // The store will manage its own DbContext lifetime
            IServiceScope scope = serviceProvider.CreateScope();
            ConversationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ConversationDbContext>();

            return new PostgresChatMessageStore(dbContext, ctx.SerializedState, ctx.JsonSerializerOptions);
        }

        // Create ChatOptions with tools (function calling)
        var chatOptions = new ChatOptions
        {
            Tools =
            [
                createTaskTool,
                listTasksTool,
                getTaskDetailsTool,
                updateTaskTool,
                deleteTaskTool,
                getTaskSummaryTool,
            ]
        };

        // Create and return agent with tools via ChatOptions and message store
        return chatClient.CreateAIAgent(
            new ChatClientAgentOptions
            {
                Instructions = AgentInstructions.TASK_AGENT_INSTRUCTIONS,
                ChatOptions = chatOptions,
                ChatMessageStoreFactory = MessageStoreFactory
            }
        );
    }
}
