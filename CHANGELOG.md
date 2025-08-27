# Changelog

## [0.2.27](https://github.com/chemodun/X4-XMLDiffAndPatch/compare/v0.2.26...v0.2.27) (2025-08-27)


### Bug Fixes

* **bat:** Correct original file path assignment in RunXMLDiff.bat ([c4cb9ed](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/c4cb9ed32e338245467d12b396f34accd2eefec1))


### Code Refactoring

* **XMLDiff:** add --ignore-diff-in-attribute option to ignore differences in the specified attribute ([c8deb05](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/c8deb0510513c78774954d0c3c44615a6aa4d358))


### Documentation

* **bbcode:** Update bbcode files ([03ae262](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/03ae26249c2fe870538862ce33b2bd9dd74b75af))
* **bbcode:** Update bbcode files ([b290772](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/b290772290c182d4a867befc6ced9de07f89b03d))
* **README:** clarify Changelog ([ad600b0](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/ad600b04f1f741e35ce7c0cadca0c678a0c45326))
* **vscode:** Fix list formatting and improve clarity in instructions in nexus version ([21111ae](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/21111ae98da364de98debec8c6ba1dcc29589c6f))
* **vscode:** update demo video formatting ([0f6e003](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/0f6e00371c2138eed4d30547f35a67c7fc41eef0))
* **XMLDiff:** Improve formatting and clarity in XMLDiff with VSCode documentation ([890528a](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/890528a786b73f672340d06956b8d33d0dbfd0ee))
* **XMLDiff:** reflect improvement in documentation ([c8deb05](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/c8deb0510513c78774954d0c3c44615a6aa4d358))

## [0.2.26](https://github.com/chemodun/X4-XMLDiffAndPatch/compare/v0.2.25...v0.2.26) (2025-06-16)


### Bug Fixes

* **RunXMLDiff.bat:** improve original file path determination logic ([b6be689](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/b6be689bad753436161b764faa95a01f5b9c2b2e))
* **XMLDiff:** previously implemented handling of in-line comments for defining position of added elements in diff ([abf81f8](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/abf81f8f6ac684dc1b15de4a507743711ee05665))


### Miscellaneous Chores

* **XMLDiff:** implement handling of in-line comments for defining position of added elements in diff ([b6be689](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/b6be689bad753436161b764faa95a01f5b9c2b2e))


### Documentation

* **bbcode:** Update bbcode files ([6ca6a97](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/6ca6a970af6c88ed3a7c2a26eb955cbea57df567))
* **bbcode:** Update bbcode files ([adb84d1](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/adb84d12420941045a465d9e36853e270948b856))
* **readme:** enhance documentation for in-line comment position generation and update changelog for version 0.2.26 ([b6be689](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/b6be689bad753436161b764faa95a01f5b9c2b2e))
* **readme:** reallocate the file XMLDiff with VSCode documentation and correct link to it ([46592c1](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/46592c13f155d30a510aa9dacfba78d41856826a))
* **vscode:** clarify usage of original and extracted folders in instructions ([ea88515](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/ea88515b055fdf77f5ea4b44056acd0c6b991ee0))
* **vscode:** update notes and add demo video for better usage guidance ([30818d1](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/30818d14b14c189823f6ba5cc8b82dc13cd8ea2b))
* **vscode:** update section title from "Small demo" to "Demo" ([07beece](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/07beece128a332f39284a170af84126941dba555))

## [0.2.25](https://github.com/chemodun/X4-XMLDiffAndPatch/compare/v0.2.24...v0.2.25) (2025-03-31)


### Bug Fixes

* **XMLDiff:** fixed not comparing the attributes in the root node ([43c9e2b](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/43c9e2b8e2bee9ccef170ca2be998cf31fb2b22a))
* **XMLDiff:** fixed numbering an elements (was wrong index) ([43c9e2b](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/43c9e2b8e2bee9ccef170ca2be998cf31fb2b22a))
* **XMLDiff:** identification of element via sibling now fully comply to XMLPath requirements ([43c9e2b](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/43c9e2b8e2bee9ccef170ca2be998cf31fb2b22a))
* **XMLDiff:** relation on removed element in next operations, now remove operation will be shifted after the referred ([43c9e2b](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/43c9e2b8e2bee9ccef170ca2be998cf31fb2b22a))


### Code Refactoring

* **XMLDiff:** `pathForParent` now is right named ([43c9e2b](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/43c9e2b8e2bee9ccef170ca2be998cf31fb2b22a))

## [0.2.24](https://github.com/chemodun/X4-XMLDiffAndPatch/compare/v0.2.23...v0.2.24) (2025-03-16)


