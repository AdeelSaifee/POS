# POS central API Sync Ingest Endpoint Contract Documentation

This document defines the authoritative API contract for the central POS sync ingest endpoint. It is designed as a technical guide for API and Desktop UI synchronization engine developers.

---

## 1. Overview
*   **Route**: `POST /api/sync/ingest`
*   **Protocol**: HTTPS
*   **Request Content-Type**: `application/json`
*   **Response Content-Type**: `application/json`
*   **Purpose**: Central gateway for online terminals to upload/synchronize batches of offline outbox events.
*   **Transformation Behavior**: 
    > [!IMPORTANT]
    > **Ingestion is currently asynchronous / deferred.** Receiving a `200 OK` with status `"Received"` means that the complete request package and event payloads have been successfully received and durably preserved in central database metadata storage. The actual transformation/parsing of events into core business tables (`Orders`, `Payments`, `Shifts`, `CashDrawerMovements`) is deferred to future ledger processing work.

---

## 2. Authorization Contract
*   **Policy Enforced**: `PosDevice`
*   **Authentication Type**: Bearer JWT Token
*   **Mandatory Token Claims**:
    *   `client_type` (must be exactly `"device"`)
    *   `tenant_id` (must be a positive integer matching client tenant context)
    *   `location_id` (must be a positive integer)
    *   `terminal_id` (must be a positive integer)
*   **Optional Token Claims**:
    *   `device_id` (string tracker)
*   **Access Denied HTTP States**:
    *   **401 Unauthorized**: Omission of Bearer token or invalid signature.
    *   **403 Forbidden**: Token has wrong `client_type` (e.g. `"user"` or `"admin"`), missing required identity claims, non-numeric values, or zero/negative integer values.

---

## 3. Identity Integrity Rules
*   **Authoritative Identity**: The claims extracted from the JWT token are the absolute source of truth.
*   **Body Validation Check**: The parameters inside the request body (`tenantId`, `locationId`, `terminalId`) are cross-checked against token claims to prevent spoofing or request tampering.
*   **Rejection Behavior**: If the body credentials do not match the token claims exactly, the API rejects the request immediately with **`400 BadRequest`** (`Device Identity Mismatch`), preventing processing of the payload.

---

## 4. Request Body Contract
The request body contains a JSON object representing a `SyncIngestRequest` containing metadata and an events list.

### 4.1 SyncIngestRequest Schema
| Property Name | Data Type | Constraint | Description |
| :--- | :--- | :--- | :--- |
| `tenantId` | `int` | Positive, matches claim | Unique identifier of the tenant. |
| `locationId` | `int` | Positive, matches claim | Unique identifier of the terminal location. |
| `terminalId` | `int` | Positive, matches claim | Unique identifier of the terminal. |
| `chunkSequence` | `long` | Monotonic increasing | The sequence index of the chunk prepared by the terminal. |
| `chunkIdempotencyKey` | `string` | Unique, Required, Max 120 | Idempotency key generated for the batch chunk. |
| `requestHash` | `string` | Required, Max 128 | Terminal-provided request integrity hash. The API stores and compares this value during replay/conflict detection, and also verifies the persisted envelope metadata. Full canonical server-side hash recomputation is deferred. |
| `correlationId` | `string` | Required, Max 100 | Trace identifier assigned to this sync chunk. |
| `events` | `array` | Min 1 item | List of outbox events contained in this batch chunk. |

### 4.2 SyncIngestEvent Schema
Each entry in the `events` array represents a single `SyncIngestEvent`:
| Property Name | Data Type | Constraint | Description |
| :--- | :--- | :--- | :--- |
| `businessDate` | `string` | ISO format `YYYY-MM-DD` | The business date of the POS shift when the event occurred. |
| `terminalSequence` | `long` | Positive | The sequence order of the event generated on the local outbox. |
| `eventType` | `string` | Required, Max 80 | The type of POS event (e.g. `OrderCompleted`, `ShiftOpened`). |
| `eventId` | `string (Guid)` | Unique, Required | The unique identifier of the outbox event. |
| `payloadJson` | `string` | JSON string, Required | Serialized JSON string representing the event's business payload. |
| `payloadHash` | `string` | Required, Max 128 | SHA-256 hash of the raw `payloadJson` to verify integrity. |
| `idempotencyKey` | `string` | Required, Max 100 | The unique idempotency key for this individual event. |
| `correlationId` | `string` | Required, Max 100 | Trace identifier of the event. |
| `chunkSequence` | `long?` | Optional | Sequence number of the parent chunk. |

