/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
using System;
using System.Management.Automation;
using System.Xml.Linq;
using System.Collections;
using System.Collections.Generic;

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
            OmiInterface oi = new OmiInterface();

            if (propertySpecified)
            {
                string value;
                oi.GetOmiValue(nameSpace, className, property, out value);
                WriteObject(value);
                return;
            }

            OmiData data;
            oi.GetOmiValues(nameSpace, className, out data);

            // Convert OmiData type to array of objects
            ArrayList array = new ArrayList();
            foreach (Dictionary<string, string> d in data.Values)
            {
                PSObject o = new PSObject();

                foreach (string p in data.Properties)
                {
                    string value = String.Empty;
                    if (d.ContainsKey(p))
                    {
                        value = d[p];
                    }
                    PSNoteProperty psp = new PSNoteProperty(p, value);
                    o.Members.Add(psp);
                }
                array.Add(o);
            }

            WriteObject((Object[])array.ToArray());

        } // EndProcessing
        
        #endregion
    }

    #endregion
    
} // namespace Microsoft.PowerShell.Commands


