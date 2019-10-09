// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !SILVERLIGHT // ComObject
#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Dynamic;

namespace System.Management.Automation.ComInterop
{
    /// <summary>
    /// This is a helper class for runtime-callable-wrappers of COM instances. We create one instance of this type
    /// for every generic RCW instance.
    /// </summary>
    internal class ComObject : IDynamicMetaObjectProvider
    {
        internal ComObject(object rcw)
        {
            Debug.Assert(ComObject.IsComObject(rcw));
            RuntimeCallableWrapper = rcw;
        }

        /// <summary>
        /// The runtime-callable wrapper.
        /// </summary>
        internal object RuntimeCallableWrapper { get; }

        private static readonly object s_comObjectInfoKey = new object();

        /// <summary>
        /// This is the factory method to get the ComObject corresponding to an RCW.
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        public static ComObject ObjectToComObject(object rcw)
        {
            Debug.Assert(ComObject.IsComObject(rcw));

            // Marshal.Get/SetComObjectData has a LinkDemand for UnmanagedCode which will turn into
            // a full demand. We could avoid this by making this method SecurityCritical
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
            Debug.Assert(comObject != null && typeof(ComObject).IsAssignableFrom(comObject.Type), "must be ComObject");

            return Expression.Property(
                Helpers.Convert(comObject, typeof(ComObject)),
                typeof(ComObject).GetProperty("RuntimeCallableWrapper", BindingFlags.NonPublic | BindingFlags.Instance)
            );
        }

        // Expression that finds or creates a ComObject that corresponds to given Rcw
        internal static MethodCallExpression RcwToComObject(Expression rcw)
        {
            return Expression.Call(
                typeof(ComObject).GetMethod("ObjectToComObject"),
                Helpers.Convert(rcw, typeof(object))
            );
        }

        private static ComObject CreateComObject(object rcw)
        {
            IDispatch dispatchObject = rcw as IDispatch;
            if (dispatchObject != null)
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

        private static readonly Type s_comObjectType = typeof(object).Assembly.GetType("System.__ComObject");

        internal static bool IsComObject(object obj)
        {
            // we can't use System.Runtime.InteropServices.Marshal.IsComObject(obj) since it doesn't work in partial trust
            return obj != null && s_comObjectType.IsAssignableFrom(obj.GetType());
        }
    }
}

#endif

