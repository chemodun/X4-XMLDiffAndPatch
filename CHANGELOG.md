# Changelog

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
