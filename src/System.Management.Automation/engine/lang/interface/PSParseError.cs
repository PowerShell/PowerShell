// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/********************************************************************++

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
    public sealed class PSParseError
    {
        internal PSParseError(RuntimeException rte)
        {
            Dbg.Assert(rte != null, "exception argument should not be null");
            Dbg.Assert(rte.ErrorToken != null, "token for exception should not be null");

            Message = rte.Message;
            Token = new PSToken(rte.ErrorToken);
        }

        internal PSParseError(Language.ParseError error)
        {
            Message = error.Message;
            Token = new PSToken(error.Extent);
        }

        /// <summary>
        /// The token that indicates the error location.
        /// </summary>
        /// <remarks>
        /// This can either be the real token at which place the error happens or a position
        /// token indicating the location where error happens.
        /// </remarks>
        public PSToken Token { get; }

        /// <summary>
        /// Error message.
        /// </summary>
        public string Message { get; }
    }
}
