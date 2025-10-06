@echo off
echo ======================================================
echo    Cake Build - Creating ably.io.*.nupkg            
echo ======================================================
echo.
if "%1"=="" (
    echo Provide version number like: package-cake.cmd 1.2.8
) else (
    call build-cake.cmd --target=Package --version=%1
)
