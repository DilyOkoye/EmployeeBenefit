namespace Benefits.Employees.Domain;

public sealed class Employee
{
    private Employee(EmployeeNumber employeeNo, NewStarter details, DateTimeOffset createdAt)
    {
        EmployeeNo = employeeNo;
        Title = details.Title;
        FirstName = details.FirstName;
        Surname = details.Surname;
        DateOfBirth = details.DateOfBirth;
        Gender = details.Gender;
        Email = details.Email;
        Address = details.Address;
        CreatedAt = createdAt;
    }

    public EmployeeNumber EmployeeNo { get; }
    public Title Title { get; }
    public string FirstName { get; }
    public string Surname { get; }
    public DateOnly DateOfBirth { get; }
    public Gender Gender { get; }
    public EmailAddress Email { get; }
    public Address Address { get; }
    public DateTimeOffset CreatedAt { get; }

    public static Employee Register(EmployeeNumber employeeNo, NewStarter details, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(details);
        return employeeNo.Value is null ? throw new ArgumentException("An employee number must be allocated.", nameof(employeeNo)) : new Employee(employeeNo, details, createdAt);
    }
}
