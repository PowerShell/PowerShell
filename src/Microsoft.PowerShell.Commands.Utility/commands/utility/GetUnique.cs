// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Unique", DefaultParameterSetName = "AsString",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113335", RemotingCapability = RemotingCapability.None)]
    public sealed class GetUniqueCommand : PSCmdlet
    {
        #region Parameters
        /// <summary>
        /// </summary>
        /// <value></value>
        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject { set; get; } = AutomationNull.Value;

        /// <summary>
        /// This parameter specifies that objects should be converted to
        /// strings and the strings should be compared.
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = "AsString")]
        public SwitchParameter AsString
        {
            get { return _asString; }

            set { _asString = value; }
        }

        private bool _asString;

        /// <summary>
        /// This parameter specifies that just the types of the objects
        /// should be compared.
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = "UniqueByType")]
        public SwitchParameter OnType
        {
            get { return _onType; }

            set { _onType = value; }
        }

        private bool _onType = false;
        #endregion Parameters

        #region Overrides
        /// <summary>
        /// </summary>
        protected override void ProcessRecord()
        {
            bool isUnique = true;
            if (_lastObject == null)
            {
                // always write first object, but return nothing
                // on "MSH> get-unique"
                if (AutomationNull.Value == InputObject)
                    return;
            }
            else if (OnType)
            {
                isUnique = (InputObject.InternalTypeNames[0] != _lastObject.InternalTypeNames[0]);
            }
            else if (AsString)
            {
                string inputString = InputObject.ToString();
                if (_lastObjectAsString == null)
                {
                    _lastObjectAsString = _lastObject.ToString();
                }

                if (0 == string.Compare(
                    inputString,
                    _lastObjectAsString,
                    StringComparison.CurrentCulture))
                {
                    isUnique = false;
                }
                else
                {
                    _lastObjectAsString = inputString;
                }
            }
            else // compare as objects
            {
                if (_comparer == null)
                {
                    _comparer = new ObjectCommandComparer(
                        true, // ascending (doesn't matter)
                        CultureInfo.CurrentCulture,
                        true); // case-sensitive
                }

                isUnique = (0 != _comparer.Compare(InputObject, _lastObject));
            }

            if (isUnique)
            {
                WriteObject(InputObject);
                _lastObject = InputObject;
            }
        }
        #endregion Overrides

        #region Internal
        private PSObject _lastObject = null;
        private string _lastObjectAsString = null;
        private ObjectCommandComparer _comparer = null;
        #endregion Internal
    }
}
