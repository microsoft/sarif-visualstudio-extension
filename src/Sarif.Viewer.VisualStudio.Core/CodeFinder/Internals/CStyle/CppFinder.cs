// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Sarif.Viewer.CodeFinder.Internal.CStyle
{
    /// <summary>
    /// Finds code in C and C++ files.
    /// </summary>
    internal class CppFinder : CStyleFinder
    {
        public CppFinder(string fileContents)
            : base(fileContents)
        {
            // List of C++ keywords taken from https://en.cppreference.com/w/cpp/keyword.
            // Note that "except", "final", and "finally" were then manually added.
            keywords = new HashSet<string>
            {
                "alignas", "alignof", "and", "and_eq", "asm", "atomic_cancel", "atomic_commit", "atomic_noexcept", "auto", "bitand", "bitor", "bool", "break",
                "case", "catch", "char", "char8_t", "char16_t", "char32_t", "class", "compl", "concept", "const", "consteval", "constexpr", "constinit", "const_cast",
                "continue", "co_await", "co_return", "co_yield", "decltype", "default", "delete", "do", "double", "dynamic_cast", "else", "enum", "except", "explicit", "export",
                "extern", "false", "final", "finally", "float", "for", "friend", "goto", "if", "inline", "int", "long", "mutable", "namespace", "new", "noexcept", "not", "not_eq", "nullptr",
                "operator", "or", "or_eq", "private", "protected", "public", "reflexpr", "register", "reinterpret_cast", "requires", "return", "short", "signed",
                "sizeof", "static", "static_assert", "static_cast", "struct", "switch", "synchronized", "template", "this", "thread_local", "throw", "true", "try",
                "typedef", "typeid", "typename", "union", "unsigned", "using", "virtual", "void", "volatile", "wchar_t", "while", "xor", "xor_eq",
            };
        }

        protected override List<ScopeIdentifier> ParseFunctionSignature(string functionSignature)
        {
            var identifiers = new List<ScopeIdentifier>();

            if (string.IsNullOrWhiteSpace(functionSignature))
            {
                return identifiers;
            }

            // We may get something that looks like the function prototype. In that case, try to parse out the function signature.
            // E.g.:
            //  int Foo(int a, int b) -> Foo
            //  void MyClass::ToString() -> MyClass::ToString
            //  char const * MyFunction(unsigned long) -> MyFunction
            if (functionSignature.Contains("(") && functionSignature.Contains(")"))
            {
                // First, get everything before the "(".
                functionSignature = functionSignature.Substring(0, functionSignature.IndexOf("("));

                // Split what's left by spaces and return the last element. This should be the actual function name/signature.
                functionSignature = functionSignature.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            }

            // At this point, there should be no spaces and some content in the function signature.
            // If not, it's not something we can work with so return nothing. This will ensure we fall back to the basic line-scanning algorithm.
            // We expect something like:
            //  "Function"
            //  "Class::Method"
            //  "Namespace::Class::Mathod"
            if (functionSignature.Contains(" ") || string.IsNullOrWhiteSpace(functionSignature))
            {
                return identifiers;
            }

            // C++ signatures are delimited by double colons and periods ("::" and ".").
            // Split it into its individual parts and then sanitize them.
            List<string> parts = SanitizeFunctionSignature(functionSignature);

            // Go through the sanitized parts and assign hints.
            for (int i = 0; i < parts.Count; i++)
            {
                ScopeType typeHint = ScopeType.Unknown;
                string part = parts[i];

                // Because C++ lets you use "using namespace" to implicitly declare (one or more) namespace scopes,
                // anything up until the last 2 parts (class and method) may not actually have an explicit scope.
                // However, the classes and methods should always have an explicit scope somewhere.
                bool explicitScope = false;

                if (i == (parts.Count - 1))
                {
                    // The last part is the method.
                    typeHint = ScopeType.Function;
                    explicitScope = true;
                }
                else if (i == (parts.Count - 2))
                {
                    // The second-to-last part is the class.
                    typeHint = ScopeType.Class;
                    explicitScope = true;
                }
                else if (i == 0)
                {
                    typeHint = ScopeType.Namespace;
                }

                identifiers.Add(new ScopeIdentifier(part, typeHint, explicitScope));
            }

            return identifiers;
        }

        /// <summary>
        /// "Sanitizes" the function signature and returns its individual parts.
        /// We may completely remove parts that won't be defined in the code.
        /// We may also clean up some invalid characters (from parts that otherwise appear to be valid).
        /// Sometimes we see function signatures with invalid parts but they can still be fixed up.
        /// E.g.:
        /// <code>
        ///  `Class::Method'::`1'::catch${number}
        ///  `Method'::`1'::fin$0
        ///  Class::Method::__l2::{lambda}::operator()
        ///  Namespace::Class::Method__lambda_1b1f5ee28e310718866d896377259c1c___
        /// </code>
        /// </summary>
        /// <param name="functionSignature">The function signature.</param>
        /// <returns>The individual parts of the function signature.</returns>
        private static List<string> SanitizeFunctionSignature(string functionSignature)
        {
            // First, process the function signature as a whole.

            // Sometimes function signatures have brackets in them, usually to indicate something implicit
            // that we won't find in the code. Remove the brackets and everything between them.
            functionSignature = functionSignature.RemoveBetween('[', ']');

            // See if the function signature indicates a scope related to a try block (e.g. inside a catch or finally block).
            // Sometimes when this happens the valid part of the signature is wrapped in underscores and we'll need to remove them (later).
            bool tryBlockIndicated = false;
            if (functionSignature.Contains("catch$") ||
                functionSignature.Contains("fin$") ||
                functionSignature.Contains("filt$"))
            {
                tryBlockIndicated = true;
            }

            // If the function has a template sometimes that info is embedded in the function signature.
            // If the template has more than one typename then we can simply look for a comma to clue us in
            // to the template part. We assume the template part is then whatever comes after the underscore
            // that precedes the first comma.
            // Note that this doesn't cover every case where template types are embedded in the function signature.
            // E.g. the class has a template, the embedded types have underscores in their name, etc.
            int comma = functionSignature.IndexOf(',');
            if (comma != -1)
            {
                // Find the underscore that precedes the comma.
                int underscore = functionSignature.LastIndexOf('_', comma);
                if (underscore != -1)
                {
                    // Keep everything that precedes the underscore.
                    functionSignature = functionSignature.Substring(0, underscore);
                }
            }

            // Now split the function signature and process each part individually.
            string[] parts = functionSignature.Split(new string[] { "::", "." }, StringSplitOptions.RemoveEmptyEntries);
            var newParts = new List<string>();
            bool stopProcessing = false;
            string prevPart = string.Empty;
            foreach (string part in parts)
            {
                // If we decided to stop processing more parts in the previous iteration, break out now.
                if (stopProcessing)
                {
                    break;
                }

                string newPart = part;

                // Step 1: Trim any odd characters (like ` and ')
                newPart = newPart.Trim(new char[] { '`', '\'' });

                // Step 2: Look to see if this is a completely invalid token.

                // Sometimes a part is a number surrounded by underscores, e.g. "_1_". Ignore these.
                if (newPart.StartsWith("_") && newPart.EndsWith("_") && newPart.Length >= 3)
                {
                    string testPart = newPart.Substring(1, newPart.Length - 2);
                    if (int.TryParse(testPart, out _))
                    {
                        continue;
                    }
                }

                // If this part indicates a lambda without any other context then it and
                // any part after it isn't usable b/c CodeFinder doesn't currently identify lambda scopes.
                if (newPart.StartsWith("<lambda") ||
                    newPart.StartsWith("__lambda_"))
                {
                    break;
                }
                else if (newPart.StartsWith("__l"))
                {
                    // Sometimes a lambda part is preceded by a "__l<int>" part. Ignore these.
                    string intPart = newPart.Substring(3);
                    if (int.TryParse(intPart, out _))
                    {
                        continue;
                    }
                }
                else if (newPart == "_anonymous_namespace_")
                {
                    // Skip anonymous namespaces.
                    continue;
                }

                // Step 3: See if the part needs to (and can be) fixed up to become a valid token.

                if (newPart == "ctor" ||
                    newPart == "cctor" ||
                    newPart == "{ctor}" ||
                    newPart == "{cctor}")
                {
                    // "ctor" (and the like) indicates this is the constructor.
                    // Presumably the previous part is the class, so set this part to the class name.
                    newPart = prevPart;
                }
                else if (newPart == "dtor" ||
                         newPart == "{dtor}")
                {
                    // "dtor" indicates this is the destructor.
                    // Presumably the previous part is the class, so set this part to the class name with "~" prepended to indicate it's the destructor.
                    newPart = $"~{prevPart}";
                }
                else if (newPart == $"_{prevPart}")
                {
                    // If this part is the same as the previous part, but preceded by an underscore then it's the destructor.
                    newPart = $"~{prevPart}";
                }
                else if (newPart.Contains("$catch$") ||
                         newPart.Contains("$filt$") ||
                         newPart.Contains("$fin$"))
                {
                    // Catch, except, and finally blocks are sometimes appended to the method name like "Method$catch$0". Preserve the method name.

                    // Get the index of the second-to-last "$" so that we can keep everything before it.
                    int index = newPart.LastIndexOf('$');
                    index = newPart.LastIndexOf('$', index - 1);
                    newPart = newPart.Substring(0, index);
                }
                else if (newPart.Contains("__lambda_"))
                {
                    // Some lambda parts may be the mathod name with "__lambda_<guid>" appended. Remove the lambda part to preserve the method name.
                    newPart = newPart.Split(new string[] { "__lambda_" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                    // Subsequent parts should be ignored since CodeFinder doesn't identify lambda scopes.
                    stopProcessing = true;
                }

                // Step 4: At this point, we've done all the fixing up we can so the part *should* be a valid token.
                // If so, add it to the list of parts to return.
                if (IsWord(newPart, false))
                {
                    newParts.Add(newPart);
                    prevPart = newPart;
                }
            }

            // As mentioned above, if the function signature indicated a scope related to a try block we may need to remove
            // a leading underscore from the first part and a trailing underscore from the last part.
            // Only do this if there are at least 2 parts as the extra underscores don't seem to be added for function
            // signatures with only one valid part.
            // E.g. _ToastController::TryStartLifetimeManagerIfNecessary_ -> ToastController::TryStartLifetimeManagerIfNecessary
            if (tryBlockIndicated && newParts.Count > 1)
            {
                string firstPart = newParts[0];
                string lastPart = newParts[newParts.Count - 1];
                if (firstPart[0] == '_' &&
                    lastPart[lastPart.Length - 1] == '_')
                {
                    newParts[0] = firstPart.Substring(1);
                    newParts[newParts.Count - 1] = lastPart.Substring(0, lastPart.Length - 1);
                }
            }

            return newParts;
        }
    }
}
