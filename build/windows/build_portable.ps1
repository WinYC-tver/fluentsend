#!/usr/bin/env pwsh
<#
.SYNOPSIS
构建 FluentSend Windows 便携包：self-contained 单文件 exe + Rust cdylib
#>
param(
    [string]$OutDir = (Resolve-Path "$PSScriptRoot\..\.."),
    [string]$Config = "Release",
    [string]$Rid    = "win-x64"
)

$ErrorActionPreference = "Stop"
$Root  = Resolve-Path "$PSScriptRoot\..\.."
$RustProj = Join-Path $Root "src\FluentSend.Core.Native"
$Project = Join-Path $Root "src\FluentSend\FluentSend.Desktop\FluentSend.Desktop.csproj"
$Stage  = Join-Path $OutDir "publish\$Rid"
$Zip    = Join-Path $OutDir "fluentsend-0.45.0-$Rid.zip"

Write-Host "[1/4] 构建 Rust cdylib (fluentsend_core.dll)..."
Push-Location $RustProj
try {
    cargo build --release
    if ($LASTEXITCODE -ne 0) { throw "cargo build 失败 ($LASTEXITCODE)" }
} finally { Pop-Location }

$rustDll = Join-Path $RustProj "target\release\fluentsend_core.dll"
if (-not (Test-Path $rustDll)) {
    throw "未找到 Rust 构建产物: $rustDll"
}

Write-Host "[2/4] 发布 FluentSend 桌面应用 ($Rid, $Config)..."
if (Test-Path $Stage) { Remove-Item $Stage -Recurse -Force }
dotnet publish $Project `
    -c $Config `
    -f net10.0 `
    -r $Rid `
    --self-contained true `
    -p:PublishSingleFile=$true `
    -p:PublishReadyToRun=$true `
    -p:IncludeNativeLibrariesForSelfExtract=$true `
    -p:DebugType=none `
    -p:EnableCompressionInSingleFile=$true `
    -o $Stage
if ($LASTEXITCODE -ne 0) { throw "dotnet publish 失败 ($LASTEXITCODE)" }

Write-Host "[3/4] 复制运行时依赖..."
Copy-Item -Path $rustDll -Destination (Join-Path $Stage "fluentsend_core.dll") -Force
$assetsSrc = Join-Path $Root "src\FluentSend\FluentSend\Assets"
if (Test-Path $assetsSrc) {
    $assetsDst = Join-Path $Stage "Assets"
    New-Item -ItemType Directory -Force -Path $assetsDst | Out-Null
    Copy-Item -Path "$assetsSrc\*" -Destination $assetsDst -Recurse -Force
}
Copy-Item -Path (Join-Path $Root "LICENSE") -Destination $Stage -Force
Copy-Item -Path (Join-Path $Root "README.md") -Destination $Stage -Force

Write-Host "[4/4] 打包 zip 便携包..."
if (Test-Path $Zip) { Remove-Item $Zip -Force }
Compress-Archive -Path "$Stage\*" -DestinationPath $Zip -CompressionLevel Optimal

$size = [math]::Round((Get-Item $Zip).Length / 1MB, 2)
Write-Host "完成：$Zip ($size MB)"
return $Zip
