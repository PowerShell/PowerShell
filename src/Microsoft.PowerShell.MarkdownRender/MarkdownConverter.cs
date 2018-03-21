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
    /// Type of conversion from markdown.
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
    /// Object representing the conversion from markdown.
    /// </summary>
    public class MarkdownInfo
    {
        /// <summary>
        /// Html content after conversion.
        /// </summary>
        public string Html { get; internal set;}

        /// <summary>
        /// VT100 encoded string after conversion.
        /// </summary>
        public string VT100EncodedString { get; internal set;}

        /// <summary>
        /// AST of the markdown string.
        /// </summary>
        public Markdig.Syntax.MarkdownDocument Tokens { get; internal set; }
    }

    /// <summary>
    /// Class to convert a markdown string to VT100, HTML or AST.
    /// </summary>
    public sealed class MarkdownConverter
    {
        /// <summary>
        /// Convert from markdown string to VT100 encoded string or HTML. Returns MarkdownInfo object.
        /// </summary>
        /// <param name="markdownString">string with markdown content to be converted</param>
        /// <param name="conversionType">specifies type of conversion, either VT100 or HTML</param>
        /// <param name="optionInfo">specifies the rendering options for VT100 rendering</param>
        public static MarkdownInfo Convert(string markdownString, MarkdownConversionType conversionType, MarkdownOptionInfo optionInfo)
        {
            var renderInfo = new MarkdownInfo();
            var writer = new StringWriter();
            MarkdownPipeline pipeline = null;

            if(conversionType.HasFlag(MarkdownConversionType.HTML))
            {
                pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                var renderer = new Markdig.Renderers.HtmlRenderer(writer);
                renderInfo.Html = Markdig.Markdown.Convert(markdownString, renderer, pipeline).ToString();
            }

            if(conversionType.HasFlag(MarkdownConversionType.VT100))
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
