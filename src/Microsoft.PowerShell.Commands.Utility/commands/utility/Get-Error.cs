// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Class for Get-Error implementation.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Error",
        HelpUri = "https://go.microsoft.com/fwlink/?linkid=2241804",
        DefaultParameterSetName = NewestParameterSetName)]
    [OutputType("System.Management.Automation.ErrorRecord#PSExtendedError", "System.Exception#PSExtendedError")]
    public sealed class GetErrorCommand : PSCmdlet
    {
        internal const string ErrorParameterSetName = "Error";
        internal const string NewestParameterSetName = "Newest";
        internal const string AliasNewest = "Last";
        internal const string ErrorRecordPSExtendedError = "System.Management.Automation.ErrorRecord#PSExtendedError";
        internal const string ExceptionPSExtendedError = "System.Exception#PSExtendedError";

        /// <summary>
        /// Gets or sets the error object to resolve.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true, ParameterSetName = ErrorParameterSetName)]
        [ValidateNotNullOrEmpty]
        public PSObject InputObject { get; set; }

        /// <summary>
        /// Gets or sets the number of error objects to resolve starting with newest first.
        /// </summary>
        [Parameter(ParameterSetName = NewestParameterSetName)]
        [Alias(AliasNewest)]
        [ValidateRange(1, int.MaxValue)]
        public int Newest { get; set; } = 1;

        /// <summary>
        /// Process the error object.
        /// </summary>
        protected override void ProcessRecord()
        {
            var errorRecords = new List<object>();
            var index = 0;

            if (InputObject != null)
            {
                if (InputObject.BaseObject is Exception || InputObject.BaseObject is ErrorRecord)
                {
                    errorRecords.Add(InputObject);
                }
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
            bool addErrorIdentifier = errorRecords.Count > 1;

            foreach (object errorRecord in errorRecords)
            {
                var obj = PSObject.AsPSObject(errorRecord, storeTypeNameAndInstanceMembersLocally: true);

                if (obj.TypeNames.Contains("System.Management.Automation.ErrorRecord"))
                {
                    if (!obj.TypeNames.Contains(ErrorRecordPSExtendedError))
                    {
                        obj.TypeNames.Insert(0, ErrorRecordPSExtendedError);

                        // Need to remove so this rendering doesn't take precedence as ErrorRecords is "OutOfBand"
                        obj.TypeNames.Remove("System.Management.Automation.ErrorRecord");
                    }
                }

                if (obj.TypeNames.Contains("System.Exception"))
                {
                    if (!obj.TypeNames.Contains(ExceptionPSExtendedError))
                    {
                        obj.TypeNames.Insert(0, ExceptionPSExtendedError);

                        // Need to remove so this rendering doesn't take precedence as Exception is "OutOfBand"
                        obj.TypeNames.Remove("System.Exception");
                    }
                }

                if (addErrorIdentifier)
                {
                    obj.Properties.Add(new PSNoteProperty("PSErrorIndex", index++));
                }

                WriteObject(obj);
            }
        }
    }
}
