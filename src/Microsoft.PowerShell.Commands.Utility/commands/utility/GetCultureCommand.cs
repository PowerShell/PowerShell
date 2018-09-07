// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Returns:
    ///     - the thread's current culture
    ///     - culture by name
    ///     - list of all supported cultures.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Culture", DefaultParameterSetName = DefaultParameterSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113312")]
    [OutputType(typeof(System.Globalization.CultureInfo))]
    public sealed class GetCultureCommand : PSCmdlet
    {
        private const string DefaultParameterSet = "Default";
        private const string NameParameterSet = "Name";
        private const string ListAvailableParameterSet = "ListAvailable";

        /// <summary>
        /// Gets or sets culture names for which CultureInfo values are returned.
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet, Position = 0, ValueFromPipeline = true)]
        [ValidateNotNull]
        public string[] Name { get; set; }

        /// <summary>
        /// Gets or sets a switch to list all available cultures.
        /// </summary>
        [Parameter(ParameterSetName = ListAvailableParameterSet)]
        public SwitchParameter ListAvailable { get; set; }

        /// <summary>
        /// Output:
        ///     - the thread's current culture
        ///     - culture by name
        ///     - list of all supported cultures.
        /// </summary>
        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case DefaultParameterSet:
                    WriteObject(Host.CurrentCulture);

                    break;
                case NameParameterSet:
                    try
                    {
                        foreach (var cultureName in Name)
                        {
                            CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);
                            WriteObject(ci);
                        }
                    }
                    catch (CultureNotFoundException exc)
                    {
                        WriteError(new ErrorRecord(exc, "ItemNotFoundException", ErrorCategory.ObjectNotFound, Name));
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
