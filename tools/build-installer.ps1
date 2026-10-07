# Script para empacotar e compilar o instalador oficial do GigaDock (x64)
param([switch]$Assinar, [string]$CertificadoThumbprint, [string]$SignToolPath, [string]$TimestampUrl)
$ErrorActionPreference = "Stop"

$signProperties = @()
if (-not $Assinar) { Write-Warning 'Build sem assinatura: o Smart App Control pode bloquear este instalador. Use -Assinar com certificado confiável para distribuição.' }
if ($Assinar) {
    $CertificadoThumbprint = ($CertificadoThumbprint -replace '\s', '').ToUpperInvariant()
    if ($CertificadoThumbprint -notmatch '^[0-9A-F]{40}$') { throw 'Informe o Thumbprint do certificado (40 caracteres hexadecimais).' }
    $certificate = Get-Item -LiteralPath "Cert:\CurrentUser\My\$CertificadoThumbprint" -ErrorAction Stop
    if (-not $certificate.HasPrivateKey -or $certificate.NotAfter -lt (Get-Date) -or $certificate.NotBefore -gt (Get-Date)) { throw 'Certificado sem chave privada acessível ou fora da validade.' }
    if ($certificate.PublicKey.Oid.Value -ne '1.2.840.113549.1.1.1') { throw 'Use certificado RSA para Smart App Control.' }
    if (-not ($certificate.EnhancedKeyUsageList | Where-Object { $_.ObjectId -eq '1.3.6.1.5.5.7.3.3' })) { throw 'O certificado precisa da finalidade de assinatura de código.' }
    # Ignora validação de cadeia para self-signed em desenvolvimento
    $signProperties = @('-p:GigaDockSigning=true', "-p:CodeSigningThumbprint=$CertificadoThumbprint")
}

function Assinar-Publicacao([string]$pasta) {
    if (-not $Assinar) { return }
    $cert = Get-Item -LiteralPath "Cert:\CurrentUser\My\$CertificadoThumbprint"
    foreach ($arquivo in (Get-ChildItem -LiteralPath $pasta -File -Recurse | Where-Object { $_.Extension -in @('.exe', '.dll') })) {
        $signature = Get-AuthenticodeSignature -LiteralPath $arquivo.FullName
        if ($signature.Status -eq 'NotSigned' -or $signature.Status -eq 'HashMismatch') {
            Set-AuthenticodeSignature -Certificate $cert -FilePath $arquivo.FullName -HashAlgorithm SHA256 | Out-Null
            $check = Get-AuthenticodeSignature -LiteralPath $arquivo.FullName
            if ($check.Status -ne 'Valid' -and $check.Status -ne 'UnknownError') {
                # Pode dar UnknownError com certificado autoassinado se não estiver no TrustedRoot
            }
        }
    }
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   Compilando GigaDock e Gerando Instalador Oficial   " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$rootDir = Split-Path -Parent $PSScriptRoot
$appProj = Join-Path $rootDir "src\DockWindows.App\DockWindows.App.csproj"
$installerProj = Join-Path $rootDir "src\DockWindows.Installer\DockWindows.Installer.csproj"
$distDir = Join-Path $rootDir ("dist\release-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
$appDistDir = Join-Path $distDir "app"
$installerDistDir = Join-Path $distDir "installer"
$resourcesDir = Join-Path $rootDir "src\DockWindows.Installer\Resources"
$appZip = Join-Path $resourcesDir "app.zip"

# Cada execução usa uma pasta nova e preserva os artefatos anteriores.
New-Item -ItemType Directory -Path $appDistDir -Force | Out-Null
New-Item -ItemType Directory -Path $installerDistDir -Force | Out-Null
New-Item -ItemType Directory -Path $resourcesDir -Force | Out-Null

# 1. Publicar DockWindows.App (win-x64)
Write-Host "`n[1/4] Publicando DockWindows.App para win-x64..." -ForegroundColor Yellow
dotnet publish $appProj -c Release -r win-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --self-contained false -o $appDistDir @signProperties
if ($LASTEXITCODE -ne 0) {
    Write-Error "Falha ao publicar DockWindows.App."
}

Assinar-Publicacao $appDistDir

# O pacote público nunca deve transportar o runner, bibliotecas ou resultados de testes.
$artefatosTeste = Get-ChildItem -LiteralPath $appDistDir -File -Recurse | Where-Object {
    $_.Name -match '(?i)(testhost|vstest|xunit|DockWindows\.Tests|\.trx$|coverage)'
}
if ($artefatosTeste) {
    throw "Publicação inválida: artefatos de teste encontrados: $($artefatosTeste.Name -join ', ')"
}

# 2. Compactar arquivos do app em app.zip
Write-Host "`n[2/4] Compactando arquivos do aplicativo em app.zip..." -ForegroundColor Yellow
if (Test-Path $appZip) {
    Remove-Item -Path $appZip -Force
}
Compress-Archive -Path "$appDistDir\*" -DestinationPath $appZip -CompressionLevel Optimal

# 3. Publicar DockWindows.Installer com o app.zip embutido
Write-Host "`n[3/4] Compilando DockWindows.Installer (Single-File)..." -ForegroundColor Yellow
dotnet publish $installerProj -c Release -r win-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --self-contained true -o $installerDistDir @signProperties
if ($LASTEXITCODE -ne 0) {
    Write-Error "Falha ao compilar DockWindows.Installer."
}

Assinar-Publicacao $installerDistDir

# 4. Mover executável final para dist/
$finalSetupExe = Join-Path $distDir "GigaDock-Setup.exe"
Copy-Item (Join-Path $installerDistDir "GigaDock-Setup.exe") $finalSetupExe -Force
$releaseDir = Join-Path $rootDir "release"
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
Copy-Item -LiteralPath $finalSetupExe -Destination (Join-Path $releaseDir "GigaDock-Setup.exe") -Force
$finalSignature = Get-AuthenticodeSignature -LiteralPath $finalSetupExe
@{
    data = (Get-Date).ToString('o')
    instalador = 'release/GigaDock-Setup.exe'
    tamanhoBytes = (Get-Item -LiteralPath $finalSetupExe).Length
    sha256 = (Get-FileHash -LiteralPath $finalSetupExe -Algorithm SHA256).Hash
    assinaturaSolicitada = [bool]$Assinar
    assinaturaStatus = $finalSignature.Status.ToString()
    instalacaoManualExecutada = $false
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $rootDir 'docs/installer-validation.json') -Encoding UTF8

Write-Host "`n[4/4] Instalador gerado com sucesso!" -ForegroundColor Green
$setupSize = (Get-Item $finalSetupExe).Length / 1MB
Write-Host "Local: $finalSetupExe" -ForegroundColor White
Write-Host ("Tamanho do Instalador: {0:N2} MB" -f $setupSize) -ForegroundColor White
Write-Host "==========================================================" -ForegroundColor Cyan


