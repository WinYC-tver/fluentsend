use std::env;
use std::path::PathBuf;

fn main() {
    let crate_dir = env::var("CARGO_MANIFEST_DIR").unwrap();

    let config = cbindgen::Config::from_file(
        PathBuf::from(&crate_dir).join("cbindgen.toml"),
    )
    .unwrap_or_default();

    cbindgen::generate_with_config(&crate_dir, config)
        .expect("Unable to generate C bindings")
        .write_to_file("fluentsend_core.h");

    println!("cargo:rerun-if-changed=src/lib.rs");
    println!("cargo:rerun-if-changed=src/discovery.rs");
    println!("cargo:rerun-if-changed=src/server.rs");
    println!("cargo:rerun-if-changed=src/send.rs");
    println!("cargo:rerun-if-changed=src/json_util.rs");
    println!("cargo:rerun-if-changed=cbindgen.toml");
}
