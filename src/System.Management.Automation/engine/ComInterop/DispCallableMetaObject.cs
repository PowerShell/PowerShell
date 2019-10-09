// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !SILVERLIGHT

#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif
using System.Linq;
using System.Dynamic;
using System.Collections.Generic;
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
            ComMethodDesc method;
            var target = _callable.DispatchComObject;
            var name = _callable.MemberName;

            if (target.TryGetMemberMethod(name, out method) ||
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
            ComMethodDesc method;
            var target = _callable.DispatchComObject;
            var name = _callable.MemberName;

            bool holdsNull = value.Value == null && value.HasValue;
            if (target.TryGetPropertySetter(name, out method, value.LimitType, holdsNull) ||
                target.TryGetPropertySetterExplicit(name, out method, value.LimitType, holdsNull))
            {
                List<ParameterExpression> temps = new List<ParameterExpression>();
                List<Expression> initTemps = new List<Expression>();

                bool[] isByRef = ComBinderHelpers.ProcessArgumentsForCom(method, ref indexes, temps, initTemps);
                isByRef = isByRef.AddLast(false);

                // Convert the value to the target type
                DynamicMetaObject updatedValue = new DynamicMetaObject(value.CastOrConvertMethodArgument(
                                value.LimitType,
                                name,
                                "SetIndex",
                                temps,
                                initTemps), value.Restrictions);

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
            var callable = Expression;
            var dispCall = Helpers.Convert(callable, typeof(DispCallable));

            DynamicMetaObject invoke = new ComInvokeBinder(
                callInfo,
                indexes,
                isByRef,
                DispCallableRestrictions(),
                Expression.Constant(method),
                Expression.Property(
                    dispCall,
                    typeof(DispCallable).GetProperty("DispatchObject")
                ),
                method
            ).Invoke();

            if ((temps != null) && (temps.Any()))
            {
                Expression invokeExpression = invoke.Expression;
                Expression call = Expression.Block(invokeExpression.Type, temps, initTemps.Append(invokeExpression));
                invoke = new DynamicMetaObject(call, invoke.Restrictions);
            }

            return invoke;
        }

        private BindingRestrictions DispCallableRestrictions()
        {
            var callable = Expression;

            var callableTypeRestrictions = BindingRestrictions.GetTypeRestriction(callable, typeof(DispCallable));
            var dispCall = Helpers.Convert(callable, typeof(DispCallable));
            var dispatch = Expression.Property(dispCall, typeof(DispCallable).GetProperty("DispatchComObject"));
            var dispId = Expression.Property(dispCall, typeof(DispCallable).GetProperty("DispId"));

            var dispatchRestriction = IDispatchMetaObject.IDispatchRestriction(dispatch, _callable.DispatchComObject.ComTypeDesc);
            var memberRestriction = BindingRestrictions.GetExpressionRestriction(
                Expression.Equal(dispId, Expression.Constant(_callable.DispId))
            );

            return callableTypeRestrictions.Merge(dispatchRestriction).Merge(memberRestriction);
        }
    }
}

#endif

