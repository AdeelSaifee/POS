using System;
using System.Collections.Generic;
using System.Linq;
using POS.Desktop.Services.Sync;
using Xunit;

namespace POS.Desktop.Tests.Services.Sync;

/// <summary>
/// Unit tests for the <see cref="SyncIngestRequestBuilder"/> mapping, hashing, and validation logic.
/// </summary>
public sealed class SyncIngestRequestBuilderTests
{
    private readonly SyncIngestRequestBuilder _builder = new();

    private SyncOutboxBatchItem CreateItem(
        Guid? id = null,
        int tenantId = 1,
        int locationId = 10,
        int terminalId = 20,
        DateOnly? businessDate = null,
        long terminalSequence = 100,
        string eventType = "OrderCompleted",
        Guid? eventId = null,
        string payloadJson = "{\"id\":\"123\"}",
        string payloadHash = "hash-123",
        string idempotencyKey = "idem-123",
        string correlationId = "corr-123")
    {
        return new SyncOutboxBatchItem(
            Id: id ?? Guid.NewGuid(),
            TenantId: tenantId,
            LocationId: locationId,
            TerminalId: terminalId,
            BusinessDate: businessDate ?? new DateOnly(2026, 5, 30),
            TerminalSequence: terminalSequence,
            EventType: eventType,
            EventId: eventId ?? Guid.NewGuid(),
            PayloadJson: payloadJson,
            PayloadHash: payloadHash,
            IdempotencyKey: idempotencyKey,
            CorrelationId: correlationId
        );
    }

