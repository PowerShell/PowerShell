// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Security;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

namespace System.Management.Automation.Language
{
    // Item1 - member name
    // Item2 - class containing dynamic site (for protected/private member access)
    // Item3 - static (true) or instance (false)
    // Item4 - enumerating (true) or not (false)
    using PSGetMemberBinderKeyType = Tuple<string, Type, bool, bool>;

    // Item1 - member name
    // Item2 - class containing dynamic site (for protected/private member access)
    // Item3 - enumerating (true) or not (false)
    using PSSetMemberBinderKeyType = Tuple<string, Type, bool>;

    // Item1 - member name
    // Item2 - callinfo (# of args and (not used) named arguments)
    // Item3 - property setter (true) or not (false)
    // Item4 - enumerating (true) or not (false)
    // Item5 - invocation constraints (casts used in the invocation expression used to guide overload resolution)
    // Item6 - static (true) or instance (false)
    // Item7 - class containing dynamic site (for protected/private member access)
    using PSInvokeMemberBinderKeyType = Tuple<string, CallInfo, bool, bool, PSMethodInvocationConstraints, bool, Type>;

    // Item1 - callinfo (# of args and (not used) named arguments)
    // Item2 - invocation constraints (casts used in the invocation expression used to guide overload resolution)
    // Item3 - property setter (true) or not (false)
    // Item4 - static (true) or instance (false)
    // Item5 - class containing dynamic site (for protected/private member access)
    using PSInvokeDynamicMemberBinderKeyType = Tuple<CallInfo, PSMethodInvocationConstraints, bool, bool, Type>;

    // Item1 - class containing dynamic site (for protected/private member access)
    // Item2 - static (true) or instance (false)
    using PSGetOrSetDynamicMemberBinderKeyType = Tuple<Type, bool>;

    /// <summary>
    /// Extension methods for DynamicMetaObject.  Some of these extensions help handle PSObject, both in terms of
    /// getting the type or value from a DynamicMetaObject, but also help to generate binding restrictions that
    /// account for values that are optionally wrapped in PSObject.
    /// </summary>
    internal static class DynamicMetaObjectExtensions
    {
        internal static readonly DynamicMetaObject FakeError
            = new DynamicMetaObject(ExpressionCache.NullConstant, BindingRestrictions.Empty);

        internal static DynamicMetaObject WriteToDebugLog(this DynamicMetaObject obj, DynamicMetaObjectBinder binder)
        {
#if ENABLE_BINDER_DEBUG_LOGGING
            if (obj != FakeError)
            {
                System.Diagnostics.Debug.WriteLine("Binder: {0}\r\n    Restrictions: {2}\r\n    Target: {1}",
                                                   binder.ToString(),
                                                   obj.Expression.ToDebugString(),
                                                   obj.Restrictions.ToDebugString());
            }
#endif
            return obj;
        }

        internal static BindingRestrictions GetSimpleTypeRestriction(this DynamicMetaObject obj)
        {
            if (obj.Value == null)
            {
                return BindingRestrictions.GetInstanceRestriction(obj.Expression, obj.Value);
            }

            return BindingRestrictions.GetTypeRestriction(obj.Expression, obj.Value.GetType());
        }

        internal static BindingRestrictions PSGetMethodArgumentRestriction(this DynamicMetaObject obj)
        {
            var baseValue = PSObject.Base(obj.Value);
            if (baseValue != null && baseValue.GetType() == typeof(object[]))
            {
                var effectiveArgType = Adapter.EffectiveArgumentType(obj.Value);
                var methodInfo = effectiveArgType != typeof(object[])
                    ? CachedReflectionInfo.PSInvokeMemberBinder_IsHomogeneousArray.MakeGenericMethod(effectiveArgType.GetElementType())
                    : CachedReflectionInfo.PSInvokeMemberBinder_IsHeterogeneousArray;

                BindingRestrictions restrictions;
                Expression test;
                if (obj.Value != baseValue)
                {
                    // Need PSObject...
                    restrictions = BindingRestrictions.GetTypeRestriction(obj.Expression, typeof(PSObject));

                    var temp = Expression.Variable(typeof(object[]));
                    test = Expression.Block(
                        new[] { temp },
                        Expression.Assign(temp, Expression.TypeAs(Expression.Call(CachedReflectionInfo.PSObject_Base, obj.Expression), typeof(object[]))),
                        Expression.AndAlso(
                            Expression.NotEqual(temp, ExpressionCache.NullObjectArray),
                            Expression.Call(methodInfo, temp)));
                }
                else
                {
                    restrictions = BindingRestrictions.GetTypeRestriction(obj.Expression, typeof(object[]));
                    var arrayExpr = obj.Expression.Cast(typeof(object[]));
                    test = Expression.Call(methodInfo, arrayExpr);
                }

                return restrictions.Merge(BindingRestrictions.GetExpressionRestriction(test));
            }

            return obj.PSGetTypeRestriction();
        }

        internal static BindingRestrictions PSGetStaticMemberRestriction(this DynamicMetaObject obj)
        {
            if (obj.Restrictions != BindingRestrictions.Empty)
            {
                return obj.Restrictions;
            }

            if (obj.Value == null)
            {
                return BindingRestrictions.GetInstanceRestriction(obj.Expression, obj.Value);
            }

            var baseValue = PSObject.Base(obj.Value);
            if (baseValue == null)
            {
                Diagnostics.Assert(obj.Value == AutomationNull.Value, "PSObject.Base should only return null for AutomationNull.Value");
                return BindingRestrictions.GetExpressionRestriction(Expression.Equal(obj.Expression, Expression.Constant(AutomationNull.Value)));
            }

            BindingRestrictions restrictions;

            if (baseValue is Type)
            {
                if (obj.Value == baseValue)
                {
                    //     newObj == oldObj  (if not wrapped in PSObject) or
                    restrictions = BindingRestrictions.GetInstanceRestriction(obj.Expression, obj.Value);
                }
                else
                {
                    //     newObj.GetType() == typeof(PSObject) && PSObject.Base(newObj) == oldObj
                    restrictions = BindingRestrictions.GetTypeRestriction(obj.Expression, obj.LimitType);
                    restrictions = restrictions.Merge(
                        BindingRestrictions.GetInstanceRestriction(
                            Expression.Call(CachedReflectionInfo.PSObject_Base, obj.Expression),
                            baseValue));
                }
            }
            else if (obj.Value != baseValue)
            {
                // Binding restriction will look like:
                //     newObj.GetType() == typeof(PSObject) && PSObject.Base(newObj).GetType() == typeof(oldType)

                restrictions = BindingRestrictions.GetTypeRestriction(
                    Expression.Call(CachedReflectionInfo.PSObject_Base, obj.Expression),
                    baseValue.GetType());
            }
            else
            {
                restrictions = BindingRestrictions.GetTypeRestriction(obj.Expression, obj.LimitType);
            }

            return restrictions;
        }

        internal static BindingRestrictions PSGetTypeRestriction(this DynamicMetaObject obj)
        {
            if (obj.Restrictions != BindingRestrictions.Empty)
            {
                return obj.Restrictions;
            }

            if (obj.Value == null)
            {
                return BindingRestrictions.GetInstanceRestriction(obj.Expression, obj.Value);
            }

            var baseValue = PSObject.Base(obj.Value);
            if (baseValue == null)
            {
                Diagnostics.Assert(obj.Value == AutomationNull.Value, "PSObject.Base should only return null for AutomationNull.Value");
                return BindingRestrictions.GetExpressionRestriction(Expression.Equal(obj.Expression, Expression.Constant(AutomationNull.Value)));
            }

            // The default restriction is a simple type test.  We use this type test even if the object is a PSObject,
            // this way we can avoid calling PSObject.Base in all restriction checks.
            var restrictions = BindingRestrictions.GetTypeRestriction(obj.Expression, obj.LimitType);

            if (obj.Value != baseValue)
            {
                // Binding restriction will look like:
                //     newObj.GetType() == typeof(PSObject) && PSObject.Base(newObj).GetType() == typeof(oldType)

                restrictions = restrictions.Merge(
                    BindingRestrictions.GetTypeRestriction(Expression.Call(CachedReflectionInfo.PSObject_Base, obj.Expression),
                                                           baseValue.GetType()));
            }
            else if (baseValue is PSObject)
            {
                // We have an empty custom object.  The restrictions must check this explicitly, otherwise we have
                // a simple type test on PSObject, which obviously tests true for many objects.
                // So we end up with:
                //     newObj.GetType() == typeof(PSObject) && PSObject.Base(newObj) == newObj

                restrictions = restrictions.Merge(
                    BindingRestrictions.GetExpressionRestriction(
                        Expression.Equal(Expression.Call(CachedReflectionInfo.PSObject_Base, obj.Expression), obj.Expression)));
            }

            return restrictions;
        }

        internal static BindingRestrictions CombineRestrictions(this DynamicMetaObject target, params DynamicMetaObject[] args)
        {
            var restrictions = target.Restrictions == BindingRestrictions.Empty ? target.PSGetTypeRestriction() : target.Restrictions;

            for (int index = 0; index < args.Length; index++)
            {
                var r = args[index];
                restrictions =
                    restrictions.Merge(r.Restrictions == BindingRestrictions.Empty
                                           ? r.PSGetTypeRestriction()
                                           : r.Restrictions);
            }

            return restrictions;
        }

        internal static Expression CastOrConvertMethodArgument(this DynamicMetaObject target,
                                                               Type parameterType,
                                                               string parameterName,
                                                               string methodName,
                                                               bool allowCastingToByRefLikeType,
                                                               List<ParameterExpression> temps,
                                                               List<Expression> initTemps)
        {
            if (target.Value == AutomationNull.Value)
            {
                return Expression.Constant(null, parameterType);
            }

            var argType = target.LimitType;
            if (parameterType == typeof(object) && argType == typeof(PSObject))
            {
                return Expression.Call(CachedReflectionInfo.PSObject_Base, target.Expression.Cast(typeof(PSObject)));
            }

            // If the conversion can't fail, skip wrapping the conversion in try/catch so we generate less code.
            if (parameterType.IsAssignableFrom(argType))
            {
                return target.Expression.Cast(parameterType);
            }

            ConversionRank? rank = null;
            if (parameterType.IsByRefLike && allowCastingToByRefLikeType)
            {
                var conversionResult = PSConvertBinder.ConvertToByRefLikeTypeViaCasting(target, parameterType);
                if (conversionResult != null)
                {
                    return conversionResult;
                }

                rank = ConversionRank.None;
            }

            var exceptionParam = Expression.Variable(typeof(Exception));
            var targetTemp = Expression.Variable(target.Expression.Type);
            bool debase = false;

            // ConstrainedLanguage note - calls to this conversion are covered by the method resolution algorithm
            // (which ignores method arguments with disallowed types)
            var conversion = rank == ConversionRank.None
                ? LanguagePrimitives.NoConversion
                : LanguagePrimitives.FigureConversion(target.Value, parameterType, out debase);
            var invokeConverter = PSConvertBinder.InvokeConverter(conversion, targetTemp, parameterType, debase, ExpressionCache.InvariantCulture);
            var expr =
                Expression.Block(new[] { targetTemp },
                    Expression.TryCatch(
                        Expression.Block(
                            Expression.Assign(targetTemp, target.Expression),
                            invokeConverter),
                        Expression.Catch(exceptionParam,
                            Expression.Block(
                                Expression.Call(CachedReflectionInfo.ExceptionHandlingOps_ConvertToArgumentConversionException,
                                                exceptionParam,
                                                Expression.Constant(parameterName),
                                                targetTemp.Cast(typeof(object)),
                                                Expression.Constant(methodName),
                                                Expression.Constant(parameterType, typeof(Type))),
                                Expression.Default(invokeConverter.Type)))));
            var tmp = Expression.Variable(expr.Type);
            temps.Add(tmp);
            initTemps.Add(Expression.Assign(tmp, expr));
            return tmp;
        }

        internal static Expression CastOrConvert(this DynamicMetaObject target, Type type)
        {
            if (target.LimitType == type)
            {
                return target.Expression.Cast(type);
            }

            bool debase;

            // ConstrainedLanguage note - calls to this conversion are done by:
            // Switch statements (always to Object), method invocation (protected by InvokeMember binder),
            // and hard-coded casts to integral types.
            var conversion = LanguagePrimitives.FigureConversion(target.Value, type, out debase);
            return PSConvertBinder.InvokeConverter(conversion, target.Expression, type, debase, ExpressionCache.InvariantCulture);
        }

        internal static DynamicMetaObject ThrowRuntimeError(this DynamicMetaObject target, DynamicMetaObject[] args,
                                                            BindingRestrictions moreTests, string errorID,
                                                            string resourceString, params Expression[] exceptionArgs)
        {
            return new DynamicMetaObject(Compiler.ThrowRuntimeError(errorID, resourceString, exceptionArgs),
                                         target.CombineRestrictions(args).Merge(moreTests));
        }

        internal static DynamicMetaObject ThrowRuntimeError(this DynamicMetaObject target, BindingRestrictions bindingRestrictions,
                                                            string errorID, string resourceString, params Expression[] exceptionArgs)
        {
            return new DynamicMetaObject(Compiler.ThrowRuntimeError(errorID, resourceString, exceptionArgs),
                                         bindingRestrictions);
        }

#if ENABLE_BINDER_DEBUG_LOGGING
        internal static string ToDebugString(this BindingRestrictions restrictions)
        {
            return restrictions.ToExpression().ToDebugString();
        }
#endif
    }

    internal static class DynamicMetaObjectBinderExtensions
    {
        internal static DynamicMetaObject DeferForPSObject(this DynamicMetaObjectBinder binder, DynamicMetaObject target, bool targetIsComObject = false)
        {
            Diagnostics.Assert(target.Value is PSObject, "target must be a psobject");

            BindingRestrictions restrictions = BindingRestrictions.Empty;
            Expression expr = ProcessOnePSObject(target, ref restrictions, argIsComObject: targetIsComObject);
            return new DynamicMetaObject(DynamicExpression.Dynamic(binder, binder.ReturnType, expr), restrictions);
        }

        internal static DynamicMetaObject DeferForPSObject(this DynamicMetaObjectBinder binder, DynamicMetaObject target, DynamicMetaObject arg, bool targetIsComObject = false)
        {
            Diagnostics.Assert(target.Value is PSObject || arg.Value is PSObject, "At least one arg must be a psobject");

            BindingRestrictions restrictions = BindingRestrictions.Empty;
            Expression expr1 = ProcessOnePSObject(target, ref restrictions, argIsComObject: targetIsComObject);
            Expression expr2 = ProcessOnePSObject(arg, ref restrictions, argIsComObject: false);
            return new DynamicMetaObject(DynamicExpression.Dynamic(binder, binder.ReturnType, expr1, expr2), restrictions);
        }

        internal static DynamicMetaObject DeferForPSObject(this DynamicMetaObjectBinder binder, DynamicMetaObject[] args, bool targetIsComObject = false)
        {
            Diagnostics.Assert(args != null && args.Length > 0, "args should not be null or empty");
            Diagnostics.Assert(args.Any(mo => mo.Value is PSObject), "At least one arg must be a psobject");

            Expression[] exprs = new Expression[args.Length];
            BindingRestrictions restrictions = BindingRestrictions.Empty;

            // Target maps to arg[0] of the binder.
            exprs[0] = ProcessOnePSObject(args[0], ref restrictions, targetIsComObject);
            for (int i = 1; i < args.Length; i++)
            {
                exprs[i] = ProcessOnePSObject(args[i], ref restrictions, argIsComObject: false);
            }

            return new DynamicMetaObject(DynamicExpression.Dynamic(binder, binder.ReturnType, exprs), restrictions);
        }

        private static Expression ProcessOnePSObject(DynamicMetaObject arg, ref BindingRestrictions restrictions, bool argIsComObject = false)
        {
            Expression expr = null;
            object baseValue = PSObject.Base(arg.Value);
            if (baseValue != arg.Value)
            {
                expr = Expression.Call(CachedReflectionInfo.PSObject_Base, arg.Expression.Cast(typeof(object)));

                if (argIsComObject)
                {
                    // The 'base' is a COM object, so bake that in the rule.
                    restrictions = restrictions
                        .Merge(arg.GetSimpleTypeRestriction())
                        .Merge(BindingRestrictions.GetExpressionRestriction(Expression.Call(CachedReflectionInfo.Utils_IsComObject, expr)));
                }
                else
                {
                    // Use a more general condition for the rule: 'arg' is a PSObject and 'base != arg'.
                    restrictions = restrictions
                        .Merge(arg.GetSimpleTypeRestriction())
                        .Merge(BindingRestrictions.GetExpressionRestriction(Expression.NotEqual(expr, arg.Expression)));
                }
            }
            else
            {
                expr = arg.Expression;
                restrictions = restrictions.Merge(arg.PSGetTypeRestriction());
            }

            return expr;
        }

        internal static DynamicMetaObject UpdateComRestrictionsForPsObject(this DynamicMetaObject binder, DynamicMetaObject[] args)
        {
            // Add a restriction that prevents PSObject arguments (so that they get based)
            BindingRestrictions newRestrictions = binder.Restrictions;
            newRestrictions = args.Aggregate(newRestrictions, (current, arg) =>
            {
                if (arg.LimitType.IsValueType)
                {
                    return current.Merge(arg.GetSimpleTypeRestriction());
                }
                else
                {
                    return current.Merge(BindingRestrictions.GetExpressionRestriction(
                        Expression.Equal(
                            Expression.Call(CachedReflectionInfo.PSObject_Base, arg.Expression),
                            arg.Expression)));
                }
            });

            return new DynamicMetaObject(binder.Expression, newRestrictions);
        }
    }

    internal static class BinderUtils
    {
        internal static BindingRestrictions GetVersionCheck(DynamicMetaObjectBinder binder, int expectedVersionNumber)
        {
            return BindingRestrictions.GetExpressionRestriction(
                Expression.Equal(Expression.Field(Expression.Constant(binder), "_version"),
                                 ExpressionCache.Constant(expectedVersionNumber)));
        }

        internal static BindingRestrictions GetLanguageModeCheckIfHasEverUsedConstrainedLanguage()
        {
            // Also add a language mode check to detect toggling between language modes
            if (ExecutionContext.HasEverUsedConstrainedLanguage)
            {
                var context = LocalPipeline.GetExecutionContextFromTLS();

                var tmp = Expression.Variable(typeof(ExecutionContext));
                var langModeFromContext = Expression.Property(tmp, CachedReflectionInfo.ExecutionContext_LanguageMode);
                var constrainedLanguageMode = Expression.Constant(PSLanguageMode.ConstrainedLanguage);

                // Execution context might be null if we're called from a thread with no runspace (e.g. a PSObject
                // is used in some C# w/ dynamic). This is sometimes fine, we don't always need a runspace to access
                // properties.
                Expression test = context?.LanguageMode == PSLanguageMode.ConstrainedLanguage
                    ? Expression.AndAlso(
                          Expression.NotEqual(tmp, ExpressionCache.NullExecutionContext),
                          Expression.Equal(langModeFromContext, constrainedLanguageMode))
                    : Expression.OrElse(
                          Expression.Equal(tmp, ExpressionCache.NullExecutionContext),
                          Expression.NotEqual(langModeFromContext, constrainedLanguageMode));

                return BindingRestrictions.GetExpressionRestriction(
                    Expression.Block(
                        new[] { tmp },
                        Expression.Assign(tmp, ExpressionCache.GetExecutionContextFromTLS),
                        test));
            }

            return BindingRestrictions.Empty;
        }

        internal static BindingRestrictions GetOptionalVersionAndLanguageCheckForType(DynamicMetaObjectBinder binder, Type targetType, int expectedVersionNumber)
        {
            BindingRestrictions additionalBindingRestrictions = BindingRestrictions.Empty;

            // If this uses a potentially unsafe type, we also need a version check
            if (!CoreTypes.Contains(targetType))
            {
                if (expectedVersionNumber != -1)
                {
                    additionalBindingRestrictions = additionalBindingRestrictions.Merge(BinderUtils.GetVersionCheck(binder, expectedVersionNumber));
                }

                additionalBindingRestrictions = additionalBindingRestrictions.Merge(GetLanguageModeCheckIfHasEverUsedConstrainedLanguage());
            }

            return additionalBindingRestrictions;
        }
    }

    #region PowerShell non-standard language binders

    /// <summary>
    /// Some classes that implement IEnumerable are not considered as enumerable from the perspective of pipelines,
    /// this binder implements those semantics.
    ///
    /// The standard interop ConvertBinder is used to allow third party dynamic objects to get the first chance
    /// at the conversion in case they do support enumeration, but do not implement IEnumerable directly.
    /// </summary>
    internal sealed class PSEnumerableBinder : ConvertBinder
    {
        private static readonly PSEnumerableBinder s_binder = new PSEnumerableBinder();

        internal static PSEnumerableBinder Get()
        {
            return s_binder;
        }

        private PSEnumerableBinder()
            : base(typeof(IEnumerator), false)
        {
            CacheTarget((Func<CallSite, object, IEnumerator>)(PSObjectStringRule));
            CacheTarget((Func<CallSite, object, IEnumerator>)(ArrayRule));
            CacheTarget((Func<CallSite, object, IEnumerator>)(StringRule));
            CacheTarget((Func<CallSite, object, IEnumerator>)(NotEnumerableRule));
            CacheTarget((Func<CallSite, object, IEnumerator>)(PSObjectNotEnumerableRule));
            CacheTarget((Func<CallSite, object, IEnumerator>)(AutomationNullRule));
        }

        public override string ToString()
        {
            return "ToEnumerable";
        }

        internal static BindingRestrictions GetRestrictions(DynamicMetaObject target)
        {
            return (target.Value is PSObject)
                ? BindingRestrictions.GetTypeRestriction(target.Expression, target.Value.GetType())
                : target.PSGetTypeRestriction();
        }

        private DynamicMetaObject NullResult(DynamicMetaObject target)
        {
            // The object is not enumerable from PowerShell's perspective.  Rather than raise an exception, we let the
            // caller check for null and take the appropriate action.
            return new DynamicMetaObject(
                MaybeDebase(this, static e => ExpressionCache.NullEnumerator, target),
                GetRestrictions(target));
        }

        internal static Expression MaybeDebase(DynamicMetaObjectBinder binder, Func<Expression, Expression> generator, DynamicMetaObject target)
        {
            if (target.Value is not PSObject)
            {
                return generator(target.Expression);
            }

            object targetValue = PSObject.Base(target.Value);

            var tmp = Expression.Parameter(typeof(object), "value");
            return Expression.Block(
                new ParameterExpression[] { tmp },
                Expression.Assign(tmp, Expression.Call(CachedReflectionInfo.PSObject_Base, target.Expression)),
                Expression.Condition(
                    targetValue == null ?
                        (Expression)Expression.AndAlso(Expression.Equal(tmp, ExpressionCache.NullConstant),
                                                       Expression.Not(Expression.Equal(target.Expression, ExpressionCache.AutomationNullConstant)))
                        : Expression.TypeEqual(tmp, targetValue.GetType()),
                    generator(tmp), binder.GetUpdateExpression(binder.ReturnType)));
        }

        public override DynamicMetaObject FallbackConvert(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue)
            {
                return Defer(target).WriteToDebugLog(this);
            }

            if (target.Value == AutomationNull.Value)
            {
                return new DynamicMetaObject(
                    Expression.Call(Expression.Constant(Array.Empty<object>()), typeof(Array).GetMethod("GetEnumerator")),
                    BindingRestrictions.GetInstanceRestriction(target.Expression, AutomationNull.Value)).WriteToDebugLog(this);
            }

            var targetValue = PSObject.Base(target.Value);

            if (targetValue == null || targetValue is string || targetValue is PSObject)
            {
                return (errorSuggestion ?? NullResult(target)).WriteToDebugLog(this);
            }

            if (targetValue.GetType().IsArray)
            {
                return (new DynamicMetaObject(
                    MaybeDebase(this, static e => Expression.Call(Expression.Convert(e, typeof(Array)), typeof(Array).GetMethod("GetEnumerator")),
                        target),
                    GetRestrictions(target))).WriteToDebugLog(this);
            }

            if (targetValue is IDictionary || targetValue is XmlNode)
            {
                return (errorSuggestion ?? NullResult(target)).WriteToDebugLog(this);
            }

            if (targetValue is DataTable)
            {
                // Generate:
                //
                //  DataRowCollection rows;
                //  DataTable table = (DataTable)obj;
                //  if ((rows = table.Rows) != null)
                //      return rows.GetEnumerator();
                //  else
                //      return null;

                return (new DynamicMetaObject(
                    MaybeDebase(this, e =>
                        {
                            var table = Expression.Parameter(typeof(DataTable), "table");
                            var rows = Expression.Parameter(typeof(DataRowCollection), "rows");
                            return Expression.Block(new ParameterExpression[] { table, rows },
                                Expression.Assign(table, e.Cast(typeof(DataTable))),
                                Expression.Condition(
                                    Expression.NotEqual(Expression.Assign(rows, Expression.Property(table, "Rows")), ExpressionCache.NullConstant),
                                    Expression.Call(rows, typeof(DataRowCollection).GetMethod("GetEnumerator")),
                                    ExpressionCache.NullEnumerator));
                        },
                        target),
                    GetRestrictions(target))).WriteToDebugLog(this);
            }

            if (Marshal.IsComObject(targetValue))
            {
                // Pretend that all com objects are enumerable, even if they aren't.  We do this because it's technically impossible
                // to know if a com object is enumerable without just trying to cast it to IEnumerable.  We could generate a rule like:
                //
                //     if (IsComObject(obj)) { return obj as IEnumerable; } else { UpdateSite; }
                //
                // But code that calls PSEnumerableBinder.IsEnumerable and generate code based on the true/false result of that
                // function wouldn't work properly.  Instead, we'll fix things up after the binding decisions are made, see
                // EnumerableOps.NonEnumerableObjectEnumerator for more comments on how this works.

                var bindingRestrictions = BindingRestrictions.GetExpressionRestriction(
                    Expression.Call(CachedReflectionInfo.Utils_IsComObject,
                                    Expression.Call(CachedReflectionInfo.PSObject_Base, target.Expression)));
                return new DynamicMetaObject(
                    Expression.Call(CachedReflectionInfo.EnumerableOps_GetCOMEnumerator, target.Expression), bindingRestrictions).WriteToDebugLog(this);
            }

            var enumerable = targetValue as IEnumerable;
            if (enumerable != null)
            {
                // Normally it's safe to just call IEnumerable.GetEnumerator, but in some rare cases, the
                // non-generic implementation throws or returns null, so we'll just avoid that problem and
                // call the generic version if it exists.

                foreach (var i in targetValue.GetType().GetInterfaces())
                {
                    if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        return (new DynamicMetaObject(
                            MaybeDebase(this, e => Expression.Call(
                                CachedReflectionInfo.EnumerableOps_GetGenericEnumerator.MakeGenericMethod(i.GetGenericArguments()[0]), Expression.Convert(e, i)),
                                target),
                            GetRestrictions(target))).WriteToDebugLog(this);
                    }
                }

                return (new DynamicMetaObject(
                    MaybeDebase(this, static e => Expression.Call(CachedReflectionInfo.EnumerableOps_GetEnumerator, Expression.Convert(e, typeof(IEnumerable))),
                        target),
                    GetRestrictions(target))).WriteToDebugLog(this);
            }

            var enumerator = targetValue as IEnumerator;
            if (enumerator != null)
            {
                return (new DynamicMetaObject(
                    MaybeDebase(this, static e => e.Cast(typeof(IEnumerator)), target),
                    GetRestrictions(target))).WriteToDebugLog(this);
            }

