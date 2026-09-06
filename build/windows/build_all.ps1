#!/usr/bin/env pwsh
<#
.SYNOPSIS
FluentSend Windows 一键构建：便携包 + MSIX
.DESCRIPTION
依次构建 Rust cdylib、.NET 桌面应用，生成便携 zip 与 MSIX。
.PARAMETER Pfx
代码签名证书路径（可选）。提供则对 MSIX 签名。
.PARAMETER Password
证书密码（可选）。
.EXAMPLE
./build_all.ps1
.EXAMPLE
./build_all.ps1 -Pfx C:\certs\fluentsend.pfx -Password ********
#>
param(
    [string]$Pfx = "",
    [string]$Password = ""
)

$ErrorActionPreference = "Stop"
$Portable = & (Join-Path $PSScriptRoot "build_portable.ps1")
$Msix    = & (Join-Path $PSScriptRoot "build_msix.ps1") -Pfx $Pfx -Password $Password

Write-Host ""
Write-Host "=========================================="
Write-Host " FluentSend Windows 构建完成"
Write-Host "=========================================="
Write-Host " 便携包: $Portable"
Write-Host " MSIX:    $Msix"
Write-Host "=========================================="

return @{ Portable = $Portable; Msix = $Msix }
