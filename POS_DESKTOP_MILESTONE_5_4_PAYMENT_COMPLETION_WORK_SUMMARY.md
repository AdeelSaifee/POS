# POS Desktop Milestone 5.4 Work Summary — Payment & Completion Service

**Project:** POS Desktop UI Integration  
**Repository:** `AdeelSaifee/POS`  
**Branch:** `main`  
**Milestone:** Phase 5 / Milestone 5.4 — Payment & completion service  
**Status:** Completed  
**Date:** 2026-05-28  

---

## 0. Simple Step-by-Step Flow

Milestone 5.4 ka main goal yeh tha ke checkout cart ko **real payment completion flow** se connect kiya jaye. Pehle cart frontend/demo style mein tha, ab payment backend service, SQLite persistence, receipt, outbox, print queue, idempotency, aur `payment_screen.html` bridge wiring ke saath complete hai.

```text
1. Cashier cart banata hai main_checkout.html par
2. Cashier payment_screen.html open karta hai
3. payment_screen.html backend se current cart read karta hai via order.getCart
4. payment_screen.html backend se active tender methods read karta hai via payment.getTenderMethods
5. Cash/card/wallet/split tender select hota hai
6. UI ek stable idempotencyKey generate karta hai
7. UI payment.complete bridge call bhejta hai
8. C# PosWebMessageRouter request ko PaymentCompletionRequest mein map karta hai
9. PaymentService validations karta hai:
   - terminal provisioned?
   - active session?
   - open terminal session?
   - open shift?
   - cart non-empty?
   - tender method valid?
   - tender amount enough?
   - cash change allowed?
   - idempotency safe?
10. PaymentService ek SQLite transaction start karta hai
11. Same transaction mein save hota hai:
   - LocalOrder
   - LocalOrderLine(s)
   - LocalPayment(s)
   - SyncOutbox row
   - PrintQueue receipt job
12. ReceiptRenderer backend data se receipt text banata hai
13. Transaction commit hoti hai
14. Draft cart clear hota hai
15. payment_screen.html backend receiptText modal mein show karta hai
```

---

## 1. Milestone 5.4 Tasks Status

| Task | Status | Summary |
|---|---:|---|
| 5.4.1 | Done | `IPaymentService` contract banaya |
| 5.4.2 | Done | Payment per tender method persist kiya |
| 5.4.3 | Done | Cash change calculation add ki |
| 5.4.4 | Done | Order/lines/payments atomic transaction mein commit kiye |
| 5.4.5 | Done | `SyncOutbox` event enqueue kiya |
| 5.4.6 | Done | `PrintQueue` receipt job enqueue kiya |
| 5.4.7 | Done | Data-driven receipt renderer banaya |
| 5.4.8 | Done | Idempotent completion / double-submit safety add ki |
| 5.4.9 | Done | `payment_screen.html` real bridge flow se wire kiya |
| 5.4.10 | Done | Final focused tests + verification complete |

Final recorded test count: **365 desktop tests passing** and **49 POS.Tests passing**.

---

## 2. High-Level Architecture After Milestone 5.4

```text
payment_screen.html
   |
   |-- order.getCart ---------------------------> IOrderService / DraftCartStore
   |
   |-- payment.getTenderMethods ----------------> PosLocalDbContext.LocalTenderMethods
   |
   |-- payment.complete ------------------------> IPaymentService.CompleteOrderAsync
                                                    |
                                                    |-- validate terminal/session/shift/cart/tenders
                                                    |-- enforce idempotency
                                                    |-- compute change
                                                    |-- create LocalOrder/Lines/Payments
                                                    |-- render receipt
                                                    |-- create SyncOutbox
                                                    |-- create PrintQueue
                                                    |-- commit transaction
                                                    |-- clear draft cart
```

**Simple analogy:**

`PaymentService` ek cashier supervisor jaisa hai. UI sirf request bhejti hai, lekin final faisla backend karta hai: payment valid hai ya nahi, kitna change dena hai, receipt kya hogi, aur database mein sale save karni hai ya reject karni hai.

