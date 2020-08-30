// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;
using System.ComponentModel;

namespace Microsoft.Sarif.Viewer.Tags
{
    internal class SarifTag : ISarifTag
    {
        private TextMarkerTag textMarkerTag;

        public SarifTag(IPersistentSpan persistentSpan, TextSpan initialSpan, TextMarkerTag textMarkerTag)
        {
            this.PersistentSpan = persistentSpan;
            this.textMarkerTag = textMarkerTag;
            this.InitialSpan = initialSpan;
        }

        public IPersistentSpan PersistentSpan { get; }

        public TextSpan InitialSpan { get; }

        public TextMarkerTag Tag
        {
            get => this.textMarkerTag;

            set
            {
                if (value != this.textMarkerTag)
                {
                    this.textMarkerTag = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Tag)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
