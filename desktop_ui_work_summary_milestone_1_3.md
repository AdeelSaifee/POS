# Desktop UI Integration Work Summary — Milestone 1.3

**Project:** IMAGYN POS Desktop UI Integration  
**Milestone:** 1.3 — Full-screen kiosk shell hosting WebView2  
**Task Range Covered:** Task 1.3.1 through Task 1.3.10  
**Main Desktop Project:** `POS.Desktop`  
**Solution File:** `POS.slnx`  
**Working Branch:** `main`  
**Target Framework:** `net8.0-windows`  
**UI Hosting Strategy:** WPF shell + WebView2  
**Status:** Completed and verified  
**Last updated:** 2026-05-23

---

## 1. Executive Summary

Milestone 1.3 converted the previously blank WPF `MainWindow` into a full-screen kiosk-style WebView2 shell.

By the end of this milestone:

- `MainWindow` opens as a borderless maximized shell.
- A single `WebView2` WPF control named `MainWebView` fills the window.
- A dedicated `WebViewHost` class exists under `POS.Desktop/Shell/`.
- WebView2 uses an explicit per-user data folder under `%LocalAppData%`.
- `CoreWebView2Environment` is created with that explicit user-data folder.
- `EnsureCoreWebView2Async(...)` is awaited before any render/navigation work.
- Initialization ordering is guarded through `EnsureInitialized()`.
- A minimal placeholder page is rendered using `NavigateToString(...)`.
- Initialization failure is surfaced to the user via `MessageBox` and the app shuts down cleanly.
- Full-screen/borderless presentation was verified using Windows window-style inspection.
- Placeholder rendering stability was verified with repeated launches.
- No real prototype screen routing, virtual host mapping, JS bridge, database migration startup, or Phase 2 asset ingestion was implemented in this milestone.

Milestone 1.3 is now a stable baseline for the next milestone: **Milestone 1.4 — Startup database migration and first-run readiness**.

---

## 2. Starting Point Before Milestone 1.3

Before this milestone:

- Milestone 1.1 was complete.
- Milestone 1.2 was complete and reviewed as PASS.
- `POS.Desktop` already targeted `net8.0-windows`.
- `Microsoft.Web.WebView2` was already installed.
- Generic Host startup was already implemented.
- `MainWindow` was already resolved from DI.
- `App.xaml` no longer used `StartupUri`.
- `App.xaml.cs` started/stopped the Generic Host and resolved `MainWindow` from DI.
- `appsettings.json` was already copied to output so host configuration could load it at runtime.

The purpose of Milestone 1.3 was not to wire the real prototype UI yet. It was only to create a stable desktop shell that can host web content safely.

---

## 3. Final Runtime Flow After Milestone 1.3

```text
App starts
  ↓
Generic Host starts
  ↓
MainWindow is resolved from DI
  ↓
MainWindow.Show()
  ↓
MainWindow Loaded event fires
  ↓
WebViewHost.InitializeAsync()
  ↓
Resolve WebView2 user-data folder
  ↓
Create user-data directory if needed
  ↓
Create CoreWebView2Environment
  ↓
Await EnsureCoreWebView2Async(environment)
  ↓
Mark WebView2 as initialized
  ↓
Render temporary placeholder page via NavigateToString
  ↓
App runs as full-screen kiosk shell
```

---

## 4. Final Files Involved

### Modified or created during Milestone 1.3

```text
POS.Desktop/MainWindow.xaml
POS.Desktop/MainWindow.xaml.cs
POS.Desktop/Shell/WebViewHost.cs
POS.Desktop/appsettings.json
```

### Verification-only tasks with no final source changes

```text
Task 1.3.9
Task 1.3.10
```

---

## 5. Current MainWindow.xaml State

`MainWindow.xaml` is now a full-screen, borderless WPF shell containing one WebView2 control.

Important properties:

```xml
Title="IMAGYN POS"
WindowStyle="None"
WindowState="Maximized"
ResizeMode="NoResize"
WindowStartupLocation="CenterScreen"
```

