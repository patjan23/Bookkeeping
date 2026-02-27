using Bookkeeping.App.Services;
using Bookkeeping.Core.Interfaces;
using Bookkeeping.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Bookkeeping.App.ViewModels;

public partial class AccountsViewModel : ObservableValidator, IPageViewModel
{
    private readonly IAccountRepository _repo;
    private readonly ILogger<AccountsViewModel> _logger;

    [ObservableProperty] private ObservableCollection<Account> _accounts = new();
    [ObservableProperty] private Account? _selectedAccount;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isStatusError;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(100)]
    private string _editName = string.Empty;

    [ObservableProperty] private AccountType _editType    = AccountType.Bank;
    [ObservableProperty] private decimal     _editBalance = 0m;

    public Array AccountTypes => Enum.GetValues(typeof(AccountType));
    private int? _editingId;

    public AccountsViewModel(IAccountRepository repo, ILogger<AccountsViewModel> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            IsLoading     = true;
            IsStatusError = false;
            var items = await _repo.GetAllAsync();
            Accounts = new ObservableCollection<Account>(items);
            _logger.LogInformation("Loaded {Count} accounts", Accounts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load accounts");
            StatusMessage = "Failed to load accounts.";
            IsStatusError = true;
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void StartAdd()
    {
        _editingId    = null;
        EditName      = string.Empty;
        EditType      = AccountType.Bank;
        EditBalance   = 0m;
        IsEditing     = true;
        StatusMessage = string.Empty;
        ClearErrors();
    }

    [RelayCommand]
    private void StartEdit()
    {
        if (SelectedAccount is null) return;
        _editingId    = SelectedAccount.Id;
        EditName      = SelectedAccount.Name;
        EditType      = SelectedAccount.Type;
        EditBalance   = SelectedAccount.Balance;
        IsEditing     = true;
        StatusMessage = string.Empty;
        ClearErrors();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidateAllProperties();
        if (HasErrors) return;

        try
        {
            IsLoading = true;
            var acc = new Account
            {
                Id      = _editingId ?? 0,
                Name    = EditName.Trim(),
                Type    = EditType,
                Balance = EditBalance
            };

            if (_editingId.HasValue)
            {
                await _repo.UpdateAsync(acc);
                StatusMessage = "Account updated.";
            }
            else
            {
                await _repo.AddAsync(acc);
                StatusMessage = "Account added.";
            }

            IsEditing     = false;
            IsStatusError = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save account");
            StatusMessage = "Save failed: " + ex.Message;
            IsStatusError = true;
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void CancelEdit() { IsEditing = false; ClearErrors(); }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedAccount is null) return;
        try
        {
            IsLoading = true;
            await _repo.DeleteAsync(SelectedAccount.Id);
            StatusMessage = "Account deleted.";
            IsStatusError = false;
            SelectedAccount = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete account");
            StatusMessage = "Delete failed. Account may have linked transactions.";
            IsStatusError = true;
        }
        finally { IsLoading = false; }
    }
}
