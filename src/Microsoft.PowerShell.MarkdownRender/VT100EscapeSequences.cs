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
    /// Class to represent color preference options for various markdown elements.
    /// </summary>
    public sealed class MarkdownOptionInfo
    {
        private const char Esc = (char)0x1b;

        /// <summary>
        /// Gets or sets current VT100 escape sequence for header 1.
        /// </summary>
        public string Header1 { get; set; }

        /// <summary>
        /// Gets or sets current VT100 escape sequence for header 2.
        /// </summary>
        public string Header2 { get; set; }

        /// <summary>
        /// Gets or sets current VT100 escape sequence for header 3.
        /// </summary>
        public string Header3 { get; set; }

        /// <summary>
        /// Gets or sets current VT100 escape sequence for header 4.
        /// </summary>
        public string Header4 { get; set; }

        /// <summary>
        /// Gets or sets current VT100 escape sequence for header 5.
        /// </summary>
        public string Header5 { get; set; }

        /// <summary>
        /// Gets or sets current VT100 escape sequence for header 6.
        /// </summary>
        public string Header6 { get; set; }

        /// <summary>
        /// Gets or sets current VT100 escape sequence for code inline and code blocks.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets current VT100 escape sequence for links.
        /// </summary>
        public string Link { get; set; }

        /// <summary>
        /// Gets or sets current VT100 escape sequence for images.
        /// </summary>
        public string Image { get; set; }

        /// <summary>
        /// Gets or sets current VT100 escape sequence for bold text.
        /// </summary>
        public string EmphasisBold { get; set; }

        /// <summary>
        /// Gets or sets current VT100 escape sequence for italics text.
        /// </summary>
        public string EmphasisItalics { get; set; }

        /// <summary>
        /// Get the property as an rendered escape sequence.
        /// This is used for typesps1xml for displaying.
        /// </summary>
        /// <param name="propertyName">Name of the property to get as escape sequence.</param>
        /// <returns>Specified property name as escape sequence.</returns>
        public string AsEscapeSequence(string propertyName)
        {
            var propertyValue = this.GetType().GetProperty(propertyName)?.GetValue(this) as string;

            if (!string.IsNullOrEmpty(propertyValue))
            {
                return string.Concat(Esc, propertyValue, propertyValue, Esc, "[0m");
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MarkdownOptionInfo"/> class and sets dark as the default theme.
        /// </summary>
        public MarkdownOptionInfo()
        {
            SetDarkTheme();
        }

        /// <summary>
        /// Set all preference for dark theme.
        /// </summary>
        public void SetDarkTheme()
        {
            Header1 = "[7m";
            Header2 = "[4;93m";
            Header3 = "[4;94m";
            Header4 = "[4;95m";
            Header5 = "[4;96m";
            Header6 = "[4;97m";
            Code = "[48;2;155;155;155;38;2;30;30;30m";
            Link = "[4;38;5;117m";
            Image = "[33m";
            EmphasisBold = "[1m";
            EmphasisItalics = "[36m";
        }

        /// <summary>
        /// Set all preference for light theme.
        /// </summary>
        public void SetLightTheme()
        {
            Header1 = "[7m";
            Header2 = "[4;33m";
            Header3 = "[4;34m";
            Header4 = "[4;35m";
            Header5 = "[4;36m";
            Header6 = "[4;30m";
            Code = "[48;2;155;155;155;38;2;30;30;30m";
            Link = "[4;38;5;117m";
            Image = "[33m";
            EmphasisBold = "[1m";
            EmphasisItalics = "[36m";
        }
    }

    /// <summary>
    /// Class to represent default VT100 escape sequences.
    /// </summary>
    public class VT100EscapeSequences
    {
        private const char Esc = (char)0x1B;

        private string endSequence = Esc + "[0m";

        private MarkdownOptionInfo options;

        /// <summary>
        /// Initializes a new instance of the <see cref="VT100EscapeSequences"/> class.
        /// </summary>
        /// <param name="optionInfo">MarkdownOptionInfo object to initialize with.</param>
        public VT100EscapeSequences(MarkdownOptionInfo optionInfo)
        {
            if (optionInfo == null)
            {
                throw new ArgumentNullException("optionInfo");
            }

            options = optionInfo;
        }

        /// <summary>
        /// Class to represent default VT100 escape sequences.
        /// </summary>
        /// <param name="headerText">Text of the header to format.</param>
        /// <returns>Formatted Header 1 string.</returns>
        public string FormatHeader1(string headerText)
        {
            return string.Concat(Esc, options.Header1, headerText, endSequence);
        }

        /// <summary>
        /// Class to represent default VT100 escape sequences.
        /// </summary>
        /// <param name="headerText">Text of the header to format.</param>
        /// <returns>Formatted Header 2 string.</returns>
        public string FormatHeader2(string headerText)
        {
            return string.Concat(Esc, options.Header2, headerText, endSequence);
        }

        /// <summary>
        /// Class to represent default VT100 escape sequences.
        /// </summary>
        /// <param name="headerText">Text of the header to format.</param>
        /// <returns>Formatted Header 3 string.</returns>
        public string FormatHeader3(string headerText)
        {
            return string.Concat(Esc, options.Header3, headerText, endSequence);
        }

        /// <summary>
        /// Class to represent default VT100 escape sequences.
        /// </summary>
        /// <param name="headerText">Text of the header to format.</param>
        /// <returns>Formatted Header 4 string.</returns>
        public string FormatHeader4(string headerText)
        {
            return string.Concat(Esc, options.Header4, headerText, endSequence);
        }

        /// <summary>
        /// Class to represent default VT100 escape sequences.
        /// </summary>
        /// <param name="headerText">Text of the header to format.</param>
        /// <returns>Formatted Header 5 string.</returns>
        public string FormatHeader5(string headerText)
        {
            return string.Concat(Esc, options.Header5, headerText, endSequence);
        }

        /// <summary>
        /// Class to represent default VT100 escape sequences.
        /// </summary>
        /// <param name="headerText">Text of the header to format.</param>
        /// <returns>Formatted Header 6 string.</returns>
        public string FormatHeader6(string headerText)
        {
            return string.Concat(Esc, options.Header6, headerText, endSequence);
        }

        /// <summary>
        /// Class to represent default VT100 escape sequences.
        /// </summary>
        /// <param name="codeText">Text of the code block to format.</param>
        /// <param name="isInline">True if it is a inline code block, false otherwise.</param>
        /// <returns>Formatted code block string.</returns>
        public string FormatCode(string codeText, bool isInline)
        {
            if (isInline)
            {
                return string.Concat(Esc, options.Code, codeText, endSequence);
            }
            else
            {
                // For code blocks, [500@ make sure that the whole line has background color.
                return string.Concat(Esc, options.Code, codeText, Esc, "[500@", endSequence);
            }
        }

        /// <summary>
        /// Class to represent default VT100 escape sequences.
        /// </summary>
        /// <param name="linkText">Text of the link to format.</param>
        /// <param name="url">URL of the link.</param>
        /// <param name="hideUrl">True url should be hidden, false otherwise. Default is true.</param>
        /// <returns>Formatted link string.</returns>
        public string FormatLink(string linkText, string url, bool hideUrl = true)
        {
            if (hideUrl)
            {
                return string.Concat(Esc, options.Link, "\"", linkText, "\"", endSequence);
            }
            else
            {
                return string.Concat("\"", linkText, "\" (", Esc, options.Link, url, endSequence, ")");
            }
        }

        /// <summary>
        /// Class to represent default VT100 escape sequences.
        /// </summary>
        /// <param name="emphasisText">Text to format as emphasis.</param>
        /// <param name="isBold">True if it is to be formatted as bold, false to format it as italics.</param>
        /// <returns>Formatted emphasis string.</returns>
        public string FormatEmphasis(string emphasisText, bool isBold)
        {
            var sequence = isBold ? options.EmphasisBold : options.EmphasisItalics;
            return string.Concat(Esc, sequence, emphasisText, endSequence);
        }

        /// <summary>
        /// Class to represent default VT100 escape sequences.
        /// </summary>
        /// <param name="altText">Text of the image to format.</param>
        /// <returns>Formatted image string.</returns>
        public string FormatImage(string altText)
        {
            return string.Concat(Esc, options.Image, "[", altText, "]", endSequence);
        }
    }
}
