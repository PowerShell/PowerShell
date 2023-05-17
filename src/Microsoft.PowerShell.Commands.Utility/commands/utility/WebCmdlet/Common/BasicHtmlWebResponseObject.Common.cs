// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Management.Automation;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Response object for html content without DOM parsing.
    /// </summary>
    public class BasicHtmlWebResponseObject : WebResponseObject
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicHtmlWebResponseObject"/> class.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public BasicHtmlWebResponseObject(HttpResponseMessage response, CancellationToken cancellationToken) : this(response, null, cancellationToken) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicHtmlWebResponseObject"/> class
        /// with the specified <paramref name="contentStream"/>.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="contentStream">The content stream associated with the response.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public BasicHtmlWebResponseObject(HttpResponseMessage response, Stream? contentStream, CancellationToken cancellationToken) : base(response, contentStream, cancellationToken)
        {
            InitializeContent(cancellationToken);
            InitializeRawContent(response);
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets the text body content of this response.
        /// </summary>
        /// <value>
        /// Content of the response body, decoded using <see cref="Encoding"/>,
        /// if the <c>Content-Type</c> response header is a recognized text
        /// type.  Otherwise <see langword="null"/>.
        /// </value>
        public new string Content { get; private set; }

        /// <summary>
        /// Gets the encoding of the text body content of this response.
        /// </summary>
        /// <value>
        /// Encoding of the response body from the <c>Content-Type</c> header,
        /// or <see langword="null"/> if the encoding could not be determined.
        /// </value>
        public Encoding? Encoding { get; private set; }

        private WebCmdletElementCollection? _inputFields;

        /// <summary>
        /// Gets the HTML input field elements parsed from <see cref="Content"/>.
        /// </summary>
        public WebCmdletElementCollection InputFields
        {
            get
            {
                if (_inputFields == null)
                {
                    List<PSObject> parsedFields = new();
                    MatchCollection fieldMatch = HtmlParser.InputFieldRegex.Matches(Content);
                    foreach (Match field in fieldMatch)
                    {
                        parsedFields.Add(CreateHtmlObject(field.Value, "INPUT"));
                    }

                    _inputFields = new WebCmdletElementCollection(parsedFields);
                }

                return _inputFields;
            }
        }

        private WebCmdletElementCollection? _links;

        /// <summary>
        /// Gets the HTML a link elements parsed from <see cref="Content"/>.
        /// </summary>
        public WebCmdletElementCollection Links
        {
            get
            {
                if (_links == null)
                {
                    List<PSObject> parsedLinks = new();
                    MatchCollection linkMatch = HtmlParser.LinkRegex.Matches(Content);
                    foreach (Match link in linkMatch)
                    {
                        parsedLinks.Add(CreateHtmlObject(link.Value, "A"));
                    }

                    _links = new WebCmdletElementCollection(parsedLinks);
                }

                return _links;
            }
        }

        private WebCmdletElementCollection? _images;

        /// <summary>
        /// Gets the HTML img elements parsed from <see cref="Content"/>.
        /// </summary>
        public WebCmdletElementCollection Images
        {
            get
            {
                if (_images == null)
                {
                    List<PSObject> parsedImages = new();
                    MatchCollection imageMatch = HtmlParser.ImageRegex.Matches(Content);
                    foreach (Match image in imageMatch)
                    {
                        parsedImages.Add(CreateHtmlObject(image.Value, "IMG"));
                    }

                    _images = new WebCmdletElementCollection(parsedImages);
                }

                return _images;
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Reads the response content from the web response.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        [MemberNotNull(nameof(Content))]
        protected void InitializeContent(CancellationToken cancellationToken)
        {
            string? contentType = ContentHelper.GetContentType(BaseResponse);
            if (ContentHelper.IsText(contentType))
            {
                // Fill the Content buffer
                string? characterSet = WebResponseHelper.GetCharacterSet(BaseResponse);

                Content = StreamHelper.DecodeStream(RawContentStream, characterSet, out Encoding encoding, cancellationToken);
                Encoding = encoding;
            }
            else
            {
                Content = string.Empty;
            }
        }

        private static PSObject CreateHtmlObject(string html, string tagName)
        {
            PSObject elementObject = new();

            elementObject.Properties.Add(new PSNoteProperty("outerHTML", html));
            elementObject.Properties.Add(new PSNoteProperty("tagName", tagName));

            ParseAttributes(html, elementObject);

            return elementObject;
        }

        private void InitializeRawContent(HttpResponseMessage baseResponse)
        {
            StringBuilder raw = ContentHelper.GetRawContentHeader(baseResponse);
            raw.Append(Content);
            RawContent = raw.ToString();
        }

        private static void ParseAttributes(string outerHtml, PSObject elementObject)
        {
            // We might get an empty input for a directive from the HTML file
            if (!string.IsNullOrEmpty(outerHtml))
            {
                // Extract just the opening tag of the HTML element (omitting the closing tag and any contents,
                // including contained HTML elements)
                Match match = HtmlParser.TagRegex.Match(outerHtml);

                // Extract all the attribute specifications within the HTML element opening tag
                MatchCollection attribMatches = HtmlParser.AttribsRegex.Matches(match.Value);

                foreach (Match attribMatch in attribMatches)
                {
                    // Extract the name and value for this attribute (allowing for variations like single/double/no
                    // quotes, and no value at all)
                    Match nvMatches = HtmlParser.AttribNameValueRegex.Match(attribMatch.Value);
                    Debug.Assert(nvMatches.Groups.Count == 5);

                    // Name is always captured by group #1
                    string name = nvMatches.Groups[1].Value;

                    // The value (if any) is captured by group #2, #3, or #4, depending on quoting or lack thereof
                    string? value = null;
                    if (nvMatches.Groups[2].Success)
                    {
                        value = nvMatches.Groups[2].Value;
                    }
                    else if (nvMatches.Groups[3].Success)
                    {
                        value = nvMatches.Groups[3].Value;
                    }
                    else if (nvMatches.Groups[4].Success)
                    {
                        value = nvMatches.Groups[4].Value;
                    }

                    elementObject.Properties.Add(new PSNoteProperty(name, value));
                }
            }
        }

        #endregion Methods

        // This class is needed so the static Regexes are initialized only the first time they are used
        private static class HtmlParser
        {
            internal static readonly Regex AttribsRegex = new Regex(@"(?<=\s+)([^""'>/=\s\p{Cc}]+(\s*=\s*(?:"".*?""|'.*?'|[^'"">\s]+))?)", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

            internal static readonly Regex AttribNameValueRegex = new Regex(@"([^""'>/=\s\p{Cc}]+)(?:\s*=\s*(?:""(.*?)""|'(.*?)'|([^'"">\s]+)))?", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

            internal static readonly Regex ImageRegex = new Regex(@"<img\s[^>]*?>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

            internal static readonly Regex InputFieldRegex = new Regex(@"<input\s+[^>]*(/?>|>.*?</input>)", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

            internal static readonly Regex LinkRegex = new Regex(@"<a\s+[^>]*(/>|>.*?</a>)", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

            internal static readonly Regex TagRegex = new Regex(@"<\w+((\s+[^""'>/=\s\p{Cc}]+(\s*=\s*(?:"".*?""|'.*?'|[^'"">\s]+))?)+\s*|\s*)/?>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }
}
