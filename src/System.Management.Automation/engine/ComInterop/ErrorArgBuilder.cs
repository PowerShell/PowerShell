/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

#if !SILVERLIGHT // ComObject

#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace System.Management.Automation.ComInterop
{
    internal class ErrorArgBuilder : SimpleArgBuilder
    {
        internal ErrorArgBuilder(Type parameterType)
            : base(parameterType)
        {
            Debug.Assert(parameterType == typeof(ErrorWrapper));
        }

        internal override Expression Marshal(Expression parameter)
        {
            // parameter.ErrorCode
            return Expression.Property(
                Helpers.Convert(base.Marshal(parameter), typeof(ErrorWrapper)),
                "ErrorCode"
            );
        }

        internal override Expression UnmarshalFromRef(Expression value)
        {
            // new ErrorWrapper(value)
            return base.UnmarshalFromRef(
                Expression.New(
                    typeof(ErrorWrapper).GetConstructor(new Type[] { typeof(int) }),
                    value
                )
            );
        }
    }
}

#endif

