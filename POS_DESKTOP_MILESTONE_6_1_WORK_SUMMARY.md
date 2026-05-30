# POS Desktop UI Integration — Milestone 6.1 Work Summary

**Milestone:** Phase 6 / Milestone 6.1 — POS.Api Sync Ingest Endpoint  
**Status:** 100% Complete  
**Main Goal:** POS Desktop ke local `SyncOutbox` events ko central `POS.Api` tak safely receive karna, device auth ke through protect karna, duplicate/retry cases ko idempotently handle karna, aur central DB mein durable acknowledgement save karna.

---

## 0. Simple Step-by-Step Flow

Milestone 6.1 ka simple flow yeh hai:

```text
1. Desktop offline mode mein orders/payments/cash movements ko local SyncOutbox mein save karta hai.
2. Future sync client ye outbox events ko batch/chunk bana kar POS.Api ko bhejega.
3. API endpoint: POST /api/sync/ingest
4. API pehle JWT device token check karta hai.
5. Token claims se tenant/location/terminal identity nikalti hai.
6. Request body identity claims ke against cross-check hoti hai.
7. API same batch ke duplicate EventId / IdempotencyKey reject karti hai.
8. API chunk-level duplicate/retry ko detect karti hai.
9. Agar safe retry ho to old ack response return hota hai.
10. Agar same key different payload ho to 409 conflict return hota hai.
11. New valid chunk ke liye SyncIngestAck row central DB mein save hoti hai.
12. AckPayloadJson mein full request + response envelope preserve hota hai.
13. Response status "Received" return hota hai.
```

Important: **`Received` ka matlab yeh nahi ke Order/Payment/Shift central business tables mein transform ho chuki hai.** Is milestone mein API sirf durable sync receipt/preservation gateway bana hai. Actual business event transformation future sync/business processing milestones mein hogi.

---

## 1. Milestone 6.1 Tasks Completed

| Task | Status | Summary |
|---|---:|---|
| 6.1.1 | Complete | Shared ingest contract DTOs banaye |
| 6.1.2 | Complete | POS.Api sync service scaffolding banaya |
| 6.1.3 | Complete | `POST /api/sync/ingest` endpoint add kiya |
| 6.1.4 | Complete | `PosDevice` policy apply ki |
| 6.1.5 | Complete | Idempotent persist + dedupe implement kiya |
| 6.1.6 | Complete | `SyncIngestAck` ke through durable ack save kiya |
| 6.1.7 | Complete | Unauthorized / invalid callers reject kiye |
| 6.1.8 | Complete | Duplicate event IDs and duplicate sequences handle kiye |
| 6.1.9 | Complete | API integration tests add kiye |
| 6.1.10 | Complete | Endpoint contract documentation add ki |

---

## 2. What Problem 6.1 Solves

### Pehle problem kya thi?

Desktop app local SQLite mein sales/order/payment/cash events save kar sakti thi, lekin central API mein abhi koi real sync ingest target nahi tha. Matlab future Desktop sync processor ke paas koi authenticated server endpoint nahi tha jahan woh local outbox events push kare.

### 6.1 ne kya solve kiya?

Milestone 6.1 ne central API mein sync ingest ka foundation bana diya:

```text
Desktop SyncOutbox -> API Sync Ingest Endpoint -> SyncIngestAck durable receipt
```

Isse future Phase 6.2 / 6.3 mein Desktop HTTP client aur outbox drain processor safely build ho sakenge.

### Real POS system mein faida

Real POS mein network unreliable hota hai. Terminal offline orders save karta hai aur baad mein sync karta hai. Agar same request retry ho jaye, API duplicate sale/payment create nahi karegi. Is milestone ka core faida yahi hai: **safe retry + no double-write foundation**.

---

## 3. Files Added / Modified

### Group 1 — Contract + API scaffold

```text
POS.Shared/Contracts/Sync/SyncIngestEvent.cs
POS.Shared/Contracts/Sync/SyncIngestRequest.cs
POS.Shared/Contracts/Sync/SyncIngestEventAck.cs
POS.Shared/Contracts/Sync/SyncIngestResponse.cs
POS.Api/Application/Sync/SyncIngestIdentity.cs
POS.Api/Application/Sync/ISyncIngestService.cs
POS.Api/Application/Sync/SyncIngestService.cs
```

### Group 2 — Endpoint + authorization boundary

