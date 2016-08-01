/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Diagnostics.CodeAnalysis;
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
        /// gets or protected sets the Content property
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public byte[] Content { get; protected set; }

        /// <summary>
        /// gets the StatusCode property
        /// </summary>
        public int StatusCode
        {
            get { return (WebResponseHelper.GetStatusCode(BaseResponse)); }
        }

        /// <summary>
        /// gets the StatusDescription property
        /// </summary>
        public string StatusDescription
        {
            get { return (WebResponseHelper.GetStatusDescription(BaseResponse)); }
        }

        private MemoryStream _rawContentStream;
        /// <summary>
        /// gets the RawContentStream property
        /// </summary>
        public MemoryStream RawContentStream
        {
            get { return (_rawContentStream); }
        }

        /// <summary>
        /// gets the RawContentLength property
        /// </summary>
        public long RawContentLength
        {
            get { return (null == RawContentStream ? -1 : RawContentStream.Length); }
        }

        /// <summary>
        /// gets or protected sets the RawContent property
        /// </summary>
        public string RawContent { get; protected set; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Reads the response content from the web response.
        /// </summary>
        private void InitializeContent()
        {
            this.Content = this.RawContentStream.ToArray();
        }

        private bool IsPrintable(char c)
        {
            return (Char.IsLetterOrDigit(c) || Char.IsPunctuation(c) || Char.IsSeparator(c) || Char.IsSymbol(c) || Char.IsWhiteSpace(c));
        }

        /// <summary>
        /// Returns the string representation of this web response.
        /// </summary>
        /// <returns>The string representation of this web response.</returns>
        public sealed override string ToString()
        {
            char[] stringContent = System.Text.Encoding.ASCII.GetChars(Content);
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