---

# Group 1 — Tasks 5.4.1 to 5.4.4

## 3. Core Payment Service + Local Order/Payment Persistence

### Related files

```text
POS.Desktop/Services/Payments/IPaymentService.cs
POS.Desktop/Services/Payments/PaymentCompletionRequest.cs
POS.Desktop/Services/Payments/PaymentTenderRequest.cs
POS.Desktop/Services/Payments/PaymentCompletionResult.cs
POS.Desktop/Services/Payments/PaymentService.cs
POS.Desktop/Data/LocalEntities/LocalOrder.cs
POS.Desktop/Data/LocalEntities/LocalOrderLine.cs
POS.Desktop/Data/LocalEntities/LocalPayment.cs
POS.Desktop/Data/Configurations/Local/LocalOrderConfiguration.cs
POS.Desktop/Data/Configurations/Local/LocalOrderLineConfiguration.cs
POS.Desktop/Data/Configurations/Local/LocalPaymentConfiguration.cs
POS.Desktop/Data/Migrations/Local/20260528094204_AddLocalOrderPaymentTables.cs
POS.Desktop/Data/Migrations/Local/20260528094204_AddLocalOrderPaymentTables.Designer.cs
POS.Desktop/Data/PosLocalDbContext.cs
POS.Desktop/Configuration/DesktopHostBuilder.cs
POS.Desktop.Tests/Services/Payments/PaymentServiceTests.cs
```

### Yeh kya karta hai?

Group 1 mein actual payment service foundation banaya gaya. Iska kaam hai current draft cart ko paid sale mein convert karna.

```csharp
public interface IPaymentService
{
    Task<PaymentCompletionResult> CompleteOrderAsync(
        PaymentCompletionRequest request,
        CancellationToken cancellationToken = default);
}
```

Payment request ke andar tender lines aati hain:

```csharp
public sealed record PaymentCompletionRequest(
    IReadOnlyList<PaymentTenderRequest> Tenders,
    string? GuestName = null,
    string? GuestPhone = null,
    string? IdempotencyKey = null);

public sealed record PaymentTenderRequest(
    int TenderMethodId,
    decimal Amount,
    string? ExternalPaymentReference = null);
```

### Kyun banaya gaya?

Pehle checkout/payment ka reliable backend completion flow nahi tha. Real POS system mein sale complete karte waqt:

```text
- order row save honi chahiye
- order lines save honi chahiye
- payments save honi chahiye
- cash change calculate hona chahiye
- failed payment cart ko clear nahi karni chahiye
- completed sale half-save nahi honi chahiye
```

### Agar na karte to kya hota?

```text
- UI fake payment show karti rehti
- payment ke baad local SQLite mein sale record na hota
- sync/reporting/receipt future mein impossible hota
- cash/card/split tender proof available na hota
- app restart ke baad sale history missing hoti
```

### Key validations

`PaymentService` completion se pehle yeh checks karta hai:

```text
1. IdempotencyKey present?
2. Terminal provisioned?
3. Active operator session?
4. LocalTerminalSession open?
5. LocalEmployee exists?
6. LocalShift open?
7. Cart empty nahi?
8. Tender list present?
9. Tender amount > 0?
10. Tender methods valid?
11. Total paid >= due?
12. Non-cash overpayment reject?
13. Cash change calculate?
```

Example validation snippet:

```csharp
if (request.Tenders == null || request.Tenders.Count == 0)
{
    return new PaymentCompletionResult(
        Success: false,
        ErrorCode: "NO_TENDERS",
        ErrorMessage: "At least one tender is required.");
}
```

### Cash change calculation

