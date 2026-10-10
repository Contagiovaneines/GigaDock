param([Parameter(Mandatory=$true)][string]$OutputDirectory, [string]$CertificateThumbprint)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$sdkRoot=Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
$sdk=Get-ChildItem -LiteralPath $sdkRoot -Directory | Sort-Object Name -Descending | Where-Object { Test-Path (Join-Path $_.FullName 'x64\makeappx.exe') } | Select-Object -First 1
if (!$sdk) { throw 'Instale o Windows SDK para gerar a identidade das notificações.' }
$output=New-Item -ItemType Directory -Path $OutputDirectory -Force
$staging=Join-Path $output.FullName 'staging'
New-Item -ItemType Directory -Path (Join-Path $staging 'Assets') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'packaging\windows\Identity.AppxManifest.xml') -Destination (Join-Path $staging 'AppxManifest.xml') -Force
Add-Type -AssemblyName PresentationCore,WindowsBase
$source=[Windows.Media.Imaging.BitmapImage]::new([Uri](Join-Path $root 'assets\gigadock.png'))
foreach($asset in @(@('StoreLogo',50),@('Logo150',150),@('Logo44',44))) {
    $visual=[Windows.Media.DrawingVisual]::new(); $context=$visual.RenderOpen()
    $context.DrawImage($source,[Windows.Rect]::new(0,0,$asset[1],$asset[1])); $context.Close()
    $bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new($asset[1],$asset[1],96,96,[Windows.Media.PixelFormats]::Pbgra32); $bitmap.Render($visual)
    $encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new(); $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream=[IO.File]::Create((Join-Path $staging ('Assets\'+$asset[0]+'.png')))
    try { $encoder.Save($stream) } finally { $stream.Dispose() }
}
$package=Join-Path $output.FullName 'GigaDock.Identity.msix'
& (Join-Path $sdk.FullName 'x64\makeappx.exe') pack /o /nv /d $staging /p $package
if($LASTEXITCODE -ne 0) { throw 'Falha ao empacotar a identidade de notificações.' }
if($CertificateThumbprint) {
    $cert=Get-Item -LiteralPath "Cert:\CurrentUser\My\$CertificateThumbprint"
    [xml]$manifest=Get-Content -LiteralPath (Join-Path $staging 'AppxManifest.xml') -Raw
    if($cert.Subject -ne $manifest.Package.Identity.Publisher) { throw 'O Subject do certificado deve coincidir com Publisher dos dois manifestos.' }
    & (Join-Path $sdk.FullName 'x64\signtool.exe') sign /fd SHA256 /sha1 $CertificateThumbprint $package
    if($LASTEXITCODE -ne 0) { throw 'Falha ao assinar a identidade de notificações.' }
}
else { Write-Warning 'Identidade gerada sem assinatura: o Windows não poderá registrá-la até receber assinatura confiável.' }
Write-Host "Identidade: $package"
