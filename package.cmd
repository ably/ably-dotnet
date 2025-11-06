@echo off
echo ======================================================
echo    Cake Build - Creating ably.io.*.nupkg
echo ======================================================
echo.
if "%1"=="" (
    echo Provide version number like: package.cmd 1.2.8
) else (
    dotnet tool restore
    dotnet cake cake-build/build.cake -- --target=Package --version=%1
)
