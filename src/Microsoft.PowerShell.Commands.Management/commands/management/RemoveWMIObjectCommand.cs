//
//    Copyright (C) Microsoft.  All rights reserved.
//
using System;
using System.Management;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command to Remove WMI Object
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "WmiObject", DefaultParameterSetName = "class",SupportsShouldProcess = true,
        HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113381", RemotingCapability = RemotingCapability.OwnedByCommand)]
    public class RemoveWmiObject : WmiBaseCmdlet
    {
        #region Parameters
        /// <summary>
        /// The WMI Object to use
        /// </summary>
        /// 
        [Parameter(ValueFromPipeline = true, Mandatory = true, ParameterSetName = "object")]
        public ManagementObject InputObject
        {
            get { return this.inputObject; }
            set { this.inputObject = value; }
        }
        /// <summary>
        /// The WMI Path to use
        /// </summary>
        [Parameter(Mandatory = true,ParameterSetName = "path")]
        public string Path
        {
            get { return this.path; }
            set { this.path = value; }
        }
        /// <summary>
        /// The WMI class to use
        /// </summary>
        [Parameter(Position = 0, Mandatory = true,ParameterSetName = "class")]
        public string Class
        {
            get { return this.className; }
            set { this.className = value; }
        }

        #endregion Parameters

        #region parameter data
        private string path = null;
        private string className = null;
        private ManagementObject inputObject = null;

        #endregion parameter data
        #region Command code
        /// <summary>
        /// Remove an object given either path,class name or pipeline input.
        /// </summary>
        protected override void ProcessRecord()
        {
            if( this.AsJob )
            {
                RunAsJob("Remove-WMIObject");
                return;
            }
            if (inputObject != null)
            {
                try
                {
                    if (!ShouldProcess(inputObject["__PATH"].ToString()) )
                    {
                        return;
                    }
                    inputObject.Delete();
                }
                catch (ManagementException e)
                {
                    ErrorRecord errorRecord = new ErrorRecord(e, "RemoveWMIManagementException", ErrorCategory.InvalidOperation, null);
                    WriteError(errorRecord);
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    ErrorRecord errorRecord = new ErrorRecord(e, "RemoveWMICOMException", ErrorCategory.InvalidOperation, null);
                    WriteError(errorRecord);
                }
                return;
            }
            else
            {
                ConnectionOptions options = GetConnectionOption();
                ManagementPath mPath = null;
                ManagementObject mObject = null;
                if (path != null)
                {
                    mPath = new ManagementPath(path);
                    if(String.IsNullOrEmpty(mPath.NamespacePath))
                    {
                        mPath.NamespacePath = this.Namespace;
                    }
                    else if( namespaceSpecified )
                    {
                        //ThrowTerminatingError
                        ThrowTerminatingError(new ErrorRecord(
                            new InvalidOperationException(),
                            "NamespaceSpecifiedWithPath",
                            ErrorCategory.InvalidOperation,
                            this.Namespace));
                    }

                    if( mPath.Server != "." && serverNameSpecified )
                    {
                        //ThrowTerminatingError
                        ThrowTerminatingError(new ErrorRecord(
                            new InvalidOperationException(),
                            "ComputerNameSpecifiedWithPath",
                            ErrorCategory.InvalidOperation,
                            this.ComputerName));
                    }
                    if (!(mPath.Server == "." && serverNameSpecified))
                    {
                        string[] serverName = new string[] { mPath.Server };
                        ComputerName = serverName;
                    }
                }
                foreach (string name in ComputerName)
                {
                    try
                    {
                        if( path != null )
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
                            ManagementClass mClass = new ManagementClass(className);
                            mObject = mClass;
                            mObject.Scope = scope;
                        }
                        if (!ShouldProcess(mObject["__PATH"].ToString()) )
                        {
                            continue;
                        }
                        mObject.Delete();
                    }
                    catch (ManagementException e)
                    {
                        ErrorRecord errorRecord = new ErrorRecord(e, "RemoveWMIManagementException", ErrorCategory.InvalidOperation, null);
                        WriteError(errorRecord);
                    }
                    catch (System.Runtime.InteropServices.COMException e)
                    {
                        ErrorRecord errorRecord = new ErrorRecord(e, "RemoveWMICOMException", ErrorCategory.InvalidOperation, null);
                        WriteError(errorRecord);
                    }
                }
            }
        }
        #endregion Command code
    }
}
