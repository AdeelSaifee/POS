using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POS.Api.Data;
using POS.Shared.Contracts.Sync;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Application.Sync;

/// <summary>
/// Service implementation for ingesting terminal sync batches centrally.
/// </summary>
public sealed class SyncIngestService : ISyncIngestService
{
    private readonly PosCentralDbContext _db;
    private readonly ILogger<SyncIngestService> _logger;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of <see cref="SyncIngestService"/>.
    /// </summary>
    public SyncIngestService(PosCentralDbContext db, ILogger<SyncIngestService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SyncIngestResponse> IngestAsync(
        SyncIngestIdentity identity,
        SyncIngestRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Perform parameter and identity validation guards
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request), "Sync ingest request cannot be null.");
        }

        if (request.Events == null || request.Events.Count == 0)
        {
            throw new SyncConflictException("EMPTY_BATCH", "Sync ingest batch request must contain at least one outbox event.");
        }

        if (identity.TenantId <= 0 || identity.LocationId <= 0 || identity.TerminalId <= 0)
        {
            throw new SyncConflictException("INVALID_IDENTITY", "Authenticated device identity values must be positive integers.");
        }

        if (identity.TenantId != request.TenantId ||
            identity.LocationId != request.LocationId ||
            identity.TerminalId != request.TerminalId)
        {
            throw new SyncConflictException("IDENTITY_MISMATCH", "Authenticated device identity does not match request body parameters.");
        }

        // Validate duplicate event IDs inside the incoming batch
        var eventIds = new HashSet<Guid>();
        var idempotencyKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var ev in request.Events)
        {
            if (!eventIds.Add(ev.EventId))
            {
                throw new SyncConflictException(
                    "DUPLICATE_EVENT_ID",
                    $"Duplicate EventId '{ev.EventId}' found inside the same sync batch. Duplicate event identifiers inside the same sync batch are not allowed.");
            }

            if (!string.IsNullOrWhiteSpace(ev.IdempotencyKey))
            {
                if (!idempotencyKeys.Add(ev.IdempotencyKey))
                {
                    throw new SyncConflictException(
                        "DUPLICATE_EVENT_IDEMPOTENCY_KEY",
                        $"Duplicate event IdempotencyKey '{ev.IdempotencyKey}' found inside the same sync batch. Duplicate event identifiers inside the same sync batch are not allowed.");
                }
            }
        }

        // 2. Perform idempotency/dedupe checks using IgnoreQueryFilters to ensure absolute safety
        var existingAck = await _db.SyncIngestAcks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.TenantId == identity.TenantId && x.ChunkIdempotencyKey == request.ChunkIdempotencyKey,
                cancellationToken);

        if (existingAck != null)
        {
            _logger.LogInformation(
                "Safe duplicate sync retry received for Tenant {TenantId}, ChunkKey {ChunkKey}. Verifying stored payload.",
                identity.TenantId,
                request.ChunkIdempotencyKey);

            return ParseAndVerifyPersistedAckResponse(existingAck, request);
        }

        // Case C: Same TenantId + TerminalId + ChunkSequence + different ChunkIdempotencyKey => Sequence conflict
        var existingSequenceAck = await _db.SyncIngestAcks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.TenantId == identity.TenantId && x.TerminalId == identity.TerminalId && x.ChunkSequence == request.ChunkSequence,
                cancellationToken);

        if (existingSequenceAck != null)
        {
            _logger.LogWarning(
                "Sequence conflict detected for Tenant {TenantId}, Terminal {TerminalId}, Sequence {Sequence}. Already processed under chunk key {ExistingKey}.",
                identity.TenantId,
                identity.TerminalId,
                request.ChunkSequence,
                existingSequenceAck.ChunkIdempotencyKey);

            throw new SyncConflictException(
                "SEQUENCE_CONFLICT",
                $"Chunk sequence {request.ChunkSequence} for terminal {identity.TerminalId} has already been processed under a different chunk idempotency key.");
        }

        // 3. Construct successful response payload
        var ackId = Guid.NewGuid();
        var eventAcks = request.Events.Select(ev => new SyncIngestEventAck(
            ev.EventId,
            ev.IdempotencyKey,
            ev.TerminalSequence,
            "Received",
            null,
            null
        )).ToList();

        var response = new SyncIngestResponse(
            ackId,
            request.ChunkSequence,
            request.ChunkIdempotencyKey,
            "Received",
            request.Events.Count,
            eventAcks,
            null,
            null
        );

        // Serialize full request details along with response inside AckPayloadJson
        var envelope = new SyncIngestAckEnvelope(
            request,
            response,
            DateTimeOffset.UtcNow,
            "Received means durable sync receipt and payload preservation only; business event transformation is deferred."
        );
        var serializedEnvelope = JsonSerializer.Serialize(envelope, JsonSerializerOptions);

        var sortedDates = request.Events.Select(x => x.BusinessDate).OrderBy(x => x).ToList();
        var firstBusinessDate = sortedDates[0];
        var lastBusinessDate = sortedDates[^1];

        // 4. Create and persist central SyncIngestAck row
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
            FirstBusinessDate = firstBusinessDate == default ? null : firstBusinessDate,
            LastBusinessDate = lastBusinessDate == default ? null : lastBusinessDate,
            Status = "Received",
            AckPayloadJson = serializedEnvelope,
            ErrorCode = null,
            ErrorMessage = null,
            ReceivedOn = DateTimeOffset.UtcNow,
            ExpiresOn = DateTimeOffset.UtcNow.AddDays(30),
            CorrelationId = request.CorrelationId,
            IsActive = true,
            CreatedBy = identity.DeviceId ?? "sync-device",
            CreatedOn = DateTimeOffset.UtcNow
        };

        try
        {
            _db.SyncIngestAcks.Add(ack);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Durable sync chunk received and persisted for Tenant {TenantId}, Terminal {TerminalId}, ChunkKey {ChunkKey}.",
                identity.TenantId,
                identity.TerminalId,
                request.ChunkIdempotencyKey);

            return response;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "DbUpdateException unique index violation caught during SyncIngestAck save. Attempting race recovery.");

            // Case D: Concurrency unique constraint race recovery
            var raceAck = await _db.SyncIngestAcks
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    x => x.TenantId == identity.TenantId && x.ChunkIdempotencyKey == request.ChunkIdempotencyKey,
                    cancellationToken);

            if (raceAck != null)
            {
                return ParseAndVerifyPersistedAckResponse(raceAck, request);
            }

            var raceSeqAck = await _db.SyncIngestAcks
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    x => x.TenantId == identity.TenantId && x.TerminalId == identity.TerminalId && x.ChunkSequence == request.ChunkSequence,
                    cancellationToken);

            if (raceSeqAck != null)
            {
                throw new SyncConflictException(
                    "SEQUENCE_CONFLICT",
                    $"Chunk sequence {request.ChunkSequence} for terminal {identity.TerminalId} has already been processed under a different chunk idempotency key.");
            }

            throw; // Rethrow if it was a different database error
        }
    }

    private SyncIngestResponse ParseAndVerifyPersistedAckResponse(
        SyncIngestAck existingAck,
        SyncIngestRequest request)
    {
        if (existingAck.RequestHash != request.RequestHash)
        {
            _logger.LogWarning(
                "Idempotency conflict detected for Tenant {TenantId}, ChunkKey {ChunkKey}. Different request hash found.",
                existingAck.TenantId,
                existingAck.ChunkIdempotencyKey);

            throw new SyncConflictException(
                "IDEMPOTENCY_CONFLICT",
                "Chunk idempotency key is already used with a different request payload.");
        }

        try
        {
            // Try to deserialize as SyncIngestAckEnvelope
            var envelope = JsonSerializer.Deserialize<SyncIngestAckEnvelope>(existingAck.AckPayloadJson, JsonSerializerOptions);
            if (envelope != null)
            {
                if (!IsStoredEnvelopeEquivalentToRequest(envelope, request))
                {
                    _logger.LogWarning(
                        "Stored envelope mismatch detected for Tenant {TenantId}, ChunkKey {ChunkKey} despite matching request hash.",
                        existingAck.TenantId,
                        existingAck.ChunkIdempotencyKey);

                    throw new SyncConflictException(
                        "STORED_ENVELOPE_CONFLICT",
                        "The stored chunk request payload details do not match the incoming request.");
                }

                return envelope.Response;
            }
        }
        catch (SyncConflictException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize chunk acknowledgment envelope.");
        }

        throw new SyncConflictException(
            "DESERIALIZATION_FAILURE",
            "Stored chunk acknowledgment payload is corrupt or unparseable.");
    }

    private static bool IsStoredEnvelopeEquivalentToRequest(SyncIngestAckEnvelope envelope, SyncIngestRequest request)
    {
        if (envelope?.Request == null || request == null)
        {
            return false;
        }

        var req = envelope.Request;

        if (req.ChunkSequence != request.ChunkSequence ||
            req.ChunkIdempotencyKey != request.ChunkIdempotencyKey ||
            req.RequestHash != request.RequestHash ||
            req.TenantId != request.TenantId ||
            req.LocationId != request.LocationId ||
            req.TerminalId != request.TerminalId)
        {
            return false;
        }

        var envEvents = req.Events;
        var reqEvents = request.Events;

        if (envEvents == null && reqEvents == null)
        {
            return true;
        }

        if (envEvents == null || reqEvents == null)
        {
            return false;
        }

        if (envEvents.Count != reqEvents.Count)
        {
            return false;
        }

        for (int i = 0; i < envEvents.Count; i++)
        {
            var evEnv = envEvents[i];
            var evReq = reqEvents[i];

            if (evEnv.EventId != evReq.EventId ||
                evEnv.TerminalSequence != evReq.TerminalSequence ||
                evEnv.EventType != evReq.EventType ||
                evEnv.IdempotencyKey != evReq.IdempotencyKey ||
                evEnv.PayloadHash != evReq.PayloadHash ||
                evEnv.CorrelationId != evReq.CorrelationId)
            {
                return false;
            }
        }

        return true;
    }

    private sealed record SyncIngestAckEnvelope(
        SyncIngestRequest Request,
        SyncIngestResponse Response,
        DateTimeOffset ReceivedOn,
        string StatusMeaning
    );
}
