# POS Desktop UI Integration - Current Session Context

## Current Milestone & Group
- **Milestone**: Phase 4 / Milestone 4.4 - Catalog read service (replace ITEMS[])
- **Group**: Group 1 (Tasks 4.4.1 - 4.4.4) - Completed

## Status of Tasks in this Session
- `[x]` Task 4.4.1 - Define ICatalogService
- `[x]` Task 4.4.2 - Implement CatalogService
- `[x]` Task 4.4.3 - Add catalog DTOs
- `[x]` Task 4.4.4 - Add list/search/scan bridge handlers

## Files Created/Changed in this Session

### New (production)
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

### New (tests)
- [ADD] `POS.Desktop.Tests/Services/Catalog/CatalogServiceTests.cs`
- [ADD] `POS.Desktop.Tests/Shell/CatalogBridgeHandlerTests.cs`
- [MODIFY] `POS.Desktop.Tests/Shell/PosWebMessageRouterTests.cs`

## Scope Boundaries & Constraints
- Implemented tasks 4.4.1 through 4.4.4 only.
- Did NOT modify main_checkout.html.
- Did NOT remove ITEMS[] / CATEGORIES[] from any JS file.
- Did NOT add any API/network/sync code.
- Did NOT add a new EF migration.
- Did NOT add cart/order/payment logic.
- Did NOT add UI wiring or bridge call-sites in JavaScript.
- Did NOT start tasks 4.4.5 or later.
- No commit or push performed.

## Important Decisions
- **DTO location**: `POS.Desktop/Services/Catalog/` - co-located with the service, not under Bridge/.
- **ICatalogService is read-only**: No SaveChangesAsync, no mutations, no seeding.
- **Tenant scoping**: Service relies entirely on PosLocalDbContext global query filters. No IgnoreQueryFilters() in service reads. Unprovisioned terminal (CurrentTenantId=0) returns empty collections naturally.
- **Default price list**: `PriceListId = 1` is the only supported list at this phase (matches seed data).
- **BuildItemQuery()**: Single private method returns `IQueryable<CatalogItemDto>` composed from 7-table join with inner joins for required data (variant, price, UoM) and left joins for optional data (identifier, category, tax rule). All downstream methods compose Where/Take/OrderBy on top. Active-only: `where item.Status == ItemStatus.Active`; variant join filters `.Where(v => v.IsDefault && v.IsSellable && v.Status == ItemStatus.Active)`.
- **FindByIdentifierAsync**: Trims identifierValue before the DB lookup; blank/whitespace still returns null via the `IsNullOrWhiteSpace` guard.
- **Status field**: Uses a CASE-style conditional expression (`item.Status == ItemStatus.Active ? "Active" : ...`) instead of `.ToString()` to guarantee safe SQL translation with EF Core 8 + SQLite.
- **Search**: Uses `EF.Functions.Like(field, "%text%")` - case-insensitive for ASCII in SQLite.
- **Bridge handler pattern**: 4 private handler methods in `PosWebMessageRouter.cs`, registered in the constructor. `ParseCatalogItemQuery` is a `static` helper. All handlers catch `JsonException` and return safe structured errors with no stack traces.
- **DI registration**: `services.AddScoped<ICatalogService, CatalogService>()` in `DesktopHostBuilder.cs`.
- **Router test update**: `PosWebMessageRouterTests.cs` updated - type count assertion from 6 to 10, 4 new catalog type assertions in `Router_ExposesBuiltInHandlers`.
- **No non-ASCII bytes**: All comment em-dashes and en-dashes replaced with plain ASCII `-` across all catalog files.

## Bridge Message Types Added
- `catalog.listCategories` - payload: `{}` (optional), response: `{ categories: [...] }`
- `catalog.listItems` - payload: `{ categoryId?, searchText?, limit? }`, response: `{ items: [...] }`
- `catalog.searchItems` - payload: `{ searchText?, limit? }`, response: `{ items: [...] }`
- `catalog.lookupByIdentifier` - payload: `{ identifierValue }`, response: `{ found, item }`

## Verification Summary
- `dotnet build POS.Desktop/POS.Desktop.csproj --configuration Debug`: 0 warnings, 0 errors
- `dotnet build POS.slnx --configuration Debug`: 0 warnings, 0 errors
- `dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug`: 159/159 passed (122 prior + 34 Group 1 + 3 cleanup)
- `git diff --check`: No whitespace errors (LF/CRLF note on context file is non-blocking)
- `git status --short --untracked-files=all`: Only expected files (4 M, 10 ??)

## Tests Added (37 new: 34 Group 1 + 3 cleanup)

**CatalogServiceTests.cs (25 tests):**
- ListCategories: returns 3 categories; sorted by SortOrder; unprovisioned returns empty
- ListItems: returns all 3 items; all joined fields populated (category, tax, identifier, UoM, status, isSellable); category filter; limit respected; unprovisioned returns empty
- SearchItems: by name, by code, by SKU, by identifier value; blank text returns all; no match returns empty; unprovisioned returns empty
- FindByIdentifier: known barcode returns correct item; unknown returns null; blank returns null; unprovisioned returns null; whitespace-padded barcode resolves correctly
- Active-only filtering: blocked item excluded from ListItems; non-sellable variant item excluded from ListItems

**CatalogBridgeHandlerTests.cs (12 tests):**
- All 4 handlers registered in router
- listCategories: returns 3 categories; unprovisioned returns empty array
- listItems: no payload returns all; category filter works; unprovisioned returns empty
- searchItems: name match; no match; empty payload returns all
- lookupByIdentifier: known barcode found=true; unknown found=false item=null; missing payload MALFORMED_REQUEST; missing identifierValue property MALFORMED_REQUEST; unprovisioned found=false; error response does not leak stack traces

## Prior Milestone Context (4.3 complete)
- Catalog tables exist in SQLite (migration `20260527064212_AddLocalCatalogSchema`)
- Seed: 2 UoMs, 1 TaxRule, 3 Categories, 3 Items, 3 Variants, 3 Identifiers, 3 Prices, 2 TenderMethods, 3 ReasonCodes
- Seeder is idempotent and retargets on re-provision
- 122 tests were passing before this group

## Remaining Next Group
- Tasks 4.4.5 - 4.4.8 (UI wiring: wire catalog.listCategories/listItems/searchItems/lookupByIdentifier into main_checkout.html; replace ITEMS[]/CATEGORIES[]; connect product grid, category chips, search bar, scan input)

## Known Risks & Notes
- No EF navigation properties configured between catalog entities - service uses explicit LINQ joins.
- `BuildItemQuery()` inner-joins on IsDefault variant and PriceListId=1; items without these will not appear in results. Acceptable for bootstrap phase.
- Left joins for identifier/category/taxRule are in the ON clause for EF Core 8 SQLite - tested and working.
- `EF.Functions.Like` is case-insensitive for ASCII in SQLite. Non-ASCII barcode search may not be case-insensitive.
- Phase 6 live sync will replace bootstrap seed with real central data.
