# Desktop UI Integration Plan

**Project:** IMAGYN POS — converting the HTML prototype screens into the real `POS.Desktop` terminal application
**Author:** Senior .NET Desktop Architecture review
**Status:** Planning document — **no implementation code yet**
**Last updated:** 2026-05-23

---

## 1. Context — Why this document exists

`POS.Desktop` is the Windows terminal for a multi-tenant, offline-first POS platform. Today the project compiles and runs but shows **nothing useful**: `MainWindow.xaml` is an empty `<Grid/>`. Meanwhile, a complete, polished UI already exists — but only as an **HTML prototype** under `docs/ui-prototype/screens/*` (7 screens, ~6,690 lines, 100% inline CSS/JS, full IMAGYN branding). All of its behaviour is fake: demo arrays, `localStorage`/`sessionStorage`, and `setTimeout` simulations.

The goal is to turn those exact prototype screens into the real terminal **without redesigning them**, and to progressively replace the fake browser logic with real C# services backed by local SQLite and the `POS.Shared` domain model — then later wire sync to `POS.Api` and hardware via `POS.Desktop.Hardware`.

> **Prime directive:** The final desktop UI must look and behave like `docs/ui-prototype/screens/*` — same theme, colors, layout, spacing, buttons, branding, flow. We do **not** redesign. We change *logic*, not *appearance*.
>
> `docs/ui-prototype/index.html` is **only a simulator/launcher** (an iframe wrapper). It is **not** the app design. The individual screen files are the source of truth.

> **Architecture Note:** `POS.Desktop` targets `net8.0-windows` and uses **WebView2 (SDK v1.0.2903.40)** to host the existing HTML prototype as the production UI. This ensures visual parity and avoids a costly redesign. Deployment requires the **Microsoft Edge WebView2 Evergreen Runtime** on all terminals. This dependency is strictly for the Desktop UI shell, not the backend API.

---

## 2. Verified current state (ground truth)

### Solution layout (`POS.slnx`)

| Project | Type / TFM | Role | Current state |
|---|---|---|---|
| **POS.Api** | ASP.NET Core `net8.0`, SQL Server | Central backend | `PosCentralDbContext` (30+ DbSets), JWT auth (policies `UserOrAdmin`, `PosDevice`, `SystemScope`), 5 **read-only** controllers (Health, Categories, Locations, Tenant, UoM). `Sync/` folder exists but **empty**. |
| **POS.Desktop** | WPF `net8.0-windows` WinExe | Windows terminal | **Blank `MainWindow`**. Manual `ServiceCollection` DI. SQLite `PosLocalDbContext` (6 local entities). EF migration present. **No WebView2, no Blazor.** |
| **POS.Shared** | Class lib `net8.0` | Domain models/contracts | 37 entities, 70+ enums, 2 contracts. No external packages. |
| **POS.Desktop.Hardware** | Class lib `net8.0` | Hardware abstraction | **Empty stub folders only:** `CashDrawer/`, `CustomerDisplay/`, `Gateway/`, `PaymentTerminal/`, `Printers/`, `Scanner/`. No `.cs` files. |
| **POS.Tests** | xUnit `net8.0` | Tests | References POS.Api + POS.Shared. |

### POS.Desktop specifics
- `App.xaml.cs` builds a hand-rolled `ServiceCollection` (not Generic Host) and registers:
  - `IProvisionedTerminalContext` → `NoProvisionedTerminalContext` (fail-closed; returns invalid tenant/location/terminal IDs).
  - `PosLocalDbContext` with SQLite, connection string from `LocalDatabaseConfigurationGuard.GetRequiredConnectionString()` (reads `appsettings.json` → `ConnectionStrings:LocalDatabase`).
- `PosLocalDbContext` local entities (all `LocalOperationalEntity`): `SyncOutbox`, `PrintQueue`, `LocalRecoveryJournal`, `PaymentReconciliationQueue`, `SyncCursor`, `LocalRetentionState`. Each has a tenant query filter keyed off `IProvisionedTerminalContext` — **so nothing is queryable until provisioning is real.**
- One migration: `20260518194918_Local_InitialPhase1Schema`. Design-time factory present.

