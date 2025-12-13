using TaskAgent.ServiceDefaults;

namespace TaskAgent.WebApi.Extensions;

/// <summary>
/// Extension methods for configuring application middleware pipeline.
/// </summary>
public static class MiddlewarePipelineExtensions
{
    /// <summary>
    /// Configures the middleware pipeline based on environment.
    /// </summary>
    public static WebApplication ConfigureMiddlewarePipeline(this WebApplication app)
    {
        app.MapDefaultEndpoints();

        app.ValidateConfiguration();

        if (app.Environment.IsDevelopment())
        {
            ConfigureDevelopmentMiddleware(app);
        }
        else
        {
            ConfigureProductionMiddleware(app);
        }

        ConfigureCommonMiddleware(app);

        return app;
    }

    private static void ConfigureDevelopmentMiddleware(WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskAgent API v1");
            options.RoutePrefix = "swagger";
        });
    }

    private static void ConfigureProductionMiddleware(WebApplication app)
    {
        app.UseHsts();
    }

    private static void ConfigureCommonMiddleware(WebApplication app)
    {
        app.UseHttpsRedirection();

        // Enable CORS with default policy (configured in PresentationServiceExtensions)
        app.UseCors();

        // Authentication must come before Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Map controllers for REST API
        app.MapControllers();

        // Expose agent via AG-UI protocol at /agui endpoint
        // Agent already registered in AddAgentServices
        app.MapAgentEndpoint();
    }
}