```text
POS.Api/Controllers/SyncController.cs
POS.Api/Program.cs
```

### Group 3 — Persistence + idempotency

```text
POS.Api/Application/Sync/SyncConflictException.cs
POS.Api/Application/Sync/SyncIngestService.cs
POS.Api/Controllers/SyncController.cs
```

### Group 4 — Integration tests

```text
POS.Tests/IntegrationTests/SyncIngestEndpointTests.cs
```

### Group 5 — Documentation + context

```text
docs/sync/SYNC_INGEST_ENDPOINT.md
docs/antigravity-context/POS_DESKTOP_CURRENT_CONTEXT.md
```

---

## 4. Sync Contract DTOs

### Related path

```text
POS.Shared/Contracts/Sync/
```

### Kya banaya gaya?

Shared contracts banaye gaye jo Desktop aur API dono use kar sakte hain:

```text
SyncIngestRequest
SyncIngestEvent
SyncIngestResponse
SyncIngestEventAck
```

### Core request shape

```csharp
public sealed record SyncIngestRequest(
    int TenantId,
    int LocationId,
    int TerminalId,
    long ChunkSequence,
    string ChunkIdempotencyKey,
    string RequestHash,
    string CorrelationId,
    IReadOnlyList<SyncIngestEvent> Events);
```

### Core event shape

```csharp
public sealed record SyncIngestEvent(
    DateOnly BusinessDate,
    long TerminalSequence,
    string EventType,
    Guid EventId,
    string PayloadJson,
    string PayloadHash,
    string IdempotencyKey,
    string CorrelationId,
    long? ChunkSequence);
```

### Core response shape

```csharp
public sealed record SyncIngestResponse(
    Guid AckId,
    long ChunkSequence,
    string ChunkIdempotencyKey,
    string Status,
    int EventCount,
    IReadOnlyList<SyncIngestEventAck> Events,
    string? ErrorCode,
    string? ErrorMessage);
```

### Kyun banaya gaya?

Desktop `SyncOutbox` events ko API tak consistent format mein bhejne ke liye. Contract shared project mein rakha gaya taake Desktop aur API ke beech DTO drift na ho.

### Agar na karte to kya hota?

Desktop aur API alag-alag JSON shape assume karte. Future sync client mein bugs aate, jaise casing mismatch, missing fields, ya response parse failure.

### Safe hai ya risky?

Safe hai, kyun ke yeh sirf DTO contract hai. Business logic DTOs mein nahi rakhi gayi.

---

## 5. API Service Scaffolding

### Related path

```text
POS.Api/Application/Sync/
```

### Kya banaya gaya?

API side sync service abstraction banayi gayi:

```csharp
public interface ISyncIngestService
{
    Task<SyncIngestResponse> IngestAsync(
        SyncIngestIdentity identity,
        SyncIngestRequest request,
        CancellationToken cancellationToken = default);
}
```

### Identity object

```csharp
public sealed record SyncIngestIdentity(
    int TenantId,
    int LocationId,
    int TerminalId,
    string? DeviceId);
```

### Kyun banaya gaya?

Controller ko thin rakhne ke liye. Controller HTTP/auth handle kare, aur business sync logic service mein rahe.

### Real POS faida

Future mein isi service mein retry, ack, processing queue, ledger parser, aur outbox mapping easily extend ho sakti hai.

---

## 6. API Endpoint Added

### Related file

```text
POS.Api/Controllers/SyncController.cs
```

### Endpoint

```text
POST /api/sync/ingest
```

### Controller attributes

```csharp
[ApiController]
[Route("api/sync")]
[Authorize(Policy = "PosDevice")]
public sealed class SyncController : ControllerBase
```

### Kya karta hai?

Yeh endpoint sync batch receive karta hai. Sirf device token wale callers allowed hain.

### Important behavior

```text
No token -> 401
Wrong client_type -> 403
Missing/invalid device claims -> 403
Body identity mismatch -> 400
Sync conflict -> 409
Success / safe replay -> 200
```

### Kyun banaya gaya?

Desktop terminal ko central API par ek official, authenticated ingest door dene ke liye.

---

## 7. PosDevice Authorization

### Related file

```text
POS.Api/Program.cs
```

### Policy concept

API mein already `PosDevice` policy configured thi. Milestone 6.1 mein sync endpoint par apply ki gayi.

```csharp
[Authorize(Policy = "PosDevice")]
```

