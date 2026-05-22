# architecture.md — Enterprise SaaS POS: Authoritative Architecture

## 1. Stack and Topology
N-tier distributed, offline-first.

- **Central API**: ASP.NET Core 8 Web API. Hosts authoritative business logic, tenant configuration, central reporting, sync ingestion. RESTful + JWT auth. HTTP/2 preferred for sync transport.
- **Central Database**: Microsoft SQL Server. System of record. Multi-tenant. Append-only for orders, payments, audit, inventory movements.
- **POS Client**: WPF host (.NET 8) embedding `BlazorWebView` (WebView2) for the UI. Native WPF code owns hardware drivers and the security boundary.
- **Local Store**: SQLite encrypted via SQLCipher, inside the WPF process, accessed through EF Core. Bounded operational cache + durable pending-sync journal. NOT an archival store.
- **Shared Domain Package**: `POS.Shared` referenced by both API and Desktop. Contains entities, contracts, and deterministic business rules (pricing, tax, discount, return rules, tender validation, manager-override evaluation). Versioned. Both runtimes execute the same rule snapshot.

## 2. Solution Layout
- `POS.Shared` — domain entities, value objects, rule engine, DTOs, idempotency primitives. No infrastructure.
- `POS.Api` — ASP.NET Core 8, EF Core with SQL Server provider, sync ingestion, tenant admin.
- `POS.Desktop` — WPF host, BlazorWebView, hardware gateway, EF Core with SQLite (SQLCipher) provider, outbox, sync orchestrator.
- `POS.Desktop.Hardware` — native driver adapters (printer, drawer, scanner, payment terminal, scale, customer display). No Blazor, no EF Core references.

## 3. Hardware Boundary: `IPosHardwareGateway`

### 3.1 Rule
Blazor MUST NOT touch drivers. Blazor injects high-level application services (`ICheckoutService`, `IShiftService`, etc.). Those services call `IPosHardwareGateway`, which is WPF-owned and lives in `POS.Desktop` DI.

### 3.2 Contract Surface (conceptual)
- `PrintReceiptAsync(PrintReceiptCommand, CT)` → `HardwareCommandResult`
- `OpenCashDrawerAsync(OpenCashDrawerCommand, CT)` → `HardwareCommandResult`
- `PaymentTerminalAuthorizeAsync(...)`, `ScalePollAsync(...)`, `CustomerDisplayUpdateAsync(...)`
- `GetHealthAsync(CT)` → `DeviceHealthSnapshot`

Every command carries: `IdempotencyKey`, `LocalOrderId`, `TerminalId`, `OperatorId`, optional `ReasonCode`, optional `AuthorizedByManagerId`.

### 3.3 Per-Device Command Queue
Each physical device has a single serialized command channel. Two rapid UI taps cannot produce two drawer kicks or two prints. The gateway dedupes on `IdempotencyKey` before dispatching to the driver.

### 3.4 Application-Service Responsibilities (before calling the gateway)
Validate: transaction state, operator role, tenant/store/terminal scope, manager override status if required, idempotency. Only then dispatch.

### 3.5 JavaScript Interop Boundary
- WebView navigation is locked to local packaged app content. No remote URL ever reaches the native bridge.
- If `window.chrome.webview.postMessage` is used, messages MUST be allowlisted by type, schema-validated, tied to the active terminal session, and rejected if origin is not local.
- High-frequency hardware events DO NOT flow through JSInterop. They flow through C# DI (see §4).
- JSInterop is reserved for DOM-level concerns: scanner wedge focus, file pickers, payment terminal redirect callbacks.

## 4. Hardware State Aggregator and Thread Marshalling

### 4.1 The Process and Thread Model
BlazorWebView runs the WPF dispatcher, the Blazor renderer synchronization context, and hardware threads (often COM STA) in the .NET host process. The DOM renders in a separate `msedgewebview2.exe` over IPC. Driver callbacks arrive on non-UI threads. Mutating Blazor state from a non-renderer thread is a defect.

### 4.2 Aggregator Design
- A hosted background service consumes a bounded `System.Threading.Channels` channel.
- Driver events publish into the channel; the aggregator normalizes them into immutable status snapshots (one per device class) with a monotonic version number.
- `IHardwareStatusService` is a Blazor-side scoped service exposing snapshots + change notifications.
- All Blazor state mutations are scheduled via the renderer dispatcher (`ComponentBase.InvokeAsync` → `StateHasChanged`).

