// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using Markdig;
using Markdig.Syntax;
using Markdig.Renderers;

namespace Microsoft.PowerShell.MarkdownRender
{
    /// <summary>
    /// Implementation of the VT100 renderer.
    /// </summary>
    public sealed class VT100Renderer : TextRendererBase<VT100Renderer>
    {
        /// <summary>
        /// Initialize the VT100 renderer with <param name="optionInfo"/> and write the output <param name="writer"/>.
        /// </summary>
        public VT100Renderer(TextWriter writer, MarkdownOptionInfo optionInfo) : base(writer)
        {
            EscapeSequences = new VT100EscapeSequences(optionInfo);

            // Add the various element renderers.
            ObjectRenderers.Add(new HeaderBlockRenderer());
            ObjectRenderers.Add(new LineBreakRenderer());
            ObjectRenderers.Add(new CodeInlineRenderer());
            ObjectRenderers.Add(new FencedCodeBlockRenderer());
            ObjectRenderers.Add(new EmphasisInlineRenderer());
            ObjectRenderers.Add(new ParagraphBlockRenderer());
            ObjectRenderers.Add(new LeafInlineRenderer());
            ObjectRenderers.Add(new LinkInlineRenderer());
            ObjectRenderers.Add(new ListBlockRenderer());
            ObjectRenderers.Add(new ListItemBlockRenderer());
            ObjectRenderers.Add(new QuoteBlockRenderer());
        }

        /// <summary>
        /// Get the current escape sequences.
        /// </summary>
        public VT100EscapeSequences EscapeSequences { get; private set;}
    }
}
