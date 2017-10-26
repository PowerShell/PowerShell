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
using System.Diagnostics;

namespace System.Management.Automation.ComInterop
{
    internal class StringArgBuilder : SimpleArgBuilder
    {
        private readonly bool _isWrapper;

        internal StringArgBuilder(Type parameterType)
            : base(parameterType)
        {
            Debug.Assert(parameterType == typeof(string) ||
                        parameterType == typeof(BStrWrapper));

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
                    typeof(BStrWrapper).GetProperty("WrappedObject")
                );
            };

            return parameter;
        }

        internal override Expression MarshalToRef(Expression parameter)
        {
            parameter = Marshal(parameter);


            // Marshal.StringToBSTR(parameter)
            return Expression.Call(
                typeof(Marshal).GetMethod("StringToBSTR"),
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
                    typeof(Marshal).GetMethod("PtrToStringBSTR"),
                    value
                )
            );

            if (_isWrapper)
            {
                unmarshal = Expression.New(
                    typeof(BStrWrapper).GetConstructor(new Type[] { typeof(string) }),
                    unmarshal
                );
            };

            return base.UnmarshalFromRef(unmarshal);
        }
    }
}

#endif

