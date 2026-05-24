# POS

A multi-tenant, offline-first Point-of-Sale platform built on **.NET 8**. The system pairs a centralized ASP.NET Core API with a Windows desktop terminal that operates against a local SQLite database and reconciles with the server via an outbox/cursor sync protocol.

## Solution Layout

| Project | Type | Purpose |
| --- | --- | --- |
| `POS.Api` | ASP.NET Core 8 Web API | Multi-tenant backend, JWT auth, SQL Server via EF Core |
| `POS.Desktop` | WPF (`net8.0-windows`) | Terminal app, local SQLite, sync engine |
| `POS.Desktop.Hardware` | Class library | Hardware abstractions (cash drawer, printers, scanners, payment terminals, customer display, gateway) |
| `POS.Shared` | Class library | Domain entities, enums, DTOs, cross-cutting contracts |
| `POS.Tests` | xUnit | Integration + unit tests (`Microsoft.AspNetCore.Mvc.Testing`) |

## Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB or full) for the central API database
- Windows (the desktop client targets `net8.0-windows`)
- **Microsoft Edge WebView2 Evergreen Runtime** (Required for `POS.Desktop` UI)

## Runtime Data (%LocalAppData%)

`POS.Desktop` stores all mutable state and diagnostic data in the user's local application data folder to ensure compatibility with locked-down terminal environments. It does not depend on its installation directory for writable access.

- **Local Database:** `%LocalAppData%/IMAGYN/POS/Desktop/Data/pos_local.db`
- **WebView2 User Data:** `%LocalAppData%/IMAGYN/POS/Desktop/WebView2`
- **Diagnostic Logs:** `%LocalAppData%/IMAGYN/POS/Desktop/Logs/pos-desktop.log`

**Deployment Note:** Install or ensure the Microsoft Edge WebView2 Evergreen Runtime is present on the target machine before launching `POS.Desktop`.

## Getting Started

```powershell
# Restore + build
dotnet restore POS.slnx
dotnet build POS.slnx

# Apply central DB migrations
dotnet ef database update --project POS.Api --context PosCentralDbContext

# Apply local SQLite migrations (design-time DB lives next to the project)
dotnet ef database update --project POS.Desktop --context PosLocalDbContext

# Run the API
dotnet run --project POS.Api

# Run the desktop client (separate terminal)
dotnet run --project POS.Desktop
```

Configure connection strings and JWT settings in `POS.Api/appsettings.json` (or `appsettings.Development.json`).

## Testing

```powershell
dotnet test POS.Tests
```

Tests use `ApiWebApplicationFactory` with a stubbed JWT handler (`TestAuthenticationHandler`) and a seeded in-memory dataset.

## Key Endpoints

All endpoints (except `/api/health`) require a JWT bearer token whose claims satisfy one of the configured policies (`UserOrAdmin`, `PosDevice`, `SystemScope`).

| Route | Policy | Description |
| --- | --- | --- |
| `GET /api/health` | none | Liveness probe |
| `GET /api/categories` | UserOrAdmin | List product categories |
| `GET /api/locations` | UserOrAdmin | List business locations |
| `GET /api/unitofmeasure` | UserOrAdmin | List units of measure |
| `GET /api/tenant/profile` | UserOrAdmin | Current tenant profile |

## Repository Conventions

- **Multi-tenancy** is enforced via EF Core query filters on `ICurrentTenantContext.CurrentTenantId`. Never bypass `TenantScopedEntity`.
- **Offline writes** on the desktop go through `SyncOutbox` (append-only with idempotency keys) — never write directly to a central-only entity from the client.
- **Domain entities** live in `POS.Shared/Domain/Entities/Central`. Shared enums in `POS.Shared/Enums`.
- See [ARCHITECTURE.md](ARCHITECTURE.md) for the full system design, data model, and sync protocol.

## Project Status

Phase 1 schema is in place (initial EF Core migrations dated 2026-05-18). `POS.Desktop.Hardware`, the Blazor UI shell, and the server-side `Sync` pipeline are scaffolded but not yet implemented.
