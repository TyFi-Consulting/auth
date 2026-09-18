using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TyFi.Auth.Identity.Abstractions;

namespace TyFi.Auth.Identity;

/// <summary>Produces and verifies keyed hashes of one-time codes and refresh tokens for storage.</summary>
public interface ISecretHasher
{
    /// <summary>Computes the keyed hash of a secret, encoded as base64.</summary>
    string Hash(string secret);

    /// <summary>Verifies a secret against a previously computed hash using a constant-time comparison.</summary>
    bool Verify(string secret, string expectedHash);
}

/// <inheritdoc cref="ISecretHasher" />
public sealed class HmacSecretHasher : ISecretHasher
{
    private readonly byte[] _key;

    /// <summary>Creates the hasher using the configured <see cref="AuthIdentityOptions.HashingKey"/>.</summary>
    public HmacSecretHasher(IOptions<AuthIdentityOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Value.HashingKey);
        _key = Convert.FromBase64String(options.Value.HashingKey);
    }

    /// <inheritdoc />
    public string Hash(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        using var hmac = new HMACSHA256(_key);
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(secret));
        return Convert.ToBase64String(bytes);
    }

    /// <inheritdoc />
    public bool Verify(string secret, string expectedHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedHash);

        var actualBytes = Convert.FromBase64String(Hash(secret));
        var expectedBytes = Convert.FromBase64String(expectedHash);
        return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }
}
