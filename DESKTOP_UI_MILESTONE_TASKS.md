# Desktop UI Milestone Tasks

**Project:** IMAGYN POS — `POS.Desktop` terminal build-out
**Companion to:** `DESKTOP_UI_INTEGRATION_PLAN.md` (roadmap) and `DESKTOP_UI_PHASE_MILESTONES.md` (milestones)
**Status:** Planning document — **no implementation code yet**
**Last updated:** 2026-05-23

---

## How to read this document

Every milestone from `DESKTOP_UI_PHASE_MILESTONES.md` is broken into **exactly 10 ordered, actionable tasks**. Tasks are sequential — do them top to bottom. Each task is small enough to implement in one sitting.

Each task has:
- **Description** — what to do.
- **Files/folders** — where the work lands (paths relative to repo root).
- **Expected output** — the concrete result.
- **Verification** — how to confirm it's done.
- **Risk/notes** — what to watch.

**Standing guardrails (apply to every task):**
- Phase 2 includes a controlled production UI/UX modernization. HTML/CSS edits and a shared `app.css` design system are allowed there; later UI edits should be narrow and intentional.
- `docs/ui-prototype/screens/*` and `POS.Desktop/Assets/ui/*` must stay synchronized for the shipped UI assets until one source is formally promoted.
- `docs/ui-prototype/index.html` is a **simulator only** — never the final desktop design.
- No business logic in the UI; C# owns all decisions. Offline-first; never block on the network.
- Do not delete `docs/ui-prototype/*` until in-app production UI/UX sign-off is confirmed (Phase 2+).

**Numbering:** `Task P.M.T` = Phase P, Milestone M, Task T.

---

## Phase 1 — Desktop shell integration

### Milestone 1.1 — WebView2 dependency & framework baseline

**Task 1.1.1 — Inventory current TFMs and build outputs**
- **Description:** Catalogue the target frameworks actually produced (note stray `net10-windows` artifacts under `obj/`).
- **Files/folders:** `POS.Desktop/POS.Desktop.csproj`, `POS.Desktop/obj/`, `POS.Desktop/bin/`.
- **Expected output:** A short note listing current TFM(s) and any leftover build outputs.
- **Verification:** TFM list matches what the csproj declares; stray outputs identified.
- **Risk/notes:** Read-only audit; don't change anything yet.

**Task 1.1.2 — Clean stale build artifacts**
- **Description:** Remove leftover `obj/`/`bin/` outputs (especially non-`net8` ones) to avoid mixed-TFM confusion.
- **Files/folders:** `POS.Desktop/obj/`, `POS.Desktop/bin/`.
- **Expected output:** Clean build directories.
- **Verification:** `dotnet clean` succeeds; no `net10` folders remain.
- **Risk/notes:** Safe/reversible (regenerated on build).

**Task 1.1.3 — Pin the target framework**
- **Description:** Confirm/lock `TargetFramework` to `net8.0-windows` so all outputs are consistent.
- **Files/folders:** `POS.Desktop/POS.Desktop.csproj`.
- **Expected output:** Single, explicit TFM.
- **Verification:** Build produces only `net8.0-windows` outputs.
- **Risk/notes:** Align with sibling projects (`POS.Shared`, `POS.Desktop.Hardware` are `net8.0`).

**Task 1.1.4 — Add the WebView2 package reference**
- **Description:** Add `Microsoft.Web.WebView2` to the desktop project.
- **Files/folders:** `POS.Desktop/POS.Desktop.csproj`.
- **Expected output:** Package reference present.
- **Verification:** Package appears in the project; restore resolves it.
- **Risk/notes:** Pick a stable version compatible with the deployable runtime.

**Task 1.1.5 — Choose and record the WebView2 SDK/runtime version**
- **Description:** Decide the SDK version and matching Evergreen runtime baseline for terminals.
- **Files/folders:** repo notes (e.g., a line in `DESKTOP_UI_INTEGRATION_PLAN.md` §20 area or a NOTES file).
- **Expected output:** Documented SDK + runtime version decision.
- **Verification:** Version recorded and referenced by Phase 8.1.
- **Risk/notes:** Runtime is an external deployment dependency.

**Task 1.1.6 — Restore and resolve package conflicts**
- **Description:** Run restore; resolve any transitive version conflicts with EF Core packages.
- **Files/folders:** `POS.Desktop/POS.Desktop.csproj`, `POS.slnx`.
- **Expected output:** Clean restore.
- **Verification:** `dotnet restore` succeeds with no warnings about downgrades.
- **Risk/notes:** EF Core 8.0.27 is already pinned — keep alignment.

**Task 1.1.7 — Confirm the WebView2 control type resolves**
- **Description:** Verify the `WebView2` control namespace is referenceable (compile-time check, no UI code yet).
- **Files/folders:** `POS.Desktop/` (build only).
- **Expected output:** Type resolves at build.
- **Verification:** A trivial reference compiles; remove it after.
- **Risk/notes:** Don't commit throwaway references.

**Task 1.1.8 — Build the full solution**
- **Description:** Build `POS.slnx` end-to-end.
- **Files/folders:** `POS.slnx`.
- **Expected output:** Clean build across all projects.
- **Verification:** `dotnet build POS.slnx` succeeds.
- **Risk/notes:** Fix any regressions before proceeding.

**Task 1.1.9 — Establish a warnings baseline**
- **Description:** Capture nullable/analyzer warning count as a baseline to avoid silent growth.
- **Files/folders:** build output/log.
- **Expected output:** Recorded baseline.
- **Verification:** No new warnings introduced by 1.1.x.
- **Risk/notes:** `Nullable` is enabled — keep it clean.

**Task 1.1.10 — Document the dependency decision**
- **Description:** Write a short note on WebView2 choice, version, and TFM for the team.
- **Files/folders:** repo notes/README area.
- **Expected output:** Documented baseline.
- **Verification:** Note exists and is discoverable.
- **Risk/notes:** Feeds Phase 8 packaging.

### Milestone 1.2 — Generic Host bootstrap (DI rework)

**Task 1.2.1 — Add the hosting package**
- **Description:** Add `Microsoft.Extensions.Hosting` to the desktop project.
- **Files/folders:** `POS.Desktop/POS.Desktop.csproj`.
- **Expected output:** Hosting package referenced.
- **Verification:** Restore resolves it.
- **Risk/notes:** Keeps DI consistent with future services.

**Task 1.2.2 — Design the composition root layout**
- **Description:** Decide where the host builder + service registrations live.
- **Files/folders:** `POS.Desktop/Configuration/` (new helper alongside `LocalDatabaseConfigurationGuard`).
- **Expected output:** A planned registration entry point.
- **Verification:** Layout agreed and documented.
- **Risk/notes:** One composition root only — avoid scattering registrations.

**Task 1.2.3 — Introduce the host builder**
- **Description:** Stand up an `IHostBuilder`/`IHost` with configuration + DI (no business services yet).
- **Files/folders:** `POS.Desktop/App.xaml.cs`, `POS.Desktop/Configuration/`.
- **Expected output:** A buildable host.
- **Verification:** Host builds without error at startup.
- **Risk/notes:** Don't open the window before the host is ready.

**Task 1.2.4 — Move configuration loading into the host**
- **Description:** Wire `appsettings.json` through host configuration.
- **Files/folders:** `POS.Desktop/appsettings.json`, host builder.
- **Expected output:** Config available via DI.
- **Verification:** Connection string resolves through host config.
- **Risk/notes:** Reuse existing `LocalDatabaseConfigurationGuard` semantics.

**Task 1.2.5 — Register the local DbContext via the host**
- **Description:** Move `PosLocalDbContext` registration into the host.
- **Files/folders:** `POS.Desktop/App.xaml.cs`, `POS.Desktop/Data/PosLocalDbContext.cs` (consumer).
- **Expected output:** DbContext resolvable from the container.
- **Verification:** Resolving the context succeeds.
- **Risk/notes:** Preserve SQLite options/connection string.

**Task 1.2.6 — Register the provisioning context via the host**
- **Description:** Register `IProvisionedTerminalContext` → `NoProvisionedTerminalContext` (unchanged for now).
- **Files/folders:** `POS.Desktop/App.xaml.cs`, `POS.Desktop/Services/Provisioning/`.
- **Expected output:** Context resolvable.
- **Verification:** Resolution succeeds; still fail-closed.
- **Risk/notes:** Real context comes in Phase 4.

**Task 1.2.7 — Build/start the host in OnStartup**
- **Description:** Replace the manual `ServiceCollection` with host build/start in `App.OnStartup`.
- **Files/folders:** `POS.Desktop/App.xaml.cs`.
- **Expected output:** Host starts on app launch.
- **Verification:** App launches with no DI errors.
- **Risk/notes:** Watch WPF startup ordering.

**Task 1.2.8 — Stop/dispose the host on exit**
- **Description:** Stop and dispose the host in `App.OnExit`.
- **Files/folders:** `POS.Desktop/App.xaml.cs`.
- **Expected output:** Clean shutdown.
- **Verification:** No disposal exceptions on exit.
- **Risk/notes:** Ensure background services (later) stop cleanly.

**Task 1.2.9 — Resolve MainWindow from the container**
- **Description:** Construct `MainWindow` via DI so it can take dependencies later.
- **Files/folders:** `POS.Desktop/App.xaml.cs`, `POS.Desktop/MainWindow.xaml.cs`.
- **Expected output:** Window resolved from host.
- **Verification:** Window opens normally.
- **Risk/notes:** Remove `StartupUri` if it conflicts.

**Task 1.2.10 — Smoke-test startup**
- **Description:** Launch and confirm no resolution/lifetime errors.
- **Files/folders:** `POS.Desktop/`.
- **Expected output:** Clean launch + exit.
- **Verification:** App runs and closes without errors.
- **Risk/notes:** Baseline for all later DI work.

### Milestone 1.3 — Full-screen kiosk shell hosting WebView2

**Task 1.3.1 — Define a borderless full-screen window**
- **Description:** Configure `MainWindow` as borderless, maximized/full-screen (shell chrome, not prototype design).
- **Files/folders:** `POS.Desktop/MainWindow.xaml`.
- **Expected output:** Full-screen kiosk window.
- **Verification:** Opens borderless at target resolution.
- **Risk/notes:** This is shell styling, not UI redesign.

**Task 1.3.2 — Add the WebView2 control**
- **Description:** Place a single `WebView2` control filling the window.
- **Files/folders:** `POS.Desktop/MainWindow.xaml`.
- **Expected output:** WebView2 host region.
- **Verification:** Control present in layout.
- **Risk/notes:** One WebView2 instance for all screens.

**Task 1.3.3 — Outline the WebViewHost responsibilities**
- **Description:** Create `Shell/WebViewHost.cs` to own init, mapping, navigation, bridge.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs` (new).
- **Expected output:** Host class skeleton.
- **Verification:** Compiles; wired to MainWindow.
- **Risk/notes:** Keep shell concerns out of code-behind.

**Task 1.3.4 — Choose an explicit user-data folder**
- **Description:** Set a writable user-data folder path for WebView2.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`, `appsettings.json`.
- **Expected output:** Defined writable path.
- **Verification:** Folder created and writable at runtime.
- **Risk/notes:** Defaults can fail under locked-down accounts.

**Task 1.3.5 — Initialize the CoreWebView2 environment**
- **Description:** Create the environment with the chosen user-data folder.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`.
- **Expected output:** Environment initialized.
- **Verification:** No init exception.
- **Risk/notes:** Environment must precede control init.

**Task 1.3.6 — Await EnsureCoreWebView2Async before navigation**
- **Description:** Guarantee init completes before any navigation/bridge wiring.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`.
- **Expected output:** Ordered async init.
- **Verification:** Navigation only after init completes.
- **Risk/notes:** Common source of race bugs.

**Task 1.3.7 — Render a placeholder page**
- **Description:** Navigate to a minimal local placeholder to confirm rendering.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`, temp asset.
- **Expected output:** Placeholder visible.
- **Verification:** Page renders in-app.
- **Risk/notes:** Replace with real screens in Phase 2.

**Task 1.3.8 — Handle initialization failure**
- **Description:** Add a basic failure path if init throws.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`.
- **Expected output:** Graceful failure handling.
- **Verification:** Simulated failure shows a message, no crash.
- **Risk/notes:** Full guard is Milestone 1.5.

**Task 1.3.9 — Verify full-screen presentation**
- **Description:** Confirm borderless full-screen at the terminal resolution (e.g., 1366×768).
- **Files/folders:** `POS.Desktop/MainWindow.xaml`.
- **Expected output:** Correct kiosk presentation.
- **Verification:** No window chrome; fills screen.
- **Risk/notes:** Verify at target res, not a dev monitor.

**Task 1.3.10 — Confirm placeholder renders post-init**
- **Description:** End-to-end check that init→navigate works.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`.
- **Expected output:** Reliable render on launch.
- **Verification:** Repeated launches render consistently.
- **Risk/notes:** Baseline for Phase 2.

### Milestone 1.4 — Startup database migration & first-run readiness

**Task 1.4.1 — Add a startup migration hook**
- **Description:** Run migrations during host startup before the window shows.
- **Files/folders:** `POS.Desktop/App.xaml.cs`, `POS.Desktop/Configuration/`.
- **Expected output:** Migration step at boot.
- **Verification:** Hook executes on launch.
- **Risk/notes:** Order before UI display.

**Task 1.4.2 — Resolve the DbContext in a startup scope**
- **Description:** Create a DI scope to access `PosLocalDbContext` for migration.
- **Files/folders:** `POS.Desktop/App.xaml.cs`.
- **Expected output:** Scoped context for startup.
- **Verification:** Context resolves in scope.
- **Risk/notes:** Dispose the scope after migration.

**Task 1.4.3 — Apply pending migrations**
- **Description:** Call `Database.Migrate()` (guarded by config flag).
- **Files/folders:** `POS.Desktop/Data/PosLocalDbContext.cs` (consumer), startup code.
- **Expected output:** Schema applied.
- **Verification:** Tables created on fresh DB.
- **Risk/notes:** Existing migration `20260518194918_Local_InitialPhase1Schema`.

**Task 1.4.4 — Verify the DB file path**
- **Description:** Confirm the SQLite path from `LocalDatabaseConfigurationGuard`.
- **Files/folders:** `POS.Desktop/Configuration/LocalDatabaseConfigurationGuard.cs`, `appsettings.json`.
- **Expected output:** Correct, writable DB path.
- **Verification:** DB created at expected location.
- **Risk/notes:** Avoid design-time DB (`pos_local_designtime.db`) in production.

**Task 1.4.5 — Create DB on a fresh machine**
- **Description:** Validate first-run DB creation.
- **Files/folders:** runtime DB path.
- **Expected output:** DB exists after first launch.
- **Verification:** File present; tables exist.
- **Risk/notes:** Test on a clean profile.

**Task 1.4.6 — Confirm idempotent second run**
- **Description:** Ensure repeat launches don't re-migrate.
- **Files/folders:** startup code.
- **Expected output:** No-op on second run.
- **Verification:** No errors; no duplicate work.
- **Risk/notes:** Migration is inherently idempotent — confirm anyway.

**Task 1.4.7 — Add connectivity check + logged failure**
- **Description:** Verify the DB opens; log a clear failure if not.
- **Files/folders:** startup code, log sink.
- **Expected output:** Open-check with logging.
- **Verification:** Forced bad path logs cleanly.
- **Risk/notes:** Don't crash the app on DB failure.

**Task 1.4.8 — Surface migration failure to UI**
- **Description:** Show a graceful message if migration fails.
- **Files/folders:** `POS.Desktop/Shell/`, `MainWindow`.
- **Expected output:** User-visible error, no crash.
- **Verification:** Simulated failure shows message.
- **Risk/notes:** Keep message non-technical.

**Task 1.4.9 — Run migration off the UI thread**
- **Description:** Ensure migration doesn't block the UI thread.
- **Files/folders:** startup code.
- **Expected output:** Responsive startup.
- **Verification:** UI not frozen during migration.
- **Risk/notes:** Coordinate with window display timing.

**Task 1.4.10 — Test fresh + repeat scenarios**
- **Description:** Exercise both first-run and subsequent runs.
- **Files/folders:** runtime DB.
- **Expected output:** Both paths pass.
- **Verification:** Documented test pass.
- **Risk/notes:** Data reads still empty until provisioning (Phase 4).

### Milestone 1.5 — Shell diagnostics & WebView2 runtime guard

**Task 1.5.1 — Detect runtime presence**
- **Description:** Use `GetAvailableBrowserVersionString` to detect the runtime.
- **Files/folders:** `POS.Desktop/Shell/` (new guard helper).
- **Expected output:** Runtime detection at boot.
- **Verification:** Returns version when present.
- **Risk/notes:** Handle exception when absent.

**Task 1.5.2 — Define a fallback message**
- **Description:** Author a friendly "runtime required" message (text only).
- **Files/folders:** `POS.Desktop/Shell/`, simple message view.
- **Expected output:** Clear actionable text.
- **Verification:** Message displays on absence.
- **Risk/notes:** Not a redesign — minimal shell text.

**Task 1.5.3 — Branch startup on runtime presence**
- **Description:** Normal boot if present; message if absent.
- **Files/folders:** `POS.Desktop/App.xaml.cs`, `Shell/`.
- **Expected output:** Two clear startup paths.
- **Verification:** Both paths behave correctly.
- **Risk/notes:** No silent failure.

**Task 1.5.4 — Add a minimal logging sink**
- **Description:** Configure shell-level logging (file/console).
- **Files/folders:** `appsettings.json`, `Shell/`.
- **Expected output:** Logs written.
- **Verification:** Startup events logged.
- **Risk/notes:** Full telemetry deferred to Phase 8.3.

**Task 1.5.5 — Log CoreWebView2 lifecycle events**
- **Description:** Log init start/success/failure.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`.
- **Expected output:** Lifecycle visibility.
- **Verification:** Events appear in logs.
- **Risk/notes:** Keep noise low.

