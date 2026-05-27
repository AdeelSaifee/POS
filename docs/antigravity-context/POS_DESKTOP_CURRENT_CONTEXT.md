# POS Desktop UI Integration - Current Session Context

## Current Milestone & Group
- **Milestone**: Phase 4 / Milestone 4.2 — Provisioning persistence & screen wiring
- **Group**: Group 4 (Tasks 4.2.7 - 4.2.8) — Completed

## Status of Tasks in this Session
- `[x]` Task 4.2.7 - Persist tenant/location/terminal durably (startup load from SQLite)
- `[x]` Task 4.2.8 - Verify provisioning survives restart (temp-file restart test)

## Files Created/Changed in this Session
- [NEW] [TerminalProvisioningStartupLoader.cs](file:///A:/Ps/POS/POS.Desktop/Services/Provisioning/TerminalProvisioningStartupLoader.cs)
- [NEW] [TerminalProvisioningStartupLoaderTests.cs](file:///A:/Ps/POS/POS.Desktop.Tests/Services/Provisioning/TerminalProvisioningStartupLoaderTests.cs)
- [MODIFY] [App.xaml.cs](file:///A:/Ps/POS/POS.Desktop/App.xaml.cs)
- [MODIFY] [DesktopHostBuilder.cs](file:///A:/Ps/POS/POS.Desktop/Configuration/DesktopHostBuilder.cs)

## Scope Boundaries & Constraints
- Work ONLY on Tasks 4.2.7 and 4.2.8.
- Did NOT start Task 4.2.9.
- Did NOT implement controlled re-provisioning.
- Did NOT touch UI files or bridge handlers.
- Did NOT add migrations.
- Did NOT modify skills-lock.json or .agents/skills/*.

## Important Decisions
- Used a plain singleton service (`TerminalProvisioningStartupLoader`) rather than `IHostedService`.
  Reason: `host.StartAsync()` runs hosted services *before* `ApplyLocalDatabaseStartupAsync()` applies migrations. The startup loader must run *after* migrations so the `TerminalProvisioning` table is guaranteed to exist.
- `DesktopHostBuilder` continues to seed `ProvisionedTerminalContext` as `Unprovisioned` via `ProvisioningConfigLoader` (appsettings has no Provisioning section by default). The startup loader then overwrites that with the durable SQLite state.
- `TerminalProvisioningStartupLoader.LoadAsync()` catches all non-cancellation exceptions and stays fail-closed — it never crashes the app even if the DB read fails.
- Logs never contain raw tenant/location/terminal IDs; only state transitions ("provisioned", "unprovisioned", "partial/invalid") are logged.
- Restart-survival test uses a temp SQLite file + `SqliteConnection.ClearAllPools()` before cleanup to avoid a Windows file-lock on the temp DB.

## Startup Durable Load Approach
- `TerminalProvisioningStartupLoader` is registered as a singleton in `DesktopHostBuilder`.
- `App.xaml.cs` calls `loader.LoadAsync()` after `ApplyLocalDatabaseStartupAsync()` (migrations).
- The loader creates a DI scope, resolves `ITerminalProvisioningStore`, reads the persisted record.
- If record is fully valid → calls `ProvisionedTerminalContext.UpdateState(record)`.
- If no row / partial/invalid row → leaves context fail-closed (no state change).

## Verification Summary
- `dotnet build POS.Desktop/POS.Desktop.csproj --configuration Debug`: ✅ 0 warnings, 0 errors
- `dotnet build POS.slnx --configuration Debug`: ✅ 0 warnings, 0 errors
- `dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug`: ✅ 98/98 passed
- `Select-String provision_terminal.html terminal_config|localStorage|sessionStorage`: ✅ 0 matches
- `git diff --check`: ✅ no whitespace errors
- `git status`: ✅ exactly 4 expected files changed (2 new, 2 modified)

## Remaining Next Group
- Milestone 4.2 Group 5: Tasks 4.2.9 - 4.2.10 only (controlled re-provisioning flow and related tests).

## Known Risks & Notes
- `ProvisioningConfigLoader` is still called at DI registration time to provide the initial seed. Since `appsettings.json` has no `Provisioning` section, this always produces `Unprovisioned` — safe. If a `Provisioning` section is ever added to appsettings.json, it would seed the context before the startup loader can override it; the loader still wins because it runs later and calls `UpdateState`.
- SQLite connection pool holds file handles on Windows; `SqliteConnection.ClearAllPools()` is required before deleting temp test databases.
