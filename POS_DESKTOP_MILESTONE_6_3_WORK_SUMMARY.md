# POS Desktop — Phase 6 / Milestone 6.3 Work Summary

**Milestone:** Phase 6 / Milestone 6.3 — Outbox Drain Processor  
**Status:** 100% Complete  
**Branch:** `main`  
**Repository:** `AdeelSaifee/POS`  
**Final milestone commit:** `4aa614f — Add sync processor pipeline verification`  
**Previous related commits in this milestone:**

| Group | Commit | Message |
|---|---|---|
| Group 1 | `8a7fa32` | Add sync processor lifecycle foundation |
| Group 2 | `d3b63a7` | Add sync outbox batch reader |
| Group 3 | `3f02f72` | Post sync outbox batches |
| Group 4 | `ec5e855` | Apply sync ingest acknowledgements |
| Group 5 | `4aa614f` | Add sync processor pipeline verification |

---

## 1. Simple Step-by-Step Flow

Milestone 6.3 ka main kaam tha: **local SQLite `SyncOutbox` mein pending events ko background mein central API tak bhejna, phir success par local rows ko Acked mark karna aur cursor advance karna.**

### Full Flow

```text
1. Payment/order complete hota hai
   ↓
2. Local SQLite mein SyncOutbox row Pending status ke saath save hoti hai
   ↓
3. SyncProcessor background mein run hota hai
   ↓
4. EfSyncOutboxBatchReader pending rows read karta hai
   ↓
5. SyncIngestRequestBuilder deterministic SyncIngestRequest banata hai
   ↓
6. ISyncIngestClient central API /api/sync/ingest ko POST karta hai
   ↓
7. Central-style SyncIngestResponse aata hai
   ↓
8. EfSyncAckApplier response validate karta hai
   ↓
9. Local SyncOutbox rows Acked hoti hain
   ↓
10. SyncCursor "push:outbox" stream advance hota hai
   ↓
11. Same rows dobara pending batch mein nahi aati
```

### Roman Urdu Explanation

Simple words mein:

```text
POS terminal offline/local mode mein sale complete karta hai.
Sale ka data local SQLite mein safe hota hai.
Saath hi SyncOutbox mein ek Pending event ban jata hai.
Background SyncProcessor us event ko uthata hai.
Event ko proper API request mein convert karta hai.
Central server ko bhejta hai.
Server agar Received ack deta hai to local row Acked ho jati hai.
Cursor update hota hai taake future mein sync progress yaad rahe.
```

---

## 2. Milestone 6.3 Task Checklist

| Task | Status | Kya hua |
|---|---:|---|
| 6.3.1 Define the SyncProcessor | ✅ | Background hosted service banaya |
| 6.3.2 Register as hosted service | ✅ | DI mein `AddHostedService<SyncProcessor>()` add hua |
| 6.3.3 Batch unsent outbox rows | ✅ | Pending rows ka scoped EF batch reader bana |
| 6.3.4 Post the batch | ✅ | Batch ko `SyncIngestRequest` bana kar `ISyncIngestClient` se post kiya |
| 6.3.5 Mark rows sent on success | ✅ | Successful ack ke baad rows `Acked` hoti hain |
| 6.3.6 Advance the cursor | ✅ | `SyncCursor` stream `push:outbox` advance hoti hai |
| 6.3.7 Run off UI thread | ✅ | BackgroundService + async loop use hua |
| 6.3.8 Tune batch size/interval | ✅ | `BatchSize` aur `PollIntervalSeconds` config add hui |
| 6.3.9 Pause cleanly on shutdown | ✅ | CancellationToken + clean shutdown handling |
| 6.3.10 Test events reach central | ✅ | Processor-driven pipeline integration test add hua |

---

## 3. Main Files Created

