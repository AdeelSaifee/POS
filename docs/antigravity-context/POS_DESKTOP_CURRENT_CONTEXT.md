# POS Desktop UI Integration - Current Session Context

## Current Milestone & Group
- **Milestone**: Phase 5 / Milestone 5.2 - Shift open service
- **Group**: Group 4 (Tasks 5.2.8, 5.2.9) - Completed

## Status of Tasks in this Session
- `[x]` Task 5.2.8 - Source checklist/policy from config (config-driven limits + checklist via `shift.getOpenPolicy` bridge endpoint)
- `[x]` Task 5.2.9 - Navigate to checkout on open (success overlay + 1600ms redirect to `main_checkout.html` confirmed and preserved)

## Files Created/Changed in this Session

### Group 4 (Current uncommitted changes)
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
- Group 3 (Tasks 5.2.6 - 5.2.7) - Completed:
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
- Group 2 (Tasks 5.2.4 - 5.2.5) - Completed:
  - [MODIFY] `POS.Desktop/Assets/ui/shift_open.html`
  - [MODIFY] `docs/ui-prototype/screens/shift_open.html`
- Group 1 (Tasks 5.2.1 - 5.2.3) - Completed:
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
`POS.Desktop/Services/Shifts/ShiftOpenPolicyOptions.cs` — constants `DefaultCashDrawerLimit`, `DefaultAutoSafeDropThreshold`, `MaxChecklistItems` (10), and `DefaultChecklist()` ensure a single authoritative source for defaults used by both the service and tests.

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
- **Database Gated Authority:** All operational screens (`main_checkout.html`, `payment_screen.html`, `cash_control.html`, `shift_close.html`) now asynchronously request the `"shift.getCurrent"` bridge endpoint on `DOMContentLoaded`. If the SQLite database does not record an open active shift (`isOpen: false`), they show a user-friendly error toast (`Please open your shift first.`) and redirect to `shift_open.html` after a `1.5-second` delay.
- **Fail Safe / Locked Terminal:** If the bridge transport is unavailable, terminal session context is invalid, or the terminal is unprovisioned, the screens fail closed/locked and redirect immediately to `shift_open.html` without exposing internal exception details.
- **Consistent Bridge Contracts:** Leveraged the `"shift.getCurrent"` message type across all operational flows, returning structured success payloads of type `ShiftDetailsResult`.
- **Strict Location Isolation Gating:** Both `OpenShiftAsync` and `GetCurrentShiftAsync` filter open shifts and sequences strictly by location and terminal identifier (`LocationId == CurrentLocationId` and `TerminalId == CurrentTerminalId`), ensuring shifts opened at different locations do not bleed through.
- **Identical Copies:** Kept `POS.Desktop/Assets/ui/*.html` and `docs/ui-prototype/screens/*.html` identically synchronized. SHA-256 verified after every edit: `84F0198FA66DA7EE5E2EFE5633BB80D575F3E2ED25FACE581D49F5723E35B632`.

## Verification Summary (Milestone 5.2 Group 4)
- `dotnet build POS.Desktop/POS.Desktop.csproj --configuration Debug`: Built successfully with 0 errors/0 warnings.
- `dotnet build POS.slnx --configuration Debug`: Built successfully with 0 errors/0 warnings.
- `dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug`: **250/250 tests passed** (8 new policy tests + 1 updated router count assertion).
- `dotnet test POS.Tests/POS.Tests.csproj --configuration Debug`: 49/49 tests passed.
- `git diff --check`: Zero whitespace/layout errors.
- `git status --short --untracked-files=all`: 10 modified + 2 new files, exactly as planned.
- SHA-256 sync check: Both `shift_open.html` copies identical (`84F0198F...`).

## Search Checks (both shift_open.html files)
| Check | Result |
|---|---|
| `PKR 25,000` hardcoded as authoritative text | ✗ Not present |
| `PKR 20,000` hardcoded as authoritative text | ✗ Not present |
| `shift.getOpenPolicy` call present | ✓ Present |
| `shift.open` call preserved | ✓ Present |
| `pos_shift_open` present | ✗ Not present |
| `pos_shift_float` present | ✗ Not present |
| `id="cash-limit-text"` placeholder | ✓ Present |
| `id="safe-drop-threshold-text"` placeholder | ✓ Present |
| `id="shift-checklist"` container | ✓ Present |
| `success-overlay` preserved | ✓ Present |
| `main_checkout.html` redirect preserved | ✓ Present |

## Next Recommended Group
- **Group 5**: Task 5.2.10 — End-to-end shift open + gate unlock test (runtime + POS.Tests coverage).
