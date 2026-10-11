@echo off
rem Builds a self-contained Release package for Windows x64 that runs without an installed .NET.
rem The result is the folder publish\win-x64 - zip it and send it.
rem
rem The local CIVILIZATION folder (the original game, in the repository root) is copied into the package, next to the
rem exe, so the recipient has nothing to copy. These are somebody else's files: use the package only privately and
rem never publish it. Saved games (*.SVE, *.MAP, *.HP) stay out; "publish.cmd withsaves" takes them too.
cd /d "%~dp0"

dotnet publish Civilization1Remaster\Civilization1Remaster.csproj -c Release -r win-x64 --self-contained true -o publish\win-x64
if errorlevel 1 exit /b 1

if exist publish\win-x64\CIVILIZATION rmdir /s /q publish\win-x64\CIVILIZATION

if not exist CIVILIZATION\ (
    echo.
    echo WARNING: the CIVILIZATION folder was not found in the repository root - the package has no game files.
    echo          The recipient has to copy their own into a CIVILIZATION folder next to the exe.
    goto done
)

if /i "%~1"=="withsaves" (
    robocopy CIVILIZATION publish\win-x64\CIVILIZATION /E /NFL /NDL /NJH /NJS /NP
) else (
    robocopy CIVILIZATION publish\win-x64\CIVILIZATION /E /XF *.SVE *.MAP *.HP /NFL /NDL /NJH /NJS /NP
)
rem robocopy exit codes below 8 mean success
if errorlevel 8 (
    echo ERROR: copying the CIVILIZATION folder failed.
    exit /b 1
)
echo Copied CIVILIZATION into the package.

:done
echo.
echo Done: %~dp0publish\win-x64
exit /b 0
