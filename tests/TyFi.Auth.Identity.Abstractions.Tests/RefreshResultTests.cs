namespace TyFi.Auth.Identity.Abstractions.Tests;

public sealed class RefreshResultTests
{
    [Fact]
    public void Succeeded_CarriesTokens()
    {
        var tokens = new AuthTokens("access", DateTimeOffset.UtcNow, "refresh", DateTimeOffset.UtcNow);
        var result = RefreshResult.Succeeded(tokens);

        Assert.Equal(RefreshOutcome.Succeeded, result.Outcome);
        Assert.Same(tokens, result.Tokens);
    }

    [Fact]
    public void Succeeded_ThrowsForNullTokens()
    {
        Assert.Throws<ArgumentNullException>(() => RefreshResult.Succeeded(null!));
    }

    [Theory]
    [InlineData(RefreshOutcome.InvalidToken)]
    [InlineData(RefreshOutcome.Expired)]
    [InlineData(RefreshOutcome.Reused)]
    public void Failed_HasNullTokens(RefreshOutcome outcome)
    {
        var result = RefreshResult.Failed(outcome);

        Assert.Equal(outcome, result.Outcome);
        Assert.Null(result.Tokens);
    }

    [Fact]
    public void Failed_RejectsSucceededOutcome()
    {
        Assert.Throws<ArgumentException>(() => RefreshResult.Failed(RefreshOutcome.Succeeded));
    }
}
