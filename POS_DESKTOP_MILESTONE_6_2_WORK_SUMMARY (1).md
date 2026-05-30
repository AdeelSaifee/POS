# POS Desktop — Milestone 6.2 Complete Work Summary

**Milestone:** Phase 6 / Milestone 6.2 — Device-authenticated HTTP client  
**Status:** 100% Complete  
**Completed Tasks:** 6.2.1 to 6.2.10  
**Next Milestone:** Phase 6 / Milestone 6.3 — Outbox drain processor

---

## 1. Simple Step-by-Step Flow

Milestone 6.2 ka main goal yeh tha ke **POS.Desktop** ke andar ek safe HTTP sync client foundation ban jaye jo future mein local SQLite outbox events ko central API ke `/api/sync/ingest` endpoint par bhej sake.

Flow simple words mein:

```text
POS.Desktop appsettings.json
        ↓
SyncClientOptions
        ↓
ISyncIngestClient contract
        ↓
IDeviceTokenProvider token source
        ↓
SyncIngestClient
        ↓
POST /api/sync/ingest
        ↓
Central API returns SyncIngestResponse
        ↓
Desktop receives typed SyncIngestClientResult
```

Real POS analogy:

```text
Cashier terminal = local POS machine
Central API = head office server
SyncIngestClient = courier/rider
Device token = rider ka ID card
SyncIngestRequest = parcel/batch
SyncIngestResponse = receiving slip/acknowledgement
```

Agar courier ke paas ID card nahi hai, parcel nahi bheja jata. Agar network down hai, app crash nahi hoti; typed error return hota hai.

---

## 2. Overall Milestone 6.2 Grouping

Milestone 6.2 ko 4 groups mein complete kiya gaya:

| Group | Tasks | Purpose | Status |
|---|---|---|---|
| Group 1 | 6.2.1–6.2.2 | Config + interfaces + result/error models | Complete |
| Group 2 | 6.2.3–6.2.5 | HTTP client + token provider + refresh shape | Complete |
| Group 3 | 6.2.6–6.2.8 | DI registration + timeout/failure hardening | Complete |
| Group 4 | 6.2.9–6.2.10 | Smoke verification + async/no UI-blocking check | Complete |

---

## 3. Task Completion Checklist

```text
[x] 6.2.1  Add API base URL config
[x] 6.2.2  Define the sync client interface
[x] 6.2.3  Implement the client
[x] 6.2.4  Acquire a device token
[x] 6.2.5  Implement token refresh
[x] 6.2.6  Register the typed HttpClient
[x] 6.2.7  Map failures to typed results
[x] 6.2.8  Handle timeouts/skew
[x] 6.2.9  Smoke test ingest call
[x] 6.2.10 Ensure no UI-thread blocking
```

---

# Group 1 — Config + Interface Foundation

## 4. Task 6.2.1 — Add API Base URL Config

### File Changed

```text
POS.Desktop/appsettings.json
```

### Added Config

```json
"Sync": {
  "ApiBaseUrl": "https://localhost:5001",
  "IngestPath": "/api/sync/ingest",
  "TimeoutSeconds": 15,
  "ClockSkewSeconds": 300
}
```

### Yeh kya karta hai?

Yeh desktop app ko batata hai:

```text
Central API kahan hai?
Sync endpoint ka route kya hai?
Network call kitni der wait kare?
Token time difference / clock skew tolerance kitni hai?
```

### Kyun banaya gaya?

Hard-coded URLs avoid karne ke liye. Real POS system mein API URL environment ke hisaab se change ho sakta hai:

```text
Development: https://localhost:5001
Staging:     https://staging-api.company.com
Production:  https://api.company.com
```

### Agar na karte to kya hota?

Client code mein direct URL hardcode hota. Future deployment ke time har environment ke liye code change karna padta.

### Safe hai ya risky?

Safe hai because:

```text
No JWT token
No password
No signing key
No device secret
```

### Real POS faida

Har branch/store terminal ko same app build mil sakta hai, bas config change hogi.

---

## 5. SyncClientOptions

### File Added

```text
POS.Desktop/Services/Sync/SyncClientOptions.cs
```

### Important Snippet

