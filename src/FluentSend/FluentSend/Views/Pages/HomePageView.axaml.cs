using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FluentSend.ViewModels;

namespace FluentSend.Views.Pages;

public partial class HomePageView : UserControl
{
    public HomePageView()
    {
        InitializeComponent();
    }

    private void OnMessageKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is HomePageViewModel vm && vm.SendMessageCommand.CanExecute(null))
            {
                vm.SendMessageCommand.Execute(null);
            }
        }
    }

    private async void OnSendFileClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not HomePageViewModel vm)
            return;
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return;
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择要发送的文件",
            AllowMultiple = true
        });
        var paths = files.Select(f => f.TryGetLocalPath()).Where(p => p != null).Select(p => p!).ToArray();
        if (paths.Length > 0)
            vm.SendPickedFiles(paths);
    }

    private async void OnSendFolderClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not HomePageViewModel vm)
            return;
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return;
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择要发送的文件夹",
            AllowMultiple = true
        });
        var paths = folders.Select(f => f.TryGetLocalPath()).Where(p => p != null).Select(p => p!).ToArray();
        if (paths.Length > 0)
            vm.SendPickedFiles(paths);
    }

    private async void OnSendClipboardClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not HomePageViewModel vm)
            return;
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard == null)
            return;
        var text = await topLevel.Clipboard.TryGetTextAsync();
        if (!string.IsNullOrEmpty(text))
            vm.SendClipboard(text);
    }
}
