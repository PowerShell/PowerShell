/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

#if !SILVERLIGHT
#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif

using System.Collections.Generic;
using System.Dynamic;
//using Microsoft.Scripting.Utils;
//using AstUtils = Microsoft.Scripting.Ast.Utils;
using AstUtils = System.Management.Automation.Interpreter.Utils;

namespace System.Management.Automation.ComInterop
{
    internal sealed class TypeLibInfoMetaObject : DynamicMetaObject
    {
        private readonly ComTypeLibInfo _info;

        internal TypeLibInfoMetaObject(Expression expression, ComTypeLibInfo info)
            : base(expression, BindingRestrictions.Empty, info)
        {
            _info = info;
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            string name = binder.Name;

            if (name == _info.Name)
            {
                name = "TypeLibDesc";
            }
            else if (name != "Guid" &&
              name != "Name" &&
              name != "VersionMajor" &&
              name != "VersionMinor")
            {
                return binder.FallbackGetMember(this);
            }

            return new DynamicMetaObject(
                Expression.Convert(
                    Expression.Property(
                        AstUtils.Convert(Expression, typeof(ComTypeLibInfo)),
                        typeof(ComTypeLibInfo).GetProperty(name)
                    ),
                    typeof(object)
                ),
                ComTypeLibInfoRestrictions(this)
            );
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return _info.GetMemberNames();
        }

        private BindingRestrictions ComTypeLibInfoRestrictions(params DynamicMetaObject[] args)
        {
            return BindingRestrictions.Combine(args).Merge(BindingRestrictions.GetTypeRestriction(Expression, typeof(ComTypeLibInfo)));
        }
    }
}

#endif

