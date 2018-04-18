// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation;
using System.Collections.Generic;

namespace Microsoft.PowerShell.Commands
{
    #region WriteOutputCommand
    /// <summary>
    /// This class implements Write-output command
    ///
    /// </summary>
    [Cmdlet(VerbsCommunications.Write, "Output", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113427", RemotingCapability = RemotingCapability.None)]
    public sealed class WriteOutputCommand : PSCmdlet
    {
        private object _inputObjects = null;

        /// <summary>
        /// Holds the list of objects to be Written
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromRemainingArguments = true)]
        [AllowNull]
        [AllowEmptyCollection]
        public object InputObject
        {
            get { return _inputObjects; }
            set { _inputObjects = value; }
        }

        /// <summary>
        /// Prevents Write-Output from unravelling collections passed to the InputObject
        /// parameter.
        /// </summary>
        [Parameter()]
        public SwitchParameter NoEnumerate
        {
            get;
            set;
        }

        /// <summary>
        /// This method implements the BeginProcessing method for Write-output command
        /// </summary>
        protected override void BeginProcessing() {
            // If the input is a List<object> instance with a single element,
            // assume that it is a single argument bound via ValueFromRemainingArguments and unwrap it.
            // Note: 
            //  * This case is indistinguishable from something like the following:
            //      Write-Output -NoEnumerate -InputObject ([System.Collections.Generic.List[object]]::new((, 1)))
            //    However, this seems like an acceptable price to pay in order to prevent unexpected wrapping of
            //    a scalar in a collection when using -NoEnumerate.
            //  * Is the case of *multiple* ValueFromRemainingArguments values, the List<object> instance
            //    is passed through when -NoEnumerate is specified.
            List<object> lst;
            if (_inputObjects is List<object> && (lst = (List<object>)_inputObjects).Count == 1) {
                _inputObjects = lst[0];
            }            
        }

        /// <summary>
        /// This method implements the ProcessRecord method for Write-output command
        /// </summary>
        protected override void ProcessRecord()
        {
            if (null == _inputObjects)
            {
                WriteObject(_inputObjects);
                return;
            }

            bool enumerate = true;
            if (NoEnumerate.IsPresent)
            {
                enumerate = false;
            }

            WriteObject(_inputObjects, enumerate);
        }//processrecord
    }//WriteOutputCommand
    #endregion
}
