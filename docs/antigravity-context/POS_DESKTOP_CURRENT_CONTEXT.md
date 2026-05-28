# POS Desktop UI Integration - Current Session Context

## Current Milestone & Group
- **Milestone**: Phase 5 / Milestone 5.4 - Payment & completion service
- **Group**: Group 1 (Tasks 5.4.1 to 5.4.4 - completed)

## Status of All Milestone 5.4 Tasks (Current)
- `[x]` Task 5.4.1 - Define IPaymentService (Tender, change, completion contract)
- `[x]` Task 5.4.2 - Record Payment per TenderMethod (Persist tenders cash/card/wallet/split)
- `[x]` Task 5.4.3 - Compute cash change (Change = tendered - due, MoneyRounder-policy)
- `[x]` Task 5.4.4 - Commit order append-only (Atomic order/lines/payments save with SQLite transaction)
- `[ ]` Task 5.4.5 - Enqueue SyncOutbox event
- `[ ]` Task 5.4.6 - Enqueue PrintQueue receipt
- `[ ]` Task 5.4.7 - Render receipt from data
- `[ ]` Task 5.4.8 - Ensure idempotent completion
- `[ ]` Task 5.4.9 - Wire payment_screen.html
- `[ ]` Task 5.4.10 - Unit test tender/change/completion

## Status of All Milestone 5.3 Tasks
- `[x]` Task 5.3.1 - Define IOrderService
- `[x]` Task 5.3.2 - Decide draft persistence
- `[x]` Task 5.3.3 - Implement add/qty/remove
- `[x]` Task 5.3.4 - Implement discount handling
- `[x]` Task 5.3.5 - Implement totals calculation
- `[x]` Task 5.3.6 - Implement tax via TaxRule
- `[x]` Task 5.3.7 - Centralize money rounding
- `[x]` Task 5.3.8 - Add cart bridge handlers
- `[x]` Task 5.3.9 - Wire main_checkout cart + remove pos_cart
- `[x]` Task 5.3.10 - Unit test cart math + tax / final cart math, bridge, UI, and regression verification

## Status of All Milestone 5.2 Tasks
- `[x]` Task 5.2.1 - LocalShift entity + EF migration
- `[x]` Task 5.2.2 - IShiftService + ShiftService (OpenShiftAsync)
- `[x]` Task 5.2.3 - shift.open bridge handler + tests
- `[x]` Task 5.2.4 - shift_open.html float input + validation UI
- `[x]` Task 5.2.5 - shift_open.html bridge wire-up
- `[x]` Task 5.2.6 - shift.getCurrent bridge handler + ShiftDetailsResult
- `[x]` Task 5.2.7 - Gate all operational screens on open shift (shift.getCurrent)
- `[x]` Task 5.2.8 - Source checklist/policy from config (config-driven limits + checklist via `shift.getOpenPolicy` bridge endpoint)
- `[x]` Task 5.2.9 - Navigate to checkout on open (success overlay + 1600ms redirect to `main_checkout.html` confirmed and preserved)
- `[x]` Task 5.2.10 - End-to-end verification: full builds, full test suite, search checks, SHA-256 sync checks, bug fix for stale docs copy

## Files Created/Changed in this Milestone

