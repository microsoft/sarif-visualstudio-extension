// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using FluentAssertions;
    using Microsoft.CodeAnalysis.Sarif.Converters;
    using Xunit;

    public class ConverterResourceTests : SarifViewerPackageUnitTests
    {
        private const string FilterResourceNamePrefix = "Import";
        private const string FilterResourceNameSuffix = "Filter";

        [Fact]
        public void VerifyToolFormatOpenLogFileResourcesExist()
        {
            FieldInfo[] toolFormatFieldInfos = typeof(ToolFormat).GetFields();
            foreach (FieldInfo fieldInfo in toolFormatFieldInfos)
            {
                // We do not add the "none" tool format to the open log dialog so it is not expected to exist.
                if (fieldInfo.Name.Equals(ToolFormat.None, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string filterString = Resources.ResourceManager.GetString(string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", FilterResourceNamePrefix, fieldInfo.Name, FilterResourceNameSuffix), CultureInfo.CurrentCulture);
                filterString.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public void VerifyOpenLogFileResourceExistInToolFormat()
        {
            foreach (PropertyInfo resourcePropertyInfo in typeof(Resources).GetProperties())
            {
                // The SARIF filter in the open log dialog is added explicitly and is not in the tool format
                // class.
                if (resourcePropertyInfo.Name.StartsWith(FilterResourceNamePrefix, StringComparison.OrdinalIgnoreCase) &&
                    resourcePropertyInfo.Name.EndsWith(FilterResourceNameSuffix, StringComparison.OrdinalIgnoreCase) &&
                    !resourcePropertyInfo.Name.Equals(nameof(Resources.ImportSARIFFilter)))
                {
                    string fieldName = resourcePropertyInfo.Name.Substring(
                        FilterResourceNamePrefix.Length, 
                        resourcePropertyInfo.Name.Length - (FilterResourceNamePrefix.Length + FilterResourceNameSuffix.Length));
                    FieldInfo field = typeof(ToolFormat).GetField(fieldName);
                    field.Should().NotBeNull();
                }
            }
        }
    }
}
