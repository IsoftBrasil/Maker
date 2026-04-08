@echo off
setlocal enabledelayedexpansion

REM =============================================================
REM Maker Print Agent - Build/Publish helper
REM =============================================================

set "SCRIPT_DIR=%~dp0"
set "PROJECT_FILE=%SCRIPT_DIR%PrintAgent.csproj"
set "OUTPUT_DIR=%SCRIPT_DIR%bin\Release\net8.0-windows\win-x64\publish"

if not exist "%PROJECT_FILE%" (
  echo [ERRO] Nao encontrei o projeto:
  echo        "%PROJECT_FILE%"
  echo.
  echo Execute este .bat dentro da pasta print-agent original.
  pause
  exit /b 1
)

echo [1/4] Verificando .NET SDK...
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERRO] dotnet nao encontrado no PATH.
  echo Instale o .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
  pause
  exit /b 1
)

echo [2/4] Limpando build anterior (opcional)...
if exist "%OUTPUT_DIR%" (
  rmdir /s /q "%OUTPUT_DIR%"
)

echo [3/4] Publicando executavel (single-file, self-contained)...
dotnet publish "%PROJECT_FILE%" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
if errorlevel 1 (
  echo.
  echo [ERRO] Falha no publish.
  pause
  exit /b 1
)

echo [4/4] Concluido!
echo Saida: "%OUTPUT_DIR%"

echo.
choice /m "Deseja abrir a pasta de saida agora"
if errorlevel 2 goto :end
if errorlevel 1 start "" "%OUTPUT_DIR%"

:end
echo.
echo Pronto.
endlocal
exit /b 0
