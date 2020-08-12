// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.DesiredStateConfiguration
{
    internal class JsonDeserializer
    {
        #region Constructors

        /// <summary>
        /// Instantiates a default deserializer
        /// </summary>
        public static JsonDeserializer Create()
        {
            return new JsonDeserializer();
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Returns schema of Cim classes from specified json file
        /// </summary>
        public IEnumerable<PSObject> DeserializeClasses(string json, bool useNewRunspace = false)
        {
            IEnumerable<PSObject> result = null;
            RunspaceMode runspaceMode = useNewRunspace ? RunspaceMode.NewRunspace : RunspaceMode.CurrentRunspace;
            using (var powerShell = System.Management.Automation.PowerShell.Create(runspaceMode))
            {
                powerShell.AddCommand("ConvertFrom-Json");
                powerShell.AddParameter("InputObject", json);
                powerShell.AddParameter("Depth", 100); // maximum supported by cmdlet

                result = powerShell.Invoke();
            }

            return result;
        }

        #endregion Methods
    }
}