### Required claims

```text
client_type = device
tenant_id = positive int
location_id = positive int
terminal_id = positive int
device_id = optional
```

### Kyun zaroori hai?

Sync ingest endpoint financial/operational events receive karta hai. Agar normal user/admin token se sync allow ho jaye to security boundary weak ho jati.

### Real POS faida

Sirf provisioned/known POS terminal sync kar sakta hai. User login token ya browser request se fake sync push nahi ho sakta.

---

## 8. Claims-Derived Identity

### Related file

```text
POS.Api/Controllers/SyncController.cs
```

### Snippet

```csharp
var tenantIdClaim = User.FindFirstValue(ApiClaimTypes.TenantId);
var locationIdClaim = User.FindFirstValue(ApiClaimTypes.LocationId);
var terminalIdClaim = User.FindFirstValue(ApiClaimTypes.TerminalId);
var deviceId = User.FindFirstValue(ApiClaimTypes.DeviceId);
```

### Positive ID validation

```csharp
if (string.IsNullOrEmpty(tenantIdClaim) ||
    string.IsNullOrEmpty(locationIdClaim) ||
    string.IsNullOrEmpty(terminalIdClaim) ||
    !int.TryParse(tenantIdClaim, out var tenantId) ||
    !int.TryParse(locationIdClaim, out var locationId) ||
    !int.TryParse(terminalIdClaim, out var terminalId) ||
    tenantId <= 0 ||
    locationId <= 0 ||
    terminalId <= 0)
{
    return Forbid();
}
```

### Kyun banaya gaya?

Request body ke `tenantId`, `locationId`, `terminalId` ko source of truth nahi banaya. Token claims source of truth hain.

### Agar na karte to kya hota?

Koi device apni body mein dusre tenant ka `tenantId` daal kar data spoof kar sakta tha.

---

## 9. Body Identity Cross-Check

### Related file

```text
POS.Api/Controllers/SyncController.cs
```

### Snippet

```csharp
if (identity.TenantId != request.TenantId)
{
    return Problem(
        detail: $"TenantId in request body ({request.TenantId}) does not match authenticated device claim ({identity.TenantId}).",
        statusCode: StatusCodes.Status400BadRequest,
        title: "Device Identity Mismatch");
}
```

Same check `LocationId` aur `TerminalId` ke liye bhi hai.

### Kyun banaya gaya?

Token claims aur request body sync honi chahiye. Body mein mismatch ho to API usay tampered request samajh kar reject karti hai.

### Safe hai ya risky?

Safe hai. Yeh tenant spoofing aur wrong-terminal upload ko block karta hai.

---

## 10. SyncIngestAck Persistence

### Related files

```text
POS.Api/Application/Sync/SyncIngestService.cs
POS.Shared/Domain/Entities/Central/SyncIngestAck.cs
POS.Api/Data/Configurations/Central/SyncIngestAckConfiguration.cs
```

### Kya save hota hai?

Successful new sync chunk ke liye central DB mein `SyncIngestAck` row save hoti hai.

### Important fields

```csharp
var ack = new SyncIngestAck
{
    Id = ackId,
    TenantId = identity.TenantId,
    LocationId = identity.LocationId,
    TerminalId = identity.TerminalId,
    ChunkSequence = request.ChunkSequence,
    ChunkIdempotencyKey = request.ChunkIdempotencyKey,
    RequestHash = request.RequestHash,
    EventCount = request.Events.Count,
    Status = "Received",
    AckPayloadJson = serializedEnvelope,
    ReceivedOn = DateTimeOffset.UtcNow,
    ExpiresOn = DateTimeOffset.UtcNow.AddDays(30),
    CorrelationId = request.CorrelationId,
    IsActive = true,
    CreatedBy = identity.DeviceId ?? "sync-device",
    CreatedOn = DateTimeOffset.UtcNow
};
```

### Kyun save hota hai?

Agar Desktop same chunk retry kare, API ko pata hona chahiye ke yeh pehle receive ho chuka hai. Ack row durable memory ka kaam karti hai.

### Real POS faida

Network timeout ke baad Desktop retry kare to central API duplicate transaction nahi banati. Same ack return kar sakti hai.

---

## 11. AckPayloadJson Envelope

### Related file

```text
POS.Api/Application/Sync/SyncIngestService.cs
```

### Kya store hota hai?

