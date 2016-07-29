/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

#if !SILVERLIGHT // ComObject

namespace System.Management.Automation.ComInterop
{
    internal class ComTypeLibMemberDesc
    {
        private readonly ComType _kind;

        internal ComTypeLibMemberDesc(ComType kind)
        {
            _kind = kind;
        }

        public ComType Kind
        {
            get { return _kind; }
        }
    }
}

#endif

