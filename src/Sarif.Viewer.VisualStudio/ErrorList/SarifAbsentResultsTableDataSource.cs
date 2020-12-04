// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    // A "stub" data source whose sole purpose is to cause the Category column
    // to be displayed.
    //
    // For an explanation of why these this source is necessary, see the comment
    // near the top of SarifResultTableEntry.cs
    internal class SarifAbsentResultsTableDataSource : SarifTableDataSourceBase
    {
        private static SarifAbsentResultsTableDataSource _instance;

        private SarifAbsentResultsTableDataSource()
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108 // Assert thread affinity unconditionally

                this.Initialize(SarifResultTableEntry.AbsentResultColumns);
            }
        }

        public static SarifAbsentResultsTableDataSource Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SarifAbsentResultsTableDataSource();
                }

                return _instance;
            }
        }

        public override string Identifier => $"{Guids.GuidVSPackageString}-{nameof(SarifAbsentResultsTableDataSource)}";

        public override string DisplayName => Resources.ErrorListAbsentResultsDataSourceDisplayName;

        public override IDisposable Subscribe(ITableDataSink sink)
        {
            throw new NotImplementedException();
        }
    }
}
