/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

#if !SILVERLIGHT
#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif

using System.Collections.Generic;
using System.Dynamic;
//using AstUtils = Microsoft.Scripting.Ast.Utils;
using AstUtils = System.Management.Automation.Interpreter.Utils;

namespace System.Management.Automation.ComInterop
{
    internal class TypeLibMetaObject : DynamicMetaObject
    {
        private readonly ComTypeLibDesc _lib;

        internal TypeLibMetaObject(Expression expression, ComTypeLibDesc lib)
            : base(expression, BindingRestrictions.Empty, lib)
        {
            _lib = lib;
        }

        private DynamicMetaObject TryBindGetMember(string name)
        {
            if (_lib.HasMember(name))
            {
                BindingRestrictions restrictions =
                    BindingRestrictions.GetTypeRestriction(
                        Expression, typeof(ComTypeLibDesc)
                    ).Merge(
                        BindingRestrictions.GetExpressionRestriction(
                            Expression.Equal(
                                Expression.Property(
                                    AstUtils.Convert(
                                        Expression, typeof(ComTypeLibDesc)
                                    ),
                                    typeof(ComTypeLibDesc).GetProperty("Guid")
                                ),
                                AstUtils.Constant(_lib.Guid)
                            )
                        )
                    );

                return new DynamicMetaObject(
                    AstUtils.Constant(
                        ((ComTypeLibDesc)Value).GetTypeLibObjectDesc(name)
                    ),
                    restrictions
                );
            }

            return null;
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            return TryBindGetMember(binder.Name) ?? base.BindGetMember(binder);
        }

        public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
        {
            var result = TryBindGetMember(binder.Name);
            if (result != null)
            {
                return binder.FallbackInvoke(result, args, null);
            }

            return base.BindInvokeMember(binder, args);
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return _lib.GetMemberNames();
        }
    }
}

#endif

