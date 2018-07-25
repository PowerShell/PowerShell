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
    /// Type of conversion from Markdown.
    /// </summary>
    [Flags]
    public enum MarkdownConversionType
    {
        /// <summary>
        /// Convert to HTML.
        /// </summary>
        HTML = 1,

        /// <summary>
        /// Convert to VT100 encoded string.
        /// </summary>
        VT100 = 2
    }

    /// <summary>
    /// Object representing the conversion from Markdown.
    /// </summary>
    public class MarkdownInfo
    {
        /// <summary>
        /// Gets the Html content after conversion.
        /// </summary>
        public string Html { get; internal set; }

        /// <summary>
        /// Gets the VT100 encoded string after conversion.
        /// </summary>
        public string VT100EncodedString { get; internal set; }

        /// <summary>
        /// Gets the AST of the Markdown string.
        /// </summary>
        public Markdig.Syntax.MarkdownDocument Tokens { get; internal set; }
    }

    /// <summary>
    /// Class to convert a Markdown string to VT100, HTML or AST.
    /// </summary>
    public sealed class MarkdownConverter
    {
        /// <summary>
        /// Convert from Markdown string to VT100 encoded string or HTML. Returns MarkdownInfo object.
        /// </summary>
        /// <param name="markdownString">String with Markdown content to be converted.</param>
        /// <param name="conversionType">Specifies type of conversion, either VT100 or HTML.</param>
        /// <param name="optionInfo">Specifies the rendering options for VT100 rendering.</param>
        /// <returns>MarkdownInfo object with the converted output.</returns>
        public static MarkdownInfo Convert(string markdownString, MarkdownConversionType conversionType, PSMarkdownOptionInfo optionInfo)
        {
            var renderInfo = new MarkdownInfo();
            var writer = new StringWriter();
            MarkdownPipeline pipeline = null;

            if (conversionType.HasFlag(MarkdownConversionType.HTML))
            {
                pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                var renderer = new Markdig.Renderers.HtmlRenderer(writer);
                renderInfo.Html = Markdig.Markdown.Convert(markdownString, renderer, pipeline).ToString();
            }

            if (conversionType.HasFlag(MarkdownConversionType.VT100))
            {
                pipeline = new MarkdownPipelineBuilder().Build();

                // Use the VT100 renderer.
                var renderer = new VT100Renderer(writer, optionInfo);
                renderInfo.VT100EncodedString = Markdig.Markdown.Convert(markdownString, renderer, pipeline).ToString();
            }

            // Always have AST available.
            var parsed = Markdig.Markdown.Parse(markdownString, pipeline);
            renderInfo.Tokens = parsed;

            return renderInfo;
        }
    }
}