The root `Grid` contains:

```xml
<wv2:WebView2 x:Name="MainWebView" />
```

Meaning:

- The normal Windows title bar is removed.
- Minimize/maximize/close caption buttons are removed.
- The app opens maximized.
- The user cannot resize the window through normal desktop chrome.
- The WebView2 control occupies the shell area.

---

## 6. Current MainWindow.xaml.cs State

`MainWindow.xaml.cs` now receives `IConfiguration` through DI and creates the shell host wrapper:

```csharp
_webViewHost = new WebViewHost(MainWebView, configuration);
Loaded += MainWindow_Loaded;
```

On load:

```csharp
await _webViewHost.InitializeAsync();
```

If initialization fails:

```csharp
MessageBox.Show(
    $"WebView2 initialization failed: {ex.Message}\n\nThe application will now shut down.",
    "Initialization Error",
    MessageBoxButton.OK,
    MessageBoxImage.Error);

Application.Current.Shutdown();
```

This prevents a broken blank terminal window from staying open if WebView2 cannot initialize.

---

## 7. Current WebViewHost.cs State

`WebViewHost` owns WebView2 shell responsibilities:

- WebView2 control reference.
- Configuration reference.
- User-data folder resolution.
- CoreWebView2 environment creation.
- Initialization guard.
- Placeholder rendering.
- Future stubs for virtual host mapping, JS bridge, and real navigation.

Important fields:

```csharp
private readonly WebView2 _webView;
private readonly IConfiguration _configuration;
private bool _isInitialized;
```

Important initialization sequence:

```csharp
var userDataFolder = ConfigureUserDataFolder();
Directory.CreateDirectory(userDataFolder);
var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
await _webView.EnsureCoreWebView2Async(environment);
_isInitialized = true;
RenderPlaceholderPage();
```

Important guard:

```csharp
private void EnsureInitialized()
{
    if (!_isInitialized || _webView.CoreWebView2 is null)
    {
        throw new InvalidOperationException(
            "WebView2 must be initialized before navigation or bridge operations.");
    }
}
```

Future methods currently remain guarded stubs:

```csharp
ConfigureVirtualHostMapping();
RegisterMessageBridge();
NavigateToInitialScreen();
```

These are intentionally not implemented yet.

---

## 8. Current appsettings.json State

The desktop app now contains a WebView2 configuration section:

```json
"WebView2": {
  "UserDataFolder": "IMAGYN/POS/Desktop/WebView2"
}
```

The final resolved runtime path is:

```text
%LocalAppData%\IMAGYN\POS\Desktop\WebView2
```

Example verified machine path:

```text
C:\Users\adeel\AppData\Local\IMAGYN\POS\Desktop\WebView2
```

WebView2 creates its internal data folder under:

```text
%LocalAppData%\IMAGYN\POS\Desktop\WebView2\EBWebView
```

This path is user-scoped and writable without administrator access, which is safer for locked-down POS terminals than writing under `Program Files`.

---

# 9. Task-by-Task Summary

---

## Task 1.3.1 — Define a Borderless Full-Screen Window

### Purpose

Turn the blank WPF `MainWindow` into a kiosk-style POS shell.

### Files changed

```text
POS.Desktop/MainWindow.xaml
```

### Changes made

Added shell/window chrome settings:

```xml
Title="IMAGYN POS"
WindowStyle="None"
WindowState="Maximized"
ResizeMode="NoResize"
WindowStartupLocation="CenterScreen"
```

Removed hardcoded development sizing:

```xml
Height="450"
Width="800"
```

### Result

The app opens as a maximized borderless shell.

### Safe

- Correct for POS/kiosk terminals.
- Prevents accidental resize/minimize through default Windows chrome.

### Risk

- Default Windows close/minimize/maximize buttons are no longer visible.
- A future app-level exit/logout/admin-close flow will be needed.

### Verification

- `POS.Desktop` build passed.
- Full solution build passed.
- App launched with title `IMAGYN POS`.
- Window opened without normal desktop chrome.

### Commit reference

