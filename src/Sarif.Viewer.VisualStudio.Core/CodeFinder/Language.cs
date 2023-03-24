// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Sarif.Viewer.VisualStudio.Core.CodeFinder
{
    public enum Language
    {
        /// <summary>
        /// When the language is unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// The C language.
        /// </summary>
        C,

        /// <summary>
        /// The C++ programming language.
        /// </summary>
        Cpp,

        /// <summary>
        /// The C# programming language.
        /// </summary>
        CSharp,

        /// <summary>
        /// The TypeScript programming language.
        /// </summary>
        TypeScript,

        /// <summary>
        /// The JavaScript programming language.
        /// </summary>
        JavaScript,

        /// <summary>
        /// The Python programming language.
        /// </summary>
        Python,

        /// <summary>
        /// The Swift programming language.
        /// </summary>
        Swift,

        /// <summary>
        /// The Go programming language.
        /// </summary>
        Go,

        /// <summary>
        /// The VisaulBasic programming language.
        /// </summary>
        VisualBasic,

        /// <summary>
        /// The python programming language.
        /// </summary>
        ObjectiveC,

        /// <summary>
        /// The Objective C++ programming language.
        /// </summary>
        ObjectiveCpp,

        /// <summary>
        /// The GoLang programming language.
        /// </summary>
        GoLang,

        /// <summary>
        /// The F# programming language.
        /// </summary>
        FSharp,
    }
}
