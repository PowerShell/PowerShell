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
    /// Renderer for adding VT100 escape sequences for links.
    /// </summary>
    internal class LinkInlineRenderer : VT100ObjectRenderer<LinkInline>
    {
        protected override void Write(VT100Renderer renderer, LinkInline obj)
        {
            string text = obj.FirstChild?.ToString();

            // Format link as image or link.
            if (obj.IsImage)
            {
                renderer.Write(renderer.EscapeSequences.FormatImage(text));
            }
            else
            {
                renderer.Write(renderer.EscapeSequences.FormatLink(text, obj.Url));
            }
        }
    }
}
