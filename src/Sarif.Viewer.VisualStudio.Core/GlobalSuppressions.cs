// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "E.g. RunExtensions class in Run.Extensions.cs. Will fix later.", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "DelegateCommand.cs includes DelegateCommand and DelegateCommand<T>. Will fix later.", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1302:Interface names should begin with I", Justification = "Has interfaces SLoadSarifLogService, SCloseSarifLogService, and SDataService", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:Accessible fields should begin with upper-case letter", Justification = "SarifFileAndContentDefinitions defined 2 fields with export attribute", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Has some c style fields like S_OK / E_FAIL", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1314:Type parameter names should begin with T", Justification = "SdkUIUtilities has secondary generic type S", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Too many moving code around, will fix in follow up PRs", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "Too many moving code around, will fix in follow up PRs", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "Too many moving code around, will fix in follow up PRs", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1214:Readonly fields should appear before non-readonly fields", Justification = "Too many moving code around, will fix in follow up PRs", Scope = "module")]
