/********************************************************************++
    Copyright (C) Microsoft Corporation, 2003

    Project:     PowerShell


    Contents:    PowerShell error interface for syntax editors 

    Classes:     System.Management.Automation.PSParseError

--********************************************************************/

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// This is a class that represents a syntax error from parsing. 
    /// </summary>
    sealed public class PSParseError
    {
        internal PSParseError(RuntimeException rte)
        {
            Dbg.Assert(rte != null, "exception argument should not be null");
            Dbg.Assert(rte.ErrorToken != null, "token for exception should not be null");

            _message = rte.Message;
            _psToken = new PSToken(rte.ErrorToken);
        }

        internal PSParseError(Language.ParseError error)
        {
            _message = error.Message;
            _psToken = new PSToken(error.Extent);
        }

        private PSToken _psToken;

        /// <summary>
        /// The token that indicates the error location. 
        /// </summary>
        /// <remarks>
        /// This can either be the real token at which place the error happens or a position
        /// token indicating the location where error happens. 
        /// </remarks>
        public PSToken Token
        {
            get
            {
                return _psToken;
            }
        }

        private string _message;

        /// <summary>
        /// Error message. 
        /// </summary>
        public string Message
        {
            get
            {
                return _message;
            }
        }
    }
}
