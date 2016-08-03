/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

#if !SILVERLIGHT

#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif

namespace System.Management.Automation.ComInterop
{
    internal class ConvertArgBuilder : SimpleArgBuilder
    {
        private readonly Type _marshalType;

        internal ConvertArgBuilder(Type parameterType, Type marshalType)
            : base(parameterType)
        {
            _marshalType = marshalType;
        }

        internal override Expression Marshal(Expression parameter)
        {
            parameter = base.Marshal(parameter);
            return Expression.Convert(parameter, _marshalType);
        }

        internal override Expression UnmarshalFromRef(Expression newValue)
        {
            return base.UnmarshalFromRef(Expression.Convert(newValue, ParameterType));
        }
    }
}

#endif

