// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Class for Resolve-ErrorRecord implementation.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Resolve, "ErrorRecord", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=", DefaultParameterSetName = NewestParameterSetName)]
    public sealed class ResolveErrorRecordCommand : PSCmdlet
    {
        internal const string ErrorRecordParameterSetName = "ErrorRecord";
        internal const string NewestParameterSetName = "Newest";

        /// <summary>
        /// The ErrorRecord object to resolve.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true, ParameterSetName = ErrorRecordParameterSetName)]
        public ErrorRecord ErrorRecord { set; get; }

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
            var errorRecords = new List<ErrorRecord>();
            var index = 0;

            if (ErrorRecord != null)
            {
                errorRecords.Add(ErrorRecord);
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
                    errorRecords.Add((ErrorRecord)errors[index]);
                    index++;
                    count--;
                }
            }

            index = 0;
            bool addErrorIdentifier = errorRecords.Count > 1 ? true : false;

            foreach (ErrorRecord errorRecord in errorRecords)
            {
                var psObj = new PSObject(errorRecord);
                psObj.TypeNames.Insert(0, "System.Management.Automation.ErrorRecord#ResolvedErrorRecord");
                psObj.TypeNames.Remove("System.Management.Automation.ErrorRecord");
                if (addErrorIdentifier)
                {
                    psObj.Properties.Add(new PSNoteProperty("PSErrorIdentifier", index++));
                }

                WriteObject(errorRecord);
            }
        }
    }
}
