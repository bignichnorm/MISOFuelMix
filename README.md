# QueryMISO

QueryMISO is an ASP.NET Core service that imports fuel-mix snapshots from the [MISO real-time FuelMix API](https://public-api.misoenergy.org/api/FuelMix),
stores them in SQL Server, and exposes the stored data through a read-only HTTP API.

## Features

- Fetches the current MISO fuel-mix snapshot through a typed `HttpClient`.
- Runs ingestion in a hosted background service:
  - one ingestion attempt when the application starts;
  - one subsequent attempt every minute.
- Persists snapshots and their fuel-category readings with Entity Framework Core.
- Supports filtering read results by a date time interval and fuel category.
- Logs successful ingestion and client operations, and logs background ingestion failures without terminating the hosted service.
- Includes an EF Core migration and xUnit tests for the client, ingestion service, polling service, persistence behavior, and HTTP layer.

## Technology

- .NET 10
- ASP.NET Core
- Entity Framework Core 10
- SQL Server
- xUnit, Moq, FluentAssertions

## Project structure

```text
QueryMISO/
├── MISOConsoleApp/
│   ├── API/Controllers/       HTTP endpoints
│   ├── Client/                MISO API client and response DTOs
│   ├── Migrations/            EF Core database migrations
│   ├── Repository/            DbContext and persistence models
│   └── Services/              Ingestion and scheduled polling
├── MISOQueryingApp.Tests/     Unit and HTTP integration tests
└── QueryMISO.slnx
```

## Prerequisites

- .NET 10 SDK
- SQL Server 2019 or later, or an accessible SQL Server-compatible instance
- Network access to `https://public-api.misoenergy.org`

The application currently uses SQL Server in `Program.cs`. The test project uses SQLite in-memory databases for persistence tests.

## Configuration

Set the `ConnectionStrings:Database` value before starting the application. The value can be supplied 
through `MISOConsoleApp/appsettings.json`, user secrets, an environment variable, or another standard ASP.NET Core configuration provider.

Example connection string:

```json
{
  "ConnectionStrings": {
    "Database": "Server=localhost;Database=MisoFuelMix;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

For a SQL Server container or SQL login, use the corresponding SQL Server connection string, for example:

```text
Server=localhost,1433;Database=MisoFuelMix;User Id=sa;Password=<password>;TrustServerCertificate=True
```

## Create the database

From the repository root, restore the solution and apply the checked-in migration:

```powershell
dotnet restore .\QueryMISO.slnx
dotnet ef database update `
  --project .\MISOConsoleApp\MISOQueryingApp.csproj `
  --startup-project .\MISOConsoleApp\MISOQueryingApp.csproj
```

The migration creates two related tables:

- `FuelMixSnapshots` stores the MISO interval timestamp and total generation.
- `FuelMixElements` stores one row per fuel category and references its snapshot through `SnapshotId`.

`FuelMixSnapshots.IntervalEst` has a unique index to prevent duplicate snapshots for the same MISO interval. 
Fuel category and snapshot foreign-key columns are indexed to support category and relationship lookups.

## Run locally

After configuring SQL Server and applying the migration:

```powershell
dotnet run --project .\MISOConsoleApp\MISOQueryingApp.csproj
```

The application starts the background importer automatically. It makes an initial request to MISO, then waits one minute between subsequent requests.
The polling interval is intentionally not shorter than one minute so the service does not exceed MISO's public API polling limit.

## Read API

### Get fuel-mix snapshots

```http
GET /api/miso-fuel-mix
```

Optional query parameters:

| Parameter | Description |
| --- | --- |
| `from` | Start of the interval as a `DateTimeOffset`. Must be supplied with `to`. |
| `to` | End of the interval as a `DateTimeOffset`. Must be supplied with `from`. |
| `category` | Fuel category to include, such as `Wind` or `Coal`. |

The `from` and `to` bounds are inclusive. The results are ordered by `IntervalEst`.

Example:

```powershell
Invoke-RestMethod `
  "https://localhost:58706/api/miso-fuel-mix?from=2026-10-03T00:00:00-04:00&to=2026-10-04T00:00:00-04:00&category=Wind"
```

Example response:

```json
[
  {
    "intervalEst": "2026-10-03T12:00:00-04:00",
    "totalMegaWatts": 74523,
    "fuel": [
      {
        "category": "Wind",
        "megaWatts": 13789
      }
    ]
  }
]
```

Invalid interval combinations return `400 Bad Request`, including a missing bound or a `from` value later than `to`.

## Ingestion behavior

The ingestion flow is separated into three components:

1. `MISOFuelMixClient` requests `/api/FuelMix` from the public MISO API and deserializes the response.
2. `MISOFuelMixIngestionService` validates the total, interval, and fuel readings, converts the response to relational entities, and saves one snapshot with its child readings.
3. `MISOApiPoolingService` creates a dependency-injection scope and invokes ingestion immediately and once per minute afterward.

A failed ingestion attempt is logged and does not stop the hosted polling service; the next scheduled attempt can run normally. 
The current client does not add a separate retry loop, so a failure does not cause additional requests between scheduled polling attempts.

Note: MISO interval timestamps are stored as `DateTimeOffset` values representing Eastern Standard Time as supplied by the source response.

## Testing

Run all tests from the repository root:

```powershell
dotnet test .\QueryMISO.slnx
```

The tests cover:

- MISO response deserialization and invalid HTTP/JSON responses.
- Ingestion persistence, validation failures, and negative generation values.
- Polling-service startup, failure logging, and dependency resolution.
- HTTP response and query validation behavior.
- EF Core persistence using an in-memory SQLite database.
