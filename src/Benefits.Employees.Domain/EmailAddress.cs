using System.Net.Mail;

namespace Benefits.Employees.Domain;

public sealed record EmailAddress
{
    private const int MaxLength = 254; // RFC 5321 path limit

    public string Value { get; }

    private EmailAddress(string value) => Value = value;

    public static bool TryCreate(string? input, out EmailAddress? emailAddress)
    {
        emailAddress = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var trimmed = input.Trim();
        if (trimmed.Length > MaxLength)
        {
            return false;
        }
        
        if (!MailAddress.TryCreate(trimmed, out var parsed)
            || !string.Equals(parsed.Address, trimmed, StringComparison.Ordinal)
            || !parsed.Host.Contains('.', StringComparison.Ordinal)
            || parsed.Host.StartsWith('.') || parsed.Host.EndsWith('.'))
        {
            return false;
        }

        emailAddress = new EmailAddress(trimmed.ToLowerInvariant());
        return true;
    }

    public override string ToString() => Value;
}
