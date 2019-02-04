// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !SILVERLIGHT // ComObject
#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif
using System.Dynamic;
using System.Globalization;

namespace System.Management.Automation.ComInterop
{
    /// <summary>
    /// This represents a bound dispmember on a IDispatch object.
    /// </summary>
    internal sealed class DispCallable : IPseudoComObject
    {
        internal DispCallable(IDispatchComObject dispatch, string memberName, int dispId)
        {
            DispatchComObject = dispatch;
            MemberName = memberName;
            DispId = dispId;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "<bound dispmethod {0}>", MemberName);
        }

        public IDispatchComObject DispatchComObject { get; }

        public IDispatch DispatchObject
        {
            get { return DispatchComObject.DispatchObject; }
        }

        public string MemberName { get; }

        public int DispId { get; }

        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new DispCallableMetaObject(parameter, this);
        }

        public override bool Equals(object obj)
        {
            var other = obj as DispCallable;
            return other != null && other.DispatchComObject == DispatchComObject && other.DispId == DispId;
        }

        public override int GetHashCode()
        {
            return DispatchComObject.GetHashCode() ^ DispId;
        }
    }
}

#endif

