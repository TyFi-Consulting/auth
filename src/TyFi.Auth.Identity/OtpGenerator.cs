using System.Security.Cryptography;

namespace TyFi.Auth.Identity;

/// <summary>Generates cryptographically random numeric one-time codes.</summary>
public interface IOtpGenerator
{
    /// <summary>Generates a random numeric code of the given length.</summary>
    string Generate(int length);
}

/// <inheritdoc cref="IOtpGenerator" />
public sealed class OtpGenerator : IOtpGenerator
{
    /// <inheritdoc />
    public string Generate(int length)
    {
        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "Code length must be positive.");
        }

        var buffer = new byte[length];
        RandomNumberGenerator.Fill(buffer);

        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = (char)('0' + (buffer[i] % 10));
        }

        return new string(chars);
    }
}
