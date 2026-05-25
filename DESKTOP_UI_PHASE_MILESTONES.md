# Desktop UI Phase Milestones

**Project:** IMAGYN POS — `POS.Desktop` terminal build-out
**Companion to:** `DESKTOP_UI_INTEGRATION_PLAN.md` (the 8-phase roadmap is §16 there)
**Status:** Planning document — **no implementation code yet**
**Last updated:** 2026-05-23

---

## How to read this document

This file takes each of the 8 phases from `DESKTOP_UI_INTEGRATION_PLAN.md` and breaks it into **practical, sequenced milestones**. Every milestone is intentionally scoped so it can later be decomposed into **exactly 10 implementation tasks** — we are *not* writing those tasks here.

Each milestone is described with:

- **Purpose** — why it exists, in one or two lines.
- **Expected output** — the concrete, demonstrable result.
- **Files/folders likely involved** — where work will land (paths relative to repo root).
- **Dependencies** — milestones (or external facts) that must be true first.
- **Acceptance criteria** — the checklist that says "done."
- **Risk notes** — what could go wrong or needs care.

**Sizing rule:** a milestone is "right-sized" when its 10 future tasks would each be a small, single-sitting change (a file, a method, a wiring step, a test). If a milestone looks like it needs 25 tasks, it's split; if it needs 3, it's merged.

**Guardrails carried from the integration plan (apply to every milestone):**
- Phase 2 now includes a controlled production UI/UX modernization. HTML/CSS edits are allowed there; later phases should keep UI changes narrow and intentional.
- Do **not** remove `docs/ui-prototype/*` until the in-app UI/UX sign-off is complete (Phase 2+).
- No business logic in the UI — the HTML captures input and renders responses; C# owns all decisions.
- Offline-first: the terminal never blocks on the network.

**Phase → milestone count:** P1 (5), P2 (6), P3 (5), P4 (5), P5 (6), P6 (5), P7 (6), P8 (6) = **44 milestones**.

---

## Phase 1 — Desktop shell integration

**Phase objective:** Replace the blank window with a proper application host that boots cleanly and renders web content full-screen, with the local database ready on first run.

### Milestone 1.1 — WebView2 dependency & framework baseline
- **Purpose:** Make WebView2 available and lock a consistent target framework before any shell code is written.
- **Expected output:** `POS.Desktop` references `Microsoft.Web.WebView2`, builds clean on a single TFM, and the SDK control type is confirmed usable.
- **Files/folders:** `POS.Desktop/POS.Desktop.csproj`; solution `POS.slnx`; stray `obj/` build artifacts.
- **Dependencies:** None (entry point of the whole effort).
- **Acceptance criteria:** `dotnet build POS.slnx` succeeds; WebView2 package restored; only `net8.0-windows` outputs are produced (no stray `net10` artifacts); no new analyzer warnings.
- **Risk notes:** Mixed/leftover TFM build outputs already exist under `obj/`; pin the TFM and clean to avoid confusion. WebView2 SDK version should match a runtime that can be deployed.

### Milestone 1.2 — Generic Host bootstrap (DI rework)
- **Purpose:** Replace the hand-rolled `ServiceCollection` in `App.xaml.cs` with `Microsoft.Extensions.Hosting`, giving a single composition root for all future services.
- **Expected output:** App starts via a built `IHost`; `PosLocalDbContext` and `IProvisionedTerminalContext` are resolved from the host container; lifetime is managed on shutdown.
- **Files/folders:** `POS.Desktop/App.xaml.cs`, `POS.Desktop/App.xaml`; new `POS.Desktop/Configuration/` host/registration helpers (alongside existing `LocalDatabaseConfigurationGuard`).
- **Dependencies:** Milestone 1.1.
- **Acceptance criteria:** App launches with no DI resolution errors; existing `NoProvisionedTerminalContext` and SQLite registration still resolve; host disposes cleanly on exit; configuration (`appsettings.json`) flows through the host.
- **Risk notes:** WPF + Generic Host lifetime ordering (`OnStartup`/`OnExit`) must be correct so the window doesn't open before the host is ready.

### Milestone 1.3 — Full-screen kiosk shell hosting WebView2
- **Purpose:** Turn `MainWindow` into a borderless, full-screen terminal shell containing a single WebView2 control.
- **Expected output:** App opens maximized/borderless; WebView2 initializes its `CoreWebView2` environment and displays a placeholder page.
- **Files/folders:** `POS.Desktop/MainWindow.xaml`, `POS.Desktop/MainWindow.xaml.cs`; new `POS.Desktop/Shell/WebViewHost.cs`.
- **Dependencies:** Milestones 1.1, 1.2.
- **Acceptance criteria:** Window is full-screen with no chrome; `CoreWebView2` initializes without exception; a placeholder/local page renders; user-data folder location is explicit and writable.
- **Risk notes:** `EnsureCoreWebView2Async` is async — initialization must complete before navigation. User-data folder defaults can fail under locked-down accounts.

