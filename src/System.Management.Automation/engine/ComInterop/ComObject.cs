// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Management.Automation.ComInterop
{
    /// <summary>
    /// The ComObject class wraps a runtime-callable-wrapper and enables it to be used with the Dynamic Language Runtime and the C# dynamic keyword.
    /// </summary>
    internal class ComObject : IDynamicMetaObjectProvider
    {
        internal ComObject(object rcw)
        {
            Debug.Assert(ComBinder.IsComObject(rcw));
            RuntimeCallableWrapper = rcw;
        }

        internal object RuntimeCallableWrapper { get; }

        private static readonly object s_comObjectInfoKey = new object();

        /// <summary>
        /// Gets a <see cref="ComObject"/> that wraps the runtime-callable-wrapper, or creates one if none currently exists.
        /// </summary>
        /// <returns></returns>
        public static ComObject ObjectToComObject(object rcw)
        {
            Debug.Assert(ComBinder.IsComObject(rcw));

            object data = Marshal.GetComObjectData(rcw, s_comObjectInfoKey);
            if (data != null)
            {
                return (ComObject)data;
            }

            lock (s_comObjectInfoKey)
            {
                data = Marshal.GetComObjectData(rcw, s_comObjectInfoKey);
                if (data != null)
                {
                    return (ComObject)data;
                }

                ComObject comObjectInfo = CreateComObject(rcw);
                if (!Marshal.SetComObjectData(rcw, s_comObjectInfoKey, comObjectInfo))
                {
                    throw Error.SetComObjectDataFailed();
                }

                return comObjectInfo;
            }
        }

        // Expression that unwraps ComObject
        internal static MemberExpression RcwFromComObject(Expression comObject)
        {
            Debug.Assert(comObject != null && (typeof(ComObject).IsAssignableFrom(comObject.Type) || comObject.Type == typeof(object)), "must be ComObject");

            return Expression.Property(
                Helpers.Convert(comObject, typeof(ComObject)),
                typeof(ComObject).GetProperty(nameof(RuntimeCallableWrapper), BindingFlags.NonPublic | BindingFlags.Instance)
            );
        }

        // Expression that finds or creates a ComObject that corresponds to given Rcw
        internal static MethodCallExpression RcwToComObject(Expression rcw)
        {
            return Expression.Call(
                typeof(ComObject).GetMethod(nameof(ObjectToComObject)),
                Helpers.Convert(rcw, typeof(object))
            );
        }

        private static ComObject CreateComObject(object rcw)
        {
            if (rcw is IDispatch dispatchObject)
            {
                // We can do method invocations on IDispatch objects
                return new IDispatchComObject(dispatchObject);
            }

            // There is not much we can do in this case
            return new ComObject(rcw);
        }

        internal virtual IList<string> GetMemberNames(bool dataOnly)
        {
            return Array.Empty<string>();
        }

        internal virtual IList<KeyValuePair<string, object>> GetMembers(IEnumerable<string> names)
        {
            return Array.Empty<KeyValuePair<string, object>>();
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return new ComFallbackMetaObject(parameter, BindingRestrictions.Empty, this);
        }
    }
}
