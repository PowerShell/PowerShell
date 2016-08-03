/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using Microsoft.Management.Infrastructure;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    internal class MISerializer
    {
        internal InternalMISerializer internalSerializer;

        public MISerializer(int depth)
        {
            internalSerializer = new InternalMISerializer(depth);
        }

        public CimInstance Serialize(object source)
        {
            return internalSerializer.Serialize(source);
        }
    }
}