```csharp
public sealed class SyncClientOptions
{
    public string? ApiBaseUrl { get; set; }
    public string IngestPath { get; set; } = "/api/sync/ingest";
    public int TimeoutSeconds { get; set; } = 15;
    public int ClockSkewSeconds { get; set; } = 300;

    public bool Validate(out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(ApiBaseUrl))
        {
            errorMessage = "ApiBaseUrl cannot be null, empty, or whitespace.";
            return false;
        }

        if (!Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out var uriResult) ||
            (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
        {
            errorMessage = "ApiBaseUrl must be a valid absolute URI starting with http:// or https://.";
            return false;
        }

        return true;
    }
}
```

### Yeh kya karta hai?

Yeh config ko C# object mein convert karta hai aur validate karta hai.

### Kyun banaya gaya?

Taake sync client clean tareeqe se settings use kare. Har jagah string keys use na karni paren.

### Validation kya check karti hai?

```text
ApiBaseUrl blank na ho
ApiBaseUrl valid http/https URI ho
IngestPath blank na ho
IngestPath / se start ho
Timeout 1–300 seconds ke andar ho
ClockSkew 0–1800 seconds ke andar ho
```

### Real POS faida

Agar store config galat ho, terminal crash nahi karega; sync operation typed configuration error dega.

---

## 6. Task 6.2.2 — Define Sync Client Interface

### File Added

```text
POS.Desktop/Services/Sync/ISyncIngestClient.cs
```

### Snippet

```csharp
public interface ISyncIngestClient
{
    Task<SyncIngestClientResult> IngestAsync(
        SyncIngestRequest request,
        CancellationToken cancellationToken = default);
}
```

### Yeh kya karta hai?

Yeh contract define karta hai ke desktop sync client ka kaam kya hoga:

```text
SyncIngestRequest lo
Central API ko bhejo
SyncIngestClientResult return karo
```

### Kyun banaya gaya?

Interface se code testable aur replaceable hota hai. Future mein real implementation, fake implementation, ya test implementation easily swap ho sakti hai.

### Agar na karte to kya hota?

Business/service layer direct concrete `SyncIngestClient` par depend karti. Testing aur future refactoring mushkil hoti.

### Safe hai ya risky?

Safe. Yeh sirf contract hai, koi network call ya secret nahi.

---

## 7. SyncIngestClientResult

### File Added

```text
POS.Desktop/Services/Sync/SyncIngestClientResult.cs
```

### Snippet

```csharp
public sealed class SyncIngestClientResult
{
    public bool Success { get; }
    public SyncIngestResponse? Response { get; }
    public SyncIngestClientError? Error { get; }

    public static SyncIngestClientResult Succeeded(SyncIngestResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new SyncIngestClientResult(true, response, null);
    }

    public static SyncIngestClientResult Failed(SyncIngestClientError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new SyncIngestClientResult(false, null, error);
    }
}
```

### Yeh kya karta hai?

Network/API result ko safe structure mein return karta hai:

```text
Success true  → Response available
Success false → Error available
```

### Kyun banaya gaya?

Network failure ko exception ke form mein UI tak nahi bhejna. UI ya future SyncProcessor clean result dekh kar decision le sakta hai.

### Agar na karte to kya hota?

Har caller ko try/catch likhna padta. Network errors app crash bhi kar sakte thay.

### Real POS faida

Cashier terminal agar offline ho to app crash nahi hoti. System keh sakta hai:

```text
Sync pending hai, network unavailable hai.
```

---

## 8. SyncIngestClientError

### File Added

```text
POS.Desktop/Services/Sync/SyncIngestClientError.cs
```

### Error Categories

```csharp
public enum SyncIngestClientErrorType
{
    None = 0,
    Configuration = 1,
    Offline = 2,
    Timeout = 3,
    Unauthorized = 4,
    Forbidden = 5,
    Conflict = 6,
    Validation = 7,
    ServerError = 8,
    Unexpected = 9
}
```

### Yeh kya karta hai?

Har failure ko category deta hai:

| Error Type | Meaning |
|---|---|
| Configuration | appsettings/config issue |
| Offline | network/server unreachable |
| Timeout | request time out |
| Unauthorized | token missing/invalid |
| Forbidden | device claims/permission issue |
| Conflict | idempotency/sequence conflict |
| Validation | bad/mismatched request |
| ServerError | API side issue |
| Unexpected | unknown client-side issue |

### Kyun banaya gaya?

Future UI/SyncProcessor ko readable decision milta hai:

