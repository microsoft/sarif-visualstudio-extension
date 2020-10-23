// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.VisualStudio.Text;

namespace Microsoft.Sarif.Viewer.Models
{
    public class ReplacementModel : NotifyPropertyChangedObject
    {
        private int _offset;
        private int _deletedLength;
        private byte[] _insertedBytes;
        private string _insertedString;

        public int Offset
        {
            get
            {
                return _offset;
            }
            set
            {
                if (value != this._offset)
                {
                    _offset = value;

                    NotifyPropertyChanged();
                }
            }
        }

        public int DeletedLength
        {
            get
            {
                return _deletedLength;
            }
            set
            {
                if (value != this._deletedLength)
                {
                    _deletedLength = value;

                    NotifyPropertyChanged();
                }
            }
        }

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

        /// <summary>
        /// A persistent span that represents the range of bytes replaced by this object.
        /// </summary>
        public IPersistentSpan PersistentSpan { get; set; }
    }
}
