using Bookkeeping.App.ViewModels;
using System.Windows;

namespace Bookkeeping.App.Views;

public partial class AddEditTransactionDialog : Window
{
    public AddEditTransactionDialog(AddEditTransactionViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        // Wire the ViewModel's close request to the WPF DialogResult
        vm.RequestClose += result =>
        {
            DialogResult = result;
            Close();
        };
    }
}
