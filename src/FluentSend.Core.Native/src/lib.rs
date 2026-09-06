//! FluentSend C-ABI shim over the original LocalSend Rust core.
//!
//! Re-uses LocalSend's protocol (HTTP v2 server/client, multicast discovery,
//! crypto) unchanged. The FluentSend .NET UI drives it via P/Invoke through
//! the `fs_*` functions exposed here, receiving events as JSON on a callback.

mod discovery;
mod json_util;
mod send;
mod server;

use localsend::discovery::DiscoveryHandle;
use localsend::http::server::ServerHandle;
use once_cell::sync::Lazy;
use serde_json::Value;
use std::ffi::{c_char, c_void, CString};
use std::sync::{Arc, Mutex};
use tokio::runtime::Runtime;
use tokio::sync::oneshot;

/// Global Tokio runtime that drives all async localsend core operations.
/// P/Invoke calls are synchronous; long-running work is spawned here and
/// results surfaced via event callbacks.
static RUNTIME: Lazy<Runtime> = Lazy::new(|| Runtime::new().expect("Failed to create Tokio runtime"));

/// Callback invoked by the core to deliver an event as a JSON string.
/// `ctx` is the opaque pointer the caller passed when starting the subsystem.
/// The `json` pointer is only valid for the duration of the call.
pub type FsEventCallback = extern "C" fn(ctx: *mut c_void, json: *const c_char);

/// Callback invoked during a send to report per-file progress.
pub type FsProgressCallback = extern "C" fn(ctx: *mut c_void, file_id: *const c_char, sent: u64, total: u64);

/// Opaque handle returned by `fs_discovery_start` / `fs_server_start`.
#[repr(C)]
pub struct FsHandle {
    _private: [u8; 0],
}

/// Wraps a raw context pointer so it can be sent across threads.
#[derive(Clone, Copy)]
struct SendPtr(pub *mut c_void);
unsafe impl Send for SendPtr {}
unsafe impl Sync for SendPtr {}

/// Owns a running subsystem. Kept opaque to the C side; the .NET layer only
/// holds the `*mut FsHandle` pointer.
enum HandleData {
    Discovery {
        handle: DiscoveryHandle,
        stop_tx: Arc<Mutex<Option<oneshot::Sender<()>>>>,
    },
    Server {
        handle: ServerHandle,
        stop_tx: Arc<Mutex<Option<oneshot::Sender<()>>>>,
        registry: Arc<server::ServerRegistry>,
    },
}

/// Emits an event JSON string to the C# callback.
fn emit_event(cb: FsEventCallback, ctx: SendPtr, json: &str) {
    let c_str = CString::new(json).unwrap_or_default();
    cb(ctx.0, c_str.as_ptr());
}

/// Serializes a JSON value into a C string owned by the caller (freed via `fs_free_string`).
fn cstring_from_json(value: &Value) -> CString {
    CString::new(value.to_string()).unwrap_or_default()
}

/// Generates a new device identity (self-signed RSA-2048 certificate).
/// Writes a JSON string to `out_json`: { certPem, privateKeyPem, publicKeyPem, fingerprint }.
/// Returns 0 on success. Caller must free the string with `fs_free_string`.
#[no_mangle]
pub extern "C" fn fs_generate_identity(out_json: *mut *mut c_char) -> i32 {
    if out_json.is_null() {
        return -1;
    }
    let cert = match localsend::crypto::cert::generate_self_signed() {
        Ok(c) => c,
        Err(_) => return -2,
    };
    let json = serde_json::json!({
        "certPem": cert.certificate_pem,
        "privateKeyPem": cert.private_key_pem,
        "publicKeyPem": cert.public_key_pem,
        "fingerprint": cert.fingerprint,
    });
    let c_str = cstring_from_json(&json);
    unsafe {
        *out_json = c_str.into_raw();
    }
    0
}

/// Frees a string previously returned by the core (e.g. from `fs_generate_identity`
/// or `fs_discovery_devices`).
#[no_mangle]
pub extern "C" fn fs_free_string(ptr: *mut c_char) {
    if !ptr.is_null() {
        unsafe {
            let _ = CString::from_raw(ptr);
        }
    }
}
