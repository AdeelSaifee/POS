# Bridge Conventions

## 1. Purpose
This document is intended for future bridge handler authors. It complements the `BRIDGE_ENVELOPE_SCHEMA.md` and helps prevent drift between the C# WebView2 backend and the JavaScript frontend.

## 2. Scope
This guide covers:
- JSON casing
- Serializer settings
- Request/response envelope rules
- Error conventions
- `requestId` conventions
- Payload conventions
- Logging and security rules
- Legacy transport probe notes
- Future router handler rules

## 3. JSON Casing Conventions
- JSON sent over the bridge **must be `camelCase`**.
- C# properties in models and DTOs may be `PascalCase`.
- `BridgeJsonSerializerOptions.Default` **must** be used for all v1 bridge envelope serialization.
- Do not hand-build JSON strings for bridge envelopes.

## 4. Serializer Conventions
- Use `System.Text.Json` for all bridge serialization.
- Use the shared `BridgeJsonSerializerOptions.Default` configuration.
- Null fields **must** be preserved where required by the v1 envelope. Specifically, the `payload` and `error` fields must remain present on responses even if they are null.
- Do not globally ignore null values for bridge envelopes (e.g., do not set `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull` on the bridge serializer options).

## 5. Request Envelope Conventions
- `version` must always be `"v1"`.
- `type` must follow the `namespace.action` format (e.g., `auth.login`, `cart.addItem`).
- `requestId` must be a non-empty string provided by the JavaScript client.
- `payload` must be a JSON object or `null`.
- `metadata` is optional and must **not** contain any sensitive data or secrets.

## 6. Response Envelope Conventions
- Every response must echo the exact `requestId` from the corresponding request.
- `ok: true`: `payload` will be an object or `null`, and `error` must be `null`.
- `ok: false`: `payload` must be `null`, and `error` must be populated with a valid error object.
- `type` should match the original request type where possible.

## 7. Error Conventions
- Errors must use the standard `code`/`message`/`details` shape.
- `code` should be machine-readable uppercase snake style (e.g., `MALFORMED_REQUEST`, `UNSUPPORTED_TYPE`).
- `message` must be operator-safe and appropriate for display or logging.
- `details` must be non-sensitive and purely diagnostic.
- **Never** send stack traces to JS.
- **Never** expose DB paths, file paths, connection strings, tokens, PINs, or card data in error messages or details.

## 8. requestId Conventions
- JavaScript creates and owns the `requestId`.
- C# simply echoes the `requestId` back in the response.
- `requestId` is strictly for correlation of asynchronous messages, not for security or authentication.
- If a message is completely unparseable raw JSON (i.e. invalid JSON or missing basic envelope fields), the backend may return `"unrecognized"` for the `requestId` as defined by the schema examples.

## 9. Payload Conventions
- `payload` must be a JSON object or `null`.
- Avoid raw primitives (strings, numbers, booleans) directly as the payload; wrap them in an object.
- Do not log sensitive payloads.
- Business validation belongs in C# backend services, not in the JS UI helper layer.

## 10. Logging/Security Conventions
- **Safe logs:** Event source, message type, `requestId`, error code.
- **Unsafe logs:** Raw payload content, raw JSON strings, credentials, PINs, card data, token values.
- The JavaScript bridge helper should log safe metadata only.

## 11. Legacy Probe Note
- `transport.ping` / `transport.pong` is a legacy probe introduced in Milestone 3.1 and may remain during the transition period.
- Formal v1 request/response flows use the complete `version`/`requestId`/`ok`/`error` shape.

## 12. Future Router Rules
- Milestone 3.3 will introduce `PosWebMessageRouter`.
- Handler authors must not bypass these envelope conventions when adding features.
- All router and handler logic must use the shared DTOs (`BridgeRequestEnvelope`, `BridgeResponseEnvelope`) and the shared serializer options (`BridgeJsonSerializerOptions.Default`).

## 13. Checklist
When authoring a new bridge handler or reviewing code:
- [ ] Does it use `v1`?
- [ ] Does it echo the `requestId`?
- [ ] Does it use `BridgeJsonSerializerOptions.Default`?
- [ ] Does it avoid raw payload logging?
- [ ] Does it return structured errors for failures?
- [ ] Does it preserve the `payload`/`error` envelope shape?
