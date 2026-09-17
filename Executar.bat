@echo off
title WorkIT — Painel de Vagas TI
echo ============================================
echo   Iniciando WorkIT — Painel de Vagas TI
echo ============================================
echo.

cd /d "%~dp0"

if exist "bin\Release\net10.0-windows\WorkIT.App.exe" (
    echo Abrindo executavel Release...
    start "" "bin\Release\net10.0-windows\WorkIT.App.exe"
    exit
)

if exist "bin\Debug\net10.0-windows\WorkIT.App.exe" (
    echo Abrindo executavel Debug...
    start "" "bin\Debug\net10.0-windows\WorkIT.App.exe"
    exit
)

echo Executando via dotnet run...
dotnet run
