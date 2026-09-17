@echo off
chcp 65001 >nul
title WorkIT - Painel de Vagas TI
echo ============================================
echo   Iniciando WorkIT - Painel de Vagas TI
echo ============================================
echo.

cd /d "%~dp0"

if exist "bin\Release\net10.0-windows\WorkIT.App.exe" (
    echo Abrindo executavel Release...
    start "" "bin\Release\net10.0-windows\WorkIT.App.exe"
    exit /b 0
)

if exist "bin\Debug\net10.0-windows\WorkIT.App.exe" (
    echo Abrindo executavel Debug...
    start "" "bin\Debug\net10.0-windows\WorkIT.App.exe"
    exit /b 0
)

echo Compilando e iniciando via dotnet run...
dotnet run
