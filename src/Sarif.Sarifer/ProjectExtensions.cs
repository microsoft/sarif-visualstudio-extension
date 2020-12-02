// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using EnvDTE;

using Microsoft.VisualStudio;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal static class ProjectExtensions
    {
        public static List<string> GetMemberFiles(this Project project)
        {
            VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            var memberFiles = new List<string>();

            ProjectItems projectItems = project.ProjectItems;
            for (int i = 0; i < projectItems.Count; ++i)
            {
                ProjectItem projectItem = projectItems.Item(i + 1); // One-based index.
                for (short j = 0; j < projectItem.FileCount; ++j)
                {
                    string memberFile = null;

                    // Certain project items use 0-based indexing for their file names, while
                    // others use 1-based indexing. Do our best to get it right, but catch and
                    // ignore any exceptions if we get it wrong.
                    // https://stackoverflow.com/questions/34884079/how-to-get-a-file-path-from-a-projectitem-via-the-filenames-property
                    var projItemGuid = new Guid(projectItem.Kind);
                    try
                    {
                        memberFile = projItemGuid == VSConstants.GUID_ItemType_PhysicalFile
                            ? projectItem.FileNames[0]
                            : projectItem.FileNames[1];
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to index into projectItem.FileNames. index = {j}, exception = {ex}");
                    }

                    // Make sure it's a file and not a directory.
                    if (memberFile != null && File.Exists(memberFile))
                    {
                        memberFiles.Add(memberFile);
                    }
                }
            }

            return memberFiles;
        }
    }
}
