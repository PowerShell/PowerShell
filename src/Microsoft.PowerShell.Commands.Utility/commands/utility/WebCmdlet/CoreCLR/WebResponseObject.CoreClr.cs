#if CORECLR

/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System;
using System.Text;
using System.Net.Http;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// WebResponseObject
    /// </summary>
    public partial class WebResponseObject
    {
        #region Properties

        /// <summary>
        /// gets or sets the BaseResponse property
        /// </summary>
        public HttpResponseMessage BaseResponse { get; set; }

        /// <summary>
        /// gets the Headers property
        /// </summary>
        public Dictionary<string, IEnumerable<string>> Headers
        {
            get
            {
                if(_headers == null)
                {
                    _headers = WebResponseHelper.GetHeadersDictionary(BaseResponse);
                }

                return _headers;
            }
        }

        private Dictionary<string, IEnumerable<string>> _headers = null;

        /// <summary>
        /// gets the RelationLink property
        /// </summary>
        public Dictionary<string, string> RelationLink { get; internal set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor for WebResponseObject
        /// </summary>
        /// <param name="response"></param>
        public WebResponseObject(HttpResponseMessage response)
            : this(response, null)
        { }

        /// <summary>
        /// Constructor for WebResponseObject with contentStream
        /// </summary>
        /// <param name="response"></param>
        /// <param name="contentStream"></param>
        public WebResponseObject(HttpResponseMessage response, Stream contentStream)
        {
            SetResponse(response, contentStream);
            InitializeContent();
            InitializeRawContent(response);
        }

        #endregion Constructors

        #region Methods

        private void InitializeRawContent(HttpResponseMessage baseResponse)
        {
            StringBuilder raw = ContentHelper.GetRawContentHeader(baseResponse);

            // Use ASCII encoding for the RawContent visual view of the content.
            if (Content.Length > 0)
            {
                raw.Append(this.ToString());
            }

            this.RawContent = raw.ToString();
        }

        private void SetResponse(HttpResponseMessage response, Stream contentStream)
        {
            if (null == response) { throw new ArgumentNullException("response"); }

            BaseResponse = response;

            MemoryStream ms = contentStream as MemoryStream;
            if (null != ms)
            {
                _rawContentStream = ms;
            }
            else
            {
                Stream st = contentStream;
                if (contentStream == null)
                {
                    st = StreamHelper.GetResponseStream(response);
                }

                long contentLength = response.Content.Headers.ContentLength.Value;
                if (0 >= contentLength)
                {
                    contentLength = StreamHelper.DefaultReadBuffer;
                }
                int initialCapacity = (int)Math.Min(contentLength, StreamHelper.DefaultReadBuffer);
                _rawContentStream = new WebResponseContentMemoryStream(st, initialCapacity, null);
            }
            // set the position of the content stream to the beginning
            _rawContentStream.Position = 0;
        }

        #endregion
    }
}
#endif