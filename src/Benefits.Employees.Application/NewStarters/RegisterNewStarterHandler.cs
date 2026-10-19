using Benefits.Employees.Application.Abstractions;
using Benefits.Employees.Domain;

namespace Benefits.Employees.Application.NewStarters;

/// <summary>Use case: an HR Administrator adds a new starter so they can access OneHub.</summary>
public sealed class RegisterNewStarterHandler(
    IEmployeeRepository repository,
    IEmployeeNumberGenerator employeeNumberGenerator,
    TimeProvider timeProvider)
{
    internal const int MaxEmployeeNumberAttempts = 5;

    public async Task<RegisterNewStarterResult> HandleAsync(RegisterNewStarter command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var now = timeProvider.GetUtcNow();
        var validation = NewStarterValidator.Validate(command, DateOnly.FromDateTime(now.UtcDateTime));
        if (!validation.IsValid)
        {
            return new RegisterNewStarterResult.Invalid(validation.Errors);
        }

        var newStarter = validation.Value!;
        for (var attempt = 1; attempt <= MaxEmployeeNumberAttempts; attempt++)
        {
            var employeeNo = await employeeNumberGenerator.NextAsync(cancellationToken);
            var employee = Employee.Register(employeeNo, newStarter, now);

            switch (await repository.AddAsync(employee, cancellationToken))
            {
                case AddEmployeeOutcome.Added:
                    return new RegisterNewStarterResult.Registered(employee);
                case AddEmployeeOutcome.DuplicateEmail:
                    return new RegisterNewStarterResult.EmailAlreadyInUse(newStarter.Email);
                case AddEmployeeOutcome.DuplicateEmployeeNo:
                    continue;
                default:
                    throw new InvalidOperationException("Unexpected repository outcome.");
            }
        }

        throw new InvalidOperationException(
            $"Could not allocate a unique employee number after {MaxEmployeeNumberAttempts} attempts.");
    }
}
