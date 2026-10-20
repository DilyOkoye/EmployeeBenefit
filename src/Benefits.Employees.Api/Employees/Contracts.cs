using Benefits.Employees.Application.NewStarters;
using Benefits.Employees.Domain;

namespace Benefits.Employees.Api.Employees;

/// <summary>POST /api/v1/employees request body.</summary>
public sealed record CreateEmployeeRequest(
    string? Title,
    string? FirstName,
    string? Surname,
    string? DateOfBirth,
    string? Gender,
    string? Email,
    AddressDto? Address)
{
    public RegisterNewStarter ToCommand() => new(
        Title,
        FirstName,
        Surname,
        DateOfBirth,
        Gender,
        Email,
        Address is null
            ? null
            : new RegisterNewStarterAddress(Address.Line1, Address.Line2, Address.Town, Address.County, Address.Postcode));
}

public sealed record AddressDto(string? Line1, string? Line2, string? Town, string? County, string? Postcode);

public sealed record EmployeeResponse(
    string EmployeeNo,
    string Title,
    string FirstName,
    string Surname,
    DateOnly DateOfBirth,
    string Gender,
    string Email,
    AddressDto Address,
    DateTimeOffset CreatedAt)
{
    public static EmployeeResponse From(Employee employee) => new(
        employee.EmployeeNo.Value,
        employee.Title.ToString(),
        employee.FirstName,
        employee.Surname,
        employee.DateOfBirth,
        employee.Gender.ToString(),
        employee.Email.Value,
        new AddressDto(
            employee.Address.Line1,
            employee.Address.Line2,
            employee.Address.Town,
            employee.Address.County,
            employee.Address.Postcode),
        employee.CreatedAt);
}