| File | Purpose |
|---|---|
| `POS.Desktop/Services/Sync/SyncProcessor.cs` | Background worker jo outbox drain flow orchestrate karta hai |
| `POS.Desktop/Services/Sync/SyncProcessorOptions.cs` | Batch size aur poll interval config |
| `POS.Desktop/Services/Sync/SyncOutboxBatch.cs` | Read-only batch wrapper |
| `POS.Desktop/Services/Sync/SyncOutboxBatchItem.cs` | Read-only outbox row projection |
| `POS.Desktop/Services/Sync/ISyncOutboxBatchReader.cs` | Batch reader contract |
| `POS.Desktop/Services/Sync/EfSyncOutboxBatchReader.cs` | EF Core pending-row reader |
| `POS.Desktop/Services/Sync/ISyncIngestRequestBuilder.cs` | Request builder contract |
| `POS.Desktop/Services/Sync/SyncIngestRequestBuilder.cs` | Deterministic `SyncIngestRequest` builder |
| `POS.Desktop/Services/Sync/ISyncAckApplier.cs` | Ack apply contract |
| `POS.Desktop/Services/Sync/EfSyncAckApplier.cs` | Ack validation + row/cursor DB update |
| `POS.Desktop/Services/Sync/SyncAckApplyResult.cs` | Ack apply result model |
| `POS.Desktop.Tests/Services/Sync/SyncProcessorOptionsTests.cs` | Options validation tests |
| `POS.Desktop.Tests/Services/Sync/SyncProcessorTests.cs` | Processor lifecycle/post/guard tests |
| `POS.Desktop.Tests/Services/Sync/SyncOutboxBatchReaderTests.cs` | Batch selection tests |
| `POS.Desktop.Tests/Services/Sync/SyncIngestRequestBuilderTests.cs` | Request builder deterministic mapping tests |
| `POS.Desktop.Tests/Services/Sync/SyncAckApplierTests.cs` | Ack validation + DB transaction tests |
| `POS.Desktop.Tests/Services/Sync/SyncProcessorPipelineIntegrationTests.cs` | Complete processor pipeline verification |

---

## 4. Main Files Modified

| File | Change |
|---|---|
| `POS.Desktop/Configuration/DesktopHostBuilder.cs` | Sync services DI registration |
| `POS.Desktop/appsettings.json` | Sync batch/poll config |
| `POS.Desktop.Tests/Services/Sync/SyncDiResolutionTests.cs` | DI registration verification |
| `docs/antigravity-context/POS_DESKTOP_CURRENT_CONTEXT.md` | Current milestone context updated |

---

# Group 1 — SyncProcessor Lifecycle Foundation

## Completed Tasks

```text
6.3.1 Define SyncProcessor
6.3.2 Register as hosted service
6.3.7 Run off UI thread
6.3.8 Tune batch size/interval
6.3.9 Pause cleanly on shutdown
```

## Files Changed

```text
POS.Desktop/Services/Sync/SyncProcessor.cs
POS.Desktop/Services/Sync/SyncProcessorOptions.cs
POS.Desktop/Configuration/DesktopHostBuilder.cs
POS.Desktop/appsettings.json
POS.Desktop.Tests/Services/Sync/SyncProcessorOptionsTests.cs
POS.Desktop.Tests/Services/Sync/SyncProcessorTests.cs
POS.Desktop.Tests/Services/Sync/SyncDiResolutionTests.cs
docs/antigravity-context/POS_DESKTOP_CURRENT_CONTEXT.md
```

## What Was Built

Group 1 mein humne background sync ka **foundation** banaya.

### Code Snippet — Processor Options

```csharp
public sealed class SyncProcessorOptions
{
    public int BatchSize { get; set; } = 50;
    public int PollIntervalSeconds { get; set; } = 10;

    public bool Validate(out string? errorMessage)
    {
        errorMessage = null;

        if (BatchSize < 1 || BatchSize > 500)
        {
            errorMessage = "BatchSize must be between 1 and 500.";
            return false;
        }

        if (PollIntervalSeconds < 1 || PollIntervalSeconds > 3600)
        {
            errorMessage = "PollIntervalSeconds must be between 1 and 3600.";
            return false;
        }

        return true;
    }
}
```

### Explanation

**Yeh kya karta hai?**  
`SyncProcessorOptions` processor ko batata hai ke ek batch mein kitni rows uthani hain aur kitni der baad outbox check karna hai.

**Kyun banaya gaya?**  
Hard-coded values risky hoti hain. Real POS system mein store/terminal ke load ke hisaab se batch size aur interval tune karna pad sakta hai.

**Agar na karte to kya hota?**  
Processor ya to bohat zyada rows ek saath utha leta, ya bohat frequent polling karta, jis se local machine/API par load aa sakta tha.

