// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

using Microsoft.VisualStudio.Shell;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Models
{
    internal class AuthStateDialogPage: DialogPage
    {
        private bool _refusedLogin = false;

        public bool RefusedLogin
        {
            get { return _refusedLogin; }
            set { _refusedLogin = value; }
        }
    }
}