### POS.Shared domain model (the real targets)
Master data: `Company`, `Location`, `Terminal`, `Employee`, `Customer`, `Category`, `Item` (+ `ItemVariant`, `ItemIdentifier`, `ItemPrice`, `ItemStock`), `UnitOfMeasure`, `TenderMethod`, `TaxRule`, `PriceList`, `ReceiptTemplate`, `ReasonCode`, `EmployeeLocationRole`, `CashAccount`.
Operations: `Order`, `OrderLine`, `Payment`, `Shift`, `TerminalSession`, `ZReport`, `CashDrawerMovement`, `CashAccountMovement`, `InventoryMovement`, `ManagerAction`, `SyncIngestAck`.
Contracts: `IProvisionedTerminalContext`, `ICurrentTenantContext`.

### Prototype reality (`docs/ui-prototype/`)
- 7 screens, flat in `screens/`, plus `logo.png`. `index.html` is the simulator wrapper.
- **Theme** (consistent across screens, all inline CSS): green `#A8E63D` (primary/CTA), charcoal `#202020`, surface `#F7F7F7`, amber `#F59E0B`, red `#EF4444`. Fonts: **Space Grotesk** (headings), **Inter Tight** (body), **IBM Plex Mono** (numbers/receipts). **Material Symbols Outlined** icons. Google Fonts via `<link>`.
- **State (fake):**
  - `localStorage`: `terminal_config`, `terminal_operator`.
  - `sessionStorage`: `pos_shift_open`, `pos_shift_float`, `pos_cart`, `pos_completed_transactions`, `pos_safe_drops`, `pos_last_receipt`.
- **Demo data baked into JS:** `operators[]` (with PINs), `ITEMS[]` + `CATEGORIES[]`, `DENOMS[]`, hardcoded receipt + Z-report text, `setTimeout` fake provisioning/card/wallet flows.
- **Flow:** `provision_terminal` → `terminal_login` → `shift_open` → `main_checkout` → `payment_screen` → `cash_control` → `shift_close` → back to `terminal_login`.

---

## 3. Architecture decision — how to host the UI

We were asked to evaluate four options. Summary verdict first, rationale after.

| Option | Verdict | Why |
|---|---|---|
| **1. Pure WPF native UI** | ❌ Reject | Reproducing numpads, modals, animations, glassmorphism, custom Google fonts, and exact spacing in XAML cannot match the approved design pixel-for-pixel. Discards ~6,690 lines of finished UI and costs weeks — incompatible with the deadline and the "do not redesign" directive. |
| **2. WPF + WebView2 (host the existing HTML)** | ✅ **Core mechanism** | Renders the *actual* prototype files → **zero visual drift**. Mature JS↔C# seam: `CoreWebView2.SetVirtualHostNameToFolderMapping(...)` to serve screens, `WebMessageReceived` / `AddHostObjectToScript` to call C#. |
| **3. WPF + BlazorWebView** | ❌ Reject (for now) | Requires porting all markup to `.razor` and re-wiring CSS — that *is* a UI rewrite, i.e. redesign risk + slower. Blazor's benefit (C# in markup) is moot when finished markup already exists and must be preserved. Keep as an optional future north-star only. |
| **4. Phased: HTML first, then swap fakes for services** | ✅ **Recommended strategy** | This is the *process*, realized on top of Option 2. Ship a working terminal quickly, preserve identity, and migrate business logic into testable C# services incrementally — no UI churn. |

### Recommendation
**Adopt Option 4 (phased), implemented with Option 2 (WPF + WebView2).**