### Milestone 1.4 — Startup database migration & first-run readiness
- **Purpose:** Guarantee the local SQLite schema exists before any screen needs it.
- **Expected output:** On startup the app applies pending EF migrations to the local DB and verifies connectivity before showing UI.
- **Files/folders:** `POS.Desktop/Data/PosLocalDbContext.cs` (consumer), `POS.Desktop/Configuration/LocalDatabaseConfigurationGuard.cs`, host startup code in `App.xaml.cs`/`Shell/`.
- **Dependencies:** Milestones 1.2, 1.3.
- **Acceptance criteria:** Fresh machine → DB file created and migrated on first launch; second launch is a no-op; a clear, logged failure path if the DB can't be opened.
- **Risk notes:** Tenant query filters mean data reads will still be empty until provisioning is real (Phase 4) — this milestone only guarantees *schema*, not *data*.

### Milestone 1.5 — Shell diagnostics & WebView2 runtime guard
- **Purpose:** Detect a missing/incompatible WebView2 runtime and surface startup errors instead of crashing silently.
- **Expected output:** A runtime-presence check at boot; a friendly fallback message if the Evergreen runtime is absent; basic shell-level logging.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`, new `POS.Desktop/Shell/` startup-guard helper, `appsettings.json` (log settings).
- **Dependencies:** Milestones 1.3, 1.4.
- **Acceptance criteria:** With runtime present → normal boot; with runtime absent → clear actionable message, no unhandled exception; startup events are logged to a known location.
- **Risk notes:** Don't over-build telemetry here — full logging/telemetry is Phase 8. Keep this to a minimal guard + log.

---

## Phase 2 — Host screens, route them, and modernize the UI/UX

**Phase objective:** Get all 7 screens rendering and navigable in the shell (WPF shell replaces the `index.html` simulator) **and** modernized to a production-ready refined white/light IMAGYN POS UI/UX. Strict pixel parity with the original prototype is **retired** in favor of a UI/UX sign-off, but the dark "Operator Terminal" direction is not retained.

### Milestone 2.1 — Asset ingestion pipeline
- **Purpose:** Bring the screen assets into the app as build content and keep source/production copies synchronized.
- **Expected output:** `docs/ui-prototype/screens/*` and `logo.png` copied to `POS.Desktop/Assets/ui/`, marked as Content with copy-to-output; build emits them next to the binary.
- **Files/folders:** `POS.Desktop/Assets/ui/*`, `POS.Desktop/POS.Desktop.csproj` (Content globs).
- **Dependencies:** Phase 1 complete.
- **Acceptance criteria:** All 7 HTML files + `logo.png` present in the build output; `docs/ui-prototype/screens/` and `POS.Desktop/Assets/ui/` hashes match for the shipped UI assets.
- **Risk notes:** Manual copies can drift. Keep `docs/` and `Assets/ui/` synchronized until cleanup promotes one production source.

### Milestone 2.2 — Virtual host mapping & initial navigation
- **Purpose:** Serve the local screens under a stable origin so fetch/host-object semantics work.
- **Expected output:** `SetVirtualHostNameToFolderMapping("pos.app", …)` wired; shell navigates to `https://pos.app/terminal_login.html` on boot.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`.
- **Dependencies:** Milestones 1.3, 2.1.
- **Acceptance criteria:** Login screen loads over the virtual host; relative asset references (CSS/JS/img) resolve; no file:// origin in use.
- **Risk notes:** Resource-access kind and host name must be consistent; getting the mapped folder path wrong yields blank screens.

### Milestone 2.3 — In-app screen routing (retire the simulator)
- **Purpose:** Make the 7-screen flow navigable inside the shell without `index.html`.
- **Expected output:** Existing `window.location.href` links resolve under the virtual host across the full flow; the simulator wrapper is not used by the app.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`; screen HTML/CSS only if routing or Phase 2 UI modernization requires it.
- **Dependencies:** Milestone 2.2.
- **Acceptance criteria:** provision → login → shift_open → main_checkout → payment → cash_control → shift_close all reachable by their existing navigation; back-to-login works; `index.html` is never loaded by the shell.
- **Risk notes:** The simulator used `postMessage` to sync a parent sidebar — that machinery is irrelevant now and must not leak into the app.

### Milestone 2.4 — Production UI/UX sign-off across all 7 screens
- **Purpose:** Confirm the 7 screens read as one polished, cohesive production POS terminal (not pixel-matching the old prototype).
- **Expected output:** A documented review pass (per screen) against the production-ready bar.
- **Files/folders:** `POS.Desktop/Assets/ui/*` (review); a short UI/UX sign-off note.
- **Dependencies:** Milestone 2.3; Milestone 2.6 (modernization applied).
- **Acceptance criteria:** Consistent refined white/light IMAGYN POS theme across screens; strong visual hierarchy; large touch targets; high readability; no demo/security-sensitive visible wording; full flow works at terminal resolution.
- **Risk notes:** Subtle DPI/zoom differences in WebView2; verify at the target terminal resolution, not a dev monitor.

### Milestone 2.5 — Font, icon & asset loading reliability
- **Purpose:** Ensure Google Fonts and Material Symbols load reliably (online now; bundling is Phase 8).
- **Expected output:** Confirmed font/icon loading behavior, with a documented offline-degradation note.
- **Files/folders:** `POS.Desktop/Assets/ui/*` (observe only), shell network/log output.
- **Dependencies:** Milestone 2.4.
- **Acceptance criteria:** Fonts/icons render when online; a clear record of what degrades when offline (drives the Phase 8 bundling milestone); no console 404s for local assets.
- **Risk notes:** Do **not** rewrite `<link>`s to local fonts yet — that's Phase 8. This milestone only characterizes the dependency.

### Milestone 2.6 — Refined white/light UI cleanup
- **Purpose:** Keep the approved white/light IMAGYN POS theme while applying production UI/UX cleanup that removes demo/security/simulator artifacts.
- **Expected output:** All 7 screens retain the refined white/light theme, production-safe copy, readable modals/forms/keypads/tables, and every JS hook preserved. The discarded dark `app.css` theme is not active.
- **Files/folders:** all 7 screen `.html` files in `docs/ui-prototype/screens/` and `POS.Desktop/Assets/ui/`.
- **Dependencies:** Milestones 2.1–2.3.
- **Acceptance criteria:** No `app.css` dark theme is active; all screens remain synchronized between `docs/` and `Assets/ui/`; no Tailwind CDN, demo shortcut bars, visible PIN hints, or simulator coupling are reintroduced; build succeeds.
- **Risk notes:** Do not accidentally revert earlier production freeze/copy/security wording improvements while removing only the dark theme layer.

---

## Phase 3 — Replace fake browser state (establish the bridge)

**Phase objective:** Stand up a clean JS↔C# bridge, move state ownership out of the browser, and prove the seam end-to-end with the login PIN round-trip.

### Milestone 3.1 — Bridge transport foundation
- **Purpose:** Establish the two-way channel between WebView2 content and C#.
- **Expected output:** `postMessage` → `WebMessageReceived` working both ways; a host object exposed at `window.chrome.webview.hostObjects.pos`.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`, new `POS.Desktop/Shell/PosHostApi.cs`.
- **Dependencies:** Phase 2 complete.
- **Acceptance criteria:** A trivial "ping" from JS reaches C# and a response renders back in the page; host object is reachable from any hosted screen.
- **Risk notes:** Host-object COM visibility/threading rules; messages arrive on the UI thread and must marshal correctly to async services.

### Milestone 3.2 — Bridge contract & message envelope
- **Purpose:** Define a single, typed request/response envelope (with error model) so JS and C# can't silently drift.
- **Expected output:** A versioned envelope (`type`, `requestId`, `payload`, `ok`, `error`) and JSON serialization conventions, plus a tiny JS client helper used by screens.
- **Files/folders:** new `POS.Desktop/Bridge/*` (DTOs, envelope, serializer settings); a small shared JS helper inside `Assets/ui/` `<script>` scope.
- **Dependencies:** Milestone 3.1.
- **Acceptance criteria:** Round-trips carry correlation IDs; malformed/unknown messages return a structured error, not a crash; serialization casing is fixed and documented.
- **Risk notes:** Establish camelCase/PascalCase policy once; mismatches are the most common bridge bug. Keep the JS helper thin (transport only, no logic).

### Milestone 3.3 — Message router & service dispatch
- **Purpose:** Route inbound messages to the correct C# handler/service.
- **Expected output:** `PosWebMessageRouter` maps message `type` → handler, resolves services from the host, and returns enveloped responses.
- **Files/folders:** new `POS.Desktop/Shell/PosWebMessageRouter.cs`; host registrations in `App.xaml.cs`/`Configuration/`.
- **Dependencies:** Milestones 3.1, 3.2.
- **Acceptance criteria:** Registering a new handler is a one-line addition; unknown `type` → structured "unsupported" error; handler exceptions become structured errors with logging, never an unhandled crash.
- **Risk notes:** Async dispatch + DI scope per request; ensure a `PosLocalDbContext` scope is created/disposed per message.

### Milestone 3.4 — Operator session service
- **Purpose:** Own current-operator/session state in C# (replacing `localStorage.terminal_operator`).
- **Expected output:** `ISessionService` holding current operator + login time, queryable over the bridge.
- **Files/folders:** new `POS.Desktop/Services/Session/*`; host registration.
- **Dependencies:** Milestone 3.3.
- **Acceptance criteria:** Screens can read "who is logged in" from the bridge; session clears on logout/shift close; no operator identity stored in the browser.
- **Risk notes:** Session is process state for now (single terminal, single operator); don't over-engineer multi-user concurrency.

### Milestone 3.5 — Swap browser state for bridge (login PIN proof)
- **Purpose:** Replace `localStorage`/`sessionStorage` usage on the login path with bridge calls, proving the whole seam.
- **Expected output:** `terminal_login.html` validates PIN via the bridge (stubbed validator acceptable here) and sets session through `ISessionService`; no storage writes on that path.
- **Files/folders:** `POS.Desktop/Assets/ui/terminal_login.html` (`<script>` only), `Services/Session/*`, `Bridge/*`.
- **Dependencies:** Milestone 3.4.
- **Acceptance criteria:** Login no longer reads/writes `localStorage`/`sessionStorage`; PIN decision comes from C#; success transitions to `shift_open`; the in-JS `operators[]`/PIN check is removed from the login path.
- **Risk notes:** Real `Employee`-backed validation is Phase 4/5; here a deterministic stub is fine, but it must already flow through the bridge so Phase 5 only swaps the validator body.

---

## Phase 4 — Connect SQLite / local services (real data in)

**Phase objective:** Make provisioning real so tenant filters resolve, then feed screens real catalog data from SQLite.

### Milestone 4.1 — Real provisioned-terminal context
- **Purpose:** Replace `NoProvisionedTerminalContext` so the local DB's tenant query filters return data.
- **Expected output:** A real `IProvisionedTerminalContext` populated from a persisted provisioning record, registered in the host.
- **Files/folders:** new `POS.Desktop/Services/Provisioning/*`; `App.xaml.cs` registration; `Data/PosLocalDbContext.cs` (consumer of the context).
- **Dependencies:** Phase 3 complete.
- **Acceptance criteria:** When provisioned, queries scoped by tenant/location/terminal return rows; when not provisioned, behavior stays fail-closed; the context is resolved consistently across DB scopes.
- **Risk notes:** This is the lynchpin — nothing local reads correctly until it's right. Guard against a half-provisioned state.

### Milestone 4.2 — Provisioning persistence & screen wiring
- **Purpose:** Make `provision_terminal.html` perform a real provisioning that survives restart.
- **Expected output:** Provisioning service persists tenant/location/terminal; the screen drives it over the bridge with real step reporting (no `setTimeout`); `terminal_config` localStorage removed.
- **Files/folders:** `POS.Desktop/Services/Provisioning/*`, `POS.Desktop/Assets/ui/provision_terminal.html` (`<script>` only), local persistence (DB or settings).
- **Dependencies:** Milestone 4.1.
- **Acceptance criteria:** After provisioning, restart keeps the terminal provisioned; re-provisioning is possible/controlled; progress UI reflects real steps; no fake delays.
- **Risk notes:** Catalog seeding from POS.Api isn't available yet — provisioning must succeed without it (seed handled in 4.3).

### Milestone 4.3 — Minimal local catalog schema & seed
- **Purpose:** Provide enough local catalog data for offline checkout before sync exists.
- **Expected output:** Local representation of catalog (items/categories/prices/identifiers/tax) and a minimal seed routine.
- **Files/folders:** `POS.Desktop/Data/*` (entities/config/migration as needed), `POS.Shared` model reuse, a seed source under `POS.Desktop/`.
- **Dependencies:** Milestones 4.1, 4.2.
- **Acceptance criteria:** A small, realistic catalog exists locally post-provision; seed is idempotent; data is tenant-scoped correctly.
- **Risk notes:** Decide where catalog lives locally (mirror of `POS.Shared` central entities vs local tables). Keep the seed small; full sync is Phase 6.

### Milestone 4.4 — Catalog read service (replace `ITEMS[]`)
- **Purpose:** Serve the checkout catalog from SQLite instead of the JS demo array.
- **Expected output:** `ICatalogService` with list/search/by-identifier reads, exposed over the bridge; `main_checkout.html` renders real items/categories.
- **Files/folders:** new `POS.Desktop/Services/Catalog/*`, `Bridge/*` DTOs, `POS.Desktop/Assets/ui/main_checkout.html` (`<script>` only).
- **Dependencies:** Milestones 4.3, 3.3.
- **Acceptance criteria:** Product grid, category chips, and search/scan-by-code all read from the service; `ITEMS[]`/`CATEGORIES[]` removed from checkout; prices/tax come from data, not JS.
- **Risk notes:** Search performance on larger catalogs; index identifiers/SKUs. Keep cart/order logic out of this milestone (that's Phase 5).

### Milestone 4.5 — Data-access conventions & tenant-filter validation
- **Purpose:** Lock in repository/service patterns and prove tenant isolation before flows are built on top.
- **Expected output:** Documented data-access conventions (scoping, async, disposal) and tests confirming filters behave provisioned vs unprovisioned.
- **Files/folders:** `POS.Desktop/Services/*` (conventions), `POS.Tests/*` (integration tests against SQLite).
- **Dependencies:** Milestones 4.1–4.4.
- **Acceptance criteria:** Integration tests show provisioned reads return data and unprovisioned reads return nothing; per-message DB scope verified; conventions written down for Phase 5 authors.
- **Risk notes:** Without this guardrail, Phase 5 services may each reinvent scoping and leak tenants.

---

## Phase 5 — Real flows (login, shift, order, payment, cash control, Z-report)

**Phase objective:** Deliver an end-to-end shift on real SQLite data: login → open shift → sell → pay → cash control → close + Z-report, with append-only persistence and outbox/print enqueue.

### Milestone 5.1 — Authentication & login service
- **Purpose:** Validate operator PIN against `Employee` and open a `TerminalSession`.
- **Expected output:** `IAuthService` performs real PIN validation for the provisioned location; success creates a `TerminalSession` and sets `ISessionService`.
- **Files/folders:** new `POS.Desktop/Services/Auth/*`, `Services/Session/*`, `terminal_login.html` (`<script>` only), `POS.Shared` (`Employee`, `TerminalSession`).
- **Dependencies:** Phase 4 complete; Milestone 3.5 (login already on the bridge).
- **Acceptance criteria:** Valid PIN → session + navigation to shift open; invalid PIN → existing error UI; lockout/empty-state handled; operator-grid + PIN-keypad UX unchanged (no username/password fields).
- **Risk notes:** PIN storage/comparison must be secure (hashing, no plaintext); never log PINs.

### Milestone 5.2 — Shift open service
- **Purpose:** Create a real open `Shift` with declared float, unlocking POS operations.
- **Expected output:** `IShiftService.OpenShift` persists a `Shift` (open) with opening float; `shift_open.html` drives it over the bridge; `pos_shift_*` sessionStorage removed.
- **Files/folders:** new `POS.Desktop/Services/Shifts/*`, `shift_open.html` (`<script>` only), `POS.Shared` (`Shift`).
- **Dependencies:** Milestone 5.1.
- **Acceptance criteria:** Opening a shift persists it and unlocks checkout; reopening guards against double-open; float captured accurately; checklist/policy values sourced from config.
- **Risk notes:** Define what "unlocked" means app-wide (gate other screens on an open shift).

### Milestone 5.3 — Order / cart service
- **Purpose:** Build a draft order with lines, discounts, and C#-computed tax/totals.
- **Expected output:** `IOrderService` manages a draft (add/qty/remove/discount); totals + tax computed via `TaxRule`; `main_checkout.html` reflects authoritative numbers; `pos_cart` removed.
- **Files/folders:** new `POS.Desktop/Services/Orders/*`, `main_checkout.html` (`<script>` only), `POS.Shared` (`Order`, `OrderLine`, `ItemPrice`, `TaxRule`).
- **Dependencies:** Milestones 5.2, 4.4.
- **Acceptance criteria:** Cart math (subtotal, discount, tax, grand total) matches expectations and is computed server-side; line edits round-trip; "Pay" hands a consistent draft to payment.
- **Risk notes:** Money rounding and tax-rule edge cases; centralize rounding. Draft persistence strategy (in-memory vs DB) decided here.

### Milestone 5.4 — Payment & completion service
- **Purpose:** Take tender, compute change, and commit the sale append-only with side-effects enqueued.
- **Expected output:** `IPaymentService` records `Payment` per `TenderMethod`, computes cash change, and on completion writes `Order`/`OrderLine`/`Payment`, enqueues `SyncOutbox` + `PrintQueue`, clears the draft; `payment_screen.html` wired; hardcoded receipt removed.
- **Files/folders:** new `POS.Desktop/Services/Payments/*`, `Services/Reporting`/receipt rendering, `payment_screen.html` (`<script>` only), `POS.Shared` (`Payment`, `TenderMethod`, `ReceiptTemplate`).
- **Dependencies:** Milestone 5.3.
- **Acceptance criteria:** Cash/card/wallet/split tenders supported; change correct; completed sale is append-only and idempotent; outbox + print rows created; card/wallet call a stubbed hardware path (no `setTimeout`).
- **Risk notes:** Idempotency on completion (avoid double-charge on retry); receipt content rendered from data, not hardcoded. Real pinpad is Phase 7.

### Milestone 5.5 — Cash control service
- **Purpose:** Real safe drops / float injections with manager authorization and a live ledger.
- **Expected output:** `ICashControlService` writes `CashDrawerMovement` (with `ReasonCode` + manager PIN check); threshold alerts computed in C#; `cash_control.html` ledger is a query; `pos_safe_drops` removed.
- **Files/folders:** new `POS.Desktop/Services/CashControl/*`, `cash_control.html` (`<script>` only), `POS.Shared` (`CashDrawerMovement`, `ReasonCode`).
- **Dependencies:** Milestone 5.2 (shift must be open).
- **Acceptance criteria:** Drops/injections persist and update drawer balance; manager PIN enforced; over-limit alert fires from C#; ledger reflects real movements.
- **Risk notes:** Manager-authorization reuse from `IAuthService`; ensure movements are tied to the active shift.

### Milestone 5.6 — Shift close & Z-report
- **Purpose:** Reconcile, produce a real `ZReport`, and lock the terminal back to login.
- **Expected output:** `IShiftService.CloseShift` computes expected cash (`opening + cash sales − drops + injections`) and variance from counted denominations; `IReportingService` builds a `ZReport` from real data; `shift_close.html` wired; FBR submission is a stubbed integration point.
- **Files/folders:** `POS.Desktop/Services/Shifts/*`, new `POS.Desktop/Services/Reporting/*`, `shift_close.html` (`<script>` only), `POS.Shared` (`Shift`, `ZReport`, `CashDrawerMovement`).
- **Dependencies:** Milestones 5.4, 5.5.
- **Acceptance criteria:** Denomination count → counted cash; variance correct and color-coded as in the prototype; `ZReport` persisted from real `Order`/`Payment`/movement data; close locks to login; `DENOMS[]` demo metrics removed.
- **Risk notes:** Variance/reconciliation correctness is audit-sensitive; cover with unit tests. FBR is out of scope here (defined point only).

---

## Phase 6 — Sync / outbox ↔ POS.Api

**Phase objective:** Replicate offline-created records to the central API when connectivity allows, without ever blocking the terminal.

### Milestone 6.1 — POS.Api sync ingest endpoint (server side)
- **Purpose:** Build the currently-empty central `Sync/` ingest so the terminal has a target.
- **Expected output:** An authenticated ingest endpoint that accepts outbox events and acks via `SyncIngestAck`.
- **Files/folders:** `POS.Api/Sync/*` (new), `POS.Api/Controllers/*`, `POS.Api/Program.cs` (route/policy), `POS.Shared` (`SyncIngestAck`, shared DTOs).
- **Dependencies:** Phase 5 complete (there are events worth syncing).
- **Acceptance criteria:** Endpoint accepts a batch, persists/acks idempotently, and rejects unauthorized callers; duplicate event IDs are no-ops.
- **Risk notes:** Server-side idempotency is critical; design the ack/dedupe key before the client sends anything.

### Milestone 6.2 — Device-authenticated HTTP client
- **Purpose:** Give the terminal a typed client that authenticates as a device.
- **Expected output:** A typed `HttpClient` using the `PosDevice` JWT policy, with token acquisition/refresh.
- **Files/folders:** new `POS.Desktop/Services/Sync/*` (client), host registration, `appsettings.json` (API base URL).
- **Dependencies:** Milestone 6.1.
- **Acceptance criteria:** Client obtains a device token and calls the ingest endpoint successfully; expired tokens refresh transparently; failures surface as typed results, not exceptions.
- **Risk notes:** Token storage/refresh and clock skew; never block UI on auth.

### Milestone 6.3 — Outbox drain processor
- **Purpose:** Continuously drain `SyncOutbox` to the API and advance the cursor.
- **Expected output:** A background `SyncProcessor` that batches outbox rows, posts them, marks them sent, and advances `SyncCursor`.
- **Files/folders:** `POS.Desktop/Services/Sync/*`, `Data/LocalEntities/SyncOutbox.cs` + `SyncCursor.cs` (consumers).
- **Dependencies:** Milestone 6.2.
- **Acceptance criteria:** Events created during a shift appear centrally shortly after; cursor advances monotonically; processor runs without blocking the UI thread.
- **Risk notes:** Ordering and batch-size tuning; ensure the processor pauses cleanly on shutdown.

### Milestone 6.4 — Retry, recovery & reconciliation
- **Purpose:** Make sync resilient to failures using the existing recovery tables.
- **Expected output:** Retry with backoff via `LocalRecoveryJournal`; payment reconciliation via `PaymentReconciliationQueue`.
- **Files/folders:** `POS.Desktop/Services/Sync/*`, `Data/LocalEntities/LocalRecoveryJournal.cs`, `PaymentReconciliationQueue.cs`.
- **Dependencies:** Milestone 6.3.
- **Acceptance criteria:** Transient failures retry and eventually succeed; poison events are quarantined, not retried forever; payment reconciliation closes the loop.
- **Risk notes:** Backoff/poison-message policy must avoid hot loops; bound retries.

### Milestone 6.5 — Connectivity handling & sync observability
- **Purpose:** Detect online/offline transitions and make sync state visible.
- **Expected output:** Connectivity awareness driving processor activity; minimal sync status/metrics surfaced.
- **Files/folders:** `POS.Desktop/Services/Sync/*`, optional bridge status for the UI, logging config.
- **Dependencies:** Milestone 6.4.
- **Acceptance criteria:** Going offline pauses attempts gracefully; coming online resumes; pending/last-synced counts are observable; offline operation is unaffected.
- **Risk notes:** Don't let connectivity checks themselves block; keep them cheap and out of the UI thread.

---

## Phase 7 — Hardware integration

**Phase objective:** Define hardware abstractions, ship safe no-op stubs by default, then wire real devices behind config — driven by services, never the UI.

### Milestone 7.1 — Hardware abstraction contracts
- **Purpose:** Define the device interfaces in the (currently empty) hardware project.
- **Expected output:** `IReceiptPrinter`, `ICashDrawer`, `IBarcodeScanner`, `IPaymentTerminal`, `ICustomerDisplay` interfaces with clear method/result contracts.
- **Files/folders:** `POS.Desktop.Hardware/{Printers,CashDrawer,Scanner,PaymentTerminal,CustomerDisplay}/*`, `POS.Shared` (shared result/enum types as needed).
- **Dependencies:** Phase 5 complete (flows that will call hardware exist).
- **Acceptance criteria:** Interfaces compile and express real device operations; no concrete dependencies leak into contracts; results are typed/awaitable.
- **Risk notes:** Avoid over-abstracting for hypothetical devices; cover the five known device types only.

### Milestone 7.2 — Stub implementations, config selection & DI
- **Purpose:** Let the app run with no physical devices and select implementations by configuration.
- **Expected output:** No-op/console stubs for each interface; config-driven registration in the host.
- **Files/folders:** `POS.Desktop.Hardware/*` (stubs), `POS.Desktop/App.xaml.cs`/`Configuration/*`, `appsettings.json` (device selection).
- **Dependencies:** Milestone 7.1.
- **Acceptance criteria:** With default config the full flow runs using stubs; switching a device type in config swaps the implementation without code changes; stubs log their calls.
- **Risk notes:** Keep stub vs real selection explicit so a misconfigured terminal fails loudly, not silently.

### Milestone 7.3 — Receipt printing (drain `PrintQueue`)
- **Purpose:** Turn queued print jobs into real receipts.
- **Expected output:** A printer consumer that drains `PrintQueue` and renders receipts via `IReceiptPrinter` (ESC/POS for the first real driver).
- **Files/folders:** `POS.Desktop.Hardware/Printers/*`, `POS.Desktop/Services/*` (queue consumer), `Data/LocalEntities/PrintQueue.cs`.
- **Dependencies:** Milestones 7.2, 5.4 (payment enqueues prints).
- **Acceptance criteria:** Completing a sale produces a printed (or stub-logged) receipt; failed prints retry/queue; receipt content matches the rendered template.
- **Risk notes:** Printer driver variance (ESC/POS dialects); start with one known model.

### Milestone 7.4 — Barcode scanner → checkout
- **Purpose:** Feed scanned codes into the checkout flow.
- **Expected output:** Scanner input resolves via `ItemIdentifier` and adds to the cart through `IOrderService`.
- **Files/folders:** `POS.Desktop.Hardware/Scanner/*`, `POS.Desktop/Services/{Catalog,Orders}/*`, `main_checkout.html` (input handling, `<script>` only if needed).
- **Dependencies:** Milestones 7.2, 5.3, 4.4.
- **Acceptance criteria:** A scan adds the right item; unknown codes show the existing not-found UX; HID/keyboard-wedge scanners work without app changes.
- **Risk notes:** Keyboard-wedge scanners emit keystrokes — ensure focus handling doesn't double-enter or steal numpad input.

### Milestone 7.5 — Cash drawer & customer display
- **Purpose:** Wire the drawer kick and the secondary customer display.
- **Expected output:** `ICashDrawer` opens on cash payment / explicit action; `ICustomerDisplay` mirrors cart/total.
- **Files/folders:** `POS.Desktop.Hardware/{CashDrawer,CustomerDisplay}/*`, `POS.Desktop/Services/{Payments,Orders}/*`.
- **Dependencies:** Milestones 7.2, 5.4.
- **Acceptance criteria:** Drawer kicks at the right moments (and is auditable); customer display reflects live cart/total; both degrade to stubs cleanly when absent.
- **Risk notes:** Drawer-open events should be logged for cash accountability; second-display resolution/availability varies.

### Milestone 7.6 — Payment terminal (pinpad) integration
- **Purpose:** Replace the fake card/wallet flow with a real payment terminal.
- **Expected output:** `IPaymentTerminal` drives card/wallet authorization; `IPaymentService` consumes real results instead of the stub.
- **Files/folders:** `POS.Desktop.Hardware/{PaymentTerminal,Gateway}/*`, `POS.Desktop/Services/Payments/*`, `payment_screen.html` (status rendering, `<script>` only).
- **Dependencies:** Milestones 7.2, 5.4.
- **Acceptance criteria:** Card/wallet payments authorize through the terminal; approvals/declines reflected in the existing UI; reconciliation/`Payment` records reflect real auth data.
- **Risk notes:** Real pinpad SDKs and certification are vendor-specific; keep the interface stable so the gateway can change without touching flows.

---

## Phase 8 — Production hardening

**Phase objective:** Make the terminal deployable: reliable runtime, locked-down kiosk, observable, offline-complete, and packaged — with the prototype/simulator retired.

### Milestone 8.1 — WebView2 runtime bootstrap & install handling
- **Purpose:** Guarantee the WebView2 runtime is present on every terminal.
- **Expected output:** A bootstrap/install check (and remediation path) for the Evergreen runtime, integrated with startup.
- **Files/folders:** `POS.Desktop/Shell/*` (extends 1.5 guard), installer/bootstrap scripts.
- **Dependencies:** Phases 1–7 complete.
- **Acceptance criteria:** A clean machine ends up with a working runtime via the documented path; missing runtime never produces a silent failure.
- **Risk notes:** Offline install scenarios for the runtime; bundle or pre-stage where required.

### Milestone 8.2 — Kiosk lockdown & security hardening
- **Purpose:** Prevent operators from escaping the app or reaching dev tooling.
- **Expected output:** Disabled dev tools/context menu/zoom/navigation-to-external; restricted to the virtual host; window can't be minimized/closed casually.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`, `MainWindow.xaml(.cs)`, settings.
- **Dependencies:** Milestone 8.1.
- **Acceptance criteria:** No navigation outside `pos.app`; no dev tools/context menu in production; only trusted local content loads; remote script blocked.
- **Risk notes:** Balance lockdown with supportability — provide a guarded support/exit gesture.

### Milestone 8.3 — Error handling, logging & telemetry
- **Purpose:** Make failures diagnosable in the field.
- **Expected output:** Structured logging across shell/bridge/services, global exception handling, and basic telemetry/log rotation.
- **Files/folders:** `POS.Desktop/Shell/*`, `Services/*`, `Bridge/*`, `appsettings.json` (logging).
- **Dependencies:** Milestone 8.2.
- **Acceptance criteria:** Unhandled exceptions are caught, logged, and shown gracefully; bridge errors carry correlation IDs; logs rotate and never store secrets/PINs.
- **Risk notes:** Don't log sensitive data (PINs, card data, tokens); scrub payloads.

### Milestone 8.4 — Offline asset bundling (fonts/icons)
- **Note (Phase 2 freeze, 2026-05-25):** the login screen's **Tailwind CDN was already removed** and inlined as local utility CSS during the Phase 2 production UI freeze, so this milestone now covers only the remaining **Google Fonts + Material Symbols** offline bundling.
- **Purpose:** Remove the runtime dependency on Google Fonts/Material Symbols for kiosk reliability.
- **Expected output:** Fonts/icons bundled under `Assets/ui/fonts/`; `<link>`s switched to local `@font-face` — **no visual change**.
- **Files/folders:** `POS.Desktop/Assets/ui/fonts/*`, `Assets/ui/*` (`<link>`/`@font-face` only).
- **Dependencies:** Milestone 2.5 (dependency characterized).
- **Acceptance criteria:** Screens render identically with networking disabled; no external font/icon requests; parity re-verified against the prototype.
- **Risk notes:** Font licensing for redistribution; confirm before bundling. This is the one allowed CSS touch (font source only, not appearance).

### Milestone 8.5 — FBR fiscal integration point
- **Purpose:** Provide a real, stubbed seam for FBR fiscal submission used by payment/Z-report.
- **Expected output:** An `IFiscalService` (stub by default) invoked at completion/close, with config to enable a real provider later.
- **Files/folders:** new `POS.Desktop/Services/*` (fiscal), `Services/{Payments,Reporting}/*` (call sites), `appsettings.json`.
- **Dependencies:** Milestones 5.4, 5.6.
- **Acceptance criteria:** Completion/close call the fiscal seam; the stub records intent without external calls; enabling a real provider requires no flow changes.
- **Risk notes:** FBR compliance specifics are external/regulatory — keep the contract generic until requirements are confirmed.

### Milestone 8.6 — Packaging, installer & prototype cleanup
- **Purpose:** Produce a deployable artifact and retire the now-redundant prototype/simulator.
- **Expected output:** An installer/package; `index.html` simulator removed; duplicate/stale design folders cleaned per §11 after parity is confirmed.
- **Files/folders:** packaging config, `docs/ui-prototype/*` (retire after parity), repo cleanup.
- **Dependencies:** Milestones 8.1–8.5; Phase 2 parity confirmed.
- **Acceptance criteria:** A fresh install runs the full flow; `Assets/ui/` is the single UI source; `index.html` no longer ships; a documented scan precedes any folder deletion; prototype removed only after in-app parity is signed off.
- **Risk notes:** Don't delete `docs/ui-prototype/*` prematurely; gate on confirmed parity. Installer signing/runtime prerequisites must be verified.

---

## Milestone dependency map (high level)

```
P1 (shell) ─► P2 (screens) ─► P3 (bridge) ─► P4 (data) ─► P5 (flows) ─► P6 (sync)
                                                              │
                                                              └─► P7 (hardware)
                                                                       │
P8 (hardening) ◄───────────────────────────────────────────── all of the above
```

- Phases are largely sequential; **P6 (sync)** and **P7 (hardware)** can proceed in parallel once **P5** lands.
- **P8** items are mostly cross-cutting and finish last, except **8.4** which only needs **2.5**.

## Next step (not in this document)

Each milestone above will get its own file decomposing it into **exactly 10 implementation tasks** (e.g., `MILESTONE_1.1_TASKS.md`). Those task files are out of scope here — this document defines the milestone boundaries they must fit inside.
