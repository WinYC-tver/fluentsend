#!/usr/bin/env pwsh
<#
.SYNOPSIS
构建 FluentSend Windows 便携包：self-contained 单文件 exe
#>
param(
    [string]$OutDir = (Resolve-Path "$PSScriptRoot\..\.."),
    [string]$Config = "Release",
    [string]$Rid    = "win-x64"
)

$ErrorActionPreference = "Stop"
$Root  = Resolve-Path "$PSScriptRoot\..\.."
$Project = Join-Path $Root "src\FluentSend\FluentSend.Desktop\FluentSend.Desktop.csproj"
$Stage  = Join-Path $OutDir "publish\$Rid"
$Zip    = Join-Path $OutDir "fluentsend-0.45.0-$Rid.zip"

Write-Host "[1/3] 发布 FluentSend 桌面应用 ($Rid, $Config)..."
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

Write-Host "[2/3] 复制运行时依赖..."
$assetsSrc = Join-Path $Root "src\FluentSend\FluentSend\Assets"
if (Test-Path $assetsSrc) {
    $assetsDst = Join-Path $Stage "Assets"
    New-Item -ItemType Directory -Force -Path $assetsDst | Out-Null
    Copy-Item -Path "$assetsSrc\*" -Destination $assetsDst -Recurse -Force
}
Copy-Item -Path (Join-Path $Root "LICENSE") -Destination $Stage -Force

Write-Host "[3/3] 打包 zip 便携包..."
if (Test-Path $Zip) { Remove-Item $Zip -Force }
Compress-Archive -Path "$Stage\*" -DestinationPath $Zip -CompressionLevel Optimal

Write-Host "完成：$Zip"
return $Zip
