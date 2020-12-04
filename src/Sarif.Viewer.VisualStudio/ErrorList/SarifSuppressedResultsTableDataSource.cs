// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    // A "stub" data source whose sole purpose is to cause the Supression State column
    // to be displayed.
    //
    // For an explanation of why these this is necessary, see the comment near the top
    // of SarifResultTableEntry.cs
    internal class SarifSuppressedResultsTableDataSource : SarifTableDataSourceBase
    {
        private static SarifSuppressedResultsTableDataSource _instance;

        private SarifSuppressedResultsTableDataSource()
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108 // Assert thread affinity unconditionally

                this.Initialize(SarifResultTableEntry.SuppressedResultColumns);
            }
        }

        public static SarifSuppressedResultsTableDataSource Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SarifSuppressedResultsTableDataSource();
                }

                return _instance;
            }
        }

        public override string Identifier => $"{Guids.GuidVSPackageString}-{nameof(SarifSuppressedResultsTableDataSource)}";

        public override string DisplayName => Resources.ErrorListSuppressedResultsDataSourceDisplayName;

        public override IDisposable Subscribe(ITableDataSink sink)
        {
            throw new NotImplementedException();
        }
    }
}
