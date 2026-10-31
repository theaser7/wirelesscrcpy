@echo off
setlocal

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" (
    echo Error: C# compiler csc.exe not found at %CSC%
    exit /b 1
)

if "%1"=="test" goto :run_tests
if "%1"=="clean" goto :clean

:build_app
if not exist "bin\Release" mkdir "bin\Release"
echo [BUILD] Compiling wirelesscrcpy.exe...
"%CSC%" /nologo /target:winexe /main:WirelessCrcpy.Program /out:bin\Release\wirelesscrcpy.exe /platform:anycpu ^
    /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll /r:System.IO.Compression.FileSystem.dll ^
    /recurse:src\*.cs
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Build failed!
    exit /b %ERRORLEVEL%
)
echo [SUCCESS] Built bin\Release\wirelesscrcpy.exe successfully.
exit /b 0

:run_tests
if not exist "bin\Test" mkdir "bin\Test"
echo [TEST] Compiling tests...
"%CSC%" /nologo /target:exe /main:WirelessCrcpy.Tests.TestRunner /out:bin\Test\Tests.exe /platform:anycpu ^
    /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll /r:System.IO.Compression.FileSystem.dll ^
    /recurse:src\*.cs /recurse:tests\*.cs
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Test compilation failed!
    exit /b %ERRORLEVEL%
)
echo [TEST] Running tests...
bin\Test\Tests.exe
exit /b %ERRORLEVEL%

:clean
if exist "bin" rd /s /q bin
echo Cleaned build artifacts.
exit /b 0
