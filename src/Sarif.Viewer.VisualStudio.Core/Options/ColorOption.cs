// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.Options
{
    public class ColorOption
    {
        /// <summary>
        /// Gets or sets a descriptive name to describe the highlight that will be displayed to the user.
        /// </summary>
        public string ColorName { get; set; }

        /// <summary>
        /// Gets or sets the text that is to be used when created the color swatch to display to the user.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the text that is used by the VS API to determine how to highlight a particular span. Must be from the <see cref="PredefinedErrorTypeName"/> class or will always display a red underline.
        /// </summary>
        public string PredefinedErrorTypeName { get; set; }

        public ColorOption(string colorName, string text, string predefinedErrorTypeName)
        {
            this.ColorName = colorName;
            this.Text = text;
            this.PredefinedErrorTypeName = predefinedErrorTypeName;
        }
    }
}
