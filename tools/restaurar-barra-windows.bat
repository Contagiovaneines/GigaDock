@echo off
title Restaurar Barra de Tarefas do Windows
echo =======================================================
echo  Dock Windows - Restauracao da Barra de Tarefas Nativa
echo =======================================================
echo.
echo Executando script de restauracao do Windows...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0restaurar-barra-windows.ps1"
echo.
echo Operacao concluida. Pressione qualquer tecla para sair.
pause >nul
