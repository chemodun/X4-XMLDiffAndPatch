# Using XMLDIff with Visual Studio Code

## Introduction

This document describes how to use XMLDiff with Visual Studio Code.

## Prerequisites

- Visual Studio Code
- XMLDiff.exe and [RunXMLDiff.bat](/forVSCode/RunXMLDiff.bat) from this repository
- VSCode extension [Run on Save](https://marketplace.visualstudio.com/items?itemName=emeraldwalk.RunOnSave) by [emeraldwalk](https://marketplace.visualstudio.com/publishers/emeraldwalk)

## Steps

1. Prepare a folders structure like the following:

    ```plaintext
    RootExtensionFolder
    │   XMLDiffAndPatch
    │   │   XMLDiff.exe
    │   RunXMLDiff.bat
    │   aiscripts
    │   aiscripts.modified
    │   aiscripts.original
    │   md
    │   md.modified
    │   md.original
    ```

2. You can skip creation `*.original` folders, if you will use the `extracted` files folder.
3. Copy the [RunXMLDiff.bat](/forVSCode/RunXMLDiff.bat) file to the root of your project.
4. Copy the `XMLDiff.exe` file to the `XMLDiffAndPatch` folder in the root of your project.
5. Install the VSCode extension [Run on Save](https://marketplace.visualstudio.com/items?itemName=emeraldwalk.RunOnSave) by [emeraldwalk](https://marketplace.visualstudio.com/publishers/emeraldwalk)
6. Open the settings of the extension by pressing `Ctrl + ,` and search for `runOnSave.commands`
7. Add the following configuration to the settings:
    7.1 In case of not to use the "Extracted" files folder, i.e. with `*.original` folders:

    ```json
    "commands": [
        {
            "match": "\\.modified\\\\.+?\\.xml$",
            "cmd": "${workspaceFolder}\\RunXMLDiff.bat ${file} ${workspaceFolder}\\XMLDiffAndPatch\\XMLDiff.exe
        }
    ]
    ```

    7.2. With the `extracted` folder, which is outside the project folder:

    ```json
    "commands": [
        {
            "match": "\\.modified\\\\.+?\\.xml$",
            "cmd": "${workspaceFolder}\\RunXMLDiff.bat ${file} ${workspaceFolder}\\XMLDiffAndPatch\\XMLDiff.exe" ${workspaceFolder}\\..\\extracted"
        }
    ]
    ```

8. Save the settings and close the settings tab.

Now you can modify the `*.modified` files and the `RunXMLDiff.bat` will be executed automatically on save.

You can check an output in the `OUTPUT` tab in the VSCode, selecting the `Run on Save` item in the dropdown list.

## Description of the RunXMLDiff.bat

`RunXMLDiff.bat` is a batch script that compares a modified XML file with its original version using the `XMLDiff.exe` utility. The script takes the paths to the modified file, the XMLDiff utility, and optionally the path to the original files as input. It then constructs the appropriate paths, checks for the existence of necessary files and directories, and runs the XMLDiff utility to generate a diff file.

### Usage

```batch
RunXMLDiff.bat <ModifiedFilePath> <XMLDiffPath> [OriginalFilesPath]
```

#### Parameters

- `<ModifiedFilePath>`: The full path to the modified XML file. This parameter is mandatory.
- `<XMLDiffPath>`: The full path to the `XMLDiff.exe` utility. This parameter is mandatory.
- `[OriginalFilesPath]`: The full path to the directory containing the original XML files. This parameter is optional.

#### Example

```batch
RunXMLDiff.bat "C:\path\to\md.modified\file.xml" "C:\path\to\XMLDiff.exe" "C:\path\to\original\files"
```

#### How It Works

1. **Extract Type and Filename**: The script extracts the type (e.g., `aiscript` or `md`) and filename from the modified file path.
2. **Determine Target File Path**: The script constructs the target file path by removing `.modified` from the modified file path.
3. **Determine Original File Path**:
   - If the `OriginalFilesPath` is provided, the original file path is constructed as `OriginalFilesPath\type\filename`.
   - If the `OriginalFilesPath` is not provided, the original file path is constructed by replacing `.modified` with `.original` in the modified file path.
4. **Check Existence**:
   - The script checks for the existence of `XMLDiff.exe`, the modified file, the original file, and the target file directory.
5. **Run XMLDiff**: If all checks pass, the script runs `XMLDiff.exe` with the appropriate parameters to generate the diff file.

### Error Handling

The script will output error messages and exit with a non-zero status code if any of the following conditions are met:

- `XMLDiff.exe` is not found at the specified path.
- The modified file is not found at the specified path.
- The original file is not found at the constructed path.
- The target file directory does not exist.

### Output

If the script runs successfully, it will output a message indicating that the XMLDiff operation completed successfully and provide the path to the generated diff file.

### Notes

- Ensure that the `XMLDiff.exe` utility is accessible and has the necessary permissions to execute.
- The script assumes that the modified file path follows the convention where the last folder in the path is of the form `type.modified`.

### License

This script is provided "as-is" without any warranty. Use it at your own risk.
