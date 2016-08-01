/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

#if !SILVERLIGHT // ComObject

using System;

namespace System.Management.Automation.ComInterop
{
    internal class ComEventDesc
    {
        internal Guid sourceIID;
        internal int dispid;
    };
}

#endif

