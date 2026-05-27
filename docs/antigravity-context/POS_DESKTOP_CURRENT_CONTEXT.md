# POS Desktop UI Integration - Current Session Context

## Current Milestone & Group
- **Milestone**: Phase 4 / Milestone 4.2 — Provisioning persistence & screen wiring
- **Group**: Group 2 (Tasks 4.2.2 - 4.2.3)

## Status of Tasks in this Session
- `[x]` Task 4.2.2 - Add provisionTerminal handler (Completed)
- `[x]` Task 4.2.3 - Add getProvisioningStatus handler (Completed)

## Files Created/Changed in this Session
- [NEW] [ITerminalProvisioningStore.cs](file:///A:/Ps/POS/POS.Desktop/Services/Provisioning/ITerminalProvisioningStore.cs)
- [NEW] [EfTerminalProvisioningStore.cs](file:///A:/Ps/POS/POS.Desktop/Services/Provisioning/EfTerminalProvisioningStore.cs)
- [NEW] [TerminalProvisioningStoreHandlerTests.cs](file:///A:/Ps/POS/POS.Desktop.Tests/Services/Provisioning/TerminalProvisioningStoreHandlerTests.cs)
- [MODIFY] [ProvisioningRecord.cs](file:///A:/Ps/POS/POS.Desktop/Services/Provisioning/ProvisioningRecord.cs)
- [MODIFY] [DesktopHostBuilder.cs](file:///A:/Ps/POS/POS.Desktop/Configuration/DesktopHostBuilder.cs)
- [MODIFY] [PosWebMessageRouter.cs](file:///A:/Ps/POS/POS.Desktop/Shell/PosWebMessageRouter.cs)
- [MODIFY] [POS_DESKTOP_CURRENT_CONTEXT.md](file:///A:/Ps/POS/docs/antigravity-context/POS_DESKTOP_CURRENT_CONTEXT.md)

## Scope Boundaries & Constraints
- Work ONLY on Task 4.2.2 and Task 4.2.3.
- Do NOT start Task 4.2.4 (wiring HTML screens).
- Do NOT touch UI screens or remove `terminal_config` localStorage.
- Do NOT add database tables or migrations beyond the minimal `TerminalProvisioning` table.

## Important Decisions
- Extracted database persistence and validation into a separate service `EfTerminalProvisioningStore` to keep `PosWebMessageRouter` thin and focused on bridge mechanics.
- Registered the new store service as a scoped dependency to avoid captive dependency on the scoped DbContext.
- Registered `provisioning.provisionTerminal` and `provisioning.getProvisioningStatus` actions using namespaced conventions.
- Blocked re-provisioning over the bridge if the database is already fully provisioned with different details, keeping state updates secure.
- Designed `getProvisioningStatus` to read from the SQLite database and return a fail-closed status if the DB row is missing or contains partial/invalid null fields.

## Verification Commands & Results
- `git status --short --untracked-files=all`: Clean status prior to edits.
- `dotnet build POS.Desktop/POS.Desktop.csproj --configuration Debug`: Built successfully.
- `dotnet build POS.slnx --configuration Debug`: Built successfully.
- `dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug`: All 91 tests passed.

## Remaining Next Group
- Milestone 4.2 Group 3: Tasks 4.2.4 - 4.2.6 (wire `provision_terminal.html` to bridge, remove setTimeout, remove localStorage).

## Known Risks & Notes
- Re-provisioning attempts with different details return `REPROVISION_BLOCKED`. Controlled re-provisioning under a guarded path belongs to Task 4.2.9.
