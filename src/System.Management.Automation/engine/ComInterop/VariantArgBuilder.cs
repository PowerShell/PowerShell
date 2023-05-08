// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Management.Automation.ComInterop
{
    internal class VariantArgBuilder : SimpleArgBuilder
    {
        private readonly bool _isWrapper;

        internal VariantArgBuilder(Type parameterType)
            : base(parameterType)
        {
            _isWrapper = parameterType == typeof(VariantWrapper);
        }

        internal override Expression Marshal(Expression parameter)
        {
            // parameter.WrappedObject
            if (_isWrapper)
            {
                parameter = Expression.Property(
                    Helpers.Convert(parameter, typeof(VariantWrapper)),
                    typeof(VariantWrapper).GetProperty(nameof(VariantWrapper.WrappedObject))
                );
            }

            return Helpers.Convert(parameter, typeof(object));
        }

        internal override Expression MarshalToRef(Expression parameter)
        {
            parameter = Marshal(parameter);

            // parameter == UnsafeMethods.GetVariantForObject(parameter);
            return Expression.Call(
                typeof(UnsafeMethods).GetMethod(nameof(UnsafeMethods.GetVariantForObject), BindingFlags.Static | BindingFlags.NonPublic),
                parameter
            );
        }

        internal override Expression UnmarshalFromRef(Expression value)
        {
            // value == IntPtr.Zero ? null : Marshal.GetObjectForNativeVariant(value);

            Expression unmarshal = Expression.Call(
                typeof(UnsafeMethods).GetMethod(nameof(UnsafeMethods.GetObjectForVariant)),
                value
            );

            if (_isWrapper)
            {
                unmarshal = Expression.New(
                    typeof(VariantWrapper).GetConstructor(new Type[] { typeof(object) }),
                    unmarshal
                );
            }

            return base.UnmarshalFromRef(unmarshal);
        }
    }
}
