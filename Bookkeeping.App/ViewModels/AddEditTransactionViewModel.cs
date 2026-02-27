using Bookkeeping.Core.Interfaces;
using Bookkeeping.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Bookkeeping.App.ViewModels;

public partial class AddEditTransactionViewModel : ObservableValidator
{
    private readonly ITransactionRepository _txRepo;
    private readonly ICategoryRepository _catRepo;
    private readonly IAccountRepository _accRepo;
    private readonly ILogger<AddEditTransactionViewModel> _logger;

    private int? _existingId;

    // ── Form fields ────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0.01, 1_000_000_000, ErrorMessage = "Amount must be between 0.01 and 1,000,000,000")]
    private decimal _amount;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Description is required")]
    [MaxLength(200, ErrorMessage = "Max 200 characters")]
    private string _description = string.Empty;

    [ObservableProperty]
    private DateTime _date = DateTime.Today;

    [ObservableProperty]
    private TransactionType _transactionType = TransactionType.Expense;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(AddEditTransactionViewModel), nameof(ValidateCategory))]
    private Category? _selectedCategory;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(AddEditTransactionViewModel), nameof(ValidateAccount))]
    private Account? _selectedAccount;

    // ── State ──────────────────────────────────────────────────────
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _title = "Add Transaction";
    [ObservableProperty] private string _validationSummary = string.Empty;

    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<Account>  Accounts   { get; } = new();

    public Array TransactionTypes => Enum.GetValues(typeof(TransactionType));

    public event Action<bool>? RequestClose;

    public AddEditTransactionViewModel(
        ITransactionRepository txRepo,
        ICategoryRepository catRepo,
        IAccountRepository accRepo,
        ILogger<AddEditTransactionViewModel> logger)
    {
        _txRepo = txRepo;
        _catRepo = catRepo;
        _accRepo = accRepo;
        _logger = logger;
    }

    public async Task InitializeAsync(int? existingId = null)
    {
        _existingId = existingId;
        Title = existingId.HasValue ? "Edit Transaction" : "Add Transaction";

        IsLoading = true;
        try
        {
            var cats = await _catRepo.GetAllAsync();
            Categories.Clear();
            foreach (var c in cats) Categories.Add(c);

            var accs = await _accRepo.GetAllAsync();
            Accounts.Clear();
            foreach (var a in accs) Accounts.Add(a);

            if (existingId.HasValue)
            {
                var tx = await _txRepo.GetByIdAsync(existingId.Value);
                if (tx is not null)
                {
                    Amount          = tx.Amount;
                    Description     = tx.Description;
                    Date            = tx.Date;
                    TransactionType = tx.Type;
                    SelectedCategory = Categories.FirstOrDefault(c => c.Id == tx.CategoryId);
                    SelectedAccount  = Accounts.FirstOrDefault(a => a.Id == tx.AccountId);
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidateAllProperties();
        if (HasErrors)
        {
            ValidationSummary = string.Join(" | ",
                GetErrors()
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrEmpty(m)));
            return;
        }

        ValidationSummary = string.Empty;
        IsLoading = true;
        try
        {
            var tx = new Transaction
            {
                Id          = _existingId ?? 0,
                Amount      = Amount,
                Description = Description.Trim(),
                Date        = Date,
                Type        = TransactionType,
                CategoryId  = SelectedCategory!.Id,
                AccountId   = SelectedAccount!.Id
            };

            if (_existingId.HasValue)
            {
                await _txRepo.UpdateAsync(tx);
                _logger.LogInformation("Updated transaction {Id}", tx.Id);
            }
            else
            {
                await _txRepo.AddAsync(tx);
                _logger.LogInformation("Added transaction {Description}", tx.Description);
            }

            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save transaction");
            ValidationSummary = "Save failed: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);

    // ── Validators ─────────────────────────────────────────────────
    public static ValidationResult? ValidateCategory(Category? cat, ValidationContext ctx)
        => cat is not null ? ValidationResult.Success
            : new ValidationResult("Category is required");

    public static ValidationResult? ValidateAccount(Account? acc, ValidationContext ctx)
        => acc is not null ? ValidationResult.Success
            : new ValidationResult("Account is required");

    partial void OnTransactionTypeChanged(TransactionType value)
    {
        // When type changes, reset category selection so user picks an appropriate one
        SelectedCategory = null;
    }
}