```text
970d415 — feat(ui): update MainWindow properties for branding and fullscreen settings
```

---

## Task 1.3.2 — Add the WebView2 Control

### Purpose

Place a single WebView2 control into the full-screen shell.

### Files changed

```text
POS.Desktop/MainWindow.xaml
```

### Changes made

Added WebView2 XML namespace:

```xml
xmlns:wv2="clr-namespace:Microsoft.Web.WebView2.Wpf;assembly=Microsoft.Web.WebView2.Wpf"
```

Added a single WebView2 control:

```xml
<wv2:WebView2 x:Name="MainWebView" />
```

### Result

The full-screen shell now contains the WebView2 host region.

### Safe

- Minimal XAML-only change.
- No CoreWebView2 initialization yet.
- No navigation yet.
- No prototype files imported.

### Verification

- `POS.Desktop` build passed.
- Full solution build passed.
- App launched successfully.
- MainWindow still closed cleanly.

### Commit reference

```text
621012b — feat(ui): add WebView2 control to MainWindow for improved web content integration
```

---

## Task 1.3.3 — Outline the WebViewHost Responsibilities

### Purpose

Create a dedicated shell class to keep WebView2 lifecycle concerns out of WPF code-behind.

### Files changed

```text
POS.Desktop/Shell/WebViewHost.cs
POS.Desktop/MainWindow.xaml.cs
```

### Changes made

Created:

```text
POS.Desktop/Shell/WebViewHost.cs
```

Added a `WebViewHost` class skeleton with responsibility stubs for:

- Initialization.
- User-data folder setup.
- Virtual host mapping.
- Initial navigation.
- JS-to-C# bridge registration.
- Failure handling.

Wired `WebViewHost` from `MainWindow.xaml.cs`.

### Result

WebView2 shell responsibilities moved into a dedicated class.

### Safe

- Correct separation of shell concerns.
- Keeps `MainWindow.xaml.cs` minimal.

### Risk / Important Guardrail Note

One commit associated with this stage also changed prototype files under `docs/ui-prototype/screens/*` by renaming manager demo data from `Nadia Mirza` to `Zainab Malik` in:

```text
docs/ui-prototype/screens/main_checkout.html
docs/ui-prototype/screens/terminal_login.html
```

This was outside the stated Milestone 1.3 scope and should be reviewed separately. If the change was not intentional, it should be reverted before Phase 2 parity work, because prototype screens are supposed to remain the visual source of truth.

### Verification

- `POS.Desktop` build passed.
- Full solution build passed.
- Launch smoke test passed after allowing enough initialization time.

### Commit reference

```text
6431cb9 — feat(ui): integrate WebViewHost into MainWindow and update manager details in UI prototype
```

---

## Task 1.3.4 — Choose an Explicit User-Data Folder

### Purpose

Avoid relying on WebView2 default user-data folder behavior by choosing a known writable location.

### Files changed

```text
POS.Desktop/appsettings.json
POS.Desktop/Shell/WebViewHost.cs
POS.Desktop/MainWindow.xaml.cs
```

### Changes made

Added configuration:

```json
"WebView2": {
  "UserDataFolder": "IMAGYN/POS/Desktop/WebView2"
}
```

Injected `IConfiguration` into `MainWindow`, then passed it into `WebViewHost`.

Implemented user-data folder path resolution:

```csharp
var subFolder = _configuration["WebView2:UserDataFolder"] ?? "IMAGYN/POS/Desktop/WebView2";
var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
return Path.Combine(localAppData, subFolder);
```

### Result

The app now has a deterministic WebView2 user-data folder path:

```text
%LocalAppData%\IMAGYN\POS\Desktop\WebView2
```

### Safe

- User-scoped path.
- Does not require administrator permissions.
- Better for terminal deployment than random/default engine state.

### Risk

- The setting currently stores a relative subfolder, then combines it with `%LocalAppData%`.
- If operators need a fully custom absolute path later, path handling may need refinement.

### Verification

- `POS.Desktop` build passed.
- Full solution build passed.
- App launch verified.
- Commit was pushed to `main`.

