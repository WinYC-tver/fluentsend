using System.Runtime.InteropServices;

namespace FluentSend.Interop;

internal static class NativeMethods
{
    private const string LibName = "fluentsend_core";

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int fs_generate_identity(out IntPtr outJson);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void fs_free_string(IntPtr ptr);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr fs_discovery_start(
        string configJson,
        [MarshalAs(UnmanagedType.FunctionPtr)] FsEventCallback eventCb,
        IntPtr cbCtx);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void fs_discovery_announce(IntPtr handle);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void fs_discovery_stop(IntPtr handle);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int fs_discovery_devices(IntPtr handle, out IntPtr outJson);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int fs_send_files(
        string configJson,
        [MarshalAs(UnmanagedType.FunctionPtr)] FsProgressCallback progressCb,
        IntPtr cbCtx);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr fs_server_start(
        string configJson,
        [MarshalAs(UnmanagedType.FunctionPtr)] FsEventCallback eventCb,
        IntPtr cbCtx);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int fs_server_respond(
        IntPtr handle,
        string sessionId,
        string decisionJson);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int fs_server_file_target(
        IntPtr handle,
        string sessionId,
        string fileId,
        string path);
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void FsEventCallback(IntPtr ctx, IntPtr json);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void FsProgressCallback(IntPtr ctx, IntPtr fileId, ulong sent, ulong total);