Keep the prototype HTML as the **View layer**, hosted in a single WebView2 control inside a full-screen WPF shell. Stand up a small **JS↔C# bridge**. Then, screen by screen, delete the fake JS (`localStorage`/`sessionStorage`/`setTimeout`/demo arrays) and route those actions through the bridge into real C# services that use `PosLocalDbContext` (SQLite) and `POS.Shared` models.

### WebView2 vs BlazorWebView (explicit answer)
**WebView2, because the design is the binding constraint.** We must preserve a finished HTML/CSS/JS UI exactly; WebView2 runs it unchanged. BlazorWebView would force a rewrite of that same UI into Razor components and a CSS re-host — effort spent re-creating what we already have, with a real risk of visual divergence. BlazorWebView only becomes attractive in a *future* greenfield phase where the team wants to retire JS and own the UI entirely in C#. Not now.

---

## 4. What is what — implemented vs fake vs real-to-build vs UI-only

### 4.1 Already implemented (reuse, don't rebuild)
- SQLite `PosLocalDbContext` + initial migration + design-time factory.
- 6 local operational entities for the outbox/sync/print/recovery patterns.
- DI scaffold in `App.xaml.cs` and the `LocalDatabaseConfigurationGuard` connection-string guard.
- `IProvisionedTerminalContext` contract and tenant query-filter wiring.
- The entire central domain model in `POS.Shared` (orders, payments, shifts, cash movements, etc.).
- POS.Api read endpoints + JWT auth (basis for later catalog seeding + sync).

### 4.2 Prototype / fake / demo only (to be removed or replaced)
- JS demo arrays: `operators[]` (+ PINs), `ITEMS[]`, `CATEGORIES[]`, `DENOMS[]`.
- All `localStorage`/`sessionStorage` reads & writes.
- `setTimeout`-driven fake provisioning progress, fake card approval, fake wallet confirmation.
- Hardcoded receipt and Z-report text; random "FBR" reference codes.
- `index.html` simulator and its per-screen "demo shortcut" bars.

### 4.3 Should become real business logic (C# services + SQLite + POS.Shared)
Terminal provisioning · operator PIN login · shift open/close + Z-report · cart/order build + tax/totals · payment/tender/change · cash control movements (safe drop / float) · sync/outbox · hardware I/O.

### 4.4 Should remain UI-only (stays in HTML/CSS/JS — no business rules)
Layout, theme, fonts, colors, spacing, animations/transitions, tab switching, modal show/hide, numpad **key capture** (input only), toasts, and **display formatting**. Authoritative numbers (totals, tax, change, variance, PIN validity) always come from C#; the UI only renders them.

---

## 5. Integration mechanics — how the screens enter POS.Desktop

1. **Add the WebView2 package** (`Microsoft.Web.WebView2`) to `POS.Desktop.csproj`.
2. **Bring the screens into the app:** copy `docs/ui-prototype/screens/*` + `logo.png` into `POS.Desktop/Assets/ui/`, marked as Content with `CopyToOutputDirectory`. (Keep the `docs/` originals as the canonical design reference until parity is confirmed — see §11 cleanup.)
3. **Serve them locally:** in the WebView2 host call
   `coreWebView2.SetVirtualHostNameToFolderMapping("pos.app", assetsUiPath, CoreWebView2HostResourceAccessKind.Allow);`
   then navigate to `https://pos.app/terminal_login.html`. This gives the HTML a stable origin (needed for fetch/host-object semantics) without a real web server.
4. **Navigate between screens** either by letting existing `window.location.href = '...html'` links resolve under the virtual host, or by routing navigation through the shell. The WPF shell **replaces `index.html`** as the launcher.
5. **Bridge:** register a host object (`window.chrome.webview.hostObjects.pos`) and/or use `postMessage` → `WebMessageReceived`. A `PosWebMessageRouter` dispatches typed requests to services and returns JSON responses.

