SET BUILD_TYPE=Release
if "%1" == "Debug" set BUILD_TYPE=Debug

REM Determine whether we are on an 32 or 64 bit machine
if "%PROCESSOR_ARCHITECTURE%"=="x86" if "%PROCESSOR_ARCHITEW6432%"=="" goto x86

set ProgramFilesPath=%ProgramFiles(x86)%

goto startInstall

:x86

set ProgramFilesPath=%ProgramFiles(x86)%

:startInstall

pushd "%~dp0"

SET WIX_BUILD_LOCATION=%ProgramFilesPath%\WiX Toolset v3.10\bin
SET APP_INTERMEDIATE_PATH=..\obj\%BUILD_TYPE%
SET OUTPUTNAME=..\bin\%BUILD_TYPE%\myForecastSetup_1.0.1.msi

REM Cleanup leftover intermediate files

del /f /q "%APP_INTERMEDIATE_PATH%\*.wixobj"
del /f /q "%OUTPUTNAME%"

REM Build the MSI for the setup package

"%WIX_BUILD_LOCATION%\candle.exe" setup.wxs -dBuildType=%BUILD_TYPE% -out "%APP_INTERMEDIATE_PATH%\setup.wixobj"
"%WIX_BUILD_LOCATION%\light.exe" "%APP_INTERMEDIATE_PATH%\setup.wixobj" -cultures:en-US -ext "%WIX_BUILD_LOCATION%\WixUIExtension.dll" -ext "%WIX_BUILD_LOCATION%\WixUtilExtension.dll" -loc setup-en-us.wxl -out "%OUTPUTNAME%"

popd
