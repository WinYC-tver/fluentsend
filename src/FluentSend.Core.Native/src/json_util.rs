use localsend::discovery::{DiscoveryEvent, DiscoveredDevice, StatefulDevice};
use localsend::http::server::v2::{ServerEventV2, SessionEndReasonV2};
use localsend::http::server::web::WebDownloadEvent;
use localsend::http::server::PeerIp;
use localsend::http::server::common::save::FileUploadTarget;
use localsend::model::discovery::{DeviceType, ProtocolType};
use localsend::discovery::{DeviceChannel, ChannelStatus};
use serde_json::{json, Value};

pub fn device_type_str(dt: &DeviceType) -> &'static str {
    match dt {
        DeviceType::Mobile => "mobile",
        DeviceType::Desktop => "desktop",
        DeviceType::Web => "web",
        DeviceType::Headless => "headless",
        DeviceType::Server => "server",
    }
}

pub fn protocol_str(p: ProtocolType) -> &'static str {
    p.as_str()
}

pub fn peer_ip_str(ip: &PeerIp) -> String {
    ip.to_string()
}

pub fn channel_json(channel: &DeviceChannel) -> Value {
    match channel {
        DeviceChannel::Http(http) => json!({
            "type": "http",
            "host": http.host,
            "port": http.port,
            "protocol": protocol_str(http.protocol),
        }),
    }
}

pub fn channel_status_str(status: ChannelStatus) -> &'static str {
    match status {
        ChannelStatus::Available => "available",
        ChannelStatus::NotReachable => "not_reachable",
    }
}

pub fn discovered_device_json(device: &DiscoveredDevice) -> Value {
    json!({
        "alias": device.alias,
        "version": device.version,
        "device_model": device.device_model,
        "device_type": device.device_type.as_ref().map(device_type_str),
        "fingerprint": device.fingerprint,
        "channel": channel_json(&device.channel),
        "download": device.download,
    })
}

pub fn stateful_device_json(sd: &StatefulDevice) -> Value {
    let channels: Vec<Value> = sd
        .channels
        .iter()
        .map(|(ch, status)| {
            let mut entry = channel_json(ch);
            if let Value::Object(ref mut obj) = entry {
                obj.insert("status".to_string(), Value::String(channel_status_str(*status).to_string()));
            }
            entry
        })
        .collect();

    let mut device = discovered_device_json(&sd.device);
    if let Value::Object(ref mut obj) = device {
        obj.insert("channels".to_string(), Value::Array(channels));
    }
    device
}

pub fn discovery_event_json(event: &DiscoveryEvent) -> Value {
    match event {
        DiscoveryEvent::Discovered { device } => json!({
            "type": "discovered",
            "device": discovered_device_json(device),
        }),
        DiscoveryEvent::Updated { device } => json!({
            "type": "updated",
            "device": discovered_device_json(device),
        }),
        DiscoveryEvent::MulticastFailed => json!({
            "type": "multicast_failed",
        }),
    }
}

pub fn session_end_reason_str(reason: SessionEndReasonV2) -> &'static str {
    match reason {
        SessionEndReasonV2::Finished => "finished",
        SessionEndReasonV2::Cancelled => "cancelled",
    }
}

pub fn server_event_json(event: ServerEventV2) -> (Value, ServerEventExtracted) {
    match event {
        ServerEventV2::Register { ip, info } => (
            json!({
                "type": "register",
                "ip": peer_ip_str(&ip),
                "info": serde_json::to_value(&info).unwrap_or(Value::Null),
            }),
            ServerEventExtracted::None,
        ),
        ServerEventV2::PrepareUpload {
            session_id,
            ip,
            info,
            cert_fingerprint,
            files,
            decision_tx,
        } => (
            json!({
                "type": "prepare_upload",
                "session_id": session_id,
                "ip": peer_ip_str(&ip),
                "info": serde_json::to_value(&info).unwrap_or(Value::Null),
                "cert_fingerprint": cert_fingerprint,
                "files": serde_json::to_value(&files).unwrap_or(Value::Null),
            }),
            ServerEventExtracted::PrepareUpload { session_id, decision_tx },
        ),
        ServerEventV2::FileUpload {
            session_id,
            file_id,
            file,
            target_tx,
        } => (
            json!({
                "type": "file_upload",
                "session_id": session_id,
                "file_id": file_id,
                "file": serde_json::to_value(&file).unwrap_or(Value::Null),
            }),
            ServerEventExtracted::FileUpload {
                session_id,
                file_id,
                target_tx,
            },
        ),
        ServerEventV2::SessionEnd { session_id, reason } => (
            json!({
                "type": "session_end",
                "session_id": session_id,
                "reason": session_end_reason_str(reason),
            }),
            ServerEventExtracted::None,
        ),
        ServerEventV2::PrepareUploadAborted { session_id } => (
            json!({
                "type": "prepare_upload_aborted",
                "session_id": session_id,
            }),
            ServerEventExtracted::PrepareUploadAborted { session_id },
        ),
        ServerEventV2::CancelReceived { ip, session_id } => (
            json!({
                "type": "cancel_received",
                "ip": peer_ip_str(&ip),
                "session_id": session_id,
            }),
            ServerEventExtracted::None,
        ),
        ServerEventV2::ListenerFailed { error } => (
            json!({
                "type": "listener_failed",
                "error": error,
            }),
            ServerEventExtracted::None,
        ),
    }
}

pub enum ServerEventExtracted {
    None,
    PrepareUpload {
        session_id: String,
        decision_tx: tokio::sync::oneshot::Sender<localsend::http::server::v2::PrepareUploadDecisionV2>,
    },
    FileUpload {
        session_id: String,
        file_id: String,
        target_tx: tokio::sync::oneshot::Sender<FileUploadTarget>,
    },
    PrepareUploadAborted {
        session_id: String,
    },
}

pub fn web_download_event_json(event: WebDownloadEvent) -> (Value, WebDownloadEventExtracted) {
    match event {
        WebDownloadEvent::PrepareDownload {
            ip,
            session_id,
            user_agent,
            decision_tx,
        } => (
            json!({
                "type": "prepare_download",
                "ip": peer_ip_str(&ip),
                "session_id": session_id,
                "user_agent": user_agent,
            }),
            WebDownloadEventExtracted::PrepareDownload { session_id, decision_tx },
        ),
        WebDownloadEvent::FileDownload {
            session_id,
            file_id,
            file,
            content_tx,
        } => (
            json!({
                "type": "file_download",
                "session_id": session_id,
                "file_id": file_id,
                "file": serde_json::to_value(&file).unwrap_or(Value::Null),
            }),
            WebDownloadEventExtracted::FileDownload {
                session_id,
                file_id,
                content_tx,
            },
        ),
    }
}

pub enum WebDownloadEventExtracted {
    PrepareDownload {
        session_id: String,
        decision_tx: tokio::sync::oneshot::Sender<bool>,
    },
    FileDownload {
        session_id: String,
        file_id: String,
        content_tx: tokio::sync::oneshot::Sender<localsend::model::transfer::FileContent>,
    },
}
