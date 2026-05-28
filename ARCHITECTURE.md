# POS — Architecture

This document describes the structure, data model, runtime behavior, and desktop service architecture of the POS solution. It is the authoritative reference for engineers extending the system.

## 1. System Overview

POS is a **multi-tenant, offline-first** point-of-sale platform with two deployable units:

1. **POS.Api** — central ASP.NET Core 8 backend over SQL Server. Owns the master catalog, tenants, locations, employees, and the durable record of transactions.
2. **POS.Desktop** — WPF terminal (`net8.0-windows`) hosting a WebView2-rendered HTML/JS UI. Operates fully offline against a local SQLite database and exposes operational logic to the UI through a typed JS↔C# bridge.

`POS.Shared` carries the domain model, enums, and contracts that both sides depend on. `POS.Desktop.Hardware` is the (still-deferred) abstraction layer for peripheral devices.

```
+--------------------+     JWT / HTTPS (deferred sync)     +-----------------------+
|  POS.Desktop (WPF) |  <--------------------------------> |   POS.Api (ASP.NET)   |
|  + WebView2 shell  |     Outbox push / cursor pull       |   + SQL Server        |
|  + JS↔C# bridge    |        (Phase 6 — not wired)         |   + EF Core 8         |
|  + SQLite (local)  |                                      |   + JWT bearer        |
|  + C# services     |                                      |                       |
+--------------------+                                      +-----------------------+
```

### Design Principles

- **Tenant isolation by query filter.** Every central entity inherits from `TenantScopedEntity` / `TenantRootEntity` / `GuidTenantScopedEntity`; both `PosCentralDbContext` and `PosLocalDbContext` apply a global filter against `CurrentTenantId`.
- **Append-only transactions.** Orders, payments, shifts, and cash drawer movements are immutable. Corrections are new rows, never mutations.
- **Offline-first writes.** The desktop owns the source-of-truth for in-flight transactions until the API acknowledges them. Each event carries an `IdempotencyKey`.
- **Versioned catalog/rules.** `Item` and `Order` carry `CatalogVersion` / `RuleVersion` so that price/tax outcomes are reproducible.
- **C# owns logic; JS owns rendering.** Operational data never lives in browser storage; UI screens call bridge endpoints for both reads and writes.

## 2. Projects

### 2.1 POS.Api

ASP.NET Core 8 Web API.

| Folder | Responsibility |
| --- | --- |
| `Controllers/` | Thin HTTP layer |
| `Application/` | Read services (`ICategoryReadService`, `ILocationReadService`, `IUnitOfMeasureReadService`, `IHealthStatusService`, `ITenantProfileReadService`) |
| `Contracts/` | Sealed-record DTOs |
| `Data/` | `PosCentralDbContext`, entity configurations, migrations (`Data/Migrations/Central`) |
| `Services/` | `ICurrentTenantContext`, tenant profile composition |
| `Auth/` | JWT setup, `ApiClaimTypes`, policy handlers |
| `Configuration/` | Startup guards for connection strings / JWT options |
| `Sync/` | Placeholder for inbound ingest endpoints (not yet implemented) |

Endpoints:

| Method | Route | Policy |
| --- | --- | --- |
| GET | `/api/health` | (anonymous) |
| GET | `/api/categories` | `UserOrAdmin` |
| GET | `/api/locations` | `UserOrAdmin` |
| GET | `/api/unitofmeasure` | `UserOrAdmin` |
| GET | `/api/tenant/profile` | `UserOrAdmin` |

Authorization policies: `UserOrAdmin`, `PosDevice`, `SystemScope`. Custom claim names in `Auth/ApiClaimTypes.cs`.

### 2.2 POS.Desktop

WPF terminal. The entry point composes a Generic Host (`DesktopHostBuilder`) for DI; the shell hosts WebView2 with a virtual host mapping to `https://pos.app/` for local HTML assets.

