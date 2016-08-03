/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
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
    //
    // ComFallbackMetaObject just delegates everything to the binder.
    //
    // Note that before performing FallBack on a ComObject we need to unwrap it so that
    // binder would act upon the actual object (typically Rcw)
    //
    // Also: we don't need to implement these for any operations other than those
    // supported by ComBinder
    internal class ComFallbackMetaObject : DynamicMetaObject
    {
        internal ComFallbackMetaObject(Expression expression, BindingRestrictions restrictions, object arg)
            : base(expression, restrictions, arg)
        {
        }

        public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
        {
            return binder.FallbackGetIndex(UnwrapSelf(), indexes);
        }

        public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
        {
            return binder.FallbackSetIndex(UnwrapSelf(), indexes, value);
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            return binder.FallbackGetMember(UnwrapSelf());
        }

        public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
        {
            return binder.FallbackInvokeMember(UnwrapSelf(), args);
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            return binder.FallbackSetMember(UnwrapSelf(), value);
        }

        protected virtual ComUnwrappedMetaObject UnwrapSelf()
        {
            return new ComUnwrappedMetaObject(
                ComObject.RcwFromComObject(Expression),
                Restrictions.Merge(ComBinderHelpers.GetTypeRestrictionForDynamicMetaObject(this)),
                ((ComObject)Value).RuntimeCallableWrapper
            );
        }
    }

    // This type exists as a signal type, so ComBinder knows not to try to bind
    // again when we're trying to fall back
    internal sealed class ComUnwrappedMetaObject : DynamicMetaObject
    {
        internal ComUnwrappedMetaObject(Expression expression, BindingRestrictions restrictions, object value)
            : base(expression, restrictions, value)
        {
        }
    }
}

#endif

