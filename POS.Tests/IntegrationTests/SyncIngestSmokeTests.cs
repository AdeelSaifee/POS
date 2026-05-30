using System;
using System.Collections.Generic;
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

/// <summary>
/// A direct API smoke test for 6.2.9 verification that bypasses the desktop client
/// and utilizes the standard API test harness to ensure the ingest endpoint remains fully functional.
/// </summary>
public sealed class SyncIngestSmokeTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public SyncIngestSmokeTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Ingest_SmokeTest_ReturnsReceivedAck()
    {
        // 1. Seed a provisioned terminal in the test database
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
            TerminalCode = "TRM-SMOKE-API",
            DeviceId = Guid.NewGuid(),
            DeviceSecretHash = "test-device-secret-hash",
            ProvisioningStatus = TerminalProvisioningStatus.Provisioned,
            IsActive = true,
            CreatedBy = "api-smoke-tests",
            CreatedOn = DateTimeOffset.UtcNow
        };

        db.Terminals.Add(terminal);
        await db.SaveChangesAsync();

        // 2. Build the request requestPayload
        var requestPayload = new SyncIngestRequest(
            TenantId: 101,
            LocationId: terminal.LocationId,
            TerminalId: terminal.Id,
            ChunkSequence: 1,
            ChunkIdempotencyKey: $"smoke-api-{Guid.NewGuid():N}",
            RequestHash: "smoke-api-hash",
            CorrelationId: "smoke-api-correlation",
            Events: new[]
            {
                new SyncIngestEvent(
                    BusinessDate: DateOnly.FromDateTime(DateTime.UtcNow),
                    TerminalSequence: 101,
                    EventType: "OrderCompleted",
                    EventId: Guid.NewGuid(),
                    PayloadJson: "{ \"orderId\": 1001 }",
                    PayloadHash: "payload-hash-xyz",
                    IdempotencyKey: $"event-idem-{Guid.NewGuid():N}",
                    CorrelationId: "smoke-api-correlation",
                    ChunkSequence: null
                )
            }
        );

        // 3. Make HTTP request with TestRequestAuthentication.Apply
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

        // 4. Send request and verify "Received" response
        var response = await client.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var syncResponse = JsonSerializer.Deserialize<SyncIngestResponse>(responseContent, JsonSerializerOptions);

        Assert.NotNull(syncResponse);
        Assert.Equal("Received", syncResponse.Status);
        Assert.Equal(requestPayload.ChunkSequence, syncResponse.ChunkSequence);
        Assert.Equal(requestPayload.ChunkIdempotencyKey, syncResponse.ChunkIdempotencyKey);
    }
}