### Commit reference

```text
81d1d3f — Define WebView2 user data folder path
```

---

## Task 1.3.5 — Initialize the CoreWebView2 Environment

### Purpose

Create the real WebView2 environment using the explicit user-data folder.

### Files changed

```text
POS.Desktop/Shell/WebViewHost.cs
POS.Desktop/MainWindow.xaml.cs
```

### Changes made

Added:

```csharp
using Microsoft.Web.WebView2.Core;
```

Implemented initialization:

```csharp
var userDataFolder = ConfigureUserDataFolder();
Directory.CreateDirectory(userDataFolder);
var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
await _webView.EnsureCoreWebView2Async(environment);
```

Triggered initialization through WPF `Loaded` event:

```csharp
Loaded += MainWindow_Loaded;
```

`MainWindow_Loaded` calls:

```csharp
await _webViewHost.InitializeAsync();
```

### Error-handling refinement

An internal `try/catch` was removed from `WebViewHost.InitializeAsync()` so initialization exceptions bubble up to `MainWindow_Loaded`. This avoids silent initialization failures.

### Result

CoreWebView2 initializes using the explicit user-data folder.

### Safe

- Initialization happens after WPF loaded event.
- Constructor is not async.
- Exceptions are visible to caller.

### Risk

- `async void` event handlers are normal in WPF, but errors must be caught carefully. This is handled in `MainWindow_Loaded`.

### Verification

- `POS.Desktop` build passed.
- Full solution build passed.
- Runtime verification confirmed creation of:

```text
%LocalAppData%\IMAGYN\POS\Desktop\WebView2\EBWebView
```

- App launched with title `IMAGYN POS`.
- `CloseMainWindow()` succeeded.
- Graceful shutdown verified.

### Commit reference

```text
7d72c7c — feat(ui): initialize WebView2 asynchronously and handle initialization errors in MainWindow
```

---

## Task 1.3.6 — Await EnsureCoreWebView2Async Before Navigation

### Purpose

Make initialization ordering explicit so future navigation and bridge operations cannot run before WebView2 is ready.

### Files changed

```text
POS.Desktop/Shell/WebViewHost.cs
```

### Changes made

Added initialization state:

```csharp
private bool _isInitialized;
```

Set state after successful initialization:

```csharp
await _webView.EnsureCoreWebView2Async(environment);
_isInitialized = true;
```

Added guard:

```csharp
private void EnsureInitialized()
{
    if (!_isInitialized || _webView.CoreWebView2 is null)
    {
        throw new InvalidOperationException(
            "WebView2 must be initialized before navigation or bridge operations.");
    }
}
```

Added guard calls inside future stubs:

```csharp
ConfigureVirtualHostMapping();
RegisterMessageBridge();
NavigateToInitialScreen();
```

### Result

Future navigation/bridge/mapping logic has an explicit safe ordering check.

### Safe

- Prevents race conditions.
- Does not add actual navigation.
- Does not add bridge logic.

### Verification

- `POS.Desktop` build passed.
- Full solution build passed.
- Runtime launch verified.
- `EBWebView` folder found.
- Graceful shutdown verified.

### Commit reference

```text
ece6a7c — feat(ui): add initialization guard for WebView2 and enforce checks in dependent methods
```

---

## Task 1.3.7 — Render a Placeholder Page

### Purpose

Confirm that WebView2 can render content after initialization.

### Files changed

```text
POS.Desktop/Shell/WebViewHost.cs
```

### Changes made

After successful initialization:

```csharp
RenderPlaceholderPage();
```

Added method:

```csharp
private void RenderPlaceholderPage()
{
    EnsureInitialized();
    _webView.CoreWebView2.NavigateToString(html);
}
```

Placeholder content:

```text
IMAGYN POS Desktop Shell
WebView2 initialized successfully.
```

Simple placeholder styling:

```text
Background: #202020
Text: #A8E63D
Centered layout
```

### Result

The shell renders a minimal temporary placeholder page.

### Safe

