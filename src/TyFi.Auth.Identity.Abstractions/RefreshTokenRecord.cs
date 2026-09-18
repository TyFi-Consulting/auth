namespace TyFi.Auth.Identity.Abstractions;

/// <summary>The data <see cref="IRefreshTokenStore"/> persists for one refresh token.</summary>
public sealed class RefreshTokenRecord
{
    /// <summary>Keyed hash of the opaque refresh token. Never store the plaintext token.</summary>
    public required string TokenHash { get; set; }

    /// <summary>Identifies the chain of tokens produced by successive rotations of one login session.</summary>
    public required string FamilyId { get; set; }

    /// <summary>The user this token belongs to.</summary>
    public required string UserId { get; set; }

    /// <summary>When this token expires if never used.</summary>
    public DateTimeOffset ExpiresUtc { get; set; }

    /// <summary>Set once this token has been exchanged for a new one (rotation). A second presentation
    /// of a rotated token is a reuse signal -- see <see cref="IRefreshTokenStore.RevokeFamilyAsync"/>.</summary>
    public DateTimeOffset? RotatedUtc { get; set; }

    /// <summary>Set once this token (and its whole family) has been explicitly revoked, e.g. via logout.</summary>
    public DateTimeOffset? RevokedUtc { get; set; }
}
