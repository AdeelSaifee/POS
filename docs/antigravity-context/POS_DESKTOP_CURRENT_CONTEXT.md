# POS Desktop UI Integration - Current Session Context

## Current Milestone & Group
- **Milestone**: Phase 6 / Milestone 6.3 - Outbox drain processor
- **Group**: Group 5 (Task 6.3.10 - completed) — Milestone 6.3 100% COMPLETE

## Status of All Milestone 6.3 Tasks (ALL COMPLETE)
- `[x]` Task 6.3.1 - Define the SyncProcessor (Completed)
- `[x]` Task 6.3.2 - Register as a hosted service (Completed)
- `[x]` Task 6.3.3 - Batch unsent outbox rows (Completed)
- `[x]` Task 6.3.4 - Post the batch (Completed)
- `[x]` Task 6.3.5 - Mark rows sent on success (Completed)
- `[x]` Task 6.3.6 - Advance the cursor (Completed)
- `[x]` Task 6.3.7 - Run off the UI thread (Completed)
- `[x]` Task 6.3.8 - Tune batch size/interval (Completed)
- `[x]` Task 6.3.9 - Pause cleanly on shutdown (Completed)
- `[x]` Task 6.3.10 - Test events reach central (Completed)

## Status of All Milestone 6.2 Tasks (Group 4 COMPLETE - 100% COMPLETE)
- `[x]` Task 6.2.1 - Add API base URL config
- `[x]` Task 6.2.2 - Define the sync client interface
- `[x]` Task 6.2.3 - Implement the client
- `[x]` Task 6.2.4 - Acquire a device token
- `[x]` Task 6.2.5 - Implement token refresh
- `[x]` Task 6.2.6 - Register the typed HttpClient (Completed)
- `[x]` Task 6.2.7 - Map failures to typed results (Completed)
- `[x]` Task 6.2.8 - Handle timeouts/skew (Completed)
- `[x]` Task 6.2.9 - Smoke test ingest call (Completed)
- `[x]` Task 6.2.10 - Ensure no UI-thread blocking (Completed)

## Status of All Milestone 6.1 Tasks (100% COMPLETE)
- `[x]` Task 6.1.1 - Design the ingest contract (Shared contract records in POS.Shared)
- `[x]` Task 6.1.2 - Create the Sync structures (API sync service scaffolding in POS.Api)
- `[x]` Task 6.1.3 - Add the ingest endpoint (POST api/sync/ingest implemented in SyncController)
- `[x]` Task 6.1.4 - Apply the PosDevice policy (PosDevice authorization mapping enforced)
- `[x]` Task 6.1.5 - Implement idempotent persist + dedupe (Persist inside SyncIngestAck with custom identity validations)
- `[x]` Task 6.1.6 - Ack via SyncIngestAck (Durable response and event-level Received acknowledgement)
- `[x]` Task 6.1.7 - Reject unauthorized callers (Confirm policy enforcement and reject invalid tokens)
- `[x]` Task 6.1.8 - Handle duplicate event IDs (Secure replay safe matching and duplicate sequences blocking)
- `[x]` Task 6.1.9 - Add an API integration test (Comprehensive 12-scenario integration test suite added)
- `[x]` Task 6.1.10 - Document the endpoint contract (Authoritative endpoint API contract documentation added)


## Status of All Milestone 5.5 Tasks (Current)
- `[x]` Task 5.5.1 - Define ICashControlService
- `[x]` Task 5.5.2 - Write CashDrawerMovement / local cash drawer movement persistence
- `[x]` Task 5.5.3 - Attach reason codes
- `[x]` Task 5.5.4 - Enforce manager PIN
- `[x]` Task 5.5.5 - Compute drawer balance
- `[x]` Task 5.5.6 - Compute threshold alerts
- `[x]` Task 5.5.7 - Add handlers + ledger query
- `[x]` Task 5.5.8 - Wire cash_control.html + remove pos_safe_drops
- `[x]` Task 5.5.9 - Tie movements to active shift
- `[x]` Task 5.5.10 - Test drops/injections + alerts

## Status of All Milestone 5.4 Tasks
- `[x]` Task 5.4.1 - Define IPaymentService (Tender, change, completion contract)
- `[x]` Task 5.4.2 - Record Payment per TenderMethod (Persist tenders cash/card/wallet/split)
- `[x]` Task 5.4.3 - Compute cash change (Change = tendered - due, MoneyRounder-policy)
- `[x]` Task 5.4.4 - Commit order append-only (Atomic order/lines/payments save with SQLite transaction)
- `[x]` Task 5.4.5 - Enqueue SyncOutbox event (Pending outbox row inside same order transaction)
- `[x]` Task 5.4.6 - Enqueue PrintQueue receipt (Pending receipt print job inside same order transaction)
- `[x]` Task 5.4.7 - Render receipt from data (Fully data-driven plain text receipt rendering service)
- `[x]` Task 5.4.8 - Ensure idempotent completion
- `[x]` Task 5.4.9 - Wire payment_screen.html
- `[x]` Task 5.4.10 - Unit test tender/change/completion

## Status of All Milestone 5.3 Tasks
- `[x]` Task 5.3.1 - Define IOrderService
- `[x]` Task 5.3.2 - Decide draft persistence
- `[x]` Task 5.3.3 - Implement add/qty/remove
- `[x]` Task 5.3.4 - Implement discount handling
- `[x]` Task 5.3.5 - Implement totals calculation
- `[x]` Task 5.3.6 - Implement tax via TaxRule
- `[x]` Task 5.3.7 - Centralize money rounding
- `[x]` Task 5.3.8 - Add cart bridge handlers
- `[x]` Task 5.3.9 - Wire main_checkout cart + remove pos_cart
- `[x]` Task 5.3.10 - Unit test cart math + tax / final cart math, bridge, UI, and regression verification

## Status of All Milestone 5.2 Tasks
- `[x]` Task 5.2.1 - LocalShift entity + EF migration
- `[x]` Task 5.2.2 - IShiftService + ShiftService (OpenShiftAsync)
- `[x]` Task 5.2.3 - shift.open bridge handler + tests
- `[x]` Task 5.2.4 - shift_open.html float input + validation UI
- `[x]` Task 5.2.5 - shift_open.html bridge wire-up
- `[x]` Task 5.2.6 - shift.getCurrent bridge handler + ShiftDetailsResult
- `[x]` Task 5.2.7 - Gate all operational screens on open shift (shift.getCurrent)
- `[x]` Task 5.2.8 - Source checklist/policy from config (config-driven limits + checklist via `shift.getOpenPolicy` bridge endpoint)
- `[x]` Task 5.2.9 - Navigate to checkout on open (success overlay + 1600ms redirect to `main_checkout.html` confirmed and preserved)
- `[x]` Task 5.2.10 - End-to-end verification: full builds, full test suite, search checks, SHA-256 sync checks, bug fix for stale docs copy

## Files Created/Changed in this Milestone

### Phase 6 / Milestone 6.3 - Group 5 (Task 6.3.10 completed)
- [ADD] `POS.Desktop.Tests/Services/Sync/SyncProcessorPipelineIntegrationTests.cs` (2 SyncProcessor-driven integration tests proving the complete desktop push pipeline end-to-end using real SQLite, real EF reader/builder/ack applier, fake capturing ISyncIngestClient, and TaskCompletionSource synchronization)

### Phase 6 / Milestone 6.3 - Group 4 (Tasks 6.3.5 and 6.3.6 completed)
- [ADD] `POS.Desktop/Services/Sync/SyncAckApplyResult.cs` (Outcome carrier representing db mutations result)
- [ADD] `POS.Desktop/Services/Sync/ISyncAckApplier.cs` (Durable db mutation scoped service contract)
- [ADD] `POS.Desktop/Services/Sync/EfSyncAckApplier.cs` (Durable SQLite transaction and monotonic cursor implementation)
- [MODIFY] `POS.Desktop/Configuration/DesktopHostBuilder.cs` (Registered scoped business service ISyncAckApplier)
- [MODIFY] `POS.Desktop/Services/Sync/SyncProcessor.cs` (Durable DB ack updates resolved dynamically and in-memory guard updated only after database commit)
- [ADD] `POS.Desktop.Tests/Services/Sync/SyncAckApplierTests.cs` (17 targeted in-memory SQLite integration tests covering identity matches, count validations, monotonic cursors and transaction rollbacks)
- [MODIFY] `POS.Desktop.Tests/Services/Sync/SyncProcessorTests.cs` (Added mock tests for successful ingest, failed ack database updates, null responses, and client ingest failures)
- [MODIFY] `POS.Desktop.Tests/Services/Sync/SyncDiResolutionTests.cs` (Added assertions for resolving ISyncAckApplier to EfSyncAckApplier)

### Phase 6 / Milestone 6.3 - Group 3 (Task 6.3.4 completed)
- [ADD] `POS.Desktop/Services/Sync/ISyncIngestRequestBuilder.cs` (Decoupled, stateless pure interface defining outbox to ingest request mapper)
- [ADD] `POS.Desktop/Services/Sync/SyncIngestRequestBuilder.cs` (Pure, validated deterministic implementation generating stable sequences, idempotency keys, correlation IDs, and canonical request hashes)
- [MODIFY] `POS.Desktop/Configuration/DesktopHostBuilder.cs` (Registered ISyncIngestRequestBuilder as a Singleton service in dependency container)
- [MODIFY] `POS.Desktop/Services/Sync/SyncProcessor.cs` (Resolved builder, client, and batch reader dynamically; added in-memory process-local HashSet one-flight guard to prevent duplicate post loop)
- [ADD] `POS.Desktop.Tests/Services/Sync/SyncIngestRequestBuilderTests.cs` (Robust unit test suite covering empty batch exceptions, deterministic sequence/key/hash calculations, uniqueness constraints, and mixed tenant validations)
- [MODIFY] `POS.Desktop.Tests/Services/Sync/SyncProcessorTests.cs` (Added test cases verifying empty batch bypass, failed client retries, success logs, and one-flight guard duplicate blocks)
- [MODIFY] `POS.Desktop.Tests/Services/Sync/SyncDiResolutionTests.cs` (Verify DI container resolves ISyncIngestRequestBuilder as SyncIngestRequestBuilder)

