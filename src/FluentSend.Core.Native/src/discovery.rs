use crate::{cstring_from_json, emit_event, HandleData, SendPtr, RUNTIME, FsHandle, FsEventCallback};
use localsend::discovery::{DeviceIdentity, DiscoveryConfig, DiscoveryEvent};
use localsend::model::discovery::{DeviceType, ProtocolType, PROTOCOL_VERSION_V2};
use localsend::multicast::{DEFAULT_MULTICAST_GROUP, DEFAULT_PORT, DEFAULT_MULTICAST_GROUP_V6, MulticastDevice};
use localsend::util::interface::InterfaceFilter;
use serde::Deserialize;
use std::ffi::{c_char, CStr};
use std::net::{Ipv4Addr, Ipv6Addr};
use std::sync::{Arc, Mutex};
use std::time::Duration;
use tokio::sync::{mpsc, oneshot};

pub(crate) fn parse_device_type(s: &str) -> Option<DeviceType> {
    Some(match s.to_lowercase().as_str() {
        "mobile" => DeviceType::Mobile,
        "desktop" => DeviceType::Desktop,
        "web" => DeviceType::Web,
        "headless" => DeviceType::Headless,
        "server" => DeviceType::Server,
        _ => DeviceType::Desktop,
    })
}

pub(crate) fn parse_protocol(s: &str) -> ProtocolType {
    match s.to_lowercase().as_str() {
        "https" => ProtocolType::Https,
        _ => ProtocolType::Http,
    }
}

pub(crate) fn read_c_string(ptr: *const c_char) -> Option<String> {
    if ptr.is_null() {
        return None;
    }
    let cstr = unsafe { CStr::from_ptr(ptr) };
    Some(cstr.to_string_lossy().into_owned())
}

#[derive(Deserialize)]
#[serde(rename_all = "camelCase")]
struct DiscoveryConfigJson {
    alias: String,
    version: Option<String>,
    device_model: Option<String>,
    device_type: Option<String>,
    fingerprint: String,
    http_port: u16,
    protocol: String,
    download: Option<bool>,
    cert_pem: String,
    private_key_pem: String,
    multicast_port: Option<u16>,
    multicast_group: Option<String>,
    multicast_group_v6: Option<String>,
    interface_whitelist: Option<Vec<String>>,
    interface_blacklist: Option<Vec<String>>,
    timeout_ms: Option<u64>,
}

#[no_mangle]
pub extern "C" fn fs_discovery_start(
    config_json: *const c_char,
    event_cb: FsEventCallback,
    cb_ctx: *mut std::ffi::c_void,
) -> *mut FsHandle {
    let config_str = match read_c_string(config_json) {
        Some(s) => s,
        None => return std::ptr::null_mut(),
    };
    let config_json: DiscoveryConfigJson = match serde_json::from_str(&config_str) {
        Ok(c) => c,
        Err(_) => return std::ptr::null_mut(),
    };

    let ctx = SendPtr(cb_ctx);

    let protocol = parse_protocol(&config_json.protocol);
    let device_type = config_json.device_type.as_deref().and_then(parse_device_type);
    let version = config_json.version.unwrap_or_else(|| PROTOCOL_VERSION_V2.to_string());

    let device = MulticastDevice {
        alias: config_json.alias,
        version,
        device_model: config_json.device_model,
        device_type,
        fingerprint: config_json.fingerprint,
        port: config_json.http_port,
        protocol,
        download: config_json.download.unwrap_or(false),
    };

    let identity = DeviceIdentity {
        cert_pem: config_json.cert_pem,
        private_key_pem: config_json.private_key_pem,
    };

    let group = config_json
        .multicast_group
        .and_then(|g| g.parse::<Ipv4Addr>().ok())
        .unwrap_or(DEFAULT_MULTICAST_GROUP);

    let group_v6 = if let Some(g) = config_json.multicast_group_v6 {
        g.parse::<Ipv6Addr>().ok().or(Some(DEFAULT_MULTICAST_GROUP_V6))
    } else {
        Some(DEFAULT_MULTICAST_GROUP_V6)
    };

    let port = config_json.multicast_port.unwrap_or(DEFAULT_PORT);

    let interface_filter = InterfaceFilter {
        whitelist: config_json.interface_whitelist,
        blacklist: config_json.interface_blacklist,
    };

    let timeout = Duration::from_millis(config_json.timeout_ms.unwrap_or(500));

    let (event_tx, mut event_rx) = mpsc::channel::<DiscoveryEvent>(64);

    let config = DiscoveryConfig {
        group,
        group_v6,
        port,
        interface_filter,
        device,
        identity,
        timeout,
        event_tx: Some(event_tx),
    };

    let (stop_tx, stop_rx) = oneshot::channel();

    let handle = RUNTIME.block_on(async {
        localsend::discovery::start(config, stop_rx).await
    });

    let stop_tx = Arc::new(Mutex::new(Some(stop_tx)));

    RUNTIME.spawn(async move {
        while let Some(event) = event_rx.recv().await {
            let json = crate::json_util::discovery_event_json(&event);
            let json_str = json.to_string();
            emit_event(event_cb, ctx, &json_str);
        }
    });

    let data = HandleData::Discovery { handle, stop_tx };
    Box::into_raw(Box::new(data)) as *mut FsHandle
}

#[no_mangle]
pub extern "C" fn fs_discovery_announce(handle: *mut FsHandle) {
    if handle.is_null() {
        return;
    }
    let data = unsafe { &*(handle as *const HandleData) };
    if let HandleData::Discovery { handle, .. } = data {
        RUNTIME.block_on(handle.announce());
    }
}

#[no_mangle]
pub extern "C" fn fs_discovery_stop(handle: *mut FsHandle) {
    if handle.is_null() {
        return;
    }
    let data = unsafe { &*(handle as *const HandleData) };
    if let HandleData::Discovery { handle, stop_tx } = data {
        let tx = stop_tx.lock().unwrap().take();
        if let Some(tx) = tx {
            let _ = tx.send(());
        }
        RUNTIME.block_on(handle.wait_stopped());
    }
}

#[no_mangle]
pub extern "C" fn fs_discovery_devices(
    handle: *mut FsHandle,
    out_json: *mut *mut c_char,
) -> i32 {
    if handle.is_null() || out_json.is_null() {
        return -1;
    }
    let data = unsafe { &*(handle as *const HandleData) };
    if let HandleData::Discovery { handle, .. } = data {
        let devices = handle.devices();
        let json_arr: Vec<serde_json::Value> = devices
            .iter()
            .map(crate::json_util::stateful_device_json)
            .collect();
        let json = serde_json::Value::Array(json_arr);
        let c_str = cstring_from_json(&json);
        unsafe {
            *out_json = c_str.into_raw();
        }
        return 0;
    }
    -1
}