```text
Offline → retry later
Unauthorized → device login/token issue
Conflict → data consistency issue
Validation → bad request/report developer
```

### Safe hai ya risky?

Safe because raw exception details expose nahi kiye gaye.

---

# Group 2 — HTTP Client + Token Provider

## 9. Task 6.2.3 — Implement SyncIngestClient

### File Added

```text
POS.Desktop/Services/Sync/SyncIngestClient.cs
```

### Client Flow

```text
IngestAsync(request)
    ↓
request null check
    ↓
options validation
    ↓
token provider se token lo
    ↓
absolute URI banao
    ↓
Authorization: Bearer <token> header lagao
    ↓
POST JSON to /api/sync/ingest
    ↓
200 OK → deserialize SyncIngestResponse
    ↓
non-200/error → typed SyncIngestClientError
```

### Important Snippet

```csharp
if (!_options.Validate(out var optionsError))
{
    return SyncIngestClientResult.Failed(new SyncIngestClientError(
        SyncIngestClientErrorType.Configuration,
        optionsError ?? "Invalid sync client configuration options.",
        "INVALID_CONFIGURATION"));
}

var tokenResult = await _tokenProvider.GetTokenAsync(cancellationToken)
    .ConfigureAwait(false);

if (tokenResult is null || !tokenResult.Success || string.IsNullOrWhiteSpace(tokenResult.Token))
{
    return SyncIngestClientResult.Failed(new SyncIngestClientError(
        SyncIngestClientErrorType.Unauthorized,
        tokenResult?.ErrorMessage ?? "Device authentication token acquisition failed.",
        "TOKEN_ACQUISITION_FAILED"));
}
```

### Authorization Snippet

```csharp
using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri);
httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Token);
httpRequest.Content = JsonContent.Create(request, typeof(SyncIngestRequest), mediaType: null, SerializerOptions);

using var responseMessage = await _httpClient.SendAsync(httpRequest, cancellationToken)
    .ConfigureAwait(false);
```

### Yeh kya karta hai?

Desktop client ko central API se connect karta hai. Yeh local outbox batch ko central ingest endpoint par bhejne ka base client hai.

### Kyun banaya gaya?

Milestone 6.3 mein jab SyncOutbox drain processor banega, usko central API se baat karne ke liye isi client ki zaroorat hogi.

### Agar na karte to kya hota?

SyncOutbox future mein ready hota, lekin central API ko call karne ka reliable client nahi hota.

### Safe hai ya risky?

Safe design because:

```text
No raw exception leak
No token logging
No full payload logging
No server signing key in desktop
No local JWT generation
Network errors typed result ban jate hain
```

---

## 10. HTTP Failure Mapping

### Important Snippet

```csharp
return responseMessage.StatusCode switch
{
    HttpStatusCode.BadRequest => SyncIngestClientResult.Failed(new SyncIngestClientError(
        SyncIngestClientErrorType.Validation,
        "The server rejected the sync batch as malformed or mismatched with device identity.",
        "BAD_REQUEST")),

    HttpStatusCode.Unauthorized => SyncIngestClientResult.Failed(new SyncIngestClientError(
        SyncIngestClientErrorType.Unauthorized,
        "The central server rejected the device credentials (invalid or expired token).",
        "UNAUTHORIZED")),

    HttpStatusCode.Forbidden => SyncIngestClientResult.Failed(new SyncIngestClientError(
        SyncIngestClientErrorType.Forbidden,
        "The device has insufficient permissions or missing tenant/location/terminal claims.",
        "FORBIDDEN")),

    HttpStatusCode.Conflict => SyncIngestClientResult.Failed(new SyncIngestClientError(
        SyncIngestClientErrorType.Conflict,
        "The sync batch conflicted with an existing key, terminal sequence, or batch duplicates.",
        "CONFLICT")),

    _ => SyncIngestClientResult.Failed(new SyncIngestClientError(
        SyncIngestClientErrorType.Unexpected,
        $"The central server returned an unmapped status code: {(int)responseMessage.StatusCode}",
        $"HTTP_ERROR_{(int)responseMessage.StatusCode}"))
};
```

### Demonstration

| API Response | Client Result |
|---|---|
| 200 OK | Success + SyncIngestResponse |
| 400 BadRequest | Validation |
| 401 Unauthorized | Unauthorized |
| 403 Forbidden | Forbidden |
| 409 Conflict | Conflict |
| 500/501 | ServerError |
| Network fail | Offline |
| Timeout | Timeout |
| Bad JSON | Unexpected |

