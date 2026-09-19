using System.Security.Cryptography;

namespace TyFi.Auth.Identity;

/// <summary>An opaque refresh token issuance: the plaintext token to return to the caller and its family id.</summary>
public readonly record struct RefreshTokenIssuance(string PlainTextToken, string FamilyId);

/// <summary>Generates opaque refresh tokens.</summary>
public interface IRefreshTokenGenerator
{
    /// <summary>
    /// Generates a new refresh token. Pass the previous token's family id when rotating an existing
    /// session so the whole chain shares one family id; omit it (or pass null) for a brand-new login.
    /// </summary>
    RefreshTokenIssuance Generate(string? existingFamilyId);
}

/// <inheritdoc cref="IRefreshTokenGenerator" />
public sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    /// <inheritdoc />
    public RefreshTokenIssuance Generate(string? existingFamilyId)
    {
        var tokenBytes = new byte[32];
        RandomNumberGenerator.Fill(tokenBytes);
        var plainTextToken = Convert.ToBase64String(tokenBytes);
        var familyId = string.IsNullOrEmpty(existingFamilyId) ? Guid.NewGuid().ToString("N") : existingFamilyId;
        return new RefreshTokenIssuance(plainTextToken, familyId);
    }
}
