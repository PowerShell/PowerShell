// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Management.Automation;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;

namespace Microsoft.PowerShell.MarkdownRender
{
    /// <summary>
    /// Enum to name all the properties of PSMarkdownOptionInfo.
    /// </summary>
    public enum MarkdownOptionInfoProperty
    {
        /// <summary>
        /// Property name Header1.
        /// </summary>
        Header1,

        /// <summary>
        /// Property name Header2.
        /// </summary>
        Header2,

        /// <summary>
        /// Property name Header3.
        /// </summary>
        Header3,

        /// <summary>
        /// Property name Header4.
        /// </summary>
        Header4,

        /// <summary>
        /// Property name Header5.
        /// </summary>
        Header5,

        /// <summary>
        /// Property name Header6.
        /// </summary>
        Header6,

        /// <summary>
        /// Property name Code.
        /// </summary>
        Code,

        /// <summary>
        /// Property name Link.
        /// </summary>
        Link,

        /// <summary>
        /// Property name Image.
        /// </summary>
        Image,

        /// <summary>
        /// Property name EmphasisBold.
        /// </summary>
        EmphasisBold,

        /// <summary>
        /// Property name EmphasisItalics.
        /// </summary>
        EmphasisItalics
    }

    /// <summary>
    /// Class to represent color preference options for various Markdown elements.
    /// </summary>
    public sealed class PSMarkdownOptionInfo
    {
        private const char Esc = (char)0x1b;
        private const string EndSequence = "[0m";

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
        /// Gets or sets a value indicating whether VT100 escape sequences should be added. Default it true.
        /// </summary>
        public bool EnableVT100Encoding { get; set; }

        /// <summary>
        /// Get the property as an rendered escape sequence.
        /// This is used by formatting system for displaying.
        /// </summary>
        /// <param name="propertyName">Name of the property to get as escape sequence.</param>
        /// <returns>Specified property name as escape sequence.</returns>
        public string AsEscapeSequence(MarkdownOptionInfoProperty propertyName)
        {
            switch (propertyName)
            {
                case MarkdownOptionInfoProperty.Header1:
                    return string.Concat(Esc, Header1, Header1, Esc, EndSequence);

                case MarkdownOptionInfoProperty.Header2:
                    return string.Concat(Esc, Header2, Header2, Esc, EndSequence);

                case MarkdownOptionInfoProperty.Header3:
                    return string.Concat(Esc, Header3, Header3, Esc, EndSequence);

                case MarkdownOptionInfoProperty.Header4:
                    return string.Concat(Esc, Header4, Header4, Esc, EndSequence);

                case MarkdownOptionInfoProperty.Header5:
                    return string.Concat(Esc, Header5, Header5, Esc, EndSequence);

                case MarkdownOptionInfoProperty.Header6:
                    return string.Concat(Esc, Header6, Header6, Esc, EndSequence);

                case MarkdownOptionInfoProperty.Code:
                    return string.Concat(Esc, Code, Code, Esc, EndSequence);

                case MarkdownOptionInfoProperty.Link:
                    return string.Concat(Esc, Link, Link, Esc, EndSequence);

                case MarkdownOptionInfoProperty.Image:
                    return string.Concat(Esc, Image, Image, Esc, EndSequence);

                case MarkdownOptionInfoProperty.EmphasisBold:
                    return string.Concat(Esc, EmphasisBold, EmphasisBold, Esc, EndSequence);

                case MarkdownOptionInfoProperty.EmphasisItalics:
                    return string.Concat(Esc, EmphasisItalics, EmphasisItalics, Esc, EndSequence);

                default:
                    break;
            }

            return null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PSMarkdownOptionInfo"/> class and sets dark as the default theme.
        /// </summary>
        public PSMarkdownOptionInfo()
        {
            SetDarkTheme();
            EnableVT100Encoding = true;
        }

        private const string Header1Dark = "[7m";
        private const string Header2Dark = "[4;93m";
        private const string Header3Dark = "[4;94m";
        private const string Header4Dark = "[4;95m";
        private const string Header5Dark = "[4;96m";
        private const string Header6Dark = "[4;97m";
        private const string CodeDark = "[48;2;155;155;155;38;2;30;30;30m";
        private const string CodeMacOS = "[107;95m";
        private const string LinkDark = "[4;38;5;117m";
        private const string ImageDark = "[33m";
        private const string EmphasisBoldDark = "[1m";
        private const string EmphasisItalicsDark = "[36m";

        private const string Header1Light = "[7m";
        private const string Header2Light = "[4;33m";
        private const string Header3Light = "[4;34m";
        private const string Header4Light = "[4;35m";
        private const string Header5Light = "[4;36m";
        private const string Header6Light = "[4;30m";
        private const string CodeLight = "[48;2;155;155;155;38;2;30;30;30m";
        private const string LinkLight = "[4;38;5;117m";
        private const string ImageLight = "[33m";
        private const string EmphasisBoldLight = "[1m";
        private const string EmphasisItalicsLight = "[36m";

        /// <summary>
        /// Set all preference for dark theme.
        /// </summary>
        public void SetDarkTheme()
        {
            Header1 = Header1Dark;
            Header2 = Header2Dark;
            Header3 = Header3Dark;
            Header4 = Header4Dark;
            Header5 = Header5Dark;
            Header6 = Header6Dark;
            Link = LinkDark;
            Image = ImageDark;
            EmphasisBold = EmphasisBoldDark;
            EmphasisItalics = EmphasisItalicsDark;
            SetCodeColor(isDarkTheme: true);
        }

        /// <summary>
        /// Set all preference for light theme.
        /// </summary>
        public void SetLightTheme()
        {
            Header1 = Header1Light;
            Header2 = Header2Light;
            Header3 = Header3Light;
            Header4 = Header4Light;
            Header5 = Header5Light;
            Header6 = Header6Light;
            Link = LinkLight;
            Image = ImageLight;
            EmphasisBold = EmphasisBoldLight;
            EmphasisItalics = EmphasisItalicsLight;
            SetCodeColor(isDarkTheme: false);
        }

        private void SetCodeColor(bool isDarkTheme)
        {
            // MacOS terminal app does not support extended colors for VT100, so we special case for it.
            Code = Platform.IsMacOS ? CodeMacOS : isDarkTheme ? CodeDark : CodeLight;
        }
    }