### Preserving the exact design
- Host the HTML **unmodified**; the *only* edits are inside `<script>` blocks (replace fake logic with bridge calls). **Never touch markup, CSS, class names, or `logo.png`.**
- Keep the Google Fonts `<link>`s for now; for kiosk/offline reliability, **bundle the fonts** (Space Grotesk, Inter Tight, IBM Plex Mono, Material Symbols) under `Assets/ui/fonts/` and switch the `<link>`s to local `@font-face` in Phase 8 — a technical change with **no visual effect**.

---

## 6. Replacing browser state with real desktop state

| Prototype state | Becomes | Backed by |
|---|---|---|
| `localStorage.terminal_config` | `IProvisioningService` / app settings | persisted provisioning record + `IProvisionedTerminalContext` |
| `localStorage.terminal_operator` | `ISessionService` (current operator, login time) | in-memory app session + `TerminalSession` |
| `sessionStorage.pos_shift_open` / `pos_shift_float` | `IShiftService` | `Shift` (SQLite) |
| `sessionStorage.pos_cart` | `IOrderService` (draft order) | in-memory draft → `Order`/`OrderLine` on pay |
| `sessionStorage.pos_completed_transactions` | query of completed orders | `Order` + `Payment` rows |
| `sessionStorage.pos_safe_drops` | `ICashControlService` | `CashDrawerMovement` rows |
| `sessionStorage.pos_last_receipt` | `IReceiptService` | rendered from `Order`/`Payment` + `ReceiptTemplate` |

**Principle:** no business state lives in the browser. The UI asks the bridge for what it needs on load and pushes user actions to the bridge; C# owns the truth.

### Keeping logic out of the UI
The HTML must only (a) capture input, (b) call a bridge method, (c) render the response. All decisions — PIN correctness, tax, totals, allowed actions, variance — live in services. Demo arrays are deleted and replaced by bridge-fetched data. No `if (pin === '1111')` in JS; the service decides.

---

## 7. How each real flow should work

- **Provisioning (`provision_terminal`):** operator picks store/terminal code → `IProvisioningService` persists tenant/location/terminal, swaps `NoProvisionedTerminalContext` for a **real** `IProvisionedTerminalContext`, and (later) seeds the local catalog from POS.Api. The fake `setTimeout` progress log becomes real step reporting over the bridge. Until catalog seeding exists, seed a small local set so checkout works offline.
- **Login (`terminal_login`):** operator grid + 4-digit PIN keypad (unchanged UI). PIN is validated **server-side-of-the-bridge** against `Employee` for the provisioned location; on success open a `TerminalSession` and set `ISessionService`. *(Login UX stays operator-grid + PIN keypad — never username/password fields.)*
- **Shift open (`shift_open`):** declare opening float → create `Shift` (status open) with float; checklist/policy values come from config. Unlocks the POS.
- **Checkout (`main_checkout`):** catalog grid + search/scan from `ICatalogService` (replaces `ITEMS[]`). Cart is a **draft order**; line add/qty/remove/discount update via `IOrderService`; totals + tax computed by C# using `TaxRule`. "Pay" hands the draft to payment.
- **Payment (`payment_screen`):** `IPaymentService` records `Payment` per tender (`TenderMethod`), computes change for cash, and on completion writes the `Order` + `OrderLine` + `Payment` **append-only**, enqueues a `SyncOutbox` event, queues a `PrintQueue` receipt, and clears the draft. Card/wallet flows call the (stubbed) payment-terminal hardware instead of `setTimeout`.
- **Cash control (`cash_control`):** safe drop / float injection → `CashDrawerMovement` with reason code + manager PIN (validated via service). Threshold alerts (e.g. drawer > limit) computed by C#. Ledger is a query, not `sessionStorage`.
- **Shift close + Z-report (`shift_close`):** denomination count → counted cash; `IShiftService` computes expected cash (`opening + cash sales − drops + injections`) and variance; `IReportingService` produces a `ZReport` from real `Order`/`Payment`/`CashDrawerMovement` data; closing locks the terminal back to login. FBR submission is a **defined integration point**, stubbed for now.

---

## 8. Sync / outbox → POS.Api (later)

