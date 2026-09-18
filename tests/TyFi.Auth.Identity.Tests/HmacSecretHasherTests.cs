namespace TyFi.Auth.Identity.Tests;

public sealed class HmacSecretHasherTests
{
    private static HmacSecretHasher CreateHasher(string base64Key = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")
        => new(Microsoft.Extensions.Options.Options.Create(new Abstractions.AuthIdentityOptions { HashingKey = base64Key }));

    [Fact]
    public void Hash_IsDeterministic_ForSameSecret()
    {
        var hasher = CreateHasher();
        Assert.Equal(hasher.Hash("123456"), hasher.Hash("123456"));
    }

    [Fact]
    public void Hash_DiffersAcrossKeys()
    {
        var first = CreateHasher("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
        var second = CreateHasher("QUJDREVGR0hJSktMTU5PUFFSU1RVVldYWVoxMjM0NTY=");

        Assert.NotEqual(first.Hash("123456"), second.Hash("123456"));
    }

    [Fact]
    public void Verify_ReturnsTrue_ForMatchingSecret()
    {
        var hasher = CreateHasher();
        var hash = hasher.Hash("123456");

        Assert.True(hasher.Verify("123456", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForNonMatchingSecret()
    {
        var hasher = CreateHasher();
        var hash = hasher.Hash("123456");

        Assert.False(hasher.Verify("654321", hash));
    }
}