```csharp
decimal totalTendered = MoneyRounder.Round(request.Tenders.Sum(t => t.Amount));
decimal totalDue = cartState.TotalAmount;

decimal changeAmount = 0m;
if (totalTendered > totalDue)
{
    var hasCashTender = request.Tenders.Any(t =>
    {
        var method = dbTenderMethods.First(m => m.Id == t.TenderMethodId);
        return method.AllowsChange;
    });

    if (!hasCashTender)
    {
        return new PaymentCompletionResult(
            Success: false,
            ErrorCode: "OVERPAYMENT_REJECTED",
            ErrorMessage: "Overpayment is not allowed for non-cash payment methods.");
    }

    changeAmount = MoneyRounder.Round(totalTendered - totalDue);
}
```

### Atomic transaction

Sale complete karte waqt order, lines, payments same transaction mein save hote hain.

```csharp
using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
try
{
    _db.LocalOrders.Add(localOrder);
    _db.LocalOrderLines.AddRange(orderLines);
    _db.LocalPayments.AddRange(payments);

    await _db.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
}
catch
{
    await transaction.RollbackAsync(cancellationToken);
    throw;
}
```

### Safe hai ya risky?

**Safe:** transaction use hui, safe error messages use hue, tender methods DB se validate hue.  
**Risk managed:** non-cash overpayment reject hota hai, cash overpayment change calculate karta hai.

### Real POS system mein faida

```text
- Accurate sale record
- Payment audit trail
- Split tender support
- Cashier accountability
- Future sync/reporting/receipt support
```

---

# Group 2 — Tasks 5.4.5 to 5.4.7

## 4. SyncOutbox + PrintQueue + Receipt Renderer

### Related files

```text
POS.Desktop/Services/Receipts/IReceiptRenderer.cs
POS.Desktop/Services/Receipts/ReceiptRenderer.cs
POS.Desktop/Services/Payments/PaymentService.cs
POS.Desktop/Services/Payments/PaymentCompletionResult.cs
POS.Desktop/Data/LocalEntities/SyncOutbox.cs
POS.Desktop/Data/LocalEntities/PrintQueue.cs
POS.Desktop.Tests/Services/Payments/PaymentServiceTests.cs
POS.Desktop/Configuration/DesktopHostBuilder.cs
```

### Yeh kya karta hai?

Payment complete hone ke baad same transaction mein:

```text
1. SyncOutbox event banta hai
2. PrintQueue receipt job banti hai
3. ReceiptRenderer backend data se plain text receipt generate karta hai
```

### Kyun banaya gaya?

Real POS mein payment sirf local save nahi hota. Uske side effects bhi chahiye:

```text
- central server ko sync karna
- receipt print karna
- customer/cashier ko proof dena
```

### SyncOutbox snippet

```csharp
syncOutbox = new SyncOutbox
{
    Id = Guid.NewGuid(),
    TenantId = currentTenantId,
    LocationId = currentLocationId,
    TerminalId = currentTerminalId,
    BusinessDate = businessDate,
    TerminalSequence = nextOrderSequence,
    EventType = "OrderCompleted",
    EventId = orderId,
    PayloadJson = payloadJson,
    PayloadHash = payloadHash,
    IdempotencyKey = $"order-completed:{orderId}",
    CorrelationId = correlationId,
    Status = SyncOutboxStatus.Pending,
    AttemptCount = 0,
    IsActive = true,
    CreatedBy = currentSession.DisplayName,
    CreatedOn = localOrder.CreatedOn
};
```

### PrintQueue snippet

```csharp
printQueue = new PrintQueue
{
    Id = Guid.NewGuid(),
    TenantId = currentTenantId,
    LocationId = currentLocationId,
    TerminalId = currentTerminalId,
    OrderId = orderId,
    PrintJobType = "Receipt",
    ReceiptNumber = receiptNumber,
    PayloadJson = printPayloadJson,
    RenderedContent = receiptText,
    Status = PrintQueueStatus.Pending,
    Priority = 1,
    AttemptCount = 0,
    IdempotencyKey = $"receipt-print:{orderId}",
    CorrelationId = correlationId,
    IsActive = true,
    CreatedBy = currentSession.DisplayName,
    CreatedOn = localOrder.CreatedOn
};
```

### Receipt result fields

