# WebView2 Bridge Transport Options

## 1. Purpose
This document records the transport decision reached after the completion of Milestone 3.1. It serves as a guide for future Phase 3 bridge development to ensure architectural consistency, security, and maintainability.

## 2. Current Transport Components
The following foundational components have been implemented:
- **C# Shell Inbound:** `WebMessageReceived` event handler in `WebViewHost.cs`.
- **C# Shell Outbound:** `PostWebMessageAsJson` method in `WebViewHost.cs`.
- **C# Host Object:** `PosHostApi` skeleton (COM-visible) in `POS.Desktop.Shell`.
- **JS Shell Inbound:** `message` event listener in `pos-bridge-transport.js`.
- **JS Shell Outbound:** `window.chrome.webview.postMessage` API.
- **JS Host Object Access:** `window.chrome.webview.hostObjects.pos` reference.
- **JS Transport Helper:** `pos-bridge-transport.js` shared helper for connectivity verification.

## 3. When to use `postMessage` / `WebMessageReceived`
This is the **primary message bus** for the application.

Use this for:
- **Event-driven communication:** UI notifying the shell of actions (e.g., "item_added", "payment_started").
- **Asynchronous requests:** Commands that trigger background C# services (auth, database, hardware).
- **Contract-based messaging:** Future envelope-based requests that require validation and routing.
- **Broadcasts:** C# shell pushing updates to the UI (e.g., "sync_status_changed").

**Rationale:** This pattern decoupling JS from C# implementation details, supports async/await patterns naturally, and allows for a central router (Task 3.3) to manage dispatching.

## 4. When to use the Host Object (`pos`)
The host object should be used **sparingly**.

Use this for:
- **Direct Shell APIs:** Simple, synchronous capabilities that don't involve complex business logic.
- **Support & Status:** Direct calls for bridge health checks or shell environment metadata.
- **Scoped host-only functions:** Cases where exposing a specific C# method directly to JS is architecturally cleaner than a message/router flow.

**Rationale:** Host objects provide a more "native" API feel but can lead to tighter coupling if overused for business logic.

## 5. Security & Safety Rules
To maintain the integrity of the POS terminal, the following rules apply:
- **No Business Logic in JS:** JavaScript remains a view-only layer. All calculations and state transitions happen in C# services.
- **No Direct DB Access:** UI never calls database APIs directly; it requests data through the bridge.
- **Sensitive Data Protection:** 
    - Never log raw PINs, card data, tokens, or operator credentials.
    - Never log full JSON payloads in production.
    - Log only safe message types and sources.
- **Validation:** All inbound bridge messages must be validated in C# before dispatching to services.

## 6. Logging Guidelines
- Use structured logging in C# (e.g., `_logger.LogDebug("Inbound message [Type: {Type}]", messageType)`).
- Use `console.debug` in JavaScript for transport-level visibility.
- Ensure malformed JSON does not crash the shell; use defensive parsing (`JsonDocument`).

## 7. Future Impact
- **Milestone 3.2:** Will define the formal message envelope and DTO schema.
- **Milestone 3.3:** Will introduce the `PosWebMessageRouter` to replace manual `if/else` logic in `WebViewHost`.
- **Milestones 3.4+:** Will begin replacing `localStorage` with real C# services using these transport options.

## 8. Summary Decision
- **Main Bus:** `postMessage` / `WebMessageReceived`
- **Utility/Direct API:** `window.chrome.webview.hostObjects.pos`
- **Helper:** Keep `pos-bridge-transport.js` thin and strictly focused on transport delivery.