    /// <summary>
    /// Class to represent default VT100 escape sequences.
    /// </summary>
    public class VT100EscapeSequences
    {
        private const char Esc = (char)0x1B;
        private string endSequence = Esc + "[0m";

        // For code blocks, [500@ make sure that the whole line has background color.
        private const string LongBackgroundCodeBlock = "[500@";
        private PSMarkdownOptionInfo options;

        /// <summary>
        /// Initializes a new instance of the <see cref="VT100EscapeSequences"/> class.
        /// </summary>
        /// <param name="optionInfo">PSMarkdownOptionInfo object to initialize with.</param>
        public VT100EscapeSequences(PSMarkdownOptionInfo optionInfo)
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
            return FormatHeader(headerText, options.Header1);
        }

        /// <summary>
        /// Class to represent default VT100 escape sequences.
        /// </summary>
        /// <param name="headerText">Text of the header to format.</param>
        /// <returns>Formatted Header 2 string.</returns>
        public string FormatHeader2(string headerText)
        {
            return FormatHeader(headerText, options.Header2);
        }

        /// <summary>
        /// Class to represent default VT100 escape sequences.
        /// </summary>
        /// <param name="headerText">Text of the header to format.</param>
        /// <returns>Formatted Header 3 string.</returns>
        public string FormatHeader3(string headerText)
        {
            return FormatHeader(headerText, options.Header3);
        }

        /// <summary>
        /// Class to represent default VT100 escape sequences.
        /// </summary>
        /// <param name="headerText">Text of the header to format.</param>
        /// <returns>Formatted Header 4 string.</returns>
        public string FormatHeader4(string headerText)
        {
            return FormatHeader(headerText, options.Header4);
        }

        /// <summary>
        /// Class to represent default VT100 escape sequences.
        /// </summary>
        /// <param name="headerText">Text of the header to format.</param>
        /// <returns>Formatted Header 5 string.</returns>
        public string FormatHeader5(string headerText)
        {
            return FormatHeader(headerText, options.Header5);
        }

        /// <summary>
        /// Class to represent default VT100 escape sequences.
        /// </summary>
        /// <param name="headerText">Text of the header to format.</param>
        /// <returns>Formatted Header 6 string.</returns>
        public string FormatHeader6(string headerText)
        {
            return FormatHeader(headerText, options.Header6);
        }

        /// <summary>
        /// Class to represent default VT100 escape sequences.
        /// </summary>
        /// <param name="codeText">Text of the code block to format.</param>
        /// <param name="isInline">True if it is a inline code block, false otherwise.</param>
        /// <returns>Formatted code block string.</returns>
        public string FormatCode(string codeText, bool isInline)
        {
            bool isVT100Enabled = options.EnableVT100Encoding;

            if (isInline)
            {
                if (isVT100Enabled)
                {
                    return string.Concat(Esc, options.Code, codeText, endSequence);
                }
                else
                {
                    return codeText;
                }
            }
            else
            {
                if (isVT100Enabled)
                {
                    return string.Concat(Esc, options.Code, codeText, Esc, LongBackgroundCodeBlock, endSequence);
                }
                else
                {
                    return codeText;
                }
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
            bool isVT100Enabled = options.EnableVT100Encoding;

            if (hideUrl)
            {
                if (isVT100Enabled)
                {
                    return string.Concat(Esc, options.Link, "\"", linkText, "\"", endSequence);
                }
                else
                {
                    return string.Concat("\"", linkText, "\"");
                }
            }
            else
            {
                if (isVT100Enabled)
                {
                    return string.Concat("\"", linkText, "\" (", Esc, options.Link, url, endSequence, ")");
                }
                else
                {
                    return string.Concat("\"", linkText, "\" (", url, ")");
                }
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

            if (options.EnableVT100Encoding)
            {
                return string.Concat(Esc, sequence, emphasisText, endSequence);
            }
            else
            {
                return emphasisText;
            }
        }

        /// <summary>
        /// Class to represent default VT100 escape sequences.
        /// </summary>
        /// <param name="altText">Text of the image to format.</param>
        /// <returns>Formatted image string.</returns>
        public string FormatImage(string altText)
        {
            var text = altText;

            if (string.IsNullOrEmpty(altText))
            {
                text = "Image";
            }

            if (options.EnableVT100Encoding)
            {
                return string.Concat(Esc, options.Image, "[", text, "]", endSequence);
            }
            else
            {
                return string.Concat("[", text, "]");
            }
        }

        private string FormatHeader(string headerText, string headerEscapeSequence)
        {
            if (options.EnableVT100Encoding)
            {
                return string.Concat(Esc, headerEscapeSequence, headerText, endSequence);
            }
            else
            {
                return headerText;
            }
        }
    }
}