```csharp
public sealed record PaymentCompletionResult(
    bool Success,
    Guid? OrderId = null,
    string? ReceiptNumber = null,
    decimal ChangeAmount = 0m,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    string? ReceiptText = null,
    Guid? PrintJobId = null,
    Guid? OutboxEventId = null);
```

### Agar na karte to kya hota?

```text
- sale local DB mein hoti, lekin sync queue missing hoti
- receipt print job missing hoti
- UI ko backend receiptText nahi milta
- future offline sync/reporting pipeline weak hoti
```

### Safe hai ya risky?

**Safe:** outbox + print queue same transaction mein hain. Agar transaction fail hoti hai to partial data nahi bachta.

### Real POS system mein faida

```text
- Offline-first sync possible
- Receipt print queue ready
- Sale completion ka audit event ready
- Future printer integration simple hogi
```

---

# Group 3 — Task 5.4.8

## 5. Idempotent Completion / Double-Charge Protection

### Related files

```text
POS.Desktop/Services/Payments/PaymentService.cs
POS.Desktop/Data/Configurations/Local/LocalOrderConfiguration.cs
POS.Desktop/Data/Migrations/Local/20260528104909_AddLocalOrderIdempotencyKeyIndex.cs
POS.Desktop/Data/Migrations/Local/20260528104909_AddLocalOrderIdempotencyKeyIndex.Designer.cs
POS.Desktop/Data/Migrations/Local/PosLocalDbContextModelSnapshot.cs
POS.Desktop.Tests/Services/Payments/PaymentServiceTests.cs
```

### Yeh kya karta hai?

Agar cashier payment button double-click kare ya UI retry kare, same sale dobara create nahi hoti.

### Rule

```text
Missing IdempotencyKey → reject
Same IdempotencyKey + same payload → existing result return
Same IdempotencyKey + different payload → conflict
Duplicate database insert race → reload existing order safely
```

### Mandatory key snippet

```csharp
if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
{
    return new PaymentCompletionResult(
        Success: false,
        ErrorCode: "IDEMPOTENCY_KEY_REQUIRED",
        ErrorMessage: "An idempotency key is required to complete this order.");
}
```

### Early lookup before empty cart

Important: successful first completion ke baad draft cart clear ho jata hai. Agar retry aaye aur cart empty ho, phir bhi saved sale return honi chahiye.

```csharp
var existingOrder = await _db.LocalOrders
    .AsNoTracking()
    .FirstOrDefaultAsync(o =>
        o.TenantId == currentTenantId &&
        o.IdempotencyKey == request.IdempotencyKey,
        cancellationToken);

var cartState = await _orderService.GetCartStateAsync(cancellationToken);

if (existingOrder != null)
{
    return new PaymentCompletionResult(
        Success: true,
        OrderId: existingOrder.Id,
        ReceiptNumber: existingOrder.ReceiptNumber,
        ChangeAmount: existingOrder.ChangeAmount,
        ReceiptText: printJob?.RenderedContent,
        PrintJobId: printJob?.Id,
        OutboxEventId: outboxEvent?.Id);
}
```

### Payload fingerprint

Same idempotency key ko different cart/tender ke saath reuse karna dangerous hai. Isliye fingerprint store hota hai.

```csharp
var details = new
{
    TenantId = tenantId,
    LocationId = locationId,
    TerminalId = terminalId,
    ShiftId = shiftId,
    BusinessDate = businessDate,
    Lines = cartState.Lines.Select(l => new
    {
        l.ItemId,
        l.VariantId,
        l.Quantity,
        l.UnitPrice,
        l.GrossAmount,
        l.DiscountAmount,
        l.TaxAmount,
        l.NetAmount
    }).OrderBy(l => l.ItemId).ThenBy(l => l.VariantId).ToList(),
    Tenders = request.Tenders.Select(t => new
    {
        t.TenderMethodId,
        Amount = MoneyRounder.Round(t.Amount),
        ExternalPaymentReference = t.ExternalPaymentReference ?? string.Empty
    }).OrderBy(t => t.TenderMethodId)
      .ThenBy(t => t.Amount)
      .ThenBy(t => t.ExternalPaymentReference)
      .ToList()
};
```

