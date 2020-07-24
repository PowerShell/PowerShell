// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Dynamic;
using System.Linq.Expressions;

namespace System.Management.Automation.ComInterop
{
    internal class DispCallableMetaObject : DynamicMetaObject
    {
        private readonly DispCallable _callable;

        internal DispCallableMetaObject(Expression expression, DispCallable callable)
            : base(expression, BindingRestrictions.Empty, callable)
        {
            _callable = callable;
        }

        public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
        {
            return BindGetOrInvoke(indexes, binder.CallInfo) ??
                base.BindGetIndex(binder, indexes);
        }

        public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
        {
            return BindGetOrInvoke(args, binder.CallInfo) ??
                base.BindInvoke(binder, args);
        }

        private DynamicMetaObject BindGetOrInvoke(DynamicMetaObject[] args, CallInfo callInfo)
        {
            IDispatchComObject target = _callable.DispatchComObject;
            string name = _callable.MemberName;

            if (target.TryGetMemberMethod(name, out ComMethodDesc method) ||
                target.TryGetMemberMethodExplicit(name, out method))
            {

                bool[] isByRef = ComBinderHelpers.ProcessArgumentsForCom(ref args);
                return BindComInvoke(method, args, callInfo, isByRef);
            }
            return null;
        }

        public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
        {
            IDispatchComObject target = _callable.DispatchComObject;
            string name = _callable.MemberName;

            bool holdsNull = value.Value == null && value.HasValue;
            if (target.TryGetPropertySetter(name, out ComMethodDesc method, value.LimitType, holdsNull) ||
                target.TryGetPropertySetterExplicit(name, out method, value.LimitType, holdsNull))
            {

                bool[] isByRef = ComBinderHelpers.ProcessArgumentsForCom(ref indexes);
                isByRef = isByRef.AddLast(false);
                DynamicMetaObject result = BindComInvoke(method, indexes.AddLast(value), binder.CallInfo, isByRef);

                // Make sure to return the value; some languages need it.
                return new DynamicMetaObject(
                    Expression.Block(result.Expression, Expression.Convert(value.Expression, typeof(object))),
                    result.Restrictions
                );
            }

            return base.BindSetIndex(binder, indexes, value);
        }

        private DynamicMetaObject BindComInvoke(ComMethodDesc method, DynamicMetaObject[] indexes, CallInfo callInfo, bool[] isByRef)
        {
            Expression callable = Expression;
            Expression dispCall = Helpers.Convert(callable, typeof(DispCallable));

            return new ComInvokeBinder(
                callInfo,
                indexes,
                isByRef,
                DispCallableRestrictions(),
                Expression.Constant(method),
                Expression.Property(
                    dispCall,
                    typeof(DispCallable).GetProperty(nameof(DispCallable.DispatchObject))
                ),
                method
            ).Invoke();
        }

        private BindingRestrictions DispCallableRestrictions()
        {
            Expression callable = Expression;

            BindingRestrictions callableTypeRestrictions = BindingRestrictions.GetTypeRestriction(callable, typeof(DispCallable));
            Expression dispCall = Helpers.Convert(callable, typeof(DispCallable));
            MemberExpression dispatch = Expression.Property(dispCall, typeof(DispCallable).GetProperty(nameof(DispCallable.DispatchComObject)));
            MemberExpression dispId = Expression.Property(dispCall, typeof(DispCallable).GetProperty(nameof(DispCallable.DispId)));

            BindingRestrictions dispatchRestriction = IDispatchMetaObject.IDispatchRestriction(dispatch, _callable.DispatchComObject.ComTypeDesc);
            BindingRestrictions memberRestriction = BindingRestrictions.GetExpressionRestriction(
                Expression.Equal(dispId, Expression.Constant(_callable.DispId))
            );

            return callableTypeRestrictions.Merge(dispatchRestriction).Merge(memberRestriction);
        }
    }
}
