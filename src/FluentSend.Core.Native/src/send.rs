use crate::discovery::{parse_device_type, parse_protocol, read_c_string};
use crate::{SendPtr, RUNTIME, FsProgressCallback};
use localsend::http::client::{ClientError, LsHttpClient, LsHttpClientV2, LsHttpClientVersion};
use localsend::http::dto::{PrepareUploadRequestDto, RegisterDto};
use localsend::model::discovery::{ProtocolType, PROTOCOL_VERSION_V2};
use localsend::model::transfer::{FileContent, FileDto};
use serde::Deserialize;
use std::collections::HashMap;
use std::ffi::{c_char, CString};
use std::path::PathBuf;
use tokio_util::sync::CancellationToken;

#[derive(Deserialize)]
#[serde(rename_all = "camelCase")]
struct SendConfigJson {
    ip: String,
    port: u16,
    protocol: String,
    alias: String,
    version: Option<String>,
    device_model: Option<String>,
    device_type: Option<String>,
    fingerprint: String,
    cert_pem: Option<String>,
    private_key_pem: Option<String>,
    pin: Option<String>,
    sender_port: Option<u16>,
    files: Vec<SendFileJson>,
}

#[derive(Deserialize)]
#[serde(rename_all = "camelCase")]
struct SendFileJson {
    id: String,
    file_name: String,
    size: u64,
    file_type: String,
    sha256: Option<String>,
    preview: Option<String>,
    path: String,
}

#[no_mangle]
pub extern "C" fn fs_send_files(
    config_json: *const c_char,
    progress_cb: FsProgressCallback,
    cb_ctx: *mut std::ffi::c_void,
) -> i32 {
    let config_str = match read_c_string(config_json) {
        Some(s) => s,
        None => return -1,
    };
    let config: SendConfigJson = match serde_json::from_str(&config_str) {
        Ok(c) => c,
        Err(_) => return -1,
    };

    let ctx = SendPtr(cb_ctx);
    let protocol = parse_protocol(&config.protocol);
    let device_type = config.device_type.as_deref().and_then(parse_device_type);
    let version = config
        .version
        .unwrap_or_else(|| PROTOCOL_VERSION_V2.to_string());
    let sender_port = config.sender_port.unwrap_or(0);

    let client = match protocol {
        ProtocolType::Https => {
            let (cert_pem, private_key_pem) = match (config.cert_pem, config.private_key_pem) {
                (Some(c), Some(k)) => (c, k),
                _ => return -2,
            };
            match LsHttpClient::new(
                &private_key_pem,
                &cert_pem,
                LsHttpClientVersion::V2,
                Some(config.fingerprint.clone()),
                None,
            ) {
                Ok(c) => c,
                Err(_) => return -3,
            }
        }
        ProtocolType::Http => LsHttpClient::V2(
            match LsHttpClientV2::try_new_without_cert() {
                Ok(c) => c,
                Err(_) => return -3,
            },
        ),
    };

    let file_paths: HashMap<String, PathBuf> = config
        .files
        .iter()
        .map(|f| (f.id.clone(), PathBuf::from(&f.path)))
        .collect();

    let file_sizes: HashMap<String, u64> = config
        .files
        .iter()
        .map(|f| (f.id.clone(), f.size))
        .collect();

    let files: HashMap<String, FileDto> = config
        .files
        .into_iter()
        .map(|f| {
            let dto = FileDto {
                id: f.id.clone(),
                file_name: f.file_name,
                size: f.size,
                file_type: f.file_type,
                sha256: f.sha256,
                preview: f.preview,
                metadata: None,
            };
            (f.id, dto)
        })
        .collect();

    let register_dto = RegisterDto {
        alias: config.alias,
        version,
        device_model: config.device_model,
        device_type,
        token: config.fingerprint,
        port: sender_port,
        protocol,
        has_web_interface: false,
    };

    let prepare_dto = PrepareUploadRequestDto {
        info: register_dto,
        files,
    };

    let pin = config.pin;
    let ip = config.ip;
    let port = config.port;

    let cancel = CancellationToken::new();

    let result = RUNTIME.block_on(async {
        let prepare_result = client
            .prepare_upload(
                protocol,
                &ip,
                port,
                None,
                prepare_dto,
                pin.as_deref(),
                cancel.clone(),
            )
            .await;

        let prepare_result = match prepare_result {
            Ok(r) => r,
            Err(ClientError::StatusCode(e)) => return Err(e.status as i32),
            Err(_) => return Err(-5),
        };

        if prepare_result.status_code == 204 || prepare_result.response.is_none() {
            return Ok(0);
        }

        let response = prepare_result.response.unwrap();
        let session_id = response.session_id;

        for (file_id, token) in &response.files {
            let path = match file_paths.get(file_id) {
                Some(p) => p.clone(),
                None => continue,
            };

            let file_size = match file_sizes.get(file_id) {
                Some(s) => *s,
                None => continue,
            };

            let fid = file_id.clone();
            let ctx_raw = cb_ctx as usize;
            let cb = progress_cb;

            let progress = move |sent: u64| {
                let c_file_id = CString::new(fid.as_str()).unwrap_or_default();
                cb(ctx_raw as *mut std::ffi::c_void, c_file_id.as_ptr(), sent, file_size);
            };

            let content = FileContent::Path(path);

            if let Err(ClientError::StatusCode(e)) = client
                .upload(
                    protocol,
                    &ip,
                    port,
                    None,
                    &session_id,
                    file_id,
                    token,
                    content,
                    progress,
                    cancel.clone(),
                )
                .await
            {
                return Err(e.status as i32);
            }
        }

        Ok(0)
    });

    match result {
        Ok(code) => code,
        Err(code) => code,
    }
}
