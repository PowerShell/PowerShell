/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

#if !SILVERLIGHT // ComObject
#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif
using System.Runtime.InteropServices;
using System.Reflection;

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
                    typeof(VariantWrapper).GetProperty("WrappedObject")
                );
            };

            return Helpers.Convert(parameter, typeof(object));
        }

        internal override Expression MarshalToRef(Expression parameter)
        {
            parameter = Marshal(parameter);

            // parameter == UnsafeMethods.GetVariantForObject(parameter);
            return Expression.Call(
                typeof(UnsafeMethods).GetMethod("GetVariantForObject", BindingFlags.Static | System.Reflection.BindingFlags.NonPublic),
                parameter
            );
        }


        internal override Expression UnmarshalFromRef(Expression value)
        {
            // value == IntPtr.Zero ? null : Marshal.GetObjectForNativeVariant(value);

            Expression unmarshal = Expression.Call(
                typeof(UnsafeMethods).GetMethod("GetObjectForVariant"),
                value
            );

            if (_isWrapper)
            {
                unmarshal = Expression.New(
                    typeof(VariantWrapper).GetConstructor(new Type[] { typeof(object) }),
                    unmarshal
                );
            };

            return base.UnmarshalFromRef(unmarshal);
        }
    }
}

#endif

