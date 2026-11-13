// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation.Language;
using System.Runtime.InteropServices.ComTypes;

namespace System.Management.Automation.ComInterop
{
    internal sealed class IDispatchMetaObject : ComFallbackMetaObject
    {
        private readonly IDispatchComObject _self;

        internal IDispatchMetaObject(Expression expression, IDispatchComObject self)
            : base(expression, BindingRestrictions.Empty, self)
        {
            _self = self;
        }

        public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
        {
            Requires.NotNull(binder);

            ComMethodDesc method = null;

            // See if this is actually a property set
            ComBinder.ComInvokeMemberBinder comInvokeBinder = binder as ComBinder.ComInvokeMemberBinder;
            if ((comInvokeBinder != null) && (comInvokeBinder.IsPropertySet))
            {
                DynamicMetaObject value = args[args.Length - 1];

                bool holdsNull = value.Value == null && value.HasValue;
                if (!_self.TryGetPropertySetter(binder.Name, out method, value.LimitType, holdsNull))
                {
                    _self.TryGetPropertySetterExplicit(binder.Name, out method, value.LimitType, holdsNull);
                }
            }

            // Otherwise, try property get
            if (method == null)
            {
                if (!_self.TryGetMemberMethod(binder.Name, out method))
                {
                    _self.TryGetMemberMethodExplicit(binder.Name, out method);
                }
            }

            if (method != null)
            {
                List<ParameterExpression> temps = new List<ParameterExpression>();
                List<Expression> initTemps = new List<Expression>();

                bool[] isByRef = ComBinderHelpers.ProcessArgumentsForCom(method, ref args, temps, initTemps);
                return BindComInvoke(args, method, binder.CallInfo, isByRef, temps, initTemps);
            }

            return base.BindInvokeMember(binder, args);
        }

        public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
        {
            Requires.NotNull(binder);

            if (_self.TryGetGetItem(out ComMethodDesc method))
            {
                List<ParameterExpression> temps = new List<ParameterExpression>();
                List<Expression> initTemps = new List<Expression>();

                bool[] isByRef = ComBinderHelpers.ProcessArgumentsForCom(method, ref args, temps, initTemps);
                return BindComInvoke(args, method, binder.CallInfo, isByRef, temps, initTemps);
            }

            return base.BindInvoke(binder, args);
        }

