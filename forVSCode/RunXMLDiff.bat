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
if "%~3"=="" (
    echo Error: Full path to the workspace is mandatory.
    exit /b 1
)

set "ModifiedFilePath=%~1"
set "XMLDiffPath=%~2"
set "WorkspacePath=%~3"
set "OriginalFilesPath=%~4"

REM Normalize paths (convert forward slashes to backslashes and remove trailing slashes)
set "ModifiedFilePath=%ModifiedFilePath:/=\%"
set "WorkspacePath=%WorkspacePath:/=\%"
if "%WorkspacePath:~-1%"=="\" set "WorkspacePath=%WorkspacePath:~0,-1%"

REM Validate that the modified file path is inside the workspace
setlocal EnableDelayedExpansion
set "TempModified=!ModifiedFilePath:%WorkspacePath%=!"
if "!TempModified!"=="!ModifiedFilePath!" (
    endlocal
    echo Error: Modified file path must be inside the workspace.
    echo Workspace: %WorkspacePath%
    echo File path: %ModifiedFilePath%
    exit /b 1
)
endlocal

REM Extract the filename from the modified file path
for %%I in ("%ModifiedFilePath%") do set "ModifiedFileName=%%~nxI"

REM Calculate the relative path from workspace to the modified file
call :GetRelativePath "%WorkspacePath%" "%ModifiedFilePath%" RelativePath

REM Extract the type and subdirectories from the relative path
REM The type is the parent folder of the .modified folder
REM RelativePath format: subdir1\subdir2\...\type.modified\filename.xml
for %%I in ("%RelativePath%") do set "ModifiedFileName=%%~nxI"
for %%I in ("%RelativePath%") do set "RelativeDir=%%~dpI"
set "RelativeDir=%RelativeDir:~0,-1%"
for %%I in ("%RelativeDir%") do set "Type=%%~nxI"
set "Type=%Type:.modified=%"

REM Remove .modified from the relative directory path
set "RelativeDirNoModified=%RelativeDir:.modified=%"

REM Construct target file path (diff version)
set "TargetFilePath=%WorkspacePath%\%RelativeDirNoModified%.diff\%ModifiedFileName%"

REM Construct local original file path
set "OriginalFilePathLocal=%WorkspacePath%\%RelativeDirNoModified%.original\%ModifiedFileName%"

REM Determine the original file path
if not "%OriginalFilesPath%"=="" (
    if not exist "%OriginalFilePathLocal%" (
        REM When using external original files path, preserve the relative structure
        set "OriginalFilePath=%OriginalFilesPath%\%RelativeDirNoModified%\%ModifiedFileName%"
    ) else (
        set "OriginalFilePath=%OriginalFilePathLocal%"
    )
) else (
    set "OriginalFilePath=%OriginalFilePathLocal%"
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
exit /b 0

:GetRelativePath
REM Function to calculate relative path from base to target
REM %1 = base path (workspace)
REM %2 = target path (modified file)
REM %3 = output variable name
setlocal EnableDelayedExpansion
set "base=%~1"
set "target=%~2"

REM Ensure paths end without backslash
if "%base:~-1%"=="\" set "base=%base:~0,-1%"
if "%target:~-1%"=="\" set "target=%target:~0,-1%"

REM Check if target starts with base
set "temp=!target:%base%=!"
if "!temp!"=="!target!" (
    REM Target doesn't start with base - return full target path
    endlocal & set "%~3=%target%"
    exit /b 0
)

REM Remove base path and leading backslash
set "relative=!target:%base%\=!"
endlocal & set "%~3=%relative%"
exit /b 0