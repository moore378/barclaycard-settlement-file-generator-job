@echo off
ECHO %1
SET CONFIG=%1
if "%1" == "" SET CONFIG=Debug
REM NOTE: nuget command line must be installed manually since it is not part of VS command line tools.
nuget restore
msbuild TransactionManagement.sln /m /p:"Platform=Mixed Platforms" /p:Configuration=%CONFIG%
if %errorlevel% neq 0 exit /b %errorlevel%