### Real POS faida

Central server down ho, token expire ho, ya duplicate batch ho — har case mein system understandable response deta hai.

---

## 11. Exception Masking

### Snippet

```csharp
catch (Exception)
{
    return SyncIngestClientResult.Failed(new SyncIngestClientError(
        SyncIngestClientErrorType.Unexpected,
        "An unexpected connection error occurred while contacting the central API.",
        "UNEXPECTED_EXCEPTION"));
}
```

### Yeh kyun important hai?

Raw `ex.Message` UI ya result mein leak nahi hota. Is se sensitive internal details expose nahi hoti.

### Agar na karte to kya hota?

Exception message mein server name, path, token-related hints, ya internal implementation details aa sakti thi.

---

## 12. Task 6.2.4 / 6.2.5 — Token Provider Contract + Refresh Shape

### File Modified/Added

```text
POS.Desktop/Services/Sync/IDeviceTokenProvider.cs
POS.Desktop/Services/Sync/FixedDeviceTokenProvider.cs
```

### IDeviceTokenProvider Snippet

```csharp
public sealed record DeviceTokenResult(
    bool Success,
    string? Token = null,
    string? ErrorMessage = null,
    DateTimeOffset? ExpiresAtUtc = null
);

public interface IDeviceTokenProvider
{
    Task<DeviceTokenResult> GetTokenAsync(CancellationToken cancellationToken = default);
    Task<DeviceTokenResult> ForceRefreshAsync(CancellationToken cancellationToken = default);
}
```

### FixedDeviceTokenProvider Snippet

```csharp
public sealed class FixedDeviceTokenProvider : IDeviceTokenProvider
{
    public Task<DeviceTokenResult> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_token))
        {
            return Task.FromResult(new DeviceTokenResult(false, null, "Device token is missing or blank."));
        }

        if (_expiresAtUtc.HasValue && _expiresAtUtc.Value <= DateTimeOffset.UtcNow)
        {
            return Task.FromResult(new DeviceTokenResult(false, null, "Device token has expired."));
        }

        return Task.FromResult(new DeviceTokenResult(true, _token, null, _expiresAtUtc));
    }
}
```

### Yeh kya karta hai?

Token lene ka abstraction banata hai. Real API token endpoint abhi nahi hai, isliye production token generation nahi ki gayi.

### Kyun banaya gaya?

Future mein real device token endpoint add hoga. Tab sirf token provider implementation replace hogi, sync client ka contract stable rahega.

### Important Safety Decision

```text
POS.Desktop mein server JWT signing key nahi rakhi gayi.
POS.Desktop local JWT generate nahi karta.
appsettings.json mein static token nahi dala gaya.
```

### Real POS faida

Agar future mein device login/provisioning system badalta hai, sync client ko rewrite nahi karna padega.

---

# Group 3 — DI Registration + Timeout Hardening

## 13. Task 6.2.6 — Register Typed HttpClient

### File Modified

```text
POS.Desktop/Configuration/DesktopHostBuilder.cs
```

### Snippet

```csharp
services.Configure<SyncClientOptions>(hostContext.Configuration.GetSection("Sync"));
services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<SyncClientOptions>>().Value);
services.AddSingleton<IDeviceTokenProvider, UnconfiguredDeviceTokenProvider>();
services.AddHttpClient<ISyncIngestClient, SyncIngestClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<SyncClientOptions>();

    if (Uri.TryCreate(options.ApiBaseUrl, UriKind.Absolute, out var baseUri) &&
        (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps))
    {
        client.BaseAddress = baseUri;
    }

    var timeoutSecs = options.TimeoutSeconds is > 0 and <= 300
        ? options.TimeoutSeconds
        : 15;

    client.Timeout = TimeSpan.FromSeconds(timeoutSecs);
});
```

### Yeh kya karta hai?

Desktop app ke dependency injection container mein sync services register karta hai:

```text
SyncClientOptions
IDeviceTokenProvider
ISyncIngestClient via HttpClientFactory
```

### Kyun banaya gaya?

Real app runtime mein service resolve kar sake:

```csharp
var client = serviceProvider.GetRequiredService<ISyncIngestClient>();
```

