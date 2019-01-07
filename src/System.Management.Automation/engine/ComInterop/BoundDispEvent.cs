// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !SILVERLIGHT // ComObject
#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif
using System.Dynamic;

//using Microsoft.Scripting.Utils;

namespace System.Management.Automation.ComInterop
{
    internal sealed class BoundDispEvent : DynamicObject
    {
        private object _rcw;
        private Guid _sourceIid;
        private int _dispid;

        internal BoundDispEvent(object rcw, Guid sourceIid, int dispid)
        {
            _rcw = rcw;
            _sourceIid = sourceIid;
            _dispid = dispid;
        }

        /// <summary>
        /// Provides the implementation of performing AddAssign and SubtractAssign binary operations.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="handler">The handler for the operation.</param>
        /// <param name="result">The result of the operation.</param>
        /// <returns>True if the operation is complete, false if the call site should determine behavior.</returns>
        public override bool TryBinaryOperation(BinaryOperationBinder binder, object handler, out object result)
        {
            if (binder.Operation == ExpressionType.AddAssign)
            {
                result = InPlaceAdd(handler);
                return true;
            }

            if (binder.Operation == ExpressionType.SubtractAssign)
            {
                result = InPlaceSubtract(handler);
                return true;
            }

            result = null;
            return false;
        }

        private static void VerifyHandler(object handler)
        {
            if (handler is Delegate && handler.GetType() != typeof(Delegate))
            {
                return; // delegate
            }

            if (handler is IDynamicMetaObjectProvider)
            {
                return; // IDMOP
            }

            if (handler is DispCallable)
            {
                return;
            }

            throw Error.UnsupportedHandlerType();
        }

        /// <summary>
        /// Adds a handler to an event.
        /// </summary>
        /// <param name="handler">The handler to be added.</param>
        /// <returns>The original event with handler added.</returns>
        private object InPlaceAdd(object handler)
        {
            VerifyHandler(handler);

            ComEventSink comEventSink = ComEventSink.FromRuntimeCallableWrapper(_rcw, _sourceIid, true);
            comEventSink.AddHandler(_dispid, handler);
            return this;
        }

        /// <summary>
        /// Removes handler from the event.
        /// </summary>
        /// <param name="handler">The handler to be removed.</param>
        /// <returns>The original event with handler removed.</returns>
        private object InPlaceSubtract(object handler)
        {
            VerifyHandler(handler);

            ComEventSink comEventSink = ComEventSink.FromRuntimeCallableWrapper(_rcw, _sourceIid, false);
            if (comEventSink != null)
            {
                comEventSink.RemoveHandler(_dispid, handler);
            }

            return this;
        }
    }
}

#endif

