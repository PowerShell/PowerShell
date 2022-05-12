// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

namespace System.Management.Automation.ComInterop
{
    internal class StringArgBuilder : SimpleArgBuilder
    {
        private readonly bool _isWrapper;

        internal StringArgBuilder(Type parameterType)
            : base(parameterType)
        {
            Debug.Assert(parameterType == typeof(string) || parameterType == typeof(BStrWrapper));
            _isWrapper = parameterType == typeof(BStrWrapper);
        }

        internal override Expression Marshal(Expression parameter)
        {
            parameter = base.Marshal(parameter);

            // parameter.WrappedObject
            if (_isWrapper)
            {
                parameter = Expression.Property(
                    Helpers.Convert(parameter, typeof(BStrWrapper)),
                    typeof(BStrWrapper).GetProperty(nameof(BStrWrapper.WrappedObject))
                );
            }

            return parameter;
        }

        internal override Expression MarshalToRef(Expression parameter)
        {
            parameter = Marshal(parameter);

            // Marshal.StringToBSTR(parameter)
            return Expression.Call(
                typeof(Marshal).GetMethod(nameof(System.Runtime.InteropServices.Marshal.StringToBSTR)),
                parameter
            );
        }

        internal override Expression UnmarshalFromRef(Expression value)
        {
            // value == IntPtr.Zero ? null : Marshal.PtrToStringBSTR(value);
            Expression unmarshal = Expression.Condition(
                Expression.Equal(value, Expression.Constant(IntPtr.Zero)),
                Expression.Constant(null, typeof(string)),   // default value
                Expression.Call(
                    typeof(Marshal).GetMethod(nameof(System.Runtime.InteropServices.Marshal.PtrToStringBSTR)),
                    value
                )
            );

            if (_isWrapper)
            {
                unmarshal = Expression.New(
                    typeof(BStrWrapper).GetConstructor(new Type[] { typeof(string) }),
                    unmarshal
                );
            }

            return base.UnmarshalFromRef(unmarshal);
        }
    }
}