### Agar na karte to kya hota?

Client code exist karta, tests pass hoti, lekin real app DI se `ISyncIngestClient` resolve nahi kar pati.

### Safe hai ya risky?

Safe because:

```text
UnconfiguredDeviceTokenProvider default hai
No fake/mock production token registered
Invalid ApiBaseUrl startup crash nahi karta
Timeout bounded hai
```

---

## 14. UnconfiguredDeviceTokenProvider

### File Added

```text
POS.Desktop/Services/Sync/UnconfiguredDeviceTokenProvider.cs
```

### Snippet

```csharp
public sealed class UnconfiguredDeviceTokenProvider : IDeviceTokenProvider
{
    public Task<DeviceTokenResult> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new DeviceTokenResult(false, null, "Device token source is not configured."));
    }

    public Task<DeviceTokenResult> ForceRefreshAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new DeviceTokenResult(false, null, "Device token refresh source is not configured."));
    }
}
```

### Yeh kya karta hai?

Default DI token provider hai jo clear failure return karta hai jab tak real token source available nahi hota.

### Kyun banaya gaya?

DI resolution fail na ho, lekin production mein fake token bhi na chale.

### Agar na karte to kya hota?

Do risky options hoti:

```text
1. Fake token register karna → bad security/design
2. Token provider register na karna → DI resolution fail
```

Is provider ne dono issues avoid kiye.

---

## 15. Task 6.2.7 — Failure Mapping via DI Boundary

### File Added

```text
POS.Desktop.Tests/Services/Sync/SyncDiResolutionTests.cs
```

### Test Purpose

Tests verify:

```text
ISyncIngestClient DI se resolve hota hai
SyncClientOptions appsettings se bind hoti hai
IDeviceTokenProvider default UnconfiguredDeviceTokenProvider hai
Unconfigured provider typed failure return karta hai
Invalid URL/timeout DI startup crash nahi karte
```

### Real POS faida

App boot ke waqt sync config galat ho bhi jaye, terminal crash nahi karega.

---

## 16. Task 6.2.8 — Timeout/Skew Hardening

### Timeout Rule

```text
If TimeoutSeconds > 0 and <= 300 → use configured value
Otherwise → fallback 15 seconds
```

### Why important?

Agar timeout invalid ho:

```text
0 seconds → immediately fail
999999 seconds → app long hang
negative → exception risk
```

Isliye bounded fallback 15 seconds use kiya.

### ClockSkew

ClockSkewSeconds options mein present hai and validation mein bounded hai. Real clock-skew server verification future real token endpoint ke saath meaningful hogi.

---

# Group 4 — Smoke Verification + No UI-Thread Blocking

## 17. Task 6.2.9 — Smoke Test Ingest Call

### File Added

```text
POS.Tests/IntegrationTests/SyncIngestSmokeTests.cs
```

### Important Design Decision

Pehle idea tha ke `POS.Tests` se desktop client ko directly reference kar ke end-to-end test banaya jaye. Yeh reject kiya gaya because:

```text
POS.Tests API integration project hai
Isko net8.0-windows nahi banana chahiye
Isme POS.Desktop reference nahi hona chahiye
```

Final approach:

```text
POS.Tests remains net8.0
POS.Tests references only POS.Api + POS.Shared
Smoke test direct API-side integration test hai
```

### Smoke Test Snippet

```csharp
using var client = _factory.CreateClient();
using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/sync/ingest");
httpRequest.Content = JsonContent.Create(requestPayload);

var auth = TestRequestAuthentication.Authenticated()
    .WithClaim(ApiClaimTypes.ClientType, "device")
    .WithClaim(ApiClaimTypes.TenantId, "101")
    .WithClaim(ApiClaimTypes.LocationId, terminal.LocationId.ToString())
    .WithClaim(ApiClaimTypes.TerminalId, terminal.Id.ToString())
    .WithClaim(ApiClaimTypes.DeviceId, terminal.DeviceId.ToString());

auth.Apply(httpRequest);

var response = await client.SendAsync(httpRequest);
response.EnsureSuccessStatusCode();
```

### Ack Verification Snippet