`AckPayloadJson` mein full envelope store hota hai:

```csharp
private sealed record SyncIngestAckEnvelope(
    SyncIngestRequest Request,
    SyncIngestResponse Response,
    DateTimeOffset ReceivedOn,
    string StatusMeaning);
```

### Envelope create snippet

```csharp
var envelope = new SyncIngestAckEnvelope(
    request,
    response,
    DateTimeOffset.UtcNow,
    "Received means durable sync receipt and payload preservation only; business event transformation is deferred."
);

var serializedEnvelope = JsonSerializer.Serialize(envelope, JsonSerializerOptions);
```

### Kyun zaroori hai?

Group 3 mein new inbound event table add nahi ki gayi. Isliye request + response + event payloads ko `AckPayloadJson` mein preserve kiya gaya, taake future processor/debugging ke liye raw event information available rahe.

### Important limitation

Yeh final event ledger nahi hai. Yeh temporary durable preservation envelope hai. Actual business table transformation future milestones mein hogi.

---

## 12. Idempotency / Safe Replay

### Related file

```text
POS.Api/Application/Sync/SyncIngestService.cs
```

### Flow

```text
1. API checks TenantId + ChunkIdempotencyKey.
2. Agar existing ack milti hai, request replay ho sakti hai.
3. API RequestHash compare karti hai.
4. API stored envelope ko incoming request se compare karti hai.
5. Agar exact equivalent hai to old response return karti hai.
6. New row create nahi hoti.
```

### Snippet

```csharp
var existingAck = await _db.SyncIngestAcks
    .IgnoreQueryFilters()
    .FirstOrDefaultAsync(
        x => x.TenantId == identity.TenantId && x.ChunkIdempotencyKey == request.ChunkIdempotencyKey,
        cancellationToken);

if (existingAck != null)
{
    return ParseAndVerifyPersistedAckResponse(existingAck, request);
}
```

### Replay verification snippet

```csharp
if (existingAck.RequestHash != request.RequestHash)
{
    throw new SyncConflictException(
        "IDEMPOTENCY_CONFLICT",
        "Chunk idempotency key is already used with a different request payload.");
}
```

### Envelope equivalence check

```csharp
if (!IsStoredEnvelopeEquivalentToRequest(envelope, request))
{
    throw new SyncConflictException(
        "STORED_ENVELOPE_CONFLICT",
        "The stored chunk request payload details do not match the incoming request.");
}

return envelope.Response;
```

### Kyun banaya gaya?

Network retries common hotay hain. Agar request already save ho chuki hai, API safely previous response return kar sakti hai.

---

## 13. Duplicate Event Protection

### Related file

```text
POS.Api/Application/Sync/SyncIngestService.cs
```

### Same-batch duplicate EventId

```csharp
var eventIds = new HashSet<Guid>();

foreach (var ev in request.Events)
{
    if (!eventIds.Add(ev.EventId))
    {
        throw new SyncConflictException(
            "DUPLICATE_EVENT_ID",
            $"Duplicate EventId '{ev.EventId}' found inside the same sync batch. Duplicate event identifiers inside the same sync batch are not allowed.");
    }
}
```

### Same-batch duplicate event IdempotencyKey

```csharp
var idempotencyKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

if (!string.IsNullOrWhiteSpace(ev.IdempotencyKey))
{
    if (!idempotencyKeys.Add(ev.IdempotencyKey))
    {
        throw new SyncConflictException(
            "DUPLICATE_EVENT_IDEMPOTENCY_KEY",
            $"Duplicate event IdempotencyKey '{ev.IdempotencyKey}' found inside the same sync batch. Duplicate event identifiers inside the same sync batch are not allowed.");
    }
}
```

### What is deferred?

Cross-chunk duplicate EventId detection abhi defer hai, kyun ke proper inbound event ledger/table abhi nahi bani.

---

## 14. Sequence Conflict Protection

### Related file

```text
POS.Api/Application/Sync/SyncIngestService.cs
```

### Flow

```text
Same TenantId + Same TerminalId + Same ChunkSequence
but different chunk idempotency key
=> 409 Sequence Conflict
```

### Snippet

