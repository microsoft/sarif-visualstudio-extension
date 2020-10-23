// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.VisualStudio.Text;

namespace Microsoft.Sarif.Viewer.Models
{
    public class ReplacementModel : NotifyPropertyChangedObject
    {
        private Region region;
        private byte[] _insertedBytes;
        private string _insertedString;

        public Region Region
        {
            get
            {
                return this.region;
            }

            set
            {
                if (!(this.region?.ValueEquals(value) == true))
                {
                    this.region = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public int Offset => IsTextReplacement
            ? this.region.CharOffset
            : this.region.ByteOffset;

        public int DeletedLength => IsTextReplacement
            ? this.region.CharLength
            : this.region.ByteLength;

        public byte[] InsertedBytes
        {
            get
            {
                return _insertedBytes;
            }
            set
            {
                if (value != this._insertedBytes)
                {
                    _insertedBytes = value;

                    NotifyPropertyChanged();
                }
            }
        }

        public string InsertedString
        {
            get
            {
                return _insertedString;
            }
            set
            {
                if (value != this._insertedString)
                {
                    _insertedString = value;

                    NotifyPropertyChanged();
                }
            }
        }

        public bool IsTextReplacement => InsertedString != null;

        public bool IsBinaryReplacement => InsertedBytes != null;

        /// <summary>
        /// A persistent span that represents the range of bytes replaced by this object.
        /// </summary>
        public IPersistentSpan PersistentSpan { get; set; }
    }
}
