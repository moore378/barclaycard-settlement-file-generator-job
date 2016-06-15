@echo off
ECHO %1
SET CONFIG=%1
if "%1" == "" SET CONFIG=Debug
msbuild TransactionManagement.sln /m /p:"Platform=Mixed Platforms" /p:Configuration=%CONFIG%
if %errorlevel% neq 0 exit /b %errorlevel%
pushd CCTM\bin\%CONFIG%
..\..\..\scripts\make-deploy.py
if %errorlevel% neq 0 exit /b %errorlevel%
popd