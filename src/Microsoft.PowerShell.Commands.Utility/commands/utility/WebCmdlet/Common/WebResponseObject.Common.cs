// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// WebResponseObject.
    /// </summary>
    public class WebResponseObject
    {
        #region Properties

        /// <summary>
        /// Gets or sets the BaseResponse property.
        /// </summary>
        public HttpResponseMessage BaseResponse { get; set; }

        /// <summary>
        /// Gets or protected sets the response body content.
        /// </summary>
        public byte[]? Content { get; protected set; }

        /// <summary>
        /// Gets the Headers property.
        /// </summary>
        public Dictionary<string, IEnumerable<string>> Headers => _headers ??= WebResponseHelper.GetHeadersDictionary(BaseResponse);

        private Dictionary<string, IEnumerable<string>>? _headers;

        /// <summary>
        /// Gets or protected sets the full response content.
        /// </summary>
        /// <value>
        /// Full response content, including the HTTP status line, headers, and body.
        /// </value>
        public string? RawContent { get; protected set; }

        /// <summary>
        /// Gets the length (in bytes) of <see cref="RawContentStream"/>.
        /// </summary>
        public long RawContentLength => RawContentStream is null ? -1 : RawContentStream.Length;

        /// <summary>
        /// Gets or protected sets the response body content as a <see cref="MemoryStream"/>.
        /// </summary>
        public MemoryStream RawContentStream { get; protected set; }

        /// <summary>
        /// Gets the RelationLink property.
        /// </summary>
        public Dictionary<string, string>? RelationLink { get; internal set; }

        /// <summary>
        /// Gets the response status code.
        /// </summary>
        public int StatusCode => WebResponseHelper.GetStatusCode(BaseResponse);

        /// <summary>
        /// Gets the response status description.
        /// </summary>
        public string StatusDescription => WebResponseHelper.GetStatusDescription(BaseResponse);

        /// <summary>
        /// Gets or sets the output file path.
        /// </summary>
        public string? OutFile { get; internal set; }

        #endregion Properties

        #region Protected Fields

        /// <summary>
        /// Time permitted between reads or Timeout.InfiniteTimeSpan for no timeout.
        /// </summary>
        protected TimeSpan perReadTimeout;

        #endregion Protected Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WebResponseObject"/> class.
        /// </summary>
        /// <param name="response">The Http response.</param>
        /// <param name="perReadTimeout">Time permitted between reads or Timeout.InfiniteTimeSpan for no timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public WebResponseObject(HttpResponseMessage response, TimeSpan perReadTimeout, CancellationToken cancellationToken) : this(response, null, perReadTimeout, cancellationToken) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebResponseObject"/> class
        /// with the specified <paramref name="contentStream"/>.
        /// </summary>
        /// <param name="response">Http response.</param>
        /// <param name="contentStream">The http content stream.</param>
        /// <param name="perReadTimeout">Time permitted between reads or Timeout.InfiniteTimeSpan for no timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public WebResponseObject(HttpResponseMessage response, Stream? contentStream, TimeSpan perReadTimeout, CancellationToken cancellationToken)
        {
            this.perReadTimeout = perReadTimeout;
            SetResponse(response, contentStream, cancellationToken);
            InitializeContent();
            InitializeRawContent(response);
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Reads the response content from the web response.
        /// </summary>
        private void InitializeContent()
        {
            Content = RawContentStream.ToArray();
        }

        private void InitializeRawContent(HttpResponseMessage baseResponse)
        {
            StringBuilder raw = ContentHelper.GetRawContentHeader(baseResponse);

            // Use ASCII encoding for the RawContent visual view of the content.
            if (Content?.Length > 0)
            {
                raw.Append(ToString());
            }

            RawContent = raw.ToString();
        }

        private static bool IsPrintable(char c) => char.IsLetterOrDigit(c)
                                                || char.IsPunctuation(c)
                                                || char.IsSeparator(c)
                                                || char.IsSymbol(c)
                                                || char.IsWhiteSpace(c);

        [MemberNotNull(nameof(RawContentStream))]
        [MemberNotNull(nameof(BaseResponse))]
        private void SetResponse(HttpResponseMessage response, Stream? contentStream, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(response);

            BaseResponse = response;

            if (contentStream is MemoryStream ms)
            {
                RawContentStream = ms;
            }
            else
            {
                Stream st = contentStream ?? StreamHelper.GetResponseStream(response, cancellationToken);

                long contentLength = response.Content.Headers.ContentLength.GetValueOrDefault();
                if (contentLength <= 0)
                {
                    contentLength = StreamHelper.DefaultReadBuffer;
                }

                int initialCapacity = (int)Math.Min(contentLength, StreamHelper.DefaultReadBuffer);
                RawContentStream = new WebResponseContentMemoryStream(st, initialCapacity, cmdlet: null, response.Content.Headers.ContentLength.GetValueOrDefault(), perReadTimeout, cancellationToken);
            }

            // Set the position of the content stream to the beginning
            RawContentStream.Position = 0;
        }

        /// <summary>
        /// Returns the string representation of this web response.
        /// </summary>
        /// <returns>The string representation of this web response.</returns>
        public sealed override string ToString()
        {
            if (Content is null)
            {
                return string.Empty;
            }

            char[] stringContent = Encoding.ASCII.GetChars(Content);
            for (int counter = 0; counter < stringContent.Length; counter++)
            {
                if (!IsPrintable(stringContent[counter]))
                {
                    stringContent[counter] = '.';
                }
            }

            return new string(stringContent);
        }

        #endregion Methods
    }
}
