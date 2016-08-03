/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

#if !SILVERLIGHT
#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif

namespace System.Management.Automation.ComInterop
{
    /// <summary>
    /// ArgBuilder which always produces null.  
    /// </summary>
    internal sealed class NullArgBuilder : ArgBuilder
    {
        internal NullArgBuilder() { }

        internal override Expression Marshal(Expression parameter)
        {
            return Expression.Constant(null);
        }
    }
}

#endif

