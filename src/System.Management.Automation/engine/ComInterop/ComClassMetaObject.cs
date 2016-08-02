/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

#if !SILVERLIGHT // ComObject

#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif

using System.Dynamic;
//using AstUtils = Microsoft.Scripting.Ast.Utils;
using AstUtils = System.Management.Automation.Interpreter.Utils;

namespace System.Management.Automation.ComInterop
{
    internal class ComClassMetaObject : DynamicMetaObject
    {
        internal ComClassMetaObject(Expression expression, ComTypeClassDesc cls)
            : base(expression, BindingRestrictions.Empty, cls)
        {
        }

        public override DynamicMetaObject BindCreateInstance(CreateInstanceBinder binder, DynamicMetaObject[] args)
        {
            return new DynamicMetaObject(
                Expression.Call(
                    AstUtils.Convert(Expression, typeof(ComTypeClassDesc)),
                    typeof(ComTypeClassDesc).GetMethod("CreateInstance")
                ),
                BindingRestrictions.Combine(args).Merge(
                    BindingRestrictions.GetTypeRestriction(Expression, typeof(ComTypeClassDesc))
                )
            );
        }
    }
}

#endif

