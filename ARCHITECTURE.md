# POS — Architecture

This document describes the structure, data model, and runtime behavior of the POS solution. It is the authoritative reference for engineers extending the system.

## 1. System Overview

POS is a **multi-tenant, offline-first** point-of-sale platform with two deployable units:

1. **POS.Api** — central ASP.NET Core 8 backend over SQL Server. Owns the master catalog, tenants, locations, employees, and the durable record of transactions.
2. **POS.Desktop** — WPF terminal app over a local SQLite database. Operates fully offline; uses a sync engine to push transactions and pull catalog/configuration changes.

`POS.Shared` carries the domain model, enums, and contracts that both sides depend on. `POS.Desktop.Hardware` is the abstraction layer for peripheral devices.

```
+--------------------+        JWT / HTTPS         +-----------------------+
|  POS.Desktop (WPF) |  <----------------------> |   POS.Api (ASP.NET)   |
|  + SQLite          |   Outbox push / cursor    |   + SQL Server        |
|  + Sync engine     |   pull                     |   + EF Core 8         |
|  + Hardware HAL    |                            |   + JWT bearer        |
+--------------------+                            +-----------------------+
        |                                                   |
        v                                                   v
  Peripherals (cash drawer,                          Multi-tenant central
  printers, scanners,                                store (catalog,
  payment terminal,                                  config, transactions)
  customer display)
```

### Design Principles

- **Tenant isolation by query filter.** Every central entity inherits from `TenantScopedEntity` / `TenantRootEntity` / `GuidTenantScopedEntity`; `PosCentralDbContext` applies a global filter against `ICurrentTenantContext.CurrentTenantId`.
- **Append-only transactions.** Orders, payments, shifts, and inventory movements derive from `AppendOnlyEntity`. Corrections are new entries, not mutations.
- **Offline-first writes.** The desktop owns the source-of-truth for *in-flight* transactions until the API acknowledges them. Each event carries an idempotency key.
- **Versioned catalog/rules.** `Item` and `Order` carry `CatalogVersion` / `RuleVersion` so that price/tax outcomes are reproducible.

## 2. Projects

### 2.1 POS.Api

ASP.NET Core 8 Web API.

| Folder | Responsibility |
| --- | --- |
| `Controllers/` | Thin HTTP layer — see endpoint table below |
| `Application/` | Read services (`ICategoryReadService`, `ILocationReadService`, `IUnitOfMeasureReadService`, `IHealthStatusService`, `ITenantProfileReadService`) |
| `Contracts/` | Sealed-record DTOs returned to clients |
| `Data/` | `PosCentralDbContext`, entity configurations, migrations (`Data/Migrations/Central`) |
| `Services/` | Cross-cutting: `ICurrentTenantContext`, tenant profile composition |
| `Auth/` | JWT setup, `ApiClaimTypes`, policy handlers |
| `Configuration/` | Startup guards for connection strings / JWT options |
| `Sync/` | Placeholder for the inbound-ingest endpoints |

**Endpoints**

| Method | Route | Policy |
| --- | --- | --- |
| GET | `/api/health` | (anonymous) |
| GET | `/api/categories` | `UserOrAdmin` |
| GET | `/api/locations` | `UserOrAdmin` |
| GET | `/api/unitofmeasure` | `UserOrAdmin` |
| GET | `/api/tenant/profile` | `UserOrAdmin` |

**Authentication** (`Program.cs:34-50`)

- JWT bearer, HS256, configured from `appsettings.json`.
- Three authorization policies:
  - `UserOrAdmin` — `client_type` claim ∈ {`user`,`admin`}
  - `PosDevice` — `client_type` = `device` (used by the desktop terminal)
  - `SystemScope` — `system_scope` = `true` (cross-tenant maintenance)
- Custom claim names live in `Auth/ApiClaimTypes.cs` (`tenant_id`, `employee_id`, `terminal_id`, `location_id`, `device_id`, …). `CurrentTenantContext` reads these to scope every request.

### 2.2 POS.Desktop

WPF (`net8.0-windows`) terminal.

| Folder | Responsibility |
| --- | --- |
| `Data/` | `PosLocalDbContext` (SQLite), local-only entity configs, migrations (`Data/Migrations/Local`) |
| `Services/Provisioning/` | Terminal provisioning state machine (`IProvisionedTerminalContext`) |
| `Configuration/` | Startup guards for the local connection string |
| `Sync/` | (placeholder) outbound pusher / inbound puller |
| `Shell/`, `Blazor/` | (placeholder) UI shell and Blazor-hosted views |
| `App.xaml.cs` | Composition root — DI container, EF Core wiring |