**Task 1.5.6 — Choose a writable log location**
- **Description:** Pick a known, writable log path.
- **Files/folders:** `appsettings.json`.
- **Expected output:** Defined log directory.
- **Verification:** Logs created there.
- **Risk/notes:** Match user-data folder constraints.

**Task 1.5.7 — Test with runtime present**
- **Description:** Confirm normal boot path.
- **Files/folders:** runtime.
- **Expected output:** App boots normally.
- **Verification:** Placeholder renders.
- **Risk/notes:** Baseline happy path.

**Task 1.5.8 — Test with runtime absent**
- **Description:** Simulate missing runtime (rename/registry/test rig).
- **Files/folders:** test environment.
- **Expected output:** Message shown, no crash.
- **Verification:** Graceful behavior confirmed.
- **Risk/notes:** Document how to simulate.

**Task 1.5.9 — Ensure no unhandled startup exception**
- **Description:** Wrap startup so nothing escapes uncaught.
- **Files/folders:** `POS.Desktop/App.xaml.cs`.
- **Expected output:** All startup errors handled.
- **Verification:** No crash dialogs.
- **Risk/notes:** Foundation for Phase 8.3 global handler.

**Task 1.5.10 — Document the runtime requirement**
- **Description:** Record the runtime prerequisite for deployment.
- **Files/folders:** repo notes.
- **Expected output:** Documented prerequisite.
- **Verification:** Referenced by Phase 8.1.
- **Risk/notes:** Critical for field installs.

---

## Phase 2 — Preserve prototype screen UI and route screens properly

### Milestone 2.1 — Asset ingestion pipeline

**Task 2.1.1 — Create the assets folder**
- **Description:** Add `POS.Desktop/Assets/ui/` to hold the production copy of the screen assets.
- **Files/folders:** `POS.Desktop/Assets/ui/` (new).
- **Expected output:** Empty target folder.
- **Verification:** Folder exists in the project.
- **Risk/notes:** Keep it synchronized with `docs/ui-prototype/screens/` until cleanup promotes one source.

**Task 2.1.2 — Copy the 7 screens + shared assets**
- **Description:** Copy the screen files, `app.css`, and `logo.png` from `docs/ui-prototype/screens/`.
- **Files/folders:** `POS.Desktop/Assets/ui/*` ← `docs/ui-prototype/screens/*`.
- **Expected output:** 7 HTML files + `app.css` + `logo.png` present.
- **Verification:** File list matches source.
- **Risk/notes:** Source and production copies must remain synchronized.

**Task 2.1.3 — Add Content globs to the csproj**
- **Description:** Include `Assets/ui/**` as Content.
- **Files/folders:** `POS.Desktop/POS.Desktop.csproj`.
- **Expected output:** Assets tracked by the build.
- **Verification:** Files show as Content.
- **Risk/notes:** Use a glob so future assets are included.

**Task 2.1.4 — Set copy-to-output**
- **Description:** Mark assets `CopyToOutputDirectory=PreserveNewest`.
- **Files/folders:** `POS.Desktop/POS.Desktop.csproj`.
- **Expected output:** Assets emitted to build output.
- **Verification:** Files appear next to the binary after build.
- **Risk/notes:** PreserveNewest avoids needless copies.

**Task 2.1.5 — Verify synchronized UI assets**
- **Description:** Hash-compare copied files vs `docs/` source assets.
- **Files/folders:** `POS.Desktop/Assets/ui/*`, `docs/ui-prototype/screens/*`.
- **Expected output:** Matching hashes.
- **Verification:** Hashes equal for all files.
- **Risk/notes:** Guarantees source/production drift is caught.

**Task 2.1.6 — Build and confirm output**
- **Description:** Build and inspect the output directory for assets.
- **Files/folders:** `POS.Desktop/bin/`.
- **Expected output:** Assets present in output.
- **Verification:** All files copied.
- **Risk/notes:** Path casing matters on some setups.

**Task 2.1.7 — Confirm source and production stay in sync**
- **Description:** Verify `docs/ui-prototype/screens/` and `POS.Desktop/Assets/ui/` contain the same shipped UI files.
- **Files/folders:** `docs/ui-prototype/`.
- **Expected output:** No source/production drift.
- **Verification:** Hashes match for all 7 screens, `app.css`, and `logo.png`.
- **Risk/notes:** `docs/` may change during Phase 2 UI modernization, but the asset copy must match it.

**Task 2.1.8 — Decide and record copy-vs-link policy**
- **Description:** Document that `Assets/ui/` is a copy and how it stays in sync until `docs/` retires.
- **Files/folders:** repo notes.
- **Expected output:** Documented policy.
- **Verification:** Note exists.
- **Risk/notes:** Drift risk if both edited — single-source after parity.

**Task 2.1.9 — Note the post-sign-off promotion**
- **Description:** Record that one UI source is promoted only after Phase 2 UI/UX sign-off (per cleanup §11).
- **Files/folders:** repo notes.
- **Expected output:** Cleanup linkage documented.
- **Verification:** Referenced by Phase 8.6.
- **Risk/notes:** Gate deletion on UI/UX sign-off.

**Task 2.1.10 — Verify source/asset sync**
- **Description:** Confirm the production asset copy exactly matches the source screen folder.
- **Files/folders:** `POS.Desktop/Assets/ui/*`.
- **Expected output:** Identical source/asset content.
- **Verification:** Diff vs source is empty.
- **Risk/notes:** This is a sync check, not a ban on sanctioned Phase 2 UI/CSS changes.

### Milestone 2.2 — Virtual host mapping & initial navigation

**Task 2.2.1 — Compute the assets path at runtime**
- **Description:** Resolve the absolute path to `Assets/ui/` in the output dir.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`.
- **Expected output:** Correct runtime path.
- **Verification:** Path exists at runtime.
- **Risk/notes:** Use base directory, not CWD.

**Task 2.2.2 — Map the virtual host**
- **Description:** Call `SetVirtualHostNameToFolderMapping("pos.app", path, Allow)`.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`.
- **Expected output:** Virtual host active.
- **Verification:** `https://pos.app/...` resolves to local files.
- **Risk/notes:** Resource-access kind must allow local content.

**Task 2.2.3 — Choose the host scheme**
- **Description:** Standardize on `https://pos.app` as the origin.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`.
- **Expected output:** Consistent origin.
- **Verification:** All navigation uses it.
- **Risk/notes:** https origin needed for some web APIs.

**Task 2.2.4 — Navigate to the login screen on boot**
- **Description:** Set initial navigation to `terminal_login.html`.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`.
- **Expected output:** Login screen on launch.
- **Verification:** Login renders at startup.
- **Risk/notes:** Provisioning gate handled in Phase 4.

**Task 2.2.5 — Verify relative asset resolution**
- **Description:** Confirm CSS/JS/img relative refs load under the host.
- **Files/folders:** `POS.Desktop/Assets/ui/*`.
- **Expected output:** No broken resources.
- **Verification:** No 404s in WebView2.
- **Risk/notes:** Inline CSS/JS reduces risk; logo.png is the main asset.

**Task 2.2.6 — Confirm no file:// origin**
- **Description:** Ensure the app never loads screens via `file://`.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`.
- **Expected output:** Virtual host only.
- **Verification:** Address/origin is `pos.app`.
- **Risk/notes:** file:// breaks host-object/fetch semantics.

**Task 2.2.7 — Handle mapping-before-init ordering**
- **Description:** Ensure mapping is set after init, before navigation.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`.
- **Expected output:** Correct ordering.
- **Verification:** No "navigated before mapped" failures.
- **Risk/notes:** Ties to Task 1.3.6.

**Task 2.2.8 — Test reload/refresh stability**
- **Description:** Confirm refresh re-resolves assets correctly.
- **Files/folders:** runtime.
- **Expected output:** Stable reloads.
- **Verification:** Refresh keeps rendering.
- **Risk/notes:** Cache behavior under virtual host.

**Task 2.2.9 — Verify logo loads via host**
- **Description:** Confirm `logo.png` renders through `pos.app`.
- **Files/folders:** `POS.Desktop/Assets/ui/logo.png`.
- **Expected output:** Logo visible.
- **Verification:** Image loads, no fallback text.
- **Risk/notes:** Screens have an onerror text fallback — ensure real image loads.

**Task 2.2.10 — Confirm login renders fully**
- **Description:** End-to-end check of the login screen under the host.
- **Files/folders:** `POS.Desktop/Assets/ui/terminal_login.html`.
- **Expected output:** Complete login UI.
- **Verification:** Operator grid + keypad render.
- **Risk/notes:** Baseline for routing milestone.

### Milestone 2.3 — In-app screen routing (retire the simulator)

**Task 2.3.1 — Enumerate inter-screen links**
- **Description:** List all `window.location.href`/link targets per screen.
- **Files/folders:** `POS.Desktop/Assets/ui/*`.
- **Expected output:** Navigation map.
- **Verification:** All targets catalogued.
- **Risk/notes:** Read-only analysis.

**Task 2.3.2 — Confirm targets resolve under pos.app**
- **Description:** Verify each link works against the virtual host.
- **Files/folders:** `POS.Desktop/Assets/ui/*`.
- **Expected output:** Resolvable links.
- **Verification:** Each navigates correctly.
- **Risk/notes:** Relative paths preferred.

**Task 2.3.3 — Adjust link paths if needed (script-only)**
- **Description:** Fix broken link paths inside `<script>`/`href` if needed.
- **Files/folders:** `POS.Desktop/Assets/ui/*`.
- **Expected output:** Working navigation.
- **Verification:** No dead links.
- **Risk/notes:** Keep routing fixes separate from the sanctioned 2.6 UI modernization work.

**Task 2.3.4 — Verify provision→login→shift_open**
- **Description:** Walk the first leg of the flow.
- **Files/folders:** relevant screens.
- **Expected output:** Leg navigable.
- **Verification:** Each transition works.
- **Risk/notes:** Provisioning is still fake here (Phase 4 makes it real).

**Task 2.3.5 — Verify checkout→payment→cash_control→shift_close**
- **Description:** Walk the second leg.
- **Files/folders:** relevant screens.
- **Expected output:** Leg navigable.
- **Verification:** Each transition works.
- **Risk/notes:** Logic still fake until later phases.

**Task 2.3.6 — Verify shift_close→login loop**
- **Description:** Confirm closing returns to login.
- **Files/folders:** `shift_close.html`, `terminal_login.html`.
- **Expected output:** Loop-back works.
- **Verification:** Returns to login.
- **Risk/notes:** Real lock behavior in Phase 5.6.