| Folder | Responsibility |
| --- | --- |
| `Assets/ui/` | HTML/CSS/JS screens served via the WebView2 virtual host |
| `Bridge/` | Request/response envelope contracts, JSON options |
| `Shell/` | `PosWebMessageRouter` (bridge dispatch), `WebViewHost`, window/shell wiring |
| `Configuration/` | `DesktopHostBuilder` — DI composition, EF wiring, options binding |
| `Data/` | `PosLocalDbContext`, `LocalEntities/`, `Configurations/Local/`, `Migrations/Local/` |
| `Services/Provisioning/` | `ITerminalProvisioningStore`, `ProvisionedTerminalContext`, startup loader |
| `Services/Catalog/` | `ICatalogService` / `CatalogService` and DTOs |
| `Services/Auth/` | `IAuthService` / `LocalEmployeeAuthService`, `IPinVerifier` / `PinVerifier` |
| `Services/Session/` | `ISessionService` / `OperatorSessionService`, `OperatorSession` |
| `Services/Shifts/` | `IShiftService` / `ShiftService`, `ShiftOpenPolicyOptions` |
| `Services/Orders/` | `IOrderService` / `OrderService`, `IDraftCartStore` / `DraftCartStore`, `MoneyRounder` |
| `Services/Payments/` | `IPaymentService` / `PaymentService`, completion requests/results |
| `Services/Receipts/` | `IReceiptRenderer` / `ReceiptRenderer` |
| `Services/CashControl/` | `ICashControlService` / `CashControlService` |

### 2.3 POS.Desktop.Hardware

Class library with vendor-neutral folders — `CashDrawer`, `CustomerDisplay`, `PaymentTerminal`, `Printers`, `Scanner`, `Gateway`. Concrete drivers are not implemented; planned for Phase 7.

### 2.4 POS.Shared

Domain model and contracts shared by both API and desktop:

- **Base classes:** `TenantScopedEntity`, `TenantRootEntity`, `GuidTenantScopedEntity`, `OfflineCreatedEntity`, `AppendOnlyEntity`, `LocalOperationalEntity`.
- **Central entities:** `Domain/Entities/Central/` — see §4.1.
- **Enums:** order, payment, shift, inventory, sync, retention, recovery, terminal provisioning, cash movement (`CashDrawerMovementType` — only `Drop` is used by the desktop today).
- **Contracts:** `ICurrentTenantContext`, `IProvisionedTerminalContext`.

### 2.5 POS.Tests

xUnit + `Microsoft.AspNetCore.Mvc.Testing`. Integration tests spin up the API via `ApiWebApplicationFactory`, swap in `TestAuthenticationHandler`, and seed data via `ApiTestDataSeeder`. Coverage focuses on tenant isolation, authorization policies, and ensuring secrets never leak into responses. Current count recorded in project context: **49 passing**.

### 2.6 POS.Desktop.Tests

xUnit. Covers:

- **Service tests** — auth, session, shift, order, payment, cash control, catalog.
- **Bridge handler tests** — one suite per registered router type, validating envelope, payload validation, success path, idempotency mapping, and safe-failure mapping.
- **Static HTML tests** — assert byte-identical SHA-256 parity between `POS.Desktop/Assets/ui/*.html` and `docs/ui-prototype/screens/*.html`, presence of required bridge calls, absence of forbidden patterns (in-JS PIN compare, `sessionStorage`/`localStorage` gating, demo `setTimeout` simulations).

Current count recorded in project context: **451 passing**.

## 3. Desktop Architecture Flow

Every operational request follows the same path:

```
HTML screen
  → pos-bridge-transport.js  (posBridge.request("type", payload))
  → WebView2 web-message channel
  → WebViewHost
  → PosWebMessageRouter      (envelope validation, DI scope, dispatch)
  → C# service               (IAuthService, IShiftService, IOrderService,
                              IPaymentService, ICashControlService, ...)
  → PosLocalDbContext        (SQLite, append-only, tenant-filtered)
  → response envelope back to JS
```

Each bridge request runs inside a freshly-created `IServiceScope`, so scoped services (e.g. `PosLocalDbContext`, `IOrderService`) get a clean instance per call. `DraftCartStore` is the deliberate exception — registered as a thread-safe `Singleton` so that the cart survives across the transient bridge scopes that operate on it. `OperatorSessionService` is also a `Singleton` (single active operator per terminal).

## 4. Data Model

### 4.1 Central database (`PosCentralDbContext`, SQL Server)

30+ `DbSet`s grouped by concern:

