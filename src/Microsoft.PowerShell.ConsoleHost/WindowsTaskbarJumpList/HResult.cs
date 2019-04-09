// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.PowerShell
{
    /// <summary>
    /// HRESULT Wrapper    
    /// </summary>    
    internal enum HResult
    {
        /// <summary>     
        /// S_OK          
        /// </summary>    
        Ok = 0x0000,

        /// <summary>
        /// S_FALSE.
        /// </summary>        
        False = 0x0001,

        /// <summary>
        /// E_INVALIDARG.
        /// </summary>
        InvalidArguments = unchecked((int)0x80070057),

        /// <summary>
        /// E_OUTOFMEMORY.
        /// </summary>
        OutOfMemory = unchecked((int)0x8007000E),

        /// <summary>
        /// E_NOINTERFACE.
        /// </summary>
        NoInterface = unchecked((int)0x80004002),

        /// <summary>
        /// E_FAIL.
        /// </summary>
        Fail = unchecked((int)0x80004005),

        /// <summary>
        /// E_ELEMENTNOTFOUND.
        /// </summary>
        ElementNotFound = unchecked((int)0x80070490),

        /// <summary>
        /// TYPE_E_ELEMENTNOTFOUND.
        /// </summary>
        TypeElementNotFound = unchecked((int)0x8002802B),

        /// <summary>
        /// NO_OBJECT.
        /// </summary>
        NoObject = unchecked((int)0x800401E5),

        /// <summary>
        /// Win32 Error code: ERROR_CANCELLED.
        /// </summary>
        Win32ErrorCanceled = 1223,

        /// <summary>
        /// ERROR_CANCELLED.
        /// </summary>
        Canceled = unchecked((int)0x800704C7),

        /// <summary>
        /// The requested resource is in use.
        /// </summary>
        ResourceInUse = unchecked((int)0x800700AA),

        /// <summary>
        /// The requested resources is read-only.
        /// </summary>
        AccessDenied = unchecked((int)0x80030005)
    }
}
