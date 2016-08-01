/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Text;
using System.Net;
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
        public WebResponse BaseResponse { get; set; }

        /// <summary>
        /// gets the Headers property
        /// </summary>
        public Dictionary<string, string> Headers
        {
            get
            {
                Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (string key in BaseResponse.Headers.Keys)
                {
                    headers[key] = BaseResponse.Headers[key];
                }

                return headers;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor for WebResponseObject
        /// </summary>
        /// <param name="response"></param>
        public WebResponseObject(WebResponse response)
            : this(response, null)
        { }

        /// <summary>
        /// Constructor for WebResponseObject with contentStream
        /// </summary>
        /// <param name="response"></param>
        /// <param name="contentStream"></param>
        public WebResponseObject(WebResponse response, Stream contentStream)
        {
            SetResponse(response, contentStream);
            InitializeContent();
            InitializeRawContent(response);
        }

        #endregion Constructors

        #region Methods

        private void InitializeRawContent(WebResponse baseResponse)
        {
            StringBuilder raw = ContentHelper.GetRawContentHeader(baseResponse);

            // Use ASCII encoding for the RawContent visual view of the content.
            if (Content.Length > 0)
            {
                raw.Append(this.ToString());
            }

            this.RawContent = raw.ToString();
        }

        private void SetResponse(WebResponse response, Stream contentStream)
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

                long contentLength = response.ContentLength;
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