- **Tenant & org:** `Company`, `Location`, `Terminal`
- **People:** `Employee`, `EmployeeLocationRole`, `Customer`, `TerminalSession`
- **Catalog:** `Category`, `Item`, `ItemVariant`, `ItemIdentifier`, `ItemPrice`, `ItemStock`, `UnitOfMeasure`, `PriceList`
- **Tax & tender config:** `TaxRule`, `ReasonCode`, `TenderMethod`, `ReceiptTemplate`
- **Transactions:** `Order` → `OrderLine`, `Payment`, `Shift`, `CashDrawerMovement`, `ZReport`
- **Operations:** `InventoryMovement`, `ManagerAction`, `SyncIngestAck`

Multi-tenant query filters are applied centrally in `PosCentralDbContext.OnModelCreating`.

### 4.2 Local database (`PosLocalDbContext`, SQLite)

| Area | Entities |
| --- | --- |
| Provisioning / terminal | `TerminalProvisioning` |
| Catalog / pricing / tax / tender / reasons | `LocalCategory`, `LocalItem`, `LocalItemVariant`, `LocalItemIdentifier`, `LocalItemPrice`, `LocalUnitOfMeasure`, `LocalTaxRule`, `LocalTenderMethod`, `LocalReasonCode` |
| Auth / employees / sessions | `LocalEmployee`, `LocalEmployeeLocationRole`, `LocalTerminalSession` |
| Shift | `LocalShift` |
| Orders / sales | `LocalOrder`, `LocalOrderLine`, `LocalPayment` |
| Cash control | `LocalCashDrawerMovement` (append-only, currently `Drop` only) |
| Operational queues | `SyncOutbox`, `PrintQueue`, `SyncCursor`, `PaymentReconciliationQueue`, `LocalRecoveryJournal`, `LocalRetentionState` |

Every local entity is filtered by `CurrentTenantId` from `IProvisionedTerminalContext`. Cash and order entities additionally enforce location/terminal/shift scoping at the service layer.

EF Core migrations live under `POS.Desktop/Data/Migrations/Local`. Migrations applied to date include the order/payment tables, the unique index on `(TenantId, IdempotencyKey)` for `LocalOrder`, and the `LocalCashDrawerMovement` table with positive-amount and non-empty check constraints and unique `(TerminalSequence)` / `(IdempotencyKey)` indexes.

## 5. Implemented Desktop Services

### 5.1 Auth

- **`IAuthService` / `LocalEmployeeAuthService`** — validates operator PIN against `LocalEmployee` rows (filtered by tenant + location role), creates a fresh `LocalTerminalSession`, and exposes `ValidateManagerPinAsync` for manager overrides.
- **`IPinVerifier` / `PinVerifier`** — hash compare; raw PINs are never logged or returned.
- `auth.validatePin` is the only bridge entry point; `terminal_login.html` does not compare PINs in JS.

### 5.2 Session

- **`ISessionService` / `OperatorSessionService`** — in-memory, process-lifetime, single active operator per terminal. `Singleton` registration.
- **`OperatorSession`** — `OperatorId`, `DisplayName`, `Role`, `LoginTime`, `TerminalId`, `SessionId`. No PINs, no tokens.
- **`LocalTerminalSession`** — durable login/logout history rows in SQLite.

### 5.3 Shift

- **`IShiftService` / `ShiftService`** — `OpenShiftAsync(openingFloat)`, `GetCurrentShiftAsync()`, `GetOpenPolicyAsync()`. Persists `LocalShift`. Filters by tenant + location + terminal so shifts opened elsewhere never bleed through.
- **`ShiftOpenPolicyOptions`** — typed options bound from `appsettings.json` `"ShiftOpen"`. Constants: `DefaultCashDrawerLimit` (25,000), `DefaultAutoSafeDropThreshold` (20,000), `MaxChecklistItems` (10). Sanitization replaces non-positive limits with defaults and trims/caps the checklist.

### 5.4 Order / Cart

