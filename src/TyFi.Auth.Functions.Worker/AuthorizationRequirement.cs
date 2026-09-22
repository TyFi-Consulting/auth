namespace TyFi.Auth.Functions.Worker;

/// <summary>The authorization policy resolved for a single function invocation.</summary>
/// <param name="AllowAnonymous">True when the function carries <c>[AllowAnonymous]</c>.</param>
/// <param name="RequiredPolicy">The <c>[Authorize(Policy = "...")]</c> value, or null.</param>
/// <param name="RequireAuthenticatedOnly">
/// True when the function carries <c>[AuthorizeAuthenticated]</c> and no explicit policy — the
/// caller must be authenticated, but no specific role is required.
/// </param>
public sealed record AuthorizationRequirement(
    bool AllowAnonymous,
    string? RequiredPolicy,
    bool RequireAuthenticatedOnly = false);
