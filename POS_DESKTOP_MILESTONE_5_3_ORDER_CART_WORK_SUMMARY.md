# POS Desktop UI Integration — Milestone 5.3 Work Summary

**Project:** IMAGYN POS Desktop UI Integration  
**Repository:** `AdeelSaifee/POS`  
**Branch:** `main`  
**Main desktop project:** `POS.Desktop`  
**Solution file:** `POS.slnx`  
**Target framework:** `net8.0-windows`  
**UI hosting strategy:** WPF shell + WebView2 + local HTML screens served through `https://pos.app/`  
**Milestone:** `5.3 — Order / cart service`  
**Tasks covered:** `5.3.1` to `5.3.10`  
**Status:** Completed, committed, pushed, and verified  
**Summary style:** Simple Roman Urdu + English mix, practical, step-by-step.

---

## 1. Simple Step-by-Step Flow

Milestone 5.3 ka main goal tha:

```text
Browser sessionStorage.pos_cart
        ↓
C# IOrderService draft cart
        ↓
Singleton DraftCartStore
        ↓
OrderService add/update/remove/discount/tax/totals
        ↓
order.* bridge handlers
        ↓
main_checkout.html bridge-backed cart rendering
        ↓
No sessionStorage.pos_cart source of truth
        ↓
Final tests + verification
```

Final cashier flow:

```text
1. Cashier shift open ke baad main_checkout.html par aata hai
2. UI shift.getCurrent se confirm karti hai ke shift open hai
3. UI order.getCart call karti hai
4. C# current CartStateDto return karta hai
5. Cashier product card tap karta hai
6. JS order.addItem bridge call karta hai
7. PosWebMessageRouter request receive karta hai
8. OrderService item catalog se validate karta hai
9. DraftCartStore cart state update karta hai
10. OrderService totals/tax/discount recalculate karta hai
11. C# full CartStateDto return karta hai
12. JS sirf returned state render karti hai
13. Qty, remove, discount, clear sab order.* bridge se hota hai
14. Browser sessionStorage.pos_cart active source of truth nahi raha
```

---

## 2. Milestone 5.3 Objective

**Milestone 5.3 — Order / cart service**

Purpose:

```text
Checkout cart ko browser sessionStorage aur JavaScript business logic se nikal kar C# service layer mein shift karna.
```

Expected output:

```text
IOrderService
    → in-memory draft cart
    → add / qty / remove / clear / discount operations
    → authoritative totals + tax + rounding
    → order.* bridge endpoints
    → main_checkout.html bridge-backed cart rendering
    → no sessionStorage.pos_cart source of truth
```

Simple meaning:

```text
Pehle cart browser mein chal raha tha.
Ab cart C# ke paas hai.
JS sirf UI render karta hai.
```

---

## 3. What Was Intentionally NOT Done

Milestone 5.3 mein yeh cheezen intentionally nahi ki gayi:

```text
No payment completion
No Order / OrderLine SQLite persistence
No Payment rows
No TenderMethod handling
No receipt generation
No print queue
No sync outbox event
No card / wallet / cash tender logic
No payment_screen.html rewrite
No SQLite draft cart table
No migration
No POS.Api change
No POS.Shared change
No hardware scanner integration
No real manager PIN override for void
No full hold/resume order feature
```

Deferred work:

```text
Milestone 5.4 → Payment & completion service
Milestone 5.5 → Cash control service + manager PIN enforcement
Milestone 5.6 → Shift close / Z-report
Phase 7 → Hardware scanner/printer/pinpad integration
```

---

## 4. Final Files Changed / Added in Milestone 5.3

### Order/cart service files

```text
POS.Desktop/Services/Orders/IOrderService.cs
POS.Desktop/Services/Orders/CartStateDto.cs
POS.Desktop/Services/Orders/CartLineDto.cs
POS.Desktop/Services/Orders/IDraftCartStore.cs
POS.Desktop/Services/Orders/DraftCartStore.cs
POS.Desktop/Services/Orders/OrderService.cs
POS.Desktop/Services/Orders/OrderValidationException.cs
POS.Desktop/Services/Orders/MoneyRounder.cs
```

### Catalog files changed

```text
POS.Desktop/Services/Catalog/ICatalogService.cs
POS.Desktop/Services/Catalog/CatalogService.cs
```

### DI / bridge files changed

```text
POS.Desktop/Configuration/DesktopHostBuilder.cs
POS.Desktop/Shell/PosWebMessageRouter.cs
```

### UI files changed

```text
POS.Desktop/Assets/ui/main_checkout.html
docs/ui-prototype/screens/main_checkout.html
```

### Test files changed / added

