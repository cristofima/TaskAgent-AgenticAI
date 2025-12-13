using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using TaskAgent.WebApi.Extensions;
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

        // HTTP Context accessor for user context in Infrastructure layer
        services.AddHttpContextAccessor();

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

        // Configure Microsoft Entra ID authentication
        services.AddEntraIdAuthentication(configuration);

        // Configure Swagger/OpenAPI for API documentation with JWT Bearer auth
        services.AddEndpointsApiExplorer();
        services.AddSwaggerWithAuth();

        return services;
    }
}
