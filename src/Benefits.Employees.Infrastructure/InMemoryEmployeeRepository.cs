using Benefits.Employees.Application.Abstractions;
using Benefits.Employees.Domain;

namespace Benefits.Employees.Infrastructure;

/// <summary>
/// Thread-safe in-memory store. Both uniqueness checks and the insert happen under one lock, mirroring
/// what unique indexes on EmployeeNo and Email would give us in a real database.
/// </summary>
public sealed class InMemoryEmployeeRepository : IEmployeeRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<EmployeeNumber, Employee> _byEmployeeNo = [];
    private readonly HashSet<string> _emails = new(StringComparer.OrdinalIgnoreCase);

    public Task<AddEmployeeOutcome> AddAsync(Employee employee, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(employee);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_emails.Contains(employee.Email.Value))
            {
                return Task.FromResult(AddEmployeeOutcome.DuplicateEmail);
            }

            if (_byEmployeeNo.ContainsKey(employee.EmployeeNo))
            {
                return Task.FromResult(AddEmployeeOutcome.DuplicateEmployeeNo);
            }

            _byEmployeeNo.Add(employee.EmployeeNo, employee);
            _emails.Add(employee.Email.Value);
            return Task.FromResult(AddEmployeeOutcome.Added);
        }
    }

    public Task<Employee?> FindAsync(EmployeeNumber employeeNo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(_byEmployeeNo.GetValueOrDefault(employeeNo));
        }
    }
}
