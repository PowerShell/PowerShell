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

namespace System.Management.Automation.ComInterop
{
    internal class UnknownArgBuilder : SimpleArgBuilder
    {
        private readonly bool _isWrapper;

        internal UnknownArgBuilder(Type parameterType)
            : base(parameterType)
        {
            _isWrapper = parameterType == typeof(UnknownWrapper);
        }

        internal override Expression Marshal(Expression parameter)
        {
            parameter = base.Marshal(parameter);

            // parameter.WrappedObject
            if (_isWrapper)
            {
                parameter = Expression.Property(
                    Helpers.Convert(parameter, typeof(UnknownWrapper)),
                    typeof(UnknownWrapper).GetProperty("WrappedObject")
                );
            };

            return Helpers.Convert(parameter, typeof(object));
        }

        internal override Expression MarshalToRef(Expression parameter)
        {
            parameter = Marshal(parameter);

            // parameter == null ? IntPtr.Zero : Marshal.GetIUnknownForObject(parameter);
            return Expression.Condition(
                Expression.Equal(parameter, Expression.Constant(null)),
                Expression.Constant(IntPtr.Zero),
                Expression.Call(
                    typeof(Marshal).GetMethod("GetIUnknownForObject"),
                    parameter
                )
            );
        }


        internal override Expression UnmarshalFromRef(Expression value)
        {
            // value == IntPtr.Zero ? null : Marshal.GetObjectForIUnknown(value);
            Expression unmarshal = Expression.Condition(
                Expression.Equal(value, Expression.Constant(IntPtr.Zero)),
                Expression.Constant(null),
                Expression.Call(
                    typeof(Marshal).GetMethod("GetObjectForIUnknown"),
                    value
                )
            );

            if (_isWrapper)
            {
                unmarshal = Expression.New(
                    typeof(UnknownWrapper).GetConstructor(new Type[] { typeof(object) }),
                    unmarshal
                );
            };

            return base.UnmarshalFromRef(unmarshal);
        }
    }
}

#endif

