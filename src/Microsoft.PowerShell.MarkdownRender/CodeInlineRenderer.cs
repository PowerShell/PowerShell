// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
            renderer.Write(renderer.EscapeSequences.FormatCode(obj.Content, isInline: true));
        }
    }
}
