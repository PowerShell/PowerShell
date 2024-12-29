// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq.Expressions;

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
                Expression.Constant((short)(-1)),
                Expression.Constant((short)0)
            );
        }

        internal override Expression UnmarshalFromRef(Expression value)
        {
            //parameter = temp != 0
            return base.UnmarshalFromRef(
                Expression.NotEqual(
                     value,
                     Expression.Constant((short)0)
                )
            );
        }
    }
}