The local side already has the pieces: writes enqueue `SyncOutbox` events; `SyncCursor` tracks progress; `LocalRecoveryJournal` + `PaymentReconciliationQueue` cover retries/reconciliation. To complete the loop:
- Add a typed `HttpClient` (auth via the `PosDevice` JWT policy already in POS.Api).
- Add a `SyncProcessor` background service that drains `SyncOutbox`, posts to a **new** POS.Api `Sync/` ingest endpoint (currently an empty folder), and advances the cursor; the API acks via `SyncIngestAck`.
- Idempotency keys/correlation IDs on outbox events make retries safe. Everything stays offline-first: the terminal never blocks on the network.

---

## 9. Hardware integration (later) — `POS.Desktop.Hardware`

Define interfaces (the folders already exist; add the contracts), start with **no-op/console stubs** selected by config so the app runs without any device:

| Interface | Folder | First real wiring |
|---|---|---|
| `IReceiptPrinter` | `Printers/` | drain `PrintQueue` → ESC/POS receipt |
| `ICashDrawer` | `CashDrawer/` | kick drawer on cash payment / open-drawer action |
| `IBarcodeScanner` | `Scanner/` | scan → checkout add-to-cart by `ItemIdentifier` |
| `IPaymentTerminal` | `PaymentTerminal/` | replace fake card/wallet `setTimeout` with real pinpad |
| `ICustomerDisplay` | `CustomerDisplay/` | mirror cart/total to second display |

Hardware is invoked by services (not the UI). The scanner feeds the **checkout** flow; the printer is driven by the existing **`PrintQueue`**.

---

## 10. Risks

### Using HTML prototype screens as production UI
- **WebView2 Evergreen runtime dependency** — must be installed/bootstrapped on each terminal.
- **Cross-boundary debugging** — bugs span JS and C#; needs disciplined logging on the bridge.
- **Weaker compile-time safety** in the JS layer — mitigate by keeping JS thin (input + render only) and contract-testing the bridge.
- **Bridge/state complexity** — one clear request/response contract; no hidden state in the browser.
- **Security discipline** — only load trusted local content over the virtual host; no remote script; sanitize anything echoed into the DOM.
- **Offline asset packaging** — fonts/icons must be bundled for kiosk reliability.

### Rewriting everything in WPF
- **Schedule blowout** against the deadline.
- **Visual divergence** from the approved, signed-off design.
- **Re-implementing** numpads, modals, animations, exact spacing — high-effort, low-value.
- **Throwaway** of ~6,690 lines of finished UI.

---

## 11. Cleanup plan for duplicate prototype/design folders

Conservative and parity-gated:
1. Keep `docs/ui-prototype/screens/*` as the **canonical design reference** until Phase 2 parity is confirmed.
2. Promote the screens into `POS.Desktop/Assets/ui/` as the **production copy** (single source going forward).
3. **Retire `index.html`** (the simulator) — the WPF shell replaces it; it should not ship.
4. Only after in-app parity is verified, remove older/duplicate design-doc folders. **Run a repo scan first** to enumerate any stale design folders before deleting anything.
5. Do **not** remove the prototype until the real desktop shell renders all screens correctly.

---

## 12. Recommended final `POS.Desktop` folder structure

