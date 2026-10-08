# Script para criar certificado auto-assinado e rodar o instalador
$ErrorActionPreference = "Stop"

# 1. Verifica se já existe um certificado do GigaDock
$certName = "GigaDock OpenSource"
$cert = Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object { $_.Subject -match $certName } | Select-Object -First 1

if (-not $cert) {
    Write-Host "Criando novo certificado de assinatura de código: $certName..." -ForegroundColor Yellow
    # Cria o certificado
    $cert = New-SelfSignedCertificate -Subject "CN=$certName" -Type CodeSigningCert -CertStoreLocation "Cert:\CurrentUser\My" -KeyAlgorithm RSA -KeyLength 2048 -NotAfter (Get-Date).AddYears(5)
    
    # Adiciona à raiz confiável para que o Smart App Control e Defender confiem nele
    Write-Host "Adicionando certificado às Autoridades de Certificação Raiz Confiáveis (isso requer permissão de administrador)..." -ForegroundColor Yellow
    try {
        $store = [System.Security.Cryptography.X509Certificates.X509Store]::new('Root', 'CurrentUser')
        $store.Open('ReadWrite')
        $store.Add($cert)
        $store.Close()
        Write-Host "Certificado confiável com sucesso!" -ForegroundColor Green
    }
    catch {
        Write-Warning "Aviso: Nao foi possivel adicionar à raiz confiavel automaticamente. Você precisará instalar o certificado manualmente se o Smart App Control bloquear."
    }
} else {
    Write-Host "Certificado encontrado: $certName ($($cert.Thumbprint))" -ForegroundColor Green
}

# 2. Chama o script de build oficial passando o thumbprint
Write-Host "Iniciando compilação e assinatura..." -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'build_release.ps1') -Assinar -CertificadoThumbprint $cert.Thumbprint

Write-Host "Finalizado." -ForegroundColor Green
