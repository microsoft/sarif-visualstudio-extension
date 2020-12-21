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
                    this.NotifyPropertyChanged();
                }
            }
        }

        public int Offset => this.IsTextReplacement
            ? this.region.CharOffset
            : this.region.ByteOffset;

        public int DeletedLength => this.IsTextReplacement
            ? this.region.CharLength
            : this.region.ByteLength;

        public byte[] InsertedBytes
        {
            get
            {
                return this._insertedBytes;
            }
            set
            {
                if (value != this._insertedBytes)
                {
                    this._insertedBytes = value;

                    this.NotifyPropertyChanged();
                }
            }
        }

        public string InsertedString
        {
            get
            {
                return this._insertedString;
            }
            set
            {
                if (value != this._insertedString)
                {
                    this._insertedString = value;

                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool IsTextReplacement => this.Region.CharOffset >= 0;

        public bool IsBinaryReplacement => !this.IsTextReplacement && this.Region.ByteOffset >= 0;

        /// <summary>
        /// A persistent span that represents the range of bytes replaced by this object.
        /// </summary>
        public IPersistentSpan PersistentSpan { get; set; }
    }
}
