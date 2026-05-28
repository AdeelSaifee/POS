# POS Desktop UI Integration - Current Session Context

## Current Milestone & Group
- **Milestone**: Phase 5 / Milestone 5.2 - Shift open service
- **Group**: Group 1 (Tasks 5.2.1, 5.2.2, 5.2.3) - Completed

## Status of Tasks in this Session
- `[x]` Task 5.2.1 - Define IShiftService.OpenShift
- `[x]` Task 5.2.2 - Implement OpenShift
- `[x]` Task 5.2.3 - Add openShift bridge handler

## Files Created/Changed in this Session

### Group 1 (Current uncommitted changes)
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

### Prior Completed Groups & Milestones
- Milestone 5.1 - Authentication & login service (Committed & Pushed - HEAD: `acc7c5ae` + Group 4 changes uncommitted)
  - [ADD] `POS.Desktop.Tests/Configuration/DesktopHostBuilderTests.cs`
  - [MODIFY] `POS.Desktop.Tests/Services/Auth/AuthValidatePinTests.cs`
  - [MODIFY] `POS.Desktop.Tests/Services/Auth/LocalEmployeeAuthServiceTests.cs`
- Milestone 4.5 - Data-access conventions & tenant-filter validation (Committed)
- Milestone 4.4 - Local catalog search & scan bridge integration (Committed)

## Scope Boundaries & Constraints
- Do NOT modify any UI files in this group (`shift_open.html`, sessionStorage).
- Do NOT modify POS.Api or central API migrations.
- Do NOT commit or push.
- Keep double-open prevention at the service level only (no full screen gate yet).
- Reject openingFloat <= 0.

## Important Decisions
- **SessionId to EmployeeId Resolution:** In memory, `OperatorSession` does not contain `EmployeeId` (only `OperatorId`). We safely parse `CurrentSession.SessionId` to an integer (representing the SQLite DB session PK) and query `LocalTerminalSessions` to resolve `EmployeeId` securely, preventing any session injection.
- **Strict Location & Terminal Boundaries:** Verified that active session parameters in SQLite align exactly with the current `IProvisionedTerminalContext` tenant, location, and terminal identifiers before permitting a shift to open.
- **Idempotency & Correlation IDs:** Generated unique non-empty string GUIDs for `IdempotencyKey` and `CorrelationId` to ensure local offline operation auditability.
- **Consistent Float Validation:** Enforced strictly positive opening cash floats (`OpeningCashAmount > 0` DB constraint and `openingFloat <= 0` service rejection).


## Auth Coverage Matrix
| Path Type | Description | Test Case / Verification | Status |
|---|---|---|---|
| **Valid** | 1. Valid employee PIN returns success | `ValidatePinAsync_Succeeds_WithValidEmployeeAndRole` | Covered |
| | 2. Valid employee PIN creates LocalTerminalSession | `ValidatePinAsync_PersistsSession_AndIncrementsSequence_OnSuccess` | Covered |
| | 3. Valid employee PIN sets ISessionService through router | `ValidatePin_ValidCredentials_ReturnsIsValidTrue_AndStartsSession` | Covered |
| | 4. Valid manager PIN succeeds for manager/supervisor roles | `ValidateManagerPinAsync_Succeeds_ForAuthorizedRoles` (covers Manager, Supervisor, manager, supervisor) | Covered |
| | 5. Exact location role is preferred over global role | `ValidatePinAsync_PrefersExactLocationRole_OverGlobalRole` | Covered |
| | 6. Auth result/session response contains no credentials | Verified structurally in all contract returned models | Covered |
| **Invalid** | 1. Wrong PIN fails | `ValidatePinAsync_Fails_WithWrongPin` | Covered |
| | 2. Unknown operator fails | `ValidatePinAsync_Fails_WithUnknownOperator` | Covered |
| | 3. Inactive operator fails | `ValidatePinAsync_Fails_WithInactiveOperator` | Covered |
| | 4. Employee with missing PinHash fails | `ValidatePinAsync_FailsClosed_WithMissingPinHashOrSaltOrAlgorithm` | Covered |
| | 5. Employee with missing PinSalt fails | `ValidatePinAsync_FailsClosed_WithMissingPinHashOrSaltOrAlgorithm` | Covered |
| | 6. Employee with missing PinHashAlgorithm fails | `ValidatePinAsync_FailsClosed_WithMissingPinHashOrSaltOrAlgorithm` | Covered |
| | 7. Empty LocalEmployees table fails closed | `ValidatePinAsync_FailsClosed_WithEmptyLocalEmployeesTable` | Covered |
| | 8. No active location role fails | `ValidatePinAsync_FailsClosed_WithExpiredEndsOnRole` / `ValidatePinAsync_Fails_WithWrongLocation` | Covered |
| | 9. Future StartsOn role fails | `ValidatePinAsync_FailsClosed_WithFutureStartsOnRole` | Covered |
| | 10. Expired EndsOn role fails | `ValidatePinAsync_FailsClosed_WithExpiredEndsOnRole` | Covered |
| | 11. Wrong tenant/location fails | `ValidatePinAsync_FailsClosed_WithWrongTenant` / `ValidateManagerPinAsync_FailsClosed_WithWrongTenant` / `ValidatePinAsync_Fails_WithWrongLocation` | Covered |
| | 12. Unprovisioned terminal fails closed | `ValidatePinAsync_FailsClosed_WhenTerminalUnprovisioned` | Covered |
| | 13. Malformed/missing bridge payload returns MALFORMED_REQUEST | `ValidatePin_MalformedPayload_ReturnsStructuredErrorSafely` | Covered |
| | 14. Missing session id returns SESSION_NOT_CREATED error | `ValidatePin_MissingSessionId_ReturnsSessionNotCreatedError` | Covered |
| | 15. Failed auth does not create LocalTerminalSession | `ValidatePinAsync_DoesNotPersistSession_OnFailure` | Covered |
| | 16. Failed auth does not mutate ISessionService | `ValidatePin_VariousFailures_ReturnsGenericFailureWithoutDetails` | Covered |
| | 17. StubAuthService is no longer default IAuthService | `CreateHostBuilder_RegistersLocalEmployeeAuthService_AsIAuthService` | Covered |
| | 18. No credentials/raw payloads leaked in logs | `ValidatePinAsync_DoesNotLogSensitiveData_OnSuccessAndFailure` / `Router_DoesNotLogSensitiveDataOrRawPayloads` | Covered |

## Verification Summary (Milestone 5.2 Group 1)
- `git status --short --untracked-files=all`: Checked and verified only expected files are changed/created.
- `dotnet build POS.Desktop/POS.Desktop.csproj --configuration Debug`: Built successfully (0 errors, 0 warnings).
- `dotnet build POS.slnx --configuration Debug`: Built successfully (0 errors, 0 warnings).
- `dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug`: Passed successfully (234/234 passed).
- `dotnet test POS.Tests/POS.Tests.csproj --configuration Debug`: Passed successfully (49/49 passed).
- `git diff --check`: Verified zero formatting or whitespace issues.

## Next Recommended Group
- **Group 2**: Tasks 5.2.4 to 5.2.5, wire `shift_open.html` to bridge and remove `pos_shift_*` sessionStorage.
