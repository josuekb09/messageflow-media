@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..\..") do set "REPO_ROOT=%%~fI"

set "SOLUTION=%REPO_ROOT%\MessageFlow.sln"
set "APP_PROJECT=%REPO_ROOT%\src\MessageFlow.App\MessageFlow.App.csproj"
set "SOURCE_DB=%REPO_ROOT%\database\messageflow.db"
set "RELEASE_DIR=D:\MessageFlow Release\MessageFlow"
set "RELEASE_DB_DIR=%RELEASE_DIR%\database"
set "RELEASE_DB=%RELEASE_DB_DIR%\messageflow.db"
set "README=%RELEASE_DIR%\README_CHURCH_INSTALL.txt"

echo MessageFlow church release build
echo Repository: %REPO_ROOT%
echo Release:    %RELEASE_DIR%
echo.

if not exist "%SOLUTION%" (
    echo ERROR: MessageFlow.sln was not found.
    exit /b 1
)

if not exist "%APP_PROJECT%" (
    echo ERROR: MessageFlow.App project was not found.
    exit /b 1
)

if not exist "%SOURCE_DB%" (
    echo ERROR: Production database was not found:
    echo %SOURCE_DB%
    exit /b 1
)

if exist "%SOURCE_DB%-wal" (
    for %%I in ("%SOURCE_DB%-wal") do (
        if not "%%~zI"=="0" (
            echo ERROR: The production database has a non-empty WAL file.
            echo Close MessageFlow and rerun this script so the copied database is complete.
            echo WAL: %SOURCE_DB%-wal
            exit /b 1
        )
    )
)

echo Restoring Windows x64 publish assets...
dotnet restore "%APP_PROJECT%" -r win-x64
if errorlevel 1 exit /b 1

echo.
echo Building MessageFlow.App in Release mode...
dotnet build "%APP_PROJECT%" -c Release -r win-x64 --self-contained true --no-restore
if errorlevel 1 exit /b 1

echo.
echo Publishing self-contained Windows x64 release...
dotnet publish "%APP_PROJECT%" -c Release -r win-x64 --self-contained true --no-restore -o "%RELEASE_DIR%" -p:DebugType=None -p:DebugSymbols=false
if errorlevel 1 exit /b 1

echo.
echo Copying production database...
if not exist "%RELEASE_DB_DIR%" mkdir "%RELEASE_DB_DIR%"
copy /Y "%SOURCE_DB%" "%RELEASE_DB%" > nul
if errorlevel 1 exit /b 1

echo.
echo Writing church install README...
> "%README%" echo MessageFlow Media Church Install
>> "%README%" echo ================================
>> "%README%" echo.
>> "%README%" echo Free church use notice:
>> "%README%" echo MessageFlow Media is distributed free of charge for church use.
>> "%README%" echo Not for sale. Do not sell this software or bundled content.
>> "%README%" echo Do not add paid subscriptions, ads, in-app purchases, or fundraising gates.
>> "%README%" echo.
>> "%README%" echo Install:
>> "%README%" echo 1. Run MessageFlowMediaSetup.exe.
>> "%README%" echo 2. Follow the installer prompts.
>> "%README%" echo 3. Launch MessageFlow Media from the desktop shortcut or Start menu.
>> "%README%" echo.
>> "%README%" echo Manual folder install, if needed:
>> "%README%" echo 1. Copy this whole MessageFlow folder to the church computer.
>> "%README%" echo 2. Run MessageFlow.App.exe.
>> "%README%" echo 3. Do not delete the database folder.
>> "%README%" echo.
>> "%README%" echo Connect a TV or projector:
>> "%README%" echo 1. Connect HDMI to the TV or projector.
>> "%README%" echo 2. Press Windows + P and choose Extend.
>> "%README%" echo 3. Start MessageFlow Media.
>> "%README%" echo 4. Before service, open Admin ^> Test Projection Display.
>> "%README%" echo 5. The TV/projector must show only projection text.
>> "%README%" echo 6. The laptop/operator screen should show the MessageFlow controls.
>> "%README%" echo.
>> "%README%" echo Basic troubleshooting:
>> "%README%" echo - If projection appears on the wrong screen, check Windows Display Settings and keep Windows + P set to Extend.
>> "%README%" echo - If only one screen is connected, use the windowed projection preview for testing.
>> "%README%" echo - If content is missing, confirm the database folder is still beside MessageFlow.App.exe.
>> "%README%" echo - If Windows shows a security warning, confirm the installer came from the official GitHub Release before running it.
>> "%README%" echo.
>> "%README%" echo Release structure:
>> "%README%" echo MessageFlow\
>> "%README%" echo   MessageFlow.App.exe
>> "%README%" echo   database\
>> "%README%" echo     messageflow.db

echo.
echo Copying public notice files...
if exist "%REPO_ROOT%\NOTICE.md" (
    copy /Y "%REPO_ROOT%\NOTICE.md" "%RELEASE_DIR%\NOTICE.md" > nul
    if errorlevel 1 exit /b 1
)
if exist "%REPO_ROOT%\docs\PERMISSION_AND_CONTENT_NOTICE.md" (
    copy /Y "%REPO_ROOT%\docs\PERMISSION_AND_CONTENT_NOTICE.md" "%RELEASE_DIR%\PERMISSION_AND_CONTENT_NOTICE.md" > nul
    if errorlevel 1 exit /b 1
)

if exist "%RELEASE_DIR%\The Table.lnk" (
    echo ERROR: The Table.lnk is present in the release folder. Remove it before distribution.
    exit /b 1
)

echo.
echo Church release created successfully:
echo %RELEASE_DIR%
echo.
echo Run:
echo "%RELEASE_DIR%\MessageFlow.App.exe"

endlocal
