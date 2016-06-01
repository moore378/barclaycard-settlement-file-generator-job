
msbuild TransactionManagement.sln /m /p:"Platform=Mixed Platforms" /p:Configuration=Debug
if %errorlevel% neq 0 exit /b %errorlevel%
pushd CCTM\bin\Debug
make-deploy.py
if %errorlevel% neq 0 exit /b %errorlevel%
popd