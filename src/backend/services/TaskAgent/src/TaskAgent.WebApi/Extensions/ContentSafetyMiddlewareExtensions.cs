using TaskAgent.WebApi.Middleware;

namespace TaskAgent.WebApi.Extensions;

/// <summary>
/// Extension methods for middleware registration
/// </summary>
public static class ContentSafetyMiddlewareExtensions
{
    public static IApplicationBuilder UseContentSafety(this IApplicationBuilder builder) =>
        builder.UseMiddleware<ContentSafetyMiddleware>();
}
