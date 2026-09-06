using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using FluentSend.ViewModels;

namespace FluentSend.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void OnSelectionChanged(object? sender, FANavigationViewSelectionChangedEventArgs e)
    {
        if (e.SelectedItem is Control item && item.Tag?.ToString() is string tag)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.NavigateTo(tag);
            }
        }
    }

    private void OnMobileNavClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control btn && btn.Tag?.ToString() is string tag &&
            DataContext is MainViewModel vm)
        {
            vm.NavigateTo(tag);
        }
    }
}
