using Benefits.Employees.Application.Abstractions;
using Benefits.Employees.Application.NewStarters;
using Benefits.Employees.Domain;
using Benefits.Employees.Infrastructure;
using Xunit;

namespace Benefits.Employees.UnitTests.Application;

public class RegisterNewStarterHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 9, 30, 0, TimeSpan.Zero);
    private readonly InMemoryEmployeeRepository _repository = new();

    private RegisterNewStarterHandler CreateHandler(IEmployeeNumberGenerator? generator = null) =>
        new(_repository, generator ?? new SequentialEmployeeNumberGenerator(), new FixedTimeProvider(Now));

    [Fact]
    public async Task Registers_a_valid_new_starter_with_a_generated_employee_number()
    {
        var result = await CreateHandler().HandleAsync(TestData.ValidCommand(), CancellationToken.None);

        var registered = Assert.IsType<RegisterNewStarterResult.Registered>(result);
        Assert.Equal("EMP000001", registered.Employee.EmployeeNo.Value);
        Assert.Equal(Now, registered.Employee.CreatedAt);
        Assert.Same(registered.Employee, await _repository.FindAsync(registered.Employee.EmployeeNo, CancellationToken.None));
    }

    [Fact]
    public async Task Each_new_starter_gets_a_different_employee_number()
    {
        var handler = CreateHandler();

        var first = await handler.HandleAsync(TestData.ValidCommand("a@gmail.com"), CancellationToken.None);
        var second = await handler.HandleAsync(TestData.ValidCommand("b@gmail.com"), CancellationToken.None);

        Assert.NotEqual(
            Assert.IsType<RegisterNewStarterResult.Registered>(first).Employee.EmployeeNo,
            Assert.IsType<RegisterNewStarterResult.Registered>(second).Employee.EmployeeNo);
    }

    [Fact]
    public async Task Rejects_an_email_already_in_use_regardless_of_case()
    {
        var handler = CreateHandler();
        await handler.HandleAsync(TestData.ValidCommand("dili.okoye@gmail.com"), CancellationToken.None);

        var result = await handler.HandleAsync(TestData.ValidCommand("DILI.OKOYE@Gmail.com"), CancellationToken.None);

        var duplicate = Assert.IsType<RegisterNewStarterResult.EmailAlreadyInUse>(result);
        Assert.Equal("dili.okoye@gmail.com", duplicate.Email.Value);
    }

    [Fact]
    public async Task Returns_validation_errors_and_allocates_no_number_for_invalid_input()
    {
        var generator = new ScriptedGenerator(1);
        var result = await CreateHandler(generator).HandleAsync(
            TestData.ValidCommand() with { Email = "nope" }, CancellationToken.None);

        var invalid = Assert.IsType<RegisterNewStarterResult.Invalid>(result);
        Assert.Contains("email", invalid.Errors.Keys);
        Assert.Equal(0, generator.Calls);
    }

    [Fact]
    public async Task Retries_with_a_fresh_number_if_the_generated_one_is_already_taken()
    {
        await CreateHandler(new ScriptedGenerator(1)).HandleAsync(TestData.ValidCommand("first@gmail.com"), CancellationToken.None);
        var generator = new ScriptedGenerator(1, 2);

        var result = await CreateHandler(generator).HandleAsync(TestData.ValidCommand("second@gmail.com"), CancellationToken.None);

        var registered = Assert.IsType<RegisterNewStarterResult.Registered>(result);
        Assert.Equal("EMP000002", registered.Employee.EmployeeNo.Value);
        Assert.Equal(2, generator.Calls);
    }

    [Fact]
    public async Task Gives_up_after_repeated_number_collisions()
    {
        await CreateHandler(new ScriptedGenerator(1)).HandleAsync(TestData.ValidCommand("first@gmail.com"), CancellationToken.None);
        var alwaysOne = new ScriptedGenerator(Enumerable.Repeat(1L, RegisterNewStarterHandler.MaxEmployeeNumberAttempts).ToArray());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateHandler(alwaysOne).HandleAsync(TestData.ValidCommand("second@gmail.com"), CancellationToken.None));
    }

    private sealed class ScriptedGenerator(params long[] sequence) : IEmployeeNumberGenerator
    {
        public int Calls { get; private set; }

        public Task<EmployeeNumber> NextAsync(CancellationToken cancellationToken) =>
            Task.FromResult(EmployeeNumber.FromSequence(sequence[Calls++]));
    }
}
