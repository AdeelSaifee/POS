# POS

A multi-tenant, offline-first Point-of-Sale platform built on **.NET 8**. The solution pairs a centralized ASP.NET Core API (SQL Server) with a Windows desktop terminal that operates against a local SQLite database. The desktop UI is a WPF shell hosting Microsoft Edge WebView2; HTML/CSS/JS screens are served via a virtual host mapping (`https://pos.app/`) and call into C# services through a typed JS↔C# bridge.

## Solution Layout

| Project | Type | Purpose |
| --- | --- | --- |
| `POS.Api` | ASP.NET Core 8 Web API | Central multi-tenant backend, JWT auth, SQL Server via EF Core |
| `POS.Desktop` | WPF (`net8.0-windows`) | Terminal shell, WebView2 hosting, local SQLite, JS↔C# bridge, operational services |
| `POS.Desktop.Hardware` | Class library | Hardware abstractions (cash drawer, printers, scanners, payment terminal, customer display, gateway) — deferred |
| `POS.Shared` | Class library | Shared domain entities, enums, DTOs, contracts |
| `POS.Tests` | xUnit | Integration and unit tests for `POS.Api` |
| `POS.Desktop.Tests` | xUnit | Service, bridge handler, and static HTML/parity tests for `POS.Desktop` |

## Desktop UI Integration — Phase Status

| Phase | Status | Scope |
| --- | --- | --- |
| Phase 1 | Complete | WPF shell bootstrap, Generic Host DI, borderless full-screen WebView2 foundation, local SQLite migration on startup, Evergreen runtime guard, prototype hosting basics |
| Phase 2 | Complete | Asset ingestion under `https://pos.app/`, bridge transport groundwork, shell stability, UI prototype integration foundation |
| Phase 3 | Complete | Bridge envelope/contracts, `PosWebMessageRouter` dispatch, session/auth/provisioning handlers, operational shell integration |
| Phase 4 | Complete | Local SQLite provisioning store, catalog/tax/tender/reason-code seed and read services, local DB foundation |
| Phase 5.1 | Complete | Authentication & login service (`IAuthService` / `LocalEmployeeAuthService` / PIN verifier, `LocalTerminalSession` persistence) |
| Phase 5.2 | Complete | Shift open flow (`IShiftService`, opening cash, config-driven checklist & policy, shift gating) |
| Phase 5.3 | Complete | Order/cart service (cart math, tax, proportional discount, central money rounding) |
| Phase 5.4 | Complete | Payment completion (tenders, change, atomic order/lines/payments/outbox/print, receipt rendering, idempotency) |
| Phase 5.5 | Complete | Cash control service (safe drops, manager PIN, drawer summary, threshold alerts, ledger, reason codes) |

The current head of work is **Phase 5 / Milestone 5.5 — Cash control service, COMPLETE**.

For detailed phase roadmaps and task lists, see:
- [DESKTOP_UI_PHASE_MILESTONES.md](DESKTOP_UI_PHASE_MILESTONES.md)
- [DESKTOP_UI_MILESTONE_TASKS.md](DESKTOP_UI_MILESTONE_TASKS.md)
- [DESKTOP_UI_INTEGRATION_PLAN.md](DESKTOP_UI_INTEGRATION_PLAN.md)

## Implemented Desktop Flows

- **Terminal provisioning** — `TerminalProvisioning` row persisted in local SQLite; loaded into `ProvisionedTerminalContext` at startup; fail-closed on unprovisioned/half-provisioned state; controlled re-provisioning via an explicit `allowReprovision` flag.
- **Login** — operator grid + 4-digit keypad in `terminal_login.html`; PIN validated in C# by `LocalEmployeeAuthService` (via `IPinVerifier`); successful login starts an `OperatorSession` and persists a `LocalTerminalSession`.
- **Operator session** — in-memory `OperatorSessionService`, single-active-operator per terminal, cleared on logout/shift close. Local `LocalTerminalSession` records login/logout history.
- **Shift open** — `shift_open.html` reads cash-drawer limit, safe-drop threshold, and pre-shift checklist from `shift.getOpenPolicy`; `shift.open` persists a `LocalShift` with the opening cash float. All operational screens gate on `shift.getCurrent`.
- **Checkout / cart** — `main_checkout.html` is backed by `IOrderService` / `DraftCartStore`; add/quantity/remove, line and cart discount, tax (inclusive/exclusive), proportional discount distribution, and centralized money rounding via `MoneyRounder`.
- **Payment completion** — `payment_screen.html` calls `payment.getTenderMethods` and `payment.complete`; `PaymentService` atomically writes `LocalOrder` / `LocalOrderLine` / `LocalPayment`, enqueues a `SyncOutbox` event and a `PrintQueue` receipt row, computes cash change, renders a plain-text receipt via `ReceiptRenderer`, and enforces a mandatory `IdempotencyKey` with deterministic SHA-256 payload fingerprinting.
- **Cash control** — `cash_control.html` calls `cash.getSummary`, `cash.recordMovement`, `cash.getLedger`, and `cash.getReasonCodes`. `CashControlService` persists `LocalCashDrawerMovement` (Drop only), enforces manager PIN via `IAuthService.ValidateManagerPinAsync` when the resolved reason code requires approval, computes drawer balance/alerts against `ShiftOpenPolicyOptions`, and scopes movements to the active open shift on this terminal/location.
- **UI prototype parity** — every operational screen exists in both `POS.Desktop/Assets/ui` and `docs/ui-prototype/screens`; copies are kept byte-identical and verified by SHA-256 parity tests.

