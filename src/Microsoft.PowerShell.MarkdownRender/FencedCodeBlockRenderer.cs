// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using Markdig;
using Markdig.Helpers;
using Markdig.Renderers;
using Markdig.Syntax;

namespace Microsoft.PowerShell.MarkdownRender
{
    /// <summary>
    /// Renderer for adding VT100 escape sequences for code blocks with language type.
    /// </summary>
    internal class FencedCodeBlockRenderer : VT100ObjectRenderer<FencedCodeBlock>
    {
        protected override void Write(VT100Renderer renderer, FencedCodeBlock obj)
        {
            if (obj?.Lines.Lines != null)
            {
                foreach (StringLine codeLine in obj.Lines.Lines)
                {
                    if (!string.IsNullOrWhiteSpace(codeLine.ToString()))
                    {
                        // If the code block is of type YAML, then tab to right to improve readability.
                        // This specifically helps for parameters help content.
                        if (string.Equals(obj.Info, "yaml", StringComparison.OrdinalIgnoreCase))
                        {
                            renderer.Write("\t").WriteLine(codeLine.ToString());
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
}