### Phase 6 / Milestone 6.3 - Group 2 (Task 6.3.3 completed)
- [ADD] `POS.Desktop/Services/Sync/SyncOutboxBatch.cs` (Read-only record holding a read-only projection list of SyncOutbox items)
- [ADD] `POS.Desktop/Services/Sync/SyncOutboxBatchItem.cs` (Read-only projected DTO modeling SyncOutbox columns to protect tracking state)
- [ADD] `POS.Desktop/Services/Sync/ISyncOutboxBatchReader.cs` (Interface contract for querying pending outbox records)
- [ADD] `POS.Desktop/Services/Sync/EfSyncOutboxBatchReader.cs` (EF Core non-tracking implementation with index-optimized sequencing)
- [MODIFY] `POS.Desktop/Configuration/DesktopHostBuilder.cs` (Registered scoped business service ISyncOutboxBatchReader)
- [MODIFY] `POS.Desktop/Services/Sync/SyncProcessor.cs` (Injected IServiceScopeFactory, resolved scoped batch reader inside transient scopes, and logged pending counts)
- [ADD] `POS.Desktop.Tests/Services/Sync/SyncOutboxBatchReaderTests.cs` (Robust in-memory SQLite integration tests covering unprovisioned safety, status filters, ordering constraints, and batch sizing)
- [MODIFY] `POS.Desktop.Tests/Services/Sync/SyncProcessorTests.cs` (Updated worker unit tests to supply clean mock service scope factory and stubbed reader)
- [MODIFY] `POS.Desktop.Tests/Services/Sync/SyncDiResolutionTests.cs` (Verify hosted container resolves ISyncOutboxBatchReader as EfSyncOutboxBatchReader)

### Phase 6 / Milestone 6.3 - Group 1 (Tasks 6.3.1, 6.3.2, 6.3.7 - 6.3.9 completed)
- [ADD] `POS.Desktop/Services/Sync/SyncProcessorOptions.cs` (Configuration options for worker batch size and poll interval with validation)
- [ADD] `POS.Desktop/Services/Sync/SyncProcessor.cs` (Core background service outbox sync worker with yielding, unprovisioned gating, and safe cancellation)
- [MODIFY] `POS.Desktop/Configuration/DesktopHostBuilder.cs` (Registered options and added SyncProcessor as an IHostedService background worker)
- [MODIFY] `POS.Desktop/appsettings.json` (Added BatchSize and PollIntervalSeconds parameters inside the Sync config block)
- [ADD] `POS.Desktop.Tests/Services/Sync/SyncProcessorOptionsTests.cs` (Unit tests verifying option constraints and valid boundaries)
- [ADD] `POS.Desktop.Tests/Services/Sync/SyncProcessorTests.cs` (Lifecycle, unprovisioned safety gating, options validation shutdown, and cancellation tests for the hosted service)
- [MODIFY] `POS.Desktop.Tests/Services/Sync/SyncDiResolutionTests.cs` (Verify hosted service resolves SyncProcessor from container and configurations bind correctly)