**Safe hai ya risky?**  
Safe hai because validation bounds add hain:

```text
BatchSize: 1 to 500
PollIntervalSeconds: 1 to 3600
```

### Code Snippet — appsettings Sync Config

```json
"Sync": {
  "ApiBaseUrl": "https://localhost:5001",
  "IngestPath": "/api/sync/ingest",
  "TimeoutSeconds": 15,
  "ClockSkewSeconds": 300,
  "BatchSize": 50,
  "PollIntervalSeconds": 10
}
```

### Code Snippet — Background Loop

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    await Task.Yield();

    if (!_options.Validate(out var validationError))
    {
        _logger.LogError("SyncProcessorOptions validation failed: {Error}. Sync processor stopping.", validationError);
        return;
    }

    while (!stoppingToken.IsCancellationRequested)
    {
        if (_provisioningContext.IsProvisioned)
        {
            await RunOnceAsync(stoppingToken).ConfigureAwait(false);
        }

        await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken)
            .ConfigureAwait(false);
    }
}
```

### Explanation

**Yeh kya karta hai?**  
`SyncProcessor` background mein repeatedly outbox check karta hai.

**Kyun banaya gaya?**  
POS screen/UI ko block kiye baghair sync background mein chalta rahe.

**Agar na karte to kya hota?**  
Sale screen slow ya freeze ho sakti thi, especially jab API down ya slow ho.

**Safe hai ya risky?**  
Safe design:

```text
- BackgroundService use hua
- CancellationToken honor hota hai
- .Result / .Wait() use nahi hua
- Terminal unprovisioned ho to idle rehta hai
```

---

# Group 2 — Pending Outbox Batch Reader

## Completed Task

```text
6.3.3 Batch unsent outbox rows
```

## Files Changed

```text
POS.Desktop/Services/Sync/SyncOutboxBatch.cs
POS.Desktop/Services/Sync/SyncOutboxBatchItem.cs
POS.Desktop/Services/Sync/ISyncOutboxBatchReader.cs
POS.Desktop/Services/Sync/EfSyncOutboxBatchReader.cs
POS.Desktop/Services/Sync/SyncProcessor.cs
POS.Desktop/Configuration/DesktopHostBuilder.cs
POS.Desktop.Tests/Services/Sync/SyncOutboxBatchReaderTests.cs
docs/antigravity-context/POS_DESKTOP_CURRENT_CONTEXT.md
```

## What Was Built

Group 2 mein humne ek scoped EF service banayi jo sirf current terminal ke Pending outbox rows ko read karta hai.

### Code Snippet — Read-Only Batch Item

```csharp
public sealed record SyncOutboxBatchItem(
    Guid Id,
    int TenantId,
    int LocationId,
    int TerminalId,
    DateOnly BusinessDate,
    long TerminalSequence,
    string EventType,
    Guid EventId,
    string PayloadJson,
    string PayloadHash,
    string IdempotencyKey,
    string CorrelationId);
```

### Explanation

**Yeh kya karta hai?**  
EF entity ko directly return nahi karta. Instead ek read-only DTO return karta hai.

**Kyun banaya gaya?**  
Accidental DB mutation avoid karne ke liye. Reader sirf read kare, update na kare.

**Agar na karte to kya hota?**  
Agar tracked EF entities processor mein pass hoti, to accidental state changes ka risk hota.

### Code Snippet — Pending Batch Query

```csharp
var items = await _db.SyncOutbox
    .AsNoTracking()
    .Where(x => x.LocationId == currentLocationId &&
                x.TerminalId == currentTerminalId &&
                x.Status == SyncOutboxStatus.Pending &&
                x.IsActive)
    .OrderBy(x => x.BusinessDate)
    .ThenBy(x => x.TerminalSequence)
    .ThenBy(x => x.Id)
    .Take(_options.BatchSize)
    .Select(x => new SyncOutboxBatchItem(
        x.Id,
        x.TenantId,
        x.LocationId,
        x.TerminalId,
        x.BusinessDate,
        x.TerminalSequence,
        x.EventType,
        x.EventId,
        x.PayloadJson,
        x.PayloadHash,
        x.IdempotencyKey,
        x.CorrelationId))
    .ToListAsync(cancellationToken);
