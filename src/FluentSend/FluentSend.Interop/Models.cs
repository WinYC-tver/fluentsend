using System.Text.Json;
using System.Text.Json.Serialization;

namespace FluentSend.Interop;

public sealed class DeviceIdentity
{
    [JsonPropertyName("certPem")]
    public string CertPem { get; set; } = string.Empty;

    [JsonPropertyName("privateKeyPem")]
    public string PrivateKeyPem { get; set; } = string.Empty;

    [JsonPropertyName("publicKeyPem")]
    public string PublicKeyPem { get; set; } = string.Empty;

    [JsonPropertyName("fingerprint")]
    public string Fingerprint { get; set; } = string.Empty;
}

public sealed class DiscoveryConfig
{
    [JsonPropertyName("alias")]
    public string Alias { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("deviceModel")]
    public string? DeviceModel { get; set; }

    [JsonPropertyName("deviceType")]
    public string? DeviceType { get; set; }

    [JsonPropertyName("fingerprint")]
    public string Fingerprint { get; set; } = string.Empty;

    [JsonPropertyName("httpPort")]
    public ushort HttpPort { get; set; }

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = "https";

    [JsonPropertyName("download")]
    public bool? Download { get; set; }

    [JsonPropertyName("certPem")]
    public string CertPem { get; set; } = string.Empty;

    [JsonPropertyName("privateKeyPem")]
    public string PrivateKeyPem { get; set; } = string.Empty;

    [JsonPropertyName("multicastPort")]
    public ushort? MulticastPort { get; set; }

    [JsonPropertyName("multicastGroup")]
    public string? MulticastGroup { get; set; }

    [JsonPropertyName("multicastGroupV6")]
    public string? MulticastGroupV6 { get; set; }

    [JsonPropertyName("interfaceWhitelist")]
    public List<string>? InterfaceWhitelist { get; set; }

    [JsonPropertyName("interfaceBlacklist")]
    public List<string>? InterfaceBlacklist { get; set; }

    [JsonPropertyName("timeoutMs")]
    public ulong? TimeoutMs { get; set; }
}

public sealed class ServerConfig
{
    [JsonPropertyName("port")]
    public ushort Port { get; set; }

    [JsonPropertyName("alias")]
    public string Alias { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("deviceModel")]
    public string? DeviceModel { get; set; }

    [JsonPropertyName("deviceType")]
    public string? DeviceType { get; set; }

    [JsonPropertyName("fingerprint")]
    public string Fingerprint { get; set; } = string.Empty;

    [JsonPropertyName("certPem")]
    public string? CertPem { get; set; }

    [JsonPropertyName("privateKeyPem")]
    public string? PrivateKeyPem { get; set; }

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = "https";

    [JsonPropertyName("pin")]
    public string? Pin { get; set; }

    [JsonPropertyName("verifyChecksums")]
    public bool? VerifyChecksums { get; set; }
}

public sealed class SendFile
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public ulong Size { get; set; }

    [JsonPropertyName("fileType")]
    public string FileType { get; set; } = "file";

    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }

    [JsonPropertyName("preview")]
    public string? Preview { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
}

public sealed class SendConfig
{
    [JsonPropertyName("ip")]
    public string Ip { get; set; } = string.Empty;

    [JsonPropertyName("port")]
    public ushort Port { get; set; }

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = "https";

    [JsonPropertyName("alias")]
    public string Alias { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("deviceModel")]
    public string? DeviceModel { get; set; }

    [JsonPropertyName("deviceType")]
    public string? DeviceType { get; set; }

    [JsonPropertyName("fingerprint")]
    public string Fingerprint { get; set; } = string.Empty;

    [JsonPropertyName("certPem")]
    public string? CertPem { get; set; }

    [JsonPropertyName("privateKeyPem")]
    public string? PrivateKeyPem { get; set; }

    [JsonPropertyName("pin")]
    public string? Pin { get; set; }

    [JsonPropertyName("senderPort")]
    public ushort? SenderPort { get; set; }

    [JsonPropertyName("files")]
    public List<SendFile> Files { get; set; } = [];
}

public sealed class Decision
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = "accept";

    [JsonPropertyName("fileIds")]
    public List<string>? FileIds { get; set; }
}

public sealed class DeviceChannel
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("host")]
    public string Host { get; set; } = string.Empty;

    [JsonPropertyName("port")]
    public ushort Port { get; set; }

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Status { get; set; }
}

public sealed class DiscoveredDevice
{
    [JsonPropertyName("alias")]
    public string Alias { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("deviceModel")]
    public string? DeviceModel { get; set; }

    [JsonPropertyName("deviceType")]
    public string? DeviceType { get; set; }

    [JsonPropertyName("fingerprint")]
    public string Fingerprint { get; set; } = string.Empty;

    [JsonPropertyName("channel")]
    public DeviceChannel? Channel { get; set; }

    [JsonPropertyName("channels")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<DeviceChannel>? Channels { get; set; }

    [JsonPropertyName("download")]
    public bool Download { get; set; }
}

public sealed class DiscoveryEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("device")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DiscoveredDevice? Device { get; set; }
}

public sealed class ServerEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("ip")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Ip { get; set; }

    [JsonPropertyName("info")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement Info { get; set; }

    [JsonPropertyName("sessionId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SessionId { get; set; }

    [JsonPropertyName("certFingerprint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CertFingerprint { get; set; }

    [JsonPropertyName("files")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Files { get; set; }

    [JsonPropertyName("fileId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FileId { get; set; }

    [JsonPropertyName("file")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? File { get; set; }

    [JsonPropertyName("reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reason { get; set; }
}
