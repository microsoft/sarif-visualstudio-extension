// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.ViewModels
{
    /// <summary>
    /// This type is only used by the VS designer. It provides the data which is
    /// displayed in the designer.
    /// </summary>
    internal static class ViewModelLocator
    {
        private static readonly object _syncroot = new object();
        private static SarifErrorListItem _designTime;

        // This is the view model displayed by the Visual Studio designer.
        public static SarifErrorListItem DesignTime
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (_designTime == null)
                {
                    lock (_syncroot)
                    {
                        if (_designTime == null)
                        {
                            _designTime = GetDesignTimeViewModel1();
                        }
                    }
                }

                return _designTime;
            }
        }

        private static SarifErrorListItem GetDesignTimeViewModel1()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var viewModel = new SarifErrorListItem
            {
                Message = "Potential mismatch between sizeof and countof quantities. Use sizeof() to scale byte sizes.",

                Tool = new ToolModel()
                {
                    Name = "FxCop",
                    Version = "1.0.0.0",
                },

                Rule = new RuleModel()
                {
                    Id = "CA1823",
                    Name = "Avoid unused private fields",
                    HelpUri = "http://aka.ms/analysis/ca1823",
                    DefaultFailureLevel = FailureLevel.None,
                },

                Invocation = new InvocationModel()
                {
                    CommandLine = @"""C:\Temp\Foo.exe"" target.file /o out.sarif",
                    FileName = @"C:\Temp\Foo.exe",
                },
            };

            viewModel.Locations.Add(new LocationModel(resultId: 0, runIndex: 0)
            {
                FilePath = @"D:\GitHub\NuGet.Services.Metadata\src\Ng\Catalog2Dnx.cs",
                Region = new Region(11, 1, 11, 2, 0, 0, 0, 0, snippet: null, message: null, sourceLanguage: "en-US", properties: null),
            });

            viewModel.Locations.Add(new LocationModel(resultId: 0, runIndex: 0)
            {
                FilePath = @"D:\GitHub\NuGet.Services.Metadata\src\Ng\Catalog2Dnx.cs",
                Region = new Region(12, 1, 12, 2, 0, 0, 0, 0, snippet: null, message: null, sourceLanguage: "en-US", properties: null),
            });

            viewModel.RelatedLocations.Add(new LocationModel(resultId: 0, runIndex: 0)
            {
                FilePath = @"D:\GitHub\NuGet.Services.Metadata\src\Ng\Catalog2Dnx.cs",
                Region = new Region(21, 1, 21, 2, 0, 0, 0, 0, snippet: null, message: null, sourceLanguage: "en-US", properties: null),
            });

            viewModel.RelatedLocations.Add(new LocationModel(resultId: 0, runIndex: 0)
            {
                FilePath = @"D:\GitHub\NuGet.Services.Metadata\src\Ng\Catalog2Dnx.cs",
                Region = new Region(22, 1, 22, 2, 0, 0, 0, 0, snippet: null, message: null, sourceLanguage: "en-US", properties: null),
            });

            viewModel.RelatedLocations.Add(new LocationModel(resultId: 0, runIndex: 0)
            {
                FilePath = @"D:\GitHub\NuGet.Services.Metadata\src\Ng\Catalog2Dnx.cs",
                Region = new Region(23, 1, 23, 2, 0, 0, 0, 0, snippet: null, message: null, sourceLanguage: "en-US", properties: null),
            });

            viewModel.AnalysisSteps.Add(new AnalysisStep(
                new List<AnalysisStepNode>
                {
                    new AnalysisStepNode(resultId: 0, runIndex: 0)
                    {
                        Location = new ThreadFlowLocation(),
                    },

                    new AnalysisStepNode(resultId: 0, runIndex: 0)
                    {
                        Location = new ThreadFlowLocation(),
                        Children = new List<AnalysisStepNode>
                        {
                            new AnalysisStepNode(resultId: 0, runIndex: 0)
                            {
                                Location = new ThreadFlowLocation(),
                            },
                        },
                    },
                }));

            var stack1 = new StackCollection("Stack A1")
            {
                new StackFrameModel(resultId: 0, runIndex: 0)
                {
                    Message = "Message A1.1",
                    FilePath = @"D:\GitHub\NuGet.Services.Metadata\src\Ng\Catalog2Dnx.cs",
                    Line = 11,
                    Column = 1,
                    FullyQualifiedLogicalName = "My.Assembly.Main(string[] args)",
                    Module = "My.Module.dll",
                },
                new StackFrameModel(resultId: 0, runIndex: 0)
                {
                    Message = "Message A1.2",
                    FilePath = @"D:\GitHub\NuGet.Services.Metadata\src\Ng\Catalog2Dnx.cs",
                    Line = 12,
                    Column = 1,
                    FullyQualifiedLogicalName = "Our.Shared.Library.Method(int param)",
                    Module = "My.Module.dll",
                },
                new StackFrameModel(resultId: 0, runIndex: 0)
                {
                    Message = "Message A1.3",
                    FilePath = @"D:\GitHub\NuGet.Services.Metadata\src\Ng\Catalog2Dnx.cs",
                    Line = 1,
                    Column = 1,
                    FullyQualifiedLogicalName = "Your.PIA.External()",
                },
            };
            viewModel.Stacks.Add(stack1);

            return viewModel;
        }
    }
}
