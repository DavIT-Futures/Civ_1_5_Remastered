@echo off
rem Updates (git pull), builds, tests, publishes and starts Civ 1.5 Remastered for Windows x64.
rem Requires Git and the .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0
rem Usage: build.bat [nopull] [notest] [norun]
rem        nopull - do not download the latest changes first
rem        notest - skip the unit tests
rem        norun  - do not start the game after building

rem "git pull" may replace this very file while it runs, which confuses cmd.exe.
rem Therefore the script first copies itself to %TEMP% and continues from the copy.
if /i not "%~1"=="--from-temp" (
    copy /y "%~f0" "%TEMP%\civ_1_5_build.bat" >nul
    "%TEMP%\civ_1_5_build.bat" --from-temp "%~dp0" %*
    exit /b %errorlevel%
)

setlocal
cd /d "%~2"

set "PULL=1"
set "TEST=1"
set "RUN=1"

:parse
shift
if "%~2"=="" goto :parsed
if /i "%~2"=="nopull" set "PULL=0"
if /i "%~2"=="notest" set "TEST=0"
if /i "%~2"=="norun" set "RUN=0"
goto :parse
:parsed

if "%PULL%"=="1" (
    where git >nul 2>nul
    if errorlevel 1 (
        echo [WARNING] git was not found, skipping the update. Install Git: https://git-scm.com/download/win
    ) else (
        echo === Downloading latest changes ^(git pull^) ===
        git pull --ff-only
        if errorlevel 1 (
            echo.
            echo [WARNING] git pull failed ^(local changes or no network^).
            choice /c YN /m "Continue building the local version"
            if errorlevel 2 goto :fail
        )
    )
)

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] dotnet was not found. Install the .NET 10 SDK:
    echo         https://dotnet.microsoft.com/download/dotnet/10.0
    goto :fail
)

echo === Building solution ===
dotnet build Civ_1_5_Remastered.sln -c Release
if errorlevel 1 goto :fail

if "%TEST%"=="1" (
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

if "%RUN%"=="1" (
    start "" "publish\Civ_1_5_Remastered.Desktop.exe"
)

endlocal
pause
exit /b 0

:fail
echo.
echo [ERROR] Build stopped.
endlocal
pause
exit /b 1