## Bridge Overview

Communication between the hosted HTML/JS UI and the C# WPF shell goes through a Promise-based `posBridge.request(type, payload)` channel routed by `PosWebMessageRouter`. Business logic, validation, and persistence live in C#.

Registered local desktop bridge endpoints (verified in `POS.Desktop/Shell/PosWebMessageRouter.cs`):

| Type | Purpose |
| --- | --- |
| `transport.echo` | Transport health check |
| `session.get` | Returns active operator session |
| `session.clear` | Clears the operator session |
| `auth.validatePin` | Validates operator PIN, starts session, persists `LocalTerminalSession` |
| `provisioning.provisionTerminal` | Persists tenant/location/terminal identity to SQLite |
| `provisioning.getProvisioningStatus` | Fail-closed provisioning status |
| `catalog.listCategories` | Lists catalog categories |
| `catalog.listItems` | Lists/filters catalog items |
| `catalog.searchItems` | Search catalog items |
| `catalog.lookupByIdentifier` | Barcode/identifier lookup |
| `shift.open` | Opens a shift with an opening float |
| `shift.getCurrent` | Returns current open shift (gate for operational screens) |
| `shift.getOpenPolicy` | Returns cash limit, safe-drop threshold, checklist from config |
| `order.getCart` | Returns the current draft cart |
| `order.addItem` | Adds a variant to the cart |
| `order.updateLineQuantity` | Updates a cart line quantity |
| `order.removeItem` | Removes a cart line |
| `order.clearCart` | Clears the cart |
| `order.applyDiscount` | Applies a cart-level discount |
| `order.removeDiscount` | Removes the cart-level discount |
| `payment.getTenderMethods` | Lists local tender methods |
| `payment.complete` | Completes an order atomically with idempotency |
| `cash.getSummary` | Drawer balance, totals, and alert state |
| `cash.recordMovement` | Records a cash drawer movement (Drop only) |
| `cash.getLedger` | Returns movements for the active open shift |
| `cash.getReasonCodes` | Reason codes for cash control (with category fallback) |

`payment.complete` and `cash.recordMovement` require a non-blank `idempotencyKey`. `cash.recordMovement` accepts only `Drop` (string, case-insensitive) or numeric enum value `4`; all other movement types are rejected at the bridge layer with `INVALID_MOVEMENT_TYPE`.

For envelope and convention details, see:
- [BRIDGE_ENVELOPE_SCHEMA.md](docs/bridge/BRIDGE_ENVELOPE_SCHEMA.md)
- [BRIDGE_CONVENTIONS.md](docs/bridge/BRIDGE_CONVENTIONS.md)
- [WEBVIEW2_TRANSPORT_OPTIONS.md](docs/bridge/WEBVIEW2_TRANSPORT_OPTIONS.md)
- [OPERATOR_SESSION_MODEL.md](docs/bridge/OPERATOR_SESSION_MODEL.md)

## Runtime Data

`POS.Desktop` keeps all mutable state under the user's local application data folder so it does not depend on its installation directory for writable access.

- **Local database (SQLite):** `%LocalAppData%/IMAGYN/POS/Desktop/Data/pos_local.db`
- **WebView2 user data:** `%LocalAppData%/IMAGYN/POS/Desktop/WebView2`
- **Diagnostic logs:** `%LocalAppData%/IMAGYN/POS/Desktop/Logs/pos-desktop.log`

