// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq.Expressions;

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

        protected Type ParameterType { get; }

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
