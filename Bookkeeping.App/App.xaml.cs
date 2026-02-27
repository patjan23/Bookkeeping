using Bookkeeping.App.Services;
using Bookkeeping.App.ViewModels;
using Bookkeeping.Infrastructure;
using Bookkeeping.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IO;
using System.Windows;

namespace Bookkeeping.App;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ── Serilog ───────────────────────────────────────────────
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Bookkeeping", "Logs");
        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(logDir, "bookkeeping-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("Bookkeeping application starting");

        // ── DB ────────────────────────────────────────────────────
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Bookkeeping");
        Directory.CreateDirectory(dataDir);
        var dbPath         = Path.Combine(dataDir, "bookkeeping.db");
        var connectionString = $"Data Source={dbPath}";
        Log.Information("Database: {DbPath}", dbPath);

        // ── Host / DI ─────────────────────────────────────────────
        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((_, services) =>
            {
                services.AddInfrastructure(connectionString);

                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IDialogService, DialogService>();

                // Page VMs — singletons so state persists within a session
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<TransactionsViewModel>();
                services.AddSingleton<CategoriesViewModel>();
                services.AddSingleton<AccountsViewModel>();
                services.AddSingleton<ReportsViewModel>();

                // Dialog VM — transient: fresh instance each open
                services.AddTransient<AddEditTransactionViewModel>();

                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        // ── Seed DB ───────────────────────────────────────────────
        using (var scope = _host.Services.CreateScope())
        {
            var db  = scope.ServiceProvider.GetRequiredService<BookkeepingContext>();
            var log = scope.ServiceProvider.GetRequiredService<ILogger<App>>();
            await DbInitializer.InitializeAsync(db, log);
        }

        // ── Launch window ─────────────────────────────────────────
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.DataContext = _host.Services.GetRequiredService<MainViewModel>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        Log.Information("Application shutting down");
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