### Database unique index

```text
UX_LocalOrders_Tenant_IdempotencyKey
LocalOrders(TenantId, IdempotencyKey)
```

Yeh ensure karta hai ke same tenant ke andar same key se duplicate order row insert na ho.

### Agar na karte to kya hota?

```text
- Double-click se 2 sales ban sakti thin
- Card/wallet duplicate charge risk hota
- PrintQueue duplicate receipt job bana sakti thi
- SyncOutbox duplicate event bhej sakta tha
```

### Real POS faida

```text
- Double charge prevention
- Safe retry behavior
- Network/bridge delay ke bawajood stable result
- Audit-safe payment completion
```

---

# Group 4 — Task 5.4.9

## 6. payment_screen.html Bridge Wiring

### Related files

```text
POS.Desktop/Shell/PosWebMessageRouter.cs
POS.Desktop/Assets/ui/payment_screen.html
docs/ui-prototype/screens/payment_screen.html
POS.Desktop.Tests/Shell/PaymentBridgeHandlerTests.cs
POS.Desktop.Tests/Shell/PaymentScreenStaticTests.cs
POS.Desktop.Tests/Shell/PosWebMessageRouterTests.cs
```

### Yeh kya karta hai?

Payment screen ab fake frontend payment nahi karti. Ab woh backend bridge se:

```text
- cart read karti hai: order.getCart
- tender methods read karti hai: payment.getTenderMethods
- payment complete karti hai: payment.complete
```

### Bridge endpoint — payment.getTenderMethods

```csharp
var methods = await db.LocalTenderMethods
    .OrderBy(t => t.SortOrder)
    .ThenBy(t => t.Name)
    .Select(t => new
    {
        id = t.Id,
        code = t.Code,
        name = t.Name,
        tenderType = t.TenderType,
        allowsChange = t.AllowsChange,
        requiresExternalReference = t.RequiresExternalReference,
        sortOrder = t.SortOrder
    })
    .ToListAsync(cancellationToken);

return BridgeResponseEnvelope.Success(request.Type, request.RequestId, new { methods });
```

### Bridge endpoint — payment.complete

```csharp
var tenders = new List<PaymentTenderRequest>();
foreach (var t in tendersProp.EnumerateArray())
{
    if (!t.TryGetProperty("tenderMethodId", out var tmIdProp) ||
        !tmIdProp.TryGetInt32(out var tenderMethodId))
    {
        return BridgeResponseEnvelope.Failure(
            request.Type,
            request.RequestId,
            "MALFORMED_REQUEST",
            "Each tender must have an integer 'tenderMethodId'.");
    }

    if (!t.TryGetProperty("amount", out var amtProp) ||
        !amtProp.TryGetDecimal(out var amount))
    {
        return BridgeResponseEnvelope.Failure(
            request.Type,
            request.RequestId,
            "MALFORMED_REQUEST",
            "Each tender must have a numeric 'amount'.");
    }

    string? extRef = null;
    if (t.TryGetProperty("externalPaymentReference", out var extRefProp))
        extRef = extRefProp.GetString();

    tenders.Add(new PaymentTenderRequest(tenderMethodId, amount, extRef));
}

var completionRequest = new PaymentCompletionRequest(
    tenders,
    guestName,
    guestPhone,
    idempotencyKey);

var result = await paymentService.CompleteOrderAsync(completionRequest, cancellationToken);
```

### payment_screen.html — backend source of truth

```javascript
async function loadCartFromBridge() {
  const cartState = await bridgeRequest('order.getCart', null);
  renderOrderSummaryFromState(cartState);
  renderQuickAmounts();
  updateChangeDisplay(0, dueAmount);
}
```

### Tender mapping without hardcoded IDs

