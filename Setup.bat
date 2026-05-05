@echo off
setlocal

REM Creates a junction `_Torch` -> the Torch DedicatedServer install directory.
REM
REM Usage:
REM   Setup.bat "C:\path\to\Torch"
REM
REM Or, set the TORCH_PATH environment variable and run without arguments:
REM   set TORCH_PATH=C:\path\to\Torch
REM   Setup.bat

set "TORCH_DIR=%~1"
if "%TORCH_DIR%"=="" set "TORCH_DIR=%TORCH_PATH%"

if "%TORCH_DIR%"=="" (
    echo ERROR: No Torch path provided.
    echo.
    echo Usage:   Setup.bat "C:\path\to\Torch"
    echo Or set: set TORCH_PATH=C:\path\to\Torch  ^&^&  Setup.bat
    exit /b 1
)

if not exist "%TORCH_DIR%" (
    echo ERROR: Torch path "%TORCH_DIR%" does not exist.
    exit /b 1
)

REM Run the script from its own directory so the junction is created beside the .sln.
pushd "%~dp0"

if exist "_Torch" (
    echo Removing existing _Torch link...
    rmdir "_Torch" 2>nul
    if exist "_Torch" (
        echo ERROR: _Torch exists and could not be removed. Delete it manually.
        popd
        exit /b 1
    )
)

mklink /j "_Torch" "%TORCH_DIR%"
set "RC=%ERRORLEVEL%"

popd
exit /b %RC%