### Group 1 (Tasks 5.4.1 to 5.4.4 - completed)
- [ADD] `POS.Desktop/Services/Payments/IPaymentService.cs` (Defines core tender, change, completion contract)
- [ADD] `POS.Desktop/Services/Payments/PaymentTenderRequest.cs` (Tender item in request)
- [ADD] `POS.Desktop/Services/Payments/PaymentCompletionRequest.cs` (Payload required to complete order)
- [ADD] `POS.Desktop/Services/Payments/PaymentCompletionResult.cs` (Result containing status, change, receipt)
- [ADD] `POS.Desktop/Services/Payments/PaymentValidationException.cs` (Custom payment validation exception)
- [ADD] `POS.Desktop/Services/Payments/PaymentService.cs` (Validates session/shift/tenders and commits atomically)
- [ADD] `POS.Desktop/Data/LocalEntities/LocalOrder.cs` (Local order SQLite db entity)
- [ADD] `POS.Desktop/Data/LocalEntities/LocalOrderLine.cs` (Local order line SQLite db entity)
- [ADD] `POS.Desktop/Data/LocalEntities/LocalPayment.cs` (Local payment SQLite db entity)
- [ADD] `POS.Desktop/Data/Configurations/Local/LocalOrderConfiguration.cs` (Local order EF mapping & database check constraints)
- [ADD] `POS.Desktop/Data/Configurations/Local/LocalOrderLineConfiguration.cs` (Local order line EF mapping & check constraints)
- [ADD] `POS.Desktop/Data/Configurations/Local/LocalPaymentConfiguration.cs` (Local payment EF mapping & check constraints)
- [ADD] `POS.Desktop/Data/Migrations/Local/20260528094204_AddLocalOrderPaymentTables.cs` (EF Core SQLite schema migration)
- [ADD] `POS.Desktop/Data/Migrations/Local/20260528094204_AddLocalOrderPaymentTables.Designer.cs` (EF migration designer)
- [MODIFY] `POS.Desktop/Data/PosLocalDbContext.cs` (Added DbSet registers and global query filters for new entities)
- [MODIFY] `POS.Desktop/Configuration/DesktopHostBuilder.cs` (Registered IPaymentService in dependency container)
- [ADD] `POS.Desktop.Tests/Services/Payments/PaymentServiceTests.cs` (Self-contained test suite covering cash/card/wallet/split payments, change calculations, error conditions, SQLite transaction rollback, and cart clearing)
- [MODIFY] `docs/antigravity-context/POS_DESKTOP_CURRENT_CONTEXT.md` (Updated context file to record Milestone 5.4 Group 1 status)

### Group 5 (Task 5.3.10 - completed)
- [MODIFY] `POS.Desktop.Tests/Services/Orders/OrderServiceTests.cs` (Added mixed rates exclusive tax with discount, tax included item with fixed discount, zero/null tax rate items, percentage discount consistent rounding, and equations verification tests)
- [MODIFY] `POS.Desktop.Tests/Shell/OrderBridgeHandlerTests.cs` (Added generic non-validation exceptions safe mapping verification test)
- [MODIFY] `docs/antigravity-context/POS_DESKTOP_CURRENT_CONTEXT.md` (Updated context to reflect final milestone verification state)

### Group 4 (Tasks 5.3.8 and 5.3.9 - completed)
- [ADD] `POS.Desktop.Tests/Shell/OrderBridgeHandlerTests.cs`
- [MODIFY] `POS.Desktop/Shell/PosWebMessageRouter.cs`
- [MODIFY] `POS.Desktop.Tests/Shell/PosWebMessageRouterTests.cs`
- [MODIFY] `POS.Desktop/Assets/ui/main_checkout.html`
- [MODIFY] `docs/ui-prototype/screens/main_checkout.html`

### Group 3 (Tasks 5.3.5, 5.3.6, and 5.3.7 - completed)
- [ADD] `POS.Desktop/Services/Orders/MoneyRounder.cs`
- [MODIFY] `POS.Desktop/Services/Orders/CartLineDto.cs`
- [MODIFY] `POS.Desktop/Services/Orders/OrderService.cs`
- [MODIFY] `POS.Desktop.Tests/Services/Orders/OrderServiceTests.cs`

### Group 2 (Tasks 5.3.3 and 5.3.4 - completed)
- [ADD] `POS.Desktop/Services/Orders/IDraftCartStore.cs`
- [ADD] `POS.Desktop/Services/Orders/DraftCartStore.cs`
- [ADD] `POS.Desktop/Services/Orders/OrderService.cs`
- [ADD] `POS.Desktop/Services/Orders/OrderValidationException.cs`
- [ADD] `POS.Desktop.Tests/Services/Orders/OrderServiceTests.cs`
- [MODIFY] `POS.Desktop/Services/Catalog/ICatalogService.cs`
- [MODIFY] `POS.Desktop/Services/Catalog/CatalogService.cs`
- [MODIFY] `POS.Desktop/Configuration/DesktopHostBuilder.cs`

