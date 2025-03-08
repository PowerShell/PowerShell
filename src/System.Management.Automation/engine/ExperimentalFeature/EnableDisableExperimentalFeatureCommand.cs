// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Configuration;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Base class for Enable/Disable-ExperimentalFeature cmdlet.
    /// </summary>
    public class EnableDisableExperimentalFeatureCommandBase : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the feature names.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true, Position = 0, Mandatory = true)]
        [ArgumentCompleter(typeof(ExperimentalFeatureNameCompleter))]
        public string[] Name { get; set; }

        /// <summary>
        /// Gets or sets the scope of persistence of updating the PowerShell configuration json.
        /// </summary>
        [Parameter]
        public ConfigScope Scope { get; set; } = ConfigScope.CurrentUser;

        /// <summary>
        /// EndProcessing method.
        /// </summary>
        protected override void EndProcessing()
        {
            WriteWarning(ExperimentalFeatureStrings.ExperimentalFeaturePending);
        }
    }

    /// <summary>
    /// Implements Enable-ExperimentalFeature cmdlet.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Enable, "ExperimentalFeature", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2046964")]
    public class EnableExperimentalFeatureCommand : EnableDisableExperimentalFeatureCommandBase
    {
        /// <summary>
        /// ProcessRecord method of this cmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            ExperimentalFeatureConfigHelper.UpdateConfig(this, Name, Scope, enable: true);
        }
    }

    /// <summary>
    /// Implements Enable-ExperimentalFeature cmdlet.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Disable, "ExperimentalFeature", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2046963")]
    public class DisableExperimentalFeatureCommand : EnableDisableExperimentalFeatureCommandBase
    {
        /// <summary>
        /// ProcessRecord method of this cmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            ExperimentalFeatureConfigHelper.UpdateConfig(this, Name, Scope, enable: false);
        }
    }

    internal static class ExperimentalFeatureConfigHelper
    {
        internal static void UpdateConfig(PSCmdlet cmdlet, string[] name, ConfigScope scope, bool enable)
        {
            IEnumerable<WildcardPattern> namePatterns = SessionStateUtilities.CreateWildcardsFromStrings(name, WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant);
            GetExperimentalFeatureCommand getExperimentalFeatureCommand = new GetExperimentalFeatureCommand();
            getExperimentalFeatureCommand.Context = cmdlet.Context;
            bool foundFeature = false;
            foreach (ExperimentalFeature feature in getExperimentalFeatureCommand.GetAvailableExperimentalFeatures(namePatterns))
            {
                foundFeature = true;
                if (!cmdlet.ShouldProcess(feature.Name))
                {
                    return;
                }

                PowerShellConfig.Instance.SetExperimentalFeatures(scope, feature.Name, enable);
            }

            if (!foundFeature)
            {
                string errMsg = string.Format(CultureInfo.InvariantCulture, ExperimentalFeatureStrings.ExperimentalFeatureNameNotFound, name);
                cmdlet.WriteError(new ErrorRecord(new ItemNotFoundException(errMsg), "ItemNotFoundException", ErrorCategory.ObjectNotFound, name));
                return;
            }
        }
    }

    /// <summary>
    /// Provides argument completion for ExperimentalFeature names.
    /// </summary>
    public sealed class ExperimentalFeatureNameCompleter : IArgumentCompleter
    {
        /// <summary>
        /// Gets all possible ExperimentalFeature name completion values.
        /// </summary>
        public IEnumerable<string> PossibleCompletionValues => GetExperimentalFeatureNames();

        /// <summary>
        /// Gets experimental feature names using Get-Command.
        /// </summary>
        /// <returns>Sorted set of experimental feature names.</returns>
        private static SortedSet<string> GetExperimentalFeatureNames()
        {
            using var ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace);
            ps.AddCommand("Get-ExperimentalFeature");

            Collection<ExperimentalFeature> expirmentalFeatures = ps.Invoke<ExperimentalFeature>();

            SortedSet<string> featureNames = new(StringComparer.OrdinalIgnoreCase);

            foreach (ExperimentalFeature feature in expirmentalFeatures)
            {
                featureNames.Add(feature.Name);
            }

            return featureNames;
        }
    }
}
