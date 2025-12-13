using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaskAgent.Application.Interfaces;

namespace TaskAgent.Infrastructure.Services;

/// <summary>
/// Provides access to the current authenticated user from HTTP context
/// </summary>
public class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Returns the user ID from claims, or throws if not authenticated.
    /// The exception is suppressed via attribute as this is intentional behavior.
    /// </remarks>
    [SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations",
        Justification = "UserId is required for authenticated operations; throwing is intentional")]
    public string UserId
    {
        get
        {
            var userId = GetClaimValue(ClaimTypes.NameIdentifier)
                ?? GetClaimValue("oid") // Object ID in Azure AD tokens
                ?? GetClaimValue("sub"); // Subject claim

            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated or UserId claim is missing");
            }

            return userId;
        }
    }

    /// <inheritdoc />
    public string? Email => GetClaimValue(ClaimTypes.Email)
        ?? GetClaimValue("preferred_username")
        ?? GetClaimValue("email");

    /// <inheritdoc />
    public string? DisplayName => GetClaimValue(ClaimTypes.Name)
        ?? GetClaimValue("name");

    /// <inheritdoc />
    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    private string? GetClaimValue(string claimType)
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value;
    }
}
