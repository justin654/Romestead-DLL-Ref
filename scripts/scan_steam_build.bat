@echo off
setlocal
set "EXITCODE=0"

pushd "%~dp0.." >nul

if not defined GAME_ROOT set "GAME_ROOT=C:\Program Files (x86)\Steam\steamapps\common\romestead"

set "BASELINE="
for /f "usebackq delims=" %%I in (`powershell -NoProfile -Command "$f = Get-ChildItem '.\output\latest\history' -Filter '*-basegame.json' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName; if ($f) { $f }"`) do set "BASELINE=%%I"

dotnet build ".\src\RomesteadRef\RomesteadRef.csproj" -c Release
if errorlevel 1 (
    set "EXITCODE=%ERRORLEVEL%"
    goto :end
)

if defined BASELINE (
    ".\src\RomesteadRef\bin\Release\net8.0\romestead-ref.exe" scan --root "%GAME_ROOT%" --assembly-exact Romestead --assembly-exact Shared --assembly-exact CandideServer --assembly-exact CandideCreator.Shared --baseline "%BASELINE%" %*
) else (
    ".\src\RomesteadRef\bin\Release\net8.0\romestead-ref.exe" scan --root "%GAME_ROOT%" --assembly-exact Romestead --assembly-exact Shared --assembly-exact CandideServer --assembly-exact CandideCreator.Shared %*
)
if errorlevel 1 (
    set "EXITCODE=%ERRORLEVEL%"
    goto :end
)

echo.
if exist "%CD%\output\latest\diff.html" (
    echo Diff report:
    echo %CD%\output\latest\diff.html
) else (
    echo Diff report:
    echo not written because no baseline snapshot was selected
)
echo Catalog:
echo %CD%\output\latest\index.html

:end
popd >nul
exit /b %EXITCODE%