- **`IOrderService` / `OrderService`** — add/qty/remove, apply/remove cart discount, totals, tax, money rounding. Throws `OrderValidationException` with an `ErrorCode` + safe message for bridge mapping.
- **`IDraftCartStore` / `DraftCartStore`** — thread-safe `Singleton` backing store for the active draft cart.
- **`MoneyRounder`** — centralized 2-dp rounding with `MidpointRounding.AwayFromZero`.
- **Tax rules:** exclusive — `taxAmount = (gross − discount) * rate / 100`; inclusive — `taxAmount = base − base / (1 + rate / 100)`.
- **Cart-level discount distribution:** proportional across lines by gross share; last line absorbs the rounding remainder.

### 5.5 Payment

- **`IPaymentService` / `PaymentService`** — validates session/shift/tenders, computes change (cash only), commits `LocalOrder` + lines + payments + `SyncOutbox` event + `PrintQueue` row inside a single SQLite transaction.
- **`PaymentCompletionRequest`** carries tenders, optional `GuestName` / `GuestPhone`, and a **mandatory** `IdempotencyKey`.
- **Idempotency** — early lookup before cart-empty validation; deterministic SHA-256 fingerprint over tenant/location/terminal/shift/business-date/cart-lines/tenders/guest. Duplicate keys with mismatched payloads return `IDEMPOTENCY_CONFLICT`; matching duplicates return the original outcome. Unique-index race collisions are rolled back, the persisted order is reloaded, and the original result is returned safely.
- **`IReceiptRenderer` / `ReceiptRenderer`** — fully data-driven plain-text receipt; safe width helper guards against negative-width edge cases.

### 5.6 Cash Control

- **`ICashControlService` / `CashControlService`** — `RecordMovementAsync(...)`, `GetDrawerSummaryAsync(...)`. Validates session/shift/amount/reason code, looks up idempotency before any side effect, persists `LocalCashDrawerMovement` inside an isolated transaction.
- **`LocalCashDrawerMovement`** is append-only with positive-amount and non-empty check constraints and unique indexes on `(TerminalSequence)` and `(IdempotencyKey)` per tenant/shift.

See §6 for the cash control rules in detail.

## 6. Cash Control Architecture

- **Drop only.** The service and bridge only accept `CashDrawerMovementType.Drop`. `FloatInjection` is **not** present as an enum value; `Payout`, `Correction`, `NoSale`, `OpeningFloat`, `SaleCashIn`, and `RefundCashOut` are explicitly rejected at the bridge layer with `INVALID_MOVEMENT_TYPE`. The UI Injection tab is rendered as a deferred placeholder and blocked.
- **Active shift scoping.** The service resolves the active open shift from the current `LocationId` + `TerminalId` itself — `ShiftId` is **not** accepted from the UI payload. Closed shifts, other-terminal shifts, and other-location shifts are ignored/rejected. A null `ShiftId` in the terminal session is tolerated when a valid open shift exists.
- **Manager PIN.** When the resolved `LocalReasonCode.RequiresManagerApproval` is true, the service calls `IAuthService.ValidateManagerPinAsync(managerOperatorId, managerPin)`. The verified employee id is persisted in `LocalCashDrawerMovement.AuthorizedByEmployeeId`. The raw `managerPin` is never logged, persisted, or returned. The idempotency check runs **before** PIN verification so retries of an already-persisted movement do not re-prompt for a manager.
- **Drawer balance formula:**

  ```
  ExpectedDrawerBalance = OpeningFloat + CashSales − SafeDrops + FloatInjections
  ```

  `FloatInjections` is currently always `0` (the enum value does not exist). Sums are evaluated in memory because the SQLite EF provider cannot translate `decimal` aggregation / `DateTimeOffset` ordering reliably.

- **Threshold alerts.** Computed against `ShiftOpenPolicyOptions.CashDrawerLimit` and `AutoSafeDropThreshold`. Returns one of `OK`, `SAFE_DROP_RECOMMENDED`, `OVER_LIMIT`. Config anomalies (`<= 0`) fall back to the constant defaults.
- **Reason codes.** `cash.getReasonCodes` filters `LocalReasonCodes` by `ReasonCategory == "CashControl"` (case-insensitive). If zero match, it falls back to all active codes and sets `usedFallback = true` so the UI can render appropriately.
- **Ledger.** `cash.getLedger` is restricted to the active open shift, joins reason codes, sorts by `TerminalSequence` desc then `OccurredOn` desc, capped at 100 rows.

## 7. Security & Audit Notes

