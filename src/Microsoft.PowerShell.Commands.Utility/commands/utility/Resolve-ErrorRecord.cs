// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Class for Resolve-ErrorRecord implementation.
    /// </summary>
    [Experimental("Microsoft.PowerShell.Utility.PSResolveErrorRecord", ExperimentAction.Show)]
    [Cmdlet(VerbsDiagnostic.Resolve, "ErrorRecord", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=", DefaultParameterSetName = NewestParameterSetName)]
    public sealed class ResolveErrorRecordCommand : PSCmdlet
    {
        internal const string ErrorRecordParameterSetName = "ErrorRecord";
        internal const string NewestParameterSetName = "Newest";

        /// <summary>
        /// The error object to resolve.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true, ParameterSetName = ErrorRecordParameterSetName)]
        public PSObject InputObject { set; get; }

        /// <summary>
        /// The number of ErrorRecords to resolve starting with newest first.
        /// </summary>
        [Parameter(ParameterSetName = NewestParameterSetName)]
        [ValidateRange(0, int.MaxValue)]
        public int Newest { set; get; } = 1;

        /// <summary>
        /// Process the ErrorRecord.
        /// </summary>
        protected override void ProcessRecord()
        {
            var errorRecords = new List<object>();
            var index = 0;

            if (InputObject != null)
            {
                if (InputObject.BaseObject is Exception || InputObject.BaseObject is ErrorRecord)
                errorRecords.Add(InputObject);
            }
            else
            {
                var errorVariable = SessionState.PSVariable.Get("error");
                var count = Newest;
                ArrayList errors = (ArrayList)errorVariable.Value;
                if (count > errors.Count)
                {
                    count = errors.Count;
                }

                while (count > 0)
                {
                    errorRecords.Add(errors[index]);
                    index++;
                    count--;
                }
            }

            index = 0;
            bool addErrorIdentifier = errorRecords.Count > 1 ? true : false;

            foreach (object errorRecord in errorRecords)
            {
                PSObject obj = PSObject.AsPSObject(errorRecord);
                obj.TypeNames.Insert(0, "System.Management.Automation.ErrorRecord#ResolvedErrorRecord");
                // Remove some types so they don't get rendered by those formats
                obj.TypeNames.Remove("System.Management.Automation.ErrorRecord");
                obj.TypeNames.Remove("System.Exception");

                if (addErrorIdentifier)
                {
                    obj.Properties.Add(new PSNoteProperty("PSErrorIdentifier", index++));
                }

                WriteObject(obj);
            }
        }
    }
}
