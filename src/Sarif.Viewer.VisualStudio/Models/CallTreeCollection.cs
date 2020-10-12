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
                return _verbosity;
            }
            set
            {
                if (!SarifViewerPackage.IsUnitTesting)
                {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                    ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108 // Assert thread affinity unconditionally
                }

                if (_verbosity != value)
                {
                    _verbosity = value;

                    ThreadFlowLocationImportance importance;
                    if (_verbosity >= 200)
                    {
                        importance = ThreadFlowLocationImportance.Unimportant;
                    }
                    else if (_verbosity >= 100)
                    {
                        importance = ThreadFlowLocationImportance.Important;
                    }
                    else
                    {
                        importance = ThreadFlowLocationImportance.Essential;
                    }

                    SetVerbosity(importance);

                    SelectVisibleNode();
                }
            }
        }

        public DelegateCommand ExpandAllCommand
        {
            get
            {
                if (_expandAllCommand == null)
                {
                    _expandAllCommand = new DelegateCommand(() => {
                        ExpandAll();
                    });
                }

                return _expandAllCommand;
            }
            set
            {
                _expandAllCommand = value;
            }
        }

        public DelegateCommand CollapseAllCommand
        {
            get
            {
                if (_collapseAllCommand == null)
                {
                    _collapseAllCommand = new DelegateCommand(() =>
                    {
                        CollapseAll();
                    });
                }

                return _collapseAllCommand;
            }
            set
            {
                _collapseAllCommand = value;
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
#pragma warning restore VSTHRD108 // Assert thread affinity unconditionally
                }

                if (_intelligentExpandCommand == null)
                {
                    _intelligentExpandCommand = new DelegateCommand(() =>
                    {
                        IntelligentExpand();
                    });
                }

                return _intelligentExpandCommand;
            }
            set
            {
                _intelligentExpandCommand = value;
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
#pragma warning restore VSTHRD108 // Assert thread affinity unconditionally
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
#pragma warning restore VSTHRD108 // Assert thread affinity unconditionally
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