    [Fact]
    public void Build_Throws_WhenBatchIsEmpty()
    {
        // Arrange
        var batch = new SyncOutboxBatch(Array.Empty<SyncOutboxBatchItem>());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _builder.Build(batch));
    }

    [Fact]
    public void Build_Throws_WhenBatchIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _builder.Build(null!));
    }

    [Fact]
    public void Build_MapsBatchItemsToSyncIngestEvents()
    {
        // Arrange
        var item1 = CreateItem(terminalSequence: 100, eventId: Guid.NewGuid());
        var item2 = CreateItem(terminalSequence: 101, eventId: Guid.NewGuid());
        var batch = new SyncOutboxBatch(new[] { item1, item2 });

        // Act
        var request = _builder.Build(batch);

        // Assert
        Assert.NotNull(request);
        Assert.Equal(item1.TenantId, request.TenantId);
        Assert.Equal(item1.LocationId, request.LocationId);
        Assert.Equal(item1.TerminalId, request.TerminalId);
        Assert.Equal(2, request.Events.Count);

        var event1 = request.Events[0];
        Assert.Equal(item1.BusinessDate, event1.BusinessDate);
        Assert.Equal(item1.TerminalSequence, event1.TerminalSequence);
        Assert.Equal(item1.EventType, event1.EventType);
        Assert.Equal(item1.EventId, event1.EventId);
        Assert.Equal(item1.PayloadJson, event1.PayloadJson);
        Assert.Equal(item1.PayloadHash, event1.PayloadHash);
        Assert.Equal(item1.IdempotencyKey, event1.IdempotencyKey);
        Assert.Equal(item1.CorrelationId, event1.CorrelationId);
        Assert.Equal(100, event1.ChunkSequence); // min sequence of the batch
    }

    [Fact]
    public void Build_UsesMinimumTerminalSequenceAsTemporaryChunkSequence()
    {
        // Arrange
        var item1 = CreateItem(terminalSequence: 450);
        var item2 = CreateItem(terminalSequence: 230);
        var item3 = CreateItem(terminalSequence: 789);
        var batch = new SyncOutboxBatch(new[] { item1, item2, item3 });

        // Act
        var request = _builder.Build(batch);

        // Assert
        Assert.Equal(230, request.ChunkSequence);
        Assert.All(request.Events, ev => Assert.Equal(230, ev.ChunkSequence));
    }

    [Fact]
    public void Build_GeneratesDeterministicChunkIdempotencyKey_ForSameBatch()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var item1 = CreateItem(terminalSequence: 100, eventId: id1);
        var item2 = CreateItem(terminalSequence: 101, eventId: id2);

        var batch1 = new SyncOutboxBatch(new[] { item1, item2 });
        var batch2 = new SyncOutboxBatch(new[] { item1, item2 });

        // Act
        var request1 = _builder.Build(batch1);
        var request2 = _builder.Build(batch2);

        // Assert
        Assert.Equal(request1.ChunkIdempotencyKey, request2.ChunkIdempotencyKey);
        Assert.StartsWith("chunk:1:10:20:20260530:100:101:2:", request1.ChunkIdempotencyKey);
    }

    [Fact]
    public void Build_GeneratesDeterministicCorrelationId_ForSameBatch()
    {
        // Arrange
        var item1 = CreateItem(terminalSequence: 100);
        var item2 = CreateItem(terminalSequence: 101);
        var batch1 = new SyncOutboxBatch(new[] { item1, item2 });
        var batch2 = new SyncOutboxBatch(new[] { item1, item2 });

        // Act
        var request1 = _builder.Build(batch1);
        var request2 = _builder.Build(batch2);

        // Assert
        Assert.Equal(request1.CorrelationId, request2.CorrelationId);
        Assert.StartsWith("sync-chunk-", request1.CorrelationId);
    }

    [Fact]
    public void Build_GeneratesDeterministicRequestHash_ForSameBatch()
    {
        // Arrange
        var item1 = CreateItem(terminalSequence: 100);
        var item2 = CreateItem(terminalSequence: 101);
        var batch1 = new SyncOutboxBatch(new[] { item1, item2 });
        var batch2 = new SyncOutboxBatch(new[] { item1, item2 });

        // Act
        var request1 = _builder.Build(batch1);
        var request2 = _builder.Build(batch2);

        // Assert
        Assert.Equal(request1.RequestHash, request2.RequestHash);
        Assert.Equal(64, request1.RequestHash.Length); // Full SHA-256 Hex length
    }

    [Fact]
    public void Build_RequestHashChanges_WhenPayloadHashChanges()
    {
        // Arrange
        var item1 = CreateItem(terminalSequence: 100, payloadHash: "hash-A");
        var item2 = CreateItem(terminalSequence: 100, payloadHash: "hash-B");

        var batch1 = new SyncOutboxBatch(new[] { item1 });
        var batch2 = new SyncOutboxBatch(new[] { item2 });

        // Act
        var request1 = _builder.Build(batch1);
        var request2 = _builder.Build(batch2);

        // Assert
        Assert.NotEqual(request1.RequestHash, request2.RequestHash);
    }

    [Fact]
    public void Build_Throws_WhenBatchHasMixedTenantLocationOrTerminal()
    {
        // Arrange & Act & Assert
        var item1 = CreateItem(tenantId: 1);
        var item2 = CreateItem(tenantId: 2); // Different TenantId
        var batchTenant = new SyncOutboxBatch(new[] { item1, item2 });
        Assert.Throws<ArgumentException>(() => _builder.Build(batchTenant));

        var item3 = CreateItem(locationId: 10);
        var item4 = CreateItem(locationId: 20); // Different LocationId
        var batchLoc = new SyncOutboxBatch(new[] { item3, item4 });
        Assert.Throws<ArgumentException>(() => _builder.Build(batchLoc));

        var item5 = CreateItem(terminalId: 5);
        var item6 = CreateItem(terminalId: 6); // Different TerminalId
        var batchTerm = new SyncOutboxBatch(new[] { item5, item6 });
        Assert.Throws<ArgumentException>(() => _builder.Build(batchTerm));
    }

    [Fact]
    public void Build_Throws_WhenDuplicateEventIdsExist()
    {
        // Arrange
        var duplicateId = Guid.NewGuid();
        var item1 = CreateItem(eventId: duplicateId);
        var item2 = CreateItem(eventId: duplicateId);
        var batch = new SyncOutboxBatch(new[] { item1, item2 });

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _builder.Build(batch));
    }

    [Theory]
    [InlineData("", "idem-123", "hash-123", "payload", "corr-123")]
    [InlineData("OrderCompleted", "", "hash-123", "payload", "corr-123")]
    [InlineData("OrderCompleted", "idem-123", "", "payload", "corr-123")]
    [InlineData("OrderCompleted", "idem-123", "hash-123", "", "corr-123")]
    [InlineData("OrderCompleted", "idem-123", "hash-123", "payload", "")]
    public void Build_Throws_WhenRequiredStringFieldsAreBlank(
        string eventType,
        string idempotencyKey,
        string payloadHash,
        string payloadJson,
        string correlationId)
    {
        // Arrange
        var item = CreateItem(
            eventType: eventType,
            idempotencyKey: idempotencyKey,
            payloadHash: payloadHash,
            payloadJson: payloadJson,
            correlationId: correlationId
        );
        var batch = new SyncOutboxBatch(new[] { item });

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _builder.Build(batch));
    }

    [Fact]
    public void Build_ChunkIdempotencyKey_DoesNotExceed120Characters()
    {
        // Arrange
        var item = CreateItem(
            tenantId: 99999,
            locationId: 99999,
            terminalId: 99999,
            terminalSequence: 999999999
        );
        var batch = new SyncOutboxBatch(new[] { item });

        // Act
        var request = _builder.Build(batch);

        // Assert
        Assert.True(request.ChunkIdempotencyKey.Length <= 120, $"IdempotencyKey '{request.ChunkIdempotencyKey}' length is {request.ChunkIdempotencyKey.Length}");
    }

    [Fact]
    public void Build_CorrelationId_DoesNotExceed100Characters()
    {
        // Arrange
        var item = CreateItem();
        var batch = new SyncOutboxBatch(new[] { item });

        // Act
        var request = _builder.Build(batch);

        // Assert
        Assert.True(request.CorrelationId.Length <= 100, $"CorrelationId '{request.CorrelationId}' length is {request.CorrelationId.Length}");
    }
}
