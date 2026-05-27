# POS Desktop UI Integration - Current Session Context

## Current Milestone & Group
- **Milestone**: Phase 4 / Milestone 4.2 — Provisioning persistence & screen wiring
- **Group**: Group 1 (Task 4.2.1)

## Status of Tasks in this Session
- `[x]` Task 4.1.1 - Design the provisioning record (Completed)
- `[x]` Task 4.1.2 - Implement real `IProvisionedTerminalContext` (Completed)
- `[x]` Task 4.1.3 - Load provisioning state at startup (Completed)
- `[x]` Task 4.1.4 - Replace NoProvisionedTerminalContext registration (Completed)
- `[x]` Task 4.1.5 - Ensure consistency across DB scopes (Completed)
- `[x]` Task 4.1.6 - Handle the unprovisioned state (Completed)
- `[x]` Task 4.1.7 - Guard against half-provisioned state (Completed)
- `[x]` Task 4.1.8 - Verify provisioned reads return rows (Completed)
- `[x]` Task 4.1.9 - Verify unprovisioned reads are empty (Completed)
- `[x]` Task 4.1.10 - Integration test provisioned vs not (Completed)
- `[x]` Task 4.2.1 - Define the persistence store (Completed)

## Files Created/Changed in this Session
- [NEW] [TerminalProvisioning.cs](file:///A:/Ps/POS/POS.Desktop/Data/LocalEntities/TerminalProvisioning.cs)
- [NEW] [TerminalProvisioningConfiguration.cs](file:///A:/Ps/POS/POS.Desktop/Data/Configurations/Local/TerminalProvisioningConfiguration.cs)
- [NEW] [TerminalProvisioningStoreTests.cs](file:///A:/Ps/POS/POS.Desktop.Tests/Services/Provisioning/TerminalProvisioningStoreTests.cs)
- [NEW] EF Migration `AddTerminalProvisioningTable`
- [MODIFY] [PosLocalDbContext.cs](file:///A:/Ps/POS/POS.Desktop/Data/PosLocalDbContext.cs)
- [MODIFY] [POS_DESKTOP_CURRENT_CONTEXT.md](file:///A:/Ps/POS/docs/antigravity-context/POS_DESKTOP_CURRENT_CONTEXT.md)

*Note: Antigravity local implementation plan was created outside the repo.*

## Scope Boundaries & Constraints
- Work ONLY on Task 4.2.1.
- Do NOT start Task 4.2.2 (provisionTerminal handler).
- Do NOT add bridge handlers or wire UI screens.
- Do NOT add database tables or migrations beyond the minimal `TerminalProvisioning` table.

## Important Decisions
- Stored provisioning state in a local SQLite table `TerminalProvisioning` containing a single-row invariant (Id = 1) enforced at database level with a SQLite CHECK constraint.
- Added database CHECK constraints to enforce positive-or-null values on `TenantId`, `LocationId`, and `TerminalId` (`Id = 1`, `TenantId IS NULL OR TenantId > 0`, etc.) to prevent half-provisioned or invalid identities.
- Opted for SQLite over settings/config stores to guarantee consistency with the rest of the desktop's offline database, transactional safety, and lifecycle longevity next to other SQLite tables.
- Did not apply a query filter to the `TerminalProvisioning` entity, enabling startup loading of the provisioning state before a tenant context is established.

## Verification Commands & Results
- `git status --short --untracked-files=all`: Clean status prior to edits.
- `dotnet build POS.Desktop/POS.Desktop.csproj --configuration Debug`: Built successfully.
- `dotnet build POS.slnx --configuration Debug`: Built successfully.
- `dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug`: All 76 tests passed.

## Remaining Next Group
- Milestone 4.2 Group 2: Tasks 4.2.2 - 4.2.3 (provisionTerminal and getProvisioningStatus handlers).

## Known Risks & Notes
- Since persistent storage integration is deferred to Milestone 4.2, configuration is the sole startup source. If config is absent/malformed, the application startup continues cleanly but registers an unprovisioned context that fails closed on local DB filters.
- Current SyncOutbox query filter behavior appears tenant-scoped; location/terminal-specific filtering should be reviewed before Phase 5 services rely on it.
