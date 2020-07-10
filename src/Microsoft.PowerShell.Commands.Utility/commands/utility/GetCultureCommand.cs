// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
    [Cmdlet(VerbsCommon.Get, "Culture", DefaultParameterSetName = CurrentCultureParameterSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097017")]
    [OutputType(typeof(System.Globalization.CultureInfo))]
    public sealed class GetCultureCommand : PSCmdlet
    {
        private const string CurrentCultureParameterSet = "CurrentCulture";
        private const string NameParameterSet = "Name";
        private const string ListAvailableParameterSet = "ListAvailable";

        /// <summary>
        /// Gets or sets culture names for which CultureInfo values are returned.
        /// Empty string matches Invariant culture.
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet, Position = 0, ValueFromPipeline = true)]
        [ValidateSet(typeof(ValidateCultureNamesGenerator))]
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
            CultureInfo ci;

            switch (ParameterSetName)
            {
                case CurrentCultureParameterSet:
                    if (NoUserOverrides)
                    {
                        ci = CultureInfo.GetCultureInfo(Host.CurrentCulture.Name);
                    }
                    else
                    {
                        ci = Host.CurrentCulture;
                    }

                    WriteObject(ci);

                    break;
                case NameParameterSet:
                    try
                    {
                        foreach (var cultureName in Name)
                        {
                            if (!NoUserOverrides && string.Equals(cultureName, Host.CurrentCulture.Name, StringComparison.CurrentCultureIgnoreCase))
                            {
                                ci = Host.CurrentCulture;
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
                case ListAvailableParameterSet:
                    foreach (var cultureInfo in CultureInfo.GetCultures(CultureTypes.AllCultures))
                    {
                        WriteObject(cultureInfo);
                    }

                    break;
            }
        }
    }

    /// <summary>
    /// Get list of valid culture names for ValidateSet attribute.
    /// </summary>
    public class ValidateCultureNamesGenerator : IValidateSetValuesGenerator
    {
        string[] IValidateSetValuesGenerator.GetValidValues()
        {
            var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            var result = new List<string>(cultures.Length);
            foreach (var cultureInfo in cultures)
            {
                result.Add(cultureInfo.Name);
            }

            return result.ToArray();
        }
    }
}
