// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Configuration;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implements Enable-ExperimentalFeature cmdlet.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Enable, "ExperimentalFeature", SupportsShouldProcess = true, HelpUri = "")]
    public class EnableExperimentalFeatureCommand : PSCmdlet
    {
        /// <summary>
        /// Get and set the feature names.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true, Position = 0, Mandatory = true)]
        [ArgumentCompleter(typeof(NameArgumentCompleter))]
        [ValidateNotNullOrEmpty]
        public string[] Name { get; set; }

        /// <summary>
        /// Get and set the scope of persistence of updating the PowerShell configuration json.
        /// </summary>
        [Parameter()]
        public ConfigScope Scope { get; set; } = ConfigScope.CurrentUser;

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
    [Cmdlet(VerbsLifecycle.Disable, "ExperimentalFeature", SupportsShouldProcess = true, HelpUri = "")]
    public class DisableExperimentalFeatureCommand : PSCmdlet
    {
        /// <summary>
        /// Get and set the feature names.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true, Position = 0, Mandatory = true)]
        [ArgumentCompleter(typeof(NameArgumentCompleter))]
        [ValidateNotNullOrEmpty]
        public string[] Name { get; set; }

        /// <summary>
        /// Get and set the scope of persistence of updating the PowerShell configuration json.
        /// </summary>
        [Parameter()]
        public ConfigScope Scope { get; set; } = ConfigScope.CurrentUser;

        /// <summary>
        /// ProcessRecord method of this cmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            ExperimentalFeatureConfigHelper.UpdateConfig(this, Name, Scope, enable: false);
        }
    }

    internal class ExperimentalFeatureConfigHelper
    {
        internal static void UpdateConfig(PSCmdlet cmdlet, string[] Name, ConfigScope scope, bool enable)
        {
            IEnumerable<WildcardPattern> namePatterns = SessionStateUtilities.CreateWildcardsFromStrings(Name, WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant);
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
                cmdlet.WriteObject(feature);
            }

            if (!foundFeature)
            {
                string errMsg = String.Format(CultureInfo.InvariantCulture, ExperimentalFeatureStrings.ExperimentalFeatureNameNotFound, Name);
                cmdlet.WriteError(new ErrorRecord(new ItemNotFoundException(errMsg), "ItemNotFoundException", ErrorCategory.ObjectNotFound, Name));
                return;
            }

            cmdlet.WriteWarning(ExperimentalFeatureStrings.ExperimentalFeaturePending);
        }
    }

    /// <summary>
    /// Provides argument completion for ExperimentalFeature names.
    /// </summary>
    public class NameArgumentCompleter : IArgumentCompleter
    {
        /// <summary>
        /// Implement CompleteArgument()
        /// </summary>
        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            if (fakeBoundParameters == null)
            {
                throw PSTraceSource.NewArgumentNullException("fakeBoundParameters");
            }

            var commandInfo = new CmdletInfo("Get-ExperimentalFeature", typeof(GetExperimentalFeatureCommand));
            var ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace).
                AddCommand(commandInfo).
                AddParameter("Name", wordToComplete + "*");

            HashSet<string> names = new HashSet<string>();
            var results = ps.Invoke<ExperimentalFeature>();
            foreach (var result in results)
            {
                names.Add(result.Name);
            }

            return names.OrderBy(name => name).Select(name => new CompletionResult(name, name, CompletionResultType.Text, name));

        }
    }
}
