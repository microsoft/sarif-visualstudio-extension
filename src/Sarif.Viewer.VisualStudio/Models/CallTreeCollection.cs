// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System.Collections.ObjectModel;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.Models
{
    internal class CallTreeCollection : ObservableCollection<CallTree>
    {
        private int _verbosity;
        private DelegateCommand _expandAllCommand;
        private DelegateCommand _collapseAllCommand;
        private DelegateCommand _intelligentExpandCommand;

        public CallTreeCollection()
        {
        }

        public int Verbosity
        {
            get
            {
                return this._verbosity;
            }
            set
            {
                if (!SarifViewerPackage.IsUnitTesting)
                {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                    ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108
                }

                if (this._verbosity != value)
                {
                    this._verbosity = value;

                    ThreadFlowLocationImportance importance;
                    if (this._verbosity >= 200)
                    {
                        importance = ThreadFlowLocationImportance.Unimportant;
                    }
                    else if (this._verbosity >= 100)
                    {
                        importance = ThreadFlowLocationImportance.Important;
                    }
                    else
                    {
                        importance = ThreadFlowLocationImportance.Essential;
                    }

                    this.SetVerbosity(importance);

                    this.SelectVisibleNode();
                }
            }
        }

        public DelegateCommand ExpandAllCommand
        {
            get
            {
                if (this._expandAllCommand == null)
                {
                    this._expandAllCommand = new DelegateCommand(() =>
                    {
                        this.ExpandAll();
                    });
                }

                return this._expandAllCommand;
            }
            set
            {
                this._expandAllCommand = value;
            }
        }

        public DelegateCommand CollapseAllCommand
        {
            get
            {
                if (this._collapseAllCommand == null)
                {
                    this._collapseAllCommand = new DelegateCommand(() =>
                    {
                        this.CollapseAll();
                    });
                }

                return this._collapseAllCommand;
            }
            set
            {
                this._collapseAllCommand = value;
            }
        }

        public DelegateCommand IntelligentExpandCommand
        {
            get
            {
                if (!SarifViewerPackage.IsUnitTesting)
                {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                    ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108
                }

                if (this._intelligentExpandCommand == null)
                {
                    this._intelligentExpandCommand = new DelegateCommand(() =>
                    {
                        this.IntelligentExpand();
                    });
                }

                return this._intelligentExpandCommand;
            }
            set
            {
                this._intelligentExpandCommand = value;
            }
        }

        internal void ExpandAll()
        {
            foreach (CallTree callTree in this)
            {
                callTree.ExpandAll();
            }
        }

        internal void CollapseAll()
        {
            foreach (CallTree callTree in this)
            {
                callTree.CollapseAll();
            }
        }

        internal void IntelligentExpand()
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108
            }
            foreach (CallTree callTree in this)
            {
                CallTreeNode selectedItem = callTree.SelectedItem;

                callTree.IntelligentExpand();

                callTree.SelectedItem = selectedItem;
            }
        }

        internal void SetVerbosity(ThreadFlowLocationImportance importance)
        {
            foreach (CallTree callTree in this)
            {
                callTree.SetVerbosity(importance);
            }
        }

        internal void SelectVisibleNode()
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108
            }

            foreach (CallTree callTree in this)
            {
                if (callTree.SelectedItem != null && callTree.SelectedItem.Visibility != System.Windows.Visibility.Visible)
                {
                    callTree.SelectedItem = callTree.FindPrevious();
                }
            }
        }
    }
}
