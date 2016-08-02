/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

#if !SILVERLIGHT // ComObject
#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif
using System.Diagnostics;

namespace System.Management.Automation.ComInterop
{
    internal sealed class DateTimeArgBuilder : SimpleArgBuilder
    {
        internal DateTimeArgBuilder(Type parameterType)
            : base(parameterType)
        {
            Debug.Assert(parameterType == typeof(DateTime));
        }

        internal override Expression MarshalToRef(Expression parameter)
        {
            // parameter.ToOADate()
            return Expression.Call(
                Marshal(parameter),
                typeof(DateTime).GetMethod("ToOADate")
            );
        }

        internal override Expression UnmarshalFromRef(Expression value)
        {
            // DateTime.FromOADate(value)
            return base.UnmarshalFromRef(
                Expression.Call(
                    typeof(DateTime).GetMethod("FromOADate"),
                    value
                )
            );
        }
    }
}

#endif

