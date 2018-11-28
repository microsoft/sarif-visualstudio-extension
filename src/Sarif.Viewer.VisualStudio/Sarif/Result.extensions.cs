﻿// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.Sarif.Viewer.Sarif
{
    static class ResultExtensions
    {
        const string NOTARGETFILEPATH = "NOTARGETFILEPATH";

        public static string FormatForVisualStudio(this Result result, IRule rule)
        {
            var messageLines = new List<string>();
            foreach (var location in result.Locations)
            {
                Uri uri = location.PhysicalLocation.FileLocation.Uri;
                string path = uri.IsFile ? uri.LocalPath : uri.ToString();
                messageLines.Add(
                    string.Format(
                        CultureInfo.InvariantCulture, "{0}{1}: {2} {3}: {4}",
                        path,
                        location.PhysicalLocation.Region.FormatForVisualStudio(),
                        result.Level.FormatForVisualStudio(),
                        result.RuleId,
                        result.GetMessageText(rule)
                        ));
            }

            return string.Join(Environment.NewLine, messageLines);
        }

        /// <summary>
        /// Gets the path of the primary target file for the given result.
        /// This method should only be called while the runs are being processed.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static string GetPrimaryTargetFile(this Result result)
        {
            if (result == null)
            {
                return null;
            }

            if (result.Locations == null || result.Locations.Count == 0)
            {
                return string.Empty;
            }

            Location primaryLocation = result.Locations[0];
            RunDataCache dataCache = CodeAnalysisResultManager.Instance.CurrentRunDataCache;

            if (primaryLocation.PhysicalLocation?.FileLocation != null)
            {
                Uri uri = primaryLocation.PhysicalLocation.FileLocation.Uri;
                string uriBaseId = primaryLocation.PhysicalLocation.FileLocation.UriBaseId;

                if (!string.IsNullOrEmpty(uriBaseId) && dataCache.OriginalUriBasePaths.ContainsKey(uriBaseId))
                {
                    Uri baseUri = dataCache.OriginalUriBasePaths[uriBaseId];
                    uri = new Uri(baseUri, uri);
                }

                return uri.ToPath();
            }
            else if (primaryLocation.FullyQualifiedLogicalName != null)
            {
                return primaryLocation.FullyQualifiedLogicalName;
            }

            return string.Empty;
        }

        public static Region GetPrimaryTargetRegion(this Result result)
        {
            if (result == null || result.Locations == null || result.Locations.Count == 0)
            {
                return null;
            }

            Location primaryLocation = result.Locations[0];

            if (primaryLocation.PhysicalLocation != null)
            {
                return primaryLocation.PhysicalLocation.Region;
            }
            else
            {
                return null;
            }
        }
    }
}
