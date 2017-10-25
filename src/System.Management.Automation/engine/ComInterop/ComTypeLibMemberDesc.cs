/********************************************************************++
Copyright (c) Microsoft Corporation. All Rights Reserved.
--********************************************************************/

#if !SILVERLIGHT // ComObject

namespace System.Management.Automation.ComInterop
{
    internal class ComTypeLibMemberDesc
    {
        internal ComTypeLibMemberDesc(ComType kind)
        {
            Kind = kind;
        }

        public ComType Kind { get; }
    }
}

#endif

