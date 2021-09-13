// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    // A "stub" data source whose sole purpose is to cause the Supression State column
    // to be displayed. It never actually adds entries to the Error List.
    //
    // For an explanation of why these this is necessary, see the comment near the top
    // of SarifResultTableEntry.cs
    internal class SuppressionStateTableDataSource : SarifTableDataSourceBase
    {
        private static SuppressionStateTableDataSource _instance;

        private SuppressionStateTableDataSource()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.Initialize(SarifResultTableEntry.SuppressedResultColumns);
        }

        public static SuppressionStateTableDataSource Instance =>
            _instance ?? (_instance = new SuppressionStateTableDataSource());

        public override string Identifier => $"{Guids.GuidVSPackageString}-{nameof(SuppressionStateTableDataSource)}";

        public override string DisplayName => Resources.ErrorListSuppressedResultsDataSourceDisplayName;

        public override IDisposable Subscribe(ITableDataSink sink) => new StubDisposable();
    }
}
