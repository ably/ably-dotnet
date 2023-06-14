@echo off
echo ======================================================
echo        Script for building ably.io.*.nupkg
echo ======================================================
if "%~1"=="" (echo "Provide version number like package.cmd 1.1.15") else (build.cmd Package -v %*)
