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
    /// Renderer for adding VT100 escape sequences for code blocks with language type.
    /// </summary>
    internal class FencedCodeBlockRenderer : VT100ObjectRenderer<FencedCodeBlock>
    {
        protected override void Write(VT100Renderer renderer, FencedCodeBlock obj)
        {
            foreach (var codeLine in obj.Lines.Lines)
            {
                if (!String.IsNullOrWhiteSpace(codeLine.ToString()))
                {
                    // If the code block is of type YAML, then tab to right to improve readability.
                    // This specifically helps for parameters help content.
                    if (String.Equals(obj.Info, "yaml", StringComparison.OrdinalIgnoreCase))
                    {
                        renderer.WriteLine("\t" + codeLine.ToString());
                    }
                    else
                    {
                        renderer.WriteLine(renderer.EscapeSequences.FormatCode(codeLine.ToString(), isInline: false));
                    }
                }
            }

            // Add a blank line after the code block for better readability.
            renderer.WriteLine();
        }
    }
}
