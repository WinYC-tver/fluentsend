using FluentSend.Services;
using FluentSend.ViewModels;
using Xunit;

namespace FluentSend.Tests;

public class SettingsPageViewModelTests
{
    [Fact]
    public void ConstructedFromSession_ReflectsSessionState()
    {
        var session = new FluentSendSession
        {
            Alias = "MyPC",
            SavePath = "/tmp/fluentsend",
            Port = 12345,
            Protocol = "http"
        };

        var vm = new SettingsPageViewModel(session);

        Assert.Equal("MyPC", vm.DeviceName);
        Assert.Equal("/tmp/fluentsend", vm.DownloadPath);
        Assert.Equal(12345, vm.ServerPort);
        Assert.Equal(1, vm.ProtocolIndex);
    }

    [Fact]
    public void ChangingDeviceName_SyncsBackToSession()
    {
        var session = new FluentSendSession { Alias = "Initial" };
        var vm = new SettingsPageViewModel(session);

        vm.DeviceName = "NewName";

        Assert.Equal("NewName", session.Alias);
    }

    [Fact]
    public void ChangingDownloadPath_SyncsBackToSession()
    {
        var session = new FluentSendSession { SavePath = "old" };
        var vm = new SettingsPageViewModel(session);

        vm.DownloadPath = "new";

        Assert.Equal("new", session.SavePath);
    }

    [Fact]
    public void ChangingServerPort_SyncsBackToSession()
    {
        var session = new FluentSendSession { Port = 53317 };
        var vm = new SettingsPageViewModel(session);

        vm.ServerPort = 9999;

        Assert.Equal(9999, session.Port);
    }

    [Fact]
    public void ChangingProtocolIndex_SyncsBackToSession()
    {
        var session = new FluentSendSession { Protocol = "https" };
        var vm = new SettingsPageViewModel(session);

        vm.ProtocolIndex = 1;
        Assert.Equal("http", session.Protocol);

        vm.ProtocolIndex = 0;
        Assert.Equal("https", session.Protocol);
    }
}
