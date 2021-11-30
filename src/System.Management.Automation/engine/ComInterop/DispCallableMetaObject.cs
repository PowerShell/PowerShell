// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation.Language;

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
                List<ParameterExpression> temps = new List<ParameterExpression>();
                List<Expression> initTemps = new List<Expression>();

                bool[] isByRef = ComBinderHelpers.ProcessArgumentsForCom(method, ref args, temps, initTemps);
                return BindComInvoke(method, args, callInfo, isByRef, temps, initTemps);
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
                List<ParameterExpression> temps = new List<ParameterExpression>();
                List<Expression> initTemps = new List<Expression>();

                bool[] isByRef = ComBinderHelpers.ProcessArgumentsForCom(method, ref indexes, temps, initTemps);
                isByRef = isByRef.AddLast(false);
                // Convert the value to the target type
                DynamicMetaObject updatedValue = new DynamicMetaObject(
                    value.CastOrConvertMethodArgument(
                        value.LimitType,
                        name,
                        "SetIndex",
                        allowCastingToByRefLikeType: false,
                        temps,
                        initTemps),
                    value.Restrictions);

                var result = BindComInvoke(method, indexes.AddLast(updatedValue), binder.CallInfo, isByRef, temps, initTemps);

                // Make sure to return the value; some languages need it.
                return new DynamicMetaObject(
                    Expression.Block(result.Expression, Expression.Convert(value.Expression, typeof(object))),
                    result.Restrictions
                );
            }

            return base.BindSetIndex(binder, indexes, value);
        }

        private DynamicMetaObject BindComInvoke(ComMethodDesc method, DynamicMetaObject[] indexes, CallInfo callInfo, bool[] isByRef,
            List<ParameterExpression> temps, List<Expression> initTemps)
        {
            Expression callable = Expression;
            Expression dispCall = Helpers.Convert(callable, typeof(DispCallable));

            DynamicMetaObject invoke = new ComInvokeBinder(
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

            if (temps != null && temps.Count > 0)
            {
                Expression invokeExpression = invoke.Expression;
                Expression call = Expression.Block(invokeExpression.Type, temps, initTemps.Append(invokeExpression));
                invoke = new DynamicMetaObject(call, invoke.Restrictions);
            }

            return invoke;
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
