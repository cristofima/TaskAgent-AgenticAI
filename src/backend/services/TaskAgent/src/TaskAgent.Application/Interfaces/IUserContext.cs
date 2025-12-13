namespace TaskAgent.Application.Interfaces;

/// <summary>
/// Provides access to the current authenticated user context
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Gets the unique identifier of the current authenticated user (from Microsoft Entra ID)
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Thrown when no user is authenticated</exception>
    string UserId { get; }

    /// <summary>
    /// Gets the email address of the current authenticated user (if available)
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets the display name of the current authenticated user (if available)
    /// </summary>
    string? DisplayName { get; }

    /// <summary>
    /// Returns true if the current request has an authenticated user
    /// </summary>
    bool IsAuthenticated { get; }
}
