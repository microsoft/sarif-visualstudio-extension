// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif.Converters;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class ConverterResourceTests : SarifViewerPackageUnitTests
    {
        [Fact]
        public void VerifyToolFormatOpenLogFileResourcesExist()
        {
            FieldInfo[] toolFormatFieldInfos = typeof(ToolFormat).GetFields();
            foreach (FieldInfo fieldInfo in toolFormatFieldInfos)
            {
                string filterString = Resources.ResourceManager.GetString(string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", OpenLogFileCommands.FilterResourceNamePrefix, fieldInfo.Name, OpenLogFileCommands.FilterResourceNameSuffix), CultureInfo.CurrentCulture);
                filterString.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public void VerifyOpenLogFileResourceExistInToolFormat()
        {
            foreach (PropertyInfo resourcePropertyInfo in typeof(Resources).GetProperties())
            {
                // The ImportNoneFilter corresponds to the SARIF file format which is consistent
                // with how the import logic behaves.
                if (resourcePropertyInfo.Name.StartsWith(OpenLogFileCommands.FilterResourceNamePrefix, StringComparison.Ordinal) &&
                    resourcePropertyInfo.Name.EndsWith(OpenLogFileCommands.FilterResourceNameSuffix, StringComparison.Ordinal))
                {
                    string fieldName = resourcePropertyInfo.Name.Substring(
                        OpenLogFileCommands.FilterResourceNamePrefix.Length,
                        resourcePropertyInfo.Name.Length - (OpenLogFileCommands.FilterResourceNamePrefix.Length + OpenLogFileCommands.FilterResourceNameSuffix.Length));
                    FieldInfo field = typeof(ToolFormat).GetField(fieldName);
                    field.Should().NotBeNull();
                }
            }
        }
    }
}