```text
POS.Desktop.Tests/Services/Orders/OrderServiceTests.cs
POS.Desktop.Tests/Shell/OrderBridgeHandlerTests.cs
POS.Desktop.Tests/Shell/PosWebMessageRouterTests.cs
```

### Context / documentation

```text
docs/antigravity-context/POS_DESKTOP_CURRENT_CONTEXT.md
```

---

## 5. Final Architecture After Milestone 5.3

```text
main_checkout.html
    ↓ posBridge.request("order.addItem", ...)
PosWebMessageRouter
    ↓ HandleAddItemAsync(...)
IOrderService / OrderService
    ↓ validates catalog item + cart rule
IDraftCartStore / DraftCartStore
    ↓ process-lifetime in-memory state
OrderService.RecalculateTotals(...)
    ↓ MoneyRounder + tax + discount
CartStateDto
    ↓ bridge response payload
main_checkout.html renderCart/updateTotals()
```

### Important design decision

```text
IDraftCartStore = Singleton
IOrderService / OrderService = Scoped
```

Why?

```text
Bridge har request ke liye DI scope bana sakta hai.
Agar cart state scoped service ke andar hoti to har request par reset ho sakti thi.
Singleton DraftCartStore process-lifetime state rakhta hai.
Scoped OrderService future scoped dependencies safely use kar sakta hai.
```

---

# Task-by-Task Summary

---

## 6. Task 5.3.1 — Define `IOrderService`

### Related file

```text
POS.Desktop/Services/Orders/IOrderService.cs
```

### What was done?

`IOrderService` interface banaya gaya jo draft cart/order state ke operations define karta hai.

Important snippet:

```csharp
public interface IOrderService
{
    Task<CartStateDto> GetCartStateAsync(CancellationToken cancellationToken = default);
    Task<CartStateDto> AddItemAsync(int variantId, int quantity = 1, CancellationToken cancellationToken = default);
    Task<CartStateDto> UpdateLineQuantityAsync(int variantId, int quantity, CancellationToken cancellationToken = default);
    Task<CartStateDto> RemoveItemAsync(int variantId, CancellationToken cancellationToken = default);
    Task<CartStateDto> ClearCartAsync(CancellationToken cancellationToken = default);
    Task<CartStateDto> ApplyDiscountAsync(string discountType, decimal discountValue, CancellationToken cancellationToken = default);
    Task<CartStateDto> RemoveDiscountAsync(CancellationToken cancellationToken = default);
}
```

### Yeh kya karta hai?

Yeh contract batata hai ke cart service future mein kaun kaun se actions support karegi:

```text
Get cart
Add item
Update quantity
Remove item
Clear cart
Apply discount
Remove discount
```

### Kyun banaya gaya?

C# ko cart ka source of truth banana tha. Interface se UI/bridge ko ek clear backend-style API mil gayi.

### Agar na karte to kya hota?

Cart logic scattered rehta:

```text
some JS mein
some service mein
some future payment flow mein
```

Is se POS mein money calculation inconsistent ho sakti thi.

### Safe ya risky?

Safe. Sirf contract define hua. No DB write, no payment write, no browser change.

### Real POS system mein faida

Checkout, payment, receipt, Z-report sab same cart service ke state/totals par depend kar sakte hain.

### Status

```text
PASS
```

---

## 7. Task 5.3.2 — Decide Draft Persistence

### Decision

```text
Milestone 5.3 ke liye draft cart in-memory C# store mein rahega.
No SQLite draft cart table.
No JSON draft persistence.
No Order/OrderLine persistence yet.
```

### Related files

```text
POS.Desktop/Services/Orders/IDraftCartStore.cs
POS.Desktop/Services/Orders/DraftCartStore.cs
POS.Desktop/Configuration/DesktopHostBuilder.cs
```

### Important snippet

```csharp
services.AddSingleton<IDraftCartStore, DraftCartStore>();
services.AddScoped<IOrderService, OrderService>();
```

### Yeh kya karta hai?

`DraftCartStore` process lifetime ke liye current draft cart hold karta hai. `OrderService` scoped business logic layer hai.

### Kyun banaya gaya?

Draft cart frequent change hota hai. Har quantity tap, item add, discount update par SQLite row update karna abhi unnecessary complexity hoti.

### Agar na karte to kya hota?

Agar browser cart rakhta:

```text
Cashier browser refresh/state manipulation se cart badal sakta
Tax/discount JS mein unreliable rehte
Payment flow ko C# authoritative total nahi milta
```

Agar SQLite draft table bana dete:

```text
Extra migrations
Extra cleanup logic
Crash recovery design abhi premature
Payment milestone se pehle DB complexity
```

### Safe ya risky?

