// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Returns the thread's current culture.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Culture", DefaultParameterSetName = NameParameterSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113312")]
    [OutputType(typeof(System.Globalization.CultureInfo))]
    public sealed class GetCultureCommand : PSCmdlet
    {
        private const string NameParameterSet = "Name";
        private const string ListAvailableParameterSet = "ListAvailable";

        /// <summary>
        /// Gets and sets the specified culture.
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet)]
        [ValidateNotNull]
        public string Name { get; set; }

        /// <summary>
        /// Gets and sets a switch to list all available cultures.
        /// </summary>
        [Parameter(ParameterSetName = ListAvailableParameterSet)]
        public SwitchParameter ListAvailable { get; set; }

        /// <summary>
        /// Output the current Culture info object.
        /// </summary>
        protected override void EndProcessing()
        {
            switch (ParameterSetName)
            {
                case NameParameterSet:
                    if (Name == null)
                    {
                        WriteObject(Host.CurrentCulture);
                    }
                    else
                    {
                        CultureInfo ci = CultureInfo.GetCultureInfo(Name);
                        WriteObject(ci);
                    }

                    break;
                case ListAvailableParameterSet:
                    foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.AllCultures))
                    {
                        WriteObject(ci);
                    }

                    break;
            }
        }
    }
}
