// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Security;

//
// Now define the set of commands for manipulating modules.
//

namespace Microsoft.PowerShell.Commands
{
    #region Export-ModuleMember
    /// <summary>
    /// Implements a cmdlet that loads a module.
    /// </summary>
    [Cmdlet(VerbsData.Export, "ModuleMember", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096578")]
    public sealed class ExportModuleMemberCommand : PSCmdlet
    {
        /// <summary>
        /// This parameter specifies the functions to import from the module...
        /// </summary>
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        [AllowEmptyCollection]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        public string[] Function
        {
            get
            {
                return _functionList;
            }

            set
            {
                _functionList = value;
                // Create the list of patterns to match at parameter bind time
                // so errors will be reported before loading the module...
                _functionPatterns = new List<WildcardPattern>();
                if (_functionList != null)
                {
                    foreach (string pattern in _functionList)
                    {
                        _functionPatterns.Add(WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase));
                    }
                }
            }
        }

        private string[] _functionList;
        private List<WildcardPattern> _functionPatterns;

        /// <summary>
        /// This parameter specifies the functions to import from the module...
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [AllowEmptyCollection]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        public string[] Cmdlet
        {
            get
            {
                return _cmdletList;
            }

            set
            {
                _cmdletList = value;
                // Create the list of patterns to match at parameter bind time
                // so errors will be reported before loading the module...
                _cmdletPatterns = new List<WildcardPattern>();
                if (_cmdletList != null)
                {
                    foreach (string pattern in _cmdletList)
                    {
                        _cmdletPatterns.Add(WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase));
                    }
                }
            }
        }

        private string[] _cmdletList;
        private List<WildcardPattern> _cmdletPatterns;

        /// <summary>
        /// This parameter specifies the variables to import from the module...
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNull]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        public string[] Variable
        {
            get
            {
                return _variableExportList;
            }

            set
            {
                _variableExportList = value;
                // Create the list of patterns to match at parameter bind time
                // so errors will be reported before loading the module...
                _variablePatterns = new List<WildcardPattern>();
                if (_variableExportList != null)
                {
                    foreach (string pattern in _variableExportList)
                    {
                        _variablePatterns.Add(WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase));
                    }
                }
            }
        }

        private string[] _variableExportList;
        private List<WildcardPattern> _variablePatterns;

        /// <summary>
        /// This parameter specifies the aliases to import from the module...
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNull]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        public string[] Alias
        {
            get
            {
                return _aliasExportList;
            }

            set
            {
                _aliasExportList = value;
                // Create the list of patterns to match at parameter bind time
                // so errors will be reported before loading the module...
                _aliasPatterns = new List<WildcardPattern>();
                if (_aliasExportList != null)
                {
                    foreach (string pattern in _aliasExportList)
                    {
                        _aliasPatterns.Add(WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase));
                    }
                }
            }
        }

        private string[] _aliasExportList;
        private List<WildcardPattern> _aliasPatterns;

        /// <summary>
        /// Export the specified functions...
        /// </summary>
        protected override void ProcessRecord()
        {
            if (Context.EngineSessionState == Context.TopLevelSessionState)
            {
                string message = StringUtil.Format(Modules.CanOnlyBeUsedFromWithinAModule);
                InvalidOperationException invalidOp = new InvalidOperationException(message);
                ErrorRecord er = new ErrorRecord(invalidOp, "Modules_CanOnlyExecuteExportModuleMemberInsideAModule",
                    ErrorCategory.PermissionDenied, null);
                ThrowTerminatingError(er);
            }

            // Prevent script injection attack by disallowing ExportModuleMemberCommand to export module members across
            // language boundaries. This will prevent injected untrusted script from exporting private trusted module functions.
            if (Context.EngineSessionState.Module?.LanguageMode != null &&
                Context.LanguageMode != Context.EngineSessionState.Module.LanguageMode)
            {
                if (SystemPolicy.GetSystemLockdownPolicy() != SystemEnforcementMode.Audit)
                {
                    var se = new PSSecurityException(Modules.CannotExportMembersAccrossLanguageBoundaries);
                    var er = new ErrorRecord(se, "Modules_CannotExportMembersAccrossLanguageBoundaries", ErrorCategory.SecurityError, this);
                    ThrowTerminatingError(er);
                }

                SystemPolicy.LogWDACAuditMessage(
                    context: Context,
                    title: Modules.WDACExportModuleCommandLogTitle,
                    message: StringUtil.Format(Modules.WDACExportModuleCommandLogMessage, Context.EngineSessionState.Module.Name, Context.EngineSessionState.Module.LanguageMode, Context.LanguageMode),
                    fqid: "ExportModuleMemberCmdletNotAllowed",
                    dropIntoDebugger: true);
            }

            ModuleIntrinsics.ExportModuleMembers(this,
                this.Context.EngineSessionState,
                _functionPatterns, _cmdletPatterns, _aliasPatterns, _variablePatterns, null);
        }
    }
    #endregion Export-ModuleMember
}
