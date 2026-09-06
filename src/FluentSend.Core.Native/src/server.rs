use crate::discovery::{parse_device_type, read_c_string};
use crate::json_util::{server_event_json, ServerEventExtracted};
use crate::{emit_event, HandleData, SendPtr, RUNTIME, FsEventCallback, FsHandle};
use localsend::http::server::common::save::FileUploadTarget;
use localsend::http::server::v2::{PrepareUploadDecisionV2, ServerEventV2};
use localsend::http::server::{start_with_port, ServerConfigV2, TlsConfig};
use localsend::http::server::web::WebConfig;
use localsend::http::state::ClientInfo;
use localsend::model::discovery::PROTOCOL_VERSION_V2;
use serde::Deserialize;
use std::collections::{HashMap, HashSet};
use std::ffi::c_char;
use std::path::PathBuf;
use std::sync::{Arc, Mutex};
use tokio::sync::{mpsc, oneshot};

pub(crate) struct ServerRegistry {
    pub event_cb: FsEventCallback,
    pub cb_ctx: SendPtr,
    pending_decisions: Mutex<HashMap<String, oneshot::Sender<PrepareUploadDecisionV2>>>,
    pending_file_targets: Mutex<HashMap<(String, String), oneshot::Sender<FileUploadTarget>>>,
}

#[derive(Deserialize)]
#[serde(rename_all = "camelCase")]
struct ServerConfigJson {
    port: u16,
    alias: String,
    version: Option<String>,
    device_model: Option<String>,
    device_type: Option<String>,
    fingerprint: String,
    cert_pem: Option<String>,
    private_key_pem: Option<String>,
    protocol: String,
    pin: Option<String>,
    verify_checksums: Option<bool>,
}

#[derive(Deserialize)]
#[serde(rename_all = "camelCase")]
struct DecisionJson {
    action: String,
    file_ids: Option<Vec<String>>,
}

#[no_mangle]
pub extern "C" fn fs_server_start(
    config_json: *const c_char,
    event_cb: FsEventCallback,
    cb_ctx: *mut std::ffi::c_void,
) -> *mut FsHandle {
    let config_str = match read_c_string(config_json) {
        Some(s) => s,
        None => return std::ptr::null_mut(),
    };
    let config: ServerConfigJson = match serde_json::from_str(&config_str) {
        Ok(c) => c,
        Err(_) => return std::ptr::null_mut(),
    };

    let ctx = SendPtr(cb_ctx);

    let device_type = config.device_type.as_deref().and_then(parse_device_type);
    let version = config
        .version
        .unwrap_or_else(|| PROTOCOL_VERSION_V2.to_string());

    let info = ClientInfo {
        alias: config.alias,
        version,
        device_model: config.device_model,
        device_type,
        token: config.fingerprint,
    };

    let tls_config = match (config.cert_pem, config.private_key_pem) {
        (Some(cert), Some(key)) => Some(TlsConfig {
            cert,
            private_key: key,
        }),
        _ => None,
    };

    let (event_tx, mut event_rx) = mpsc::channel::<ServerEventV2>(64);
    let v2_config = ServerConfigV2 {
        pin: config.pin,
        verify_checksums: config.verify_checksums.unwrap_or(true),
        event_tx,
    };

    let web_config = WebConfig::default();
    let (stop_tx, stop_rx) = oneshot::channel();

    let registry = Arc::new(ServerRegistry {
        event_cb,
        cb_ctx: ctx,
        pending_decisions: Mutex::new(HashMap::new()),
        pending_file_targets: Mutex::new(HashMap::new()),
    });

    let registry_for_task = registry.clone();
    RUNTIME.spawn(async move {
        while let Some(event) = event_rx.recv().await {
            let (json, extracted) = server_event_json(event);
            match extracted {
                ServerEventExtracted::PrepareUpload {
                    session_id,
                    decision_tx,
                } => {
                    registry_for_task
                        .pending_decisions
                        .lock()
                        .unwrap()
                        .insert(session_id, decision_tx);
                }
                ServerEventExtracted::FileUpload {
                    session_id,
                    file_id,
                    target_tx,
                } => {
                    registry_for_task
                        .pending_file_targets
                        .lock()
                        .unwrap()
                        .insert((session_id, file_id), target_tx);
                }
                _ => {}
            }
            let json_str = json.to_string();
            emit_event(event_cb, ctx, &json_str);
        }
    });

    let handle = match RUNTIME.block_on(async {
        start_with_port(
            config.port,
            tls_config,
            info,
            None,
            Some(v2_config),
            web_config,
            stop_rx,
        )
        .await
    }) {
        Ok(h) => h,
        Err(_) => return std::ptr::null_mut(),
    };

    let stop_tx = Arc::new(Mutex::new(Some(stop_tx)));
    let data = HandleData::Server {
        handle,
        stop_tx,
        registry,
    };
    Box::into_raw(Box::new(data)) as *mut FsHandle
}

#[no_mangle]
pub extern "C" fn fs_server_respond(
    handle: *mut FsHandle,
    session_id: *const c_char,
    decision_json: *const c_char,
) -> i32 {
    if handle.is_null() {
        return -1;
    }
    let session_id = match read_c_string(session_id) {
        Some(s) => s,
        None => return -1,
    };
    let decision_str = match read_c_string(decision_json) {
        Some(s) => s,
        None => return -1,
    };
    let decision: DecisionJson = match serde_json::from_str(&decision_str) {
        Ok(d) => d,
        Err(_) => return -1,
    };

    let data = unsafe { &*(handle as *const HandleData) };
    if let HandleData::Server { registry, .. } = data {
        let tx = registry
            .pending_decisions
            .lock()
            .unwrap()
            .remove(&session_id);
        if let Some(tx) = tx {
            let decision_v2 = match decision.action.as_str() {
                "decline" => PrepareUploadDecisionV2::Decline,
                _ => {
                    let ids: HashSet<String> =
                        decision.file_ids.unwrap_or_default().into_iter().collect();
                    PrepareUploadDecisionV2::Accept(ids)
                }
            };
            let _ = tx.send(decision_v2);
            return 0;
        }
        return -2;
    }
    -1
}

#[no_mangle]
pub extern "C" fn fs_server_file_target(
    handle: *mut FsHandle,
    session_id: *const c_char,
    file_id: *const c_char,
    path: *const c_char,
) -> i32 {
    if handle.is_null() {
        return -1;
    }
    let session_id = match read_c_string(session_id) {
        Some(s) => s,
        None => return -1,
    };
    let file_id = match read_c_string(file_id) {
        Some(s) => s,
        None => return -1,
    };
    let path = match read_c_string(path) {
        Some(s) => s,
        None => return -1,
    };

    let data = unsafe { &*(handle as *const HandleData) };
    if let HandleData::Server { registry, .. } = data {
        let key = (session_id, file_id);
        let tx = registry
            .pending_file_targets
            .lock()
            .unwrap()
            .remove(&key);
        if let Some(tx) = tx {
            let (result_tx, result_rx) = oneshot::channel::<Result<(), String>>();
            let target = FileUploadTarget::Path {
                path: PathBuf::from(&path),
                result_tx,
                progress_tx: None,
            };
            let _ = tx.send(target);
            RUNTIME.spawn(async move {
                let _ = result_rx.await;
            });
            return 0;
        }
        return -2;
    }
    -1
}
