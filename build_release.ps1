param(
    [switch]$Assinar,
    [string]$CertificadoThumbprint,
    [string]$SignToolPath,
    [string]$TimestampUrl,
    [string]$AppPublicadaPath,
    [switch]$ExigirAplicativoAssinado
)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'tools/build-installer.ps1') @PSBoundParameters
