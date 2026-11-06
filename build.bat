@echo off
rem Builds Civ 1.5 Remastered for Windows x64 and starts the game.
rem Requires the .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0
rem Usage: build.bat          - build, run tests, start the game
rem        build.bat notest   - build and start without running tests
rem        build.bat norun    - build and run tests, do not start the game

setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] dotnet was not found. Install the .NET 10 SDK:
    echo         https://dotnet.microsoft.com/download/dotnet/10.0
    goto :fail
)

echo === Building solution ===
dotnet build Civ_1_5_Remastered.sln -c Release
if errorlevel 1 goto :fail

if /i not "%~1"=="notest" (
    echo === Running tests ===
    dotnet test Civ_1_5_Remastered.Tests\Civ_1_5_Remastered.Tests.csproj -c Release --no-build
    if errorlevel 1 goto :fail
)

echo === Publishing to publish\ ===
dotnet publish Civ_1_5_Remastered.Desktop\Civ_1_5_Remastered.Desktop.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
if errorlevel 1 goto :fail

echo.
echo Done: %cd%\publish\Civ_1_5_Remastered.Desktop.exe
echo The game looks for a folder named CIVILIZATION with the original DOS files
echo next to the exe or in any parent folder (e.g. %cd%\CIVILIZATION).

if /i not "%~1"=="norun" (
    start "" "publish\Civ_1_5_Remastered.Desktop.exe"
)

endlocal
pause
exit /b 0

:fail
echo.
echo [ERROR] Build failed.
endlocal
pause
exit /b 1
