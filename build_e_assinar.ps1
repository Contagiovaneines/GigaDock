# Usa um certificado existente; nao cria certificados nem altera a confianca do Windows.
param(
    [Parameter(Mandatory = $true)]
    [string]$CertificadoThumbprint
)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build_release.ps1') -Assinar -CertificadoThumbprint $CertificadoThumbprint
