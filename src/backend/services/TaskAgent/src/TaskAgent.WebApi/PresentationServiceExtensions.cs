using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;
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

        // Register presentation layer services
        services.AddScoped<SseStreamingService>();

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
                    Version = "v2.0",
                    Description =
                        "AI-powered task management API with Microsoft Agent Framework and AG-UI Protocol",
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

        return services;
    }
}
