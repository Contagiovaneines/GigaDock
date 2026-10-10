# Script para empacotar e compilar o instalador oficial do GigaDock (x64)
param(
    [switch]$Assinar,
    [string]$CertificadoThumbprint,
    [string]$SignToolPath,
    [string]$TimestampUrl,
    [string]$AppPublicadaPath,
    [switch]$ExigirAplicativoAssinado,
    [string]$RelatorioValidacao = 'docs/installer-validation.json'
)
$ErrorActionPreference = "Stop"

$signProperties = @()
if (-not $Assinar) { Write-Warning 'BUILD SOMENTE PARA DESENVOLVIMENTO: o aplicativo e o instalador não serão assinados e podem ser bloqueados pelo Smart App Control. Para distribuição, use -Assinar com certificado confiável.' }
if ($Assinar) {
    $CertificadoThumbprint = ($CertificadoThumbprint -replace '\s', '').ToUpperInvariant()
    if ($CertificadoThumbprint -notmatch '^[0-9A-F]{40}$') { throw 'Informe o Thumbprint do certificado (40 caracteres hexadecimais).' }
    $certificate = Get-Item -LiteralPath "Cert:\CurrentUser\My\$CertificadoThumbprint" -ErrorAction Stop
    if ($certificate.Subject -eq $certificate.Issuer) { throw 'Certificado autoassinado nao atende a confianca publica exigida pelo Smart App Control. Use um provedor confiavel.' }
    if (-not $certificate.HasPrivateKey -or $certificate.NotAfter -lt (Get-Date) -or $certificate.NotBefore -gt (Get-Date)) { throw 'Certificado sem chave privada acessível ou fora da validade.' }
    if ($certificate.PublicKey.Oid.Value -ne '1.2.840.113549.1.1.1') { throw 'Use certificado RSA para Smart App Control.' }
    if (-not ($certificate.EnhancedKeyUsageList | Where-Object { $_.ObjectId -eq '1.3.6.1.5.5.7.3.3' })) { throw 'O certificado precisa da finalidade de assinatura de código.' }
    $signProperties = @(
        '-p:GigaDockSigning=true',
        "-p:CodeSigningThumbprint=$CertificadoThumbprint"
    )
}

function Assinar-Publicacao([string]$pasta) {
    if (-not $Assinar) { return }
    $cert = Get-Item -LiteralPath "Cert:\CurrentUser\My\$CertificadoThumbprint"
    foreach ($arquivo in (Get-ChildItem -LiteralPath $pasta -File -Recurse | Where-Object { $_.Extension -in @('.exe', '.dll') })) {
        $signature = Get-AuthenticodeSignature -LiteralPath $arquivo.FullName
        if ($signature.Status -eq 'NotSigned' -or $signature.Status -eq 'HashMismatch') {
            Set-AuthenticodeSignature -Certificate $cert -FilePath $arquivo.FullName -HashAlgorithm SHA256 | Out-Null
            $signature = Get-AuthenticodeSignature -LiteralPath $arquivo.FullName
        }
        if ($signature.Status -ne 'Valid') { throw "Assinatura invalida em $($arquivo.Name): $($signature.Status)." }
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

# 1. Publicar DockWindows.App (win-x64) ou usar a publicação assinada pelo CI.
if ([string]::IsNullOrWhiteSpace($AppPublicadaPath)) {
    Write-Host "`n[1/4] Publicando DockWindows.App para win-x64..." -ForegroundColor Yellow
    dotnet publish $appProj -c Release -r win-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --self-contained false -o $appDistDir @signProperties
    if ($LASTEXITCODE -ne 0) { Write-Error "Falha ao publicar DockWindows.App." }
    Assinar-Publicacao $appDistDir
}
else {
    $appPublicadaResolvida = (Resolve-Path -LiteralPath $AppPublicadaPath -ErrorAction Stop).Path
    $exeAssinado = Join-Path $appPublicadaResolvida 'DockWindows.App.exe'
    if (-not (Test-Path -LiteralPath $exeAssinado -PathType Leaf)) { throw 'A publicação informada não contém DockWindows.App.exe.' }
    if ($ExigirAplicativoAssinado) {
        $assinaturaApp = Get-AuthenticodeSignature -LiteralPath $exeAssinado
        if ($assinaturaApp.Status -ne 'Valid') { throw "DockWindows.App.exe não possui assinatura Authenticode válida: $($assinaturaApp.Status)." }
    }
    Copy-Item -Path (Join-Path $appPublicadaResolvida '*') -Destination $appDistDir -Recurse -Force
    Write-Host "`n[1/4] Usando publicação validada de DockWindows.App." -ForegroundColor Yellow
}

# O pacote público nunca deve transportar o runner, bibliotecas ou resultados de testes.
$artefatosTeste = Get-ChildItem -LiteralPath $appDistDir -File -Recurse | Where-Object {
    $_.Name -match '(?i)(testhost|vstest|xunit|DockWindows\.Tests|GigaDock\.Tests|\.trx$|coverage)'
}
if ($artefatosTeste) {
    throw "Publicação inválida: artefatos de teste encontrados: $($artefatosTeste.Name -join ', ')"
}

# Identidade externa opcional: preserva o instalador existente e declara o listener.
$identityDir = Join-Path $distDir 'identity'
$identityArgs = @{ OutputDirectory = $identityDir }
if ($Assinar) { $identityArgs.CertificateThumbprint = $CertificadoThumbprint }
& (Join-Path $PSScriptRoot 'build-notification-identity.ps1') @identityArgs
New-Item -ItemType Directory -Path (Join-Path $appDistDir 'identity') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $identityDir 'GigaDock.Identity.msix') -Destination (Join-Path $appDistDir 'identity\GigaDock.Identity.msix') -Force

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
$finalSignature = Get-AuthenticodeSignature -LiteralPath $finalSetupExe
if ($Assinar -and $finalSignature.Status -ne 'Valid') {
    throw "O instalador final não possui assinatura Authenticode válida: $($finalSignature.Status)."
}
Copy-Item -LiteralPath $finalSetupExe -Destination (Join-Path $releaseDir "GigaDock-Setup.exe") -Force
@{
    data = (Get-Date).ToString('o')
    instalador = 'release/GigaDock-Setup.exe'
    tamanhoBytes = (Get-Item -LiteralPath $finalSetupExe).Length
    sha256 = (Get-FileHash -LiteralPath $finalSetupExe -Algorithm SHA256).Hash
    assinaturaSolicitada = [bool]$Assinar
    assinaturaStatus = $finalSignature.Status.ToString()
    instalacaoManualExecutada = $false
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $rootDir $RelatorioValidacao) -Encoding UTF8

Write-Host "`n[4/4] Instalador gerado com sucesso!" -ForegroundColor Green
$setupSize = (Get-Item $finalSetupExe).Length / 1MB
Write-Host "Local: $finalSetupExe" -ForegroundColor White
Write-Host ("Tamanho do Instalador: {0:N2} MB" -f $setupSize) -ForegroundColor White
Write-Host "==========================================================" -ForegroundColor Cyan


