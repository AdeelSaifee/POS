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

The repository contains the implementation of the desktop UI integration up to **Milestone 4.2**:
- **Phase 1 Complete:** Generic Host bootstrap (Dependency Injection), borderless full-screen WebView2 shell, startup SQLite migration, and diagnostics/Evergreen runtime presence guard.
- **Phase 2 Complete:** Asset ingestion of 7 HTML screens hosted under `https://pos.app/` origin via virtual host mapping (retiring the simulator wrapper `index.html` for in-app views).
- **Milestone 3.1 Complete:** Two-way asynchronous postMessage transport channel.
- **Milestone 3.2 Complete:** Standardized v1 message envelope (`version`, `type`, `requestId`, `payload`, `ok`, `error`) with camelCase serialization and JS bridge helper.
- **Milestone 3.3 Complete:** `PosWebMessageRouter` dispatching bridge requests to registered C# handlers within scoped DI context.
- **Milestone 3.4 Complete:** In-memory operator session service (`ISessionService`), `session.get` and `session.clear` bridge message handlers, and post-login UI redirection utilizing bridge-backed C# session state.
- **Milestone 3.5 Complete:** Login PIN proof now goes through the JS↔C# bridge using `auth.validatePin`; valid stub credentials set `ISessionService`; `terminal_login.html` no longer compares PINs in JS and no longer writes to `localStorage.terminal_operator`.
- **Milestone 4.1 Complete:** Real provisioned-terminal context, fail-closed unprovisioned/half-provisioned behavior, and tenant-scoped query filter verification.
- **Milestone 4.2 Complete:** Durable `TerminalProvisioning` SQLite store, provisioning bridge handlers, `provision_terminal.html` bridge wiring, removal of `terminal_config` localStorage, startup loader for restart survival, and controlled reprovisioning support.
- **Next Milestone:** **Phase 4 / Milestone 4.3** — Minimal local catalog schema & seed.

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
  - `auth.validatePin` (Validates login PIN credentials over the bridge)
  - `provisioning.provisionTerminal` (Persists tenant/location/terminal identity through C# into SQLite)
  - `provisioning.getProvisioningStatus` (Returns fail-closed provisioning status from the persisted store)

The `auth.validatePin` action is currently implemented as a temporary deterministic stub for proving the login flow. It validates operator credentials against a local mock collection in C# and starts the in-memory operator session upon success. Real Employee/database verification is deferred to Phase 5.1 work.

Controlled re-provisioning is supported only through an explicit guarded `allowReprovision` flag; normal accidental re-provisioning remains blocked.

For conventions and schema details, see:
- [BRIDGE_ENVELOPE_SCHEMA.md](docs/bridge/BRIDGE_ENVELOPE_SCHEMA.md)
- [BRIDGE_CONVENTIONS.md](docs/bridge/BRIDGE_CONVENTIONS.md)
- [WEBVIEW2_TRANSPORT_OPTIONS.md](docs/bridge/WEBVIEW2_TRANSPORT_OPTIONS.md)

## Operator Session Model

The operator session tracks identity and login state for the active cashier:
- **Lifetime:** In-memory, process-lifetime only. No SQLite persistence.
- **Scope:** Single terminal, single active operator. Registered as a Singleton `ISessionService`.
- **Security:** Contains only safe metadata (`operatorId`, `displayName`, `role`, `loginTime`, `terminalId`, `sessionId`). It **never** stores sensitive credentials like PINs, passwords, payment card tokens, or payment details.
- **State Control:** Exposes current status through `session.get` and clears state via `session.clear` on shift close/logout. The C# session is set by `auth.validatePin` upon a valid login. `terminal_login.html` no longer writes the operator identity to `localStorage.terminal_operator`.
- **Source of Truth:** Operator session is still in-memory only. Terminal provisioning config is no longer stored in browser localStorage. Terminal provisioning truth now lives in the local SQLite TerminalProvisioning store and is loaded into ProvisionedTerminalContext on startup. Browser storage must not be used as source of truth for terminal/operator/provisioning state.

For more details, see [OPERATOR_SESSION_MODEL.md](docs/bridge/OPERATOR_SESSION_MODEL.md).

## Terminal Provisioning Model

- `TerminalProvisioning` is stored locally in SQLite.
- The table is single-row, `Id = 1`.
- It stores `TenantId`, `LocationId`, `TerminalId`, and `UpdatedAt`.
- It does not store PINs, passwords, tokens, payment/card data, DB paths, or connection strings.
- Provisioning state is loaded after local DB migrations at desktop startup.
- Missing/partial/invalid persisted state fails closed.
- Controlled re-provisioning requires explicit override and does not add catalog seed or sync.

## Login PIN Proof Status

The operator login workflow preserves the existing visual operator grid and 4-digit keypad UX, but delegates the PIN validation decision to C# via `posBridge.request('auth.validatePin', ...)`:
- In-JS PIN comparison has been removed.
- Operator PIN values were completely removed from the JavaScript `operators` collection in `terminal_login.html`.
- A successful login continues to trigger the checkmark overlay animation and navigates to `shift_open.html`.
- An invalid login still triggers the error shake and red dots UI feedback.
- This is a stub proof of authentication; integration with real database entities belongs to Phase 5.1.

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

API tests use `ApiWebApplicationFactory` with a stubbed JWT handler (`TestAuthenticationHandler`) and a seeded in-memory dataset. Desktop tests cover the `PosWebMessageRouter`, operator session lifecycle, `auth.validatePin` routing, validation outcomes (valid/invalid PIN), `session.get`, provisioning context tests, `TerminalProvisioning` SQLite store tests, provisioning bridge handler tests, startup loader/restart survival tests, controlled re-provisioning tests, and provisioning without catalog seed tests.

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

Central API/read endpoints remain in place.
POS.Desktop WPF/WebView2 shell, JS↔C# bridge, operator session, bridge-backed login PIN proof, and terminal provisioning through Milestone 4.2 are complete.

Remaining future work:
- **Phase 4 / Milestone 4.3:** Minimal local catalog schema & seed
- **Phase 4.4:** Catalog read service and bridge endpoints if listed in milestone docs
- **Phase 5:** Employee-backed authentication, shift, order, payment, cash control, Z-report flows
- **Phase 6:** Push/pull synchronization
- **Phase 7:** Hardware integration
- **Phase 8:** Packaging, offline assets, telemetry hardening
