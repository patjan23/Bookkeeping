using Bookkeeping.App.Services;
using Bookkeeping.Core.Interfaces;
using Bookkeeping.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Bookkeeping.App.ViewModels;

public partial class CategoriesViewModel : ObservableValidator, IPageViewModel
{
    private readonly ICategoryRepository _repo;
    private readonly ILogger<CategoriesViewModel> _logger;

    [ObservableProperty] private ObservableCollection<Category> _categories = new();
    [ObservableProperty] private Category? _selectedCategory;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isStatusError;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(100)]
    private string _editName = string.Empty;

    [ObservableProperty] private TransactionType _editType = TransactionType.Expense;
    [ObservableProperty] private string _editColor = "#808080";

    public Array TransactionTypes => Enum.GetValues(typeof(TransactionType));
    private int? _editingId;

    public CategoriesViewModel(ICategoryRepository repo, ILogger<CategoriesViewModel> logger)
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
            Categories = new ObservableCollection<Category>(items);
            _logger.LogInformation("Loaded {Count} categories", Categories.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load categories");
            StatusMessage = "Failed to load categories.";
            IsStatusError = true;
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void StartAdd()
    {
        _editingId    = null;
        EditName      = string.Empty;
        EditType      = TransactionType.Expense;
        EditColor     = "#808080";
        IsEditing     = true;
        StatusMessage = string.Empty;
        ClearErrors();
    }

    [RelayCommand]
    private void StartEdit()
    {
        if (SelectedCategory is null) return;
        _editingId    = SelectedCategory.Id;
        EditName      = SelectedCategory.Name;
        EditType      = SelectedCategory.Type;
        EditColor     = SelectedCategory.Color;
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
            var cat = new Category
            {
                Id    = _editingId ?? 0,
                Name  = EditName.Trim(),
                Type  = EditType,
                Color = EditColor
            };

            if (_editingId.HasValue)
            {
                await _repo.UpdateAsync(cat);
                StatusMessage = "Category updated.";
                _logger.LogInformation("Updated category {Id}", cat.Id);
            }
            else
            {
                await _repo.AddAsync(cat);
                StatusMessage = "Category added.";
                _logger.LogInformation("Added category {Name}", cat.Name);
            }

            IsEditing     = false;
            IsStatusError = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save category");
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
        if (SelectedCategory is null) return;
        try
        {
            IsLoading = true;
            await _repo.DeleteAsync(SelectedCategory.Id);
            _logger.LogInformation("Deleted category {Id}", SelectedCategory.Id);
            StatusMessage = "Category deleted.";
            IsStatusError = false;
            SelectedCategory = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete category");
            StatusMessage = "Delete failed. Category may be in use.";
            IsStatusError = true;
        }
        finally { IsLoading = false; }
    }
}