**Task 2.3.7 — Ensure index.html is never loaded**
- **Description:** Confirm the shell never navigates to the simulator.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`.
- **Expected output:** No simulator usage.
- **Verification:** index.html absent from navigation.
- **Risk/notes:** Simulator is reference-only.

**Task 2.3.8 — Neutralize simulator sidebar postMessage**
- **Description:** Ensure prototype's parent-sidebar `postMessage` sync is inert in-app.
- **Files/folders:** `POS.Desktop/Assets/ui/*` (`<script>` only if present).
- **Expected output:** No errors from absent parent.
- **Verification:** No console errors about missing parent.
- **Risk/notes:** That machinery was for the iframe simulator only.

**Task 2.3.9 — Walk the full 7-screen flow**
- **Description:** Navigate the entire flow in-app.
- **Files/folders:** all screens.
- **Expected output:** Complete flow works.
- **Verification:** End-to-end pass.
- **Risk/notes:** Production UI/UX sign-off is verified in 2.4 after modernization.

**Task 2.3.10 — Confirm no index.html dependency remains**
- **Description:** Verify nothing references the simulator.
- **Files/folders:** `POS.Desktop/Assets/ui/*`.
- **Expected output:** Simulator-free app.
- **Verification:** Grep shows no live references.
- **Risk/notes:** Keep `docs/index.html` as reference only.

### Milestone 2.4 — Production UI/UX sign-off (all 7 screens)

**Task 2.4.1 — Capture reviewed screen shots**
- **Description:** Screenshot each modernized screen in a desktop browser or WebView2 at the target resolution.
- **Files/folders:** `docs/ui-prototype/screens/*`.
- **Expected output:** 7 review images.
- **Verification:** 7 screens captured.
- **Risk/notes:** Use target resolution.

**Task 2.4.2 — Capture in-app shots**
- **Description:** Screenshot each screen rendered in the shell.
- **Files/folders:** runtime.
- **Expected output:** In-app images.
- **Verification:** 7 in-app captures.
- **Risk/notes:** Same resolution as review shots.

**Task 2.4.3 — Review dark terminal theme**
- **Description:** Verify dark charcoal surfaces, IMAGYN green CTAs/highlights, and semantic amber/red states are consistent.
- **Files/folders:** all 7 screens.
- **Expected output:** Cohesive dark operator-terminal theme.
- **Verification:** No screen looks like an old light prototype or generic SaaS page.
- **Risk/notes:** WebView2 color management edge cases.

**Task 2.4.4 — Verify fonts and hierarchy**
- **Description:** Verify Space Grotesk / Inter Tight / IBM Plex Mono render.
- **Files/folders:** all 7 screens.
- **Expected output:** Clear headings, readable body text, and mono totals/receipts.
- **Verification:** Headings/body/mono roles are consistent.
- **Risk/notes:** Online fonts now; bundling in 8.4.

**Task 2.4.5 — Verify touch targets and keypads**
- **Description:** Verify keypads, primary CTAs, tabs, and action buttons are comfortable for touch.
- **Files/folders:** relevant screens.
- **Expected output:** Large, stable operator controls.
- **Verification:** Key controls meet the 44px+ practical touch target guideline.
- **Risk/notes:** Avoid hover/active states that cause layout shift.

**Task 2.4.6 — Verify modals/overlays**
- **Description:** Verify receipt/confirm/customer/approval modals are readable on the dark theme.
- **Files/folders:** relevant screens.
- **Expected output:** Dark modals are readable; receipt/Z-report paper remains intentionally light and readable.
- **Verification:** No light text on light paper and no dark text on dark surfaces.
- **Risk/notes:** Z-index/backdrop.

**Task 2.4.7 — Verify spacing, density, and layout**
- **Description:** Verify cashier workflows stay dense but not cramped.
- **Files/folders:** all 7 screens.
- **Expected output:** Practical store-counter layout.
- **Verification:** No overlapping text, clipped controls, or accidental horizontal scroll.
- **Risk/notes:** Subtle DPI differences.

**Task 2.4.8 — Verify branding and active navigation**
- **Description:** Verify `logo.png`, active nav, status pills, and IMAGYN green highlights read consistently.
- **Files/folders:** screens using the logo.
- **Expected output:** Cohesive IMAGYN terminal identity.
- **Verification:** Logo loads and active state is obvious.
- **Risk/notes:** Fallback text must not appear.

**Task 2.4.9 — Log any UI defects**
- **Description:** Record contrast, spacing, state, or readability defects with screenshots.
- **Files/folders:** UI/UX notes.
- **Expected output:** Defect list (ideally empty).
- **Verification:** Items triaged.
- **Risk/notes:** Fix UI-only defects before Phase 3 starts.

**Task 2.4.10 — Produce production UI/UX sign-off**
- **Description:** Create a UI/UX checklist and sign it off before bridge work.
- **Files/folders:** UI/UX notes.
- **Expected output:** Signed UI/UX record.
- **Verification:** Sign-off recorded for all 7 screens.
- **Risk/notes:** Gates `docs/` cleanup (8.6).

### Milestone 2.5 — Font/icon/asset loading reliability

**Task 2.5.1 — Enumerate external font/icon links**
- **Description:** List Google Fonts + Material Symbols `<link>`s per screen.
- **Files/folders:** `POS.Desktop/Assets/ui/*`.
- **Expected output:** Dependency list.
- **Verification:** All externals catalogued.
- **Risk/notes:** Read-only.

**Task 2.5.2 — Observe network requests**
- **Description:** Watch WebView2 network for font/icon fetches.
- **Files/folders:** runtime/devtools.
- **Expected output:** Observed requests.
- **Verification:** Requests logged.
- **Risk/notes:** Dev tools disabled later (8.2) — observe now.

**Task 2.5.3 — Confirm online load**
- **Description:** Verify fonts/icons load when online.
- **Files/folders:** runtime.
- **Expected output:** Correct rendering online.
- **Verification:** No fallback glyphs.
- **Risk/notes:** Baseline.

**Task 2.5.4 — Test offline degradation**
- **Description:** Disable network and observe rendering.
- **Files/folders:** runtime.
- **Expected output:** Documented degradation.
- **Verification:** Behavior recorded.
- **Risk/notes:** Motivates 8.4 bundling.

**Task 2.5.5 — Record fallback behavior**
- **Description:** Note which fonts/glyphs fall back offline.
- **Files/folders:** notes.
- **Expected output:** Fallback inventory.
- **Verification:** Inventory complete.
- **Risk/notes:** Material Symbols especially.

**Task 2.5.6 — Confirm no local-asset 404s**
- **Description:** Ensure local assets (logo) never 404.
- **Files/folders:** `POS.Desktop/Assets/ui/`.
- **Expected output:** No local 404s.
- **Verification:** Clean network log for locals.
- **Risk/notes:** Distinguish local vs external failures.

**Task 2.5.7 — Note Material Symbols dependency**
- **Description:** Flag the icon-font dependency explicitly.
- **Files/folders:** notes.
- **Expected output:** Documented dependency.
- **Verification:** Referenced by 8.4.
- **Risk/notes:** Icons are critical to UI legibility.

**Task 2.5.8 — Document offline findings**
- **Description:** Summarize online/offline behavior.
- **Files/folders:** notes.
- **Expected output:** Findings doc.
- **Verification:** Doc exists.
- **Risk/notes:** Feeds Phase 8 planning.

**Task 2.5.9 — Define the bundling requirement**
- **Description:** Translate findings into the 8.4 requirement.
- **Files/folders:** notes.
- **Expected output:** Clear bundling spec.
- **Verification:** Spec references fonts/icons to bundle.
- **Risk/notes:** Licensing check needed (8.4.1).

**Task 2.5.10 — Confirm no premature link rewrite**
- **Description:** Ensure external font/icon `<link>`s are not rewritten to local files yet.
- **Files/folders:** `POS.Desktop/Assets/ui/*`.
- **Expected output:** Google Fonts / Material Symbols still load as characterized dependencies.
- **Verification:** No local font bundling changes before Phase 8.4.
- **Risk/notes:** Bundling is strictly Phase 8.4.

### Milestone 2.6 — Dark UI/UX modernization (shared `app.css` design system)

**Task 2.6.1 — Define dark terminal tokens**
- **Description:** Create the shared `app.css` token layer for charcoal surfaces, IMAGYN green, semantic states, borders, type, radii, and shadows.
- **Files/folders:** `docs/ui-prototype/screens/app.css`, `POS.Desktop/Assets/ui/app.css`.
- **Expected output:** One reusable dark design system.
- **Verification:** Tokens exist and override legacy per-screen light variables.
- **Risk/notes:** Keep receipt/Z-report paper readable; do not globally darken pure white paper blindly.

**Task 2.6.2 — Link app.css from every screen**
- **Description:** Add the shared stylesheet link to all 7 screens.
- **Files/folders:** all 7 `.html` screens in both folders.
- **Expected output:** Every screen consumes the shared design system.
- **Verification:** Grep confirms `app.css` in all 14 screen files.
- **Risk/notes:** Preserve existing Google Font/Icon links until 8.4.

**Task 2.6.3 — Normalize legacy light surfaces**
- **Description:** Override old hardcoded light cards, panels, tables, tabs, inputs, and badges.
- **Files/folders:** `app.css` plus screen-specific style blocks.
- **Expected output:** No accidental bright panels except intentional paper surfaces.
- **Verification:** Static scan and visual pass show no unreadable light/dark combinations.
- **Risk/notes:** Old inline styles may need `!important` token remaps.

**Task 2.6.4 — Upgrade touch ergonomics**
- **Description:** Ensure primary CTAs, numpads, tender tabs, and cash controls feel large and stable for cashier touch use.
- **Files/folders:** `app.css`, affected screen CSS.
- **Expected output:** Comfortable operator controls.
- **Verification:** Main controls are at least 44px high/wide where practical.
- **Risk/notes:** Avoid hover transforms that shift layout.

**Task 2.6.5 — Verify readable states**
- **Description:** Check normal, hover, active, disabled, selected, warning, error, and success states.
- **Files/folders:** all 7 screens.
- **Expected output:** High-contrast states across the terminal.
- **Verification:** No dark text on dark surfaces and no light text on light surfaces.
- **Risk/notes:** Pay special attention to pills, badges, modals, and disabled buttons.

**Task 2.6.6 — Keep JS hooks stable**
- **Description:** Preserve existing IDs/classes/functions used by the demo scripts and future bridge work.
- **Files/folders:** all 7 screens.
- **Expected output:** UI styling changes do not break current screen behavior.
- **Verification:** Hooks such as `operator-grid`, `pin-dots`, `login-btn`, `cart-items-list`, `denom-list`, and tender tabs remain present.
- **Risk/notes:** Do not rename script-targeted elements for styling convenience.

**Task 2.6.7 — Remove production-unfit visible wording**
- **Description:** Confirm demo shortcut bars, visible credential hints, and simulator-only copy are absent.
- **Files/folders:** all 7 screens.
- **Expected output:** Cashier-facing copy reads production-ready.
- **Verification:** Grep for Tailwind CDN, simulator references, demo/security hints, and visible PIN hints.
- **Risk/notes:** Demo data may still exist in JS until Phase 3/5, but it should not be visible as a security hint.

**Task 2.6.8 — Sync source and production copies**
- **Description:** Copy the modernized source screens and `app.css` into `POS.Desktop/Assets/ui/`.
- **Files/folders:** both UI folders.
- **Expected output:** Source and production assets match.
- **Verification:** Hash-compare all 7 screens and `app.css`.
- **Risk/notes:** Do this after every UI edit until one source is promoted.

**Task 2.6.9 — Update planning docs**
- **Description:** Replace strict parity/no-redesign language with the controlled Phase 2 UI/UX modernization direction.
- **Files/folders:** `DESKTOP_UI_INTEGRATION_PLAN.md`, `DESKTOP_UI_PHASE_MILESTONES.md`, `DESKTOP_UI_MILESTONE_TASKS.md`.
- **Expected output:** Roadmap matches the new UI direction.
- **Verification:** Search for conflicting `do not redesign`, `pixel-identical`, `byte-identical prototype copy`, and `script-only` wording.
- **Risk/notes:** Do not rewrite unrelated backend/sync/hardware phases.

**Task 2.6.10 — Build and handoff review**
- **Description:** Run a full solution build and record any visual-review gaps before Phase 3.
- **Files/folders:** `POS.slnx`, UI assets.
- **Expected output:** Build passes; remaining visual QA items are explicit.
- **Verification:** `dotnet build POS.slnx` succeeds.
- **Risk/notes:** Build success does not replace manual WebView2 visual sign-off.

---

## Phase 3 — Replace fake browser state (establish the bridge)

### Milestone 3.1 — Bridge transport foundation

**Task 3.1.1 — Enable WebMessageReceived**
- **Description:** Subscribe to `CoreWebView2.WebMessageReceived`.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`.
- **Expected output:** Inbound message hook.
- **Verification:** Handler fires on a test message.
- **Risk/notes:** Runs on UI thread.

**Task 3.1.2 — Create the host object skeleton**
- **Description:** Add `Shell/PosHostApi.cs` as the COM-visible host object.
- **Files/folders:** `POS.Desktop/Shell/PosHostApi.cs` (new).
- **Expected output:** Host object class.
- **Verification:** Compiles; COM-visible.
- **Risk/notes:** COM visibility attributes required.

**Task 3.1.3 — Register the host object**
- **Description:** `AddHostObjectToScript("pos", …)` after init.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`.
- **Expected output:** `window.chrome.webview.hostObjects.pos` available.
- **Verification:** Reachable from JS.
- **Risk/notes:** Register after CoreWebView2 init.

**Task 3.1.4 — Add a JS ping sender**
- **Description:** Minimal script helper to send a ping.
- **Files/folders:** `POS.Desktop/Assets/ui/` (shared `<script>` helper).
- **Expected output:** JS can send a message.
- **Verification:** Message reaches C#.
- **Risk/notes:** Transport only — no logic.

**Task 3.1.5 — Echo a response in C#**
- **Description:** Reply to the ping with a pong.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`.
- **Expected output:** Round-trip works.
- **Verification:** JS receives pong.
- **Risk/notes:** Confirms both directions.

**Task 3.1.6 — Marshal handlers correctly**
- **Description:** Ensure async work doesn't block the UI thread.
- **Files/folders:** `POS.Desktop/Shell/`.
- **Expected output:** Non-blocking handling.
- **Verification:** UI stays responsive.
- **Risk/notes:** Marshal back to UI for posting replies.

**Task 3.1.7 — Confirm reachability from any screen**
- **Description:** Verify the host object works on every screen.
- **Files/folders:** `POS.Desktop/Assets/ui/*`.
- **Expected output:** Universal access.
- **Verification:** Ping works from each screen.
- **Risk/notes:** Re-injected on navigation.

**Task 3.1.8 — Add basic message logging**
- **Description:** Log inbound/outbound messages (no sensitive data).
- **Files/folders:** `POS.Desktop/Shell/`.
- **Expected output:** Bridge traffic visibility.
- **Verification:** Messages logged.
- **Risk/notes:** Scrub secrets (PINs) even now.

**Task 3.1.9 — Test ping/pong round-trip**
- **Description:** End-to-end transport validation.
- **Files/folders:** runtime.
- **Expected output:** Reliable round-trip.
- **Verification:** Consistent pong.
- **Risk/notes:** Baseline for contract milestone.

**Task 3.1.10 — Document transport options**
- **Description:** Note when to use host object vs `postMessage`.
- **Files/folders:** repo notes.
- **Expected output:** Transport guidance.
- **Verification:** Doc exists.
- **Risk/notes:** Consistency across handlers.

### Milestone 3.2 — Bridge contract & message envelope

**Task 3.2.1 — Define the envelope schema**
- **Description:** Specify `type`, `requestId`, `payload`, `ok`, `error`.
- **Files/folders:** `POS.Desktop/Bridge/` (new).
- **Expected output:** Envelope spec.
- **Verification:** Schema documented.
- **Risk/notes:** Version the envelope.

**Task 3.2.2 — Create envelope DTOs**
- **Description:** Add request/response DTO types.
- **Files/folders:** `POS.Desktop/Bridge/*`.
- **Expected output:** DTO classes.
- **Verification:** Compile.
- **Risk/notes:** Keep DTOs minimal.

**Task 3.2.3 — Set serializer settings**
- **Description:** Fix JSON casing/options (e.g., camelCase) once.
- **Files/folders:** `POS.Desktop/Bridge/*`.
- **Expected output:** Shared serializer config.
- **Verification:** Round-trip preserves shape.
- **Risk/notes:** Casing mismatch is the #1 bridge bug.

**Task 3.2.4 — Define the error model**
- **Description:** Standard `{ code, message }` error.
- **Files/folders:** `POS.Desktop/Bridge/*`.
- **Expected output:** Error DTO.
- **Verification:** Errors serialize predictably.
- **Risk/notes:** No stack traces to JS.

**Task 3.2.5 — Create the thin JS client helper**
- **Description:** A `posBridge.request(type, payload)` helper (transport only).
- **Files/folders:** `POS.Desktop/Assets/ui/` (`<script>` helper).
- **Expected output:** Promise-based client.
- **Verification:** Resolves on response; rejects on error.
- **Risk/notes:** No business logic in the helper.

**Task 3.2.6 — Implement requestId correlation**
- **Description:** Match responses to requests by id.
- **Files/folders:** `POS.Desktop/Bridge/*`, JS helper.
- **Expected output:** Correlated round-trips.
- **Verification:** Concurrent requests don't cross.
- **Risk/notes:** Timeout handling for lost replies.

**Task 3.2.7 — Handle malformed messages**
- **Description:** Return a structured error for unparseable input.
- **Files/folders:** `POS.Desktop/Shell/`, `Bridge/*`.
- **Expected output:** Graceful error.
- **Verification:** Bad input → error, no crash.
- **Risk/notes:** Defensive parsing.

**Task 3.2.8 — Handle unknown message types**
- **Description:** Return "unsupported type" error.
- **Files/folders:** `POS.Desktop/Bridge/*`.
- **Expected output:** Clear unknown-type error.
- **Verification:** Unknown type → structured error.
- **Risk/notes:** Helps catch contract drift.

**Task 3.2.9 — Document conventions**
- **Description:** Write casing/serialization/error conventions.
- **Files/folders:** repo notes.
- **Expected output:** Conventions doc.
- **Verification:** Doc exists.
- **Risk/notes:** Shared by all handler authors.

**Task 3.2.10 — Add an envelope contract test**
- **Description:** Unit test envelope (de)serialization round-trip.
- **Files/folders:** `POS.Tests/*`.
- **Expected output:** Passing contract test.
- **Verification:** Test green.
- **Risk/notes:** Guards JS↔C# shape drift.

### Milestone 3.3 — Message router & service dispatch

**Task 3.3.1 — Create the router**
- **Description:** Add `Shell/PosWebMessageRouter.cs`.
- **Files/folders:** `POS.Desktop/Shell/PosWebMessageRouter.cs` (new).
- **Expected output:** Router class.
- **Verification:** Compiles; wired to host.
- **Risk/notes:** Single dispatch point.

**Task 3.3.2 — Define the handler map**
- **Description:** Map message `type` → handler delegate/class.
- **Files/folders:** `POS.Desktop/Shell/PosWebMessageRouter.cs`.
- **Expected output:** Registration map.
- **Verification:** Lookups work.
- **Risk/notes:** Keep registration one-line.

**Task 3.3.3 — Resolve services from the host**
- **Description:** Pull handlers/services from DI.
- **Files/folders:** `POS.Desktop/Shell/`, host registrations.
- **Expected output:** DI-resolved handlers.
- **Verification:** Handler resolves.
- **Risk/notes:** Scope per message (next task).

**Task 3.3.4 — Create a per-message DI scope**
- **Description:** New scope per inbound message (DbContext lifetime).
- **Files/folders:** `POS.Desktop/Shell/PosWebMessageRouter.cs`.
- **Expected output:** Scoped handling.
- **Verification:** Scope created/disposed per message.
- **Risk/notes:** Prevents DbContext leaks.

**Task 3.3.5 — Dispatch and return response**
- **Description:** Route envelope to handler, return enveloped result.
- **Files/folders:** `POS.Desktop/Shell/`.
- **Expected output:** Working dispatch.
- **Verification:** Example handler responds.
- **Risk/notes:** Await async handlers.

**Task 3.3.6 — Unknown type → unsupported error**
- **Description:** Structured error for unmapped types.
- **Files/folders:** `POS.Desktop/Shell/`.
- **Expected output:** Clear error.
- **Verification:** Unknown type handled.
- **Risk/notes:** Mirrors 3.2.8.

**Task 3.3.7 — Handler exception → structured error + log**
- **Description:** Catch handler exceptions, convert to error, log.
- **Files/folders:** `POS.Desktop/Shell/`.
- **Expected output:** No unhandled crashes.
- **Verification:** Thrown handler → error response.
- **Risk/notes:** Don't leak internals to JS.

**Task 3.3.8 — Wire one example handler end-to-end**
- **Description:** Add a trivial handler proving the pipeline.
- **Files/folders:** `POS.Desktop/Shell/`, `Bridge/*`.
- **Expected output:** Working sample.
- **Verification:** JS call → C# → response.
- **Risk/notes:** Remove sample later if throwaway.

**Task 3.3.9 — Verify registration ergonomics**
- **Description:** Confirm adding a handler is a one-liner.
- **Files/folders:** `POS.Desktop/Shell/`.
- **Expected output:** Simple registration.
- **Verification:** New handler added quickly.
- **Risk/notes:** Sets pattern for Phases 4–5.

**Task 3.3.10 — Test dispatch + error paths**
- **Description:** Cover success, unknown, and exception paths.
- **Files/folders:** `POS.Tests/*`.
- **Expected output:** Passing tests.
- **Verification:** All paths green.
- **Risk/notes:** Foundation reliability.

### Milestone 3.4 — Operator session service

**Task 3.4.1 — Define ISessionService**
- **Description:** Contract for current operator + login time.
- **Files/folders:** `POS.Desktop/Services/Session/` (new).
- **Expected output:** Interface.
- **Verification:** Compiles.
- **Risk/notes:** Single-terminal scope.

**Task 3.4.2 — Implement the session service**
- **Description:** In-memory implementation of the contract.
- **Files/folders:** `POS.Desktop/Services/Session/*`.
- **Expected output:** Working service.
- **Verification:** Set/get/clear works.
- **Risk/notes:** Process state only.

**Task 3.4.3 — Register in the host**
- **Description:** Register as singleton.
- **Files/folders:** `POS.Desktop/App.xaml.cs`.
- **Expected output:** Resolvable service.
- **Verification:** Resolves.
- **Risk/notes:** Singleton fits a single operator.

**Task 3.4.4 — Add get-session handler**
- **Description:** Bridge handler returning current session.
- **Files/folders:** `POS.Desktop/Shell/`, `Bridge/*`.
- **Expected output:** Session query over bridge.
- **Verification:** JS reads session.
- **Risk/notes:** Return safe fields only.

**Task 3.4.5 — Add clear-session (logout) handler**
- **Description:** Bridge handler to clear session.
- **Files/folders:** `POS.Desktop/Shell/`, `Services/Session/*`.
- **Expected output:** Logout works.
- **Verification:** Session cleared.
- **Risk/notes:** Used by shift close.

**Task 3.4.6 — Expose login-time/operator fields**
- **Description:** Provide operator id/name/role/login time.
- **Files/folders:** `POS.Desktop/Services/Session/*`, `Bridge/*`.
- **Expected output:** Header/badge data available.
- **Verification:** Screens can show operator.
- **Risk/notes:** No PINs in session payload.

**Task 3.4.7 — Ensure no operator state in browser**
- **Description:** Remove reliance on `localStorage.terminal_operator`.
- **Files/folders:** `POS.Desktop/Assets/ui/*` (`<script>` only).
- **Expected output:** Browser-free operator state.
- **Verification:** No operator localStorage usage.
- **Risk/notes:** Full removal completed in 3.5.

**Task 3.4.8 — Clear session on logout/shift close**
- **Description:** Hook session clearing to those events.
- **Files/folders:** `POS.Desktop/Services/Session/*`.
- **Expected output:** Consistent lifecycle.
- **Verification:** Session ends on close.
- **Risk/notes:** Coordinate with Phase 5.6.

**Task 3.4.9 — Unit test session lifecycle**
- **Description:** Test set/get/clear semantics.
- **Files/folders:** `POS.Tests/*`.
- **Expected output:** Passing tests.
- **Verification:** Green.
- **Risk/notes:** Simple but important.

**Task 3.4.10 — Document session model**
- **Description:** Note that session is process state (single terminal/operator).
- **Files/folders:** repo notes.
- **Expected output:** Documented model.
- **Verification:** Doc exists.
- **Risk/notes:** Avoid over-engineering multi-user.

### Milestone 3.5 — Swap browser state for bridge (login PIN proof)

**Task 3.5.1 — Identify storage usage in login**
- **Description:** Find all `localStorage`/`sessionStorage` in `terminal_login.html`.
- **Files/folders:** `POS.Desktop/Assets/ui/terminal_login.html`.
- **Expected output:** Usage inventory.
- **Verification:** All references found.
- **Risk/notes:** Read-only first.

**Task 3.5.2 — Add a validatePin handler (stub)**
- **Description:** Bridge handler with a deterministic stub validator.
- **Files/folders:** `POS.Desktop/Shell/`, `Services/Auth/` (placeholder), `Bridge/*`.
- **Expected output:** PIN check over bridge.
- **Verification:** Stub returns expected result.
- **Risk/notes:** Real `Employee` validation in Phase 5.1.

**Task 3.5.3 — Replace in-JS PIN check with bridge call**
- **Description:** Swap `operators[]`/PIN logic for `posBridge.request('validatePin', …)`.
- **Files/folders:** `terminal_login.html` (`<script>` only).
- **Expected output:** UI delegates to C#.
- **Verification:** No PIN comparison in JS.
- **Risk/notes:** Keep operator-grid + keypad UX intact.

**Task 3.5.4 — Set session on success**
- **Description:** On valid PIN, set `ISessionService`.
- **Files/folders:** `POS.Desktop/Shell/`, `Services/Session/*`.
- **Expected output:** Session established.
- **Verification:** Session reflects operator.
- **Risk/notes:** Single source of truth.

**Task 3.5.5 — Remove terminal_operator writes**
- **Description:** Delete `localStorage.terminal_operator` usage.
- **Files/folders:** `terminal_login.html` (`<script>` only).
- **Expected output:** No operator localStorage.
- **Verification:** Diff confirms removal.
- **Risk/notes:** Script-only change.

**Task 3.5.6 — Preserve login UX**
- **Description:** Confirm operator grid + 4-digit keypad unchanged.
- **Files/folders:** `terminal_login.html` (markup untouched).
- **Expected output:** Identical login UI.
- **Verification:** Visual parity holds.
- **Risk/notes:** Never username/password fields.

**Task 3.5.7 — Wire success → shift_open**
- **Description:** Navigate to `shift_open.html` on success.
- **Files/folders:** `terminal_login.html` (`<script>` only).
- **Expected output:** Correct transition.
- **Verification:** Navigates on valid PIN.
- **Risk/notes:** Mirrors prototype flow.

**Task 3.5.8 — Wire failure → existing error UI**
- **Description:** Use the prototype's shake/toast on invalid PIN.
- **Files/folders:** `terminal_login.html` (`<script>` only).
- **Expected output:** Error feedback preserved.
- **Verification:** Invalid PIN shows existing UX.
- **Risk/notes:** Don't redesign feedback.

**Task 3.5.9 — Confirm no storage on login path**
- **Description:** Verify zero `localStorage`/`sessionStorage` reads/writes during login.
- **Files/folders:** `terminal_login.html`.
- **Expected output:** Storage-free login.
- **Verification:** Diff + runtime check.
- **Risk/notes:** Proves the bridge pattern.

**Task 3.5.10 — Test login round-trip**
- **Description:** End-to-end login via bridge.
- **Files/folders:** runtime.
- **Expected output:** Working login flow.
- **Verification:** Valid/invalid both behave.
- **Risk/notes:** Phase 5.1 only swaps the validator body.

---

## Phase 4 — Connect SQLite / local services (real data in)

### Milestone 4.1 — Real provisioned-terminal context

**Task 4.1.1 — Design the provisioning record**
- **Description:** Define how tenant/location/terminal IDs persist locally.
- **Files/folders:** `POS.Desktop/Services/Provisioning/` (new), `Data/`.
- **Expected output:** Persistence design.
- **Verification:** Design documented.
- **Risk/notes:** DB table vs settings — decide here.

**Task 4.1.2 — Implement real IProvisionedTerminalContext**
- **Description:** Create a real context populated from the persisted record.
- **Files/folders:** `POS.Desktop/Services/Provisioning/*`, `POS.Shared/Contracts`.
- **Expected output:** Real context implementation.
- **Verification:** Returns valid IDs when provisioned.
- **Risk/notes:** Mirror `IProvisionedTerminalContext` contract exactly.

**Task 4.1.3 — Load provisioning state at startup**
- **Description:** Read the record during host startup.
- **Files/folders:** `POS.Desktop/App.xaml.cs`, `Services/Provisioning/*`.
- **Expected output:** State loaded before screens.
- **Verification:** Context reflects stored state.
- **Risk/notes:** Handle "no record yet."

**Task 4.1.4 — Replace NoProvisionedTerminalContext registration**
- **Description:** Register the real context in the host.
- **Files/folders:** `POS.Desktop/App.xaml.cs`.
- **Expected output:** Real context in DI.
- **Verification:** Resolves to real impl.
- **Risk/notes:** Keep fail-closed fallback when unprovisioned.

**Task 4.1.5 — Ensure consistency across DB scopes**
- **Description:** Same provisioning values across all DbContext scopes.
- **Files/folders:** `POS.Desktop/Data/PosLocalDbContext.cs`, `Services/Provisioning/*`.
- **Expected output:** Consistent tenant filters.
- **Verification:** Filters resolve identically per request.
- **Risk/notes:** Singleton provisioning, scoped DbContext.

**Task 4.1.6 — Handle the unprovisioned state**
- **Description:** Keep fail-closed behavior (invalid IDs) until provisioned.
- **Files/folders:** `Services/Provisioning/*`.
- **Expected output:** Safe default.
- **Verification:** Unprovisioned → no data.
- **Risk/notes:** Matches current `NoProvisionedTerminalContext` semantics.

**Task 4.1.7 — Guard against half-provisioned state**
- **Description:** Treat partial records as unprovisioned.
- **Files/folders:** `Services/Provisioning/*`.
- **Expected output:** Atomic provisioning state.
- **Verification:** Partial record → fail-closed.
- **Risk/notes:** Avoid leaking one tenant's data.

**Task 4.1.8 — Verify provisioned reads return rows**
- **Description:** Confirm tenant-filtered queries work when provisioned.
- **Files/folders:** `Data/`, test harness.
- **Expected output:** Rows returned.
- **Verification:** Query returns seeded data.
- **Risk/notes:** Depends on 4.3 seed.

**Task 4.1.9 — Verify unprovisioned reads are empty**
- **Description:** Confirm filters block data when unprovisioned.
- **Files/folders:** `Data/`, test harness.
- **Expected output:** Empty results.
- **Verification:** No rows leak.
- **Risk/notes:** Security-relevant.

**Task 4.1.10 — Integration test provisioned vs not**
- **Description:** Automated test for both states.
- **Files/folders:** `POS.Tests/*`.
- **Expected output:** Passing tests.
- **Verification:** Both paths green.
- **Risk/notes:** The lynchpin — cover thoroughly.

### Milestone 4.2 — Provisioning persistence & screen wiring

**Task 4.2.1 — Define the persistence store**
- **Description:** Create the table/settings store for provisioning.
- **Files/folders:** `POS.Desktop/Data/*` or settings, migration if DB.
- **Expected output:** Persistence target.
- **Verification:** Store created on migrate.
- **Risk/notes:** Keep it minimal.

**Task 4.2.2 — Add provisionTerminal handler**
- **Description:** Bridge handler that persists provisioning.
- **Files/folders:** `POS.Desktop/Shell/`, `Services/Provisioning/*`, `Bridge/*`.
- **Expected output:** Provisioning over bridge.
- **Verification:** Record persisted.
- **Risk/notes:** Validate inputs.

**Task 4.2.3 — Add getProvisioningStatus handler**
- **Description:** Bridge handler returning current status.
- **Files/folders:** `POS.Desktop/Shell/`, `Bridge/*`.
- **Expected output:** Status query.
- **Verification:** Reflects stored state.
- **Risk/notes:** Used for boot gating.

**Task 4.2.4 — Wire provision_terminal.html to bridge**
- **Description:** Replace fake provisioning with bridge calls (script-only).
- **Files/folders:** `POS.Desktop/Assets/ui/provision_terminal.html` (`<script>`).
- **Expected output:** Real provisioning UI flow.
- **Verification:** Form drives the service.
- **Risk/notes:** Keep store/terminal-code UI unchanged.

**Task 4.2.5 — Replace setTimeout progress with real steps**
- **Description:** Drive the progress log from real step events.
- **Files/folders:** `provision_terminal.html` (`<script>`), `Services/Provisioning/*`.
- **Expected output:** Honest progress reporting.
- **Verification:** Steps reflect real work.
- **Risk/notes:** No fake delays.

**Task 4.2.6 — Remove terminal_config localStorage**
- **Description:** Delete the localStorage config usage.
- **Files/folders:** `provision_terminal.html` (`<script>`).
- **Expected output:** Browser-free config.
- **Verification:** Diff confirms removal.
- **Risk/notes:** Config now in C#.

**Task 4.2.7 — Persist tenant/location/terminal durably**
- **Description:** Ensure values survive restart.
- **Files/folders:** `Services/Provisioning/*`, store.
- **Expected output:** Durable provisioning.
- **Verification:** Restart keeps provisioning.
- **Risk/notes:** Tie to 4.1 loader.

**Task 4.2.8 — Verify provisioning survives restart**
- **Description:** Launch twice; confirm still provisioned.
- **Files/folders:** runtime.
- **Expected output:** Persistent state.
- **Verification:** Second launch skips provisioning.
- **Risk/notes:** Boot should route past provisioning.

**Task 4.2.9 — Support controlled re-provisioning**
- **Description:** Allow re-provisioning via a guarded path.
- **Files/folders:** `Services/Provisioning/*`, `provision_terminal.html`.
- **Expected output:** Re-provision capability.
- **Verification:** Re-provision updates record.
- **Risk/notes:** Consider data implications of switching tenant.

**Task 4.2.10 — Test provisioning without catalog seed**
- **Description:** Confirm provisioning succeeds before seeding exists.
- **Files/folders:** runtime.
- **Expected output:** Decoupled provisioning.
- **Verification:** Provision completes; seed handled in 4.3.
- **Risk/notes:** Catalog from API is Phase 6.

### Milestone 4.3 — Minimal local catalog schema & seed

**Task 4.3.1 — Decide catalog representation**
- **Description:** Mirror `POS.Shared` central entities locally vs dedicated local tables.
- **Files/folders:** `POS.Desktop/Data/*`, `POS.Shared/Domain/Entities/Central/*`.
- **Expected output:** Decision recorded.
- **Verification:** Approach documented.
- **Risk/notes:** Reuse shared models where possible.

**Task 4.3.2 — Add/extend EF entities**
- **Description:** Define the local catalog entities.
- **Files/folders:** `POS.Desktop/Data/*`.
- **Expected output:** Entities ready.
- **Verification:** Compile.
- **Risk/notes:** Keep tenant scoping consistent.

**Task 4.3.3 — Add EF configurations**
- **Description:** Fluent configs for the new entities.
- **Files/folders:** `POS.Desktop/Data/Configurations/Local/*`.
- **Expected output:** Configs applied.
- **Verification:** Model builds.
- **Risk/notes:** Follow existing config patterns.

**Task 4.3.4 — Create a migration**
- **Description:** Add a migration for catalog tables.
- **Files/folders:** `POS.Desktop/Data/Migrations/Local/*`.
- **Expected output:** New migration.
- **Verification:** Applies cleanly.
- **Risk/notes:** Don't break the existing Phase1 migration.

**Task 4.3.5 — Define a minimal seed dataset**
- **Description:** Small set of items/categories/prices/tax/identifiers.
- **Files/folders:** `POS.Desktop/` seed source.
- **Expected output:** Seed data defined.
- **Verification:** Covers checkout needs.
- **Risk/notes:** Mirror prototype `ITEMS[]` shape for parity.

**Task 4.3.6 — Implement an idempotent seed routine**
- **Description:** Seed only if absent.
- **Files/folders:** `POS.Desktop/` seed code.
- **Expected output:** Safe seeding.
- **Verification:** Re-run is a no-op.
- **Risk/notes:** Avoid duplicate rows.

**Task 4.3.7 — Run seed post-provision**
- **Description:** Trigger seeding after provisioning completes.
- **Files/folders:** `Services/Provisioning/*`, seed code.
- **Expected output:** Catalog present after provision.
- **Verification:** Data exists post-provision.
- **Risk/notes:** Tenant-scope the seed.

**Task 4.3.8 — Verify seed re-run is no-op**
- **Description:** Confirm idempotency in practice.
- **Files/folders:** runtime.
- **Expected output:** No duplicates.
- **Verification:** Row counts stable.
- **Risk/notes:** Re-provision interplay.

**Task 4.3.9 — Apply migration on startup**
- **Description:** Ensure the new migration runs at boot (extends 1.4).
- **Files/folders:** startup code.
- **Expected output:** Tables present.
- **Verification:** Fresh DB has catalog tables.
- **Risk/notes:** Migration ordering.

**Task 4.3.10 — Test seeded catalog presence**
- **Description:** Confirm seeded data is queryable when provisioned.
- **Files/folders:** `POS.Tests/*`.
- **Expected output:** Passing test.
- **Verification:** Items returned.
- **Risk/notes:** Feeds 4.4.

### Milestone 4.4 — Catalog read service (replace ITEMS[])

**Task 4.4.1 — Define ICatalogService**
- **Description:** List/search/by-identifier read operations.
- **Files/folders:** `POS.Desktop/Services/Catalog/` (new).
- **Expected output:** Interface.
- **Verification:** Compiles.
- **Risk/notes:** Read-only service.

**Task 4.4.2 — Implement the catalog service**
- **Description:** Query SQLite via `PosLocalDbContext`.
- **Files/folders:** `POS.Desktop/Services/Catalog/*`.
- **Expected output:** Working reads.
- **Verification:** Returns seeded data.
- **Risk/notes:** Respect tenant filters.

**Task 4.4.3 — Add catalog DTOs**
- **Description:** Bridge DTOs for items/categories.
- **Files/folders:** `POS.Desktop/Bridge/*`.
- **Expected output:** DTOs.
- **Verification:** Serialize cleanly.
- **Risk/notes:** Match fields the UI needs.

**Task 4.4.4 — Add list/search/scan handlers**
- **Description:** Bridge handlers for catalog operations.
- **Files/folders:** `POS.Desktop/Shell/`, `Bridge/*`.
- **Expected output:** Catalog over bridge.
- **Verification:** Handlers respond.
- **Risk/notes:** Paginate if needed.

**Task 4.4.5 — Wire the product grid**
- **Description:** Render grid from service (script-only).
- **Files/folders:** `main_checkout.html` (`<script>`).
- **Expected output:** Real product grid.
- **Verification:** Items render from DB.
- **Risk/notes:** Preserve card design.

**Task 4.4.6 — Wire category chips**
- **Description:** Drive categories from service.
- **Files/folders:** `main_checkout.html` (`<script>`).
- **Expected output:** Real categories.
- **Verification:** Filtering works.
- **Risk/notes:** Keep chip styling.

**Task 4.4.7 — Wire search + scan-by-code**
- **Description:** Search box and scan resolve via service/`ItemIdentifier`.
- **Files/folders:** `main_checkout.html` (`<script>`), `Services/Catalog/*`.
- **Expected output:** Working search/scan.
- **Verification:** Code lookups succeed.
- **Risk/notes:** Hardware scanner wiring is Phase 7.4.

**Task 4.4.8 — Remove ITEMS[]/CATEGORIES[]**
- **Description:** Delete demo arrays from checkout.
- **Files/folders:** `main_checkout.html` (`<script>`).
- **Expected output:** No demo data.
- **Verification:** Diff confirms removal.
- **Risk/notes:** Cart logic untouched (Phase 5.3).

**Task 4.4.9 — Index identifiers/SKUs**
- **Description:** Add indexes for fast lookup/search.
- **Files/folders:** `POS.Desktop/Data/Configurations/Local/*`, migration.
- **Expected output:** Indexed columns.
- **Verification:** Search performant.
- **Risk/notes:** Migration update.

**Task 4.4.10 — Test catalog rendering + search**
- **Description:** Verify grid, chips, search, scan end-to-end.
- **Files/folders:** runtime, `POS.Tests/*`.
- **Expected output:** Working catalog UI on real data.
- **Verification:** All paths pass.
- **Risk/notes:** Visual parity preserved.

### Milestone 4.5 — Data-access conventions & tenant-filter validation

**Task 4.5.1 — Draft data-access conventions**
- **Description:** Document scoping/async/disposal rules.
- **Files/folders:** repo notes.
- **Expected output:** Conventions draft.
- **Verification:** Draft exists.
- **Risk/notes:** Prevents Phase 5 divergence.

**Task 4.5.2 — Standardize per-message scope usage**
- **Description:** Codify the per-message DbContext scope pattern.
- **Files/folders:** `POS.Desktop/Shell/`, `Services/*`.
- **Expected output:** Consistent scoping.
- **Verification:** Services follow it.
- **Risk/notes:** Ties to 3.3.4.

**Task 4.5.3 — Define service base patterns**
- **Description:** Establish repository/service conventions.
- **Files/folders:** `POS.Desktop/Services/*`.
- **Expected output:** Pattern reference.
- **Verification:** Documented + sample.
- **Risk/notes:** Avoid over-abstraction.

**Task 4.5.4 — Test: provisioned reads return data**
- **Description:** Integration test under provisioned context.
- **Files/folders:** `POS.Tests/*`.
- **Expected output:** Passing test.
- **Verification:** Data returned.
- **Risk/notes:** Use file/in-memory SQLite.

**Task 4.5.5 — Test: unprovisioned reads empty**
- **Description:** Integration test under unprovisioned context.
- **Files/folders:** `POS.Tests/*`.
- **Expected output:** Passing test.
- **Verification:** No rows.
- **Risk/notes:** Tenant isolation proof.

**Task 4.5.6 — Verify scope create/dispose**
- **Description:** Confirm scopes are disposed per message.
- **Files/folders:** `POS.Desktop/Shell/`.
- **Expected output:** No leaks.
- **Verification:** Diagnostics show disposal.
- **Risk/notes:** Memory/connection hygiene.

**Task 4.5.7 — Set up SQLite test harness**
- **Description:** Shared test fixture for DB tests.
- **Files/folders:** `POS.Tests/*`.
- **Expected output:** Reusable harness.
- **Verification:** Tests use it.
- **Risk/notes:** Seed control per test.

**Task 4.5.8 — Validate filters across entities**
- **Description:** Check query filters on each local entity.
- **Files/folders:** `POS.Tests/*`, `Data/*`.
- **Expected output:** Consistent isolation.
- **Verification:** All entities filter correctly.
- **Risk/notes:** Catch any missing filter.

**Task 4.5.9 — Document conventions for Phase 5**
- **Description:** Finalize conventions doc for flow authors.
- **Files/folders:** repo notes.
- **Expected output:** Final conventions.
- **Verification:** Doc complete.
- **Risk/notes:** Referenced by all Phase 5 milestones.

**Task 4.5.10 — Review/sign-off conventions**
- **Description:** Team review of conventions.
- **Files/folders:** repo notes.
- **Expected output:** Sign-off.
- **Verification:** Approved.
- **Risk/notes:** Gate for Phase 5 start.

---

## Phase 5 — Real flows (login, shift, order, payment, cash control, Z-report)

### Milestone 5.1 — Authentication & login service

**Task 5.1.1 — Define IAuthService**
- **Description:** Contract for PIN validation + manager-PIN checks.
- **Files/folders:** `POS.Desktop/Services/Auth/` (new).
- **Expected output:** Interface.
- **Verification:** Compiles.
- **Risk/notes:** Reused by cash control (5.5).

**Task 5.1.2 — Implement the auth service**
- **Description:** Validate PIN against `Employee` for the provisioned location.
- **Files/folders:** `POS.Desktop/Services/Auth/*`, `POS.Shared` (`Employee`).
- **Expected output:** Working validation.
- **Verification:** Known operator validates.
- **Risk/notes:** Respect tenant/location scope.

**Task 5.1.3 — Implement secure PIN handling**
- **Description:** Hash/compare PINs; never store plaintext.
- **Files/folders:** `POS.Desktop/Services/Auth/*`.
- **Expected output:** Secure comparison.
- **Verification:** No plaintext PINs anywhere.
- **Risk/notes:** Align with `Employee` credential storage.

**Task 5.1.4 — Resolve operator for the location**
- **Description:** Filter operators by provisioned location/role.
- **Files/folders:** `Services/Auth/*`, `POS.Shared` (`EmployeeLocationRole`).
- **Expected output:** Location-scoped operators.
- **Verification:** Only valid operators accepted.
- **Risk/notes:** Drives the login grid too.

**Task 5.1.5 — Create TerminalSession on success**
- **Description:** Persist a `TerminalSession` for the login.
- **Files/folders:** `Services/Auth/*`, `POS.Shared` (`TerminalSession`).
- **Expected output:** Session record.
- **Verification:** Row created on login.
- **Risk/notes:** Append-only semantics.

**Task 5.1.6 — Set ISessionService on success**
- **Description:** Populate in-memory session.
- **Files/folders:** `Services/Auth/*`, `Services/Session/*`.
- **Expected output:** Active session.
- **Verification:** Session reflects operator.
- **Risk/notes:** Ties to 3.4.

**Task 5.1.7 — Swap the stub validator**
- **Description:** Replace the 3.5 stub with real validation in the handler.
- **Files/folders:** `POS.Desktop/Shell/`, `Services/Auth/*`.
- **Expected output:** Real PIN check.
- **Verification:** Login uses `Employee` data.
- **Risk/notes:** UI/bridge unchanged — only the validator body.

**Task 5.1.8 — Handle invalid/lockout/empty states**
- **Description:** Define behavior for wrong PIN, lockout, no operators.
- **Files/folders:** `Services/Auth/*`, `terminal_login.html` (`<script>`).
- **Expected output:** Robust edge handling.
- **Verification:** Each state behaves correctly.
- **Risk/notes:** Use existing error UX.

**Task 5.1.9 — Ensure no PIN logging**
- **Description:** Scrub PINs from all logs/errors.
- **Files/folders:** `Services/Auth/*`, `Shell/`.
- **Expected output:** PIN-free logs.
- **Verification:** Logs contain no PINs.
- **Risk/notes:** Security requirement.

**Task 5.1.10 — Unit test valid/invalid paths**
- **Description:** Cover validation outcomes.
- **Files/folders:** `POS.Tests/*`.
- **Expected output:** Passing tests.
- **Verification:** Green.
- **Risk/notes:** Include lockout logic.

### Milestone 5.2 — Shift open service

**Task 5.2.1 — Define IShiftService.OpenShift**
- **Description:** Contract to open a shift with a float.
- **Files/folders:** `POS.Desktop/Services/Shifts/` (new).
- **Expected output:** Interface method.
- **Verification:** Compiles.
- **Risk/notes:** Shared with close (5.6).

**Task 5.2.2 — Implement OpenShift**
- **Description:** Persist a `Shift` (open) with opening float.
- **Files/folders:** `Services/Shifts/*`, `POS.Shared` (`Shift`).
- **Expected output:** Open shift row.
- **Verification:** Row created with float.
- **Risk/notes:** Append-only.

**Task 5.2.3 — Add openShift handler**
- **Description:** Bridge handler for opening a shift.
- **Files/folders:** `POS.Desktop/Shell/`, `Bridge/*`.
- **Expected output:** Open over bridge.
- **Verification:** Handler creates shift.
- **Risk/notes:** Validate float input.

**Task 5.2.4 — Wire shift_open.html**
- **Description:** Drive open via bridge (script-only).
- **Files/folders:** `shift_open.html` (`<script>`).
- **Expected output:** Real shift open UI.
- **Verification:** Form opens a shift.
- **Risk/notes:** Keep float quick-amounts UI.

**Task 5.2.5 — Remove pos_shift_* sessionStorage**
- **Description:** Delete sessionStorage shift flags.
- **Files/folders:** `shift_open.html` (`<script>`).
- **Expected output:** Browser-free shift state.
- **Verification:** Diff confirms removal.
- **Risk/notes:** State now in `Shift`.

**Task 5.2.6 — Guard against double-open**
- **Description:** Prevent opening when one is already open.
- **Files/folders:** `Services/Shifts/*`.
- **Expected output:** Single active shift.
- **Verification:** Second open rejected.
- **Risk/notes:** Clear error to UI.

**Task 5.2.7 — Define the app-wide "shift open" gate**
- **Description:** Gate other screens on an open shift.
- **Files/folders:** `Services/Shifts/*`, `Shell/`, screens.
- **Expected output:** Locked-until-open behavior.
- **Verification:** Checkout requires open shift.
- **Risk/notes:** Mirrors prototype lock toasts.

**Task 5.2.8 — Source checklist/policy from config**
- **Description:** Pull policy limits/checklist from config.
- **Files/folders:** `appsettings.json`, `Services/Shifts/*`, `shift_open.html`.
- **Expected output:** Config-driven policies.
- **Verification:** Values render from config.
- **Risk/notes:** No hardcoded limits in JS.

**Task 5.2.9 — Navigate to checkout on open**
- **Description:** Transition to `main_checkout.html`.
- **Files/folders:** `shift_open.html` (`<script>`).
- **Expected output:** Correct transition.
- **Verification:** Opens checkout after success.
- **Risk/notes:** Preserve success overlay.

**Task 5.2.10 — Test open + unlock**
- **Description:** End-to-end shift open.
- **Files/folders:** runtime, `POS.Tests/*`.
- **Expected output:** Working open + gate.
- **Verification:** Passes.
- **Risk/notes:** Foundation for sales.

### Milestone 5.3 — Order / cart service

**Task 5.3.1 — Define IOrderService**
- **Description:** Draft operations: add/qty/remove/discount.
- **Files/folders:** `POS.Desktop/Services/Orders/` (new).
- **Expected output:** Interface.
- **Verification:** Compiles.
- **Risk/notes:** Draft vs committed order distinction.

**Task 5.3.2 — Decide draft persistence**
- **Description:** In-memory draft vs DB-backed draft.
- **Files/folders:** `Services/Orders/*`.
- **Expected output:** Decision recorded.
- **Verification:** Documented.
- **Risk/notes:** Crash-recovery implications.

**Task 5.3.3 — Implement add/qty/remove**
- **Description:** Cart line operations.
- **Files/folders:** `Services/Orders/*`, `POS.Shared` (`OrderLine`).
- **Expected output:** Mutable draft.
- **Verification:** Lines update correctly.
- **Risk/notes:** Quantity bounds.

**Task 5.3.4 — Implement discount handling**
- **Description:** Amount/percentage discount with rules.
- **Files/folders:** `Services/Orders/*`, `POS.Shared` (`ReasonCode` if needed).
- **Expected output:** Discount applied.
- **Verification:** Totals reflect discount.
- **Risk/notes:** Manager approval threshold (prototype: >PKR 1000).

**Task 5.3.5 — Implement totals calculation**
- **Description:** Subtotal/discount/grand total in C#.
- **Files/folders:** `Services/Orders/*`.
- **Expected output:** Authoritative totals.
- **Verification:** Matches expected math.
- **Risk/notes:** Centralize rounding.

**Task 5.3.6 — Implement tax via TaxRule**
- **Description:** Compute tax using `TaxRule` per item.
- **Files/folders:** `Services/Orders/*`, `POS.Shared` (`TaxRule`, `ItemPrice`).
- **Expected output:** Correct tax.
- **Verification:** Tax matches rules (e.g., 5% GST, 0%, 18%).
- **Risk/notes:** Mixed tax rates per cart.

**Task 5.3.7 — Centralize money rounding**
- **Description:** Single rounding policy for currency.
- **Files/folders:** `Services/Orders/*` (shared helper).
- **Expected output:** Consistent rounding.
- **Verification:** No rounding drift.
- **Risk/notes:** Audit-sensitive.

**Task 5.3.8 — Add cart bridge handlers**
- **Description:** Handlers for all cart operations.
- **Files/folders:** `POS.Desktop/Shell/`, `Bridge/*`.
- **Expected output:** Cart over bridge.
- **Verification:** Each op round-trips.
- **Risk/notes:** Return full recomputed totals.

**Task 5.3.9 — Wire main_checkout cart + remove pos_cart**
- **Description:** Drive cart UI via bridge; delete `pos_cart` sessionStorage (script-only).
- **Files/folders:** `main_checkout.html` (`<script>`).
- **Expected output:** Real cart; no browser state.
- **Verification:** Cart works; diff confirms removal.
- **Risk/notes:** Preserve cart UI exactly.

**Task 5.3.10 — Unit test cart math + tax**
- **Description:** Cover totals/discount/tax edge cases.
- **Files/folders:** `POS.Tests/*`.
- **Expected output:** Passing tests.
- **Verification:** Green.
- **Risk/notes:** Include mixed-rate carts.

### Milestone 5.4 — Payment & completion service

**Task 5.4.1 — Define IPaymentService**
- **Description:** Tender, change, completion contract.
- **Files/folders:** `POS.Desktop/Services/Payments/` (new).
- **Expected output:** Interface.
- **Verification:** Compiles.
- **Risk/notes:** Supports split tender.

**Task 5.4.2 — Record Payment per TenderMethod**
- **Description:** Persist tenders against the order.
- **Files/folders:** `Services/Payments/*`, `POS.Shared` (`Payment`, `TenderMethod`).
- **Expected output:** Payment rows.
- **Verification:** Tenders recorded.
- **Risk/notes:** Cash/card/wallet/split.

**Task 5.4.3 — Compute cash change**
- **Description:** Change = tendered − due (cash).
- **Files/folders:** `Services/Payments/*`.
- **Expected output:** Correct change.
- **Verification:** Matches expected.
- **Risk/notes:** Use central rounding.

**Task 5.4.4 — Commit order append-only**
- **Description:** Write `Order`/`OrderLine`/`Payment` atomically.
- **Files/folders:** `Services/Payments/*`, `Services/Orders/*`, `POS.Shared`.
- **Expected output:** Committed sale.
- **Verification:** Records persisted.
- **Risk/notes:** Transaction boundaries.

**Task 5.4.5 — Enqueue SyncOutbox event**
- **Description:** Add an outbox event on completion.
- **Files/folders:** `Services/Payments/*`, `Data/LocalEntities/SyncOutbox.cs`.
- **Expected output:** Outbox row.
- **Verification:** Event enqueued.
- **Risk/notes:** Idempotency key set.

**Task 5.4.6 — Enqueue PrintQueue receipt**
- **Description:** Queue a receipt print job.
- **Files/folders:** `Services/Payments/*`, `Data/LocalEntities/PrintQueue.cs`.
- **Expected output:** Print job row.
- **Verification:** Job enqueued.
- **Risk/notes:** Printer wiring in 7.3.

**Task 5.4.7 — Render receipt from data**
- **Description:** Build receipt text from `Order`/`Payment` + `ReceiptTemplate`.
- **Files/folders:** `Services/Payments/*` or `Services/Reporting/*`, `POS.Shared` (`ReceiptTemplate`).
- **Expected output:** Data-driven receipt.
- **Verification:** Matches prototype format.
- **Risk/notes:** Remove hardcoded receipt text.

**Task 5.4.8 — Ensure idempotent completion**
- **Description:** Prevent double-charge on retry/double-click.
- **Files/folders:** `Services/Payments/*`.
- **Expected output:** Safe completion.
- **Verification:** Repeat submit = one sale.
- **Risk/notes:** Critical for money correctness.

**Task 5.4.9 — Wire payment_screen.html**
- **Description:** Drive tenders/complete via bridge; card/wallet call stubbed hardware (script-only).
- **Files/folders:** `payment_screen.html` (`<script>`).
- **Expected output:** Real payment flow.
- **Verification:** All tenders work; no `setTimeout` fakes.
- **Risk/notes:** Real pinpad in 7.6.

**Task 5.4.10 — Unit test tender/change/completion**
- **Description:** Cover cash/card/wallet/split + change + idempotency.
- **Files/folders:** `POS.Tests/*`.
- **Expected output:** Passing tests.
- **Verification:** Green.
- **Risk/notes:** Include split-tender math.

### Milestone 5.5 — Cash control service

**Task 5.5.1 — Define ICashControlService**
- **Description:** Safe drop / float injection contract.
- **Files/folders:** `POS.Desktop/Services/CashControl/` (new).
- **Expected output:** Interface.
- **Verification:** Compiles.
- **Risk/notes:** Tie to active shift.

**Task 5.5.2 — Write CashDrawerMovement**
- **Description:** Persist drops/injections.
- **Files/folders:** `Services/CashControl/*`, `POS.Shared` (`CashDrawerMovement`).
- **Expected output:** Movement rows.
- **Verification:** Rows created with type/amount.
- **Risk/notes:** Append-only.

**Task 5.5.3 — Attach reason codes**
- **Description:** Require/record `ReasonCode`.
- **Files/folders:** `Services/CashControl/*`, `POS.Shared` (`ReasonCode`).
- **Expected output:** Reason captured.
- **Verification:** Reason persisted.
- **Risk/notes:** Dropdown sourced from data.

**Task 5.5.4 — Enforce manager PIN**
- **Description:** Validate manager PIN via `IAuthService`.
- **Files/folders:** `Services/CashControl/*`, `Services/Auth/*`.
- **Expected output:** Authorized movements only.
- **Verification:** Bad PIN rejected.
- **Risk/notes:** Reuse 5.1 (no new PIN logic).

**Task 5.5.5 — Compute drawer balance**
- **Description:** Balance = float + cash sales − drops + injections.
- **Files/folders:** `Services/CashControl/*`.
- **Expected output:** Live balance.
- **Verification:** Matches expected.
- **Risk/notes:** Shared formula with 5.6.

**Task 5.5.6 — Compute threshold alerts**
- **Description:** Over-limit / safe-drop-required alerts in C#.
- **Files/folders:** `Services/CashControl/*`, config.
- **Expected output:** Alert state.
- **Verification:** Crossing limit flips banner.
- **Risk/notes:** Limits from config (prototype: 25k/20k).

**Task 5.5.7 — Add handlers + ledger query**
- **Description:** Bridge handlers for movements + ledger read.
- **Files/folders:** `POS.Desktop/Shell/`, `Bridge/*`.
- **Expected output:** Cash control over bridge.
- **Verification:** Ledger reflects movements.
- **Risk/notes:** Ledger is a query, not storage.

**Task 5.5.8 — Wire cash_control.html + remove pos_safe_drops**
- **Description:** Drive UI via bridge; delete sessionStorage (script-only).
- **Files/folders:** `cash_control.html` (`<script>`).
- **Expected output:** Real cash control.
- **Verification:** Works; diff confirms removal.
- **Risk/notes:** Preserve tabs/numpad UI.

**Task 5.5.9 — Tie movements to active shift**
- **Description:** Associate movements with the open `Shift`.
- **Files/folders:** `Services/CashControl/*`, `Services/Shifts/*`.
- **Expected output:** Shift-scoped movements.
- **Verification:** Movements link to shift.
- **Risk/notes:** Required for Z-report (5.6).

**Task 5.5.10 — Test drops/injections + alerts**
- **Description:** Cover movement math + manager auth + alerts.
- **Files/folders:** `POS.Tests/*`.
- **Expected output:** Passing tests.
- **Verification:** Green.
- **Risk/notes:** Include over-limit case.

### Milestone 5.6 — Shift close & Z-report

**Task 5.6.1 — Define IShiftService.CloseShift**
- **Description:** Close contract with counted denominations.
- **Files/folders:** `POS.Desktop/Services/Shifts/*`.
- **Expected output:** Interface method.
- **Verification:** Compiles.
- **Risk/notes:** Pairs with OpenShift.

**Task 5.6.2 — Capture denomination counts**
- **Description:** Accept counts → counted cash total.
- **Files/folders:** `Services/Shifts/*`, `shift_close.html` (`<script>`).
- **Expected output:** Counted cash.
- **Verification:** Sum matches counts.
- **Risk/notes:** Mirror prototype `DENOMS` rows (data-driven).

**Task 5.6.3 — Compute expected cash + variance**
- **Description:** Expected = float + cash sales − drops + injections; variance = counted − expected.
- **Files/folders:** `Services/Shifts/*`.
- **Expected output:** Variance value.
- **Verification:** Matches expected.
- **Risk/notes:** Reuse 5.5 formula.

**Task 5.6.4 — Define IReportingService + ZReport build**
- **Description:** Build a `ZReport` from real data.
- **Files/folders:** `POS.Desktop/Services/Reporting/` (new), `POS.Shared` (`ZReport`).
- **Expected output:** Z-report model.
- **Verification:** Fields populated from data.
- **Risk/notes:** Use real `Order`/`Payment`/movements.

**Task 5.6.5 — Aggregate sales/tender breakdown**
- **Description:** Gross/net/tax/discounts + cash/card/wallet mix.
- **Files/folders:** `Services/Reporting/*`.
- **Expected output:** Reconciliation data.
- **Verification:** Totals tie out.
- **Risk/notes:** Replaces demo metrics.

**Task 5.6.6 — Add close + z-report handlers**
- **Description:** Bridge handlers for close and report.
- **Files/folders:** `POS.Desktop/Shell/`, `Bridge/*`.
- **Expected output:** Close over bridge.
- **Verification:** Handlers respond.
- **Risk/notes:** Confirm-modal flow preserved.

**Task 5.6.7 — Wire shift_close.html + remove DENOMS[]**
- **Description:** Drive UI via bridge; delete demo metrics (script-only).
- **Files/folders:** `shift_close.html` (`<script>`).
- **Expected output:** Real close + Z-report UI.
- **Verification:** Works; diff confirms removal.
- **Risk/notes:** Preserve variance color-coding.

**Task 5.6.8 — Lock terminal back to login**
- **Description:** On close, clear session and return to login.
- **Files/folders:** `Services/Shifts/*`, `Services/Session/*`, `shift_close.html`.
- **Expected output:** Locked terminal.
- **Verification:** Returns to login.
- **Risk/notes:** Ties to 3.4.8.

**Task 5.6.9 — Add stubbed FBR seam**
- **Description:** Define the FBR submission call point (stub).
- **Files/folders:** `Services/Reporting/*`.
- **Expected output:** Integration point present.
- **Verification:** Stub invoked, no external call.
- **Risk/notes:** Real FBR in 8.5.

**Task 5.6.10 — Unit test variance/reconciliation**
- **Description:** Cover balanced/over/short cases.
- **Files/folders:** `POS.Tests/*`.
- **Expected output:** Passing tests.
- **Verification:** Green.
- **Risk/notes:** Audit-critical correctness.

---

## Phase 6 — Sync / outbox ↔ POS.Api

### Milestone 6.1 — POS.Api sync ingest endpoint (server side)

**Task 6.1.1 — Design the ingest contract**
- **Description:** Define request/response DTOs for outbox ingest.
- **Files/folders:** `POS.Shared/*` (shared DTOs), `POS.Api/Sync/*` (new).
- **Expected output:** Contract spec.
- **Verification:** DTOs documented.
- **Risk/notes:** Share DTOs with Desktop.

**Task 6.1.2 — Create the Sync structures**
- **Description:** Add the `Sync/` application/service classes.
- **Files/folders:** `POS.Api/Sync/*`.
- **Expected output:** Sync scaffolding.
- **Verification:** Compiles.
- **Risk/notes:** Folder currently empty.

**Task 6.1.3 — Add the ingest endpoint**
- **Description:** Controller/endpoint accepting a batch of events.
- **Files/folders:** `POS.Api/Controllers/*`, `POS.Api/Program.cs`.
- **Expected output:** Reachable endpoint.
- **Verification:** Accepts a batch.
- **Risk/notes:** Versioned route.

**Task 6.1.4 — Apply the PosDevice policy**
- **Description:** Require the device JWT policy.
- **Files/folders:** `POS.Api/Program.cs`, controller.
- **Expected output:** Authorized endpoint.
- **Verification:** Unauthorized → 401/403.
- **Risk/notes:** Reuse existing policy.

**Task 6.1.5 — Implement idempotent persist + dedupe**
- **Description:** Persist events keyed by idempotency/correlation id.
- **Files/folders:** `POS.Api/Sync/*`, `PosCentralDbContext`.
- **Expected output:** Dedupe on ingest.
- **Verification:** Duplicates ignored.
- **Risk/notes:** Define the dedupe key first.

**Task 6.1.6 — Ack via SyncIngestAck**
- **Description:** Return/persist acks for processed events.
- **Files/folders:** `POS.Api/Sync/*`, `POS.Shared` (`SyncIngestAck`).
- **Expected output:** Ack response.
- **Verification:** Acks returned per event.
- **Risk/notes:** Drives client cursor.

**Task 6.1.7 — Reject unauthorized callers**
- **Description:** Confirm policy enforcement.
- **Files/folders:** `POS.Api/*`.
- **Expected output:** Hardened endpoint.
- **Verification:** Non-device tokens rejected.
- **Risk/notes:** Security boundary.

**Task 6.1.8 — Handle duplicate event IDs**
- **Description:** Treat repeats as no-ops returning prior ack.
- **Files/folders:** `POS.Api/Sync/*`.
- **Expected output:** Safe replays.
- **Verification:** Replays don't double-write.
- **Risk/notes:** Idempotency correctness.

**Task 6.1.9 — Add an API integration test**
- **Description:** Test ingest happy path + dedupe + auth.
- **Files/folders:** `POS.Tests/*` (uses `Mvc.Testing`).
- **Expected output:** Passing tests.
- **Verification:** Green.
- **Risk/notes:** Existing test infra available.

**Task 6.1.10 — Document the endpoint contract**
- **Description:** Write request/response/auth docs.
- **Files/folders:** repo notes / API docs.
- **Expected output:** Endpoint contract doc.
- **Verification:** Doc exists.
- **Risk/notes:** Client depends on it.

### Milestone 6.2 — Device-authenticated HTTP client

**Task 6.2.1 — Add API base URL config**
- **Description:** Add the API endpoint to settings.
- **Files/folders:** `POS.Desktop/appsettings.json`.
- **Expected output:** Configurable base URL.
- **Verification:** Read at runtime.
- **Risk/notes:** Per-environment values.

**Task 6.2.2 — Define the sync client interface**
- **Description:** Typed client contract for ingest.
- **Files/folders:** `POS.Desktop/Services/Sync/` (new).
- **Expected output:** Interface.
- **Verification:** Compiles.
- **Risk/notes:** Keep it small.

**Task 6.2.3 — Implement the client**
- **Description:** Call the ingest endpoint.
- **Files/folders:** `POS.Desktop/Services/Sync/*`.
- **Expected output:** Working client.
- **Verification:** Posts a batch.
- **Risk/notes:** Use shared DTOs.

**Task 6.2.4 — Acquire a device token**
- **Description:** Obtain the `PosDevice` JWT.
- **Files/folders:** `POS.Desktop/Services/Sync/*`.
- **Expected output:** Auth token.
- **Verification:** Token acquired.
- **Risk/notes:** Secure token storage.

**Task 6.2.5 — Implement token refresh**
- **Description:** Refresh on expiry transparently.
- **Files/folders:** `POS.Desktop/Services/Sync/*`.
- **Expected output:** Seamless refresh.
- **Verification:** Expired token auto-refreshes.
- **Risk/notes:** Handle clock skew.

**Task 6.2.6 — Register the typed HttpClient**
- **Description:** Add via `IHttpClientFactory` in the host.
- **Files/folders:** `POS.Desktop/App.xaml.cs`.
- **Expected output:** DI-registered client.
- **Verification:** Resolves.
- **Risk/notes:** Set timeouts.

**Task 6.2.7 — Map failures to typed results**
- **Description:** Convert HTTP/network errors to results, not exceptions.
- **Files/folders:** `POS.Desktop/Services/Sync/*`.
- **Expected output:** Result-based errors.
- **Verification:** No exceptions surface to UI.
- **Risk/notes:** Offline must be graceful.

**Task 6.2.8 — Handle timeouts/skew**
- **Description:** Bounded timeouts; tolerate clock differences.
- **Files/folders:** `POS.Desktop/Services/Sync/*`.
- **Expected output:** Robust calls.
- **Verification:** Timeout path tested.
- **Risk/notes:** Avoid hangs.

**Task 6.2.9 — Smoke test ingest call**
- **Description:** Post a sample batch to the API.
- **Files/folders:** runtime / `POS.Tests/*`.
- **Expected output:** Successful call.
- **Verification:** API acks.
- **Risk/notes:** Requires API running.

**Task 6.2.10 — Ensure no UI-thread blocking**
- **Description:** All client calls async/off-UI.
- **Files/folders:** `POS.Desktop/Services/Sync/*`.
- **Expected output:** Responsive UI.
- **Verification:** UI stays responsive during sync.
- **Risk/notes:** Offline-first principle.

### Milestone 6.3 — Outbox drain processor

**Task 6.3.1 — Define the SyncProcessor**
- **Description:** Background service to drain the outbox.
- **Files/folders:** `POS.Desktop/Services/Sync/*`.
- **Expected output:** Processor class.
- **Verification:** Compiles.
- **Risk/notes:** `BackgroundService` pattern.

**Task 6.3.2 — Register as a hosted service**
- **Description:** Add to the host's hosted services.
- **Files/folders:** `POS.Desktop/App.xaml.cs`.
- **Expected output:** Runs on startup.
- **Verification:** Starts with host.
- **Risk/notes:** Stops on shutdown (6.3.9).

**Task 6.3.3 — Batch unsent outbox rows**
- **Description:** Query pending `SyncOutbox` rows in batches.
- **Files/folders:** `POS.Desktop/Services/Sync/*`, `Data/LocalEntities/SyncOutbox.cs`.
- **Expected output:** Batches assembled.
- **Verification:** Correct rows selected.
- **Risk/notes:** Order by sequence.

**Task 6.3.4 — Post the batch**
- **Description:** Send via the sync client.
- **Files/folders:** `POS.Desktop/Services/Sync/*`.
- **Expected output:** Batch transmitted.
- **Verification:** API receives it.
- **Risk/notes:** Handle partial success.

**Task 6.3.5 — Mark rows sent on success**
- **Description:** Update status for acked rows.
- **Files/folders:** `POS.Desktop/Services/Sync/*`.
- **Expected output:** Sent markers.
- **Verification:** Rows flagged sent.
- **Risk/notes:** Only on confirmed ack.

**Task 6.3.6 — Advance the cursor**
- **Description:** Update `SyncCursor` monotonically.
- **Files/folders:** `Data/LocalEntities/SyncCursor.cs`, `Services/Sync/*`.
- **Expected output:** Progress tracked.
- **Verification:** Cursor advances.
- **Risk/notes:** Never regress.

**Task 6.3.7 — Run off the UI thread**
- **Description:** Ensure background execution.
- **Files/folders:** `POS.Desktop/Services/Sync/*`.
- **Expected output:** Non-blocking sync.
- **Verification:** UI unaffected.
- **Risk/notes:** Core requirement.

**Task 6.3.8 — Tune batch size/interval**
- **Description:** Configure batch size and poll interval.
- **Files/folders:** `appsettings.json`, `Services/Sync/*`.
- **Expected output:** Tunable processor.
- **Verification:** Values applied.
- **Risk/notes:** Balance latency vs load.

**Task 6.3.9 — Pause cleanly on shutdown**
- **Description:** Honor cancellation on host stop.
- **Files/folders:** `POS.Desktop/Services/Sync/*`.
- **Expected output:** Graceful stop.
- **Verification:** No mid-batch corruption.
- **Risk/notes:** Cooperative cancellation.

**Task 6.3.10 — Test events reach central**
- **Description:** End-to-end: sale → central record.
- **Files/folders:** runtime, `POS.Tests/*`.
- **Expected output:** Data replicates.
- **Verification:** Central shows the event.
- **Risk/notes:** Requires API + DB.

### Milestone 6.4 — Retry, recovery & reconciliation

**Task 6.4.1 — Define a retry policy**
- **Description:** Backoff schedule for transient failures.
- **Files/folders:** `POS.Desktop/Services/Sync/*`.
- **Expected output:** Retry policy.
- **Verification:** Backoff applied.
- **Risk/notes:** Avoid hot loops.

**Task 6.4.2 — Persist retry state**
- **Description:** Track attempts via `LocalRecoveryJournal`.
- **Files/folders:** `Data/LocalEntities/LocalRecoveryJournal.cs`, `Services/Sync/*`.
- **Expected output:** Durable retry state.
- **Verification:** Attempts recorded.
- **Risk/notes:** Survive restarts.

**Task 6.4.3 — Bound retries / quarantine**
- **Description:** Cap retries; quarantine poison events.
- **Files/folders:** `POS.Desktop/Services/Sync/*`.
- **Expected output:** Poison handling.
- **Verification:** Poison events parked.
- **Risk/notes:** Don't block the queue.

**Task 6.4.4 — Wire reconciliation queue**
- **Description:** Use `PaymentReconciliationQueue` for payment sync.
- **Files/folders:** `Data/LocalEntities/PaymentReconciliationQueue.cs`, `Services/Sync/*`.
- **Expected output:** Reconciliation flow.
- **Verification:** Queue processed.
- **Risk/notes:** Payment accuracy.

**Task 6.4.5 — Reconcile payment acks**
- **Description:** Match central acks to local payments.
- **Files/folders:** `POS.Desktop/Services/Sync/*`.
- **Expected output:** Closed reconciliation.
- **Verification:** Payments reconciled.
- **Risk/notes:** Handle mismatches.

**Task 6.4.6 — Prevent failure hot-loops**
- **Description:** Ensure repeated failures back off.
- **Files/folders:** `POS.Desktop/Services/Sync/*`.
- **Expected output:** Stable under failure.
- **Verification:** No CPU spin.
- **Risk/notes:** Observability needed.

**Task 6.4.7 — Surface quarantined items**
- **Description:** Make poison events inspectable.
- **Files/folders:** `Services/Sync/*`, logs/bridge.
- **Expected output:** Visibility.
- **Verification:** Quarantine listed.
- **Risk/notes:** Manual remediation path.

**Task 6.4.8 — Test transient → eventual success**
- **Description:** Simulate transient failure then recovery.
- **Files/folders:** `POS.Tests/*`.
- **Expected output:** Eventual success.
- **Verification:** Event syncs after retries.
- **Risk/notes:** Deterministic test harness.

**Task 6.4.9 — Test poison handling**
- **Description:** Simulate a permanently failing event.
- **Files/folders:** `POS.Tests/*`.
- **Expected output:** Quarantined, not retried forever.
- **Verification:** Bounded retries.
- **Risk/notes:** Queue keeps flowing.

**Task 6.4.10 — Test reconciliation closes loop**
- **Description:** Verify payment reconciliation completes.
- **Files/folders:** `POS.Tests/*`.
- **Expected output:** Reconciled state.
- **Verification:** No dangling payments.
- **Risk/notes:** Financial integrity.

### Milestone 6.5 — Connectivity handling & sync observability

**Task 6.5.1 — Add connectivity detection**
- **Description:** Cheap online/offline check.
- **Files/folders:** `POS.Desktop/Services/Sync/*`.
- **Expected output:** Connectivity signal.
- **Verification:** Reflects network state.
- **Risk/notes:** Keep checks cheap.

**Task 6.5.2 — Pause processor when offline**
- **Description:** Suspend attempts while offline.
- **Files/folders:** `POS.Desktop/Services/Sync/*`.
- **Expected output:** No futile calls.
- **Verification:** Processor idles offline.
- **Risk/notes:** Resume promptly online.

**Task 6.5.3 — Resume when online**
- **Description:** Restart draining on reconnect.
- **Files/folders:** `POS.Desktop/Services/Sync/*`.
- **Expected output:** Auto-resume.
- **Verification:** Sync resumes.
- **Risk/notes:** Debounce flapping.

**Task 6.5.4 — Track pending/last-synced counts**
- **Description:** Maintain sync metrics.
- **Files/folders:** `POS.Desktop/Services/Sync/*`.
- **Expected output:** Status counters.
- **Verification:** Counts accurate.
- **Risk/notes:** Cheap to compute.

**Task 6.5.5 — Expose sync status via bridge**
- **Description:** Optional status query for the UI.
- **Files/folders:** `POS.Desktop/Shell/`, `Bridge/*`.
- **Expected output:** Status over bridge.
- **Verification:** UI can read status.
- **Risk/notes:** Don't redesign UI — data only.

**Task 6.5.6 — Add sync logging/metrics**
- **Description:** Log sync activity and outcomes.
- **Files/folders:** `Services/Sync/*`, logging.
- **Expected output:** Observable sync.
- **Verification:** Logs present.
- **Risk/notes:** No sensitive payloads.

**Task 6.5.7 — Ensure offline ops unaffected**
- **Description:** Confirm full POS flow works offline.
- **Files/folders:** runtime.
- **Expected output:** Offline-capable terminal.
- **Verification:** Sell/pay/close offline.
- **Risk/notes:** Core guarantee.

**Task 6.5.8 — Keep checks off UI thread**
- **Description:** Connectivity checks must not block UI.
- **Files/folders:** `Services/Sync/*`.
- **Expected output:** Responsive UI.
- **Verification:** No UI stalls.
- **Risk/notes:** Watch synchronous DNS calls.

**Task 6.5.9 — Test offline→online transition**
- **Description:** Verify pause/resume behavior.
- **Files/folders:** `POS.Tests/*` / runtime.
- **Expected output:** Smooth transition.
- **Verification:** Backlog drains on reconnect.
- **Risk/notes:** Simulate network toggling.

**Task 6.5.10 — Verify no blocking on checks**
- **Description:** Confirm connectivity checks are non-blocking.
- **Files/folders:** `Services/Sync/*`.
- **Expected output:** Non-blocking design.
- **Verification:** Profiling confirms.
- **Risk/notes:** Final Phase 6 gate.

---

## Phase 7 — Hardware integration

### Milestone 7.1 — Hardware abstraction contracts

**Task 7.1.1 — Define IReceiptPrinter**
- **Description:** Printer operations (print receipt, status).
- **Files/folders:** `POS.Desktop.Hardware/Printers/*`.
- **Expected output:** Interface.
- **Verification:** Compiles.
- **Risk/notes:** Keep printer-agnostic.

**Task 7.1.2 — Define ICashDrawer**
- **Description:** Drawer kick + status.
- **Files/folders:** `POS.Desktop.Hardware/CashDrawer/*`.
- **Expected output:** Interface.
- **Verification:** Compiles.
- **Risk/notes:** Often driven via printer.

**Task 7.1.3 — Define IBarcodeScanner**
- **Description:** Scan event/read contract.
- **Files/folders:** `POS.Desktop.Hardware/Scanner/*`.
- **Expected output:** Interface.
- **Verification:** Compiles.
- **Risk/notes:** HID/wedge vs SDK.

**Task 7.1.4 — Define IPaymentTerminal**
- **Description:** Card/wallet auth contract.
- **Files/folders:** `POS.Desktop.Hardware/PaymentTerminal/*`.
- **Expected output:** Interface.
- **Verification:** Compiles.
- **Risk/notes:** Vendor-specific later.

**Task 7.1.5 — Define ICustomerDisplay**
- **Description:** Display cart/total contract.
- **Files/folders:** `POS.Desktop.Hardware/CustomerDisplay/*`.
- **Expected output:** Interface.
- **Verification:** Compiles.
- **Risk/notes:** Second-screen variance.

**Task 7.1.6 — Define shared result/enum types**
- **Description:** Common device result/status types.
- **Files/folders:** `POS.Shared/*` or `POS.Desktop.Hardware/*`.
- **Expected output:** Shared types.
- **Verification:** Reused across interfaces.
- **Risk/notes:** Avoid leaking vendor types.

**Task 7.1.7 — Keep contracts free of concrete deps**
- **Description:** No SDK/driver references in interfaces.
- **Files/folders:** `POS.Desktop.Hardware/*`.
- **Expected output:** Clean contracts.
- **Verification:** No vendor packages referenced.
- **Risk/notes:** Swappable implementations.

**Task 7.1.8 — Make operations awaitable**
- **Description:** Async/typed operation signatures.
- **Files/folders:** `POS.Desktop.Hardware/*`.
- **Expected output:** Async contracts.
- **Verification:** Signatures consistent.
- **Risk/notes:** Device I/O is async.

**Task 7.1.9 — Build the hardware project clean**
- **Description:** Ensure `POS.Desktop.Hardware` compiles with interfaces.
- **Files/folders:** `POS.Desktop.Hardware/*`.
- **Expected output:** Clean build.
- **Verification:** Builds.
- **Risk/notes:** Project was empty stubs.

**Task 7.1.10 — Document each interface**
- **Description:** Brief responsibility note per interface.
- **Files/folders:** repo notes.
- **Expected output:** Interface docs.
- **Verification:** Docs exist.
- **Risk/notes:** Cover only the 5 known devices.

### Milestone 7.2 — Stub implementations, config selection & DI

**Task 7.2.1 — Create no-op/console stubs**
- **Description:** Stub implementation per interface.
- **Files/folders:** `POS.Desktop.Hardware/*`.
- **Expected output:** Stubs.
- **Verification:** Compile + log calls.
- **Risk/notes:** Default for dev/no-device.

**Task 7.2.2 — Add device-selection config**
- **Description:** Config keys to pick implementations.
- **Files/folders:** `POS.Desktop/appsettings.json`.
- **Expected output:** Selection config.
- **Verification:** Read at startup.
- **Risk/notes:** Per-terminal config.

**Task 7.2.3 — Implement config-driven registration**
- **Description:** Register the chosen impl per device.
- **Files/folders:** `POS.Desktop/App.xaml.cs`, `Configuration/*`.
- **Expected output:** Config-driven DI.
- **Verification:** Config selects impl.
- **Risk/notes:** Fail loudly on bad config.

**Task 7.2.4 — Register stubs by default**
- **Description:** Default to stubs when unconfigured.
- **Files/folders:** `POS.Desktop/App.xaml.cs`.
- **Expected output:** Safe defaults.
- **Verification:** Runs with no devices.
- **Risk/notes:** No hard device dependency.

**Task 7.2.5 — Log stub calls**
- **Description:** Stubs log invocations for debugging.
- **Files/folders:** `POS.Desktop.Hardware/*`.
- **Expected output:** Traceable stubs.
- **Verification:** Calls logged.
- **Risk/notes:** Useful in demos.

**Task 7.2.6 — Verify flow runs on stubs**
- **Description:** Full POS flow with all-stub hardware.
- **Files/folders:** runtime.
- **Expected output:** End-to-end on stubs.
- **Verification:** Flow completes.
- **Risk/notes:** Hardware never blocks flow.

**Task 7.2.7 — Verify config swap**
- **Description:** Switch a device impl via config.
- **Files/folders:** `appsettings.json`.
- **Expected output:** Hot-swap by config.
- **Verification:** Impl changes without code edits.
- **Risk/notes:** Restart may be required.

**Task 7.2.8 — Fail loudly on misconfiguration**
- **Description:** Clear error for unknown device type.
- **Files/folders:** `Configuration/*`.
- **Expected output:** Loud failure.
- **Verification:** Bad config → clear error.
- **Risk/notes:** Avoid silent stub fallback in prod.

**Task 7.2.9 — Document config keys**
- **Description:** Write the device-config reference.
- **Files/folders:** repo notes.
- **Expected output:** Config docs.
- **Verification:** Docs exist.
- **Risk/notes:** Deployment aid.

**Task 7.2.10 — Test default stub flow**
- **Description:** Automated/manual check of stub defaults.
- **Files/folders:** runtime / `POS.Tests/*`.
- **Expected output:** Passing default flow.
- **Verification:** Works out-of-box.
- **Risk/notes:** Baseline for real drivers.

### Milestone 7.3 — Receipt printing (drain PrintQueue)

**Task 7.3.1 — Define the print-queue consumer**
- **Description:** Service that drains `PrintQueue`.
- **Files/folders:** `POS.Desktop/Services/*`.
- **Expected output:** Consumer service.
- **Verification:** Compiles.
- **Risk/notes:** Background service.

**Task 7.3.2 — Register as hosted service**
- **Description:** Add to host.
- **Files/folders:** `POS.Desktop/App.xaml.cs`.
- **Expected output:** Runs on startup.
- **Verification:** Starts.
- **Risk/notes:** Stop cleanly on shutdown.

**Task 7.3.3 — Drain PrintQueue rows**
- **Description:** Pull pending print jobs.
- **Files/folders:** `Data/LocalEntities/PrintQueue.cs`, consumer.
- **Expected output:** Jobs dequeued.
- **Verification:** Pending jobs picked up.
- **Risk/notes:** Order/ack handling.

**Task 7.3.4 — Render via IReceiptPrinter**
- **Description:** Send rendered content to the printer.
- **Files/folders:** `POS.Desktop.Hardware/Printers/*`, consumer.
- **Expected output:** Printed/stub output.
- **Verification:** Content emitted.
- **Risk/notes:** Use 5.4.7 receipt content.

**Task 7.3.5 — Implement first ESC/POS driver**
- **Description:** Real driver for one printer model.
- **Files/folders:** `POS.Desktop.Hardware/Printers/*`.
- **Expected output:** Working driver.
- **Verification:** Prints on hardware.
- **Risk/notes:** ESC/POS dialect variance.

**Task 7.3.6 — Mark jobs done/failed**
- **Description:** Update job status post-print.
- **Files/folders:** consumer, `PrintQueue`.
- **Expected output:** Status tracking.
- **Verification:** Statuses set.
- **Risk/notes:** Distinguish transient vs permanent.

**Task 7.3.7 — Retry failed prints**
- **Description:** Re-attempt failed jobs with bounds.
- **Files/folders:** consumer.
- **Expected output:** Retry behavior.
- **Verification:** Failed job retries.
- **Risk/notes:** Avoid infinite reprints.

**Task 7.3.8 — Match content to template**
- **Description:** Ensure output matches `ReceiptTemplate`.
- **Files/folders:** `Services/*`, `POS.Shared` (`ReceiptTemplate`).
- **Expected output:** Template-accurate receipts.
- **Verification:** Matches prototype format.
- **Risk/notes:** Layout fidelity.

**Task 7.3.9 — Handle printer offline**
- **Description:** Queue/hold when printer absent.
- **Files/folders:** consumer, `Printers/*`.
- **Expected output:** Graceful degradation.
- **Verification:** No data loss when offline.
- **Risk/notes:** Re-print when back.

**Task 7.3.10 — Test sale → receipt**
- **Description:** Verify completion produces a receipt.
- **Files/folders:** runtime.
- **Expected output:** Printed/stub receipt.
- **Verification:** Receipt emitted post-sale.
- **Risk/notes:** Depends on 5.4.6.

### Milestone 7.4 — Barcode scanner → checkout

**Task 7.4.1 — Define scanner input acquisition**
- **Description:** Decide HID/keyboard-wedge vs SDK capture.
- **Files/folders:** `POS.Desktop.Hardware/Scanner/*`.
- **Expected output:** Capture approach.
- **Verification:** Documented.
- **Risk/notes:** Wedge emits keystrokes.

**Task 7.4.2 — Implement IBarcodeScanner**
- **Description:** Concrete scanner implementation.
- **Files/folders:** `POS.Desktop.Hardware/Scanner/*`.
- **Expected output:** Working scanner.
- **Verification:** Emits scan events.
- **Risk/notes:** Debounce/segment input.

**Task 7.4.3 — Resolve code via ItemIdentifier**
- **Description:** Look up scanned code in catalog.
- **Files/folders:** `Services/Catalog/*`, `POS.Shared` (`ItemIdentifier`).
- **Expected output:** Item resolution.
- **Verification:** Known code resolves.
- **Risk/notes:** Reuse 4.4 lookups.

**Task 7.4.4 — Add scanned item to cart**
- **Description:** Push resolved item to `IOrderService`.
- **Files/folders:** `Services/Orders/*`, scanner wiring.
- **Expected output:** Scan adds to cart.
- **Verification:** Cart updates.
- **Risk/notes:** Increment if already present.

**Task 7.4.5 — Manage input focus**
- **Description:** Prevent double-entry/focus theft.
- **Files/folders:** `main_checkout.html` (`<script>` if needed), `Shell/`.
- **Expected output:** Clean input handling.
- **Verification:** No duplicate adds.
- **Risk/notes:** Wedge vs numpad conflict.

**Task 7.4.6 — Handle unknown codes**
- **Description:** Show existing not-found UX.
- **Files/folders:** `main_checkout.html` (`<script>`).
- **Expected output:** Graceful unknown handling.
- **Verification:** Unknown code → existing message.
- **Risk/notes:** No redesign.

**Task 7.4.7 — Avoid stealing numpad input**
- **Description:** Ensure scanner doesn't capture manual keys.
- **Files/folders:** `Shell/`, `main_checkout.html`.
- **Expected output:** Separated input paths.
- **Verification:** Manual entry unaffected.
- **Risk/notes:** Timing-based segmentation.

**Task 7.4.8 — Wire into checkout (script-only)**
- **Description:** Connect scan events to the bridge if needed.
- **Files/folders:** `main_checkout.html` (`<script>`).
- **Expected output:** Scan→cart pipeline.
- **Verification:** End-to-end scan add.
- **Risk/notes:** Keep markup intact.

**Task 7.4.9 — Test wedge without app changes**
- **Description:** Verify keyboard-wedge scanners work.
- **Files/folders:** runtime.
- **Expected output:** Plug-and-play wedge.
- **Verification:** Scans add items.
- **Risk/notes:** Common deployment case.

**Task 7.4.10 — Test scan → add-to-cart**
- **Description:** Full scan flow validation.
- **Files/folders:** runtime.
- **Expected output:** Working scan checkout.
- **Verification:** Items added by scan.
- **Risk/notes:** Unknown-code path too.

### Milestone 7.5 — Cash drawer & customer display

**Task 7.5.1 — Implement ICashDrawer kick**
- **Description:** Concrete drawer-open implementation.
- **Files/folders:** `POS.Desktop.Hardware/CashDrawer/*`.
- **Expected output:** Working kick.
- **Verification:** Drawer opens (or stub logs).
- **Risk/notes:** Often via printer pulse.

**Task 7.5.2 — Trigger drawer on cash payment**
- **Description:** Kick drawer when cash tendered.
- **Files/folders:** `Services/Payments/*`, `CashDrawer/*`.
- **Expected output:** Auto-open on cash.
- **Verification:** Opens at right moment.
- **Risk/notes:** Only for cash/explicit.

**Task 7.5.3 — Add explicit open-drawer action**
- **Description:** Manual no-sale open (authorized).
- **Files/folders:** `Services/CashControl/*`, `CashDrawer/*`.
- **Expected output:** Manual open path.
- **Verification:** Opens on action.
- **Risk/notes:** Require authorization.

**Task 7.5.4 — Log drawer-open events**
- **Description:** Record opens for accountability.
- **Files/folders:** `Services/*`, logging.
- **Expected output:** Audit trail.
- **Verification:** Events logged.
- **Risk/notes:** Cash accountability.

**Task 7.5.5 — Implement ICustomerDisplay**
- **Description:** Concrete display implementation.
- **Files/folders:** `POS.Desktop.Hardware/CustomerDisplay/*`.
- **Expected output:** Working display.
- **Verification:** Shows content (or stub logs).
- **Risk/notes:** Device/protocol variance.

**Task 7.5.6 — Mirror cart/total**
- **Description:** Push live cart/total to display.
- **Files/folders:** `Services/Orders/*`, `CustomerDisplay/*`.
- **Expected output:** Live mirror.
- **Verification:** Display tracks cart.
- **Risk/notes:** Update throttling.

**Task 7.5.7 — Handle missing devices**
- **Description:** Degrade to stubs cleanly.
- **Files/folders:** `Hardware/*`.
- **Expected output:** No-device tolerance.
- **Verification:** Flow unaffected.
- **Risk/notes:** Common in dev.

**Task 7.5.8 — Handle display resolution variance**
- **Description:** Support second-display differences.
- **Files/folders:** `CustomerDisplay/*`.
- **Expected output:** Robust display.
- **Verification:** Works across configs.
- **Risk/notes:** Test on real hardware.

**Task 7.5.9 — Test drawer timing**
- **Description:** Verify kick at correct moments.
- **Files/folders:** runtime.
- **Expected output:** Correct timing.
- **Verification:** Opens on cash/action only.
- **Risk/notes:** No spurious opens.

**Task 7.5.10 — Test customer display mirroring**
- **Description:** Verify display reflects cart/total.
- **Files/folders:** runtime.
- **Expected output:** Accurate mirror.
- **Verification:** Matches cart.
- **Risk/notes:** Latency acceptable.

### Milestone 7.6 — Payment terminal (pinpad) integration

**Task 7.6.1 — Implement IPaymentTerminal**
- **Description:** Card/wallet authorization implementation.
- **Files/folders:** `POS.Desktop.Hardware/PaymentTerminal/*`.
- **Expected output:** Working terminal driver.
- **Verification:** Auth round-trip (or stub).
- **Risk/notes:** Vendor SDK + certification.

**Task 7.6.2 — Integrate vendor SDK behind Gateway**
- **Description:** Wrap the vendor SDK in `Gateway/`.
- **Files/folders:** `POS.Desktop.Hardware/Gateway/*`.
- **Expected output:** Encapsulated SDK.
- **Verification:** Gateway calls succeed.
- **Risk/notes:** Keep `IPaymentTerminal` stable.

**Task 7.6.3 — Replace stubbed card/wallet path**
- **Description:** Swap 5.4 stub for real terminal calls.
- **Files/folders:** `Services/Payments/*`.
- **Expected output:** Real card/wallet.
- **Verification:** Uses terminal.
- **Risk/notes:** No UI change required.

**Task 7.6.4 — Map approvals/declines to Payment**
- **Description:** Persist auth results on `Payment`.
- **Files/folders:** `Services/Payments/*`, `POS.Shared` (`Payment`).
- **Expected output:** Auth data recorded.
- **Verification:** Approve/decline reflected.
- **Risk/notes:** Store auth codes.

**Task 7.6.5 — Reflect status in payment_screen**
- **Description:** Show terminal status (script-only).
- **Files/folders:** `payment_screen.html` (`<script>`).
- **Expected output:** Live status UI.
- **Verification:** Status updates render.
- **Risk/notes:** Reuse existing status UI.

**Task 7.6.6 — Capture auth data for reconciliation**
- **Description:** Record fields needed for reconciliation.
- **Files/folders:** `Services/Payments/*`, `Services/Sync/*`.
- **Expected output:** Reconcilable data.
- **Verification:** Fields present.
- **Risk/notes:** Ties to 6.4.

**Task 7.6.7 — Keep the interface stable**
- **Description:** Ensure gateway swaps don't touch flows.
- **Files/folders:** `Hardware/PaymentTerminal/*`.
- **Expected output:** Stable contract.
- **Verification:** Flow unchanged on swap.
- **Risk/notes:** Future gateway changes.

**Task 7.6.8 — Handle terminal offline/timeout**
- **Description:** Graceful handling of unavailable terminal.
- **Files/folders:** `Services/Payments/*`, `PaymentTerminal/*`.
- **Expected output:** Robust errors.
- **Verification:** Timeout handled.
- **Risk/notes:** No stuck payments.

**Task 7.6.9 — Test approval flow**
- **Description:** Verify a successful authorization.
- **Files/folders:** runtime.
- **Expected output:** Approved payment.
- **Verification:** Completes sale.
- **Risk/notes:** Sandbox/test card.

**Task 7.6.10 — Test decline flow**
- **Description:** Verify a declined authorization.
- **Files/folders:** runtime.
- **Expected output:** Decline handled.
- **Verification:** Sale not completed.
- **Risk/notes:** Clear operator feedback.

---

## Phase 8 — Production hardening

### Milestone 8.1 — WebView2 runtime bootstrap & install handling

**Task 8.1.1 — Detect runtime at startup**
- **Description:** Extend the 1.5 guard with a definitive presence check.
- **Files/folders:** `POS.Desktop/Shell/*`.
- **Expected output:** Reliable detection.
- **Verification:** Detects present/absent.
- **Risk/notes:** Reuse 1.5 logic.

**Task 8.1.2 — Define the remediation path**
- **Description:** Decide install/repair flow when missing.
- **Files/folders:** installer/bootstrap config.
- **Expected output:** Remediation plan.
- **Verification:** Documented path.
- **Risk/notes:** Offline scenarios.

**Task 8.1.3 — Bundle/pre-stage the installer**
- **Description:** Include the Evergreen bootstrapper/offline installer.
- **Files/folders:** installer assets.
- **Expected output:** Available installer.
- **Verification:** Present in package.
- **Risk/notes:** Licensing/distribution terms.

**Task 8.1.4 — Add a bootstrap script**
- **Description:** Script to install runtime if absent.
- **Files/folders:** installer/bootstrap scripts.
- **Expected output:** Bootstrap automation.
- **Verification:** Installs on clean machine.
- **Risk/notes:** Admin rights handling.

**Task 8.1.5 — Integrate the check with startup**
- **Description:** Gate launch on runtime presence.
- **Files/folders:** `POS.Desktop/App.xaml.cs`, `Shell/*`.
- **Expected output:** Guarded startup.
- **Verification:** Missing runtime → remediation, not crash.
- **Risk/notes:** Clear UX.

**Task 8.1.6 — Handle offline install**
- **Description:** Support runtime install without internet.
- **Files/folders:** installer assets.
- **Expected output:** Offline-capable install.
- **Verification:** Installs offline.
- **Risk/notes:** Use offline installer variant.

**Task 8.1.7 — Test clean machine → working runtime**
- **Description:** Validate the full bootstrap path.
- **Files/folders:** test environment.
- **Expected output:** Working runtime post-bootstrap.
- **Verification:** App boots after install.
- **Risk/notes:** VM/clean profile.

**Task 8.1.8 — Ensure no silent failure**
- **Description:** Always surface runtime issues.
- **Files/folders:** `Shell/*`.
- **Expected output:** Visible failures.
- **Verification:** No silent exits.
- **Risk/notes:** Field supportability.

**Task 8.1.9 — Document the prerequisite**
- **Description:** Deployment doc for the runtime.
- **Files/folders:** repo notes / deploy docs.
- **Expected output:** Prerequisite doc.
- **Verification:** Doc exists.
- **Risk/notes:** Links to 1.1.5 decision.

**Task 8.1.10 — Verify post-install boot**
- **Description:** Confirm normal operation after install.
- **Files/folders:** runtime.
- **Expected output:** Stable boot.
- **Verification:** Full flow runs.
- **Risk/notes:** Regression check.

### Milestone 8.2 — Kiosk lockdown & security hardening

**Task 8.2.1 — Disable dev tools in production**
- **Description:** Turn off F12/dev tools.
- **Files/folders:** `POS.Desktop/Shell/WebViewHost.cs`.
- **Expected output:** No dev tools.
- **Verification:** Dev tools unavailable.
- **Risk/notes:** Keep a debug-only toggle.

**Task 8.2.2 — Disable context menu**
- **Description:** Suppress the default right-click menu.
- **Files/folders:** `Shell/WebViewHost.cs`.
- **Expected output:** No context menu.
- **Verification:** Right-click inert.
- **Risk/notes:** Touch terminals.

**Task 8.2.3 — Disable zoom/pinch**
- **Description:** Lock zoom level.
- **Files/folders:** `Shell/WebViewHost.cs`.
- **Expected output:** Fixed zoom.
- **Verification:** Pinch/zoom disabled.
- **Risk/notes:** Preserve layout fidelity.

**Task 8.2.4 — Restrict navigation to pos.app**
- **Description:** Block navigation outside the virtual host.
- **Files/folders:** `Shell/WebViewHost.cs`.
- **Expected output:** Locked origin.
- **Verification:** External nav blocked.
- **Risk/notes:** Allowlist only local.

**Task 8.2.5 — Block remote script/navigation**
- **Description:** Prevent loading remote content (except allowed fonts pre-8.4).
- **Files/folders:** `Shell/WebViewHost.cs`.
- **Expected output:** No untrusted remote loads.
- **Verification:** Remote requests blocked.
- **Risk/notes:** After 8.4, fully offline.

**Task 8.2.6 — Prevent casual minimize/close**
- **Description:** Lock window controls.
- **Files/folders:** `MainWindow.xaml(.cs)`.
- **Expected output:** Kiosk window behavior.
- **Verification:** Can't casually exit.
- **Risk/notes:** Provide support gesture (next task).

**Task 8.2.7 — Add a guarded support/exit gesture**
- **Description:** Hidden, authorized exit/support path.
- **Files/folders:** `Shell/*`, `MainWindow.xaml.cs`.
- **Expected output:** Controlled exit.
- **Verification:** Requires authorization.
- **Risk/notes:** Balance lockdown vs support.

**Task 8.2.8 — Verify no external navigation**
- **Description:** Attempt external nav; confirm blocked.
- **Files/folders:** runtime.
- **Expected output:** Hardened nav.
- **Verification:** All external attempts fail.
- **Risk/notes:** Security check.

**Task 8.2.9 — Verify only trusted content loads**
- **Description:** Confirm only `pos.app` content runs.
- **Files/folders:** runtime.
- **Expected output:** Trusted-only loading.
- **Verification:** No untrusted origins.
- **Risk/notes:** Inspect network log.

**Task 8.2.10 — Security review pass**
- **Description:** Review lockdown + bridge surface.
- **Files/folders:** `Shell/*`, `Bridge/*`.
- **Expected output:** Sign-off.
- **Verification:** Review complete.
- **Risk/notes:** Consider /security-review.

### Milestone 8.3 — Error handling, logging & telemetry

**Task 8.3.1 — Add a global exception handler**
- **Description:** Catch unhandled UI/dispatcher exceptions.
- **Files/folders:** `POS.Desktop/App.xaml.cs`.
- **Expected output:** No uncaught crashes.
- **Verification:** Unhandled → logged + graceful.
- **Risk/notes:** Extends 1.5.9.

**Task 8.3.2 — Structured logging across layers**
- **Description:** Consistent logging in shell/bridge/services.
- **Files/folders:** `Shell/*`, `Services/*`, `Bridge/*`.
- **Expected output:** Structured logs.
- **Verification:** Logs are queryable.
- **Risk/notes:** Use a single logging abstraction.

**Task 8.3.3 — Correlation IDs on bridge errors**
- **Description:** Propagate request IDs into logs/errors.
- **Files/folders:** `Bridge/*`, `Shell/*`.
- **Expected output:** Traceable errors.
- **Verification:** IDs link JS↔C#.
- **Risk/notes:** Reuse 3.2.6.

**Task 8.3.4 — Graceful error UI**
- **Description:** User-friendly messages for unhandled cases.
- **Files/folders:** `Shell/*`, screens (script-only).
- **Expected output:** Non-technical errors.
- **Verification:** Errors shown gracefully.
- **Risk/notes:** No stack traces to operators.

**Task 8.3.5 — Log rotation policy**
- **Description:** Cap/rotate log files.
- **Files/folders:** logging config, `appsettings.json`.
- **Expected output:** Bounded log growth.
- **Verification:** Rotation works.
- **Risk/notes:** Disk usage on terminals.

**Task 8.3.6 — Scrub secrets from logs**
- **Description:** Redact PINs/card data/tokens.
- **Files/folders:** logging, `Services/*`.
- **Expected output:** Clean logs.
- **Verification:** No secrets present.
- **Risk/notes:** Compliance.

**Task 8.3.7 — Add basic telemetry counters**
- **Description:** Track key events (sales, errors, syncs).
- **Files/folders:** `Services/*`.
- **Expected output:** Minimal telemetry.
- **Verification:** Counters update.
- **Risk/notes:** Don't over-build.

**Task 8.3.8 — Configurable log levels**
- **Description:** Set levels via config.
- **Files/folders:** `appsettings.json`.
- **Expected output:** Tunable verbosity.
- **Verification:** Levels apply.
- **Risk/notes:** Default to info/warn.

**Task 8.3.9 — Test exception → logged + graceful**
- **Description:** Force an exception; verify handling.
- **Files/folders:** runtime / `POS.Tests/*`.
- **Expected output:** Logged + graceful UI.
- **Verification:** No crash dialog.
- **Risk/notes:** Cover bridge + shell.

**Task 8.3.10 — Verify no sensitive data in logs**
- **Description:** Audit logs for leaks.
- **Files/folders:** logs.
- **Expected output:** Clean audit.
- **Verification:** No PIN/card/token strings.
- **Risk/notes:** Sign-off item.

### Milestone 8.4 — Offline asset bundling (fonts/icons)

**Task 8.4.1 — Confirm font licensing**
- **Description:** Verify redistribution rights for each font.
- **Files/folders:** licensing notes.
- **Expected output:** Licensing decision.
- **Verification:** Rights confirmed.
- **Risk/notes:** Blocker if not allowed.

**Task 8.4.2 — Acquire font files**
- **Description:** Get Space Grotesk / Inter Tight / IBM Plex Mono.
- **Files/folders:** `POS.Desktop/Assets/ui/fonts/`.
- **Expected output:** Font files present.
- **Verification:** Files added.
- **Risk/notes:** Correct weights/styles.

**Task 8.4.3 — Acquire Material Symbols asset**
- **Description:** Get the icon font locally.
- **Files/folders:** `POS.Desktop/Assets/ui/fonts/`.
- **Expected output:** Icon font present.
- **Verification:** File added.
- **Risk/notes:** Match glyph set used.

**Task 8.4.4 — Place under Assets/ui/fonts**
- **Description:** Organize bundled assets.
- **Files/folders:** `POS.Desktop/Assets/ui/fonts/`.
- **Expected output:** Structured fonts folder.
- **Verification:** Files served via host.
- **Risk/notes:** Update Content globs.

**Task 8.4.5 — Add @font-face definitions**
- **Description:** Define local `@font-face` (font source only).
- **Files/folders:** `Assets/ui/*` (CSS font source only).
- **Expected output:** Local font faces.
- **Verification:** Fonts resolve locally.
- **Risk/notes:** Only font source changes — no appearance change.

**Task 8.4.6 — Switch links to local**
- **Description:** Replace Google `<link>`s with local references.
- **Files/folders:** `Assets/ui/*`.
- **Expected output:** No external font links.
- **Verification:** No external font requests.
- **Risk/notes:** The one sanctioned CSS touch.

**Task 8.4.7 — Verify identical render offline**
- **Description:** Disable network; confirm rendering unchanged.
- **Files/folders:** runtime.
- **Expected output:** Offline parity.
- **Verification:** Looks identical offline.
- **Risk/notes:** Compare to 2.4 baseline.

**Task 8.4.8 — Confirm no external requests**
- **Description:** Check network log for external font/icon calls.
- **Files/folders:** runtime.
- **Expected output:** Fully local assets.
- **Verification:** Zero external requests.
- **Risk/notes:** Kiosk reliability.

**Task 8.4.9 — Re-verify parity vs prototype**
- **Description:** Re-run the 2.4 parity check.
- **Files/folders:** parity notes.
- **Expected output:** Parity maintained.
- **Verification:** No visual drift.
- **Risk/notes:** Bundling must not alter appearance.

**Task 8.4.10 — Document bundling + licensing**
- **Description:** Record what was bundled and under which license.
- **Files/folders:** repo notes.
- **Expected output:** Bundling doc.
- **Verification:** Doc exists.
- **Risk/notes:** Audit trail.

### Milestone 8.5 — FBR fiscal integration point

**Task 8.5.1 — Define IFiscalService**
- **Description:** Contract for fiscal submission.
- **Files/folders:** `POS.Desktop/Services/*` (fiscal).
- **Expected output:** Interface.
- **Verification:** Compiles.
- **Risk/notes:** Keep generic (regulatory specifics later).

**Task 8.5.2 — Create a stub implementation**
- **Description:** Default no-external-call stub.
- **Files/folders:** `Services/*` (fiscal).
- **Expected output:** Stub.
- **Verification:** Records intent only.
- **Risk/notes:** Default in non-prod.

**Task 8.5.3 — Add enable-real-provider config**
- **Description:** Config switch for a real provider.
- **Files/folders:** `appsettings.json`, registration.
- **Expected output:** Config toggle.
- **Verification:** Switch selects impl.
- **Risk/notes:** Real provider TBD.

**Task 8.5.4 — Invoke at payment completion**
- **Description:** Call fiscal seam on sale completion.
- **Files/folders:** `Services/Payments/*`.
- **Expected output:** Completion hook.
- **Verification:** Stub invoked on sale.
- **Risk/notes:** No flow change for real later.

**Task 8.5.5 — Invoke at shift close/Z-report**
- **Description:** Call fiscal seam on close.
- **Files/folders:** `Services/Reporting/*`, `Services/Shifts/*`.
- **Expected output:** Close hook.
- **Verification:** Stub invoked on close.
- **Risk/notes:** Ties to 5.6.9.

**Task 8.5.6 — Record fiscal intent in stub**
- **Description:** Persist/log the intended submission.
- **Files/folders:** `Services/*` (fiscal).
- **Expected output:** Intent record.
- **Verification:** Recorded.
- **Risk/notes:** Useful for later real wiring.

**Task 8.5.7 — Keep contract generic**
- **Description:** Ensure swapping to real needs no flow edits.
- **Files/folders:** `Services/*` (fiscal).
- **Expected output:** Stable contract.
- **Verification:** Real impl drop-in.
- **Risk/notes:** Avoid vendor leakage.

**Task 8.5.8 — Add config keys + docs**
- **Description:** Document fiscal config.
- **Files/folders:** repo notes, `appsettings.json`.
- **Expected output:** Config docs.
- **Verification:** Docs exist.
- **Risk/notes:** Compliance reference.

**Task 8.5.9 — Test stub invocation**
- **Description:** Verify hooks fire at completion/close.
- **Files/folders:** `POS.Tests/*`.
- **Expected output:** Passing test.
- **Verification:** Hooks invoked.
- **Risk/notes:** No external calls.

**Task 8.5.10 — Confirm no external calls in stub mode**
- **Description:** Ensure stub stays offline.
- **Files/folders:** runtime.
- **Expected output:** Offline stub.
- **Verification:** No network on fiscal.
- **Risk/notes:** Default safety.

### Milestone 8.6 — Packaging, installer & prototype cleanup

**Task 8.6.1 — Choose the packaging method**
- **Description:** Decide MSIX/installer approach.
- **Files/folders:** packaging config.
- **Expected output:** Packaging decision.
- **Verification:** Documented.
- **Risk/notes:** Update/deploy implications.

**Task 8.6.2 — Configure the installer**
- **Description:** Set up the installer with prerequisites.
- **Files/folders:** packaging config.
- **Expected output:** Installer project/config.
- **Verification:** Builds an installer.
- **Risk/notes:** Include EF/SQLite needs.

**Task 8.6.3 — Include WebView2 runtime prerequisite**
- **Description:** Wire the runtime bootstrap into the installer.
- **Files/folders:** packaging config.
- **Expected output:** Runtime handled at install.
- **Verification:** Clean machine gets runtime.
- **Risk/notes:** Ties to 8.1.

**Task 8.6.4 — Sign the installer/app**
- **Description:** Code-sign artifacts.
- **Files/folders:** packaging config.
- **Expected output:** Signed package.
- **Verification:** Signature valid.
- **Risk/notes:** Certificate management.

**Task 8.6.5 — Confirm Assets/ui is the single source**
- **Description:** Verify the app uses only `Assets/ui/`.
- **Files/folders:** `POS.Desktop/Assets/ui/*`.
- **Expected output:** Single UI source.
- **Verification:** No `docs/` dependency at runtime.
- **Risk/notes:** Pre-cleanup gate.

**Task 8.6.6 — Remove index.html from ship**
- **Description:** Ensure the simulator isn't packaged.
- **Files/folders:** packaging config, `Assets/ui/`.
- **Expected output:** No simulator in build.
- **Verification:** `index.html` absent from package.
- **Risk/notes:** Reference copy may remain in `docs/`.

**Task 8.6.7 — Scan for stale design folders**
- **Description:** Enumerate duplicate/old design folders before deletion.
- **Files/folders:** repo-wide scan.
- **Expected output:** Cleanup candidate list.
- **Verification:** List produced.
- **Risk/notes:** Don't delete blindly.

**Task 8.6.8 — Retire docs/ui-prototype after UI/UX sign-off**
- **Description:** Remove prototype only after production UI/UX sign-off (2.4.10).
- **Files/folders:** `docs/ui-prototype/*`.
- **Expected output:** Clean repo.
- **Verification:** UI/UX sign-off recorded first.
- **Risk/notes:** Irreversible — gate strictly.

**Task 8.6.9 — Test fresh install runs full flow**
- **Description:** Install on a clean machine and run end-to-end.
- **Files/folders:** test environment.
- **Expected output:** Working install.
- **Verification:** Full flow passes.
- **Risk/notes:** Final acceptance.

**Task 8.6.10 — Document deployment + cleanup**
- **Description:** Write deploy + cleanup runbook.
- **Files/folders:** repo notes / deploy docs.
- **Expected output:** Runbook.
- **Verification:** Doc exists.
- **Risk/notes:** Hand-off readiness.

---

## Summary

- **8 phases · 44 milestones · 440 tasks** (every milestone = exactly 10 ordered tasks).
- Tasks are sequential within a milestone and small enough to implement one at a time.
- The build order from the integration plan still applies: do **Phase 1 → 2 (including production UI/UX sign-off) → 3 → 4 → 5 (login/shift/checkout/payment slice)** first for a usable terminal; **Phase 6 (sync)** and **Phase 7 (hardware)** can run in parallel after Phase 5; **Phase 8** finishes (except 8.4, which only needs 2.5).
- Standing guardrails held throughout: Phase 2 allows controlled UI modernization, `docs/ui-prototype/screens/*` and `Assets/ui/*` stay synchronized until one source is promoted, `index.html` is a simulator only, no business logic in the UI, offline-first, and the prototype is not deleted until production UI/UX sign-off is recorded.

**Next step (separate effort):** implementation, one task at a time, starting at Task 1.1.1. This document defines the boundaries; it does not contain code.
