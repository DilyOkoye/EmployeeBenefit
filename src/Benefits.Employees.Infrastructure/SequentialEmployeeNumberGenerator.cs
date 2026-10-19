using Benefits.Employees.Application.Abstractions;
using Benefits.Employees.Domain;

namespace Benefits.Employees.Infrastructure;

/// <summary>
/// Allocates EMP000001, EMP000002, ... Lock-free and safe under concurrency.
/// In production this would be a database sequence so numbers survive restarts and scale-out.
/// </summary>
public sealed class SequentialEmployeeNumberGenerator : IEmployeeNumberGenerator
{
    private long _last;

    public SequentialEmployeeNumberGenerator(long startAfter = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startAfter);
        _last = startAfter;
    }

    public Task<EmployeeNumber> NextAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(EmployeeNumber.FromSequence(Interlocked.Increment(ref _last)));
    }
}
