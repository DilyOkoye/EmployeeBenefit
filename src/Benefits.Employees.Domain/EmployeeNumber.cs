using System.Globalization;
using System.Text.RegularExpressions;

namespace Benefits.Employees.Domain;

public readonly partial record struct EmployeeNumber
{
    private const string Prefix = "EMP";
    private const string Format = "D6";

    public string Value { get; }

    private EmployeeNumber(string value) => Value = value;

    public static EmployeeNumber FromSequence(long sequence)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequence);
        return new EmployeeNumber(Prefix + sequence.ToString(Format, CultureInfo.InvariantCulture));
    }

    public static bool TryParse(string? input, out EmployeeNumber employeeNumber)
    {
        employeeNumber = default;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var candidate = input.Trim().ToUpperInvariant();
        if (!Pattern().IsMatch(candidate))
        {
            return false;
        }

        employeeNumber = new EmployeeNumber(candidate);
        return true;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^EMP\d{6,18}$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