### Group 1 (Tasks 5.3.1 and 5.3.2 - committed/pushed)
- [ADD] `POS.Desktop/Services/Orders/IOrderService.cs`
- [ADD] `POS.Desktop/Services/Orders/CartStateDto.cs`
- [ADD] `POS.Desktop/Services/Orders/CartLineDto.cs`

### Group 5 (Task 5.2.10 - committed)
- [SYNC-FIX] `docs/ui-prototype/screens/main_checkout.html` - stale copy (hardcoded ITEMS[]/CATEGORIES[]) synced from authoritative Assets version

### Group 4 (Tasks 5.2.8, 5.2.9 - committed: `ad24f24`)
- [ADD] `POS.Desktop/Services/Shifts/ShiftOpenPolicyOptions.cs`
- [ADD] `POS.Desktop/Services/Shifts/ShiftOpenPolicyResult.cs`
- [MODIFY] `POS.Desktop/Services/Shifts/IShiftService.cs`
- [MODIFY] `POS.Desktop/Services/Shifts/ShiftService.cs`
- [MODIFY] `POS.Desktop/Configuration/DesktopHostBuilder.cs`
- [MODIFY] `POS.Desktop/Shell/PosWebMessageRouter.cs`
- [MODIFY] `POS.Desktop/appsettings.json`
- [MODIFY] `POS.Desktop/Assets/ui/shift_open.html`
- [MODIFY] `docs/ui-prototype/screens/shift_open.html`
- [MODIFY] `POS.Desktop.Tests/Services/Shifts/ShiftServiceTests.cs`
- [MODIFY] `POS.Desktop.Tests/Shell/ShiftBridgeHandlerTests.cs`
- [MODIFY] `POS.Desktop.Tests/Shell/PosWebMessageRouterTests.cs`

### Prior Completed Groups & Milestones
- Group 3 (Tasks 5.2.6 - 5.2.7) - Completed & committed:
  - [ADD] `POS.Desktop/Services/Shifts/ShiftDetailsResult.cs`
  - [MODIFY] `POS.Desktop/Services/Shifts/IShiftService.cs`
  - [MODIFY] `POS.Desktop/Services/Shifts/ShiftService.cs`
  - [MODIFY] `POS.Desktop/Shell/PosWebMessageRouter.cs`
  - [MODIFY] `POS.Desktop/Assets/ui/main_checkout.html`
  - [MODIFY] `docs/ui-prototype/screens/main_checkout.html`
  - [MODIFY] `POS.Desktop/Assets/ui/payment_screen.html`
  - [MODIFY] `docs/ui-prototype/screens/payment_screen.html`
  - [MODIFY] `POS.Desktop/Assets/ui/cash_control.html`
  - [MODIFY] `docs/ui-prototype/screens/cash_control.html`
  - [MODIFY] `POS.Desktop/Assets/ui/shift_close.html`
  - [MODIFY] `docs/ui-prototype/screens/shift_close.html`
  - [MODIFY] `POS.Desktop.Tests/Services/Shifts/ShiftServiceTests.cs`
  - [MODIFY] `POS.Desktop.Tests/Shell/ShiftBridgeHandlerTests.cs`
  - [MODIFY] `POS.Desktop.Tests/Shell/PosWebMessageRouterTests.cs`
- Group 2 (Tasks 5.2.4 - 5.2.5) - Completed & committed:
  - [MODIFY] `POS.Desktop/Assets/ui/shift_open.html`
  - [MODIFY] `docs/ui-prototype/screens/shift_open.html`