```csharp
var existingSequenceAck = await _db.SyncIngestAcks
    .IgnoreQueryFilters()
    .FirstOrDefaultAsync(
        x => x.TenantId == identity.TenantId &&
             x.TerminalId == identity.TerminalId &&
             x.ChunkSequence == request.ChunkSequence,
        cancellationToken);

if (existingSequenceAck != null)
{
    throw new SyncConflictException(
        "SEQUENCE_CONFLICT",
        $"Chunk sequence {request.ChunkSequence} for terminal {identity.TerminalId} has already been processed under a different chunk idempotency key.");
}
```

### Important note

Yeh duplicate sequence collision block karta hai. Strict gap/out-of-order checking abhi deferred hai.

---

## 15. Conflict Exception

### Related file

```text
POS.Api/Application/Sync/SyncConflictException.cs
```

### Kya banaya gaya?

Custom exception banayi gayi jo sync conflicts ko cleanly represent karti hai.

```csharp
public sealed class SyncConflictException : Exception
{
    public string ErrorCode { get; }

    public SyncConflictException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
```

### Controller mapping

```csharp
catch (SyncConflictException ex)
{
    return Problem(
        detail: ex.Message,
        statusCode: StatusCodes.Status409Conflict,
        title: ex.ErrorCode);
}
```

### Kyun banaya gaya?

Service business conflict detect karti hai, controller usay HTTP `409 Conflict` mein convert karta hai.

---

## 16. Error Behavior Matrix

| Case | HTTP Status | Explanation |
|---|---:|---|
| No token | 401 | `[Authorize]` handles it |
| Wrong `client_type` | 403 | `PosDevice` policy rejects |
| Missing/invalid claims | 403 | Controller `Forbid()` returns |
| Body identity mismatch | 400 | Token and body mismatch |
| Same key + same payload | 200 | Safe replay, previous response |
| Same key + different payload | 409 | Idempotency conflict |
| Same terminal sequence + different key | 409 | Sequence conflict |
| Duplicate EventId in same batch | 409 | Duplicate event conflict |
| Duplicate event IdempotencyKey in same batch | 409 | Duplicate event conflict |
| Unexpected server error | 500 | Normal ASP.NET behavior |

---

## 17. Integration Tests

### Related file

```text
POS.Tests/IntegrationTests/SyncIngestEndpointTests.cs
```

### Test coverage added

Group 4 mein 19 new API integration tests add hue. Total API tests 68 pass hue.

### Covered cases

```text
- unauthenticated caller -> 401
- wrong client_type -> 403
- missing tenant/location/terminal claims -> 403
- invalid zero/negative claims -> 403
- non-numeric claim -> 403
- body identity mismatch -> 400
- happy path -> 200 + Received
- SyncIngestAck row exists
- AckPayloadJson envelope preserves request + response
- duplicate replay returns same AckId
- duplicate replay does not create extra DB row
- same key different hash -> 409
- same terminal sequence different key -> 409
- duplicate EventId -> 409
- duplicate event IdempotencyKey -> 409
```

### Terminal seed detail

Tests ne valid terminal seed kiya, kyun ke `SyncIngestAck` FK `TenantId + TerminalId` and `TenantId + LocationId` require karta hai. Location ID assume nahi ki gayi; DB se location query karke terminal create kiya gaya.

---

## 18. Endpoint Contract Documentation

### Related file

```text
docs/sync/SYNC_INGEST_ENDPOINT.md
```

### Kya document hua?

```text
- Route: POST /api/sync/ingest
- Authorization: PosDevice policy
- Required JWT claims
- Request DTO contract
- Response DTO contract
- Status code matrix
- Idempotency and dedupe behavior
- Persistence contract
- AckPayloadJson envelope behavior
- JSON examples
- Deferred work
```

### Why important?

Future 6.2 Desktop HTTP client isi contract ko follow karega. Agar docs clear na hoti to Desktop side wrong JSON body, wrong claim assumption, ya wrong retry behavior implement kar sakti thi.

---

## 19. Demonstration — Happy Path Example

### Request

```http
POST /api/sync/ingest
Authorization: Bearer <device-jwt>
Content-Type: application/json
```

```json
{
  "tenantId": 101,
  "locationId": 1,
  "terminalId": 5,
  "chunkSequence": 1,
  "chunkIdempotencyKey": "chunk-idem-abc123",
  "requestHash": "req-hash-abc123",
  "correlationId": "corr-abc123",
  "events": [
    {
      "businessDate": "2026-05-28",
      "terminalSequence": 1001,
      "eventType": "OrderCompleted",
      "eventId": "f7d75fa5-e3d8-4f24-9b24-738b5505f0a0",
      "payloadJson": "{\"orderId\":\"ORD-1\",\"netAmount\":175.00}",
      "payloadHash": "payload-hash-abc123",
      "idempotencyKey": "event-idem-abc123",
      "correlationId": "corr-abc123",
      "chunkSequence": 1
    }
  ]
}
```

