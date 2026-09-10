namespace TyFi.Auth.Functions.Worker;

/// <summary>The authorization policy resolved for a single function invocation.</summary>
public sealed record AuthorizationRequirement(bool AllowAnonymous, string? RequiredPolicy);
