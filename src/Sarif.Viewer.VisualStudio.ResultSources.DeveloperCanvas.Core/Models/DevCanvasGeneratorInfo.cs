// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis.Sarif;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Models
{
    /// <summary>
    /// Describes the generator that produced an insight.
    /// </summary>
    public class DevCanvasGeneratorInfo : ToolComponent
    {
        private const string InsightDisplayNameKey = "InsightDisplayName";
        private const string AbbreviationKey = "Abbreviation";

        /// <summary>
        /// A paramterless contructor. Used when setting parameters manually.
        /// </summary>
        public DevCanvasGeneratorInfo() : base() { }

        /// <summary>
        /// Abbreviated 6-character version of 'Name' for use in certain space-limited client-side scenarios.
        /// Should be all-caps and simply represent 'Name' in condensed form.
        /// </summary>
        public string ShortName
        {
            get
            {
                this.TryGetProperty(AbbreviationKey, out string? shortName);
                return shortName;
            }
            set
            {
                this.SetProperty(AbbreviationKey, value);
            }
        }

        /// <summary>
        /// An override of the Fullname property from <see cref="ToolComponent"/>
        /// Appends the version and locale (currently only us) onto the generator name
        /// </summary>
        public override string FullName
        {
            get
            { return $"{this.Name} {this.Version} (en-us)"; }
        }


        /// <summary>
        /// This is the about link where the end-user can get more information about the insight from insight generator.
        /// </summary>
        public string AboutLink
        {
            get { return this.InformationUri.ToString(); }
            set { this.InformationUri = new Uri(value); }
        }

        /// <summary>
        /// Name of the insight in the client
        /// </summary>
        public string InsightDisplayName
        {
            get { return this.GetProperty<string>(InsightDisplayNameKey); }
            set { this.SetProperty<string>(InsightDisplayNameKey, value); }
        }
    }
}
