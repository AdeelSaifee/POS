# POS Desktop UI Integration - Current Session Context

## Current Milestone & Group
- **Milestone**: Phase 4 / Milestone 4.2 ?" Provisioning persistence & screen wiring
- **Group**: Group 5 (Tasks 4.2.9 - 4.2.10) ?" Completed

## Status of Tasks in this Session
- `[x]` Task 4.2.9 - Support controlled re-provisioning
- `[x]` Task 4.2.10 - Test provisioning without catalog seed

## Files Created/Changed in this Session
- [MODIFY] `POS.Desktop/Services/Provisioning/ITerminalProvisioningStore.cs`
- [MODIFY] `POS.Desktop/Services/Provisioning/EfTerminalProvisioningStore.cs`
- [MODIFY] `POS.Desktop/Shell/PosWebMessageRouter.cs`
- [MODIFY] `POS.Desktop.Tests/Services/Provisioning/TerminalProvisioningStoreHandlerTests.cs`
- [MODIFY] `POS.Desktop.Tests/Services/Provisioning/TerminalProvisioningStartupLoaderTests.cs`

## Scope Boundaries & Constraints
- Work ONLY on Tasks 4.2.9 and 4.2.10.
- Did NOT start Milestone 4.3.
- Did NOT add catalog seed or catalog tables.
- Did NOT touch UI files (frontend).
- Did NOT add migrations.
- Did NOT modify skills-lock.json or .agents/skills/*.

## Important Decisions
- **Controlled re-provisioning behavior:** Added `allowReprovision` boolean to the `provisioning.provisionTerminal` bridge payload and `ProvisionTerminalAsync` method. Normal calls (missing or `false`) remain blocked from re-provisioning if already provisioned. Explicit override (`allowReprovision = true`) allows safely updating the existing row (Id = 1) and in-memory context.
- **Provisioning without catalog seed verification:** Added tests utilizing an empty SQLite schema to confirm that provisioning strictly persists the `TerminalProvisioning` row and succeeds independently of any catalog table setup or seeding.
- **UI untouched:** Given instructions, since "confirmation UI is too much, keep UI unchanged and only implement backend/test-controlled path", `provision_terminal.html` remains unchanged while the backend safely supports the override.

## Verification Summary
- `dotnet build POS.Desktop/POS.Desktop.csproj --configuration Debug`: o. 0 warnings, 0 errors
- `dotnet build POS.slnx --configuration Debug`: o. 0 warnings, 0 errors
- `dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug`: o. 104/104 passed
- `Select-String provision_terminal.html terminal_config|localStorage|sessionStorage`: o. 0 matches
- `git diff --check`: o. no whitespace errors
- `git status`: o. exactly 6 expected files modified (including this context file)

## Remaining Next Group
- Milestone 4.3 only.

## Known Risks & Notes
- Re-provisioning overrides the tenant/location/terminal ID directly. Depending on cache invalidation strategy down the line, modifying these values during runtime might necessitate careful re-scoping of currently displayed local tenant-scoped data in UI. Handled safely right now by immediately updating the `ProvisionedTerminalContext`.
