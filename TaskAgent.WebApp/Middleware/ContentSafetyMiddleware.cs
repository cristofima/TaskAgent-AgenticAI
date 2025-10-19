using System.Text.Json;
using TaskAgent.Application.DTOs;
using TaskAgent.Application.Interfaces;
using TaskAgent.WebApp.Constants;
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
            // Extract and store the request for reuse
            (string? userMessage, ChatRequestDto? chatRequest) = await ExtractUserMessageAsync(
                context
            );

            if (userMessage == null)
            {
                await _next(context);
                return;
            }

            // Store the parsed request in HttpContext.Items so the controller can reuse it
            context.Items["ChatRequest"] = chatRequest;

            // Execute both safety checks in parallel for better performance
            Task<PromptInjectionResult> injectionTask =
                contentSafetyService.DetectPromptInjectionAsync(userMessage);
            Task<ContentSafetyResult> contentTask = contentSafetyService.AnalyzeTextAsync(
                userMessage
            );

            // Wait for both tasks to complete
            await Task.WhenAll(injectionTask, contentTask);

            // Get results (tasks are already completed, no additional await needed)
            PromptInjectionResult injectionResult = await injectionTask;
            ContentSafetyResult contentResult = await contentTask;

            // Check prompt injection first (security priority)
            if (!injectionResult.IsSafe)
            {
                _logger.LogWarning(
                    "Prompt injection blocked: {Type}",
                    injectionResult.DetectedAttackType
                );
                await BlockSecurityViolationAsync(context, injectionResult);
                return;
            }

            // Then check content safety
            if (!contentResult.IsSafe)
            {
                _logger.LogWarning(
                    "Content policy violation: {Categories}",
                    string.Join(", ", contentResult.ViolatedCategories ?? [])
                );
                await BlockContentViolationAsync(context, contentResult);
                return;
            }

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
        return context.Request.Path.StartsWithSegments($"/{ApiRoutes.CHAT}")
            && context.Request.Method == HttpMethods.Post;
    }

    /// <summary>
    /// Extracts user message from request body
    /// </summary>
    private static async Task<(string? message, ChatRequestDto? request)> ExtractUserMessageAsync(
        HttpContext context
    )
    {
        context.Request.EnableBuffering();

        // Read the request body
        using var reader = new StreamReader(
            context.Request.Body,
            encoding: System.Text.Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true // Critical: Don't close the stream
        );

        string requestBody = await reader.ReadToEndAsync();

        // Reset stream position for next middleware/controller
        context.Request.Body.Seek(0, SeekOrigin.Begin);

        if (string.IsNullOrWhiteSpace(requestBody))
        {
            return (null, null);
        }

        ChatRequestDto? chatRequest = JsonSerializer.Deserialize<ChatRequestDto>(
            requestBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        return (chatRequest?.Message, chatRequest);
    }

    /// <summary>
    /// Blocks request due to security violation
    /// </summary>
    private async Task BlockSecurityViolationAsync(
        HttpContext context,
        PromptInjectionResult result
    )
    {
        _logger.LogWarning("Prompt injection blocked: {AttackType}", result.DetectedAttackType);

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(
            new
            {
                error = ErrorCodes.PROMPT_INJECTION_DETECTED,
                message = "Your message was flagged as attempting to manipulate the system. "
                    + "Please rephrase your request for legitimate task management.",
                details = result.DetectedAttackType,
            }
        );
    }

    /// <summary>
    /// Blocks request due to content policy violation
    /// </summary>
    private async Task BlockContentViolationAsync(HttpContext context, ContentSafetyResult result)
    {
        _logger.LogWarning(
            "Content violation blocked: {Violations}",
            string.Join(", ", result.ViolatedCategories ?? [])
        );

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(
            new
            {
                error = ErrorCodes.CONTENT_POLICY_VIOLATION,
                message = "Your message contains content that violates our policy. "
                    + "Please rephrase your request.",
                violations = result.ViolatedCategories,
                categoryScores = result.CategoryScores,
            }
        );
    }
}
