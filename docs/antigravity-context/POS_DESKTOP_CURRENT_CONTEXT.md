# POS Desktop UI Integration - Current Session Context

## Current Milestone & Group
- **Milestone**: Phase 4 / Milestone 4.3 — Minimal local catalog schema & seed
- **Group**: Group 1 (Tasks 4.3.1 - 4.3.4) — Completed

## Status of Tasks in this Session
- `[x]` Task 4.3.1 - Decide catalog representation
- `[x]` Task 4.3.2 - Add/extend EF entities
- `[x]` Task 4.3.3 - Add EF configurations
- `[x]` Task 4.3.4 - Create a migration

## Files Created/Changed in this Session
- [ADD] `POS.Desktop/Data/LocalEntities/LocalCatalogEntity.cs` (and subclasses)
- [ADD] `POS.Desktop/Data/Configurations/Local/LocalCatalogConfigurations.cs`
- [MODIFY] `POS.Desktop/Data/PosLocalDbContext.cs`
- [ADD] `POS.Desktop/Data/Migrations/Local/*_AddLocalCatalogSchema.cs`

## Scope Boundaries & Constraints
- Only implemented tasks 4.3.1 through 4.3.4.
- Did NOT implement catalog seeding.
- Did NOT touch frontend UI files.
- Maintained fail-closed tenant scoping for all new catalog DbSets via `PosLocalDbContext` query filters.
- Skipped `LocalEmployee` since PINs are sensitive and it's not strictly required for the immediate catalog representation in Group 1.

## Important Decisions
- **Local Catalog Representation:** Decided to mirror central catalog entities (Category, Item, ItemVariant, ItemIdentifier, ItemPrice, UnitOfMeasure, TaxRule, TenderMethod, ReasonCode) with a subset of key columns, omitting complex tracking not required for offline checkout.
- **Tenant Isolation:** Kept `TenantId` on all catalog tables, relying on the `PosLocalDbContext` global query filter (based on `CurrentTenantId` from `IProvisionedTerminalContext`).
- **Primary Keys:** The local primary key `Id` represents the central `Id` to simplify synchronization and lookup, configured with `ValueGeneratedNever()`.

## Verification Summary
- `dotnet build POS.Desktop/POS.Desktop.csproj --configuration Debug`: ✔ 0 warnings, 0 errors
- `dotnet build POS.slnx --configuration Debug`: ✔ 0 warnings, 0 errors
- `dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug`: ✔ 105/105 passed
- `git status --short --untracked-files=all`: ✔ Shows only DB model changes and context file updates.

## Remaining Next Group
- Tasks 4.3.5 – 4.3.7: Implement offline catalog seed, seed tests, and wiring it into startup.

## Known Risks & Notes
- `LocalItemPrice` requires specific composite lookup behavior during checkout; keeping it separate from `LocalItemVariant` mirrors the central schema safely.
- Catalog will be populated by a temporary static/idempotent local seed in Tasks 4.3.5–4.3.7; live API/sync data comes later in Phase 6.
- `LocalEmployee` remains intentionally deferred. No plaintext PINs or sensitive auth fields are stored in Group 1. Real employee/auth handling belongs to later auth/security work.
