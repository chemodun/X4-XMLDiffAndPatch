# Modding tools: XML diff and patch for X4: Foundations

This toolset is a simple XML diff and patch tools for X4: Foundations. It is designed to help modders to compare and patch XML files.

The format of diff XML files is compatible with the appropriate `diff.xsd` format definition. It is means - you can you this tool to create diff files for any XML files used in game.
Also, you can use appropriate tool to patch XML files with diff files, this action has reason to check how your diff file will be applied to the vanilla XML file. Or better understand, what other modders did in their mods.

## Important note

It is highly recommended to use the `diff.xsd` file to validate the diff XML files. It is especially important when you creating them by `XMLDiff.exe`.

But there is one **significant limitation** of the current version of the `diff.xsd` file (distributed with game version 8.00HF3/4)- it has strict rules for the `replace` operation. It has to support only one element in the `replace` element. It is means - if your files have more than one element in the `replace` element, the validation will fail. So, not use the `diff.xsd` file for validation in this case at all.

If the `diff.xsd` file if it located in the "current" folder it will be used automatically. If you want to use another `diff.xsd` file, you can specify it with the `-x` option.

## How to use

- Download the latest release from:
  - GitHub [releases page](https://github.com/chemodun/X4-XMLDiffAndPatch/releases/) - there is an archive file `XMLDiffAndPatch.zip`.
  - [NexusMods](https://www.nexusmods.com/x4foundations/mods/1578) - there is an archive file `XMLDiffAndPatch.zip`.
- Extract the archive file to any useful location.
- Inside will be a folder named `XMLDiffAndPatch` with two executables - `XMLDiff.exe` and `XMLPatch.exe`.

### How to create a diff file

There is a command line help for the `XMLDiff` tool:

```shell
XMLDiff 1.0.1
Developed by Chem O`Dun

  -o, --original_xml            Required. Original XML file or directory.

  -m, --modified_xml            Required. Modified XML file or directory.

  -d, --diff_xml                Required. Output diff file or directory.

  -x, --xsd                     Path to diff.xsd (default: diff.xsd).

  -l, --log-to-file             Enable file logging at the specified level: error|warn|info|debug. Console always logs at info level.

  -a, --append-to-log           (Default: false) Append to existing log file instead of overwriting.

  --only-full-path              (Default: false) Generate only full absolute XPath (no // shorthand).

  --use-all-attributes          (Default: false) Include all attributes in XPath predicates.

  --ignore-diff-in-attribute    Attribute name to ignore when comparing elements.

  --help                        Display this help screen.

  --version                     Display version information.
```

Example:

```shell
XMLDiff.exe -o vanilla.xml -m modified.xml -d diff.xml
```

### Example of resulting diff files

There the is example of the diff files created by tool:

- with add operation:

    ```xml
    <?xml version="1.0" encoding="utf-8" standalone="yes"?>
    <diff>
    <add sel="//ware[@id=&quot;scanningarrays&quot;]" pos="before">
        <ware id="xenon_psi_emitter_mk1" name="{1972092403, 7002}" description="{1972092403, 7002}" transport="equipment" volume="1" tags="satellite noplayerbuild">
        <price min="845800" average="901420" max="1054580" />
        <production time="60" amount="0" method="default" name="Xenon Psi Emitter" />
        <production time="60" amount="0" method="xenon" name="Xenon Psi Emitter" />
        <production time="60" amount="0" method="terran" name="Xenon Psi Emitter" />
        <component ref="xenon_psi_emitter_macro" amount="0" />
        <use threshold="0" />
        </ware>
    </add>
    </diff>
    ```

- with replace operation:

    ```xml
    <?xml version='1.0' encoding='UTF-8'?>
    <diff>
      <replace sel="//do_if[@value=&quot;@$speak and not this.assignedcontrolled.nextorder and (@$defaultorder.id != 'Patrol') and (@$defaultorder.id != 'ProtectPosition') and (@$defaultorder.id != 'ProtectShip') and (@$defaultorder.id != 'ProtectStation') and (@$defaultorder.id != 'Plunder') and (@$defaultorder.id != 'Police') and (not this.assignedcontrolled.commander or (this.assignedcontrolled.commander == player.occupiedship)) and notification.npc_await_orders.active&quot;]/@value">@$speak and not this.assignedcontrolled.nextorder and (@$defaultorder.id != 'ProtectSector') and (@$defaultorder.id != 'Patrol') and (@$defaultorder.id != 'ProtectPosition') and (@$defaultorder.id != 'ProtectShip') and (@$defaultorder.id != 'ProtectStation') and (@$defaultorder.id != 'Plunder') and (@$defaultorder.id != 'Police') and (not this.assignedcontrolled.commander or (this.assignedcontrolled.commander == player.occupiedship)) and notification.npc_await_orders.active</replace>
    </diff>
    ```

#### Path options

##### Default: `//` shorthand when globally unique

By default the tool uses the `//` XPath shorthand when an element is globally unique in the document, producing shorter and more readable `sel` paths.

Example:

```xml
<?xml version="1.0" encoding="utf-8" standalone="yes"?>
<diff>
<add sel="//ware[@id=&quot;scanningarrays&quot;]" pos="before">
    <ware id="xenon_psi_emitter_mk1" ...>
    ...
    </ware>
</add>
</diff>
```

##### `--only-full-path`: always use full absolute path

The `--only-full-path` option forces the tool to always generate a full path (starting from `/rootElement/...`) and never use the `//` shorthand.

Example:

```xml
<?xml version="1.0" encoding="utf-8" standalone="yes"?>
<diff>
<add sel="/wares/ware[@id=&quot;scanningarrays&quot;]" pos="before">
    <ware id="xenon_psi_emitter_mk1" name="{1972092403, 7002}" description="{1972092403, 7002}" transport="equipment" volume="1" tags="satellite noplayerbuild">
    <price min="845800" average="901420" max="1054580" />
    <production time="60" amount="0" method="default" name="Xenon Psi Emitter" />
    <production time="60" amount="0" method="xenon" name="Xenon Psi Emitter" />
    <production time="60" amount="0" method="terran" name="Xenon Psi Emitter" />
    <component ref="xenon_psi_emitter_macro" amount="0" />
    <use threshold="0" />
    </ware>
</add>
</diff>
```

##### Minimal XPath attribute predicates

The tool adds attribute predicates to a path step only as needed to make it unique within its parent. If the element name alone is already unique at that level, no attributes are added. Additional attributes are appended one by one only until uniqueness is achieved.

This produces cleaner, more readable `sel` paths compared to always including the first attribute regardless of need.

##### Use all attributes in XPath

The `--use-all-attributes` option forces all attributes of an element to be included in its XPath predicate, regardless of uniqueness. Useful when maximum specificity is preferred.

Example:

```xml
<?xml version="1.0" encoding="utf-8" standalone="yes"?>
<diff>
<add sel="/wares/ware[@id=&quot;scanningarrays&quot;][@name=&quot;{20201,3301}&quot;][@description=&quot;{20201,3302}&quot;][@factoryname=&quot;{20201,3304}&quot;][@group=&quot;hightech&quot;][@transport=&quot;container&quot;][@volume=&quot;38&quot;][@tags=&quot;container economy&quot;]" pos="before">
    <ware id="xenon_psi_emitter_mk1" name="{1972092403, 7002}" description="{1972092403, 7002}" transport="equipment" volume="1" tags="satellite noplayerbuild">
    <price min="845800" average="901420" max="1054580" />
    <production time="60" amount="0" method="default" name="Xenon Psi Emitter" />
    <production time="60" amount="0" method="xenon" name="Xenon Psi Emitter" />
    <production time="60" amount="0" method="terran" name="Xenon Psi Emitter" />
    <component ref="xenon_psi_emitter_macro" amount="0" />
    <use threshold="0" />
    </ware>
</add>
</diff>
```

##### Ignore differences in the specified attribute

The `--ignore-diff-in-attribute` option will ignore differences in the specified attribute when comparing elements. It is useful when you want to ignore differences in attributes like `version`, `comment`, etc.

#### Defining the position generation for the diff by in-line comment

The `pos` attribute of the `add` is usually set to `after`, taking in account the common logic of how the program is working. But in some cases, you may want to have a diff file with the `pos` attribute set to `before`. Mostly it is useful when working in conjunction with VS Code.
You can define the position of the `add` element in the diff file by using an in-line comment in the original XML file. The comment should be placed before the element you want to add and should contain the text `<!-- pos="before" -->`.

### How to apply a diff file

There is a command line help for the `XMLPatch` tool:

```shell
XMLPatch 1.0.1
Developed by Chem O`Dun

  -o, --original_xml    Required. Path to the original XML file or directory.

  -d, --diff_xml        Required. Path to the diff XML file or directory.

  -u, --output_xml      Required. Path for the output XML file or directory.

  -x, --xsd             Path to diff.xsd (default: diff.xsd).

  -l, --log-to-file     Enable file logging at the specified level: error|warn|info|debug. Console always logs at info level.

  -a, --append-to-log   (Default: false) Append to existing log file instead of overwriting.

  --allow-doubles       (Default: false) Skip duplicate-element guard when applying <add> operations.

  --help                Display this help screen.

  --version             Display version information.
```

Example:

```shell
XMLPatch.exe -o vanilla.xml -d diff.xml -u modified.xml
```

### Example of resulting patched XML files

There is an example of the patched XML files created by the tool:

- with add operation:

    ```xml
      <ware id="satellite_mk2" name="{20201,20401}" description="{20201,20402}" transport="equipment" volume="1" tags="equipment satellite">
        <price min="44380" average="52215" max="60045"/>
        <production time="60" amount="1" method="default" name="{20206,101}">
          <primary>
            <ware ware="advancedelectronics" amount="5"/>
            <ware ware="energycells" amount="10"/>
            <ware ware="scanningarrays" amount="5"/>
          </primary>
        </production>
        <production time="60" amount="1" method="xenon" name="{20206,601}" tags="noplayerbuild">
          <primary>
            <ware ware="energycells" amount="10"/>
            <ware ware="silicon" amount="1"/>
          </primary>
        </production>
        <component ref="eq_arg_satellite_02_macro"/>
        <use threshold="0"/>
      </ware>
      <ware id="xenon_psi_emitter_mk1" name="{1972092403, 7002}" description="{1972092403, 7002}" transport="equipment" volume="1" tags="satellite noplayerbuild">
        <price min="845800" average="901420" max="1054580"/>
        <production time="60" amount="0" method="default" name="Xenon Psi Emitter"/>
        <production time="60" amount="0" method="xenon" name="Xenon Psi Emitter"/>
        <production time="60" amount="0" method="terran" name="Xenon Psi Emitter"/>
        <component ref="xenon_psi_emitter_macro" amount="0"/>
        <use threshold="0"/>
      </ware>
    ```

- with replace operation:

    ```xml
        <set_to_default_flight_control_model object="this.assignedcontrolled"/>
        <set_value name="$defaultorder" exact="this.assignedcontrolled.defaultorder"/>
        <do_if value="@$speak and not this.assignedcontrolled.nextorder and (@$defaultorder.id != 'Patrol') and (@$defaultorder.id != 'ProtectSector') and (@$defaultorder.id != 'ProtectPosition') and (@$defaultorder.id != 'ProtectShip') and (@$defaultorder.id != 'ProtectStation') and (@$defaultorder.id != 'Plunder') and (@$defaultorder.id != 'Police') and (not this.assignedcontrolled.commander or (this.assignedcontrolled.commander == player.occupiedship)) and notification.npc_await_orders.active">
          <set_value name="$speakline" exact="10304" comment="Awaiting orders."/>
    ```

### If output XML is a directory

If the output XML is a directory, the tool will create a new XML file with the same name as the original XML file in the output directory.
For example, if the original XML file is `vanilla.xml` and the output directory is `output`, the tool will create a new XML file `output/vanilla.xml`.

### How to apply the tools to directories

#### Applying XMLDiff to directories

You can apply the XMLDiff tool to directories. In this case, the tool will traverse the directory structure and create diff files for XML files with identical names and relative paths. The process is as follows:

- If all input parameters are directories, the tool will create diff files for all XML files in the directories.
- The tool will recursively go through the directory structure, using modified files as a "key" for it.
- For each changed file, the corresponding original XML file in the original directory with the same relative path will be checked.
- If the original XML file is not found, the operation will be skipped.
- If the original XML file is found, the diff file will be created in the output directory with the same relative path.

Example:

```shell
XMLDiff.exe -o vanilla_dir -m modified_dir -d diff_dir
```

#### Applying XMLPatch to directories

You can apply the XMLPatch tool to directories. In this case, the tool will traverse the directory structure and apply the patch to XML files with identical names and relative paths. The process is as follows:

- If all input parameters are directories, the tool will apply the patch to all XML files in the directories.
- The tool will recursively go through the directory structure, using diff files as a "key" for it.
- For each diff file, the corresponding original XML file in the original directory with the same relative path will be checked.
- If the original XML file is not found, the operation will be skipped.
- If the original XML file is found, the diff file will be patched with the original XML file, and a new patched XML file will be created in the output directory with the same relative path.

Example:

```shell
XMLPatch.exe -o vanilla_dir -d diff_dir -u modified_dir
```

## Issues reporting

If you have any issues with the tool, please create an issue on the [issues page](https://github.com/chemodun/x4_XMLDiffAndPatch/issues).
It will be highly appreciated if you provide the version of the used tool and the `XMLDiff.log` or `XMLPatch.log` file respectively.
To create such a log file, use the `-l` option with the `debug` level, e.g. `-l debug`. The console output is always at `info` level; detailed debug information is written only to the file.

## License

This tool is licensed under the Apache License, Version 2.0. You can find it in the [LICENSE](LICENSE) file.

## Credits

Special thanks to [Duncaroos](https://forum.egosoft.com/memberlist.php?mode=viewprofile&u=419622) for the patience, testing, and valuable feedback.

## Additional links

There is a topic on the [EGOSOFT forum](https://forum.egosoft.com/viewtopic.php?t=468623), related to this toolset.

## Antivirus scanning

Please be aware - each release archive has an appropriate link to the [VirusTotal](https://www.virustotal.com). Follow the link to be sure that the archive is safe.

## Changelog

### [1.1.0] - 2026-05-17

- Improved:
  - XMLDiff: implement LCS-based child comparison and edit processing

### [1.0.5] - 2026-05-17

- Improved:
  - XMLDiff: enhance matching logic for original and modified elements

### [1.0.4] - 2026-05-16

- Fixed:
  - XMLDiff: attribute comparison logic

### [1.0.3] - 2026-05-15

- Fixed:
  - Single attribute change handling.
  - Text nodes comparison.

### [1.0.2] - 2026-05-15

- Improved:
  - Logging.

### [1.0.1] - 2026-05-15

- Improved:
  - Full internal rewrite.

### [0.2.31] - 2026-05-15

- Fixed:
  - XMLDiff: `replace`/`remove` operation detection logic for attributes

### [0.2.30] - 2026-05-15

- Fixed:
  - XMLDiff: `replace`/`remove` operation detection logic

### [0.2.29] - 2026-03-02

- Improved:
  - XMLDiff: `replace` operation detection logic

### [0.2.28] - 2026-02-22

- Improved:
  - XMLDiff and XMLPatch: added possibility to process multi-line `replace` diff elements.

### [0.2.27] - 2025-08-28

- Improved:
  - XMLDiff: added --ignore-diff-in-attribute option to ignore differences in the specified attribute when comparing elements. It is useful when you want to ignore differences in attribute `version` in X4 script files.

### [0.2.26] - 2025-06-16

- Improved:
  - XMLDiff: added in-line comments processing for the `add` elements in the modified XML file. It allows to define the position attribute of the `add` element in the diff file.

### [0.2.25] - 2025-03-31

- Fixed:
  - XMLDiff: fixed issue missed root element attributes comparison
  - XMLDiff: fixed usage of the removed element path for the further diff operations
  - XMLDiff: fixed incorrect index number when addressing the elements in XPath
  - XMLDiff: fixed identification elements via `sibling` keyword in XPath

### [0.2.24] - 2025-03-17

- Improved:
  - Both utilities: result folder will be created in recursive processing as it made now for the single file.
  - XMLDiff: sibling keyword usage in XPath for the elements.
  - XMLDiff: XPath generation when the element has child elements, which can unique identify it.

- Changed:
  - XMLDiff: --only-full-path option replaced by --anywhere-is-allowed. And default behavior is to use full path.
  - Both utilities: log level of console will not be more detailed than for the log file.
  - Both utilities: the unknown options will be ignored.

- Fixed:
  - XMLDiff: doubling the first attribute in XPath for the elements, if more than one attribute is used.

### [0.2.23] - 2025-03-15

- Fixed:
  - XMLDiff: fixed issue with the last sub element comparison (index out of range)

### [0.2.22] - 2025-03-10

- Improved
  - XMLDiff: The first attribute of elements in XPath will be always added to make a diff more clear.
  - XMLDiff: If one attribute is not enough to define the element, the next one will be added to the XPath, iteratively.
  - XMLDiff: Added the [short description of integration with VSCode](/forVSCode/XMLDIffwithVSCode.md) via RunXMLDiff.bat script and appropriate extension to make a diffs "on the fly" during editing XML files (modified ones).

### [0.2.21] - 2025-03-03

- Fixed
  - XMLDiff: fixed issue with text nodes differing
  - XMLDiff: improved "pos" definition logic
  - XMLDiff: improved logging information
- Added
  - XMLPatch: added --allow-doubles option, useful for scripts patching
  - XMLPatch: added comments processing, now comments will be added to the resulting XML file

### [0.2.20] - 2025-02-27

- Fixed
  - XMLDiff: wrong attribute selection for the path
  - XMLDiff: fixed issue with not applied --append-to-log option
  - XMLDiff: fixed issue with wrong changed attributes count detection
  - XMLDiff: fixed usage or remove/add instead of replace for elements
  - XMLPatch: skip the diff file without diff elements
- Improved
  - Both utilities: --log-to-file option now requires a log level (error, warn, info, debug) as a parameter

### [0.2.17] - 2025-02-25

- Fixed
  - Added checks to prevent duplicate elements during addition

### [0.2.16] - 2025-02-25

- Fixed
  - Fixed issue with element replacements
- Improved
  - Logging information

### [0.2.15] - 2025-02-24

- Fixed
  - Fixed loading the diff.xsd
  - Fixed issue, if resulting file has to be located in current folder
- Improved
  Logging information, especially about wrong sel value. More info logged about processed XML elements.
- Added
  - Possibility to append into existing debug log

### [0.2.14] - 2025-01-17

- Added
  - First public version coded in C#.
