// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Runspaces;

namespace System.Management.Automation
{
    internal class PowerShellExecutionHelper
    {
        #region Constructors

        // Creates a new PowerShellExecutionHelper with the PowerShell instance that will be used to execute the tab expansion commands
        // Used by the ISE
        internal PowerShellExecutionHelper(PowerShell powershell)
        {
            if (powershell == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(powershell));
            }

            CurrentPowerShell = powershell;
        }

        #endregion Constructors

        #region Fields and Properties

        // Gets and sets a flag set to false at the beginning of each tab completion and
        // set to true if a pipeline is stopped to indicate all commands should return empty matches
        internal bool CancelTabCompletion { get; set; }

        // Gets and sets the PowerShell instance used to run command completion commands
        // Used by the ISE
        internal PowerShell CurrentPowerShell { get; set; }

        // Returns true if this instance is currently executing a command
        internal bool IsRunning => CurrentPowerShell.InvocationStateInfo.State == PSInvocationState.Running;

        // Returns true if the command executed by this instance was stopped
        internal bool IsStopped => CurrentPowerShell.InvocationStateInfo.State == PSInvocationState.Stopped;

        #endregion Fields and Properties

        #region Command Execution

        internal bool ExecuteCommandAndGetResultAsBool()
        {
            Exception exceptionThrown;
            Collection<PSObject> streamResults = ExecuteCurrentPowerShell(out exceptionThrown);

            if (exceptionThrown != null || streamResults == null || streamResults.Count == 0)
            {
                return false;
            }

            // we got back one or more objects.
            return (streamResults.Count > 1) || (LanguagePrimitives.IsTrue(streamResults[0]));
        }

        internal Collection<PSObject> ExecuteCurrentPowerShell(out Exception exceptionThrown, IEnumerable input = null)
        {
            return ExecuteCurrentPowerShell(out exceptionThrown, out _, input);
        }

        internal Collection<PSObject> ExecuteCurrentPowerShell(out Exception exceptionThrown, out bool hadErrors, IEnumerable input = null)
        {
            exceptionThrown = null;

            // This flag indicates a previous call to this method had its pipeline cancelled
            if (CancelTabCompletion)
            {
                hadErrors = false;
                return new Collection<PSObject>();
            }

            Collection<PSObject> results = null;
            try
            {
                results = CurrentPowerShell.Invoke(input);

                // If this pipeline has been stopped lets set a flag to cancel all future tab completion calls
                // until the next completion
                if (IsStopped)
                {
                    results = new Collection<PSObject>();
                    CancelTabCompletion = true;
                }
            }
            catch (Exception e)
            {
                exceptionThrown = e;
            }
            finally
            {
                hadErrors = CurrentPowerShell.HadErrors;
                CurrentPowerShell.Commands.Clear();
            }

            return results;
        }

        #endregion Command Execution

        #region Helpers

        /// <summary>
        /// Converts an object to a string safely...
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <returns>The result of the conversion...</returns>
        internal static string SafeToString(object obj)
        {
            if (obj == null)
            {
                return string.Empty;
            }

            try
            {
                PSObject pso = obj as PSObject;
                string result;
                if (pso != null)
                {
                    object baseObject = pso.BaseObject;
                    if (baseObject != null && baseObject is not PSCustomObject)
                        result = baseObject.ToString();
                    else
                        result = pso.ToString();
                }
                else
                {
                    result = obj.ToString();
                }

                return result;
            }
            catch (Exception)
            {
                // We swallow all exceptions from command completion because we don't want the shell to crash
                return string.Empty;
            }
        }

        /// <summary>
        /// Converts an object to a string adn, if the string is not empty, adds it to the list.
        /// </summary>
        /// <param name="list">The list to update.</param>
        /// <param name="obj">The object to convert to a string...</param>
        internal static void SafeAddToStringList(List<string> list, object obj)
        {
            if (list == null)
                return;
            string result = SafeToString(obj);
            if (!string.IsNullOrEmpty(result))
                list.Add(result);
        }

        #endregion Helpers
    }

    internal static class PowerShellExtensionHelpers
    {
        internal static PowerShell AddCommandWithPreferenceSetting(this PowerShellExecutionHelper helper,
            string command, Type type = null)
        {
            return helper.CurrentPowerShell.AddCommandWithPreferenceSetting(command, type);
        }

        internal static PowerShell AddCommandWithPreferenceSetting(this PowerShell powershell, string command, Type type = null)
        {
            Diagnostics.Assert(powershell != null, "the passed-in powershell cannot be null");
            Diagnostics.Assert(!string.IsNullOrWhiteSpace(command),
                "the passed-in command name should not be null or whitespaces");

            if (type != null)
            {
                var cmdletInfo = new CmdletInfo(command, type);

                powershell.AddCommand(cmdletInfo);
            }
            else
            {
                powershell.AddCommand(command);
            }

            powershell
                .AddParameter("ErrorAction", ActionPreference.Ignore)
                .AddParameter("WarningAction", ActionPreference.Ignore)
                .AddParameter("InformationAction", ActionPreference.Ignore)
                .AddParameter("Verbose", false)
                .AddParameter("Debug", false);

            return powershell;
        }
    }
}
