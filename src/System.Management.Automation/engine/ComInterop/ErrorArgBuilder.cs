// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

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
                nameof(ErrorWrapper.ErrorCode)
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
