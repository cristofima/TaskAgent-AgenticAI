using System.Text.Json;
using TaskAgent.Application.DTOs;
using TaskAgent.Application.Interfaces;
using TaskAgent.WebApp.Models;

namespace TaskAgent.WebApp.Middleware;

/// <summary>
/// Middleware that automatically applies content safety checks to AI agent interactions
/// </summary>
public class ContentSafetyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ContentSafetyMiddleware> _logger;

    public ContentSafetyMiddleware(RequestDelegate next, ILogger<ContentSafetyMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, IContentSafetyService contentSafetyService)
    {
        if (!ShouldApplySafetyChecks(context))
        {
            await _next(context);
            return;
        }

        try
        {
            var userMessage = await ExtractUserMessage(context);
            if (userMessage == null)
            {
                await _next(context);
                return;
            }

            _logger.LogInformation("Applying 2-layer content safety checks (parallel execution)");

            // Execute both safety checks in parallel for better performance
            var injectionTask = contentSafetyService.DetectPromptInjectionAsync(userMessage);
            var contentTask = contentSafetyService.AnalyzeTextAsync(userMessage);

            // Wait for both tasks to complete
            await Task.WhenAll(injectionTask, contentTask);

            // Get results (tasks are already completed, no additional await needed)
            var injectionResult = await injectionTask;
            var contentResult = await contentTask;

            _logger.LogInformation(
                "Prompt Shield result - AttackDetected: {Detected}, Type: {Type}",
                injectionResult.InjectionDetected,
                injectionResult.DetectedAttackType
            );

            _logger.LogInformation(
                "Content moderation result - IsSafe: {Safe}, Violations: {Count}",
                contentResult.IsSafe,
                contentResult.ViolatedCategories?.Count ?? 0
            );

            // Check prompt injection first (security priority)
            if (!injectionResult.IsSafe)
            {
                _logger.LogWarning("Blocking request - Prompt injection detected");
                await BlockSecurityViolation(context, injectionResult);
                return;
            }

            // Then check content safety
            if (!contentResult.IsSafe)
            {
                _logger.LogWarning("Blocking request - Content policy violation");
                await BlockContentViolation(context, contentResult);
                return;
            }

            _logger.LogInformation("Content safety checks passed");
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in content safety middleware");
            await _next(context);
        }
    }

    /// <summary>
    /// Determines if safety checks should apply to this request
    /// </summary>
    private static bool ShouldApplySafetyChecks(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments("/api/chat")
            && context.Request.Method == HttpMethods.Post;
    }

    /// <summary>
    /// Extracts user message from request body
    /// </summary>
    private static async Task<string?> ExtractUserMessage(HttpContext context)
    {
        context.Request.EnableBuffering();
        var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Position = 0;

        var chatRequest = JsonSerializer.Deserialize<ChatRequestDto>(
            requestBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        return chatRequest?.Message;
    }

    /// <summary>
    /// Blocks request due to security violation
    /// </summary>
    private async Task BlockSecurityViolation(HttpContext context, PromptInjectionResult result)
    {
        _logger.LogWarning("Prompt injection blocked: {AttackType}", result.DetectedAttackType);

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(
            new
            {
                error = "SecurityViolation",
                message = "Your message was flagged as attempting to manipulate the system. "
                    + "Please rephrase your request for legitimate task management.",
                details = result.DetectedAttackType,
            }
        );
    }

    /// <summary>
    /// Blocks request due to content policy violation
    /// </summary>
    private async Task BlockContentViolation(HttpContext context, ContentSafetyResult result)
    {
        _logger.LogWarning(
            "Content violation blocked: {Violations}",
            string.Join(", ", result.ViolatedCategories ?? new List<string>())
        );

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(
            new
            {
                error = "ContentPolicyViolation",
                message = "Your message contains content that violates our policy. "
                    + "Please rephrase your request.",
                violations = result.ViolatedCategories,
                categoryScores = result.CategoryScores,
            }
        );
    }
}
