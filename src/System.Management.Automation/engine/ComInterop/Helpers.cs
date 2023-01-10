// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq.Expressions;

namespace System.Management.Automation.ComInterop
{
    // Miscellaneous helpers that don't belong anywhere else
    internal static class Helpers
    {
        internal static Expression Convert(Expression expression, Type type)
        {
            if (expression.Type == type)
            {
                return expression;
            }

            if (expression.Type == typeof(void))
            {
                return Expression.Block(expression, Expression.Default(type));
            }

            if (type == typeof(void))
            {
                return Expression.Block(expression, Expression.Empty());
            }

            return Expression.Convert(expression, type);
        }
    }

    internal static class Requires
    {
        [System.Diagnostics.Conditional("DEBUG")]
        internal static void NotNull(object value, string paramName)
        {
            ArgumentNullException.ThrowIfNull(value, paramName);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        internal static void Condition(bool precondition, string paramName)
        {
            if (!precondition)
            {
                throw new ArgumentException(paramName);
            }
        }
    }
}