### Phase 6 / Milestone 6.2 - Group 4 (Tasks 6.2.9 & 6.2.10 - completed)
- [ADD] `POS.Tests/IntegrationTests/SyncIngestSmokeTests.cs` (Direct API-side integration smoke test verifying that a valid POST request to /api/sync/ingest successfully authenticates via TestRequestAuthentication and is acknowledged as Received by the persistence engine)
- [ADD] `POS.Desktop.Tests/Services/Sync/SyncStaticAnalysisTests.cs` (Static async-safety checks to prevent thread-blocking C# code by asserting no files inside `POS.Desktop/Services/Sync/` contain `.Result`, `.Wait(`, or `GetAwaiter().GetResult()`)
- [VERIFY] `POS.Tests/POS.Tests.csproj` remains `net8.0` and references only `POS.Api` + `POS.Shared`; no `POS.Desktop` reference was added.

### Phase 6 / Milestone 6.2 - Group 3 (Tasks 6.2.6 to 6.2.8 - completed)
- [MODIFY] `POS.Desktop/Configuration/DesktopHostBuilder.cs` (Registered typed HttpClient `ISyncIngestClient` with safe, exception-free ApiBaseUrl Uri.TryCreate parsing and safely bounded timeout configuration, registered IDeviceTokenProvider as UnconfiguredDeviceTokenProvider, and bound configuration options)
- [MODIFY] `POS.Desktop/POS.Desktop.csproj` (Added package reference to Microsoft.Extensions.Http to enable IHttpClientFactory AddHttpClient extension)
- [ADD] `POS.Desktop/Services/Sync/UnconfiguredDeviceTokenProvider.cs` (Default unconfigured device token provider implementation to protect the client resolution boundary from container start-up failures)
- [ADD] `POS.Desktop.Tests/Services/Sync/SyncDiResolutionTests.cs` (Integration tests verifying container resolution, options binding, boundary safety checks, and unconfigured token fallback result mappings)

### Phase 6 / Milestone 6.2 - Group 2 (Tasks 6.2.3 to 6.2.5 - completed)
- [ADD] `POS.Desktop/Services/Sync/SyncIngestClient.cs` (Core sync ingest client implementation utilizing HttpClient to POST outbox batches to /api/sync/ingest, intercepting headers with Bearer tokens, mapping network timeouts and socket failures to SyncIngestClientResult DTOs)
- [ADD] `POS.Desktop/Services/Sync/FixedDeviceTokenProvider.cs` (A safe, in-memory IDeviceTokenProvider implementation for testing and development environments that does not store credentials, read configurations, generate JWTs, or access private keys)
- [MODIFY] `POS.Desktop/Services/Sync/IDeviceTokenProvider.cs` (Refined contract adding ForceRefreshAsync and optional ExpiresAtUtc to support transparent expiration checks and refresh stubs)
- [ADD] `POS.Desktop.Tests/Services/Sync/DeviceTokenProviderTests.cs` (Focused tests validating fixed token validity, expiration, in-memory refresh stubs, and blank token handling)
- [ADD] `POS.Desktop.Tests/Services/Sync/SyncIngestClientTests.cs` (Comprehensive client tests using FakeHttpMessageHandler verifying HTTP POST routing, authorization headers, offline connectivity mapping, timeout/cancellation errors, and deserialization failures)

### Phase 6 / Milestone 6.2 - Group 1 (Tasks 6.2.1 & 6.2.2 - completed)
- [ADD] `POS.Desktop/Services/Sync/SyncClientOptions.cs` (Configuration options model with built-in validation helpers for absolute URI, leading slash on IngestPath, positive TimeoutSeconds, and positive ClockSkewSeconds)
- [ADD] `POS.Desktop/Services/Sync/ISyncIngestClient.cs` (Typed client contract using POS.Shared sync DTOs for non-blocking outbox ingest execution)
- [ADD] `POS.Desktop/Services/Sync/SyncIngestClientError.cs` (Safe error model and enum covering Configuration, Offline, Timeout, Unauthorized, Forbidden, Conflict, Validation, ServerError, and Unexpected categories)
- [ADD] `POS.Desktop/Services/Sync/SyncIngestClientResult.cs` (Structured outcome carrier with success state, SyncIngestResponse, and structured SyncIngestClientError; provides Succeeded and Failed factory methods)
- [ADD] `POS.Desktop/Services/Sync/IDeviceTokenProvider.cs` (Optional abstract token provider interface and DeviceTokenResult record for decoupled JWT acquisition planning)
- [ADD] `POS.Desktop.Tests/Services/Sync/SyncClientOptionsTests.cs` (Unit test suite with 22 validation scenarios verifying configuration edge cases, outcome factories, and error model safety bounds)
- [MODIFY] `POS.Desktop/appsettings.json` (Added configuration parameters block for "Sync" specifying base URL, relative ingest route, 15-second timeout, and 300-second clock skew)

### Phase 6 / Milestone 6.1 - Group 5 (Task 6.1.10 - completed)
- [ADD] `docs/sync/SYNC_INGEST_ENDPOINT.md` (Authoritative sync ingest endpoint contract documentation detailing POST /api/sync/ingest routing, JWT authorization policies, identity mismatch constraints, request/response models, safe replays equivalence verification, batch validations, and deferred transformation logic)

### Phase 6 / Milestone 6.1 - Group 4 (Tasks 6.1.7 & 6.1.9 - completed)
- [ADD] `POS.Tests/IntegrationTests/SyncIngestEndpointTests.cs` (Comprehensive 12-scenario integration test suite verifying dynamic authentication, mismatch validation, persistence, replay, same-batch duplicate event validation, and conflict mapping)

### Phase 6 / Milestone 6.1 - Group 3 (Tasks 6.1.5, 6.1.6 & 6.1.8 - completed)
- [ADD] `POS.Api/Application/Sync/SyncConflictException.cs` (Exception thrown centrally on key/sequence conflicts)
- [MODIFY] `POS.Api/Application/Sync/SyncIngestService.cs` (Implemented lookups, validations, idempotency logic, duplicate safe recovery, and SaveChanges context persistence)
- [MODIFY] `POS.Api/Controllers/SyncController.cs` (Updated to handle SyncConflictException and return 409 Conflict with descriptive ProblemDetails)

### Phase 6 / Milestone 6.1 - Group 2 (Tasks 6.1.3 & 6.1.4 - completed)
- [ADD] `POS.Api/Controllers/SyncController.cs` (Ingest controller with POST api/sync/ingest protected by PosDevice policy, claims validation and 501 fallback mapping)
- [MODIFY] `POS.Api/Program.cs` (Registered `ISyncIngestService` scoped service in API container and imported sync namespace)

### Phase 6 / Milestone 6.1 - Group 1 (Tasks 6.1.1 & 6.1.2 - completed)
- [ADD] `POS.Shared/Contracts/Sync/SyncIngestEvent.cs` (DTO record representing individual POS outbox event in the sync contract)
- [ADD] `POS.Shared/Contracts/Sync/SyncIngestRequest.cs` (DTO record representing the bulk batch chunk request containing POS outbox events)
- [ADD] `POS.Shared/Contracts/Sync/SyncIngestEventAck.cs` (DTO record representing single event process acknowledgement status)
- [ADD] `POS.Shared/Contracts/Sync/SyncIngestResponse.cs` (DTO record representing overall central sync response including event-level acks)
- [ADD] `POS.Api/Application/Sync/SyncIngestIdentity.cs` (Record representing authenticated claims-derived device context identity)
- [ADD] `POS.Api/Application/Sync/ISyncIngestService.cs` (Application service abstraction interface for central sync chunk ingestion)
- [ADD] `POS.Api/Application/Sync/SyncIngestService.cs` (Scaffolded service implementation throwing clear NotImplementedException for deferred persistence)

### Group 1 (Tasks 5.5.1 to 5.5.3 - completed)
- [ADD] `POS.Desktop/Services/CashControl/ICashControlService.cs` (Defines core RecordMovementAsync generic contract and request/result records)
- [ADD] `POS.Desktop/Services/CashControl/CashControlService.cs` (Implements RecordMovementAsync validation, pre-flight idempotency lookup, and transaction-isolated persistence)
- [ADD] `POS.Desktop/Data/LocalEntities/LocalCashDrawerMovement.cs` (Append-only SQLite entity for local cash control persistence)
- [ADD] `POS.Desktop/Data/Configurations/Local/LocalCashDrawerMovementConfiguration.cs` (EF Core mapping with positive and non-empty check constraints, unique sequence/idempotency keys, and shift query indexes)
- [ADD] `POS.Desktop/Data/Migrations/Local/20260528140231_AddLocalCashDrawerMovementsTable.cs` (SQLite local database migration)
- [ADD] `POS.Desktop.Tests/Services/CashControl/CashControlServiceTests.cs` (Comprehensive 12-test suite for unprovisioned, session, shift, amount, reason code, idempotency conflict, and append-only drop validations)
- [MODIFY] `POS.Desktop/Data/PosLocalDbContext.cs` (Registered DbSet and CurrentTenantId query filter)
- [MODIFY] `POS.Desktop/Configuration/DesktopHostBuilder.cs` (Registered ICashControlService as a Scoped business service)

### Group 2 (Tasks 5.5.4 to 5.5.6 - completed)
- [MODIFY] `POS.Desktop/Services/CashControl/ICashControlService.cs` (Extended `CashControlMovementRequest` with nullable manager OperatorId and PIN; added `CashDrawerSummaryResult` DTO and `GetDrawerSummaryAsync` method signature)
- [MODIFY] `POS.Desktop/Services/CashControl/CashControlService.cs` (Implemented manager PIN verification, early idempotency check flow, GetDrawerSummaryAsync live balance calculation using local in-memory sums to resolve SQLite decimal aggregator limits, and ShiftOpenPolicyOptions limit/threshold alerts)
- [MODIFY] `POS.Desktop.Tests/Services/CashControl/CashControlServiceTests.cs` (Added unit tests for manager PIN enforcement, duplicate idempotency checks, empty drawer summaries, and alert state transitions)

### Group 3 (Task 5.5.7 - completed)
- [MODIFY] `POS.Desktop/Shell/PosWebMessageRouter.cs` (Registered and implemented handlers for cash.getSummary, cash.recordMovement, cash.getLedger, and cash.getReasonCodes)
- [ADD] `POS.Desktop.Tests/Shell/CashControlBridgeHandlerTests.cs` (Comprehensive 18-test suite verifying bridge routing, success and malformed payload mapping, string/numeric movementType parsing, and fallback categories)
- [MODIFY] `POS.Desktop.Tests/Shell/PosWebMessageRouterTests.cs` (Updated CanHandle registrations list and count assertions to exactly 26)

### Group 4 (Task 5.5.8 - completed)
- [MODIFY] `POS.Desktop/Assets/ui/cash_control.html` (Wired UI to bridge endpoints, implemented dynamic summary and ledger binding, added Manager ID field, implemented Manager PIN Visibility logic, and integrated idempotency key handling)
- [SYNC] `docs/ui-prototype/screens/cash_control.html` (Synchronized byte-identical copy)
- [ADD] `POS.Desktop.Tests/Shell/CashControlScreenStaticTests.cs` (Added 18 static tests checking file parity, bridge endpoint invocations, and sessionStorage exclusions)

### Group 5 (Tasks 5.5.9 to 5.5.10 - completed)
- [MODIFY] `POS.Desktop.Tests/Services/CashControl/CashControlServiceTests.cs` (Added 10 tests verifying stale shifts, terminal/location shift isolation, null session ShiftId grace, valid drop details persistence, duplicate idempotency checks, non-Drop types rejection, alert state transition, and configured limits fallback)
- [MODIFY] `POS.Desktop.Tests/Shell/CashControlBridgeHandlerTests.cs` (Added 6 tests verifying bridge-level rejection of deferred Injection/NoSale/OpeningFloat types, and strict shift/terminal ledger query isolation)
- [MODIFY] `POS.Desktop.Tests/Shell/CashControlScreenStaticTests.cs` (Added 3 tests verifying Injection tab deferral block, Alert code bindings, and Drop-only movementType constraints)

### Test count: 451 passing (was 425; +26 new cash control integration & static tests)
### Prior Test count: 425 passing (was 407; +18 new CashControl static tests)

### Group 5 (Task 5.4.10 - completed)
- [MODIFY] `POS.Desktop.Tests/Shell/PaymentBridgeHandlerTests.cs` (+3 tests: missing `tenderMethodId` -> MALFORMED_REQUEST; `guestName` mapped to `PaymentCompletionRequest.GuestName`; multiple tenders all mapped with amounts and external references)

### Prior Test count: 365 passing (was 362 after 5.4.9; +3 targeted bridge handler tests)

### Group 4 (Task 5.4.9 - completed, post-review fixes applied)
- [MODIFY] `POS.Desktop/Shell/PosWebMessageRouter.cs` (Added `payment.getTenderMethods` and `payment.complete` bridge handlers; added `using POS.Desktop.Data`, `using POS.Desktop.Services.Payments`, `using Microsoft.EntityFrameworkCore`)
- [MODIFY] `POS.Desktop/Assets/ui/payment_screen.html` (Removed `sessionStorage` cart + demo fallback; removed `localStorage` terminal config read; removed `simulateCardPay`/`simulateWalletPay` with setTimeout; added `bridgeRequest()` helper; robust tender resolution via `getCashTenderMethod()`/`getCardTenderMethod()`/`getWalletTenderMethod()` with property-based fallbacks; stable per-attempt external refs via `getOrCreateCardRef()`/`getOrCreateWalletRef()` (`TXN-CARD-...`/`TXN-WALLET-...`); idempotency reset on every cash key, CLEAR, back, setExact, quick-amount select, split input change, wallet phone change, tender tab change; wallet phone sent as `guestPhone` in `payment.complete`; `approveWalletStub` guards against missing wallet method; `renderOrderSummaryFromState()` drives totals from backend; `buildReceipt()` removed)
- [SYNC] `docs/ui-prototype/screens/payment_screen.html` (Byte-identical copy - SHA-256 verified)
- [ADD] `POS.Desktop.Tests/Shell/PaymentBridgeHandlerTests.cs` (15 tests: endpoint registration, tender method DB query + sort, payload validation, success path, idempotency key mapping, external reference mapping, wallet guestPhone mapping, stable card ref mapping, service failure propagation, exception safe handling)
- [ADD] `POS.Desktop.Tests/Shell/PaymentScreenStaticTests.cs` (24 static HTML tests: forbidden patterns, required bridge calls, stub functions, robust resolution helpers, stable ref helpers, wallet fallback body check, idempotency reset breadth, SHA-256 parity)
- [MODIFY] `POS.Desktop.Tests/Shell/PosWebMessageRouterTests.cs` (Added assertions for `payment.getTenderMethods` and `payment.complete`; updated registered type count from 20 to 22)

### Fix 4 confirmed - no code change needed
`LocalTenderMethod` has `HasQueryFilter(x => x.TenantId == CurrentTenantId)` in `PosLocalDbContext`. No `IsActive` field exists; all tenant-filtered rows are active. Current handler is correct.

### Test count after 5.4.9: 362 passing (was 325)

### Group 3 (Task 5.4.8 - completed)
- [ADD] `POS.Desktop/Data/Migrations/Local/20260528104909_AddLocalOrderIdempotencyKeyIndex.cs` (EF Core SQLite local database schema migration adding unique index)
- [ADD] `POS.Desktop/Data/Migrations/Local/20260528104909_AddLocalOrderIdempotencyKeyIndex.Designer.cs` (EF Core migration designer file)
- [MODIFY] `POS.Desktop/Data/Migrations/Local/PosLocalDbContextModelSnapshot.cs` (Updated database snapshot incorporating unique index)
- [MODIFY] `POS.Desktop/Data/Configurations/Local/LocalOrderConfiguration.cs` (Added unique index configuration on TenantId + IdempotencyKey to satisfy database-level uniqueness requirement and enforce save concurrency constraint catches)
- [MODIFY] `POS.Desktop/Services/Payments/PaymentService.cs` (Enforced strict mandatory idempotency key check; early lookup before cart-empty; added deterministic SHA-256 fingerprint matching of TenantId, cart lines variants, itemIds, gross/tax/net prices, totals, tenders, and guest name/phone; bypassed fingerprint on empty-cart retries; integrated unique SQLite duplicate key constraint rollback recovery)
- [MODIFY] `POS.Desktop.Tests/Services/Payments/PaymentServiceTests.cs` (Updated 15 existing test cases to supply mandatory unique idempotency key; added 5 new integration tests verifying missing key rejection, successful fingerprint storage, post-success empty cart retry bypass, same key conflict, and concurrent unique index race collision safe reloads)
- [MODIFY] `docs/antigravity-context/POS_DESKTOP_CURRENT_CONTEXT.md` (Updated context file with Group 3 / Task 5.4.8 completed status, database migrations, and verifications)

### Group 2 (Tasks 5.4.5 to 5.4.7 - completed)
- [ADD] `POS.Desktop/Services/Receipts/IReceiptRenderer.cs` (Decoupled receipt rendering contract)
- [ADD] `POS.Desktop/Services/Receipts/ReceiptRenderer.cs` (Data-driven plain text receipt rendering engine with Math.Max boundary protection)
- [MODIFY] `POS.Desktop/Configuration/DesktopHostBuilder.cs` (Registered IReceiptRenderer in DI container)
- [MODIFY] `POS.Desktop/Services/Payments/PaymentCompletionResult.cs` (Extended with optional parameters for ReceiptText, PrintJobId, and OutboxEventId)
- [MODIFY] `POS.Desktop/Services/Payments/PaymentService.cs` (Integrated receipt rendering and atomic outbox + print enqueuing inside order transaction)
- [MODIFY] `POS.Desktop.Tests/Services/Payments/PaymentServiceTests.cs` (Added integration tests for outbox enqueuing, print queue enqueuing, formatting checks, and database transaction rollback)
- [MODIFY] `docs/antigravity-context/POS_DESKTOP_CURRENT_CONTEXT.md` (Updated context file to preserve prior decisions and update Group 2 status)

### Group 1 (Tasks 5.4.1 to 5.4.4 - completed)
- [ADD] `POS.Desktop/Services/Payments/IPaymentService.cs` (Defines core tender, change, completion contract)
- [ADD] `POS.Desktop/Services/Payments/PaymentTenderRequest.cs` (Tender item in request)
- [ADD] `POS.Desktop/Services/Payments/PaymentCompletionRequest.cs` (Payload required to complete order)
- [ADD] `POS.Desktop/Services/Payments/PaymentCompletionResult.cs` (Result containing status, change, receipt)
- [ADD] `POS.Desktop/Services/Payments/PaymentValidationException.cs` (Custom payment validation exception)
- [ADD] `POS.Desktop/Services/Payments/PaymentService.cs` (Validates session/shift/tenders and commits atomically)
- [ADD] `POS.Desktop/Data/LocalEntities/LocalOrder.cs` (Local order SQLite db entity)
- [ADD] `POS.Desktop/Data/LocalEntities/LocalOrderLine.cs` (Local order line SQLite db entity)
- [ADD] `POS.Desktop/Data/LocalEntities/LocalPayment.cs` (Local payment SQLite db entity)
- [ADD] `POS.Desktop/Data/Configurations/Local/LocalOrderConfiguration.cs` (Local order EF mapping & database check constraints)
- [ADD] `POS.Desktop/Data/Configurations/Local/LocalOrderLineConfiguration.cs` (Local order line EF mapping & check constraints)
- [ADD] `POS.Desktop/Data/Configurations/Local/LocalPaymentConfiguration.cs` (Local payment EF mapping & check constraints)
- [ADD] `POS.Desktop/Data/Migrations/Local/20260528094204_AddLocalOrderPaymentTables.cs` (EF Core SQLite schema migration)
- [ADD] `POS.Desktop/Data/Migrations/Local/20260528094204_AddLocalOrderPaymentTables.Designer.cs` (EF migration designer)
- [MODIFY] `POS.Desktop/Data/PosLocalDbContext.cs` (Added DbSet registers and global query filters for new entities)
- [MODIFY] `POS.Desktop/Configuration/DesktopHostBuilder.cs` (Registered IPaymentService in dependency container)
- [ADD] `POS.Desktop.Tests/Services/Payments/PaymentServiceTests.cs` (Self-contained test suite covering cash/card/wallet/split payments, change calculations, error conditions, SQLite transaction rollback, and cart clearing)

### Group 5 (Task 5.3.10 - completed)
- [MODIFY] `POS.Desktop.Tests/Services/Orders/OrderServiceTests.cs` (Added mixed rates exclusive tax with discount, tax included item with fixed discount, zero/null tax rate items, percentage discount consistent rounding, and equations verification tests)
- [MODIFY] `POS.Desktop.Tests/Shell/OrderBridgeHandlerTests.cs` (Added generic non-validation exceptions safe mapping verification test)

### Group 4 (Tasks 5.3.8 and 5.3.9 - completed)
- [ADD] `POS.Desktop.Tests/Shell/OrderBridgeHandlerTests.cs`
- [MODIFY] `POS.Desktop/Shell/PosWebMessageRouter.cs`
- [MODIFY] `POS.Desktop.Tests/Shell/PosWebMessageRouterTests.cs`
- [MODIFY] `POS.Desktop/Assets/ui/main_checkout.html`
- [MODIFY] `docs/ui-prototype/screens/main_checkout.html`

### Group 3 (Tasks 5.3.5, 5.3.6, and 5.3.7 - completed)
- [ADD] `POS.Desktop/Services/Orders/MoneyRounder.cs`
- [MODIFY] `POS.Desktop/Services/Orders/CartLineDto.cs`
- [MODIFY] `POS.Desktop/Services/Orders/OrderService.cs`
- [MODIFY] `POS.Desktop.Tests/Services/Orders/OrderServiceTests.cs`

### Group 2 (Tasks 5.3.3 and 5.3.4 - completed)
- [ADD] `POS.Desktop/Services/Orders/IDraftCartStore.cs`
- [ADD] `POS.Desktop/Services/Orders/DraftCartStore.cs`
- [ADD] `POS.Desktop/Services/Orders/OrderService.cs`
- [ADD] `POS.Desktop/Services/Orders/OrderValidationException.cs`
- [ADD] `POS.Desktop.Tests/Services/Orders/OrderServiceTests.cs`
- [MODIFY] `POS.Desktop/Services/Catalog/ICatalogService.cs`
- [MODIFY] `POS.Desktop/Services/Catalog/CatalogService.cs`
- [MODIFY] `POS.Desktop/Configuration/DesktopHostBuilder.cs`

### Group 1 (Tasks 5.3.1 and 5.3.2 - committed/pushed)
- [ADD] `POS.Desktop/Services/Orders/IOrderService.cs`
- [ADD] `POS.Desktop/Services/Orders/CartStateDto.cs`
- [ADD] `POS.Desktop/Services/Orders/CartLineDto.cs`

### Group 5 (Task 5.2.10 - committed)
- [SYNC-FIX] `docs/ui-prototype/screens/main_checkout.html` - stale copy synced from Assets version

### Group 4 (Tasks 5.2.8, 5.2.9 - committed)
- [ADD] `POS.Desktop/Services/Shifts/ShiftOpenPolicyOptions.cs`
- [ADD] `POS.Desktop/Services/Shifts/ShiftOpenPolicyResult.cs`
- [MODIFY] `POS.Desktop/Services/Shifts/IShiftService.cs`
- [MODIFY] `POS.Desktop/Services/Shifts/ShiftService.cs`
- [MODIFY] `POS.Desktop/Configuration/DesktopHostBuilder.cs`
- [MODIFY] `POS.Desktop/Shell/PosWebMessageRouter.cs`
- [MODIFY] `POS.Desktop/appsettings.json`
- [MODIFY] `POS.Desktop/Assets/ui/shift_open.html`
- [MODIFY] `docs/ui-prototype/screens/shift_open.html`
- [MODIFY] `POS.Desktop.Tests/Services/Shifts/ShiftServiceTests.cs`
- [MODIFY] `POS.Desktop.Tests/Shell/ShiftBridgeHandlerTests.cs`
- [MODIFY] `POS.Desktop.Tests/Shell/PosWebMessageRouterTests.cs`

## Scope Boundaries & Constraints
- Do NOT use localStorage or sessionStorage for operational screen gating.
- Preserve original element class/ID names in HTML/JS. No UI/CSS redesign.
- Do NOT implement real pinpad/hardware integration.
- Do NOT implement printer wiring.
- Do NOT modify POS.Api or central API migrations.
- Do NOT commit or push.
- Keep all new receipt/outbox/print behavior inside local desktop layer.
- Keep PaymentService safe: return user-safe messages, log details internally.

## Config-Driven Policy Behaviour (Task 5.2.8)

### Configuration
A `"ShiftOpen"` section was added to `appsettings.json`:
```json
"ShiftOpen": {
  "CashDrawerLimit": 25000,
  "AutoSafeDropThreshold": 20000,
  "Checklist": [ ... 5 items ... ]
}
```

### Typed Options
`POS.Desktop/Services/Shifts/ShiftOpenPolicyOptions.cs` - constants `DefaultCashDrawerLimit`, `DefaultAutoSafeDropThreshold`, `MaxChecklistItems` (10), and `DefaultChecklist()` ensure defaults used by both service and tests.

### Bridge Endpoint
`shift.getOpenPolicy` registered in `PosWebMessageRouter`. Returns:
```json
{ "cashDrawerLimit": 25000, "autoSafeDropThreshold": 20000, "checklist": [...] }
```

### Sanitization Rules
- `CashDrawerLimit <= 0` -> replaced with `DefaultCashDrawerLimit`
- `AutoSafeDropThreshold <= 0` -> replaced with `DefaultAutoSafeDropThreshold`
- Null/whitespace checklist items removed; values trimmed; capped at `MaxChecklistItems` (10)

## Checkout Navigation Confirmation (Task 5.2.9)
The `openShift()` function in `shift_open.html` transition flow:
1. `shift.open` bridge call succeeds
2. `.success-overlay.open` class applied -> overlay fades in
3. Progress bar animates to 100% after 50ms
4. `window.location.href = 'main_checkout.html'` fires after 1600ms

## Important Decisions & Gate Behaviour
- **Singleton DraftCartStore & Scoped OrderService:** To manage cart state across multiple bridge requests within transient message scopes, the cart's backing store (`DraftCartStore`) is registered as a thread-safe process-lifetime `Singleton`. The business logic layer (`OrderService`) is registered as `Scoped` so that it can inject scoped services (like `ICatalogService` and database contexts) safely without creating captive dependencies.
- **Database Gated Authority:** All operational screens (`main_checkout.html`, `payment_screen.html`, `cash_control.html`, `shift_close.html`) asynchronously request the `"shift.getCurrent"` bridge endpoint on `DOMContentLoaded`. If the SQLite database does not record an open active shift (`isOpen: false`), they show a user-friendly error toast and redirect to `shift_open.html` after a `1.5-second` delay.
- **Fail Safe / Locked Terminal:** If the bridge transport is unavailable, terminal session context is invalid, or the terminal is unprovisioned, the screens fail closed/locked and redirect immediately to `shift_open.html` without exposing internal exception details.
- **Consistent Bridge Contracts:** Leveraged `"shift.getCurrent"`, returning structured success payloads of type `ShiftDetailsResult`.
- **Strict Location Isolation Gating:** Both `OpenShiftAsync` and `GetCurrentShiftAsync` filter open shifts strictly by location and terminal identifier, ensuring shifts opened at different locations do not bleed through.
- **Identical Copies:** Kept `POS.Desktop/Assets/ui/*.html` and `docs/ui-prototype/screens/*.html` identically synchronized. All 5 milestone-touched screens SHA-256 verified identical.
- **MoneyRounder Policy:** Money calculations use decimal math only. Values are rounded to 2 decimal places using `MidpointRounding.AwayFromZero` commercial rounding, centralized in `POS.Desktop/Services/Orders/MoneyRounder.cs`.
- **Tax Calculation Rules:**
  - **Tax-Exclusive Prices:**
    - `taxableBase = grossAmount - lineDiscount`
    - `taxAmount = taxableBase * taxRate / 100` (rounded)
    - `netAmount = taxableBase + taxAmount` (rounded)
  - **Tax-Inclusive Prices:**
    - `taxableBase = grossAmount - lineDiscount`
    - `taxAmount = taxableBase - (taxableBase / (1 + taxRate / 100))` (rounded)
    - `netAmount = taxableBase`
- **Proportional Discount Distribution:** Cart-level discounts are distributed proportionally across cart lines before tax based on each line's share of total gross subtotal. The last line absorbs any rounding remainder.
- **Deferred Temporary Hold Behavior:** The `holdActiveOrder` function saves the current cart state snapshot to `sessionStorage` under the key `pos_held_order` as a temporary deferred feature. After holding, it calls the `order.clearCart` bridge endpoint to clear the active C# cart. `pos_held_order` is not the active cart source of truth.
- **SQLite Atomic Transactions (Group 1 & 2):** Uses DbContext Transaction to save order, order lines, payments, sync outbox events, and print queue records as an atomic unit, ensuring no partial records can ever be committed.
- **Tender Overpayment Rules:** Prevents non-cash overpayment change drift. Correctly rejects non-cash overpayment unless cash is present to absorb change.
- **Atomic Order Side Effects (Group 2):** Enqueued `SyncOutbox` order completed events and `PrintQueue` receipt print jobs directly inside the `PaymentService` SQLite transaction, ensuring atomic consistency for completing sales.
- **Built-in plain text formatting (Group 2):** Spacing count is dynamically calculated and protected with a safe helper using `Math.Max(0, count)` inside `ReceiptRenderer.cs` to prevent negative width string instantiation errors.
- **Enforced Mandated Idempotency Key (Group 3):** To prevent silent double-charge bypass, all payment completions strictly require a non-blank `IdempotencyKey`, throwing `IDEMPOTENCY_KEY_REQUIRED` early if missing. No random Guid fallback is generated.
- **Early Lookup & Empty Cart Post-Success Bypass (Group 3):** The idempotency check is performed right after operational authorization context validation, and *before* cart-empty validation. Since the draft cart is cleared upon success, retried completed orders bypass fingerprint matching if the active cart is empty, safely returning the original enqueued outcome.
- **Deterministic SHA-256 Payload Fingerprinting (Group 3):** Fingerprint matches TenantId, LocationId, TerminalId, ShiftId, BusinessDate, cart lines details (ItemId, VariantId, Quantity, UnitPrice, GrossAmount, DiscountAmount, TaxAmount, NetAmount), cart totals, normalized tenders, and normalized guest details to reject requests with different payloads as `IDEMPOTENCY_CONFLICT`.
- **SQLite Concurrency Index Race Rollback & Safe Reload (Group 3):** DB update exceptions due to SQLite UNIQUE constraint violations on the `IdempotencyKey` index are intercepted. The transaction is rolled back, the committed order is safely reloaded, fingerprint payload verification is applied, and the original result is returned cleanly.

## Verification Summary (Milestone 5.4 Group 3)

### Builds
- `dotnet build POS.Desktop/POS.Desktop.csproj --configuration Debug`: **0 errors / 0 warnings**
- `dotnet build POS.slnx --configuration Debug`: **0 errors / 0 warnings**

### Tests
- `dotnet test POS.Desktop.Tests`: **325/325 passed** (all 316 existing + 4 Group 2 + 5 new comprehensive idempotency/concurrency integration tests passed successfully)
- `dotnet test POS.Tests`: **49/49 passed** (all 49 central API/core tests passed successfully)

### Git hygiene
- `git diff --check`: Zero whitespace/layout errors
- `git status --short --untracked-files=all`: Verified clean state (only `PaymentService.cs`, `PaymentServiceTests.cs`, `LocalOrderConfiguration.cs`, `PosLocalDbContextModelSnapshot.cs`, and `POS_DESKTOP_CURRENT_CONTEXT.md` modified; new index migration files added).

### SHA-256 sync check (all 5 milestone screens)
| File | Assets hash | Result |
|---|---|---|
| `shift_open.html` | `84F0198FA66D...` | IDENTICAL |
| `main_checkout.html` | `A831FA77E0D6...` | IDENTICAL |
| `payment_screen.html` | `61B638BB1561...` | IDENTICAL (strictly unedited/deferred) |
| `cash_control.html` | `D1ED98B1271D...` | IDENTICAL |
| `shift_close.html` | `49E73F7062E5...` | IDENTICAL |
| **All files synchronized** | | **True** |

## Search Checks (all operational screens)

### shift_open.html
- `PKR 25,000` / `PKR 20,000` hardcoded text: [No] Not present
- `shift.getOpenPolicy` call: [Yes] Present
- `shift.open` call: [Yes] Present
- `id="cash-limit-text"` / `id="safe-drop-threshold-text"` / `id="shift-checklist"` placeholders: [Yes] Present
- `success-overlay` and `main_checkout.html` redirect: [Yes] Present

### All 4 operational gate screens (main_checkout, payment_screen, cash_control, shift_close)
- `shift.getCurrent` gate call: [Yes] Present
- `isOpen` check used for gate logic: [Yes] Present
- `shift_open.html` redirect on gate failure: [Yes] Present
- `localStorage` / `sessionStorage` used for gating: [No] Not present
- Raw exception text shown to cashier: [No] Not present

## Deferred Items
- Real pinpad/hardware integration deferred to Phase 7.6
- Real printer wiring deferred to Phase 7.3

## Verification Summary (Milestone 5.4 Group 5 - Task 5.4.10)

### Builds
- `dotnet build POS.Desktop.Tests`: **0 errors / 0 warnings**

### Tests
- `dotnet test POS.Desktop.Tests`: **365/365 passed** (362 existing + 3 new bridge handler tests)
- Payment filter: **61/61 passed** (20 service + 18 bridge + 24 static + PaymentReconciliationQueue tenant filter)

### Static checks (payment_screen.html)
| Check | Result |
|---|---|
| `sessionStorage.getItem('pos_cart')` absent | [Yes] True |
| `simulateCardPay` absent | [Yes] True |
| `simulateWalletPay` absent | [Yes] True |
| Fake 1800ms timeout absent | [Yes] True |
| Fake 2200ms timeout absent | [Yes] True |
| `order.getCart` call present | [Yes] True |
| `payment.getTenderMethods` call present | [Yes] True |
| `payment.complete` call present | [Yes] True |
| `receiptText` usage present | [Yes] True |

### SHA-256 parity
- `POS.Desktop/Assets/ui/payment_screen.html`: `AB8B001613275593DDAE4055B3CEBE1927928EC90AB957E93CA2C8FBA2D281B6`
- `docs/ui-prototype/screens/payment_screen.html`: `AB8B001613275593DDAE4055B3CEBE1927928EC90AB957E93CA2C8FBA2D281B6`
- **IDENTICAL: True**

### Confirmations
- POS.Api: untouched
- No new migrations created
- No production code modified
- Milestone 5.4: **COMPLETE** (all 10 tasks)

## Verification Summary (Milestone 5.5 Group 2)

### Design Decisions & Implementation Details
- **Manager PIN Verification**: Manager credentials (`ManagerOperatorId` and `ManagerPin`) are validated via `IAuthService.ValidateManagerPinAsync` when the resolved `LocalReasonCode.RequiresManagerApproval` is true. The raw PIN is never logged, stored, or returned. The verified manager employee ID is saved in `LocalCashDrawerMovement.AuthorizedByEmployeeId`.
- **Early Idempotency check**: Handled prior to manager PIN verification, meaning repeat retries of already-persisted movements return the existing receipt immediately without prompting for a manager PIN again.
- **Drawer Balance Calculation**: Calculated expected drawer balance using the formula `OpeningFloat + CashSales - SafeDrops` where `FloatInjections` is set to 0. `Sum()` and `Max()` are processed in-memory to prevent EF Core SQLite database provider decimal sum and DateTimeOffset order-by translation exceptions.
- **Threshold Alerts**: Expected drawer balance is compared against policy values. Alerts code outputs `OVER_LIMIT`, `SAFE_DROP_RECOMMENDED`, or `OK` using configuration-driven threshold limits.

### Builds
- `dotnet build POS.Desktop/POS.Desktop.csproj --configuration Debug`: **0 errors / 0 warnings**
- `dotnet build POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug`: **0 errors / 0 warnings**
- `dotnet build POS.slnx --configuration Debug`: **0 errors / 0 warnings**

### Tests
- `dotnet test POS.Desktop.Tests`: **388/388 passed** (377 existing + 11 new Group 2 tests)
- `dotnet test POS.Tests`: **49/49 passed** (49 central API/core tests)

## Verification Summary (Milestone 5.5 Group 3)

### Design Decisions & Implementation Details
- **Bridge Endpoints Registered**: Registered and routed `cash.getSummary`, `cash.recordMovement`, `cash.getLedger`, and `cash.getReasonCodes`.
- **cash.getSummary**: Returns camelCase expected balance, safe drops, cash sales, opening float, alert thresholds, alert message and code.
- **cash.recordMovement**: Validates amount, reasonCodeId, and idempotencyKey. Decodes movementType string ("Drop" case-insensitive) or numeric (only 4 is valid). Discards/prevents logging or returning `managerPin`. Passes values to `ICashControlService.RecordMovementAsync` and maps the outcome.
- **cash.getLedger**: Restricts querying to active open shifts and active movements. Joins reason codes, sorting by TerminalSequence descending and OccurredOn descending, capped at 100 rows.
- **cash.getReasonCodes**: Searches `LocalReasonCodes`. Filters by ReasonCategory == "CashControl" (case-insensitive) if any match; falls back to all active codes if category has 0 matches, returning `usedFallback = true` metadata.

### Builds
- `dotnet build POS.Desktop/POS.Desktop.csproj --configuration Debug`: **0 errors / 0 warnings**
- `dotnet build POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug`: **0 errors / 0 warnings**
- `dotnet build POS.slnx --configuration Debug`: **0 errors / 0 warnings**

### Tests
- `dotnet test POS.Desktop.Tests`: **407/407 passed** (388 existing + 19 new Group 3 bridge tests)
- `dotnet test POS.Tests`: **49/49 passed** (49 central API/core tests)

## Verification Summary (Milestone 5.5 Group 3)

### Design Decisions & Implementation Details
- **Bridge Endpoints Registered**: Registered and routed `cash.getSummary`, `cash.recordMovement`, `cash.getLedger`, and `cash.getReasonCodes`.
- **cash.getSummary**: Returns camelCase expected balance, safe drops, cash sales, opening float, alert thresholds, alert message and code.
- **cash.recordMovement**: Validates amount, reasonCodeId, and idempotencyKey. Decodes movementType string ("Drop" case-insensitive) or numeric (only 4 is valid). Discards/prevents logging or returning `managerPin`. Passes values to `ICashControlService.RecordMovementAsync` and maps the outcome.
- **cash.getLedger**: Restricts querying to active open shifts and active movements. Joins reason codes, sorting by TerminalSequence descending and OccurredOn descending, capped at 100 rows.
- **cash.getReasonCodes**: Searches `LocalReasonCodes`. Filters by ReasonCategory == "CashControl" (case-insensitive) if any match; falls back to all active codes if category has 0 matches, returning `usedFallback = true` metadata.

### Builds
- `dotnet build POS.Desktop/POS.Desktop.csproj --configuration Debug`: **0 errors / 0 warnings**
- `dotnet build POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug`: **0 errors / 0 warnings**
- `dotnet build POS.slnx --configuration Debug`: **0 errors / 0 warnings**

### Tests
- `dotnet test POS.Desktop.Tests`: **407/407 passed** (388 existing + 19 new Group 3 bridge tests)
- `dotnet test POS.Tests`: **49/49 passed** (49 central API/core tests)

## Verification Summary (Milestone 5.5 Group 4)

### Design Decisions & Implementation Details
- **Wired cash_control.html**: Replaced demo session/localStorage fallback source of truth with real `cash.getSummary`, `cash.recordMovement`, `cash.getLedger`, and `cash.getReasonCodes` bridge requests.
- **Static UI Checks**: Added static tests in `CashControlScreenStaticTests.cs` to check and verify byte-identical SHA-256 parity, sessionStorage exclusions, bridge calls, and preserved switchTab/numKey/submitAction ergonomics.

### Builds
- `dotnet build POS.Desktop/POS.Desktop.csproj --configuration Debug`: **0 errors / 0 warnings**
- `dotnet build POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug`: **0 errors / 0 warnings**
- `dotnet build POS.slnx --configuration Debug`: **0 errors / 0 warnings**

### Tests
- `dotnet test POS.Desktop.Tests`: **425/425 passed** (407 existing + 18 static UI checks)
- `dotnet test POS.Tests`: **49/49 passed**

## Verification Summary (Milestone 5.5 Group 5)

### Design Decisions & Implementation Details
- **Active Shift Hardening & Consistency**: Verified that cash drawer movements strictly resolve the current terminal and location open shift as source of truth. No ShiftId is accepted from the UI payload. Closed shifts, other terminal shifts, or shifts on other locations are successfully ignored/rejected. Having terminal session `ShiftId` as null while a valid shift exists is gracefully handled and succeeds.
- **Injection Explicit Deferral**: Proved that `FloatInjection`, `NoSale`, `Payout`, and `Correction` are rejected with `INVALID_MOVEMENT_TYPE` across UI, Bridge, and Service layers. Verified that no C# enum values for FloatInjection were introduced, keeping the domain model completely pristine.
- **Alert Transitions & Config Fallbacks**: Proved via a state-machine integration test that recording a drop correctly reduces `ExpectedDrawerBalance` and transitions the alert code down from `SAFE_DROP_RECOMMENDED` to `OK`. Verified that policy limit config anomalies fall back safely to default limits (25,000 drawer limit / 20,000 safe drop threshold).

### Builds
- `dotnet build POS.Desktop/POS.Desktop.csproj --configuration Debug`: **0 errors / 0 warnings**
- `dotnet build POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug`: **0 errors / 0 warnings**
- `dotnet build POS.slnx --configuration Debug`: **0 errors / 0 warnings**

### Tests
- `dotnet test POS.Desktop.Tests`: **451/451 passed** (425 existing + 10 service tests + 6 bridge tests + 3 static UI tests)
- `dotnet test POS.Tests`: **49/49 passed** (49 core/API tests)

## Verification Summary (Milestone 6.1 Group 2)

### Design Decisions & Implementation Details
- **Sync Ingest Endpoint Route:** Implemented in `SyncController.cs` under the endpoint `POST /api/sync/ingest`.
- **PosDevice Policy Protection:** Configured `[Authorize(Policy = "PosDevice")]` at the controller level to restrict access strictly to authenticated device clients.
- **Claims-derived identity:** Extracted `tenant_id`, `location_id`, and `terminal_id` from JWT Claims. We added strict validation to reject missing, non-numeric, zero, or negative IDs with `403 Forbidden` (using `Forbid()`).
- **Body identity cross-check:** Cross-checked request body properties (`TenantId`, `LocationId`, `TerminalId`) against the claims-derived identity, returning a descriptive `400 BadRequest` problem response in case of mismatch.
- **Service Ingestion Call:** Safely routed consistent requests to `ISyncIngestService.IngestAsync(...)`.
- **NotImplementedException Mapping:** Handled the current service-level `NotImplementedException` gracefully at the controller layer, returning a clean `501 Not Implemented` response detailing that persistence is deferred. No silent success or faked records occur.
- **Deferred Operations:** Idempotent database writes, deduplication logic, central orders/payments event transformation, and integration tests are deferred to later groups.

### Builds
- `dotnet build POS.slnx --configuration Debug`: **0 errors / 0 warnings**

### Tests
- `dotnet test POS.Tests/POS.Tests.csproj --configuration Debug`: **49/49 passed** (0 warnings/errors)

## Verification Summary (Milestone 6.1 Group 3)

### Design Decisions & Implementation Details
- **Sync Ingest Gateway Status:** Standardized on `"Received"` status instead of plain "Accepted" to prevent operational ledger completion drift. Individual events are acknowledged with `"Received"` status.
- **Durable Payload Preservation:** Raw client event lists (including serialized `PayloadJson` and properties) are securely preserved by storing the complete `SyncIngestAckEnvelope` (containing the request, response, receive timestamp, and status meaning text) inside `SyncIngestAck.AckPayloadJson` using camelCase serialization. This preserves full request/event payload data for future deferred business transformation without requiring new central database event tables or migrations.
- **Idempotency Checks & Equivalence Verification:**
  - Same `ChunkIdempotencyKey` + Same `RequestHash` => Safe duplicate replay. Deserializes the stored `AckPayloadJson` strictly as `SyncIngestAckEnvelope` (throwing a clear `DESERIALIZATION_FAILURE` if parsing fails; direct `SyncIngestResponse` fallback is disabled/removed to enforce request-envelope data security).
  - Replay is guarded by a comprehensive, strict private equivalence helper (`IsStoredEnvelopeEquivalentToRequest`) comparing chunk headers (`chunkSequence`, `chunkIdempotencyKey`, `requestHash`, `tenantId`, `locationId`, `terminalId`), event counts, and every single event metadata property by exact index order (`eventId`, `terminalSequence`, `eventType`, `idempotencyKey`, `payloadHash`, `correlationId`).
  - If the stored envelope differs from the incoming request (despite matching RequestHash), we immediately throw a `SyncConflictException` returning `409 Conflict` with `STORED_ENVELOPE_CONFLICT` or `IDEMPOTENCY_CONFLICT` code.
  - Same `ChunkIdempotencyKey` + Different `RequestHash` => Throws `SyncConflictException` returning `409 Conflict` (`IDEMPOTENCY_CONFLICT`).
  - Same `TenantId` + `TerminalId` + `ChunkSequence` + Different Key => Throws `SyncConflictException` returning `409 Conflict` (`SEQUENCE_CONFLICT`).
- **Same-Batch Duplicate Event Rejection:** Enforces strict internal consistency inside the incoming batch. If duplicate `EventId` or duplicate non-blank `IdempotencyKey` values are detected inside `request.Events`, the API immediately rejects the request with a `SyncConflictException` using `DUPLICATE_EVENT_ID` or `DUPLICATE_EVENT_IDEMPOTENCY_KEY` respectively, returning `409 Conflict`.
- **Client Hash Trust Guarding:** The `RequestHash` parameter is still client-provided, but we no longer blindly trust it. We strictly guard the request integrity by verifying the stored request envelope matches the incoming request fields exactly. Full server-side canonical request hash computation remains deferred.
- **DbUpdateException Concurrency Recovery:** Intercepts `DbUpdateException` UNIQUE index violations on concurrent incoming requests, automatically rolls back, fetches the winning persisted ack from the database, runs the exact same safe-replay payload equivalence checks, and returns the response or conflict cleanly.
- **Deduplication Constraints & Deferrals:**
  - Duplicate terminal sequence blocking is enforced by unique sequence index; strict out-of-order/gap checking is deferred.
  - Same-batch duplicate EventId and duplicate event IdempotencyKey are rejected.
  - Cross-chunk event ID duplicate detection is deferred until a proper inbound event ledger/table or processor exists.
- **Deferred Operations:** Both business event ledger transformations (mapping events to operational tables) and central canonical hash recalculations remain deferred.

## Verification Summary (Milestone 6.1 Group 4)

### Design Decisions & Implementation Details
- **Dynamic Test Authentication Scheme**: Leveraged the `ApiWebApplicationFactory` and `TestRequestAuthentication` headers scheme (`X-Test-Authenticate` / `X-Test-Claim-`) to inject custom device context profiles.
- **Valid Seeding Setup**: Automatically queries existing seeded locations and inserts a unique active `Terminal` instance per test run to provide real database identity constraints.
- **Strict Authorization Mapping**: Integrated 10 distinct integration scenarios validating `401 Unauthorized` block on non-token requests, `403 Forbidden` block on non-device clients, missing/blank identity claims, malformed/non-positive integer identifiers, and `400 BadRequest` body identity mismatch.
- **Ingest Envelope Durable Persistence**: Successfully tested that the happy path returns `200 OK` with `Received` status, persists the `SyncIngestAck` record, and stores the complete serialized `SyncIngestAckEnvelope` preserving events payload, hash, and idempotency key.
- **Safe Replays Verification**: Proved that multiple duplicate chunk requests return the same `AckId` outcome with no duplicate row created in the database.
- **Accurate Conflict Rejection**: Proved that duplicate keys with different hashes (`IDEMPOTENCY_CONFLICT`), sequence collision (`SEQUENCE_CONFLICT`), and same-batch duplicate `EventId` (`DUPLICATE_EVENT_ID`) or event `IdempotencyKey` (`DUPLICATE_EVENT_IDEMPOTENCY_KEY`) are rejected with `409 Conflict` and descriptive ProblemDetails.

### Builds
- `dotnet build POS.slnx --configuration Debug`: **0 errors / 0 warnings**

### Tests
- `dotnet test POS.Tests/POS.Tests.csproj --configuration Debug`: **68/68 passed** (49 prior central tests + 19 new integration test runs covering 12 scenarios)

## Verification Summary (Milestone 6.1 Group 5)

### Design Decisions & Implementation Details
- **Authoritative Contract Documentation**: Created a premium technical endpoint specification file at `docs/sync/SYNC_INGEST_ENDPOINT.md`.
- **Thorough Specifications Included**:
  - Detailed `/api/sync/ingest` HTTP method, headers, auth policies (`PosDevice` requirement), and claims verification.
  - Identity integrity cross-checking rules mapped.
  - Comprehensive schemas of `SyncIngestRequest`, `SyncIngestEvent`, `SyncIngestResponse`, and `SyncIngestEventAck` with exact database length restrictions.
  - Exact deduplication algorithms, safe replay envelope comparison (`IsStoredEnvelopeEquivalentToRequest`), same-batch duplicate event/idempotency keys validations, and DB indexes explained.
  - Detailed Status Code Matrix and realistic, fully validated camelCase JSON payload examples.
  - Roadmap of deferred milestones clearly listed.

### Builds
- `dotnet build POS.slnx --configuration Debug`: **0 errors / 0 warnings**

### Tests
- **Not Run**: Tests were not run for this group because it is a documentation-only and context-only change. The existing 68/68 test suite remains fully correct and functional.

## Verification Summary (Milestone 6.2 Group 1)

### Design Decisions & Implementation Details
- **Sync Configuration Section Added**: Added the `"Sync"` configuration block to `appsettings.json` specifying default localhost API URL (`https://localhost:5001`), default relative ingest route (`/api/sync/ingest`), 15-second request timeout limit, and 300-second clock skew tolerance margin. No production secrets, JWT signing keys, static tokens, or device credentials are configured.
- **Sync Client Options Model**: Created `SyncClientOptions.cs` in `POS.Desktop/Services/Sync/` with self-contained, robust parameters validation checking. It rejects non-http/https absolute URLs, blank routes, non-positive or unbounded timeouts, and negative or unbounded clock skew margins.
- **Typed Synchronization Client Interface**: Established `ISyncIngestClient.cs` using the core `POS.Shared` synchronization DTO records, providing a pristine, decoupled, non-blocking network boundary layer.
- **Category-Safe Error Model**: Implemented `SyncIngestClientError.cs` defining a non-sensitive `SyncIngestClientErrorType` enum categorizing errors into `Configuration`, `Offline`, `Timeout`, `Unauthorized`, `Forbidden`, `Conflict`, `Validation`, `ServerError`, and `Unexpected`. This completely prevents raw network exceptions from leaking to the UI thread.
- **Outcome Result Wrapper**: Created `SyncIngestClientResult.cs` providing type-safe `Succeeded(response)` and `Failed(error)` static factories to wrap operational results.
- **Optional Clean Token Interface**: Drafted `IDeviceTokenProvider.cs` with an abstract token contract returning `DeviceTokenResult` (Success/Token/ErrorMessage record) to lay a robust groundwork for future JWT bearer integration.
- **Focused Unit Test Suite**: Added a comprehensive unit test suite in `SyncClientOptionsTests.cs` (22 test scenarios across 8 test methods) verifying absolute URI formatting, bounds, result factories, and error safety.
- **Explicit Scope Boundaries Enforced**:
  - No HTTP client implementation was started.
  - No token acquisition or refresh flow was built.
  - No DI registration or delegating handler was configured.
  - Background hosted sync worker (`SyncProcessor`) and SQLite outbox drainage (`SyncOutbox`) were not started.
  - POS.Api was untouched, and no database migrations were created.
  - Zero server signing keys or JWT generation logic exist in the desktop project.

### Builds
- `dotnet build POS.slnx --configuration Debug`: **0 errors / 0 warnings**

### Tests
- `dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug --filter "FullyQualifiedName~Services.Sync"`: **22/22 passed**
- `dotnet test POS.slnx --configuration Debug`: **541/541 passed** (473 desktop tests + 68 API integration tests)

## Next Recommended Milestone
- **Phase 6 / Milestone 6.2 - Group 2** (Implementation of the HTTP sync client and device token/refresh provider flows)

## Verification Summary (Milestone 6.2 Group 2)

### Design Decisions & Implementation Details
- **Sync Ingest Client Implemented**: Created `SyncIngestClient.cs` performing safe POST requests to `POST /api/sync/ingest`. It validates parameters, retrieves the device token, formats absolute request URIs, handles serialization/deserialization, and intercepts status code errors. No raw exceptions, network errors, or uncaught messages escape the client interface bounds or leak into result payloads; unexpected exceptions are caught and mapped to a generic operator-safe error message. No security token value, full request body payload, or event payload JSON is logged.
- **Decoupled Safe Token Provider**: Refined `IDeviceTokenProvider.cs` to add custom expiration and `ForceRefreshAsync` parameters. Implemented `FixedDeviceTokenProvider.cs` using a simple in-memory structure without file persistence, secrets, or signature keys.
- **Safe Refresh Semantics**: Integrated automatic expiration checking (supporting `ExpiresAtUtc`) and a custom test-refresh callback delegate. Exposes standard "Device token refresh source is not configured." message on default force refreshes.
- **Extensive Unit Test Harness**: Added 22 unit tests across `DeviceTokenProviderTests.cs` and `SyncIngestClientTests.cs` (including safety verification tests for masking unexpected exceptions behind a generic operator-safe message) using `FakeHttpMessageHandler` to mock HTTP status outcomes (400, 401, 403, 409, 500, 501), socket exceptions, timeouts, and JSON parse failures.
- **Explicit Scope Boundaries Enforced**:
  - No DI registration or delegating handler was added in `DesktopHostBuilder.cs` (Group 3 concern).
  - Background workers (`SyncProcessor`) and SQLite outbox drainage (`SyncOutbox`) remain completely unstarted.
  - POS.Api was untouched, and no database migrations were created.
  - Zero server signing keys, static configuration tokens, or device credentials are owned by the client project.

### Builds
- `dotnet build POS.slnx --configuration Debug`: **0 errors / 0 warnings**

### Tests
- `dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug --filter "FullyQualifiedName~Services.Sync"`: **44/44 passed** (+22 new Group 2 tests after the exception masking safety fix)
- `dotnet test POS.slnx --configuration Debug`: **563/563 passed** (495 desktop tests + 68 API integration tests)

## Verification Summary (Milestone 6.2 Group 3)

### Design Decisions & Implementation Details
- **Typed HttpClient Registered**: Registered `ISyncIngestClient` via typed client `AddHttpClient<ISyncIngestClient, SyncIngestClient>` inside `DesktopHostBuilder.cs` (Task 6.2.6). Enabled transitively via Microsoft.Extensions.Http package reference.
- **Exception-Free DI Configuration**: Hardened client setup in DI container so that configuration anomalies (blank or structurally invalid URLs/timeouts) never throw exceptions at boot-time or DI resolution time. Invalid configurations are safely handled at call-time by `SyncIngestClient.IngestAsync`.
- **Safe Timeout & Bounded Mappings**: The timeout is securely set from `TimeoutSeconds` if it is greater than 0 and less than or equal to 300 seconds, falling back to a safe, bounded standard of 15 seconds.
- **Default Fail-Safe Token Provider**: Registered `IDeviceTokenProvider` as `UnconfiguredDeviceTokenProvider` to provide a clean fallback boundary that returns typed failure results indicating missing token sources without generating exceptions (Task 6.2.7).
- **Thorough DI Integration Tests**: Added a new test class `SyncDiResolutionTests.cs` (3 test methods / 6 test cases) to verify host DI resolution, standard binding logic, timeout boundaries, fallback defaults, and result type mapping when configured with unconfigured token providers.

### Builds
- `dotnet build POS.slnx --configuration Debug`: **0 errors / 0 warnings**

### Tests
- `dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug --filter "FullyQualifiedName~Services.Sync"`: **50/50 passed** (+6 new Group 3 DI/container safety test cases)
- `dotnet test POS.slnx --configuration Debug`: **569/569 passed** (501 desktop tests + 68 API integration tests)

## Verification Summary (Milestone 6.2 Group 4)

### Design Decisions & Implementation Details
- **API-Side Smoke Verification**: Added `SyncIngestSmokeTests.cs` (Task 6.2.9) verifying direct API ingest call processing. This test uses the existing `TestRequestAuthentication.Apply` helper directly and successfully validates that a valid chunk request yields a `Received` response, without introducing any dependency from `POS.Tests` on `POS.Desktop`.
- **Asynchronous Safety Check**: Created `SyncStaticAnalysisTests.cs` (Task 6.2.10) to automatically scan the C# sync services layer, asserting that no `.Result`, `.Wait(`, or `GetAwaiter().GetResult()` blocking operations are present in active production files.
- **Pure API Target Integration**: Verified `POS.Tests/POS.Tests.csproj` remains `net8.0` with only `POS.Api` and `POS.Shared` project references, preserving pure non-WPF API test scope.

### Builds
- `dotnet build POS.slnx --configuration Debug`: **0 errors / 0 warnings**

### Tests
- `dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug --filter "FullyQualifiedName~Services.Sync"`: **51/51 passed** (+1 new static async-safety test case)
- `dotnet test POS.Tests/POS.Tests.csproj --configuration Debug --filter "FullyQualifiedName~SyncIngestSmokeTests"`: **1/1 passed** (+1 new API smoke test case)
- `dotnet test POS.slnx --configuration Debug`: **571/571 passed** (502 desktop tests + 69 API integration tests)

## Verification Summary (Milestone 6.3 Group 1)

### Design Decisions & Implementation Details
- **SyncProcessor Background Worker**: Implemented as a hosted service deriving from `BackgroundService`. The lifecycle execution immediately yields control using `await Task.Yield()`, avoiding any block of WPF startup or UI thread initialization.
- **Unprovisioned Device Safety**: Gated outbox sweeps on `IProvisionedTerminalContext.IsProvisioned` check. If terminal is not provisioned, DB and API sync operations are skipped and the service delays until the next interval.
- **Clean Cooperative Shutdown**: Handled the host `CancellationToken` gracefully. Captured `OperationCanceledException` when cancellation is requested to ensure clean worker shutdown without unhandled exceptions on application exit.
- **Config-Driven Options**: Tuned `BatchSize` (1-500) and `PollIntervalSeconds` (1-3600) default values in `appsettings.json` and bound to `SyncProcessorOptions` POCO mapped via Microsoft.Extensions.Options.
- **Thorough Test Coverage**: Created `SyncProcessorOptionsTests.cs` (5 unit tests) and `SyncProcessorTests.cs` (3 lifecycle/safety tests), and expanded `SyncDiResolutionTests.cs` (hosted service container registration assertions).
- **Asynchronous Safety Check**: Verified that no blocking calls (`.Result`, `.Wait()`, or `GetAwaiter().GetResult()`) exist in the new files via `SyncStaticAnalysisTests.cs`.

### Builds
- `dotnet build POS.slnx --configuration Debug`: **0 errors / 0 warnings**

### Tests
- `dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug --filter "FullyQualifiedName~Services.Sync"`: **68/68 passed** (+17 new test cases/methods for processor lifecycle and options)
- `dotnet test POS.slnx --configuration Debug`: **588/588 passed** (519 desktop tests + 69 API integration tests)

## Verification Summary (Milestone 6.3 Group 2)

### Design Decisions & Implementation Details
- **Decoupled Captive Dependency Management**: Created a separate scoped interface `ISyncOutboxBatchReader` and implementation `EfSyncOutboxBatchReader` to query SQLite. Resolved it dynamically inside `SyncProcessor` (Singleton) loop cycles using short-lived scopes generated via `IServiceScopeFactory`, protecting host DB container lifecycle limits.
- **Index-Optimized Sequencing**: Aligned the LINQ outbox query strictly with the composite SQLite database index `IX_SyncOutbox_Status_Order` (Status, BusinessDate, TerminalSequence) using `.OrderBy(x => x.BusinessDate).ThenBy(x => x.TerminalSequence).ThenBy(x => x.Id)` to bypass expensive full-table scans.
- **No-Tracking Projection**: Mapped records directly into lightweight, read-only DTO records (`SyncOutboxBatch` and `SyncOutboxBatchItem`) with `.AsNoTracking()`. This reduces change tracking allocations and guarantees that subsequent worker code cannot accidentally mutate in-memory entities.
- **Robust Integration Testing**: Created `SyncOutboxBatchReaderTests.cs` using native in-memory SQLite providers. Verified unprovisioned terminal logic, status filtering boundaries, identity matches, chronological sequences, and batch size take limits without external third-party mocking packages.
- **Asynchronous Safety Check**: Verified that no blocking calls (`.Result`, `.Wait()`, or `GetAwaiter().GetResult()`) exist in the new files via `SyncStaticAnalysisTests.cs`.

### Builds
- `dotnet build POS.slnx --configuration Debug`: **0 errors / 0 warnings**

### Tests
- `dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug --filter "FullyQualifiedName~Services.Sync"`: **74/74 passed** (+6 new integration tests verifying query constraints, batch ordering, and projections; +0 regressions)
- `dotnet test POS.slnx --configuration Debug`: **594/594 passed** (525 desktop tests + 69 API integration tests)

## Verification Summary (Milestone 6.3 Group 3)

### Design Decisions & Implementation Details
- **Stateless Request Builder Pattern**: Created `ISyncIngestRequestBuilder` and implementation `SyncIngestRequestBuilder` to isolate the payload creation from the worker thread. It has zero external dependencies, database access, or network integrations, making it completely decoupled and pure.
- **Deterministic Batch Sequence Identification**: Calculated sequence values without `SyncCursor` dependency by resolving `Min(x => x.TerminalSequence)` from the current batch items.
- **Deterministic Key / Hash / Correlation Generation**:
  - `ChunkIdempotencyKey`: Mapped via composite string values including tenant, location, terminal, business date ranges, min/max terminal sequences, event counts, and 32-character SHA-256 hex hashes of ordered events identity material. Max length remains well under 120 characters (~73 characters).
  - `CorrelationId`: Formatted deterministically as `sync-chunk-{shortHash}`, remaining under 100 characters.
  - `RequestHash`: Generated deterministically from canonical request metadata and event collections mapped via `|` structured delimiters, strictly preventing any process-skew or non-deterministic variance.
- **Process-Local Duplicate Post Guard**: Implemented a thread-safe `HashSet` duplication guard (`_successfullyPostedChunkKeysThisSession`) in `SyncProcessor` to prevent spamming the central API during the same application session. This guard bridges the gap until Group 4 introduces persistent database outbox updates and cursor increments.
- **Asynchronous Safety Check**: Maintained 100% asynchronous safety; zero thread-blocking calls (`.Result`, `.Wait()`, `GetAwaiter().GetResult()`) exist in the new files.

### Builds
- `dotnet build POS.slnx --configuration Debug`: **0 errors / 0 warnings**

### Tests
- `dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug --filter "FullyQualifiedName~Services.Sync"`: **95/95 passed** (+16 new tests: 12 builder validation and uniqueness checks, 4 processor guard/retry checks; +0 regressions)
- `dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug --filter "FullyQualifiedName~SyncStaticAnalysisTests"`: **1/1 passed** (no async-blocking warnings)
- `dotnet test POS.slnx --configuration Debug`: **615/615 passed** (546 desktop tests + 69 API integration tests)

## Verification Summary (Milestone 6.3 Group 4)

### Design Decisions & Implementation Details
- **Separate Scoped database mutation service**: Resolved `ISyncAckApplier` as a scoped service `EfSyncAckApplier` dynamically inside `SyncProcessor` loop iterations to execute SQLite mutations within shorter database contexts.
- **Full Chunk "All-or-Nothing" Validation**: Strictly validated Central API response identity context, chunk sequence numbers, idempotency keys, response counts, and single-event Received acknowledgment details before applying database mutations. Missing or rejected event acks cleanly rollback changes.
- **Durable Atomic SQLite Transactions**: Applied all database updates (acknowledging `SyncOutbox` pending rows and updating/inserting `SyncCursor` records) within a single SQLite transaction scope, preventing database state mismatch on write failures.
- **Monotonic SyncCursor Advancement**: Monotonically advanced the unique SQLite cursor registered under stream name `"push:outbox"` and identified strictly by unique index fields `TenantId + TerminalId + StreamName` (excluding `LocationId` from uniqueness query).
- **Process-Local Guard Integration**: Hardened the in-memory session duplicate-post guard to ONLY store successfully processed chunk keys after database transactions commit cleanly.
- **Asynchronous Safety Check**: Maintained 100% asynchronous safety; zero thread-blocking calls (`.Result`, `.Wait()`, `GetAwaiter().GetResult()`) exist in production files.

### Builds
- `dotnet build POS.slnx --configuration Debug`: **0 errors / 0 warnings**

### Tests
- `dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug --filter "FullyQualifiedName~Services.Sync"`: **116/116 passed** (+17 new SQLite integration tests covering validations/cursors; +4 new processor workers tests; +0 regressions)
- `dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug --filter "FullyQualifiedName~SyncStaticAnalysisTests"`: **1/1 passed** (no async-blocking warnings)
- `dotnet test POS.slnx --configuration Debug`: **636/636 passed** (567 desktop tests + 69 API integration tests)

## Verification Summary (Milestone 6.3 Group 5)

### Design Decisions & Implementation Details
- **SyncProcessor-driven integration test**: Task 6.3.10 is proven by `SyncProcessorPipelineIntegrationTests.cs`, which runs the real `SyncProcessor` background service against real in-memory SQLite. This exercises the full pipeline: `EfSyncOutboxBatchReader` → `SyncIngestRequestBuilder` → `CapturingSyncIngestClient` (fake) → `EfSyncAckApplier`.
- **TaskCompletionSource synchronization**: The `SignalingSyncAckApplier` wrapper fires a `TaskCompletionSource<SyncAckApplyResult>` after `EfSyncAckApplier.ApplySuccessAsync` returns. The test awaits this TCS (bounded by 5 seconds), ensuring assertions run only AFTER the SQLite transaction is committed — not against a sleep timer.
- **PollIntervalSeconds = 60**: After one cycle the processor parks in `Task.Delay(60s, stoppingToken)`. `StopAsync` cancels that delay immediately — tests do not wait 60 seconds.
- **ServiceCollection wiring**: `EfSyncAckApplier` is registered as a concrete scoped type; `ISyncAckApplier` is bound to a factory that wraps it with `SignalingSyncAckApplier`, sharing a single TCS captured in the factory lambda closure. This avoids captive-dependency problems while allowing per-scope real applier instances.
- **No true API-backed E2E**: Deferred. The API ingest behavior is already proven by 12 endpoint tests (`SyncIngestEndpointTests`) and a smoke test (`SyncIngestSmokeTests`). Group 5 focuses on the desktop pipeline; full connectivity integration is deferred to a later explicit milestone.
- **No retry/backoff/quarantine**: Strictly out of scope for Milestone 6.3 (Milestone 6.4 concern).
- **No production code changes**: All new code is test-local. `SyncProcessor`, `EfSyncAckApplier`, and all other production services are unchanged.

### Builds
- `dotnet build POS.slnx --configuration Debug`: **0 errors / 0 warnings**

### Tests
- `dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug --filter "FullyQualifiedName~Services.Sync"`: **118/118 passed** (+2 new SyncProcessor pipeline integration tests)
- `dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug --filter "FullyQualifiedName~SyncStaticAnalysisTests"`: **1/1 passed** (no async-blocking warnings; no new production sync files added)
- `dotnet test POS.slnx --configuration Debug`: **638/638 passed** (569 desktop tests + 69 API integration tests)

### Git hygiene
- `git diff --check`: Zero whitespace/layout errors
- `git status --short`: Only `SyncProcessorPipelineIntegrationTests.cs` (untracked new file) and `.claude/settings.local.json` (tool config, not production code)

## Next Recommended Milestone
- **Phase 6 / Milestone 6.3: COMPLETE** (all 10 tasks done)
- **Next: Phase 6 / Milestone 6.4 — Retry and backoff / quarantine** (failed post handling, dead-letter, exponential backoff)
