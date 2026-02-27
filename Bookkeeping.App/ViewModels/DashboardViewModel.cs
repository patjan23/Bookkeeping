using Bookkeeping.App.Services;
using Bookkeeping.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Bookkeeping.App.ViewModels;

public partial class DashboardViewModel : ObservableObject, IPageViewModel
{
    private readonly ITransactionRepository _transactions;
    private readonly IDialogService _dialog;
    private readonly INavigationService _nav;
    private readonly ILogger<DashboardViewModel> _logger;

    [ObservableProperty] private decimal _totalIncome;
    [ObservableProperty] private decimal _totalExpenses;
    [ObservableProperty] private decimal _net;
    [ObservableProperty] private bool _isNetNegative;
    [ObservableProperty] private string _monthLabel = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isStatusError;

    public DashboardViewModel(
        ITransactionRepository transactions,
        IDialogService dialog,
        INavigationService nav,
        ILogger<DashboardViewModel> logger)
    {
        _transactions = transactions;
        _dialog = dialog;
        _nav = nav;
        _logger = logger;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            IsLoading     = true;
            IsStatusError = false;
            StatusMessage = string.Empty;

            var now = DateTime.Now;
            MonthLabel = now.ToString("MMMM yyyy");
            _logger.LogInformation("Loading dashboard for {Month}", MonthLabel);

            var summary = await _transactions.GetMonthlySummaryAsync(now.Year, now.Month);
            TotalIncome   = summary.TotalIncome;
            TotalExpenses = summary.TotalExpenses;
            Net           = summary.Net;
            IsNetNegative = Net < 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load dashboard");
            StatusMessage = "Failed to load dashboard data.";
            IsStatusError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task QuickAddTransactionAsync()
    {
        var saved = await _dialog.ShowAddEditTransactionAsync();
        if (saved) await LoadAsync();
    }

    [RelayCommand]
    private void GoToTransactions() => _nav.NavigateTo<TransactionsViewModel>();
}