```

### Explanation

**Yeh kya karta hai?**  
Pending active rows uthata hai jo current location/terminal ki hain.

**Important filters:**

```text
Status == Pending
IsActive == true
LocationId == current terminal location
TerminalId == current terminal
Tenant filter global DbContext se apply hota hai
```

**Kyun `AsNoTracking()` use hua?**  
Reader sirf data read karta hai. Tracking unnecessary memory/side effects create karti.

**Ordering kyun important hai?**

```text
BusinessDate → TerminalSequence → Id
```

Isse deterministic order milta hai. Same pending data hamesha same order mein batch hota hai.

---

# Group 3 — Build and Post SyncIngestRequest

## Completed Task

```text
6.3.4 Post the batch
```

## Files Changed

```text
POS.Desktop/Services/Sync/ISyncIngestRequestBuilder.cs
POS.Desktop/Services/Sync/SyncIngestRequestBuilder.cs
POS.Desktop/Services/Sync/SyncProcessor.cs
POS.Desktop/Configuration/DesktopHostBuilder.cs
POS.Desktop.Tests/Services/Sync/SyncIngestRequestBuilderTests.cs
POS.Desktop.Tests/Services/Sync/SyncProcessorTests.cs
docs/antigravity-context/POS_DESKTOP_CURRENT_CONTEXT.md
```

## What Was Built

Group 3 mein humne pending batch ko API-ready `SyncIngestRequest` mein convert kiya aur `ISyncIngestClient` se central ko post karna add kiya.

### Code Snippet — Request Builder Interface

```csharp
public interface ISyncIngestRequestBuilder
{
    SyncIngestRequest Build(SyncOutboxBatch batch);
}
```

### Code Snippet — Deterministic ChunkSequence

```csharp
long chunkSequence = items.Min(x => x.TerminalSequence);
```

### Explanation

**Yeh kya karta hai?**  
Batch ka temporary `ChunkSequence` minimum terminal sequence se banata hai.

**Kyun?**  
Group 3 mein `SyncCursor` update nahi hota tha. Is liye sequence deterministic hona zaroori tha taake same batch repeated ho to same identity banay.

**Agar random hota to kya hota?**  
Central API duplicate sequence / idempotency conflict de sakti thi.

### Code Snippet — Deterministic Chunk Key

```csharp
var chunkIdempotencyKey =
    $"chunk:{tenantId}:{locationId}:{terminalId}:{firstBusinessDate}:{minSeq}:{maxSeq}:{count}:{shortHash}";
```

### Explanation

**Yeh kya karta hai?**  
Same batch ke liye same chunk key banata hai.

**Kyun important hai?**  
Sync retry mein idempotency key same honi chahiye. Agar API ko same chunk dobara mile to woh safely duplicate handle kar sake.

### Code Snippet — Deterministic RequestHash

```csharp
var canonicalString = string.Join("|", hashParts);
var canonicalBytes = Encoding.UTF8.GetBytes(canonicalString);
var requestHashBytes = sha256.ComputeHash(canonicalBytes);
requestHash = Convert.ToHexString(requestHashBytes).ToLowerInvariant();
```

### Explanation

**Yeh kya karta hai?**  
Request ka stable SHA-256 hash banata hai.

**Kyun?**  
Central side same idempotency key ke saath different payload aaye to conflict detect kar sake.

### Code Snippet — SyncProcessor Post Flow

```csharp
var request = builder.Build(batch);

var result = await client.IngestAsync(request, cancellationToken)
    .ConfigureAwait(false);

