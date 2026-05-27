# POS Desktop UI Integration - Current Session Context

## Current Milestone & Group
- **Milestone**: Phase 4 / Milestone 4.5 - Data-access conventions & tenant-filter validation
- **Group**: Group 1 (Tasks 4.5.1 - 4.5.3) - Completed

## Status of Tasks in this Session
- `[x]` Task 4.5.1 - Draft data-access conventions
- `[x]` Task 4.5.2 - Standardize per-message scope usage
- `[x]` Task 4.5.3 - Define service base patterns

## Files Created/Changed in this Session

### Group 1 (Current uncommitted changes)
- [ADD] `docs/desktop/DATA_ACCESS_CONVENTIONS.md`
- [MODIFY] `docs/antigravity-context/POS_DESKTOP_CURRENT_CONTEXT.md`

### Prior Milestone 4.4 Completed Changes (Committed)
- [ADD] `POS.Desktop/Services/Catalog/ICatalogService.cs`
- [ADD] `POS.Desktop/Services/Catalog/CatalogService.cs`
- [ADD] `POS.Desktop/Services/Catalog/CatalogItemQuery.cs`
- [ADD] `POS.Desktop/Services/Catalog/CatalogCategoryDto.cs`
- [ADD] `POS.Desktop/Services/Catalog/CatalogItemDto.cs`
- [ADD] `POS.Desktop/Services/Catalog/CatalogListCategoriesResponse.cs`
- [ADD] `POS.Desktop/Services/Catalog/CatalogListItemsResponse.cs`
- [ADD] `POS.Desktop/Services/Catalog/CatalogLookupResponse.cs`
- [MODIFY] `POS.Desktop/Shell/PosWebMessageRouter.cs`
- [MODIFY] `POS.Desktop/Configuration/DesktopHostBuilder.cs`
- [ADD] `POS.Desktop.Tests/Services/Catalog/CatalogServiceTests.cs`
- [ADD] `POS.Desktop.Tests/Shell/CatalogBridgeHandlerTests.cs`
- [MODIFY] `POS.Desktop.Tests/Shell/PosWebMessageRouterTests.cs`
- [MODIFY] `POS.Desktop/Assets/ui/main_checkout.html`
- [MODIFY] `POS.Desktop/Data/Configurations/Local/LocalCatalogConfigurations.cs`
- [ADD] `POS.Desktop/Data/Migrations/Local/*_AddLocalCatalogSearchIndexes.cs`
- [MODIFY] `POS.Desktop.Tests/Data/LocalCatalogMigrationTests.cs`
- [ADD] `POS.Desktop.Tests/Assets/MainCheckoutCatalogWiringTests.cs`

## Scope Boundaries & Constraints
- Milestone 4.5 Group 1 is documentation-only, defining core patterns for future database migrations and repository authors.
- No production C# code changes or JS client UI modifications were made.
- Did NOT implement unprovisioned test integration or SQLite harnesses (deferred to Group 2).
- Work is left in the working directory uncommitted and unpushed for review.

## Important Decisions

### Group 1 (Milestone 4.5)
- **DbContext Lifetime:** Standardized Scoped lifetime for `PosLocalDbContext` within the DI container, tying context instance lifetimes strictly to the logical scope of a single asynchronously executed bridge message.
- **Router DI Scoping:** Verified and documented `PosWebMessageRouter.RouteAsync`'s use of `IServiceScopeFactory.CreateScope()` to generate isolated scopes for message handling, ensuring proper disposal of database connections.
- **Service Boundaries:** Business logic remains strictly in C# services (resolved dynamically inside scopes) while handlers in `PosWebMessageRouter` serve solely as thin correlation dispatch wrappers mapping exceptions to structured, safe errors. No database query logic is allowed directly in UI JS files or router handlers.
- **Global Tenant Filters:** Confirmed global query filters on `PosLocalDbContext` enforce segregation by default, failing closed (zero rows returned) in unprovisioned states when current tenant ID defaults to `0`. Avoid `IgnoreQueryFilters()` except under explicitly reviewed seeding or sync code.

## Bridge Message Types (All Active)
- `catalog.listCategories` - payload: `{}`, response: `{ categories: [...] }`
- `catalog.listItems` - payload: `{ categoryId?, searchText?, limit? }`, response: `{ items: [...] }`
- `catalog.searchItems` - payload: `{ searchText?, limit? }`, response: `{ items: [...] }`
- `catalog.lookupByIdentifier` - payload: `{ identifierValue }`, response: `{ found, item }`

## Verification Summary (Milestone 4.5 Group 1)
- `git status --short --untracked-files=all`:
  - ` M docs/antigravity-context/POS_DESKTOP_CURRENT_CONTEXT.md`
  - ` A docs/desktop/DATA_ACCESS_CONVENTIONS.md`
- `dotnet build POS.slnx --configuration Debug`: 0 warnings, 0 errors.
- `dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug`: 161/161 passed.
- `git diff --check`: No whitespace errors.

## Existing Test Suite Coverage (161 tests baseline)

**CatalogServiceTests.cs (25 tests):**
- ListCategories: returns 3 categories; sorted by SortOrder; unprovisioned returns empty
- ListItems: returns all 3 items; all joined fields populated; category filter; limit respected; unprovisioned returns empty
- SearchItems: by name, by code, by SKU, by identifier value; blank returns all; no match returns empty; unprovisioned returns empty
- FindByIdentifier: known barcode; unknown returns null; blank returns null; unprovisioned returns null; whitespace-padded barcode resolves correctly
- Active-only filtering: blocked item excluded; non-sellable variant excluded

**CatalogBridgeHandlerTests.cs (12 tests):**
- All 4 handlers registered; listCategories, listItems, searchItems, lookupByIdentifier full coverage; unprovisioned returns empty/found=false; no stack traces in error responses

**LocalCatalogMigrationTests.cs (1 test):**
- Verify index migration successfully registers and creates SQLite search indexes.

**MainCheckoutCatalogWiringTests.cs (1 test):**
- Verify main_checkout.html has no static items/categories arrays and uses bridge handlers.

## Prior Milestone Context (4.4 complete)
- ICatalogService and CatalogService exist.
- Catalog bridge handlers are active (`catalog.listCategories`, `catalog.listItems`, `catalog.searchItems`, `catalog.lookupByIdentifier`).
- `main_checkout.html` uses bridge-backed catalog for product grid, category chips, search, and barcode scan.
- Static `ITEMS` and `CATEGORIES` arrays are removed.
- `AddLocalCatalogSearchIndexes` migration exists, creating local database indexes for lookups.
- `POS.Desktop.Tests` baseline passes at 161/161.

## Remaining Next Milestone
- Phase 4 / Milestone 4.5 Group 2 (Tasks 4.5.4 - 4.5.5)

## Known Risks & Notes
- No EF navigation properties on catalog entities - service uses explicit LINQ joins.
- `BuildItemQuery()` inner-joins on IsDefault variant and PriceListId=1; items without these won't appear.
- `EF.Functions.Like` is case-insensitive for ASCII in SQLite. Non-ASCII barcode search may not be case-insensitive.
- Phase 6 live sync will replace bootstrap seed with real central data.
