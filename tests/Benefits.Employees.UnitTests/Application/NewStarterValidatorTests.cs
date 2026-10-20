using Benefits.Employees.Application.NewStarters;
using Benefits.Employees.Domain;
using Xunit;

namespace Benefits.Employees.UnitTests.Application;

public class NewStarterValidatorTests
{
    private static ValidationResult<NewStarter> Validate(RegisterNewStarter command) =>
        NewStarterValidator.Validate(command, TestData.Today);

    private static RegisterNewStarter WithPostcode(string postcode) =>
        TestData.ValidCommand() with { Address = TestData.ValidCommand().Address! with { Postcode = postcode } };

    [Fact]
    public void Valid_input_is_parsed_into_a_new_starter()
    {
        var result = Validate(TestData.ValidCommand());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        var starter = result.Value!;
        Assert.Equal(Title.Mr, starter.Title);
        Assert.Equal("Dili", starter.FirstName);
        Assert.Equal("Okoye", starter.Surname);
        Assert.Equal(new DateOnly(1990, 5, 17), starter.DateOfBirth);
        Assert.Equal(Gender.Male, starter.Gender);
        Assert.Equal("dili.okoye@gmail.com", starter.Email.Value);
        Assert.Equal(new Address("1 High Street", null, "Derby", "Derbyshire", "DE1 1AA"), starter.Address);
    }

    [Fact]
    public void Input_is_normalised()
    {
        var command = TestData.ValidCommand() with
        {
            Title = "  mx ",
            FirstName = "  dilián ",
            Gender = "nonbinary",
            Email = " dili.okoye@gmail.com ",
            Address = new RegisterNewStarterAddress(" 1 High Street ", "  ", "Derby", "", "sw1a1aa"),
        };

        var starter = Validate(command).Value!;

        Assert.Equal(Title.Mx, starter.Title);
        Assert.Equal("dilián", starter.FirstName);
        Assert.Equal(Gender.NonBinary, starter.Gender);
        Assert.Equal("dili.okoye@gmail.com", starter.Email.Value);
        Assert.Equal(new Address("1 High Street", null, "Derby", null, "SW1A 1AA"), starter.Address);
    }

    [Fact]
    public void Every_missing_field_is_reported_at_once()
    {
        var result = Validate(new RegisterNewStarter(null, null, null, null, null, null, null));

        Assert.False(result.IsValid);
        Assert.Null(result.Value);
        string[] expected = ["address", "dateOfBirth", "email", "firstName", "gender", "surname", "title"];
        Assert.Equal(expected, result.Errors.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Missing_address_fields_are_reported_with_their_json_path()
    {
        var command = TestData.ValidCommand() with { Address = new RegisterNewStarterAddress(null, null, null, null, null) };

        var result = Validate(command);

        string[] expected = ["address.line1", "address.postcode", "address.town"];
        Assert.Equal(expected, result.Errors.Keys.Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("King")]
    [InlineData("1")]
    [InlineData("Mr,Mrs")]
    public void Unknown_titles_are_rejected(string title)
    {
        var result = Validate(TestData.ValidCommand() with { Title = title });
        Assert.Contains("title", result.Errors.Keys);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("Unknown")]
    public void Unknown_genders_are_rejected(string gender)
    {
        var result = Validate(TestData.ValidCommand() with { Gender = gender });
        Assert.Contains("gender", result.Errors.Keys);
    }

    [Theory]
    [InlineData("17/05/1990")]
    [InlineData("1990-13-01")]
    [InlineData("1990-05-17T00:00:00")]
    [InlineData("2026-10-19")] // tomorrow
    [InlineData("2010-10-19")] // turns 16 tomorrow: one day too young
    [InlineData("1925-10-17")] // over 100
    public void Invalid_dates_of_birth_are_rejected(string dateOfBirth)
    {
        var result = Validate(TestData.ValidCommand() with { DateOfBirth = dateOfBirth });
        Assert.Contains("dateOfBirth", result.Errors.Keys);
    }

    [Fact]
    public void A_starter_who_turns_sixteen_today_is_accepted()
    {
        var result = Validate(TestData.ValidCommand() with { DateOfBirth = "2010-10-18" });
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("M1 1AE", "M1 1AE")]
    [InlineData("b338th", "B33 8TH")]
    [InlineData("CR2 6XH", "CR2 6XH")]
    [InlineData("dn55  1pt", "DN55 1PT")]
    [InlineData("W1A 0AX", "W1A 0AX")]
    [InlineData("EC1A1BB", "EC1A 1BB")]
    public void Uk_postcodes_are_accepted_and_canonicalised(string input, string expected)
    {
        var result = Validate(WithPostcode(input));
        Assert.Equal(expected, result.Value!.Address.Postcode);
    }

    [Theory]
    [InlineData("XYZ")]
    [InlineData("12345")]
    [InlineData("SW1A 1A")]
    public void Invalid_postcodes_are_rejected(string postcode)
    {
        var result = Validate(WithPostcode(postcode));
        Assert.Contains("address.postcode", result.Errors.Keys);
    }

    [Fact]
    public void Overlong_names_are_rejected()
    {
        var result = Validate(TestData.ValidCommand() with { FirstName = new string('a', NewStarterValidator.MaxNameLength + 1) });
        Assert.Contains("firstName", result.Errors.Keys);
    }
}
