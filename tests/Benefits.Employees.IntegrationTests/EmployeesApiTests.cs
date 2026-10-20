using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Benefits.Employees.Api.Employees;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Benefits.Employees.IntegrationTests;

public class EmployeesApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EmployeesUrl = "/api/v1/employees";
    private readonly HttpClient _client = factory.CreateClient();

    private static string UniqueEmail() => $"new.starter.{Guid.NewGuid():N}@example.com";

    private static Dictionary<string, object?> ValidRequest(string? email = null) => new()
    {
        ["title"] = "Ms",
        ["firstName"] = "Dili",
        ["surname"] = "Okoye",
        ["dateOfBirth"] = "1990-05-17",
        ["gender"] = "Male",
        ["email"] = email ?? UniqueEmail(),
        ["address"] = new Dictionary<string, object?>
        {
            ["line1"] = "1 High Street",
            ["line2"] = "Flat 2",
            ["town"] = "Derby",
            ["county"] = "Derbyshire",
            ["postcode"] = "de1 1aa",
        },
    };

    [Fact]
    public async Task Post_creates_the_employee_and_returns_201_with_a_location_header()
    {
        var email = UniqueEmail();

        var response = await _client.PostAsJsonAsync(EmployeesUrl, ValidRequest(email));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EmployeeResponse>();
        Assert.NotNull(created);
        Assert.Matches(@"^EMP\d{6}$", created.EmployeeNo);
        Assert.Equal("Ms", created.Title);
        Assert.Equal("Dili", created.FirstName);
        Assert.Equal("Okoye", created.Surname);
        Assert.Equal(new DateOnly(1990, 5, 17), created.DateOfBirth);
        Assert.Equal("Male", created.Gender);
        Assert.Equal(email, created.Email);
        Assert.Equal(new AddressDto("1 High Street", "Flat 2", "Derby", "Derbyshire", "DE1 1AA"), created.Address);
        Assert.Equal($"{EmployeesUrl}/{created.EmployeeNo}", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task The_created_employee_can_be_fetched_from_its_location()
    {
        var response = await _client.PostAsJsonAsync(EmployeesUrl, ValidRequest());
        var created = await response.Content.ReadFromJsonAsync<EmployeeResponse>();

        var fetched = await _client.GetFromJsonAsync<EmployeeResponse>(response.Headers.Location);

        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task Responses_use_camel_case_property_names()
    {
        var response = await _client.PostAsJsonAsync(EmployeesUrl, ValidRequest());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var names = json.RootElement.EnumerateObject().Select(p => p.Name).ToArray();

        string[] expected =
            ["employeeNo", "title", "firstName", "surname", "dateOfBirth", "gender", "email", "address", "createdAt"];
        Assert.Equal(expected, names);
    }

    [Fact]
    public async Task Employee_numbers_are_unique_across_concurrent_requests()
    {
        var responses = await Task.WhenAll(Enumerable.Range(0, 50)
            .Select(_ => _client.PostAsJsonAsync(EmployeesUrl, ValidRequest())));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        var numbers = await Task.WhenAll(responses.Select(async r =>
            (await r.Content.ReadFromJsonAsync<EmployeeResponse>())!.EmployeeNo));
        Assert.Equal(numbers.Length, numbers.Distinct().Count());
    }

    [Fact]
    public async Task Post_with_an_email_already_in_use_returns_409_regardless_of_case()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync(EmployeesUrl, ValidRequest(email));

        var response = await _client.PostAsJsonAsync(EmployeesUrl, ValidRequest(email.ToUpperInvariant()));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(409, problem.RootElement.GetProperty("status").GetInt32());
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("email", out _));
    }

    [Fact]
    public async Task Post_with_invalid_details_returns_400_with_errors_for_each_field()
    {
        var response = await _client.PostAsJsonAsync(EmployeesUrl, new Dictionary<string, object?>
        {
            ["title"] = "King",
            ["dateOfBirth"] = "not-a-date",
            ["email"] = "not-an-email",
            ["address"] = new Dictionary<string, object?> { ["postcode"] = "XYZ" },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var fields = problem.RootElement.GetProperty("errors").EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal);
        string[] expected =
        [
            "address.line1", "address.postcode", "address.town", "dateOfBirth", "email", "firstName", "gender", "surname", "title",
        ];
        Assert.Equal(expected, fields);
    }

    [Theory]
    [InlineData("{not json")]
    [InlineData("")]
    [InlineData("[]")]
    public async Task Post_with_an_unreadable_body_returns_400_problem_details(string body)
    {
        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(EmployeesUrl, content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("EMP999999")]
    [InlineData("not-an-employee-number")]
    public async Task Get_for_an_unknown_employee_returns_404_problem_details(string employeeNo)
    {
        var response = await _client.GetAsync($"{EmployeesUrl}/{employeeNo}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
