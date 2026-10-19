using Benefits.Employees.Application.Abstractions;
using Benefits.Employees.Application.NewStarters;
using Benefits.Employees.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Benefits.Employees.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEmployees(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<RegisterNewStarterHandler>();

        // Storage is out of scope for my implementation; however for a production system, I will swap these for EF Core / a DB sequence without touching the use case.
        services.AddSingleton<IEmployeeRepository, InMemoryEmployeeRepository>();
        services.AddSingleton<IEmployeeNumberGenerator>(_ => new SequentialEmployeeNumberGenerator());

        return services;
    }
}
