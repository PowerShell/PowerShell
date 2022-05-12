// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq.Expressions;
using System.Management.Automation.Interpreter;

namespace System.Management.Automation.ComInterop
{
    internal class ConversionArgBuilder : ArgBuilder
    {
        private readonly SimpleArgBuilder _innerBuilder;
        private readonly Type _parameterType;

        internal ConversionArgBuilder(Type parameterType, SimpleArgBuilder innerBuilder)
        {
            _parameterType = parameterType;
            _innerBuilder = innerBuilder;
        }

        internal override Expression Marshal(Expression parameter)
        {
            return _innerBuilder.Marshal(Helpers.Convert(parameter, _parameterType));
        }

        internal override Expression MarshalToRef(Expression parameter)
        {
            //we are not supporting conversion InOut
            throw Assert.Unreachable;
        }
    }
}
