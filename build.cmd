@echo off
cls
if "%~1"=="" (fake -v run build.fsx) else (fake -v run build.fsx -t %*)