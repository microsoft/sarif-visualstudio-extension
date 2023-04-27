// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.CodeFinding.Internal.CStyle
{
    public enum ScopeType
    {
        /// <summary>
        /// Indicates this is not actually a scope.
        /// </summary>
        None,

        /// <summary>
        /// This is a scope but the type is unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// This scope is a control block (e.g. "if", "for", "try", etc.).
        /// </summary>
        Control,

        /// <summary>
        /// This scope is a namespace.
        /// </summary>
        Namespace,

        /// <summary>
        /// This scope is a class.
        /// </summary>
        Class,

        /// <summary>
        /// This scope is a struct.
        /// </summary>
        Struct,

        /// <summary>
        /// This scope is a function (or a method).
        /// </summary>
        Function,
    }

    /// <summary>
    /// Represents an identifier for a scope ("scope" being everything within a pair of matching curly braces, "{ }").
    /// For example, in "class Foo { ... }", "Foo" is the identifier.
    /// </summary>
    internal class ScopeIdentifier
    {
        /// <summary>
        /// Gets or sets the name of the identifier itself.
        /// </summary>
        public string Name { get; set;  }

        /// <summary>
        /// Gets or sets the scope's type, if known.
        /// </summary>
        public ScopeType Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether identifier is expected to have an explicit scope.
        /// For example, in C++ "using namespace Foo;" implicitly declares that Foo's scope is the entire file. In this case, Explicit should be false.
        /// However, in C++ we would expect class and method definitions to have an explicit scope.
        /// </summary>
        public bool Explicit { get; set;  }

        public ScopeIdentifier(string name, ScopeType type = ScopeType.Unknown, bool explicitScope = true)
        {
            Name = name;
            Type = type;
            Explicit = explicitScope;
        }
    }
}
