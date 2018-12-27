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
    /// Renderer for adding VT100 escape sequences for inline code elements.
    /// </summary>
    internal class CodeInlineRenderer : VT100ObjectRenderer<CodeInline>
    {
        protected override void Write(VT100Renderer renderer, CodeInline obj)
        {
            renderer.Write(renderer.EscapeSequences.FormatCode(obj.Content, isInline : true));
        }
    }
}