Safe for current milestone:

```text
In-memory only
Payment not persisted yet
No money transaction committed yet
```

Risk / limitation:

```text
App restart par draft cart lost hoga.
Yeh accepted hai kyun ke 5.3 draft service hai, committed sale nahi.
```

### Real POS system mein faida

Payment milestone mein same C# cart state committed order/payment mein convert ho sakti hai.

### Status

```text
PASS
```

---

## 8. Task 5.3.3 — Implement Add / Qty / Remove

### Related files

```text
POS.Desktop/Services/Orders/OrderService.cs
POS.Desktop/Services/Orders/OrderValidationException.cs
POS.Desktop/Services/Catalog/ICatalogService.cs
POS.Desktop/Services/Catalog/CatalogService.cs
```

### What was done?

`OrderService` implement hua:

```text
AddItemAsync
UpdateLineQuantityAsync
RemoveItemAsync
ClearCartAsync
GetCartStateAsync
```

### Important add item snippet

```csharp
var item = await _catalogService.FindByVariantIdAsync(variantId, cancellationToken);
if (item == null || !item.IsSellable || item.Status != "Active")
{
    throw new OrderValidationException("Item not found or is not sellable.", "ITEM_NOT_SELLABLE");
}
```

### Important line creation snippet

```csharp
var newLine = new CartLineDto
{
    Id = variantId.ToString(),
    ItemId = item.ItemId,
    VariantId = variantId,
    Name = item.ItemName,
    Quantity = quantity,
    UnitPrice = item.UnitPrice,
    GrossAmount = quantity * item.UnitPrice,
    Unit = item.UnitCode,
    CategoryCode = item.CategoryCode ?? string.Empty,
    TaxRuleId = item.TaxRuleId,
    TaxCode = item.TaxCode,
    TaxRate = item.TaxRate,
    IsTaxIncluded = item.IsTaxIncluded
};
```

### Validation rules added

```text
variantId <= 0 → INVALID_VARIANT_ID
quantity <= 0 → INVALID_QUANTITY
quantity > 9999 → EXCESSIVE_QUANTITY
unknown/not sellable item → ITEM_NOT_SELLABLE
update/remove missing item → ITEM_NOT_IN_CART
```

### Yeh kya karta hai?

Cashier jab item add karta hai, service catalog se item validate karti hai, cart line banati hai, aur updated cart state return karti hai.

### Kyun banaya gaya?

Checkout cart real POS business state hai. Yeh JS array mein nahi rehna chahiye.

### Agar na karte to kya hota?

Browser cart manipulate ho sakta tha:

```text
price change
qty tampering
tax bypass
discount wrong
payment total mismatch
```

### Safe ya risky?

Safe. It is still draft cart. No sale committed. Validation errors safe `OrderValidationException` se return hotay hain.

### Real POS system mein faida

Payment, receipt, drawer balance, audit logs later same C# cart data use karenge.

### Status

```text
PASS
```

---

## 9. Task 5.3.4 — Implement Discount Handling

### Related file

```text
POS.Desktop/Services/Orders/OrderService.cs
```

### What was done?

Discount operations added:

```text
ApplyDiscountAsync
RemoveDiscountAsync
```

Supported discount types:

```text
amount → fixed PKR amount discount
pct    → percentage discount
```

### Important snippet

```csharp
if (discountType != "amount" && discountType != "pct")
{
    throw new OrderValidationException(
        "Invalid discount type. Supported types: 'amount' and 'pct'.",
        "INVALID_DISCOUNT_TYPE");
}
```

### Discount validation

```text
Empty cart discount → EMPTY_CART_DISCOUNT
Invalid type → INVALID_DISCOUNT_TYPE
Amount <= 0 → INVALID_DISCOUNT_AMOUNT
Amount > subtotal → INVALID_DISCOUNT_AMOUNT
Percent <= 0 or > 100 → INVALID_DISCOUNT_PERCENT
```

### Yeh kya karta hai?

Cashier fixed ya percentage discount apply kar sakta hai. Service validate karti hai aur updated cart totals return karti hai.

### Kyun banaya gaya?

Discount direct money effect karta hai. Isliye discount calculation JS mein nahi rehni chahiye.

### Agar na karte to kya hota?

JS se koi invalid discount apply ho sakta:

```text
negative discount
100% se zyada discount
subtotal se zyada fixed discount
empty cart par discount
```

### Safe ya risky?

Safe for 5.3 because:

```text
Validation C# mein hai
Discount response safe hai
Payment not committed yet
```

Risk / future:

```text
Manager approval discount threshold future task ho sakta hai.
```

### Real POS system mein faida

Discount audit aur payment calculation consistent rahega.

