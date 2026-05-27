# POS.Desktop Data-Access Conventions

This document defines the architectural patterns and conventions for data access in `POS.Desktop`. Adhering to these rules prevents diagnostic drift, memory leaks, and tenant data leaks in our offline-first local SQLite environment.

---

## 1. DbContext Lifetime and Scope Rules

* **Lifetime Registration:** `PosLocalDbContext` is registered in `DesktopHostBuilder.cs` using `services.AddDbContext<PosLocalDbContext>()` which defaults to a **Scoped** lifetime.
* **No Singleton/Transient Contexts:** Never register or instantiate the DbContext as a Singleton or Transient, and never use a single context instance concurrently across threads.
* **Tied to Asynchronous Operations:** A DbContext instance's life is bound strictly to the logical scope of a single request or message. When the scope ends, the context must be disposed to close active connections.

---

## 2. Per-Message DI Scope Pattern

Each request coming from the WebView2 JavaScript client follows a standardized, decoupled DI scope flow:
1. **Web Request:** JS client invokes bridge transport with a request envelope (including `requestId`, `version`, `type`, and `payload`).
2. **Envelope Routing:** `PosWebMessageRouter.RouteAsync` intercepts the request.
3. **DI Scope Creation:** The router creates a new scoped provider instance:
   ```csharp
   using var scope = _scopeFactory.CreateScope();
   ```
4. **Service Resolution:** Scoped business services (e.g. `ICatalogService`, `ITerminalProvisioningStore`) are resolved from `scope.ServiceProvider`. These services implicitly receive the scoped `PosLocalDbContext` via constructor injection.
5. **Execution & Return:** The resolved handler executes, maps its response to a safe response envelope, and returns the response.
6. **Automatic Disposal:** When exiting the `using` block, the DI scope is disposed. This automatically disposes the resolved `PosLocalDbContext` and releases database connection handles back to the SQLite connection pool.

```
JS Request -> WebView2 -> PosWebMessageRouter.RouteAsync()
                               |
                        [Create Scope]
                               |
                     [Resolve Scoped Services] (e.g., ICatalogService, DbContext)
                               |
                       [Execute Handler]
                               |
                        [Return Success/Fail]
                               |
                       [Dispose Scope] (Releases DbContext & SqliteConnection)
```

---

## 3. Service Layer and Handler Boundaries

To keep the application modular and testable, we enforce strict logical boundaries:

* **Thin Bridge Handlers:** Bridge handlers in `PosWebMessageRouter` must remain thin. Their responsibilities are:
  * Checking payload structure and parameter types (e.g. JSON schema validation).
  * Dispatching work to the C# service layer.
  * Capturing exceptions and converting them to correlation-aware, safe `BridgeResponseEnvelope.Failure` responses.
* **No Direct DB Access from Handlers:** Handlers must never query or save changes directly using `PosLocalDbContext`. They must interact with scoped interfaces (e.g. `ICatalogService`).
* **No C# Business Logic in JS:** The WebView2 frontend is purely presentational. All calculation, validation, and transaction logic must occur within C# backend services.

---

## 4. Tenant Scoping and Global Query Filters

Our SQLite DB is shared across tenants but strictly segregated using EF Core query filters:

* **Automatic Filtering:** `PosLocalDbContext` configures global filters for all local entities, e.g.:
  ```csharp
  modelBuilder.Entity<LocalItem>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
  ```
* **Fail-Closed Unprovisioned Behavior:** If a terminal is unprovisioned, `CurrentTenantId` evaluates to `0` (`InvalidTenantId`). No query will match a tenant ID of 0, resulting in empty returns (fail-closed) rather than data leakage.
* **Rule against Ignoring Filters:** Never use `IgnoreQueryFilters()` in normal services. Its use is restricted to:
  * Database initialization and provisioning seeding.
  * Low-level synchronization outbox queries when synchronizing with the central server.
  * These exceptions must be explicitly justified and documented.

---

## 5. Service Design Patterns (Phase 5 Guidelines)

### A. Read-Only Service Pattern
Services that only read data (like catalog list/search) should optimize for speed and memory hygiene:
* **No Tracking:** Use `.AsNoTracking()` on EF Core queries to bypass EF tracking caches.
* **Map to DTOs:** Never return raw EF entity classes directly to the bridge. Project queries directly into flat, immutable DTOs (e.g. `CatalogItemDto`) to limit data exposure.

```csharp
public async Task<IReadOnlyList<CatalogCategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken = default)
{
    return await _db.LocalCategories
        .AsNoTracking()
        .OrderBy(c => c.SortOrder)
        .Select(c => new CatalogCategoryDto
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.Name
        })
        .ToListAsync(cancellationToken);
}
```

### B. Write Service Pattern
Services that perform mutations must validate state before persisting changes:
* **Validation before Persistence:** Business rules, state check constraints, and validation checks must run in C# before invoking DB updates.
* **Transactional Operations:** Single logical modifications should call `SaveChangesAsync(cancellationToken)` once.

```csharp
public async Task<WriteResult> UpdateItemStatusAsync(int itemId, ItemStatus newStatus, CancellationToken cancellationToken)
{
    // 1. Validation
    var item = await _db.LocalItems.FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken);
    if (item == null)
        return WriteResult.Failure("ITEM_NOT_FOUND", "Item does not exist.");

    if (item.Status == ItemStatus.Blocked && newStatus == ItemStatus.Active)
        return WriteResult.Failure("INVALID_STATE", "Blocked items must be unlocked by a manager first.");

    // 2. Mutation
    item.Status = newStatus;

    // 3. Persist
    await _db.SaveChangesAsync(cancellationToken);
    return WriteResult.Success();
}
```

### C. Append-Only Transaction Flow Rule
* To ensure audit integrity, tables capturing financial or session changes (e.g., sales, orders, shift logs) must follow an **append-only** flow.
* Insert new records rather than updating existing lines. Do not alter line item prices, quantities, or tax components post-completion. Corrections must be handled via void records or offset transactions.

---

## 6. Async/Await & Disposal Guidelines

* **Fully Async Paths:** All data access operations must be async. Ensure `cancellationToken` is propagated down to all EF Core execution paths (e.g. `ToListAsync(cancellationToken)`, `FirstOrDefaultAsync(cancellationToken)`).
* **Scope Disposables:** Wrap temporary resources in `using` blocks to prevent leakages of system handles or memory.

---

## 7. Security and Logging Guidelines

* **No Secrets/Sensitive Data:** Never store or log PINs, cleartext passwords, OAuth tokens, credit card PANs, security keys, or database connection strings.
* **Safe Diagnostics:** Log only structural metadata such as `requestId`, message `type`, error codes, and record counts.
* **No Raw Payload Logging:** Never log the full request payload or raw JSON strings containing operator inputs.
* **Safe Error Message Returns:** Do not return stack traces, internal file paths, or raw DB exception details in response envelopes. Keep messages general and friendly, using code strings (e.g. `VALIDATION_FAILED`) for diagnostics.
