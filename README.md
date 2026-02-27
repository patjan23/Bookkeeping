# Bookkeeping — WPF Desktop App

A clean MVVM bookkeeping application built with .NET 9, WPF, CommunityToolkit.Mvvm, EF Core + SQLite, Polly, and Serilog.

---

## Tech Stack

| Component | Library |
|---|---|
| UI Framework | WPF (.NET 9) |
| MVVM | CommunityToolkit.Mvvm 8 |
| DI / Hosting | Microsoft.Extensions.Hosting 9 |
| Database | EF Core 9 + SQLite |
| Resilience | Polly 8 (RetryPolicy + CircuitBreaker) |
| Logging | Serilog 3 (Console + rolling file) |
| Unit Tests | xUnit 2 + Moq 4 + FluentAssertions 6 |

---

## Quick Start

### 1. Prerequisites
- Windows 10/11
- .NET 9 SDK (`dotnet --version` → `9.x`)
- Git (optional)

### 2. Clone / Open
```powershell
cd "D:\code\C#\desktop\wpf\test"
```

### 3. Scaffold the solution (first time only)

```powershell
# Create solution
dotnet new sln -n Bookkeeping

# Create projects
dotnet new classlib -n Bookkeeping.Core           -f net9.0         -o Bookkeeping.Core
dotnet new classlib -n Bookkeeping.Infrastructure -f net9.0         -o Bookkeeping.Infrastructure
dotnet new wpf     -n Bookkeeping.App             -f net9.0-windows  -o Bookkeeping.App
dotnet new xunit   -n Bookkeeping.Tests           -f net9.0-windows  -o Bookkeeping.Tests

# Add to solution
dotnet sln add Bookkeeping.Core/Bookkeeping.Core.csproj
dotnet sln add Bookkeeping.Infrastructure/Bookkeeping.Infrastructure.csproj
dotnet sln add Bookkeeping.App/Bookkeeping.App.csproj
dotnet sln add Bookkeeping.Tests/Bookkeeping.Tests.csproj

# Project references
dotnet add Bookkeeping.Infrastructure reference Bookkeeping.Core
dotnet add Bookkeeping.App            reference Bookkeeping.Core
dotnet add Bookkeeping.App            reference Bookkeeping.Infrastructure
dotnet add Bookkeeping.Tests          reference Bookkeeping.Core
dotnet add Bookkeeping.Tests          reference Bookkeeping.Infrastructure
dotnet add Bookkeeping.Tests          reference Bookkeeping.App

# NuGet — Infrastructure
dotnet add Bookkeeping.Infrastructure package Microsoft.EntityFrameworkCore.Sqlite
dotnet add Bookkeeping.Infrastructure package Polly
dotnet add Bookkeeping.Infrastructure package Polly.Extensions
dotnet add Bookkeeping.Infrastructure package Serilog
dotnet add Bookkeeping.Infrastructure package Serilog.Sinks.Console
dotnet add Bookkeeping.Infrastructure package Serilog.Sinks.File
dotnet add Bookkeeping.Infrastructure package Microsoft.Extensions.DependencyInjection
dotnet add Bookkeeping.Infrastructure package Microsoft.Extensions.Logging.Abstractions

# NuGet — App
dotnet add Bookkeeping.App package CommunityToolkit.Mvvm
dotnet add Bookkeeping.App package Microsoft.Extensions.Hosting
dotnet add Bookkeeping.App package Serilog.Extensions.Hosting
dotnet add Bookkeeping.App package Serilog.Sinks.Console
dotnet add Bookkeeping.App package Serilog.Sinks.File

# NuGet — Tests
dotnet add Bookkeeping.Tests package Moq
dotnet add Bookkeeping.Tests package FluentAssertions
```

### 4. Build & Run
```powershell
dotnet build Bookkeeping.sln
dotnet run --project Bookkeeping.App
```

The app will:
- Create the SQLite database at `%LOCALAPPDATA%\Bookkeeping\bookkeeping.db`
- Seed it with 4 accounts, 8 categories, and 8 sample transactions
- Open the main window on the Dashboard

---

## Project Structure

```
Bookkeeping.Core/          # Models, Interfaces, DTOs — no WPF, no EF
Bookkeeping.Infrastructure/ # EF Core DbContext, Repositories, Polly, Serilog setup
Bookkeeping.App/           # WPF application
  ├─ App.xaml / App.xaml.cs       ← DI host, Serilog init
  ├─ MainWindow.xaml              ← Sidebar + ContentControl router
  ├─ Views/                       ← UserControls (no business logic)
  ├─ ViewModels/                  ← ObservableObject + ObservableValidator
  ├─ Services/                    ← INavigationService, IDialogService
  ├─ Converters/                  ← Value converters
  └─ Themes/                      ← Colors.xaml, Styles.xaml
Bookkeeping.Tests/         # xUnit ViewModel tests
Automation/                # UIAutomation smoke script + MCP config
```

---

## Running Tests
```powershell
dotnet test Bookkeeping.Tests --logger "console;verbosity=normal"
```

---

## Logging

Logs are written to:
- **Console** — structured format with level/source
- **Rolling file** — `%LOCALAPPDATA%\Bookkeeping\Logs\bookkeeping-YYYYMMDD.log`

Logged events: startup, navigation, all data operations (Add/Update/Delete), Polly retries, errors.

---

## Resilience (Polly)

Every repository method is wrapped in a `ResiliencePipeline` that:
1. **Retries** up to 3 times with exponential back-off (200 ms base, jitter)
2. **Circuit-Breaker** opens after 50 % failure ratio over a 30-second window, stays open for 10 s

Configuration: `Bookkeeping.Infrastructure/Policies/ResiliencePolicies.cs`

---

## UI Automation / WPF-MCP

See `Automation/README.md` for full instructions.

Quick smoke test (native UIAutomation, no extra tools):
```powershell
.\Automation\smoke-flow.ps1 -BuildFirst
```

---

## Architecture Decisions

| Decision | Choice | Reason |
|---|---|---|
| Navigation | ContentControl + DataTemplates | Pure MVVM, no Frame, no code-behind |
| Auto-load on nav | `IPageViewModel.LoadAsync()` called by NavigationService | Avoids Blend Behaviors dependency |
| DB | EF Core `EnsureCreated` | MVP simplicity (no migrations needed) |
| Dialog pattern | ViewModel fires `RequestClose` event; Window code-behind wires it | Minimal code-behind, clean separation |
| Polly version | v8 (`ResiliencePipeline` API) | Current stable, fluent builder style |
