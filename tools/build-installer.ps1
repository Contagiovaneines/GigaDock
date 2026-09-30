# Script para empacotar e compilar o instalador oficial do Dock Windows (x64)
$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   Compilando Dock Windows e Gerando Instalador Oficial   " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$rootDir = Split-Path -Parent $PSScriptRoot
$appProj = Join-Path $rootDir "src\DockWindows.App\DockWindows.App.csproj"
$installerProj = Join-Path $rootDir "src\DockWindows.Installer\DockWindows.Installer.csproj"
$distDir = Join-Path $rootDir "dist"
$appDistDir = Join-Path $distDir "app"
$installerDistDir = Join-Path $distDir "installer"
$resourcesDir = Join-Path $rootDir "src\DockWindows.Installer\Resources"
$appZip = Join-Path $resourcesDir "app.zip"

# Limpar diretórios temporários de distribuição
if (Test-Path $distDir) {
    Remove-Item -Path $distDir -Recurse -Force
}
New-Item -ItemType Directory -Path $appDistDir -Force | Out-Null
New-Item -ItemType Directory -Path $installerDistDir -Force | Out-Null
New-Item -ItemType Directory -Path $resourcesDir -Force | Out-Null

# 1. Publicar DockWindows.App (win-x64)
Write-Host "`n[1/4] Publicando DockWindows.App para win-x64..." -ForegroundColor Yellow
dotnet publish $appProj -c Release -r win-x64 --self-contained false -o $appDistDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "Falha ao publicar DockWindows.App."
}

# 2. Compactar arquivos do app em app.zip
Write-Host "`n[2/4] Compactando arquivos do aplicativo em app.zip..." -ForegroundColor Yellow
if (Test-Path $appZip) {
    Remove-Item -Path $appZip -Force
}
Compress-Archive -Path "$appDistDir\*" -DestinationPath $appZip -CompressionLevel Optimal

# 3. Publicar DockWindows.Installer com o app.zip embutido
Write-Host "`n[3/4] Compilando DockWindows.Installer (Single-File)..." -ForegroundColor Yellow
dotnet publish $installerProj -c Release -r win-x64 -p:PublishSingleFile=true --self-contained false -o $installerDistDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "Falha ao compilar DockWindows.Installer."
}

# 4. Mover executável final para dist/
$finalSetupExe = Join-Path $distDir "DockWindows-Setup.exe"
Copy-Item (Join-Path $installerDistDir "DockWindows-Setup.exe") $finalSetupExe -Force

Write-Host "`n[4/4] Instalador gerado com sucesso!" -ForegroundColor Green
$setupSize = (Get-Item $finalSetupExe).Length / 1MB
Write-Host "Local: $finalSetupExe" -ForegroundColor White
Write-Host ("Tamanho do Instalador: {0:N2} MB" -f $setupSize) -ForegroundColor White
Write-Host "==========================================================" -ForegroundColor Cyan
