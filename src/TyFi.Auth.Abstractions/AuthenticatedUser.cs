using System.Security.Claims;

namespace TyFi.Auth.Abstractions;

/// <summary>A caller identified by a successfully validated bearer token.</summary>
public sealed record AuthenticatedUser(ClaimsPrincipal Principal, string Issuer, string Subject, string? Name, IReadOnlyList<string> Roles)
{
    /// <summary>Creates an <see cref="AuthenticatedUser"/>, validating the required fields.</summary>
    public static AuthenticatedUser Create(ClaimsPrincipal principal, string issuer, string subject, string? name, IReadOnlyList<string> roles)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentNullException.ThrowIfNull(roles);

        return new AuthenticatedUser(principal, issuer, subject, name, roles);
    }
}
