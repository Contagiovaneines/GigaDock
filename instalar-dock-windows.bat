@echo off
setlocal EnableExtensions
title Preparar desenvolvimento do Dock para Windows

echo ================================================
echo  Dock Windows - instalacao de ferramentas
echo ================================================
echo.

net session >nul 2>&1
if not %errorlevel%==0 (
  echo Solicitando permissao de administrador...
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs" >nul 2>&1
  if errorlevel 1 echo Falha ao solicitar administrador. Execute este BAT como administrador.
  exit /b
)

where winget >nul 2>&1
if errorlevel 1 (
  echo [ERRO] winget nao encontrado. Instale ou atualize o App Installer da Microsoft Store.
  echo https://apps.microsoft.com/detail/9nblggh4nns1
  goto :fim
)

echo [1/3] Verificando Git...
where git >nul 2>&1
if not errorlevel 1 (
  echo Git ja instalado.
) else (
  winget list --id Git.Git --exact --source winget 2>nul | findstr /C:"Git.Git" >nul
  if not errorlevel 1 (
    echo Git ja consta como instalado.
  ) else (
    winget install --id Git.Git --exact --source winget --accept-source-agreements --accept-package-agreements
    if errorlevel 1 echo [ERRO] Falha na instalacao do Git.
  )
)

echo.
echo [2/3] Verificando .NET SDK 10...
set "DOTNET10=0"
where dotnet >nul 2>&1
if not errorlevel 1 (
  dotnet --list-sdks 2>nul | findstr /R /B "10\." >nul
  if not errorlevel 1 set "DOTNET10=1"
)
if "%DOTNET10%"=="1" (
  echo .NET SDK 10 ja instalado.
) else (
  winget list --id Microsoft.DotNet.SDK.10 --exact --source winget 2>nul | findstr /C:"Microsoft.DotNet.SDK.10" >nul
  if not errorlevel 1 (
    echo .NET SDK 10 ja consta como instalado.
  ) else (
    winget install --id Microsoft.DotNet.SDK.10 --exact --source winget --accept-source-agreements --accept-package-agreements
    if errorlevel 1 echo [ERRO] Falha na instalacao do .NET SDK 10.
  )
)

echo.
echo [3/3] Verificando Visual Studio 2026 e desenvolvimento desktop .NET...
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist "%VSWHERE%" (
  "%VSWHERE%" -version "[18.0,19.0)" -products * -requires Microsoft.VisualStudio.Workload.ManagedDesktop -property installationPath | findstr /R "." >nul
  if not errorlevel 1 (
    echo Visual Studio 2026 com desenvolvimento desktop .NET ja instalado.
    goto :fim
  )
)

set "VSPATH="
if exist "%VSWHERE%" for /f "usebackq delims=" %%V in (`"%VSWHERE%" -version "[18.0,19.0)" -products * -property installationPath`) do if not defined VSPATH set "VSPATH=%%V"
if defined VSPATH (
  echo Visual Studio 2026 encontrado. Adicionando desenvolvimento desktop .NET...
  "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\setup.exe" modify --installPath "%VSPATH%" --add Microsoft.VisualStudio.Workload.ManagedDesktop --includeRecommended --passive --norestart
  if errorlevel 1 echo [ERRO] Falha ao adicionar a carga de trabalho. Abra o Visual Studio Installer e marque Desenvolvimento para desktop com .NET.
) else (
  echo Instalando Visual Studio Community 2026 com desenvolvimento desktop .NET...
  winget install --id Microsoft.VisualStudio.Community --exact --source winget --accept-source-agreements --accept-package-agreements --override "--passive --wait --norestart --add Microsoft.VisualStudio.Workload.ManagedDesktop --includeRecommended"
  if errorlevel 1 echo [ERRO] Falha no Visual Studio. Abra o Visual Studio Installer e marque Desenvolvimento para desktop com .NET.
)

:fim
echo.
echo Instalacao finalizada. Reinicie o Windows se solicitado.
echo Em um NOVO PowerShell, confira: dotnet --list-sdks
echo Confira tambem o Visual Studio Installer e o Git.
pause
endlocal