if (result.Success)
{
    // Group 4 mein local ack apply added hua
}
else
{
    var error = result.Error;
    _logger.LogError(
        "SyncProcessor failed to post outbox batch {Sequence}. ErrorType: {ErrorType}, Code: {Code}, Message: {Message}",
        request.ChunkSequence,
        error?.ErrorType,
        error?.Code,
        error?.Message);
}
```

### Safety Rule

Group 3 mein **DB rows mutate nahi ki gayi**. Sirf post + result handling hua.

---

# Group 4 — Apply Acknowledgement and Advance Cursor

## Completed Tasks

```text
6.3.5 Mark rows sent on success
6.3.6 Advance the cursor
```

## Files Changed

```text
POS.Desktop/Services/Sync/SyncAckApplyResult.cs
POS.Desktop/Services/Sync/ISyncAckApplier.cs
POS.Desktop/Services/Sync/EfSyncAckApplier.cs
POS.Desktop/Services/Sync/SyncProcessor.cs
POS.Desktop/Configuration/DesktopHostBuilder.cs
POS.Desktop.Tests/Services/Sync/SyncAckApplierTests.cs
POS.Desktop.Tests/Services/Sync/SyncProcessorTests.cs
docs/antigravity-context/POS_DESKTOP_CURRENT_CONTEXT.md
```

## What Was Built

Group 4 mein successful central ack ke baad local durable state close ki gayi:

```text
Pending rows → Acked
AckedOn set
ChunkSequence set
SyncCursor advanced
```

### Code Snippet — Ack Applier Interface

```csharp
public interface ISyncAckApplier
{
    Task<SyncAckApplyResult> ApplySuccessAsync(
        SyncOutboxBatch batch,
        SyncIngestRequest request,
        SyncIngestResponse response,
        CancellationToken cancellationToken = default);
}
```

### Explanation

**Yeh kya karta hai?**  
Central API response ko local DB state mein apply karta hai.

**Kyun separate service banayi?**  
`SyncProcessor` singleton hosted service hai. DB work scoped service mein rehna chahiye. Yeh clean architecture aur safe DI pattern hai.

### Code Snippet — Full Received Ack Validation

```csharp
if (response.ChunkSequence != request.ChunkSequence)
{
    return SyncAckApplyResult.Failed("SEQUENCE_MISMATCH", ...);
}

if (response.ChunkIdempotencyKey != request.ChunkIdempotencyKey)
{
    return SyncAckApplyResult.Failed("IDEMPOTENCY_KEY_MISMATCH", ...);
}

if (!string.Equals(response.Status, "Received", StringComparison.OrdinalIgnoreCase))
{
    return SyncAckApplyResult.Failed("INVALID_RESPONSE_STATUS", ...);
}
```

### Code Snippet — Event Ack Matching

```csharp
foreach (var ev in request.Events)
{
    if (!ackMap.TryGetValue(ev.EventId, out var ack) ||
        ack.IdempotencyKey != ev.IdempotencyKey ||
        ack.TerminalSequence != ev.TerminalSequence)
    {
        return SyncAckApplyResult.Failed("EVENT_ACK_MISMATCH", ...);
    }
}
```

### Explanation

**Yeh kya karta hai?**  
Har event ka central ack match karta hai.

**Kyun?**  
Agar server ne kisi aur event ka ack bhej diya, ya partial/mismatch response aaya, to local row galti se Acked nahi honi chahiye.

**Design decision:**  
Group 4 mein **all-or-nothing** apply use hua:

```text
Agar full chunk valid hai → all matching rows Acked
Agar mismatch hai → koi row update nahi
```

### Code Snippet — Transaction Boundary

```csharp
using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

try
{
    var dbRows = await _db.SyncOutbox
        .Where(x => batchIds.Contains(x.Id) &&
                    x.LocationId == locationId &&
                    x.TerminalId == terminalId &&
                    x.Status == SyncOutboxStatus.Pending &&
                    x.IsActive)
        .ToListAsync(cancellationToken);

    // validate rows
    // update rows
    // update cursor

    await _db.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
}
catch
{
    await transaction.RollbackAsync(cancellationToken);
    throw;
}
```

### Explanation

**Yeh kya karta hai?**  
Rows aur cursor dono ek transaction mein update hote hain.

**Kyun?**  
Agar rows Acked ho jayein lekin cursor update fail ho jaye, sync state inconsistent ho sakti hai. Transaction se dono ya to saath save hotay hain, ya rollback.

### Code Snippet — Mark Rows Acked

```csharp
foreach (var row in dbRows)
{
    row.Status = SyncOutboxStatus.Acked;
    row.AckedOn = now;
    row.ChunkSequence = response.ChunkSequence;
    row.LastErrorCode = null;
    row.LastErrorMessage = null;
    row.UpdatedBy = "sync-processor";
    row.UpdatedOn = now;
}
```

### Explanation

**Yeh kya karta hai?**  
Successfully received events ko local SQLite mein synced/acked mark karta hai.

**Kyun?**  
Taake woh dobara pending batch mein na aayein.

**Important:**  
Group 4 ne yeh intentionally update nahi kiya:

```text
AttemptCount
LastAttemptOn
RetainUntil
```

Yeh retry/backoff/quarantine milestone 6.4 ka kaam hai.

### Code Snippet — Cursor Update

```csharp
var cursor = await _db.SyncCursors
    .FirstOrDefaultAsync(x =>
        x.TerminalId == terminalId &&
        x.StreamName == "push:outbox",
        cancellationToken);

