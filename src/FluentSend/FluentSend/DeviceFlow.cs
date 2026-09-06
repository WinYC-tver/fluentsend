using System;
using Avalonia;

namespace FluentSend;

public static class DeviceFlow
{
    public static bool IsMobile => OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

    public static bool IsDesktop => OperatingSystem.IsWindows() || OperatingSystem.IsLinux() ||
                                    OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD();
}
