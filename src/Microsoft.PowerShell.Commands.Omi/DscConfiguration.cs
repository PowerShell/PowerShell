/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
using System;
using System.IO;
using System.Text;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands.Omi
{
    #region Start-DscConfiguration

    /// <summary> 
    /// implementation for the Start-DscConfiguration command 
    /// </summary> 
    [Cmdlet( VerbsLifecycle.Start, "DscConfiguration" )]
    [OutputType(typeof(object))]
    public sealed class StartDscConfigurationCommand : Cmdlet
    {
        #region parameters

        [Parameter(Mandatory = true)]
        [Alias("CM")]
        public string ConfigurationMof
        {
            get
            {
                return mofPath;
            }
            set
            {
                mofPath = value;
            }
        }
        private string mofPath;

        #endregion

        #region methods

        protected override void ProcessRecord()
        {
            if (mofPath == null)
            {
                throw new ArgumentNullException();
            }

            if (!Platform.IsLinux())
            {
                throw new PlatformNotSupportedException();
            }

            OmiInterface oi = new OmiInterface();

            const string nameSpace = "root/Microsoft/DesiredStateConfiguration";
            const string instanceName = "{ MSFT_DSCLocalConfigurationManager }";
            const string methodName = "SendConfigurationApply";

            string mof = File.ReadAllText(mofPath);
            byte[] asciiBytes = Encoding.ASCII.GetBytes(mof);

            StringBuilder sb = new StringBuilder();
            sb.Append(" { ConfigurationData [ ");
            foreach (byte b in asciiBytes)
            {
                sb.Append(b.ToString());
                sb.Append(' ');
            }
            sb.Append("] ");
            sb.Append("}");
            string parameters = sb.ToString();

            string arguments = $"iv {nameSpace} {instanceName} {methodName} {parameters} -xml";
            oi.ExecuteOmiCliCommand(arguments);

            OmiData data = oi.GetOmiData();
            object[] array = data.ToObjectArray();

            WriteObject(array, true);

        } // EndProcessing
        
        #endregion
    }

    #endregion

    #region Get-DscConfiguration

    /// <summary> 
    /// implementation for the Get-DscConfiguration command 
    /// </summary> 
    [Cmdlet( VerbsCommon.Get, "DscConfiguration" )]
    [OutputType(typeof(object))]
    public sealed class GetDscConfigurationCommand : Cmdlet
    {
        #region methods

        protected override void ProcessRecord()
        {
            if (!Platform.IsLinux())
            {
                throw new PlatformNotSupportedException();
            }

            OmiInterface oi = new OmiInterface();

            const string nameSpace = "root/Microsoft/DesiredStateConfiguration";
            const string instanceName = "{ MSFT_DSCLocalConfigurationManager }";
            const string methodName = "GetConfiguration";

            string arguments = $"iv {nameSpace} {instanceName} {methodName} -xml";
            oi.ExecuteOmiCliCommand(arguments);

            OmiData data = oi.GetOmiData();
            object[] array = data.ToObjectArray();

            WriteObject(array, true);

        } // EndProcessing
        
        #endregion
    }

    #endregion
    
} // namespace Microsoft.PowerShell.Commands


