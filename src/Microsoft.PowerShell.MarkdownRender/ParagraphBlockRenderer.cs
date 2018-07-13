// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;

namespace Microsoft.PowerShell.MarkdownRender
{
    /// <summary>
    /// Renderer for adding VT100 escape sequences for paragraphs.
    /// </summary>
    internal class ParagraphBlockRenderer : VT100ObjectRenderer<ParagraphBlock>
    {
        protected override void Write(VT100Renderer renderer, ParagraphBlock obj)
        {
            // Call the renderer for children, leaf inline or line breaks.
            renderer.WriteChildren(obj.Inline);

            // Add new line at the end of the paragraph.
            renderer.WriteLine();
        }
    }
}