```javascript
function findTenderByCode(code) {
  return tenderMethods.find(t => t.code === code.toUpperCase()) || null;
}

function getCashTenderMethod() {
  return findTenderByCode('CASH') || tenderMethods.find(t => t.allowsChange) || null;
}

function getCardTenderMethod() {
  return findTenderByCode('CARD') || tenderMethods.find(t => t.requiresExternalReference) || null;
}

function getWalletTenderMethod() {
  return findTenderByCode('WALLET') || getCardTenderMethod();
}
```

### Stable external payment refs

```javascript
function getOrCreateCardRef() {
  if (!currentCardRef) currentCardRef = 'TXN-CARD-' + _genSuffix();
  return currentCardRef;
}

function getOrCreateWalletRef() {
  if (!currentWalletRef) currentWalletRef = 'TXN-WALLET-' + _genSuffix();
  return currentWalletRef;
}
```

### Idempotency key in UI

```javascript
function getOrCreateIdempotencyKey() {
  if (!currentIdempotencyKey) {
    currentIdempotencyKey = crypto.randomUUID
      ? crypto.randomUUID()
      : Date.now().toString(36) + Math.random().toString(36).substring(2);
  }
  return currentIdempotencyKey;
}

function resetIdempotencyKey() {
  currentIdempotencyKey = null;
  currentCardRef = null;
  currentWalletRef = null;
}
```

### Complete transaction flow in UI

```javascript
const completionPayload = {
  tenders,
  idempotencyKey: getOrCreateIdempotencyKey()
};

if (guestPhone) completionPayload.guestPhone = guestPhone;

const payload = await bridgeRequest('payment.complete', completionPayload);

resetIdempotencyKey();

document.getElementById('receipt-text').textContent =
  payload && payload.receiptText ? payload.receiptText : '(No receipt available)';
document.getElementById('receipt-modal').style.display = 'flex';
```

### Fake flow removed

Before 5.4.9, payment screen had fake/demo payment behavior. After 5.4.9:

```text
- no sessionStorage.getItem('pos_cart') as payment source
- no fake setTimeout approval flow for card/wallet
- no hardcoded receipt generation
- no hardcoded tender method IDs
```

### Safe hai ya risky?

**Safe:** UI does not decide final payment. Backend validates everything.  
**Risk managed:** wallet fallback CARD par hai if WALLET tender missing; real wallet integration future mein aayegi.

### Real POS faida

```text
- Payment UI real backend state se connected hai
- Fake/demo cart dependency removed
- Double-click/retry safety UI + backend dono side
- Receipt backend-generated hai
- Card/wallet future hardware integration ke liye ready hooks hain
```

---

# Group 5 — Task 5.4.10

## 7. Final Tests & Verification

### Related files

```text
POS.Desktop.Tests/Shell/PaymentBridgeHandlerTests.cs
POS.Desktop.Tests/Shell/PaymentScreenStaticTests.cs
POS.Desktop.Tests/Shell/PosWebMessageRouterTests.cs
POS.Desktop.Tests/Services/Payments/PaymentServiceTests.cs
docs/antigravity-context/POS_DESKTOP_CURRENT_CONTEXT.md
```

### Yeh kya karta hai?

Group 5 mein final coverage gap review kiya gaya. Sirf missing tests add kiye gaye, production code change nahi kiya.

### Added tests

```text
1. Complete_TenderMissingTenderMethodId_ReturnsMalformedRequest
2. Complete_GuestName_IsMappedToService
3. Complete_MultipleTenders_AllMappedToService
```

### Coverage areas

```text
PaymentServiceTests:
- cash exact payment
- cash overpayment/change
- card payment
- wallet/card style external references
- split tender
- underpaid reject
- invalid tender method reject
- non-cash overpayment reject
- empty cart reject
- unprovisioned/no session/no open shift
- atomic commit
- outbox/print/receipt
- idempotency missing key
- retry after cart cleared
- idempotency conflict
- duplicate key race handling

PaymentBridgeHandlerTests:
- payment.complete registration
- payload mapping
- tender mapping
- guestName / guestPhone mapping
- multiple tenders
- safe validation failure
- safe exception handling
- payment.getTenderMethods safe data

PaymentScreenStaticTests:
- payment_screen uses order.getCart
- uses payment.getTenderMethods
- uses payment.complete
- no sessionStorage.getItem('pos_cart')
- no fake setTimeout approval flow
- backend receiptText used
- Assets and docs copies identical
```

