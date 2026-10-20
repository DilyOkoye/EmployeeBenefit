using Benefits.Employees.Application.Abstractions;
using Benefits.Employees.Application.NewStarters;
using Benefits.Employees.Domain;
using Benefits.Employees.Infrastructure;
using Xunit;

namespace Benefits.Employees.UnitTests.Infrastructure;

public class ConcurrencyTests
{
    [Fact]
    public async Task Generator_never_hands_out_the_same_number_twice_under_contention()
    {
        var generator = new SequentialEmployeeNumberGenerator();

        var numbers = await Task.WhenAll(Enumerable.Range(0, 1_000)
            .Select(_ => Task.Run(() => generator.NextAsync(CancellationToken.None))));

        Assert.Equal(numbers.Length, numbers.Distinct().Count());
    }

    [Fact]
    public async Task Only_one_of_many_simultaneous_registrations_with_the_same_email_succeeds()
    {
        var handler = new RegisterNewStarterHandler(
            new InMemoryEmployeeRepository(), new SequentialEmployeeNumberGenerator(), TimeProvider.System);

        var results = await Task.WhenAll(Enumerable.Range(0, 100)
            .Select(i => Task.Run(() => handler.HandleAsync(
                TestData.ValidCommand(i % 2 == 0 ? "dili@gmail.com" : "DILI@gmail.com"), CancellationToken.None))));

        Assert.Single(results.OfType<RegisterNewStarterResult.Registered>());
        Assert.Equal(99, results.OfType<RegisterNewStarterResult.EmailAlreadyInUse>().Count());
    }

    [Fact]
    public async Task Repository_reports_duplicate_employee_numbers()
    {
        var repository = new InMemoryEmployeeRepository();
        var number = EmployeeNumber.FromSequence(1);

        var added = await repository.AddAsync(NewEmployee(number, "a@gmail.com"), CancellationToken.None);
        var clash = await repository.AddAsync(NewEmployee(number, "b@gmail.com"), CancellationToken.None);

        Assert.Equal(AddEmployeeOutcome.Added, added);
        Assert.Equal(AddEmployeeOutcome.DuplicateEmployeeNo, clash);
    }

    private static Employee NewEmployee(EmployeeNumber number, string email)
    {
        EmailAddress.TryCreate(email, out var address);
        var details = new NewStarter(
            Title.Mr, "Alex", "Smith", new DateOnly(1990, 1, 1), Gender.Male, address!,
            new Address("1 High Street", null, "Derby", null, "DE1 1AA"));
        return Employee.Register(number, details, DateTimeOffset.UtcNow);
    }
}