### 4.3 Coalescing and Backpressure
- Default coalescing window per device: 250 ms. Snapshots update at most 4×/sec.
- High-frequency feeds (scale weight, EMV step) are exposed through a separate observable. Only components that need them subscribe, and they render in isolated regions so parent layouts do not invalidate.

### 4.4 Persistent Hardware Strip
The shell hosts a persistent header strip bound to the aggregator. Modals NEVER suppress hardware alerts. Failures during an active workflow surface as non-blocking inline banners.

### 4.5 Shutdown
The gateway owns a clean shutdown path: drain command queue → complete or roll back in-flight commands → release native handles (especially COM STA apartments) → signal Blazor teardown. Crash recovery (§9) assumes the previous run did not finish cleanly.

### 4.6 Customer-Facing Display
Cart state lives in a process-singleton scoped service, NOT in a Blazor page component, so a second `BlazorWebView` window (customer display) can subscribe to the same state.

## 5. Terminal Provisioning Model

### 5.1 Single-Store, Immutable Until Re-Provisioned
Each terminal is provisioned to exactly one `(TenantId, StoreId)` pair for its operational lifetime. Roaming terminals are NOT supported.

### 5.2 Provisioning Workflow
- Administrative action only. Cashier/store-manager cannot trigger it.
- Issues a device certificate / device secret bound to `(TenantId, StoreId, TerminalId)`.
- Generates the SQLCipher key, sealed under DPAPI machine scope.
- Pulls initial catalog, price lists, tax rules, manager credentials.

### 5.3 Re-Provisioning
Moving a terminal between stores or tenants requires re-provisioning, which:
- Drains the outbox if possible.
- Wipes the SQLite file.
- Rotates device credentials and the SQLCipher key.
- Re-pulls catalog under the new `(TenantId, StoreId)`.

### 5.4 Operator Login Across Stores
Users may belong to multiple stores within a tenant; the terminal still belongs to one store. Operators logging into a terminal from a store other than the terminal's provisioned store are rejected at the local credential check AND at the API.

### 5.5 Remote Deprovision
Central can issue a deprovision command (tenant cancellation, hardware retirement). Terminal honors before any further sale: drain outbox, securely delete SQLite + DPAPI secret, refuse to operate until re-provisioned.

## 6. SQLite: Bounded Operational Store

### 6.1 What SQLite Holds
- Current catalog, price lists, tax rules, discounts, manager credentials (offline use).
- Open / current business-day transactions.
- Recently completed transactions (for reprint, return, recovery).
- Pending outbox (orders, inventory movements, audit, payment state, print jobs).
- Short-term local audit until central acks.

### 6.2 What SQLite Is NOT
Not the long-term audit store. Not the reporting store. Not an archive.

