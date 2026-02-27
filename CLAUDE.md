# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```powershell
# Build entire solution
dotnet build Bookkeeping.sln --nologo

# Run the WPF app
dotnet run --project Bookkeeping.App

# Run all tests
dotnet test Bookkeeping.Tests --nologo

# Run a single test by name
dotnet test Bookkeeping.Tests --filter "FullyQualifiedName~DashboardViewModelTests.LoadAsync_SetsKpiValues"

# Run a single test class
dotnet test Bookkeeping.Tests --filter "ClassName~DashboardViewModelTests"

# Smoke-test automation (builds then runs UIAutomation flow)
.\Automation\smoke-flow.ps1 -BuildFirst
```

## Architecture

Four projects with a strict dependency chain:

```
Bookkeeping.Core (net9.0)
  └─ Bookkeeping.Infrastructure (net9.0)
       └─ Bookkeeping.App (net9.0-windows, WPF)
            └─ Bookkeeping.Tests (net9.0-windows, UseWPF)
```

**Core** — pure C# models (`Account`, `Category`, `Transaction`), repository interfaces, and DTOs. No WPF, no EF.

**Infrastructure** — EF Core 9 + SQLite via `IDbContextFactory<BookkeepingContext>` (pooled, not singleton). `ServiceCollectionExtensions.AddInfrastructure()` is the single entry point that registers the factory, a singleton `ResiliencePipeline`, and all three repositories as Scoped. Every repository method wraps its DB call in `await _pipeline.ExecuteAsync(...)`. The Polly pipeline (Retry ×3 exponential + CircuitBreaker) is built in `ResiliencePolicies.BuildDbPipeline()`.

**App** — DI composition root is `App.xaml.cs` (`OnStartup`). Serilog is configured there before the host is built. Page ViewModels are registered as **singletons** (state persists within a session); `AddEditTransactionViewModel` is **transient** (fresh instance each dialog open).

**Tests** — must target `net9.0-windows` with `<UseWPF>true</UseWPF>` to reference the WPF App project. Uses xUnit 2 + Moq + FluentAssertions. All ViewModels are tested against mocked repository interfaces with `NullLogger<T>`.

## Navigation Pattern

`MainWindow` contains a sidebar and a single `ContentControl` bound to `MainViewModel.CurrentPage`. Navigation happens entirely through `INavigationService.NavigateTo<TViewModel>()`, which resolves the VM from DI, fires `NavigationChanged` (consumed by `MainViewModel` to update `CurrentPage`), then calls `IPageViewModel.LoadAsync()` if the VM implements it.

The mapping from ViewModel type to View is declared as typed `DataTemplate` entries in `App.xaml` — **no Frame, no code-behind navigation logic**.

To add a new page:
1. Create `XxxViewModel` implementing `ObservableObject` (and `IPageViewModel` if it loads data).
2. Create `XxxView.xaml` / `XxxView.xaml.cs` (UserControl, only `InitializeComponent()` in code-behind).
3. Add a `DataTemplate` entry in `App.xaml`.
4. Register the VM as singleton in `App.xaml.cs`.
5. Add a nav button in `MainWindow.xaml` with the matching `DataTrigger` for active state.
6. Add a `case` in `MainViewModel.NavigateTo`.

## Dialog Pattern

Dialogs are full Windows opened by `DialogService`. The `AddEditTransactionViewModel` exposes `event Action<bool>? RequestClose`. `AddEditTransactionDialog.xaml.cs` subscribes to it in the constructor and sets `DialogResult` — this is the **only** acceptable code-behind logic beyond `InitializeComponent()`.

## ViewModel Conventions

- All state properties use `[ObservableProperty]` generating `IsLoading`, `StatusMessage`, `IsStatusError` etc.
- Every `[ObservableProperty]` that should react to changes uses `partial void OnXxxChanged(...)`.
- When multiple properties need to change without triggering reactive side-effects (e.g. clearing filters), use a `_isResetting` guard flag.
- Validation ViewModels inherit `ObservableValidator` and use `[NotifyDataErrorInfo]` + `ValidateAllProperties()`. Use `GetErrors().Select(e => e.ErrorMessage)` — **not** `.ErrorMessages` (does not exist).
- `[RelayCommand]` generates `XxxCommand` and `XxxAsyncCommand` automatically; the async variant exposes `ExecuteAsync`.

## NuGet Version Constraints

| Package | Version | Reason |
|---|---|---|
| `Microsoft.EntityFrameworkCore.Sqlite` | `9.0.*` | 10.x requires .NET 10 |
| `Serilog` | `4.*` | `Serilog.Sinks.Console 6.x` requires ≥ 4.0 |
| `Polly` | `8.*` | Uses new `ResiliencePipeline` API (not `Policy`) |

When adding packages, always specify the major version with a wildcard (`--version "9.*"`) to avoid pulling an incompatible next-major release.

## Runtime Data Locations

| Resource | Path |
|---|---|
| SQLite database | `%LOCALAPPDATA%\Bookkeeping\bookkeeping.db` |
| Rolling log files | `%LOCALAPPDATA%\Bookkeeping\Logs\bookkeeping-YYYYMMDD.log` |

The DB is created and seeded automatically on first run via `DbInitializer.InitializeAsync()` (EF `EnsureCreated` — no migrations).

## UI Automation

All interactive controls expose `AutomationProperties.AutomationId` matching their logical name (e.g. `Nav_Transactions`, `Btn_AddTransaction`, `Field_Amount`). `Automation/smoke-flow.ps1` drives a full add-transaction flow using Windows UIAutomation COM APIs. `Automation/mcp-config.json` configures the WPF-MCP server for AI-driven automation via Claude Code.
