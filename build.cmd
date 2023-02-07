@echo off
cls
dotnet tool restore
if "%~1"=="" (dotnet fake run build.fsx) else (dotnet fake run build.fsx -t %*)
