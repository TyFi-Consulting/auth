namespace TyFi.Auth.Identity;

/// <summary>Normalizes email addresses to a canonical form used as the sign-in/lookup key.</summary>
public interface IEmailNormalizer
{
    /// <summary>Returns the canonical (trimmed, lowercased) form of the given email address.</summary>
    string Normalize(string email);
}

/// <inheritdoc cref="IEmailNormalizer" />
public sealed class EmailNormalizer : IEmailNormalizer
{
    /// <inheritdoc />
    public string Normalize(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return email.Trim().ToLowerInvariant();
    }
}
