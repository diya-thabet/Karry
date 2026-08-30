namespace Karry.Domain.Identity;

/// <summary>
/// Validated email address value object. Immutable structural equality.
/// </summary>
public sealed class EmailAddress : Common.ValueObject
{
    private const int MaxLength = 254;

    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static EmailAddress Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        var trimmed = email.Trim();

        if (trimmed.Length > MaxLength)
        {
            throw new ArgumentException($"Email must be at most {MaxLength} characters.", nameof(email));
        }

        // Basic structural validation: exactly one @, non-empty local/domain, no spaces.
        if (trimmed.Count(c => c == '@') != 1)
        {
            throw new ArgumentException("Email must contain exactly one '@'.", nameof(email));
        }

        var parts = trimmed.Split('@');
        var local = parts[0];
        var domain = parts[1];

        if (local.Length == 0 || local.Length > 64)
        {
            throw new ArgumentException("Email local part is invalid.", nameof(email));
        }

        if (domain.Length < 2 || domain.Length > 255 || domain.Contains(' '))
        {
            throw new ArgumentException("Email domain part is invalid.", nameof(email));
        }

        if (local.Any(char.IsWhiteSpace) || domain.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("Email must not contain whitespace.", nameof(email));
        }

        return new EmailAddress(trimmed.ToLowerInvariant());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}