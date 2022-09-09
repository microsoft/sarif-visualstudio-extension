﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("Sarifer test data producer extension for Visual Studio")]
[assembly: AssemblyDescription("Generates test data in SARIF format and provides it to the SARIF viewer extension, upon which it depends.")]

[assembly: NeutralResourcesLanguage("en")]

// This reference necessary for the MOQ test engine.
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("Sarif.Sarifer.UnitTests")]
