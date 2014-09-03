
msbuild TransactionManagement.sln /m
if %errorlevel% neq 0 exit /b %errorlevel%
pushd RTCC\bin\Debug
make-deploy.py
if %errorlevel% neq 0 exit /b %errorlevel%
popd