// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !SILVERLIGHT // ComObject

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;
//using Microsoft.Scripting.Utils;

namespace System.Management.Automation.ComInterop
{
    /// <summary>
    /// This class implements an event sink for a particular RCW.
    /// Unlike the implementation of events in TlbImp'd assemblies,
    /// we will create only one event sink per RCW (theoretically RCW might have
    /// several ComEventSink evenk sinks - but all these implement different source interfaces).
    /// Each ComEventSink contains a list of ComEventSinkMethod objects - which represent
    /// a single method on the source interface an a multicast delegate to redirect
    /// the calls. Notice that we are chaining multicast delegates so that same
    /// ComEventSinkMethod can invoke multiple event handlers).
    ///
    /// ComEventSink implements an IDisposable pattern to Unadvise from the connection point.
    /// Typically, when RCW is finalized the corresponding Dispose will be triggered by
    /// ComEventSinksContainer finalizer. Notice that lifetime of ComEventSinksContainer
    /// is bound to the lifetime of the RCW.
    /// </summary>
    internal sealed class ComEventSink : MarshalByRefObject, IReflect, IDisposable
    {
        #region private fields

        private Guid _sourceIid;
        private ComTypes.IConnectionPoint _connectionPoint;
        private int _adviseCookie;
        private List<ComEventSinkMethod> _comEventSinkMethods;
        private object _lockObject = new object(); // We cannot lock on ComEventSink since it causes a DoNotLockOnObjectsWithWeakIdentity warning

        #endregion

        #region private classes

        /// <summary>
        /// Contains a methods DISPID (in a string formatted of "[DISPID=N]"
        /// and a chained list of delegates to invoke.
        /// </summary>
        private class ComEventSinkMethod
        {
            public string _name;
            public Func<object[], object> _handlers;
        }
        #endregion

        #region ctor

        private ComEventSink(object rcw, Guid sourceIid)
        {
            Initialize(rcw, sourceIid);
        }

        #endregion

        private void Initialize(object rcw, Guid sourceIid)
        {
            _sourceIid = sourceIid;
            _adviseCookie = -1;

            Debug.Assert(_connectionPoint == null, "re-initializing event sink w/o unadvising from connection point");

            ComTypes.IConnectionPointContainer cpc = rcw as ComTypes.IConnectionPointContainer;
            if (cpc == null)
                throw Error.COMObjectDoesNotSupportEvents();

            cpc.FindConnectionPoint(ref _sourceIid, out _connectionPoint);
            if (_connectionPoint == null)
                throw Error.COMObjectDoesNotSupportSourceInterface();

            // Read the comments for ComEventSinkProxy about why we need it
            ComEventSinkProxy proxy = new ComEventSinkProxy(this, _sourceIid);
            _connectionPoint.Advise(proxy.GetTransparentProxy(), out _adviseCookie);
        }

        #region static methods

        public static ComEventSink FromRuntimeCallableWrapper(object rcw, Guid sourceIid, bool createIfNotFound)
        {
            List<ComEventSink> comEventSinks = ComEventSinksContainer.FromRuntimeCallableWrapper(rcw, createIfNotFound);

            if (comEventSinks == null)
            {
                return null;
            }

            ComEventSink comEventSink = null;
            lock (comEventSinks)
            {
                foreach (ComEventSink sink in comEventSinks)
                {
                    if (sink._sourceIid == sourceIid)
                    {
                        comEventSink = sink;
                        break;
                    }
                    else if (sink._sourceIid == Guid.Empty)
                    {
                        // we found a ComEventSink object that
                        // was previously disposed. Now we will reuse it.
                        sink.Initialize(rcw, sourceIid);
                        comEventSink = sink;
                    }
                }

                if (comEventSink == null && createIfNotFound == true)
                {
                    comEventSink = new ComEventSink(rcw, sourceIid);
                    comEventSinks.Add(comEventSink);
                }
            }

            return comEventSink;
        }

        #endregion

        public void AddHandler(int dispid, object func)
        {
            string name = string.Format(CultureInfo.InvariantCulture, "[DISPID={0}]", dispid);

            lock (_lockObject)
            {
                ComEventSinkMethod sinkMethod;
                sinkMethod = FindSinkMethod(name);

                if (sinkMethod == null)
                {
                    if (_comEventSinkMethods == null)
                    {
                        _comEventSinkMethods = new List<ComEventSinkMethod>();
                    }

                    sinkMethod = new ComEventSinkMethod();
                    sinkMethod._name = name;
                    _comEventSinkMethods.Add(sinkMethod);
                }

                sinkMethod._handlers += new SplatCallSite(func).Invoke;
            }
        }

