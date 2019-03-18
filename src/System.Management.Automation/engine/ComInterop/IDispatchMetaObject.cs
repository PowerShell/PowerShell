// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !SILVERLIGHT // ComObject
#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif
using System.Linq;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.InteropServices.ComTypes;
using System.Management.Automation.Language;

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
            ComMethodDesc method;
            if (_self.TryGetGetItem(out method))
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
                    typeof(IDispatchComObject).GetProperty("DispatchObject")
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

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            ComBinder.ComGetMemberBinder comBinder = binder as ComBinder.ComGetMemberBinder;
            bool canReturnCallables = comBinder == null ? false : comBinder._CanReturnCallables;

            ComMethodDesc method;
            ComEventDesc @event;

            // 1. Try methods
            if (_self.TryGetMemberMethod(binder.Name, out method))
            {
                if (((method.InvokeKind & INVOKEKIND.INVOKE_PROPERTYGET) ==
                    INVOKEKIND.INVOKE_PROPERTYGET) &&
                    (method.ParamCount == 0))
                {
                    return BindGetMember(method, canReturnCallables);
                }
            }

            // 2. Try events
            if (_self.TryGetMemberEvent(binder.Name, out @event))
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
                    typeof(ComRuntimeHelpers).GetMethod("CreateDispCallable"),
                    Helpers.Convert(Expression, typeof(IDispatchComObject)),
                    Expression.Constant(method)
                ),
                IDispatchRestriction()
            );
        }

        private DynamicMetaObject BindEvent(ComEventDesc @event)
        {
            // BoundDispEvent CreateComEvent(object rcw, Guid sourceIid, int dispid)
            Expression result =
                Expression.Call(
                    typeof(ComRuntimeHelpers).GetMethod("CreateComEvent"),
                    ComObject.RcwFromComObject(Expression),
                    Expression.Constant(@event.sourceIID),
                    Expression.Constant(@event.dispid)
                );

            return new DynamicMetaObject(
                result,
                IDispatchRestriction()
            );
        }

        public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
        {
            ComMethodDesc getItem;
            if (_self.TryGetGetItem(out getItem))
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
            ComMethodDesc setItem;
            if (_self.TryGetSetItem(out setItem))
            {
                List<ParameterExpression> temps = new List<ParameterExpression>();
                List<Expression> initTemps = new List<Expression>();

                bool[] isByRef = ComBinderHelpers.ProcessArgumentsForCom(setItem, ref indexes, temps, initTemps);
                isByRef = isByRef.AddLast(false);

                // Convert the value to the target type
                DynamicMetaObject updatedValue = new DynamicMetaObject(value.CastOrConvertMethodArgument(
                                value.LimitType,
                                setItem.Name,
                                "SetIndex",
                                temps,
                                initTemps), value.Restrictions);

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
            ComMethodDesc method;
            bool holdsNull = value.Value == null && value.HasValue;
            if (_self.TryGetPropertySetter(binder.Name, out method, value.LimitType, holdsNull) ||
                _self.TryGetPropertySetterExplicit(binder.Name, out method, value.LimitType, holdsNull))
            {
                BindingRestrictions restrictions = IDispatchRestriction();
                Expression dispatch =
                    Expression.Property(
                        Helpers.Convert(Expression, typeof(IDispatchComObject)),
                        typeof(IDispatchComObject).GetProperty("DispatchObject")
                    );

                var result = new ComInvokeBinder(
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
            ComEventDesc @event;
            if (_self.TryGetMemberEvent(binder.Name, out @event) && value.LimitType == typeof(BoundDispEvent))
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
                            typeof(IDispatchComObject).GetProperty("ComTypeDesc")
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

#endif

