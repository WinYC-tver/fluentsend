using System;
using System.IO;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentSend.Services;

namespace FluentSend.ViewModels;

public partial class SettingsPageViewModel : ViewModelBase
{
    private readonly FluentSendSession? _session;

    [ObservableProperty]
    private string _deviceName = "My Device";

    [ObservableProperty]
    private string _downloadPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "FluentSend");

    [ObservableProperty]
    private bool _autoStart = false;

    [ObservableProperty]
    private string _hotkeyText = "Ctrl+Shift+S";

    [ObservableProperty]
    private int _themeIndex = 0;

    [ObservableProperty]
    private int _backdropIndex = 0;

    [ObservableProperty]
    private string _accentColorHex = "#0078D4";

    [ObservableProperty]
    private ushort _serverPort = 53317;

    [ObservableProperty]
    private int _protocolIndex = 0;

    [ObservableProperty]
    private string _pin = string.Empty;

    [ObservableProperty]
    private bool _verifyChecksums = true;

    public SettingsPageViewModel() { }

    public SettingsPageViewModel(FluentSendSession session)
    {
        _session = session;
        DeviceName = session.Alias;
        DownloadPath = session.SavePath;
        ServerPort = session.Port;
        ProtocolIndex = string.Equals(session.Protocol, "http", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
    }

    partial void OnDeviceNameChanged(string value)
    {
        if (_session != null)
            _session.Alias = value;
    }

    partial void OnDownloadPathChanged(string value)
    {
        if (_session != null)
            _session.SavePath = value;
    }

    partial void OnServerPortChanged(ushort value)
    {
        if (_session != null)
            _session.Port = value;
    }

    partial void OnProtocolIndexChanged(int value)
    {
        if (_session != null)
            _session.Protocol = value == 1 ? "http" : "https";
    }
}
