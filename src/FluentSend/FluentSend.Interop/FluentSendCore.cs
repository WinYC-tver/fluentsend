using System.Runtime.InteropServices;
using System.Text.Json;

namespace FluentSend.Interop;

public sealed class FluentSendCore
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static DeviceIdentity GenerateIdentity()
    {
        int rc = NativeMethods.fs_generate_identity(out IntPtr ptr);
        if (rc != 0 || ptr == IntPtr.Zero)
            throw new InvalidOperationException($"fs_generate_identity failed: {rc}");

        try
        {
            string json = PtrToString(ptr)!;
            return JsonSerializer.Deserialize<DeviceIdentity>(json, JsonOpts)!;
        }
        finally
        {
            NativeMethods.fs_free_string(ptr);
        }
    }

    public static DiscoveryHandle StartDiscovery(DiscoveryConfig config, Action<DiscoveryEvent> onEvent)
    {
        string json = JsonSerializer.Serialize(config, JsonOpts);
        GCHandle ctxHandle = GCHandle.Alloc(onEvent);
        FsEventCallback cb = (ctx, jsonPtr) =>
        {
            var cb2 = (Action<DiscoveryEvent>)GCHandle.FromIntPtr(ctx).Target!;
            string? jsonStr = PtrToString(jsonPtr);
            if (jsonStr != null)
            {
                var evt = JsonSerializer.Deserialize<DiscoveryEvent>(jsonStr, JsonOpts);
                if (evt != null)
                    cb2(evt);
            }
        };
        IntPtr ptr = NativeMethods.fs_discovery_start(json, cb, GCHandle.ToIntPtr(ctxHandle));
        if (ptr == IntPtr.Zero)
        {
            ctxHandle.Free();
            throw new InvalidOperationException("fs_discovery_start returned null");
        }
        return new DiscoveryHandle(ptr, ctxHandle, cb);
    }

    public static List<DiscoveredDevice> GetDevices(DiscoveryHandle handle)
    {
        int rc = NativeMethods.fs_discovery_devices(handle.Ptr, out IntPtr ptr);
        if (rc != 0 || ptr == IntPtr.Zero)
            return [];

        try
        {
            string json = PtrToString(ptr)!;
            return JsonSerializer.Deserialize<List<DiscoveredDevice>>(json, JsonOpts) ?? [];
        }
        finally
        {
            NativeMethods.fs_free_string(ptr);
        }
    }

    public static ServerHandle StartServer(ServerConfig config, Action<ServerEvent> onEvent)
    {
        string json = JsonSerializer.Serialize(config, JsonOpts);
        GCHandle ctxHandle = GCHandle.Alloc(onEvent);
        FsEventCallback cb = (ctx, jsonPtr) =>
        {
            var cb2 = (Action<ServerEvent>)GCHandle.FromIntPtr(ctx).Target!;
            string? jsonStr = PtrToString(jsonPtr);
            if (jsonStr != null)
            {
                var evt = JsonSerializer.Deserialize<ServerEvent>(jsonStr, JsonOpts);
                if (evt != null)
                    cb2(evt);
            }
        };
        IntPtr ptr = NativeMethods.fs_server_start(json, cb, GCHandle.ToIntPtr(ctxHandle));
        if (ptr == IntPtr.Zero)
        {
            ctxHandle.Free();
            throw new InvalidOperationException("fs_server_start returned null");
        }
        return new ServerHandle(ptr, ctxHandle, cb);
    }

    public static int ServerRespond(ServerHandle handle, string sessionId, Decision decision)
    {
        string json = JsonSerializer.Serialize(decision, JsonOpts);
        return NativeMethods.fs_server_respond(handle.Ptr, sessionId, json);
    }

    public static int ServerFileTarget(ServerHandle handle, string sessionId, string fileId, string path)
    {
        return NativeMethods.fs_server_file_target(handle.Ptr, sessionId, fileId, path);
    }

    public static int SendFiles(SendConfig config, Action<string, ulong, ulong>? onProgress)
    {
        string json = JsonSerializer.Serialize(config, JsonOpts);
        GCHandle ctxHandle = onProgress != null ? GCHandle.Alloc(onProgress) : default;
        FsProgressCallback cb = onProgress != null ? (ctx, fileIdPtr, sent, total) =>
        {
            var cb2 = (Action<string, ulong, ulong>)GCHandle.FromIntPtr(ctx).Target!;
            string? fileId = PtrToString(fileIdPtr);
            if (fileId != null)
                cb2(fileId, sent, total);
        } : (ctx, fileIdPtr, sent, total) => { };

        try
        {
            return NativeMethods.fs_send_files(json, cb, ctxHandle != default ? GCHandle.ToIntPtr(ctxHandle) : IntPtr.Zero);
        }
        finally
        {
            if (onProgress != null && ctxHandle.IsAllocated)
                ctxHandle.Free();
        }
    }

    internal static string? PtrToString(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
            return null;
        int len = 0;
        while (Marshal.ReadByte(ptr, len) != 0)
            len++;
        byte[] buffer = new byte[len];
        Marshal.Copy(ptr, buffer, 0, len);
        return System.Text.Encoding.UTF8.GetString(buffer);
    }
}

public sealed class DiscoveryHandle : IDisposable
{
    internal IntPtr Ptr { get; }
    private readonly GCHandle _ctxHandle;
    private readonly FsEventCallback _cb;

    internal DiscoveryHandle(IntPtr ptr, GCHandle ctxHandle, FsEventCallback cb)
    {
        Ptr = ptr;
        _ctxHandle = ctxHandle;
        _cb = cb;
    }

    public void Announce() => NativeMethods.fs_discovery_announce(Ptr);

    public void Dispose()
    {
        NativeMethods.fs_discovery_stop(Ptr);
        GC.KeepAlive(_cb);
        if (_ctxHandle.IsAllocated)
            _ctxHandle.Free();
    }
}

public sealed class ServerHandle : IDisposable
{
    internal IntPtr Ptr { get; }
    private readonly GCHandle _ctxHandle;
    private readonly FsEventCallback _cb;

    internal ServerHandle(IntPtr ptr, GCHandle ctxHandle, FsEventCallback cb)
    {
        Ptr = ptr;
        _ctxHandle = ctxHandle;
        _cb = cb;
    }

    public void Dispose()
    {
        NativeMethods.fs_discovery_stop(Ptr);
        GC.KeepAlive(_cb);
        if (_ctxHandle.IsAllocated)
            _ctxHandle.Free();
    }
}
