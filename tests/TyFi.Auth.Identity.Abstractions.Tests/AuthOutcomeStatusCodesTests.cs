namespace TyFi.Auth.Identity.Abstractions.Tests;

public sealed class AuthOutcomeStatusCodesTests
{
    [Theory]
    [InlineData(VerifyCodeOutcome.Succeeded, 200)]
    [InlineData(VerifyCodeOutcome.InvalidCode, 400)]
    [InlineData(VerifyCodeOutcome.Expired, 400)]
    [InlineData(VerifyCodeOutcome.TooManyAttempts, 429)]
    [InlineData(VerifyCodeOutcome.AccountLocked, 423)]
    public void ForVerifyCode_MapsOutcomeToExpectedStatusCode(VerifyCodeOutcome outcome, int expected)
    {
        Assert.Equal(expected, AuthOutcomeStatusCodes.ForVerifyCode(outcome));
    }

    [Theory]
    [InlineData(RefreshOutcome.Succeeded, 200)]
    [InlineData(RefreshOutcome.InvalidToken, 401)]
    [InlineData(RefreshOutcome.Expired, 401)]
    [InlineData(RefreshOutcome.Reused, 401)]
    public void ForRefresh_MapsOutcomeToExpectedStatusCode(RefreshOutcome outcome, int expected)
    {
        Assert.Equal(expected, AuthOutcomeStatusCodes.ForRefresh(outcome));
    }
}
