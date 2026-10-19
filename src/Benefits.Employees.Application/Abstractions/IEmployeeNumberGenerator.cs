using Benefits.Employees.Domain;

namespace Benefits.Employees.Application.Abstractions;

public interface IEmployeeNumberGenerator
{
    Task<EmployeeNumber> NextAsync(CancellationToken cancellationToken);
}