### Response

```json
{
  "ackId": "b1bcfb7e-97c4-42f1-bb21-7299a9bbf870",
  "chunkSequence": 1,
  "chunkIdempotencyKey": "chunk-idem-abc123",
  "status": "Received",
  "eventCount": 1,
  "events": [
    {
      "eventId": "f7d75fa5-e3d8-4f24-9b24-738b5505f0a0",
      "idempotencyKey": "event-idem-abc123",
      "terminalSequence": 1001,
      "status": "Received",
      "errorCode": null,
      "errorMessage": null
    }
  ],
  "errorCode": null,
  "errorMessage": null
}
```

### DB result

```text
SyncIngestAcks table mein 1 row save hoti hai.
AckPayloadJson mein request + response envelope store hota hai.
```

---

## 20. Demonstration — Safe Replay

### Scenario

Desktop ne request bheji, API ne save kar li, lekin network timeout ki wajah se Desktop response receive nahi kar saka. Desktop same request dobara bhejta hai.

### API behavior

```text
Same ChunkIdempotencyKey
Same RequestHash
Same stored envelope metadata
=> API previous SyncIngestResponse return karti hai
=> New DB row create nahi hoti
```

### Benefit

Double sale/payment/cash movement ka risk kam hota hai.

---

## 21. Demonstration — Conflict

### Scenario

Same `ChunkIdempotencyKey` ke saath different payload aa gaya.

### API behavior

```text
Same ChunkIdempotencyKey
Different RequestHash or envelope mismatch
=> 409 Conflict
```

### Example 409 response

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "IDEMPOTENCY_CONFLICT",
  "status": 409,
  "detail": "Chunk idempotency key is already used with a different request payload.",
  "instance": "/api/sync/ingest"
}
```

---

## 22. What Was Intentionally Deferred

Milestone 6.1 intentionally did **not** implement these things:

```text
- Desktop HTTP sync client
- Desktop background sync processor
- Pull sync from API to Desktop
- Event transformation into central Orders/Payments/Shifts/Cash tables
- New inbound event ledger table
- Cross-chunk duplicate EventId search
- Strict sequence gap / out-of-order blocking
- Device token acquisition/refresh
```

These belong to future Phase 6 milestones, mainly 6.2 onward.

---

## 23. Verification Summary

### Build

```text
dotnet build POS.slnx --configuration Debug
Result: Build succeeded
Warnings: 0
Errors: 0
```

### API tests

```text
dotnet test POS.Tests/POS.Tests.csproj --configuration Debug
Result: Test Run Successful
Total tests: 68
Passed: 68
```

---

## 24. Simple Analogy

Is milestone ko courier office jaisa samjho:

```text
Desktop = shop ka cashier
SyncOutbox = parcels ka bag
Sync endpoint = courier counter
JWT device token = shop ka official ID card
SyncIngestAck = receipt slip
AckPayloadJson = parcel ki photocopy + receipt details
Idempotency key = parcel tracking number
```

Agar cashier same parcel dobara counter par le aaye, courier system bolta hai:

```text
Yeh parcel pehle receive ho chuka hai, yeh rahi same receipt.
```

Agar tracking number same hai lekin parcel andar se different hai:

```text
Conflict! Same tracking number different parcel ke liye use nahi ho sakta.
```

---

## 25. Final Outcome

Milestone 6.1 complete hone ke baad central API ke paas ek safe, authenticated, idempotent sync ingest foundation hai.

### Current final capability

```text
POS Desktop future sync client can POST local outbox chunks to POS.Api.
API verifies device identity.
API rejects spoofed/mismatched identity.
API persists durable SyncIngestAck receipt.
API supports safe retry/replay.
API rejects duplicate/conflicting requests.
API has integration test coverage.
API contract is documented.
```

### Next milestone

```text
Phase 6 / Milestone 6.2 — Device-authenticated HTTP client
```

Iska kaam Desktop side HTTP client banana hoga jo device auth ke saath `POST /api/sync/ingest` call kare.