- Uses `NavigateToString(...)`.
- No production asset pipeline yet.
- No prototype files copied.
- No virtual host mapping.
- No JS bridge.

### Risk

- This placeholder is temporary and must be replaced by real screen hosting in Phase 2.

### Verification

- `POS.Desktop` build passed.
- Full solution build passed.
- Runtime launch passed.
- `EBWebView` folder verified.
- Window title `IMAGYN POS` verified.
- Graceful shutdown verified.

### Commit reference

```text
c3a83ed — Render WebView2 placeholder page
```

---

## Task 1.3.8 — Handle Initialization Failure

### Purpose

Make WebView2 initialization failure visible and prevent the app from staying open in a broken blank state.

### Files changed

```text
POS.Desktop/MainWindow.xaml.cs
```

### Changes made

Updated `MainWindow_Loaded` catch block.

Before:

```csharp
MessageBox.Show($"WebView2 initialization failed: {ex.Message}", ...);
```

After:

```csharp
MessageBox.Show($"WebView2 initialization failed: {ex.Message}\n\nThe application will now shut down.", ...);
Application.Current.Shutdown();
```

### Result

If initialization fails:

1. User sees a clear error message.
2. App shuts down instead of staying open as a broken kiosk shell.

### Safe

- Minimal WPF-level failure handling.
- Does not overbuild diagnostics.
- Full runtime guard/logging is still deferred to Milestone 1.5.

### Verification

- `POS.Desktop` build passed.
- Full solution build passed.
- Success path smoke test passed.
- Failure path was verified by code audit, not destructive runtime simulation.

### Commit reference

```text
4c66e7c — feat(ui): handle WebView2 initialization failure by shutting down application
```

---

## Task 1.3.9 — Verify Full-Screen Presentation

### Purpose

Verify that the kiosk shell really opens borderless and maximized/full-screen.

### Files changed

```text
None
```

### Verification performed

Inspected `MainWindow.xaml` and confirmed:

```xml
WindowStyle="None"
WindowState="Maximized"
ResizeMode="NoResize"
WindowStartupLocation="CenterScreen"
Title="IMAGYN POS"
<wv2:WebView2 x:Name="MainWebView" />
```

Built solution:

```powershell
dotnet build POS.slnx --configuration Debug
```

Runtime verification used Windows APIs through PowerShell:

- `GetWindowLong(...)`
- `GetWindowRect(...)`

Observed evidence:

```text
WindowTitle: IMAGYN POS
HasCaption: False
WindowStyleHex: 0x17080000
Width: 1536
Height: 864
WebView2DataFolderExists: True
CloseMainWindow returned: True
Graceful shutdown successful
```

### Result

The shell was objectively verified as borderless and maximized.

### Safe

- Verification-only task.
- No source changes.
- Working tree stayed clean.

### Limitation

CLI inspection cannot visually inspect pixels directly. However, window style bitmask and window bounds provide strong objective evidence.

---

## Task 1.3.10 — Confirm Placeholder Renders Post-Init

### Purpose

Confirm init → placeholder render works reliably across repeated launches.

### Files changed

```text
None
```

### Verification performed

Built solution:

```powershell
dotnet build POS.slnx --configuration Debug
```

Ran three launch cycles.

Each launch verified:

- Process started.
- Window title was `IMAGYN POS`.
- `EBWebView` folder existed.
- App remained responsive.
- `CloseMainWindow()` worked.
- Graceful shutdown succeeded.

Observed result:

```text
Launch #1: Success
Launch #2: Success
Launch #3: Success
```

### Result

Placeholder rendering path is stable enough to be used as the baseline for the next milestone.

### Safe

- Verification-only task.
- No source changes.
- Working tree stayed clean.

### Limitation

CLI could not directly inspect rendered pixels, but repeated successful WebView2 initialization, persistent `EBWebView` data, responsive window state, and graceful shutdown provide strong confidence.

---

# 10. Verification Summary

## Build verification

Repeatedly verified during the milestone:

```powershell
dotnet build POS.Desktop/POS.Desktop.csproj --configuration Debug
dotnet build POS.slnx --configuration Debug
```

