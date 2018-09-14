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
    [Cmdlet(VerbsCommon.Get, "Culture", DefaultParameterSetName = CurrentCultureParameterSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113312")]
    [OutputType(typeof(System.Globalization.CultureInfo))]
    public sealed class GetCultureCommand : PSCmdlet
    {
        private const string CurrentCultureParameterSet = "CurrentCulture";
        private const string NameParameterSet = "Name";
        private const string ListParameterSet = "List";

        /// <summary>
        /// Gets or sets culture names for which CultureInfo values are returned.
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet, Position = 0, ValueFromPipeline = true)]
        [ValidateNotNull]
        public string[] Name { get; set; }

        /// <summary>
        /// Gets or sets a switch to return current culture with user overrides (by default).
        /// With the switch on, we return current culture without user overrides.
        /// </summary>
        [Parameter(ParameterSetName = CurrentCultureParameterSet)]
        [Parameter(ParameterSetName = NameParameterSet)]
        public SwitchParameter NoUserOverrides { get; set; }

        /// <summary>
        /// Gets or sets a filter to list subset or all available cultures.
        /// </summary>
        [Parameter(ParameterSetName = ListParameterSet)]
        public CultureTypes List { get; set; } = CultureTypes.AllCultures;

        /// <summary>
        /// Output:
        ///     - the thread's current culture
        ///     - culture by name
        ///     - list of all supported cultures.
        /// </summary>
        protected override void ProcessRecord()
        {
            CultureInfo ci;

            switch (ParameterSetName)
            {
                case CurrentCultureParameterSet:
                    if (NoUserOverrides)
                    {
                        ci = CultureInfo.GetCultureInfo(Host.CurrentCulture.LCID);
                    }
                    else
                    {
                        ci = Host.CurrentCulture;
                    }

                    WriteObject(Host.CurrentCulture);

                    break;
                case NameParameterSet:
                    try
                    {
                        foreach (var cultureName in Name)
                        {
                            if (NoUserOverrides && string.Equals(cultureName, Host.CurrentCulture.Name))
                            {
                                ci = new CultureInfo(cultureName, false);
                            }
                            else
                            {
                                ci = CultureInfo.GetCultureInfo(cultureName);
                            }

                            WriteObject(ci);
                        }
                    }
                    catch (CultureNotFoundException exc)
                    {
                        WriteError(new ErrorRecord(exc, "ItemNotFoundException", ErrorCategory.ObjectNotFound, Name));
                    }

                    break;
                case ListParameterSet:
                    foreach (CultureInfo culture in CultureInfo.GetCultures(List))
                    {
                        WriteObject(culture);
                    }

                    break;
            }
        }
    }
}
