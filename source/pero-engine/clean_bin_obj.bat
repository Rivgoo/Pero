@echo off
:: Set the current directory to the folder where this script is located
cd /d "%~dp0"

echo Searching for and deleting 'bin' and 'obj' folders...

:: Loop recursively through all folders looking for bin and obj
for /d /r . %%d in (bin,obj) do (
    if exist "%%d" (
        echo Deleting: "%%d"
        rd /s /q "%%d"
    )
)

echo.
echo Done! All 'bin' and 'obj' folders have been deleted.
pause