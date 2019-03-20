# SARIF Viewer Visual Studio extension Release History

## **v2.0.0** [SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)
* Updated SDK compatibility with [v2.0.0-csd.2.beta.2019-01-09](https://www.nuget.org/packages/Sarif.Sdk/2.0.0-csd.2.beta.2019-01-09)
* FEATURE: Cleaned up the Stacks view in the tool window
* BUGFIX: Rebasing of file paths based on both user selection and originalUriBaseIds now works correctly
* BUGFIX: handling of regions now works correctly
* BUGFIX: clear text markers on file load
* BUGFIX: corrected handling of multiple-sentence result messages
* BUGFIX: correct support for multiple runs