### Status

```text
PASS
```

---

## 10. Task 5.3.5 — Implement Totals Calculation

### Related files

```text
POS.Desktop/Services/Orders/OrderService.cs
POS.Desktop/Services/Orders/CartStateDto.cs
POS.Desktop/Services/Orders/CartLineDto.cs
```

### What was done?

Authoritative cart totals C# mein calculate hone lage:

```text
SubtotalAmount
DiscountAmount
TaxAmount
TotalAmount
Line GrossAmount
Line DiscountAmount
Line TaxAmount
Line NetAmount
```

### Important snippet

```csharp
return new CartStateDto
{
    SubtotalAmount = subtotalAmount,
    DiscountAmount = cartDiscount,
    TaxAmount = MoneyRounder.Round(totalTaxAmount),
    TotalAmount = MoneyRounder.Round(totalNetAmount),
    Lines = updatedLines,
    DiscountType = discountType,
    DiscountValue = discountValue
};
```

### Yeh kya karta hai?

Cart ka final amount C# calculate karta hai. JS summary sirf display karti hai.

### Kyun banaya gaya?

POS mein total money calculation audit-sensitive hoti hai. Yeh browser rendering layer mein nahi honi chahiye.

### Agar na karte to kya hota?

JS aur future payment service ke totals mismatch ho sakte thay.

Example:

```text
JS total = 103.50
C# payment total = 103.49
Receipt total = 103.50
Audit mismatch
```

### Safe ya risky?

Safe. Totals deterministic decimal math se calculate hotay hain.

### Real POS system mein faida

Future payment completion exact same C# total use karegi.

### Status

```text
PASS
```

---

## 11. Task 5.3.6 — Implement Tax via TaxRule

### Related files

```text
POS.Desktop/Services/Orders/CartLineDto.cs
POS.Desktop/Services/Orders/OrderService.cs
POS.Desktop/Services/Catalog/CatalogItemDto.cs
```

### What was done?

Cart line ab item ke tax fields snapshot karta hai:

```csharp
public int? TaxRuleId { get; init; }
public string? TaxCode { get; init; }
public decimal? TaxRate { get; init; }
public bool IsTaxIncluded { get; init; }
```

### Tax rules supported

```text
No tax / null tax
Tax-exclusive price
Tax-inclusive price
Mixed tax rates in same cart
```

### Tax-exclusive formula

```text
taxableBase = grossAmount - lineDiscount
taxAmount   = taxableBase * taxRate / 100
netAmount   = taxableBase + taxAmount
```

### Tax-inclusive formula

```text
taxableBase = grossAmount - lineDiscount
taxAmount   = taxableBase - (taxableBase / (1 + taxRate / 100))
netAmount   = taxableBase
```

### Important snippet

```csharp
if (taxRate <= 0m)
{
    taxAmount = 0m;
    netAmount = taxableBase;
}
else if (line.IsTaxIncluded)
{
    decimal calculatedTax = taxableBase - (taxableBase / (1m + taxRate / 100m));
    taxAmount = MoneyRounder.Round(calculatedTax);
    netAmount = taxableBase;
}
else
{
    decimal calculatedTax = taxableBase * (taxRate / 100m);
    taxAmount = MoneyRounder.Round(calculatedTax);
    netAmount = MoneyRounder.Round(taxableBase + taxAmount);
}
```

### Yeh kya karta hai?

Har line ka tax us line ke tax rule ke hisaab se calculate hota hai. Agar cart mein multiple tax rates hain, har item apni tax calculation follow karta hai.

### Kyun banaya gaya?

Real POS mein sab products ka tax same nahi hota. Example:

```text
Bread → 0%
Beverage → 5%
Imported/other product → 18%
```

### Agar na karte to kya hota?

Cart-level single tax rate wrong result de sakta tha.

### Safe ya risky?

Safe because tax fields catalog se aate hain aur line snapshot mein store hotay hain.

Risk / future:

```text
Real production tax rules central backend sync se aayenge.
Tax audit/reporting later payment/order persistence mein connect hoga.
```

### Real POS system mein faida

Mixed tax carts ka calculation correct aur audit-friendly ho gaya.

### Status

```text
PASS
```

---

## 12. Task 5.3.7 — Centralize Money Rounding

### Related file

```text
POS.Desktop/Services/Orders/MoneyRounder.cs
```

### What was done?

Central money rounding helper add hua:

```csharp
public static decimal Round(decimal value)
{
    return Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
```

### Yeh kya karta hai?

Money values 2 decimal places par round hoti hain using `MidpointRounding.AwayFromZero`.

### Kyun banaya gaya?

