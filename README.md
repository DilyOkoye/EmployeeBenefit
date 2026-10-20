# Benefits – New Starter API

> *As an HR Administrator I would like to be able to add a new starter so that they can gain access to OneHub and take full advantage of their employment package.*

A small, production-shaped RESTful API in **.NET 10 / ASP.NET Core minimal APIs** that lets an HR Administrator add a new starter. It is designed for the Frontend Engineers who will build on it. The contract is predictable, every error says which field is wrong, and the API never answers a client mistake with a `500`.

## Quick start

**Prerequisite:** the [.NET 10 SDK]

```bash
# run the API on http://localhost:5080 (Swagger UI at http://localhost:5080/swagger)
dotnet run --project src/Benefits.Employees.Api

# run all unit + integration tests
dotnet test
```

The easiest way to explore it is **Swagger UI** at <http://localhost:5080/swagger>, where you can see every endpoint, its request and response shapes, and send requests. The raw OpenAPI document is at `/swagger/v1/swagger.json`, which a frontend can use to generate a typed client. Both are enabled in Development only.

Alternatively, open `src/Benefits.Employees.Api/Benefits.Employees.Api.http` in Visual Studio, Rider or VS Code (REST Client) and send the requests. You can also use curl:

```bash
curl -i -X POST http://localhost:5080/api/v1/employees \
  -H "Content-Type: application/json" \
  -d '{
        "title": "Ms",
        "firstName": "Dili",
        "surname": "Okoye",
        "dateOfBirth": "1990-05-17",
        "gender": "Male",
        "email": "dili.okoye@gmail.com",
        "address": { "line1": "1 High Street", "town": "Derby", "county": "Derbyshire", "postcode": "ng24 1aa" }
      }'
```

## API

| Method | Route | Success | Errors |
|---|---|---|---|
| `POST` | `/api/v1/employees` | `201 Created` + `Location` header + employee body | `400` invalid/unreadable body, `409` email already in use |
| `GET` | `/api/v1/employees/{employeeNo}` | `200 OK` + employee body | `404` not found |
| `GET` | `/health` | `200 Healthy` | |

**Successful response (`201`)**

```json
{
  "employeeNo": "EMP000001",
  "title": "Ms",
  "firstName": "Dili",
  "surname": "Okoye",
  "dateOfBirth": "1990-05-17",
  "gender": "Male",
  "email": "dili.okoye@gmail.com",
  "address": { "line1": "1 High Street", "town": "Derby", "county": "Derbyshire", "postcode": "NG24 1AA" },
  "createdAt": "2026-10-18T09:30:00+00:00"
}
```

**Errors** are always [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457) `application/problem+json`. Validation errors are keyed by the JSON path of the field and returned all at once, so a form can show every message next to its input in one round-trip:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "The new starter's details are not valid.",
  "status": 400,
  "errors": {
    "title": ["'King' is not a valid title. Allowed values: Mr, Mrs, Miss, Ms, Mx, Dr."],
    "email": ["'nope' is not a valid email address."],
    "address.postcode": ["'XYZ' is not a valid UK postcode."]
  }
}
```

A duplicate email returns `409 Conflict` in the same shape, with an `errors.email` entry. The frontend can therefore handle both cases with a single code path.

### Acceptance criteria → implementation

| Criterion | How it is met |
|---|---|
| Store EmployeeNo, Title, Firstname, Surname, DOB, Gender, Email, Address | `Employee` aggregate (Domain), persisted via `IEmployeeRepository` |
| EmployeeNo is generated and unique | The server always allocates it (clients cannot supply one). `SequentialEmployeeNumberGenerator` uses `Interlocked` so concurrent requests never share a number. The repository also rejects a duplicate number, and the use case retries with a fresh one. |
| Email is unique | Normalised (trimmed, lower-cased) so `Dili@Gmail.com` and `dili@gmail.com` clash. The uniqueness check and the insert are one atomic operation, so two simultaneous requests with the same email cannot both succeed. A test fires 100 concurrently and exactly one wins. |

## Design

```
src/
  Benefits.Employees.Domain          Employee aggregate, value objects (EmployeeNumber, EmailAddress, Address), enums
  Benefits.Employees.Application     RegisterNewStarter use case, validation, ports (IEmployeeRepository, IEmployeeNumberGenerator)
  Benefits.Employees.Infrastructure  In-memory adapters for the ports
  Benefits.Employees.Api             HTTP contracts, endpoints, problem details, composition root
tests/
  Benefits.Employees.UnitTests         domain, validation, use case, concurrency
  Benefits.Employees.IntegrationTests  the HTTP contract end-to-end via WebApplicationFactory
```

Dependencies point inwards (Api → Application → Domain; Infrastructure → Application). Domain and Application have no framework or package dependencies.

Key decisions:

- **Parse, don't validate.** The request is all nullable strings. `NewStarterValidator` turns it into a `NewStarter` built from strongly typed values, or into a dictionary of every error. Invalid data can never reach the domain, and the client gets field-level messages instead of a generic JSON deserialisation error.
- **Uniqueness lives in the store, not in a pre-check.** A "check then insert" has a race condition. The repository contract requires an atomic add that reports `DuplicateEmail` or `DuplicateEmployeeNo`, which maps directly onto unique indexes in a real database.
- **Ports for storage and numbering.** Storage was out of scope, so the in-memory adapters sit behind interfaces. Moving to SQL Server/EF Core with a database `SEQUENCE` touches only Infrastructure and one line of composition.
- **Human-friendly employee numbers** (`EMP000001`) rather than GUIDs. People read them out on the phone and they appear on payslips. They are also the resource identifier in the URL.
- **Typed results** (`Results<CreatedAtRoute<…>, ValidationProblem, ProblemHttpResult>`) make every possible response visible in the endpoint signature, and Swagger reads them, so the generated OpenAPI document lists the `201`, `400`, `409` and `404` responses straight from the code and cannot drift out of date.
- **`TimeProvider`** is injected so date-of-birth and age rules are deterministic in tests.
- **No 500s for client mistakes.** Malformed JSON, an empty body, or a JSON array all return a `400` problem response. They are not logged as server errors.

## Assumptions

- **Authentication/authorisation** are out of scope, as the brief states. In production this endpoint would require an HR Administrator role or policy.
- **Storage** is in-memory, as the brief allows. Data resets when the app restarts.
- **Employees are UK based.** Addresses use UK fields and postcodes are validated and canonicalised (`sw1a1aa` → `SW1A 1AA`).
- **Age:** a new starter must be at least 16 (the UK school-leaving age) on the day they are added. Dates of birth more than 100 years ago are rejected as probable typos.
- **Names** are only trimmed and length-checked (100 characters).
- **Title and Gender** are fixed lists, matched case-insensitively. Gender includes `NonBinary`, `Other` and `PreferNotToSay`.
- **Email** is stored lower-cased and must be unique case-insensitively, which is how real mail systems behave in practice.

## Real World Implementations/ If I had more time

- Persist to a real database with unique indexes on `EmployeeNo` and `Email`, and allocate numbers from a database sequence so they survive restarts and multiple instances.
- Add authentication with an `HrAdministrator` policy, plus an audit trail of who added whom.
- Support an `Idempotency-Key` header on `POST`, so a double-clicked "Save" or a retried request returns the original `201` instead of a `409`.
- Publish an `EmployeeRegistered` event (via an outbox) so OneHub account provisioning and the welcome email happen asynchronously.
- Add list/search, update and leaver endpoints as further stories require them.
- Treat DOB, gender and address as personal data under UK GDPR: keep them out of logs, encrypt them at rest, and agree retention rules.
