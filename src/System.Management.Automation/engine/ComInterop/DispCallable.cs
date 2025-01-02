// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Dynamic;
using System.Globalization;
using System.Linq.Expressions;

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

        public override string ToString() => $"<bound dispmethod {MemberName}>";

        public IDispatchComObject DispatchComObject { get; }

        public IDispatch DispatchObject => DispatchComObject.DispatchObject;

        public string MemberName { get; }

        public int DispId { get; }

        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new DispCallableMetaObject(parameter, this);
        }

        public override bool Equals(object obj)
        {
            return obj is DispCallable other && other.DispatchComObject == DispatchComObject && other.DispId == DispId;
        }

        public override int GetHashCode()
        {
            return DispatchComObject.GetHashCode() ^ DispId;
        }
    }
}
