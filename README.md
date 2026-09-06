<div align="center">

<img src="src/FluentSend/FluentSend/Assets/Icon.png" alt="FluentSend" width="128" height="128">

# FluentSend

### 你的最后一款文件传输软件

跨平台离线局域网文件传输 · 兼容 LocalSend 协议 · Fluent Design 2 界面

[![License: Apache 2.0](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20Android-success.svg)](#下载)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com)
[![Avalonia](https://img.shields.io/badge/Avalonia-12.1-01A0E9.svg)](https://avaloniaui.net)
[![FluentAvalonia](https://img.shields.io/badge/FluentAvalonia-3.1-7E57C2.svg)](https://github.com/amwx/FluentAvalonia)
[![Rust Core](https://img.shields.io/badge/Rust%20Core-reuse%20LocalSend-DEA584.svg)](https://github.com/localsend/localsend)
[![Version](https://img.shields.io/badge/Version-v0.45.0--alpha-orange.svg)](#下载)

**Copyright © 2026 HZStudio · 基于 [LocalSend](https://github.com/localsend/localsend) 二次开发**

</div>

---

## 简介

**FluentSend** 是基于 [LocalSend](https://github.com/localsend/localsend) 协议的跨平台离线文件传输软件，宣传语为「你的最后一款文件传输软件」。
* 完全离线，不依赖互联网或任何第三方服务器
* 兼容 LocalSend v2/v3 协议及任意第三方实现
* 同一局域网内自动发现设备，一键发送文本/文件/文件夹/剪贴板
* 三端（Windows / Linux / Android）保持一致的体验与功能
* Fluent Design 2 界面，支持云母（Mica）、薄云母（Mica Alt）、亚克力（Acrylic）材质
* 收件全屏弹窗、文件历史、跨平台一致的交互
* 适用于大型企业内部安全离线传输需求

## 功能一览

| 功能 | Windows | Linux | Android |
|---|:---:|:---:|:---:|
| 局域网设备自动发现（多播） | ✅ | ✅ | ✅ |
| 发送文本 / 文件 / 文件夹 | ✅ | ✅ | ✅ |
| 发送剪贴板最新记录 | ✅ | ✅ | ✅ |
| 发送已安装应用 APK | – | – | ✅ |
| 接收全屏弹窗（确认/取消/编辑保存位置） | ✅ | ✅ | ✅ |
| 文件历史（打开 / 定位文件夹 / 删除 / 清空） | ✅ | ✅ | ✅ |
| 设置（常规 + 高级，会话实时同步） | ✅ | ✅ | ✅ |
| Fluent Design 2（云母/薄云母/亚克力/主题/强调色） | ✅ | ✅ | ✅ |
| 跨平台一致响应式布局（左侧/底部导航） | ✅ | ✅ | ✅ |

## 界面预览

> v0.45.0-alpha 版本截图将随后追加到本节。

| 页面 | 描述 |
|---|---|
| 首页 | 左侧设备列表，右侧聊天式发送区，加号菜单支持文件/文件夹/剪贴板 |
| 文件 | 接收历史列表，支持双击打开、右键定位/删除、清空 |
| 设置 | 常规设置（设备名/下载位置/快捷键）+ 外观（主题/材质/强调色）+ 高级（端口/协议/PIN/校验和） |
| 关于 | 版本、开源协议、致谢 |

## 下载

| 平台 | 产物 | 状态 |
|---|---|---|
| Windows 10 1809+ | `fluentsend-0.45.0-win-x64.zip`（便携）/ `fluentsend-0.45.0-win-x64.msix`（商店/安装包） | ✅ 便携包可由本仓库构建脚本产出 |
| Linux (Ubuntu 20.04+ / Debian 11+) | `fluentsend_0.45.0_amd64.deb` | ✅ 由 `build/linux/build_deb.sh` 产出 |
| Android 9+ (API 23+) | `fluentsend-0.45.0-android.apk` | ✅ 由 `build/android/build_apk.sh` 产出 |

> 当前为 v0.45.0-alpha 阶段，正式版将上架 Microsoft Store。
> macOS / iOS 留给社区贡献者实现。

## 构建

### 依赖

* **.NET 10 SDK**（含 Android 工作负载：`dotnet workload install android`）
* **Avalonia 11.12+** 模板（可选，仅新建工程时需要）
* **Rust 1.78+**（用于构建 `FluentSend.Core.Native` cdylib）
* Windows SDK 10.0.22621+（用于 MSIX 打包与 signtool 签名）

### 从源码构建

```bash
# 克隆仓库（含 LocalSend 原版子模块）
git clone --recurse-submodules https://github.com/WinYC-tver/fluentsend.git
cd fluentsend

# 1. 构建 Rust 内核 C-ABI（cdylib）
cd src/FluentSend/FluentSend.Core.Native
cargo build --release
cd ../../..

# 2. 构建 Windows 便携包
pwsh build/windows/build_portable.ps1

# 3. 构建 Linux deb（在 Linux 上执行）
bash build/linux/build_deb.sh

# 4. 构建 Android APK（在已安装 Android workload 的环境执行）
bash build/android/build_apk.sh

# 5. 运行单元测试
dotnet test src/FluentSend/FluentSend.Tests/FluentSend.Tests.csproj
```

## 架构

```
┌──────────────────────────────────────────────────────────────┐
│                      FluentSend App                          │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  Avalonia UI (FluentAvalonia) + MVVM (CommunityToolkit)│  │
│  └────────────────────────┬───────────────────────────────┘  │
│                           │  P/Invoke                         │
│  ┌────────────────────────▼───────────────────────────────┐  │
│  │         FluentSend.Interop (C# 绑定 / JSON 编解码)     │  │
│  └────────────────────────┬───────────────────────────────┘  │
│                           │  C-ABI (fs_generate_identity …)  │
│  ┌────────────────────────▼───────────────────────────────┐  │
│  │     FluentSend.Core.Native (Rust cdylib shim,           │  │
│  │     通过 cbindgen 导出 fluentsend_core.h)                │  │
│  └────────────────────────┬───────────────────────────────┘  │
│                           │  复用                              │
│  ┌────────────────────────▼───────────────────────────────┐  │
│  │   LocalSend 原版 Rust 内核 (localsend core, features=full)│  │
│  │   - 多播设备发现 / HTTPS 服务端 / 客户端发送 / 证书生成  │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

## 目录结构

```
fluentsend/
├── LICENSE                            # Apache 2.0
├── README.md                          # 本文件
├── localsend/                         # 原版 LocalSend 源码（子模块）
├── src/
│   └── FluentSend/
│       ├── FluentSend.Core.Native/    # Rust cdylib 桥接层
│       ├── FluentSend.Interop/        # C# P/Invoke 绑定
│       ├── FluentSend/                # Avalonia 应用主项目
│       ├── FluentSend.Android/        # Android 主项目
│       ├── FluentSend.Desktop/        # Windows/Linux 桌面入口
│       └── FluentSend.Tests/          # xUnit 单元测试
├── build/
│   ├── windows/                       # exe 便携包 + MSIX 打包脚本
│   ├── linux/                        # deb 控制文件 + 打包脚本
│   └── android/                      # APK 打包脚本
└── dist/                             # 构建产物输出
```

## 致谢

* [LocalSend](https://github.com/localsend/localsend) - 本项目直接复用 LocalSend 的 Rust 内核与协议设计
* [Avalonia UI](https://avaloniaui.net) - 跨平台 .NET UI 框架
* [FluentAvalonia](https://github.com/amwx/FluentAvalonia) - Fluent Design 控件库
* [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - MVVM 框架

## 协议

FluentSend 以 [Apache License 2.0](LICENSE) 协议开源，Copyright © 2026 HZStudio。
本项目基于开源软件 [LocalSend](https://github.com/localsend/localsend) 进行二次开发，遵循其原协议要求致谢与衍生声明。

## 贡献

欢迎提交 Issue 与 PR。
当前为 alpha 阶段，提交前请确保：

* `dotnet build` 与 `dotnet test` 全部通过
* 不破坏跨平台一致性（Windows / Linux / Android 三端同时考虑）
* 遵循现有代码风格与文件结构

> macOS / iOS 平台留待社区贡献者实现。

---

<div align="center">

**FluentSend** — 你的最后一款文件传输软件

Made with ❤️ by HZStudio · 2026

</div>
