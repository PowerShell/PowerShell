// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !SILVERLIGHT
#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif
using System.Diagnostics;
using System.Dynamic;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation.Language;

namespace System.Management.Automation.ComInterop
{
    internal static class ComBinderHelpers
    {
        internal static bool PreferPut(Type type, bool holdsNull)
        {
            Debug.Assert(type != null);

            if (type.IsValueType || type.IsArray) return true;

            if (type == typeof(string) ||
                type == typeof(DBNull) ||
                holdsNull ||
                type == typeof(System.Reflection.Missing) ||
                type == typeof(CurrencyWrapper))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static bool IsByRef(DynamicMetaObject mo)
        {
            ParameterExpression pe = mo.Expression as ParameterExpression;
            return pe != null && pe.IsByRef;
        }

        internal static bool IsPSReferenceArg(DynamicMetaObject o)
        {
            Type t = o.LimitType;
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(PSReference<>);
        }

        // this helper prepares arguments for COM binding by transforming ByVal StongBox arguments
        // into ByRef expressions that represent the argument's Value fields.
        internal static bool[] ProcessArgumentsForCom(ComMethodDesc method, ref DynamicMetaObject[] args,
            List<ParameterExpression> temps, List<Expression> initTemps)
        {
            Debug.Assert(args != null);

            DynamicMetaObject[] newArgs = new DynamicMetaObject[args.Length];
            bool[] isByRefArg = new bool[args.Length];

            for (int i = 0; i < args.Length; i++)
            {
                DynamicMetaObject curArgument = args[i];

                // set new arg infos to their original values or set default ones
                // we will do this fixup early so that we can assume we always have
                // arginfos in COM binder.

                if (IsByRef(curArgument))
                {
                    newArgs[i] = curArgument;
                    isByRefArg[i] = true;
                }
                else
                {
                    if (IsPSReferenceArg(curArgument))
                    {
                        var restrictions = curArgument.Restrictions.Merge(
                            GetTypeRestrictionForDynamicMetaObject(curArgument)
                        );

                        // we have restricted this argument to LimitType so we can convert and conversion will be trivial cast.
                        Expression boxedValueAccessor = Expression.Property(
                            Helpers.Convert(
                                curArgument.Expression,
                                curArgument.LimitType
                            ),
                            curArgument.LimitType.GetProperty("Value")
                        );

                        PSReference value = curArgument.Value as PSReference;
                        object boxedValue = value != null ? value.Value : null;

                        newArgs[i] = new DynamicMetaObject(
                            boxedValueAccessor,
                            restrictions,
                            boxedValue
                        );

                        isByRefArg[i] = true;
                    }
                    else
                    {
                        if ((method.ParameterInformation != null) && (i < method.ParameterInformation.Length))
                        {
                            newArgs[i] = new DynamicMetaObject(curArgument.CastOrConvertMethodArgument(
                                method.ParameterInformation[i].parameterType,
                                i.ToString(CultureInfo.InvariantCulture),
                                method.Name,
                                temps,
                                initTemps), curArgument.Restrictions);
                        }
                        else
                        {
                            newArgs[i] = curArgument;
                        }

                        isByRefArg[i] = false;
                    }
                }
            }

            args = newArgs;
            return isByRefArg;
        }

        internal static BindingRestrictions GetTypeRestrictionForDynamicMetaObject(DynamicMetaObject obj)
        {
            if (obj.Value == null && obj.HasValue)
            {
                // If the meta object holds a null value, create an instance restriction for checking null
                return BindingRestrictions.GetInstanceRestriction(obj.Expression, null);
            }

            return BindingRestrictions.GetTypeRestriction(obj.Expression, obj.LimitType);
        }
    }
}

#endif

