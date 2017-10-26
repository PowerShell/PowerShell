/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// enums for web request method.
    /// </summary>
    public enum WebRequestMethod
    {
        /// <summary>
        /// Default method
        /// </summary>
        Default,

        /// <summary>
        /// GET method
        /// </summary>
        Get,

        /// <summary>
        /// HEAD method
        /// </summary>
        Head,

        /// <summary>
        /// POST method
        /// </summary>
        Post,

        /// <summary>
        /// PUT method
        /// </summary>
        Put,

        /// <summary>
        /// DELETE method
        /// </summary>
        Delete,

        /// <summary>
        /// TRACE method
        /// </summary>
        Trace,

        /// <summary>
        /// OPTIONS method
        /// </summary>
        Options,

        /// <summary>
        /// MERGE method
        /// </summary>
        Merge,

        /// <summary>
        /// PATCH method
        /// </summary>
        Patch,
    }
}
