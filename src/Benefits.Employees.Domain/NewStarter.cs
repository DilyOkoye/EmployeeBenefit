namespace Benefits.Employees.Domain;

public sealed record NewStarter(
    Title Title,
    string FirstName,
    string Surname,
    DateOnly DateOfBirth,
    Gender Gender,
    EmailAddress Email,
    Address Address);
