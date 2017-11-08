/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System.Net.Http;
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
        /// Constructor for BasicHtmlWebResponseObject
        /// </summary>
        /// <param name="response"></param>
        public BasicHtmlWebResponseObject(HttpResponseMessage response)
            : this(response, null)
        { }

        /// <summary>
        /// Constructor for HtmlWebResponseObject with memory stream
        /// </summary>
        /// <param name="response"></param>
        /// <param name="contentStream"></param>
        public BasicHtmlWebResponseObject(HttpResponseMessage response, Stream contentStream)
            : base(response, contentStream)
        {
            EnsureHtmlParser();
            InitializeContent();
            InitializeRawContent(response);
        }

        #endregion Constructors

        #region Methods

        private void InitializeRawContent(HttpResponseMessage baseResponse)
        {
            StringBuilder raw = ContentHelper.GetRawContentHeader(baseResponse);
            raw.Append(Content);
            this.RawContent = raw.ToString();
        }

        #endregion Methods
    }
}
