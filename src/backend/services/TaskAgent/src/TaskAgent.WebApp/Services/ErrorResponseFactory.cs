using Microsoft.AspNetCore.Mvc;
using TaskAgent.Application.DTOs;

namespace TaskAgent.WebApp.Services;

/// <summary>
/// Factory for creating standardized error responses
/// </summary>
public static class ErrorResponseFactory
{
    /// <summary>
    /// Creates a BadRequest (400) response with structured error data
    /// </summary>
    public static IActionResult CreateBadRequest(
        string error,
        string message,
        object? details = null
    )
    {
        return new BadRequestObjectResult(
            new ErrorResponse
            {
                Error = error,
                Message = message,
                Details = details,
            }
        );
    }

    /// <summary>
    /// Creates an InternalServerError (500) response
    /// </summary>
    public static IActionResult CreateInternalServerError(string message, object? details = null)
    {
        return new ObjectResult(
            new ErrorResponse
            {
                Error = "InternalServerError",
                Message = message,
                Details = details,
            }
        )
        {
            StatusCode = StatusCodes.Status500InternalServerError,
        };
    }
}