        private DynamicMetaObject BindComInvoke(DynamicMetaObject[] args, ComMethodDesc method, CallInfo callInfo, bool[] isByRef,
            List<ParameterExpression> temps, List<Expression> initTemps)
        {
            DynamicMetaObject invoke = new ComInvokeBinder(
                callInfo,
                args,
                isByRef,
                IDispatchRestriction(),
                Expression.Constant(method),
                Expression.Property(
                    Helpers.Convert(Expression, typeof(IDispatchComObject)),
                    typeof(IDispatchComObject).GetProperty(nameof(IDispatchComObject.DispatchObject))
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

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            ComBinder.ComGetMemberBinder comBinder = binder as ComBinder.ComGetMemberBinder;
            bool canReturnCallables = comBinder?._canReturnCallables ?? false;

            Requires.NotNull(binder);

            // 1. Try methods
            if (_self.TryGetMemberMethod(binder.Name, out ComMethodDesc method))
            {
                if (((method.InvokeKind & INVOKEKIND.INVOKE_PROPERTYGET) ==
                    INVOKEKIND.INVOKE_PROPERTYGET) &&
                    (method.ParamCount == 0))
                {
                    return BindGetMember(method, canReturnCallables);
                }
            }

            // 2. Try events
            if (_self.TryGetMemberEvent(binder.Name, out ComEventDesc @event))
            {
                return BindEvent(@event);
            }

            // 3. Try methods explicitly by name
            if (_self.TryGetMemberMethodExplicit(binder.Name, out method))
            {
                if (((method.InvokeKind & INVOKEKIND.INVOKE_PROPERTYGET) ==
                    INVOKEKIND.INVOKE_PROPERTYGET) &&
                    (method.ParamCount == 0))
                {
                    return BindGetMember(method, canReturnCallables);
                }
            }

            // 4. Fallback
            return base.BindGetMember(binder);
        }

        private DynamicMetaObject BindGetMember(ComMethodDesc method, bool canReturnCallables)
        {
            if (method.IsDataMember)
            {
                if (method.ParamCount == 0)
                {
                    return BindComInvoke(DynamicMetaObject.EmptyMetaObjects, method, new CallInfo(0), Array.Empty<bool>(), null, null);
                }
            }

            // ComGetMemberBinder does not expect callables. Try to call always.
            if (!canReturnCallables)
            {
                return BindComInvoke(DynamicMetaObject.EmptyMetaObjects, method, new CallInfo(0), Array.Empty<bool>(), null, null);
            }

            return new DynamicMetaObject(
                Expression.Call(
                    typeof(ComRuntimeHelpers).GetMethod(nameof(ComRuntimeHelpers.CreateDispCallable)),
                    Helpers.Convert(Expression, typeof(IDispatchComObject)),
                    Expression.Constant(method)
                ),
                IDispatchRestriction()
            );
        }

        private DynamicMetaObject BindEvent(ComEventDesc eventDesc)
        {
            // BoundDispEvent CreateComEvent(object rcw, Guid sourceIid, int dispid)
            Expression result =
                Expression.Call(
                    typeof(ComRuntimeHelpers).GetMethod(nameof(ComRuntimeHelpers.CreateComEvent)),
                    ComObject.RcwFromComObject(Expression),
                    Expression.Constant(eventDesc.SourceIID),
                    Expression.Constant(eventDesc.Dispid)
                );

            return new DynamicMetaObject(
                result,
                IDispatchRestriction()
            );
        }

        public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
        {
            Requires.NotNull(binder);

            if (_self.TryGetGetItem(out ComMethodDesc getItem))
            {
                List<ParameterExpression> temps = new List<ParameterExpression>();
                List<Expression> initTemps = new List<Expression>();

                bool[] isByRef = ComBinderHelpers.ProcessArgumentsForCom(getItem, ref indexes, temps, initTemps);
                return BindComInvoke(indexes, getItem, binder.CallInfo, isByRef, temps, initTemps);
            }

            return base.BindGetIndex(binder, indexes);
        }

        public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
        {
            Requires.NotNull(binder);

            if (_self.TryGetSetItem(out ComMethodDesc setItem))
            {
                List<ParameterExpression> temps = new List<ParameterExpression>();
                List<Expression> initTemps = new List<Expression>();

                bool[] isByRef = ComBinderHelpers.ProcessArgumentsForCom(setItem, ref indexes, temps, initTemps);
                isByRef = isByRef.AddLast(false);

                // Convert the value to the target type
                DynamicMetaObject updatedValue = new DynamicMetaObject(
                    value.CastOrConvertMethodArgument(
                        value.LimitType,
                        setItem.Name,
                        "SetIndex",
                        allowCastingToByRefLikeType: false,
                        temps,
                        initTemps),
                    value.Restrictions);

                var result = BindComInvoke(indexes.AddLast(updatedValue), setItem, binder.CallInfo, isByRef, temps, initTemps);

                // Make sure to return the value; some languages need it.
                return new DynamicMetaObject(
                    Expression.Block(result.Expression, Expression.Convert(value.Expression, typeof(object))),
                    result.Restrictions
                );
            }

            return base.BindSetIndex(binder, indexes, value);
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            Requires.NotNull(binder);

            return
                // 1. Check for simple property put
                TryPropertyPut(binder, value) ??

                // 2. Check for event handler hookup where the put is dropped
                TryEventHandlerNoop(binder, value) ??

                // 3. Fallback
                base.BindSetMember(binder, value);
        }

        private DynamicMetaObject TryPropertyPut(SetMemberBinder binder, DynamicMetaObject value)
        {
            bool holdsNull = value.Value == null && value.HasValue;
            if (_self.TryGetPropertySetter(binder.Name, out ComMethodDesc method, value.LimitType, holdsNull) ||
                _self.TryGetPropertySetterExplicit(binder.Name, out method, value.LimitType, holdsNull))
            {
                BindingRestrictions restrictions = IDispatchRestriction();
                Expression dispatch =
                    Expression.Property(
                        Helpers.Convert(Expression, typeof(IDispatchComObject)),
                        typeof(IDispatchComObject).GetProperty(nameof(IDispatchComObject.DispatchObject))
                    );

                DynamicMetaObject result = new ComInvokeBinder(
                    new CallInfo(1),
                    new[] { value },
                    new bool[] { false },
                    restrictions,
                    Expression.Constant(method),
                    dispatch,
                    method
                ).Invoke();

                // Make sure to return the value; some languages need it.
                return new DynamicMetaObject(
                    Expression.Block(result.Expression, Expression.Convert(value.Expression, typeof(object))),
                    result.Restrictions
                );
            }

            return null;
        }

        private DynamicMetaObject TryEventHandlerNoop(SetMemberBinder binder, DynamicMetaObject value)
        {
            if (_self.TryGetMemberEvent(binder.Name, out _) && value.LimitType == typeof(BoundDispEvent))
            {
                // Drop the event property set.
                return new DynamicMetaObject(
                    Expression.Constant(null),
                    value.Restrictions.Merge(IDispatchRestriction()).Merge(BindingRestrictions.GetTypeRestriction(value.Expression, typeof(BoundDispEvent)))
                );
            }

            return null;
        }

        private BindingRestrictions IDispatchRestriction()
        {
            return IDispatchRestriction(Expression, _self.ComTypeDesc);
        }

        internal static BindingRestrictions IDispatchRestriction(Expression expr, ComTypeDesc typeDesc)
        {
            return BindingRestrictions.GetTypeRestriction(
                expr, typeof(IDispatchComObject)
            ).Merge(
                BindingRestrictions.GetExpressionRestriction(
                    Expression.Equal(
                        Expression.Property(
                            Helpers.Convert(expr, typeof(IDispatchComObject)),
                            typeof(IDispatchComObject).GetProperty(nameof(IDispatchComObject.ComTypeDesc))
                        ),
                        Expression.Constant(typeDesc)
                    )
                )
            );
        }

        protected override ComUnwrappedMetaObject UnwrapSelf()
        {
            return new ComUnwrappedMetaObject(
                ComObject.RcwFromComObject(Expression),
                IDispatchRestriction(),
                _self.RuntimeCallableWrapper
            );
        }
    }
}
