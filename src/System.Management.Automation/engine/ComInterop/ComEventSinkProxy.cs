// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !SILVERLIGHT // ComObject

using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

//using Microsoft.Scripting.Utils;

namespace System.Management.Automation.ComInterop
{
    /// <summary>
    /// ComEventSinkProxy class is responsible for handling QIs for sourceIid
    /// on instances of ComEventSink.
    ///
    /// Background: When a COM even sink advises to a connection point it is
    /// supposed to hand over the dispinterface. Now, some hosts will trust
    /// the COM client to pass the correct pointer, but some will not.
    /// E.g. Excel's implementation of Connection Points will not cause a
    /// QI on the pointer that has been passed, however Word will QI the
    /// pointer to return the required interface.
    ///
    /// ComEventSink does not, strongly speaking, implements the interface
    /// that it claims to implement - it is just "faking" it by using IReflect.
    /// Thus, Word's QIs on the pointer passed to ICP::Advise would fail. To
    /// prevent this we take advantage of RealProxy's ability of
    /// "dressing up" like other classes and hence successfully respond to QIs
    /// for interfaces that it does not really support( it is OK to say
    /// "I implement this interface" for event sinks only since the common
    /// practice is to use IDispatch.Invoke when calling into event sinks).
    /// </summary>
    internal sealed class ComEventSinkProxy : RealProxy
    {
        private Guid _sinkIid;
        private ComEventSink _sink;
        private static readonly MethodInfo s_methodInfoInvokeMember = typeof(ComEventSink).GetMethod("InvokeMember", BindingFlags.Instance | BindingFlags.Public);

        #region ctors

        private ComEventSinkProxy()
        {
        }

        public ComEventSinkProxy(ComEventSink sink, Guid sinkIid)
            : base(typeof(ComEventSink))
        {
            _sink = sink;
            _sinkIid = sinkIid;
        }

        #endregion

        #region Base Class Overrides

        public override IntPtr SupportsInterface(ref Guid iid)
        {
            // if the iid is the sink iid, we ask the base class for an rcw to IDispatch
            if (iid == _sinkIid)
            {
                IntPtr retVal = IntPtr.Zero;
                retVal = Marshal.GetIDispatchForObject(_sink);
                return retVal;
            }

            return base.SupportsInterface(ref iid);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public override IMessage Invoke(IMessage msg)
        {
            // Only know how to handle method calls (property and fields accessors count as methods)
            IMethodCallMessage methodCallMessage = msg as IMethodCallMessage;
            if (methodCallMessage == null)
                throw new NotSupportedException();

            // ComEventSink.InvokeMember is handled specially.
            // The reason we need to do that is due to how namedParameters arg (7th element in the IMethodCallMessage.Args array)
            // is marshalled when called through RealProxy.Invoke.
            // In RealProxy.Invoke namedParameters is typed as object[], while InvokeMember expects it to be string[].
            // If we simply let this call go through (with RemotingServices.ExecuteMessage)
            // we get an InvalidCastException when Remoting tries to pass namedParameters (of type object[])
            // to InvokeMember (which expects namedParameters to be string[]).
            // Since we don't use namedParameters in ComEventSink.InvokeMember - we simply ignore it here
            // and pass-in null.
            MethodInfo methodInfo = (MethodInfo)methodCallMessage.MethodBase;
            if (methodInfo == s_methodInfoInvokeMember)
            {
                object retVal = null;

                try
                {
                    // InvokeMember(string name, BindingFlags bindingFlags, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
                    retVal = ((IReflect)_sink).InvokeMember(
                        /*name*/                methodCallMessage.Args[0] as string,
                        /*bindingFlags*/        (BindingFlags)methodCallMessage.Args[1],
                        /*binder*/              methodCallMessage.Args[2] as Binder,
                        /*target*/              null,
                        /*args*/                methodCallMessage.Args[4] as object[],
                        /*modifiers*/           methodCallMessage.Args[5] as ParameterModifier[],
                        /*culture*/             methodCallMessage.Args[6] as CultureInfo,
                        /*namedParameters*/     null);
                }
                catch (Exception ex)
                {
                    return new ReturnMessage(ex.InnerException, methodCallMessage);
                }

                return new ReturnMessage(retVal, methodCallMessage.Args, methodCallMessage.ArgCount, null, methodCallMessage);
            }

            return RemotingServices.ExecuteMessage(_sink, methodCallMessage);
        }

        #endregion
    }
}

#endif

