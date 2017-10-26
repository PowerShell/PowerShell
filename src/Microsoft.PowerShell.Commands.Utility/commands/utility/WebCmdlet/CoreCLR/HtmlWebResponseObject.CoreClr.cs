#if CORECLR

/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;
using System.Net.Http;
using System.IO;
using System.Text;
using ExecutionContext = System.Management.Automation.ExecutionContext;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Response object for html content
    /// </summary>
    public partial class HtmlWebResponseObject : WebResponseObject, IDisposable
    {
        #region Constructors

        /// <summary>
        /// Constructor for HtmlWebResponseObject
        /// </summary>
        /// <param name="response"></param>
        /// <param name="executionContext"></param>
        internal HtmlWebResponseObject(HttpResponseMessage response, ExecutionContext executionContext)
            : this(response, null, executionContext)
        { }

        /// <summary>
        /// Constructor for HtmlWebResponseObject with memory stream
        /// </summary>
        /// <param name="response"></param>
        /// <param name="contentStream"></param>        /// <param name="executionContext"></param>
        internal HtmlWebResponseObject(HttpResponseMessage response, Stream contentStream, ExecutionContext executionContext)
            : base(response, contentStream)
        {
            if (executionContext == null)
            {
                throw PSTraceSource.NewArgumentNullException("executionContext");
            }

            _executionContext = executionContext;
            InitializeContent();
            InitializeRawContent(response);
        }

        #endregion Constructors

       #region Properties

        /// <summary>
        /// Gets the Encoding that was used to decode the Content
        /// </summary>
        /// <value>
        /// The Encoding used to decode the Content; otherwise, a null reference if the content is not text.
        /// </value>
        public Encoding Encoding { get; private set; }

        #endregion Properties

        #region Methods

        // NOTE: Currently this code path is not enabled.
        // See FillRequestStream in WebRequestPSCmdlet.CoreClr.cs and
        // GetResponseObject in WebResponseObjectFactory.CoreClr.cs for details.
        private void InitializeContent()
        {
            string contentType = ContentHelper.GetContentType(BaseResponse);
            string content = null;
            if (ContentHelper.IsText(contentType))
            {
                Encoding encoding = null;
                // fill the Content buffer
                string characterSet = WebResponseHelper.GetCharacterSet(BaseResponse);
                this.Content = StreamHelper.DecodeStream(RawContentStream, characterSet, out encoding);
                this.Encoding = encoding;
            }
            else
            {
                this.Content = string.Empty;
            }
        }

        private void InitializeRawContent(HttpResponseMessage baseResponse)
        {
            StringBuilder raw = ContentHelper.GetRawContentHeader(baseResponse);
            if (null != Content)
            {
                raw.Append(Content);
            }
            this.RawContent = raw.ToString();
        }

        /// <summary>
        /// Dispose the the instance of the class.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
#endif