# Microsoft SARIF Viewer

[![Build Status](https://dev.azure.com/mseng/1ES/_apis/build/status/Security%20Detections/microsoft.sarif-visualstudio-extension?branchName=main)](https://dev.azure.com/mseng/1ES/_build/latest?definitionId=10703&branchName=main)

The Microsoft SARIF Viewer extension for Visual Studio provides a convenient UI for analyzing static analysis log files and taking action on the items they contain. The SARIF Viewer integrates with the Visual Studio environment, displaying a list of analysis results in the Error List and result details in a dockable tool window.

## Features

- View the set of results from a SARIF log file
- View details about each result, including:
  - Information about the rule that was violated
  - Locations of the defect
  - Code paths and call stacks that lead to the defect
  - Suggested fixes for the defect
  - Details about the static analysis run and the tool that performed it
- Navigate to the defect location in the target file
- Extract target files embedded in the SARIF log
- Preview and apply suggested fixes in the target file with the click of a button
- Automatically transform SARIF v1 logs to v2
- Automatically convert log files from many other static analysis formats
- Open SARIF log files in the SARIF Viewer from your own Visual Studio extension using the [SARIF Viewer Interop Library](https://www.nuget.org/packages/Sarif.Viewer.VisualStudio.Interop)

## Installation

The Microsoft SARIF Viewer extension can be downloaded and installed from the Visual Studio Marketplace.

- [Microsoft SARIF Viewer for Visual Studio 2022](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer2022)
- [Microsoft SARIF Viewer for Visual Studio 2017/2019](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer)

Alternatively, in Visual Studio, open the Extension Manager (Menu: Extensions -> Manage Extensions), search for "Microsoft SARIF Viewer", select the entry, and click on the Download button.

## License

Microsoft SARIF Viewer is licensed under the [MIT license](https://github.com/microsoft/sarif-visualstudio-extension/blob/main/LICENSE).
