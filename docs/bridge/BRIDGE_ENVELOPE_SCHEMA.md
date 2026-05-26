# Bridge Envelope Schema (v1)

## 1. Purpose
This document standardizes the message envelope used for asynchronous communication between the JavaScript UI and the C# desktop shell. It ensures that both sides of the bridge adhere to a consistent contract, preventing silent failures and making the bridge easy to debug and extend.

This builds on the transport foundation established in Milestone 3.1. This is a **contract specification only**; implementation DTOs are defined in Task 3.2.2.

> **Note on Milestone 3.1 Probes:** The existing `transport.ping` and `transport.pong` messages implemented in Milestone 3.1 served as transport foundation probes. Future Task 3.2.x work will migrate formal request/response handling to the "v1" envelope defined here.

## 2. Scope
- Request envelope structure (JS → C#).
- Response envelope structure (C# → JS).
- Error data shape.
- Casing and naming conventions.
- Request/Response correlation rules.
- Security and logging constraints.

## 3. Versioning
- **Current Version:** `v1`
- Versioning allows the bridge to evolve without breaking existing screens.
- Future incompatible changes will require a version bump (e.g., `v2`).
- The C# shell should reject messages with unsupported version strings.

## 4. Request Envelope Schema (JS → C#)
All requests from the UI must use the following JSON structure via `window.chrome.webview.postMessage`.

| Field | Required | Type | Purpose | Example |
| :--- | :---: | :--- | :--- | :--- |
| `version` | Yes | `string` | The envelope version. | `"v1"` |
| `type` | Yes | `string` | The action or command name. | `"catalog.search"` |
| `requestId` | Yes | `string` | Unique ID for matching the response. | `"550e8400-e29b..."` |
| `payload` | Yes | `object \| null` | Action-specific parameters. | `{ "query": "milk" }` |
| `metadata` | No | `object \| null` | Optional context (timestamp, source). | `{ "screen": "checkout" }` |

### Validation Rules:
- `version` must be `"v1"`.
- `type` must follow the `namespace.action` convention.
- `requestId` must be provided so the JS client can resolve the matching Promise.
- `payload` must be an object or `null` (no raw primitives for business data).

## 5. Response Envelope Schema (C# → JS)
All responses from the shell use the following JSON structure via `PostWebMessageAsJson`. All fields are **required** to ensure a stable shape for the JS receiver.

| Field | Required | Type | Purpose | Example |
| :--- | :---: | :--- | :--- | :--- |
| `version` | Yes | `string` | Matches the request version. | `"v1"` |
| `type` | Yes | `string` | Matches the request type. | `"catalog.search"` |
| `requestId` | Yes | `string` | Echoes the request's ID. | `"550e8400-e29b..."` |
| `ok` | Yes | `boolean` | `true` if handled successfully. | `true` |
| `payload` | Yes | `object \| null` | The result data (if `ok` is `true`). | `{ "results": [...] }` |
| `error` | Yes | `object \| null` | Error details (if `ok` is `false`). | `{ "code": "NOT_FOUND" }` |

### Validation Rules:
- `requestId` must exactly match the request.
- **If `ok` is `true`**: `payload` may be an object or `null`; `error` **must** be `null`.
- **If `ok` is `false`**: `payload` **must** be `null`; `error` **must** be populated.

## 6. Error Shape
Errors are returned as a structured object to avoid exposing sensitive internal details.

| Field | Required | Type | Purpose | Example |
| :--- | :---: | :--- | :--- | :--- |
| `code` | Yes | `string` | A machine-readable error code. | `"UNSUPPORTED_TYPE"` |
| `message` | Yes | `string` | A human-readable (operator-safe) message. | `"The search failed."` |
| `details` | No | `object \| null` | Non-sensitive additional info. | `{ "field": "query" }` |

**Security Rules:**
- **No stack traces** sent to JavaScript.
- **No internal DB/Path details** exposed.
- Keep operator-facing messages safe and non-technical.
- Task 3.2.4 will formalize the specific error codes.

## 7. Casing and Naming Conventions
- **JSON Casing:** All fields and keys must use `camelCase`.
- **Message Type Style:** `namespace.action` (e.g., `session.login`, `order.addItem`).
- **C# Implementation:** DTOs must be configured with `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` in Task 3.2.3.
- **JS Implementation:** The `posBridge` helper will handle `requestId` generation and Promise management.

## 8. Examples

### Successful Request (JS → C#)
```json
{
  "version": "v1",
  "type": "catalog.search",
  "requestId": "req-12345",
  "payload": {
    "query": "bread"
  }
}
```

### Successful Response (C# → JS)
```json
{
  "version": "v1",
  "type": "catalog.search",
  "requestId": "req-12345",
  "ok": true,
  "payload": {
    "results": [{ "id": 1, "name": "Sourdough" }]
  },
  "error": null
}
```

### Failed Response (C# → JS)
```json
{
  "version": "v1",
  "type": "order.addItem",
  "requestId": "req-67890",
  "ok": false,
  "payload": null,
  "error": {
    "code": "INVALID_QUANTITY",
    "message": "Cannot add negative items to the cart.",
    "details": { "qty": -1 }
  }
}
```

### Malformed Request (Incoming Example)
*Example of a message that fails version check or is missing required fields.*
```json
{
  "version": "unknown",
  "type": "some.action"
}
```

### Malformed Request Error Response (C# → JS)
```json
{
  "version": "v1",
  "type": "unknown",
  "requestId": "unrecognized",
  "ok": false,
  "payload": null,
  "error": {
    "code": "MALFORMED_REQUEST",
    "message": "The message envelope was invalid or version is unsupported."
  }
}
```

### Unsupported/Unknown Type Response (C# → JS)
```json
{
  "version": "v1",
  "type": "future.feature",
  "requestId": "req-999",
  "ok": false,
  "payload": null,
  "error": {
    "code": "UNSUPPORTED_TYPE",
    "message": "The requested action 'future.feature' is not implemented."
  }
}
```

## 9. Payload & Security Rules
- **No Raw Primitives:** Business payloads must be wrapped in an object.
- **No Sensitive Logging:** Never log raw PINs, card data, or tokens.
- **View-Only JS:** JavaScript remains a client for display and input; C# validates all inputs and owns all business decisions.
- **Source of Truth:** C# services remain the authoritative source of truth for session, cart, and totals.

## 10. Future Task Mapping
- **Task 3.2.2:** Create C# DTO classes matching this schema.
- **Task 3.2.3:** Set up global JSON serializer settings (camelCase).
- **Task 3.2.4:** Formalize the C# Error model and common codes.
- **Task 3.2.5:** Implement the Promise-based `posBridge.request` JS helper.
- **Task 3.2.6:** Implement C# `requestId` correlation logic.
- **Milestone 3.3:** Build the `PosWebMessageRouter` to dispatch these messages.
