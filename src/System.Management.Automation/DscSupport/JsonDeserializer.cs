// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace Microsoft.PowerShell.DesiredStateConfiguration.Json
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
            System.Management.Automation.PowerShell powerShell = null;

            if (useNewRunspace)
            {
                // currently using RunspaceMode.NewRunspace will reset PSModulePath env var for the entire process
                // this is something we want to avoid in DSC GuestConfigAgent scenario, so we use following workaround
                var s_iss = InitialSessionState.CreateDefault();
                s_iss.EnvironmentVariables.Add(
                    new SessionStateVariableEntry(
                        "PSModulePath",
                        Environment.GetEnvironmentVariable("PSModulePath"),
                        description: null));
                powerShell = System.Management.Automation.PowerShell.Create(s_iss);
            }
            else
            {
                powerShell = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace);
            }

            using (powerShell)
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
