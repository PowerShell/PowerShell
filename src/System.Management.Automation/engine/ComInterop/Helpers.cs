// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif

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

            return Expression.Convert(expression, type);
        }
    }
}

