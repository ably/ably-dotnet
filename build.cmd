@echo off
cls
if "%~1"=="" (fake run build.fsx) else (fake run build.fsx -t %*)