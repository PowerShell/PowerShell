/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

#if !SILVERLIGHT // ComObject

#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif
using System.Dynamic;

namespace System.Management.Automation.ComInterop
{
    internal sealed class ComTypeLibInfo : IDynamicMetaObjectProvider
    {
        private readonly ComTypeLibDesc _typeLibDesc;

        internal ComTypeLibInfo(ComTypeLibDesc typeLibDesc)
        {
            _typeLibDesc = typeLibDesc;
        }

        public string Name
        {
            get { return _typeLibDesc.Name; }
        }

        public Guid Guid
        {
            get { return _typeLibDesc.Guid; }
        }

        public short VersionMajor
        {
            get { return _typeLibDesc.VersionMajor; }
        }

        public short VersionMinor
        {
            get { return _typeLibDesc.VersionMinor; }
        }

        public ComTypeLibDesc TypeLibDesc
        {
            get { return _typeLibDesc; }
        }

        // TODO: internal
        public string[] GetMemberNames()
        {
            return new string[] { this.Name, "Guid", "Name", "VersionMajor", "VersionMinor" };
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return new TypeLibInfoMetaObject(parameter, this);
        }
    }
}

#endif

