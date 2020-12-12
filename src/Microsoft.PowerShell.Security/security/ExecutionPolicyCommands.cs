// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System;
// System.Management.Automation is the namespace which contains the types and
// methods pertaining to the Microsoft Command Shell
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Tracing;
#endregion

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Defines the implementation of the 'Get-ExecutionPolicy' cmdlet.
    /// This cmdlet gets the effective execution policy of the shell.
    ///
    /// In priority-order (highest priority first,) these come from:
    ///    - Machine-wide Group Policy
    ///    - Current-user Group Policy
    ///    - Current session preference
    ///    - Current user machine preference
    ///    - Local machine preference.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "ExecutionPolicy", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096594")]
    [OutputType(typeof(ExecutionPolicy))]
    public class GetExecutionPolicyCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the scope of the execution policy.
        /// </summary>
        [Parameter(Position = 0, Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public ExecutionPolicyScope Scope
        {
            get { return _executionPolicyScope; }

            set { _executionPolicyScope = value; _scopeSpecified = true; }
        }

        private ExecutionPolicyScope _executionPolicyScope = ExecutionPolicyScope.LocalMachine;
        private bool _scopeSpecified = false;

        /// <summary>
        /// Gets or sets the List parameter, which lists all scopes and their execution
        /// policies.
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter List
        {
            get { return _list; }

            set { _list = value; }
        }

        private bool _list;

        /// <summary>
        /// Outputs the execution policy.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (_list && _scopeSpecified)
            {
                string message = ExecutionPolicyCommands.ListAndScopeSpecified;

                ErrorRecord errorRecord = new(
                    new InvalidOperationException(),
                    "ListAndScopeSpecified",
                    ErrorCategory.InvalidOperation,
                    targetObject: null);

                errorRecord.ErrorDetails = new ErrorDetails(message);
                ThrowTerminatingError(errorRecord);

                return;
            }

            string shellId = base.Context.ShellID;

            if (_list)
            {
                foreach (ExecutionPolicyScope scope in SecuritySupport.ExecutionPolicyScopePreferences)
                {
                    PSObject outputObject = new();

                    ExecutionPolicy policy = SecuritySupport.GetExecutionPolicy(shellId, scope);
                    PSNoteProperty inputNote = new("Scope", scope);
                    outputObject.Properties.Add(inputNote);
                    inputNote = new PSNoteProperty(
                            "ExecutionPolicy", policy);
                    outputObject.Properties.Add(inputNote);

                    WriteObject(outputObject);
                }
            }
            else if (_scopeSpecified)
            {
                WriteObject(SecuritySupport.GetExecutionPolicy(shellId, _executionPolicyScope));
            }
            else
            {
                WriteObject(SecuritySupport.GetExecutionPolicy(shellId));
            }
        }
    }

    /// <summary>
    /// Defines the implementation of the 'Set-ExecutionPolicy' cmdlet.
    /// This cmdlet sets the local preference for the execution policy of the
    /// shell.
    ///
    /// The execution policy may be overridden by settings in Group Policy.
    /// If the Group Policy setting overrides the desired behaviour, the Cmdlet
    /// generates a terminating error.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "ExecutionPolicy", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096612")]
    public class SetExecutionPolicyCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the execution policy that the user requests.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public ExecutionPolicy ExecutionPolicy
        {
            get { return _executionPolicy; }

            set { _executionPolicy = value; }
        }

        private ExecutionPolicy _executionPolicy;

        /// <summary>
        /// Gets or sets the scope of the execution policy.
        /// </summary>
        [Parameter(Position = 1, Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public ExecutionPolicyScope Scope
        {
            get { return _executionPolicyScope; }

            set { _executionPolicyScope = value; }
        }

        private ExecutionPolicyScope _executionPolicyScope = ExecutionPolicyScope.LocalMachine;

        /// <summary>
        /// Specifies whether to force the execution policy change.
        /// </summary>
        /// <value></value>
        [Parameter]
        public SwitchParameter Force
        {
            get
            {
                return _force;
            }

            set
            {
                _force = value;
            }
        }

        private SwitchParameter _force;

        /// <summary>
        /// Sets the execution policy (validation).
        /// </summary>
        protected override void BeginProcessing()
        {
            // Verify they've specified a valid scope
            if ((_executionPolicyScope == ExecutionPolicyScope.UserPolicy) ||
                (_executionPolicyScope == ExecutionPolicyScope.MachinePolicy))
            {
                string message = ExecutionPolicyCommands.CantSetGroupPolicy;

                ErrorRecord errorRecord = new(
                    new InvalidOperationException(),
                    "CantSetGroupPolicy",
                    ErrorCategory.InvalidOperation,
                    targetObject: null);

                errorRecord.ErrorDetails = new ErrorDetails(message);
                ThrowTerminatingError(errorRecord);

                return;
            }
        }

        /// <summary>
        /// Set the desired execution policy.
        /// </summary>
        protected override void ProcessRecord()
        {
            string shellId = base.Context.ShellID;
            string executionPolicy = SecuritySupport.GetExecutionPolicy(ExecutionPolicy);

            if (ShouldProcessPolicyChange(executionPolicy))
            {
                try
                {
                    SecuritySupport.SetExecutionPolicy(_executionPolicyScope, ExecutionPolicy, shellId);
                }
                catch (UnauthorizedAccessException exception)
                {
                    OnAccessDeniedError(exception);
                }
                catch (System.Security.SecurityException exception)
                {
                    OnAccessDeniedError(exception);
                }

                // Ensure it is now the effective execution policy
                if (ExecutionPolicy != ExecutionPolicy.Undefined)
                {
                    string effectiveExecutionPolicy = SecuritySupport.GetExecutionPolicy(shellId).ToString();
                    if (!string.Equals(effectiveExecutionPolicy, executionPolicy, StringComparison.OrdinalIgnoreCase))
                    {
                        string message = StringUtil.Format(ExecutionPolicyCommands.ExecutionPolicyOverridden, effectiveExecutionPolicy);
                        string recommendedAction = ExecutionPolicyCommands.ExecutionPolicyOverriddenRecommendedAction;

                        ErrorRecord errorRecord = new(
                            new System.Security.SecurityException(),
                            "ExecutionPolicyOverride",
                            ErrorCategory.PermissionDenied,
                            targetObject: null);

                        errorRecord.ErrorDetails = new ErrorDetails(message);
                        errorRecord.ErrorDetails.RecommendedAction = recommendedAction;
                        ThrowTerminatingError(errorRecord);
                    }
                }

                PSEtwLog.LogSettingsEvent(MshLog.GetLogContext(Context, MyInvocation),
                    EtwLoggingStrings.ExecutionPolicyName, executionPolicy, null);
            }
        }

        // Determine if we should process this policy change
#if CORECLR // Seems that we cannot find if the cmdlet is executed interactive or through a script on CoreCLR
        private bool ShouldProcessPolicyChange(string localPreference)
        {
            return ShouldProcess(localPreference);
        }
#else
        private bool ShouldProcessPolicyChange(string localPreference)
        {
            if (ShouldProcess(localPreference))
            {
                // See if we're being invoked directly at the
                // command line. In that case, give a warning.
                if (!Force)
                {
                    // We don't give this warning if we're in a script, or
                    // if we don't have a window handle
                    // (i.e.: PowerShell -command Set-ExecutionPolicy Unrestricted)
                    if (IsProcessInteractive())
                    {
                        string query = ExecutionPolicyCommands.SetExecutionPolicyQuery;
                        string caption = ExecutionPolicyCommands.SetExecutionPolicyCaption;

                        try
                        {
                            bool yesToAllNoToAllDefault = false;
                            if (!ShouldContinue(query, caption, true, ref yesToAllNoToAllDefault, ref yesToAllNoToAllDefault))
                            {
                                return false;
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            // Host is non-interactive. This should
                            // return false, but must return true due
                            // to backward compatibility.
                            return true;
                        }
                        catch (System.Management.Automation.Host.HostException)
                        {
                            // Host doesn't implement ShouldContinue. This should
                            // return false, but must return true due
                            // to backward compatibility.
                            return true;
                        }
                    }
                }

                return true;
            }

            return false;
        }

        private bool IsProcessInteractive()
        {
            // CommandOrigin != Runspace means it is in a script
            if (MyInvocation.CommandOrigin != CommandOrigin.Runspace)
                return false;

            // If we don't own the window handle, we've been invoked
            // from another process that just calls "PowerShell -Command"
            if (System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle == IntPtr.Zero)
                return false;

            // If the window has been idle for less than a second,
            // they're probably still calling "PowerShell -Command"
            // but from Start-Process, or the StartProcess API
            try
            {
                System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                TimeSpan timeSinceStart = DateTime.Now - currentProcess.StartTime;
                TimeSpan idleTime = timeSinceStart - currentProcess.TotalProcessorTime;

                if (idleTime.TotalSeconds > 1)
                    return true;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Don't have access to the properties
                return false;
            }

            return false;
        }
#endif

        // Throw terminating error when the access to the registry is denied
        private void OnAccessDeniedError(Exception exception)
        {
            string message = StringUtil.Format(ExecutionPolicyCommands.SetExecutionPolicyAccessDeniedError, exception.Message);
            ErrorRecord errorRecord = new(
                exception,
                exception.GetType().FullName,
                ErrorCategory.PermissionDenied,
                targetObject: null);

            errorRecord.ErrorDetails = new ErrorDetails(message);
            ThrowTerminatingError(errorRecord);
        }
    }
}
