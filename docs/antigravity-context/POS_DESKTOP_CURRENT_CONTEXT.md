# POS Desktop UI Integration - Current Session Context

## Current Milestone & Group
- **Milestone**: Phase 4 / Milestone 4.3 — Minimal local catalog schema & seed
- **Group**: Group 2 (Tasks 4.3.5 - 4.3.7) — Completed

## Status of Tasks in this Session
- `[x]` Task 4.3.1 - Decide catalog representation
- `[x]` Task 4.3.2 - Add/extend EF entities
- `[x]` Task 4.3.3 - Add EF configurations
- `[x]` Task 4.3.4 - Create a migration
- `[x]` Task 4.3.5 - Define a minimal seed dataset
- `[x]` Task 4.3.6 - Implement an idempotent seed routine
- `[x]` Task 4.3.7 - Run seed post-provision

## Files Created/Changed in this Session
- [ADD] `POS.Desktop/Data/Seeding/ILocalCatalogSeeder.cs`
- [ADD] `POS.Desktop/Data/Seeding/LocalCatalogSeeder.cs`
- [ADD] `POS.Desktop/Data/Seeding/LocalCatalogSeedData.cs`
- [MODIFY] `POS.Desktop/Services/Provisioning/EfTerminalProvisioningStore.cs`
- [MODIFY] `POS.Desktop/Configuration/DesktopHostBuilder.cs`
- [ADD] `POS.Desktop.Tests/Data/Seeding/LocalCatalogSeederTests.cs`
- [MODIFY] `POS.Desktop.Tests/Services/Provisioning/TerminalProvisioningStartupLoaderTests.cs`
- [MODIFY] `POS.Desktop.Tests/Services/Provisioning/TerminalProvisioningStoreHandlerTests.cs`

## Scope Boundaries & Constraints
- Implemented tasks 4.3.5 through 4.3.7 only.
- Did NOT add any UI/HTML/JS/CSS changes.
- Did NOT add any API/network/sync code.
- Did NOT add a new EF migration (schema from Group 1 is sufficient).
- Did NOT add a catalog read service or bridge handler.
- Did NOT add LocalEmployee/auth/PIN work.
- No commit or push performed.

## Important Decisions
- **Seeding folder**: `POS.Desktop/Data/Seeding/` — clean separation from Services.
- **`LocalCatalogSeedData`**: `internal` static class; holds deterministic constant data only. `EffectiveFrom` is a fixed `DateTimeOffset(2026, 1, 1, ...)` — never `UtcNow`.
- **Enum symbols used**: `ItemType.Stock`, `ItemStatus.Active`, `MeasurementType.Count/Weight`, `TaxCalculationMode.Inclusive` — no magic integers.
- **Idempotency / retarget strategy**: The seeder uses an insert-or-retarget (upsert) policy keyed on `Id`. Because local catalog PKs are `ValueGeneratedNever()` with a single-column PK, the same bootstrap Id cannot exist for two tenants in the same SQLite DB. When a row with the same Id already exists (e.g., after controlled re-provision to a different tenant), the seeder calls `Entry(existing).CurrentValues.SetValues(seedRow)` — retargeting `TenantId` and all seed-controlled fields to the new tenant. No duplicate rows are created. `IgnoreQueryFilters()` used in all seeder queries.
- **Re-provision model**: Because local bootstrap IDs use a single-column PK, the temporary seed represents the currently provisioned tenant only. On controlled re-provision to a different tenant, existing bootstrap seed rows are retargeted to the new tenant (not orphaned). After re-provision, the new tenant has full catalog visibility and the old TenantId has zero visible rows. Phase 6 live sync will replace this bootstrap entirely.
- **Seeder wiring**: `ILocalCatalogSeeder` injected into `EfTerminalProvisioningStore`. Called after `UpdateState(newRecord)` in `ProvisionTerminalAsync`. Seed failure returns `ProvisioningResult(false, "SEED_FAILED", "...")` — provisioning row is already committed, so a retry with the same IDs is safe.
- **Logger added**: `ILogger<EfTerminalProvisioningStore>` added to constructor for seed failure logging.
- **Existing test compatibility**: `TerminalProvisioningStoreHandlerTests` and `TerminalProvisioningStartupLoaderTests` updated to register `ILocalCatalogSeeder` in their DI setups. `AddLogging()` added where missing for `ILogger<T>` resolution.

## Seed Dataset Summary
- Units of Measure: 2 (EACH, KG)
- Tax Rules: 1 (GST17 — 17% inclusive)
- Categories: 3 (GROCERY, BEVERAGES, SNACKS)
- Items: 3 (Mineral Water, Whole Milk, Salted Crackers)
- Item Variants: 3 (one default sellable variant per item)
- Item Identifiers: 3 (BARCODE per variant)
- Item Prices: 3 (PriceListId=1, IsTaxIncluded=true)
- Tender Methods: 2 (CASH, CARD)
- Reason Codes: 3 (DISC-MGR, VOID-ITEM, RETURN-STD)

## Verification Summary
- `dotnet build POS.Desktop/POS.Desktop.csproj --configuration Debug`: ✔ 0 warnings, 0 errors
- `dotnet build POS.slnx --configuration Debug`: ✔ 0 warnings, 0 errors
- `dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug`: ✔ 116/116 passed (105 prior + 11 new)
- `git status --short --untracked-files=all`: ✔ Only expected files; no UI/migration/API changes
- `git diff --check`: ✔ No whitespace errors (CRLF warnings only — Windows git normal behavior)

## Tests Added (11 new)
In `POS.Desktop.Tests/Data/Seeding/LocalCatalogSeederTests.cs`:
1. `SeedAsync_InsertsExpectedRowCounts_ForProvisionedTenant`
2. `SeedAsync_IsIdempotent_RowCountsStableOnRerun`
3. `SeedAsync_WithInvalidTenantId_ThrowsAndInsertsNothing` (Theory ×2)
4. `SeedAsync_EachTenantGetCorrectTenantIdOnAllRows`
5. `CatalogEntities_DoNotHaveSensitiveProperties`
6. `ProvisionTerminalAsync_CallsSeeder_WithCorrectTenantIdAfterSuccess`
7. `ProvisionTerminalAsync_WithInvalidPayload_DoesNotCallSeeder` (Theory ×3)
8. `ProvisionTerminalAsync_WhenSeederThrows_ReturnsSafeStructuredError`

## Remaining Next Group
- Tasks 4.3.8 – 4.3.10

## Known Risks & Notes
- Local seed IDs are bootstrap-only. Phase 6 live sync will replace this seed with real central data.
- Re-provisioning retargets existing rows to the new tenant — no orphaned rows left behind for the old tenant.
- `LocalCatalogSeedData` is `internal` — not exposed outside `POS.Desktop`. Tests access it indirectly via `LocalCatalogSeeder` and DbContext.
- Group 2 seeder fix (cleanup pass): initial implementation had an ID-skip bug where re-provision to a different tenant in the same DB would leave the new tenant with zero visible catalog rows. Fixed by switching from skip-on-existing-Id to insert-or-retarget (upsert) using `Entry.CurrentValues.SetValues`.