Local SQLite is owned by `PosLocalDbContext`. EF Core migrations live under `POS.Desktop/Data/Migrations/Local` and are applied automatically on desktop startup. The central SQL Server database is owned by `POS.Api` / `PosCentralDbContext` and is unaffected by desktop migrations.

Local entity areas (all tenant-scoped via `CurrentTenantId` query filter):

- **Provisioning:** `TerminalProvisioning`
- **Catalog & config:** `LocalCategory`, `LocalItem`, `LocalItemVariant`, `LocalItemIdentifier`, `LocalItemPrice`, `LocalUnitOfMeasure`, `LocalTaxRule`, `LocalTenderMethod`, `LocalReasonCode`
- **Auth / session:** `LocalEmployee`, `LocalEmployeeLocationRole`, `LocalTerminalSession`
- **Shift / sales:** `LocalShift`, `LocalOrder`, `LocalOrderLine`, `LocalPayment`
- **Cash control:** `LocalCashDrawerMovement` (append-only, Drop only)
- **Operational queues:** `SyncOutbox`, `PrintQueue`, `SyncCursor`, `PaymentReconciliationQueue`, `LocalRecoveryJournal`, `LocalRetentionState`

## Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB or full) for the central API database
- Windows (`POS.Desktop` targets `net8.0-windows`)
- **Microsoft Edge WebView2 Evergreen Runtime** on the terminal machine

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

Configure connection strings and JWT settings in `POS.Api/appsettings.json`. Configure cash drawer / safe-drop limits and the shift-open checklist under the `ShiftOpen` section in `POS.Desktop/appsettings.json`.

## Testing

```powershell
# Central API tests
dotnet test POS.Tests

# Desktop service, bridge, and static UI tests
dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug

# Full solution build
dotnet build POS.slnx --configuration Debug
```

`POS.Desktop.Tests` includes:

- Service tests (auth, session, shift, order, payment, cash control)
- Bridge handler tests for every registered router endpoint
- Static HTML tests asserting that `Assets/ui` and `docs/ui-prototype/screens` copies are SHA-256 identical, that operational screens use the bridge (not `localStorage` / `sessionStorage`) for source-of-truth state, and that forbidden patterns (demo timeouts, in-JS PIN compares, etc.) stay out

The latest project context records **451 desktop tests passing** in `POS.Desktop.Tests` and **49 tests passing** in `POS.Tests`. (Counts are taken from `docs/antigravity-context/POS_DESKTOP_CURRENT_CONTEXT.md`; this README does not claim a fresh local test run.)

## Project Conventions

- **Browser storage is never the source of truth for operational data.** `sessionStorage` / `localStorage` may not gate operational screens, store provisioning truth, hold the cart, or hold tender state.
- **Business logic lives in C# services**, not in HTML/JS. JS screens call bridge endpoints and render results.
- **UI prototype parity.** Each screen in `POS.Desktop/Assets/ui/*.html` has a byte-identical sibling in `docs/ui-prototype/screens/*.html`; SHA-256 parity is enforced by static tests.
- **No logging of sensitive data.** PINs (operator or manager) are never logged, persisted raw, or returned in responses.
- **Idempotency on writes.** `payment.complete` and `cash.recordMovement` require a non-blank `idempotencyKey`; the database enforces uniqueness on the relevant indexes.
- **Multi-tenancy** is enforced via EF Core query filters on `CurrentTenantId` in both `PosCentralDbContext` and `PosLocalDbContext`. Never bypass `TenantScopedEntity`.
- **Append-only local cash movements / outbox / print queue.** Corrections become new rows, not mutations.

See [ARCHITECTURE.md](ARCHITECTURE.md) for the full system design and the desktop service architecture.

## Project Status

**Implemented** — Central API read endpoints; desktop shell, WebView2 hosting, bridge transport and router; provisioning; catalog read; authentication & login through Milestone 5.1; shift open through Milestone 5.2; cart/order through Milestone 5.3; payment completion through Milestone 5.4; cash control through Milestone 5.5.

**Deferred / not implemented:**

- `FloatInjection` domain design (enum value not added; UI Injection tab deferred and blocked at bridge layer)
- `Payout` and `Correction` cash movement implementations (rejected by bridge and service)
- Shift close and Z-report (Phase 5.6)
- Real hardware integrations — cash drawer, pinpad, printer wiring (Phase 7)
- Central sync transport for cash movements and other local writes (Phase 6) — local outbox rows exist, push/pull pipeline does not
- Packaging, offline assets, telemetry hardening (Phase 8)