Agar har jagah `Math.Round` different style se use hota to totals drift ho sakte thay.

### Agar na karte to kya hota?

Example:

```text
Line tax rounded one way
Cart total rounded another way
Payment change rounded third way
```

Result: audit mismatch.

### Safe ya risky?

Safe. Helper small, deterministic, local to `POS.Desktop`.

### Real POS system mein faida

Payment, receipt, cash drawer aur reports same rounding policy reuse kar sakte hain.

### Status

```text
PASS
```

---

## 13. Discount + Tax Order of Calculation

Final order:

```text
1. Gross line amount = quantity × unit price
2. Cart subtotal = sum gross line amounts
3. Cart discount = fixed amount or percentage amount
4. Discount distribute across lines proportionally
5. Last line absorbs rounding remainder
6. Taxable base = gross - line discount
7. Tax calculate per line using tax rule
8. Cart tax = sum line tax
9. Cart total = sum line net
```

Example:

```text
Item A gross = PKR 1000, tax 5%
Item B gross = PKR 2000, tax 18%
Cart discount = PKR 300
Discount distribution:
    Item A gets PKR 100 discount
    Item B gets PKR 200 discount
Tax calculation:
    Item A taxable base = 900
    Item B taxable base = 1800
Tax per item uses its own rate
Final total = sum line net amounts
```

Why this matters:

```text
Discount tax se pehle apply hota hai.
Mixed tax rates correct rehte hain.
Line-level tax audit possible hoti hai.
```

---

## 14. Task 5.3.8 — Add Cart Bridge Handlers

### Related file

```text
POS.Desktop/Shell/PosWebMessageRouter.cs
```

### Bridge endpoints added

```text
order.getCart
order.addItem
order.updateLineQuantity
order.removeItem
order.clearCart
order.applyDiscount
order.removeDiscount
```

### Handler registration snippet

```csharp
Register("order.getCart", sp => (req, ct) => HandleGetCartAsync(
    sp.GetRequiredService<IOrderService>(),
    req,
    ct));

Register("order.addItem", sp => (req, ct) => HandleAddItemAsync(
    sp.GetRequiredService<IOrderService>(),
    req,
    ct));
```

### Payload contracts

```text
order.getCart
Payload: null
Response: CartStateDto

order.addItem
Payload: { variantId: number, quantity?: number }
Response: CartStateDto

order.updateLineQuantity
Payload: { variantId: number, quantity: number }
Response: CartStateDto

order.removeItem
Payload: { variantId: number }
Response: CartStateDto

order.clearCart
Payload: null
Response: CartStateDto

order.applyDiscount
Payload: { discountType: string, discountValue: number }
Response: CartStateDto

order.removeDiscount
Payload: null
Response: CartStateDto
```

### Error mapping

```text
Malformed payload → MALFORMED_REQUEST
OrderValidationException → safe ErrorCode + SafeMessage
Other exceptions → router generic HANDLER_ERROR
```

### Important malformed request example

```csharp
if (!payload.TryGetProperty("variantId", out var variantIdProp) ||
    variantIdProp.ValueKind != JsonValueKind.Number ||
    !variantIdProp.TryGetInt32(out var variantId))
{
    return BridgeResponseEnvelope.Failure(
        request.Type,
        request.RequestId,
        "MALFORMED_REQUEST",
        "Parameter 'variantId' is required and must be an integer number.");
}
```

### Yeh kya karta hai?

JS direct cart service call nahi karti. JS bridge message bhejti hai, router payload parse karta hai, `IOrderService` call karta hai, aur updated cart state return karta hai.

### Kyun banaya gaya?

Bridge architecture ka rule hai:

```text
JS = input + rendering
C# = business logic + state
```

### Agar na karte to kya hota?

UI C# service tak cart actions nahi bhej pati. Cart browser mein hi reh jata.

### Safe ya risky?

Safe. Payload strict parse hota hai. Raw internal exception UI ko expose nahi hota.

### Real POS system mein faida

Checkout UI aur C# business layer clean boundary follow karte hain.

### Status

```text
PASS
```

---

## 15. Task 5.3.9 — Wire `main_checkout.html` Cart + Remove `pos_cart`

### Related files

```text
POS.Desktop/Assets/ui/main_checkout.html
docs/ui-prototype/screens/main_checkout.html
```

### What was done?

Checkout UI ab cart ke liye `order.*` bridge endpoints use karti hai.

Removed as source of truth:

```text
sessionStorage.getItem('pos_cart')
sessionStorage.setItem('pos_cart')
saveCartToSession active behavior
```

### Load cart from bridge