### Final verification commands

```powershell
dotnet build POS.Desktop/POS.Desktop.csproj --configuration Debug
dotnet build POS.slnx --configuration Debug
dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug
dotnet test POS.Tests --configuration Debug
git diff --check
```

### Final result

```text
POS.Desktop.Tests: 365 passed
POS.Tests: 49 passed
Build: 0 errors / 0 warnings
Milestone 5.4: Completed
```

---

## 8. Demonstration Scenarios

### Demo 1 — Exact cash payment

```text
Cart total: PKR 1,500
Tender: Cash PKR 1,500
Backend result:
- LocalOrder saved
- LocalPayment saved
- ChangeAmount = 0
- Receipt generated
- PrintQueue row created
- SyncOutbox row created
```

### Demo 2 — Cash overpayment

```text
Cart total: PKR 1,500
Tender: Cash PKR 2,000
Backend result:
- ChangeAmount = PKR 500
- Payment accepted because cash AllowsChange = true
```

### Demo 3 — Non-cash overpayment rejected

```text
Cart total: PKR 1,500
Tender: Card PKR 2,000
Backend result:
- ErrorCode = OVERPAYMENT_REJECTED
- No LocalOrder saved
- Cart not cleared
```

### Demo 4 — Card payment

```text
UI generates: TXN-CARD-ABC123...
Payload:
{
  "tenders": [
    { "tenderMethodId": 2, "amount": 1500, "externalPaymentReference": "TXN-CARD-ABC123" }
  ],
  "idempotencyKey": "stable-guid"
}

Backend:
- validates tender method
- saves LocalPayment.ExternalPaymentReference
- returns receiptText
```

### Demo 5 — Wallet payment fallback

```text
If WALLET tender exists:
  wallet uses WALLET tender

If WALLET tender missing:
  wallet uses CARD tender as script-only stub

UI still sends:
- TXN-WALLET-...
- guestPhone if user entered phone
```

### Demo 6 — Split tender

```text
Cart total: PKR 5,000
Tender lines:
- Cash PKR 2,000
- Card PKR 3,000 with TXN-CARD-...

Backend:
- validates both tender methods
- paid amount covers due
- saves two LocalPayment rows
```

### Demo 7 — Double click / retry

```text
First request:
- idempotencyKey = K1
- sale saved
- cart cleared

Second request with same K1:
- backend finds existing LocalOrder before cart-empty validation
- returns same OrderId / ReceiptNumber / PrintJobId / OutboxEventId
- no duplicate sale
```

---

## 9. Data Written On Successful Completion

```text
LocalOrder
  - receipt number
  - subtotal/discount/tax/total
  - paid amount
  - change amount
  - idempotency key
  - fingerprint metadata

LocalOrderLine
  - item id / variant id
  - quantity
  - unit price
  - discount
  - tax
  - net amount

LocalPayment
  - tender method id
  - amount
  - external payment reference
  - status paid

SyncOutbox
  - EventType = OrderCompleted
  - payload JSON
  - payload hash
  - status pending

PrintQueue
  - PrintJobType = Receipt
  - RenderedContent = receipt text
  - status pending
```

---

## 10. Important Design Decisions

### Decision 1 — PaymentService is backend authority

UI cannot decide final sale. UI only sends payment request. Backend validates and persists.

### Decision 2 — IdempotencyKey required

Payment completion is money-sensitive. Missing key rejects request.

### Decision 3 — Same transaction for order + side effects

`LocalOrder`, `LocalPayments`, `SyncOutbox`, and `PrintQueue` are saved together. This avoids half-completed sale.

### Decision 4 — Browser storage removed as payment source

