// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.Options
{
    public class ColorOption
    {
        public string ColorName { get; set; }

        public string Text { get; set; }

        public ColorOption(string colorName, string text)
        {
            this.ColorName = colorName;
            this.Text = text;
        }
    }
}
