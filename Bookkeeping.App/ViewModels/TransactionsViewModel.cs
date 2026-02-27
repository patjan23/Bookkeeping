using Bookkeeping.App.Services;
using Bookkeeping.Core.Interfaces;
using Bookkeeping.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Bookkeeping.App.ViewModels;

public partial class TransactionsViewModel : ObservableObject, IPageViewModel
{
    private readonly ITransactionRepository _repo;
    private readonly IDialogService _dialog;
    private readonly ILogger<TransactionsViewModel> _logger;

    [ObservableProperty] private ObservableCollection<Transaction> _transactions = new();
    [ObservableProperty] private Transaction? _selectedTransaction;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private DateTime? _fromDate;
    [ObservableProperty] private DateTime? _toDate;
    [ObservableProperty] private TransactionType? _filterType;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isStatusError;

    // Flag to suppress reactive loads while resetting filters
    private bool _isResetting;

    public TransactionsViewModel(
        ITransactionRepository repo,
        IDialogService dialog,
        ILogger<TransactionsViewModel> logger)
    {
        _repo = repo;
        _dialog = dialog;
        _logger = logger;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            IsLoading     = true;
            IsStatusError = false;
            _logger.LogInformation("Loading transactions");

            var items = await _repo.GetFilteredAsync(
                FromDate, ToDate, null, FilterType,
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText);

            Transactions = new ObservableCollection<Transaction>(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load transactions");
            StatusMessage = "Failed to load transactions.";
            IsStatusError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddTransactionAsync()
    {
        var saved = await _dialog.ShowAddEditTransactionAsync();
        if (saved)
        {
            StatusMessage = "Transaction added successfully.";
            IsStatusError = false;
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task EditTransactionAsync()
    {
        if (SelectedTransaction is null) return;
        var saved = await _dialog.ShowAddEditTransactionAsync(SelectedTransaction.Id);
        if (saved)
        {
            StatusMessage = "Transaction updated.";
            IsStatusError = false;
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteTransactionAsync()
    {
        if (SelectedTransaction is null) return;
        if (!_dialog.Confirm($"Delete \"{SelectedTransaction.Description}\"?")) return;

        try
        {
            IsLoading = true;
            var id = SelectedTransaction.Id;
            await _repo.DeleteAsync(id);
            _logger.LogInformation("Deleted transaction {Id}", id);
            StatusMessage = "Transaction deleted.";
            IsStatusError = false;
            SelectedTransaction = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete transaction");
            StatusMessage = "Delete failed.";
            IsStatusError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        _isResetting = true;
        try
        {
            SearchText = string.Empty;
            FromDate   = null;
            ToDate     = null;
            FilterType = null;
        }
        finally
        {
            _isResetting = false;
        }
        await LoadAsync();
    }

    // Reactive filter — reload on each filter change (debounce via _isResetting)
    partial void OnSearchTextChanged(string value)           { if (!_isResetting) _ = LoadAsync(); }
    partial void OnFilterTypeChanged(TransactionType? value) { if (!_isResetting) _ = LoadAsync(); }
    partial void OnFromDateChanged(DateTime? value)          { if (!_isResetting) _ = LoadAsync(); }
    partial void OnToDateChanged(DateTime? value)            { if (!_isResetting) _ = LoadAsync(); }
}