```javascript
async function loadCartFromBridge() {
  try {
    const state = await bridgeRequest('order.getCart', null);
    updateLocalCartState(state);
  } catch (e) {
    console.error('[Cart] Failed to load cart:', e);
    showToast('Failed to load cart from service.', 'error');
  }
}
```

### Add item from bridge

```javascript
async function addToCart(item) {
  if (!item) return;
  try {
    const state = await bridgeRequest('order.addItem', {
      variantId: Number(item.variantId),
      quantity: 1
    });
    updateLocalCartState(state);
    renderCart();
    renderGrid();
  } catch (e) {
    showToast(e.message || 'Failed to add item.', 'warning');
  }
}
```

### Update quantity from bridge

```javascript
const state = await bridgeRequest('order.updateLineQuantity', {
  variantId: Number(line.variantId),
  quantity: targetQty
});
updateLocalCartState(state);
```

### Totals render from C# state

```javascript
function updateTotals() {
  const subtotal = latestCartState ? latestCartState.subtotalAmount : 0;
  const discountAmt = latestCartState ? latestCartState.discountAmount : 0;
  const tax = latestCartState ? latestCartState.taxAmount : 0;
  const grand = latestCartState ? latestCartState.totalAmount : 0;

  document.getElementById('summary-subtotal').textContent = fmt(subtotal);
  document.getElementById('summary-discount').textContent = discountAmt > 0 ? `— ${fmt(discountAmt)}` : '— PKR 0';
  document.getElementById('summary-tax').textContent = fmt(tax);
  document.getElementById('summary-total').textContent = fmt(grand);
  updatePayBtn(grand);
}
```

### Yeh kya karta hai?

JS ab cart calculations khud nahi karta. C# se returned `CartStateDto` ko render karta hai.

### Kyun banaya gaya?

Browser source of truth remove karna tha.

### Agar na karte to kya hota?

```text
Browser cart aur C# cart alag ho sakte thay
Payment total unreliable hota
Cart refresh/session issue hotay
```

### Safe ya risky?

Safe. UI design preserve hua. Sirf script behavior change hua.

Risk / note:

```text
holdActiveOrder abhi pos_held_order temporary snapshot use karta hai.
Yeh active cart source of truth nahi hai.
Full hold/resume later revisit hoga.
```

### Real POS system mein faida

Checkout UI ab real C# business state se driven hai.

### Status

```text
PASS
```

---

## 16. Task 5.3.10 — Final Tests + Verification

### Related test files

```text
POS.Desktop.Tests/Services/Orders/OrderServiceTests.cs
POS.Desktop.Tests/Shell/OrderBridgeHandlerTests.cs
POS.Desktop.Tests/Shell/PosWebMessageRouterTests.cs
```

### What was tested?

Order service tests cover:

```text
Empty cart state
Add item creates line
Add same item increments quantity
Update quantity
Remove item
Clear cart
Fixed amount discount
Percentage discount
Remove discount
Invalid variant
Unknown variant
Not sellable/inactive item
Invalid quantity
Excessive quantity
Empty cart discount
Discount greater than subtotal
Invalid percentage
No tax item
5% tax-exclusive item
18% tax-exclusive item
Mixed tax rates
Tax-included item
Discount before tax
Rounding remainder
Total equations
```

Bridge tests cover:

```text
All order.* endpoints registered
order.getCart
order.addItem
order.updateLineQuantity
order.removeItem
order.clearCart
order.applyDiscount
order.removeDiscount
Malformed payloads
Validation error mapping
Generic internal error does not leak raw details
```

UI/search verification covers:

```text
No sessionStorage.getItem('pos_cart')
No sessionStorage.setItem('pos_cart')
No active sessionStorage.pos_cart source of truth
main_checkout.html bridge-backed cart active
Assets and docs checkout copies SHA-256 identical
payment_screen.html unedited/deferred
```

### Final reported verification

```text
dotnet build POS.Desktop/POS.Desktop.csproj --configuration Debug
Result: PASS, 0 errors / 0 warnings

dotnet build POS.slnx --configuration Debug
Result: PASS, 0 errors / 0 warnings

dotnet test POS.Desktop.Tests
Result: 305/305 passed

dotnet test POS.Tests
Result: 49/49 passed

git diff --check
Result: clean
```

### Status

```text
PASS
```

---

# 17. Demonstration Flow

## Demo 1 — Open checkout

```text
1. Cashier login already done
2. Shift already open
3. main_checkout.html loads
4. Screen calls shift.getCurrent
5. If shift is open, screen continues
6. Screen calls order.getCart
7. Empty cart state returns
8. Cart panel shows "Cart is empty"
```

Expected cart response:

```json
{
  "subtotalAmount": 0,
  "discountAmount": 0,
  "taxAmount": 0,
  "totalAmount": 0,
  "lines": [],
  "discountType": "",
  "discountValue": 0
}
```

---

## Demo 2 — Add item

```text
1. Cashier taps product card
2. JS calls order.addItem
3. Payload: { variantId: 101, quantity: 1 }
4. Router parses payload
5. OrderService validates item from CatalogService
6. DraftCartStore updates cart
7. Totals/tax recalculate
8. Updated CartStateDto returns
9. UI renders line + summary total
```

Example JS:

```javascript
const state = await bridgeRequest('order.addItem', {
  variantId: Number(item.variantId),
  quantity: 1
});
updateLocalCartState(state);
renderCart();
```

---

## Demo 3 — Increase/decrease quantity

```text
1. Cashier taps + button
2. JS calculates targetQty
3. JS calls order.updateLineQuantity
4. C# validates item exists in cart
5. C# updates quantity
6. C# recalculates totals/tax
7. UI re-renders from returned CartStateDto
```

Example payload:

```json
{
  "variantId": 101,
  "quantity": 3
}
```

---

## Demo 4 — Remove item

```text
1. Cashier taps remove/void item
2. JS calls order.removeItem
3. C# removes line
4. If cart becomes empty, discount resets
5. C# returns updated cart
6. UI re-renders
```

Example payload:

```json
{
  "variantId": 101
}
```

---

## Demo 5 — Apply fixed discount

```text
Cart subtotal: PKR 1000
Cashier applies PKR 100 discount
JS calls order.applyDiscount
C# validates amount <= subtotal
C# distributes discount before tax
C# recalculates line tax/totals
```

Example payload:

```json
{
  "discountType": "amount",
  "discountValue": 100
}
```

---

## Demo 6 — Apply percentage discount

```text
Cart subtotal: PKR 2000
Cashier applies 10% discount
C# calculates discount = PKR 200
Discount distributed across lines
Tax calculated after discount
```

Example payload:

```json
{
  "discountType": "pct",
  "discountValue": 10
}
```

---

## Demo 7 — Void entire cart

```text
1. Cashier taps void order
2. Confirmation appears
3. JS calls order.clearCart
4. C# clears DraftCartStore
5. UI returns to empty cart
```

Example:

```javascript
const state = await bridgeRequest('order.clearCart', null);
updateLocalCartState(state);
renderCart();
```

---

## Demo 8 — Proceed to payment

```text
1. Cashier clicks pay
2. UI checks currentCart length
3. Navigation to payment_screen.html happens
4. Payment flow remains deferred to Milestone 5.4
```

Important:

```text
5.3 does not complete sale.
5.3 only prepares authoritative cart state.
```

---

# 18. Full Cart State Shape

Final `CartStateDto`:

```json
{
  "subtotalAmount": 1000.00,
  "discountAmount": 100.00,
  "taxAmount": 45.00,
  "totalAmount": 945.00,
  "discountType": "amount",
  "discountValue": 100.00,
  "lines": [
    {
      "id": "101",
      "itemId": 10,
      "variantId": 101,
      "name": "Sample Item",
      "quantity": 1,
      "unitPrice": 1000.00,
      "grossAmount": 1000.00,
      "discountAmount": 100.00,
      "taxAmount": 45.00,
      "netAmount": 945.00,
      "taxRuleId": 1,
      "taxCode": "GST5",
      "taxRate": 5.00,
      "isTaxIncluded": false,
      "unit": "PCS",
      "categoryCode": "GENERAL"
    }
  ]
}
```

---

# 19. Error Handling Flow

## Example: invalid variant ID

```text
JS sends:
{ variantId: 0 }

C# returns:
ok=false
code=INVALID_VARIANT_ID
message=Variant ID must be greater than zero.
```

## Example: malformed payload

```text
JS sends:
{ variantId: "abc" }

C# returns:
ok=false
code=MALFORMED_REQUEST
message=Parameter 'variantId' is required and must be an integer number.
```

## Example: internal exception

```text
Raw exception details are not shown to cashier.
Router returns safe generic handler error.
```

Why this matters:

```text
Cashier ko useful error milta hai.
Developer logs internal details dekh sakte hain.
Sensitive details UI mein leak nahi hotay.
```

---

# 20. Browser Storage Status After 5.3

Removed as active source of truth:

```text
sessionStorage.pos_cart
```

Still present intentionally:

```text
sessionStorage.pos_attached_customer
sessionStorage.pos_held_order
localStorage.terminal_config
```

Why?

```text
pos_attached_customer → customer attach demo/state, not active cart source of truth
pos_held_order → temporary hold snapshot, full hold/resume deferred
terminal_config → provisioning area, already moved away from source-of-truth usage in active flow
```

