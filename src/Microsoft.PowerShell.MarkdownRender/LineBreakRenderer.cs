// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax.Inlines;

namespace Microsoft.PowerShell.MarkdownRender
{
    /// <summary>
    /// Renderer for adding VT100 escape sequences for line breaks.
    /// </summary>
    internal class LineBreakRenderer : VT100ObjectRenderer<LineBreakInline>
    {
        protected override void Write(VT100Renderer renderer, LineBreakInline obj)
        {
            // If it is a hard line break add new line at the end.
            // Else, add a space for after the last character to improve readability.
            if (obj.IsHard)
            {
                renderer.WriteLine();
            }
            else
            {
                renderer.Write(" ");
            }
        }
    }
}
