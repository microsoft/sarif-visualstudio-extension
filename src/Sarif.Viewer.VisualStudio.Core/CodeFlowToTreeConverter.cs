// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.VisualStudio
{
    internal static class CodeFlowToTreeConverter
    {
        internal static List<AnalysisStepNode> Convert(CodeFlow codeFlow, Run run, int resultId, int runIndex)
        {
            var root = new AnalysisStepNode(resultId: resultId, runIndex: runIndex)
            {
                Children = new List<AnalysisStepNode>(),
            };

            ThreadFlow threadFlow = codeFlow.ThreadFlows?[0];

            if (threadFlow != null)
            {
                int lastNestingLevel = 0;
                int index = 0;
                AnalysisStepNode lastParent = root;
                AnalysisStepNode lastNewNode = null;

                foreach (ThreadFlowLocation location in threadFlow.Locations)
                {
                    ArtifactLocation artifactLocation = location.Location?.PhysicalLocation?.ArtifactLocation;

                    if (artifactLocation != null)
                    {
                        Uri uri = location.Location?.PhysicalLocation?.ArtifactLocation?.Uri;

                        if (uri == null && artifactLocation.Index > -1)
                        {
                            artifactLocation.Uri = run.Artifacts[artifactLocation.Index].Location.Uri;
                        }
                    }

                    var newNode = new AnalysisStepNode(resultId: resultId, runIndex: runIndex, index: location.Index == 0 ? ++index : location.Index)
                    {
                        Location = location,
                        Children = new List<AnalysisStepNode>(),
                    };

                    if (location.NestingLevel > lastNestingLevel)
                    {
                        // The previous node was a call, so this new node's parent is that node
                        lastParent = lastNewNode;
                    }
                    else if (location.NestingLevel < lastNestingLevel)
                    {
                        // The previous node was a return, so this new node's parent is the previous node's grandparent
                        lastParent = lastNewNode.Parent.Parent;
                    }

                    newNode.Parent = lastParent;
                    lastParent.Children.Add(newNode);
                    lastNewNode = newNode;
                    lastNestingLevel = location.NestingLevel;
                }

                root.Children.ForEach(n => n.Parent = null);
            }

            return root.Children;
        }

        internal static List<AnalysisStepNode> ToFlatList(CodeFlow codeFlow, Run run, SarifErrorListItem sarifErrorListItem, int runIndex)
        {
            var results = new List<AnalysisStepNode>();

            ThreadFlow threadFlow = codeFlow.ThreadFlows?[0];

            if (threadFlow != null)
            {
                // nestingLevel is an integer value which is greater than or equals to 0
                // according to schema http://json.schemastore.org/sarif-2.1.0-rtm.5.
                // min nesting level can be used to offset nesting level starts from value greater than 0.e
                int minNestingLevel = threadFlow.Locations.Min(l => l.NestingLevel);
                int index = 0;

                foreach (ThreadFlowLocation location in threadFlow.Locations)
                {
                    ArtifactLocation artifactLocation = location.Location?.PhysicalLocation?.ArtifactLocation;

                    if (artifactLocation != null
                        && artifactLocation.Uri == null
                        && artifactLocation.Index > -1)
                    {
                        artifactLocation.Uri = run.Artifacts[artifactLocation.Index].Location.Uri;
                    }

                    var newNode = new AnalysisStepNode(
                        resultId: sarifErrorListItem?.ResultId ?? 0,
                        runIndex: runIndex,
                        index: location.Index == -1 ? ++index : location.Index,
                        resultGuid: sarifErrorListItem?.ResultGuid,
                        ruleId: sarifErrorListItem.Rule?.Id)
                    {
                        Location = location,
                        Children = new List<AnalysisStepNode>(),
                        NestingLevel = location.NestingLevel - minNestingLevel,
                        State = new ObservableCollection<AnalysisStepState>(ConvertToAnalysisStepState(location)),
                    };

                    results.Add(newNode);
                }
            }

            return results;
        }

        internal static IEnumerable<AnalysisStepState> ConvertToAnalysisStepState(ThreadFlowLocation location)
        {
            IDictionary<string, MultiformatMessageString> states = location?.State;
            if (states?.Any() == true)
            {
                foreach (string key in states.Keys)
                {
                    yield return new AnalysisStepState(key, states[key]?.Text);
                }
            }
        }
    }
}
