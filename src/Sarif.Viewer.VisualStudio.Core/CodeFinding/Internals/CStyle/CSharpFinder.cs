// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Sarif.Viewer.CodeFinding.Internal.CStyle
{
    /// <summary>
    /// Finds code in C# files.
    /// </summary>
    internal class CSharpFinder : CStyleFinder
    {
        public CSharpFinder(string fileContents)
            : base(fileContents)
        {
            // List of C# keywords taken from https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/.
            keywords = new HashSet<string>
            {
                "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal",
                "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
                "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null",
                "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte",
                "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
                "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
            };
        }

        /// <summary>
        /// Parses the given function signature and returns a list of identifiers, from largest to smallest scope.
        /// </summary>
        /// <param name="functionSignature">The string representation to parse.</param>
        /// <returns>The list of scope identifiers from largest to smallest.</returns>
        protected override List<ScopeIdentifier> ParseFunctionSignature(string functionSignature)
        {
            var identifiers = new List<ScopeIdentifier>();

            if (string.IsNullOrWhiteSpace(functionSignature))
            {
                return identifiers;
            }

            // C# function signatures typically take the following forms:
            //  "Namespace::Class.Method"
            //  "Namespace1::Namespace2::Namespace3::Class.Method"
            //  "Namespace::Class+_IEnumerableMethod_d__24.MoveNext"
            //  "Namespace.Class1+Class2..ctor"
            // Thus we split the function signature on "::", ".", and "+".
            string[] parts = functionSignature.Split(new string[] { "::", ".", "+" }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
            {
                identifiers.Add(new ScopeIdentifier(functionSignature, ScopeType.Function));
            }
            else if (parts[parts.Length - 2].StartsWith("_") && parts[parts.Length - 1] == "MoveNext")
            {
                // If this method returns an IEnumerable then the last part should be "MoveNext" and the second to last part
                // (which is the actual method) should start with an underscore.
                // E.g. "Namespace.Class+_IEnumerableMethod_d__24.MoveNext"
                // If this is the case then we'll need to do some special handling to make sure the method name
                // is returned correctly.

                if (parts.Length < 4)
                {
                    throw new ArgumentException("Function signature for a method that returns an IEnumerable must at least have Namespace, Class, Method, and \"MoveNext\"");
                }

                for (int i = 0; i < parts.Length - 1; i++)
                {
                    ScopeType typeHint = ScopeType.Unknown;
                    string part = parts[i];

                    if (i == (parts.Length - 2))
                    {
                        // The second-to-last part is the actual method name. However, it typically looks something
                        // like "_Method_d__24" so we need to clean it up.
                        typeHint = ScopeType.Function;

                        if (part.Contains("_"))
                        {
                            part = part.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries)[0];
                        }
                    }
                    else if (i == (parts.Length - 3))
                    {
                        // Since "MoveNext" is the last part, the third-to-last part should be the class name.
                        typeHint = ScopeType.Class;
                    }
                    else if (i == 0)
                    {
                        // The first part must be a namespace.
                        typeHint = ScopeType.Namespace;
                    }

                    identifiers.Add(new ScopeIdentifier(part, typeHint));
                }
            }
            else
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    bool addPart = true;
                    ScopeType typeHint = ScopeType.Unknown;
                    string part = parts[i];

                    if (i == (parts.Length - 1))
                    {
                        // The final part must be a method.
                        typeHint = ScopeType.Function;

                        if (i > 0 && part == "ctor")
                        {
                            // If the final part indicates a constructor then substitute it for the previous part
                            // (which is presumably a class name).
                            part = parts[i - 1];
                        }
                        else if (part == "cctor")
                        {
                            // If the final part is "cctor" then the code is actually scoped to the class (usually
                            // a private static variable) so don't add this part as a scope identifier.
                            addPart = false;
                        }
                    }
                    else if (i == (parts.Length - 2))
                    {
                        // The second-to-last part must be a class.
                        typeHint = ScopeType.Class;
                    }
                    else if (i == 0)
                    {
                        // The first part must be a namespace.
                        typeHint = ScopeType.Namespace;
                    }

                    if (addPart)
                    {
                        identifiers.Add(new ScopeIdentifier(part, typeHint));
                    }
                }
            }

            return identifiers;
        }
    }
}