### Bug Fixes

* **workflow:** add xargs to handle file names with spaces in markdown update workflow ([970f072](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/970f072010c9dfa0cc025b0944c6824c1cb48ed6))
* **XMLDiff, XMLPatch:** log level of console will not be more detailed than for the log file. Resolves [#22](https://github.com/chemodun/X4-XMLDiffAndPatch/issues/22) ([12f9a48](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/12f9a486d6d477e5d3c98fe8bc96e9c428daa449))
* **XMLDiff, XMLPatch:** result folder will be created in recursive processing too (as for the single file) Resolves [#21](https://github.com/chemodun/X4-XMLDiffAndPatch/issues/21) ([dd442ff](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/dd442ffa5ddd3e549ec1ad791d945d6db8748ea5))
* **XMLDiff:** doubling the first attribute in XPath (Fixes [#20](https://github.com/chemodun/X4-XMLDiffAndPatch/issues/20)) ([c0d489d](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/c0d489da0b79182351e564e0faaecc157e4aca8e))
* **XMLDiff:** sibling keyword usage in XPath ([c0d489d](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/c0d489da0b79182351e564e0faaecc157e4aca8e))


### Code Refactoring

* **workflow:** simplify markdown update process by using changed-files action ([44147e4](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/44147e47d974d721f00cb04d0586b40074e5cc52))
* **XMLDiff:** --only-full-path option replaced by --anywhere-is-allowed. And default behavior is to use full path. ([50aa71f](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/50aa71fbcc71038a3d64a6b1432c3ad5aeb79086))
* **XMLDiff, XMLPatch:** enable ignoring unknown arguments and improve help output ([50aa71f](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/50aa71fbcc71038a3d64a6b1432c3ad5aeb79086))
* **XMLDiff:** improve XPath generation when the element has child elements, which can unique identify it ([c0d489d](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/c0d489da0b79182351e564e0faaecc157e4aca8e))


### Miscellaneous Chores

* **workflow:** fix ([f817944](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/f817944d2fde3421bfb503f143af97a5b2a04555))
* **workflow:** simplify file specification in markdown update workflow ([4bfc97e](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/4bfc97ea13a1445e13583a468ae71abf3a2c4d22))
* **workflow:** update changed-files action to version 0.1.1 ([17f189d](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/17f189d5c096dab85793211e39e083682feac39b))


### Documentation

* **bbcode:** Update bbcode files ([fc4d3e3](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/fc4d3e3785feadb3407012238a3ac24327a3f8de))
* **readme:** correct spacing in developer attribution for XMLDiff tool ([17b8515](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/17b8515faa7e5faece828550ed903d3895126af9))
* **readme:** fix formatting in command line help section for XMLDiff tool ([144b9e0](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/144b9e01db6e1f6a5a575a8a04edf80e780ff34f))
* **readme:** fix formatting issue in command line help section for XMLDiff tool ([6d3b751](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/6d3b75142ee6ee90a9bdc21523ebf9415e4838e1))
* **readme:** fix spacing in developer attribution for XMLDiff tool ([5949fc8](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/5949fc82e5a41c5d3effc7eeb6cb7cc0673882c0))
* **readme:** update changelog for version 0.2.24 with improvements, changes, and fixes ([179faaf](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/179faaf8b928d9db938918a8fd98dc2d3906e620))
* **readme:** update version to 0.2.24 and modify command line options description ([50aa71f](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/50aa71fbcc71038a3d64a6b1432c3ad5aeb79086))

## [0.2.23](https://github.com/chemodun/X4-XMLDiffAndPatch/compare/v0.2.22...v0.2.23) (2025-03-15)


### Bug Fixes

* **workflow:** enhance file change detection in markdown update workflow ([ea8405f](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/ea8405f8bf5c4a7c5865b4b1c5d6b272c8375e6c))
* **workflow:** rename variable for clarity in markdown update workflow ([5604053](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/560405355683c98058f16b64e57056c0d76b2fcc))
* **workflow:** trying to replace compromised tj-actions ([6d5082d](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/6d5082d745c17d47951229ed60a8a29d7af30d17))
* **workflow:** update environment variable reference for file masks in markdown update workflow ([880bc08](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/880bc0884cf904f934308c84b796e6091e7070cc))
* **XMLDiff:** improve element comparison logic to right handle last elements ([4824209](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/48242090fe19e67e129c85b8cebd4236e5505d92))


### Documentation

* **CHANGELOG:** add entry for version 0.2.23 with XMLDiff fix ([8128d54](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/8128d547a0f3c72fa4217d501230e81def387842))
* **README:** clarify XMLDiff fix description for index out of range issue ([129598f](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/129598feb45f52853ab5761c35f8b18540c985d4))
* **README:** clarify XMLDiff fix description for version 0.2.23 ([7796856](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/779685606b306e73cd9aeccf3308e9289fe86498))
* **README:** simplify XMLDiff fix description for version 0.2.23 ([ba7ed4c](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/ba7ed4c4bbd455ab61b74a01bab0340f8c2ca721))
* **README:** update XMLDiff fix description to clarify index out of range issue ([5bbab50](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/5bbab500a1e28f67f62f254198b180170c1a69b2))

## [0.2.22](https://github.com/chemodun/X4-XMLDiffAndPatch/compare/v0.2.21...v0.2.22) (2025-03-10)


### Code Refactoring

* **XMLDiff:** always include the first attribute in the XPath element ([f3aec88](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/f3aec88c8f9ccc47056fa3b72f09b7f4a221b748))
* **XMLDiff:** extract attribute to XPath element conversion into a separate method ([f3aec88](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/f3aec88c8f9ccc47056fa3b72f09b7f4a221b748))
* **XMLDiff:** if one of attribute is not enough to identify the element, include step by step next attributes ([f3aec88](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/f3aec88c8f9ccc47056fa3b72f09b7f4a221b748))
* **XMLDiff:** improve replace attribute detection ([37ab85e](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/37ab85e71112c5b7e4c6844e8900ca0500d8b0e8))


### Miscellaneous Chores

* **XMLDiff:** add RunXMLDiff.bat script and documentation for integration with VSCode; update cspell.json ([67153e9](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/67153e91c6433f12440c445af5d2a1ebf2296652))


### Documentation

* **bbcode:** Update bbcode files ([66f41a2](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/66f41a2014859845de7cb4b5419ac8ac904537fe))
* **README:** add integration details for VSCode and RunXMLDiff.bat script ([dad5f06](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/dad5f06b7ea576ec733a4e4d40637ddc0b645d92))
* **README:** update changelog for version 0.2.22 with XMLDiff improvements ([31c042b](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/31c042b3f8778635ef2f7e095d02d06b97e915fe))
* **README:** update VSCode integration details and improve link formatting ([203c8e2](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/203c8e2229c777937c21a59e02bc98d53fc08500))

## [0.2.21](https://github.com/chemodun/X4-XMLDiffAndPatch/compare/v0.2.20...v0.2.21) (2025-03-03)


### Bug Fixes

* **XMLDiff:** get back enhanced element comparison logic to handle next matched elements correctly ([eb05c82](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/eb05c826e69d72247cc76d9f7495fdbe199d68ae))
* **XMLDiff:** missed removing the attributes ([5595509](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/5595509b4290afd329d5845543947987ae7ec132))
* **XMLDiff:** rename variable for clarity in tracking removed or replaced elements ([e4644ee](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/e4644eebfa9069a53672ef1cf4e03f955eca073b))
* **XMLDiff:** simplify condition for identifying replace or remove operations ([3485f06](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/3485f0675369e045fd8f4f838365818441eb016b))
* **XMLDiff:** track last removed element to improve XPath matching logic ([b639741](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/b639741788804306e64befb250f81527f3c793f7))
* **XMLPatch:** streamline option attribute formatting for AllowDoubles property ([3df7df5](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/3df7df50c1afdb651667780f96745a2960a442af))
* **XMLPatch:** when multiple items added the order can be broken ([7ed6837](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/7ed6837c1f4b345848b351830fd80897eb318c28))


### Code Refactoring

* **XMLDiff:** enhance add operation logic for XML element positioning, use "after" and "prepend" instead of "before" in most cases&gt; ([dc92ca1](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/dc92ca117641b34123ad68c694d84be1cd4d76b7))
* **XMLDiff:** extract text value retrieval into a separate method for improved readability, as XML libraries on Value return all text from all inherited items ([de4ea36](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/de4ea366f61e0c131e30cbab9bacbcec73964255))
* **XMLDiff:** if difference in one attribute when them more then one, and all inherited elements are equal - it will be marked as change of attribute ([0b81ee8](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/0b81ee843a9bae7fc770813883fb80ec6c1059c9))
* **XMLDiff:** if difference in one attribute, but there is one attribute in element - it will be marked as totally differ ([0b81ee8](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/0b81ee843a9bae7fc770813883fb80ec6c1059c9))
* **XMLDiff:** improve add operation logic to exclude numeric ID patterns if possible ([aa64598](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/aa64598ebf63a7b9538be05631dc404e9835d957))
* **XMLDiff:** update logging to use Info level for operations and improve clarity ([0b81ee8](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/0b81ee843a9bae7fc770813883fb80ec6c1059c9))
* **XMLPatch:** add option to allow doubles in diff XML processing ([8e4e053](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/8e4e053ed7c6f5dc6354753d8808749b7ad117e4))
* **XMLPatch:** clone and add elements and comments to improve logging and maintain state ([9b683de](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/9b683de29429692b2495632437eb5d56f1b9f5cd))
* **XMLPatch:** enhance element addition logic to handle comments and improve logging ([7ed6837](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/7ed6837c1f4b345848b351830fd80897eb318c28))
* **XMLPatch:** streamline element addition logic for improved clarity and maintainability ([bb2a720](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/bb2a7208dcccd9e99fa974766b1fe57c5381e816))


### Documentation

* **bbcode:** Update bbcode files ([bf97f53](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/bf97f53ea16a67a14c65335bdc184e008340432a))
* **bbcode:** Update bbcode files ([c9aec6e](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/c9aec6e9d6d60b61e01db7d47a604e2cdac8a0b2))
* **README:** enhance documentation with debug logging details and credits ([2705f32](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/2705f320e4ad6f825647ce7256f9c9af93186759))
* **README:** update changelog for version 0.2.20 ([2f9e134](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/2f9e13412a6a6800b348d70511f3a78cf954797e))
* **README:** update version to 0.2.20 and add --allow-doubles option description ([bd92775](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/bd92775aaee925e5b08219ba00ab8e6847e2172d))

## [0.2.20](https://github.com/chemodun/X4-XMLDiffAndPatch/compare/v0.2.19...v0.2.20) (2025-02-27)


### Bug Fixes

* **XMLPatch:** validate root element of diff XML before processing, i.e. skipping non-diff XML ([13d720b](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/13d720b1f46ad3633c99fec170ebdd4822879005))

## [0.2.19](https://github.com/chemodun/X4-XMLDiffAndPatch/compare/v0.2.18...v0.2.19) (2025-02-27)


### Bug Fixes

* **XMLDiff:** fix always usage remove for elements, replace detection is implemented, i.e. fixes [#9](https://github.com/chemodun/X4-XMLDiffAndPatch/issues/9) ([c070c59](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/c070c59b68d6ce73c263694130012286e54d8206))
* **XMLDiff:** fix detection of changes in more then one attribute ([c070c59](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/c070c59b68d6ce73c263694130012286e54d8206))
* **XMLDiff:** on adding to the end to `after` is used now ([c070c59](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/c070c59b68d6ce73c263694130012286e54d8206))

## [0.2.18](https://github.com/chemodun/X4-XMLDiffAndPatch/compare/v0.2.17...v0.2.18) (2025-02-27)


### Bug Fixes

* **XMLDiff:** fix losing the previously gathered path if some parent is accessible via "//" ([a04494d](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/a04494d2e3cf3a0a216dc9e071101f310fbbb7ab))
* **XMLDiff:** fix skipping valid xpath step in case if "//" doesn't work with it ([a04494d](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/a04494d2e3cf3a0a216dc9e071101f310fbbb7ab))
* **XMLDiff:** in total fixes [#10](https://github.com/chemodun/X4-XMLDiffAndPatch/issues/10) ([a04494d](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/a04494d2e3cf3a0a216dc9e071101f310fbbb7ab))
* **XMLDiff:** update log file deletion behavior to respect appendToLog option. Fixes  [#8](https://github.com/chemodun/X4-XMLDiffAndPatch/issues/8) ([e31b246](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/e31b2462a74c135a27a80984b2d6d6f9e861dafc))

## [0.2.17](https://github.com/chemodun/X4-XMLDiffAndPatch/compare/v0.2.16...v0.2.17) (2025-02-25)


### Bug Fixes

* **XMLPatch:** add checks to prevent duplicate elements during addition ([89b7626](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/89b76263d6d59886848783a7a905cc5ae6ed0071))

## [0.2.16](https://github.com/chemodun/X4-XMLDiffAndPatch/compare/v0.2.15...v0.2.16) (2025-02-24)


### Bug Fixes

* **XMLPatch:** fixed replace operation for elements ([817299a](https://github.com/chemodun/X4-XMLDiffAndPatch/commit/817299adb27cff58b3bb13159111afc774ddb706))

## [0.2.15](https://github.com/chemodun/X4_XMLDiffAndPatch/compare/v0.2.14...v0.2.15) (2025-02-24)


### Bug Fixes

* enhance logging configuration with append option and improve debug messages ([3ccb99f](https://github.com/chemodun/X4_XMLDiffAndPatch/commit/3ccb99f20ba3c287afee2fe54baeb91fa0e70d80))

## [0.2.14](https://github.com/chemodun/x4_XMLDiffAndPatch/compare/v0.2.13...v0.2.14) (2025-01-17)


### Bug Fixes

* removed the git commit information from the version info ([6d8e803](https://github.com/chemodun/x4_XMLDiffAndPatch/commit/6d8e803415c0155b400cde475ff129e6accf9a8f))

## 0.2.13 (2025-01-16)


### Features

* refactor from Python to C#. XMLDiff going first ([a4c909a](https://github.com/chemodun/x4_XMLDiffAndPatch/commit/a4c909a1fdf4999f0e8a1ea78ac92f1008f62ffa))
* **XMLDiff:** added command line parameter "--only-full-path" ([409deb9](https://github.com/chemodun/x4_XMLDiffAndPatch/commit/409deb97500d0f31781b6ffad96005b71e41bb9a))
* **XMLDiff:** added new command line parameter --use-all-attribute, means to use all element attribute in xpath ([42dc194](https://github.com/chemodun/x4_XMLDiffAndPatch/commit/42dc19439c2f149a5d7c3ca164eacfd8c2570abf))


### Bug Fixes

* **diff:** fix XPath generation ([e3b0726](https://github.com/chemodun/x4_XMLDiffAndPatch/commit/e3b0726ee1e5636c6196ad85b57de13a6d279bb1))
* **patch:** add XMLPatch project and update solution structure ([2b2294f](https://github.com/chemodun/x4_XMLDiffAndPatch/commit/2b2294f198f7aba7a674324b26e513ff8cb8044c))
* **XMLDiff:** correct matchedEnough logic for child element comparison to avoid infinity looping ([e4de925](https://github.com/chemodun/x4_XMLDiffAndPatch/commit/e4de925b7023c4ce1f07e36bcc5d4aad7541c3fb))
* **XMLDiff:** if modified children is only by 1 more then original ones, the difference is not detected ... ([f701cda](https://github.com/chemodun/x4_XMLDiffAndPatch/commit/f701cda64143ad3318d4583dcd678cd3c59a006c))
* **XMLDiff:** improve child element comparison to handle edge cases and prevent incorrect matches ([409deb9](https://github.com/chemodun/x4_XMLDiffAndPatch/commit/409deb97500d0f31781b6ffad96005b71e41bb9a))
* **XMLDiff:** refactor add operation logic to handle cases with no original last child, i.e. no child at all and ensure proper addition of modified children ([10ccea8](https://github.com/chemodun/x4_XMLDiffAndPatch/commit/10ccea815ee516c2a094df185facf413250b732e))
* **XMLDiff:** return back the Indent detection and write ([1abd530](https://github.com/chemodun/x4_XMLDiffAndPatch/commit/1abd530bcb8a8b928b6e415ad3ea5d857a22b20e))
* **XMLPatch:** default add pos now is append ([1abd530](https://github.com/chemodun/x4_XMLDiffAndPatch/commit/1abd530bcb8a8b928b6e415ad3ea5d857a22b20e))
* **XMLPatch:** enhance ApplyAdd method to use the XPathSelectElements with avoiding multiple node processing  and implement attribute addition logic via "type" ([f6af80a](https://github.com/chemodun/x4_XMLDiffAndPatch/commit/f6af80a0969fda7394108b09e7613c2b99dc3a01))
* **XMLPatch:** fix overwriting the diff files by result of patching ([55c512b](https://github.com/chemodun/x4_XMLDiffAndPatch/commit/55c512b0e31e8b312403001524dfd7bc9a1a4f05))
* **XMLPatch:** return back the Indent detection and write ([1abd530](https://github.com/chemodun/x4_XMLDiffAndPatch/commit/1abd530bcb8a8b928b6e415ad3ea5d857a22b20e))
* **XMLPatch:** use XmlReader only to check diff xml, but not read, to prevent formatting(indentation) issue when elements from diff is inserted into the original ([cf94be7](https://github.com/chemodun/x4_XMLDiffAndPatch/commit/cf94be7d0c3a7387f0fc938018ed21461c47d400))


### Miscellaneous Chores

* release 0.2.13 ([fe176e6](https://github.com/chemodun/x4_XMLDiffAndPatch/commit/fe176e6f792434dea0adca576a5f725f8949d0e2))
