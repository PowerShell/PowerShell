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
        internal ComTypeLibInfo(ComTypeLibDesc typeLibDesc)
        {
            TypeLibDesc = typeLibDesc;
        }

        public string Name
        {
            get { return TypeLibDesc.Name; }
        }

        public Guid Guid
        {
            get { return TypeLibDesc.Guid; }
        }

        public short VersionMajor
        {
            get { return TypeLibDesc.VersionMajor; }
        }

        public short VersionMinor
        {
            get { return TypeLibDesc.VersionMinor; }
        }

        public ComTypeLibDesc TypeLibDesc { get; }

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