- Group 1 (Tasks 5.2.1 - 5.2.3) - Completed & committed:
  - [ADD] `POS.Desktop/Services/Shifts/IShiftService.cs`
  - [ADD] `POS.Desktop/Services/Shifts/ShiftOpenResult.cs`
  - [ADD] `POS.Desktop/Services/Shifts/ShiftService.cs`
  - [ADD] `POS.Desktop/Data/LocalEntities/LocalShift.cs`
  - [ADD] `POS.Desktop/Data/Configurations/Local/LocalShiftConfiguration.cs`
  - [ADD] `POS.Desktop/Data/Migrations/Local/20260528025043_AddLocalShiftsTable.cs`
  - [ADD] `POS.Desktop/Data/Migrations/Local/20260528025043_AddLocalShiftsTable.Designer.cs`
  - [MODIFY] `POS.Desktop/Data/Migrations/Local/PosLocalDbContextModelSnapshot.cs`
  - [MODIFY] `POS.Desktop/Data/PosLocalDbContext.cs`
  - [MODIFY] `POS.Desktop/Configuration/DesktopHostBuilder.cs`
  - [MODIFY] `POS.Desktop/Shell/PosWebMessageRouter.cs`
  - [ADD] `POS.Desktop.Tests/Services/Shifts/ShiftServiceTests.cs`
  - [ADD] `POS.Desktop.Tests/Shell/ShiftBridgeHandlerTests.cs`
  - [MODIFY] `POS.Desktop.Tests/Shell/PosWebMessageRouterTests.cs`
- Milestone 5.1 - Authentication & login service (Committed & Pushed)

## Scope Boundaries & Constraints
- Do NOT use localStorage or sessionStorage for operational screen gating.
- Preserve original element class/ID names in HTML/JS. No UI/CSS redesign.
- Do NOT modify POS.Api or central API migrations.
- Do NOT commit or push.

## Config-Driven Policy Behaviour (Task 5.2.8)

### Configuration
A `"ShiftOpen"` section was added to `appsettings.json`:
```json
"ShiftOpen": {
  "CashDrawerLimit": 25000,
  "AutoSafeDropThreshold": 20000,
  "Checklist": [ ... 5 items ... ]
}
```

### Typed Options
`POS.Desktop/Services/Shifts/ShiftOpenPolicyOptions.cs` - constants `DefaultCashDrawerLimit`, `DefaultAutoSafeDropThreshold`, `MaxChecklistItems` (10), and `DefaultChecklist()` ensure a single authoritative source for defaults used by both the service and tests.

### Bridge Endpoint
`shift.getOpenPolicy` registered in `PosWebMessageRouter`. Requires no active session or open shift. Returns:
```json
{ "cashDrawerLimit": 25000, "autoSafeDropThreshold": 20000, "checklist": [...] }
```
Never exposes raw exception details. Logs internally only.

### Sanitization Rules (in ShiftService.GetOpenPolicyAsync)
- `CashDrawerLimit <= 0` → replaced with `DefaultCashDrawerLimit`
- `AutoSafeDropThreshold <= 0` → replaced with `DefaultAutoSafeDropThreshold`
- Null/whitespace checklist items removed; values trimmed
- List capped at `MaxChecklistItems` (10)
- After filtering, empty checklist → `DefaultChecklist()`

### UI Rendering (shift_open.html)
- `id="cash-limit-text"` and `id="safe-drop-threshold-text"` are dynamic placeholders
- `id="shift-checklist"` container is populated at runtime from bridge response
- `Intl.NumberFormat('en-PK')` used for PKR formatting
- `escapeHtml()` helper escapes `&`, `<`, `>`, `"`, `'` before inserting checklist items
- Policy fetch is **non-blocking**: failure logs to `console.error` and renders safe UI fallback defaults matching `ShiftOpenPolicyOptions` defaults; `shift.open` is never blocked by a policy display failure

