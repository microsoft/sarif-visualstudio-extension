# SARIF Viewer Visual Studio extension Release History

## **v2.0.0** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* Updated SDK compatibility with [v2.0.0-csd.2.beta.2019-01-09](https://www.nuget.org/packages/Sarif.Sdk/2.0.0-csd.2.beta.2019-01-09)
* FEATURE: Cleaned up the Stacks view in the tool window
* BUGFIX: Rebasing of file paths based on both user selection and originalUriBaseIds now works correctly
* BUGFIX: Handling of regions now works correctly
* BUGFIX: Clear text markers on file load
* BUGFIX: Corrected handling of multiple-sentence result messages
* BUGFIX: Correct support for multiple runs

## **v2.1.0** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* Updated SDK compatibility with [v2.1.0-rtm.0](https://www.nuget.org/packages/Sarif.Sdk/2.1.0)
* Verified Visual Studio 2019 compatibility
* BUGFIX: Crash when trying to rebase a file path without a uriBaseId

## **v2.1.1** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* BUGFIX: Crash when an artifactLocation has a null uri property

## **v2.1.2** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* FEATURE: add support for embedded hyperlinks in the error list that point to web URLs
