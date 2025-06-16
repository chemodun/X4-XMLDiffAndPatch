@echo off
setlocal

REM Check if the required parameters are provided
if "%~1"=="" (
    echo Error: Full path to the modified file is mandatory.
    exit /b 1
)
if "%~2"=="" (
    echo Error: Full path to XMLDiff.exe utility is mandatory.
    exit /b 1
)

set "ModifiedFilePath=%~1"
set "XMLDiffPath=%~2"
set "OriginalFilesPath=%~3"

REM Extract the type and filename from the modified file path
for %%I in ("%ModifiedFilePath%") do set "ModifiedFileName=%%~nxI"
for %%I in ("%ModifiedFilePath%") do set "ModifiedDirectory=%%~dpI"
for %%I in ("%ModifiedDirectory:~0,-1%") do set "Type=%%~nxI"
set "Type=%Type:.modified=%"
set "TargetFilePath=%ModifiedFilePath:.modified=%"

REM Determine the original file path
if not "%OriginalFilesPath%"=="" (
    set "OriginalFilePathLocal=%ModifiedFilePath:.modified=.original%"
    if not exist "%OriginalFilePathLocal%" (
        set "OriginalFilePath=%OriginalFilesPath%\%Type%\%ModifiedFileName%"
    ) else (
        set "OriginalFilePath=%OriginalFilePathLocal"
    )
) else (
    set "OriginalFilePath=%ModifiedFilePath:.modified=.original%"
)

REM Check existence of XMLDiff.exe
if not exist "%XMLDiffPath%" (
    echo Error: XMLDiff.exe not found at path: %XMLDiffPath%
    exit /b 1
)

REM Check existence of modified file
if not exist "%ModifiedFilePath%" (
    echo Error: Modified file not found at path: %ModifiedFilePath%
    exit /b 1
)

REM Check existence of original file
if not exist "%OriginalFilePath%" (
    echo Error: Original file not found at path: %OriginalFilePath%
    exit /b 1
)

REM Check existence of target file directory
for %%I in ("%TargetFilePath%") do set "TargetDirectory=%%~dpI"
if not exist "%TargetDirectory%" (
    echo Error: Target directory not found at path: %TargetDirectory%
    exit /b 1
)

REM Run XMLDiff.exe
"%XMLDiffPath%" -o "%OriginalFilePath%" -m "%ModifiedFilePath%" -d "%TargetFilePath%"
if errorlevel 1 (
    echo Error: XMLDiff.exe failed with exit code: %errorlevel%
    exit /b 1
)

echo XMLDiff completed successfully. Output file: %TargetFilePath%
endlocal