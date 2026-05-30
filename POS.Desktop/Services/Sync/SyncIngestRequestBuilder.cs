using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using POS.Shared.Contracts.Sync;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// A pure, deterministic request builder that maps outbox batches to sync ingest requests.
/// </summary>
public sealed class SyncIngestRequestBuilder : ISyncIngestRequestBuilder
{
    /// <inheritdoc />
    public SyncIngestRequest Build(SyncOutboxBatch batch)
    {
        if (batch == null)
        {
            throw new ArgumentNullException(nameof(batch));
        }

        if (!batch.HasItems)
        {
            throw new ArgumentException("Cannot build a sync request from an empty batch.", nameof(batch));
        }

        var items = batch.Items;

        // 1. Validate TenantId, LocationId, and TerminalId consistency across all items
        var firstItem = items[0];
        var tenantId = firstItem.TenantId;
        var locationId = firstItem.LocationId;
        var terminalId = firstItem.TerminalId;

        // Ensure Event IDs are unique within the batch
        var uniqueEventIds = new HashSet<Guid>();

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];

            if (item.TenantId != tenantId)
            {
                throw new ArgumentException($"Batch contains inconsistent TenantIds. Expected: {tenantId}, Found: {item.TenantId} at index {i}.", nameof(batch));
            }

            if (item.LocationId != locationId)
            {
                throw new ArgumentException($"Batch contains inconsistent LocationIds. Expected: {locationId}, Found: {item.LocationId} at index {i}.", nameof(batch));
            }

            if (item.TerminalId != terminalId)
            {
                throw new ArgumentException($"Batch contains inconsistent TerminalIds. Expected: {terminalId}, Found: {item.TerminalId} at index {i}.", nameof(batch));
            }

            if (!uniqueEventIds.Add(item.EventId))
            {
                throw new ArgumentException($"Duplicate EventId '{item.EventId}' detected in outbox batch.", nameof(batch));
            }

            // String validations
            if (string.IsNullOrWhiteSpace(item.EventType))
            {
                throw new ArgumentException($"EventType cannot be blank for EventId '{item.EventId}' at index {i}.", nameof(batch));
            }

            if (string.IsNullOrWhiteSpace(item.IdempotencyKey))
            {
                throw new ArgumentException($"IdempotencyKey cannot be blank for EventId '{item.EventId}' at index {i}.", nameof(batch));
            }

            if (string.IsNullOrWhiteSpace(item.PayloadHash))
            {
                throw new ArgumentException($"PayloadHash cannot be blank for EventId '{item.EventId}' at index {i}.", nameof(batch));
            }

            if (string.IsNullOrWhiteSpace(item.PayloadJson))
            {
                throw new ArgumentException($"PayloadJson cannot be blank for EventId '{item.EventId}' at index {i}.", nameof(batch));
            }

            if (string.IsNullOrWhiteSpace(item.CorrelationId))
            {
                throw new ArgumentException($"CorrelationId cannot be blank for EventId '{item.EventId}' at index {i}.", nameof(batch));
            }
        }

        // 2. Generate deterministic ChunkSequence
        // In Group 3, this is the minimum TerminalSequence in the batch
        long chunkSequence = items.Min(x => x.TerminalSequence);

        // 3. Generate deterministic short SHA-256 hex prefix from ordered event identity material
        // We order by sequence and EventId to ensure deterministic hashing even if batch order changed
        var orderedItems = items
            .OrderBy(x => x.TerminalSequence)
            .ThenBy(x => x.EventId)
            .ToList();

        var eventBuilder = new StringBuilder();
        foreach (var item in orderedItems)
        {
            eventBuilder.Append(item.EventId).Append(':')
                        .Append(item.TerminalSequence).Append(':')
                        .Append(item.EventType).Append(':')
                        .Append(item.IdempotencyKey).Append(':')
                        .Append(item.PayloadHash).Append(':')
                        .Append(item.CorrelationId).Append(',');
        }

        string shortHash;
        using (var sha256 = SHA256.Create())
        {
            var eventMaterialBytes = Encoding.UTF8.GetBytes(eventBuilder.ToString());
            var eventHashBytes = sha256.ComputeHash(eventMaterialBytes);
            var eventFullHashHex = Convert.ToHexString(eventHashBytes).ToLowerInvariant();
            shortHash = eventFullHashHex.Substring(0, 32);
        }

        // 4. Generate deterministic ChunkIdempotencyKey
        var firstBusinessDate = firstItem.BusinessDate.ToString("yyyyMMdd");
        var minSeq = items.Min(x => x.TerminalSequence);
        var maxSeq = items.Max(x => x.TerminalSequence);
        var count = items.Count;

        var chunkIdempotencyKey = $"chunk:{tenantId}:{locationId}:{terminalId}:{firstBusinessDate}:{minSeq}:{maxSeq}:{count}:{shortHash}";

        if (chunkIdempotencyKey.Length > 120)
        {
            throw new InvalidOperationException($"Generated ChunkIdempotencyKey '{chunkIdempotencyKey}' exceeds maximum permitted length of 120 characters.");
        }

        // 5. Generate deterministic CorrelationId
        var correlationId = $"sync-chunk-{shortHash}";

        if (correlationId.Length > 100)
        {
            throw new InvalidOperationException($"Generated CorrelationId '{correlationId}' exceeds maximum permitted length of 100 characters.");
        }

        // 6. Map events with sequence
        var mappedEvents = new List<SyncIngestEvent>(items.Count);
        foreach (var item in items)
        {
            mappedEvents.Add(new SyncIngestEvent(
                BusinessDate: item.BusinessDate,
                TerminalSequence: item.TerminalSequence,
                EventType: item.EventType,
                EventId: item.EventId,
                PayloadJson: item.PayloadJson,
                PayloadHash: item.PayloadHash,
                IdempotencyKey: item.IdempotencyKey,
                CorrelationId: item.CorrelationId,
                ChunkSequence: chunkSequence
            ));
        }

        // 7. Generate deterministic canonical RequestHash
        var hashParts = new List<string>
        {
            tenantId.ToString(),
            locationId.ToString(),
            terminalId.ToString(),
            chunkSequence.ToString(),
            chunkIdempotencyKey,
            correlationId
        };

        // requestEvents must preserve selected batch order
        foreach (var ev in mappedEvents)
        {
            hashParts.Add(ev.BusinessDate.ToString("yyyy-MM-dd"));
            hashParts.Add(ev.TerminalSequence.ToString());
            hashParts.Add(ev.EventType);
            hashParts.Add(ev.EventId.ToString());
            hashParts.Add(ev.PayloadJson);
            hashParts.Add(ev.PayloadHash);
            hashParts.Add(ev.IdempotencyKey);
            hashParts.Add(ev.CorrelationId);
            hashParts.Add(chunkSequence.ToString());
        }

        string requestHash;
        using (var sha256 = SHA256.Create())
        {
            var canonicalString = string.Join("|", hashParts);
            var canonicalBytes = Encoding.UTF8.GetBytes(canonicalString);
            var requestHashBytes = sha256.ComputeHash(canonicalBytes);
            requestHash = Convert.ToHexString(requestHashBytes).ToLowerInvariant();
        }

        return new SyncIngestRequest(
            TenantId: tenantId,
            LocationId: locationId,
            TerminalId: terminalId,
            ChunkSequence: chunkSequence,
            ChunkIdempotencyKey: chunkIdempotencyKey,
            RequestHash: requestHash,
            CorrelationId: correlationId,
            Events: mappedEvents.AsReadOnly()
        );
    }
}
