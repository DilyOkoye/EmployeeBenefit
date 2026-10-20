using Benefits.Employees.Domain;
using Xunit;

namespace Benefits.Employees.UnitTests.Domain;

public class EmailAddressTests
{
    [Theory]
    [InlineData("dili.okoye@gmail.com", "dili.okoye@gmail.com")]
    [InlineData("  Dili.Okoye@Gmail.COM ", "dili.okoye@gmail.com")]
    [InlineData("dili+benefits@mail.gmail.co.uk", "dili+benefits@mail.gmail.co.uk")]
    public void TryCreate_trims_and_lower_cases_valid_addresses(string input, string expected)
    {
        Assert.True(EmailAddress.TryCreate(input, out var email));
        Assert.Equal(expected, email!.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("dili@localhost")]
    [InlineData("dili@@okoye.com")]
    [InlineData("Dili Okoye <dili@gmail.com>")]
    [InlineData("dili@gmail.com.")]
    public void TryCreate_rejects_invalid_addresses(string? input)
    {
        Assert.False(EmailAddress.TryCreate(input, out var email));
        Assert.Null(email);
    }

    [Fact]
    public void TryCreate_rejects_addresses_longer_than_254_characters()
    {
        var input = new string('a', 64) + "@" + new string('b', 186) + ".com"; // 255 chars
        Assert.False(EmailAddress.TryCreate(input, out _));
    }

    [Fact]
    public void Addresses_differing_only_by_case_are_equal()
    {
        EmailAddress.TryCreate("DILI@GMAIL.COM", out var upper);
        EmailAddress.TryCreate("dili@gmail.com", out var lower);
        Assert.Equal(lower, upper);
    }
}
