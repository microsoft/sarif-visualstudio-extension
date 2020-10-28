// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
	/// <summary>
	/// This class is used to expose the list of the IDs of the commands implemented
	/// by this package. This list of IDs must match the set of IDs defined inside the
	/// Buttons section of the VSCT file.
	/// </summary>
	internal static class SariferPackageCommandIds
	{
		// Now define the list a set of public static members.
		public const int cmdidMyCommand = 0x2001;
	}
}
