@echo off
echo ====================================================================================
echo    Cake Build - Creating ably.io.push.android.*.nupkg and ably.io.push.ios.*.nupkg
echo ====================================================================================
echo.
echo Warning: Run this script on macOS for iOS package support
echo.

if "%~1"=="" (
    echo Provide version number like: package-push.cmd 1.2.8
) else (
    dotnet tool restore
    dotnet cake cake-build/build.cake -- --target=PushPackage --version="%~1"
)
