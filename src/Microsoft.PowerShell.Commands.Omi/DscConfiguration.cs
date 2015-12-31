/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands.Omi
{
    #region Start-DscConfiguration

    /// <summary> 
    /// implementation for the Start-DscConfiguration command 
    /// </summary> 
    [Cmdlet( VerbsLifecycle.Start, "DscConfiguration" )]
    [OutputType(typeof(string))]
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
            OmiInterface oi = new OmiInterface();

            OmiData data;
            oi.StartDscConfiguration(mofPath, out data);

            object[] array = data.ToObjectArray();
            WriteObject(array);

        } // EndProcessing
        
        #endregion
    }

    #endregion
    
} // namespace Microsoft.PowerShell.Commands