            return (errorSuggestion ?? NullResult(target)).WriteToDebugLog(this);
        }

        /// <summary>
        /// Check if the statically known type is potentially enumerable.  We can avoid some dynamic sites if we know the
        /// type is never enumerable.
        /// </summary>
        internal static bool IsStaticTypePossiblyEnumerable(Type type)
        {
            if (type == typeof(object) || type == typeof(PSObject) || type.IsArray)
            {
                return true;
            }

            if (type == typeof(string) || typeof(IDictionary).IsAssignableFrom(type) || typeof(XmlNode).IsAssignableFrom(type))
            {
                return false;
            }

            if (type.IsSealed && !typeof(IEnumerable).IsAssignableFrom(type) && !typeof(IEnumerator).IsAssignableFrom(type))
            {
                return false;
            }

            return true;
        }

        // Binders normally cannot return null, but we want a way to detect if something is enumerable,
        // so we return null if the target is not enumerable.
        internal static DynamicMetaObject IsEnumerable(DynamicMetaObject target)
        {
            var binder = PSEnumerableBinder.Get();
            var result = binder.FallbackConvert(target, DynamicMetaObjectExtensions.FakeError);
            return (result == DynamicMetaObjectExtensions.FakeError) ? null : result;
        }

        private static IEnumerator AutomationNullRule(CallSite site, object obj)
        {
            return obj == AutomationNull.Value
                ? Array.Empty<object>().GetEnumerator()
                : ((CallSite<Func<CallSite, object, IEnumerator>>)site).Update(site, obj);
        }

        private static IEnumerator NotEnumerableRule(CallSite site, object obj)
        {
            if (obj == null)
            {
                return null;
            }

            if (obj is not PSObject
                && obj is not IEnumerable
                && obj is not IEnumerator
                && obj is not DataTable
                && !Marshal.IsComObject(obj))
            {
                return null;
            }

            return ((CallSite<Func<CallSite, object, IEnumerator>>)site).Update(site, obj);
        }

        private static IEnumerator PSObjectNotEnumerableRule(CallSite site, object obj)
        {
            var psobj = obj as PSObject;
            return psobj != null && obj != AutomationNull.Value
                ? NotEnumerableRule(site, PSObject.Base(obj))
                : ((CallSite<Func<CallSite, object, IEnumerator>>)site).Update(site, obj);
        }

        private static IEnumerator ArrayRule(CallSite site, object obj)
        {
            var array = obj as Array;
            if (array != null) return array.GetEnumerator();
            return ((CallSite<Func<CallSite, object, IEnumerator>>)site).Update(site, obj);
        }

        private static IEnumerator StringRule(CallSite site, object obj)
        {
            return obj is string ? null : ((CallSite<Func<CallSite, object, IEnumerator>>)site).Update(site, obj);
        }

        private static IEnumerator PSObjectStringRule(CallSite site, object obj)
        {
            var psobj = obj as PSObject;
            if (psobj != null && PSObject.Base(psobj) is string) return null;
            return ((CallSite<Func<CallSite, object, IEnumerator>>)site).Update(site, obj);
        }
    }

    /// <summary>
    /// This binder is used for the @() operator.
    /// </summary>
    internal sealed class PSToObjectArrayBinder : DynamicMetaObjectBinder
    {
        private static readonly PSToObjectArrayBinder s_binder = new PSToObjectArrayBinder();

        internal static PSToObjectArrayBinder Get()
        {
            return s_binder;
        }

        private PSToObjectArrayBinder()
        {
        }

        public override string ToString()
        {
            return "ToObjectArray";
        }

        public override Type ReturnType { get { return typeof(object[]); } }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            if (!target.HasValue)
            {
                return Defer(target, args);
            }

            if (target.Value == AutomationNull.Value)
            {
                return new DynamicMetaObject(Expression.Constant(Array.Empty<object>()),
                    BindingRestrictions.GetInstanceRestriction(target.Expression, AutomationNull.Value)).WriteToDebugLog(this);
            }

            var enumerable = PSEnumerableBinder.IsEnumerable(target);
            if (enumerable == null)
            {
                return new DynamicMetaObject(
                    Expression.NewArrayInit(typeof(object), target.Expression.Cast(typeof(object))),
                    target.PSGetTypeRestriction()).WriteToDebugLog(this);
            }

            var value = PSObject.Base(target.Value);
            if (value is List<object>)
            {
                return new DynamicMetaObject(
                    Expression.Call(PSEnumerableBinder.MaybeDebase(this, static e => e.Cast(typeof(List<object>)), target), CachedReflectionInfo.ObjectList_ToArray),
                    PSEnumerableBinder.GetRestrictions(target)).WriteToDebugLog(this);
            }

            // It's enumerable, but not an List<object>.  Call EnumerableOps.ToArray
            return new DynamicMetaObject(
                Expression.Call(CachedReflectionInfo.EnumerableOps_ToArray, enumerable.Expression),
                target.PSGetTypeRestriction()).WriteToDebugLog(this);
        }
    }

    internal sealed class PSPipeWriterBinder : DynamicMetaObjectBinder
    {
        private static readonly PSPipeWriterBinder s_binder = new PSPipeWriterBinder();

        internal static PSPipeWriterBinder Get()
        {
            return s_binder;
        }

        private PSPipeWriterBinder()
        {
            CacheTarget((Action<CallSite, object, Pipe, ExecutionContext>)StringRule);
            CacheTarget((Action<CallSite, object, Pipe, ExecutionContext>)AutomationNullRule);
            CacheTarget((Action<CallSite, object, Pipe, ExecutionContext>)BoolRule);
            CacheTarget((Action<CallSite, object, Pipe, ExecutionContext>)IntRule);
        }

        public override string ToString()
        {
            return "PipelineWriter";
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            // args[0] is the pipe
            // args[1] is the execution context, only used if we're enumerating

            if (!target.HasValue)
            {
                return Defer(target, args);
            }

            if (target.Value == AutomationNull.Value)
            {
                return (new DynamicMetaObject(
                    Expression.Block(typeof(void), Expression.Call(CachedReflectionInfo.PipelineOps_Nop)),
                    BindingRestrictions.GetInstanceRestriction(target.Expression, AutomationNull.Value))).WriteToDebugLog(this);
            }

            var enumerable = PSEnumerableBinder.IsEnumerable(target);
            if (enumerable == null)
            {
                var bindingResult = PSVariableAssignmentBinder.Get().Bind(target, Array.Empty<DynamicMetaObject>());
                var restrictions = target.LimitType.IsValueType
                    ? bindingResult.Restrictions
                    : target.PSGetTypeRestriction();
                return (new DynamicMetaObject(
                    Expression.Call(args[0].Expression,
                                    CachedReflectionInfo.Pipe_Add,
                                    bindingResult.Expression.Cast(typeof(object))),
                    restrictions)).WriteToDebugLog(this);
            }

            bool needsToDispose = PSObject.Base(target.Value) is not IEnumerator;
            return (new DynamicMetaObject(
                Expression.Call(CachedReflectionInfo.EnumerableOps_WriteEnumerableToPipe,
                                enumerable.Expression,
                                args[0].Expression,
                                args[1].Expression,
                                ExpressionCache.Constant(needsToDispose)),
                enumerable.Restrictions)).WriteToDebugLog(this);
        }

        private static void BoolRule(CallSite site, object obj, Pipe pipe, ExecutionContext context)
        {
            if (obj is bool) { pipe.Add(obj); }
            else { ((CallSite<Action<CallSite, object, Pipe, ExecutionContext>>)site).Update(site, obj, pipe, context); }
        }

        private static void IntRule(CallSite site, object obj, Pipe pipe, ExecutionContext context)
        {
            if (obj is int) { pipe.Add(obj); }
            else { ((CallSite<Action<CallSite, object, Pipe, ExecutionContext>>)site).Update(site, obj, pipe, context); }
        }

        private static void StringRule(CallSite site, object obj, Pipe pipe, ExecutionContext context)
        {
            if (obj is string) { pipe.Add(obj); }
            else { ((CallSite<Action<CallSite, object, Pipe, ExecutionContext>>)site).Update(site, obj, pipe, context); }
        }

        private static void AutomationNullRule(CallSite site, object obj, Pipe pipe, ExecutionContext context)
        {
            if (obj != AutomationNull.Value) { ((CallSite<Action<CallSite, object, Pipe, ExecutionContext>>)site).Update(site, obj, pipe, context); }
        }
    }

    /// <summary>
    /// This binder creates the collection we use to do multi-assignments, e.g.:
    ///     $x,$y = $z
    ///     $x,$y,$z = 1,2,3,4,5
    /// The target in this binder is the RHS, the result expression is an IList where the Count matches the
    /// number of values assigned (_elements) on the left hand side of the assign.
    /// </summary>
    internal sealed class PSArrayAssignmentRHSBinder : DynamicMetaObjectBinder
    {
        private static readonly List<PSArrayAssignmentRHSBinder> s_binders = new List<PSArrayAssignmentRHSBinder>();
        private readonly int _elements;

        internal static PSArrayAssignmentRHSBinder Get(int i)
        {
            lock (s_binders)
            {
                while (s_binders.Count <= i)
                {
                    s_binders.Add(null);
                }

                return s_binders[i] ?? (s_binders[i] = new PSArrayAssignmentRHSBinder(i));
            }
        }

        private PSArrayAssignmentRHSBinder(int elements)
        {
            _elements = elements;
        }

        public override string ToString()
        {
            return string.Create(CultureInfo.InvariantCulture, $"MultiAssignRHSBinder {_elements}");
        }

        public override Type ReturnType { get { return typeof(IList); } }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            Diagnostics.Assert(args.Length == 0, "binder doesn't expect any arguments");

            if (!target.HasValue)
            {
                return Defer(target).WriteToDebugLog(this);
            }

            if (target.Value is PSObject && (PSObject.Base(target.Value) != target.Value))
            {
                return this.DeferForPSObject(target).WriteToDebugLog(this);
            }

            var iList = target.Value as IList;
            if (iList != null)
            {
                // 3 possibilities - too few, exact, or too many elements.

                var getListCountExpr = Expression.Property(target.Expression.Cast(typeof(ICollection)), CachedReflectionInfo.ICollection_Count);

                var restrictions = target.PSGetTypeRestriction().Merge(
                    BindingRestrictions.GetExpressionRestriction(Expression.Equal(getListCountExpr,
                                                                                  ExpressionCache.Constant(iList.Count))));

                if (iList.Count == _elements)
                {
                    // Exact, we can use the value as is.
                    return new DynamicMetaObject(target.Expression.Cast(typeof(IList)), restrictions).WriteToDebugLog(this);
                }

                int i;
                Expression[] newArrayElements = new Expression[_elements];
                var temp = Expression.Variable(typeof(IList));
                if (iList.Count < _elements)
                {
                    // Too few, create an array with the correct number assigned, fill in null for the extras

                    for (i = 0; i < iList.Count; ++i)
                    {
                        newArrayElements[i] =
                            Expression.Call(temp, CachedReflectionInfo.IList_get_Item, ExpressionCache.Constant(i));
                    }

                    for (; i < _elements; ++i)
                    {
                        newArrayElements[i] = ExpressionCache.NullConstant;
                    }
                }
                else
                {
                    // Too many, create an array with the correct number assigned, and the last element contains
                    // all the extras.
                    for (i = 0; i < _elements - 1; ++i)
                    {
                        newArrayElements[i] =
                            Expression.Call(temp, CachedReflectionInfo.IList_get_Item, ExpressionCache.Constant(i));
                    }

                    newArrayElements[_elements - 1] =
                        Expression.Call(CachedReflectionInfo.EnumerableOps_GetSlice, temp, ExpressionCache.Constant(_elements - 1)).Cast(typeof(object));
                }

                return (new DynamicMetaObject(
                    Expression.Block(
                        new[] { temp },
                        Expression.Assign(temp, target.Expression.Cast(typeof(IList))),
                        Expression.NewArrayInit(typeof(object), newArrayElements)),
                    restrictions)).WriteToDebugLog(this);
            }

            // We have a single element, the rest must be null.
            return (new DynamicMetaObject(
                Expression.NewArrayInit(typeof(object), Enumerable.Repeat(ExpressionCache.NullConstant, _elements - 1)
                                                .Prepend(target.Expression.Cast(typeof(object)))),
                target.PSGetTypeRestriction())).WriteToDebugLog(this);
        }
    }

    /// <summary>
    /// This binder is used to convert objects to string in specific circumstances, including:
    ///
    ///     * The LHS of a format expression.  The arguments (the RHS objects) of the format
    ///       expression are not converted to string here, that is defered to string.Format which
    ///       may have some custom formatting to apply.
    ///     * The objects passed to the format expression as part of an expandable string.  In this
    ///       case, the format string is generated by the parser, so we know that there is no custom
    ///       formatting to consider.
    /// </summary>
    internal sealed class PSToStringBinder : DynamicMetaObjectBinder
    {
        private static readonly PSToStringBinder s_binder = new PSToStringBinder();

        internal static PSToStringBinder Get()
        {
            return s_binder;
        }

        public override Type ReturnType { get { return typeof(string); } }

        private PSToStringBinder()
        {
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            // target is the object to convert
            // args[0] is the ExecutionContext, needed to get $OFS, and possibly the type table.

            if (!target.HasValue || !args[0].HasValue)
            {
                return Defer(target, args).WriteToDebugLog(this);
            }

            if (target.Value is PSObject && (PSObject.Base(target.Value) != target.Value))
            {
                return this.DeferForPSObject(target, args[0]).WriteToDebugLog(this);
            }

            var restrictions = target.PSGetTypeRestriction();
            if (target.LimitType == typeof(string))
            {
                return (new DynamicMetaObject(
                    target.Expression.Cast(typeof(string)),
                    restrictions)).WriteToDebugLog(this);
            }

            // PSObject.ToStringParser will handle everything, but if you want to speed up conversion to string,
            // add special cases here.

            return (new DynamicMetaObject(
                InvokeToString(args[0].Expression, target.Expression),
                restrictions)).WriteToDebugLog(this);
        }

        internal static Expression InvokeToString(Expression context, Expression target)
        {
            if (target.Type == typeof(string))
            {
                return target;
            }

            // PSObject.ToStringParser will handle everything, but if you want to speed up conversion to string,
            // add special cases here.

            return Expression.Call(CachedReflectionInfo.PSObject_ToStringParser,
                                   context.Cast(typeof(ExecutionContext)),
                                   target.Cast(typeof(object)));
        }
    }

    /// <summary>
    /// This binder is used to optimize the conversion of the result.
    /// </summary>
    internal sealed class PSPipelineResultToBoolBinder : DynamicMetaObjectBinder
    {
        private static readonly PSPipelineResultToBoolBinder s_binder = new PSPipelineResultToBoolBinder();

        internal static PSPipelineResultToBoolBinder Get()
        {
            return s_binder;
        }

        private PSPipelineResultToBoolBinder()
        {
        }

        public override Type ReturnType { get { return typeof(bool); } }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            if (!target.HasValue)
            {
                return Defer(target).WriteToDebugLog(this);
            }

            IList pipelineResult = target.Value as IList;
            Diagnostics.Assert(pipelineResult != null, "Pipeline result is always an IList");

            var ilistExpr = target.Expression;
            if (typeof(IList) != ilistExpr.Type)
            {
                ilistExpr = Expression.Convert(ilistExpr, typeof(IList));
            }

            var countExpr = Expression.Property(
                Expression.Convert(ilistExpr, typeof(ICollection)), CachedReflectionInfo.ICollection_Count);

            Expression result;
            BindingRestrictions restrictions;
            switch (pipelineResult.Count)
            {
                case 0:
                    result = ExpressionCache.Constant(false);
                    restrictions = BindingRestrictions.GetExpressionRestriction(
                        Expression.Equal(countExpr, ExpressionCache.Constant(0)));
                    break;
                case 1:
                    result = Expression.Call(ilistExpr,
                                             CachedReflectionInfo.IList_get_Item,
                                             ExpressionCache.Constant(0)).Convert(typeof(bool));
                    restrictions = BindingRestrictions.GetExpressionRestriction(
                        Expression.Equal(countExpr, ExpressionCache.Constant(1)));
                    break;
                default:
                    result = ExpressionCache.Constant(true);
                    restrictions = BindingRestrictions.GetExpressionRestriction(
                        Expression.GreaterThan(countExpr, ExpressionCache.Constant(1)));
                    break;
            }

            return (new DynamicMetaObject(result, restrictions)).WriteToDebugLog(this);
        }
    }

    internal sealed class PSInvokeDynamicMemberBinder : DynamicMetaObjectBinder
    {
        private sealed class KeyComparer : IEqualityComparer<PSInvokeDynamicMemberBinderKeyType>
        {
            public bool Equals(PSInvokeDynamicMemberBinderKeyType x, PSInvokeDynamicMemberBinderKeyType y)
            {
                return x.Item1.Equals(y.Item1) &&
                       ((x.Item2 == null) ? y.Item2 == null : x.Item2.Equals(y.Item2)) &&
                       x.Item3 == y.Item3 &&
                       x.Item4 == y.Item4 &&
                       x.Item5 == y.Item5;
            }

            public int GetHashCode(PSInvokeDynamicMemberBinderKeyType obj)
            {
                return obj.GetHashCode();
            }
        }

        private static readonly
            Dictionary<PSInvokeDynamicMemberBinderKeyType, PSInvokeDynamicMemberBinder> s_binderCache
            = new Dictionary<PSInvokeDynamicMemberBinderKeyType, PSInvokeDynamicMemberBinder>(new KeyComparer());

        internal static PSInvokeDynamicMemberBinder Get(CallInfo callInfo, TypeDefinitionAst classScopeAst, bool @static, bool propertySetter, PSMethodInvocationConstraints constraints)
        {
            PSInvokeDynamicMemberBinder result;

            var classScope = classScopeAst?.Type;
            lock (s_binderCache)
            {
                var key = Tuple.Create(callInfo, constraints, propertySetter, @static, classScope);
                if (!s_binderCache.TryGetValue(key, out result))
                {
                    result = new PSInvokeDynamicMemberBinder(callInfo, @static, propertySetter, constraints, classScope);
                    s_binderCache.Add(key, result);
                }
            }

            return result;
        }

        private readonly CallInfo _callInfo;
        private readonly bool _static;
        private readonly bool _propertySetter;
        private readonly PSMethodInvocationConstraints _constraints;
        private readonly Type _classScope;

        private PSInvokeDynamicMemberBinder(CallInfo callInfo, bool @static, bool propertySetter, PSMethodInvocationConstraints constraints, Type classScope)
        {
            Diagnostics.Assert(callInfo != null, "callers make sure 'callInfo' is not null");

            _callInfo = callInfo;
            _static = @static;
            _propertySetter = propertySetter;
            _constraints = constraints;
            _classScope = classScope;
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            if (!target.HasValue || !args[0].HasValue)
            {
                return Defer(target, args[0]).WriteToDebugLog(this);
            }

            var memberNameArg = args[0];
            object memberNameValue = PSObject.Base(memberNameArg.Value);

            Expression bindingStrExpr;
            var memberName = memberNameValue as string;
            if (memberName != null)
            {
                if (memberNameArg.Value is PSObject)
                {
                    bindingStrExpr = Expression.Call(CachedReflectionInfo.PSObject_Base, memberNameArg.Expression).Cast(typeof(string));
                }
                else
                {
                    bindingStrExpr = memberNameArg.Expression.Cast(typeof(string));
                }
            }
            else
            {
                // Context is explicitly null here, we don't want $OFS to influence the property name, or
                // we'd generate code that wouldn't work consistently, depending on the value of $OFS.
                memberName = PSObject.ToStringParser(null, memberNameArg.Value);
                bindingStrExpr = PSToStringBinder.InvokeToString(ExpressionCache.NullConstant, memberNameArg.Expression);
            }
            // Note: Need to create DynamicExpression to support dynamic member invoke, see PSSetDynamicMemberBinder for example
            var result = PSInvokeMemberBinder.Get(memberName, _callInfo, _static, _propertySetter, _constraints, _classScope).FallbackInvokeMember(target, args.Skip(1).ToArray());
            BindingRestrictions restrictions = result.Restrictions;
            restrictions = restrictions.Merge(args[0].PSGetTypeRestriction());
            restrictions = restrictions.Merge(BindingRestrictions.GetExpressionRestriction(
                Expression.Call(CachedReflectionInfo.String_Equals, Expression.Constant(memberName), bindingStrExpr, ExpressionCache.Ordinal)));
            return (new DynamicMetaObject(result.Expression, restrictions)).WriteToDebugLog(this);
        }
    }

    internal class PSDynamicGetOrSetBinderKeyComparer : IEqualityComparer<PSGetOrSetDynamicMemberBinderKeyType>
    {
        public bool Equals(PSGetOrSetDynamicMemberBinderKeyType x, PSGetOrSetDynamicMemberBinderKeyType y)
        {
            return x.Item1 == y.Item1 && x.Item2 == y.Item2;
        }

        public int GetHashCode(PSGetOrSetDynamicMemberBinderKeyType obj)
        {
            return obj.GetHashCode();
        }
    }

    internal sealed class PSGetDynamicMemberBinder : DynamicMetaObjectBinder
    {
        private static readonly Dictionary<PSGetOrSetDynamicMemberBinderKeyType, PSGetDynamicMemberBinder> s_binderCache =
            new Dictionary<PSGetOrSetDynamicMemberBinderKeyType, PSGetDynamicMemberBinder>(new PSDynamicGetOrSetBinderKeyComparer());

        internal static PSGetDynamicMemberBinder Get(TypeDefinitionAst classScope, bool @static)
        {
            PSGetDynamicMemberBinder binder;
            lock (s_binderCache)
            {
                var type = classScope?.Type;
                var tuple = Tuple.Create(type, @static);
                if (!s_binderCache.TryGetValue(tuple, out binder))
                {
                    binder = new PSGetDynamicMemberBinder(type, @static);
                    s_binderCache.Add(tuple, binder);
                }
            }

            return binder;
        }

        private readonly bool _static;
        private readonly Type _classScope;

        private PSGetDynamicMemberBinder(Type classScope, bool @static)
        {
            _static = @static;
            _classScope = classScope;
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            Diagnostics.Assert(args.Length == 1, "PSGetDynamicMemberBinder::Bind handles one and only one argument");
            if (!target.HasValue || !args[0].HasValue)
            {
                return Defer(target, args[0]).WriteToDebugLog(this);
            }

            var memberNameArg = args[0];
            object memberNameValue = PSObject.Base(memberNameArg.Value);

            Expression bindingStrExpr;
            BindingRestrictions restrictions;
            var memberName = memberNameValue as string;
            if (memberName != null)
            {
                if (memberNameArg.Value is PSObject)
                {
                    bindingStrExpr = Expression.Call(CachedReflectionInfo.PSObject_Base, memberNameArg.Expression).Cast(typeof(string));
                }
                else
                {
                    bindingStrExpr = memberNameArg.Expression.Cast(typeof(string));
                }
            }
            else if (PSObject.Base(target.Value) is IDictionary)
            {
                // We don't want to convert the member name to a string, we'll just try indexing the dictionary and nothing else.
                restrictions = target.PSGetTypeRestriction();
                restrictions = restrictions.Merge(BindingRestrictions.GetExpressionRestriction(
                    Expression.Not(Expression.TypeIs(args[0].Expression, typeof(string)))));
                return new DynamicMetaObject(
                    Expression.Call(CachedReflectionInfo.PSGetDynamicMemberBinder_GetIDictionaryMember,
                                    PSGetMemberBinder.GetTargetExpr(target, typeof(IDictionary)),
                                    args[0].Expression.Cast(typeof(object))),
                    restrictions).WriteToDebugLog(this);
            }
            else
            {
                // Context is explicitly null here, we don't want $OFS to influence the property name, or
                // we'd generate code that wouldn't work consistently, depending on the value of $OFS.
                memberName = PSObject.ToStringParser(null, memberNameArg.Value);
                bindingStrExpr = PSToStringBinder.InvokeToString(ExpressionCache.NullConstant, memberNameArg.Expression);
            }

            var result = DynamicExpression.Dynamic(PSGetMemberBinder.Get(memberName, _classScope, _static), typeof(object), target.Expression);
            restrictions = BindingRestrictions.Empty;
            restrictions = restrictions.Merge(args[0].PSGetTypeRestriction());
            restrictions = restrictions.Merge(BindingRestrictions.GetExpressionRestriction(
                Expression.Call(CachedReflectionInfo.String_Equals, Expression.Constant(memberName), bindingStrExpr, ExpressionCache.Ordinal)));
            return (new DynamicMetaObject(result, restrictions)).WriteToDebugLog(this);
        }

        internal static object GetIDictionaryMember(IDictionary hash, object key)
        {
            try
            {
                key = PSObject.Base(key);
                if (hash.Contains(key))
                {
                    return hash[key];
                }
            }
            catch (InvalidOperationException)
            {
            }

            var context = LocalPipeline.GetExecutionContextFromTLS();
            if (context.IsStrictVersion(2))
            {
                // If the member is undefined and we're in strict mode, throw an exception...
                throw new PropertyNotFoundException("PropertyNotFoundStrict", null, ParserStrings.PropertyNotFoundStrict,
                                                    LanguagePrimitives.ConvertTo<string>(key));
            }

            return null;
        }
    }

    internal sealed class PSSetDynamicMemberBinder : DynamicMetaObjectBinder
    {
        private static readonly Dictionary<PSGetOrSetDynamicMemberBinderKeyType, PSSetDynamicMemberBinder> s_binderCache =
            new Dictionary<PSGetOrSetDynamicMemberBinderKeyType, PSSetDynamicMemberBinder>(new PSDynamicGetOrSetBinderKeyComparer());

        internal static PSSetDynamicMemberBinder Get(TypeDefinitionAst classScope, bool @static)
        {
            PSSetDynamicMemberBinder binder;
            lock (s_binderCache)
            {
                var type = classScope?.Type;
                var tuple = Tuple.Create(type, @static);
                if (!s_binderCache.TryGetValue(tuple, out binder))
                {
                    binder = new PSSetDynamicMemberBinder(type, @static);
                    s_binderCache.Add(tuple, binder);
                }
            }

            return binder;
        }

        private readonly bool _static;
        private readonly Type _classScope;

        private PSSetDynamicMemberBinder(Type classScope, bool @static)
        {
            _static = @static;
            _classScope = classScope;
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            Diagnostics.Assert(args.Length == 2, "PSSetDynamicMemberBinder::Bind handles two and only two arguments");
            if (!target.HasValue || !args[0].HasValue)
            {
                return Defer(target, args[0]).WriteToDebugLog(this);
            }

            var memberNameArg = args[0];
            object memberNameValue = PSObject.Base(memberNameArg.Value);

            Expression bindingStrExpr;
            var memberName = memberNameValue as string;
            if (memberName != null)
            {
                if (memberNameArg.Value is PSObject)
                {
                    bindingStrExpr = Expression.Call(CachedReflectionInfo.PSObject_Base, memberNameArg.Expression).Cast(typeof(string));
                }
                else
                {
                    bindingStrExpr = memberNameArg.Expression.Cast(typeof(string));
                }
            }
            else
            {
                // Context is explicitly null here, we don't want $OFS to influence the property name, or
                // we'd generate code that wouldn't work consistently, depending on the value of $OFS.
                memberName = PSObject.ToStringParser(null, memberNameArg.Value);
                bindingStrExpr = PSToStringBinder.InvokeToString(ExpressionCache.NullConstant, memberNameArg.Expression);
            }

            var result = DynamicExpression.Dynamic(PSSetMemberBinder.Get(memberName, _classScope, _static), typeof(object), target.Expression, args[1].Expression);
            var restrictions = BindingRestrictions.Empty;
            // Note: Optimal restriction is test target type is IDictionary or not
            restrictions = restrictions.Merge(args[0].PSGetTypeRestriction());
            restrictions = restrictions.Merge(BindingRestrictions.GetExpressionRestriction(
                Expression.Call(CachedReflectionInfo.String_Equals, Expression.Constant(memberName), bindingStrExpr, ExpressionCache.Ordinal)));

            Expression resultExpr;
            if (target.Value is IDictionary)
            {
                // We should first try:
                //     $target[$arg[0]] = $arg[1]
                // And if that fails, try arg[0] converted to string, setting a property, etc.

                var exception = Expression.Variable(typeof(Exception));
                var indexResult = PSSetIndexBinder.Get(1).FallbackSetIndex(target, new[] { args[0] }, args[1]);
                resultExpr = Expression.TryCatch(
                    indexResult.Expression,
                    Expression.Catch(exception, result));
            }
            else
            {
                resultExpr = result;
            }

            return (new DynamicMetaObject(resultExpr, restrictions)).WriteToDebugLog(this);
        }
    }

    internal sealed class PSSwitchClauseEvalBinder : DynamicMetaObjectBinder
    {
        // Increase this cache size if we add a new flag to the switch statement that:
        //    - Influences evaluation of switch elements
        //    - Is commonly used
        private static readonly PSSwitchClauseEvalBinder[] s_binderCache = new PSSwitchClauseEvalBinder[32];
        private readonly SwitchFlags _flags;

        internal static PSSwitchClauseEvalBinder Get(SwitchFlags flags)
        {
            lock (s_binderCache)
            {
                return s_binderCache[(int)flags] ?? (s_binderCache[(int)flags] = new PSSwitchClauseEvalBinder(flags));
            }
        }

        private PSSwitchClauseEvalBinder(SwitchFlags flags)
        {
            _flags = flags;
        }

        public override Type ReturnType { get { return typeof(bool); } }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            // target is the condition
            // args[0] is the value to test against the condition
            // args[1] is the execution context to use, if needed

            if (!target.HasValue || !args[0].HasValue)
            {
                return Defer(target, args[0], args[1]).WriteToDebugLog(this);
            }

            // No need to add binding restrictions on either arg, the type of the clause is all that matters.
            // We can skip the restrictions on the arg, the generated code will contain a dynamic site to convert
            // the arg to string if applicable.  If the dynamic site is removed, then arg restrictions must be added.
            var targetRestrictions = target.PSGetTypeRestriction();

            // If any args are PSObject, we typically call DeferForPSObject, but in this case,
            // we don't because args[0] should not be unwrapped, which DeferForPSObject would do.
            if (target.Value is PSObject)
            {
                return (new DynamicMetaObject(
                    DynamicExpression.Dynamic(this, this.ReturnType,
                                              Expression.Call(CachedReflectionInfo.PSObject_Base,
                                                              target.Expression.Cast(typeof(object))),
                                              args[0].Expression,
                                              args[1].Expression),
                    targetRestrictions)).WriteToDebugLog(this);
            }

            if (target.Value == null)
            {
                // If the condition is null, the we simply test the value against null.  It seems like
                // this is a silly thing to allow in a switch, maybe it should be disallowed in strict mode.
                return (new DynamicMetaObject(
                    Expression.Equal(args[0].Expression.Cast(typeof(object)), ExpressionCache.NullConstant),
                    target.PSGetTypeRestriction())).WriteToDebugLog(this);
            }

            if (target.Value is ScriptBlock)
            {
                var call = Expression.Call(target.Expression.Cast(typeof(ScriptBlock)),
                                               CachedReflectionInfo.ScriptBlock_DoInvokeReturnAsIs,
                    /*useLocalScope=*/         ExpressionCache.Constant(true),
                    /*errorHandlingBehavior=*/ Expression.Constant(ScriptBlock.ErrorHandlingBehavior.WriteToExternalErrorPipe),
                    /*dollarUnder=*/           args[0].CastOrConvert(typeof(object)),
                    /*input=*/                 ExpressionCache.AutomationNullConstant,
                    /*scriptThis=*/            ExpressionCache.AutomationNullConstant,
                    /*args=*/                  ExpressionCache.NullObjectArray);

                return (new DynamicMetaObject(
                    DynamicExpression.Dynamic(PSConvertBinder.Get(typeof(bool)), typeof(bool), call),
                                              targetRestrictions)).WriteToDebugLog(this);
            }

            // From here on out, arg must be a string.
            var executionContext = args[1].Expression;
            var argAsString = DynamicExpression.Dynamic(PSToStringBinder.Get(), typeof(string), args[0].Expression,
                                                        executionContext);

            if (target.Value is Regex || (_flags & SwitchFlags.Regex) != 0)
            {
                var call = Expression.Call(CachedReflectionInfo.SwitchOps_ConditionSatisfiedRegex,
                    /*caseSensitive=*/ ExpressionCache.Constant((_flags & SwitchFlags.CaseSensitive) != 0),
                    /*condition=*/     target.Expression.Cast(typeof(object)),
                    /*errorPosition=*/ ExpressionCache.NullExtent,
                    /*str=*/           argAsString,
                    /*context=*/       executionContext);

                return (new DynamicMetaObject(call, targetRestrictions)).WriteToDebugLog(this);
            }

            if (target.Value is WildcardPattern || (_flags & SwitchFlags.Wildcard) != 0)
            {
                var call = Expression.Call(CachedReflectionInfo.SwitchOps_ConditionSatisfiedWildcard,
                    /*caseSensitive=*/ ExpressionCache.Constant((_flags & SwitchFlags.CaseSensitive) != 0),
                    /*condition=*/     target.Expression.Cast(typeof(object)),
                    /*str=*/           argAsString,
                    /*context=*/       executionContext);

                // Binding restrictions must test the target, even if the switch is in -regex mode.
                // If we didn't add restrictions on the target, we'd incorrectly use this rule when the target
                // is a script block.
                // We can skip the restrictions on the arg, the generated code contains a dynamic site to convert
                // the arg to string, so that site handles any arg type properly.
                return (new DynamicMetaObject(call, targetRestrictions)).WriteToDebugLog(this);
            }

            var targetAsString = DynamicExpression.Dynamic(PSToStringBinder.Get(), typeof(string), target.Expression,
                                                           executionContext);
            return (new DynamicMetaObject(
                Compiler.CallStringEquals(targetAsString, argAsString, ((_flags & SwitchFlags.CaseSensitive) == 0)),
                targetRestrictions)).WriteToDebugLog(this);
        }
    }

    // This class implements the standard binder CreateInstanceBinder, but this binder handles the CallInfo a little differently.
    // The ArgumentNames are not used to invoke a constructor, instead they are used to set properties/fields in the attribute.
    internal sealed class PSAttributeGenerator : CreateInstanceBinder
    {
        private static readonly Dictionary<CallInfo, PSAttributeGenerator> s_binderCache =
            new Dictionary<CallInfo, PSAttributeGenerator>();

        internal static PSAttributeGenerator Get(CallInfo callInfo)
        {
            lock (s_binderCache)
            {
                PSAttributeGenerator binder;
                if (!s_binderCache.TryGetValue(callInfo, out binder))
                {
                    binder = new PSAttributeGenerator(callInfo);
                    s_binderCache.Add(callInfo, binder);
                }

                return binder;
            }
        }

        private PSAttributeGenerator(CallInfo callInfo)
            : base(callInfo)
        {
        }

        public override DynamicMetaObject FallbackCreateInstance(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            Diagnostics.Assert(target.HasValue && target.Value is Type, "caller to verify arguments");
            var attributeType = (Type)target.Value;
            var ctorInfos = attributeType.GetConstructors();
            var newConstructors = DotNetAdapter.GetMethodInformationArray(ctorInfos);

            // We can't use a type restriction on target, it's always a System.Type.  So make sure we always use an instance restriction
            target = new DynamicMetaObject(target.Expression, BindingRestrictions.GetInstanceRestriction(target.Expression, target.Value), target.Value);

            string errorId = null;
            string errorMsg = null;
            bool expandParamsOnBest;
            bool callNonVirtually;
            var positionalArgCount = CallInfo.ArgumentCount - CallInfo.ArgumentNames.Count;

            var bestMethod = Adapter.FindBestMethod(
                newConstructors,
                invocationConstraints: null,
                allowCastingToByRefLikeType: false,
                args.Take(positionalArgCount).Select(static arg => arg.Value).ToArray(),
                ref errorId,
                ref errorMsg,
                out expandParamsOnBest,
                out callNonVirtually);

            if (bestMethod == null)
            {
                return errorSuggestion ?? new DynamicMetaObject(
                    Expression.Throw(
                        Expression.New(CachedReflectionInfo.MethodException_ctor, Expression.Constant(errorId),
                                       Expression.Constant(null, typeof(Exception)), Expression.Constant(errorMsg),
                                       Expression.NewArrayInit(typeof(object),
                                            Expression.Constant(".ctor").Cast(typeof(object)),
                                            ExpressionCache.Constant(positionalArgCount).Cast(typeof(object)))),
                        this.ReturnType),
                    target.CombineRestrictions(args));
            }

            var constructorInfo = (ConstructorInfo)bestMethod.method;
            var parameterInfo = constructorInfo.GetParameters();
            var ctorArgs = new Expression[parameterInfo.Length];
            var argIndex = 0;
            for (; argIndex < parameterInfo.Length; ++argIndex)
            {
                var resultType = parameterInfo[argIndex].ParameterType;

                // The extension method 'CustomAttributeExtensions.GetCustomAttributes(ParameterInfo, Type, Boolean)' has inconsistent
                // behavior on its return value in both FullCLR and CoreCLR. According to MSDN, if the attribute cannot be found, it
                // should return an empty collection. However, it returns null in some rare cases [when the parameter isn't backed by
                // actual metadata].
                // This inconsistent behavior affects OneCore powershell because we are using the extension method here when compiling
                // against CoreCLR. So we need to add a null check until this is fixed in CLR.
                var paramArrayAttrs = parameterInfo[argIndex].GetCustomAttributes(typeof(ParamArrayAttribute), true);
                if (paramArrayAttrs != null && paramArrayAttrs.Length > 0 && expandParamsOnBest)
                {
                    var elementType = parameterInfo[argIndex].ParameterType.GetElementType();
                    var paramsArray = new List<Expression>();

                    int ctorArgIndex = argIndex;
                    for (int i = argIndex; i < positionalArgCount; ++i, ++argIndex)
                    {
                        bool debase;

                        // ConstrainedLanguage note - calls to this conversion are done by constructors with params arguments.
                        // Protection against conversions are covered by the method resolution algorithm
                        // (which ignores method arguments with disallowed types)
                        var conversion = LanguagePrimitives.FigureConversion(args[argIndex].Value, elementType, out debase);
                        Diagnostics.Assert(conversion.Rank != ConversionRank.None, "FindBestMethod should have failed if there is no conversion");

                        paramsArray.Add(
                            PSConvertBinder.InvokeConverter(conversion, args[i].Expression, elementType, debase, ExpressionCache.InvariantCulture));
                    }

                    ctorArgs[ctorArgIndex] = Expression.NewArrayInit(elementType, paramsArray);
                    break;
                }
                else
                {
                    var conversion = LanguagePrimitives.FigureConversion(args[argIndex].Value, resultType, out bool debase);
                    ctorArgs[argIndex] = PSConvertBinder.InvokeConverter(conversion, args[argIndex].Expression, resultType, debase, ExpressionCache.InvariantCulture);
                }
            }

            Expression result = Expression.New(constructorInfo, ctorArgs);
            if (CallInfo.ArgumentNames.Count > 0)
            {
                var tmp = Expression.Parameter(result.Type);
                var blockExprs = new List<Expression>();
                foreach (var name in CallInfo.ArgumentNames)
                {
                    var members = attributeType.GetMember(name, MemberTypes.Field | MemberTypes.Property,
                        BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    if (members.Length != 1
                        || (members[0] is not PropertyInfo && members[0] is not FieldInfo))
                    {
                        return target.ThrowRuntimeError(args, BindingRestrictions.Empty, "PropertyNotFoundForType",
                                                        ParserStrings.PropertyNotFoundForType, Expression.Constant(name),
                                                        Expression.Constant(attributeType, typeof(Type)));
                    }

                    var member = members[0];
                    Expression lhs;
                    Type propertyType;
                    var propertyInfo = member as PropertyInfo;
                    if (propertyInfo != null)
                    {
                        if (propertyInfo.GetSetMethod() == null)
                        {
                            return target.ThrowRuntimeError(args, BindingRestrictions.Empty, "PropertyIsReadOnly",
                                                            ParserStrings.PropertyIsReadOnly, Expression.Constant(name));
                        }

                        propertyType = propertyInfo.PropertyType;
                        lhs = Expression.Property(tmp.Cast(propertyInfo.DeclaringType), propertyInfo);
                    }
                    else
                    {
                        propertyType = ((FieldInfo)member).FieldType;
                        lhs = Expression.Field(tmp.Cast(member.DeclaringType), (FieldInfo)member);
                    }

                    bool debase;

                    // ConstrainedLanguage note - calls to these property assignment conversions are enforced by the
                    // property assignment binding rules (which disallow property conversions to disallowed types)
                    var conversion = LanguagePrimitives.FigureConversion(args[argIndex].Value, propertyType, out debase);
                    if (conversion.Rank == ConversionRank.None)
                    {
                        return PSConvertBinder.ThrowNoConversion(args[argIndex], propertyType, this, -1,
                            args.Except(new DynamicMetaObject[] { args[argIndex] }).Prepend(target).ToArray());
                    }

                    blockExprs.Add(
                        Expression.Assign(lhs, PSConvertBinder.InvokeConverter(conversion, args[argIndex].Expression, propertyType, debase, ExpressionCache.InvariantCulture)));
                    argIndex += 1;
                }

                // We wrap the block of assignments in a try/catch and issue a general error message whenever the assignment fails.
                var exception = Expression.Parameter(typeof(Exception));
                result = Expression.Block(
                    new ParameterExpression[] { tmp },
                    Expression.Assign(tmp, result),
                    Expression.TryCatch(
                        Expression.Block(typeof(void), blockExprs),
                        Expression.Catch(
                            exception,
                            Compiler.ThrowRuntimeErrorWithInnerException("PropertyAssignmentException", Expression.Property(exception, "Message"), exception, typeof(void)))),
                    // The result of the block is the object constructed, so the tmp must be the last expr in the block.
                    tmp);
            }

            return new DynamicMetaObject(result, target.CombineRestrictions(args));
        }
    }

    internal sealed class PSCustomObjectConverter : DynamicMetaObjectBinder
    {
        private static readonly PSCustomObjectConverter s_binder = new PSCustomObjectConverter();

        internal static PSCustomObjectConverter Get()
        {
            return s_binder;
        }

        private PSCustomObjectConverter()
        {
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            if (!target.HasValue)
            {
                return Defer(target).WriteToDebugLog(this);
            }

            var baseObjectValue = PSObject.Base(target.Value);
            var toType = baseObjectValue is OrderedDictionary || baseObjectValue is Hashtable
                ? typeof(LanguagePrimitives.InternalPSCustomObject)
                : typeof(PSObject);

            // ConstrainedLanguage note - calls to this conversion only target PSCustomObject / PSObject,
            // which is safe.
            bool debase;
            var conversion = LanguagePrimitives.FigureConversion(target.Value, toType, out debase);

            return new DynamicMetaObject(
                PSConvertBinder.InvokeConverter(conversion, target.Expression, toType, debase, ExpressionCache.InvariantCulture),
                target.PSGetTypeRestriction()).WriteToDebugLog(this);
        }
    }

    internal sealed class PSDynamicConvertBinder : DynamicMetaObjectBinder
    {
        private static readonly PSDynamicConvertBinder s_binder = new PSDynamicConvertBinder();

        internal static PSDynamicConvertBinder Get()
        {
            return s_binder;
        }

        private PSDynamicConvertBinder()
        {
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            // target is the type and is never a PSObject
            // arg is the object to be converted

            DynamicMetaObject arg = args[0];
            if (!target.HasValue || !arg.HasValue)
            {
                return Defer(target, arg).WriteToDebugLog(this);
            }

            var toType = target.Value as Type;
            Diagnostics.Assert(toType != null, "target must be a type");

            var bindingRestrictions = BindingRestrictions.GetInstanceRestriction(target.Expression, toType).Merge(arg.PSGetTypeRestriction());

            return new DynamicMetaObject(
                DynamicExpression.Dynamic(PSConvertBinder.Get(toType), toType, arg.Expression).Cast(typeof(object)),
                                          bindingRestrictions).WriteToDebugLog(this);
        }
    }

    /// <summary>
    /// This binder is used to copy mutable value types when assigning to variables, otherwise just assigning the target object directly.
    /// </summary>
    internal sealed class PSVariableAssignmentBinder : DynamicMetaObjectBinder
    {
        private static readonly PSVariableAssignmentBinder s_binder = new PSVariableAssignmentBinder();
        internal static int s_mutableValueWithInstanceMemberVersion;

        internal static PSVariableAssignmentBinder Get()
        {
            return s_binder;
        }

        private PSVariableAssignmentBinder()
        {
            CacheTarget((Func<CallSite, object, object>)(PSObjectStringRule));
            CacheTarget((Func<CallSite, object, object>)(ObjectRule));
            CacheTarget((Func<CallSite, object, object>)(IntRule));
            CacheTarget((Func<CallSite, object, object>)(EnumRule));
            CacheTarget((Func<CallSite, object, object>)(BoolRule));
            CacheTarget((Func<CallSite, object, object>)(NullRule));
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            if (!target.HasValue)
            {
                return Defer(target);
            }

            var value = target.Value;

            if (value == null)
            {
                return new DynamicMetaObject(ExpressionCache.NullConstant, target.PSGetTypeRestriction()).WriteToDebugLog(this);
            }

            var psobj = value as PSObject;
            if (psobj != null)
            {
                var restrictions = BindingRestrictions.GetTypeRestriction(target.Expression, psobj.GetType());
                var expr = target.Expression;
                var baseObject = psobj.BaseObject;
                var baseObjExpr = Expression.Property(expr.Cast(typeof(PSObject)), CachedReflectionInfo.PSObject_BaseObject);
                if (baseObject != null)
                {
                    var baseObjType = baseObject.GetType();
                    restrictions = restrictions.Merge(BindingRestrictions.GetTypeRestriction(baseObjExpr, baseObjType));
                    if (baseObjType.IsValueType)
                    {
                        expr = GetExprForValueType(baseObjType, Expression.Convert(baseObjExpr, baseObjType), expr, ref restrictions);
                    }
                }
                else
                {
                    restrictions =
                        restrictions.Merge(
                            BindingRestrictions.GetExpressionRestriction(Expression.Equal(baseObjExpr,
                                                                                          ExpressionCache.NullConstant)));
                }

                return new DynamicMetaObject(expr, restrictions).WriteToDebugLog(this);
            }

            var type = value.GetType();
            if (type.IsValueType)
            {
                var expr = target.Expression;
                var restrictions = target.PSGetTypeRestriction();
                expr = GetExprForValueType(type, Expression.Convert(expr, type), expr, ref restrictions);
                return new DynamicMetaObject(expr, restrictions).WriteToDebugLog(this);
            }

            // This rule is meant to cover all classes except PSObject.
            return new DynamicMetaObject(
                target.Expression, BindingRestrictions.GetExpressionRestriction(
                    Expression.AndAlso(
                        Expression.Not(Expression.TypeIs(target.Expression, typeof(ValueType))),
                        Expression.Not(Expression.TypeIs(target.Expression, typeof(PSObject)))))).WriteToDebugLog(this);
        }

        private static Expression GetExprForValueType(Type type,
                                                      Expression convertedExpr,
                                                      Expression originalExpr,
                                                      ref BindingRestrictions restrictions)
        {
            // A binding restriction for value types will be either:
            //    if (obj is SomeValueType)
            // or
            //    if (obj is SomeValueType && _mutableValueWithInstanceMemberVersion == someVersionNumber)
            //
            // And the expression will be one of 3 possibilities:
            //    * return expr
            //    * tmp = expr; return tmp (make a copy)
            //    * return CopyInstanceMembersOfValueType((T)expr, expr) (make a copy, also copy instance members)
            //
            // If we've never seen an instance member for a given type, we can avoid the expensive call to
            // CopyInstanceMembersOfValueType, but if somebody adds an instance member in the future, we need to invalidate
            // previously generated rules.  We do that with the version check.
            //
            // We can skip a version check if we're already generating the worst case code.  This also avoids regenerating
            // new bindings when the new binding won't differ from a previous binding.

            Expression expr;
            bool needVersionCheck = true;
            if (s_mutableValueTypesWithInstanceMembers.ContainsKey(type))
            {
                var genericMethodInfo = CachedReflectionInfo.PSVariableAssignmentBinder_CopyInstanceMembersOfValueType.MakeGenericMethod(new Type[] { type });
                expr = Expression.Call(genericMethodInfo, convertedExpr, originalExpr);
                needVersionCheck = false;
            }
            else if (IsValueTypeMutable(type))
            {
                var temp = Expression.Variable(type);
                expr = Expression.Block(new[] { temp },
                                        // The valuetype is mutable, so force a copy by assigning to a temp.
                                        Expression.Assign(temp, convertedExpr),
                                        // Box the return value because the dynamic site expects object
                                        temp.Cast(typeof(object)));
            }
            else
            {
                expr = originalExpr;
            }

            if (needVersionCheck)
            {
                restrictions = restrictions.Merge(GetVersionCheck(s_mutableValueWithInstanceMemberVersion));
            }

            return expr;
        }

        private static object EnumRule(CallSite site, object obj)
        {
            if (obj is Enum) { return obj; }

            return ((CallSite<Func<CallSite, object, object>>)site).Update(site, obj);
        }

        private static object BoolRule(CallSite site, object obj)
        {
            if (obj is bool) { return obj; }

            return ((CallSite<Func<CallSite, object, object>>)site).Update(site, obj);
        }

        private static object IntRule(CallSite site, object obj)
        {
            if (obj is int) { return obj; }

            return ((CallSite<Func<CallSite, object, object>>)site).Update(site, obj);
        }

        private static object ObjectRule(CallSite site, object obj)
        {
            if (obj is not ValueType && obj is not PSObject) { return obj; }

            return ((CallSite<Func<CallSite, object, object>>)site).Update(site, obj);
        }

        private static object PSObjectStringRule(CallSite site, object obj)
        {
            var psobj = obj as PSObject;
            if (psobj != null && psobj.BaseObject is string) return obj;
            return ((CallSite<Func<CallSite, object, object>>)site).Update(site, obj);
        }

        private static object NullRule(CallSite site, object obj)
        {
            return obj == null ? null : ((CallSite<Func<CallSite, object, object>>)site).Update(site, obj);
        }

        internal static bool IsValueTypeMutable(Type type)
        {
            // First, check for enums/primitives and compiler-defined attributes.
            if (type.IsPrimitive
                || type.IsEnum
                || type.IsDefined(typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute), inherit: false))
            {
                return false;
            }

            // If the builtin attribute is not present, check for a custom attribute from by the compiler. If the
            // library targets netstandard2.0, the compiler can't be sure the attribute will be provided by the runtime,
            // and defines its own attribute of the same name during compilation. To account for this, we must check the
            // type by name, not by reference.
            foreach (object attribute in type.GetCustomAttributes(inherit: false))
            {
                if (attribute.GetType().FullName.Equals(
                    "System.Runtime.CompilerServices.IsReadOnlyAttribute",
                    StringComparison.Ordinal))
                {
                    return false;
                }
            }

            // Fallback: check all fields (public + private) to verify whether they're all readonly.
            // If any field is not readonly, the value type is potentially mutable.
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!field.IsInitOnly)
                {
                    return true;
                }
            }

            // If all fields are init-only (read-only), then the value type is immutable.
            return false;
        }

        private static readonly ConcurrentDictionary<Type, bool> s_mutableValueTypesWithInstanceMembers =
            new ConcurrentDictionary<Type, bool>();

        internal static void NoteTypeHasInstanceMemberOrTypeName(Type type)
        {
            if (!type.IsValueType || !IsValueTypeMutable(type))
            {
                return;
            }

            if (s_mutableValueTypesWithInstanceMembers.TryAdd(type, true))
            {
                s_mutableValueWithInstanceMemberVersion += 1;
            }
        }

        internal static object CopyInstanceMembersOfValueType<T>(T t, object boxedT) where T : struct
        {
            if (PSObject.HasInstanceMembers(boxedT, out _) || PSObject.HasInstanceTypeName(boxedT, out _))
            {
                var psobj = PSObject.AsPSObject(boxedT);
                return PSObject.Base(psobj.Copy());
            }

            // We want a copy (because the value type is mutable), so return t, not boxedT.
            return t;
        }

        internal static BindingRestrictions GetVersionCheck(int expectedVersionNumber)
        {
            return BindingRestrictions.GetExpressionRestriction(
                Expression.Equal(Expression.Field(null, CachedReflectionInfo.PSVariableAssignmentBinder__mutableValueWithInstanceMemberVersion),
                                 ExpressionCache.Constant(expectedVersionNumber)));
        }
    }

    #endregion PowerShell non-standard language binders

    #region Standard binders

    /// <summary>
    /// The binder for common binary operators.  PowerShell specific binary operators are handled elsewhere.
    /// </summary>
    internal sealed class PSBinaryOperationBinder : BinaryOperationBinder
    {
        #region Constructors and factory methods

        private static readonly Dictionary<Tuple<ExpressionType, bool, bool>, PSBinaryOperationBinder> s_binderCache =
            new Dictionary<Tuple<ExpressionType, bool, bool>, PSBinaryOperationBinder>();

        internal static PSBinaryOperationBinder Get(ExpressionType operation, bool ignoreCase = true, bool scalarCompare = false)
        {
            PSBinaryOperationBinder result;
            lock (s_binderCache)
            {
                var key = Tuple.Create(operation, ignoreCase, scalarCompare);
                if (!s_binderCache.TryGetValue(key, out result))
                {
                    result = new PSBinaryOperationBinder(operation, ignoreCase, scalarCompare);
                    s_binderCache.Add(key, result);
                }
            }

            return result;
        }

        private readonly bool _ignoreCase;
        private readonly bool _scalarCompare;
        internal int _version;

        private PSBinaryOperationBinder(ExpressionType operation, bool ignoreCase, bool scalarCompare)
            : base(operation)
        {
            _ignoreCase = ignoreCase;
            _scalarCompare = scalarCompare;
            this._version = 0;
        }

        #endregion Constructors and factory methods

        #region Delegates for enumerable comparisons

        private Func<object, object, bool> _compareDelegate;

        private Func<object, object, bool> GetScalarCompareDelegate()
        {
            if (_compareDelegate == null)
            {
                var lvalExpr = Expression.Parameter(typeof(object), "lval");
                var rvalExpr = Expression.Parameter(typeof(object), "rval");

                var compareDelegate = Expression.Lambda<Func<object, object, bool>>(
                    DynamicExpression.Dynamic(Get(Operation, _ignoreCase, scalarCompare: true),
                                              typeof(object), lvalExpr, rvalExpr).Cast(typeof(bool)),
                    lvalExpr, rvalExpr).Compile();
                Interlocked.CompareExchange(ref _compareDelegate, compareDelegate, null);
            }

            return _compareDelegate;
        }

        #endregion Delegates for enumerable comparisons

        public override DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue || !arg.HasValue)
            {
                return Defer(target, arg).WriteToDebugLog(this);
            }

            if ((target.Value is PSObject && PSObject.Base(target.Value) != target.Value) ||
                     (arg.Value is PSObject && PSObject.Base(arg.Value) != arg.Value))
            {
                // When adding to an array, we don't want to unwrap the RHS - it's unnecessary,
                // and in the case of strings, we actually lose instance members on the PSObject.
                if (!(Operation == ExpressionType.Add && PSEnumerableBinder.IsEnumerable(target) != null))
                {
                    // Defer when the object is wrapped, but not for empty objects.
                    return this.DeferForPSObject(target, arg).WriteToDebugLog(this);
                }
            }

            switch (Operation)
            {
                case ExpressionType.Add:
                    return BinaryAdd(target, arg, errorSuggestion).WriteToDebugLog(this);
                case ExpressionType.Subtract:
                    return BinarySub(target, arg, errorSuggestion).WriteToDebugLog(this);
                case ExpressionType.Multiply:
                    return BinaryMultiply(target, arg, errorSuggestion).WriteToDebugLog(this);
                case ExpressionType.Divide:
                    return BinaryDivide(target, arg, errorSuggestion).WriteToDebugLog(this);
                case ExpressionType.Modulo:
                    return BinaryRemainder(target, arg, errorSuggestion).WriteToDebugLog(this);
                case ExpressionType.And:
                    return BinaryBitwiseAnd(target, arg, errorSuggestion).WriteToDebugLog(this);
                case ExpressionType.Or:
                    return BinaryBitwiseOr(target, arg, errorSuggestion).WriteToDebugLog(this);
                case ExpressionType.ExclusiveOr:
                    return BinaryBitwiseXor(target, arg, errorSuggestion).WriteToDebugLog(this);
                case ExpressionType.Equal:
                    return CompareEQ(target, arg, errorSuggestion).WriteToDebugLog(this);
                case ExpressionType.NotEqual:
                    return CompareNE(target, arg, errorSuggestion).WriteToDebugLog(this);
                case ExpressionType.GreaterThan:
                    return CompareGT(target, arg, errorSuggestion).WriteToDebugLog(this);
                case ExpressionType.GreaterThanOrEqual:
                    return CompareGE(target, arg, errorSuggestion).WriteToDebugLog(this);
                case ExpressionType.LessThan:
                    return CompareLT(target, arg, errorSuggestion).WriteToDebugLog(this);
                case ExpressionType.LessThanOrEqual:
                    return CompareLE(target, arg, errorSuggestion).WriteToDebugLog(this);
                case ExpressionType.LeftShift:
                    return LeftShift(target, arg, errorSuggestion).WriteToDebugLog(this);
                case ExpressionType.RightShift:
                    return RightShift(target, arg, errorSuggestion).WriteToDebugLog(this);
            }

            return (errorSuggestion ??
                    new DynamicMetaObject(
                        Compiler.CreateThrow(typeof(object), typeof(PSNotImplementedException), "Unimplemented operation"),
                        target.CombineRestrictions(arg))).WriteToDebugLog(this);
        }

        #region Helpers

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "PSBinaryOperationBinder {0}{1} ver:{2}",
                GetOperatorText(),
                _scalarCompare ? " scalarOnly" : string.Empty,
                _version);
        }

        internal static void InvalidateCache()
        {
            // Invalidate binders
            lock (s_binderCache)
            {
                foreach (PSBinaryOperationBinder binder in s_binderCache.Values)
                {
                    binder._version += 1;
                }
            }
        }

        private string GetOperatorText()
        {
            switch (Operation)
            {
                case ExpressionType.Add: return TokenKind.Plus.Text();
                case ExpressionType.Subtract: return TokenKind.Minus.Text();
                case ExpressionType.Multiply: return TokenKind.Multiply.Text();
                case ExpressionType.Divide: return TokenKind.Divide.Text();
                case ExpressionType.Modulo: return TokenKind.Rem.Text();
                case ExpressionType.And: return TokenKind.Band.Text();
                case ExpressionType.Or: return TokenKind.Bor.Text();
                case ExpressionType.ExclusiveOr: return TokenKind.Bxor.Text();
                case ExpressionType.Equal: return _ignoreCase ? TokenKind.Ieq.Text() : TokenKind.Ceq.Text();
                case ExpressionType.NotEqual: return _ignoreCase ? TokenKind.Ine.Text() : TokenKind.Cne.Text();
                case ExpressionType.GreaterThan: return _ignoreCase ? TokenKind.Igt.Text() : TokenKind.Cgt.Text();
                case ExpressionType.GreaterThanOrEqual: return _ignoreCase ? TokenKind.Ige.Text() : TokenKind.Cge.Text();
                case ExpressionType.LessThan: return _ignoreCase ? TokenKind.Ilt.Text() : TokenKind.Clt.Text();
                case ExpressionType.LessThanOrEqual: return _ignoreCase ? TokenKind.Ile.Text() : TokenKind.Cle.Text();
                case ExpressionType.LeftShift: return TokenKind.Shl.Text();
                case ExpressionType.RightShift: return TokenKind.Shr.Text();
            }

            Diagnostics.Assert(false, "Unexpected operator");
            return string.Empty;
        }

        private static DynamicMetaObject CallImplicitOp(string methodName, DynamicMetaObject target, DynamicMetaObject arg, string errorOperator, DynamicMetaObject errorSuggestion)
        {
            // We will assume that if we got here with a non-null errorSuggestion and DynamicObject, that we
            // are trying to generate the expression that calls the override to DynamicObject.TryBinaryOperation.
            // We get called twice for the same target object, once with a null errorSuggestion (in which case we'll have
            // returned the result below), and then a second time with a non-null errorSuggestion, which we return as is.
            if (errorSuggestion != null && target.Value is DynamicObject)
            {
                return errorSuggestion;
            }

            // TODO: use a dynamic call site to invoke the correct method or throw an error.

            return new DynamicMetaObject(
                Expression.Call(CachedReflectionInfo.ParserOps_ImplicitOp,
                                target.Expression.Cast(typeof(object)),
                                arg.Expression.Cast(typeof(object)),
                                Expression.Constant(methodName),
                                ExpressionCache.NullExtent,
                                Expression.Constant(errorOperator)),
                target.CombineRestrictions(arg));
        }

        private static bool IsValueNegative(object value, TypeCode typeCode)
        {
            switch (typeCode)
            {
                case TypeCode.SByte: return (sbyte)value < 0;
                case TypeCode.Int16: return (short)value < 0;
                case TypeCode.Int32: return (int)value < 0;
                case TypeCode.Int64: return (long)value < 0;
            }

            Diagnostics.Assert(false, "Invalid type code for testing negative value");
            return true;
        }

        private static Expression TypedZero(TypeCode typeCode)
        {
            switch (typeCode)
            {
                case TypeCode.SByte: return Expression.Constant((sbyte)0);
                case TypeCode.Int16: return Expression.Constant((short)0);
                case TypeCode.Int32: return ExpressionCache.Constant(0);
                case TypeCode.Int64: return Expression.Constant((long)0);
            }

            Diagnostics.Assert(false, "Invalid type code for testing negative value");
            return null;
        }

        private static DynamicMetaObject FigureSignedUnsignedInt(DynamicMetaObject obj, TypeCode typeCode, TypeCode currentOpType, out Type opImplType, out Type argType, out bool shouldFallbackToDoubleInCaseOfOverflow)
        {
            opImplType = null;
            argType = null;
            shouldFallbackToDoubleInCaseOfOverflow = false;

            if (IsValueNegative(obj.Value, typeCode))
            {
                switch (currentOpType)
                {
                    case TypeCode.UInt32:
                        opImplType = typeof(LongOps);
                        argType = typeof(long);
                        break;
                    case TypeCode.UInt64:
                        opImplType = typeof(DecimalOps);
                        argType = typeof(decimal);
                        // multiply operation may overflow within the [decimal] context, i.e. [int64]::minvalue * [uint64]::maxvalue.
                        // if overflow happens, we fallback to the [double] context
                        shouldFallbackToDoubleInCaseOfOverflow = true;
                        break;

                    default:
                        Diagnostics.Assert(false, "Need to figure out opImplType only for UIn32 and UInt64");
                        break;
                }

                return new DynamicMetaObject(
                    obj.Expression,
                    obj.PSGetTypeRestriction()
                        .Merge(BindingRestrictions.GetExpressionRestriction(
                            Expression.LessThan(obj.Expression.Cast(obj.LimitType), TypedZero(typeCode)))),
                    obj.Value);
            }

            return new DynamicMetaObject(
                obj.Expression,
                obj.PSGetTypeRestriction()
                    .Merge(BindingRestrictions.GetExpressionRestriction(
                        Expression.GreaterThanOrEqual(obj.Expression.Cast(obj.LimitType), TypedZero(typeCode)))),
                obj.Value);
        }

        private DynamicMetaObject BinaryNumericOp(string methodName, DynamicMetaObject target, DynamicMetaObject arg)
        {
            // The type code comparison and the code generated by this routine only supports primitive types
            // for both operands.
            Diagnostics.Assert(target.LimitType.IsNumericOrPrimitive() && arg.LimitType.IsNumericOrPrimitive(),
                               "numeric ops are only supported with primitive types");

            Type opImplType = null, argType = null;
            bool fallbackToDoubleInCaseOfOverflow = false;

            TypeCode leftTypeCode = LanguagePrimitives.GetTypeCode(target.LimitType);
            TypeCode rightTypeCode = LanguagePrimitives.GetTypeCode(arg.LimitType);
            TypeCode opTypeCode = (int)leftTypeCode >= (int)rightTypeCode ? leftTypeCode : rightTypeCode;
            if ((int)opTypeCode <= (int)TypeCode.Int32)
            {
                opImplType = typeof(IntOps);
                argType = typeof(int);
            }
            else if ((int)opTypeCode <= (int)TypeCode.UInt32)
            {
                Diagnostics.Assert(opTypeCode == TypeCode.UInt32, "opType must be UInt32 if it gets in this code path");

                // If one of the operands is signed, we need to promote to long if the value is negative, but
                // we can stay w/ an integer if the value is positive.  Either way, we'll need a type test.
                if (LanguagePrimitives.IsSignedInteger(leftTypeCode))
                {
                    target = FigureSignedUnsignedInt(target, leftTypeCode, opTypeCode, out opImplType, out argType, out fallbackToDoubleInCaseOfOverflow);
                }
                else if (LanguagePrimitives.IsSignedInteger(rightTypeCode))
                {
                    arg = FigureSignedUnsignedInt(arg, rightTypeCode, opTypeCode, out opImplType, out argType, out fallbackToDoubleInCaseOfOverflow);
                }

                if (opImplType == null)
                {
                    opImplType = typeof(UIntOps);
                    argType = typeof(uint);
                }
            }
            else if ((int)opTypeCode <= (int)TypeCode.Int64)
            {
                opImplType = typeof(LongOps);
                argType = typeof(long);
            }
            else if ((int)opTypeCode <= (int)TypeCode.UInt64)
            {
                Diagnostics.Assert(opTypeCode == TypeCode.UInt64, "opType must be UInt64 if it gets in this code path");

                if (LanguagePrimitives.IsSignedInteger(leftTypeCode))
                {
                    target = FigureSignedUnsignedInt(target, leftTypeCode, opTypeCode, out opImplType, out argType, out fallbackToDoubleInCaseOfOverflow);
                }
                else if (LanguagePrimitives.IsSignedInteger(rightTypeCode))
                {
                    arg = FigureSignedUnsignedInt(arg, rightTypeCode, opTypeCode, out opImplType, out argType, out fallbackToDoubleInCaseOfOverflow);
                }

                if (opImplType == null)
                {
                    opImplType = typeof(ULongOps);
                    argType = typeof(ulong);
                }
            }
            else if (opTypeCode == TypeCode.Decimal)
            {
                if (methodName.StartsWith("Compare", StringComparison.Ordinal))
                {
                    // Casting a double to decimal can overflow.  Instead, we are "smarter" and avoid
                    // the cast, and allow the comparison.  There may be a precision problem with values
                    // near Decimal.MaxValue or Decimal.MinValue, but V2 allowed the comparisons
                    // w/o errors, so we continue to do so.
                    if (LanguagePrimitives.IsFloating(leftTypeCode))
                    {
                        return new DynamicMetaObject(
                            Expression.Call(typeof(DecimalOps).GetMethod(methodName + "1", BindingFlags.NonPublic | BindingFlags.Static),
                                            target.Expression.Cast(target.LimitType).Cast(typeof(double)),
                                            arg.Expression.Cast(arg.LimitType).Cast(typeof(decimal))),
                            target.CombineRestrictions(arg));
                    }

                    if (LanguagePrimitives.IsFloating(rightTypeCode))
                    {
                        return new DynamicMetaObject(
                            Expression.Call(typeof(DecimalOps).GetMethod(methodName + "2", BindingFlags.NonPublic | BindingFlags.Static),
                                            target.Expression.Cast(target.LimitType).Cast(typeof(decimal)),
                                            arg.Expression.Cast(arg.LimitType).Cast(typeof(double))),
                            target.CombineRestrictions(arg));
                    }
                }

                opImplType = typeof(DecimalOps);
                argType = typeof(decimal);
            }
            else
            {
                opImplType = typeof(DoubleOps);
                argType = typeof(double);
            }

            Expression expr =
                Expression.Call(opImplType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static),
                                target.Expression.Cast(target.LimitType).Cast(argType),
                                arg.Expression.Cast(arg.LimitType).Cast(argType));

            if (fallbackToDoubleInCaseOfOverflow)
            {
                Type doubleOpType = typeof(DoubleOps);
                Type doubleArgType = typeof(double);
                Expression fallbackToDoubleExpr =
                    Expression.Call(doubleOpType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static),
                                target.Expression.Cast(target.LimitType).Cast(doubleArgType),
                                arg.Expression.Cast(arg.LimitType).Cast(doubleArgType));

                var exception = Expression.Variable(typeof(RuntimeException), "psBinaryNumericOpException");
                var catchExpr =
                    Expression.Catch(
                        exception,
                        Expression.Block(
                            Expression.IfThen(
                                Expression.Not(Expression.TypeIs(Expression.Property(exception, "InnerException"), typeof(OverflowException))),
                                Expression.Rethrow()),
                            fallbackToDoubleExpr)
                        );

                expr = Expression.TryCatch(
                           expr,
                           catchExpr);
            }

            if (target.LimitType.IsEnum)
            {
                // Make sure the result type is an enum unless we were expecting a bool.
                switch (Operation)
                {
                    case ExpressionType.Equal:
                    case ExpressionType.NotEqual:
                    case ExpressionType.GreaterThan:
                    case ExpressionType.GreaterThanOrEqual:
                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:
                        break;
                    default:
                        expr = expr.Cast(target.LimitType).Cast(typeof(object));
                        break;
                }
            }

            if (Operation == ExpressionType.Equal || Operation == ExpressionType.NotEqual)
            {
                expr = Expression.TryCatch(expr,
                    Expression.Catch(typeof(InvalidCastException),
                                     Operation == ExpressionType.Equal ? ExpressionCache.BoxedFalse : ExpressionCache.BoxedTrue));
            }

            return new DynamicMetaObject(expr, target.CombineRestrictions(arg));
        }

        private DynamicMetaObject BinaryNumericStringOp(DynamicMetaObject target, DynamicMetaObject arg)
        {
            // We can't determine the operation type at compile time, we'll need another dynamic site
            // Generate:
            //     tmp = Parser.ScanNumber(arg)
            //     if (tmp == null) { throw new RuntimeError("BadNumericConstant") }
            //     dynamic op target tmp
            // For equality comparison operators, if the conversion fails, we shouldn't raise
            // an exception, so those must be wrapped in try/catch, like:
            //     try { /* as above */ }
            //     catch (InvalidCastException) { true or false (depending on Operation type) }

            List<ParameterExpression> temps = new List<ParameterExpression>();
            List<Expression> stmts = new List<Expression>();
            Expression targetExpr = target.Expression;
            if (target.LimitType == typeof(string))
            {
                Diagnostics.Assert(
                    Operation == ExpressionType.Subtract || Operation == ExpressionType.Divide
                    || Operation == ExpressionType.Modulo || Operation == ExpressionType.And
                    || Operation == ExpressionType.Or || Operation == ExpressionType.ExclusiveOr
                    || Operation == ExpressionType.LeftShift || Operation == ExpressionType.RightShift,
                    "string lhs not allowed for operation");

                targetExpr = ConvertStringToNumber(target.Expression, arg.LimitType);
            }

            var argExpr = arg.LimitType == typeof(string) ? ConvertStringToNumber(arg.Expression, target.LimitType) : arg.Expression;

            stmts.Add(
                DynamicExpression.Dynamic(PSBinaryOperationBinder.Get(Operation), typeof(object), targetExpr, argExpr));

            Expression expr = Expression.Block(temps, stmts);
            if (Operation == ExpressionType.Equal || Operation == ExpressionType.NotEqual)
            {
                expr = Expression.TryCatch(expr,
                    Expression.Catch(typeof(InvalidCastException),
                                     Operation == ExpressionType.Equal ? ExpressionCache.BoxedFalse : ExpressionCache.BoxedTrue));
            }

            return new DynamicMetaObject(expr, target.CombineRestrictions(arg));
        }

        /// <summary>
        /// Use the tokenizer to scan a number and convert it to a number of any type.
        /// </summary>
        /// <param name="expr">
        /// The expression that refers to a string to be converted to a number of any type.
        /// </param>
        /// <param name="toType">
        /// Primarily used as part of an error message.  If the string is not a number, we want to raise an exception saying the
        /// string can't be converted to this type.  Note that if the string is a valid number, it need not be this type.
        /// </param>
        internal static Expression ConvertStringToNumber(Expression expr, Type toType)
        {
            if (!toType.IsNumeric())
            {
                // toType is only mostly for diagnostics, so it doesn't need to be correct.  If it's not numeric, we're
                // doing something like "42" - "10", and toType is the type of the "other" operand, in this case, it
                // would string.  Fall back to int if Parser.ScanNumber fails.
                toType = typeof(int);
            }

            return Expression.Call(
                CachedReflectionInfo.Parser_ScanNumber,
                expr.Cast(typeof(string)),
                Expression.Constant(toType, typeof(Type)),
                Expression.Constant(true));
        }

        private static DynamicMetaObject GetArgAsNumericOrPrimitive(DynamicMetaObject arg, Type targetType)
        {
            if (arg.Value == null)
            {
                return new DynamicMetaObject(ExpressionCache.Constant(0), arg.PSGetTypeRestriction(), 0);
            }

            bool boolToDecimal = false;
            if (arg.LimitType.IsNumericOrPrimitive() && !arg.LimitType.IsEnum)
            {
                if (!(targetType == typeof(decimal) && arg.LimitType == typeof(bool)))
                {
                    return arg;
                }
                // All other numeric conversions are simple casts, but bool=>decimal is not supported by LINQ (via Convert), so
                // we must do the conversion ourselves.
                boolToDecimal = true;
            }

            bool debase;

            // ConstrainedLanguage note - calls to this conversion only target numeric types.
            var conversion = LanguagePrimitives.FigureConversion(arg.Value, targetType, out debase);
            if (conversion.Rank == ConversionRank.ImplicitCast || boolToDecimal || arg.LimitType.IsEnum)
            {
                return new DynamicMetaObject(
                    PSConvertBinder.InvokeConverter(conversion, arg.Expression, targetType, debase, ExpressionCache.InvariantCulture),
                    arg.PSGetTypeRestriction());
            }

            return null;
        }

        private static Type GetBitwiseOpType(TypeCode opTypeCode)
        {
            Type opType;
            if ((int)opTypeCode <= (int)TypeCode.Int32) { opType = typeof(int); }
            else if ((int)opTypeCode <= (int)TypeCode.UInt32) { opType = typeof(uint); }
            else if ((int)opTypeCode <= (int)TypeCode.Int64) { opType = typeof(long); }
            // Because we use unsigned for -bnot, to be consistent, we promote to unsigned here too (-band,-bor,-xor)
            else { opType = typeof(ulong); }

            return opType;
        }

        #endregion Helpers

        #region "Arithmetic" operations

        private DynamicMetaObject BinaryAdd(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            if (target.Value == null)
            {
                return new DynamicMetaObject(arg.Expression.Cast(typeof(object)), target.CombineRestrictions(arg));
            }

            if (target.LimitType.IsNumericOrPrimitive() && target.LimitType != typeof(char))
            {
                var numericArg = GetArgAsNumericOrPrimitive(arg, target.LimitType);
                if (numericArg != null)
                {
                    return BinaryNumericOp("Add", target, numericArg);
                }

                if (arg.LimitType == typeof(string))
                {
                    return BinaryNumericStringOp(target, arg);
                }
            }

            Expression lhsStringExpr = null;
            if (target.LimitType == typeof(string))
            {
                lhsStringExpr = target.Expression.Cast(typeof(string));
            }
            else if (target.LimitType == typeof(char))
            {
                lhsStringExpr =
                    Expression.New(CachedReflectionInfo.String_ctor_char_int,
                                   target.Expression.Cast(typeof(char)),
                                   ExpressionCache.Constant(1));
            }

            if (lhsStringExpr != null)
            {
                // For string concatenation, simply add the 2 strings, possibly converting the rhs first.
                return new DynamicMetaObject(
                    Expression.Call(CachedReflectionInfo.String_Concat_String,
                                    lhsStringExpr,
                                    PSToStringBinder.InvokeToString(
                                        ExpressionCache.GetExecutionContextFromTLS,
                                        arg.Expression)),
                    target.CombineRestrictions(arg));
            }

            var lhsEnumerator = PSEnumerableBinder.IsEnumerable(target);
            if (lhsEnumerator != null)
            {
                // target is enumerable, so we're creating a new array.

                var rhsEnumerator = PSEnumerableBinder.IsEnumerable(arg);
                Expression call;
                if (rhsEnumerator != null)
                {
                    // Adding 2 lists
                    call = Expression.Call(CachedReflectionInfo.EnumerableOps_AddEnumerable,
                                           ExpressionCache.GetExecutionContextFromTLS,
                                           lhsEnumerator.Expression.Cast(typeof(IEnumerator)),
                                           rhsEnumerator.Expression.Cast(typeof(IEnumerator)));
                }
                else
                {
                    // Adding 1 item to a list
                    call = Expression.Call(CachedReflectionInfo.EnumerableOps_AddObject,
                                           ExpressionCache.GetExecutionContextFromTLS,
                                           lhsEnumerator.Expression.Cast(typeof(IEnumerator)),
                                           arg.Expression.Cast(typeof(object)));
                }

                return new DynamicMetaObject(call, target.CombineRestrictions(arg));
            }

            if (target.Value is IDictionary)
            {
                if (arg.Value is IDictionary)
                {
                    return new DynamicMetaObject(
                        Expression.Call(CachedReflectionInfo.HashtableOps_Add,
                                        target.Expression.Cast(typeof(IDictionary)),
                                        arg.Expression.Cast(typeof(IDictionary))),
                        target.CombineRestrictions(arg));
                }

                return target.ThrowRuntimeError(new DynamicMetaObject[] { arg }, BindingRestrictions.Empty,
                                                "AddHashTableToNonHashTable", ParserStrings.AddHashTableToNonHashTable);
            }

            return CallImplicitOp("op_Addition", target, arg, "+", errorSuggestion);
        }

        private DynamicMetaObject BinarySub(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            return BinarySubDivOrRem(target, arg, errorSuggestion, "Sub", "op_Subtraction", "-");
        }

        private DynamicMetaObject BinaryMultiply(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            if (target.Value == null)
            {
                // Result is null regardless of the arg.
                return new DynamicMetaObject(ExpressionCache.NullConstant, target.PSGetTypeRestriction());
            }

            if (target.LimitType.IsNumeric())
            {
                var numericArg = GetArgAsNumericOrPrimitive(arg, target.LimitType);
                if (numericArg != null)
                {
                    return BinaryNumericOp("Multiply", target, numericArg);
                }

                if (arg.LimitType == typeof(string))
                {
                    return BinaryNumericStringOp(target, arg);
                }
            }

            if (target.LimitType == typeof(string))
            {
                Expression argExpr = arg.LimitType == typeof(string)
                                         ? ConvertStringToNumber(arg.Expression, typeof(int)).Convert(typeof(int))
                                         : arg.CastOrConvert(typeof(int));

                return new DynamicMetaObject(
                    Expression.Call(CachedReflectionInfo.StringOps_Multiply,
                                    target.Expression.Cast(typeof(string)),
                                    argExpr),
                    target.CombineRestrictions(arg));
            }

            var lhsEnumerator = PSEnumerableBinder.IsEnumerable(target);
            if (lhsEnumerator != null)
            {
                Expression argExpr = arg.LimitType == typeof(string)
                                         ? ConvertStringToNumber(arg.Expression, typeof(int)).Convert(typeof(uint))
                                         : arg.CastOrConvert(typeof(uint));

                if (target.LimitType.IsArray)
                {
                    var elementType = target.LimitType.GetElementType();
                    return new DynamicMetaObject(
                        Expression.Call(CachedReflectionInfo.ArrayOps_Multiply.MakeGenericMethod(elementType),
                                        target.Expression.Cast(elementType.MakeArrayType()),
                                        argExpr),
                        target.CombineRestrictions(arg));
                }

                return new DynamicMetaObject(
                    Expression.Call(CachedReflectionInfo.EnumerableOps_Multiply,
                                    lhsEnumerator.Expression,
                                    argExpr),
                    target.CombineRestrictions(arg));
            }

            return CallImplicitOp("op_Multiply", target, arg, "*", errorSuggestion);
        }

        private DynamicMetaObject BinaryDivide(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            return BinarySubDivOrRem(target, arg, errorSuggestion, "Divide", "op_Division", "/");
        }

        private DynamicMetaObject BinaryRemainder(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            return BinarySubDivOrRem(target, arg, errorSuggestion, "Remainder", "op_Modulus", "%");
        }

        private DynamicMetaObject BinarySubDivOrRem(DynamicMetaObject target,
                                                    DynamicMetaObject arg,
                                                    DynamicMetaObject errorSuggestion,
                                                    string numericOpMethodName,
                                                    string implicitOpMethodName,
                                                    string errorOperatorText)
        {
            if (target.Value == null)
            {
                // if target is null, just use 0
                target = new DynamicMetaObject(ExpressionCache.Constant(0), target.PSGetTypeRestriction(), 0);
            }

            if (target.LimitType.IsNumericOrPrimitive())
            {
                var numericArg = GetArgAsNumericOrPrimitive(arg, target.LimitType);
                if (numericArg != null)
                {
                    return BinaryNumericOp(numericOpMethodName, target, numericArg);
                }

                if (arg.LimitType == typeof(string))
                {
                    return BinaryNumericStringOp(target, arg);
                }
            }

            if (target.LimitType == typeof(string))
            {
                // Left is a string.  We convert it to a number and try binding again.
                return BinaryNumericStringOp(target, arg);
            }

            return CallImplicitOp(implicitOpMethodName, target, arg, errorOperatorText, errorSuggestion);
        }

        private DynamicMetaObject Shift(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion, string userOp, Func<Expression, Expression, Expression> exprGenerator)
        {
            if (target.Value == null)
            {
                return new DynamicMetaObject(ExpressionCache.Constant(0).Convert(typeof(object)), target.PSGetTypeRestriction());
            }

            if (target.LimitType == typeof(string) || arg.LimitType == typeof(string))
            {
                return BinaryNumericStringOp(target, arg);
            }

            var typeCode = LanguagePrimitives.GetTypeCode(target.LimitType);
            if (!target.LimitType.IsNumeric())
            {
                return CallImplicitOp(userOp, target, arg, GetOperatorText(), errorSuggestion);
            }

            bool debase;
            var resultType = typeof(int);

            // ConstrainedLanguage note - calls to this conversion only target numeric types.
            var conversion = LanguagePrimitives.FigureConversion(arg.Value, resultType, out debase);
            if (conversion.Rank == ConversionRank.None)
            {
                return PSConvertBinder.ThrowNoConversion(arg, typeof(int), this, _version);
            }

            var numericArg = PSConvertBinder.InvokeConverter(conversion, arg.Expression, resultType, debase, ExpressionCache.InvariantCulture);

            if (typeCode == TypeCode.Decimal || typeCode == TypeCode.Double || typeCode == TypeCode.Single)
            {
                var opType = (typeCode == TypeCode.Decimal) ? typeof(DecimalOps) : typeof(DoubleOps);
                var castType = (typeCode == TypeCode.Decimal) ? typeof(decimal) : typeof(double);
                var methodName = userOp.Substring(3);  // Drop the 'op_' prefix to figure out our internal method name.
                return new DynamicMetaObject(
                    Expression.Call(opType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static), target.Expression.Cast(castType), numericArg),
                    target.CombineRestrictions(arg));
            }

            var targetExpr = target.Expression.Cast(target.LimitType);
            numericArg = Expression.And(numericArg, Expression.Constant(typeCode < TypeCode.Int64 ? 0x1f : 0x3f, typeof(int)));

            return new DynamicMetaObject(
                exprGenerator(targetExpr, numericArg).Cast(typeof(object)),
                target.CombineRestrictions(arg));
        }

        private DynamicMetaObject LeftShift(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            return Shift(target, arg, errorSuggestion, "op_LeftShift", Expression.LeftShift);
        }

        private DynamicMetaObject RightShift(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            return Shift(target, arg, errorSuggestion, "op_RightShift", Expression.RightShift);
        }

        private DynamicMetaObject BinaryBitwiseXor(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            return BinaryBitwiseOp(target, arg, errorSuggestion, Expression.ExclusiveOr, "op_ExclusiveOr", "-bxor", "BXor");
        }

        private DynamicMetaObject BinaryBitwiseOr(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            return BinaryBitwiseOp(target, arg, errorSuggestion, Expression.Or, "op_BitwiseOr", "-bor", "BOr");
        }

        private DynamicMetaObject BinaryBitwiseAnd(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            return BinaryBitwiseOp(target, arg, errorSuggestion, Expression.And, "op_BitwiseAnd", "-band", "BAnd");
        }

        private DynamicMetaObject BinaryBitwiseOp(DynamicMetaObject target,
                                                  DynamicMetaObject arg,
                                                  DynamicMetaObject errorSuggestion,
                                                  Func<Expression, Expression, Expression> exprGenerator,
                                                  string implicitMethodName,
                                                  string errorOperatorName,
                                                  string methodName)
        {
            if (target.Value == null && arg.Value == null)
            {
                return new DynamicMetaObject(ExpressionCache.Constant(0).Cast(typeof(object)), target.CombineRestrictions(arg));
            }

            var targetUnderlyingType = (target.LimitType.IsEnum) ? Enum.GetUnderlyingType(target.LimitType) : target.LimitType;
            var argUnderlyingType = (arg.LimitType.IsEnum) ? Enum.GetUnderlyingType(arg.LimitType) : arg.LimitType;

            if (targetUnderlyingType.IsNumericOrPrimitive() || argUnderlyingType.IsNumericOrPrimitive())
            {
                TypeCode leftTypeCode = LanguagePrimitives.GetTypeCode(targetUnderlyingType);
                TypeCode rightTypeCode = LanguagePrimitives.GetTypeCode(argUnderlyingType);

                Type opType;
                Type opImplType;
                Type toType;
                TypeCode opTypeCode = (int)leftTypeCode >= (int)rightTypeCode ? leftTypeCode : rightTypeCode;
                DynamicMetaObject numericTarget;
                DynamicMetaObject numericArg;
                if (!targetUnderlyingType.IsNumericOrPrimitive())
                {
                    opType = GetBitwiseOpType(rightTypeCode);
                    numericTarget = GetArgAsNumericOrPrimitive(target, opType);
                    numericArg = arg;
                }
                else if (!argUnderlyingType.IsNumericOrPrimitive())
                {
                    opType = GetBitwiseOpType(leftTypeCode);
                    numericTarget = target;
                    numericArg = GetArgAsNumericOrPrimitive(arg, opType);
                }
                else
                {
                    numericTarget = target;
                    numericArg = arg;
                }

                if (opTypeCode == TypeCode.Decimal)
                {
                    opImplType = typeof(DecimalOps);
                    toType = typeof(decimal);
                    return new DynamicMetaObject(
                        Expression.Call(opImplType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static),
                                numericTarget.Expression.Cast(numericTarget.LimitType).Convert(toType),
                                numericArg.Expression.Cast(numericArg.LimitType).Convert(toType)),
                                numericTarget.CombineRestrictions(numericArg));
                }

                if (opTypeCode == TypeCode.Double || opTypeCode == TypeCode.Single)
                {
                    opImplType = typeof(DoubleOps);
                    toType = typeof(double);
                    return new DynamicMetaObject(
                        Expression.Call(opImplType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static),
                                numericTarget.Expression.Cast(numericTarget.LimitType).Convert(toType),
                                numericArg.Expression.Cast(numericArg.LimitType).Convert(toType)),
                                numericTarget.CombineRestrictions(numericArg));
                }

                // Figure out the smallest type necessary so we don't lose information.
                // For uint, V2 promoted to long, but it's more correct to use uint.
                // For ulong, V2 incorrectly used long, this is fixed here.
                // For float, double, and decimal operands, we used to use long because V2 did.
                // Because we use unsigned for -bnot, to be consistent, we promote to unsigned here too (-band,-bor,-xor)
                opType = GetBitwiseOpType((int)leftTypeCode >= (int)rightTypeCode ? leftTypeCode : rightTypeCode);

                if (numericTarget != null && numericArg != null)
                {
                    var expr = exprGenerator(numericTarget.Expression.Cast(numericTarget.LimitType).Cast(opType),
                                             numericArg.Expression.Cast(numericArg.LimitType).Cast(opType));

                    if (target.LimitType.IsEnum)
                    {
                        expr = expr.Cast(target.LimitType);
                    }

                    expr = expr.Cast(typeof(object));
                    return new DynamicMetaObject(expr, numericTarget.CombineRestrictions(numericArg));
                }
            }

            if (target.LimitType == typeof(string) || arg.LimitType == typeof(string))
            {
                return BinaryNumericStringOp(target, arg);
            }

            return CallImplicitOp(implicitMethodName, target, arg, errorOperatorName, errorSuggestion);
        }

        #endregion "Arithmetic" operations

        #region Comparison operations

        private DynamicMetaObject CompareEQ(DynamicMetaObject target,
                                            DynamicMetaObject arg,
                                            DynamicMetaObject errorSuggestion)
        {
            if (target.Value == null)
            {
                return new DynamicMetaObject(
                    arg.Value == null ? ExpressionCache.BoxedTrue : ExpressionCache.BoxedFalse,
                    target.CombineRestrictions(arg));
            }

            var enumerable = PSEnumerableBinder.IsEnumerable(target);
            if (enumerable == null && arg.Value == null)
            {
                return new DynamicMetaObject(
                    ExpressionCache.BoxedFalse,
                    target.CombineRestrictions(arg));
            }

            return BinaryComparisonCommon(enumerable, target, arg)
                ?? BinaryEqualityComparison(target, arg);
        }

        private DynamicMetaObject CompareNE(DynamicMetaObject target,
                                            DynamicMetaObject arg,
                                            DynamicMetaObject errorSuggestion)
        {
            if (target.Value == null)
            {
                return new DynamicMetaObject(
                    arg.Value == null ? ExpressionCache.BoxedFalse : ExpressionCache.BoxedTrue,
                    target.CombineRestrictions(arg));
            }

            var enumerable = PSEnumerableBinder.IsEnumerable(target);
            if (enumerable == null && arg.Value == null)
            {
                return new DynamicMetaObject(ExpressionCache.BoxedTrue,
                    target.CombineRestrictions(arg));
            }

            return BinaryComparisonCommon(enumerable, target, arg)
                ?? BinaryEqualityComparison(target, arg);
        }

        private DynamicMetaObject BinaryEqualityComparison(DynamicMetaObject target, DynamicMetaObject arg)
        {
            var toResult = Operation == ExpressionType.NotEqual ? (Func<Expression, Expression>)Expression.Not : e => e;
            if (target.LimitType == typeof(string))
            {
                var targetExpr = target.Expression.Cast(typeof(string));

                // Doing a string comparison no matter what.
                var argExpr = arg.LimitType != typeof(string)
                                  ? DynamicExpression.Dynamic(PSToStringBinder.Get(), typeof(string),
                                                              arg.Expression, ExpressionCache.GetExecutionContextFromTLS)
                                  : arg.Expression.Cast(typeof(string));

                return new DynamicMetaObject(
                    toResult(Compiler.CallStringEquals(targetExpr, argExpr, _ignoreCase)).Cast(typeof(object)),
                    target.CombineRestrictions(arg));
            }

            if (target.LimitType == typeof(char) && _ignoreCase)
            {
                if (arg.LimitType == typeof(char))
                {
                    return new DynamicMetaObject(
                        Expression.Call(
                            Operation == ExpressionType.Equal ? CachedReflectionInfo.CharOps_CompareIeq : CachedReflectionInfo.CharOps_CompareIne,
                            target.Expression.Cast(typeof(char)),
                            arg.Expression.Cast(typeof(char))),
                        target.PSGetTypeRestriction().Merge(arg.PSGetTypeRestriction()));
                }

                if (arg.LimitType == typeof(string))
                {
                    return new DynamicMetaObject(
                        Expression.Call(
                            Operation == ExpressionType.Equal ? CachedReflectionInfo.CharOps_CompareStringIeq : CachedReflectionInfo.CharOps_CompareStringIne,
                            target.Expression.Cast(typeof(char)),
                            arg.Expression.Cast(typeof(string))),
                        target.PSGetTypeRestriction().Merge(arg.PSGetTypeRestriction()));
                }
            }

            Expression objectEqualsCall = Expression.Call(target.Expression.Cast(typeof(object)),
                                                          CachedReflectionInfo.Object_Equals,
                                                          arg.Expression.Cast(typeof(object)));
            bool debase;
            var targetType = target.LimitType;

            // ConstrainedLanguage note - calls to this conversion are protected by the binding rules below.
            var conversion = LanguagePrimitives.FigureConversion(arg.Value, targetType, out debase);
            if (conversion.Rank == ConversionRank.Identity || conversion.Rank == ConversionRank.Assignable
                || (conversion.Rank == ConversionRank.NullToRef && targetType != typeof(PSReference)))
            {
                // In these cases, no actual conversion is happening, and conversion.Converter will just return
                // the value to be converted. So there is no need to convert the value and compare again.
                return new DynamicMetaObject(toResult(objectEqualsCall).Cast(typeof(object)), target.CombineRestrictions(arg));
            }

            BindingRestrictions bindingRestrictions = target.CombineRestrictions(arg);
            bindingRestrictions = bindingRestrictions.Merge(BinderUtils.GetOptionalVersionAndLanguageCheckForType(this, targetType, _version));

            // If there is no conversion, then just rely on 'objectEqualsCall' which most likely will return false. If we attempted the
            // conversion, we'd need extra code to catch an exception we know will happen just to return false.
            if (conversion.Rank == ConversionRank.None)
            {
                return new DynamicMetaObject(toResult(objectEqualsCall).Cast(typeof(object)), bindingRestrictions);
            }

            // A conversion exists.  Generate:
            //    tmp = target.Equals(arg)
            //    try {
            //        if (!tmp) { tmp = target.Equals(Convert(arg, target.GetType())) }
            //    } catch (InvalidCastException) { tmp = false }
            //    return (operator is -eq/-ceq/-ieq) ? tmp : !tmp
            var resultTmp = Expression.Parameter(typeof(bool));

            Expression secondEqualsCall =
                Expression.Call(target.Expression.Cast(typeof(object)),
                                CachedReflectionInfo.Object_Equals,
                                PSConvertBinder.InvokeConverter(conversion, arg.Expression, targetType, debase, ExpressionCache.InvariantCulture).Cast(typeof(object)));
            var expr = Expression.Block(
                new ParameterExpression[] { resultTmp },
                Expression.Assign(resultTmp, objectEqualsCall),
                Expression.IfThen(Expression.Not(resultTmp),
                                  Expression.TryCatch(Expression.Assign(resultTmp, secondEqualsCall),
                                  Expression.Catch(typeof(InvalidCastException),
                                                   Expression.Assign(resultTmp, ExpressionCache.Constant(false))))),
                toResult(resultTmp));
            return new DynamicMetaObject(expr.Cast(typeof(object)), bindingRestrictions);
        }

        private static Expression CompareWithZero(DynamicMetaObject target, Func<Expression, Expression, Expression> comparer)
        {
            return comparer(target.Expression.Cast(target.LimitType), ExpressionCache.Constant(0).Cast(target.LimitType)).Cast(typeof(object));
        }

        private DynamicMetaObject CompareLT(DynamicMetaObject target,
                                            DynamicMetaObject arg,
                                            DynamicMetaObject errorSuggestion)
        {
            var enumerable = PSEnumerableBinder.IsEnumerable(target);
            if (enumerable == null && (target.Value == null || arg.Value == null))
            {
                Expression result =
                      target.LimitType.IsNumeric() ? CompareWithZero(target, Expression.LessThan)
                    : arg.LimitType.IsNumeric() ? CompareWithZero(arg, Expression.GreaterThanOrEqual)
                    : arg.Value != null ? ExpressionCache.BoxedTrue
                    : ExpressionCache.BoxedFalse;

                return new DynamicMetaObject(result, target.CombineRestrictions(arg));
            }

            return BinaryComparisonCommon(enumerable, target, arg)
                ?? BinaryComparison(target, arg, static e => Expression.LessThan(e, ExpressionCache.Constant(0)));
        }

        private DynamicMetaObject CompareLE(DynamicMetaObject target,
                                            DynamicMetaObject arg,
                                            DynamicMetaObject errorSuggestion)
        {
            var enumerable = PSEnumerableBinder.IsEnumerable(target);
            if (enumerable == null && (target.Value == null || arg.Value == null))
            {
                Expression result =
                      target.LimitType.IsNumeric() ? CompareWithZero(target, Expression.LessThan)
                    : arg.LimitType.IsNumeric() ? CompareWithZero(arg, Expression.GreaterThanOrEqual)
                    : target.Value != null ? ExpressionCache.BoxedFalse
                    : ExpressionCache.BoxedTrue;

                return new DynamicMetaObject(result, target.CombineRestrictions(arg));
            }

            return BinaryComparisonCommon(enumerable, target, arg)
                ?? BinaryComparison(target, arg, static e => Expression.LessThanOrEqual(e, ExpressionCache.Constant(0)));
        }

        private DynamicMetaObject CompareGT(DynamicMetaObject target,
                                            DynamicMetaObject arg,
                                            DynamicMetaObject errorSuggestion)
        {
            // Handle a null operand as a special case here unless the target is enumerable or if one of the operands is numeric,
            // in which case null is converted to 0 and regular numeric comparison is done.
            var enumerable = PSEnumerableBinder.IsEnumerable(target);
            if (enumerable == null && (target.Value == null || arg.Value == null))
            {
                Expression result =
                      target.LimitType.IsNumeric() ? CompareWithZero(target, Expression.GreaterThanOrEqual)
                    : arg.LimitType.IsNumeric() ? CompareWithZero(arg, Expression.LessThan)
                    : target.Value != null ? ExpressionCache.BoxedTrue
                    : ExpressionCache.BoxedFalse;

                return new DynamicMetaObject(result, target.CombineRestrictions(arg));
            }

            return BinaryComparisonCommon(enumerable, target, arg)
                ?? BinaryComparison(target, arg, static e => Expression.GreaterThan(e, ExpressionCache.Constant(0)));
        }

        private DynamicMetaObject CompareGE(DynamicMetaObject target,
                                            DynamicMetaObject arg,
                                            DynamicMetaObject errorSuggestion)
        {
            // Handle a null operand as a special case here unless the target is enumerable or if one of the operands is numeric,
            // in which case null is converted to 0 and regular numeric comparison is done.
            var enumerable = PSEnumerableBinder.IsEnumerable(target);
            if (enumerable == null && (target.Value == null || arg.Value == null))
            {
                Expression result =
                      target.LimitType.IsNumeric() ? CompareWithZero(target, Expression.GreaterThanOrEqual)
                    : arg.LimitType.IsNumeric() ? CompareWithZero(arg, Expression.LessThan)
                    : arg.Value != null ? ExpressionCache.BoxedFalse
                    : ExpressionCache.BoxedTrue;

                return new DynamicMetaObject(result, target.CombineRestrictions(arg));
            }

            return BinaryComparisonCommon(enumerable, target, arg)
                ?? BinaryComparison(target, arg, static e => Expression.GreaterThanOrEqual(e, ExpressionCache.Constant(0)));
        }

        private DynamicMetaObject BinaryComparison(DynamicMetaObject target, DynamicMetaObject arg, Func<Expression, Expression> toResult)
        {
            if (target.LimitType == typeof(string))
            {
                var targetExpr = target.Expression.Cast(typeof(string));

                // Doing a string comparison no matter what.
                var argExpr = arg.LimitType != typeof(string)
                                  ? DynamicExpression.Dynamic(PSToStringBinder.Get(), typeof(string),
                                                              arg.Expression, ExpressionCache.GetExecutionContextFromTLS)
                                  : arg.Expression.Cast(typeof(string));

                var expr = Expression.Call(CachedReflectionInfo.StringOps_Compare, targetExpr, argExpr, ExpressionCache.InvariantCulture,
                                           _ignoreCase
                                           ? ExpressionCache.CompareOptionsIgnoreCase
                                           : ExpressionCache.CompareOptionsNone);

                return new DynamicMetaObject(
                    toResult(expr).Cast(typeof(object)),
                    target.CombineRestrictions(arg));
            }

            bool debase;
            var targetType = target.LimitType;

            // ConstrainedLanguage note - calls to this conversion are protected by the binding rules below.
            var conversion = LanguagePrimitives.FigureConversion(arg.Value, targetType, out debase);

            BindingRestrictions bindingRestrictions = target.CombineRestrictions(arg);
            bindingRestrictions = bindingRestrictions.Merge(BinderUtils.GetOptionalVersionAndLanguageCheckForType(this, targetType, _version));

            Expression argConverted;
            if (conversion.Rank == ConversionRank.Identity || conversion.Rank == ConversionRank.Assignable)
            {
                argConverted = arg.Expression;
            }
            else if (conversion.Rank == ConversionRank.None)
            {
                // If there is no conversion, then don't bother to invoke the converter. We raise the exception directly.
                var valueToConvert = debase
                    ? Expression.Call(CachedReflectionInfo.PSObject_Base, arg.Expression)
                    : arg.Expression.Cast(typeof(object));
                var errorMsgTuple = Expression.Call(
                    CachedReflectionInfo.LanguagePrimitives_GetInvalidCastMessages,
                    valueToConvert, Expression.Constant(targetType, typeof(Type)));

                argConverted = Compiler.ThrowRuntimeError(
                    "ComparisonFailure", ExtendedTypeSystem.ComparisonFailure, targetType,
                    DynamicExpression.Dynamic(PSToStringBinder.Get(), typeof(string), target.Expression, ExpressionCache.GetExecutionContextFromTLS),
                    DynamicExpression.Dynamic(PSToStringBinder.Get(), typeof(string), arg.Expression, ExpressionCache.GetExecutionContextFromTLS),
                    Expression.Property(errorMsgTuple, "Item2"));
            }
            else
            {
                // Invoke the converter. We can raise the exception if the conversion throws InvalidCastException.
                var innerException = Expression.Parameter(typeof(InvalidCastException));
                argConverted =
                    Expression.TryCatch(
                        PSConvertBinder.InvokeConverter(conversion, arg.Expression, targetType, debase, ExpressionCache.InvariantCulture),
                        Expression.Catch(
                            innerException,
                            Compiler.ThrowRuntimeErrorWithInnerException("ComparisonFailure",
                                Expression.Constant(ExtendedTypeSystem.ComparisonFailure), innerException, targetType,
                                DynamicExpression.Dynamic(PSToStringBinder.Get(), typeof(string), target.Expression, ExpressionCache.GetExecutionContextFromTLS),
                                DynamicExpression.Dynamic(PSToStringBinder.Get(), typeof(string), arg.Expression, ExpressionCache.GetExecutionContextFromTLS),
                                Expression.Property(innerException, CachedReflectionInfo.Exception_Message))));
            }

            // Prefer IComparable<T> over IComparable if possible
            if (target.LimitType == arg.LimitType)
            {
                foreach (var i in target.Value.GetType().GetInterfaces())
                {
                    if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IComparable<>))
                    {
                        return new DynamicMetaObject(
                            toResult(Expression.Call(Expression.Convert(target.Expression, i),
                                                     i.GetMethod("CompareTo"),
                                                     argConverted.Cast(arg.LimitType))).Cast(typeof(object)),
                            bindingRestrictions);
                    }
                }
            }

            if (target.Value is IComparable)
            {
                return new DynamicMetaObject(
                    toResult(Expression.Call(target.Expression.Cast(typeof(IComparable)),
                                             CachedReflectionInfo.IComparable_CompareTo,
                                             argConverted.Cast(typeof(object)))).Cast(typeof(object)),
                    bindingRestrictions);
            }

            var throwExpr = Compiler.ThrowRuntimeError("NotIcomparable", ExtendedTypeSystem.NotIcomparable, this.ReturnType, target.Expression);

            // Try object.Equals.  If the objects compare equal, the result is known (true for -ge or -le, false for -gt or -lt), otherwise
            // throw because the objects can't be compared in any meaningful way.
            return new DynamicMetaObject(
                Expression.Condition(
                    Expression.Call(target.Expression.Cast(typeof(object)),
                                    CachedReflectionInfo.Object_Equals,
                                    arg.Expression.Cast(typeof(object))),
                    (Operation == ExpressionType.GreaterThanOrEqual || Operation == ExpressionType.LessThanOrEqual)
                        ? ExpressionCache.BoxedTrue : ExpressionCache.BoxedFalse,
                    throwExpr),
                bindingRestrictions);
        }

        private DynamicMetaObject BinaryComparisonCommon(DynamicMetaObject targetAsEnumerator, DynamicMetaObject target, DynamicMetaObject arg)
        {
            if (targetAsEnumerator != null && !_scalarCompare)
            {
                // If the target is enumerable, the we generate an object[] result with elements matching.
                // The iteration will be done in a pre-compiled method, but the comparison is done with
                // a dynamically generated lambda that uses a binder
                return new DynamicMetaObject(
                    Expression.Call(CachedReflectionInfo.EnumerableOps_Compare,
                                    targetAsEnumerator.Expression,
                                    arg.Expression.Cast(typeof(object)),
                                    Expression.Constant(GetScalarCompareDelegate())),
                    targetAsEnumerator.Restrictions.Merge(arg.PSGetTypeRestriction()));
            }

            if (target.LimitType.IsNumeric())
            {
                var numericArg = GetArgAsNumericOrPrimitive(arg, target.LimitType);
                if (numericArg != null)
                {
                    string numericMethod = null;
                    switch (Operation)
                    {
                        case ExpressionType.Equal: numericMethod = "CompareEq"; break;
                        case ExpressionType.NotEqual: numericMethod = "CompareNe"; break;
                        case ExpressionType.GreaterThan: numericMethod = "CompareGt"; break;
                        case ExpressionType.GreaterThanOrEqual: numericMethod = "CompareGe"; break;
                        case ExpressionType.LessThan: numericMethod = "CompareLt"; break;
                        case ExpressionType.LessThanOrEqual: numericMethod = "CompareLe"; break;
                    }

                    return BinaryNumericOp(numericMethod, target, numericArg);
                }

                if (arg.LimitType == typeof(string))
                {
                    return BinaryNumericStringOp(target, arg);
                }
            }

            return null;
        }

        #endregion Comparison operations
    }

    /// <summary>
    /// The binder for unary operators like !, -, or +.
    /// </summary>
    internal sealed class PSUnaryOperationBinder : UnaryOperationBinder
    {
        private static PSUnaryOperationBinder s_notBinder;
        private static PSUnaryOperationBinder s_bnotBinder;
        private static PSUnaryOperationBinder s_unaryMinus;
        private static PSUnaryOperationBinder s_unaryPlusBinder;
        private static PSUnaryOperationBinder s_incrementBinder;
        private static PSUnaryOperationBinder s_decrementBinder;

        internal static PSUnaryOperationBinder Get(ExpressionType operation)
        {
            switch (operation)
            {
                case ExpressionType.Not:
                    if (s_notBinder == null)
                        Interlocked.CompareExchange(ref s_notBinder, new PSUnaryOperationBinder(operation), null);
                    return s_notBinder;
                case ExpressionType.OnesComplement:
                    if (s_bnotBinder == null)
                        Interlocked.CompareExchange(ref s_bnotBinder, new PSUnaryOperationBinder(operation), null);
                    return s_bnotBinder;
                case ExpressionType.UnaryPlus:
                    if (s_unaryPlusBinder == null)
                        Interlocked.CompareExchange(ref s_unaryPlusBinder, new PSUnaryOperationBinder(operation), null);
                    return s_unaryPlusBinder;
                case ExpressionType.Negate:
                    if (s_unaryMinus == null)
                        Interlocked.CompareExchange(ref s_unaryMinus, new PSUnaryOperationBinder(operation), null);
                    return s_unaryMinus;
                case ExpressionType.Increment:
                    if (s_incrementBinder == null)
                        Interlocked.CompareExchange(ref s_incrementBinder, new PSUnaryOperationBinder(operation), null);
                    return s_incrementBinder;
                case ExpressionType.Decrement:
                    if (s_decrementBinder == null)
                        Interlocked.CompareExchange(ref s_decrementBinder, new PSUnaryOperationBinder(operation), null);
                    return s_decrementBinder;
            }

            throw new NotImplementedException("Unimplemented unary operation");
        }

        private PSUnaryOperationBinder(ExpressionType operation) : base(operation)
        {
        }

        public override DynamicMetaObject FallbackUnaryOperation(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue)
            {
                return Defer(target);
            }

            if (target.Value is PSObject && (PSObject.Base(target.Value) != target.Value))
            {
                return this.DeferForPSObject(target);
            }

            switch (Operation)
            {
                case ExpressionType.Not:
                    return Not(target, errorSuggestion).WriteToDebugLog(this);
                case ExpressionType.OnesComplement:
                    return BNot(target, errorSuggestion).WriteToDebugLog(this);
                case ExpressionType.UnaryPlus:
                    return UnaryPlus(target, errorSuggestion).WriteToDebugLog(this);
                case ExpressionType.Negate:
                    return UnaryMinus(target, errorSuggestion).WriteToDebugLog(this);
                case ExpressionType.Increment:
                    return IncrDecr(target, 1, errorSuggestion).WriteToDebugLog(this);
                case ExpressionType.Decrement:
                    return IncrDecr(target, -1, errorSuggestion).WriteToDebugLog(this);
            }

            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return string.Create(CultureInfo.InvariantCulture, $"PSUnaryOperationBinder {this.Operation}");
        }

        internal DynamicMetaObject Not(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue)
            {
                return Defer(target);
            }

            // TODO: check op_LogicalNot

            // This could generate a dynamic site in the expr, which means we might have the same type test twice.
            // We should do better, but this is the simplest implementation, we can add specific cases to handle more common
            // cases if necessary.
            var targetExpr = target.CastOrConvert(typeof(bool));

            return new DynamicMetaObject(
                Expression.Not(targetExpr).Cast(typeof(object)),
                target.PSGetTypeRestriction());
        }

        internal DynamicMetaObject BNot(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue)
            {
                return Defer(target);
            }

            if (target.Value is PSObject && (PSObject.Base(target.Value) != target.Value))
            {
                return this.DeferForPSObject(target);
            }

            if (target.Value == null)
            {
                return new DynamicMetaObject(ExpressionCache.Constant(-1).Cast(typeof(object)), target.PSGetTypeRestriction());
            }

            // If the type implements the operator, prefer that.
            var method = target.LimitType.GetMethod("op_OnesComplement", BindingFlags.Static | BindingFlags.Public, null,
                                                    new Type[] { target.LimitType }, null);
            if (method != null)
            {
                return new DynamicMetaObject(
                    Expression.OnesComplement(target.Expression.Cast(target.LimitType), method).Cast(typeof(object)),
                    target.PSGetTypeRestriction());
            }

            // Otherwise, do a conversion, as necessary, or throw (and say we can't convert to int, a better message
            // would be nice, but this error is good enough.)
            if (target.LimitType == typeof(string))
            {
                // If we have a string, we defer resolving the operation type until after we know the type of the operand,
                // so generate a dynamic site.
                return new DynamicMetaObject(
                    DynamicExpression.Dynamic(this, this.ReturnType,
                                              PSBinaryOperationBinder.ConvertStringToNumber(target.Expression, typeof(int))),
                    target.PSGetTypeRestriction());
            }

            Expression targetExpr = null;
            if (!target.LimitType.IsNumeric())
            {
                var resultType = typeof(int);
                bool debase;

                // ConstrainedLanguage note - calls to this conversion only target numeric types.
                var conversion = LanguagePrimitives.FigureConversion(target.Value, resultType, out debase);
                if (conversion.Rank != ConversionRank.None)
                {
                    targetExpr = PSConvertBinder.InvokeConverter(conversion, target.Expression, resultType, debase,
                                                                 ExpressionCache.InvariantCulture);
                }
                else
                {
                    resultType = typeof(long);

                    // ConstrainedLanguage note - calls to this conversion only target numeric types.
                    conversion = LanguagePrimitives.FigureConversion(target.Value, resultType, out debase);
                    if (conversion.Rank != ConversionRank.None)
                    {
                        targetExpr = PSConvertBinder.InvokeConverter(conversion, target.Expression, resultType, debase,
                                                                     ExpressionCache.InvariantCulture);
                    }
                }
            }
            else
            {
                var typeCode = LanguagePrimitives.GetTypeCode(target.LimitType);
                if (typeCode < TypeCode.Int32)
                {
                    targetExpr = target.LimitType.IsEnum
                        ? target.Expression.Cast(Enum.GetUnderlyingType(target.LimitType))
                        : target.Expression.Cast(target.LimitType);
                    targetExpr = targetExpr.Cast(typeof(int));
                }
                else if (typeCode <= TypeCode.UInt64)
                {
                    var targetConvertType = target.LimitType;
                    if (targetConvertType.IsEnum)
                    {
                        targetConvertType = Enum.GetUnderlyingType(targetConvertType);
                    }

                    targetExpr = target.Expression.Cast(targetConvertType);
                }
                else
                {
                    var opType = (typeCode == TypeCode.Decimal) ? typeof(DecimalOps) : typeof(DoubleOps);
                    var castType = (typeCode == TypeCode.Decimal) ? typeof(decimal) : typeof(double);

                    return new DynamicMetaObject(
                        Expression.Call(opType.GetMethod("BNot", BindingFlags.Static | BindingFlags.NonPublic), target.Expression.Convert(castType)),
                        target.PSGetTypeRestriction());
                }
            }

            if (targetExpr != null)
            {
                Expression result = Expression.OnesComplement(targetExpr);
                if (target.LimitType.IsEnum)
                {
                    result = result.Cast(target.LimitType);
                }

                return new DynamicMetaObject(result.Cast(typeof(object)), target.PSGetTypeRestriction());
            }

            return errorSuggestion ?? PSConvertBinder.ThrowNoConversion(target, typeof(int), this, -1);
        }

        private DynamicMetaObject UnaryPlus(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue)
            {
                return Defer(target);
            }

            if (target.Value is PSObject && (PSObject.Base(target.Value) != target.Value))
            {
                return this.DeferForPSObject(target);
            }

            if (target.LimitType.IsNumeric())
            {
                var expr = target.Expression.Cast(target.LimitType);
                if (target.LimitType == typeof(byte) || target.LimitType == typeof(sbyte))
                {
                    // promote to int, unary plus doesn't support byte directly.
                    expr = expr.Cast(typeof(int));
                }

                return new DynamicMetaObject(
                    Expression.UnaryPlus(expr).Cast(typeof(object)),
                    target.PSGetTypeRestriction());
            }

            // Use a nested dynamic site that adds 0.  This won't change the sign, but it will attempt conversions.  This is slower than it needs
            // to be, but should be hit rarely.
            return new DynamicMetaObject(
                DynamicExpression.Dynamic(PSBinaryOperationBinder.Get(ExpressionType.Add), typeof(object), ExpressionCache.Constant(0), target.Expression),
                                          target.PSGetTypeRestriction());
        }

        private DynamicMetaObject UnaryMinus(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue)
            {
                return Defer(target);
            }

            if (target.Value is PSObject && (PSObject.Base(target.Value) != target.Value))
            {
                return this.DeferForPSObject(target);
            }

            if (target.LimitType.IsNumeric())
            {
                var expr = target.Expression.Cast(target.LimitType);
                if (target.LimitType == typeof(byte) || target.LimitType == typeof(sbyte))
                {
                    // promote to int, unary plus doesn't support byte directly.
                    expr = expr.Cast(typeof(int));
                }

                return new DynamicMetaObject(
                    Expression.Negate(expr).Cast(typeof(object)),
                    target.PSGetTypeRestriction());
            }

            // Use a nested dynamic site that subtracts from 0.  This won't change the sign, but it will attempt conversions.  This is slower than it needs
            // to be, but should be hit rarely.
            return new DynamicMetaObject(
                DynamicExpression.Dynamic(PSBinaryOperationBinder.Get(ExpressionType.Subtract), typeof(object), ExpressionCache.Constant(0), target.Expression),
                                          target.PSGetTypeRestriction());
        }

        private DynamicMetaObject IncrDecr(DynamicMetaObject target, int valueToAdd, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue)
            {
                return Defer(target);
            }

            if (target.Value is PSObject && (PSObject.Base(target.Value) != target.Value))
            {
                return this.DeferForPSObject(target);
            }

            if (target.Value == null)
            {
                return new DynamicMetaObject(ExpressionCache.Constant(valueToAdd).Cast(typeof(object)), target.PSGetTypeRestriction());
            }

            if (target.LimitType.IsNumeric())
            {
                var arg = new DynamicMetaObject(ExpressionCache.Constant(valueToAdd), BindingRestrictions.Empty, valueToAdd);
                var result = PSBinaryOperationBinder.Get(ExpressionType.Add).FallbackBinaryOperation(target, arg, errorSuggestion);
                return new DynamicMetaObject(
                    result.Expression,
                    target.PSGetTypeRestriction());
            }

            return errorSuggestion ?? target.ThrowRuntimeError(
                Array.Empty<DynamicMetaObject>(),
                BindingRestrictions.Empty,
                "OperatorRequiresNumber",
                ParserStrings.OperatorRequiresNumber,
                Expression.Constant((Operation == ExpressionType.Increment ? TokenKind.PlusPlus : TokenKind.MinusMinus).Text()),
                Expression.Constant(target.LimitType, typeof(Type)));
        }
    }

    /// <summary>
    /// The binder for converting a value, e.g. [int]"42"
    /// </summary>
    internal sealed class PSConvertBinder : ConvertBinder
    {
        private static readonly Dictionary<Type, PSConvertBinder> s_binderCache = new Dictionary<Type, PSConvertBinder>();
        internal int _version;

        public static PSConvertBinder Get(Type type)
        {
            PSConvertBinder result;

            lock (s_binderCache)
            {
                if (!s_binderCache.TryGetValue(type, out result))
                {
                    result = new PSConvertBinder(type);
                    s_binderCache.Add(type, result);
                }
            }

            return result;
        }

        private PSConvertBinder(Type type)
            : base(type, /*explicit=*/false)
        {
            this._version = 0;
            if (type == typeof(string))
            {
                CacheTarget((Func<CallSite, object, string>)(StringToStringRule));
            }
        }

        public override DynamicMetaObject FallbackConvert(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue)
            {
                return Defer(target).WriteToDebugLog(this);
            }

            if (target.Value == AutomationNull.Value)
            {
                return new DynamicMetaObject(Expression.Default(this.Type), target.PSGetTypeRestriction()).WriteToDebugLog(this);
            }

            bool debase;
            var resultType = this.Type;

            // ConstrainedLanguage note - this is the main conversion mechanism. If the runspace has ever used
            // ConstrainedLanguage, then start baking in the language mode to the binding rules.
            var conversion = LanguagePrimitives.FigureConversion(target.Value, resultType, out debase);

            if (errorSuggestion != null && target.Value is DynamicObject)
            {
                return errorSuggestion.WriteToDebugLog(this);
            }

            BindingRestrictions restrictions = target.PSGetTypeRestriction();
            restrictions = restrictions.Merge(BinderUtils.GetOptionalVersionAndLanguageCheckForType(this, resultType, _version));

            return (new DynamicMetaObject(
                InvokeConverter(conversion, target.Expression, resultType, debase, ExpressionCache.InvariantCulture),
                restrictions)).WriteToDebugLog(this);
        }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "PSConvertBinder [{0}]  ver:{1}",
                Microsoft.PowerShell.ToStringCodeMethods.Type(this.Type, true),
                _version);
        }

        internal static void InvalidateCache()
        {
            // Invalidate binders
            lock (s_binderCache)
            {
                foreach (PSConvertBinder binder in s_binderCache.Values)
                {
                    binder._version += 1;
                }
            }
        }

        internal static DynamicMetaObject ThrowNoConversion(DynamicMetaObject target, Type toType, DynamicMetaObjectBinder binder,
            int currentVersion, params DynamicMetaObject[] args)
        {
            // No conversion, so the result expression raises an error:
            //   throw new PSInvalidCastException("ConvertToFinalInvalidCastException", null,
            //       ExtendedTypeSystem.InvalidCastException,
            //       valueToConvert.ToString(), ObjectToTypeNameString(valueToConvert), resultType.ToString());

            Expression expr = Expression.Call(CachedReflectionInfo.LanguagePrimitives_ThrowInvalidCastException,
                                              target.Expression.Cast(typeof(object)),
                                              Expression.Constant(toType, typeof(Type)));

            if (binder.ReturnType != typeof(void))
            {
                expr = Expression.Block(expr, Expression.Default(binder.ReturnType));
            }

            BindingRestrictions bindingRestrictions = target.CombineRestrictions(args);
            bindingRestrictions = bindingRestrictions.Merge(BinderUtils.GetOptionalVersionAndLanguageCheckForType(binder, toType, currentVersion));

            return new DynamicMetaObject(expr, bindingRestrictions);
        }

        /// <summary>
        /// Convert argument to a ByRef-like type via implicit or explicit conversion.
        /// </summary>
        /// <param name="argument">
        /// The argument to be converted to a ByRef-like type.
        /// </param>
        /// <param name="resultType">
        /// The ByRef-like type to convert to.
        /// </param>
        internal static Expression ConvertToByRefLikeTypeViaCasting(DynamicMetaObject argument, Type resultType)
        {
            var baseObject = PSObject.Base(argument.Value);

            // Source value cannot be null or AutomationNull, and it cannot be a pure PSObject.
            if (baseObject != null && baseObject is not PSObject)
            {
                Type fromType = baseObject.GetType();
                ConversionRank rank = ConversionRank.None;

                LanguagePrimitives.FigureCastConversion(fromType, resultType, ref rank);
                if (rank != ConversionRank.None)
                {
                    var valueToConvert = baseObject == argument.Value
                        ? argument.Expression
                        : Expression.Call(CachedReflectionInfo.PSObject_Base, argument.Expression);

                    return Expression.Convert(valueToConvert.Cast(fromType), resultType);
                }
            }

            return null;
        }

        internal static Expression InvokeConverter(LanguagePrimitives.IConversionData conversion,
                                                   Expression value,
                                                   Type resultType,
                                                   bool debase,
                                                   Expression formatProvider)
        {
            Expression conv;
            if (conversion.Rank == ConversionRank.Identity || conversion.Rank == ConversionRank.Assignable)
            {
                conv = debase ? Expression.Call(CachedReflectionInfo.PSObject_Base, value) : value;
            }
            else
            {
                Expression valueToConvert, valueAsPSObject;
                if (debase)
                {
                    // Caller has verified the value is a PSObject.
                    valueToConvert = Expression.Call(CachedReflectionInfo.PSObject_Base, value);
                    valueAsPSObject = value.Cast(typeof(PSObject));
                }
                else
                {
                    // Caller has verified the value is not a PSObject, or that PSObject.Base should not be called.
                    // If the object is some sort of PSObject, it's most likely a derived to base conversion.
                    valueToConvert = value.Cast(typeof(object));
                    valueAsPSObject = ExpressionCache.NullPSObject;
                }

                conv = Expression.Call(
                    Expression.Constant(conversion.Converter),
                    conversion.Converter.GetType().GetMethod("Invoke"),
                    /*valueToConvert=*/         valueToConvert,
                    /*resultType=*/             Expression.Constant(resultType, typeof(Type)),
                    /*recurse=*/                ExpressionCache.Constant(true),
                    /*originalValueToConvert=*/ valueAsPSObject,
                    /*formatProvider=*/         formatProvider,
                    /*backupTable=*/            ExpressionCache.NullTypeTable);
            }

            // Skip adding the Convert if unnecessary (same type), or impossible (InternalPSCustomObject)
            if (conv.Type == resultType || resultType == typeof(LanguagePrimitives.InternalPSCustomObject))
            {
                return conv;
            }

            if (resultType.IsValueType && Nullable.GetUnderlyingType(resultType) == null)
            {
                return Expression.Unbox(conv, resultType);
            }

            return Expression.Convert(conv, resultType);
        }

        private static string StringToStringRule(CallSite site, object obj)
        {
            var str = obj as string;
            return str ?? ((CallSite<Func<CallSite, object, string>>)site).Update(site, obj);
        }
    }

    /// <summary>
    /// The binder to get the value of an indexable object, e.g. $x[1]
    /// </summary>
    internal sealed class PSGetIndexBinder : GetIndexBinder
    {
        private static readonly Dictionary<Tuple<CallInfo, PSMethodInvocationConstraints, bool>, PSGetIndexBinder> s_binderCache
                = new Dictionary<Tuple<CallInfo, PSMethodInvocationConstraints, bool>, PSGetIndexBinder>();

        private readonly PSMethodInvocationConstraints _constraints;
        private readonly bool _allowSlicing;
        internal int _version;

        public static PSGetIndexBinder Get(int argCount, PSMethodInvocationConstraints constraints, bool allowSlicing = true)
        {
            lock (s_binderCache)
            {
                PSGetIndexBinder binder;
                var tuple = Tuple.Create(new CallInfo(argCount), constraints, allowSlicing);
                if (!s_binderCache.TryGetValue(tuple, out binder))
                {
                    binder = new PSGetIndexBinder(tuple);
                    s_binderCache.Add(tuple, binder);
                }

                return binder;
            }
        }

        private PSGetIndexBinder(Tuple<CallInfo, PSMethodInvocationConstraints, bool> tuple)
            : base(tuple.Item1)
        {
            _constraints = tuple.Item2;
            _allowSlicing = tuple.Item3;
            this._version = 0;
        }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "PSGetIndexBinder indexCount={0}{1}{2} ver:{3}",
                this.CallInfo.ArgumentCount,
                _allowSlicing ? string.Empty : " slicing disallowed",
                _constraints == null ? string.Empty : " constraints: " + _constraints,
                _version);
        }

        internal static void InvalidateCache()
        {
            // Invalidate binders
            lock (s_binderCache)
            {
                foreach (PSGetIndexBinder binder in s_binderCache.Values)
                {
                    binder._version += 1;
                }
            }
        }

        public override DynamicMetaObject FallbackGetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue || indexes.Any(static mo => !mo.HasValue))
            {
                return Defer(indexes.Prepend(target).ToArray()).WriteToDebugLog(this);
            }

            if ((target.Value is PSObject && (PSObject.Base(target.Value) != target.Value)) ||
                indexes.Any(static mo => mo.Value is PSObject && (PSObject.Base(mo.Value) != mo.Value)))
            {
                return this.DeferForPSObject(indexes.Prepend(target).ToArray()).WriteToDebugLog(this);
            }

            // Check if this is a COM Object
            DynamicMetaObject comResult;
            if (ComInterop.ComBinder.TryBindGetIndex(this, target, indexes, out comResult))
            {
                return comResult.UpdateComRestrictionsForPsObject(indexes).WriteToDebugLog(this);
            }

            if (target.Value == null)
            {
                return (errorSuggestion ??
                        target.ThrowRuntimeError(indexes, BindingRestrictions.Empty, "NullArray", ParserStrings.NullArray)).WriteToDebugLog(this);
            }

            // A null index is not allowed unless the index is one of the indices used while slicing, in which case we'll attempt
            // the usual conversions from null to whatever the value being indexed supports.
            // This is oddly inconsistent e.g.:
            //     $a[$null] # error
            //     $a[$null,$null] # no error, result is an empty array
            // The rationale: V1/V2 did it, and when people are slicing, it's better to return some of the results than none.
            if (indexes.Length == 1 && indexes[0].Value == null && _allowSlicing)
            {
                return (errorSuggestion ??
                        target.ThrowRuntimeError(indexes, BindingRestrictions.Empty, "NullArrayIndex", ParserStrings.NullArrayIndex)).WriteToDebugLog(this);
            }

            if (target.LimitType.IsArray)
            {
                return GetIndexArray(target, indexes, errorSuggestion).WriteToDebugLog(this);
            }

            var defaultMember = target.LimitType.GetCustomAttributes<DefaultMemberAttribute>(true).FirstOrDefault();
            PropertyInfo lengthProperty = null;
            foreach (var i in target.LimitType.GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    var result = GetIndexDictionary(target, indexes, i);
                    if (result != null)
                    {
                        return result.WriteToDebugLog(this);
                    }
                }

                // If the type explicitly implements an indexer specified by an interface
                // then the DefaultMemberAttribute will not carry over to the implementation.
                // This check will catch those cases.
                if (defaultMember == null)
                {
                    defaultMember = i.GetCustomAttributes<DefaultMemberAttribute>(inherit: false).FirstOrDefault();
                    if (defaultMember != null)
                    {
                        lengthProperty = i.GetProperty("Count") ?? i.GetProperty("Length");
                    }
                }
            }

            if (defaultMember != null)
            {
                return InvokeIndexer(target, indexes, errorSuggestion, defaultMember.MemberName, lengthProperty).WriteToDebugLog(this);
            }

            return errorSuggestion ?? CannotIndexTarget(target, indexes).WriteToDebugLog(this);
        }

        private DynamicMetaObject CannotIndexTarget(DynamicMetaObject target, DynamicMetaObject[] indexes)
        {
            // We want to have
            //    $x[0]
            // Be equivalent to
            //    $x
            // When $x doesn't have any other way of being indexed.  If the index is anything other than 0, we'll
            // throw an error.
            //
            // The motivation for this magic is largely driven by the desire to avoid breaking scripts written around
            // workflows.  A workflow that wrote a single object to the pipeline had always returned a collection, so
            // scripts needed to index.  This semantic doesn't match the script semantics - a single object written is
            // not wrapped in a collection.  The inconsistency had to be addressed, but this code had to be added to
            // avoid breaking partner scripts.

            // Add version / language checks
            BindingRestrictions bindingRestrictions = target.CombineRestrictions(indexes);

            // This may be thrown due to a type that was disallowed only due to constrained language.
            // Because of this, we also need a version check
            bindingRestrictions = bindingRestrictions.Merge(BinderUtils.GetVersionCheck(this, _version));

            // Also add a language mode check to detect toggling between language modes
            bindingRestrictions = bindingRestrictions.Merge(BinderUtils.GetLanguageModeCheckIfHasEverUsedConstrainedLanguage());

            var call = Expression.Call(CachedReflectionInfo.ArrayOps_GetNonIndexable, target.Expression.Cast(typeof(object)),
                                       Expression.NewArrayInit(typeof(object), indexes.Select(static d => d.Expression.Cast(typeof(object)))));
            return new DynamicMetaObject(call, bindingRestrictions);
        }

        // Index a generic dictionary via TryGetValue.  This routine does not handle slicing,
        // we defer to InvokeIndexer to handle slicing (dictionaries also support general indexing.)
        private DynamicMetaObject GetIndexDictionary(DynamicMetaObject target,
                                                     DynamicMetaObject[] indexes,
                                                     Type idictionary)
        {
            if (indexes.Length > 1)
            {
                // Let InvokeIndexer generate the slicing code, we wouldn't generate anything special here.
                return null;
            }

            var tryGetValue = idictionary.GetMethod("TryGetValue");
            Diagnostics.Assert(tryGetValue != null, "IDictionary<K,V> has TryGetValue");

            var parameters = tryGetValue.GetParameters();
            bool debase;
            var keyType = parameters[0].ParameterType;

            // ConstrainedLanguage note - Calls to this conversion are protected by the binding rules below
            var conversion = LanguagePrimitives.FigureConversion(indexes[0].Value, keyType, out debase);
            if (conversion.Rank == ConversionRank.None)
            {
                // No conversion allows us to call TryGetValue, let InvokeIndexer make the decision (possibly
                // slicing, or possibly just invoke the indexer.
                return null;
            }

            if (indexes[0].LimitType.IsArray && !keyType.IsArray)
            {
                // There was a conversion, but it's far more likely (and backwards compatible) that we want to do slicing
                return null;
            }

            BindingRestrictions bindingRestrictions = target.CombineRestrictions(indexes);
            bindingRestrictions = bindingRestrictions.Merge(BinderUtils.GetOptionalVersionAndLanguageCheckForType(this, keyType, _version));

            var keyExpr = PSConvertBinder.InvokeConverter(conversion, indexes[0].Expression, keyType, debase, ExpressionCache.InvariantCulture);
            var outParam = Expression.Parameter(parameters[1].ParameterType.GetElementType(), "outParam");
            return new DynamicMetaObject(
                Expression.Block(
                    new ParameterExpression[] { outParam },
                    Expression.Condition(
                        Expression.Call(target.Expression.Cast(idictionary), tryGetValue, keyExpr, outParam),
                        outParam.Cast(typeof(object)),
                        GetNullResult())),
                bindingRestrictions);
        }

        internal static bool CanIndexFromEndWithNegativeIndex(
            DynamicMetaObject target,
            MethodInfo indexer,
            ParameterInfo[] getterParams)
        {
            // PowerShell supports negative indexing for types that meet the following criteria:
            //      - Indexer method accepts one parameter that is typed as int
            //      - The int parameter is not a type argument from a constructed generic type
            //        (this is to exclude indexers for types that could use a negative index as
            //        a valid key like System.Linq.ILookup)
            //      - Declares a "Count" or "Length" property
            //      - Does not inherit from IDictionary<> as that is handled earlier in the binder
            // For those types, generate special code to check for negative indices, otherwise just generate
            // the call. Before we test for the above criteria explicitly, we will determine if the
            // target is of a type known to be compatible. This is done to avoid the call to Module.ResolveMethod
            // when possible.

            if (getterParams.Length != 1 || getterParams[0].ParameterType != typeof(int))
            {
                return false;
            }

            Type limitType = target.LimitType;
            if (limitType.IsArray || limitType == typeof(string) || limitType == typeof(StringBuilder))
            {
                return true;
            }

            if (typeof(IList).IsAssignableFrom(limitType))
            {
                return true;
            }

            if (typeof(OrderedDictionary).IsAssignableFrom(limitType))
            {
                return true;
            }

            // target implements IList<T>?
            if (limitType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>)))
            {
                return true;
            }

            // Get the base method definition of the indexer to determine if the int
            // parameter is a generic type parameter. Module.ResolveMethod is used
            // because the indexer could be a method from a constructed generic type.
            MethodBase baseMethod = indexer.Module.ResolveMethod(indexer.MetadataToken);
            return !baseMethod.GetParameters()[0].ParameterType.IsGenericParameter;
        }

        private DynamicMetaObject IndexWithNegativeChecks(
            DynamicMetaObject target,
            DynamicMetaObject index,
            PropertyInfo lengthProperty,
            Func<Expression, Expression, Expression> generateIndexOperation)
        {
            // Generate:
            //    try {
            //       len = obj.Length
            //       if (index < 0)
            //           index = index + len
            //       obj[index]
            //    } catch (Exception e) {
            //        if (StrictMode(3)) { throw }
            //        $null
            //    }

            var targetTmp = Expression.Parameter(target.LimitType, "target");
            var lenTmp = Expression.Parameter(typeof(int), "len");
            var indexTmp = Expression.Parameter(typeof(int), "index");

            Expression block = Expression.Block(
                new ParameterExpression[] { targetTmp, lenTmp, indexTmp },
                // Save the target because we use it multiple times.
                Expression.Assign(targetTmp, target.Expression.Cast(target.LimitType)),
                // Save the length because we use it multiple times.
                Expression.Assign(lenTmp,
                                  Expression.Property(targetTmp, lengthProperty)),
                // Save the index because we use it multiple times
                Expression.Assign(indexTmp, index.Expression),
                // Adjust the index if it's negative
                Expression.IfThen(Expression.LessThan(indexTmp, ExpressionCache.Constant(0)),
                                  Expression.Assign(indexTmp, Expression.Add(indexTmp, lenTmp))),
                // Generate the index operation
                generateIndexOperation(targetTmp, indexTmp));

            return new DynamicMetaObject(
                // Do the indexing within a try/catch so we can return $null if the index is out of bounds,
                // or if the index cast fails, e.g. $a = @(1); $a['abc']
                SafeIndexResult(block),
                target.CombineRestrictions(index));
        }

        private DynamicMetaObject GetIndexArray(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject errorSuggestion)
        {
            var array = (Array)target.Value;

            if (array.Rank > 1)
            {
                return GetIndexMultiDimensionArray(target, indexes, errorSuggestion);
            }

            if (indexes.Length > 1)
            {
                // If the binder allows slicing, we're definitely slicing, otherwise,
                // calling the indexer will fail because there are either too many or too few indices, so
                // throw an error in that case (and not null.)
                return _allowSlicing
                           ? InvokeSlicingIndexer(target, indexes)
                           : (errorSuggestion ?? CannotIndexTarget(target, indexes));
            }

            var slicingResult = CheckForSlicing(target, indexes);
            if (slicingResult != null)
            {
                return slicingResult;
            }

            var indexAsInt = ConvertIndex(indexes[0], typeof(int));
            if (indexAsInt == null)
            {
                // Calling the indexer will fail because we can't convert an index to the correct type.
                return errorSuggestion ?? PSConvertBinder.ThrowNoConversion(target, typeof(int), this, _version, indexes);
            }

            return IndexWithNegativeChecks(
                new DynamicMetaObject(target.Expression.Cast(target.LimitType), target.PSGetTypeRestriction()),
                new DynamicMetaObject(indexAsInt, indexes[0].PSGetTypeRestriction()),
                target.LimitType.GetProperty("Length"),
                static (t, i) => Expression.ArrayIndex(t, i).Cast(typeof(object)));
        }

        private DynamicMetaObject GetIndexMultiDimensionArray(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject errorSuggestion)
        {
            // We have lots of possibilities here
            //
            //   * single index - enumerable, all ints
            //        - single result, count must match array rank
            //   * single index - enumerable, all enumerable of ints
            //        - slicing, falls back to previous case
            //   * multiple indices - all ints
            //        - single result, must match array rank
            //   * multiple indices - enumerable of ints
            //        - slicing, falls back to first case
            //
            // In script, the above cases look like:
            //     $x = [array]::CreateInstance([int], 3, 3)
            //     $y = 0,0
            //     $z = $y,$y
            //     $x[$y] # case 1
            //     $x[$z] # case 2
            //     $x[1,1] # case 3
            //     $x[(1,1),(0,0)] # case 4

            var array = (Array)target.Value;

            if (indexes.Length == 1)
            {
                var enumerable = PSEnumerableBinder.IsEnumerable(indexes[0]);
                if (enumerable == null)
                {
                    return target.ThrowRuntimeError(indexes, BindingRestrictions.Empty, "NeedMultidimensionalIndex",
                                                    ParserStrings.NeedMultidimensionalIndex,
                                                    ExpressionCache.Constant(array.Rank),
                                                    DynamicExpression.Dynamic(PSToStringBinder.Get(), typeof(string),
                                                                              indexes[0].Expression, ExpressionCache.GetExecutionContextFromTLS));
                }

                return new DynamicMetaObject(
                    Expression.Call(CachedReflectionInfo.ArrayOps_GetMDArrayValueOrSlice,
                                    Expression.Convert(target.Expression, typeof(Array)),
                                    indexes[0].Expression.Cast(typeof(object))),
                    target.CombineRestrictions(indexes));
            }

            var intIndexes = indexes.Select(static index => ConvertIndex(index, typeof(int))).Where(static i => i != null).ToArray();
            if (intIndexes.Length != indexes.Length)
            {
                if (!_allowSlicing)
                {
                    return errorSuggestion ?? CannotIndexTarget(target, indexes);
                }

                return InvokeSlicingIndexer(target, indexes);
            }

            return new DynamicMetaObject(
                Expression.Call(CachedReflectionInfo.ArrayOps_GetMDArrayValue,
                                Expression.Convert(target.Expression, typeof(Array)),
                                Expression.NewArrayInit(typeof(int), intIndexes),
                                ExpressionCache.Constant(!_allowSlicing)),
                target.CombineRestrictions(indexes));
        }

        private DynamicMetaObject InvokeIndexer(DynamicMetaObject target,
                                                DynamicMetaObject[] indexes,
                                                DynamicMetaObject errorSuggestion,
                                                string methodName,
                                                PropertyInfo lengthProperty)
        {
            MethodInfo getter = PSInvokeMemberBinder.FindBestMethod(target, indexes, "get_" + methodName, false, _constraints);

            if (getter == null)
            {
                return CheckForSlicing(target, indexes) ?? errorSuggestion ?? CannotIndexTarget(target, indexes);
            }

            var getterParams = getter.GetParameters();
            if (getterParams.Length != indexes.Length)
            {
                if (getterParams.Length == 1 && _allowSlicing)
                {
                    // We have a slicing operation.
                    return InvokeSlicingIndexer(target, indexes);
                }

                // Calling the indexer will fail because there are either too many or too few indices.
                return errorSuggestion ?? CannotIndexTarget(target, indexes);
            }

            if (getterParams.Length == 1)
            {
                // The getter takes a single argument, so first check if we're slicing.
                var slicingResult = CheckForSlicing(target, indexes);
                if (slicingResult != null)
                {
                    return slicingResult;
                }
            }

            if (getter.ReturnType.IsByRefLike)
            {
                // We cannot return a ByRef-like value in PowerShell, so we disallow getting such an indexer.
                return errorSuggestion ?? new DynamicMetaObject(
                    Expression.Block(
                        Expression.IfThen(
                            Compiler.IsStrictMode(3),
                            Compiler.ThrowRuntimeError(
                                nameof(ParserStrings.CannotIndexWithByRefLikeReturnType),
                                ParserStrings.CannotIndexWithByRefLikeReturnType,
                                Expression.Constant(target.LimitType, typeof(Type)),
                                Expression.Constant(getter.ReturnType, typeof(Type)))),
                        GetNullResult()),
                    target.PSGetTypeRestriction());
            }

            Expression[] indexExprs = new Expression[getterParams.Length];
            for (int i = 0; i < getterParams.Length; ++i)
            {
                var parameterType = getterParams[i].ParameterType;
                indexExprs[i] = parameterType.IsByRefLike
                    ? PSConvertBinder.ConvertToByRefLikeTypeViaCasting(indexes[i], parameterType)
                    : ConvertIndex(indexes[i], parameterType);

                if (indexExprs[i] == null)
                {
                    // Calling the indexer will fail because we can't convert an index to the correct type.
                    return errorSuggestion ?? PSConvertBinder.ThrowNoConversion(target, parameterType, this, _version, indexes);
                }
            }

            if (CanIndexFromEndWithNegativeIndex(target, getter, getterParams))
            {
                if (lengthProperty == null)
                {
                    // Count is declared by most supported types, Length will catch some edge cases like strings.
                    lengthProperty = target.LimitType.GetProperty("Count") ??
                                     target.LimitType.GetProperty("Length");
                }

                if (lengthProperty != null)
                {
                    return IndexWithNegativeChecks(
                        new DynamicMetaObject(target.Expression.Cast(target.LimitType),
                                              target.PSGetTypeRestriction()),
                        new DynamicMetaObject(indexExprs[0], indexes[0].PSGetTypeRestriction()),
                        lengthProperty,
                        (t, i) => Expression.Call(t, getter, i).Cast(typeof(object)));
                }
            }

            // An indexer may do conversion to an unsafe type, so we need version checks
            BindingRestrictions bindingRestrictions = target.CombineRestrictions(indexes);
            bindingRestrictions = bindingRestrictions.Merge(BinderUtils.GetVersionCheck(this, _version));

            // Also add a language mode check to detect toggling between language modes
            bindingRestrictions = bindingRestrictions.Merge(BinderUtils.GetLanguageModeCheckIfHasEverUsedConstrainedLanguage());

            return new DynamicMetaObject(
                SafeIndexResult(Expression.Call(target.Expression.Cast(getter.DeclaringType), getter, indexExprs)),
                bindingRestrictions);
        }

        internal static Expression ConvertIndex(DynamicMetaObject index, Type resultType)
        {
            // ConstrainedLanguage note - Calls to this conversion are protected by the binding rules that call it.
            var conversion = LanguagePrimitives.FigureConversion(index.Value, resultType, out bool debase);
            return conversion.Rank == ConversionRank.None
                       ? null
                       : PSConvertBinder.InvokeConverter(conversion, index.Expression, resultType, debase, ExpressionCache.InvariantCulture);
        }

        private DynamicMetaObject CheckForSlicing(DynamicMetaObject target, DynamicMetaObject[] indexes)
        {
            if (!_allowSlicing)
            {
                return null;
            }

            if (indexes.Length > 1)
            {
                var nonSlicingBinder = PSGetIndexBinder.Get(1, _constraints, allowSlicing: false);
                var expr = Expression.NewArrayInit(typeof(object),
                    indexes.Select(i => DynamicExpression.Dynamic(nonSlicingBinder, typeof(object), target.Expression, i.Expression)));
                return new DynamicMetaObject(expr, target.CombineRestrictions(indexes));
            }

            var enumerableIndex = PSEnumerableBinder.IsEnumerable(indexes[0]);
            if (enumerableIndex != null)
            {
                return new DynamicMetaObject(
                    Expression.Call(CachedReflectionInfo.EnumerableOps_SlicingIndex,
                                    target.Expression.Cast(typeof(object)),
                                    enumerableIndex.Expression.Cast(typeof(IEnumerator)),
                                    Expression.Constant(GetNonSlicingIndexer())),
                    target.CombineRestrictions(enumerableIndex));
            }

            return null;
        }

        private DynamicMetaObject InvokeSlicingIndexer(DynamicMetaObject target, DynamicMetaObject[] indexes)
        {
            Diagnostics.Assert(_allowSlicing, "Slicing is not recursive");

            return new DynamicMetaObject(
                Expression.Call(CachedReflectionInfo.ArrayOps_SlicingIndex,
                                target.Expression.Cast(typeof(object)),
                                Expression.NewArrayInit(typeof(object),
                                                        indexes.Select(static dmo => dmo.Expression.Cast(typeof(object)))),
                                Expression.Constant(GetNonSlicingIndexer())),
                target.CombineRestrictions(indexes));
        }

        private Expression SafeIndexResult(Expression expr)
        {
            var exception = Expression.Parameter(typeof(Exception));
            return Expression.TryCatch(
                expr.Cast(typeof(object)),
                Expression.Catch(
                    exception,
                    Expression.Block(
                        Expression.IfThen(Compiler.IsStrictMode(3), Expression.Rethrow()),
                        GetNullResult())));
        }

        private Expression GetNullResult()
        {
            return _allowSlicing ? ExpressionCache.NullConstant : ExpressionCache.AutomationNullConstant;
        }

        private Func<object, object, object> GetNonSlicingIndexer()
        {
            // Rather than cache a single delegate, we create one for each generated rule under the assumption
            // that, although the generated rule may be used in multiple sites, it's better to have
            // multiple delegates (and hence, multiple nested sites) rather than a single nested site for
            // all non-slicing indexing.
            var targetParamExpr = Expression.Parameter(typeof(object));
            var indexParamExpr = Expression.Parameter(typeof(object));
            return Expression.Lambda<Func<object, object, object>>(
                DynamicExpression.Dynamic(PSGetIndexBinder.Get(1, _constraints, allowSlicing: false), typeof(object), targetParamExpr,
                                          indexParamExpr),
                targetParamExpr, indexParamExpr).Compile();
        }
    }

    /// <summary>
    /// The binder for setting the value of an indexable element, like $x[1] = 5.
    /// </summary>
    internal sealed class PSSetIndexBinder : SetIndexBinder
    {
        private static readonly Dictionary<Tuple<CallInfo, PSMethodInvocationConstraints>, PSSetIndexBinder> s_binderCache
                = new Dictionary<Tuple<CallInfo, PSMethodInvocationConstraints>, PSSetIndexBinder>();

        private readonly PSMethodInvocationConstraints _constraints;
        internal int _version;

        public static PSSetIndexBinder Get(int argCount, PSMethodInvocationConstraints constraints = null)
        {
            lock (s_binderCache)
            {
                PSSetIndexBinder binder;
                var tuple = Tuple.Create(new CallInfo(argCount), constraints);
                if (!s_binderCache.TryGetValue(tuple, out binder))
                {
                    binder = new PSSetIndexBinder(tuple);
                    s_binderCache.Add(tuple, binder);
                }

                return binder;
            }
        }

        private PSSetIndexBinder(Tuple<CallInfo, PSMethodInvocationConstraints> tuple)
            : base(tuple.Item1)
        {
            _constraints = tuple.Item2;
            this._version = 0;
        }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "PSSetIndexBinder indexCnt={0}{1} ver:{2}",
                CallInfo.ArgumentCount,
                _constraints == null ? string.Empty : " constraints: " + _constraints,
                _version);
        }

        internal static void InvalidateCache()
        {
            // Invalidate binders
            lock (s_binderCache)
            {
                foreach (PSSetIndexBinder binder in s_binderCache.Values)
                {
                    binder._version += 1;
                }
            }
        }

        public override DynamicMetaObject FallbackSetIndex(
            DynamicMetaObject target,
            DynamicMetaObject[] indexes,
            DynamicMetaObject value,
            DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue || indexes.Any(static mo => !mo.HasValue) || !value.HasValue)
            {
                return Defer(indexes.Prepend(target).Append(value).ToArray()).WriteToDebugLog(this);
            }

            if (target.Value is PSObject && (PSObject.Base(target.Value) != target.Value) ||
                indexes.Any(static mo => mo.Value is PSObject && (PSObject.Base(mo.Value) != mo.Value)))
            {
                return this.DeferForPSObject(indexes.Prepend(target).Append(value).ToArray()).WriteToDebugLog(this);
            }

            // Check if this is a COM Object
            DynamicMetaObject result;
            if (ComInterop.ComBinder.TryBindSetIndex(this, target, indexes, value, out result))
            {
                return result.UpdateComRestrictionsForPsObject(indexes).WriteToDebugLog(this);
            }

            if (target.Value == null)
            {
                return (errorSuggestion ??
                        target.ThrowRuntimeError(indexes, BindingRestrictions.Empty, "NullArray", ParserStrings.NullArray)).WriteToDebugLog(this);
            }

            if (indexes.Length == 1 && indexes[0].Value == null)
            {
                return (errorSuggestion ??
                        target.ThrowRuntimeError(indexes, BindingRestrictions.Empty, "NullArrayIndex", ParserStrings.NullArrayIndex).WriteToDebugLog(this));
            }

            if (target.LimitType.IsArray)
            {
                return SetIndexArray(target, indexes, value, errorSuggestion).WriteToDebugLog(this);
            }

            var defaultMember = target.LimitType.GetCustomAttributes<DefaultMemberAttribute>(true).FirstOrDefault();
            if (defaultMember != null)
            {
                return (InvokeIndexer(target, indexes, value, errorSuggestion, defaultMember.MemberName)).WriteToDebugLog(this);
            }

            return errorSuggestion ?? CannotIndexTarget(target, indexes, value).WriteToDebugLog(this);
        }

        private DynamicMetaObject CannotIndexTarget(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject value)
        {
            BindingRestrictions bindingRestrictions = value.PSGetTypeRestriction();
            bindingRestrictions = bindingRestrictions.Merge(BinderUtils.GetVersionCheck(this, _version));

            // Also add a language mode check to detect toggling between language modes
            bindingRestrictions = bindingRestrictions.Merge(BinderUtils.GetLanguageModeCheckIfHasEverUsedConstrainedLanguage());

            return target.ThrowRuntimeError(indexes, bindingRestrictions, "CannotIndex", ParserStrings.CannotIndex, Expression.Constant(target.LimitType, typeof(Type)));
        }

        private DynamicMetaObject InvokeIndexer(
            DynamicMetaObject target,
            DynamicMetaObject[] indexes,
            DynamicMetaObject value,
            DynamicMetaObject errorSuggestion,
            string methodName)
        {
            MethodInfo setter = PSInvokeMemberBinder.FindBestMethod(target, indexes.Append(value), "set_" + methodName, false, _constraints);

            if (setter == null)
            {
                return errorSuggestion ?? CannotIndexTarget(target, indexes, value);
            }

            var setterParams = setter.GetParameters();
            int paramLength = setterParams.Length;

            if (paramLength != indexes.Length + 1)
            {
                // Calling the indexer will fail because there are either too many or too few indices.
                return errorSuggestion ?? CannotIndexTarget(target, indexes, value);
            }

            if (setterParams[paramLength - 1].ParameterType.IsByRefLike)
            {
                // In theory, it's possible to call the setter with a value that can be implicitly/explicitly casted to the target ByRef-like type.
                // However, the set-property/set-indexer semantics in PowerShell requires returning the value after the setting operation. We cannot
                // return a ByRef-like value back, so we just disallow setting an indexer that takes a ByRef-like type value.
                return errorSuggestion ?? new DynamicMetaObject(
                    Compiler.ThrowRuntimeError(
                        nameof(ParserStrings.CannotIndexWithByRefLikeReturnType),
                        ParserStrings.CannotIndexWithByRefLikeReturnType,
                        Expression.Constant(target.LimitType, typeof(Type)),
                        Expression.Constant(setterParams[paramLength - 1].ParameterType, typeof(Type))),
                    target.PSGetTypeRestriction());
            }

            Expression[] indexExprs = new Expression[paramLength];
            for (int i = 0; i < paramLength; ++i)
            {
                var parameterType = setterParams[i].ParameterType;
                var argument = (i == paramLength - 1) ? value : indexes[i];

                indexExprs[i] = parameterType.IsByRefLike
                    ? PSConvertBinder.ConvertToByRefLikeTypeViaCasting(argument, parameterType)
                    : PSGetIndexBinder.ConvertIndex(argument, parameterType);

                if (indexExprs[i] == null)
                {
                    // Calling the indexer will fail because we can't convert an index to the correct type.
                    return errorSuggestion ?? PSConvertBinder.ThrowNoConversion(target, parameterType, this, _version, indexes.Append(value).ToArray());
                }
            }

            if (paramLength == 2
                && setterParams[0].ParameterType == typeof(int)
                && target.Value is not IDictionary)
            {
                // PowerShell supports negative indexing for some types (specifically, those with a single
                // int parameter to the indexer, and also have either a Length or Count property.)  For
                // those types, generate special code to check for negative indices, otherwise just generate
                // the call.
                PropertyInfo lengthProperty = target.LimitType.GetProperty("Length") ??
                                              target.LimitType.GetProperty("Count");

                if (lengthProperty != null)
                {
                    return IndexWithNegativeChecks(
                        new DynamicMetaObject(target.Expression.Cast(target.LimitType),
                                              target.PSGetTypeRestriction()),
                        new DynamicMetaObject(indexExprs[0], indexes[0].PSGetTypeRestriction()),
                        new DynamicMetaObject(indexExprs[1], value.PSGetTypeRestriction()),
                        lengthProperty,
                        (t, i, v) => Expression.Call(t, setter, i, v));
                }
            }

            BindingRestrictions bindingRestrictions = target.CombineRestrictions(indexes).Merge(value.PSGetTypeRestriction());

            // Add the version checks and (potentially) language mode checks, as this setter
            // may invoke a conversion to an unsafe type.
            bindingRestrictions = bindingRestrictions.Merge(BinderUtils.GetVersionCheck(this, _version));

            // Also add a language mode check to detect toggling between language modes
            bindingRestrictions = bindingRestrictions.Merge(BinderUtils.GetLanguageModeCheckIfHasEverUsedConstrainedLanguage());

            // We'll store the value in a temp so we can return it.  We'll also replace the expr in our array of arguments
            // to the indexer with the temp so any conversions are executed just once.
            var valExpr = indexExprs[indexExprs.Length - 1];
            var valTmp = Expression.Parameter(valExpr.Type, "value");
            indexExprs[indexExprs.Length - 1] = valTmp;
            return new DynamicMetaObject(
                Expression.Block(
                    new ParameterExpression[] { valTmp },
                    Expression.Assign(valTmp, valExpr),
                    Expression.Call(target.Expression.Cast(setter.DeclaringType), setter, indexExprs),
                    valTmp.Cast(typeof(object))),
                bindingRestrictions);
        }

        private DynamicMetaObject IndexWithNegativeChecks(
            DynamicMetaObject target,
            DynamicMetaObject index,
            DynamicMetaObject value,
            PropertyInfo lengthProperty,
            Func<Expression, Expression, Expression, Expression> generateIndexOperation)
        {
            BindingRestrictions bindingRestrictions = target.CombineRestrictions(index).Merge(value.Restrictions);

            // If the target is of an unsafe type for ConstrainedLanguage, we need to pay
            // attention to version and language mode. Otherwise, strongly-typed arrays of unsafe types
            // can be used for type conversion.
            bindingRestrictions = bindingRestrictions.Merge(BinderUtils.GetOptionalVersionAndLanguageCheckForType(this, target.LimitType, _version));

            // Generate:
            //    len = obj.Length
            //    if (index < 0)
            //        index = index + len
            //    obj[index] = value
            var targetTmp = Expression.Parameter(target.LimitType, "target");
            var lenTmp = Expression.Parameter(typeof(int), "len");
            var valueExpr = value.Expression;
            var valTmp = Expression.Parameter(valueExpr.Type, "value");
            var indexTmp = Expression.Parameter(typeof(int), "index");
            return new DynamicMetaObject(
                Expression.Block(
                    new ParameterExpression[] { targetTmp, valTmp, lenTmp, indexTmp },
                    // Save the target because we use it multiple times.
                    Expression.Assign(targetTmp, target.Expression.Cast(target.LimitType)),
                    // Save the value because it too is used multiple times, but only to keep the DLR happy
                    Expression.Assign(valTmp, valueExpr),
                    // Save the length because we use it multiple times.
                    Expression.Assign(lenTmp,
                                      Expression.Property(targetTmp, lengthProperty)),
                    // Save the index because we use it multiple times
                    Expression.Assign(indexTmp, index.Expression),
                    // Adjust the index if it's negative
                    Expression.IfThen(Expression.LessThan(indexTmp, ExpressionCache.Constant(0)),
                                      Expression.Assign(indexTmp, Expression.Add(indexTmp, lenTmp))),
                    // Do the indexing
                    generateIndexOperation(targetTmp, indexTmp, valTmp),
                    // Make sure the result of this operation is the value.  PowerShell won't use this value
                    // in any way, but the DLR requires it (and in theory, if PSObject uses this binder, other
                    // languages could use this value.)
                    valTmp.Cast(typeof(object))),
                bindingRestrictions);
        }

        private DynamicMetaObject SetIndexArray(DynamicMetaObject target,
                                                DynamicMetaObject[] indexes,
                                                DynamicMetaObject value,
                                                DynamicMetaObject errorSuggestion)
        {
            var array = (Array)target.Value;

            if (array.Rank > 1)
            {
                return SetIndexMultiDimensionArray(target, indexes, value, errorSuggestion);
            }

            if (indexes.Length > 1)
            {
                return errorSuggestion ??
                       target.ThrowRuntimeError(indexes, value.PSGetTypeRestriction(), "ArraySliceAssignmentFailed",
                                                ParserStrings.ArraySliceAssignmentFailed,
                                                Expression.Call(CachedReflectionInfo.ArrayOps_IndexStringMessage,
                                                                Expression.NewArrayInit(typeof(object),
                                                                                        indexes.Select(static i => i.Expression.Cast(typeof(object))))));
            }

            var intIndex = PSGetIndexBinder.ConvertIndex(indexes[0], typeof(int));
            if (intIndex == null)
            {
                return errorSuggestion ??
                       PSConvertBinder.ThrowNoConversion(indexes[0], typeof(int), this, _version, target, value);
            }

            var elementType = target.LimitType.GetElementType();
            var valueExpr = PSGetIndexBinder.ConvertIndex(value, elementType);
            if (valueExpr == null)
            {
                return errorSuggestion ??
                       PSConvertBinder.ThrowNoConversion(value, elementType, this, _version, indexes.Prepend(target).ToArray());
            }

            return IndexWithNegativeChecks(
                new DynamicMetaObject(target.Expression.Cast(target.LimitType), target.PSGetTypeRestriction()),
                new DynamicMetaObject(intIndex, indexes[0].PSGetTypeRestriction()),
                new DynamicMetaObject(valueExpr, value.PSGetTypeRestriction()), target.LimitType.GetProperty("Length"),
                static (t, i, v) => Expression.Assign(Expression.ArrayAccess(t, i), v));
        }

        private DynamicMetaObject SetIndexMultiDimensionArray(DynamicMetaObject target,
                                                              DynamicMetaObject[] indexes,
                                                              DynamicMetaObject value,
                                                              DynamicMetaObject errorSuggestion)
        {
            var elementType = target.LimitType.GetElementType();
            var valueExpr = PSGetIndexBinder.ConvertIndex(value, elementType);
            if (valueExpr == null)
            {
                return errorSuggestion ??
                       PSConvertBinder.ThrowNoConversion(value, elementType, this, _version, indexes.Prepend(target).ToArray());
            }

            if (indexes.Length == 1)
            {
                var indexExpr = PSGetIndexBinder.ConvertIndex(indexes[0], typeof(int[]));
                if (indexExpr == null)
                {
                    return errorSuggestion ??
                           PSConvertBinder.ThrowNoConversion(indexes[0], typeof(int[]), this, _version, new DynamicMetaObject[] { target, value });
                }

                return new DynamicMetaObject(
                    Expression.Call(CachedReflectionInfo.ArrayOps_SetMDArrayValue,
                                    target.Expression.Cast(typeof(Array)),
                                    indexExpr,
                                    valueExpr.Cast(typeof(object))),
                    target.CombineRestrictions(indexes).Merge(value.PSGetTypeRestriction()));
            }

            var array = (Array)target.Value;

            if (indexes.Length != array.Rank)
            {
                return errorSuggestion ??
                       target.ThrowRuntimeError(indexes, value.PSGetTypeRestriction(), "NeedMultidimensionalIndex",
                                                ParserStrings.NeedMultidimensionalIndex,
                                                ExpressionCache.Constant(array.Rank),
                                                Expression.Call(CachedReflectionInfo.ArrayOps_IndexStringMessage,
                                                                Expression.NewArrayInit(typeof(object),
                                                                                        indexes.Select(static i => i.Expression.Cast(typeof(object))))));
            }

            var indexExprs = new Expression[indexes.Length];
            for (int i = 0; i < indexes.Length; i++)
            {
                indexExprs[i] = PSGetIndexBinder.ConvertIndex(indexes[i], typeof(int));
                if (indexExprs[i] == null)
                {
                    return PSConvertBinder.ThrowNoConversion(indexes[i], typeof(int), this, _version,
                        indexes.Except(new DynamicMetaObject[] { indexes[i] }).Append(target).Append(value).ToArray());
                }
            }

            return new DynamicMetaObject(
                Expression.Call(CachedReflectionInfo.ArrayOps_SetMDArrayValue,
                                target.Expression.Cast(typeof(Array)),
                                Expression.NewArrayInit(typeof(int), indexExprs),
                                valueExpr.Cast(typeof(object))),
                target.CombineRestrictions(indexes).Merge(value.PSGetTypeRestriction()));
        }
    }

    /// <summary>
    /// The binder for getting a member of a class, like $foo.bar or [foo]::bar.
    /// </summary>
    internal class PSGetMemberBinder : GetMemberBinder
    {
        private sealed class KeyComparer : IEqualityComparer<PSGetMemberBinderKeyType>
        {
            public bool Equals(PSGetMemberBinderKeyType x, PSGetMemberBinderKeyType y)
            {
                // The non-static binder cache is case-sensitive because sites need the name used per site
                // when the target object is a case-sensitive IDictionary.  Under all other circumstances,
                // binding is case-insensitive.
                var stringComparison = x.Item3 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                return x.Item1.Equals(y.Item1, stringComparison) &&
                       x.Item2 == y.Item2 &&
                       x.Item3 == y.Item3 &&
                       x.Item4 == y.Item4;
            }

            public int GetHashCode(PSGetMemberBinderKeyType obj)
            {
                var stringComparer = obj.Item3 ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
                return Utils.CombineHashCodes(stringComparer.GetHashCode(obj.Item1),
                    obj.Item2 == null ? 0 : obj.Item2.GetHashCode(),
                    obj.Item3.GetHashCode(),
                    obj.Item4.GetHashCode());
            }
        }

        private sealed class ReservedMemberBinder : PSGetMemberBinder
        {
            internal ReservedMemberBinder(string name, bool ignoreCase, bool @static) : base(name, null, ignoreCase, @static, nonEnumerating: false)
            {
            }

            public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
            {
                MethodInfo mi = null;
                Expression targetExpr = null;
                switch (Name)
                {
                    case PSObject.AdaptedMemberSetName:
                        mi = CachedReflectionInfo.ReservedNameMembers_GeneratePSAdaptedMemberSet;
                        targetExpr = target.Expression.Cast(typeof(object));
                        break;
                    case PSObject.BaseObjectMemberSetName:
                        mi = CachedReflectionInfo.ReservedNameMembers_GeneratePSBaseMemberSet;
                        targetExpr = target.Expression.Cast(typeof(object));
                        break;
                    case PSObject.ExtendedMemberSetName:
                        mi = CachedReflectionInfo.ReservedNameMembers_GeneratePSExtendedMemberSet;
                        targetExpr = target.Expression.Cast(typeof(object));
                        break;
                    case PSObject.PSObjectMemberSetName:
                        mi = CachedReflectionInfo.ReservedNameMembers_GeneratePSObjectMemberSet;
                        targetExpr = target.Expression.Cast(typeof(object));
                        break;
                    case PSObject.PSTypeNames:
                        mi = CachedReflectionInfo.ReservedNameMembers_PSTypeNames;
                        targetExpr = target.Expression.Convert(typeof(PSObject));
                        break;
                }

                Diagnostics.Assert(mi != null, "ReservedMemberBinder doesn't support member Name");

                return new DynamicMetaObject(WrapGetMemberInTry(Expression.Call(mi, targetExpr)), target.PSGetTypeRestriction());
            }
        }

        private static readonly Dictionary<PSGetMemberBinderKeyType, PSGetMemberBinder> s_binderCache
            = new Dictionary<PSGetMemberBinderKeyType, PSGetMemberBinder>(new KeyComparer());

        // Because the non-static binder is case-sensitive, we need a list of all binders for a given
        // name when we discover an instance member or type table member for that given name so we
        // can update each of those binders.
        private static readonly ConcurrentDictionary<string, List<PSGetMemberBinder>> s_binderCacheIgnoringCase
            = new ConcurrentDictionary<string, List<PSGetMemberBinder>>(StringComparer.OrdinalIgnoreCase);

        static PSGetMemberBinder()
        {
            s_binderCache.Add(Tuple.Create(PSObject.AdaptedMemberSetName, (Type)null, false, false),
                new ReservedMemberBinder(PSObject.AdaptedMemberSetName, ignoreCase: true, @static: false));
            s_binderCache.Add(Tuple.Create(PSObject.ExtendedMemberSetName, (Type)null, false, false),
                new ReservedMemberBinder(PSObject.ExtendedMemberSetName, ignoreCase: true, @static: false));
            s_binderCache.Add(Tuple.Create(PSObject.BaseObjectMemberSetName, (Type)null, false, false),
                new ReservedMemberBinder(PSObject.BaseObjectMemberSetName, ignoreCase: true, @static: false));
            s_binderCache.Add(Tuple.Create(PSObject.PSObjectMemberSetName, (Type)null, false, false),
                new ReservedMemberBinder(PSObject.PSObjectMemberSetName, ignoreCase: true, @static: false));
            s_binderCache.Add(Tuple.Create(PSObject.PSTypeNames, (Type)null, false, false),
                new ReservedMemberBinder(PSObject.PSTypeNames, ignoreCase: true, @static: false));
        }

        private readonly bool _static;
        private readonly bool _nonEnumerating;
        private readonly Type _classScope;
        internal int _version;

        private bool _hasInstanceMember;

        internal bool HasInstanceMember { get { return _hasInstanceMember; } }

        internal static void SetHasInstanceMember(string memberName)
        {
            // We must invalidate dynamic sites (if any) when the first instance member (for this binder)
            // is created, but we don't need to invalidate any sites after the first instance member.
            // Before any instance members exist, restrictions might look like:
            //     if (binderVersion == oldBinderVersion && obj is string) { ... }
            // After an instance member is known to exist, the above test (for an object that has no instance
            // member) will look like:
            //    MemberInfo mi;
            //    if (binderVersion == oldBinderVersion && !TryGetInstanceMember(obj, memberName, out mi) && obj is string)
            //    {
            //        return ((string)obj).memberName;
            //    }
            //    else { update site }
            // And if there is an instance member, the generic rule will look like:
            //    MemberInfo mi;
            //    if (binderVersion == oldBinderVersion && TryGetInstanceMember(obj, memberName, out mi))
            //    {
            //        return mi.Value;
            //    }
            //    else { update site }
            // This way, we can avoid the call to TryGetInstanceMember for binders when we know there aren't any instance
            // members, yet invalidate those rules once somebody adds an instance member.

            var binderList = s_binderCacheIgnoringCase.GetOrAdd(memberName, static _ => new List<PSGetMemberBinder>());

            lock (binderList)
            {
                if (binderList.Count == 0)
                {
                    // Force one binder to be created if one hasn't been created already.
                    PSGetMemberBinder.Get(memberName, (Type)null, @static: false);
                }

                foreach (var binder in binderList)
                {
                    if (!binder._hasInstanceMember)
                    {
                        lock (binder)
                        {
                            if (!binder._hasInstanceMember)
                            {
                                binder._version += 1;
                                binder._hasInstanceMember = true;
                            }
                        }
                    }
                }
            }
        }

        private bool _hasTypeTableMember;

        internal static void TypeTableMemberAdded(string memberName)
        {
            var binderList = s_binderCacheIgnoringCase.GetOrAdd(memberName, static _ => new List<PSGetMemberBinder>());

            lock (binderList)
            {
                if (binderList.Count == 0)
                {
                    // Force one binder to be created if one hasn't been created already.
                    PSGetMemberBinder.Get(memberName, (Type)null, @static: false);
                }

                foreach (var binder in binderList)
                {
                    lock (binder)
                    {
                        binder._version += 1;
                        binder._hasTypeTableMember = true;
                    }
                }
            }
        }

        internal static void TypeTableMemberPossiblyUpdated(string memberName)
        {
            var binderList = s_binderCacheIgnoringCase.GetOrAdd(memberName, static _ => new List<PSGetMemberBinder>());

            lock (binderList)
            {
                foreach (var binder in binderList)
                {
                    Interlocked.Increment(ref binder._version);
                }
            }
        }

        public static PSGetMemberBinder Get(string memberName, TypeDefinitionAst classScope, bool @static)
        {
            return Get(memberName, classScope?.Type, @static, false);
        }

        public static PSGetMemberBinder Get(string memberName, Type classScope, bool @static)
        {
            return Get(memberName, classScope, @static, false);
        }

        private PSGetMemberBinder GetNonEnumeratingBinder()
        {
            return Get(this.Name, _classScope, @static: false, nonEnumerating: true);
        }

        private static PSGetMemberBinder Get(string memberName, Type classScope, bool @static, bool nonEnumerating)
        {
            PSGetMemberBinder result;

            lock (s_binderCache)
            {
                var tuple = Tuple.Create(memberName, classScope, @static, nonEnumerating);
                if (!s_binderCache.TryGetValue(tuple, out result))
                {
                    // We might be seeing a reserved name with a different case.  Check for that before
                    // creating a new binder.  For reserved names, we can safely use a single binder for
                    // any case.
                    if (PSMemberInfoCollection<PSMemberInfo>.IsReservedName(memberName))
                    {
                        var tupleLower = Tuple.Create(memberName.ToLowerInvariant(), (Type)null, @static, nonEnumerating);
                        result = s_binderCache[tupleLower];
                    }
                    else
                    {
                        result = new PSGetMemberBinder(memberName, classScope, true, @static, nonEnumerating);
                        if (!@static)
                        {
                            var binderList = s_binderCacheIgnoringCase.GetOrAdd(memberName, static _ => new List<PSGetMemberBinder>());
                            lock (binderList)
                            {
                                if (binderList.Count > 0)
                                {
                                    result._hasInstanceMember = binderList[0]._hasInstanceMember;
                                    result._hasTypeTableMember = binderList[0]._hasTypeTableMember;
                                }

                                binderList.Add(result);

                                Diagnostics.Assert(binderList.All(b => b._hasInstanceMember == result._hasInstanceMember),
                                                   "All binders in the list should have _hasInstanceMember set identically");
                                Diagnostics.Assert(binderList.All(b => b._hasTypeTableMember == result._hasTypeTableMember),
                                                   "All binders in the list should have _hasTypeTableMember set identically");
                            }
                        }
                    }

                    s_binderCache.Add(tuple, result);
                }
            }

            return result;
        }

        private PSGetMemberBinder(string name, Type classScope, bool ignoreCase, bool @static, bool nonEnumerating)
            : base(name, ignoreCase)
        {
            _static = @static;
            _classScope = classScope;
            this._version = 0;
            _nonEnumerating = nonEnumerating;
        }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "GetMember: {0}{1}{2} ver:{3}",
                Name,
                _static ? " static" : string.Empty,
                _nonEnumerating ? " nonEnumerating" : string.Empty,
                _version);
        }

        public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue)
            {
                return Defer(target);
            }

            // Defer COM objects or arguments wrapped in PSObjects
            if (target.Value is PSObject && (PSObject.Base(target.Value) != target.Value))
            {
                object baseObject = PSObject.Base(target.Value);
                if (baseObject != null && Marshal.IsComObject(baseObject))
                {
                    // We unwrap only if the 'base' is a COM object. It's unnecessary to unwrap in other cases,
                    // especially in the case of strings, we would lose instance members on the PSObject.
                    // Therefore, we need to use a stricter restriction to make sure PSObject 'target' with other
                    // base types doesn't get unwrapped.
                    return this.DeferForPSObject(target, targetIsComObject: true).WriteToDebugLog(this);
                }
            }

            // Check if this is a COM Object
            DynamicMetaObject result;
            if (ComInterop.ComBinder.TryBindGetMember(this, target, out result, delayInvocation: false))
            {
                result = new DynamicMetaObject(WrapGetMemberInTry(result.Expression), result.Restrictions);
                return result.WriteToDebugLog(this);
            }

            object targetValue = PSObject.Base(target.Value);

            if (targetValue == null)
            {
                // PSGetTypeRestriction will actually create an instance restriction because the targetValue is null.
                return PropertyDoesntExist(target, target.PSGetTypeRestriction()).WriteToDebugLog(this);
            }

            BindingRestrictions restrictions;
            PSMemberInfo memberInfo;
            Expression expr = null;
            if (_hasInstanceMember && TryGetInstanceMember(target.Value, Name, out memberInfo))
            {
                // If there is an instance member, we generate (roughly) the following:
                //     PSMemberInfo memberInfo;
                //     if (PSGetMemberBinder.TryGetInstanceMember(target.Value, Name, out memberInfo))
                //         return memberInfo.Value;
                //     else
                //         update the site
                // We use a generic method like this because:
                //   * If one object has an instance property with a given name, it's like many others do as well
                //   * We want to avoid generating new sites for every object with an instance member
                // As an alternative, would could generate the following psuedo-code:
                //     if (target.Value == previousInstance)
                //         return optimized value (depending on the exact PSMemberInfo subclass)
                //     else update the site
                // But the assumption here is that many sites probably performs worse than the dictionary lookup
                // and unoptimized virtual call to PSMemberInfo.Value.
                //
                // The binding restrictions could avoid a version check because it's never wrong to look for an instance member,
                // but we add the check because the DLR requires a non-empty check when the target implements IDynamicMetaObjectProvider,
                // which PSObject does.  The version check is also marginally useful if we knew we'd never see another
                // instance member with this member name, but we're not tracking things to make that a useful test.

                var memberInfoVar = Expression.Variable(typeof(PSMemberInfo));
                expr = Expression.Condition(
                    Expression.Call(CachedReflectionInfo.PSGetMemberBinder_TryGetInstanceMember, target.Expression.Cast(typeof(object)), Expression.Constant(Name), memberInfoVar),
                    Expression.Property(memberInfoVar, "Value"),
                    this.GetUpdateExpression(typeof(object)));
                expr = WrapGetMemberInTry(expr);

                return (new DynamicMetaObject(Expression.Block(new[] { memberInfoVar }, expr), BinderUtils.GetVersionCheck(this, _version))).WriteToDebugLog(this);
            }

            bool canOptimize;
            Type aliasConversionType;
            memberInfo = GetPSMemberInfo(target, out restrictions, out canOptimize, out aliasConversionType, MemberTypes.Property);

            if (!canOptimize)
            {
                Diagnostics.Assert(memberInfo == null, "We don't bother returning members if we can't optimize.");
                return new DynamicMetaObject(
                    WrapGetMemberInTry(Expression.Call(CachedReflectionInfo.PSGetMemberBinder_GetAdaptedValue,
                                                       GetTargetExpr(target, typeof(object)),
                                                       Expression.Constant(Name))),
                    restrictions).WriteToDebugLog(this);
            }

            if (memberInfo != null)
            {
                Diagnostics.Assert(memberInfo.instance == null, "We shouldn't be here if a member is already bound.");

                // The most common case - we're getting some property.  We can optimize many different kinds
                // of property accessors, so we special case each possibility.
                var propertyInfo = memberInfo as PSPropertyInfo;
                if (propertyInfo != null)
                {
                    if (!propertyInfo.IsGettable)
                    {
                        return GenerateGetPropertyException(restrictions).WriteToDebugLog(this);
                    }

                    var property = propertyInfo as PSProperty;
                    if (property != null)
                    {
                        var adapterData = property.adapterData as DotNetAdapter.PropertyCacheEntry;
                        Diagnostics.Assert(adapterData != null, "We have an unknown PSProperty that we aren't correctly optimizing.");

                        if (adapterData.member.DeclaringType.IsGenericTypeDefinition || adapterData.propertyType.IsByRefLike)
                        {
                            // We really should throw an error, but accessing property getter
                            // doesn't throw error in PowerShell since V2, even in strict mode.
                            expr = ExpressionCache.NullConstant;
                        }
                        else
                        {
                            // For static property access, the target expr must be null.  For non-static, we must convert
                            // because target.Expression is typeof(object) because this is a dynamic site.
                            var targetExpr = _static ? null : GetTargetExpr(target, adapterData.member.DeclaringType);
                            var propertyAccessor = adapterData.member as PropertyInfo;
                            if (propertyAccessor != null)
                            {
                                if (propertyAccessor.GetMethod.IsFamily &&
                                    (_classScope == null || !_classScope.IsSubclassOf(propertyAccessor.DeclaringType)))
                                {
                                    return GenerateGetPropertyException(restrictions).WriteToDebugLog(this);
                                }

                                if (propertyAccessor.PropertyType.IsByRef)
                                {
                                    expr = Expression.Call(
                                        CachedReflectionInfo.ByRefOps_GetByRefPropertyValue,
                                        targetExpr,
                                        Expression.Constant(propertyAccessor));
                                }
                                else
                                {
                                    expr = Expression.Property(targetExpr, propertyAccessor);
                                }
                            }
                            else
                            {
                                Diagnostics.Assert(adapterData.member is FieldInfo,
                                                   "A DotNetAdapter.PropertyCacheEntry has something other than PropertyInfo or FieldInfo.");
                                expr = Expression.Field(targetExpr, (FieldInfo)adapterData.member);
                            }
                        }
                    }

                    var scriptProperty = propertyInfo as PSScriptProperty;
                    if (scriptProperty != null)
                    {
                        expr = Expression.Call(Expression.Constant(scriptProperty, typeof(PSScriptProperty)),
                                               CachedReflectionInfo.PSScriptProperty_InvokeGetter, target.Expression.Cast(typeof(object)));
                    }

                    var codeProperty = propertyInfo as PSCodeProperty;
                    if (codeProperty != null)
                    {
                        Diagnostics.Assert(codeProperty.GetterCodeReference != null, "CodeProperty isn't gettable, should have generated error code above.");
                        Diagnostics.Assert(codeProperty.GetterCodeReference.IsStatic, "CodeProperty should be a static method.");

                        expr = PSInvokeMemberBinder.InvokeMethod(codeProperty.GetterCodeReference, null, new[] { target },
                            false, PSInvokeMemberBinder.MethodInvocationType.Getter);
                    }

                    var noteProperty = propertyInfo as PSNoteProperty;
                    if (noteProperty != null)
                    {
                        Diagnostics.Assert(!noteProperty.IsSettable, "If the note is settable, incorrect code is generated.");
                        expr = Expression.Property(Expression.Constant(propertyInfo, typeof(PSNoteProperty)), CachedReflectionInfo.PSNoteProperty_Value);
                    }

                    Diagnostics.Assert(expr != null, "Unexpected property type encountered");

                    if (aliasConversionType != null)
                    {
                        expr = expr.Convert(aliasConversionType);
                    }
                }
                else
                {
                    expr = Expression.Call(CachedReflectionInfo.PSGetMemberBinder_CloneMemberInfo,
                                           Expression.Constant(memberInfo, typeof(PSMemberInfo)),
                                           target.Expression.Cast(typeof(object)));
                }
            }

            if (targetValue is IDictionary)
            {
                Type genericTypeArg = null;
                bool isGeneric = IsGenericDictionary(targetValue, ref genericTypeArg);

                if (!isGeneric || genericTypeArg != null)
                {
                    var temp = Expression.Variable(typeof(object));
                    // If expr is not null, it's the fallback when no member exists.  If it is null,
                    // the fallback is the result from PropertyDoesntExist.
                    expr ??= (errorSuggestion ?? PropertyDoesntExist(target, restrictions)).Expression;

                    var method = isGeneric
                        ? CachedReflectionInfo.PSGetMemberBinder_TryGetGenericDictionaryValue.MakeGenericMethod(genericTypeArg)
                        : CachedReflectionInfo.PSGetMemberBinder_TryGetIDictionaryValue;
                    expr = Expression.Block(new[] { temp },
                        Expression.Condition(
                            Expression.Call(method, GetTargetExpr(target, method.GetParameters()[0].ParameterType), Expression.Constant(Name), temp),
                            temp,
                            expr.Cast(typeof(object))));
                }
            }

            return expr != null
                ? new DynamicMetaObject(WrapGetMemberInTry(expr), restrictions).WriteToDebugLog(this)
                : (errorSuggestion ?? PropertyDoesntExist(target, restrictions)).WriteToDebugLog(this);
        }

        private DynamicMetaObject GenerateGetPropertyException(BindingRestrictions restrictions)
        {
            return new DynamicMetaObject(
                Compiler.ThrowRuntimeError("WriteOnlyProperty", ExtendedTypeSystem.WriteOnlyProperty,
                    this.ReturnType, Expression.Constant(Name)),
                restrictions);
        }

        internal static bool IsGenericDictionary(object value, ref Type genericTypeArg)
        {
            bool isGeneric = false;
            foreach (var i in value.GetType().GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    isGeneric = true;
                    var genericArguments = i.GetGenericArguments();
                    if (genericArguments[0] == typeof(string))
                    {
                        // Our generic method for lookup takes IDictionary<string,T>, we need
                        // to remember T.
                        genericTypeArg = genericArguments[1];
                    }
                }
            }

            return isGeneric;
        }

        /// <summary>
        /// Get the actual value, as an expression, of the object represented by target.  This
        /// will get the base object if it's a psobject, plus correctly handle Nullable.
        /// </summary>
        internal static Expression GetTargetExpr(DynamicMetaObject target, Type castToType = null)
        {
            var expr = target.Expression;
            var value = target.Value;

            // If the target value is actually a deserialized PSObject, we should use the original value
            var psobj = value as PSObject;
            if (psobj != null && psobj != AutomationNull.Value && !psobj.IsDeserialized)
            {
                expr = Expression.Call(CachedReflectionInfo.PSObject_Base, expr);
                value = PSObject.Base(value);
            }

            var type = castToType ?? ((value != null) ? value.GetType() : typeof(object));

            if (expr.Type != type)
            {
                // Unbox value types (or use Nullable<T>.Value) to avoid a copy in case the value is mutated.
                // In case that castToType is System.Object and expr.Type is Nullable<ValueType>, expr.Cast(System.Object) will
                // get the underlying value by default. So "GetTargetExpr(target).Cast(typeof(object))" is actually the same as
                // "GetTargetExpr(target, typeof(object))".
                expr = type.IsValueType
                           ? (Nullable.GetUnderlyingType(expr.Type) != null
                                  ? (Expression)Expression.Property(expr, "Value")
                                  : Expression.Unbox(expr, type))
                           : expr.Cast(type);
            }

            return expr;
        }

        /// <summary>
        /// Return the binding result when no property exists.
        /// </summary>
        private DynamicMetaObject PropertyDoesntExist(DynamicMetaObject target, BindingRestrictions restrictions)
        {
            // If the property does not exist, but the target is enumerable, we'll turn this expression into roughly the equivalent
            // pipeline:
            //     $x | foreach-object { $_.Property }
            // I say roughly because we'll actually iterate through $_ if it doesn't have Property and it is enumerable, and we'll
            // do this recursively.  This makes it easy to chain property references and not worry if the property returns collections or not, e.g.:
            //     $x.Modules.ModuleName
            // If Modules returns a collection, but you want all the module names of all the modules, then it just works.
            // The _nonEnumerating aspect of this binder is simply a way of avoiding the recursing inside the binder, allowing us to
            // collect the results in the helper method we call.  One alternative to _nonEnumerating is to have the helper method
            // not recurse, but mark it's return value specially so that recursive calls to the helper can detect that the results
            // need to be flattened.
            // IsEnumerable treats AutomationNull.Value as a zero length array which we don't want to do here.
            if (!_nonEnumerating && target.Value != AutomationNull.Value)
            {
                var enumerable = PSEnumerableBinder.IsEnumerable(target);
                if (enumerable != null)
                {
                    return new DynamicMetaObject(
                        Expression.Call(CachedReflectionInfo.EnumerableOps_PropertyGetter,
                                        Expression.Constant(this.GetNonEnumeratingBinder()),
                                        enumerable.Expression), restrictions);
                }
            }

            // As part of our effort to hide how a command can return a singleton or array, we want to allow people to iterate
            // over singletons with the foreach statement (which has worked since V1) and a for loop, for example:
            //     for ($i = 0; $i -lt $x.Length; $i++) { $x[$i] }
            // If $x is a singleton, we want to return 1 for the length so code like this works correctly.
            // We do not want this magic to show up in Get-Member output, tab completion, intellisense, etc.
            if (Name.Equals("Length", StringComparison.OrdinalIgnoreCase) || Name.Equals("Count", StringComparison.OrdinalIgnoreCase))
            {
                // $null.Count should be 0, anything else should be 1
                var resultCount = PSObject.Base(target.Value) == null ? 0 : 1;
                return new DynamicMetaObject(
                    Expression.Condition(
                        Compiler.IsStrictMode(2),
                        ThrowPropertyNotFoundStrict(),
                        ExpressionCache.Constant(resultCount).Cast(typeof(object))), restrictions);
            }

            var result = Expression.Condition(
                Compiler.IsStrictMode(2),
                ThrowPropertyNotFoundStrict(),
                _nonEnumerating ? ExpressionCache.AutomationNullConstant : ExpressionCache.NullConstant);
            return new DynamicMetaObject(result, restrictions);
        }

        private Expression ThrowPropertyNotFoundStrict()
        {
            return Compiler.CreateThrow(typeof(object), typeof(PropertyNotFoundException),
                                        new[] { typeof(string), typeof(Exception), typeof(string), typeof(object[]) },
                                        "PropertyNotFoundStrict", null, ParserStrings.PropertyNotFoundStrict,
                                        new object[] { Name });
        }

        internal static DynamicMetaObject EnsureAllowedInLanguageMode(DynamicMetaObject target, object targetValue,
            string name, bool isStatic, DynamicMetaObject[] args, BindingRestrictions moreTests, string errorID, string resourceString)
        {
            var context = LocalPipeline.GetExecutionContextFromTLS();
            if (context == null)
            {
                return null;
            }

            if ((ExecutionContext.HasEverUsedConstrainedLanguage && context.LanguageMode == PSLanguageMode.ConstrainedLanguage) || 
                context.LanguageMode == PSLanguageMode.ConstrainedLanguageAudit)
            {
                if (!IsAllowedInConstrainedLanguage(targetValue, name, isStatic))
                {
                    if (context.LanguageMode == PSLanguageMode.ConstrainedLanguage)
                    {
                        return target.ThrowRuntimeError(args, moreTests, errorID, resourceString);
                    }

                    string targetName = (targetValue as Type)?.FullName;
                    SystemPolicy.LogWDACAuditMessage(
                        Title: "Parameter Binder",
                        Message: $"Method or Property {name} on type {targetName ?? string.Empty} invocation will not be allowed with policy enforcement.",
                        FQID:"MethodOrPropertyInvocationNotAllowed");
                }
            }

            return null;
        }

        internal static bool IsAllowedInConstrainedLanguage(object targetValue, string name, bool isStatic)
        {
            // ToString allowed on any type
            if (string.Equals(name, "ToString", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Otherwise, check if it's a core type
            Type targetType = targetValue as Type;
            if ((!isStatic) || (targetType == null))
            {
                targetType = targetValue.GetType();
            }

            return CoreTypes.Contains(targetType);
        }

        /// <summary>
        /// Return the binding restriction that tests that an instance member does not exist, used when the binder
        /// knows instance members might exist (because the name was added to some instance), but the object we're
        /// currently binding does not have an instance member with the given member name.
        /// </summary>
        internal BindingRestrictions NotInstanceMember(DynamicMetaObject target)
        {
            var memberInfoVar = Expression.Variable(typeof(PSMemberInfo));
            var expr = Expression.Call(CachedReflectionInfo.PSGetMemberBinder_TryGetInstanceMember,
                                       target.Expression.Cast(typeof(object)), Expression.Constant(Name), memberInfoVar);

            return BindingRestrictions.GetExpressionRestriction(Expression.Block(new[] { memberInfoVar }, Expression.Not(expr)));
        }

        private static Expression WrapGetMemberInTry(Expression expr)
        {
            // This code ensures that getting a member doesn't raise an exception.  Mostly this is so that formatting
            // always works.  As currently implemented, this will also affect C# code that uses the dynamic keyword.
            // If we decide that the dynamic keyword should not mask exceptions, then we should create a new binder
            // from PSObject.PSDynamicMetaObject.BindGetMember that passes in a flag so we know not to wrap in a try/catch.

            return Expression.TryCatch(
                expr.Cast(typeof(object)),
                Expression.Catch(typeof(TerminateException), Expression.Rethrow(typeof(object))),
                // Not sure if the following catch is necessary, but the interpreter has it.
                Expression.Catch(typeof(MethodException), Expression.Rethrow(typeof(object))),
                // This catch is only needed if we have an IDictionary
                Expression.Catch(typeof(PropertyNotFoundException), Expression.Rethrow(typeof(object))),
                Expression.Catch(typeof(Exception), ExpressionCache.NullConstant));
        }

        /// <summary>
        /// Resolve the alias, throwing an exception if a cycle is detected while resolving the alias.
        /// </summary>
        private PSMemberInfo ResolveAlias(PSAliasProperty alias, DynamicMetaObject target, HashSet<string> aliases,
            List<BindingRestrictions> aliasRestrictions)
        {
            Diagnostics.Assert(aliasRestrictions != null, "aliasRestrictions cannot be null");
            if (aliases == null)
            {
                aliases = new HashSet<string> { alias.Name };
            }
            else
            {
                if (aliases.Contains(alias.Name))
                {
                    throw new ExtendedTypeSystemException("CycleInAliasLookup", null, ExtendedTypeSystem.CycleInAlias, alias.Name);
                }

                aliases.Add(alias.Name);
            }

            bool canOptimize;
            Type aliasConversionType;
            BindingRestrictions restrictions;
            PSGetMemberBinder binder = PSGetMemberBinder.Get(alias.ReferencedMemberName, _classScope, false);
            // if binder has instance member, then GetPSMemberInfo will not be able to resolve that..only FallbackGetMember
            // can resolve that. In that case we simply return without further evaluation.
            if (binder.HasInstanceMember)
            {
                return null;
            }

            PSMemberInfo result = binder.GetPSMemberInfo(target, out restrictions, out canOptimize, out aliasConversionType,
                                                         MemberTypes.Property, aliases, aliasRestrictions);
            return result;
        }

        internal PSMemberInfo GetPSMemberInfo(DynamicMetaObject target,
                                              out BindingRestrictions restrictions,
                                              out bool canOptimize,
                                              out Type aliasConversionType,
                                              MemberTypes memberTypeToOperateOn,
                                              HashSet<string> aliases = null,
                                              List<BindingRestrictions> aliasRestrictions = null)
        {
            aliasConversionType = null;
            bool hasTypeTableMember;
            bool hasInstanceMember;
            BindingRestrictions versionRestriction;
            lock (this)
            {
                versionRestriction = BinderUtils.GetVersionCheck(this, _version);
                hasTypeTableMember = _hasTypeTableMember;
                hasInstanceMember = _hasInstanceMember;
            }

            if (_static)
            {
                restrictions = target.PSGetStaticMemberRestriction();
                restrictions = restrictions.Merge(versionRestriction);
                canOptimize = true;

                return PSObject.GetStaticCLRMember(target.Value, Name);
            }

            canOptimize = false;

            Diagnostics.Assert(!TryGetInstanceMember(target.Value, Name, out _),
                                "shouldn't get here if there is an instance member");

            PSMemberInfo memberInfo = null;
            ConsolidatedString typenames = null;
            var context = LocalPipeline.GetExecutionContextFromTLS();
            var typeTable = context?.TypeTable;

            if (hasTypeTableMember)
            {
                typenames = PSObject.GetTypeNames(target.Value);
                if (typeTable != null)
                {
                    memberInfo = typeTable.GetMembers<PSMemberInfo>(typenames)[Name];
                    if (memberInfo != null)
                    {
                        canOptimize = true;
                    }
                }
            }

            // Check if the target value is actually a deserialized PSObject.
            // - If so, we want to use the original value.
            //   Mostly, a deserialized object is a PSObject with an empty immediate base object, and it's OK to call PSObject.Base()
            //   on it in this case, because the method would just return the original PSObject. But if it's the deserialized object of
            //   a container object (i.e. an object derived from IEnumerable, IList, or IDictionary), the immediate base object is a
            //   Hashtable or ArrayList. In such case, we sometimes would lose the psadapted/psextended properties that we actually care
            //   by using the base object.
            //
            //   One example is the XmlElement, which derives from IEnumerable. It is serialized/deserialized as a container object, and
            //   the its element properties (i.e. $xmlElement.IP, where IP is actually an attribute name) are stored as psadapted properties
            //   in the top-level PSObject.
            //
            //   See the comments about 'three interesting cases' in PSInvokeMemberBinder.FallbackInvokeMember for more info.
            //
            // - If not, we want to use the base object, so that we might generate optimized code.
            var psobj = target.Value as PSObject;
            bool isTargetDeserializedObject = (psobj != null) && (psobj.IsDeserialized);
            object value = isTargetDeserializedObject ? target.Value : PSObject.Base(target.Value);

            var adapterSet = PSObject.GetMappedAdapter(value, typeTable);
            if (memberInfo == null)
            {
                canOptimize = adapterSet.OriginalAdapter.CanSiteBinderOptimize(memberTypeToOperateOn);
                // Don't bother looking for the member if we're not going to use it.
                if (canOptimize)
                {
                    memberInfo = adapterSet.OriginalAdapter.BaseGetMember<PSMemberInfo>(value, Name);
                }
            }

            if (memberInfo == null && canOptimize && adapterSet.DotNetAdapter != null)
            {
                memberInfo = adapterSet.DotNetAdapter.BaseGetMember<PSMemberInfo>(value, Name);
            }

            // The member came from the type table or an adapter and isn't instance based, so the restriction will start
            // with a version check
            restrictions = versionRestriction;

            // When returning aliasRestrictions always include the version restriction
            aliasRestrictions?.Add(versionRestriction);

            var alias = memberInfo as PSAliasProperty;
            if (alias != null)
            {
                aliasConversionType = alias.ConversionType;
                aliasRestrictions ??= new List<BindingRestrictions>();

                memberInfo = ResolveAlias(alias, target, aliases, aliasRestrictions);
                if (memberInfo == null)
                {
                    // this can happen in the cases where referenced name of the alias property
                    // maps to an adapter that cannot optimize (like ManagementObjectAdapter)
                    canOptimize = false;
                }

                // Merge alias restrictions
                foreach (var aliasRestriction in aliasRestrictions)
                {
                    restrictions = restrictions.Merge(aliasRestriction);
                }
            }

            if (_classScope != null && (target.LimitType == _classScope || target.LimitType.IsSubclassOf(_classScope)) && adapterSet.OriginalAdapter == PSObject.DotNetInstanceAdapter)
            {
                List<MethodBase> candidateMethods = null;
                foreach (var member in _classScope.GetMembers(BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic))
                {
                    if (this.Name.Equals(member.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        var propertyInfo = member as PropertyInfo;
                        if (propertyInfo != null)
                        {
                            var getMethod = propertyInfo.GetGetMethod(nonPublic: true);
                            var setMethod = propertyInfo.GetSetMethod(nonPublic: true);

                            if ((getMethod == null || getMethod.IsFamily || getMethod.IsPublic) &&
                                (setMethod == null || setMethod.IsFamily || setMethod.IsPublic))
                            {
                                memberInfo = new PSProperty(this.Name, PSObject.DotNetInstanceAdapter, target.Value, new DotNetAdapter.PropertyCacheEntry(propertyInfo));
                            }
                        }
                        else
                        {
                            var fieldInfo = member as FieldInfo;
                            if (fieldInfo != null)
                            {
                                if (fieldInfo.IsFamily)
                                {
                                    memberInfo = new PSProperty(this.Name, PSObject.DotNetInstanceAdapter, target.Value, new DotNetAdapter.PropertyCacheEntry(fieldInfo));
                                }
                            }
                            else
                            {
                                var methodInfo = member as MethodInfo;
                                if (methodInfo != null && (methodInfo.IsPublic || methodInfo.IsFamily))
                                {
                                    candidateMethods ??= new List<MethodBase>();

                                    candidateMethods.Add(methodInfo);
                                }
                            }
                        }
                    }
                }

                if (candidateMethods != null && candidateMethods.Count > 0)
                {
                    var psMethodInfo = memberInfo as PSMethod;
                    if (psMethodInfo != null)
                    {
                        var cacheEntry = (DotNetAdapter.MethodCacheEntry)psMethodInfo.adapterData;
                        candidateMethods.AddRange(cacheEntry.methodInformationStructures.Select(static e => e.method));
                        memberInfo = null;
                    }

                    if (memberInfo != null)
                    {
                        // Ambiguous, it'd be better to report an error other than "can't find member", but I'm lazy.
                        memberInfo = null;
                    }
                    else
                    {
                        DotNetAdapter.MethodCacheEntry method = new DotNetAdapter.MethodCacheEntry(candidateMethods);
                        memberInfo = PSMethod.Create(this.Name, PSObject.DotNetInstanceAdapter, null, method);
                    }
                }
            }

            if (hasInstanceMember)
            {
                // If this binder knows instance members exist, we need to make sure future objects going through this
                // rule ensure they don't have an instance member.  I don't expect this rule to be generated or hit frequently.
                restrictions = restrictions.Merge(NotInstanceMember(target));
            }

            // We always need a type check, even if we'll be using the PSTypeNames because our generated code may contain
            // conversions that don't work for arbitrary types.
            restrictions = restrictions.Merge(target.PSGetTypeRestriction());

            // If the target value is actually a deserialized PSObject, add a check to ensure that's the case. This check
            // should be done after the type check.
            if (isTargetDeserializedObject)
            {
                restrictions = restrictions.Merge(BindingRestrictions.GetExpressionRestriction(
                    Expression.Property(target.Expression.Cast(typeof(PSObject)), CachedReflectionInfo.PSObject_IsDeserialized)));
            }

            if (hasTypeTableMember)
            {
                // We need to make sure the type table we would use to find a member is the same type table that we used here to
                // find (or not find) a member.  If they were different type tables, we could easily get different results.
                restrictions = restrictions.Merge(
                    BindingRestrictions.GetInstanceRestriction(Expression.Call(CachedReflectionInfo.PSGetMemberBinder_GetTypeTableFromTLS), typeTable));

                // We also need to make sure the pstypename is the same.  It doesn't matter if we found something in the type table
                // or not - the fact that we might find something in the type table is enough to require a check on the pstypename.
                restrictions = restrictions.Merge(
                        BindingRestrictions.GetExpressionRestriction(
                            Expression.Call(CachedReflectionInfo.PSGetMemberBinder_IsTypeNameSame, target.Expression.Cast(typeof(object)), Expression.Constant(typenames.Key))));
            }

            return memberInfo;
        }

        #region Runtime helper methods

        internal static PSMemberInfo CloneMemberInfo(PSMemberInfo memberInfo, object obj)
        {
            memberInfo = memberInfo.Copy();
            memberInfo.ReplicateInstance(obj);
            return memberInfo;
        }

        internal static object GetAdaptedValue(object obj, string member)
        {
            var context = LocalPipeline.GetExecutionContextFromTLS();
            PSMemberInfo memberInfo = null;

            if ((context != null) && (context.TypeTable != null))
            {
                ConsolidatedString typenames = PSObject.GetTypeNames(obj);
                memberInfo = context.TypeTable.GetMembers<PSMemberInfo>(typenames)[member];
                if (memberInfo != null)
                {
                    memberInfo = CloneMemberInfo(memberInfo, obj);
                }
            }

            var adapterSet = PSObject.GetMappedAdapter(obj, context?.TypeTable);
            memberInfo ??= adapterSet.OriginalAdapter.BaseGetMember<PSMemberInfo>(obj, member);

            if (memberInfo == null && adapterSet.DotNetAdapter != null)
            {
                memberInfo = adapterSet.DotNetAdapter.BaseGetMember<PSMemberInfo>(obj, member);
            }

            if (memberInfo != null)
            {
                return memberInfo.Value;
            }

            if (string.Equals(member, "Length", StringComparison.OrdinalIgnoreCase) || string.Equals(member, "Count", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (context != null && context.IsStrictVersion(2))
            {
                // If the member is undefined and we're in strict mode, throw an exception...
                throw new PropertyNotFoundException("PropertyNotFoundStrict", null, ParserStrings.PropertyNotFoundStrict,
                                                    LanguagePrimitives.ConvertTo<string>(member));
            }

            return null;
        }

        internal static bool IsTypeNameSame(object value, string typeName)
        {
            return value != null && string.Equals(PSObject.GetTypeNames(value).Key, typeName, StringComparison.OrdinalIgnoreCase);
        }

        internal static TypeTable GetTypeTableFromTLS()
        {
            var executionContext = LocalPipeline.GetExecutionContextFromTLS();
            return executionContext?.TypeTable;
        }

        internal static bool TryGetInstanceMember(object value, string memberName, out PSMemberInfo memberInfo)
        {
            PSMemberInfoInternalCollection<PSMemberInfo> instanceMembers;
            memberInfo = PSObject.HasInstanceMembers(value, out instanceMembers) ? instanceMembers[memberName] : null;

            return (memberInfo != null);
        }

        internal static bool TryGetIDictionaryValue(IDictionary hash, string memberName, out object value)
        {
            try
            {
                if (hash.Contains(memberName))
                {
                    value = hash[memberName];
                    return true;
                }
            }
            catch (InvalidOperationException)
            {
                // Ignore invalid operation exception, it can happen if the dictionary
                // has keys that can't be compared to property.
            }

            value = null;
            return false;
        }

        internal static bool TryGetGenericDictionaryValue<T>(IDictionary<string, T> hash, string memberName, out object value)
        {
            T result;
            if (hash.TryGetValue(memberName, out result))
            {
                value = result;
                return true;
            }

            value = null;
            return false;
        }

        #endregion Runtime helper methods
    }

    /// <summary>
    /// The binder for setting a member, like $foo.bar = 1 or [foo]::bar = 1.
    /// </summary>
    internal class PSSetMemberBinder : SetMemberBinder
    {
        private sealed class KeyComparer : IEqualityComparer<PSSetMemberBinderKeyType>
        {
            public bool Equals(PSSetMemberBinderKeyType x, PSSetMemberBinderKeyType y)
            {
                // The non-static binder cache is case-sensitive because sites need the name used per site
                // when the target object is a case-sensitive IDictionary.  Under all other circumstances,
                // binding is case-insensitive.
                var stringComparison = x.Item3 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                return x.Item1.Equals(y.Item1, stringComparison) &&
                       x.Item2 == y.Item2 &&
                       x.Item3 == y.Item3;
            }

            public int GetHashCode(PSSetMemberBinderKeyType obj)
            {
                var stringComparer = obj.Item3 ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
                return Utils.CombineHashCodes(stringComparer.GetHashCode(obj.Item1),
                    obj.Item2 == null ? 0 : obj.Item2.GetHashCode(),
                    obj.Item3.GetHashCode());
            }
        }

        private static readonly Dictionary<PSSetMemberBinderKeyType, PSSetMemberBinder> s_binderCache
            = new Dictionary<PSSetMemberBinderKeyType, PSSetMemberBinder>(new KeyComparer());

        private readonly bool _static;
        private readonly Type _classScope;
        private readonly PSGetMemberBinder _getMemberBinder;

        public static PSSetMemberBinder Get(string memberName, TypeDefinitionAst classScopeAst, bool @static)
        {
            var classScope = classScopeAst?.Type;
            return Get(memberName, classScope, @static);
        }

        public static PSSetMemberBinder Get(string memberName, Type classScope, bool @static)
        {
            PSSetMemberBinder result;

            lock (s_binderCache)
            {
                var tuple = Tuple.Create(memberName, classScope, @static);
                if (!s_binderCache.TryGetValue(tuple, out result))
                {
                    result = new PSSetMemberBinder(memberName, true, @static, classScope);
                    s_binderCache.Add(tuple, result);
                }
            }

            return result;
        }

        public PSSetMemberBinder(string name, bool ignoreCase, bool @static, Type classScope)
            : base(name, ignoreCase)
        {
            _static = @static;
            _classScope = classScope;
            _getMemberBinder = PSGetMemberBinder.Get(name, _classScope, @static);
        }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "SetMember: {0}{1} ver:{2}",
                _static ? "static " : string.Empty,
                Name,
                _getMemberBinder._version);
        }

        private static Expression GetTransformedExpression(IEnumerable<ArgumentTransformationAttribute> transformationAttributes, Expression originalExpression)
        {
            if (transformationAttributes == null)
            {
                return originalExpression;
            }

            var attributesArray = transformationAttributes.ToArray();
            if (attributesArray.Length == 0)
            {
                return originalExpression;
            }

            Expression transformedExpression = originalExpression.Convert(typeof(object));
            var engineIntrinsicsTempVar = Expression.Variable(typeof(EngineIntrinsics));
            // apply transformation attributes from right to left
            for (int i = attributesArray.Length - 1; i >= 0; i--)
            {
                transformedExpression = Expression.Call(Expression.Constant(attributesArray[i]),
                                                CachedReflectionInfo.ArgumentTransformationAttribute_Transform,
                                                engineIntrinsicsTempVar,
                                                transformedExpression);
            }

            return Expression.Block(new[] { engineIntrinsicsTempVar },
                Expression.Assign(
                    engineIntrinsicsTempVar,
                    Expression.Property(ExpressionCache.GetExecutionContextFromTLS,
                                        CachedReflectionInfo.ExecutionContext_EngineIntrinsics)),
                transformedExpression);
        }

        public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue || !value.HasValue)
            {
                return Defer(target, value);
            }

            // Defer COM objects or arguments wrapped in PSObjects
            if ((target.Value is PSObject && (PSObject.Base(target.Value) != target.Value)) ||
                (value.Value is PSObject && (PSObject.Base(value.Value) != value.Value)))
            {
                object baseObject = PSObject.Base(target.Value);
                if (baseObject != null && Marshal.IsComObject(baseObject))
                {
                    // We unwrap only if the 'base' of 'target' is a COM object. It's unnecessary to unwrap in other cases,
                    // especially in the case that 'target' is a string, we would lose instance members on the PSObject.
                    // Therefore, we need to use a stricter restriction to make sure PSObject 'target' with other base types
                    // doesn't get unwrapped.
                    return this.DeferForPSObject(target, value, targetIsComObject: true).WriteToDebugLog(this);
                }
            }

            // Check if this is a COM Object
            DynamicMetaObject result;
            if (ComInterop.ComBinder.TryBindSetMember(this, target, value, out result))
            {
                return result.UpdateComRestrictionsForPsObject(new DynamicMetaObject[] { value }).WriteToDebugLog(this);
            }

            var targetValue = PSObject.Base(target.Value);
            if (targetValue == null)
            {
                return (target.ThrowRuntimeError(new[] { value }, BindingRestrictions.Empty, "PropertyNotFound",
                                                 ParserStrings.PropertyNotFound,
                                                 Expression.Constant(Name))).WriteToDebugLog(this);
            }

            if (value.Value == AutomationNull.Value)
            {
                // Pretend the value was null (so we actually use null as the expression, but be sure
                // to use a restriction that checks
                value = new DynamicMetaObject(ExpressionCache.NullConstant, value.PSGetTypeRestriction(), null);
            }

            PSMemberInfo memberInfo;
            if (_getMemberBinder.HasInstanceMember && PSGetMemberBinder.TryGetInstanceMember(target.Value, Name, out memberInfo))
            {
                // If there is an instance member, we generate (roughly) the following:
                //     PSMemberInfo memberInfo;
                //     if (PSGetMemberBinder.TryGetInstanceMember(target.Value, Name, out memberInfo))
                //         memberInfo.Value = value;
                //     else
                //         update the site
                // We use a generic method like this because:
                //   * If one object has an instance property with a given name, it's like many others do as well
                //   * We want to avoid generating new sites for every object with an instance member
                // As an alternative, would could generate the following psuedo-code:
                //     if (target.Value == previousInstance)
                //         return optimized value (depending on the exact PSMemberInfo subclass)
                //     else update the site
                // But the assumption here is that many sites probably performs worse than the dictionary lookup
                // and unoptimized virtual call to PSMemberInfo.Value.
                //
                // The binding restrictions could avoid a version check because it's never wrong to look for an instance member,
                // but we add the check because the DLR requires a non-empty check when the target implements IDynamicMetaObjectProvider,
                // which PSObject does.  The version check is also marginally useful if we knew we'd never see another
                // instance member with this member name, but we're not tracking things to make that a useful test.

                var memberInfoVar = Expression.Variable(typeof(PSMemberInfo));
                var temp = Expression.Variable(typeof(object));
                var expr = Expression.Condition(
                    Expression.Call(CachedReflectionInfo.PSGetMemberBinder_TryGetInstanceMember,
                                    target.Expression.Cast(typeof(object)), Expression.Constant(Name), memberInfoVar),
                    Expression.Assign(Expression.Property(memberInfoVar, "Value"), value.Expression.Cast(typeof(object))),
                    this.GetUpdateExpression(typeof(object)));
                var bindingRestrictions = BinderUtils.GetVersionCheck(_getMemberBinder, _getMemberBinder._version)
                    .Merge(value.PSGetTypeRestriction());

                return (new DynamicMetaObject(Expression.Block(new[] { memberInfoVar, temp }, expr),
                    bindingRestrictions)).WriteToDebugLog(this);
            }

            if (targetValue is IDictionary)
            {
                // We never look for properties in the underlying object, we always try to add the key.
                Type genericTypeArg = null;
                bool isGeneric = PSGetMemberBinder.IsGenericDictionary(targetValue, ref genericTypeArg);

                if (!isGeneric || genericTypeArg != null)
                {
                    // If it's a generic, we must convert our value to genericTypeArg.
                    var hashType = isGeneric
                        ? typeof(IDictionary<,>).MakeGenericType(typeof(string), genericTypeArg)
                        : typeof(IDictionary);
                    var mi = hashType.GetMethod("set_Item");

                    var temp = Expression.Variable(genericTypeArg ?? typeof(object));

                    bool debase;
                    Type elementType = temp.Type;

                    // ConstrainedLanguage note - Calls to this conversion are protected by the binding rules below
                    var conversion = LanguagePrimitives.FigureConversion(value.Value, elementType, out debase);
                    if (conversion.Rank != ConversionRank.None)
                    {
                        var valueExpr = PSConvertBinder.InvokeConverter(conversion, value.Expression, elementType,
                                                                        debase, ExpressionCache.InvariantCulture);
                        return new DynamicMetaObject(
                            Expression.Block(new[] { temp },
                                Expression.Assign(temp, valueExpr),
                                Expression.Call(PSGetMemberBinder.GetTargetExpr(target, hashType), mi,
                                                Expression.Constant(Name), valueExpr),
                                valueExpr.Cast(typeof(object))),
                            target.CombineRestrictions(value)).WriteToDebugLog(this);
                    }
                }
            }

            BindingRestrictions restrictions;
            bool canOptimize;
            Type aliasConversionType;
            memberInfo = _getMemberBinder.GetPSMemberInfo(target, out restrictions, out canOptimize, out aliasConversionType, MemberTypes.Property);

            restrictions = restrictions.Merge(value.PSGetTypeRestriction());

            // If the process has ever used ConstrainedLanguage, then we need to add the language mode
            // to the binding restrictions, and check whether it is allowed. We can't limit
            // the language check to unsafe types, as a safe type might have an unsafe method.
            if (ExecutionContext.HasEverUsedConstrainedLanguage)
            {
                restrictions = restrictions.Merge(BinderUtils.GetLanguageModeCheckIfHasEverUsedConstrainedLanguage());
            }

            // Validate that this is allowed in the current language mode.
            DynamicMetaObject runtimeError = PSGetMemberBinder.EnsureAllowedInLanguageMode(
                target, targetValue, Name, _static, new[] { value }, restrictions,
                "PropertySetterNotSupportedInConstrainedLanguage", ParserStrings.PropertySetConstrainedLanguage);
            if (runtimeError != null)
            {
                return runtimeError.WriteToDebugLog(this);
            }

            if (!canOptimize)
            {
                Diagnostics.Assert(memberInfo == null, "We don't bother returning members if we can't optimize.");
                return new DynamicMetaObject(
                    Expression.Call(CachedReflectionInfo.PSSetMemberBinder_SetAdaptedValue,
                                    PSGetMemberBinder.GetTargetExpr(target, typeof(object)),
                                    Expression.Constant(Name),
                                    value.Expression.Cast(typeof(object))),
                    restrictions).WriteToDebugLog(this);
            }

            if (memberInfo == null)
            {
                return (errorSuggestion ?? new DynamicMetaObject(
                    Compiler.ThrowRuntimeError("PropertyAssignmentException", ParserStrings.PropertyNotFound, this.ReturnType, Expression.Constant(Name)),
                    restrictions)).WriteToDebugLog(this);
            }

            var psPropertyInfo = memberInfo as PSPropertyInfo;
            if (psPropertyInfo != null)
            {
                if (!psPropertyInfo.IsSettable)
                {
                    return GeneratePropertyAssignmentException(restrictions).WriteToDebugLog(this);
                }

                var psProperty = psPropertyInfo as PSProperty;
                if (psProperty != null)
                {
                    var data = psProperty.adapterData as DotNetAdapter.PropertyCacheEntry;
                    if (data != null)
                    {
                        Expression expr;

                        if (data.member.DeclaringType.IsGenericTypeDefinition)
                        {
                            Expression innerException = Expression.New(
                                CachedReflectionInfo.SetValueException_ctor,
                                Expression.Constant("PropertyAssignmentException"),
                                Expression.Constant(null, typeof(Exception)),
                                Expression.Constant(ExtendedTypeSystem.CannotInvokeStaticMethodOnUninstantiatedGenericType),
                                Expression.NewArrayInit(typeof(object), Expression.Constant(data.member.DeclaringType.FullName)));

                            expr = Compiler.ThrowRuntimeErrorWithInnerException(
                                "PropertyAssignmentException",
                                Expression.Constant(ExtendedTypeSystem.CannotInvokeStaticMethodOnUninstantiatedGenericType),
                                innerException,
                                this.ReturnType,
                                Expression.Constant(data.member.DeclaringType.FullName));

                            return new DynamicMetaObject(expr, restrictions).WriteToDebugLog(this);
                        }

                        if (data.propertyType.IsByRefLike)
                        {
                            // In theory, it's possible to call the setter with a value that can be implicitly/explicitly casted to the target ByRef-like type.
                            // However, the set-property/set-indexer semantics in PowerShell requires returning the value after the setting operation. We cannot
                            // return a ByRef-like value back, so we just disallow setting a member that takes a ByRef-like type value.
                            expr = Expression.Throw(
                                Expression.New(
                                    CachedReflectionInfo.SetValueException_ctor,
                                    Expression.Constant(nameof(ExtendedTypeSystem.CannotAccessByRefLikePropertyOrField)),
                                    Expression.Constant(null, typeof(Exception)),
                                    Expression.Constant(ExtendedTypeSystem.CannotAccessByRefLikePropertyOrField),
                                    Expression.NewArrayInit(
                                        typeof(object),
                                        Expression.Constant(data.member.Name),
                                        Expression.Constant(data.propertyType, typeof(Type)))),
                                this.ReturnType);

                            return new DynamicMetaObject(expr, restrictions).WriteToDebugLog(this);
                        }

                        var propertyInfo = data.member as PropertyInfo;
                        Expression lhs;
                        Type lhsType;

                        // Populate transformation attributes.
                        // Order of attributes is the same as order provided by user in the code
                        // We assume that GetCustomAttributes implemented that way.
                        IEnumerable<ArgumentTransformationAttribute> argumentTransformationAttributes =
                            data.member.GetCustomAttributes<ArgumentTransformationAttribute>();
                        bool transformationNeeded = argumentTransformationAttributes.Any();

                        // For static property access, the target expr must be null.  For non-static, we must convert
                        // because target.Expression is typeof(object) because this is a dynamic site.
                        var targetExpr = _static ? null : PSGetMemberBinder.GetTargetExpr(target, data.member.DeclaringType);
                        if (propertyInfo != null)
                        {
                            if (propertyInfo.SetMethod.IsFamily &&
                                (_classScope == null || !_classScope.IsSubclassOf(propertyInfo.DeclaringType)))
                            {
                                return GeneratePropertyAssignmentException(restrictions).WriteToDebugLog(this);
                            }

                            lhsType = propertyInfo.PropertyType;
                            lhs = Expression.Property(targetExpr, propertyInfo);
                        }
                        else
                        {
                            Diagnostics.Assert(data.member is FieldInfo,
                                                "A DotNetAdapter.PropertyCacheEntry has something other than PropertyInfo or FieldInfo.");
                            var fieldInfo = (FieldInfo)data.member;
                            lhsType = fieldInfo.FieldType;
                            lhs = Expression.Field(targetExpr, fieldInfo);
                        }

                        var nullableUnderlyingType = Nullable.GetUnderlyingType(lhsType);
                        if (nullableUnderlyingType != null)
                        {
                            if (value.Value == null)
                            {
                                expr = Expression.Block(
                                    Expression.Assign(lhs, GetTransformedExpression(argumentTransformationAttributes, Expression.Constant(null, lhsType))),
                                    ExpressionCache.NullConstant);
                            }
                            else
                            {
                                var tmp = Expression.Variable(nullableUnderlyingType);
                                Expression assignmentExpression;
                                if (transformationNeeded)
                                {
                                    var transformedExpr = GetTransformedExpression(argumentTransformationAttributes, value.Expression);
                                    assignmentExpression = DynamicExpression.Dynamic(PSConvertBinder.Get(nullableUnderlyingType), nullableUnderlyingType, transformedExpr);
                                }
                                else
                                {
                                    assignmentExpression = value.CastOrConvert(nullableUnderlyingType);
                                }

                                expr = Expression.Block(
                                    new[] { tmp },
                                    Expression.Assign(tmp, assignmentExpression),
                                    Expression.Assign(lhs, Expression.New(lhsType.GetConstructor(new[] { nullableUnderlyingType }), tmp)),
                                    tmp.Cast(typeof(object)));
                            }
                        }
                        else
                        {
                            var tmp = Expression.Variable(lhsType);
                            Expression assignedValue;
                            if (transformationNeeded)
                            {
                                assignedValue = DynamicExpression.Dynamic(PSConvertBinder.Get(lhsType), lhsType,
                                   GetTransformedExpression(argumentTransformationAttributes, value.Expression));
                            }
                            else
                            {
                                assignedValue = (lhsType == typeof(object) && value.LimitType == typeof(PSObject))
                                                           ? Expression.Call(CachedReflectionInfo.PSObject_Base, value.Expression.Cast(typeof(PSObject)))
                                                           : value.CastOrConvert(lhsType);
                            }

                            expr = Expression.Block(
                                new[] { tmp },
                                Expression.Assign(tmp, assignedValue),
                                Expression.Assign(lhs, tmp),
                                tmp.Cast(typeof(object)));
                        }

                        var e = Expression.Variable(typeof(Exception));
                        expr = Expression.TryCatch(expr.Cast(typeof(object)),
                            Expression.Catch(e,
                                Expression.Block(
                                    Expression.Call(CachedReflectionInfo.ExceptionHandlingOps_ConvertToMethodInvocationException,
                                                    e,
                                                    Expression.Constant(typeof(SetValueInvocationException), typeof(Type)),
                                                    Expression.Constant(Name),
                                                    ExpressionCache.Constant(0),
                                                    Expression.Constant(null, typeof(MemberInfo))),
                                    Expression.Rethrow(typeof(object)))));
                        return new DynamicMetaObject(expr, restrictions).WriteToDebugLog(this);
                    }
                }

                var codeProperty = psPropertyInfo as PSCodeProperty;
                if (codeProperty != null)
                {
                    var setterMethod = codeProperty.SetterCodeReference;
                    var parameters = setterMethod.GetParameters();
                    var propertyType = parameters[parameters.Length - 1].ParameterType;

                    if (propertyType.IsByRefLike)
                    {
                        var expr = Expression.Throw(
                            Expression.New(
                                CachedReflectionInfo.SetValueException_ctor,
                                Expression.Constant(nameof(ExtendedTypeSystem.CannotAccessByRefLikePropertyOrField)),
                                Expression.Constant(null, typeof(Exception)),
                                Expression.Constant(ExtendedTypeSystem.CannotAccessByRefLikePropertyOrField),
                                Expression.NewArrayInit(
                                    typeof(object),
                                    Expression.Constant(codeProperty.Name),
                                    Expression.Constant(propertyType, typeof(Type)))),
                            this.ReturnType);

                        return new DynamicMetaObject(expr, restrictions).WriteToDebugLog(this);
                    }

                    var temp = Expression.Variable(typeof(object));
                    return new DynamicMetaObject(
                        Expression.Block(
                            new[] { temp },
                            Expression.Assign(temp, value.CastOrConvert(temp.Type)),
                            PSInvokeMemberBinder.InvokeMethod(
                                setterMethod,
                                target: null,
                                new[] { target, value },
                                expandParameters: false,
                                PSInvokeMemberBinder.MethodInvocationType.Setter),
                            temp),
                        restrictions).WriteToDebugLog(this);
                }

                var scriptProperty = psPropertyInfo as PSScriptProperty;
                if (scriptProperty != null)
                {
                    // Invoke Setter

                    return new DynamicMetaObject(
                        Expression.Call(Expression.Constant(scriptProperty, typeof(PSScriptProperty)),
                                        CachedReflectionInfo.PSScriptProperty_InvokeSetter,
                                        PSGetMemberBinder.GetTargetExpr(target), value.Expression.Cast(typeof(object))),
                        restrictions).WriteToDebugLog(this);
                }

                Diagnostics.Assert(false, "The property we're trying to set was unexpected.");
            }

            if (errorSuggestion != null)
            {
                return errorSuggestion.WriteToDebugLog(this);
            }

            // If we get here, the property isn't settable.  Call SetAdaptedValue, which will eventually call the setter and raise an exception
            // with a suitable error message.
            return new DynamicMetaObject(
                Expression.Call(CachedReflectionInfo.PSSetMemberBinder_SetAdaptedValue,
                                PSGetMemberBinder.GetTargetExpr(target, typeof(object)), Expression.Constant(Name),
                                value.Expression.Cast(typeof(object))),
                restrictions).WriteToDebugLog(this);
        }

        private DynamicMetaObject GeneratePropertyAssignmentException(BindingRestrictions restrictions)
        {
            Expression innerException = Expression.New(CachedReflectionInfo.SetValueException_ctor,
                Expression.Constant("PropertyAssignmentException"),
                Expression.Constant(null, typeof(Exception)),
                Expression.Constant(ParserStrings.PropertyIsReadOnly),
                Expression.NewArrayInit(typeof(object), Expression.Constant(Name)));
            var expr = Compiler.ThrowRuntimeErrorWithInnerException("PropertyAssignmentException",
                Expression.Constant(ParserStrings.PropertyIsReadOnly), innerException,
                this.ReturnType, Expression.Constant(Name));
            return new DynamicMetaObject(expr, restrictions);
        }

        internal static object SetAdaptedValue(object obj, string member, object value)
        {
            try
            {
                var context = LocalPipeline.GetExecutionContextFromTLS();
                PSMemberInfo memberInfo = null;

                if ((context != null) && (context.TypeTable != null))
                {
                    ConsolidatedString typenames = PSObject.GetTypeNames(obj);
                    memberInfo = context.TypeTable.GetMembers<PSMemberInfo>(typenames)[member];
                    if (memberInfo != null)
                    {
                        memberInfo = PSGetMemberBinder.CloneMemberInfo(memberInfo, obj);
                    }
                }

                var adapterSet = PSObject.GetMappedAdapter(obj, context?.TypeTable);
                memberInfo ??= adapterSet.OriginalAdapter.BaseGetMember<PSMemberInfo>(obj, member);

                if (memberInfo == null && adapterSet.DotNetAdapter != null)
                {
                    memberInfo = adapterSet.DotNetAdapter.BaseGetMember<PSMemberInfo>(obj, member);
                }

                if (memberInfo != null)
                {
                    memberInfo.Value = value;
                }
                else
                {
                    throw InterpreterError.NewInterpreterException(null, typeof(RuntimeException),
                                                                   null, "PropertyAssignmentException", ParserStrings.PropertyNotFound, member);
                }

                return value;
            }
            catch (SetValueException)
            {
                throw;
            }
            catch (Exception e)
            {
                ExceptionHandlingOps.ConvertToMethodInvocationException(e, typeof(SetValueInvocationException), member, 0);
                throw;
            }
        }

        internal static void InvalidateCache()
        {
            // Invalidate regular binders
            lock (s_binderCache)
            {
                foreach (PSSetMemberBinder binder in s_binderCache.Values)
                {
                    binder._getMemberBinder._version += 1;
                }
            }
        }
    }

    internal class PSInvokeBinder : InvokeBinder
    {
        internal PSInvokeBinder(CallInfo callInfo) : base(callInfo)
        {
        }

        public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            return errorSuggestion ?? target.ThrowRuntimeError(args, BindingRestrictions.Empty, "CannotInvoke", ParserStrings.CannotInvoke);
        }
    }

    internal sealed class PSInvokeMemberBinder : InvokeMemberBinder
    {
        internal enum MethodInvocationType
        {
            Ordinary,
            Setter,
            Getter,
            BaseCtor,
            NonVirtual,
        }

        private sealed class KeyComparer : IEqualityComparer<PSInvokeMemberBinderKeyType>
        {
            public bool Equals(PSInvokeMemberBinderKeyType x, PSInvokeMemberBinderKeyType y)
            {
                return x.Item1.Equals(y.Item1, StringComparison.OrdinalIgnoreCase)
                       && x.Item2.Equals(y.Item2)
                       && x.Item3 == y.Item3
                       && x.Item4 == y.Item4
                       && ((x.Item5 == null) ? (y.Item5 == null) : x.Item5.Equals(y.Item5))
                       && x.Item6 == y.Item6
                       && x.Item7 == y.Item7;
            }

            public int GetHashCode(PSInvokeMemberBinderKeyType obj)
            {
                return Utils.CombineHashCodes(
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1),
                    obj.Item2.GetHashCode(),
                    obj.Item3.GetHashCode(),
                    obj.Item4.GetHashCode(),
                    obj.Item5 == null ? 0 : obj.Item5.GetHashCode(),
                    obj.Item6.GetHashCode(),
                    obj.Item7 == null ? 0 : obj.Item7.GetHashCode());
            }
        }

        private static readonly
            Dictionary<PSInvokeMemberBinderKeyType, PSInvokeMemberBinder> s_binderCache = new Dictionary<PSInvokeMemberBinderKeyType, PSInvokeMemberBinder>(new KeyComparer());

        internal readonly PSMethodInvocationConstraints _invocationConstraints;
        internal readonly PSGetMemberBinder _getMemberBinder;

        private PSInvokeMemberBinder GetNonEnumeratingBinder()
        {
            return Get(Name, _classScope, CallInfo, @static: false, propertySetter: _propertySetter, nonEnumerating: true, constraints: _invocationConstraints);
        }

        public static PSInvokeMemberBinder Get(string memberName, CallInfo callInfo, bool @static, bool propertySetter,
                                               PSMethodInvocationConstraints constraints, Type classScope)
        {
            return Get(memberName, classScope, callInfo, @static, propertySetter, nonEnumerating: false, constraints: constraints);
        }

        private static PSInvokeMemberBinder Get(string memberName, Type classScope, CallInfo callInfo, bool @static, bool propertySetter,
                                                bool nonEnumerating, PSMethodInvocationConstraints constraints)
        {
            PSInvokeMemberBinder result;

            lock (s_binderCache)
            {
                var key = Tuple.Create(memberName, callInfo, propertySetter, nonEnumerating, constraints, @static, classScope);
                if (!s_binderCache.TryGetValue(key, out result))
                {
                    result = new PSInvokeMemberBinder(memberName, true, @static, propertySetter, nonEnumerating, callInfo, constraints, classScope);
                    s_binderCache.Add(key, result);
                }
            }

            return result;
        }

        private readonly bool _static;
        private readonly bool _propertySetter;
        private readonly bool _nonEnumerating;
        private readonly Type _classScope;

        private PSInvokeMemberBinder(string name,
                                     bool ignoreCase,
                                     bool @static,
                                     bool propertySetter,
                                     bool nonEnumerating,
                                     CallInfo callInfo,
                                     PSMethodInvocationConstraints invocationConstraints,
                                     Type classScope)
            : base(name, ignoreCase, callInfo)
        {
            _static = @static;
            _propertySetter = propertySetter;
            _nonEnumerating = nonEnumerating;
            this._invocationConstraints = invocationConstraints;
            _classScope = classScope;
            this._getMemberBinder = PSGetMemberBinder.Get(name, classScope, @static);
        }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "PSInvokeMember: {0}{1}{2} ver:{3} args:{4} constraints:<{5}>",
                _static ? "static " : string.Empty,
                _propertySetter ? "propset " : string.Empty,
                Name,
                _getMemberBinder._version,
                CallInfo.ArgumentCount,
                _invocationConstraints != null ? _invocationConstraints.ToString() : string.Empty);
        }

        public override DynamicMetaObject FallbackInvokeMember(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue || args.Any(static arg => !arg.HasValue))
            {
                return Defer(args.Prepend(target).ToArray());
            }

            // Defer COM objects or arguments wrapped in PSObjects
            if ((target.Value is PSObject && (PSObject.Base(target.Value) != target.Value)) ||
                args.Any(static mo => mo.Value is PSObject && (PSObject.Base(mo.Value) != mo.Value)))
            {
                object baseObject = PSObject.Base(target.Value);
                if (baseObject != null && Marshal.IsComObject(baseObject))
                {
                    // We unwrap only if the 'base' of 'target' is a COM object. It's unnecessary to unwrap in other cases,
                    // especially in the case that 'target' is a string, we would lose instance members on the PSObject.
                    // Therefore, we need to use a stricter restriction to make sure other type of PSObject 'target'
                    // doesn't get unwrapped.
                    return this.DeferForPSObject(args.Prepend(target).ToArray(), targetIsComObject: true).WriteToDebugLog(this);
                }
            }

            // Check if this is a COM Object
            DynamicMetaObject result;
            if (ComInterop.ComBinder.TryBindInvokeMember(this, _propertySetter, target, args, out result))
            {
                return result.UpdateComRestrictionsForPsObject(args).WriteToDebugLog(this);
            }

            var targetValue = PSObject.Base(target.Value);
            if (targetValue == null)
            {
                if (!_static && !_nonEnumerating)
                {
                    // As discussed with Bruce, the Where/ForEach operators should work on $null and return an empty collection.
                    // e.g. $null.Where{"I didn't run"} should return an empty collection
                    DynamicMetaObject emptyEnumerator = new DynamicMetaObject(
                        Expression.Call(Expression.NewArrayInit(typeof(object)), CachedReflectionInfo.IEnumerable_GetEnumerator),
                        BindingRestrictions.GetInstanceRestriction(Expression.Call(CachedReflectionInfo.PSObject_Base, target.Expression), null))
                        .WriteToDebugLog(this);
                    BindingRestrictions argRestrictions = args.Aggregate(BindingRestrictions.Empty, static (current, arg) => current.Merge(arg.PSGetMethodArgumentRestriction()));

                    if (string.Equals(Name, "Where", StringComparison.OrdinalIgnoreCase))
                    {
                        return InvokeWhereOnCollection(emptyEnumerator, args, argRestrictions).WriteToDebugLog(this);
                    }

                    if (string.Equals(Name, "ForEach", StringComparison.OrdinalIgnoreCase))
                    {
                        return InvokeForEachOnCollection(emptyEnumerator, args, argRestrictions).WriteToDebugLog(this);
                    }
                }

                return target.ThrowRuntimeError(args, BindingRestrictions.Empty, "InvokeMethodOnNull", ParserStrings.InvokeMethodOnNull).WriteToDebugLog(this);
            }

            PSMemberInfo memberInfo;
            if (_getMemberBinder.HasInstanceMember && PSGetMemberBinder.TryGetInstanceMember(target.Value, Name, out memberInfo))
            {
                // If there is an instance member, we generate (roughly) the following:
                //     PSMethodInfo methodInfo;
                //     if (PSInvokeMemberBinder.TryGetInstanceMethod(target.Value, Name, out methodInfoInfo))
                //         return methodInfo.Invoke(target, args);
                //     else
                //         update the site
                // We use a generic method like this because:
                //   * If one object has an instance property with a given name, it's like many others do as well
                //   * We want to avoid generating new sites for every object with an instance member
                // As an alternative, would could generate the following psuedo-code:
                //     if (target.Value == previousInstance)
                //         return optimized value (depending on the exact PSMemberInfo subclass)
                //     else update the site
                // But the assumption here is that many sites probably performs worse than the dictionary lookup
                // and unoptimized virtual call to PSMemberInfo.Value.
                //
                // The binding restrictions could avoid a version check because it's never wrong to look for an instance member,
                // but we add the check because the DLR requires a non-empty check when the target implements IDynamicMetaObjectProvider,
                // which PSObject does.  The version check is also marginally useful if we knew we'd never see another
                // instance member with this member name, but we're not tracking things to make that a useful test.

                var methodInfoVar = Expression.Variable(typeof(PSMethodInfo));
                var expr = Expression.Condition(
                    Expression.Call(CachedReflectionInfo.PSInvokeMemberBinder_TryGetInstanceMethod,
                                    target.Expression.Cast(typeof(object)), Expression.Constant(Name), methodInfoVar),
                    Expression.Call(methodInfoVar, CachedReflectionInfo.PSMethodInfo_Invoke,
                                    Expression.NewArrayInit(typeof(object), args.Select(static dmo => dmo.Expression.Cast(typeof(object))))),
                    this.GetUpdateExpression(typeof(object)));

                return (new DynamicMetaObject(Expression.Block(new[] { methodInfoVar }, expr),
                    BinderUtils.GetVersionCheck(_getMemberBinder, _getMemberBinder._version))).WriteToDebugLog(this);
            }

            BindingRestrictions restrictions;
            bool canOptimize;
            Type aliasConversionType;
            var methodInfo = _getMemberBinder.GetPSMemberInfo(target, out restrictions, out canOptimize, out aliasConversionType, MemberTypes.Method) as PSMethodInfo;
            restrictions = args.Aggregate(restrictions, static (current, arg) => current.Merge(arg.PSGetMethodArgumentRestriction()));

            // If the process has ever used ConstrainedLanguage, then we need to add the language mode
            // to the binding restrictions, and check whether it is allowed. We can't limit
            // the language check to unsafe types, as a safe type might have an unsafe method.
            if (ExecutionContext.HasEverUsedConstrainedLanguage)
            {
                restrictions = restrictions.Merge(BinderUtils.GetLanguageModeCheckIfHasEverUsedConstrainedLanguage());
            }

            // Validate that this is allowed in the current language mode.
            DynamicMetaObject runtimeError = PSGetMemberBinder.EnsureAllowedInLanguageMode(
                target, targetValue, Name, _static, args, restrictions,
                "MethodInvocationNotSupportedInConstrainedLanguage", ParserStrings.InvokeMethodConstrainedLanguage);
            if (runtimeError != null)
            {
                return runtimeError.WriteToDebugLog(this);
            }

            if (!canOptimize)
            {
                Diagnostics.Assert(methodInfo == null, "We don't bother returning members if we can't optimize.");

                Expression call;
                if (_propertySetter)
                {
                    call = Expression.Call(
                        CachedReflectionInfo.PSInvokeMemberBinder_InvokeAdaptedSetMember,
                        PSGetMemberBinder.GetTargetExpr(target, typeof(object)),
                        Expression.Constant(Name),
                        Expression.NewArrayInit(typeof(object),
                                                args.Take(args.Length - 1).Select(static arg => arg.Expression.Cast(typeof(object)))),
                        args.Last().Expression.Cast(typeof(object)));
                }
                else
                {
                    call = Expression.Call(
                        CachedReflectionInfo.PSInvokeMemberBinder_InvokeAdaptedMember,
                        PSGetMemberBinder.GetTargetExpr(target, typeof(object)),
                        Expression.Constant(Name),
                        Expression.NewArrayInit(typeof(object),
                                                args.Select(static arg => arg.Expression.Cast(typeof(object)))));
                }

                return new DynamicMetaObject(call, restrictions).WriteToDebugLog(this);
            }

            // If the target value is a PSObject and its base object happens to be a Hashtable or ArrayList,
            // we might have three interesting cases here:
            //  (1) the target value could be a regular PSObject that wraps the Hashtable/ArrayList, i.e. $target = [PSObject]::AsPSObject($hash)
            //  (2) the target value could be a deserialized object (PSObject) with the 'IsDeserialized' property to be false, i.e. deserialized Hashtable/ArrayList/Dictionary[string, string]
            //  (3) the target value could be a deserialized object (PSObject) with the 'IsDeserialized' property to be true, i.e. deserialized XmlElement
            // For the first two cases, it's OK to call a .NET method from the base object, such as $target.Add().
            // For the third case, calling a .NET method from the base object is incorrect, because the original type of the deserialized object doesn't have the method.
            //  example: XmlElement derives from IEnumerable, so it's treated as a container object when powershell does the serialization -- using an ArrayList to hold
            //  its elements -- but we cannot call Add() on it.
            //
            // We add restriction to do this check only if the methodInfo is a .NET method/parameterizedProperty, otherwise it's not affected by the above cases, for example, a PSScriptMethod
            // defined in the TypeTable will only get affected by the PSTypeNames.
            if (methodInfo is PSMethod || methodInfo is PSParameterizedProperty)
            {
                var psObj = target.Value as PSObject;
                if (psObj != null && (targetValue.GetType() == typeof(Hashtable) || targetValue.GetType() == typeof(ArrayList)))
                {
                    // If we get here, then the target value should have 'isDeserialized == false', otherwise we cannot get a .NET methodInfo
                    // from _getMemberBinder.GetPSMemberInfo(). This is because when 'isDeserialized' is true, we use the PSObject to find the
                    // corresponding Adapter -- PSObjectAdapter, which cannot be optimized.
                    Diagnostics.Assert(!psObj.IsDeserialized,
                        "isDeserialized should be false, because if not, we cannot get a .NET method/parameterizedProperty from GetPSMemberInfo");

                    restrictions = restrictions.Merge(BindingRestrictions.GetExpressionRestriction(
                        Expression.Not(Expression.Property(target.Expression.Cast(typeof(PSObject)), CachedReflectionInfo.PSObject_IsDeserialized))));
                }
            }

            var psMethod = methodInfo as PSMethod;
            if (psMethod != null)
            {
                var data = (DotNetAdapter.MethodCacheEntry)psMethod.adapterData;

                return InvokeDotNetMethod(CallInfo, Name, _invocationConstraints, _propertySetter ? MethodInvocationType.Setter : MethodInvocationType.Ordinary, target, args, restrictions,
                    data.methodInformationStructures, typeof(MethodException)).WriteToDebugLog(this);
            }

            var scriptMethod = methodInfo as PSScriptMethod;
            if (scriptMethod != null)
            {
                return new DynamicMetaObject(
                    Expression.Call(CachedReflectionInfo.PSScriptMethod_InvokeScript,
                                    Expression.Constant(Name),
                                    Expression.Constant(scriptMethod.Script),
                                    target.Expression.Cast(typeof(object)),
                                    Expression.NewArrayInit(typeof(object),
                                                            args.Select(static e => e.Expression.Cast(typeof(object))))),
                    restrictions).WriteToDebugLog(this);
            }

            var codeMethod = methodInfo as PSCodeMethod;
            if (codeMethod != null)
            {
                Expression expr = InvokeMethod(codeMethod.CodeReference, null, args.Prepend(target).ToArray(), false, MethodInvocationType.Ordinary);
                if (codeMethod.CodeReference.ReturnType == typeof(void))
                {
                    expr = Expression.Block(expr, ExpressionCache.AutomationNullConstant);
                }
                else
                {
                    expr = expr.Cast(typeof(object));
                }

                return new DynamicMetaObject(expr, restrictions).WriteToDebugLog(this);
            }

            var parameterizedProperty = methodInfo as PSParameterizedProperty;
            if (parameterizedProperty != null)
            {
                var p = (DotNetAdapter.ParameterizedPropertyCacheEntry)parameterizedProperty.adapterData;
                return InvokeDotNetMethod(CallInfo, Name, _invocationConstraints, _propertySetter ? MethodInvocationType.Setter : MethodInvocationType.Ordinary, target, args, restrictions,
                    _propertySetter ? p.setterInformation : p.getterInformation,
                    _propertySetter ? typeof(SetValueInvocationException) : typeof(GetValueInvocationException)).WriteToDebugLog(this);
            }

            if (errorSuggestion != null)
            {
                return errorSuggestion.WriteToDebugLog(this);
            }

            // See comment on PSGetMemberBinder.PropertyDoesntExistCheckSpecialCases - the same applies here for method calls.
            if (!_static && !_nonEnumerating && target.Value != AutomationNull.Value)
            {
                // Invoking Where and ForEach operators on collections.
                if (string.Equals(Name, "Where", StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeWhereOnCollection(target, args, restrictions).WriteToDebugLog(this);
                }

                if (string.Equals(Name, "ForEach", StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeForEachOnCollection(target, args, restrictions).WriteToDebugLog(this);
                }

                var enumerableTarget = PSEnumerableBinder.IsEnumerable(target);
                if (enumerableTarget != null)
                {
                    // Try calling the method on each member of the collection.
                    return InvokeMemberOnCollection(enumerableTarget, args, targetValue.GetType(), restrictions).WriteToDebugLog(this);
                }
            }

            var typeForMessage = _static && targetValue is Type ? (Type)targetValue : targetValue.GetType();
            return new DynamicMetaObject(
                Compiler.ThrowRuntimeError(ParserOps.MethodNotFoundErrorId, ParserStrings.MethodNotFound,
                                           Expression.Constant(typeForMessage.FullName), Expression.Constant(Name)),
                restrictions).WriteToDebugLog(this);
        }

        internal static DynamicMetaObject InvokeDotNetMethod(
            CallInfo callInfo,
            string name,
            PSMethodInvocationConstraints psMethodInvocationConstraints,
            MethodInvocationType methodInvocationType,
            DynamicMetaObject target,
            DynamicMetaObject[] args,
            BindingRestrictions restrictions,
            MethodInformation[] mi,
            Type errorExceptionType)
        {
            bool expandParamsOnBest;
            bool callNonVirtually;
            string errorId = null;
            string errorMsg = null;
            int numArgs = args.Length;
            if (methodInvocationType == MethodInvocationType.Setter)
            {
                numArgs -= 1;
            }

            object[] argValues = new object[numArgs];
            for (int i = 0; i < numArgs; ++i)
            {
                object arg = args[i].Value;
                argValues[i] = arg == AutomationNull.Value ? null : arg;
            }

            var result = Adapter.FindBestMethod(
                mi,
                psMethodInvocationConstraints,
                allowCastingToByRefLikeType: true,
                argValues,
                ref errorId,
                ref errorMsg,
                out expandParamsOnBest,
                out callNonVirtually);

            if (callNonVirtually && methodInvocationType != MethodInvocationType.BaseCtor)
            {
                methodInvocationType = MethodInvocationType.NonVirtual;
            }

            if (result != null)
            {
                var methodInfo = result.method;
                var expr = InvokeMethod(methodInfo, target, args, expandParamsOnBest, methodInvocationType);
                if (expr.Type == typeof(void))
                {
                    expr = Expression.Block(expr, ExpressionCache.AutomationNullConstant);
                }

                // Expression block runs two expressions in order:
                //  - Log method invocation to AMSI Notifications (can throw PSSecurityException)
                //  - Invoke method
                string targetName = methodInfo.ReflectedType?.FullName ?? string.Empty;
                expr = Expression.Block(
                    Expression.Call(
                        CachedReflectionInfo.MemberInvocationLoggingOps_LogMemberInvocation,
                        Expression.Constant(targetName),
                        Expression.Constant(name),
                        Expression.NewArrayInit(
                            typeof(object),
                            args.Select(static e => e.Expression.Cast(typeof(object))))),
                    expr);

                // If we're calling SteppablePipeline.{Begin|Process|End}, we don't want
                // to wrap exceptions - this is very much a special case to help error
                // propagation and ensure errors are attributed to the correct code (the
                // cmdlet invoked, not the method call from some proxy.)
                if (methodInfo.DeclaringType == typeof(SteppablePipeline)
                    && (methodInfo.Name.Equals("Begin", StringComparison.Ordinal))
                        || methodInfo.Name.Equals("Process", StringComparison.Ordinal)
                        || methodInfo.Name.Equals("End", StringComparison.Ordinal))
                {
                    return new DynamicMetaObject(expr, restrictions);
                }

                expr = expr.Cast(typeof(object));

                // Likewise, when calling methods in types defined by PowerShell, we don't
                // want to wrap the exception.
                if (methodInfo.DeclaringType.Assembly.GetCustomAttributes(typeof(DynamicClassImplementationAssemblyAttribute)).Any())
                {
                    return new DynamicMetaObject(expr, restrictions);
                }

                var e = Expression.Variable(typeof(Exception));
                expr = Expression.TryCatch(expr,
                    Expression.Catch(e,
                        Expression.Block(
                            expr.Type,
                            Expression.Call(CachedReflectionInfo.ExceptionHandlingOps_ConvertToMethodInvocationException,
                                            e,
                                            Expression.Constant(errorExceptionType, typeof(Type)),
                                            Expression.Constant(methodInfo.Name),
                                            ExpressionCache.Constant(args.Length),
                                            Expression.Constant(methodInfo, typeof(MethodBase))),
                            Expression.Rethrow(expr.Type))));

                return new DynamicMetaObject(expr, restrictions);
            }

            return new DynamicMetaObject(
                Compiler.CreateThrow(typeof(object), errorExceptionType,
                                     new[] { typeof(string), typeof(Exception), typeof(string), typeof(object[]) },
                                     errorId, null, errorMsg, new object[] { name, callInfo.ArgumentCount }),
                restrictions);
        }

        public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target,
                                                         DynamicMetaObject[] args,
                                                         DynamicMetaObject errorSuggestion)
        {
            return (errorSuggestion ?? new DynamicMetaObject(
                DynamicExpression.Dynamic(
                    new PSInvokeBinder(CallInfo),
                    typeof(object),
                    args.Prepend(target).Select(static dmo => dmo.Expression)
                ),
                target.Restrictions.Merge(BindingRestrictions.Combine(args))
            ));
        }

        internal static MethodInfo FindBestMethod(DynamicMetaObject target,
                                                  IEnumerable<DynamicMetaObject> args,
                                                  string methodName,
                                                  bool @static,
                                                  PSMethodInvocationConstraints invocationConstraints)
        {
            MethodInfo result = null;

            var psMethod = PSObject.DotNetInstanceAdapter.GetDotNetMethod<PSMethod>(PSObject.Base(target.Value), methodName);
            if (psMethod != null)
            {
                var data = (DotNetAdapter.MethodCacheEntry)psMethod.adapterData;

                string errorId = null;
                string errorMsg = null;
                bool expandParameters;
                bool callNonVirtually;

                var mi = Adapter.FindBestMethod(
                    data.methodInformationStructures,
                    invocationConstraints,
                    allowCastingToByRefLikeType: true,
                    args.Select(static arg => arg.Value == AutomationNull.Value ? null : arg.Value).ToArray(),
                    ref errorId,
                    ref errorMsg,
                    out expandParameters,
                    out callNonVirtually);

                if (mi != null)
                {
                    result = (MethodInfo)mi.method;
                }
            }

            return result;
        }

        internal static Expression InvokeMethod(MethodBase mi, DynamicMetaObject target, DynamicMetaObject[] args, bool expandParameters, MethodInvocationType invocationType)
        {
            List<ParameterExpression> temps = new List<ParameterExpression>();
            List<Expression> initTemps = new List<Expression>();
            List<Expression> copyOutTemps = new List<Expression>();

            ConstructorInfo constructorInfo = null;
            MethodInfo methodInfo = mi as MethodInfo;
            if (methodInfo != null)
            {
                Type returnType = methodInfo.ReturnType;
                if (returnType.IsByRefLike)
                {
                    ConstructorInfo exceptionCtorInfo;
                    switch (invocationType)
                    {
                        case MethodInvocationType.Getter:
                            exceptionCtorInfo = CachedReflectionInfo.GetValueException_ctor;
                            break;
                        case MethodInvocationType.Setter:
                            exceptionCtorInfo = CachedReflectionInfo.SetValueException_ctor;
                            break;
                        default:
                            exceptionCtorInfo = CachedReflectionInfo.MethodException_ctor;
                            break;
                    }

                    return Expression.Throw(
                        Expression.New(
                            exceptionCtorInfo,
                            Expression.Constant(nameof(ExtendedTypeSystem.CannotCallMethodWithByRefLikeReturnType)),
                            Expression.Constant(null, typeof(Exception)),
                            Expression.Constant(ExtendedTypeSystem.CannotCallMethodWithByRefLikeReturnType),
                            Expression.NewArrayInit(
                                typeof(object),
                                Expression.Constant(methodInfo.Name),
                                Expression.Constant(returnType, typeof(Type)))),
                        typeof(object));
                }
            }
            else
            {
                constructorInfo = (ConstructorInfo)mi;
                Type declaringType = constructorInfo.DeclaringType;
                if (declaringType.IsByRefLike)
                {
                    return Expression.Throw(
                        Expression.New(
                            CachedReflectionInfo.MethodException_ctor,
                            Expression.Constant(nameof(ExtendedTypeSystem.CannotInstantiateBoxedByRefLikeType)),
                            Expression.Constant(null, typeof(Exception)),
                            Expression.Constant(ExtendedTypeSystem.CannotInstantiateBoxedByRefLikeType),
                            Expression.NewArrayInit(
                                typeof(object),
                                Expression.Constant(declaringType, typeof(Type)))),
                        typeof(object));
                }
            }

            // Invoking a base constructor or a base method (non-virtual call) depends reflection invocation
            // via helper methods, and thus all arguments need to be casted to 'object'. The ByRef-like types
            // cannot be boxed and won't work with reflection.
            bool allowCastingToByRefLikeType =
                invocationType != MethodInvocationType.BaseCtor &&
                invocationType != MethodInvocationType.NonVirtual;
            var parameters = mi.GetParameters();
            var argExprs = new Expression[parameters.Length];

            for (int i = 0; i < parameters.Length; ++i)
            {
                Type parameterType = parameters[i].ParameterType;
                string paramName = parameters[i].Name;
                if (string.IsNullOrWhiteSpace(paramName))
                {
                    paramName = i.ToString(CultureInfo.InvariantCulture);
                }

                // The extension method 'CustomAttributeExtensions.GetCustomAttributes(ParameterInfo, Type, Boolean)' has inconsistent
                // behavior on its return value in both FullCLR and CoreCLR. According to MSDN, if the attribute cannot be found, it
                // should return an empty collection. However, it returns null in some rare cases [when the parameter isn't backed by
                // actual metadata].
                // This inconsistent behavior affects OneCore powershell because we are using the extension method here when compiling
                // against CoreCLR. So we need to add a null check until this is fixed in CLR.
                var paramArrayAttrs = parameters[i].GetCustomAttributes(typeof(ParamArrayAttribute), false);
                if (paramArrayAttrs != null && paramArrayAttrs.Length > 0)
                {
                    Diagnostics.Assert(i == parameters.Length - 1, "vararg parameter is not the last");
                    var paramElementType = parameterType.GetElementType();

                    if (expandParameters)
                    {
                        argExprs[i] = Expression.NewArrayInit(
                            paramElementType,
                            args.Skip(i).Select(
                                a => a.CastOrConvertMethodArgument(
                                    paramElementType,
                                    paramName,
                                    mi.Name,
                                    allowCastingToByRefLikeType: false,
                                    temps,
                                    initTemps)));
                    }
                    else
                    {
                        var arg = args[i].CastOrConvertMethodArgument(
                            parameterType,
                            paramName,
                            mi.Name,
                            allowCastingToByRefLikeType: false,
                            temps,
                            initTemps);
                        argExprs[i] = arg;
                    }
                }
                else if (i >= args.Length)
                {
                    Diagnostics.Assert(parameters[i].IsOptional,
                        "if there are too few arguments, FindBestMethod should only succeed if parameters are optional");
                    var argValue = parameters[i].DefaultValue;
                    if (argValue == null)
                    {
                        argExprs[i] = Expression.Default(parameterType);
                    }
                    else
                    {
                        // We don't specify the parameter type in the constant expression. Normally the default
                        // argument's type should match the parameter type, but sometimes it won't, e.g. with COM,
                        // the default can be System.Reflection.Missing.Value and this value is handled specially.
                        argExprs[i] = Expression.Constant(argValue);
                    }
                }
                else
                {
                    if (parameterType.IsByRef)
                    {
                        if (args[i].Value is not PSReference)
                        {
                            return Compiler.CreateThrow(typeof(object), typeof(MethodException),
                                     new[] { typeof(string), typeof(Exception), typeof(string), typeof(object[]) },
                                     "NonRefArgumentToRefParameterMsg", null, ExtendedTypeSystem.NonRefArgumentToRefParameter,
                                     new object[] { i + 1, typeof(PSReference).FullName, "[ref]" });
                        }

                        var temp = Expression.Variable(parameterType.GetElementType());
                        temps.Add(temp);
                        var psRefValue = Expression.Property(args[i].Expression.Cast(typeof(PSReference)), CachedReflectionInfo.PSReference_Value);
                        initTemps.Add(Expression.Assign(temp, psRefValue.Convert(temp.Type)));
                        copyOutTemps.Add(Expression.Assign(psRefValue, temp.Cast(typeof(object))));
                        argExprs[i] = temp;
                    }
                    else
                    {
                        argExprs[i] = args[i].CastOrConvertMethodArgument(
                            parameterType,
                            paramName,
                            mi.Name,
                            allowCastingToByRefLikeType,
                            temps,
                            initTemps);
                    }
                }
            }

            Expression call;
            if (constructorInfo != null)
            {
                if (invocationType == MethodInvocationType.BaseCtor)
                {
                    var targetExpr = target.Value is PSObject
                        ? target.Expression.Cast(constructorInfo.DeclaringType)
                        : PSGetMemberBinder.GetTargetExpr(target, constructorInfo.DeclaringType);
                    call = Expression.Call(
                        CachedReflectionInfo.ClassOps_CallBaseCtor,
                        targetExpr,
                        Expression.Constant(constructorInfo, typeof(ConstructorInfo)),
                        Expression.NewArrayInit(typeof(object), argExprs.Select(static x => x.Cast(typeof(object)))));
                }
                else
                {
                    call = Expression.New(constructorInfo, argExprs);
                }
            }
            else
            {
                if (invocationType == MethodInvocationType.NonVirtual && !methodInfo.IsStatic)
                {
                    call = Expression.Call(
                        methodInfo.ReturnType == typeof(void)
                            ? CachedReflectionInfo.ClassOps_CallVoidMethodNonVirtually
                            : CachedReflectionInfo.ClassOps_CallMethodNonVirtually,
                        PSGetMemberBinder.GetTargetExpr(target, methodInfo.DeclaringType),
                        Expression.Constant(methodInfo, typeof(MethodInfo)),
                        Expression.NewArrayInit(typeof(object), argExprs.Select(static x => x.Cast(typeof(object)))));
                }
                else
                {
                    call = methodInfo.IsStatic
                           ? Expression.Call(methodInfo, argExprs)
                           : Expression.Call(
                               PSGetMemberBinder.GetTargetExpr(target, methodInfo.DeclaringType),
                               methodInfo, argExprs);
                }
            }

            if (temps.Count > 0)
            {
                if (call.Type != typeof(void) && copyOutTemps.Count > 0)
                {
                    var retValue = Expression.Variable(call.Type);
                    temps.Add(retValue);
                    call = Expression.Assign(retValue, call);
                    copyOutTemps.Add(retValue);
                }

                call = Expression.Block(call.Type, temps, initTemps.Append(call).Concat(copyOutTemps));
            }

            return call;
        }

        private DynamicMetaObject InvokeMemberOnCollection(DynamicMetaObject targetEnumerator, DynamicMetaObject[] args, Type typeForMessage, BindingRestrictions restrictions)
        {
            var d = DynamicExpression.Dynamic(this, this.ReturnType, args.Select(static a => a.Expression).Prepend(ExpressionCache.NullConstant));
            return new DynamicMetaObject(
                Expression.Call(CachedReflectionInfo.EnumerableOps_MethodInvoker,
                                Expression.Constant(this.GetNonEnumeratingBinder()),
                                Expression.Constant(d.DelegateType, typeof(Type)),
                                targetEnumerator.Expression,
                                Expression.NewArrayInit(typeof(object),
                                                        args.Select(static a => a.Expression.Cast(typeof(object)))),
                                Expression.Constant(typeForMessage, typeof(Type))
                                ),
                targetEnumerator.Restrictions.Merge(restrictions));
        }

        private static DynamicMetaObject GetTargetAsEnumerable(DynamicMetaObject target)
        {
            var enumerableTarget = PSEnumerableBinder.IsEnumerable(target);
            // If null wrap the target in an array.
            enumerableTarget ??= PSEnumerableBinder.IsEnumerable(
                new DynamicMetaObject(
                    Expression.NewArrayInit(typeof(object), target.Expression.Cast(typeof(object))),
                    target.GetSimpleTypeRestriction()));

            return enumerableTarget;
        }

        /// <param name="target">The target to operate against.</param>
        /// <param name="args">
        ///     Arguments to the operator. The first argument must be either a scriptblock
        ///     or a string representing a 'simple where' expression. The second is an enum that controls
        ///     the matching behaviour returning the first, last or all matching elements.
        /// </param>
        /// <param name="argRestrictions">The binding restrictions for the arguments.</param>
        private DynamicMetaObject InvokeWhereOnCollection(DynamicMetaObject target, DynamicMetaObject[] args, BindingRestrictions argRestrictions)
        {
            var lhsEnumerator = GetTargetAsEnumerable(target);

            switch (args.Length)
            {
                case 1:
                    return new DynamicMetaObject(
                            Expression.Call(CachedReflectionInfo.EnumerableOps_Where,
                                lhsEnumerator.Expression,
                                PSGetMemberBinder.GetTargetExpr(args[0]).Convert(typeof(ScriptBlock)),
                                Expression.Constant(WhereOperatorSelectionMode.Default),
                                Expression.Constant(0)),
                            lhsEnumerator.Restrictions.Merge(argRestrictions));
                case 2:
                    return new DynamicMetaObject(
                            Expression.Call(CachedReflectionInfo.EnumerableOps_Where,
                                lhsEnumerator.Expression,
                                PSGetMemberBinder.GetTargetExpr(args[0]).Convert(typeof(ScriptBlock)),
                                PSGetMemberBinder.GetTargetExpr(args[1]).Convert(typeof(WhereOperatorSelectionMode)),
                                Expression.Constant(0)),
                            lhsEnumerator.Restrictions.Merge(argRestrictions));
                case 3:
                    return new DynamicMetaObject(
                        Expression.Call(CachedReflectionInfo.EnumerableOps_Where,
                            lhsEnumerator.Expression,
                            PSGetMemberBinder.GetTargetExpr(args[0]).Convert(typeof(ScriptBlock)),
                            PSGetMemberBinder.GetTargetExpr(args[1]).Convert(typeof(WhereOperatorSelectionMode)),
                            PSGetMemberBinder.GetTargetExpr(args[2]).Convert(typeof(int))),
                        lhsEnumerator.Restrictions.Merge(argRestrictions));
                default:
                    {
                        // If the arity is wrong, throw the extended type system exception.
                        return new DynamicMetaObject(
                            Expression.Throw(
                                Expression.New(CachedReflectionInfo.MethodException_ctor,
                                    Expression.Constant("MethodCountCouldNotFindBest"),
                                    Expression.Constant(null, typeof(Exception)),
                                    Expression.Constant(ExtendedTypeSystem.MethodArgumentCountException),
                                    Expression.NewArrayInit(typeof(object),
                                        Expression.Constant(".Where({ expression } [, mode [, numberToReturn]])").Cast(typeof(object)),
                                        ExpressionCache.Constant(args.Length).Cast(typeof(object)))),
                                this.ReturnType),
                            lhsEnumerator.Restrictions.Merge(argRestrictions));
                    }
            }
        }

        private DynamicMetaObject InvokeForEachOnCollection(DynamicMetaObject targetEnumerator, DynamicMetaObject[] args, BindingRestrictions restrictions)
        {
            targetEnumerator = GetTargetAsEnumerable(targetEnumerator);

            if (args.Length < 1)
            {
                // If the arity is wrong, throw the extended type system exception.
                return new DynamicMetaObject(
                    Expression.Throw(
                        Expression.New(CachedReflectionInfo.MethodException_ctor,
                            Expression.Constant("MethodCountCouldNotFindBest"),
                            Expression.Constant(null, typeof(Exception)),
                            Expression.Constant(ExtendedTypeSystem.MethodArgumentCountException),
                            Expression.NewArrayInit(typeof(object),
                                Expression.Constant(".ForEach(expression [, arguments...])").Cast(typeof(object)),
                                ExpressionCache.Constant(args.Length).Cast(typeof(object)))),
                        this.ReturnType),
                    targetEnumerator.Restrictions.Merge(restrictions));
            }

            var lhsEnumerator = PSEnumerableBinder.IsEnumerable(targetEnumerator).Expression;
            Expression argsToPass;
            if (args.Length > 1)
            {
                argsToPass = Expression.NewArrayInit(typeof(object),
                                      args.Skip(1).Select(static a => a.Expression.Cast(typeof(object))));
            }
            else
            {
                argsToPass = Expression.NewArrayInit(typeof(object));
            }

            return new DynamicMetaObject(
                Expression.Call(CachedReflectionInfo.EnumerableOps_ForEach,
                            lhsEnumerator, PSGetMemberBinder.GetTargetExpr(args[0], typeof(object)), argsToPass),
                targetEnumerator.Restrictions.Merge(restrictions));
        }

        #region Runtime helpers

        internal static bool IsHomogeneousArray<T>(object[] args)
        {
            if (args.Length == 0)
            {
                return false;
            }

            foreach (object element in args)
            {
                if (Adapter.GetObjectType(element, debase: true) != typeof(T))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool IsHeterogeneousArray(object[] args)
        {
            if (args.Length == 0)
            {
                return true;
            }

            var firstElement = PSObject.Base(args[0]);
            if (firstElement == null)
            {
                return true;
            }

            var firstType = firstElement.GetType();
            if (firstType.Equals(typeof(object)))
            {
                // When the effective argument type is object[], it's for one of 2 reasons
                //     * the array contains elements with different types
                //     * the array contains elements that are all object
                // Arrays of all object is rare, which is why this method is called IsHeterogeneousArray,
                // but we still want to correctly handle object[] full of objects, so we return true
                // for that case as well.
                return true;
            }

            for (int i = 1; i < args.Length; i++)
            {
                if (Adapter.GetObjectType(args[i], debase: true) != firstType)
                {
                    return true;
                }
            }

            return false;
        }

        internal static object InvokeAdaptedMember(object obj, string methodName, object[] args)
        {
            var context = LocalPipeline.GetExecutionContextFromTLS();
            var adapterSet = PSObject.GetMappedAdapter(obj, context?.TypeTable);
            var methodInfo = adapterSet.OriginalAdapter.BaseGetMember<PSMemberInfo>(obj, methodName) as PSMethodInfo;
            if (methodInfo == null && adapterSet.DotNetAdapter != null)
            {
                methodInfo = adapterSet.DotNetAdapter.BaseGetMember<PSMemberInfo>(obj, methodName) as PSMethodInfo;
            }

            if (methodInfo != null)
            {
                return methodInfo.Invoke(args);
            }

            // The object doesn't have 'Where' and 'ForEach' methods.
            // As a last resort, we invoke 'Where' and 'ForEach' operators on singletons like
            //    ([pscustomobject]@{ foo = 'bar' }).Foreach({$_})
            //    ([pscustomobject]@{ foo = 'bar' }).Where({1})
            if (string.Equals(methodName, "Where", StringComparison.OrdinalIgnoreCase))
            {
                var enumerator = (new object[] { obj }).GetEnumerator();
                switch (args.Length)
                {
                    case 1:
                        return EnumerableOps.Where(enumerator, args[0] as ScriptBlock, WhereOperatorSelectionMode.Default, 0);
                    case 2:
                        return EnumerableOps.Where(enumerator, args[0] as ScriptBlock,
                                                   LanguagePrimitives.ConvertTo<WhereOperatorSelectionMode>(args[1]), 0);
                    case 3:
                        return EnumerableOps.Where(enumerator, args[0] as ScriptBlock,
                                                   LanguagePrimitives.ConvertTo<WhereOperatorSelectionMode>(args[1]), LanguagePrimitives.ConvertTo<int>(args[2]));
                }
            }

            if (string.Equals(methodName, "Foreach", StringComparison.OrdinalIgnoreCase))
            {
                var enumerator = (new object[] { obj }).GetEnumerator();
                object[] argsToPass;

                if (args.Length > 1)
                {
                    int length = args.Length - 1;
                    argsToPass = new object[length];
                    Array.Copy(args, sourceIndex: 1, argsToPass, destinationIndex: 0, length: length);
                }
                else
                {
                    argsToPass = Array.Empty<object>();
                }

                return EnumerableOps.ForEach(enumerator, args[0], argsToPass);
            }

            throw InterpreterError.NewInterpreterException(methodName, typeof(RuntimeException), null,
                "MethodNotFound", ParserStrings.MethodNotFound, ParserOps.GetTypeFullName(obj), methodName);
        }

        internal static object InvokeAdaptedSetMember(object obj, string methodName, object[] args, object valueToSet)
        {
            var context = LocalPipeline.GetExecutionContextFromTLS();
            var adapterSet = PSObject.GetMappedAdapter(obj, context?.TypeTable);
            var methodInfo = adapterSet.OriginalAdapter.BaseGetMember<PSParameterizedProperty>(obj, methodName);
            if (methodInfo == null && adapterSet.DotNetAdapter != null)
            {
                methodInfo = adapterSet.DotNetAdapter.BaseGetMember<PSParameterizedProperty>(obj, methodName);
            }

            if (methodInfo != null)
            {
                methodInfo.InvokeSet(valueToSet, args);
                return valueToSet;
            }

            throw InterpreterError.NewInterpreterException(methodName, typeof(RuntimeException), null,
                "MethodNotFound", ParserStrings.MethodNotFound, ParserOps.GetTypeFullName(obj), methodName);
        }

        internal static bool TryGetInstanceMethod(object value, string memberName, out PSMethodInfo methodInfo)
        {
            PSMemberInfoInternalCollection<PSMemberInfo> instanceMembers;
            var memberInfo = PSObject.HasInstanceMembers(value, out instanceMembers) ? instanceMembers[memberName] : null;
            methodInfo = memberInfo as PSMethodInfo;
            if (memberInfo == null)
            {
                // No member, just return false
                return false;
            }

            if (methodInfo == null)
            {
                // Found a member, but it wasn't a method, throw an exception because we can't call it.
                throw InterpreterError.NewInterpreterException(memberName, typeof(RuntimeException), null,
                    "MethodNotFound", ParserStrings.MethodNotFound, ParserOps.GetTypeFullName(value), memberName);
            }

            return true;
        }

        internal static void InvalidateCache()
        {
            // Invalidate regular binders
            lock (s_binderCache)
            {
                foreach (PSInvokeMemberBinder binder in s_binderCache.Values)
                {
                    binder._getMemberBinder._version += 1;
                }
            }
        }

        #endregion
    }

    internal class PSCreateInstanceBinder : CreateInstanceBinder
    {
        private readonly CallInfo _callInfo;
        private readonly PSMethodInvocationConstraints _constraints;
        private readonly bool _publicTypeOnly;
        private int _version;

        private sealed class KeyComparer : IEqualityComparer<Tuple<CallInfo, PSMethodInvocationConstraints, bool>>
        {
            public bool Equals(Tuple<CallInfo, PSMethodInvocationConstraints, bool> x,
                               Tuple<CallInfo, PSMethodInvocationConstraints, bool> y)
            {
                return x.Item1.Equals(y.Item1)
                       && ((x.Item2 == null) ? (y.Item2 == null) : x.Item2.Equals(y.Item2))
                       && x.Item3 == y.Item3;
            }

            public int GetHashCode(Tuple<CallInfo, PSMethodInvocationConstraints, bool> obj)
            {
                return obj.GetHashCode();
            }
        }

        private static readonly
            Dictionary<Tuple<CallInfo, PSMethodInvocationConstraints, bool>, PSCreateInstanceBinder>
            s_binderCache =
                new Dictionary<Tuple<CallInfo, PSMethodInvocationConstraints, bool>, PSCreateInstanceBinder>(new KeyComparer());

        public static PSCreateInstanceBinder Get(CallInfo callInfo, PSMethodInvocationConstraints constraints, bool publicTypeOnly = false)
        {
            PSCreateInstanceBinder result;

            lock (s_binderCache)
            {
                var key = Tuple.Create(callInfo, constraints, publicTypeOnly);
                if (!s_binderCache.TryGetValue(key, out result))
                {
                    result = new PSCreateInstanceBinder(callInfo, constraints, publicTypeOnly);
                    s_binderCache.Add(key, result);
                }
            }

            return result;
        }

        internal static void InvalidateCache()
        {
            // Invalidate binders
            lock (s_binderCache)
            {
                foreach (var binder in s_binderCache.Values)
                {
                    binder._version += 1;
                }
            }
        }

        internal PSCreateInstanceBinder(CallInfo callInfo, PSMethodInvocationConstraints constraints, bool publicTypeOnly)
            : base(callInfo)
        {
            _publicTypeOnly = publicTypeOnly;
            _callInfo = callInfo;
            _constraints = constraints;
        }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "PSCreateInstanceBinder: ver:{0} args:{1} constraints:<{2}>",
                _version,
                _callInfo.ArgumentCount,
                _constraints != null ? _constraints.ToString() : string.Empty);
        }

        public override DynamicMetaObject FallbackCreateInstance(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue || args.Any(static arg => !arg.HasValue))
            {
                return Defer(args.Prepend(target).ToArray());
            }

            var targetValue = PSObject.Base(target.Value);
            if (targetValue == null)
            {
                return target.ThrowRuntimeError(args, BindingRestrictions.Empty, "InvokeMethodOnNull", ParserStrings.InvokeMethodOnNull).WriteToDebugLog(this);
            }

            var instanceType = targetValue as Type ?? targetValue.GetType();

            BindingRestrictions restrictions;
            if (instanceType.IsByRefLike)
            {
                // ByRef-like types are not boxable and should be used only on stack
                restrictions = BindingRestrictions.GetExpressionRestriction(
                    Expression.Call(CachedReflectionInfo.PSCreateInstanceBinder_IsTargetTypeByRefLike, target.Expression));

                return target.ThrowRuntimeError(
                    restrictions,
                    nameof(ExtendedTypeSystem.CannotInstantiateBoxedByRefLikeType),
                    ExtendedTypeSystem.CannotInstantiateBoxedByRefLikeType,
                    Expression.Call(
                        CachedReflectionInfo.PSCreateInstanceBinder_GetTargetTypeName,
                        target.Expression)).WriteToDebugLog(this);
            }

            if (_publicTypeOnly && !TypeResolver.IsPublic(instanceType))
            {
                // If 'publicTypeOnly' specified, we only support creating instance for public types.
                restrictions = BindingRestrictions.GetExpressionRestriction(
                        Expression.Call(CachedReflectionInfo.PSCreateInstanceBinder_IsTargetTypeNonPublic, target.Expression));

                return target.ThrowRuntimeError(
                    restrictions,
                    nameof(ParserStrings.MethodNotFound),
                    ParserStrings.MethodNotFound,
                    Expression.Call(
                        CachedReflectionInfo.PSCreateInstanceBinder_GetTargetTypeName,
                        target.Expression),
                    Expression.Constant("new")).WriteToDebugLog(this);
            }

            var ctors = instanceType.GetConstructors();
            restrictions = ReferenceEquals(instanceType, targetValue)
                               ? (target.Value is PSObject)
                                     ? BindingRestrictions.GetInstanceRestriction(Expression.Call(CachedReflectionInfo.PSObject_Base, target.Expression), instanceType)
                                     : BindingRestrictions.GetInstanceRestriction(target.Expression, instanceType)
                               : target.PSGetTypeRestriction();
            restrictions = restrictions.Merge(BinderUtils.GetOptionalVersionAndLanguageCheckForType(this, instanceType, _version));
            if (ctors.Length == 0 && _callInfo.ArgumentCount == 0 && instanceType.IsValueType)
            {
                // No ctors, just call the default ctor
                return new DynamicMetaObject(Expression.New(instanceType).Cast(this.ReturnType), restrictions).WriteToDebugLog(this);
            }

            var context = LocalPipeline.GetExecutionContextFromTLS();
            if (context != null && context.LanguageMode == PSLanguageMode.ConstrainedLanguage && context.LanguageMode == PSLanguageMode.ConstrainedLanguageAudit &&
                !CoreTypes.Contains(instanceType))
            {
                if (context.LanguageMode == PSLanguageMode.ConstrainedLanguage)
                {
                    return target.ThrowRuntimeError(restrictions, "CannotCreateTypeConstrainedLanguage", ParserStrings.CannotCreateTypeConstrainedLanguage).WriteToDebugLog(this);
                }

                string targetName = instanceType?.FullName;
                SystemPolicy.LogWDACAuditMessage(
                    Title: "Parameter Binder",
                    Message: $"Will not be able to create type {targetName ?? string.Empty} during binding with policy enforcement.",
                    FQID:"BinderTypeCreationNotAllowed");
            }

            restrictions = args.Aggregate(restrictions, static (current, arg) => current.Merge(arg.PSGetMethodArgumentRestriction()));
            var newConstructors = DotNetAdapter.GetMethodInformationArray(ctors);
            return PSInvokeMemberBinder.InvokeDotNetMethod(_callInfo, "new", _constraints, PSInvokeMemberBinder.MethodInvocationType.Ordinary,
                                                           target, args, restrictions, newConstructors, typeof(MethodException)).WriteToDebugLog(this);
        }

        /// <summary>
        /// Check if the target type is ByRef-like.
        /// </summary>
        internal static bool IsTargetTypeByRefLike(object target)
        {
            var targetValue = PSObject.Base(target);
            if (targetValue == null) { return false; }

            var instanceType = targetValue as Type ?? targetValue.GetType();
            return instanceType.IsByRefLike;
        }

        /// <summary>
        /// Check if the target type is not public.
        /// </summary>
        internal static bool IsTargetTypeNonPublic(object target)
        {
            var targetValue = PSObject.Base(target);
            if (targetValue == null) { return false; }

            var instanceType = targetValue as Type ?? targetValue.GetType();
            return !TypeResolver.IsPublic(instanceType);
        }

        /// <summary>
        /// Return the full name of the target type.
        /// </summary>
        internal static string GetTargetTypeName(object target)
        {
            var targetValue = PSObject.Base(target);
            Diagnostics.Assert(targetValue != null, "caller makes sure target is not null");

            var instanceType = targetValue as Type ?? targetValue.GetType();
            return instanceType.FullName;
        }
    }

    internal class PSInvokeBaseCtorBinder : InvokeMemberBinder
    {
        private readonly CallInfo _callInfo;
        private readonly PSMethodInvocationConstraints _constraints;

        private sealed class KeyComparer : IEqualityComparer<Tuple<CallInfo, PSMethodInvocationConstraints>>
        {
            public bool Equals(Tuple<CallInfo, PSMethodInvocationConstraints> x,
                               Tuple<CallInfo, PSMethodInvocationConstraints> y)
            {
                return x.Item1.Equals(y.Item1)
                       && ((x.Item2 == null) ? (y.Item2 == null) : x.Item2.Equals(y.Item2));
            }

            public int GetHashCode(Tuple<CallInfo, PSMethodInvocationConstraints> obj)
            {
                return obj.GetHashCode();
            }
        }

        private static readonly
            Dictionary<Tuple<CallInfo, PSMethodInvocationConstraints>, PSInvokeBaseCtorBinder>
            s_binderCache =
                new Dictionary<Tuple<CallInfo, PSMethodInvocationConstraints>, PSInvokeBaseCtorBinder>(new KeyComparer());

        public static PSInvokeBaseCtorBinder Get(CallInfo callInfo, PSMethodInvocationConstraints constraints)
        {
            PSInvokeBaseCtorBinder result;

            lock (s_binderCache)
            {
                var key = Tuple.Create(callInfo, constraints);
                if (!s_binderCache.TryGetValue(key, out result))
                {
                    result = new PSInvokeBaseCtorBinder(callInfo, constraints);
                    s_binderCache.Add(key, result);
                }
            }

            return result;
        }

        internal PSInvokeBaseCtorBinder(CallInfo callInfo, PSMethodInvocationConstraints constraints)
            : base(".ctor", false, callInfo)
        {
            _callInfo = callInfo;
            _constraints = constraints;
        }

        public override DynamicMetaObject FallbackInvokeMember(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue || args.Any(static arg => !arg.HasValue))
            {
                return Defer(args.Prepend(target).ToArray());
            }

            var ctors = _constraints.MethodTargetType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var restrictions = target.Value is PSObject
                ? BindingRestrictions.GetTypeRestriction(target.Expression, target.Value.GetType())
                : target.PSGetTypeRestriction();
            restrictions = args.Aggregate(restrictions, static (current, arg) => current.Merge(arg.PSGetMethodArgumentRestriction()));
            var newConstructors = DotNetAdapter.GetMethodInformationArray(ctors.Where(static c => c.IsPublic || c.IsFamily).ToArray());
            return PSInvokeMemberBinder.InvokeDotNetMethod(_callInfo, "new", _constraints, PSInvokeMemberBinder.MethodInvocationType.BaseCtor,
                                                           target, args, restrictions, newConstructors, typeof(MethodException));
        }

        public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            return (errorSuggestion ?? new DynamicMetaObject(
                DynamicExpression.Dynamic(
                    new PSInvokeBinder(CallInfo),
                    typeof(object),
                    args.Prepend(target).Select(static dmo => dmo.Expression)
                ),
                target.Restrictions.Merge(BindingRestrictions.Combine(args))
            ));
        }
    }

    #endregion Standard binders
}
