// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !SILVERLIGHT
#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif
//using Microsoft.Scripting.Utils;
using System.Management.Automation.Interpreter;

namespace System.Management.Automation.ComInterop
{
    internal class ConversionArgBuilder : ArgBuilder
    {
        private SimpleArgBuilder _innerBuilder;
        private Type _parameterType;

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
            // we are not supporting conversion InOut
            throw Assert.Unreachable;
        }
    }
}

#endif

