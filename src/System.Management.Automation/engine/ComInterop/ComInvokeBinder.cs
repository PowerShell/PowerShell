// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using System.Management.Automation.InteropServices;
using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace System.Management.Automation.ComInterop
{
    internal sealed class ComInvokeBinder
    {
        private readonly ComMethodDesc _methodDesc;
        private readonly Expression _method;        // ComMethodDesc to be called
        private readonly Expression _dispatch;      // IDispatch

        private readonly CallInfo _callInfo;
        private readonly DynamicMetaObject[] _args;
        private readonly bool[] _isByRef;
        private readonly Expression _instance;

        private BindingRestrictions _restrictions;

        private VarEnumSelector _varEnumSelector;
        private string[] _keywordArgNames;
        private int _totalExplicitArgs; // Includes the individual elements of ArgumentKind.Dictionary (if any)

        private ParameterExpression _dispatchObject;
        private ParameterExpression _dispatchPointer;
        private ParameterExpression _dispId;
        private ParameterExpression _dispParams;
        private ParameterExpression _paramVariants;
        private ParameterExpression _invokeResult;
        private ParameterExpression _returnValue;
        private ParameterExpression _dispIdsOfKeywordArgsPinned;
        private ParameterExpression _propertyPutDispId;

        internal ComInvokeBinder(
            CallInfo callInfo,
            DynamicMetaObject[] args,
            bool[] isByRef,
            BindingRestrictions restrictions,
            Expression method,
            Expression dispatch,
            ComMethodDesc methodDesc)
        {
            Debug.Assert(callInfo != null, nameof(callInfo));
            Debug.Assert(args != null, nameof(args));
            Debug.Assert(isByRef != null, nameof(isByRef));
            Debug.Assert(method != null, nameof(method));
            Debug.Assert(dispatch != null, nameof(dispatch));

            Debug.Assert(TypeUtils.AreReferenceAssignable(typeof(ComMethodDesc), method.Type), "method type");
            Debug.Assert(TypeUtils.AreReferenceAssignable(typeof(IDispatch), dispatch.Type), "dispatch type");

            _method = method;
            _dispatch = dispatch;
            _methodDesc = methodDesc;

            _callInfo = callInfo;
            _args = args;
            _isByRef = isByRef;
            _restrictions = restrictions;

            // Set Instance to some value so that CallBinderHelper has the right number of parameters to work with
            _instance = dispatch;
        }

        private ParameterExpression DispatchObjectVariable
        {
            get { return EnsureVariable(ref _dispatchObject, typeof(IDispatch), "dispatchObject"); }
        }

        private ParameterExpression DispatchPointerVariable
        {
            get { return EnsureVariable(ref _dispatchPointer, typeof(IntPtr), "dispatchPointer"); }
        }

        private ParameterExpression DispIdVariable
        {
            get { return EnsureVariable(ref _dispId, typeof(int), "dispId"); }
        }

        private ParameterExpression DispParamsVariable
        {
            get { return EnsureVariable(ref _dispParams, typeof(ComTypes.DISPPARAMS), "dispParams"); }
        }

        private ParameterExpression InvokeResultVariable
        {
            get { return EnsureVariable(ref _invokeResult, typeof(Variant), "invokeResult"); }
        }

        private ParameterExpression ReturnValueVariable
        {
            get { return EnsureVariable(ref _returnValue, typeof(object), "returnValue"); }
        }

        private ParameterExpression DispIdsOfKeywordArgsPinnedVariable
        {
            get { return EnsureVariable(ref _dispIdsOfKeywordArgsPinned, typeof(GCHandle), "dispIdsOfKeywordArgsPinned"); }
        }

        private ParameterExpression PropertyPutDispIdVariable
        {
            get { return EnsureVariable(ref _propertyPutDispId, typeof(int), "propertyPutDispId"); }
        }

        private ParameterExpression ParamVariantsVariable
        {
            get
            {
                _paramVariants ??= Expression.Variable(VariantArray.GetStructType(_args.Length), "paramVariants");
                return _paramVariants;
            }
        }

        private static ParameterExpression EnsureVariable(ref ParameterExpression var, Type type, string name)
        {
            if (var != null)
            {
                return var;
            }
            return var = Expression.Variable(type, name);
        }

        private static Type MarshalType(DynamicMetaObject mo, bool isByRef)
        {
            Type marshalType = (mo.Value == null && mo.HasValue && !mo.LimitType.IsValueType) ? null : mo.LimitType;

            // we are not checking that mo.Expression is writeable or whether evaluating it has no sideeffects
            // the assumption is that whoever matched it with ByRef arginfo took care of this.
            if (isByRef)
            {
                // Null just means that null was supplied.
                marshalType ??= mo.Expression.Type;
                marshalType = marshalType.MakeByRefType();
            }
            return marshalType;
        }

        internal DynamicMetaObject Invoke()
        {
            _keywordArgNames = _callInfo.ArgumentNames.ToArray();
            _totalExplicitArgs = _args.Length;

            Type[] marshalArgTypes = new Type[_args.Length];

            // We already tested the instance, so no need to test it again
            for (int i = 0; i < _args.Length; i++)
            {
                DynamicMetaObject curMo = _args[i];
                marshalArgTypes[i] = MarshalType(curMo, _isByRef[i]);
            }

            _varEnumSelector = new VarEnumSelector(marshalArgTypes);

            return new DynamicMetaObject(
                CreateScope(MakeIDispatchInvokeTarget()),
                BindingRestrictions.Combine(_args).Merge(_restrictions)
            );
        }

        private static void AddNotNull(List<ParameterExpression> list, ParameterExpression var)
        {
            if (var != null) list.Add(var);
        }

        private Expression CreateScope(Expression expression)
        {
            List<ParameterExpression> vars = new List<ParameterExpression>();
            AddNotNull(vars, _dispatchObject);
            AddNotNull(vars, _dispatchPointer);
            AddNotNull(vars, _dispId);
            AddNotNull(vars, _dispParams);
            AddNotNull(vars, _paramVariants);
            AddNotNull(vars, _invokeResult);
            AddNotNull(vars, _returnValue);
            AddNotNull(vars, _dispIdsOfKeywordArgsPinned);
            AddNotNull(vars, _propertyPutDispId);
            return vars.Count > 0 ? Expression.Block(vars, expression) : expression;
        }

        private Expression GenerateTryBlock()
        {
            //
            // Declare variables
            //
            ParameterExpression excepInfo = Expression.Variable(typeof(ExcepInfo), "excepInfo");
            ParameterExpression argErr = Expression.Variable(typeof(uint), "argErr");
            ParameterExpression hresult = Expression.Variable(typeof(int), "hresult");

            List<Expression> tryStatements = new List<Expression>();

            if (_keywordArgNames.Length > 0)
            {
                string[] names = _keywordArgNames.AddFirst(_methodDesc.Name);

                tryStatements.Add(
                    Expression.Assign(
                        Expression.Field(
                            DispParamsVariable,
                            typeof(ComTypes.DISPPARAMS).GetField(nameof(ComTypes.DISPPARAMS.rgdispidNamedArgs))
                        ),
                        Expression.Call(typeof(UnsafeMethods).GetMethod(nameof(UnsafeMethods.GetIdsOfNamedParameters)),
                            DispatchObjectVariable,
                            Expression.Constant(names),
                            DispIdVariable,
                            DispIdsOfKeywordArgsPinnedVariable
                        )
                    )
                );
            }

            //
            // Marshal the arguments to Variants
            //
            // For a call like this:
            //   comObj.Foo(100, 101, 102, x=123, z=125)
            // DISPPARAMS needs to be setup like this:
            //   cArgs:             5
            //   cNamedArgs:        2
            //   rgArgs:            123, 125, 102, 101, 100
            //   rgdispidNamedArgs: dispid x, dispid z (the dispids of x and z respectively)

            Expression[] parameters = MakeArgumentExpressions();

            int reverseIndex = _varEnumSelector.VariantBuilders.Length - 1;
            int positionalArgs = _varEnumSelector.VariantBuilders.Length - _keywordArgNames.Length; // args passed by position order and not by name
            for (int i = 0; i < _varEnumSelector.VariantBuilders.Length; i++, reverseIndex--)
            {
                int variantIndex;
                if (i >= positionalArgs)
                {
                    // Named arguments are in order at the start of rgArgs
                    variantIndex = i - positionalArgs;
                }
                else
                {
                    // Positional arguments are in reverse order at the tail of rgArgs
                    variantIndex = reverseIndex;
                }
                VariantBuilder variantBuilder = _varEnumSelector.VariantBuilders[i];

                Expression marshal = variantBuilder.InitializeArgumentVariant(
                    VariantArray.GetStructField(ParamVariantsVariable, variantIndex),
                    parameters[i + 1]
                );

                if (marshal != null)
                {
                    tryStatements.Add(marshal);
                }
            }

            //
            // Call Invoke
            //

            ComTypes.INVOKEKIND invokeKind;
            if (_methodDesc.IsPropertyPut)
            {
                if (_methodDesc.IsPropertyPutRef)
                {
                    invokeKind = ComTypes.INVOKEKIND.INVOKE_PROPERTYPUTREF;
                }
                else
                {
                    invokeKind = ComTypes.INVOKEKIND.INVOKE_PROPERTYPUT;
                }
            }
            else
            {
                // INVOKE_PROPERTYGET should only be needed for COM objects without typeinfo, where we might have to treat properties as methods
                invokeKind = ComTypes.INVOKEKIND.INVOKE_FUNC | ComTypes.INVOKEKIND.INVOKE_PROPERTYGET;
            }

            MethodCallExpression invoke = Expression.Call(
                typeof(UnsafeMethods).GetMethod(nameof(UnsafeMethods.IDispatchInvoke)),
                DispatchPointerVariable,
                DispIdVariable,
                Expression.Constant(invokeKind),
                DispParamsVariable,
                InvokeResultVariable,
                excepInfo,
                argErr
            );

            Expression expr = Expression.Assign(hresult, invoke);
            tryStatements.Add(expr);

            //
            // ComRuntimeHelpers.CheckThrowException(int hresult, ref ExcepInfo excepInfo, ComMethodDesc method, object[] args, uint argErr)
            List<Expression> args = new List<Expression>();
            foreach (Expression parameter in parameters)
            {
                args.Add(Expression.TypeAs(parameter, typeof(object)));
            }

            expr = Expression.Call(
                typeof(ComRuntimeHelpers).GetMethod(nameof(ComRuntimeHelpers.CheckThrowException)),
                hresult,
                excepInfo,
                Expression.Constant(_methodDesc, typeof(ComMethodDesc)),
                Expression.NewArrayInit(typeof(object), args),
                argErr
            );
            tryStatements.Add(expr);

            //
            // _returnValue = (ReturnType)_invokeResult.ToObject();
            //
            Expression invokeResultObject =
                Expression.Call(
                    InvokeResultVariable,
                    typeof(Variant).GetMethod(nameof(Variant.ToObject)));

            VariantBuilder[] variants = _varEnumSelector.VariantBuilders;

            Expression[] parametersForUpdates = MakeArgumentExpressions();
            tryStatements.Add(Expression.Assign(ReturnValueVariable, invokeResultObject));

            for (int i = 0, n = variants.Length; i < n; i++)
            {
                Expression updateFromReturn = variants[i].UpdateFromReturn(parametersForUpdates[i + 1]);
                if (updateFromReturn != null)
                {
                    tryStatements.Add(updateFromReturn);
                }
            }

            tryStatements.Add(Expression.Empty());

            return Expression.Block(new[] { excepInfo, argErr, hresult }, tryStatements);
        }

        private Expression GenerateFinallyBlock()
        {
            List<Expression> finallyStatements = new List<Expression>
            {
                //
                // UnsafeMethods.IUnknownRelease(dispatchPointer);
                //
                Expression.Call(
                    typeof(UnsafeMethods).GetMethod(nameof(UnsafeMethods.IUnknownRelease)),
                    DispatchPointerVariable
                )
            };

            //
            // Clear memory allocated for marshalling
            //
            for (int i = 0, n = _varEnumSelector.VariantBuilders.Length; i < n; i++)
            {
                Expression clear = _varEnumSelector.VariantBuilders[i].Clear();
                if (clear != null)
                {
                    finallyStatements.Add(clear);
                }
            }

            //
            // _invokeResult.Clear()
            //

            finallyStatements.Add(
                Expression.Call(
                    InvokeResultVariable,
                    typeof(Variant).GetMethod(nameof(Variant.Clear))
                )
            );

            //
            // _dispIdsOfKeywordArgsPinned.Free()
            //
            if (_dispIdsOfKeywordArgsPinned != null)
            {
                finallyStatements.Add(
                    Expression.Call(
                        DispIdsOfKeywordArgsPinnedVariable,
                        typeof(GCHandle).GetMethod(nameof(GCHandle.Free))
                    )
                );
            }

            finallyStatements.Add(Expression.Empty());
            return Expression.Block(finallyStatements);
        }

        /// <summary>
        /// Create a stub for the target of the optimized lopop.
        /// </summary>
        /// <returns></returns>
        private Expression MakeIDispatchInvokeTarget()
        {
            Debug.Assert(_varEnumSelector.VariantBuilders.Length == _totalExplicitArgs);

            List<Expression> exprs = new List<Expression>
            {
                //
                // _dispId = ((DispCallable)this).ComMethodDesc.DispId;
                //
                Expression.Assign(
                    DispIdVariable,
                    Expression.Property(_method, typeof(ComMethodDesc).GetProperty(nameof(ComMethodDesc.DispId)))
                )
            };

            //
            // _dispParams.rgvararg = RuntimeHelpers.UnsafeMethods.ConvertVariantByrefToPtr(ref _paramVariants._element0)
            //
            if (_totalExplicitArgs != 0)
            {
                exprs.Add(
                    Expression.Assign(
                        Expression.Field(
                            DispParamsVariable,
                            typeof(ComTypes.DISPPARAMS).GetField(nameof(ComTypes.DISPPARAMS.rgvarg))
                        ),
                        Expression.Call(
                            typeof(UnsafeMethods).GetMethod(nameof(UnsafeMethods.ConvertVariantByrefToPtr)),
                            VariantArray.GetStructField(ParamVariantsVariable, 0)
                        )
                    )
                );
            }

            //
            // _dispParams.cArgs = <number_of_params>;
            //
            exprs.Add(
                Expression.Assign(
                    Expression.Field(
                        DispParamsVariable,
                        typeof(ComTypes.DISPPARAMS).GetField(nameof(ComTypes.DISPPARAMS.cArgs))
                    ),
                    Expression.Constant(_totalExplicitArgs)
                )
            );

            if (_methodDesc.IsPropertyPut)
            {
                //
                // dispParams.cNamedArgs = 1;
                // dispParams.rgdispidNamedArgs = RuntimeHelpers.UnsafeMethods.GetNamedArgsForPropertyPut()
                //
                exprs.Add(
                    Expression.Assign(
                        Expression.Field(
                            DispParamsVariable,
                            typeof(ComTypes.DISPPARAMS).GetField(nameof(ComTypes.DISPPARAMS.cNamedArgs))
                        ),
                        Expression.Constant(1)
                    )
                );

                exprs.Add(
                    Expression.Assign(
                        PropertyPutDispIdVariable,
                        Expression.Constant(ComDispIds.DISPID_PROPERTYPUT)
                    )
                );

                exprs.Add(
                    Expression.Assign(
                        Expression.Field(
                            DispParamsVariable,
                            typeof(ComTypes.DISPPARAMS).GetField(nameof(ComTypes.DISPPARAMS.rgdispidNamedArgs))
                        ),
                        Expression.Call(
                            typeof(UnsafeMethods).GetMethod(nameof(UnsafeMethods.ConvertInt32ByrefToPtr)),
                            PropertyPutDispIdVariable
                        )
                    )
                );
            }
            else
            {
                //
                // _dispParams.cNamedArgs = N;
                //
                exprs.Add(
                    Expression.Assign(
                        Expression.Field(
                            DispParamsVariable,
                            typeof(ComTypes.DISPPARAMS).GetField(nameof(ComTypes.DISPPARAMS.cNamedArgs))
                        ),
                        Expression.Constant(_keywordArgNames.Length)
                    )
                );
            }

            //
            // _dispatchObject = _dispatch
            // _dispatchPointer = Marshal.GetIDispatchForObject(_dispatchObject);
            //

            exprs.Add(Expression.Assign(DispatchObjectVariable, _dispatch));

            exprs.Add(
                Expression.Assign(
                    DispatchPointerVariable,
                    Expression.Call(
                        typeof(Marshal).GetMethod(nameof(Marshal.GetIDispatchForObject)),
                        DispatchObjectVariable
                    )
                )
            );

            Expression tryStatements = GenerateTryBlock();
            Expression finallyStatements = GenerateFinallyBlock();

            exprs.Add(Expression.TryFinally(tryStatements, finallyStatements));

            exprs.Add(ReturnValueVariable);
            var vars = new List<ParameterExpression>();
            foreach (VariantBuilder variant in _varEnumSelector.VariantBuilders)
            {
                if (variant.TempVariable != null)
                {
                    vars.Add(variant.TempVariable);
                }
            }

            // If the method returns void, return AutomationNull
            if (_methodDesc.ReturnType == typeof(void))
            {
                exprs.Add(System.Management.Automation.Language.ExpressionCache.AutomationNullConstant);
            }

            return Expression.Block(vars, exprs);
        }

        /// <summary>
        /// Gets expressions to access all the arguments. This includes the instance argument.
        /// </summary>
        private Expression[] MakeArgumentExpressions()
        {
            Expression[] res;
            int copy = 0;
            if (_instance != null)
            {
                res = new Expression[_args.Length + 1];
                res[copy++] = _instance;
            }
            else
            {
                res = new Expression[_args.Length];
            }

            for (int i = 0; i < _args.Length; i++)
            {
                res[copy++] = _args[i].Expression;
            }
            return res;
        }
    }
}
