# Stargate API: Test Suite

84 tests, written with xUnit. **Line coverage is 100% and branch coverage is 95%.** The README asked for more than 50%. EF Core migrations are excluded from the measurement because they are generated code.

## Running the tests

From `exercise1/`:

```bash
dotnet test StargateAPI.sln
```

To produce a coverage report:

```bash
dotnet tool restore
dotnet test StargateAPI.sln --settings coverage.runsettings --results-directory TestResults
dotnet reportgenerator -reports:"TestResults/*/coverage.cobertura.xml" -targetdir:coverage -reporttypes:"Html;TextSummary"
```

Then open `coverage/index.html`.

## Which methods matter most, and why

These are ranked by how much damage a defect in each method would do. Test effort followed the same order.

| Rank | Method | Why it matters | Tests |
|---|---|---|---|
| 1 | `CreateAstronautDutyHandler.Handle` | <ul><li>Implements rules 3–7.</li><li>It is the only code that changes the duty history.</li><li>A defect here silently corrupts career records.</li></ul> | [CreateAstronautDutyTests](StargateAPI.Tests/Commands/CreateAstronautDutyTests.cs) |
| 2 | `CreateAstronautDutyPreProcessor.Process` | <ul><li>Rejects duty requests for unknown people.</li><li>Rejects duplicate duties.</li><li>Rejects duties that start before the current one.</li></ul> | CreateAstronautDutyTests |
| 3 | `CreatePerson` / `UpdatePerson` pre-processors and handlers | <ul><li>Enforce rule 1: a Name is unique.</li><li>Originally the create pre-processor was never registered, so it never ran.</li></ul> | [PersonCommandTests](StargateAPI.Tests/Commands/PersonCommandTests.cs) |
| 4 | Query handlers | <ul><li>Every read goes through them.</li><li>They originally contained SQL injection.</li><li>They crashed on unknown names.</li></ul> | [QueryTests](StargateAPI.Tests/Queries/QueryTests.cs) |
| 5 | `ValidationBehavior` and the validators | Stop malformed input from reaching the database. | [RequestValidationTests](StargateAPI.Tests/Validation/RequestValidationTests.cs) |
| 6 | `StargateExceptionHandler.ToResponse` | Decides the status code for every failure, and whether internal details could leak to clients. | [StargateExceptionHandlerTests](StargateAPI.Tests/Api/StargateExceptionHandlerTests.cs) |
| 7 | `ProcessLoggingBehavior` / `ProcessLogWriter` | <ul><li>Required by the README.</li><li>A failure here must never change a request's outcome.</li><li>Writing a log entry must never persist a failed request's changes.</li></ul> | [ProcessLoggingTests](StargateAPI.Tests/Logging/ProcessLoggingTests.cs) |

## How the tests are organised

| Level | What it exercises | Examples |
|---|---|---|
| **Pipeline** (most tests) | <ul><li>The real MediatR pipeline: logging → validation → pre-processors → handler.</li><li>Runs against an in-memory SQLite database built from the real migrations.</li></ul> | Commands, Queries, Validation, Logging |
| **HTTP** | <ul><li>The whole application hosted with `WebApplicationFactory`.</li><li>Covers routing, model binding, JSON and the error envelope.</li></ul> | [ApiEndpointTests](StargateAPI.Tests/Api/ApiEndpointTests.cs), [SwaggerTests](StargateAPI.Tests/Api/SwaggerTests.cs) |
| **Concurrency** | <ul><li>Parallel HTTP requests against a SQLite database file.</li><li>Shows that the rules still hold when requests race.</li></ul> | [ConcurrencyTests](StargateAPI.Tests/Api/ConcurrencyTests.cs) |
| **Unit** | Pure logic, and the behaviours that are hard to reach through the pipeline. | [DutyTitlesTests](StargateAPI.Tests/Rules/DutyTitlesTests.cs), exception mapping |
| **Schema** | Database constraints, as a final guard behind the application checks. | [SchemaTests](StargateAPI.Tests/Data/SchemaTests.cs) |

**SQLite rather than EF Core's in-memory provider.** The in-memory provider does not enforce unique indexes, filtered indexes, collations or transactions. Several rules depend on exactly those features, so tests run against it could pass while the real database fails.

**Each fixed defect has a test that would have caught it.** Those tests carry a comment beginning "Previously…".
