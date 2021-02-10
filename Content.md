This release of the Microsoft SARIF Viewer extension for Visual Studio is compatible with the final OASIS Committee Specification version of the SARIF file format.

The Microsoft SARIF Viewer extension for Visual Studio provides a convenient UI for analyzing static analysis log files and taking action on the items they contain. The SARIF Viewer integrates with the Visual Studio environment, displaying a list of analysis results in the Error List and result details in a dockable tool window.

# Features
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
- Open SARIF log files in the SARIF Viewer from your own Visual Studio extension using the [SARIF Viewer Interop Library](https://www.nuget.org/packages/Sarif.Viewer.VisualStudio.Interop/2.0.0-csd.1.0.3)

# Installation
1. In Visual Studio 2109, select menu item **Extensions** > **Manage Extensions**.
1. In the tree view, select the **Online** node.
1. In the **Search** text box, type "sarif" and then press ENTER.
1. In the Microsoft SARIF Viewer tile, select **Download**.

# Usage
To open a SARIF log, or another supported log format (see below), use the "Import Static Analysis Log File to Error List" flyout on the Tools menu. You can also open .sarif files using **File** > **Open**, or by dragging and dropping into the Visual Studio editor window.

# Supported Static Analysis Formats
The SARIF Viewer for Visual Studio can convert the following log file formats to SARIF:
- Android Studio
- Clang
- CppCheck
- Fortify
- Fortify FPR
- FxCop
- PREfast
- Pylint
- Semmle
- Static Driver Verifier
- TSLint

# Feedback
If you encounter issues, or have thoughts you'd like to share, please visit https://github.com/Microsoft/sarif-visualstudio-extension/issues.