namespace TyFi.Auth.OtpMailer.Auth;

public sealed record TokenValidationOutcome(bool IsValid, string? TenantId, string? FailureReason)
{
    public static TokenValidationOutcome Success(string tenantId) => new(true, tenantId, null);

    public static TokenValidationOutcome Failure(string reason) => new(false, null, reason);
}
