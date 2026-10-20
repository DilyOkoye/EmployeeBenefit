using Benefits.Employees.Application.Abstractions;
using Benefits.Employees.Application.NewStarters;
using Benefits.Employees.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Benefits.Employees.Api.Employees;

public static class EmployeeEndpoints
{
    private const string GetEmployeeRouteName = "GetEmployee";

    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        var employees = app.MapGroup("/api/v1/employees").WithTags("Employees");

        employees.MapPost("/", CreateAsync)
            .WithName("CreateEmployee")
            .WithSummary("Add a new starter")
            .WithDescription("Registers a new starter and allocates a unique employee number. Email addresses must be unique (case-insensitive).");

        employees.MapGet("/{employeeNo}", GetAsync)
            .WithName(GetEmployeeRouteName)
            .WithSummary("Get an employee by employee number");

        return app;
    }

    private static async Task<Results<CreatedAtRoute<EmployeeResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        CreateEmployeeRequest request,
        RegisterNewStarterHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);

        return result switch
        {
            RegisterNewStarterResult.Registered registered => TypedResults.CreatedAtRoute(
                EmployeeResponse.From(registered.Employee),
                GetEmployeeRouteName,
                new { employeeNo = registered.Employee.EmployeeNo.Value }),

            RegisterNewStarterResult.Invalid invalid => TypedResults.ValidationProblem(
                invalid.Errors.ToDictionary(),
                title: "The new starter's details are not valid.",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.1"),

            RegisterNewStarterResult.EmailAlreadyInUse duplicate => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Email address already in use.",
                detail: $"An employee with the email address '{duplicate.Email}' already exists.",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                extensions: new Dictionary<string, object?>
                {
                    // Same shape as validation errors so the frontend can highlight the field with one code path.
                    ["errors"] = new Dictionary<string, string[]>
                    {
                        ["email"] = ["This email address is already registered to another employee."],
                    },
                }),

            _ => throw new InvalidOperationException($"Unhandled result: {result.GetType().Name}"),
        };
    }

    private static async Task<Results<Ok<EmployeeResponse>, ProblemHttpResult>> GetAsync(
        string employeeNo,
        IEmployeeRepository repository,
        CancellationToken cancellationToken)
    {
        var employee = EmployeeNumber.TryParse(employeeNo, out var number)
            ? await repository.FindAsync(number, cancellationToken)
            : null;

        return employee is null
            ? TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Employee not found.",
                detail: $"No employee exists with employee number '{employeeNo}'.",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.5")
            : TypedResults.Ok(EmployeeResponse.From(employee));
    }
}
