namespace TyFi.Auth.Identity.Abstractions.Tests;

public sealed class VerifyCodeResultTests
{
    [Fact]
    public void Succeeded_CarriesTokens()
    {
        var tokens = new AuthTokens("access", DateTimeOffset.UtcNow, "refresh", DateTimeOffset.UtcNow);
        var result = VerifyCodeResult.Succeeded(tokens);

        Assert.Equal(VerifyCodeOutcome.Succeeded, result.Outcome);
        Assert.Same(tokens, result.Tokens);
    }

    [Fact]
    public void Succeeded_ThrowsForNullTokens()
    {
        Assert.Throws<ArgumentNullException>(() => VerifyCodeResult.Succeeded(null!));
    }

    [Theory]
    [InlineData(VerifyCodeOutcome.InvalidCode)]
    [InlineData(VerifyCodeOutcome.Expired)]
    [InlineData(VerifyCodeOutcome.TooManyAttempts)]
    [InlineData(VerifyCodeOutcome.AccountLocked)]
    public void Failed_HasNullTokens(VerifyCodeOutcome outcome)
    {
        var result = VerifyCodeResult.Failed(outcome);

        Assert.Equal(outcome, result.Outcome);
        Assert.Null(result.Tokens);
    }

    [Fact]
    public void Failed_RejectsSucceededOutcome()
    {
        Assert.Throws<ArgumentException>(() => VerifyCodeResult.Failed(VerifyCodeOutcome.Succeeded));
    }
}
