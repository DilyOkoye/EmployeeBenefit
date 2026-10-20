using Benefits.Employees.Domain;

namespace Benefits.Employees.Application.Abstractions;

public interface IEmployeeRepository
{
    /// <summary>
    /// Atomically stores the employee if neither its employee number nor its email address is already in use.
    /// Implementations must enforce both uniqueness rules themselves (e.g. via unique indexes) so that
    /// concurrent requests cannot create duplicates.
    /// </summary>
    Task<AddEmployeeOutcome> AddAsync(Employee employee, CancellationToken cancellationToken);

    Task<Employee?> FindAsync(EmployeeNumber employeeNo, CancellationToken cancellationToken);
}

public enum AddEmployeeOutcome
{
    Added,
    DuplicateEmail,
    DuplicateEmployeeNo,
}
