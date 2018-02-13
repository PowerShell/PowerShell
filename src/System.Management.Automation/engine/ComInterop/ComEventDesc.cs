// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !SILVERLIGHT // ComObject

namespace System.Management.Automation.ComInterop
{
    internal class ComEventDesc
    {
        internal Guid sourceIID;
        internal int dispid;
    };
}

#endif

