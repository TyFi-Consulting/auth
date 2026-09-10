using TyFi.Auth.Abstractions;

namespace TyFi.Auth.Abstractions.Tests;

public sealed class AuthorizationHeaderParserTests
{
    private readonly AuthorizationHeaderParser _parser = new();

    [Fact]
    public void ParseBearerToken_ReturnsToken_ForValidHeader()
    {
        Assert.Equal("abc123", _parser.ParseBearerToken("Bearer abc123"));
    }

    [Fact]
    public void ParseBearerToken_IsCaseInsensitive_ForBearerScheme()
    {
        Assert.Equal("abc123", _parser.ParseBearerToken("bearer abc123"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Basic abc123")]
    [InlineData("Bearer ")]
    [InlineData("Bearer    ")]
    public void ParseBearerToken_ReturnsNull_ForInvalidHeader(string? header)
    {
        Assert.Null(_parser.ParseBearerToken(header));
    }
}