- **PIN handling.** Operator and manager PINs are never logged, never stored raw, and never returned in bridge responses. They are validated through `IPinVerifier` / `IAuthService` and discarded.
- **Browser storage.** `localStorage` / `sessionStorage` are never the source of truth for operational data — provisioning, session, cart, tender, and cash movement state all flow through C# services. Static tests enforce this on every operational screen.
- **Append-only ledgers.** `LocalCashDrawerMovement`, `LocalOrder`, `LocalOrderLine`, `LocalPayment`, `SyncOutbox`, and `PrintQueue` rows are inserted, never mutated; corrections become new rows.
- **Idempotency.** `payment.complete` and `cash.recordMovement` require a non-blank `idempotencyKey`. SQLite unique indexes back the in-code checks so concurrent retries cannot double-write.
- **Scoping.** Every read and write is scoped by `TenantId` (query filter) plus `LocationId` / `TerminalId` / `ShiftId` at the service layer.
- **Fail-closed.** Unprovisioned or half-provisioned terminals cannot reach operational screens. If `shift.getCurrent` reports no open shift, the UI redirects to `shift_open.html`. Bridge handler exceptions are mapped to safe error codes with no internal detail.

## 8. Cross-Cutting Concerns

- **DI composition.** API in `Program.cs`; desktop in `Configuration/DesktopHostBuilder.cs`.
- **Configuration guards.** Both projects fail-fast at startup if required connection strings or options are missing.
- **Recovery.** `LocalRecoveryJournal` records crash points; `RequiredRecoveryAction` drives the next desktop launch.
- **Provisioning lifecycle.** `TerminalProvisioningStartupLoader` rehydrates `ProvisionedTerminalContext` from SQLite after local migrations. Controlled re-provisioning is gated by `allowReprovision = true` on the bridge payload.

## 9. Roadmap / Not Yet Implemented

- **Phase 5.6 — Shift close & Z-report.** `IShiftService.CloseShiftAsync`, expected vs. actual cash variance, Z-report reconciliation, lock-on-close.
- **`FloatInjection` / `Payout` / `Correction`.** Domain design (including an enum value for `FloatInjection`) and accompanying service / bridge / UI flows.
- **Hardware integration (Phase 7).** Real cash drawer kick, printer wiring, pinpad / payment terminal, scanner, customer display under `POS.Desktop.Hardware`.
- **Sync transport (Phase 6).** Outbound `SyncOutbox` pusher, inbound `SyncCursor` puller, and the API-side ingest controllers. Local outbox rows are produced today; transport and central ack are not yet wired.
- **Central write/sync endpoints.** Beyond the current read endpoints, the API does not yet expose order/payment/shift/cash ingest routes.
- **Packaging & telemetry (Phase 8).** Installer, offline-asset bundling, telemetry hardening.

## 10. File Map (Quick Reference)

- API auth & DI: [Program.cs](POS.Api/Program.cs)
- Central DbContext: [PosCentralDbContext.cs](POS.Api/Data/PosCentralDbContext.cs)
- Local DbContext: [PosLocalDbContext.cs](POS.Desktop/Data/PosLocalDbContext.cs)
- Desktop host: [DesktopHostBuilder.cs](POS.Desktop/Configuration/DesktopHostBuilder.cs)
- Bridge router: [PosWebMessageRouter.cs](POS.Desktop/Shell/PosWebMessageRouter.cs)
- Auth service: [LocalEmployeeAuthService.cs](POS.Desktop/Services/Auth/LocalEmployeeAuthService.cs)
- Shift service: [ShiftService.cs](POS.Desktop/Services/Shifts/ShiftService.cs)
- Order service: [OrderService.cs](POS.Desktop/Services/Orders/OrderService.cs)
- Payment service: [PaymentService.cs](POS.Desktop/Services/Payments/PaymentService.cs)
- Cash control service: [CashControlService.cs](POS.Desktop/Services/CashControl/CashControlService.cs)
- Receipt renderer: [ReceiptRenderer.cs](POS.Desktop/Services/Receipts/ReceiptRenderer.cs)
- Domain base types: [POS.Shared/Domain/Base](POS.Shared/Domain/Base)
- Enums: [POS.Shared/Enums](POS.Shared/Enums)
