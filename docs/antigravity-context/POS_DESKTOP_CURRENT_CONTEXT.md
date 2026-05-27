# POS Desktop UI Integration - Current Session Context

## Current Milestone & Group
- **Milestone**: Phase 4 / Milestone 4.4 - Catalog read service (replace ITEMS[])
- **Group**: Group 3 (Tasks 4.4.9 - 4.4.10) - Completed

## Status of Tasks in this Session
- `[x]` Task 4.4.1 - Define ICatalogService
- `[x]` Task 4.4.2 - Implement CatalogService
- `[x]` Task 4.4.3 - Add catalog DTOs
- `[x]` Task 4.4.4 - Add list/search/scan bridge handlers
- `[x]` Task 4.4.5 - Wire product grid to catalog.listItems
- `[x]` Task 4.4.6 - Wire category chips to catalog.listCategories
- `[x]` Task 4.4.7 - Wire search bar to catalog.searchItems; scan input to catalog.lookupByIdentifier
- `[x]` Task 4.4.8 - Remove ITEMS[] and CATEGORIES[] static arrays
- `[x]` Task 4.4.9 - Index identifiers/SKUs
- `[x]` Task 4.4.10 - Test catalog rendering + search

## Files Created/Changed in this Session

### Group 1 & 2 (Committed in prior sessions)
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

### Group 3 (Current uncommitted changes)
- [MODIFY] `POS.Desktop/Data/Configurations/Local/LocalCatalogConfigurations.cs`
- [ADD] `POS.Desktop/Data/Migrations/Local/*_AddLocalCatalogSearchIndexes.cs`
- [MODIFY] `POS.Desktop.Tests/Data/LocalCatalogMigrationTests.cs`
- [ADD] `POS.Desktop.Tests/Assets/MainCheckoutCatalogWiringTests.cs`
- [MODIFY] `docs/antigravity-context/POS_DESKTOP_CURRENT_CONTEXT.md`

## Scope Boundaries & Constraints
- Group 3 changes add SQLite search/lookup indexes via EF migration and verify wiring via static HTML asserts and migration tests.
- No C# production code mutations/DTO changes were made.
- Did NOT add EF migration/schema changes other than index creation/drop.
- Did NOT add cart/order/payment persistence (cart remains sessionStorage prototype).
- Did NOT integrate hardware scanner (deferred to Phase 7.4).
- Did NOT add external JS libraries.
- Work is left in the working directory uncommitted and unpushed for review.

## Important Decisions

### Group 1
- **DTO location**: `POS.Desktop/Services/Catalog/` - co-located with the service.
- **ICatalogService is read-only**: No SaveChangesAsync, no mutations, no seeding.
- **Tenant scoping**: Relies on PosLocalDbContext global query filters. No IgnoreQueryFilters(). Unprovisioned terminal returns empty collections.
- **Default price list**: `PriceListId = 1` only (matches seed data).
- **BuildItemQuery()**: 7-table join; inner joins for variant (IsDefault && IsSellable && Active), price (PriceListId=1), UoM; left joins for identifier/category/taxRule. Active-only: `where item.Status == ItemStatus.Active`.
- **FindByIdentifierAsync**: Trims identifierValue before DB lookup; blank returns null.
- **Status field**: CASE-style conditional expression to guarantee safe SQL translation with EF Core 8 + SQLite.
- **Search**: `EF.Functions.Like(field, "%text%")` - case-insensitive for ASCII in SQLite.
- **Bridge handler pattern**: 4 private handlers in PosWebMessageRouter.cs; catch JsonException; no stack traces in error responses.
- **DI registration**: `services.AddScoped<ICatalogService, CatalogService>()` in DesktopHostBuilder.cs.
- **No non-ASCII bytes**: All em-dashes/en-dashes replaced with plain ASCII `-`.

### Group 2
- **Catalog caches**: `let catalogItems = []` and `let catalogCategories = []` replace `const ITEMS` and `const CATEGORIES`.
- **State**: `activeCategoryId = null` (null=All, numeric=specific bridge category ID) replaces `activeCategory = 'all'`. `_searchTimer` for 280ms debounce added.
- **bridgeRequest()**: Wrapper checks `window.posBridge.isAvailable()` before calling; rejects with `TRANSPORT_UNAVAILABLE` if not ready.
- **getCatColor(categoryCode)**: Normalizes to lowercase; fallback palette in `CAT_COLORS` object.
- **loadCatalogCategories()**: Calls `catalog.listCategories`; on error logs warning and falls back to empty array. Always calls `renderCategories()`.
- **loadCatalogItems(query)**: Calls `catalog.listItems`; shows loading state first. On error falls back to empty array and calls `renderGrid()`.
- **renderCategories()**: Builds chips dynamically from `catalogCategories`. Synthetic "All Items" chip always first. onclick sets `activeCategoryId` then calls `loadCatalogItems`.
- **renderGrid()**: Iterates `catalogItems` (CatalogItemDto); card id = `String(item.variantId)`. Uses item initial as placeholder. Shows "No items found" if empty.
- **handleSearch()**: 280ms debounce; blank resets to `loadCatalogItems`; non-blank calls `catalog.searchItems`.
- **simulateBarcodeScan()**: Reads search input value; calls `catalog.lookupByIdentifier`; on found adds to cart and clears input; on not-found shows toast.
- **addToCart(item)**: Takes full CatalogItemDto. Cart line: `{ id: String(item.variantId), itemId, variantId, name, qty, price, unit, cat }`.
- **Hardware scanner**: Deferred to Phase 7.4; current Scan button uses manual search-input value.
- **Demo cart**: Pre-populated fallback rows removed. The cart now initializes empty (or loads from `sessionStorage`) to align with dynamic catalog wiring.
- **HTML Escaping**: Added `escapeHtml()` helper to escape dynamic catalog strings before inserting into `innerHTML` (specifically inside the product card grid, toast notifications, and cart items).

