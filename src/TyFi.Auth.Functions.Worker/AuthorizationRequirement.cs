namespace TyFi.Auth.Functions.Worker;

/// <summary>The authorization policy resolved for a single function invocation.</summary>
/// <param name="AllowAnonymous">True when the function carries <c>[AllowAnonymous]</c>.</param>
/// <param name="RequiredPolicy">The <c>[Authorize(Policy = "...")]</c> value, or null.</param>
/// <remarks>
/// <see cref="RequireAuthenticatedOnly"/> is an init-only property rather than a positional
/// parameter to preserve the original two-argument constructor and Deconstruct signature for
/// existing consumers.
/// </remarks>
public sealed record AuthorizationRequirement(
    bool AllowAnonymous,
    string? RequiredPolicy)
{
    /// <summary>
    /// True when the function carries <c>[AuthorizeAuthenticated]</c> and no explicit policy — the
    /// caller must be authenticated, but no specific role is required.
    /// </summary>
    public bool RequireAuthenticatedOnly { get; init; }
}
