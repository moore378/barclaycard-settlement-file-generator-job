@echo off
ECHO %1
SET CONFIG=%1
if "%1" == "" SET CONFIG=Debug
call deploy-build.bat %CONFIG%
if %errorlevel% neq 0 exit /b %errorlevel%
pushd FisPayDirectProcessor\bin\%CONFIG%
..\..\..\scripts\make-deploy.py
if %errorlevel% neq 0 exit /b %errorlevel%
popd