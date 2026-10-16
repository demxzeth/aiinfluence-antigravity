@echo off
REM ============================================================
REM  AIInfluence - Antigravity Build Script
REM ============================================================
setlocal
cd /d "%~dp0"

REM Toolchain priority: _toolchain portable SDK, C:\dotnet8, then PATH
set "TOOLCHAIN=%~dp0..\_toolchain\dotnet-sdk"
if not exist "%TOOLCHAIN%\dotnet.exe" (
    set "TOOLCHAIN=Z:\AIInfluenceClide\_toolchain\dotnet-sdk"
)

if exist "%TOOLCHAIN%\dotnet.exe" (
    set "DOTNET_ROOT=%TOOLCHAIN%"
    set "PATH=%TOOLCHAIN%;%PATH%"
) else if exist "C:\dotnet8\sdk" (
    set "DOTNET_ROOT=C:\dotnet8"
    set "PATH=C:\dotnet8;%PATH%"
)

where dotnet >NUL 2>&1
if errorlevel 1 (
    echo [Antigravity] ERROR: .NET SDK not found.
    exit /b 1
)

echo [Antigravity] Building Release...
dotnet build "src\AIInfluenceAntigravity.csproj" -c Release --nologo
if errorlevel 1 (
    echo [Antigravity] BUILD FAILED
    exit /b 1
)

if "%1"=="-SkipTests" goto :done
if not exist "tests\AIInfluenceAntigravity.Tests.csproj" goto :done
echo [Antigravity] Running test suite...
dotnet test "tests\AIInfluenceAntigravity.Tests.csproj" -c Release --nologo -v q
if errorlevel 1 (
    echo [Antigravity] TESTS FAILED
    exit /b 1
)

:done
echo.
echo [Antigravity] Build OK: bin\Win64_Shipping_Client\AIInfluenceAntigravity.dll
endlocal
