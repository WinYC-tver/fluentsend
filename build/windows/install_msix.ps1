# 给 FluentSend MSIX 签名并准备安装
$ErrorActionPreference = "Stop"

$msix = "E:\FluentSend\fluentsend-0.45.0-win-x64.msix"
$pfxPath = "E:\FluentSend\build\windows\fluentsend_dev.pfx"
$cerPath = "E:\FluentSend\build\windows\fluentsend_dev.cer"
$pwd = "FluentSend2026!"
$securePwd = ConvertTo-SecureString -String $pwd -Force -AsPlainText

# 证书 Subject 必须与 AppxManifest.xml 的 Publisher 一致
$subject = "CN=HZStudio, O=HZStudio, L=Beijing, S=Beijing, C=CN"

Write-Host "[1/4] 创建自签名代码签名证书..."
if (Test-Path $pfxPath) { Remove-Item $pfxPath -Force }
if (Test-Path $cerPath) { Remove-Item $cerPath -Force }

$cert = New-SelfSignedCertificate `
    -Subject $subject `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyAlgorithm RSA -KeyLength 2048 `
    -KeyUsage DigitalSignature `
    -Type CodeSigningCert `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")

Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $securePwd | Out-Null
Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null
Write-Host "  证书指纹: $($cert.Thumbprint)"
Write-Host "  PFX: $pfxPath"
Write-Host "  CER: $cerPath"

Write-Host "[2/4] 用 signtool 签名 MSIX..."
$signtool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" `
    -ErrorAction SilentlyContinue | Sort-Object Name -Descending | Select-Object -First 1
if (-not $signtool) { throw "未找到 signtool.exe" }
& $signtool.FullName sign /fd SHA256 /f $pfxPath /p $pwd $msix 2>&1 | Select-Object -Last 5
if ($LASTEXITCODE -ne 0) { throw "signtool 签名失败" }
Write-Host "  MSIX 签名完成"

Write-Host "[3/4] 导入证书到 CurrentUser\TrustedPeople（仅当前用户信任）..."
$imported = Import-Certificate -FilePath $cerPath -CertStoreLocation "Cert:\CurrentUser\TrustedPeople" -ErrorAction SilentlyContinue
if ($imported) {
    Write-Host "  证书已导入到 CurrentUser\TrustedPeople"
} else {
    Write-Warning "导入 TrustedPeople 失败，尝试 CurrentUser\Root"
    Import-Certificate -FilePath $cerPath -CertStoreLocation "Cert:\CurrentUser\Root" | Out-Null
}

Write-Host "[4/4] 尝试安装 MSIX..."
try {
    Add-AppxPackage -Path $msix -ErrorAction Stop
    Write-Host "✓ FluentSend MSIX 安装成功！"
    Write-Host "  现在可以从开始菜单启动 FluentSend"
} catch {
    Write-Host ""
    Write-Host "✗ MSIX 安装失败：$($_.Exception.Message)"
    Write-Host ""
    Write-Host "如果错误是 0x80073CF0 或证书信任问题，请用【管理员 PowerShell】执行："
    Write-Host "  Import-Certificate -FilePath `"$cerPath`" -CertStoreLocation Cert:\LocalMachine\Root"
    Write-Host "  Add-AppxPackage -Path `"$msix`""
    Write-Host ""
    Write-Host "或直接用便携包（无需安装）："
    Write-Host "  解压 E:\FluentSend\fluentsend-0.45.0-win-x64.zip 后运行 FluentSend.Desktop.exe"
}
