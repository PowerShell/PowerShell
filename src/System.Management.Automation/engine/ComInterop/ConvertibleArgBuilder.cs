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
    internal class ConvertibleArgBuilder : ArgBuilder
    {
        internal ConvertibleArgBuilder()
        {
        }

        internal override Expression Marshal(Expression parameter)
        {
            return Helpers.Convert(parameter, typeof(IConvertible));
        }

        internal override Expression MarshalToRef(Expression parameter)
        {
            // we are not supporting convertible InOut
            throw Assert.Unreachable;
        }
    }
}

#endif

