using Benefits.Employees.Domain;

namespace Benefits.Employees.Application.NewStarters;

public abstract record RegisterNewStarterResult
{
    private RegisterNewStarterResult()
    {
    }

    public sealed record Registered(Employee Employee) : RegisterNewStarterResult;

    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : RegisterNewStarterResult;

    public sealed record EmailAlreadyInUse(EmailAddress Email) : RegisterNewStarterResult;
}
