using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi;

namespace TaskAgent.WebApi.Extensions;

/// <summary>
/// Extension methods for configuring Microsoft Entra ID authentication
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds Microsoft Entra ID JWT Bearer authentication to the service collection
    /// </summary>
    public static IServiceCollection AddEntraIdAuthentication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Configure Microsoft Entra ID (Azure AD) authentication
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));

        // Add authorization services
        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Configures Swagger to support JWT Bearer authentication
    /// </summary>
    public static void AddSwaggerWithAuth(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Title = "TaskAgent API",
                    Version = "v2.2",
                    Description =
                        "AI-powered task management API with Microsoft Agent Framework, AG-UI Protocol, and Microsoft Entra ID authentication",
                    Contact = new OpenApiContact
                    {
                        Name = "TaskAgent Team",
                        Url = new Uri("https://github.com/cristofima/TaskAgent-AgenticAI"),
                    },
                }
            );

            // Add JWT Bearer authentication to Swagger
            options.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description =
                        "JWT Authorization header using the Bearer scheme. Enter your token in the text input below.\n\nExample: \"eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...\"",
                }
            );

            options.AddSecurityRequirement(
                document => new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference("Bearer", document),
                        new List<string>()
                    },
                }
            );

            // Include XML comments for better API documentation
            string xmlFilename = $"{typeof(AuthenticationExtensions).Assembly.GetName().Name}.xml";
            string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });
    }
}
