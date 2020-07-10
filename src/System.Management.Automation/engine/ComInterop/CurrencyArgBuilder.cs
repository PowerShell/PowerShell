// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !SILVERLIGHT // ComObject
#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Management.Automation.ComInterop
{
    internal sealed class CurrencyArgBuilder : SimpleArgBuilder
    {
        internal CurrencyArgBuilder(Type parameterType)
            : base(parameterType)
        {
            Debug.Assert(parameterType == typeof(CurrencyWrapper));
        }

        internal override Expression Marshal(Expression parameter)
        {
            // parameter.WrappedObject
            return Expression.Property(
                Helpers.Convert(base.Marshal(parameter), typeof(CurrencyWrapper)),
                "WrappedObject"
            );
        }

        internal override Expression MarshalToRef(Expression parameter)
        {
            // Decimal.ToOACurrency(parameter.WrappedObject)
            return Expression.Call(
                typeof(decimal).GetMethod("ToOACurrency"),
                Marshal(parameter)
            );
        }

        internal override Expression UnmarshalFromRef(Expression value)
        {
            // Decimal.FromOACurrency(value)
            return base.UnmarshalFromRef(
                Expression.New(
                    typeof(CurrencyWrapper).GetConstructor(new Type[] { typeof(decimal) }),
                    Expression.Call(
                        typeof(decimal).GetMethod("FromOACurrency"),
                        value
                    )
                )
            );
        }
    }
}

#endif

