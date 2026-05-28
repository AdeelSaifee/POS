using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using POS.Api.Auth;
using POS.Api.Data;
using POS.Shared.Contracts.Sync;
using POS.Shared.Domain.Entities.Central;
using POS.Shared.Enums;
using POS.Tests.Integration;
using POS.Tests.Integration.Auth;
using Xunit;

namespace POS.Tests.IntegrationTests;

public sealed class SyncIngestEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public SyncIngestEndpointTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region Helpers

    private async Task<Terminal> SeedTerminalAsync(string terminalCode)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PosCentralDbContext>();

        var location = await db.Locations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.TenantId == 101 && l.Code == "LOC-101-A");
        Assert.NotNull(location);

        var terminal = new Terminal
        {
            TenantId = 101,
            LocationId = location.Id,
            TerminalCode = terminalCode,
            DeviceId = Guid.NewGuid(),
            DeviceSecretHash = "test-device-secret-hash",
            ProvisioningStatus = TerminalProvisioningStatus.Provisioned,
            IsActive = true,
            CreatedBy = "integration-tests",
            CreatedOn = DateTimeOffset.UtcNow
        };

        db.Terminals.Add(terminal);
        await db.SaveChangesAsync();

        return terminal;
    }

    private static HttpRequestMessage CreateDeviceRequest(
        HttpMethod method,
        string route,
        string? clientType = "device",
        string? tenantId = "101",
        string? locationId = "1",
        string? terminalId = "1",
        string? deviceId = "DEV-01",
        bool isAuthenticated = true)
    {
        var request = new HttpRequestMessage(method, route);
        if (!isAuthenticated)
        {
            return request;
        }

        var auth = TestRequestAuthentication.Authenticated();
        if (clientType != null)
        {
            auth.WithClaim(ApiClaimTypes.ClientType, clientType);
        }
        if (tenantId != null)
        {
            auth.WithClaim(ApiClaimTypes.TenantId, tenantId);
        }
        if (locationId != null)
        {
            auth.WithClaim(ApiClaimTypes.LocationId, locationId);
        }
        if (terminalId != null)
        {
            auth.WithClaim(ApiClaimTypes.TerminalId, terminalId);
        }
        if (deviceId != null)
        {
            auth.WithClaim(ApiClaimTypes.DeviceId, deviceId);
        }

        auth.Apply(request);
        return request;
    }

    private static SyncIngestRequest CreateValidSyncIngestRequest(
        int tenantId,
        int locationId,
        int terminalId,
        long chunkSequence,
        string chunkIdempotencyKey,
        string requestHash,
        string correlationId,
        int eventCount = 1)
    {
        var events = new List<SyncIngestEvent>();
        for (int i = 0; i < eventCount; i++)
        {
            events.Add(new SyncIngestEvent(
                new DateOnly(2026, 5, 28),
                1000 + i,
                "OrderCompleted",
                Guid.NewGuid(),
                $"{{\"orderId\": \"ORD-{Guid.NewGuid():N}\"}}",
                $"ev-hash-{Guid.NewGuid():N}",
                $"ev-idem-{Guid.NewGuid():N}",
                correlationId,
                chunkSequence
            ));
        }

        return new SyncIngestRequest(
            tenantId,
            locationId,
            terminalId,
            chunkSequence,
            chunkIdempotencyKey,
            requestHash,
            correlationId,
            events
        );
    }

    private sealed record TestSyncIngestAckEnvelope(
        SyncIngestRequest Request,
        SyncIngestResponse Response,
        DateTimeOffset ReceivedOn,
        string StatusMeaning
    );

    #endregion

    #region Authorization & Identity Claims Tests

    [Fact]
    public async Task Ingest_NoAuthentication_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        using var request = CreateDeviceRequest(HttpMethod.Post, "/api/sync/ingest", isAuthenticated: false);
        request.Content = JsonContent.Create(CreateValidSyncIngestRequest(101, 1, 1, 1, "idem-1", "hash-1", "corr-1"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Ingest_WrongClientType_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        using var request = CreateDeviceRequest(HttpMethod.Post, "/api/sync/ingest", clientType: "user");
        request.Content = JsonContent.Create(CreateValidSyncIngestRequest(101, 1, 1, 1, "idem-2", "hash-2", "corr-2"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Ingest_MissingTenantIdClaim_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        using var request = CreateDeviceRequest(HttpMethod.Post, "/api/sync/ingest", tenantId: null);
        request.Content = JsonContent.Create(CreateValidSyncIngestRequest(101, 1, 1, 1, "idem-3", "hash-3", "corr-3"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Ingest_MissingLocationIdClaim_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        using var request = CreateDeviceRequest(HttpMethod.Post, "/api/sync/ingest", locationId: null);
        request.Content = JsonContent.Create(CreateValidSyncIngestRequest(101, 1, 1, 1, "idem-4", "hash-4", "corr-4"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Ingest_MissingTerminalIdClaim_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        using var request = CreateDeviceRequest(HttpMethod.Post, "/api/sync/ingest", terminalId: null);
        request.Content = JsonContent.Create(CreateValidSyncIngestRequest(101, 1, 1, 1, "idem-5", "hash-5", "corr-5"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-10")]
    public async Task Ingest_InvalidTenantIdClaim_ReturnsForbidden(string invalidTenantId)
    {
        using var client = _factory.CreateClient();
        using var request = CreateDeviceRequest(HttpMethod.Post, "/api/sync/ingest", tenantId: invalidTenantId);
        request.Content = JsonContent.Create(CreateValidSyncIngestRequest(101, 1, 1, 1, "idem-6", "hash-6", "corr-6"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    public async Task Ingest_InvalidLocationIdClaim_ReturnsForbidden(string invalidLocationId)
    {
        using var client = _factory.CreateClient();
        using var request = CreateDeviceRequest(HttpMethod.Post, "/api/sync/ingest", locationId: invalidLocationId);
        request.Content = JsonContent.Create(CreateValidSyncIngestRequest(101, 1, 1, 1, "idem-7", "hash-7", "corr-7"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task Ingest_InvalidTerminalIdClaim_ReturnsForbidden(string invalidTerminalId)
    {
        using var client = _factory.CreateClient();
        using var request = CreateDeviceRequest(HttpMethod.Post, "/api/sync/ingest", terminalId: invalidTerminalId);
        request.Content = JsonContent.Create(CreateValidSyncIngestRequest(101, 1, 1, 1, "idem-8", "hash-8", "corr-8"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Ingest_NonNumericClaim_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        using var request = CreateDeviceRequest(HttpMethod.Post, "/api/sync/ingest", tenantId: "not-an-int");
        request.Content = JsonContent.Create(CreateValidSyncIngestRequest(101, 1, 1, 1, "idem-9", "hash-9", "corr-9"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Ingest_IdentityClaimsMismatch_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        using var request = CreateDeviceRequest(HttpMethod.Post, "/api/sync/ingest", tenantId: "101", locationId: "1", terminalId: "1");
        request.Content = JsonContent.Create(CreateValidSyncIngestRequest(202, 1, 1, 1, "idem-10", "hash-10", "corr-10"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Happy Path & Replays Tests

    [Fact]
    public async Task Ingest_HappyPath_SavesToDatabaseAndReturnsAck()
    {
        var terminalCode = $"TERM-{Guid.NewGuid():N}";
        var terminal = await SeedTerminalAsync(terminalCode);

        using var client = _factory.CreateClient();
        var requestPayload = CreateValidSyncIngestRequest(
            101,
            terminal.LocationId,
            terminal.Id,
            1,
            $"idem-key-{Guid.NewGuid():N}",
            $"req-hash-{Guid.NewGuid():N}",
            $"corr-{Guid.NewGuid():N}"
        );

        using var request = CreateDeviceRequest(
            HttpMethod.Post,
            "/api/sync/ingest",
            tenantId: "101",
            locationId: terminal.LocationId.ToString(),
            terminalId: terminal.Id.ToString()
        );
        request.Content = JsonContent.Create(requestPayload);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var syncResponse = JsonSerializer.Deserialize<SyncIngestResponse>(responseContent, JsonSerializerOptions);

        Assert.NotNull(syncResponse);
        Assert.Equal("Received", syncResponse.Status);
        Assert.Equal(requestPayload.ChunkSequence, syncResponse.ChunkSequence);
        Assert.Equal(requestPayload.ChunkIdempotencyKey, syncResponse.ChunkIdempotencyKey);
        Assert.Equal(requestPayload.Events.Count, syncResponse.EventCount);
        Assert.Equal(requestPayload.Events.Count, syncResponse.Events.Count);

        // Verify DB persistence
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PosCentralDbContext>();

        var ack = await db.SyncIngestAcks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == syncResponse.AckId);

        Assert.NotNull(ack);
        Assert.Equal("Received", ack.Status);
        Assert.Equal(101, ack.TenantId);
        Assert.Equal(terminal.LocationId, ack.LocationId);
        Assert.Equal(terminal.Id, ack.TerminalId);
        Assert.Equal(requestPayload.ChunkSequence, ack.ChunkSequence);
        Assert.Equal(requestPayload.ChunkIdempotencyKey, ack.ChunkIdempotencyKey);
        Assert.Equal(requestPayload.RequestHash, ack.RequestHash);
        Assert.NotEmpty(ack.AckPayloadJson);

        // Deserialise and verify the envelope inside DB
        var envelope = JsonSerializer.Deserialize<TestSyncIngestAckEnvelope>(ack.AckPayloadJson, JsonSerializerOptions);
        Assert.NotNull(envelope);
        Assert.NotNull(envelope.Request);
        Assert.NotNull(envelope.Response);
        Assert.Equal(requestPayload.ChunkIdempotencyKey, envelope.Request.ChunkIdempotencyKey);
        Assert.Equal(syncResponse.AckId, envelope.Response.AckId);

        var firstReqEvent = requestPayload.Events[0];
        var firstEnvEvent = envelope.Request.Events[0];
        Assert.Equal(firstReqEvent.EventId, firstEnvEvent.EventId);
        Assert.Equal(firstReqEvent.PayloadHash, firstEnvEvent.PayloadHash);
        Assert.Equal(firstReqEvent.IdempotencyKey, firstEnvEvent.IdempotencyKey);
    }

    [Fact]
    public async Task Ingest_SafeDuplicateReplay_ReturnsSameAckAndNoDuplicateRow()
    {
        var terminalCode = $"TERM-{Guid.NewGuid():N}";
        var terminal = await SeedTerminalAsync(terminalCode);

        using var client = _factory.CreateClient();
        var requestPayload = CreateValidSyncIngestRequest(
            101,
            terminal.LocationId,
            terminal.Id,
            1,
            $"idem-key-{Guid.NewGuid():N}",
            $"req-hash-{Guid.NewGuid():N}",
            $"corr-{Guid.NewGuid():N}"
        );

        // First Post
        using var request1 = CreateDeviceRequest(
            HttpMethod.Post,
            "/api/sync/ingest",
            tenantId: "101",
            locationId: terminal.LocationId.ToString(),
            terminalId: terminal.Id.ToString()
        );
        request1.Content = JsonContent.Create(requestPayload);

        var response1 = await client.SendAsync(request1);
        response1.EnsureSuccessStatusCode();
        var content1 = await response1.Content.ReadAsStringAsync();
        var syncResponse1 = JsonSerializer.Deserialize<SyncIngestResponse>(content1, JsonSerializerOptions);
        Assert.NotNull(syncResponse1);

        // Second Post (Safe replay retry)
        using var request2 = CreateDeviceRequest(
            HttpMethod.Post,
            "/api/sync/ingest",
            tenantId: "101",
            locationId: terminal.LocationId.ToString(),
            terminalId: terminal.Id.ToString()
        );
        request2.Content = JsonContent.Create(requestPayload);

        var response2 = await client.SendAsync(request2);
        response2.EnsureSuccessStatusCode();
        var content2 = await response2.Content.ReadAsStringAsync();
        var syncResponse2 = JsonSerializer.Deserialize<SyncIngestResponse>(content2, JsonSerializerOptions);
        Assert.NotNull(syncResponse2);

        // Assert they return the same AckId
        Assert.Equal(syncResponse1.AckId, syncResponse2.AckId);

        // Verify DB contains only one row for this ChunkIdempotencyKey
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PosCentralDbContext>();

        var acks = await db.SyncIngestAcks
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == 101 && x.ChunkIdempotencyKey == requestPayload.ChunkIdempotencyKey)
            .ToListAsync();

        Assert.Single(acks);
    }

    #endregion

    #region Conflicts & Same-Batch Rejections Tests

    [Fact]
    public async Task Ingest_SameKeyDifferentHash_ReturnsConflict()
    {
        var terminalCode = $"TERM-{Guid.NewGuid():N}";
        var terminal = await SeedTerminalAsync(terminalCode);

        using var client = _factory.CreateClient();
        var chunkKey = $"idem-key-{Guid.NewGuid():N}";

        var requestPayload1 = CreateValidSyncIngestRequest(
            101,
            terminal.LocationId,
            terminal.Id,
            1,
            chunkKey,
            "hash-A",
            $"corr-{Guid.NewGuid():N}"
        );

        var requestPayload2 = CreateValidSyncIngestRequest(
            101,
            terminal.LocationId,
            terminal.Id,
            1,
            chunkKey,
            "hash-B", // Different RequestHash
            $"corr-{Guid.NewGuid():N}"
        );

        // First Post
        using var request1 = CreateDeviceRequest(
            HttpMethod.Post,
            "/api/sync/ingest",
            tenantId: "101",
            locationId: terminal.LocationId.ToString(),
            terminalId: terminal.Id.ToString()
        );
        request1.Content = JsonContent.Create(requestPayload1);
        var response1 = await client.SendAsync(request1);
        response1.EnsureSuccessStatusCode();

        // Second Post with different hash
        using var request2 = CreateDeviceRequest(
            HttpMethod.Post,
            "/api/sync/ingest",
            tenantId: "101",
            locationId: terminal.LocationId.ToString(),
            terminalId: terminal.Id.ToString()
        );
        request2.Content = JsonContent.Create(requestPayload2);
        var response2 = await client.SendAsync(request2);

        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
        var problemJson = await response2.Content.ReadAsStringAsync();
        Assert.Contains("IDEMPOTENCY_CONFLICT", problemJson);
    }

    [Fact]
    public async Task Ingest_SameSequenceDifferentKey_ReturnsConflict()
    {
        var terminalCode = $"TERM-{Guid.NewGuid():N}";
        var terminal = await SeedTerminalAsync(terminalCode);

        using var client = _factory.CreateClient();
        var sequence = 100L;

        var requestPayload1 = CreateValidSyncIngestRequest(
            101,
            terminal.LocationId,
            terminal.Id,
            sequence,
            $"idem-key-{Guid.NewGuid():N}",
            $"hash-{Guid.NewGuid():N}",
            $"corr-{Guid.NewGuid():N}"
        );

        var requestPayload2 = CreateValidSyncIngestRequest(
            101,
            terminal.LocationId,
            terminal.Id,
            sequence, // Same Sequence
            $"idem-key-{Guid.NewGuid():N}", // Different ChunkIdempotencyKey
            $"hash-{Guid.NewGuid():N}",
            $"corr-{Guid.NewGuid():N}"
        );

        // First Post
        using var request1 = CreateDeviceRequest(
            HttpMethod.Post,
            "/api/sync/ingest",
            tenantId: "101",
            locationId: terminal.LocationId.ToString(),
            terminalId: terminal.Id.ToString()
        );
        request1.Content = JsonContent.Create(requestPayload1);
        var response1 = await client.SendAsync(request1);
        response1.EnsureSuccessStatusCode();

        // Second Post with same sequence but different key
        using var request2 = CreateDeviceRequest(
            HttpMethod.Post,
            "/api/sync/ingest",
            tenantId: "101",
            locationId: terminal.LocationId.ToString(),
            terminalId: terminal.Id.ToString()
        );
        request2.Content = JsonContent.Create(requestPayload2);
        var response2 = await client.SendAsync(request2);

        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
        var problemJson = await response2.Content.ReadAsStringAsync();
        Assert.Contains("SEQUENCE_CONFLICT", problemJson);
    }

    [Fact]
    public async Task Ingest_DuplicateEventIdInBatch_ReturnsConflict()
    {
        var terminalCode = $"TERM-{Guid.NewGuid():N}";
        var terminal = await SeedTerminalAsync(terminalCode);

        using var client = _factory.CreateClient();
        var sameEventId = Guid.NewGuid();

        var events = new List<SyncIngestEvent>
        {
            new SyncIngestEvent(
                new DateOnly(2026, 5, 28),
                1001,
                "OrderCompleted",
                sameEventId,
                "{}",
                "hash-1",
                "idem-1",
                "corr-1",
                1
            ),
            new SyncIngestEvent(
                new DateOnly(2026, 5, 28),
                1002,
                "OrderCompleted",
                sameEventId, // Duplicate EventId
                "{}",
                "hash-2",
                "idem-2",
                "corr-1",
                1
            )
        };

        var requestPayload = new SyncIngestRequest(
            101,
            terminal.LocationId,
            terminal.Id,
            1,
            $"idem-key-{Guid.NewGuid():N}",
            $"req-hash-{Guid.NewGuid():N}",
            $"corr-{Guid.NewGuid():N}",
            events
        );

        using var request = CreateDeviceRequest(
            HttpMethod.Post,
            "/api/sync/ingest",
            tenantId: "101",
            locationId: terminal.LocationId.ToString(),
            terminalId: terminal.Id.ToString()
        );
        request.Content = JsonContent.Create(requestPayload);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problemJson = await response.Content.ReadAsStringAsync();
        Assert.Contains("DUPLICATE_EVENT_ID", problemJson);
    }

    [Fact]
    public async Task Ingest_DuplicateEventIdempotencyKeyInBatch_ReturnsConflict()
    {
        var terminalCode = $"TERM-{Guid.NewGuid():N}";
        var terminal = await SeedTerminalAsync(terminalCode);

        using var client = _factory.CreateClient();
        var sameIdempotencyKey = $"event-idem-{Guid.NewGuid():N}";

        var events = new List<SyncIngestEvent>
        {
            new SyncIngestEvent(
                new DateOnly(2026, 5, 28),
                1001,
                "OrderCompleted",
                Guid.NewGuid(),
                "{}",
                "hash-1",
                sameIdempotencyKey,
                "corr-1",
                1
            ),
            new SyncIngestEvent(
                new DateOnly(2026, 5, 28),
                1002,
                "OrderCompleted",
                Guid.NewGuid(),
                "{}",
                "hash-2",
                sameIdempotencyKey, // Duplicate IdempotencyKey
                "corr-1",
                1
            )
        };

        var requestPayload = new SyncIngestRequest(
            101,
            terminal.LocationId,
            terminal.Id,
            1,
            $"idem-key-{Guid.NewGuid():N}",
            $"req-hash-{Guid.NewGuid():N}",
            $"corr-{Guid.NewGuid():N}",
            events
        );

        using var request = CreateDeviceRequest(
            HttpMethod.Post,
            "/api/sync/ingest",
            tenantId: "101",
            locationId: terminal.LocationId.ToString(),
            terminalId: terminal.Id.ToString()
        );
        request.Content = JsonContent.Create(requestPayload);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problemJson = await response.Content.ReadAsStringAsync();
        Assert.Contains("DUPLICATE_EVENT_IDEMPOTENCY_KEY", problemJson);
    }

    #endregion
}