---

## 5. Response Body Contract
A successful sync returns a `SyncIngestResponse` with a collection of event-level acknowledgments.

### 5.1 SyncIngestResponse Schema
| Property Name | Data Type | Description |
| :--- | :--- | :--- |
| `ackId` | `string (Guid)` | Unique identifier of the central acknowledgment. |
| `chunkSequence` | `long` | Matches request `chunkSequence`. |
| `chunkIdempotencyKey` | `string` | Matches request `chunkIdempotencyKey`. |
| `status` | `string` | Overall sync status (currently always `"Received"`). |
| `eventCount` | `int` | Number of events acknowledged. |
| `events` | `array` | List of event-level acknowledgments. |
| `errorCode` | `string?` | Null on success. |
| `errorMessage` | `string?` | Null on success. |

### 5.2 SyncIngestEventAck Schema
| Property Name | Data Type | Description |
| :--- | :--- | :--- |
| `eventId` | `string (Guid)` | The unique identifier of the event. |
| `idempotencyKey` | `string` | The event idempotency key. |
| `terminalSequence` | `long` | Local sequential order of the event. |
| `status` | `string` | Event sync status (currently always `"Received"`). |
| `errorCode` | `string?` | Null on success. |
| `errorMessage` | `string?` | Null on success. |

---

## 6. Persistence Contract
*   **Ack Storage**: Successful requests persist exactly one row inside the SQL central `SyncIngestAcks` database table.
*   **AckPayloadJson Envelope**: The `AckPayloadJson` column (nvarchar(max)) stores a complete serialized `SyncIngestAckEnvelope` DTO. This contains:
    - Original request structure (`Request`) preserving all events, payloads, hashes, and identity tags.
    - Generated success response structure (`Response`).
    - Exact central server timestamp (`ReceivedOn`).
    - Standard status description: `"Received means durable sync receipt and payload preservation only; business event transformation is deferred."`
*   **Schema Indexes**:
    - Unique alternate index on `TenantId` + `ChunkIdempotencyKey` (`UX_SyncIngestAcks_Tenant_ChunkKey`) prevents double-persistence.
    - Unique index on `TenantId` + `TerminalId` + `ChunkSequence` (`UX_SyncIngestAcks_Tenant_Terminal_Sequence`) prevents sequence collisions.

---

## 7. Idempotency & Deduplication Rules

The central API enforces strict deduplication to ensure correctness and prevent transaction double-processing:

1.  **Safe Duplicate Replays**:
    *   If a request comes with a `ChunkIdempotencyKey` that already exists under that `TenantId`, the API compares the incoming payload details against the stored envelope (`IsStoredEnvelopeEquivalentToRequest`).
    *   If the headers (`chunkSequence`, `requestHash`, `tenantId`, etc.), event counts, and every single event property by exact index order match, it triggers a safe replay - returning the original `SyncIngestResponse` with `200 OK`, without creating new rows.
    *   Direct legacy fallback support is disabled; the payload must match the `SyncIngestAckEnvelope` structure. If parsing fails, it throws a `DESERIALIZATION_FAILURE` conflict.
2.  **Idempotency Conflicts**:
    *   If the same `ChunkIdempotencyKey` is supplied but the payload differs or `RequestHash` does not match, the API rejects the request with `409 Conflict` (`IDEMPOTENCY_CONFLICT` or `STORED_ENVELOPE_CONFLICT`).
3.  **Sequence Conflicts**:
    *   If a chunk sequence index has already been processed under a different idempotency key for that terminal, the API rejects it with `409 Conflict` (`SEQUENCE_CONFLICT`).
