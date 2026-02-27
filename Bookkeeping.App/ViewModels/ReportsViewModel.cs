using Bookkeeping.App.Services;
using Bookkeeping.Core.DTOs;
using Bookkeeping.Core.Interfaces;
using Bookkeeping.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Bookkeeping.App.ViewModels;

public partial class ReportsViewModel : ObservableObject, IPageViewModel
{
    private readonly ITransactionRepository _repo;
    private readonly ILogger<ReportsViewModel> _logger;

    [ObservableProperty] private int _selectedYear  = DateTime.Now.Year;
    [ObservableProperty] private int _selectedMonth = DateTime.Now.Month;
    [ObservableProperty] private ObservableCollection<MonthlySummaryDto> _incomeSummary  = new();
    [ObservableProperty] private ObservableCollection<MonthlySummaryDto> _expenseSummary = new();
    [ObservableProperty] private decimal _totalIncome;
    [ObservableProperty] private decimal _totalExpenses;
    [ObservableProperty] private decimal _net;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ObservableCollection<int> Years  { get; } = new();
    public ObservableCollection<int> Months { get; } = new(Enumerable.Range(1, 12));

    public ReportsViewModel(ITransactionRepository repo, ILogger<ReportsViewModel> logger)
    {
        _repo   = repo;
        _logger = logger;

        var now = DateTime.Now.Year;
        for (int y = now - 3; y <= now + 1; y++) Years.Add(y);
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = string.Empty;
            _logger.LogInformation("Loading report {Year}-{Month}", SelectedYear, SelectedMonth);

            var all     = (await _repo.GetCategorySummaryAsync(SelectedYear, SelectedMonth)).ToList();
            var income  = all.Where(s => s.Type == TransactionType.Income).ToList();
            var expense = all.Where(s => s.Type == TransactionType.Expense).ToList();

            double maxIncome  = income.Select(s => (double)s.TotalAmount).DefaultIfEmpty(0).Max();
            double maxExpense = expense.Select(s => (double)s.TotalAmount).DefaultIfEmpty(0).Max();

            foreach (var s in income)
                s.RelativeWidth = maxIncome  > 0 ? (double)s.TotalAmount / maxIncome  * 280 : 0;
            foreach (var s in expense)
                s.RelativeWidth = maxExpense > 0 ? (double)s.TotalAmount / maxExpense * 280 : 0;

            IncomeSummary  = new ObservableCollection<MonthlySummaryDto>(income);
            ExpenseSummary = new ObservableCollection<MonthlySummaryDto>(expense);

            var summary   = await _repo.GetMonthlySummaryAsync(SelectedYear, SelectedMonth);
            TotalIncome   = summary.TotalIncome;
            TotalExpenses = summary.TotalExpenses;
            Net           = summary.Net;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load report");
            StatusMessage = "Failed to load report.";
        }
        finally { IsLoading = false; }
    }

    partial void OnSelectedYearChanged(int value)  => _ = LoadAsync();
    partial void OnSelectedMonthChanged(int value)  => _ = LoadAsync();
}
