#!/usr/bin/env pwsh
<#
.SYNOPSIS
构建 FluentSend Windows MSIX 包（商店就绪）。需要 Windows SDK 的 makepkg 工具。
.PARAMETER OutDir
输出目录，默认为 build 脚本上级的根。
.PARAMETER Pfx
代码签名证书（可选；如未提供则跳过签名，仅生成未签名 msix）。
.PARAMETER Password
证书密码（可选）。
#>
param(
    [string]$OutDir = (Resolve-Path "$PSScriptRoot\..\.."),
    [string]$Config = "Release",
    [string]$Rid    = "win-x64",
    [string]$Pfx    = "",
    [string]$Password = ""
)

$ErrorActionPreference = "Stop"
$Root  = Resolve-Path "$PSScriptRoot\..\.."
$ManifestSrc = Join-Path $PSScriptRoot "msix\AppxManifest.xml"
$Stage = Join-Path $OutDir "msix-stage"
$Msix  = Join-Path $OutDir "fluentsend-0.45.0-$Rid.msix"

Write-Host "[1/5] 清理并创建暂存目录..."
if (Test-Path $Stage) { Remove-Item $Stage -Recurse -Force }
New-Item -ItemType Directory -Force -Path $Stage | Out-Null
$assetsDir = Join-Path $Stage "Assets"
New-Item -ItemType Directory -Force -Path $assetsDir | Out-Null

Write-Host "[2/5] 发布桌面应用..."
$publishDir = Join-Path $OutDir "publish\$Rid"
dotnet publish (Join-Path $Root "src\FluentSend\FluentSend.Desktop\FluentSend.Desktop.csproj") `
    -c $Config -f net10.0 -r $Rid --self-contained true `
    -p:PublishSingleFile=$true `
    -p:IncludeNativeLibrariesForSelfExtract=$true `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "publish 失败" }

Write-Host "[3/5] 整理 MSIX 结构..."
Copy-Item -Path "$publishDir\*" -Destination $Stage -Recurse -Force
Copy-Item -Path $ManifestSrc -Destination $Stage -Force
Copy-Item -Path (Join-Path $Root "src\FluentSend\FluentSend\Assets\Icon.png") -Destination $assetsDir -Force

Write-Host "[4/5] 生成 msix (MakeAppx)..."
$makeAppx = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\makeappx.exe" `
    -ErrorAction SilentlyContinue | Sort-Object Name -Descending | Select-Object -First 1
if (-not $makeAppx) { throw "未找到 makeappx.exe，请安装 Windows SDK" }
& $makeAppx.FullName pack /d $Stage /p $Msix /v
if ($LASTEXITCODE -ne 0) { throw "MakeAppx pack 失败" }

Write-Host "[5/5] 签名（可选）..."
if ($Pfx -and (Test-Path $Pfx)) {
    $signtool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" `
        -ErrorAction SilentlyContinue | Sort-Object Name -Descending | Select-Object -First 1
    if (-not $signtool) { Write-Warning "未找到 signtool.exe，跳过签名"; return $Msix }
    $args = @("sign", "/fd", "SHA256", "/f", $Pfx, "/p", $Password, $Msix)
    & $signtool.FullName @args
    if ($LASTEXITCODE -ne 0) { throw "signtool 失败" }
} else {
    Write-Warning "未提供证书 (-Pfx)，输出未签名 msix，不能直接安装"
}

Write-Host "完成：$Msix"
return $Msix
