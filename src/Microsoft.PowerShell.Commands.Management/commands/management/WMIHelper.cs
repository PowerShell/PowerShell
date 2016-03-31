#if !CORECLR
//
//    Copyright (C) Microsoft.  All rights reserved.
//
using System;
using System.Globalization;
using System.Management.Automation;
using System.Management;
using System.Management.Automation.Internal;
using System.Text;
using System.Management.Automation.Provider;
using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;
using System.Security.AccessControl;
using System.Runtime.InteropServices;
using System.Threading;
using System.Management.Automation.Remoting;
using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerShell.Commands.Internal;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
#region Helper Classes

    /// <summary>
    /// Base class for all WMI helper classes. This is an abstract class
    /// and the helpers need to derive from this
    /// </summary>
    internal abstract class AsyncCmdletHelper : IThrottleOperation
    {
        /// <summary>
        /// Exception raised internally when any method of this class
        /// is executed
        /// </summary>
        internal Exception InternalException
        {
            get
            {
                return internalException;
            }
        }
        protected Exception internalException = null;

    } 

    /// <summary>
    /// This class is responsible for creating WMI connection for getting objects and notifications
    /// from WMI asynchronously. This spawns a new thread to connect to WMI on remote machine.
    /// This allows the main thread to return faster and not blocked on network hops.
    /// </summary>
    internal class WmiAsyncCmdletHelper : AsyncCmdletHelper
    {
        /// <summary>
        /// Internal Constructor
        /// </summary>
        /// <param name="childJob">Job associated with this operation</param>
        /// <param name="wmiObject">object associated with this operation</param>
        /// <param name="computerName"> computer on which the operation is invoked </param>
        /// <param name="results"> sink to get wmi objects </param>
        internal WmiAsyncCmdletHelper( PSWmiChildJob childJob, Cmdlet wmiObject, string computerName, ManagementOperationObserver results)
        {
            this.wmiObject = wmiObject;
            this.computerName = computerName;
            this.results = results;
            this.State = WmiState.NotStarted;
            this.Job = childJob;
        }

        /// <summary>
        /// Internal Constructor.  This variant takes a count parameter that determines how many times 
        /// the WMI command is executed.
        /// </summary>
        /// <param name="childJob">Job associated with this operation</param>
        /// <param name="wmiObject">Object associated with this operation</param>
        /// <param name="computerName">Computer on which the operation is invoked</param>
        /// <param name="results">Sink to return wmi objects</param>
        /// <param name="count">Number of times the WMI command is executed</param>
        internal WmiAsyncCmdletHelper(PSWmiChildJob childJob, Cmdlet wmiObject, string computerName, ManagementOperationObserver results, int count)
            : this(childJob, wmiObject, computerName, results)
        {
            this.cmdCount = count;
        }

        private string computerName;
        internal event EventHandler<WmiJobStateEventArgs> WmiOperationState;
        internal event EventHandler<EventArgs> ShutdownComplete;
        private ManagementOperationObserver results;
        private int cmdCount = 1;
        private PSWmiChildJob Job;
        /// <summary>
        /// current operation state
        /// </summary>
        internal WmiState State
        {
            get { return state; }
            set { state = value; }
        }
        
        private WmiState state ;

        /// <summary>
        /// Cancel WMI connection
        /// </summary>
        internal override void StopOperation()
        {
            this.results.Cancel();
            state = WmiState.Stopped;
            RaiseOperationCompleteEvent(null, OperationState.StopComplete);
        }
         /// <summary>
        /// Uses this.filter, this.wmiClass and this.property to retrieve the filter
        /// </summary>
        private string GetWmiQueryString()
        {
            GetWmiObjectCommand getObject = (GetWmiObjectCommand)this.wmiObject;
            StringBuilder returnValue = new StringBuilder("select ");
            returnValue.Append(String.Join(", ", getObject.Property));
            returnValue.Append(" from ");
            returnValue.Append(getObject.Class);
            if(!String.IsNullOrEmpty(getObject.Filter))
            {
                returnValue.Append(" where ");
                returnValue.Append(getObject.Filter);
            }
            return returnValue.ToString();
        }

        /// <summary>
        /// Do WMI connection by creating another thread based on type of request and return immediately.
        /// </summary>
        internal override void StartOperation()
        {
            Thread thread;
            if( this.wmiObject.GetType() == typeof(GetWmiObjectCommand))
            {
                thread = new Thread (new ThreadStart(ConnectGetWMI));
            }
            else if( this.wmiObject.GetType() == typeof(RemoveWmiObject) )
            {
                thread = new Thread( new ThreadStart(ConnectRemoveWmi));
            }
            else if( this.wmiObject.GetType() == typeof(InvokeWmiMethod) )
            {
                thread = new Thread( new ThreadStart(ConnectInvokeWmi));
            }
            else if( this.wmiObject.GetType() == typeof(SetWmiInstance) )
            {
                thread = new Thread( new ThreadStart(ConnectSetWmi));
            }
            else
            {
                InvalidOperationException exception = new InvalidOperationException ( "This operation is not supported for this cmdlet." );
                internalException = exception;
                state = WmiState.Failed;
                RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                return;
            }
            thread.IsBackground = true;
            //thread.SetApartmentState( ApartmentState.STA);
            thread.Start();
        }
        
        /// <summary>
        /// 
        /// </summary>
        internal override event EventHandler<OperationStateEventArgs> OperationComplete;
        
        private Cmdlet wmiObject;

        /// <summary>
        /// Raise operation completion event
        /// </summary>
        internal void RaiseOperationCompleteEvent(EventArgs baseEventArgs, OperationState state)
        {

            OperationStateEventArgs operationStateEventArgs = new OperationStateEventArgs();
            operationStateEventArgs.OperationState = state;
            OperationComplete.SafeInvoke(this, operationStateEventArgs);

        } // RaiseOperationCompleteEvent

        ///<summary>
        /// Raise WMI state changed event
        ///</summary>
        internal void RaiseWmiOperationState(EventArgs baseEventArgs, WmiState state)
        {
            WmiJobStateEventArgs wmiJobStateEventArgs = new WmiJobStateEventArgs();
            wmiJobStateEventArgs.WmiState= state;
            WmiOperationState.SafeInvoke(this, wmiJobStateEventArgs);
        }

        /// <summary>
        /// Do the actual connection to remote machine for Set-WMIInstance cmdlet and raise operation complete event.
        /// </summary>
        private void ConnectSetWmi()
        {
            SetWmiInstance setObject = (SetWmiInstance) this.wmiObject;
            state = WmiState.Running;
            RaiseWmiOperationState(null,WmiState.Running);
            if (setObject.InputObject != null)
            {
                ManagementObject mObj = null;
                try
                {
                    PutOptions pOptions = new PutOptions();
                    //Extra check
                    if (setObject.InputObject.GetType() == typeof(ManagementClass))
                    {
                        //Check if Flag specified is CreateOnly or not
                        if( setObject.flagSpecified && setObject.PutType != PutType.CreateOnly)
                        {
                            InvalidOperationException e = new InvalidOperationException("CreateOnlyFlagNotSpecifiedWithClassPath");
                            internalException = e;
                            state = WmiState.Failed;
                            RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                            return;
                        }
                        mObj = ((ManagementClass)setObject.InputObject).CreateInstance();
                        setObject.PutType = PutType.CreateOnly;
                    }
                    else
                    {
                        //Check if Flag specified is Updateonly or UpdateOrCreateOnly or not
                        if( setObject.flagSpecified )
                        {
                            if( !(setObject.PutType == PutType.UpdateOnly || setObject.PutType == PutType.UpdateOrCreate))
                            {
                                InvalidOperationException e = new InvalidOperationException("NonUpdateFlagSpecifiedWithInstancePath");
                                internalException = e;
                                state = WmiState.Failed;
                                RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                                return;
                            }
                        }
                        else
                        {
                            setObject.PutType = PutType.UpdateOrCreate;
                        }
                        
                        mObj = (ManagementObject)setObject.InputObject.Clone();
                    }
                    if (setObject.Arguments != null)
                    {
                        IDictionaryEnumerator en = setObject.Arguments.GetEnumerator();
                        while (en.MoveNext())
                        {
                            mObj[en.Key as string] = en.Value;
                        }
                    }
                    pOptions.Type = setObject.PutType;
                    if( mObj != null)
                    {
                        mObj.Put(this.results,pOptions);
                    }
                    else
                    {
                        InvalidOperationException exp = new InvalidOperationException();
                        internalException = exp;
                        state = WmiState.Failed;
                        RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                    }
                }
                catch (ManagementException e)
                {
                    internalException = e;
                    state = WmiState.Failed;
                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    internalException = e;
                    state = WmiState.Failed;
                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                }
                catch (System.UnauthorizedAccessException e)
                {
                    internalException = e;
                    state = WmiState.Failed;
                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                }
            }
            else
            {
                ManagementPath mPath = null;
                //If Class is specified only CreateOnly flag is supported
                if( setObject.Class != null)
                {
                    if( setObject.flagSpecified && setObject.PutType != PutType.CreateOnly)
                    {
                        InvalidOperationException exp = new InvalidOperationException("CreateOnlyFlagNotSpecifiedWithClassPath");
                        internalException = exp;
                        state = WmiState.Failed;
                        RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                        return;
                    }
                    setObject.PutType = PutType.CreateOnly;
                }
                else
                {
                    mPath = new ManagementPath(setObject.Path);
                    if (String.IsNullOrEmpty(mPath.NamespacePath))
                    {
                        mPath.NamespacePath = setObject.Namespace;
                    }
                    else if (setObject.namespaceSpecified)
                    {
                        InvalidOperationException exp = new InvalidOperationException("NamespaceSpecifiedWithPath");
                        internalException = exp;
                        state = WmiState.Failed;
                        RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                        return;
                    }

                    if (mPath.Server != "." && setObject.serverNameSpecified)
                    {
                        InvalidOperationException exp = new InvalidOperationException("ComputerNameSpecifiedWithPath");
                        internalException = exp;
                        state = WmiState.Failed;
                        RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                        return;
                    }
                    if( mPath.IsClass)
                    {
                        if( setObject.flagSpecified && setObject.PutType != PutType.CreateOnly)
                        {
                            InvalidOperationException exp = new InvalidOperationException("CreateOnlyFlagNotSpecifiedWithClassPath");
                            internalException = exp;
                            state = WmiState.Failed;
                            RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                            return;
                        }
                        setObject.PutType = PutType.CreateOnly;
                    }
                    else
                    {
                        if( setObject.flagSpecified )
                        {
                            if( !(setObject.PutType == PutType.UpdateOnly || setObject.PutType == PutType.UpdateOrCreate))
                            {
                                InvalidOperationException exp = new InvalidOperationException("NonUpdateFlagSpecifiedWithInstancePath");
                                internalException = exp;
                                state = WmiState.Failed;
                                RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                                return;
                            }
                        }
                        else
                        {
                            setObject.PutType = PutType.UpdateOrCreate;
                        }
                    }
                }
                //If server name is specified loop through it.
                if( mPath != null)
                {
                    if (!(mPath.Server == "." && setObject.serverNameSpecified))
                    {
                        computerName = mPath.Server;
                    }
                }
                ConnectionOptions options = setObject.GetConnectionOption();
                ManagementObject mObject = null;
                try
                {
                     if (setObject.Path != null)
                    {
                        mPath.Server = computerName;
                        ManagementScope mScope = new ManagementScope(mPath, options);
                        if (mPath.IsClass)
                        {
                            ManagementClass mClass = new ManagementClass(mPath);
                            mClass.Scope = mScope;
                            mObject = mClass.CreateInstance();
                        }
                        else
                        {
                            //This can throw if path does not exist caller should catch it.
                            ManagementObject mInstance = new ManagementObject(mPath);
                            mInstance.Scope = mScope;
                            try
                            {
                                mInstance.Get();
                            }
                            catch (ManagementException e)
                            {
                                if (e.ErrorCode != ManagementStatus.NotFound)
                                {
                                    internalException = e;
                                    state = WmiState.Failed;
                                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                                    return;
                                }
                                int namespaceIndex = setObject.Path.IndexOf(':');
                                if( namespaceIndex == -1 )
                                {
                                    internalException = e;
                                    state = WmiState.Failed;
                                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                                    return;
                                }
                                int classIndex = (setObject.Path.Substring(namespaceIndex)).IndexOf('.');
                                if( classIndex == -1 )
                                {
                                    internalException = e;
                                    state = WmiState.Failed;
                                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                                    return;
                                }
                                //Get class object and create instance.
                                string newPath = setObject.Path.Substring(0,classIndex+namespaceIndex);
                                ManagementPath classPath = new ManagementPath(newPath);
                                ManagementClass mClass = new ManagementClass(classPath);
                                mClass.Scope = mScope;
                                mInstance = mClass.CreateInstance();
                            }
                            mObject = mInstance;
                        }
                    }
                    else
                    {
                        ManagementScope scope = new ManagementScope(WMIHelper.GetScopeString(computerName, setObject.Namespace), options);
                        ManagementClass mClass = new ManagementClass(setObject.Class);
                        mClass.Scope = scope;
                        mObject = mClass.CreateInstance();
                    }
                    if (setObject.Arguments != null)
                    {
                        IDictionaryEnumerator en = setObject.Arguments.GetEnumerator();
                        while (en.MoveNext())
                        {
                            mObject[en.Key as string] = en.Value;
                        }
                    }             
                    PutOptions pOptions = new PutOptions();
                    pOptions.Type = setObject.PutType;
                    if(mObject != null)
                    {
                        mObject.Put(this.results, pOptions);
                    }
                    else
                    {
                        InvalidOperationException exp = new InvalidOperationException();
                        internalException = exp;
                        state = WmiState.Failed;
                        RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                    }
                }
                catch (ManagementException e)
                {
                    internalException = e;
                    state = WmiState.Failed;
                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    internalException = e;
                    state = WmiState.Failed;
                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                }
                catch (System.UnauthorizedAccessException e)
                {
                    internalException = e;
                    state = WmiState.Failed;
                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                }
            }
        }
        
        /// <summary>
        /// Do the actual connection to remote machine for Invokd-WMIMethod cmdlet and raise operation complete event.
        /// </summary>
        private void ConnectInvokeWmi()
        {
            InvokeWmiMethod invokeObject = (InvokeWmiMethod)this.wmiObject;
            state = WmiState.Running;
            RaiseWmiOperationState(null,WmiState.Running);

            if (invokeObject.InputObject != null)
            {
                ManagementBaseObject inputParameters =null ; 
                try
                {
                    inputParameters = invokeObject.InputObject.GetMethodParameters(invokeObject.Name);
                    if(invokeObject.ArgumentList != null )
                    {
                        int inParamCount = invokeObject.ArgumentList.Length;
                        foreach (PropertyData property in inputParameters.Properties)
                        {
                            if (inParamCount == 0)
                                break;
                            property.Value = invokeObject.ArgumentList[invokeObject.ArgumentList.Length - inParamCount];
                            inParamCount--;
                        }
                    }
                    invokeObject.InputObject.InvokeMethod(this.results, invokeObject.Name, inputParameters,null);
                }
                catch (ManagementException e)
                {
                    internalException = e;
                    state = WmiState.Failed;
                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    internalException = e;
                    state = WmiState.Failed;
                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                }
                catch (System.UnauthorizedAccessException e)
                {
                    internalException = e;
                    state = WmiState.Failed;
                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                }
                return;
            }
            else
            {
                ConnectionOptions options = invokeObject.GetConnectionOption();
                ManagementPath mPath = null;
                ManagementObject mObject = null;
                if( invokeObject.Path != null)
                {
                    mPath = new ManagementPath(invokeObject.Path);
                    if(String.IsNullOrEmpty(mPath.NamespacePath))
                    {
                        mPath.NamespacePath = invokeObject.Namespace;
                    }
                    else if( invokeObject.namespaceSpecified )
                    {
                        InvalidOperationException e = new InvalidOperationException("NamespaceSpecifiedWithPath");
                        internalException = e;
                        state = WmiState.Failed;
                        RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                        return;
                    }
                    
                    if( mPath.Server != "." && invokeObject.serverNameSpecified )
                    {
                        InvalidOperationException e = new InvalidOperationException("ComputerNameSpecifiedWithPath");
                        internalException = e;
                        state = WmiState.Failed;
                        RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                        return;
                    }
                    //If server name is specified loop through it.
                    if ( !(mPath.Server == "." && invokeObject.serverNameSpecified))
                    {
                        computerName = mPath.Server ;
                    }
                }

                bool isLocal = false, needToEnablePrivilege = false;
                PlatformInvokes.TOKEN_PRIVILEGE currentPrivilegeState = new PlatformInvokes.TOKEN_PRIVILEGE();
                try
                {
                    needToEnablePrivilege = NeedToEnablePrivilege(computerName, invokeObject.Name, ref isLocal);
                    if (needToEnablePrivilege)
                    {
                        if (!(isLocal && PlatformInvokes.EnableTokenPrivilege(ComputerWMIHelper.SE_SHUTDOWN_NAME, ref currentPrivilegeState)) &&
                            !(!isLocal && PlatformInvokes.EnableTokenPrivilege(ComputerWMIHelper.SE_REMOTE_SHUTDOWN_NAME, ref currentPrivilegeState)))
                        {
                            string message =
                                StringUtil.Format(ComputerResources.PrivilegeNotEnabled, computerName,
                                isLocal ? ComputerWMIHelper.SE_SHUTDOWN_NAME : ComputerWMIHelper.SE_REMOTE_SHUTDOWN_NAME);
                            InvalidOperationException e = new InvalidOperationException(message);
                            internalException = e;
                            state = WmiState.Failed;
                            RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                            return;
                        }
                    }

                    if( invokeObject.Path != null )
                    {
                        mPath.Server = computerName;
                        if (mPath.IsClass)
                        {
                            ManagementClass mClass = new ManagementClass(mPath);
                            mObject = mClass;
                        }
                        else
                        {
                            ManagementObject mInstance = new ManagementObject(mPath);
                            mObject = mInstance;
                        }
                        ManagementScope mScope = new ManagementScope(mPath, options);
                        mObject.Scope = mScope;
                    }
                    else
                    {
                        ManagementScope scope = new ManagementScope(WMIHelper.GetScopeString(computerName, invokeObject.Namespace), options);
                        ManagementClass mClass = new ManagementClass(invokeObject.Class);
                        mObject = mClass;
                        mObject.Scope = scope;
                    }

                    ManagementBaseObject inputParameters =  mObject.GetMethodParameters(invokeObject.Name);
                    if(invokeObject.ArgumentList != null )
                    {
                        int inParamCount = invokeObject.ArgumentList.Length;
                        foreach (PropertyData property in inputParameters.Properties)
                        {
                            if (inParamCount == 0)
                                break;
                            property.Value = invokeObject.ArgumentList[invokeObject.ArgumentList.Length - inParamCount];
                            inParamCount--;
                        }
                    }
                    
                    if (needToEnablePrivilege)
                    {
                        ManagementBaseObject result = mObject.InvokeMethod(invokeObject.Name, inputParameters, null);
                        Dbg.Diagnostics.Assert(result != null, "result cannot be null if the Join method is invoked");
                        int returnCode = Convert.ToInt32(result["ReturnValue"], CultureInfo.CurrentCulture);
                        if (returnCode != 0)
                        {
                            var e = new Win32Exception(returnCode);
                            internalException = e;
                            state = WmiState.Failed;
                            RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                        }
                        else
                        {
                            ShutdownComplete.SafeInvoke(this, null);
                        }
                    }
                    else
                    {
                        mObject.InvokeMethod(this.results, invokeObject.Name, inputParameters, null);
                    }
                }
                catch (ManagementException e)
                {
                    internalException = e;
                    state = WmiState.Failed;
                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    internalException = e;
                    state = WmiState.Failed;
                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                }
                catch (System.UnauthorizedAccessException e)
                {
                    internalException = e;
                    state = WmiState.Failed;
                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                }
                finally
                {
                    // Restore the previous privilege state if something unexpected happened
                    if (needToEnablePrivilege)
                    {
                        PlatformInvokes.RestoreTokenPrivilege(
                            isLocal ? ComputerWMIHelper.SE_SHUTDOWN_NAME : ComputerWMIHelper.SE_REMOTE_SHUTDOWN_NAME, ref currentPrivilegeState);
                    }
                }
            }
        }

        /// <summary>
        /// Check if we need to enable the shutdown privilege
        /// </summary>
        /// <param name="computer"></param>
        /// <param name="methodName"></param>
        /// <param name="isLocal"></param>
        /// <returns></returns>
        private bool NeedToEnablePrivilege(string computer, string methodName, ref bool isLocal)
        {
            bool result = false;
            if (methodName.Equals("Win32Shutdown", StringComparison.OrdinalIgnoreCase))
            {
                result = true;

                // CLR 4.0 Port note - use https://msdn.microsoft.com/en-us/library/system.net.networkinformation.ipglobalproperties.hostname(v=vs.110).aspx
                string localName = System.Net.Dns.GetHostName();

                // And for this, use PsUtils.GetHostname()
                string localFullName = System.Net.Dns.GetHostEntry("").HostName;
                if (computer.Equals(".") || computer.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                    computer.Equals(localName, StringComparison.OrdinalIgnoreCase) ||
                    computer.Equals(localFullName, StringComparison.OrdinalIgnoreCase))
                {
                    isLocal = true;
                }
            }

            return result;
        }
        
        /// <summary>
        /// Do the actual connection to remote machine for Remove-WMIObject cmdlet and raise operation complete event.
        /// </summary>
        private void ConnectRemoveWmi()
        {
            RemoveWmiObject removeObject = (RemoveWmiObject)this.wmiObject;
            state = WmiState.Running;
            RaiseWmiOperationState(null,WmiState.Running);
            if (removeObject.InputObject != null)
            {
                try
                {
                    removeObject.InputObject.Delete(this.results);

                }
                catch (ManagementException e)
                {
                    internalException = e;
                    state = WmiState.Failed;
                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    internalException = e;
                    state = WmiState.Failed;
                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                }
                catch (System.UnauthorizedAccessException e)
                {
                    internalException = e;
                    state = WmiState.Failed;
                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                }
                return;
            }
            else
            {
                ConnectionOptions options = removeObject.GetConnectionOption();
                ManagementPath mPath = null;
                ManagementObject mObject = null;
                if (removeObject.Path != null)
                {
                    mPath = new ManagementPath(removeObject.Path);
                    if(String.IsNullOrEmpty(mPath.NamespacePath))
                    {
                        mPath.NamespacePath = removeObject.Namespace;
                    }
                    else if( removeObject.namespaceSpecified)
                    {
                        InvalidOperationException e = new InvalidOperationException("NamespaceSpecifiedWithPath");
                        internalException = e;
                        state = WmiState.Failed;
                        RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                        return;
                    }

                    if( mPath.Server != "." && removeObject.serverNameSpecified)
                    {
                        InvalidOperationException e = new InvalidOperationException("ComputerNameSpecifiedWithPath");
                        internalException = e;
                        state = WmiState.Failed;
                        RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                        return;
                    }
                    if (!(mPath.Server == "." && removeObject.serverNameSpecified))
                    {
                        computerName = mPath.Server;
                    }
                }
                try
                {
                    if( removeObject.Path != null )
                    {
                        mPath.Server = computerName;
                        if (mPath.IsClass)
                        {
                            ManagementClass mClass = new ManagementClass(mPath);
                            mObject = mClass;
                        }
                        else
                        {
                            ManagementObject mInstance = new ManagementObject(mPath);
                            mObject = mInstance;
                        }
                        ManagementScope mScope = new ManagementScope(mPath, options);
                        mObject.Scope = mScope;
                    }
                    else
                    {
                        ManagementScope scope = new ManagementScope(WMIHelper.GetScopeString(computerName, removeObject.Namespace), options);
                        ManagementClass mClass = new ManagementClass(removeObject.Class);
                        mObject = mClass;
                        mObject.Scope = scope;
                    }
                    mObject.Delete(this.results);
                }
                catch (ManagementException e)
                {
                    internalException = e;
                    state = WmiState.Failed;
                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    internalException = e;
                    state = WmiState.Failed;
                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                }
                catch (System.UnauthorizedAccessException e)
                {
                    internalException = e;
                    state = WmiState.Failed;
                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                }
            }
        }
        
        /// <summary>
        /// Do the actual connection to remote machine for Get-WMIObject cmdlet and raise operation complete event.
        /// </summary>
        private void ConnectGetWMI()
        {
            GetWmiObjectCommand getObject = (GetWmiObjectCommand)this.wmiObject;
            state = WmiState.Running;
            RaiseWmiOperationState(null,WmiState.Running);
            ConnectionOptions options = getObject.GetConnectionOption();
            if (getObject.List.IsPresent)
            {
                if(!getObject.ValidateClassFormat( ) )
                {
                    ArgumentException e = new ArgumentException(
                        String.Format(
                            Thread.CurrentThread.CurrentCulture,
                            "Class", getObject.Class));

                    internalException = e;
                    state = WmiState.Failed;
                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                    return;
                }
                try
                {
                    if( getObject.Recurse.IsPresent)
                    {
                        ArrayList namespaceArray = new ArrayList();
                        ArrayList sinkArray = new ArrayList();
                        ArrayList connectArray = new ArrayList(); // Optimization for remote namespace
                        int currentNamesapceCount = 0;
                        namespaceArray.Add(getObject.Namespace);
                        bool topNamespace = true;
                        while(currentNamesapceCount <  namespaceArray.Count)
                        {
                            string connectNamespace = (string)namespaceArray[currentNamesapceCount];
                            ManagementScope scope = new ManagementScope(WMIHelper.GetScopeString(this.computerName, connectNamespace), options);
                            scope.Connect();
                            ManagementClass namespaceClass= new ManagementClass(scope, new ManagementPath("__Namespace"), new ObjectGetOptions() );
                            foreach (ManagementBaseObject obj in namespaceClass.GetInstances() )
                            {
                                if( !getObject.IsLocalizedNamespace( (string)obj["Name"]) )
                                {
                                    namespaceArray.Add(connectNamespace + "\\" + obj["Name"]);
                                }
                            }
                            if( topNamespace)
                            {
                                 topNamespace = false;                                
                                sinkArray.Add(this.results);
                            }
                            else
                            {
                                sinkArray.Add(Job.GetNewSink());
                            }
                            connectArray.Add(scope);    
                            currentNamesapceCount++;
                        }
                        
                        if( (sinkArray.Count  != namespaceArray.Count) || (connectArray.Count  != namespaceArray.Count)) // not expected throw exception
                        {
                            internalException = new InvalidOperationException();
                            state = WmiState.Failed;
                            RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                            return;
                        }

                        currentNamesapceCount = 0;
                        while(currentNamesapceCount < namespaceArray.Count )
                        {
                            string connectNamespace = (string)namespaceArray[currentNamesapceCount];
                            ManagementObjectSearcher searcher = getObject.GetObjectList( (ManagementScope)connectArray[currentNamesapceCount]);
                            if( searcher == null )
                            {
                                currentNamesapceCount++;
                                continue;
                            }
                            if( topNamespace )
                            {
                                topNamespace = false;
                                searcher.Get(this.results);
                            }
                            else
                            {
                                searcher.Get((ManagementOperationObserver)sinkArray[currentNamesapceCount]);
                            }
                            currentNamesapceCount++;
                        }
                        
                    }
                    else
                    {
                        ManagementScope scope = new ManagementScope(WMIHelper.GetScopeString(this.computerName, getObject.Namespace), options);
                        scope.Connect();
                        ManagementObjectSearcher searcher = getObject.GetObjectList(scope);
                        if( searcher == null)
                            throw new ManagementException();
                        searcher.Get(this.results);
                    }

                }
                catch (ManagementException e)
                {
                    internalException = e;
                    state = WmiState.Failed;
                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    internalException = e;
                    state = WmiState.Failed;
                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                }
                catch (System.UnauthorizedAccessException e)
                {
                    internalException = e;
                    state = WmiState.Failed;
                    RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                }
                return;
            }
            string queryString = string.IsNullOrEmpty(getObject.Query) ? GetWmiQueryString() : getObject.Query;
            ObjectQuery query = new ObjectQuery(queryString.ToString());
            try
            {
                ManagementScope scope = new ManagementScope(WMIHelper.GetScopeString(this.computerName, getObject.Namespace), options);
                EnumerationOptions enumOptions = new EnumerationOptions();
                enumOptions.UseAmendedQualifiers = getObject.Amended;
                enumOptions.DirectRead = getObject.DirectRead;
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query, enumOptions);

                // Execute the WMI command for each count value.
                for (int i=0; i<this.cmdCount; ++i)
                {
                    searcher.Get(this.results);
                }
            }
            catch (ManagementException e)
            {
                internalException = e;
                state = WmiState.Failed;
                RaiseOperationCompleteEvent(null, OperationState.StopComplete);
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                internalException = e;
                state = WmiState.Failed;
                RaiseOperationCompleteEvent(null, OperationState.StopComplete);
            }
            catch (System.UnauthorizedAccessException e)
            {
                internalException = e;
                state = WmiState.Failed;
                RaiseOperationCompleteEvent(null, OperationState.StopComplete);
            }
        }
    }

    ///<summary>
    /// Event which will be triggered when WMI state is changed.
    /// Currently it is to notify Jobs that state has changed to running.
    /// Other states are notified via OperationComplete.
    ///</summary>
    internal sealed class WmiJobStateEventArgs:EventArgs
    {
        ///<summary>
        /// WMI state
        ///</summary>
        internal WmiState WmiState
        {
            get {return wmiState; }
            set{ wmiState = value;}
        }
        private WmiState wmiState;
    }

     /// <summary>
    /// Enumerated type defining the state of the WMI operation
    /// </summary>
    public enum WmiState
    {
        /// <summary>
        /// The operation has not been started
        /// </summary>
        NotStarted = 0,
        /// <summary>
        /// The operation is executing
        /// </summary>
        Running = 1,
        /// <summary>
        /// The operation is stoping execution.
        /// </summary>
        Stopping = 2,
        /// <summary>
        /// The operation is completed due to a stop request.
        /// </summary>
        Stopped = 3,
        /// <summary>
        /// The operation has completed.
        /// </summary>
        Completed = 4,
        /// <summary>
        /// The operation completed abnormally due to an error.
        /// </summary>
        Failed = 5,
    }

    internal static class WMIHelper
    {
        internal static string GetScopeString(string computer, string namespaceParameter)
        {
            StringBuilder returnValue = new StringBuilder("\\\\");
            returnValue.Append(computer);
            returnValue.Append("\\");
            returnValue.Append(namespaceParameter);
            return returnValue.ToString();
        }
    }
    #endregion Helper Classes


     /// <summary>
    /// A class to set WMI connection options
    /// </summary>
    public class WmiBaseCmdlet:Cmdlet
    {
        #region Parameters

         /// <summary>
        /// Perform Async operation
        /// </summary>
        [Parameter]
        public SwitchParameter AsJob
        {
            get { return this.async; }
            set { this.async = value; }
        }
        
        /// <summary>
        /// The Impersonation level to use
        /// </summary>
        [Parameter(ParameterSetName = "path")]
        [Parameter(ParameterSetName = "class")]
        [Parameter(ParameterSetName ="WQLQuery")]
        [Parameter(ParameterSetName ="query")]  
        [Parameter(ParameterSetName ="list")]                 
        public ImpersonationLevel Impersonation
        {
            get { return this.impersonationLevel; }
            set { this.impersonationLevel = value; }
        }
        /// <summary>
        /// The Authentication level to use
        /// </summary>
        [Parameter(ParameterSetName = "path")]
        [Parameter(ParameterSetName = "class")]
        [Parameter(ParameterSetName ="WQLQuery")]
        [Parameter(ParameterSetName ="query")]  
        [Parameter(ParameterSetName ="list")]                 
        public AuthenticationLevel Authentication
        {
            get { return this.authenticationLevel; }
            set { this.authenticationLevel = value; }
        }

        /// <summary>
        /// The Locale to use
        /// </summary>
        [Parameter(ParameterSetName = "path")]
        [Parameter(ParameterSetName = "class")]
        [Parameter(ParameterSetName ="WQLQuery")]
        [Parameter(ParameterSetName ="query")]  
        [Parameter(ParameterSetName ="list")]                 
        public string Locale
        {
            get { return this.locale; }
            set { this.locale = value; }
        }
        /// <summary>
        /// If all Privileges are enabled
        /// </summary>
        [Parameter(ParameterSetName = "path")]
        [Parameter(ParameterSetName = "class")]
        [Parameter(ParameterSetName ="WQLQuery")]
        [Parameter(ParameterSetName ="query")]  
        [Parameter(ParameterSetName ="list")]                 
        public SwitchParameter EnableAllPrivileges
        {
            get { return enableAllPrivileges; }
            set { enableAllPrivileges = value; }
        }

        /// <summary>
        /// The Authority to use
        /// </summary>
        [Parameter(ParameterSetName = "path")]
        [Parameter(ParameterSetName = "class")]
        [Parameter(ParameterSetName ="WQLQuery")]
        [Parameter(ParameterSetName ="query")]  
        [Parameter(ParameterSetName ="list")]                 
        public string Authority
        {
            get { return authority; }
            set { authority = value; }
        }
        /// <summary>
        /// The credential to use
        /// </summary>
        [Parameter(ParameterSetName = "path")]
        [Parameter(ParameterSetName = "class")]
        [Parameter(ParameterSetName ="WQLQuery")]
        [Parameter(ParameterSetName ="query")]  
        [Parameter(ParameterSetName ="list")]                 
        [Credential()]
        public PSCredential Credential
        {
            get { return this.credential; }
            set { this.credential = value; }
        }
        /// <summary>
        /// The credential to use
        /// </summary>
        [Parameter]
        public Int32 ThrottleLimit
        {
            get { return this.throttleLimit; }
            set { this.throttleLimit = value; }
        }
        /// <summary>
        /// The ComputerName in which to query
        /// </summary>
        [Parameter(ParameterSetName = "path")]
        [Parameter(ParameterSetName = "class")]
        [Parameter(ParameterSetName ="WQLQuery")]
        [Parameter(ParameterSetName ="query")]  
        [Parameter(ParameterSetName ="list")]                 
        [ValidateNotNullOrEmpty]
        [Alias("Cn")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ComputerName
        {
            get { return this.computerName; }
            set { this.computerName = value; serverNameSpecified = true; }
        }
         /// <summary>
        /// The WMI namespace to use
        /// </summary>
        [Parameter(ParameterSetName = "path")]
        [Parameter(ParameterSetName = "class")]
        [Parameter(ParameterSetName ="WQLQuery")]
        [Parameter(ParameterSetName ="query")]  
        [Parameter(ParameterSetName ="list")]   
        [Alias("NS")]
        public string Namespace
        {
            get { return this.nameSpace; }
            set { this.nameSpace = value; namespaceSpecified = true;}
        }
        #endregion Parameters
        
        #region parameter data
        /// <summary>
        /// The computer to query.
        /// </summary>
        private string[] computerName = new string[] { "localhost" };
         /// <summary>
        /// WMI namespace
        /// </summary>
        private string nameSpace = "root\\cimv2";
         /// <summary>
        /// Specify if namespace was specified or not.
        /// </summary>
        internal bool namespaceSpecified = false;
        /// <summary>
        /// Specify if server name was specified or not.
        /// </summary>
        internal bool serverNameSpecified = false;
        /// <summary>
        /// The credential to use
        /// </summary>
        private PSCredential credential;
        /// <summary>
        /// The Impersonation level to use
        /// </summary>
        private ImpersonationLevel impersonationLevel = ImpersonationLevel.Impersonate;
        /// <summary>
        /// The Authentication level to use
        /// </summary>
        private AuthenticationLevel authenticationLevel = AuthenticationLevel.PacketPrivacy;
        /// <summary>
        /// The Locale to use
        /// </summary>
        private string locale = null;
        /// <summary>
        /// If all Privileges are enabled
        /// </summary>
        private SwitchParameter enableAllPrivileges;
        /// <summary>
        /// The Authority to use
        /// </summary>
        private string authority = null;
        /// <summary>
        /// To perform async operation
        /// </summary>        
        private SwitchParameter async = false;
         /// <summary>
        /// Set throttle limit
        /// </summary>
        private Int32 throttleLimit = DEFAULT_THROTTLE_LIMIT;
        private static int DEFAULT_THROTTLE_LIMIT = 32;    // maximum number of items to be processed at a time

        #endregion parameter data

        #region Command code
        /// <summary>
        /// Get connection options
        /// </summary>
        internal ConnectionOptions GetConnectionOption()
        {
            ConnectionOptions options;
            options = new ConnectionOptions();
            options.Authentication = this.Authentication;
            options.Locale = this.Locale;
            options.Authority = this.Authority;
            options.EnablePrivileges = this.EnableAllPrivileges;
            options.Impersonation = this.Impersonation;
            if (this.Credential != null)
            {
                if( !(this.Credential.UserName == null  && this.Credential.Password == null) ) // Empty credential, use implicit credential
                {
                    options.Username = this.Credential.UserName;
                    options.SecurePassword = this.Credential.Password;
                }
            }
            return options;
        }
        /// <summary>
        /// Set wmi instance helper
        /// </summary>
        internal ManagementObject SetWmiInstanceGetObject(ManagementPath mPath,string serverName)
        {
            ConnectionOptions options = GetConnectionOption();
            ManagementObject mObject = null;
            if( this.GetType() == typeof(SetWmiInstance) )
            {
                 SetWmiInstance setObject = (SetWmiInstance)this;
                 if (setObject.Path != null)
                {
                    mPath.Server = serverName;
                    ManagementScope mScope = new ManagementScope(mPath, options);
                    if (mPath.IsClass)
                    {
                        ManagementClass mClass = new ManagementClass(mPath);
                        mClass.Scope = mScope;
                        mObject = mClass.CreateInstance();
                    }
                    else
                    {
                        //This can throw if path does not exist caller should catch it.
                        ManagementObject mInstance = new ManagementObject(mPath);
                        mInstance.Scope = mScope;
                        try
                        {
                            mInstance.Get();
                        }
                        catch (ManagementException e)
                        {
                            if (e.ErrorCode != ManagementStatus.NotFound)
                            {
                                throw;
                            }
                            int namespaceIndex = setObject.Path.IndexOf(':');
                            if( namespaceIndex == -1 )
                            {
                                throw;
                            }
                            int classIndex = (setObject.Path.Substring(namespaceIndex)).IndexOf('.');
                            if( classIndex == -1 )
                            {
                                throw;
                            }
                            //Get class object and create instance.
                            string newPath = setObject.Path.Substring(0,classIndex+namespaceIndex);
                            ManagementPath classPath = new ManagementPath(newPath);
                            ManagementClass mClass = new ManagementClass(classPath);
                            mClass.Scope = mScope;
                            mInstance = mClass.CreateInstance();
                        }
                        mObject = mInstance;
                    }
                }
                else
                {
                    ManagementScope scope = new ManagementScope(WMIHelper.GetScopeString(serverName, setObject.Namespace), options);
                    ManagementClass mClass = new ManagementClass(setObject.Class);
                    mClass.Scope = scope;
                    mObject = mClass.CreateInstance();
                }
                if (setObject.Arguments != null)
                {
                    IDictionaryEnumerator en = setObject.Arguments.GetEnumerator();
                    while (en.MoveNext())
                    {
                        mObject[en.Key as string] = en.Value;
                    }
                }
            }
            return mObject;
        }
        /// <summary>
        /// Set wmi instance helper for building management path
        /// </summary>
        internal ManagementPath SetWmiInstanceBuildManagementPath()
        {
            ManagementPath mPath = null;
             if( this.GetType() == typeof(SetWmiInstance) )
             {
                SetWmiInstance wmiInstance = (SetWmiInstance)this;
                //If Class is specified only CreateOnly flag is supported
                if( wmiInstance.Class != null)
                {
                    if( wmiInstance.flagSpecified && wmiInstance.PutType != PutType.CreateOnly)
                    {
                        //Throw Terminating error   
                       ThrowTerminatingError(new ErrorRecord(
                        new InvalidOperationException(),
                        "CreateOnlyFlagNotSpecifiedWithClassPath",
                        ErrorCategory.InvalidOperation,
                        wmiInstance.PutType));
                    }
                    wmiInstance.PutType = PutType.CreateOnly;
                }
                else
                {
                    mPath = new ManagementPath(wmiInstance.Path);
                    if (String.IsNullOrEmpty(mPath.NamespacePath))
                    {
                        mPath.NamespacePath = wmiInstance.Namespace;
                    }
                    else if (wmiInstance.namespaceSpecified)
                    {
                        //ThrowTerminatingError
                        ThrowTerminatingError(new ErrorRecord(
                            new InvalidOperationException(),
                            "NamespaceSpecifiedWithPath",
                            ErrorCategory.InvalidOperation,
                            wmiInstance.Namespace));
                    }

                    if (mPath.Server != "." && wmiInstance.serverNameSpecified)
                    {
                        //ThrowTerminatingError
                        ThrowTerminatingError(new ErrorRecord(
                            new InvalidOperationException(),
                            "ComputerNameSpecifiedWithPath",
                            ErrorCategory.InvalidOperation,
                            wmiInstance.ComputerName));
                    }
                    if( mPath.IsClass)
                    {
                        if( wmiInstance.flagSpecified && wmiInstance.PutType != PutType.CreateOnly)
                        {
                           //Throw Terminating error   
                           ThrowTerminatingError(new ErrorRecord(
                            new InvalidOperationException(),
                            "CreateOnlyFlagNotSpecifiedWithClassPath",
                            ErrorCategory.InvalidOperation,
                            wmiInstance.PutType));
                        }
                        wmiInstance.PutType = PutType.CreateOnly;
                    }
                    else
                    {
                        if( wmiInstance.flagSpecified )
                        {
                            if( !(wmiInstance.PutType == PutType.UpdateOnly || wmiInstance.PutType == PutType.UpdateOrCreate))
                            {
                                //Throw terminating error
                                ThrowTerminatingError(new ErrorRecord(
                                new InvalidOperationException(),
                                "NonUpdateFlagSpecifiedWithInstancePath",
                                ErrorCategory.InvalidOperation,
                                wmiInstance.PutType));
                            }
                        }
                        else
                        {
                            wmiInstance.PutType = PutType.UpdateOrCreate;
                        }
                    }
                }
            }
            return mPath;
        }

        /// <summary>
        /// Set wmi instance helper for pipeline input
        /// </summary>
        internal ManagementObject SetWmiInstanceGetPipelineObject()
        {
            //Should only be called from Set-WMIInstance cmdlet
            ManagementObject mObj = null;
            if( this.GetType() == typeof(SetWmiInstance) )
            {
                SetWmiInstance wmiInstance = (SetWmiInstance)this;
                //Extra check
                if(wmiInstance.InputObject != null)
                {
                    if (wmiInstance.InputObject.GetType() == typeof(ManagementClass))
                    {
                        //Check if Flag specified is CreateOnly or not
                        if( wmiInstance.flagSpecified && wmiInstance.PutType != PutType.CreateOnly)
                        {
                            //Throw terminating error
                            ThrowTerminatingError(new ErrorRecord(
                            new InvalidOperationException(),
                            "CreateOnlyFlagNotSpecifiedWithClassPath",
                            ErrorCategory.InvalidOperation,
                            wmiInstance.PutType));
                        }
                        mObj = ((ManagementClass)wmiInstance.InputObject).CreateInstance();
                        wmiInstance.PutType = PutType.CreateOnly;
                    }
                    else
                    {
                        //Check if Flag specified is Updateonly or UpdateOrCreateOnly or not
                        if( wmiInstance.flagSpecified )
                        {
                            if( !(wmiInstance.PutType == PutType.UpdateOnly || wmiInstance.PutType == PutType.UpdateOrCreate))
                            {
                                //Throw terminating error
                                ThrowTerminatingError(new ErrorRecord(
                                new InvalidOperationException(),
                                "NonUpdateFlagSpecifiedWithInstancePath",
                                ErrorCategory.InvalidOperation,
                                wmiInstance.PutType));
                            }
                        }
                        else
                        {
                            wmiInstance.PutType = PutType.UpdateOrCreate;
                        }
                        
                        mObj = (ManagementObject)wmiInstance.InputObject.Clone();
                    }
                    if (wmiInstance.Arguments != null)
                    {
                        IDictionaryEnumerator en = wmiInstance.Arguments.GetEnumerator();
                        while (en.MoveNext())
                        {
                            mObj[en.Key as string] = en.Value;
                        }
                    }
                }
            }
            return mObj;
        }

        /// <summary>
        /// Start this cmdlet as a WMI job...
        /// </summary>
        internal void RunAsJob(string cmdletName)
        {
            PSWmiJob wmiJob = new PSWmiJob(this, ComputerName, this.ThrottleLimit, Job.GetCommandTextFromInvocationInfo(this.MyInvocation));
            if (_context != null)
            {
                ((System.Management.Automation.Runspaces.LocalRunspace)_context.CurrentRunspace).JobRepository.Add(wmiJob);
            }
            WriteObject(wmiJob);
        }
        // Get the PowerShell execution context if it's available at cmdlet creation time...
        System.Management.Automation.ExecutionContext _context = System.Management.Automation.Runspaces.LocalPipeline.GetExecutionContextFromTLS();

        #endregion Command code
    }
     /// <summary>
    /// A class to perform async operations for WMI cmdlets
    /// </summary>

    internal class PSWmiJob : Job
    {
        #region internal constructor

        /// <summary>
        ///Internal constructor for initializing WMI jobs
        /// </summary>
        internal PSWmiJob(Cmdlet cmds, string[] computerName, int throttleLimt, string command)
        :base(command,null)
        {
            PSJobTypeName = WMIJobType;
            throttleManager.ThrottleLimit = throttleLimt;
            for( int i=0; i< computerName.Length ; i++)
            {
                PSWmiChildJob job = new PSWmiChildJob(cmds,computerName[i],throttleManager);
                job.StateChanged += new EventHandler<JobStateEventArgs>(HandleChildJobStateChanged);
                job.JobUnblocked += new EventHandler(HandleJobUnblocked);
                ChildJobs.Add(job);
            }
            CommonInit(throttleLimt);
        }

        /// <summary>
        /// Internal constructor for initializing WMI jobs, where WMI command is executed a variable
        /// number of times.
        /// </summary>
        internal PSWmiJob(Cmdlet cmds, string[] computerName, int throttleLimit, string command, int count)
            : base(command, null)
        {
            PSJobTypeName = WMIJobType;
            throttleManager.ThrottleLimit = throttleLimit;
            for (int i = 0; i < computerName.Length; ++i)
            {
                PSWmiChildJob childJob = new PSWmiChildJob(cmds, computerName[i], throttleManager, count);
                childJob.StateChanged += new EventHandler<JobStateEventArgs>(HandleChildJobStateChanged);
                childJob.JobUnblocked += new EventHandler(HandleJobUnblocked);
                ChildJobs.Add(childJob);
            }

            CommonInit(throttleLimit);
        }

        #endregion internal constructor

        // Set to true when at least one chil job failed
        private bool atleastOneChildJobFailed = false;

        // Count the number of childs which have finished
        private int finishedChildJobsCount = 0;

        // Count of number of child jobs which are blocked
        private int blockedChildJobsCount = 0;

        // WMI Job type name.
        private const string WMIJobType = "WmiJob";

        /// <summary>
        /// Handles the StateChanged event from each of the child job objects
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleChildJobStateChanged(object sender, JobStateEventArgs e)
        {
            if (e.JobStateInfo.State == JobState.Blocked)
            {
                // increment count of blocked child jobs
                lock (syncObject)
                {
                    blockedChildJobsCount++;
                }
                // if any of the child job is blocked, we set state to blocked
                SetJobState(JobState.Blocked, null);
                return;
            }

            //Ignore state changes which are not resulting in state change to finished.
            if ( (!IsFinishedState(e.JobStateInfo.State)) || (e.JobStateInfo.State == JobState.NotStarted))
            {
                return;
            }
            if (e.JobStateInfo.State == JobState.Failed)
            {
                //If any of the child job failed, we set status to failed
                atleastOneChildJobFailed = true;
            }

            bool allChildJobsFinished = false;
            lock (syncObject)
            {
                finishedChildJobsCount++;

                //We are done
                if (finishedChildJobsCount == ChildJobs.Count)
                {
                    allChildJobsFinished = true;
                }
            }
            if (allChildJobsFinished)
            {
                //if any child job failed, set status to failed
                //If stop was called set, status to stopped
                //else completed
                if (atleastOneChildJobFailed)
                {
                    SetJobState(JobState.Failed);
                }
                else if (_stopIsCalled == true)
                {
                    SetJobState(JobState.Stopped);
                }
                else
                {
                    SetJobState(JobState.Completed);
                }
            }
        }

        private bool _stopIsCalled = false;
        private string statusMessage;
        /// <summary>
        /// Message indicating status of the job
        /// </summary>
        public override string StatusMessage
        {
            get
            {
                return statusMessage;
            }
        }
        //ISSUE: Implement StatusMessage
        /// <summary>
        /// Checks the status of remote command execution
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private void SetStatusMessage()
        {
            statusMessage = "test";

        } // SetStatusMessage

        private bool moreData = false;
        /// <summary>
        /// indicates if more data is available
        /// </summary>
        /// <remarks>
        /// This has more data if any of the child jobs have more data.
        /// </remarks>
        public override bool HasMoreData
        {
            get
            {
                // moreData is set to false and will be set to true
                //if at least one child is has more data.

                //if ( (!moreData))
                //{
                    bool atleastOneChildHasMoreData = false;

                    for (int i = 0; i < ChildJobs.Count; i++)
                    {
                        if (ChildJobs[i].HasMoreData)
                        {
                            atleastOneChildHasMoreData = true;
                            break;
                        }
                    }

                    moreData = atleastOneChildHasMoreData;
                //}

                return moreData;
            }
        }    

        /// <summary>
        /// Computers on which this job is running
        /// </summary>
        public override string Location
        {
            get 
            {
                return ConstructLocation();
            }
        }
         private String ConstructLocation()
        {
            StringBuilder location = new StringBuilder();

            foreach (PSWmiChildJob job in ChildJobs)
            {
                location.Append(job.Location);
                location.Append(",");
            }
            location.Remove(location.Length - 1, 1);

            return location.ToString();
        }
        /// <summary>
        /// Stop Job 
        /// </summary>
        public override void StopJob()
        {
            //AssertNotDisposed();

            if (!IsFinishedState(JobStateInfo.State))
            {
                _stopIsCalled = true;

                throttleManager.StopAllOperations();

                Finished.WaitOne();
            }
        }
        /// <summary>
        /// Release all the resources. 
        /// </summary>
        /// <param name="disposing">
        /// if true, release all the managed objects.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!isDisposed)
                {
                    isDisposed = true;
                    try
                    {
                        if (!IsFinishedState(JobStateInfo.State))
                        {
                            StopJob();
                        }
                        throttleManager.Dispose();
                        foreach (Job job in ChildJobs)
                        {
                            job.Dispose();
                        }
                    }
                    finally
                    {
                        base.Dispose(disposing);
                    }
                }                    
            }
        }
        
        private bool isDisposed = false;
        /// <summary>
        /// Initialization common to both constructors
        /// </summary>
        void CommonInit(int throttleLimit)
        {
            //Since no results are produced by any streams. We should
            //close all the streams
            base.CloseAllStreams();

            // set status to "in progress"
            SetJobState(JobState.Running);

            // submit operations to the throttle manager
            throttleManager.EndSubmitOperations();
        }
        /// <summary>
        /// Handles JobUnblocked event from a child job and decrements
        /// count of blocked child jobs. When count reaches 0, sets the
        /// state of the parent job to running
        /// </summary>
        /// <param name="sender">sender of this event, unused</param>
        /// <param name="eventArgs">event arguments, should be empty in this
        /// case</param>
        private void HandleJobUnblocked(object sender, EventArgs eventArgs)
        {
            bool unblockjob = false;

            lock (syncObject)
            {
                blockedChildJobsCount--;

                if (blockedChildJobsCount == 0)
                {
                    unblockjob = true;
                }
            }

            if (unblockjob)
            {
                SetJobState(JobState.Running, null);
            }
        }


        
        private ThrottleManager throttleManager = new ThrottleManager();

        private new object syncObject = new object();           // sync object
    }
    
     /// <summary>
    /// Class for WmiChildJob object. This job object Execute wmi cmdlet
    /// </summary>
    internal class PSWmiChildJob : Job
    {
        #region internal constructor

        /// <summary>
        /// Internal constructor for initializing WMI jobs
        /// </summary>
        internal PSWmiChildJob(Cmdlet cmds, string computerName, ThrottleManager throttleManager)
        :base(null,null)
        {
            UsesResultsCollection = true;
            this.computerName = computerName;
            this.throttleManager = throttleManager;
            wmiSinkArray = new ArrayList();
            ManagementOperationObserver wmiSink = new ManagementOperationObserver();
            wmiSinkArray.Add( wmiSink);
            sinkCompleted++;
            wmiSink.ObjectReady += new ObjectReadyEventHandler(this.NewObject);
            wmiSink.Completed += new CompletedEventHandler(this.JobDone);
            this.helper = new WmiAsyncCmdletHelper(this, cmds, computerName, wmiSink);
            helper.WmiOperationState += new EventHandler<WmiJobStateEventArgs>(HandleWMIState);
            helper.ShutdownComplete += new EventHandler<EventArgs>(JobDoneForWin32Shutdown);
            SetJobState(JobState.NotStarted);
            IThrottleOperation operation = this.helper;
            operation.OperationComplete += new EventHandler<OperationStateEventArgs>(HandleOperationComplete);
            throttleManager.ThrottleComplete += new EventHandler<EventArgs>(HandleThrottleComplete);
            throttleManager.AddOperation(operation);
        }

        /// <summary>
        /// Internal constructor for initializing WMI jobs, where WMI command is executed a variable
        /// number of times.
        /// </summary>
        internal PSWmiChildJob(Cmdlet cmds, string computerName, ThrottleManager throttleManager, int count)
            : base(null, null)
        {
            UsesResultsCollection = true;
            this.computerName = computerName;
            this.throttleManager = throttleManager;
            wmiSinkArray = new ArrayList();
            ManagementOperationObserver wmiSink = new ManagementOperationObserver();
            wmiSinkArray.Add(wmiSink);
            sinkCompleted += count;
            wmiSink.ObjectReady += new ObjectReadyEventHandler(this.NewObject);
            wmiSink.Completed += new CompletedEventHandler(this.JobDone);
            this.helper = new WmiAsyncCmdletHelper(this, cmds, computerName, wmiSink, count);
            helper.WmiOperationState += new EventHandler<WmiJobStateEventArgs>(HandleWMIState);
            helper.ShutdownComplete += new EventHandler<EventArgs>(JobDoneForWin32Shutdown);
            SetJobState(JobState.NotStarted);
            IThrottleOperation operation = this.helper;
            operation.OperationComplete += new EventHandler<OperationStateEventArgs>(HandleOperationComplete);
            throttleManager.ThrottleComplete += new EventHandler<EventArgs>(HandleThrottleComplete);
            throttleManager.AddOperation(operation);
        }

        #endregion internal constructor

        private string computerName;
        private WmiAsyncCmdletHelper helper;
        //bool _bFinished;
        private ThrottleManager throttleManager;
        private new object syncObject = new object();           // sync object  
        private int sinkCompleted;
        private bool bJobFailed;
        private bool bAtLeastOneObject;
        

        private ArrayList wmiSinkArray;
        /// <summary>
        /// Event raised by this job to indicate to its parent that
        /// its now unblocked by the user
        /// </summary>
        internal event EventHandler JobUnblocked;
        
        /// <summary>
        /// Set the state of the current job from blocked to
        /// running and raise an event indicating to this
        /// parent job that this job is unblocked
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]        
        internal void UnblockJob()
        {
            SetJobState(JobState.Running, null);
            JobUnblocked.SafeInvoke(this, EventArgs.Empty);
        }
        internal ManagementOperationObserver GetNewSink()
        {
            ManagementOperationObserver wmiSink = new ManagementOperationObserver();
            wmiSinkArray.Add( wmiSink);
             lock (syncObject)
            {
                sinkCompleted++;
            }
            wmiSink.ObjectReady += new ObjectReadyEventHandler(this.NewObject);
            wmiSink.Completed += new CompletedEventHandler(this.JobDone);
             return wmiSink;
        }
    
        /// <summary>
        /// it recieves Management objects
        /// </summary>
        private void NewObject(object sender, ObjectReadyEventArgs obj)
        {
            if( !bAtLeastOneObject)
            {
                bAtLeastOneObject = true;
            }
            this.WriteObject(obj.NewObject);
        }
        
        /// <summary>
        /// It is called when WMI job is done.
        /// </summary>
        private void JobDone(object sender, CompletedEventArgs obj)
        {
            lock (syncObject)
            {
                sinkCompleted--;
            }
            if( obj.Status != ManagementStatus.NoError)
            {
                bJobFailed = true;
            }
            if( sinkCompleted == 0 )
            {
                //Notify throttle manager and change the state to complete
                //Two cases where _bFinished should be set to false.
                // 1) Invalid class or some other condition so that after making a connection WMI is throwing an error
                // 2) We could not get any instance for the class.
                /*if(bAtLeastOneObject )
                    _bFinished = true;*/
                helper.RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                if(  !bJobFailed )
                {
                    helper.State = WmiState.Completed;
                    SetJobState(JobState.Completed);
                }
                else
                {
                    helper.State = WmiState.Failed;
                    SetJobState(JobState.Failed);
                }
            }

        }

        /// <summary>
        /// It is called when the call to Win32shutdown is successfully completed
        /// </summary>
        private void JobDoneForWin32Shutdown(object sender, EventArgs arg)
        {
            lock (syncObject)
            {
                sinkCompleted--;
            }
            if (sinkCompleted == 0)
            {
                helper.RaiseOperationCompleteEvent(null, OperationState.StopComplete);
                helper.State = WmiState.Completed;
                SetJobState(JobState.Completed);
            }
        }

        private string statusMessage = "test";
        /// <summary>
        /// Message indicating status of the job
        /// </summary>
        public override string StatusMessage
        {
            get
            {
                return statusMessage;
            }
        }


        /// <summary>
        /// Indicates if there is more data available in
        /// this Job
        /// </summary>
        public override bool HasMoreData
        {
            get
            {
                return (Results.IsOpen || Results.Count > 0);
            }
        }

        /// <summary>
        /// Returns the computer on which this command is
        /// running
        /// </summary>
        public override string Location
        {
            get 
            {
                return computerName;
            }
        }
        
        /// <summary>
        /// Stops the job
        /// </summary>
        public override void StopJob()
        {
            AssertNotDisposed();
            throttleManager.StopOperation(helper);

            // if IgnoreStop is set, then StopOperation will
            // return immediately, but StopJob should only
            // return when job is complete. Waiting on the
            // wait handle will ensure that its blocked
            // until the job reaches a terminal state
            Finished.WaitOne();
        }

        /// <summary>
        /// Release all the resources. 
        /// </summary>
        /// <param name="disposing">
        /// if true, release all the managed objects.
        /// </param>
        protected override void Dispose(bool disposing)
        {

            if (disposing)
            {
                if (!isDisposed)
                {
                    isDisposed = true;
                    base.Dispose(disposing);
                }
            }
        }
        bool isDisposed;

        /// <summary>
        /// Handles operation complete event
        /// </summary>
        private void HandleOperationComplete(object sender, OperationStateEventArgs stateEventArgs)
        {
            WmiAsyncCmdletHelper helper = (WmiAsyncCmdletHelper) sender;
            
            if (helper.State == WmiState.NotStarted)
            {
                //This is a case WMI operation was not started.
                SetJobState(JobState.Stopped,helper.InternalException);
            }
            else if (helper.State == WmiState.Running)
            {
                SetJobState(JobState.Running,helper.InternalException);
            }
            else if (helper.State == WmiState.Completed)
            {
                SetJobState(JobState.Completed,helper.InternalException);
            }
            else if(helper.State == WmiState.Failed)
            {
                SetJobState(JobState.Failed,helper.InternalException);  
            }
            else
            {
                SetJobState(JobState.Stopped,helper.InternalException);
            }
        }
        /// <summary>
        /// Handles WMI state changed
        /// </summary>
        private void HandleWMIState(object sender ,WmiJobStateEventArgs stateEventArgs)
        {
            if(stateEventArgs.WmiState == WmiState.Running)
            {
                SetJobState(JobState.Running, helper.InternalException);
            }
            else if(stateEventArgs.WmiState == WmiState.NotStarted)
            {
                SetJobState(JobState.NotStarted, helper.InternalException);
            }
            else if(stateEventArgs.WmiState == WmiState.Completed)
            {
                SetJobState(JobState.Completed);
            }
            else if(stateEventArgs.WmiState == WmiState.Failed)
            {
                SetJobState(JobState.Failed,helper.InternalException);
            }
            else
            {
                SetJobState(JobState.Stopped,helper.InternalException);
            }
        }
       
        /// <summary>
        /// Handle a throttle complete event
        /// </summary>
        /// <param name="sender">sender of this event</param>
        /// <param name="eventArgs">not used in this method</param>
        private void HandleThrottleComplete(object sender, EventArgs eventArgs)
        {
            if (helper.State == WmiState.NotStarted)
            {
                //This is a case WMI operation was not started.
                SetJobState(JobState.Stopped,helper.InternalException);
            }
            else if (helper.State == WmiState.Running)
            {
                SetJobState(JobState.Running,helper.InternalException);
            }
            else if (helper.State == WmiState.Completed)
            {
                SetJobState(JobState.Completed,helper.InternalException);
            }
            else if(helper.State == WmiState.Failed)
            {
                SetJobState(JobState.Failed,helper.InternalException);  
            }
            else
            {
                SetJobState(JobState.Stopped,helper.InternalException);
            }
            //Do Nothing
        } // HandleThrottleComplete
    }
}

#endif
