using Benefits.Employees.Application.NewStarters;

namespace Benefits.Employees.UnitTests;

internal static class TestData
{
    public static readonly DateOnly Today = new(2026, 10, 18);

    public static RegisterNewStarter ValidCommand(string email = "dili.okoye@gmail.com") => new(
        Title: "Mr",
        FirstName: "Dili",
        Surname: "Okoye",
        DateOfBirth: "1990-05-17",
        Gender: "Male",
        Email: email,
        Address: new RegisterNewStarterAddress(
            Line1: "1 High Street",
            Line2: null,
            Town: "Derby",
            County: "Derbyshire",
            Postcode: "DE1 1AA"));
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
