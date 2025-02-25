# Modding tools: XML diff and patch for X4: Foundations

This toolset is a simple XML diff and patch tools for X4: Foundations. It is designed to help modders to compare and patch XML files.

The format of diff XML files is compatible with the appropriate `diff.xsd` format definition. It is means - you can you this tool to create diff files for any XML files used in game.
Also, you can use appropriate tool to patch XML files with diff files, this action has reason to check how your diff file will be applied to the vanilla XML file. Or better understand, what other modders did in their mods.

## Important note

It is highly recommended to use the `diff.xsd` file to validate the diff XML files. It is especially important when you creating them by `XMLDiff.exe`.
If the `diff.xsd` file if it located in the "current" folder it will be used automatically. If you want to use another `diff.xsd` file, you can specify it with the `-x` option.

## How to use

- Download the latest release from:
  - GitHub [releases page](https://github.com/chemodun/X4-XMLDiffAndPatch/releases/) - there is an archive file `XMLDiffAndPatch.zip`.
  - [NexusMods](https://www.nexusmods.com/x4foundations/mods/1578) - there is an archive file `XMLDiffAndPatch.zip`.
- Extract the archive file to any useful location.
- Inside will be a folder, named XMLDiffAndPatch with two executables - `XMLDiff.exe` and `XMLPatch.exe`.

### How to create a diff file

There is a command line help for the `XMLDiff` tool:

```shell
XMLDiff 0.2.15
Developed by Chem O`Dun

  -o, --original_xml      Required. Path to the original XML file or directory.

  -m, --modified_xml      Required. Path to the modified XML file or directory.

  -d, --diff_xml          Required. Path for the diff XML file or directory.

  -x, --xsd               Path to the diff.xsd schema file.

  -l, --log-to-file       (Default: false) Log to a file.

  -a, --append-to-log    (Default: false) Append logs to the existing log file.

  --only-full-path        (Default: false) Generate only full path.

  --use-all-attributes    (Default: false) Use all attributes in XPath.

  --help                  Display this help screen.

  --version               Display version information.
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

##### Only full path

The `--only-full-path` option will generate only the full path to the element in the XML file. It is mean - there no `//` will be in the `sel` attribute of the `add`, `replace` or `remove` element.

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

##### Use all attributes in XPath

The `--use-all-attributes` option will generate the `sel` attribute with all attributes of the element in the XML file. It is mean - there will be all attributes of the element in the `sel` attribute of the `add`, `replace` or `remove` element.

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

### How to apply a diff file

There is a command line help for the `XMLPatch` tool:

```shell
XMLPatch 0.2.15
Developed by Chem O`Dun

  -o, --original_xml    Required. Path to the original XML file or directory.

  -d, --diff_xml        Required. Path to the diff XML file or directory.

  -u, --output_xml      Required. Path for the output XML file or directory.

  -x, --xsd             Path to the diff.xsd schema file.

  -l, --log-to-file     (Default: false) Log to a file.

  -a, --append-to-log    (Default: false) Append logs to the existing log file.

  --help                Display this help screen.

  --version             Display version information.
```

Example:

```shell
XMLPatch.exe -o vanilla.xml -d diff.xml -u modified.xml
```

### Example of resulting patched XML files

There the is example of the patched XML files created by tool:

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

### How to apply a tools to a directories

You can apply the tools to directories. In this case, the tools will apply the diff or patch to all XML files in the directory.
The logic will be a next:

- all input parameters are directories - the tools will apply the diff or patch to all XML files in the directories.
- it will recursively gro thru the diff or changed XML files in the directories, respectively to the tool.
- for each diff or changed files will be checked a corresponding original XML file in the original directory with the same relative path.
- if the original XML file is not found - the diff or changed file will be skipped.
- if the original XML file is found - the diff or changed file will be patched with the original XML file and created a new patched XML file in the output directory with the same relative path.

Example:

```shell
XMLDiff.exe -o vanilla_dir -m modified_dir -d diff_dir
```

or

```shell
XMLPatch.exe -o vanilla_dir -d diff_dir -u modified_dir
```

## Issues reporting

If you have any issues with the tool, please create an issue in the [issues page](https://github.com/chemodun/x4_XMLDiffAndPatch/issues).
Will be highly appreciated if you will provide a version of used tool and XMLDiff.log or XMLPatch.log file respectively to the tool.
To create such debug file please use the `--log-to-file` option.

## License

There is a MIT license for this tool. You can find it in the [LICENSE](LICENSE) file.

## Changelog

### [0.2.17] - 2025-02-25

- Fixed
  - Made prevention of adding doubles from different files

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

## Additional links

There is a topic on the [EGOSOFT forum](https://forum.egosoft.com/viewtopic.php?t=468623), related to this toolset.

## Antivirus scanning

Please be aware - each release archive has an appropriate link to the [VirusTotal](https://www.virustotal.com). Follow the link to be sure that the archive is safe.
