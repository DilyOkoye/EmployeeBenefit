using Benefits.Employees.Domain;
using Xunit;

namespace Benefits.Employees.UnitTests.Domain;

public class EmployeeNumberTests
{
    [Theory]
    [InlineData(1, "EMP000001")]
    [InlineData(42, "EMP000042")]
    [InlineData(999999, "EMP999999")]
    [InlineData(1000000, "EMP1000000")]
    public void FromSequence_formats_with_prefix_and_zero_padding(long sequence, string expected)
    {
        Assert.Equal(expected, EmployeeNumber.FromSequence(sequence).Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void FromSequence_rejects_non_positive_numbers(long sequence)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EmployeeNumber.FromSequence(sequence));
    }

    [Theory]
    [InlineData("EMP000042", "EMP000042")]
    [InlineData(" emp000042 ", "EMP000042")]
    public void TryParse_accepts_valid_numbers_case_insensitively(string input, string expected)
    {
        Assert.True(EmployeeNumber.TryParse(input, out var number));
        Assert.Equal(expected, number.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("EMP")]
    [InlineData("EMP12")]
    [InlineData("ABC000001")]
    [InlineData("EMP00000X")]
    public void TryParse_rejects_invalid_numbers(string? input)
    {
        Assert.False(EmployeeNumber.TryParse(input, out _));
    }

    [Fact]
    public void Numbers_with_the_same_value_are_equal()
    {
        EmployeeNumber.TryParse("emp000007", out var parsed);
        Assert.Equal(EmployeeNumber.FromSequence(7), parsed);
    }
}
