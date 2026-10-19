namespace Benefits.Employees.Application.NewStarters;

public sealed record RegisterNewStarter(
    string? Title,
    string? FirstName,
    string? Surname,
    string? DateOfBirth,
    string? Gender,
    string? Email,
    RegisterNewStarterAddress? Address);

public sealed record RegisterNewStarterAddress(
    string? Line1,
    string? Line2,
    string? Town,
    string? County,
    string? Postcode);
