@echo off
setlocal

set "REPO_ROOT=%~dp0.."
pushd "%REPO_ROOT%" >nul || exit /b 1

if not exist "output\latest\index.html" (
    echo Missing output\latest\index.html.
    echo Run a scan before publishing: scripts\scan_steam_build.bat
    popd >nul
    exit /b 1
)

if exist "docs" rmdir /s /q "docs"
mkdir "docs"
xcopy "output\latest\*" "docs\" /E /I /Y >nul

if not exist "docs\.nojekyll" type nul > "docs\.nojekyll"

echo Copied output\latest to docs.
echo Commit docs/ to publish the site, diffs, snapshot, and history files.
echo Push main to deploy GitHub Pages.

popd >nul
endlocal