Payment screen no longer uses `sessionStorage.getItem('pos_cart')` as source of truth. Cart comes from `order.getCart`.

### Decision 5 — UI tender IDs are not hardcoded

Payment screen asks backend for tender methods and maps by code/fallback properties.

### Decision 6 — Real hardware deferred

Card/wallet are script-only stubs. Real pinpad/hardware integration is deferred to Phase 7.6.

### Decision 7 — Printer wiring deferred

PrintQueue row is created, but actual printer execution is deferred to Phase 7.3.

---

## 11. Files Created / Modified Summary

### New payment service files

```text
POS.Desktop/Services/Payments/IPaymentService.cs
POS.Desktop/Services/Payments/PaymentService.cs
POS.Desktop/Services/Payments/PaymentCompletionRequest.cs
POS.Desktop/Services/Payments/PaymentTenderRequest.cs
POS.Desktop/Services/Payments/PaymentCompletionResult.cs
POS.Desktop/Services/Payments/PaymentValidationException.cs
```

### New local data entities

```text
POS.Desktop/Data/LocalEntities/LocalOrder.cs
POS.Desktop/Data/LocalEntities/LocalOrderLine.cs
POS.Desktop/Data/LocalEntities/LocalPayment.cs
```

### New EF configurations + migrations

```text
POS.Desktop/Data/Configurations/Local/LocalOrderConfiguration.cs
POS.Desktop/Data/Configurations/Local/LocalOrderLineConfiguration.cs
POS.Desktop/Data/Configurations/Local/LocalPaymentConfiguration.cs
POS.Desktop/Data/Migrations/Local/20260528094204_AddLocalOrderPaymentTables.cs
POS.Desktop/Data/Migrations/Local/20260528104909_AddLocalOrderIdempotencyKeyIndex.cs
```

### New receipt files

```text
POS.Desktop/Services/Receipts/IReceiptRenderer.cs
POS.Desktop/Services/Receipts/ReceiptRenderer.cs
```

### Updated bridge/UI files

```text
POS.Desktop/Shell/PosWebMessageRouter.cs
POS.Desktop/Assets/ui/payment_screen.html
docs/ui-prototype/screens/payment_screen.html
```

### New/updated tests

```text
POS.Desktop.Tests/Services/Payments/PaymentServiceTests.cs
POS.Desktop.Tests/Shell/PaymentBridgeHandlerTests.cs
POS.Desktop.Tests/Shell/PaymentScreenStaticTests.cs
POS.Desktop.Tests/Shell/PosWebMessageRouterTests.cs
```

---

## 12. Safe / Risky / Deferred

### Safe

```text
- Atomic transaction protects order/payment/outbox/print consistency
- Idempotency protects double charge
- Backend validates tender amounts and methods
- UI no longer uses fake cart as source of truth
- Tests cover service, bridge, and static UI behavior
```

### Risks still present

```text
- Card/wallet are only script stubs, not real hardware integration
- PrintQueue creates jobs but printer worker is not wired yet
- Wallet currently can fallback to CARD if WALLET tender method does not exist
```

### Deferred items

```text
- Real pinpad/hardware integration → Phase 7.6
- Real printer wiring → Phase 7.3
- Further reporting/Z-report work → future milestone
```

---

## 13. Final Notes for Senior / Reporting

Milestone 5.4 ne POS Desktop ko demo payment se real local payment completion foundation tak upgrade kar diya.

Is milestone ke baad app ke paas:

```text
- real C# payment completion service
- SQLite local sale persistence
- cash/card/wallet/split tender support
- cash change calculation
- outbox sync event
- receipt print queue job
- backend-generated receipt text
- idempotency / double-charge protection
- payment_screen real bridge wiring
- focused automated tests
```

Simple sentence for report:

> “Milestone 5.4 completed real payment completion flow for POS Desktop. The system now converts a draft cart into an append-only local sale with order lines, payments, receipt, print queue, sync outbox, and idempotency protection, while payment_screen.html uses real bridge endpoints instead of fake browser storage/payment simulation.”