```
POS.Desktop/
├─ App.xaml(.cs)                 # Generic Host bootstrap + DI
├─ MainWindow.xaml(.cs)          # full-screen kiosk shell hosting WebView2
├─ Shell/
│  ├─ WebViewHost.cs             # WebView2 init, virtual-host mapping, navigation
│  ├─ PosHostApi.cs              # host object exposed to JS (window...hostObjects.pos)
│  └─ PosWebMessageRouter.cs     # postMessage → service dispatch → JSON response
├─ Bridge/                       # request/response DTOs + (de)serialization
├─ Services/
│  ├─ Provisioning/              # real IProvisionedTerminalContext + ProvisioningService
│  ├─ Session/                   # ISessionService (current operator)
│  ├─ Auth/                      # PIN login
│  ├─ Shifts/                    # open/close + variance
│  ├─ Orders/                    # cart/draft, totals, tax
│  ├─ Payments/                  # tender, change, completion
│  ├─ CashControl/               # safe drop / float
│  ├─ Reporting/                 # Z-report
│  ├─ Catalog/                   # local catalog read (replaces ITEMS[])
│  └─ Sync/                      # outbox processor + HttpClient (later)
├─ Data/                         # PosLocalDbContext, entities, configs, migrations (exists)
├─ Configuration/                # LocalDatabaseConfigurationGuard, settings (exists)
└─ Assets/ui/                    # the hosted prototype screens + logo.png (+ fonts later)
```

---

## 13. Screen → service mapping

| Screen | Primary service(s) | Key POS.Shared entities |
|---|---|---|
| `provision_terminal.html` | `IProvisioningService`, `ICatalogService` (seed) | `Company`, `Location`, `Terminal` |
| `terminal_login.html` | `IAuthService`, `ISessionService` | `Employee`, `TerminalSession` |
| `shift_open.html` | `IShiftService` | `Shift` |
| `main_checkout.html` | `IOrderService`, `ICatalogService` | `Order`, `OrderLine`, `Item`, `ItemPrice`, `TaxRule` |
| `payment_screen.html` | `IPaymentService`, `IReceiptService` | `Payment`, `TenderMethod`, `ReceiptTemplate` |
| `cash_control.html` | `ICashControlService` | `CashDrawerMovement`, `ReasonCode` |
| `shift_close.html` | `IShiftService`, `IReportingService` | `Shift`, `ZReport`, `CashDrawerMovement` |

## 14. Prototype screen → real module mapping (what dies, what replaces it)

| Screen | Fake logic removed | Real module |
|---|---|---|
| Provision | `setTimeout` progress, `terminal_config` localStorage | Provisioning module + real `IProvisionedTerminalContext` |
| Login | `operators[]` + PIN-in-JS, `terminal_operator` | Auth + Session modules |
| Shift open | `pos_shift_open/float` sessionStorage | Shifts module |
| Checkout | `ITEMS[]`/`CATEGORIES[]`, `pos_cart` | Catalog + Orders modules |
| Payment | fake card/wallet `setTimeout`, hardcoded receipt | Payments + Receipt modules (+ payment-terminal hardware later) |
| Cash control | `pos_safe_drops` | CashControl module |
| Shift close | `DENOMS[]` demo metrics, hardcoded Z-report | Shifts + Reporting modules |

---

## 15. Data flow (text diagram)

```
[Scanner / keyboard / touch]
        │
        ▼
[ HTML screen in WebView2 ]   ← UI only: capture input, render response
        │  window.chrome.webview.postMessage({ type, payload })
        ▼
[ PosWebMessageRouter ]        (Shell/)
        │  typed request DTO
        ▼
[ Service ]  e.g. OrderService / PaymentService / ShiftService
        │            │
        │            └────────────► [ POS.Desktop.Hardware ]  (printer, drawer, pinpad…)
        ▼
[ PosLocalDbContext (SQLite) ] + [ POS.Shared entities ]
        │  on write: enqueue
        ▼
[ SyncOutbox ] ──► [ SyncProcessor ] ──► HttpClient ──► [ POS.Api /Sync ]  (later)

Response DTO (JSON) travels back up the same path → screen renders authoritative values.
```

---

## 16. Phased roadmap

> Each phase lists **Objective · Files/folders · Tasks · Expected output · Risks**.