cursor.LastPushedChunkSequence =
    Math.Max(cursor.LastPushedChunkSequence ?? 0, response.ChunkSequence);

cursor.LastAckedChunkSequence =
    Math.Max(cursor.LastAckedChunkSequence ?? 0, response.ChunkSequence);
```

### Explanation

**Yeh kya karta hai?**  
Push sync progress ko cursor mein save karta hai.

**Kyun monotonic max use hua?**  
Cursor kabhi peeche nahi jana chahiye. Agar existing value higher hai, woh hi rakhi jati hai.

**StreamName:**

```text
push:outbox
```

Yeh stable stream name hai for local outbox push sync.

---

# Group 5 — Processor Pipeline Verification

## Completed Task

```text
6.3.10 Test events reach central
```

## Files Changed

```text
POS.Desktop.Tests/Services/Sync/SyncProcessorPipelineIntegrationTests.cs
docs/antigravity-context/POS_DESKTOP_CURRENT_CONTEXT.md
```

## What Was Built

Group 5 mein final proof add hua ke actual processor pipeline kaam kar rahi hai.

### Test Flow

```text
1. In-memory SQLite database create hoti hai
2. Pending SyncOutbox row seed hoti hai
3. Real SyncProcessor start hota hai
4. Real EfSyncOutboxBatchReader row read karta hai
5. Real SyncIngestRequestBuilder request banata hai
6. Fake central-style ISyncIngestClient request capture karta hai
7. Fake client Received response return karta hai
8. Real EfSyncAckApplier row Acked karta hai
9. SyncCursor advance hota hai
10. Test assert karta hai ke pending batch empty ho chuka hai
```

### Code Snippet — Fake Central-Style Client

```csharp
private sealed class CapturingSyncIngestClient : ISyncIngestClient
{
    public int CallCount { get; private set; }
    public SyncIngestRequest? CapturedRequest { get; private set; }

    public Task<SyncIngestClientResult> IngestAsync(
        SyncIngestRequest request,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        CapturedRequest = request;

        var acks = request.Events
            .Select(ev => new SyncIngestEventAck(
                ev.EventId,
                ev.IdempotencyKey,
                ev.TerminalSequence,
                "Received",
                null,
                null))
            .ToList();

        var response = new SyncIngestResponse(
            AckId: Guid.NewGuid(),
            ChunkSequence: request.ChunkSequence,
            ChunkIdempotencyKey: request.ChunkIdempotencyKey,
            Status: "Received",
            EventCount: request.Events.Count,
            Events: acks,
            ErrorCode: null,
            ErrorMessage: null);

        return Task.FromResult(SyncIngestClientResult.Succeeded(response));
    }
}
```

### Explanation

**Yeh kya karta hai?**  
Real API call ki jagah fake central response deta hai.

**Kyun?**  
Test fast, deterministic, aur stable rehta hai. Real API/JWT/TestServer complexity avoid hui.

**Agar real API test yahan add karte to kya risk tha?**

```text
- auth/device token setup complexity
- slow/flaky test
- central DB dependency
- CI reliability issue
```

### Code Snippet — Wait for Ack Commit

```csharp
var ackResult = await ackTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
await processor.StopAsync(CancellationToken.None);
```

### Explanation

**Yeh kya karta hai?**  
Test random sleep par depend nahi karta. It waits until ack applier transaction complete hoti hai.

### Code Snippet — Final Assertions

```csharp
Assert.Equal(1, fakeClient.CallCount);
Assert.NotNull(fakeClient.CapturedRequest);

Assert.Equal(SyncOutboxStatus.Acked, dbRow.Status);
Assert.NotNull(dbRow.AckedOn);
Assert.Equal(req.ChunkSequence, dbRow.ChunkSequence);

