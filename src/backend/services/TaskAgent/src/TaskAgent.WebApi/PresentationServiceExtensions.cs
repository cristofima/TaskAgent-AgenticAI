using System.Text.Json;
using System.Text.Json.Serialization;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.OpenApi;
using TaskAgent.Application.Functions;
using TaskAgent.Application.Interfaces;
using TaskAgent.Application.Telemetry;
using TaskAgent.WebApi.Services;

namespace TaskAgent.WebApi;

/// <summary>
/// Presentation layer dependency injection
/// </summary>
public static class PresentationServiceExtensions
{
    public static IServiceCollection AddPresentation(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Configure CORS for Next.js frontend
        string[] allowedOrigins =
            configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000"];

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        // Configure API controllers with JSON options
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                // Use camelCase for JSON properties (JavaScript convention)
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

                // Include null values in JSON responses
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;

                // Pretty print in development
                options.JsonSerializerOptions.WriteIndented = true;

                // Handle reference loops gracefully
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

                // Convert enums to strings for better readability
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                );
            });

        // Configure Swagger/OpenAPI for API documentation
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Title = "TaskAgent API",
                    Version = "v1",
                    Description = "AI-powered task management API with Microsoft Agents Framework",
                    Contact = new OpenApiContact
                    {
                        Name = "TaskAgent Team",
                        Url = new Uri("https://github.com/cristofima/TaskAgent-AgenticAI"),
                    },
                }
            );

            // Include XML comments if available
            string xmlFile =
                $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

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
            IThreadPersistenceService threadPersistence =
                sp.GetRequiredService<IThreadPersistenceService>();
            AgentMetrics metrics = sp.GetRequiredService<AgentMetrics>();
            ILogger<TaskFunctions> functionsLogger = sp.GetRequiredService<
                ILogger<TaskFunctions>
            >();

            AIAgent agent = TaskAgentService.CreateAgent(
                client,
                modelDeployment,
                taskRepository,
                metrics,
                functionsLogger
            );

            return new TaskAgentService(agent, logger, threadPersistence, metrics);
        });
    }
}