4.  **Same-Batch Duplicate Events Rejection**:
    *   If a batch contains duplicate `EventId` or duplicate non-blank event `IdempotencyKey` values inside the same `Events` array, the API rejects it with `409 Conflict` (`DUPLICATE_EVENT_ID` or `DUPLICATE_EVENT_IDEMPOTENCY_KEY`).
5.  **Concurrency Race Recovery**:
    *   If two simultaneous requests collision-race on EF Core SaveChanges, the unique index violation is caught, transaction rolls back, and it fetches the winning committed ack to run the same duplicate replay verification rules.
6.  **Deferred Deduplication**:
    *   Duplicate terminal sequence blocking is enforced; sequential gap checking is deferred.
    *   Cross-chunk duplicate `EventId` detection remains deferred until a central ledger parser exists.

---

## 8. Status Code Matrix
| Status Code | Code Title | Condition |
| :---: | :--- | :--- |
| **`200 OK`** | `Success / Replay` | Request was successfully processed and persisted, or was a safe duplicate replay retry. |
| **`400 BadRequest`** | `Device Identity Mismatch / Invalid Ingest Request` | Identity claims do not match body parameters, or the request properties are malformed. |
| **`401 Unauthorized`** | `Unauthorized` | Bearer token is missing, invalid, or expired. |
| **`403 Forbidden`** | `Forbidden` | Non-device caller tries to ingest, or token is missing mandatory device claims, or negative/zero claims are present. |
| **`409 Conflict`** | `IDEMPOTENCY_CONFLICT / SEQUENCE_CONFLICT / etc.` | Duplications detected (key mismatch, sequence collision, same-batch duplicate event). |
| **`500 Internal Error`** | `Internal Server Error` | Unexpected database exception or server error. |

---

## 9. JSON Examples

### 9.1 Sample Request Payload
```json
{
  "tenantId": 101,
  "locationId": 1,
  "terminalId": 5,
  "chunkSequence": 1,
  "chunkIdempotencyKey": "chunk-idem-2ac2097e08494c5fb03a3",
  "requestHash": "req-hash-e109d73d6b9d4fbe1b3b1f9",
  "correlationId": "corr-84d3b6f8-4b2a-4cbe-8c01-7fa174241ff8",
  "events": [
    {
      "businessDate": "2026-05-28",
      "terminalSequence": 1001,
      "eventType": "OrderCompleted",
      "eventId": "f7d75fa5-e3d8-4f24-9b24-738b5505f0a0",
      "payloadJson": "{\"orderId\":\"ORD-8fd3\",\"grossAmount\":150.00,\"taxAmount\":25.00,\"netAmount\":175.00}",
      "payloadHash": "ev-hash-4a1b028c3d7e8f0a",
      "idempotencyKey": "event-idem-8fd3b6a9e1e247b93a0b",
      "correlationId": "corr-84d3b6f8-4b2a-4cbe-8c01-7fa174241ff8",
      "chunkSequence": 1
    }
  ]
}
```

### 9.2 Sample `200 OK` Response
```json
{
  "ackId": "b1bcfb7e-97c4-42f1-bb21-7299a9bbf870",
  "chunkSequence": 1,
  "chunkIdempotencyKey": "chunk-idem-2ac2097e08494c5fb03a3",
  "status": "Received",
  "eventCount": 1,
  "events": [
    {
      "eventId": "f7d75fa5-e3d8-4f24-9b24-738b5505f0a0",
      "idempotencyKey": "event-idem-8fd3b6a9e1e247b93a0b",
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

### 9.3 Sample `409 Conflict` (Idempotency Conflict)
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

## 10. Deferred Roadmap / Next Milestones
1.  **Ledger Transformations (Phase 6.2+)**: Conversion of raw event JSON inside `AckPayloadJson` into operational central tables (`Orders`, `Payments`, etc.) is deferred.
2.  **Cross-Chunk Duplicate Event Detection**: Global database validation for already processed event IDs is deferred.
3.  **Gap/Order Validation**: Strict missing sequence/out-of-order gap analysis of chunk sequences is deferred.
4.  **Desktop HTTP Sync Client (Phase 6.2+)**: Integration of a background `SyncProcessor` worker draining SQLite local `SyncOutbox` to push chunks centrally belongs to future milestones.