```csharp
var responseContent = await response.Content.ReadAsStringAsync();
var syncResponse = JsonSerializer.Deserialize<SyncIngestResponse>(responseContent, JsonSerializerOptions);

Assert.NotNull(syncResponse);
Assert.Equal("Received", syncResponse.Status);
Assert.Equal(requestPayload.ChunkSequence, syncResponse.ChunkSequence);
Assert.Equal(requestPayload.ChunkIdempotencyKey, syncResponse.ChunkIdempotencyKey);
```

### Yeh kya karta hai?

Valid device claims ke saath `/api/sync/ingest` ko POST karta hai aur verify karta hai ke API `Received` ack return karti hai.

### Kyun banaya gaya?

Task 6.2.9 ka requirement tha: sample batch API ko post karo aur ack verify karo.

### Agar na karte to kya hota?

Milestone complete bolne ke bawajood final smoke proof missing hota.

### Safe hai ya risky?

Safe because:

```text
No POS.Desktop reference in POS.Tests
No JWT signing key
No fake bearer parser
No production token
Existing TestRequestAuthentication helper use hua
```

---

## 18. Task 6.2.10 — Ensure No UI-Thread Blocking

### File Added

```text
POS.Desktop.Tests/Services/Sync/SyncStaticAnalysisTests.cs
```

### Snippet

```csharp
var forbiddenPatterns = new[]
{
    ".Result",
    ".Wait(",
    "GetAwaiter().GetResult()"
};
```

### Scan Logic

```csharp
var syncServicesDir = Path.Combine(root, "POS.Desktop", "Services", "Sync");
var csFiles = Directory.GetFiles(syncServicesDir, "*.cs", SearchOption.AllDirectories);

foreach (var file in csFiles)
{
    var lines = File.ReadAllLines(file);
    // comments ignore kiye jate hain
    // actual code mein blocking patterns search hotay hain
}
```

### Yeh kya karta hai?

Production sync service files scan karta hai aur block karta hai agar koi sync-over-async call aa jaye:

```text
.Result
.Wait(
GetAwaiter().GetResult()
```

### Kyun banaya gaya?

WPF desktop app mein UI thread block ho sakta hai agar async network calls ko sync wait se call kiya jaye.

### Agar na karte to kya hota?

Future developer galti se `.Result` laga sakta tha. Cashier screen freeze ho sakti thi.

### Real POS faida

Network slow ho tab bhi UI freeze nahi hogi. Cashier experience stable rahega.

---

# 19. Demonstration — End-to-End Conceptual Flow

## Scenario

Cashier terminal ke local database mein future mein kuch outbox events honge:

```text
OrderCompleted
PaymentCompleted
ShiftClosed
CashDrawerAdjusted
```

Milestone 6.2 ke baad future SyncProcessor yeh flow use karega:

```text
1. SyncProcessor local SyncOutbox se events read karega
2. SyncIngestRequest banega
3. ISyncIngestClient.IngestAsync(request) call hoga
4. Client device token acquire karega
5. Client POST /api/sync/ingest karega
6. API idempotent ack return karegi
7. Client typed Success/Failure result dega
8. Processor outbox state update karega
```

Important: Step 1 and Step 8 **Milestone 6.3** ka kaam hai. 6.2 sirf HTTP client foundation tak limited raha.

---

# 20. What Was Intentionally Not Done

Milestone 6.2 mein yeh cheezen intentionally nahi ki gayi:

```text
No SyncProcessor
No background service
No SyncOutbox drain loop
No UI button wiring
No real device token endpoint
No local JWT generation in desktop
No server signing key in desktop
No production static token in appsettings
No retry/backoff engine
No business table transformation
```

### Kyun nahi kiya?

Scope control ke liye. 6.2 ka kaam HTTP client foundation tha. Outbox drain processor 6.3 ka separate milestone hai.

---

# 21. Security Decisions

## 21.1 No Signing Key in Desktop

Desktop app ko server JWT signing key nahi di gayi.

### Why?

Agar signing key desktop app mein hoti, koi bhi fake token generate kar sakta tha.

## 21.2 No Static Production Token in appsettings

`appsettings.json` mein token/secret/password/signing key add nahi hui.

## 21.3 Safe Token Provider Boundary

`IDeviceTokenProvider` abstraction banaya gaya. Future real token endpoint aaye to implementation swap hogi.

## 21.4 No Raw Exception Leaks

Unexpected exception safe generic message return karti hai:

```text
An unexpected connection error occurred while contacting the central API.
```

---

# 22. Verification Summary

## Group 1

