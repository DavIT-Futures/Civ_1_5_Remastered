@echo off
rem The quick way to the latest game: git pull, an incremental build of the game alone, start.
rem No tests, no publish, no single-file packing: that is build.bat. Requires Git and the .NET 10 SDK.
rem Usage: buildFast.bat [nopull] [norun]
rem        nopull - do not download the latest changes first
rem        norun  - do not start the game after building

rem "git pull" may replace this very file while it runs, which confuses cmd.exe.
rem Therefore the script first copies itself to %TEMP% and continues from the copy.
if /i not "%~1"=="--from-temp" (
    copy /y "%~f0" "%TEMP%\civ_1_5_build_fast.bat" >nul
    "%TEMP%\civ_1_5_build_fast.bat" --from-temp "%~dp0" %*
    exit /b %errorlevel%
)

setlocal
cd /d "%~2"

set "PULL=1"
set "RUN=1"

:parse
shift
if "%~2"=="" goto :parsed
if /i "%~2"=="nopull" set "PULL=0"
if /i "%~2"=="norun" set "RUN=0"
goto :parse
:parsed

set "PROJECT=Civ_1_5_Remastered.Desktop\Civ_1_5_Remastered.Desktop.csproj"
set "GAME=Civ_1_5_Remastered.Desktop\bin\Release\net10.0\Civ_1_5_Remastered.Desktop.exe"

if "%PULL%"=="1" (
    echo === git pull ===
    git pull --ff-only
    if errorlevel 1 echo [WARNING] git pull failed, building the local version.
)

rem Only the game and what it needs is built, without the code analyzers and without looking for packages
rem again. When the packages are not there yet (first build, or a new package), the second try restores them.
echo === Building ===
dotnet build "%PROJECT%" -c Release --no-restore -nologo -v q -clp:NoSummary -p:RunAnalyzers=false
if errorlevel 1 (
    echo === Building again, with restore ===
    dotnet build "%PROJECT%" -c Release -nologo -v q -clp:NoSummary -p:RunAnalyzers=false
    if errorlevel 1 goto :fail
)

if not exist "%GAME%" (
    echo [ERROR] The game was built, but not found at %GAME%
    goto :fail
)

echo Done: %cd%\%GAME%

if "%RUN%"=="1" start "" "%GAME%"

endlocal
exit /b 0

:fail
echo.
echo [ERROR] Build stopped. Is the game still running? Close it and try again.
endlocal
pause
exit /b 1
