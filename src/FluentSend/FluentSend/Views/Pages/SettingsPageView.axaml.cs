using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FluentSend.ViewModels;

namespace FluentSend.Views.Pages;

public partial class SettingsPageView : UserControl
{
    public SettingsPageView()
    {
        InitializeComponent();
    }

    private async void OnBrowseDownloadPath(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsPageViewModel vm)
            return;
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return;
        var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择下载位置",
            AllowMultiple = false
        });
        var path = folder.Select(f => f.TryGetLocalPath()).FirstOrDefault(p => p != null);
        if (path != null)
            vm.DownloadPath = path;
    }
}
