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
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        string? connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<TaskDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<ITaskRepository, TaskRepository>();

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

        try
        {
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
                    client.DefaultRequestHeaders.Add(
                        "Ocp-Apim-Subscription-Key",
                        contentSafetyApiKey
                    );
                }
            );

            services.AddScoped<IContentSafetyService, ContentSafetyService>();

            Console.WriteLine("INFO: Azure Content Safety enabled");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WARNING: Failed to initialize Content Safety: {ex.Message}");
        }
    }
}
