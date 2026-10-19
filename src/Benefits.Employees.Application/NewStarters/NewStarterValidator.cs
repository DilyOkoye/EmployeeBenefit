using System.Globalization;
using System.Text.RegularExpressions;
using Benefits.Employees.Domain;

namespace Benefits.Employees.Application.NewStarters;

/// <summary>
/// Parses raw input into a <see cref="NewStarter"/>, collecting every error keyed by the JSON field path
/// (e.g. <c>address.postcode</c>) so a frontend can show messages next to the right inputs.
/// </summary>
public static partial class NewStarterValidator
{
    private const int MinimumAgeInYears = 16;
    private const int MaximumAgeInYears = 100;
    public const int MaxNameLength = 100;
    private const int MaxAddressLineLength = 100;
    private const string DateFormat = "yyyy-MM-dd";

    public static ValidationResult<NewStarter> Validate(RegisterNewStarter input, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(input);
        var errors = new Dictionary<string, List<string>>();

        var title = ParseEnum<Title>(input.Title, "title", errors);
        var firstName = RequiredText(input.FirstName, "firstName", MaxNameLength, errors);
        var surname = RequiredText(input.Surname, "surname", MaxNameLength, errors);
        var dateOfBirth = ParseDateOfBirth(input.DateOfBirth, today, errors);
        var gender = ParseEnum<Gender>(input.Gender, "gender", errors);
        var email = ParseEmail(input.Email, errors);
        var address = ParseAddress(input.Address, errors);

        if (errors.Count > 0)
        {
            return ValidationResult<NewStarter>.Failure(
                errors.ToDictionary(e => e.Key, e => e.Value.ToArray(), StringComparer.Ordinal));
        }

        return ValidationResult<NewStarter>.Success(new NewStarter(
            title!.Value, firstName!, surname!, dateOfBirth!.Value, gender!.Value, email!, address!));
    }

    private static TEnum? ParseEnum<TEnum>(string? value, string field, Dictionary<string, List<string>> errors)
        where TEnum : struct, Enum
    {
        var allowed = Enum.GetNames<TEnum>();
        if (string.IsNullOrWhiteSpace(value))
        {
            Add(errors, field, $"'{field}' is required. Allowed values: {string.Join(", ", allowed)}.");
            return null;
        }

        // Match on names only: Enum.TryParse would also accept numbers such as "1" or "1,2".
        var match = Array.Find(allowed, n => string.Equals(n, value.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            Add(errors, field, $"'{value}' is not a valid {field}. Allowed values: {string.Join(", ", allowed)}.");
            return null;
        }

        return Enum.Parse<TEnum>(match);
    }

    private static string? RequiredText(string? value, string field, int maxLength, Dictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Add(errors, field, $"'{field}' is required.");
            return null;
        }

        return OptionalText(value, field, maxLength, errors);
    }

    private static string? OptionalText(string? value, string field, int maxLength, Dictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            Add(errors, field, $"'{field}' must be {maxLength} characters or fewer.");
            return null;
        }

        return trimmed;
    }

    private static DateOnly? ParseDateOfBirth(string? value, DateOnly today, Dictionary<string, List<string>> errors)
    {
        const string field = "dateOfBirth";
        if (string.IsNullOrWhiteSpace(value))
        {
            Add(errors, field, $"'{field}' is required in the format {DateFormat}.");
            return null;
        }

        if (!DateOnly.TryParseExact(value.Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
        {
            Add(errors, field, $"'{value}' is not a valid date. Use the format {DateFormat}.");
            return null;
        }

        if (dob > today)
        {
            Add(errors, field, $"'{field}' cannot be in the future.");
            return null;
        }

        var age = AgeOn(dob, today);
        if (age < MinimumAgeInYears)
        {
            Add(errors, field, $"A new starter must be at least {MinimumAgeInYears} years old.");
            return null;
        }

        if (age > MaximumAgeInYears)
        {
            Add(errors, field, $"'{field}' must be within the last {MaximumAgeInYears} years.");
            return null;
        }

        return dob;
    }

    private static EmailAddress? ParseEmail(string? value, Dictionary<string, List<string>> errors)
    {
        const string field = "email";
        if (string.IsNullOrWhiteSpace(value))
        {
            Add(errors, field, $"'{field}' is required.");
            return null;
        }

        if (!EmailAddress.TryCreate(value, out var email))
        {
            Add(errors, field, $"'{value.Trim()}' is not a valid email address.");
            return null;
        }

        return email;
    }

    private static Address? ParseAddress(RegisterNewStarterAddress? value, Dictionary<string, List<string>> errors)
    {
        if (value is null)
        {
            Add(errors, "address", "'address' is required.");
            return null;
        }

        var before = errors.Count;
        var line1 = RequiredText(value.Line1, "address.line1", MaxAddressLineLength, errors);
        var line2 = OptionalText(value.Line2, "address.line2", MaxAddressLineLength, errors);
        var town = RequiredText(value.Town, "address.town", MaxAddressLineLength, errors);
        var county = OptionalText(value.County, "address.county", MaxAddressLineLength, errors);
        var postcode = ParsePostcode(value.Postcode, errors);

        return errors.Count == before ? new Address(line1!, line2, town!, county, postcode!) : null;
    }

    private static string? ParsePostcode(string? value, Dictionary<string, List<string>> errors)
    {
        const string field = "address.postcode";
        if (string.IsNullOrWhiteSpace(value))
        {
            Add(errors, field, $"'{field}' is required.");
            return null;
        }

        var compact = WhitespacePattern().Replace(value, string.Empty).ToUpperInvariant();
        if (!UkPostcodePattern().IsMatch(compact))
        {
            Add(errors, field, $"'{value.Trim()}' is not a valid UK postcode.");
            return null;
        }

        // Canonical form: outward code, single space, 3-character inward code (e.g. "SW1A 1AA").
        return $"{compact[..^3]} {compact[^3..]}";
    }

    private static int AgeOn(DateOnly dateOfBirth, DateOnly today)
    {
        var age = today.Year - dateOfBirth.Year;
        return dateOfBirth > today.AddYears(-age) ? age - 1 : age;
    }

    private static void Add(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.TryGetValue(field, out var messages))
        {
            errors[field] = messages = [];
        }

        messages.Add(message);
    }

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespacePattern();

    [GeneratedRegex(@"^[A-Z]{1,2}[0-9][A-Z0-9]?[0-9][A-Z]{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex UkPostcodePattern();
}
