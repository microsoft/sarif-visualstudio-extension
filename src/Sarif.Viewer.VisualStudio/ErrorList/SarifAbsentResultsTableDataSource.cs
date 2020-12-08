﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    // A "stub" data source whose sole purpose is to cause the Category column
    // to be displayed. It never actually adds entries to the Error List.
    //
    // For an explanation of why these this source is necessary, see the comment
    // near the top of SarifResultTableEntry.cs
    internal class SarifAbsentResultsTableDataSource : SarifTableDataSourceBase
    {
        private static SarifAbsentResultsTableDataSource _instance;

        private SarifAbsentResultsTableDataSource()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.Initialize(SarifResultTableEntry.AbsentResultColumns);
        }

        public static SarifAbsentResultsTableDataSource Instance =>
            _instance ?? (_instance = new SarifAbsentResultsTableDataSource());

        public override string Identifier => $"{Guids.GuidVSPackageString}-{nameof(SarifAbsentResultsTableDataSource)}";

        public override string DisplayName => Resources.ErrorListAbsentResultsDataSourceDisplayName;

        public override IDisposable Subscribe(ITableDataSink sink) => new StubDisposable();
    }
}
