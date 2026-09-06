using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentSend.Services;

namespace FluentSend.ViewModels;

public partial class ReceiveOverlayViewModel : ViewModelBase
{
    private readonly FluentSendSession _session;

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private string _senderAlias = string.Empty;

    [ObservableProperty]
    private string _sessionId = string.Empty;

    [ObservableProperty]
    private string _savePath = string.Empty;

    public ObservableCollection<ReceiveFileInfo> Files { get; } = [];

    public event Action<ReceivedFileRecord>? FilesAccepted;

    public ReceiveOverlayViewModel(FluentSendSession session)
    {
        _session = session;
    }

    public void Show(string sessionId, string senderAlias, string savePath, JsonElement filesElement)
    {
        SessionId = sessionId;
        SenderAlias = senderAlias;
        SavePath = savePath;
        Files.Clear();
        if (filesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var f in filesElement.EnumerateArray())
            {
                Files.Add(new ReceiveFileInfo
                {
                    FileName = f.TryGetProperty("fileName", out var n) ? n.GetString() ?? "?" : "?",
                    SizeText = FormatSize(f.TryGetProperty("size", out var s) ? s.GetInt64() : 0),
                    FileType = f.TryGetProperty("fileType", out var t) ? t.GetString() ?? "file" : "file",
                    Id = f.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                    Size = f.TryGetProperty("size", out var sz) ? sz.GetInt64() : 0
                });
            }
        }
        IsVisible = true;
    }

    [RelayCommand]
    private void Accept()
    {
        var decision = new Interop.Decision
        {
            Action = "accept",
            FileIds = Files.Select(f => f.Id).ToList()
        };
        _session.ServerRespond(SessionId, decision);

        foreach (var f in Files)
        {
            FilesAccepted?.Invoke(new ReceivedFileRecord
            {
                FileName = f.FileName,
                Sender = SenderAlias,
                Path = System.IO.Path.Combine(SavePath, f.FileName),
                Size = f.Size,
                ReceivedTime = DateTimeOffset.Now
            });
        }
        IsVisible = false;
    }

    [RelayCommand]
    private void Decline()
    {
        var decision = new Interop.Decision { Action = "decline" };
        _session.ServerRespond(SessionId, decision);
        IsVisible = false;
    }

    [RelayCommand]
    private void Edit()
    {
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }
}

public sealed class ReceiveFileInfo
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string SizeText { get; set; } = string.Empty;
    public string FileType { get; set; } = "file";
    public long Size { get; set; }
}

public sealed class ReceivedFileRecord
{
    public string FileName { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTimeOffset ReceivedTime { get; set; }
}
