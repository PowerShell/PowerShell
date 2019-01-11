// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !SILVERLIGHT // ComObject
#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif
using System.Diagnostics;

namespace System.Management.Automation.ComInterop
{
    internal sealed class BoolArgBuilder : SimpleArgBuilder
    {
        internal BoolArgBuilder(Type parameterType)
            : base(parameterType)
        {
            Debug.Assert(parameterType == typeof(bool));
        }

        internal override Expression MarshalToRef(Expression parameter)
        {
            // parameter  ? -1 : 0
            return Expression.Condition(
                Marshal(parameter),
                Expression.Constant((Int16)(-1)),
                Expression.Constant((Int16)0)
            );
        }

        internal override Expression UnmarshalFromRef(Expression value)
        {
            // parameter = temp != 0
            return base.UnmarshalFromRef(
                Expression.NotEqual(
                     value,
                     Expression.Constant((Int16)0)
                )
            );
        }
    }
}

#endif

