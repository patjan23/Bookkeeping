using Bookkeeping.App.ViewModels;
using Bookkeeping.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Bookkeeping.App.Services;

public interface IDialogService
{
    Task<bool> ShowAddEditTransactionAsync(int? existingId = null);
    bool Confirm(string message, string title = "Confirm");
}

public class DialogService : IDialogService
{
    private readonly IServiceProvider _provider;
    public DialogService(IServiceProvider provider) => _provider = provider;

    public async Task<bool> ShowAddEditTransactionAsync(int? existingId = null)
    {
        var vm = _provider.GetRequiredService<AddEditTransactionViewModel>();
        await vm.InitializeAsync(existingId);
        var dialog = new AddEditTransactionDialog(vm);
        return dialog.ShowDialog() == true;
    }

    public bool Confirm(string message, string title = "Confirm")
    {
        var result = System.Windows.MessageBox.Show(
            message, title,
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        return result == System.Windows.MessageBoxResult.Yes;
    }
}
