using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Bookkeeping.App.Services;

/// <summary>
/// ViewModels that load async data on navigation implement this.
/// The NavigationService calls it automatically — no code-behind or Behaviors packages needed.
/// </summary>
public interface IPageViewModel
{
    Task LoadAsync();
}

public interface INavigationService
{
    ObservableObject CurrentPage { get; }
    void NavigateTo<TViewModel>() where TViewModel : ObservableObject;
    event Action? NavigationChanged;
}

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _provider;
    private ObservableObject _currentPage = null!;

    public NavigationService(IServiceProvider provider) => _provider = provider;

    public ObservableObject CurrentPage => _currentPage;
    public event Action? NavigationChanged;

    public void NavigateTo<TViewModel>() where TViewModel : ObservableObject
    {
        _currentPage = _provider.GetRequiredService<TViewModel>();
        NavigationChanged?.Invoke();

        // Already on UI thread (button click → command). Fire-and-forget via async void equivalent.
        if (_currentPage is IPageViewModel page)
            _ = page.LoadAsync();
    }
}