Important:

```text
Active cart state now lives in C# DraftCartStore.
```

---

# 21. Safe / Risky Summary

## Safe

```text
No payment committed
No Order/OrderLine DB write
No migration
No POS.Api change
No POS.Shared change
No payment_screen.html edit
Bridge errors are safe
Cart source of truth moved to C#
Money calculation centralized
Tests added
```

## Risks / limitations

```text
Draft cart is in-memory only
App restart loses draft cart
Full hold/resume not finalized
Manager PIN override in checkout void flow still prototype-style
Payment completion not implemented yet
Receipt/print/sync not implemented yet
```

## Why acceptable?

```text
Milestone 5.3 ka scope order/cart draft service tha.
Payment and persistent sale creation Milestone 5.4 mein aayegi.
```

---

# 22. Real POS System Benefits

After Milestone 5.3:

```text
- Cart state browser se C# mein shift ho gayi
- Product add/update/remove service-backed ho gaya
- Discounts C# validate karta hai
- Tax calculation C# mein line-level hoti hai
- Mixed tax rates supported hain
- Money rounding centralized hai
- UI bridge-backed cart render karti hai
- Browser sessionStorage.pos_cart active source of truth nahi raha
- Payment milestone ke liye reliable cart foundation ready hai
```

Before 5.3:

```text
Browser currentCart
Browser sessionStorage.pos_cart
JavaScript subtotal/tax/discount
```

After 5.3:

```text
C# DraftCartStore
C# OrderService
C# tax/totals/rounding
Bridge-backed main_checkout.html
```

---

# 23. Known Limitations / Deferred Work

## Payment still deferred

```text
payment_screen.html unchanged.
Sale complete nahi hoti.
Order/OrderLine/Payment rows create nahi hotay.
```

Future:

```text
Milestone 5.4 — Payment & completion service
```

## Draft cart not restart-survivable

```text
App close/restart par cart lost ho jayega.
```

Future option:

```text
Persisted draft / recovery journal / local DB draft table
```

But not needed in 5.3.

## Hold/resume temporary

```text
pos_held_order temporary snapshot use hota hai.
Active cart source of truth nahi hai.
```

Future:

```text
Dedicated hold/resume order service.
```

## Manager PIN still prototype in checkout void flow

```text
Void manager override UI abhi local/demo behavior rakhta hai.
```

Future:

```text
Milestone 5.5 cash control / manager PIN reuse
```

---

# 24. Final Verification Summary

Final milestone context reported:

```text
POS.Desktop build: PASS, 0 errors / 0 warnings
Solution build: PASS, 0 errors / 0 warnings
POS.Desktop.Tests: PASS, 305/305
POS.Tests: PASS, 49/49
git diff --check: clean
main_checkout.html Assets/docs SHA-256 identical
payment_screen.html unedited/deferred
```

---

# 25. Milestone 5.3 Final State

```text
IOrderService exists ✅
In-memory DraftCartStore exists ✅
OrderService exists ✅
Add item works ✅
Update quantity works ✅
Remove item works ✅
Clear cart works ✅
Discount apply/remove works ✅
Money rounding centralized ✅
Tax-exclusive calculation works ✅
Tax-inclusive calculation works ✅
Mixed tax rates supported ✅
order.* bridge endpoints registered ✅
main_checkout.html uses bridge-backed cart ✅
sessionStorage.pos_cart removed as active source of truth ✅
Tests pass 305/305 ✅
POS.Tests pass 49/49 ✅
Ready for Milestone 5.4 ✅
```

---

# 26. Next Recommended Milestone

```text
Phase 5 / Milestone 5.4 — Payment & completion service
```

Expected next focus:

```text
IPaymentService
Cash/card/wallet/split tender
Compute cash change
Commit Order/OrderLine/Payment atomically
Enqueue SyncOutbox event
Enqueue PrintQueue receipt
Wire payment_screen.html
Idempotent completion
Payment tests
```

---

# 27. Short Report for Senior

```text
Milestone 5.3 completed.
We moved checkout cart from browser sessionStorage to a C# order/cart service.
The desktop UI now calls order.* bridge endpoints for cart actions.
C# now owns add/update/remove/discount/tax/totals/rounding.
sessionStorage.pos_cart is no longer active source of truth.
main_checkout.html only renders returned CartStateDto.
All tests passed: POS.Desktop.Tests 305/305 and POS.Tests 49/49.
Next milestone is 5.4 Payment & completion service.
```

---

# 28. Go / No-Go

```text
Milestone 5.3: GO ✅
Order/cart foundation: GO ✅
Ready for payment milestone 5.4: YES ✅
```
