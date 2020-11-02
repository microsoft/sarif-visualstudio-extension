// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Text;
using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.Sarif.Viewer.Models
{
    public class ArtifactDetailsModel
    {
        private ArtifactContent _artifactContent;

        private readonly Lazy<string> _decodedContents;

        public ArtifactDetailsModel(Artifact artifact)
        {
            string sha256Hash;
            if (artifact.Hashes.TryGetValue("sha-256", out sha256Hash))
            {
                Sha256Hash = sha256Hash;
            }

            _artifactContent = artifact.Contents;
            _decodedContents = new Lazy<string>(DecodeContents);
        }

        public string Sha256Hash { get; }

        public string GetContents()
        {
            return _decodedContents.Value;
        }

        private string DecodeContents()
        {
            string content = _artifactContent.Text;

            if (content == null)
            {
                byte[] data = Convert.FromBase64String(_artifactContent.Binary);
                content = Encoding.UTF8.GetString(data);
            }

            _artifactContent = null; // Clear _fileContent to save memory.
            return content;
        }
    }
}
