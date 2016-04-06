//
//    Copyright (C) Microsoft.  All rights reserved.
//
using System;
using System.Management.Automation;
using System.Management;
using System.Text;
using System.Management.Automation.Provider;
using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;
using System.Security.AccessControl;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command to Set WMI Instance
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "WmiInstance", DefaultParameterSetName = "class", SupportsShouldProcess = true,
        HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113402", RemotingCapability = RemotingCapability.OwnedByCommand)]
    public sealed class SetWmiInstance : WmiBaseCmdlet
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
        [Parameter(ParameterSetName = "path", Mandatory = true)]
        public string Path
        {
            get { return this.path; }
            set { this.path = value; }
        }
        /// <summary>
        /// The WMI class to use
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "class")]
        public string Class
        {
            get { return this.className; }
            set { this.className = value; }
        }
        /// <summary>
        /// The property name /value pair
        /// </summary>
        [Parameter(ParameterSetName = "path")]
        [Parameter(Position = 2, ParameterSetName = "class")]
        [Parameter(ParameterSetName = "object")]
        [Alias("Args", "Property")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Hashtable Arguments
        {
            get { return this.propertyBag; }
            set { this.propertyBag = value; }
        }

        /// <summary>
        /// The Flag to use
        /// </summary>
        [Parameter]
        public  PutType PutType
        {
            get { return this.putType; }
            set { this.putType = value; flagSpecified = true;}
        }

        #endregion Parameters
        #region parameter data
        internal bool flagSpecified = false;
        private string path = null;
        private string className = null;
        private ManagementObject inputObject = null;
        private Hashtable propertyBag = null;
        private PutType putType = PutType.None;

        #endregion parameter data

        #region Command code
        /// <summary>
        /// Create or modify WMI Instance given either path,class name or pipeline input.
        /// </summary>
        protected override void ProcessRecord()
        {
            if( this.AsJob )
            {
                RunAsJob("Set-WMIInstance");
                return;
            }
            if (inputObject != null)
            {
                object result = null;
                ManagementObject mObj = null;
                try
                {
                    PutOptions pOptions = new PutOptions();
                    mObj = SetWmiInstanceGetPipelineObject();
                    pOptions.Type = putType;
                    if( mObj != null)
                    {
                        if (!ShouldProcess(mObj.Path.Path.ToString()) )
                        {
                            return;
                        }
                        mObj.Put(pOptions);
                    }
                    else
                    {
                        InvalidOperationException exp = new InvalidOperationException();
                        throw exp;
                    }
                    result = mObj;
                }
                catch (ManagementException e)
                {
                    ErrorRecord errorRecord = new ErrorRecord(e, "SetWMIManagementException", ErrorCategory.InvalidOperation, null);
                    WriteError(errorRecord);
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    ErrorRecord errorRecord = new ErrorRecord(e, "SetWMICOMException", ErrorCategory.InvalidOperation, null);
                    WriteError(errorRecord);
                }
                WriteObject(result);
            }
            else
            {
                ManagementPath mPath = null;
                //If Class is specified only CreateOnly flag is supported
                mPath = this.SetWmiInstanceBuildManagementPath();
                //If server name is specified loop through it.
                if( mPath != null)
                {
                    if (!(mPath.Server == "." && serverNameSpecified))
                    {
                        string[] serverName = new string[] { mPath.Server };
                        ComputerName = serverName;
                    }
                }
                ConnectionOptions options = GetConnectionOption();
                object result = null;
                ManagementObject mObject = null;
                foreach (string name in ComputerName)
                {
                    result = null;
                    try
                    {
                        mObject = this.SetWmiInstanceGetObject(mPath,name);
                        PutOptions pOptions = new PutOptions();
                        pOptions.Type = putType;
                        if( mObject != null)
                        {
                            if (!ShouldProcess(mObject.Path.Path.ToString()) )
                            {
                                continue;
                            }
                            mObject.Put(pOptions);
                        }
                        else
                        {
                            InvalidOperationException exp = new InvalidOperationException();
                            throw exp;
                        }
                        result = mObject;
                    }
                    catch (ManagementException e)
                    {
                        ErrorRecord errorRecord = new ErrorRecord(e, "SetWMIManagementException", ErrorCategory.InvalidOperation, null);
                        WriteError(errorRecord);
                    }
                    catch (System.Runtime.InteropServices.COMException e)
                    {
                        ErrorRecord errorRecord = new ErrorRecord(e, "SetWMICOMException", ErrorCategory.InvalidOperation, null);
                        WriteError(errorRecord);
                    }
                    if (result != null)
                    {
                        WriteObject(result);
                    }
                }
            }
        }
        #endregion Command code
    }
}
