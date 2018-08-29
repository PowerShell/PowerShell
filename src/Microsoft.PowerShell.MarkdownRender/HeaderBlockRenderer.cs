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
    /// Renderer for adding VT100 escape sequences for headings.
    /// </summary>
    internal class HeaderBlockRenderer : VT100ObjectRenderer<HeadingBlock>
    {
        protected override void Write(VT100Renderer renderer, HeadingBlock obj)
        {
            string headerText = obj?.Inline?.FirstChild?.ToString();

            if (!string.IsNullOrEmpty(headerText))
            {
                // Format header and then add blank line to improve readability.
                switch (obj.Level)
                {
                    case 1:
                        renderer.WriteLine(renderer.EscapeSequences.FormatHeader1(headerText));
                        renderer.WriteLine();
                        break;

                    case 2:
                        renderer.WriteLine(renderer.EscapeSequences.FormatHeader2(headerText));
                        renderer.WriteLine();
                        break;

                    case 3:
                        renderer.WriteLine(renderer.EscapeSequences.FormatHeader3(headerText));
                        renderer.WriteLine();
                        break;

                    case 4:
                        renderer.WriteLine(renderer.EscapeSequences.FormatHeader4(headerText));
                        renderer.WriteLine();
                        break;

                    case 5:
                        renderer.WriteLine(renderer.EscapeSequences.FormatHeader5(headerText));
                        renderer.WriteLine();
                        break;

                    case 6:
                        renderer.WriteLine(renderer.EscapeSequences.FormatHeader6(headerText));
                        renderer.WriteLine();
                        break;
                }
            }
        }
    }
}