        public void RemoveHandler(int dispid, object func)
        {
            string name = string.Format(CultureInfo.InvariantCulture, "[DISPID={0}]", dispid);

            lock (_lockObject)
            {
                ComEventSinkMethod sinkEntry = FindSinkMethod(name);
                if (sinkEntry == null)
                {
                    return;
                }

                // Remove the delegate from multicast delegate chain.
                // We will need to find the delegate that corresponds
                // to the func handler we want to remove. This will be
                // easy since we Target property of the delegate object
                // is a ComEventCallContext object.
                Delegate[] delegates = sinkEntry._handlers.GetInvocationList();
                foreach (Delegate d in delegates)
                {
                    SplatCallSite callContext = d.Target as SplatCallSite;
                    if (callContext != null && callContext._callable.Equals(func))
                    {
                        sinkEntry._handlers -= d as Func<object[], object>;
                        break;
                    }
                }

                // If the delegates chain is empty - we can remove
                // corresponding ComEvenSinkEntry
                if (sinkEntry._handlers == null)
                    _comEventSinkMethods.Remove(sinkEntry);

                // We can Unadvise from the ConnectionPoint if no more sink entries
                // are registered for this interface
                // (calling Dispose will call IConnectionPoint.Unadvise).
                if (_comEventSinkMethods.Count == 0)
                {
                    // notice that we do not remove
                    // ComEventSinkEntry from the list, we will re-use this data structure
                    // if a new handler needs to be attached.
                    Dispose();
                }
            }
        }

        public object ExecuteHandler(string name, object[] args)
        {
            ComEventSinkMethod site;
            site = FindSinkMethod(name);

            if (site != null && site._handlers != null)
            {
                return site._handlers(args);
            }

            return null;
        }

        #region IReflect

        #region Unimplemented members

        public FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            return null;
        }

        public FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            return Array.Empty<FieldInfo>();
        }

        public MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
        {
            return Array.Empty<MemberInfo>();
        }

        public MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            return Array.Empty<MemberInfo>();
        }

        public MethodInfo GetMethod(string name, BindingFlags bindingAttr)
        {
            return null;
        }

        public MethodInfo GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers)
        {
            return null;
        }

        public MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            return Array.Empty<MethodInfo>();
        }

        public PropertyInfo GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            return null;
        }

        public PropertyInfo GetProperty(string name, BindingFlags bindingAttr)
        {
            return null;
        }

        public PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            return Array.Empty<PropertyInfo>();
        }

        #endregion

        public Type UnderlyingSystemType
        {
            get
            {
                return typeof(object);
            }
        }

        public object InvokeMember(
            string name,
            BindingFlags invokeAttr,
            Binder binder,
            object target,
            object[] args,
            ParameterModifier[] modifiers,
            CultureInfo culture,
            string[] namedParameters)
        {
            return ExecuteHandler(name, args);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            DisposeAll();
            GC.SuppressFinalize(this);
        }

        #endregion

        ~ComEventSink()
        {
            DisposeAll();
        }

        private void DisposeAll()
        {
            if (_connectionPoint == null)
            {
                return;
            }

            if (_adviseCookie == -1)
            {
                return;
            }

            try
            {
                _connectionPoint.Unadvise(_adviseCookie);

                // _connectionPoint has entered the CLR in the constructor
                // for this object and hence its ref counter has been increased
                // by us. We have not exposed it to other components and
                // hence it is safe to call RCO on it w/o worrying about
                // killing the RCW for other objects that link to it.
                Marshal.ReleaseComObject(_connectionPoint);
            }
            catch (Exception ex)
            {
                // if something has gone wrong, and the object is no longer attached to the CLR,
                // the Unadvise is going to throw.  In this case, since we're going away anyway,
                // we'll ignore the failure and quietly go on our merry way.
                COMException exCOM = ex as COMException;
                if (exCOM != null && exCOM.ErrorCode == ComHresults.CONNECT_E_NOCONNECTION)
                {
                    Debug.Assert(false, "IConnectionPoint::Unadvise returned CONNECT_E_NOCONNECTION.");
                    throw;
                }
            }
            finally
            {
                _connectionPoint = null;
                _adviseCookie = -1;
                _sourceIid = Guid.Empty;
            }
        }

        private ComEventSinkMethod FindSinkMethod(string name)
        {
            if (_comEventSinkMethods == null)
                return null;

            ComEventSinkMethod site;
            site = _comEventSinkMethods.Find(element => element._name == name);

            return site;
        }
    }
}

#endif

