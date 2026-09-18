namespace TyFi.Auth.Identity.Tests;

public sealed class OtpGeneratorTests
{
    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(8)]
    public void Generate_ReturnsCodeOfRequestedLength_ContainingOnlyDigits(int length)
    {
        var generator = new OtpGenerator();
        var code = generator.Generate(length);

        Assert.Equal(length, code.Length);
        Assert.All(code, c => Assert.True(c is >= '0' and <= '9'));
    }

    [Fact]
    public void Generate_ThrowsForNonPositiveLength()
    {
        var generator = new OtpGenerator();
        Assert.Throws<ArgumentOutOfRangeException>(() => generator.Generate(0));
    }
}