### Group 3
- **Indexes on Local Tables**: Added key indexes for CategoryId, Name, SKU, ItemVariantId, ItemId, and SortOrder to optimize SQLite joins and search paths in CatalogService.
- **EF Migration**: Added `AddLocalCatalogSearchIndexes` which strictly creates the seven SQLite indexes, ensuring non-destructive and safe operations on the database schema.
- **Verification Strategy**: Added `MainCheckoutCatalogWiringTests` to assert layout wiring properties directly against `main_checkout.html` text, verifying zero ITEMS/CATEGORIES regression or old ID leaks.

## Bridge Message Types (All Active)
- `catalog.listCategories` - payload: `{}`, response: `{ categories: [...] }`
- `catalog.listItems` - payload: `{ categoryId?, searchText?, limit? }`, response: `{ items: [...] }`
- `catalog.searchItems` - payload: `{ searchText?, limit? }`, response: `{ items: [...] }`
- `catalog.lookupByIdentifier` - payload: `{ identifierValue }`, response: `{ found, item }`

## Verification Summary (Group 3)
- `git status --short --untracked-files=all`:
  - ` M POS.Desktop.Tests/Data/LocalCatalogMigrationTests.cs`
  - ` M POS.Desktop/Data/Configurations/Local/LocalCatalogConfigurations.cs`
  - ` M POS.Desktop/Data/Migrations/Local/PosLocalDbContextModelSnapshot.cs`
  - `?? POS.Desktop.Tests/Assets/MainCheckoutCatalogWiringTests.cs`
  - `?? POS.Desktop/Data/Migrations/Local/*_AddLocalCatalogSearchIndexes.Designer.cs`
  - `?? POS.Desktop/Data/Migrations/Local/*_AddLocalCatalogSearchIndexes.cs`
- `dotnet build POS.Desktop/POS.Desktop.csproj --configuration Debug`: 0 warnings, 0 errors
- `dotnet build POS.slnx --configuration Debug`: 0 warnings, 0 errors
- Solution Unit Tests:
  - `POS.Desktop.Tests.dll`: 161/161 passed (includes index schema asserts & static HTML wiring tests)
  - `POS.Tests.dll`: 49/49 passed
- `git diff --check`: No whitespace errors in code
- `const ITEMS` in main_checkout.html: 0 matches (removed)
- `const CATEGORIES` in main_checkout.html: 0 matches (removed)
- `escapeHtml` in main_checkout.html: used for item name, categories, initials, and toast/cart rendering
- All 4 catalog bridge calls present in main_checkout.html

## Tests Added (39 total: 34 Group 1 + 3 cleanup + 2 Group 3)

**CatalogServiceTests.cs (25 tests):**
- ListCategories: returns 3 categories; sorted by SortOrder; unprovisioned returns empty
- ListItems: returns all 3 items; all joined fields populated; category filter; limit respected; unprovisioned returns empty
- SearchItems: by name, by code, by SKU, by identifier value; blank returns all; no match returns empty; unprovisioned returns empty
- FindByIdentifier: known barcode; unknown returns null; blank returns null; unprovisioned returns null; whitespace-padded barcode resolves correctly
- Active-only filtering: blocked item excluded; non-sellable variant excluded

**CatalogBridgeHandlerTests.cs (12 tests):**
- All 4 handlers registered; listCategories, listItems, searchItems, lookupByIdentifier full coverage; unprovisioned returns empty/found=false; no stack traces in error responses

**LocalCatalogMigrationTests.cs (4 tests):**
- MigrateAsync_AppliesSearchIndexes (Group 3): Verifies index migration successfully registers in `__EFMigrationsHistory` and creates key SQLite indexes (`IX_LocalItemVariants_TenantId_ItemId`, `IX_LocalItemVariants_TenantId_SKU`, etc.).

**MainCheckoutCatalogWiringTests.cs (1 test):**
- MainCheckout_Html_MatchesCatalogWiringExpectations (Group 3): Asserts removal of static ITEMS/CATEGORIES arrays, removal of fallback IDs (I1001, I1002, I1008), presence of bridge message calls, and use of `escapeHtml` and `addToCart(item)`.

## Prior Milestone Context (4.3 complete)
- Catalog tables exist in SQLite (migration `20260527064212_AddLocalCatalogSchema`)
- Seed: 2 UoMs, 1 TaxRule, 3 Categories, 3 Items, 3 Variants, 3 Identifiers, 3 Prices, 2 TenderMethods, 3 ReasonCodes
- Seeder is idempotent and retargets on re-provision
- 122 tests were passing before Group 1

## Remaining Next Milestone
- Phase 4 / Milestone 4.5

## Known Risks & Notes
- No EF navigation properties on catalog entities - service uses explicit LINQ joins.
- `BuildItemQuery()` inner-joins on IsDefault variant and PriceListId=1; items without these won't appear.
- `EF.Functions.Like` is case-insensitive for ASCII in SQLite. Non-ASCII barcode search may not be case-insensitive.
- Phase 6 live sync will replace bootstrap seed with real central data.
