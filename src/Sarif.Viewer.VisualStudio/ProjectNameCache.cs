// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System.Collections.Generic;

using EnvDTE;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer
{
    public sealed class ProjectNameCache
    {
        private readonly Dictionary<string, string> projectNames = new Dictionary<string, string>();

        private readonly Solution solution;

        public ProjectNameCache(Solution solution)
        {
            this.solution = solution;
        }

        public string GetName(string fileName)
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108 // Assert thread affinity unconditionally
            }

            this.SetName(fileName);
            return this.projectNames[fileName];
        }

        private void SetName(string fileName)
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108 // Assert thread affinity unconditionally
            }

            if (this.projectNames.ContainsKey(fileName))
            {
                return;
            }

            ProjectItem project = this.solution?.FindProjectItem(fileName);
            if (project?.ContainingProject != null)
            {
                this.projectNames[fileName] = project.ContainingProject.Name;
            }
            else
            {
                this.projectNames[fileName] = string.Empty;
            }
        }
    }
}
