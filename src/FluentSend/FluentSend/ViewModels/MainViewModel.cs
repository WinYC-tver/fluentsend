using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentSend.Services;

namespace FluentSend.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly FluentSendSession _session;

    [ObservableProperty]
    private ObservableObject _currentPage;

    [ObservableProperty]
    private string _selectedPageTitle = "首页";

    [ObservableProperty]
    private string _selectedPageKey = "home";

    [ObservableProperty]
    private int _paneMode = 0;

    [ObservableProperty]
    private bool _isPaneOpen = true;

    [ObservableProperty]
    private bool _isMobile;

    [ObservableProperty]
    private bool _useTopNav;

    public HomePageViewModel Home { get; }
    public FilesPageViewModel Files { get; }
    public SettingsPageViewModel Settings { get; }
    public AboutPageViewModel About { get; }
    public ReceiveOverlayViewModel ReceiveOverlay { get; }

    public MainViewModel()
    {
        _session = new FluentSendSession();
        IsMobile = DeviceFlow.IsMobile;
        UseTopNav = IsMobile;

        Home = new HomePageViewModel(_session);
        Files = new FilesPageViewModel();
        Settings = new SettingsPageViewModel(_session);
        About = new AboutPageViewModel();
        ReceiveOverlay = new ReceiveOverlayViewModel(_session);

        _session.SavePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads", "FluentSend");

        _session.ServerEvent += OnServerEvent;
        ReceiveOverlay.FilesAccepted += OnFilesAccepted;

        try
        {
            _session.EnsureStarted();
        }
        catch
        {
        }

        _currentPage = Home;
    }

    private void OnServerEvent(Interop.ServerEvent e)
    {
        if (e.Type != "receive-request")
            return;

        string sessionId = e.SessionId ?? string.Empty;
        string senderAlias = "未知设备";
        if (e.Info.ValueKind == JsonValueKind.Object &&
            e.Info.TryGetProperty("alias", out var aliasEl))
        {
            senderAlias = aliasEl.GetString() ?? senderAlias;
        }

        var files = e.Files ?? default;
        Dispatcher.UIThread.Post(() =>
        {
            ReceiveOverlay.Show(sessionId, senderAlias, _session.SavePath, files);
        });
    }

    private void OnFilesAccepted(ReceivedFileRecord record)
    {
        Files.FileHistory.Add(new FileHistoryItem
        {
            FileName = record.FileName,
            Sender = record.Sender,
            Path = record.Path,
            Size = record.Size,
            ReceivedTime = record.ReceivedTime
        });
    }

    public void NavigateTo(string key)
    {
        SelectedPageKey = key;
        switch (key)
        {
            case "home":
                CurrentPage = Home;
                SelectedPageTitle = "首页";
                break;
            case "files":
                CurrentPage = Files;
                SelectedPageTitle = "文件";
                break;
            case "settings":
                CurrentPage = Settings;
                SelectedPageTitle = "设置";
                break;
            case "about":
                CurrentPage = About;
                SelectedPageTitle = "关于";
                break;
        }
    }
}