```text
dotnet build POS.slnx --configuration Debug
PASS: 0 errors / 0 warnings

Sync tests
PASS: 22/22
```

## Group 2

```text
Sync tests
PASS: 44/44

Full solution
PASS: 563/563
```

## Group 3

```text
Sync tests
PASS: 50/50

Full solution
PASS: 569/569
```

## Group 4

```text
Desktop sync tests
PASS: 51/51

API smoke test
PASS: 1/1

Full solution
PASS: 571/571
Desktop: 502
API: 69
```

---

# 23. Complete Files Added/Modified in Milestone 6.2

## Group 1 Files

```text
MODIFY POS.Desktop/appsettings.json
ADD    POS.Desktop/Services/Sync/SyncClientOptions.cs
ADD    POS.Desktop/Services/Sync/ISyncIngestClient.cs
ADD    POS.Desktop/Services/Sync/SyncIngestClientError.cs
ADD    POS.Desktop/Services/Sync/SyncIngestClientResult.cs
ADD    POS.Desktop/Services/Sync/IDeviceTokenProvider.cs
ADD    POS.Desktop.Tests/Services/Sync/SyncClientOptionsTests.cs
```

## Group 2 Files

```text
ADD/MODIFY POS.Desktop/Services/Sync/SyncIngestClient.cs
ADD        POS.Desktop/Services/Sync/FixedDeviceTokenProvider.cs
MODIFY     POS.Desktop/Services/Sync/IDeviceTokenProvider.cs
ADD        POS.Desktop.Tests/Services/Sync/DeviceTokenProviderTests.cs
ADD        POS.Desktop.Tests/Services/Sync/SyncIngestClientTests.cs
```

## Group 3 Files

```text
MODIFY POS.Desktop/Configuration/DesktopHostBuilder.cs
MODIFY POS.Desktop/POS.Desktop.csproj
ADD    POS.Desktop/Services/Sync/UnconfiguredDeviceTokenProvider.cs
ADD    POS.Desktop.Tests/Services/Sync/SyncDiResolutionTests.cs
```

## Group 4 Files

```text
ADD POS.Tests/IntegrationTests/SyncIngestSmokeTests.cs
ADD POS.Desktop.Tests/Services/Sync/SyncStaticAnalysisTests.cs
```

## Context File

```text
MODIFY docs/antigravity-context/POS_DESKTOP_CURRENT_CONTEXT.md
```

---

# 24. Simple Explanation by Question Style

## Yeh kya karta hai?

Desktop app ko central API ke sync ingest endpoint se baat karne ka proper HTTP client infrastructure deta hai.

## Kyun banaya gaya?

Offline-first POS mein sales/orders local save hoti hain. Baad mein central server ko sync karni hoti hain. Us sync ke liye reliable HTTP client foundation zaroori hai.

## Agar na karte to kya hota?

Milestone 6.3 ka SyncOutbox drain processor central API ko safely call nahi kar pata.

## Safe hai ya risky?

Mostly safe, because:

```text
No secrets in appsettings
No signing key in desktop
No production fake token
No raw exception leak
No UI blocking patterns
Typed errors instead of raw exceptions
```

Risk jo abhi intentionally deferred hai:

```text
Real device token endpoint abhi available nahi
Actual outbox drain abhi start nahi hua
Real UI-trigger test future mein hoga
```

## Real POS system mein iska faida?

```text
Terminal offline/online reliable behave karega
Central sync future mein clean hoga
Network failures crash nahi karengi
Cashier UI freeze nahi hogi
Device-auth security boundary clear rahegi
```

---

# 25. Next Milestone — 6.3 Outbox Drain Processor

Milestone 6.3 ka expected direction:

```text
Local SyncOutbox read karna
Pending events ko chunk mein group karna
ISyncIngestClient se API ko bhejna
Ack receive karna
Local outbox records ko synced/failed mark karna
Retry/backoff policy decide karna
No duplicate posting
No UI blocking
```

Important: 6.3 start karne se pehle high-level planning karni chahiye, direct implementation nahi.

---

# 26. Final Milestone 6.2 Status

```text
Phase 6 / Milestone 6.2 — Device-authenticated HTTP client
Status: COMPLETE
Tasks: 6.2.1 to 6.2.10 complete
Final tests: 571/571 passing
Next: Phase 6 / Milestone 6.3 — Outbox drain processor
```