### 6.3 Encryption
- SQLite file encrypted via **SQLCipher**.
- SQLCipher key derived from a per-terminal secret stored via Windows DPAPI in **machine scope** (POS runs under a service account, not the cashier's interactive account).
- BitLocker on the OS drive is a deployment prerequisite. The installer refuses to provision a terminal without it.

### 6.4 PII Handling
Customer name and phone retained only for in-flight transaction needs. NOT replicated into every audit payload. Pruned aggressively. Excluded from any local search index that survives logout.

### 6.5 Required Local Columns on Sync-Controlled Tables
`TenantId`, `StoreId`, `TerminalId`, `LocalId` (GUID/ULID or `(TerminalId, DeviceSequence)`), `ServerId` (when acked), `SyncStatus`, `SyncedOn`, `ServerCommitVersion` / `ServerAckId`, `RetentionUntil`, `RetryCount`, `LastErrorCode`.

### 6.6 Retention Policy

| Local Data | Keep Until | Notes |
|---|---|---|
| Pending outbox | Server ack | Never delete while Pending / InFlight / recoverable Failed |
| Acknowledged outbox | Ack + 7–14 days | Purge after window |
| Completed print jobs | 7–14 days after success | Or keep metadata only |
| Failed print jobs | Manual resolution OR successful reprint | Then retention prune |
| Local orders / lines | 30–90 days, retailer-configurable | Canonical history is central |
| Local audit | Ack + 30–90 days or size cap | Central upload first |
| Catalog snapshots | Current + previous version | Until all transactions referencing previous have synced |
| Product images | Versioned cache, size capped | LRU eviction |
| Sync attempt logs | 14–30 days | Summaries only after that |

### 6.7 Maintenance Job
Runs only when terminal is idle or after shift close. Deletes only synced records past retention. Never deletes pending orders, pending audit, unresolved payments, unresolved print jobs, or failed sync records. WAL mode + periodic checkpoints + incremental vacuum. Tracks SQLite file size; surfaces warnings before the terminal becomes unhealthy.

### 6.8 Mid-Shift Catalog Versioning
Local orders carry `CatalogVersion` and `PriceListVersion` at the moment they were opened. Resumed transactions continue under those versions. New transactions started after activation use the new version. Local cache retains the previous version until all transactions referencing it have completed AND synced.

## 7. Identifiers and Concurrency Strategy

### 7.1 IDs
- Central master / admin tables: `int` identity is permitted internally.
- Tenant-scoped entities created on a terminal (orders, order lines, payments, tenders, inventory movements, audit events, print jobs, outbox envelopes): **GUID or ULID** generated on the terminal. Integer-only IDs for offline-created records are PROHIBITED.
- Receipt numbers: composite `(StoreId, TerminalId, BusinessDate, Sequence)` — unique, auditable, human-readable.

### 7.2 RowVersion Usage
USE `RowVersion` on:
- Product / catalog master data updates.
- Price-list versioning.
- Central `ProductStock` snapshot updates inside SQL Server.
- Admin screens (products, categories, tax rules, promotions).

DO NOT use `RowVersion` to:
- Reject offline sales.
- Resolve conflicts on `Order`, `OrderLine`, `Payment`, `Tender`, `InventoryMovement`, `AuditEvent`.

### 7.3 Append-Only Transactional Model
Orders, payments, audit, inventory movements are append-only. Voids and refunds are NEW events referencing the original by ID. No record overwrite on sync. No "last-write-wins" for financial, inventory, or audit data.

### 7.4 Central Inventory Ledger
- `Product` — catalog master.
- `ProductStock` — server-maintained snapshot per `(TenantId, StoreId, ProductId)`. Derived from movements.
- `InventoryMovement` — append-only ledger: `MovementId, TenantId, StoreId, TerminalId, ProductId, QuantityDelta, MovementType, SourceOrderId, BusinessDate, DeviceSequence, IdempotencyKey, CreatedBy, CreatedOn`.

### 7.5 Negative Stock Handling
A sale is never silently dropped because synced stock went negative. It is committed and flagged as an inventory exception for investigation. High-control / serialized items may opt into stricter strategies (online-only authorization, per-terminal allocation budgets, manager-PIN override at zero/negative local stock).

## 8. Sync Network Contract

### 8.1 Envelope
- Each sync request is a chunk: a set of completed business events.
- Chunk size cap: **min(100 orders OR 1,000 events, 1 MB gzip-compressed)**.
- Chunk idempotency key: derived from `(TerminalId, ChunkSequence)`. Each event also carries its own per-event idempotency key.
- Internal ordering: `(BusinessDate, TerminalSequence)`.
- Compression: **gzip by default**. No uncompressed sync path ships.
- Transport: **HTTP/2** preferred. Multiplexing drops to 1 on marginal networks to avoid head-of-line blocking.
- Tenant / store / terminal scope is taken from the authenticated device principal, NEVER from the request body.

### 8.2 Required Envelope Fields
`TenantId` (from device cert), `StoreId`, `TerminalId`, `CashierId`, `BusinessDate`, `TerminalSequenceRange`, `ChunkIdempotencyKey`, `CatalogVersion`, `PriceListVersion`, `RuleVersion`, `CorrelationId`.

### 8.3 Server Chunk Handler (Atomic)
Runs inside a single DB transaction:
1. Authenticate device and validate Tenant→Store→Terminal scope. Reject cross-tenant or cross-store payloads.
2. Look up `ChunkIdempotencyKey`. If found, return original ack (do nothing else).
3. Deduplicate per-event idempotency keys.
4. Insert immutable order / audit / payment records.
5. Apply inventory movements; flag exceptions (e.g. negative stock).
6. Write the ack record.
7. Commit. Respond 200 with server IDs / ack ID.

All-or-nothing per chunk. Terminal never reasons about half-applied chunks.

### 8.4 Acknowledgement Retention
Server persists chunk acks for **minimum 30 days** (max supported offline window + margin). A chunk arriving after ack-archival is rejected with a **distinct error code** (`SyncAckExpired`) that escalates to a human; the terminal does NOT silently retry.

### 8.5 Catalog Pull (Separate Transport)
Server returns a paginated change feed keyed by a change token. Terminal acks receipt and persists the token. Catalog pull and order push run independently and MUST NOT block each other.

### 8.6 Sequencing on Terminal
Terminal does not send chunk N+1 until chunk N is acknowledged. Outbox walked with a streaming cursor + LIMIT. Never `SELECT *` the backlog.

### 8.7 Throttling
- During business hours: 1 chunk every 2 seconds (configurable).
- Outside business hours or under explicit manager "catch-up" command: rate raised.

### 8.8 Thundering Herd Mitigation
- Terminal applies a **jittered initial sync delay** on first post-online sync, drawn from a window proportional to chain size (default 0–30 seconds).
- Terminal honors server-supplied `Retry-After` headers.
- API enforces per-tenant AND per-terminal rate limits. Excess returns **HTTP 429 + `Retry-After`**.
- Cashier UI MUST NOT see 429s. Terminal absorbs them as normal flow control. UI surfaces only persistent failure beyond a threshold (default: 30 minutes of continuous failure).

### 8.9 Time and Ordering
- Terminal assigns `BusinessDate` from its local calendar + shift configuration. Server preserves it.
- Wall-clock timestamps recorded on terminal; server stamps `ReceivedOn`.
- Reports use `(BusinessDate, TerminalId, TerminalSequence)` for ordering, NEVER wall-clock.
- NTP is a deployment requirement on every terminal. The installer verifies.

### 8.10 Telemetry (First-Class)
- Terminal reports: chunks attempted, chunks acked, RTT, last successful sync, current outbox depth.
- API exposes the same metrics aggregated per tenant and per terminal.
- Monotonically growing outbox over 48h triggers a central support alert before the store manager calls.

## 9. Crash Recovery
On startup the terminal scans local DB for orders in non-terminal states and the first operator logging in completes a recovery workflow before normal checkout becomes available. See `context.md` §9 for the full state machine.

## 10. Authentication, JWT, and Offline Credentials
- API: JWT bearer with refresh-token rotation, sliding window (reissue at <50% remaining life).
- JWT at rest on terminal: encrypted via **DPAPI machine scope**.
- Offline operator / manager login: salted, slow-hashed PIN (Argon2id or PBKDF2 with sufficient iterations) stored locally, synced from server. Verification is a native desktop service — NOT JavaScript. Brute-force protections: attempt limits, lockout, audit logging.
- After a long offline window (e.g. 24h), require manager PIN before any high-value override.
- Card payments: online authorization required unless the payment terminal / acquirer SDK explicitly supports approved offline EMV limits. "Queue card charges and capture later" is FORBIDDEN. Unknown card state → reconciliation, not retry. Local storage: tokens / references only — NEVER PAN.

## 11. Desktop Update and Schema Migration
The updater MUST:
- Drain the outbox before applying schema migrations.
- Run migrations transactionally.
- Roll back atomically to the previous version if the new schema fails post-migration smoke checks.

Server migrations follow expand → migrate → contract phases so older terminal versions keep syncing during rollout.

## 12. Observability
- Correlation ID per business event, generated on the terminal at event creation.
- Flows: cashier action → local outbox → sync envelope → server ingestion → reporting artifacts.
- Persisted on every server-side artifact derived from the event.

## 13. Disaster Recovery
Central SQL Server is the system of record. Required before go-live:
- Backup cadence (defined).
- Point-in-time recovery target (defined).
- Restore drill cadence (defined).
- Runbook for single-tenant restore from backup without affecting other tenants.

## 14. Multi-Tenant Isolation: Four Required Controls
1. Tenant identity derived from authenticated principal or provisioned device certificate. Never from request body.
2. EF Core global query filter on `TenantId` for every tenant-scoped entity.
3. Tenant-aware composite keys and unique indexes (`(TenantId, SKU)`, etc.); FKs preserve tenant scope.
4. SQL Server Row-Level Security recommended as defense in depth when all tenants share one database.

Query filters alone are NOT isolation. They are one control of four.
