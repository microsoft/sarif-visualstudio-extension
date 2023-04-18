using System;
using System.Collections.Generic;
using System.Text;

namespace Sarif.Viewer.VisualStudio.Core.Options
{
    internal interface ISarifViewerColorOptions
    {
        string ErrorUnderlineColor { get; }

        string WarningUnderlineColor { get; }

        string NoteUnderlineColor { get; }
    }
}
