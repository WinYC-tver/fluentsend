using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentSend.ViewModels;

namespace FluentSend.Views.Pages;

public partial class FilesPageView : UserControl
{
    public FilesPageView()
    {
        InitializeComponent();
    }

    private void OnItemDoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is FilesPageViewModel vm && FileList.SelectedItem is FileHistoryItem item)
            vm.OpenFileCommand.Execute(item);
    }

    private void OnOpenFile(object? sender, RoutedEventArgs e)
    {
        if (DataContext is FilesPageViewModel vm && FileList.SelectedItem is FileHistoryItem item)
            vm.OpenFileCommand.Execute(item);
    }

    private void OnOpenFolder(object? sender, RoutedEventArgs e)
    {
        if (DataContext is FilesPageViewModel vm && FileList.SelectedItem is FileHistoryItem item)
            vm.OpenFolderCommand.Execute(item);
    }

    private void OnRemoveItem(object? sender, RoutedEventArgs e)
    {
        if (DataContext is FilesPageViewModel vm && FileList.SelectedItem is FileHistoryItem item)
            vm.RemoveItemCommand.Execute(item);
    }
}