### Phase 1 — Desktop shell integration
- **Objective:** A real window that hosts web content and has a proper app host.
- **Files/folders:** `POS.Desktop.csproj`, `App.xaml.cs`, `MainWindow.xaml(.cs)`, new `Shell/WebViewHost.cs`.
- **Tasks:** add `Microsoft.Web.WebView2`; convert manual `ServiceCollection` to **Generic Host** (`Microsoft.Extensions.Hosting`); make `MainWindow` full-screen kiosk hosting a `WebView2`; run `db.Database.Migrate()` on startup.
- **Expected output:** App launches full-screen and displays a hosted HTML page; DB migrates on first run.
- **Risks:** WebView2 runtime missing on dev/target machine; ensure consistent TFM (`net8.0-windows`).

### Phase 2 — Preserve prototype screens & route them
- **Objective:** All 7 screens render *identically* inside the app and navigate correctly.
- **Files/folders:** `POS.Desktop/Assets/ui/*` (copied screens + `logo.png`), `Shell/WebViewHost.cs`.
- **Tasks:** copy screens as build Content; `SetVirtualHostNameToFolderMapping("pos.app", …)`; navigate to `terminal_login.html`; verify in-app navigation; **WPF shell replaces `index.html`**.
- **Expected output:** Pixel-identical screens, full flow navigable in-app. Prototype in `docs/` untouched.
- **Risks:** asset paths/case sensitivity; relative links; font/icon loading.

### Phase 3 — Replace fake browser state
- **Objective:** State owned by C#, proven by one real round-trip.
- **Files/folders:** `Shell/PosHostApi.cs`, `Shell/PosWebMessageRouter.cs`, `Bridge/*`, `Services/Session/*`; edit only `<script>` blocks in screens.
- **Tasks:** establish the bridge (host object + `postMessage`); add `ISessionService`; replace `localStorage`/`sessionStorage` reads/writes with bridge calls; prove with login PIN round-trip.
- **Expected output:** No business state in the browser; login PIN validated in C#.
- **Risks:** serialization/contract mismatches; async timing; bridge error handling.

### Phase 4 — Connect SQLite/local services
- **Objective:** Screens read real data from SQLite.
- **Files/folders:** `Services/Provisioning/*`, `Services/Catalog/*`, `Data/*`.
- **Tasks:** real `IProvisionedTerminalContext` (replace `NoProvisionedTerminalContext`); catalog read service replacing `ITEMS[]`; seed a minimal local catalog for offline checkout.
- **Expected output:** Login/checkout show real persisted data; tenant filters resolve.
- **Risks:** query filters block reads if provisioning isn't set first — provision before everything.

### Phase 5 — Real flows (login, shift, order, payment, cash control)
- **Objective:** End-to-end shift on real data.
- **Files/folders:** `Services/{Auth,Shifts,Orders,Payments,CashControl,Reporting}/*`.
- **Tasks:** implement each flow per §7; append-only persistence; enqueue `SyncOutbox` + `PrintQueue`; compute tax/totals/change/variance in C#.
- **Expected output:** Open shift → sell → pay → cash control → close + Z-report, fully on SQLite.
- **Risks:** money rounding/tax correctness; concurrency; ensure idempotent writes.

### Phase 6 — Sync / outbox ↔ POS.Api
- **Objective:** Offline-created records replicate when online.
- **Files/folders:** `Services/Sync/*`; **new** POS.Api `Sync/` ingest.
- **Tasks:** typed `HttpClient` (PosDevice JWT); `SyncProcessor` draining `SyncOutbox`; cursor advance; retry via `LocalRecoveryJournal`; API ack via `SyncIngestAck`.
- **Expected output:** Terminal data appears centrally; offline still works.
- **Risks:** auth/token refresh; partial failures; idempotency on the server.

### Phase 7 — Hardware integration
- **Objective:** Pluggable devices, stubs by default.
- **Files/folders:** `POS.Desktop.Hardware/{Printers,CashDrawer,Scanner,PaymentTerminal,CustomerDisplay}/*`.
- **Tasks:** define interfaces (§9); no-op/console stubs via config; wire scanner→checkout, printer→`PrintQueue`; real pinpad replaces fake card/wallet.
- **Expected output:** App runs with no devices; real devices drop in by config.
- **Risks:** device SDK variance; driver/runtime dependencies.

