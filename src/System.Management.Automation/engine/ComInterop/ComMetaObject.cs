/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

#if !SILVERLIGHT
#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif
using System.Dynamic;

//using Microsoft.Scripting.Utils;

namespace System.Management.Automation.ComInterop
{
    // Note: we only need to support the operations used by ComBinder
    internal class ComMetaObject : DynamicMetaObject
    {
        internal ComMetaObject(Expression expression, BindingRestrictions restrictions, object arg)
            : base(expression, restrictions, arg)
        {
        }

        public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
        {
            return binder.Defer(args.AddFirst(WrapSelf()));
        }

        public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
        {
            return binder.Defer(args.AddFirst(WrapSelf()));
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            return binder.Defer(WrapSelf());
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            return binder.Defer(WrapSelf(), value);
        }

        public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
        {
            return binder.Defer(WrapSelf(), indexes);
        }

        public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
        {
            return binder.Defer(WrapSelf(), indexes.AddLast(value));
        }

        private DynamicMetaObject WrapSelf()
        {
            return new DynamicMetaObject(
                ComObject.RcwToComObject(Expression),
                BindingRestrictions.GetExpressionRestriction(
                    Expression.Call(
                        typeof(ComObject).GetMethod("IsComObject", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic),
                        Helpers.Convert(Expression, typeof(object))
                    )
                )
            );
        }
    }
}

#endif