The `pos_local_designtime.db` SQLite file is used for design-time EF migrations and ad-hoc inspection — runtime DB path is taken from config.

### 2.3 POS.Desktop.Hardware

Class library with six folders, one per peripheral family — `CashDrawer`, `CustomerDisplay`, `PaymentTerminal`, `Printers`, `Scanner`, `Gateway`. Implementations are not yet present; the intent is a vendor-agnostic interface set behind `Gateway`.

### 2.4 POS.Shared

Domain model and contracts. Shared by both API and desktop to avoid a DTO/entity drift.

- **Base classes** (`Domain/Base/`):
  - `TenantScopedEntity` (int id) — most master entities
  - `TenantRootEntity` — `Company` (tenant root)
  - `GuidTenantScopedEntity` — `Customer`, `Shift`
  - `OfflineCreatedEntity` — Guid id + `IdempotencyKey` + `CorrelationId` for client-originated rows
  - `AppendOnlyEntity` — immutable event entities (Order, Payment, etc.)
  - `LocalOperationalEntity` — local-only operational queues
- **Entities** (`Domain/Entities/Central/`): see §3.
- **Enums** (`Enums/`): 30+ status/type enums covering order, payment, shift, inventory, sync, retention, recovery, terminal provisioning.
- **Contracts** (`Contracts/`): `ICurrentTenantContext`, `IProvisionedTerminalContext`.

### 2.5 POS.Tests

xUnit 2.9.3 + `Microsoft.AspNetCore.Mvc.Testing`. Integration tests spin up the API via `ApiWebApplicationFactory`, swap in `TestAuthenticationHandler`, and seed data via `ApiTestDataSeeder`. Coverage focuses on tenant isolation, authorization policies, and ensuring secrets (connection strings, JWT keys) never leak into responses.

## 3. Data Model

### 3.1 Central database (`PosCentralDbContext`, SQL Server)

30+ `DbSet`s grouped by concern:

- **Tenant & org:** `Company`, `Location`, `Terminal`
- **People:** `Employee`, `EmployeeLocationRole`, `Customer`, `TerminalSession`
- **Catalog:** `Category`, `Item`, `ItemVariant`, `ItemIdentifier`, `ItemPrice`, `ItemStock`, `UnitOfMeasure`, `PriceList`
- **Tax & tender config:** `TaxRule`, `ReasonCode`, `TenderMethod`, `ReceiptTemplate`
- **Transactions:** `Order` → `OrderLine`, `Payment`, `Shift`, `CashDrawerMovement`, `ZReport`
- **Operations:** `InventoryMovement`, `ManagerAction`, `SyncIngestAck`

Cross-cutting properties:

- All tenant-scoped entities carry `TenantId`, `IsActive`, `CreatedBy/On`, `UpdatedBy/On`.
- Branding/media fields are lightweight external references only: `Company.LogoUrl`, `Category.ImageUrl`, and `Item.ImageUrl` store nullable image URLs, not binary media.
- `Item` supports item-level branding metadata (`BrandName`, `ManufacturerName`), while `ItemVariant` carries variant-specific physical metadata (`SizeText`, `WeightValue`, `WeightUnitOfMeasureId`).
- `Item` has `RowVersion` (optimistic concurrency).
- `Item` and `Order` carry `CatalogVersion` / `RuleVersion`.
- Multi-tenant query filters are applied centrally in `PosCentralDbContext.OnModelCreating` so callers can never accidentally read across tenants.

Key relationships:

```
Company 1—* Location 1—* Terminal
Category 1—* Item 1—* ItemVariant
Item 1—* ItemIdentifier / ItemPrice / ItemStock / InventoryMovement
Shift 1—* Order 1—* OrderLine
Order 1—* Payment
Shift 1—* CashDrawerMovement
Shift 1—1 ZReport
```

### 3.2 Local database (`PosLocalDbContext`, SQLite)

Six local-only `DbSet`s, all `LocalOperationalEntity`:

| Entity | Role |
| --- | --- |
| `SyncOutbox` | Append-only outbound event queue (EventType, EventId, PayloadJson, IdempotencyKey, CorrelationId, Status, AttemptCount, LastAttemptOn, LastErrorCode, ChunkSequence) |
| `SyncCursor` | Per-stream inbound progress (StreamName, LastPullToken, LastSuccessfulPullOn, ServerBackoffUntil, Status) |
| `PrintQueue` | Local print jobs awaiting hardware dispatch |
| `LocalRecoveryJournal` | Crash-recovery metadata (`RecoveryType`, `RequiredRecoveryAction`) |
| `PaymentReconciliationQueue` | Payments awaiting settlement reconciliation |
| `LocalRetentionState` | Local retention/cleanup state (`LocalRetentionStatus`) |