### Phase 8 — Production hardening
- **Objective:** Deployable terminal.
- **Files/folders:** `Shell/*`, `Assets/ui/fonts/*`, packaging.
- **Tasks:** WebView2 runtime bootstrap check; kiosk lockdown; error/telemetry/logging; **offline-bundle fonts** (no visual change); FBR integration point; installer; retire prototype/simulator (§11).
- **Expected output:** Installable, locked-down, offline-capable terminal.
- **Risks:** packaging/signing; field WebView2 runtime; FBR compliance specifics.

---

## 17. What to build first / what not to build yet

**Build first (in order):**
1. Generic Host + WebView2 shell (Phase 1).
2. Virtual-host the screens; flow navigable (Phase 2).
3. One bridge round-trip (login PIN) (Phase 3).
4. Login → shift open → checkout → payment **happy path** on SQLite (Phases 4–5 slice).

**Do not build yet (stub/defer):** real FBR submission; real pinpad/payment-terminal driver; full multi-terminal sync; advanced reporting/analytics. Keep clean integration points so these slot in later.

**Minimum safe implementation:** Phases 1–2 plus the login/shift/checkout/payment slice of Phase 5 on SQLite. That alone is a usable, demoable terminal that looks exactly like the prototype.

---

## 18. Testing strategy

- **Unit (xUnit, POS.Tests):** tax/total calculation, cash change, shift variance, PIN validation, shift lifecycle, outbox enqueue — pure service logic, no UI.
- **Integration:** services against a real SQLite file/in-memory DB; verify append-only writes + query filters under a provisioned context.
- **Bridge contract tests:** request/response DTO round-trips so JS↔C# shapes can't silently drift.
- **Manual UI verification:** run the WPF app and walk the full flow; also open the screens directly in a browser for fast visual checks (they're plain HTML).
- **Regression guard:** after each phase, re-walk the end-to-end flow to confirm no visual or behavioural drift.

---

## 19. Build / run verification checklist

- [ ] `dotnet build POS.slnx` succeeds (all projects, `net8.0(-windows)`).
- [ ] WebView2 Evergreen runtime present (or bootstrapper handles install).
- [ ] App launches full-screen and lands on provision/login.
- [ ] Local SQLite DB migrates on first run; provisioning sets a valid `IProvisionedTerminalContext`.
- [ ] Each flow (login → shift → checkout → payment → cash control → close) round-trips through C# services.
- [ ] No JS console errors and no unhandled bridge exceptions.
- [ ] Screens are visually identical to `docs/ui-prototype/screens/*` (spot-check theme, fonts, spacing, logo).

---

## 20. Honest gaps & assumptions

- **Technical Dependency Baseline (WebView2):**
  - **Rationale:** Chosen to host the existing high-fidelity HTML prototype directly, ensuring 100% visual parity and zero redesign risk.
  - **SDK Version:** `Microsoft.Web.WebView2` v1.0.2903.40.
  - **Target Framework:** `net8.0-windows`.
  - **Runtime Baseline:** Microsoft Edge WebView2 Evergreen Runtime.
  - **Scope:** Strictly for hosting the Desktop Terminal UI; this is NOT a replacement for the ASP.NET Core `POS.Api`.
  - **Deployment:** The runtime must be installed or bootstrapped on all terminal machines. The app verifies presence at startup.
- **POS.Api `Sync/` is empty** — the central ingest endpoint must be built before Phase 6 completes.
- **POS.Desktop.Hardware is empty** — all device interfaces are net-new (Phase 7).
- **Catalog seeding** from the API isn't implemented; Phase 4 uses a minimal local seed so offline checkout works before sync exists.
- **FBR fiscal integration** is represented only as fake text in the prototype; treat it as a defined external integration point, not in scope for the minimum safe build.
- **WebView2 runtime** is an external dependency on every terminal — must be part of deployment.
```
