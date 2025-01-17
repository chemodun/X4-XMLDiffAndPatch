# Changelog

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
