using Bookkeeping.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Bookkeeping.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _nav;
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    private ObservableObject _currentPage = null!;

    [ObservableProperty]
    private string _activeRoute = "Dashboard";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isStatusError;

    public MainViewModel(INavigationService nav, ILogger<MainViewModel> logger)
    {
        _nav = nav;
        _logger = logger;

        _nav.NavigationChanged += () =>
        {
            CurrentPage = _nav.CurrentPage;
        };

        NavigateTo("Dashboard");
    }

    [RelayCommand]
    private void NavigateTo(string route)
    {
        _logger.LogInformation("Navigating to {Route}", route);
        ActiveRoute = route;
        SetStatus("Ready");

        switch (route)
        {
            case "Dashboard":     _nav.NavigateTo<DashboardViewModel>();     break;
            case "Transactions":  _nav.NavigateTo<TransactionsViewModel>();  break;
            case "Categories":    _nav.NavigateTo<CategoriesViewModel>();    break;
            case "Accounts":      _nav.NavigateTo<AccountsViewModel>();      break;
            case "Reports":       _nav.NavigateTo<ReportsViewModel>();       break;
        }
    }

    public void SetStatus(string message, bool isError = false)
    {
        StatusMessage  = message;
        IsStatusError  = isError;
    }
}
