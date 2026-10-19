using System.Text.Json.Serialization;
using Benefits.Employees.Api;
using Benefits.Employees.Api.Employees;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    // A bare 400 (no validation errors attached) means the framework could not read the body at all.
    if (context.ProblemDetails is { Status: StatusCodes.Status400BadRequest, Detail: null }
        && context.ProblemDetails is not HttpValidationProblemDetails)
    {
        context.ProblemDetails.Title = "The request body could not be read.";
        context.ProblemDetails.Detail = "Send a JSON object matching the documented contract with Content-Type: application/json.";
    }
});
builder.Services.AddExceptionHandler<BadRequestExceptionHandler>();

// Development defaults to throwing on unreadable request bodies, which logs a client mistake as a server error.
// Return a 400 instead in every environment; UseStatusCodePages turns it into problem+json.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new() { Title = "Benefits Employees API", Version = "v1" }));
builder.Services.AddEmployees();

var app = builder.Build();


app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");
app.MapEmployeeEndpoints();

app.Run();


public partial class Program
{
}
