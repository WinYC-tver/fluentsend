using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FluentSend.ViewModels;

public partial class FilesPageViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<FileHistoryItem> _fileHistory = [];

    public bool HasHistory => FileHistory.Count > 0;
    public bool IsEmpty => FileHistory.Count == 0;

    public FilesPageViewModel()
    {
        FileHistory.CollectionChanged += OnCollectionChanged;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasHistory));
        OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    private void OpenFile(FileHistoryItem? item)
    {
        if (item == null || !File.Exists(item.Path))
            return;
        try
        {
            Process.Start(new ProcessStartInfo(item.Path) { UseShellExecute = true });
        }
        catch
        {
        }
    }

    [RelayCommand]
    private void OpenFolder(FileHistoryItem? item)
    {
        if (item == null)
            return;
        var dir = File.Exists(item.Path) ? Path.GetDirectoryName(item.Path) : item.Path;
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = dir,
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }

    [RelayCommand]
    private void ClearHistory()
    {
        FileHistory.Clear();
    }

    [RelayCommand]
    private void RemoveItem(FileHistoryItem? item)
    {
        if (item != null)
            FileHistory.Remove(item);
    }
}

public sealed class FileHistoryItem
{
    public string FileName { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTimeOffset ReceivedTime { get; set; }
    public string SizeText => FormatSize(Size);
    public string TimeText => ReceivedTime.ToString("yyyy-MM-dd HH:mm");

    private static string FormatSize(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
            _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
        };
    }
}
