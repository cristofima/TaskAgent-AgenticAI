using TaskAgent.WebApp.Middleware;

namespace TaskAgent.WebApp.Extensions;

/// <summary>
/// Extension methods for middleware registration
/// </summary>
public static class ContentSafetyMiddlewareExtensions
{
    public static IApplicationBuilder UseContentSafety(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ContentSafetyMiddleware>();
    }
}
