REM First build all of the binaries.
REM NOTE: This will also zip up RTCC

@echo off
ECHO %1
SET CONFIG=%1
if "%1" == "" SET CONFIG=Debug

call deploy-rtcc.bat %CONFIG%


REM Zip up CCTM
pushd CCTM\bin\%CONFIG%
..\..\..\scripts\make-deploy.py
if %errorlevel% neq 0 exit /b %errorlevel%
popd

REM Zip up FIS PayDirect Processor
pushd FisPayDirectProcessor\bin\%CONFIG%
..\..\..\scripts\make-deploy.py
if %errorlevel% neq 0 exit /b %errorlevel%
popd

REM Extract all the files to the Dev environment.
call extract-rtcc.bat
call extract-cctm.bat
call extract-processor-fis.bat