## Checkout Navigation Confirmation (Task 5.2.9)
The `openShift()` function in `shift_open.html` already had the correct flow from Group 2. Confirmed and preserved:
1. `shift.open` bridge call succeeds
2. `.success-overlay.open` class applied → overlay fades in
3. Progress bar animates to 100% after 50ms
4. `window.location.href = 'main_checkout.html'` fires after 1600ms
No changes were required to this flow.

## Important Decisions & Gate Behaviour
- **Singleton DraftCartStore & Scoped OrderService:** To manage cart state across multiple bridge requests within transient message scopes, the cart's backing store (`DraftCartStore`) is registered as a thread-safe process-lifetime `Singleton`. The business logic layer (`OrderService`) is registered as `Scoped` so that it can inject scoped services (like `ICatalogService` and database contexts) safely without creating captive dependencies.
- **Database Gated Authority:** All operational screens (`main_checkout.html`, `payment_screen.html`, `cash_control.html`, `shift_close.html`) now asynchronously request the `"shift.getCurrent"` bridge endpoint on `DOMContentLoaded`. If the SQLite database does not record an open active shift (`isOpen: false`), they show a user-friendly error toast (`Please open your shift first.`) and redirect to `shift_open.html` after a `1.5-second` delay.
- **Fail Safe / Locked Terminal:** If the bridge transport is unavailable, terminal session context is invalid, or the terminal is unprovisioned, the screens fail closed/locked and redirect immediately to `shift_open.html` without exposing internal exception details.
- **Consistent Bridge Contracts:** Leveraged the `"shift.getCurrent"` message type across all operational flows, returning structured success payloads of type `ShiftDetailsResult`.
- **Strict Location Isolation Gating:** Both `OpenShiftAsync` and `GetCurrentShiftAsync` filter open shifts and sequences strictly by location and terminal identifier (`LocationId == CurrentLocationId` and `TerminalId == CurrentTerminalId`), ensuring shifts opened at different locations do not bleed through.
- **Identical Copies:** Kept `POS.Desktop/Assets/ui/*.html` and `docs/ui-prototype/screens/*.html` identically synchronized. All 5 milestone-touched screens SHA-256 verified identical after Group 5 sync fix.
- **MoneyRounder Policy:** Money calculations use decimal math only. Values are rounded to 2 decimal places using `MidpointRounding.AwayFromZero` commercial rounding, centralized in the helper class `POS.Desktop/Services/Orders/MoneyRounder.cs`.
- **Tax Calculation Rules:**
  - **Tax-Exclusive Prices:**
    - `taxableBase = grossAmount - lineDiscount`
    - `taxAmount = taxableBase * taxRate / 100` (rounded)
    - `netAmount = taxableBase + taxAmount` (rounded)
  - **Tax-Inclusive Prices:**
    - `taxableBase = grossAmount - lineDiscount`
    - `taxAmount = taxableBase - (taxableBase / (1 + taxRate / 100))` (rounded)
    - `netAmount = taxableBase`
  - **Null/Zero/Negative Tax Rates:**
    - `taxAmount = 0`
    - `netAmount = taxableBase`
- **Proportional Discount Distribution:** Cart-level discounts are distributed proportionally across cart lines before tax based on each line's share of the total gross subtotal. To ensure the sum of line discounts exactly equals the cart-level discount, the last line absorbs any rounding remainder.
- **Deferred Temporary Hold Behavior:** The `holdActiveOrder` function saves the current cart state snapshot to `sessionStorage` under the key `pos_held_order` as a temporary deferred feature. After holding, it calls the `order.clearCart` bridge endpoint to clear the active C# cart. Note that `pos_held_order` is not the active cart source of truth, and the full hold/resume flow will be revisited later.
- **SQLite Atomic Transactions:** Uses DbContext Transaction to save order, order lines, and payments as an atomic unit, ensuring no partial records can ever be committed.
- **Tender overpayment rules**: Prevents non-cash overpayment change drift. Correctly rejects non-cash overpayment unless cash is present to absorb change.

## Verification Summary (Milestone 5.4 Group 1)

