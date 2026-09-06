using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentSend.Interop;
using FluentSend.Services;

namespace FluentSend.ViewModels;

public partial class HomePageViewModel : ViewModelBase
{
    private readonly FluentSendSession _session;
    private readonly ConcurrentDictionary<string, DeviceItem> _byFingerprint = new();

    [ObservableProperty]
    private ObservableCollection<DeviceItem> _devices = [];

    [ObservableProperty]
    private DeviceItem? _selectedDevice;

    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = [];

    [ObservableProperty]
    private string _messageText = string.Empty;

    [ObservableProperty]
    private bool _isDiscovering = true;

    public HomePageViewModel(FluentSendSession session)
    {
        _session = session;
        _session.DeviceSeen += OnDeviceSeen;
        _session.DeviceLeft += OnDeviceLeft;

        try
        {
            _session.EnsureStarted();
            Dispatcher.UIThread.Post(RefreshDevices);
        }
        catch
        {
            IsDiscovering = false;
        }
    }

    private void OnDeviceSeen(DiscoveredDevice d)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var item = new DeviceItem
            {
                Alias = d.Alias,
                DeviceType = d.DeviceType ?? "desktop",
                Fingerprint = d.Fingerprint,
                Ip = d.Channel?.Host ?? string.Empty,
                Port = d.Channel?.Port ?? 0,
                Protocol = d.Channel?.Protocol ?? "https"
            };
            _byFingerprint[d.Fingerprint] = item;
            RefreshDevices();
        });
    }

    private void OnDeviceLeft(string fingerprint)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _byFingerprint.TryRemove(fingerprint, out _);
            RefreshDevices();
        });
    }

    [RelayCommand]
    private void Refresh()
    {
        _session.Announce();
        RefreshDevices();
    }

    private void RefreshDevices()
    {
        Devices.Clear();
        foreach (var d in _session.GetDevices())
        {
            if (string.IsNullOrEmpty(d.Fingerprint))
                continue;
            _byFingerprint[d.Fingerprint] = new DeviceItem
            {
                Alias = d.Alias,
                DeviceType = d.DeviceType ?? "desktop",
                Fingerprint = d.Fingerprint,
                Ip = d.Channel?.Host ?? string.Empty,
                Port = d.Channel?.Port ?? 0,
                Protocol = d.Channel?.Protocol ?? "https"
            };
        }
        foreach (var item in _byFingerprint.Values)
            Devices.Add(item);
    }

    [RelayCommand]
    private void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(MessageText))
            return;
        if (SelectedDevice == null)
            return;
        try
        {
            _session.SendText(SelectedDevice.Ip, SelectedDevice.Port, SelectedDevice.Protocol, MessageText);
            Messages.Add(new ChatMessage { Text = MessageText, IsMine = true });
            MessageText = string.Empty;
        }
        catch
        {
        }
    }

    public void SendPickedFiles(string[] paths)
    {
        if (SelectedDevice == null || paths.Length == 0)
            return;
        try
        {
            _session.SendFiles(SelectedDevice.Ip, SelectedDevice.Port, SelectedDevice.Protocol, paths);
            foreach (var p in paths)
            {
                var name = System.IO.Path.GetFileName(p);
                Messages.Add(new ChatMessage { Text = $"[文件] {name}", IsMine = true });
            }
        }
        catch
        {
        }
    }

    public void SendClipboard(string text)
    {
        if (SelectedDevice == null || string.IsNullOrEmpty(text))
            return;
        try
        {
            _session.SendText(SelectedDevice.Ip, SelectedDevice.Port, SelectedDevice.Protocol, text);
            Messages.Add(new ChatMessage { Text = $"[剪贴板] {text}", IsMine = true });
        }
        catch
        {
        }
    }
}

public sealed class DeviceItem
{
    public string Alias { get; set; } = string.Empty;
    public string DeviceType { get; set; } = "desktop";
    public string Fingerprint { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public ushort Port { get; set; }
    public string Protocol { get; set; } = "https";
}

public sealed class ChatMessage
{
    public string Text { get; set; } = string.Empty;
    public bool IsMine { get; set; }
}
