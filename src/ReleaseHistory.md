# SARIF Viewer Visual Studio extension Release History

## To Be Released
## [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: Error list expando for long message does not work.
* FEATURE: Generate Popup from Xaml.

## [SARIFER](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifer)
* Publish official release to marketplace.
* Enable Spam Analyzer Dynamic Mode.

## **3.0.66.61165** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: Not able to navigate to source file under solution.

## **3.0.59** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: Enable navigation for code flow / thread flow locations.

## **v3.0.42** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: Align suppression logic with SARIF-SDK.

## **v3.0.38** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: Handle empty temporary solution path.

## **v3.0.25** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: Checking if file hash matches.

## **v3.0.16** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* Update submodule version.
* Fix test failure.

## **v2.1.20** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* Implement light bulb fixes for SARIF errors.
* Build for .NET Framework 4.7.2 rather than 4.6.1. This is in preparation for adding some features which require NuGet packages which support only 4.7.2.

## **v2.1.19** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: Ensure telemetry is properly initialized and delivered to Application Insights. [#216](https://github.com/microsoft/sarif-visualstudio-extension/issues/216)
* BUGFIX: Consolidate import static analysis logs as SARIF into one menu option and actually open resulting SARIF log in the editor. [#217](https://github.com/microsoft/sarif-visualstudio-extension/issues/217)

## **v2.1.18** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: Fix crash when closing and re-opening a file with tagged SARIF results. [#205](https://github.com/microsoft/sarif-visualstudio-extension/issues/205)
* BUGFIX: Ensure that SARIF explorer properly selects code-flow location when navigating caret in editor. [#198](https://github.com/microsoft/sarif-visualstudio-extension/issues/198)
* FEATURE: Added tool-tips for SARIF results and code-flow locations. [#189](https://github.com/microsoft/sarif-visualstudio-extension/issues/189)

## **v2.1.17** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: Display non-failure results appropriately [#199](https://github.com/microsoft/sarif-visualstudio-extension/issues/199)
* BUGFIX: Double-clicking result does not navigate to source file [#201](https://github.com/microsoft/sarif-visualstudio-extension/issues/201)
* FEATURE: A new script `New-AtomXml.ps` allows you to create a private Atom feed for pre-release versions of the viewer.

## **v2.1.16** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: Offload file reading and parsing from Visual Studio's UI thread. [#160](https://github.com/microsoft/sarif-visualstudio-extension/issues/160)

## **v2.1.15** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: Don't throw exceptions if text buffer has no file name, and properly highlight code analysis flows. [#193](https://github.com/microsoft/sarif-visualstudio-extension/issues/193)

## **v2.1.14** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: Prevent event loops when navigating editor and selecting items in SARIF tool window. [#183](https://github.com/microsoft/sarif-visualstudio-extension/issues/185), [#183](https://github.com/microsoft/sarif-visualstudio-extension/issues/185)

## **v2.1.13** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: Bump version and ensure all assemblies have correct version. [#180](https://github.com/microsoft/sarif-visualstudio-extension/issues/180)

## **v2.1.12** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: Handle multi-select from Visual Studio's open file dialog. [#174](https://github.com/microsoft/sarif-visualstudio-extension/issues/174), [#170](https://github.com/microsoft/sarif-visualstudio-extension/issues/170), [#169](https://github.com/microsoft/sarif-visualstudio-extension/issues/169), [#168](https://github.com/microsoft/sarif-visualstudio-extension/issues/168)

## **v2.1.11** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: Ensure "close logs" API actually clears errors from error list. [#172](https://github.com/microsoft/sarif-visualstudio-extension/issues/172)

## **v2.1.10** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: Ensure "load logs" API shows results from all log files. [#163](https://github.com/microsoft/sarif-visualstudio-extension/issues/163)

## **v2.1.9** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* FEATURE: Open and close multiple SARIF files at once.
* FEATURE: Display messages for locations and related locations in the Locations tab.
* FEATURE: Updated to Sarif SDK 2.2.5
* BUGFIX: Display locations and related locations in the correct order in the Locations tab.
* BUGFIX: Display default rule level in the Info tab. [#92](https://github.com/microsoft/sarif-visualstudio-extension/issues/92)
* BUGFIX: Navigate to inline links with integer targets even if the target location has no region.
* BUGFIX: Ensure trailing slash on `originalUriBaseIds`. [#127](https://github.com/microsoft/sarif-visualstudio-extension/issues/127)

## **v2.1.8** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: Make embedded links with a slash in the anchor text work. [#118](https://github.com/microsoft/sarif-visualstudio-extension/issues/118)

## **v2.1.7** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: Don't claim support for VS 2015. [#110](https://github.com/microsoft/sarif-visualstudio-extension/issues/110)

## **v2.1.6** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: The viewer crashed when navigating to a result with relative path and no `uriBaseId`. [#106](https://github.com/microsoft/sarif-visualstudio-extension/issues/106)

## **v2.1.5** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: The open file dialog did not offer an "All files" ("*.*") option. [#104](https://github.com/microsoft/sarif-visualstudio-extension/issues/104)

## **v2.1.4** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: SARIF files with empty URI properties would not open in the viewer. [#85](https://github.com/microsoft/sarif-visualstudio-extension/issues/85)

## **v2.1.3** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: Some valid SARIF files would not open in the viewer. [#98](https://github.com/microsoft/sarif-visualstudio-extension/issues/98)

## **v2.1.2** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* FEATURE: add support for embedded hyperlinks in the error list that point to web URLs
* Updated SDK compatibility with [v2.1.0-rtm.2](https://www.nuget.org/packages/Sarif.Sdk/2.1.2)
* Optimized file loading, transform, and SARIF version check logic

## **v2.1.1** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: Crash when an artifactLocation has a null uri property

## **v2.1.0** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* Updated SDK compatibility with [v2.1.0-rtm.0](https://www.nuget.org/packages/Sarif.Sdk/2.1.0)
* Verified Visual Studio 2019 compatibility
* BUGFIX: Crash when trying to rebase a file path without a uriBaseId

## **v2.0.0** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* Updated SDK compatibility with [v2.0.0-csd.2.beta.2019-01-09](https://www.nuget.org/packages/Sarif.Sdk/2.0.0-csd.2.beta.2019-01-09)
* FEATURE: Cleaned up the Stacks view in the tool window
* BUGFIX: Rebasing of file paths based on both user selection and originalUriBaseIds now works correctly
* BUGFIX: Handling of regions now works correctly
* BUGFIX: Clear text markers on file load
* BUGFIX: Corrected handling of multiple-sentence result messages
* BUGFIX: Correct support for multiple runs