Assert.NotNull(cursor);
Assert.True(cursor.LastPushedChunkSequence >= req.ChunkSequence);
Assert.True(cursor.LastAckedChunkSequence >= req.ChunkSequence);

var remainingBatch = await reader.ReadPendingBatchAsync();
Assert.False(remainingBatch.HasItems);
```

### Explanation

**Yeh prove karta hai:**

```text
- processor ne event post kiya
- request central-style client tak pohanchi
- local row Acked hui
- cursor advance hua
- row dobara pending batch mein nahi aayi
```

---

# Complete Runtime Demonstration

## Example Scenario

Ek sale complete hoti hai aur `SyncOutbox` mein row ban jati hai:

```text
TenantId = 1
LocationId = 10
TerminalId = 20
BusinessDate = 2026-05-30
TerminalSequence = 100
EventType = OrderCompleted
Status = Pending
IsActive = true
```

## Step 1 — Processor Wakes Up

```text
SyncProcessor checks:
- terminal provisioned hai?
- options valid hain?
- pending outbox rows hain?
```

## Step 2 — Reader Selects Row

```text
EfSyncOutboxBatchReader finds:
Status = Pending
IsActive = true
LocationId = 10
TerminalId = 20
```

## Step 3 — Request Builder Creates Request

```text
ChunkSequence = 100
ChunkIdempotencyKey = chunk:1:10:20:20260530:100:100:1:<hash>
CorrelationId = sync-chunk-<hash>
RequestHash = SHA256(canonical request content)
Events = 1 OrderCompleted event
```

## Step 4 — Client Posts Batch

```text
ISyncIngestClient.IngestAsync(request)
```

Central-style response:

```text
Status = Received
EventCount = 1
EventAck = Received
ChunkSequence = same as request
ChunkIdempotencyKey = same as request
```

## Step 5 — Ack Applier Validates Response

Checks:

```text
response.ChunkSequence == request.ChunkSequence
response.ChunkIdempotencyKey == request.ChunkIdempotencyKey
response.Status == Received
every event has matching EventId + IdempotencyKey + TerminalSequence
```

## Step 6 — Local DB Updates

`SyncOutbox` row:

```text
Status: Pending → Acked
AckedOn: set
ChunkSequence: set
LastErrorCode: null
LastErrorMessage: null
```

`SyncCursor`:

```text
StreamName = push:outbox
LastPushedChunkSequence = 100
LastAckedChunkSequence = 100
Status = Active
```

## Step 7 — Same Row Is Not Picked Again

Next batch query only picks:

```text
Status == Pending
```

Since row is now:

```text
Status == Acked
```

It disappears from pending batch.

---

# Why This Design Is Good for Real POS System

## 1. UI freeze nahi hoti

Sync background mein `BackgroundService` ke through run hota hai. POS sale screen fast rehti hai.

## 2. Offline-first support strong hota hai

Agar internet/API temporarily down ho, data local SQLite mein `Pending` rehta hai. User sale continue kar sakta hai.

## 3. Duplicate safe design

Deterministic keys/hash use hue:

```text
ChunkIdempotencyKey
CorrelationId
RequestHash
PayloadHash
IdempotencyKey
```

Isse duplicate/retry scenarios safer hain.

## 4. Local state reliable hai

Acked rows aur cursor same transaction mein update hotay hain. Half-synced state ka risk kam hota hai.

## 5. Test coverage strong hai

Testing layers:

```text
Options tests
Batch reader tests
Request builder tests
Ack applier transaction tests
Processor lifecycle tests
Full processor pipeline integration tests
Static async safety tests
Full solution tests
```

## 6. Future milestones ke liye ready

Milestone 6.4 retry/backoff/quarantine ke liye base ready hai:

```text
LastErrorCode
LastErrorMessage
AttemptCount
LastAttemptOn
Failed/DeadLetter statuses
ServerBackoffUntil
```

Fields already exist, but 6.3 mein intentionally failure accounting implement nahi hua.

---

# Important Design Decisions

## Decision 1 — Direct DbContext in SyncProcessor nahi diya

**Why?**  
`SyncProcessor` hosted service singleton hota hai. `PosLocalDbContext` scoped hota hai. Direct inject karna lifetime issue bana sakta tha.

**Solution:**  
`IServiceScopeFactory.CreateAsyncScope()` use hua.

```csharp
await using var scope = _scopeFactory.CreateAsyncScope();
var reader = scope.ServiceProvider.GetRequiredService<ISyncOutboxBatchReader>();
var builder = scope.ServiceProvider.GetRequiredService<ISyncIngestRequestBuilder>();
var client = scope.ServiceProvider.GetRequiredService<ISyncIngestClient>();
var ackApplier = scope.ServiceProvider.GetRequiredService<ISyncAckApplier>();
```

## Decision 2 — Batch reader read-only DTO return karta hai

Tracked EF entities expose nahi hoti. Isse accidental mutation avoid hoti hai.

## Decision 3 — Request builder pure/deterministic hai

Builder DB/network/time/random use nahi karta. Same batch → same request identity.

## Decision 4 — Ack apply all-or-nothing hai

Partial/mismatch response par local DB update nahi hoti. Yeh safer hai.

## Decision 5 — Cursor monotonic hai

Cursor kabhi regress nahi karta.

```csharp
LastPushedChunkSequence = Math.Max(existingValue, response.ChunkSequence);
LastAckedChunkSequence = Math.Max(existingValue, response.ChunkSequence);
```

## Decision 6 — Real API-backed E2E defer kiya

Group 5 ne fake central-style client use kiya because current milestone desktop pipeline proof tha. API ingest behavior 6.1 mein already covered tha.

---

# Verification Summary

Final verified results before Group 5 commit:

```text
Services.Sync tests: 118/118 passed
SyncStaticAnalysisTests: 1/1 passed
Full solution tests: 638/638 passed
git diff --check: clean except LF/CRLF warnings
```

## Main Commands Used

```powershell
dotnet build POS.slnx --configuration Debug

dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug --filter "FullyQualifiedName~Services.Sync"

dotnet test POS.Desktop.Tests/POS.Desktop.Tests.csproj --configuration Debug --filter "FullyQualifiedName~SyncStaticAnalysisTests"

dotnet test POS.slnx --configuration Debug

git diff --check
```

---

# Commit Summary

## Group 1

```text
8a7fa32 Add sync processor lifecycle foundation
```

Added processor foundation, config, hosted service registration, shutdown/cancellation basics.

## Group 2

```text
d3b63a7 Add sync outbox batch reader
```

Added read-only batch reader for Pending SyncOutbox rows.

## Group 3

```text
3f02f72 Post sync outbox batches
```

Added deterministic request builder and posting via `ISyncIngestClient`.

## Group 4

```text
ec5e855 Apply sync ingest acknowledgements
```

Added ack applier, rows Acked update, and SyncCursor advancement.

## Group 5

```text
4aa614f Add sync processor pipeline verification
```

Added processor-driven integration test proving complete desktop push pipeline.

---

# Final Outcome

Milestone 6.3 ka final result:

```text
Local POS sale/outbox event can now be:
- read from SyncOutbox
- batched safely
- converted to deterministic SyncIngestRequest
- posted to central ingest client
- acknowledged locally
- marked Acked
- tracked via SyncCursor
- verified through integration tests
```

## Next Recommended Milestone

```text
Phase 6 / Milestone 6.4 — Retry / Backoff / Quarantine
```

Milestone 6.4 mein next focus hona chahiye:

```text
- failed post attempt tracking
- AttemptCount / LastAttemptOn update
- LastErrorCode / LastErrorMessage on failures
- exponential backoff
- ServerBackoffUntil
- Failed / DeadLetter handling
- poison batch quarantine
```

---

# Quick Learning Recap

## SyncOutbox

Local queue hai jahan POS events pending state mein save hotay hain.

## SyncProcessor

Background worker hai jo queue ko process karta hai.

## BatchReader

Pending events uthata hai.

## RequestBuilder

Events ko API request mein convert karta hai.

## SyncIngestClient

Central API ko request bhejta hai.

## AckApplier

Central ack ko local DB state mein apply karta hai.

## SyncCursor

Sync progress yaad rakhta hai.

## Pipeline Test

Proof hai ke full chain kaam kar rahi hai.

---

## End Result in One Line

**Milestone 6.3 ne POS Desktop ko local pending sales/outbox events ko background mein central sync flow tak safely push karne, ack karne, cursor update karne, aur verify karne ki capability de di.**
