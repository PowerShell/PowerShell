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
    /// Renderer for adding VT100 escape sequences for leaf elements like plain text in paragraphs.
    /// </summary>
    internal class LeafInlineRenderer : VT100ObjectRenderer<LeafInline>
    {
        protected override void Write(VT100Renderer renderer, LeafInline obj)
        {
            // If the next sibling is null, then this is the last line in the paragraph.
            // Add new line character at the end.
            // Else just write without newline at the end.
            if (obj.NextSibling == null)
            {
                renderer.WriteLine(obj.ToString());
            }
            else
            {
                renderer.Write(obj.ToString());
            }
        }
    }
}
