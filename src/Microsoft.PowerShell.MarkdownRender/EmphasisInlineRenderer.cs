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
    /// Renderer for adding VT100 escape sequences for bold and italics elements.
    /// </summary>
    internal class EmphasisInlineRenderer : VT100ObjectRenderer<EmphasisInline>
    {
        protected override void Write(VT100Renderer renderer, EmphasisInline obj)
        {
            renderer.Write(renderer.EscapeSequences.FormatEmphasis(obj.FirstChild.ToString(), isBold : obj.DelimiterCount == 2 ? true : false));
        }
    }
}
