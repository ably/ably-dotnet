@echo off
cls
dotnet tool restore
REM Pass all arguments directly to Cake
dotnet cake cake-build/build.cake %*
