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
                typeof(Decimal).GetMethod("ToOACurrency"),
                Marshal(parameter)
            );
        }

        internal override Expression UnmarshalFromRef(Expression value)
        {
            // Decimal.FromOACurrency(value)
            return base.UnmarshalFromRef(
                Expression.New(
                    typeof(CurrencyWrapper).GetConstructor(new Type[] { typeof(Decimal) }),
                    Expression.Call(
                        typeof(Decimal).GetMethod("FromOACurrency"),
                        value
                    )
                )
            );
        }
    }
}

#endif

