// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management;
using System.Text;
using System.Management.Automation.Provider;
using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;
using System.Security.AccessControl;
using System.Runtime.InteropServices;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command to Invoke WMI Method.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "WmiMethod", DefaultParameterSetName = "class", SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113346", RemotingCapability = RemotingCapability.OwnedByCommand)]
    public sealed class InvokeWmiMethod : WmiBaseCmdlet
    {
        #region Parameters
        /// <summary>
        /// The WMI Object to use.
        /// </summary>
        [Parameter(ValueFromPipeline = true, Mandatory = true, ParameterSetName = "object")]
        public ManagementObject InputObject
        {
            get { return _inputObject; }

            set { _inputObject = value; }
        }
        /// <summary>
        /// The WMI Path to use.
        /// </summary>
        [Parameter(ParameterSetName = "path", Mandatory = true)]
        public string Path
        {
            get { return _path; }

            set { _path = value; }
        }
        /// <summary>
        /// The WMI class to use.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "class")]
        public string Class
        {
            get { return _className; }

            set { _className = value; }
        }
        /// <summary>
        /// The WMI Method to execute.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true)]
        public string Name
        {
            get { return _methodName; }

            set { _methodName = value; }
        }

        /// <summary>
        /// The parameters to the method specified by MethodName.
        /// </summary>
        [Parameter(ParameterSetName = "path")]
        [Parameter(Position = 2, ParameterSetName = "class")]
        [Parameter(ParameterSetName = "object")]
        [Alias("Args")]
        public object[] ArgumentList
        {
            get { return _argumentList; }

            set { _argumentList = value; }
        }

        #endregion Parameters

        #region parameter data
        private string _path = null;
        private string _className = null;
        private string _methodName = null;
        private ManagementObject _inputObject = null;
        private object[] _argumentList = null;

        #endregion parameter data
        #region Command code
        /// <summary>
        /// Invoke WMI method given either path,class name or pipeline input.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (this.AsJob)
            {
                RunAsJob("Invoke-WMIMethod");
                return;
            }

            if (_inputObject != null)
            {
                object result = null;
                ManagementBaseObject inputParameters = null;
                try
                {
                    inputParameters = _inputObject.GetMethodParameters(_methodName);
                    if (_argumentList != null)
                    {
                        int inParamCount = _argumentList.Length;
                        foreach (PropertyData property in inputParameters.Properties)
                        {
                            if (inParamCount == 0)
                                break;
                            property.Value = _argumentList[_argumentList.Length - inParamCount];
                            inParamCount--;
                        }
                    }

                    if (!ShouldProcess(
                       StringUtil.Format(WmiResources.WmiMethodNameForConfirmation,
                       _inputObject["__CLASS"].ToString(),
                       this.Name)
                   ))
                    {
                        return;
                    }

                    result = _inputObject.InvokeMethod(_methodName, inputParameters, null);
                }
                catch (ManagementException e)
                {
                    ErrorRecord errorRecord = new ErrorRecord(e, "InvokeWMIManagementException", ErrorCategory.InvalidOperation, null);
                    WriteError(errorRecord);
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    ErrorRecord errorRecord = new ErrorRecord(e, "InvokeWMICOMException", ErrorCategory.InvalidOperation, null);
                    WriteError(errorRecord);
                }

                if (result != null)
                {
                    WriteObject(result);
                }

                return;
            }
            else
            {
                ConnectionOptions options = GetConnectionOption();
                ManagementPath mPath = null;
                object result = null;
                ManagementObject mObject = null;
                if (_path != null)
                {
                    mPath = new ManagementPath(_path);
                    if (string.IsNullOrEmpty(mPath.NamespacePath))
                    {
                        mPath.NamespacePath = this.Namespace;
                    }
                    else if (namespaceSpecified)
                    {
                        // ThrowTerminatingError
                        ThrowTerminatingError(new ErrorRecord(
                            new InvalidOperationException(),
                            "NamespaceSpecifiedWithPath",
                            ErrorCategory.InvalidOperation,
                            this.Namespace));
                    }

                    if (mPath.Server != "." && serverNameSpecified)
                    {
                        // ThrowTerminatingError
                        ThrowTerminatingError(new ErrorRecord(
                            new InvalidOperationException(),
                            "ComputerNameSpecifiedWithPath",
                            ErrorCategory.InvalidOperation,
                            ComputerName));
                    }
                    // If server name is specified loop through it.
                    if (!(mPath.Server == "." && serverNameSpecified))
                    {
                        string[] serverName = new string[] { mPath.Server };
                        ComputerName = serverName;
                    }
                }

                foreach (string name in ComputerName)
                {
                    result = null;
                    try
                    {
                        if (_path != null)
                        {
                            mPath.Server = name;
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
                            ManagementScope scope = new ManagementScope(WMIHelper.GetScopeString(name, this.Namespace), options);
                            ManagementClass mClass = new ManagementClass(_className);
                            mObject = mClass;
                            mObject.Scope = scope;
                        }

                        ManagementBaseObject inputParameters = mObject.GetMethodParameters(_methodName);
                        if (_argumentList != null)
                        {
                            int inParamCount = _argumentList.Length;
                            foreach (PropertyData property in inputParameters.Properties)
                            {
                                if (inParamCount == 0)
                                    break;
                                object argument = PSObject.Base(_argumentList[_argumentList.Length - inParamCount]);
                                if (property.IsArray)
                                {
                                    property.Value = MakeBaseObjectArray(argument);
                                }
                                else
                                {
                                    property.Value = argument;
                                }

                                inParamCount--;
                            }
                        }

                        if (!ShouldProcess(
                                StringUtil.Format(WmiResources.WmiMethodNameForConfirmation,
                           mObject["__CLASS"].ToString(),
                           this.Name)
                       ))
                        {
                            return;
                        }

                        result = mObject.InvokeMethod(_methodName, inputParameters, null);
                    }
                    catch (ManagementException e)
                    {
                        ErrorRecord errorRecord = new ErrorRecord(e, "InvokeWMIManagementException", ErrorCategory.InvalidOperation, null);
                        WriteError(errorRecord);
                    }
                    catch (System.Runtime.InteropServices.COMException e)
                    {
                        ErrorRecord errorRecord = new ErrorRecord(e, "InvokeWMICOMException", ErrorCategory.InvalidOperation, null);
                        WriteError(errorRecord);
                    }

                    if (result != null)
                    {
                        WriteObject(result);
                    }
                }
            }
        }

        /// <summary>
        /// Ensure that the argument is a collection containing no PSObjects.
        /// </summary>
        /// <param name="argument"></param>
        /// <returns></returns>
        private static object MakeBaseObjectArray(object argument)
        {
            if (argument == null)
                return null;

            IList listArgument = argument as IList;
            if (listArgument == null)
            {
                return new object[] { argument };
            }

            bool needCopy = false;
            foreach (object argElement in listArgument)
            {
                if (argElement is PSObject)
                {
                    needCopy = true;
                    break;
                }
            }

            if (needCopy)
            {
                var copiedArgument = new object[listArgument.Count];
                int index = 0;
                foreach (object argElement in listArgument)
                {
                    copiedArgument[index++] = argElement != null ? PSObject.Base(argElement) : null;
                }

                return copiedArgument;
            }
            else
            {
                return argument;
            }
        }

        #endregion Command code
    }
}
