# POS

A multi-tenant, offline-first Point-of-Sale platform built on **.NET 8**. The system pairs a centralized ASP.NET Core API with a Windows desktop terminal that operates against a local SQLite database and reconciles with the server via an outbox/cursor sync protocol.

The desktop terminal UI is hosted in a WPF shell using Microsoft Edge WebView2, rendering local HTML/CSS/JS screens served via a virtual host mapping (`https://pos.app/`).

## Solution Layout

| Project | Type | Purpose |
| --- | --- | --- |
| `POS.Api` | ASP.NET Core 8 Web API | Multi-tenant backend, JWT auth, SQL Server via EF Core |
| `POS.Desktop` | WPF (`net8.0-windows`) | WPF terminal, WebView2 shell, local SQLite, bridge-enabled HTML UI |
| `POS.Desktop.Hardware` | Class library | Hardware abstractions (cash drawer, printers, scanners, payment terminals, customer display, gateway) |
| `POS.Shared` | Class library | Domain entities, enums, DTOs, cross-cutting contracts |
| `POS.Tests` | xUnit | Integration + unit tests for POS.Api (`Microsoft.AspNetCore.Mvc.Testing`) |
| `POS.Desktop.Tests` | xUnit | Unit + integration tests for `POS.Desktop` (router, bridge, and services) |

## POS.Desktop UI Integration Status

The repository contains the implementation of the desktop UI integration up to **Milestone 3.4**:
- **Phase 1 Complete:** Generic Host bootstrap (Dependency Injection), borderless full-screen WebView2 shell, startup SQLite migration, and diagnostics/Evergreen runtime presence guard.
- **Phase 2 Complete:** Asset ingestion of 7 HTML screens hosted under `https://pos.app/` origin via virtual host mapping (retiring the simulator wrapper `index.html` for in-app views).
- **Milestone 3.1 Complete:** Two-way asynchronous postMessage transport channel.
- **Milestone 3.2 Complete:** Standardized v1 message envelope (`version`, `type`, `requestId`, `payload`, `ok`, `error`) with camelCase serialization and JS bridge helper.
- **Milestone 3.3 Complete:** `PosWebMessageRouter` dispatching bridge requests to registered C# handlers within scoped DI context.
- **Milestone 3.4 Complete:** In-memory operator session service (`ISessionService`), `session.get` and `session.clear` bridge message handlers, and post-login UI redirection utilizing bridge-backed C# session state.
- **Next Milestone:** **Milestone 3.5** — Swap remaining browser local state for bridge calls (implementing login PIN proof over the bridge).

For detailed phase roadmaps and task lists, see:
- [DESKTOP_UI_PHASE_MILESTONES.md](DESKTOP_UI_PHASE_MILESTONES.md)
- [DESKTOP_UI_MILESTONE_TASKS.md](DESKTOP_UI_MILESTONE_TASKS.md)
- [DESKTOP_UI_INTEGRATION_PLAN.md](DESKTOP_UI_INTEGRATION_PLAN.md)

## Bridge Overview

Communication between the hosted JavaScript UI and the C# WPF shell goes through a Promise-based bridge:
- JavaScript requests are sent via `posBridge.request(type, payload)`, which resolves to the payload on success and rejects with a structured error on failure.
- C# receives requests in `WebViewHost`, which maps them to `PosWebMessageRouter` for type-based dispatch to services.
- Business logic, validation, and data persistence reside strictly in C#; the JS UI is restricted to input/view rendering.
- Current active bridge types:
  - `transport.echo` (Echo test)
  - `session.get` (Retrieves active operator session details)
  - `session.clear` (Clears operator session on logout/shift close)

For conventions and schema details, see:
- [BRIDGE_ENVELOPE_SCHEMA.md](docs/bridge/BRIDGE_ENVELOPE_SCHEMA.md)
- [BRIDGE_CONVENTIONS.md](docs/bridge/BRIDGE_CONVENTIONS.md)
- [WEBVIEW2_TRANSPORT_OPTIONS.md](docs/bridge/WEBVIEW2_TRANSPORT_OPTIONS.md)

## Operator Session Model

The operator session tracks identity and login state for the active cashier:
- **Lifetime:** In-memory, process-lifetime only. No SQLite persistence.
- **Scope:** Single terminal, single active operator. Registered as a Singleton `ISessionService`.
- **Security:** Contains only safe metadata (`operatorId`, `displayName`, `role`, `loginTime`, `terminalId`, `sessionId`). It **never** stores sensitive credentials like PINs, passwords, payment card tokens, or payment details.
- **State Control:** Exposes current status through `session.get` and clears state via `session.clear` on shift close/logout. PIN and Employee verification logic are not yet part of the session state. (Removal of `localStorage.terminal_operator` on login is deferred to Milestone 3.5).
- **Source of Truth:** The C# session service is the single source of truth for operator status.

For more details, see [OPERATOR_SESSION_MODEL.md](docs/bridge/OPERATOR_SESSION_MODEL.md).

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
# Run API integration and unit tests
dotnet test POS.Tests

# Run Desktop unit and integration tests
dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug

# Run full solution build
dotnet build POS.slnx --configuration Debug
```

API tests use `ApiWebApplicationFactory` with a stubbed JWT handler (`TestAuthenticationHandler`) and a seeded in-memory dataset. Desktop tests cover `PosWebMessageRouter` and `OperatorSessionService` lifecycle contracts.

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

Central API/read endpoints are in place. The `POS.Desktop` WPF host shell serves local HTML/CSS/JS screens under `https://pos.app/`. The JS↔C# bridge foundation, message router, and in-memory operator session service are fully implemented up to Milestone 3.4. Hardware integration stubs (`POS.Desktop.Hardware`), central push/pull sync synchronization logic, and real offline business flows remain as future work. Milestone 3.5 (bridge-backed login PIN verification) is the next milestone to be implemented.
