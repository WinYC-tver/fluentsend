using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentSend.Interop;

namespace FluentSend.Services;

public sealed class FluentSendSession : IAsyncDisposable
{
    public DeviceIdentity Identity { get; private set; } = null!;
    public string Alias { get; set; } = Environment.MachineName;
    public ushort Port { get; set; } = 53317;
    public string Protocol { get; set; } = "https";
    public string DeviceType { get; set; } = DeviceFlow.IsMobile ? "mobile" : "desktop";
    public string SavePath { get; set; } = string.Empty;

    private DiscoveryHandle? _discovery;
    private ServerHandle? _server;
    private readonly CancellationTokenSource _cts = new();

    public event Action<DiscoveredDevice>? DeviceSeen;
    public event Action<string>? DeviceLeft;
    public event Action<ServerEvent>? ServerEvent;

    public void EnsureStarted()
    {
        if (Identity == null)
            Identity = FluentSendCore.GenerateIdentity();

        if (_discovery == null)
        {
            var cfg = new DiscoveryConfig
            {
                Alias = Alias,
                Version = "2.0",
                DeviceModel = Environment.OSVersion.ToString(),
                DeviceType = DeviceType,
                Fingerprint = Identity.Fingerprint,
                HttpPort = Port,
                Protocol = Protocol,
                Download = true,
                CertPem = Identity.CertPem,
                PrivateKeyPem = Identity.PrivateKeyPem
            };
            _discovery = FluentSendCore.StartDiscovery(cfg, OnDiscoveryEvent);
        }

        if (_server == null)
        {
            var cfg = new ServerConfig
            {
                Port = Port,
                Alias = Alias,
                Version = "2.0",
                DeviceModel = Environment.OSVersion.ToString(),
                DeviceType = DeviceType,
                Fingerprint = Identity.Fingerprint,
                CertPem = Identity.CertPem,
                PrivateKeyPem = Identity.PrivateKeyPem,
                Protocol = Protocol,
                VerifyChecksums = true
            };
            _server = FluentSendCore.StartServer(cfg, OnServerEvent);
        }
    }

    public List<DiscoveredDevice> GetDevices()
        => _discovery != null ? FluentSendCore.GetDevices(_discovery) : [];

    public void Announce() => _discovery?.Announce();

    public int ServerRespond(string sessionId, Interop.Decision decision)
        => _server != null ? FluentSendCore.ServerRespond(_server, sessionId, decision) : -1;

    public int ServerFileTarget(string sessionId, string fileId, string path)
        => _server != null ? FluentSendCore.ServerFileTarget(_server, sessionId, fileId, path) : -1;

    public int SendText(string ip, ushort port, string protocol, string text)
    {
        var cfg = new SendConfig
        {
            Ip = ip,
            Port = port,
            Protocol = protocol,
            Alias = Alias,
            Version = "2.0",
            DeviceModel = Environment.OSVersion.ToString(),
            DeviceType = DeviceType,
            Fingerprint = Identity.Fingerprint,
            CertPem = Identity.CertPem,
            PrivateKeyPem = Identity.PrivateKeyPem,
            Files = new List<SendFile>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString("N"),
                    FileName = "text",
                    Size = (ulong)System.Text.Encoding.UTF8.GetByteCount(text),
                    FileType = "text",
                    Preview = text,
                    Path = string.Empty
                }
            }
        };
        return FluentSendCore.SendFiles(cfg, null);
    }

    public int SendFiles(string ip, ushort port, string protocol, IEnumerable<string> paths)
    {
        var files = new List<SendFile>();
        foreach (var p in paths)
        {
            var info = new FileInfo(p);
            files.Add(new SendFile
            {
                Id = Guid.NewGuid().ToString("N"),
                FileName = info.Name,
                Size = info.Exists ? (ulong)info.Length : 0,
                FileType = "file",
                Path = p
            });
        }
        var cfg = new SendConfig
        {
            Ip = ip,
            Port = port,
            Protocol = protocol,
            Alias = Alias,
            Version = "2.0",
            DeviceModel = Environment.OSVersion.ToString(),
            DeviceType = DeviceType,
            Fingerprint = Identity.Fingerprint,
            CertPem = Identity.CertPem,
            PrivateKeyPem = Identity.PrivateKeyPem,
            Files = files
        };
        return FluentSendCore.SendFiles(cfg, null);
    }

    private void OnDiscoveryEvent(DiscoveryEvent e)
    {
        if (e.Type == "seen" && e.Device != null)
            DeviceSeen?.Invoke(e.Device);
        else if (e.Type == "left" && e.Device != null)
            DeviceLeft?.Invoke(e.Device.Fingerprint);
    }

    private void OnServerEvent(ServerEvent e) => ServerEvent?.Invoke(e);

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        await ValueTask.CompletedTask;
        _discovery?.Dispose();
        _server?.Dispose();
    }
}
