/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System.Net;
using System.IO;
using System.Text;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Response object for html content without DOM parsing
    /// </summary>
    public partial class BasicHtmlWebResponseObject : WebResponseObject
    {
        #region Constructors

        /// <summary>
        /// Constructor for HtmlWebResponseObject
        /// </summary>
        /// <param name="response"></param>
        public BasicHtmlWebResponseObject(WebResponse response)
            : this(response, null)
        { }

        /// <summary>
        /// Constructor for HtmlWebResponseObject with memory stream
        /// </summary>
        /// <param name="response"></param>
        /// <param name="contentStream"></param>
        public BasicHtmlWebResponseObject(WebResponse response, Stream contentStream)
            : base(response, contentStream)
        {
            EnsureHtmlParser();
            InitializeContent();
            InitializeRawContent(response);
        }

        #endregion Constructors

        #region Methods

        private void InitializeRawContent(WebResponse baseResponse)
        {
            StringBuilder raw = ContentHelper.GetRawContentHeader(baseResponse);
            raw.Append(Content);
            this.RawContent = raw.ToString();
        }

        #endregion Methods
    }
}