Final verification:

```powershell
dotnet build POS.slnx --configuration Debug
```

Result:

```text
Build succeeded
```

## Runtime verification

Verified repeatedly:

- App starts.
- `MainWindow` appears.
- Window title is `IMAGYN POS`.
- WebView2 initializes.
- User-data folder exists.
- `EBWebView` folder exists.
- Placeholder render path executes.
- App remains responsive.
- App closes cleanly through `CloseMainWindow()`.

## Full-screen verification

Verified through:

- `WindowStyle="None"` in XAML.
- `WindowState="Maximized"` in XAML.
- Windows style inspection with `HasCaption: False`.
- Window bounds matching the screen dimensions used during verification.

---

# 11. What Was Intentionally Not Done

The following items were intentionally left for later milestones/phases:

- No startup SQLite migration hook.
- No local database first-run migration flow.
- No WebView2 runtime guard beyond basic failure handling.
- No virtual host mapping.
- No real prototype screen navigation.
- No `docs/ui-prototype/screens/*` production hosting.
- No `Assets/ui` ingestion pipeline.
- No JS-to-C# bridge.
- No host object registration.
- No business logic in HTML/JS.
- No POS checkout workflow wiring.
- No hardware integration.
- No sync/outbox integration changes.
- No Phase 2 prototype parity work.

---

# 12. Important Safety Notes

## Safe

- Main WPF shell now has a stable WebView2 host region.
- WebView2 user data is written to a predictable per-user location.
- Initialization happens asynchronously after WPF load.
- Navigation/bridge operations are guarded against running before initialization.
- Placeholder render verifies that WebView2 can display content.
- Failure path no longer leaves a broken blank full-screen shell open.

## Risky / Needs Review

- A commit during Task 1.3.3 changed prototype demo manager text in `docs/ui-prototype/screens/*`. That is outside the Milestone 1.3 guardrail and should be reviewed. If not intentionally approved, revert it before Phase 2 parity work.
- The placeholder page is temporary and should not be treated as final UI.
- Pixel-level verification of placeholder content was not possible from CLI. Repeated launch/runtime evidence was used instead.
- Full WebView2 runtime absence/incompatibility guard is still deferred to Milestone 1.5.
- A proper in-app close/logout/admin-exit flow is still needed later because the Windows title bar is hidden.

---

# 13. Current End State After Milestone 1.3

Current shell state:

```text
MainWindow
  - Borderless
  - Maximized
  - Non-resizable
  - Hosts one WebView2 control
  - Resolved through Generic Host DI

WebViewHost
  - Owns WebView2 initialization
  - Uses explicit user-data folder
  - Awaits CoreWebView2 initialization
  - Guards future operations
  - Renders temporary placeholder page
```

Current runtime path:

```text
%LocalAppData%\IMAGYN\POS\Desktop\WebView2\EBWebView
```

Current visual output:

```text
Temporary placeholder page:
"IMAGYN POS Desktop Shell"
"WebView2 initialized successfully."
```

---

# 14. Correct Next Milestone

The correct next milestone is:

```text
Milestone 1.4 — Startup database migration & first-run readiness
```

Primary goal of Milestone 1.4:

```text
Ensure the local SQLite schema exists before any screen or shell workflow depends on it.
```

Do not jump directly into Phase 2 prototype asset ingestion until Phase 1 is fully complete.

---

# 15. Recommended Next Action

Before starting Milestone 1.4:

1. Review the prototype-file changes introduced in commit `6431cb9`.
2. Decide whether the manager name changes in `docs/ui-prototype/screens/*` were intentional.
3. If not intentional, revert only those prototype changes.
4. Then start Task 1.4.1 — Add a startup migration hook.

---

## Final Verdict

Milestone 1.3 is functionally complete and verified.

The WPF desktop app now has a working full-screen WebView2 shell with controlled initialization, basic failure handling, and a temporary render target. This is the correct foundation for first-run database readiness in Milestone 1.4 and later real prototype screen hosting in Phase 2.
