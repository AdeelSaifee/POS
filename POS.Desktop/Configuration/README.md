# POS.Desktop Configuration README

## Overview
As part of the Milestone 1.2 "Generic Host bootstrap", the dependency injection and application lifecycle management are being migrated from a manual `ServiceCollection` to the standard .NET Generic Host (`Microsoft.Extensions.Hosting`).

## Composition Root Layout
To maintain a clean and centralized architecture, the following structure is adopted:

### 1. Host Builder Factory
- **Location:** `POS.Desktop/Configuration/DesktopHostBuilder.cs`
- **Role:** Encapsulates the creation and configuration of the `IHostBuilder`. It handles:
  - Standard WPF app configuration (appsettings.json).
  - Logging configuration.
  - Integration with the Service Registration Helper.

### 2. Service Registration Helpers
- **Location:** `POS.Desktop/Configuration/ServiceRegistrationHelper.cs`
- **Role:** Provides extension methods for `IServiceCollection` to group registrations:
  - `AddInfrastructureServices()`: Logging, Configuration, etc.
  - `AddDataServices()`: `PosLocalDbContext` and SQLite related services.
  - `AddShellServices()`: `MainWindow`, `WebViewHost`, and UI-specific logic.
  - `AddBusinessServices()`: Services for Provisioning, Auth, Shifts, Orders, etc.

### 3. Application Entry Point
- **Location:** `POS.Desktop/App.xaml.cs`
- **Role:** Holds the `IHost` instance.
  - `OnStartup`: Builds and starts the host, resolves the initial window.
  - `OnExit`: Gracefully stops and disposes the host.

## Rationale
- **Centralization:** All DI logic is confined to the `Configuration/` folder.
- **Maintainability:** Clear grouping of services prevents `App.xaml.cs` from becoming a "God Class".
- **Consistency:** Follows idiomatic .NET patterns while respecting WPF's unique lifecycle.