### Builds
- `dotnet build POS.Desktop/POS.Desktop.csproj --configuration Debug`: **0 errors / 0 warnings**
- `dotnet build POS.slnx --configuration Debug`: **0 errors / 0 warnings**

### Tests
- `dotnet test POS.Desktop.Tests`: **316/316 passed** (all 305 existing + 11 new comprehensive integration and transaction tests passed successfully)
- `dotnet test POS.Tests`: **49/49 passed** (all 49 central API/core tests passed successfully)

### Git hygiene
- `git diff --check`: Zero whitespace/layout errors
- `git status`: Only context update + new/modified test files before commit (no untracked files or payment screen edits)

### SHA-256 sync check (all 5 milestone screens)
| File | Assets hash | Result |
|---|---|---|
| `shift_open.html` | `84F0198FA66D...` | IDENTICAL |
| `main_checkout.html` | `A831FA77E0D6...` | IDENTICAL (authoritative main_checkout copies SHA-256 matches perfectly: `A831FA77E0D6...`) |
| `payment_screen.html` | `61B638BB1561...` | IDENTICAL (strictly unedited/deferred) |
| `cash_control.html` | `D1ED98B1271D...` | IDENTICAL |
| `shift_close.html` | `49E73F7062E5...` | IDENTICAL |
| **All files synchronized** | | **True** |

### Bug found and fixed in Group 5
- **`docs/ui-prototype/screens/main_checkout.html` was stale** - still contained hardcoded `ITEMS[]` / `CATEGORIES[]` arrays from before Group 3. The `POS.Desktop/Assets/ui/main_checkout.html` (authoritative) already had bridge-backed catalog loading (`catalogItems` / `catalogCategories`). Fixed by syncing the docs copy from Assets. SHA-256 verified identical after fix.

### Runtime GUI verification
Runtime smoke test through the WebView2 host requires launching the desktop application, which cannot be performed from the CLI session. Code review, bridge contract tests, and all automated tests confirm correct behavior.

## Search Checks (all operational screens)

### shift_open.html (both copies)
| Check | Result |
|---|---|
| `PKR 25,000` hardcoded as authoritative text | ✗ Not present |
| `PKR 20,000` hardcoded as authoritative text | ✗ Not present |
| `shift.getOpenPolicy` call present | ✓ Present |
| `shift.open` call preserved | ✓ Present |
| `id="cash-limit-text"` placeholder | ✓ Present |
| `id="safe-drop-threshold-text"` placeholder | ✓ Present |
| `id="shift-checklist"` container | ✓ Present |
| `success-overlay` preserved | ✓ Present |
| `main_checkout.html` redirect preserved | ✓ Present |

### All 4 operational gate screens (main_checkout, payment_screen, cash_control, shift_close)
| Check | Result |
|---|---|
| `shift.getCurrent` gate call present on all 4 | ✓ Present |
| `isOpen` check used for gate logic | ✓ Present |
| `shift_open.html` redirect on gate failure | ✓ Present |
| `localStorage` / `sessionStorage` used for gating | ✗ Not present |
| Raw exception text shown to cashier | ✗ Not present |

### Deferred items
- `SyncOutbox` event enqueueing is deferred to Group 2 / Task 5.4.5.
- `PrintQueue` receipt job enqueueing is deferred to Group 2 / Task 5.4.6.
- Receipt template rendering from committed order data is deferred to Group 2 / Task 5.4.7.
- payment_screen.html UI bridge routing and integration is deferred to Group 4 / Task 5.4.9.
- `pos_shift_float` / `pos_shift_open` references in `cash_control.html` and `shift_close.html` are demo metric artifacts inside `updateMetrics()` / `loadMetrics()` / `executeShiftClose()` - NOT access gates. Deferred to Milestones 5.5 and 5.6.

## Next Recommended Group
- **Milestone 5.4 Group 2 (Tasks 5.4.5 to 5.4.7 - outbox event, print queue, receipt render)**