Local rows are scoped by `CurrentTenantId` plus location/terminal where relevant, so a re-provisioned terminal cannot read another tenant's queued data.

## 4. Sync Protocol

The desktop never makes live writes to central transaction tables. Instead it follows an **outbox/cursor** model.

### 4.1 Outbound (terminal → server)

1. Business operation (`OrderCreated`, `PaymentCaptured`, `ShiftClosed`, …) is executed against the local DB inside a transaction that also inserts a `SyncOutbox` row.
2. The sync engine polls `SyncOutbox` where `Status = Pending`, batches by `CorrelationId`, and posts to the API's ingest endpoint with the `IdempotencyKey`.
3. The API persists the event, records an `SyncIngestAck`, and returns ack metadata. The local row is moved to `Acked`/`Completed`.
4. On failure, `AttemptCount` and `LastErrorCode` are incremented; retries are throttled. Chunk sequencing allows resuming partial batches.

### 4.2 Inbound (server → terminal)

1. The terminal advances a `SyncCursor` per logical stream (e.g. `catalog.items`, `config.taxrules`).
2. It calls the API with `LastPullToken`; the API returns a delta plus a new token.
3. `LastSuccessfulPullOn` and `Status` are updated. If the API signals backpressure, `ServerBackoffUntil` is set and pulls are paused.

### 4.3 Guarantees

- **At-least-once delivery** outbound, made effectively exactly-once by `IdempotencyKey` deduplication server-side.
- **Reproducible pricing/tax** via `CatalogVersion` / `RuleVersion` recorded on each `Order`.
- **Tenant safety** — every event carries `TenantId`, validated against the JWT claim on ingest.

## 5. Cross-Cutting Concerns

- **DI composition.** API in `Program.cs`; desktop in `App.xaml.cs:ConfigureServices`. Both register their DbContext, `ICurrentTenantContext`, and read services as scoped.
- **Configuration guards.** Both projects fail-fast at startup if connection strings or JWT options are missing (`POS.Api/Configuration`, `POS.Desktop/Configuration`).
- **Recovery.** `LocalRecoveryJournal` records crash points; `RequiredRecoveryAction` drives the next desktop launch.
- **Terminal session history.** `TerminalSession` records cashier login/logout history per terminal and shift, with controlled session-close updates.
- **Manager action history.** `ManagerAction` is append-only history for overrides and approvals such as checkout approval, manager-authorized item actions, and void/remove actions.
- **Cash lifecycle.** Drawer-local cash remains in `CashDrawerMovement`, while post-drawer cash uses `CashAccount` and append-only `CashAccountMovement`. `CashAccount` unifies `Vault` and `Bank` destinations under one account model, and balances are derived from ledger movements rather than stored as current-balance snapshots.

## 6. Roadmap / Not-Yet-Implemented

- `POS.Desktop.Hardware` — concrete device drivers behind the existing folder structure.
- `POS.Desktop/Sync` and `POS.Api/Sync` — the actual pusher/puller/ingest controllers (entities exist, transport does not).
- `POS.Desktop/Shell` + `POS.Desktop/Blazor` — UI shell and Blazor-hosted screens.
- `POS.Shared/ValueObjects` and `POS.Shared/Rules` — DDD enrichment (currently empty).
- Write-side controllers on the API beyond the current read endpoints.

## 7. File Map (Quick Reference)

- API auth & DI: [Program.cs](POS.Api/Program.cs)
- Central DbContext: [PosCentralDbContext.cs](POS.Api/Data/PosCentralDbContext.cs)
- Local DbContext: [PosLocalDbContext.cs](POS.Desktop/Data/PosLocalDbContext.cs)
- Domain base types: [POS.Shared/Domain/Base](POS.Shared/Domain/Base)
- Central entities: [POS.Shared/Domain/Entities/Central](POS.Shared/Domain/Entities/Central)
- Enums: [POS.Shared/Enums](POS.Shared/Enums)
- Claim types: [ApiClaimTypes.cs](POS.Api/Auth/ApiClaimTypes.cs)
- Test factory: [ApiWebApplicationFactory.cs](POS.Tests/Integration/ApiWebApplicationFactory.cs)
