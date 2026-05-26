# Operator Session Model

This document describes the design, lifetime, and constraints of the operator session model in `POS.Desktop`.

## Lifetime and Storage

> [!IMPORTANT]
> The operator session is stored strictly in **process memory (in-memory only)**.
> It exists only for the lifetime of the running terminal desktop application.

- **No Persistence to SQLite:** The session state is not saved to the SQLite database. If the application crashes, restarts, or is closed, the session is implicitly destroyed.
- **Single Terminal / Single Operator:** The terminal operates in kiosk mode under a single active operator session at any given time.
- **Dependency Injection:** `ISessionService` (implemented by `OperatorSessionService`) is registered as a **Singleton** service in the C# Generic Host container.

## Security Constraints

> [!WARNING]
> To comply with security requirements, the session model does **not** store any sensitive authentication or payment data.

- **No Sensitive Fields:** The session data **must not** store operator PINs, passwords, cryptographic tokens, card details, or payment information.
- **Safe Data Only:** It contains only safe, non-sensitive metadata for identification and display purposes.
- **Bridge Exposure:** The UI layer retrieves this safe session info through the JavaScript-to-C# bridge using the `session.get` action.

## Data Structures

The C# representation is defined in [OperatorSession.cs](../../POS.Desktop/Services/Session/OperatorSession.cs):

```csharp
public sealed record OperatorSession(
    string OperatorId,
    string DisplayName,
    string Role,
    DateTimeOffset LoginTime,
    string? TerminalId = null,
    string? SessionId = null);
```

### JSON Schema (Bridge-serialized Payload)

When queried via the bridge request `session.get`, the returned payload has the following structure:

```json
{
  "isActive": true,
  "currentSession": {
    "operatorId": "op-123",
    "displayName": "Jane Doe",
    "role": "Manager",
    "loginTime": "2026-05-27T03:00:00Z",
    "terminalId": "term-99",
    "sessionId": "sess-abc"
  }
}
```

If no session is currently active:

```json
{
  "isActive": false,
  "currentSession": null
}
```

## Bridge Interface

The session lifecycle is accessed by the HTML/JS UI screens via two primary bridge actions:

1. **`session.get`**
   - **Purpose:** Retrieves the current active operator session info.
   - **Response Payload:** Contains `isActive` (boolean) and `currentSession` (object or null).
2. **`session.clear`**
   - **Purpose:** Destroys the current in-memory operator session. Used upon logout or shift close.
   - **Response Payload:** Contains `cleared: true` and `isActive: false`.

## Boundaries & Non-Goals

- **Login & PIN Validation:** The actual verification of operator PINs is out of scope for Milestone 3.4. Stubs or mock authentication validation will be introduced in Milestone 3.5.
- **LocalStorage Migration:** The removal of `localStorage.terminal_operator` and related local storage items on the login screen is deferred to Milestone 3.5. Until then, the UI and local storage writes remain as-is, but the C# session service stands ready as the future source of truth.
- **UI State Source of Truth:** Once bridge-backed login is fully implemented in Milestone 3.5, the C# operator session service will be the single source of truth for operator state. The UI must transition to treating C# session data as authoritative, and avoid caching operator identities locally in the browser context.
