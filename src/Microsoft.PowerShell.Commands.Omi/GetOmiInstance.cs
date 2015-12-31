/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
using System;
using System.Xml.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands.Omi
{
    #region Get-OmiInstance

    /// <summary> 
    /// implementation for the Get-OmiInstance command 
    /// </summary> 
    [Cmdlet( VerbsCommon.Get, "OmiInstance" )]
    [OutputType(typeof(string))]
    [OutputType(typeof(object[]))]
    public sealed class GetOmiInstanceCommand : Cmdlet
    {
        #region parameters

        [Parameter(Mandatory = true)]
        [Alias("NS")]
        public string Namespace
        {
            get
            {
                return nameSpace;
            }
            set
            {
                nameSpace = value;
            }
        }
        private string nameSpace;

        [Parameter(Mandatory = true)]
        [Alias("CN")]        
        public string ClassName
        {
            get
            {
                return className;
            }
            set
            {
                className = value;
            }
        }
        private string className;

        [Parameter]
        public string Property
        {
            get
            {
                return property;
            }
            set
            {
                property = value;
                propertySpecified = true;
            }
        }
        private string property;
        private bool propertySpecified = false;

        #endregion

        #region methods

        protected override void ProcessRecord()
        {
            if (nameSpace == null || className == null)
            {
                throw new ArgumentNullException();
            }

            if (!Platform.IsLinux())
            {
                throw new PlatformNotSupportedException();
            }

            OmiInterface oi = new OmiInterface();

            string arguments = $"ei {nameSpace} {className} -xml";
            oi.ExecuteOmiCliCommand(arguments);

            if (propertySpecified)
            {
                if (property == null)
                {
                    throw new ArgumentNullException();
                }
                
                string value;
                string type;
                oi.GetValue(className, property, out type, out value);

                WriteObject(value);
                return;
            }

            OmiData data = oi.GetOmiData();
            object[] array = data.ToObjectArray();

            WriteObject(array);

        } // EndProcessing
        
        #endregion
    }

    #endregion
    
} // namespace Microsoft.PowerShell.Commands


