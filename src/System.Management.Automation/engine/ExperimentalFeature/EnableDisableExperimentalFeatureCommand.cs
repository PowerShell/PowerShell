// Copyright (c) Microsoft Corporation.
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
    public class ExperimentalFeatureNameCompleter : IArgumentCompleter
    {
        /// <summary>
        /// Returns completion results for experimental feature names used as arguments to experimental feature cmdlets.
        /// </summary>
        /// <param name="commandName">The command name.</param>
        /// <param name="parameterName">The parameter name.</param>
        /// <param name="wordToComplete">The word to complete.</param>
        /// <param name="commandAst">The command AST.</param>
        /// <param name="fakeBoundParameters">The fake bound parameters.</param>
        /// <returns>List of Completion Results.</returns>
        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            if (fakeBoundParameters == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(fakeBoundParameters));
            }

            var commandInfo = new CmdletInfo("Get-ExperimentalFeature", typeof(GetExperimentalFeatureCommand));
            var ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace)
                .AddCommand(commandInfo)
                .AddParameter("Name", wordToComplete + "*");

            HashSet<string> names = new HashSet<string>();
            var results = ps.Invoke<ExperimentalFeature>();
            foreach (var result in results)
            {
                names.Add(result.Name);
            }

            return names.Order().Select(static name => new CompletionResult(name, name, CompletionResultType.Text, name));
        }
    }
}
