
/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

namespace System.Management.Automation
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Generic;
    using System.Management.Automation.Runspaces;
    using System.Collections;

    /// <summary>
    /// Auxilliary class to the execution of commands as needed by
    /// CommandCompletion
    /// </summary>
    internal class CompletionExecutionHelper
    {
        #region Constructors

        // Creates a new CompletionExecutionHelper with the PowerShell instance that will be used to execute the tab expansion commands
        // Used by the ISE
        internal CompletionExecutionHelper(PowerShell powershell)
        {
            if (powershell == null)
            {
                throw PSTraceSource.NewArgumentNullException("powershell");
            }

            this.CurrentPowerShell = powershell;
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
        internal bool IsRunning
        {
            get { return CurrentPowerShell.InvocationStateInfo.State == PSInvocationState.Running; }
        }

        // Returns true if the command executed by this instance was stopped
        internal bool IsStopped
        {
            get { return CurrentPowerShell.InvocationStateInfo.State == PSInvocationState.Stopped; }
        }

        #endregion Fields and Properties

        #region Command Execution

        internal Collection<PSObject> ExecuteCommand(string command)
        {
            Exception unused;
            return this.ExecuteCommand(command, true, out unused, null);
        }

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

        internal string ExecuteCommandAndGetResultAsString()
        {
            Exception exceptionThrown;
            Collection<PSObject> streamResults = ExecuteCurrentPowerShell(out exceptionThrown);

            if (exceptionThrown != null || streamResults == null || streamResults.Count == 0)
            {
                return null;
            }

            // we got back one or more objects. Pick off the first result.
            if (streamResults[0] == null)
                return String.Empty;

            // And convert the base object into a string. We can't use the proxied
            // ToString() on the PSObject because there is no default runspace
            // available.
            return SafeToString(streamResults[0]);
        }

        internal Collection<PSObject> ExecuteCommand(string command, bool isScript, out Exception exceptionThrown, Hashtable args)
        {
            Diagnostics.Assert(command != null, "caller to verify command is not null");

            exceptionThrown = null;

            // This flag indicates a previous call to this method had its pipeline cancelled
            if (this.CancelTabCompletion)
            {
                return new Collection<PSObject>();
            }

            CurrentPowerShell.AddCommand(command);

            Command cmd = new Command(command, isScript);
            if (args != null)
            {
                foreach (DictionaryEntry arg in args)
                {
                    cmd.Parameters.Add((string)(arg.Key), arg.Value);
                }
            }

            Collection<PSObject> results = null;
            try
            {
                // blocks until all results are retrieved.
                //results = this.ExecuteCommand(cmd);

                // If this pipeline has been stopped lets set a flag to cancel all future tab completion calls
                // untill the next completion
                if (this.IsStopped)
                {
                    results = new Collection<PSObject>();
                    this.CancelTabCompletion = true;
                }
            }
            catch (Exception e)
            {
                CommandProcessorBase.CheckForSevereException(e);
                exceptionThrown = e;
            }

            return results;
        }

        internal Collection<PSObject> ExecuteCurrentPowerShell(out Exception exceptionThrown, IEnumerable input = null)
        {
            exceptionThrown = null;

            // This flag indicates a previous call to this method had its pipeline cancelled
            if (this.CancelTabCompletion)
            {
                return new Collection<PSObject>();
            }

            Collection<PSObject> results = null;
            try
            {
                results = CurrentPowerShell.Invoke(input);

                // If this pipeline has been stopped lets set a flag to cancel all future tab completion calls
                // untill the next completion
                if (this.IsStopped)
                {
                    results = new Collection<PSObject>();
                    this.CancelTabCompletion = true;
                }
            }
            catch (Exception e)
            {
                CommandProcessorBase.CheckForSevereException(e);
                exceptionThrown = e;
            }
            finally
            {
                CurrentPowerShell.Commands.Clear();
            }

            return results;
        }

        #endregion Command Execution

        #region Helpers

        /// <summary>
        /// Converts an object to a string safely...
        /// </summary>
        /// <param name="obj">The object to convert</param>
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
                    if (baseObject != null && !(baseObject is PSCustomObject))
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
            catch (Exception e)
            {
                // We swallow all exceptions from command completion because we don't want the shell to crash
                CommandProcessorBase.CheckForSevereException(e);

                return string.Empty;
            }
        }

        /// <summary>
        /// Converts an object to a string adn, if the string is not empty, adds it to the list
        /// </summary>
        /// <param name="list">The list to update</param>
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
}