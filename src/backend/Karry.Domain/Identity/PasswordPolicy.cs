namespace Karry.Domain.Identity;

/// <summary>
/// Result of password policy evaluation.
/// </summary>
public sealed record PasswordPolicyResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static PasswordPolicyResult Ok() => new(true, []);
}

/// <summary>
/// Enterprise password policy: length, character classes, and admin-set complexity rules.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 10;

    public const int MaxLength = 128;

    public static PasswordPolicyResult Validate(string? password)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(password))
        {
            errors.Add("Password is required.");
            return new PasswordPolicyResult(false, errors);
        }

        if (password.Length < MinLength)
        {
            errors.Add($"Password must be at least {MinLength} characters.");
        }

        if (password.Length > MaxLength)
        {
            errors.Add($"Password must be at most {MaxLength} characters.");
        }

        if (!password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            errors.Add("Password must contain at least one lowercase letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one digit.");
        }

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
        {
            errors.Add("Password must contain at least one special character.");
        }

        return errors.Count == 0 ? PasswordPolicyResult.Ok() : new PasswordPolicyResult(false, errors);
    }
}