/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

#if !SILVERLIGHT
#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif
using System.Diagnostics;

namespace System.Management.Automation.ComInterop
{
    /// <summary>
    /// SimpleArgBuilder produces the value produced by the user as the argument value.  It
    /// also tracks information about the original parameter and is used to create extended
    /// methods for params arrays and param dictionary functions.
    /// </summary>
    internal class SimpleArgBuilder : ArgBuilder
    {
        internal SimpleArgBuilder(Type parameterType)
        {
            ParameterType = parameterType;
        }

        internal Type ParameterType { get; }

        internal override Expression Marshal(Expression parameter)
        {
            Debug.Assert(parameter != null);
            return Helpers.Convert(parameter, ParameterType);
        }

        internal override Expression UnmarshalFromRef(Expression newValue)
        {
            Debug.Assert(newValue != null && newValue.Type.IsAssignableFrom(ParameterType));

            return base.UnmarshalFromRef(newValue);
        }
    }
}

#endif

