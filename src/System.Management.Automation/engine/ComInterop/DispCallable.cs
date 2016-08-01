/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

#if !SILVERLIGHT // ComObject
#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif

using System;
using System.Dynamic;
using System.Globalization;

namespace System.Management.Automation.ComInterop
{
    /// <summary>
    /// This represents a bound dispmember on a IDispatch object.
    /// </summary>
    internal sealed class DispCallable : IPseudoComObject
    {
        private readonly IDispatchComObject _dispatch;
        private readonly string _memberName;
        private readonly int _dispId;

        internal DispCallable(IDispatchComObject dispatch, string memberName, int dispId)
        {
            _dispatch = dispatch;
            _memberName = memberName;
            _dispId = dispId;
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "<bound dispmethod {0}>", _memberName);
        }

        public IDispatchComObject DispatchComObject
        {
            get { return _dispatch; }
        }

        public IDispatch DispatchObject
        {
            get { return _dispatch.DispatchObject; }
        }

        public string MemberName
        {
            get { return _memberName; }
        }

        public int DispId
        {
            get { return _dispId; }
        }

        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new DispCallableMetaObject(parameter, this);
        }

        public override bool Equals(object obj)
        {
            var other = obj as DispCallable;
            return other != null && other._dispatch == _dispatch && other._dispId == _dispId;
        }

        public override int GetHashCode()
        {
            return _dispatch.GetHashCode() ^ _dispId;
        }
    }
}

#endif

