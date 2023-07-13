@echo off
cls
dotnet tool restore
if "%~1"=="" (dotnet run --project ./build-script/build-script.fsproj) else (dotnet run --project ./build-script/build-script.fsproj -- -t %*)
