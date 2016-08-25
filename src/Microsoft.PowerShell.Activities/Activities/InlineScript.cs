//
//    Copyright (C) Microsoft.  All rights reserved.
//
﻿using System;
using System.Activities;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.ComponentModel;
using System.Text;
using System.Reflection;

namespace Microsoft.PowerShell.Activities
{
    /// <summary>
    /// Activity to support the invocation of PowerShell script content in a Workflow.
    /// </summary>
#if _NOTARMBUILD_
    [Designer(typeof(InlineScriptDesigner))]
#endif
    public sealed class InlineScript : PSRemotingActivity
    {
        /// <summary>
        /// The script text to invoke.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public string Command
        {
            get { return _command; }
            set
            {
                _command = value;
                _commandSpecified = true;
            }
        }
        private string _command;
        private bool _commandSpecified;

        /// <summary>
        /// Name of the command to invoke
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<string> CommandName { get; set; }

        /// <summary>
        /// Parameters to invoke the command with.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Collections.Hashtable> Parameters { get; set; }

        /// <summary>
        /// Declares that this activity supports its own remoting.
        /// </summary>        
        protected override bool SupportsCustomRemoting { get { return true; } }

        private ScriptBlock _compiledScriptForInProc;
        private ScriptBlock _compiledScriptForOutProc;
        private string _scriptWithoutUsing;
        private HashSet<string> _usingVariables;
        // Remember the names of the variables/arguments that statically exist in the
        // workflow context and potentially can be referenced by a using variable in 
        // an InlineScript. Those static variables/arguments include:
        //  1. the default arguments of InlineScript and its parents
        //  2. the workflow runtime variables
        //
        // This static set is used to decide whether to add the special prefix
        // to a variable or not, when replacing a using variable.
        private static readonly HashSet<string> StaticPotentialUsingVariableSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private const string VariablePrefix = "__PSUsingVariable_";


        static InlineScript()
        {
            PopulatePotentialUsingVariableStaticSet();
        }

        private static void PopulatePotentialUsingVariableStaticSet()
        {
            var namesToExclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                                     {
                                         // from inlinescript common arguments
                                         "Result", "PSError", "PSWarning", "PSVerbose", "PSDebug", "PSProgress", "PSInformation",

                                         // from workflow runtime variables
                                         "Other", "All",

                                         // some workflow variables/arguments conflict with the built-in powershell variables, including:
                                         //   Input, PSSessionOption, PSCulture, PSUICulture, PSVersionTable
                                         //
                                         // per discussion with Hemant and Rahim, we want to:
                                         //   1. treat $using:input, $using:PSSessionOption, $using:PSCulture, and $using:PSUICulture as workflow 
                                         //      variable; add the special prefix when replacing 'using'.
                                         //   2. treat PSVersionTable as powershell variable, so never add special prefix to it.
                                         "PSVersionTable"
                                     };

            // Handle InlineScript activity common arguments
            foreach (string argumentName in GetInlineScriptActivityArguments())
            {
                if (namesToExclude.Contains(argumentName)) { continue; }
                if (!StaticPotentialUsingVariableSet.Contains(argumentName))
                {
                    StaticPotentialUsingVariableSet.Add(argumentName);
                }
            }

            // Handle workflow runtime variables
            var wfRuntimeVariables = typeof(PSWorkflowRuntimeVariable).GetEnumNames();
            foreach (string variableName in wfRuntimeVariables)
            {
                if (namesToExclude.Contains(variableName)) { continue; }
                if (!StaticPotentialUsingVariableSet.Contains(variableName))
                {
                    StaticPotentialUsingVariableSet.Add(variableName);
                }
            }
        }

        // Use the same logic as the PSActivity.GetActivityArguments to retrieve the names of all default 
        // arguments from the InlineScript and its parents
        internal static IEnumerable<string> GetInlineScriptActivityArguments()
        {
            Type activityType = typeof(InlineScript);

            while (activityType != null)
            {
                // We don't want to support parameter defaults for arguments on
                // concrete types (as they almost guaranteed to collide with other types),
                // but base classes make sense.
                if (activityType.IsAbstract)
                {
                    // Populate any parameter defaults. We only look at fields that are defined on this
                    // specific type (as opposed to derived types) so that we don't make assumptions about
                    // other activities and their defaults.
                    foreach (PropertyInfo field in activityType.GetProperties())
                    {
                        // See if it's an argument
                        if (typeof(Argument).IsAssignableFrom(field.PropertyType))
                        {
                            // Get the argument name
                            yield return field.Name;
                        }
                    }
                }

                // Go to our base type, but stop when we go above PSActivity
                activityType = activityType.BaseType;
                if (!typeof(PSActivity).IsAssignableFrom(activityType))
                    activityType = null;
            }
        }

        /// <summary>
        /// Validates the contents of the script block for this command.
        /// </summary>
        /// <param name="metadata">Metadata for this activity</param>
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            if (! string.IsNullOrWhiteSpace(Command))
            {
                Token[] tokens;
                ParseError[] errors;
                Parser.ParseInput(Command, out tokens, out errors);
                if (errors != null && errors.Length > 0)
                {
                    string compositeErrorString = "";
                    foreach (var e in errors)
                    {
                        // Format and add each error message...
                        compositeErrorString += string.Format(CultureInfo.InvariantCulture,
                            "[{0}, {1}]: {2}\n", e.Extent.StartLineNumber, e.Extent.StartColumnNumber, e.Message);
                    }
                    metadata.AddValidationError(compositeErrorString);
                }
            }
        }

        /// <summary>
        /// Indicates if preference variables need to be updated
        /// </summary>
        protected override bool UpdatePreferenceVariable
        {
            get { return false; }
        }

        /// <summary>
        /// Returns a configured instance of System.Management.Automation.PowerShell, pre-populated with the script to run.
        /// </summary>
        /// <param name="context">The NativeActivityContext for the currently running activity.</param>
        /// <returns>A populated instance of System.Management.Automation.PowerShell</returns>
        /// <remarks>The infrastructure takes responsibility for closing and disposing the PowerShell instance returned.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "Disposed by the infrastructure.")]
        protected override ActivityImplementationContext GetPowerShell(NativeActivityContext context)
        {
            ValidateParameters();
            System.Management.Automation.PowerShell invoker = null;
            HashSet<string> allWorkflowVarNames = new HashSet<string>(StaticPotentialUsingVariableSet, StringComparer.OrdinalIgnoreCase);
            Dictionary<string, object> defaults = this.ParameterDefaults.Get(context);
            Dictionary<string, object> activityVariables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, object> activityUsingVariables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            string[] streams =
            {
                "Result", "PSError", "PSWarning", "PSVerbose", "PSDebug", "PSProgress", "PSInformation"
            };

            // First, set the variables from the user's variables
            foreach (System.ComponentModel.PropertyDescriptor property in context.DataContext.GetProperties())
            {
                if (String.Equals(property.Name, "ParameterDefaults", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Add all user-defined variables/parameters in the same scope of the InlineScript activity
                if (!allWorkflowVarNames.Contains(property.Name))
                {
                    allWorkflowVarNames.Add(property.Name);
                }

                Object value = property.GetValue(context.DataContext);
                if (value != null)
                {
                    object tempValue = value;

                    PSDataCollection<PSObject> collectionObject = value as PSDataCollection<PSObject>;

                    if (collectionObject != null && collectionObject.Count == 1)
                    {
                        tempValue = collectionObject[0];
                    }

                    activityVariables[property.Name] = tempValue;
                }               
            }

            // Then, set anything we received from parameters
            foreach (PSActivityArgumentInfo currentArgument in GetActivityArguments())
            {
                string @default = currentArgument.Name;
                if (streams.Any(item => string.Equals(item, @default, StringComparison.OrdinalIgnoreCase)))
                    continue;

                object argumentValue = currentArgument.Value.Get(context);
                if (argumentValue != null && !activityVariables.ContainsKey(currentArgument.Name))
                {
                    activityVariables[currentArgument.Name] = argumentValue;
                }
            }

            // Then, set the variables from the host defaults
            if (defaults != null)
            {
                foreach (string hostDefault in defaults.Keys)
                {
                     string @default = hostDefault;
                    if (streams.Any(item => string.Equals(item, @default, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    object propertyValue = defaults[hostDefault];
                    if (propertyValue != null && !activityVariables.ContainsKey(hostDefault))
                    {
                        activityVariables[hostDefault] = propertyValue;
                    }
                }
            }

            if (_commandSpecified)
            {
                string script = string.IsNullOrEmpty(Command) ? string.Empty : Command;
                Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: Inline Script: '{1}'.", context.ActivityInstanceId, script));

                if (IsBlocked(script))
                {
                    throw new PSInvalidOperationException(String.Format(CultureInfo.InvariantCulture, ActivityResources.CannotLaunchFormat, script));
                }

                string[] targetNodes = null;
                if (this.PSComputerName.Expression != null)
                {
                    targetNodes = this.PSComputerName.Get(context);
                }
                else
                {
                    if (defaults != null && defaults.ContainsKey("PSComputerName"))
                    {
                        targetNodes = this.ParameterDefaults.Get(context)["PSComputerName"] as string[];
                    }
                }

                // See if this command will be run in process.
                if ((targetNodes == null || targetNodes.Length == 0) && GetRunInProc(context))
                {
                    if (_compiledScriptForInProc == null || _ci == null)
                    {
                        lock (Syncroot)
                        {
                            if (_compiledScriptForInProc == null)
                            {
                                if (_scriptWithoutUsing == null)
                                {
                                    _scriptWithoutUsing = RemoveUsingPrefix(script, allWorkflowVarNames, out _usingVariables);
                                }
                                _compiledScriptForInProc = ScriptBlock.Create(_scriptWithoutUsing);
                            }

                            // Invoke using the CommandInfo for Invoke-Command directly, rather than going through
                            // the command discovery since this is much faster.
                            if (_ci == null)
                            {
                                _ci = new CmdletInfo("Invoke-Command", typeof(Microsoft.PowerShell.Commands.InvokeCommandCommand));
                            }
                        }
                    }

                    SetAvailableUsingVariables(activityVariables, activityUsingVariables);
                    Tracer.WriteMessage("PowerShell activity: executing InlineScript locally with ScriptBlock.");
                    invoker = System.Management.Automation.PowerShell.Create();
                    invoker.AddCommand(_ci).AddParameter("NoNewScope").AddParameter("ScriptBlock", _compiledScriptForInProc);
                }
                else
                {
                    // Try to convert the ScriptBlock to a powershell instance
                    if (_compiledScriptForOutProc == null)
                    {
                        lock (Syncroot)
                        {
                            if (_compiledScriptForOutProc == null)
                            {
                                _compiledScriptForOutProc = ScriptBlock.Create(script);
                            }
                        }
                    }

                    try
                    {
                        // we trust the code inside inlinescript, set isTrusted as True.
                        invoker = _compiledScriptForOutProc.GetPowerShell(activityVariables, out activityUsingVariables, true);
                        Tracer.WriteMessage("PowerShell activity: executing InlineScript with ScriptBlock to powershell conversion.");
                    }
                    catch (Exception)
                    {
                        invoker = null;
                    }

                    if (invoker == null)
                    {
                        // Since scriptblocks aren't serialized with fidelity in the remote case, we need to
                        // use AddScript instead.
                        if (_scriptWithoutUsing == null)
                        {
                            lock (Syncroot)
                            {
                                if (_scriptWithoutUsing == null)
                                {
                                    _scriptWithoutUsing = RemoveUsingPrefix(script, allWorkflowVarNames, out _usingVariables);
                                }
                            }
                        }

                        SetAvailableUsingVariables(activityVariables, activityUsingVariables);
                        Tracer.WriteMessage("PowerShell activity: executing InlineScript by using AddScript.");
                        invoker = System.Management.Automation.PowerShell.Create();
                        invoker.AddScript(_scriptWithoutUsing);
                    }
                }
            }
            else
            {
                string commandName = CommandName.Get(context);
                if (String.IsNullOrEmpty(commandName))
                {
                    throw new ArgumentException(ActivityResources.CommandNameRequired);
                }

                Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: Invoking command '{1}'.", context.ActivityInstanceId, commandName));
                invoker = System.Management.Automation.PowerShell.Create();
                invoker.AddCommand(commandName);

                System.Collections.Hashtable parameters = Parameters.Get(context);

                if (parameters != null && parameters.Count > 0)
                {
                    foreach (var key in parameters.Keys)
                    {
                        Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity: Adding parameter '-{0} {1}'.",
                            key, parameters[key]));
                    }
                    invoker.AddParameters(parameters);
                }
            }

            var implementationContext = new ActivityImplementationContext
                                            {
                                                PowerShellInstance = invoker,
                                                WorkflowContext = activityUsingVariables
                                            };

            return implementationContext;
        }

        private void SetAvailableUsingVariables(Dictionary<string, object> allActivityVariables, Dictionary<string, object> activityUsingVariables)
        {
            if (_usingVariables == null) { return; }

            foreach (string varName in _usingVariables)
            {
                object value;
                string varNameToUse = VariablePrefix + varName;
                if (allActivityVariables.TryGetValue(varName, out value) && !activityUsingVariables.ContainsKey(varNameToUse))
                {
                    activityUsingVariables.Add(varNameToUse, value);
                }
            }
        }

        private void ValidateParameters()
        {
            if (_commandSpecified)
            {
                if (CommandName.Expression != null || Parameters.Expression != null)
                {
                    throw new ArgumentException(ActivityResources.CannotSpecifyBothCommandAndCommandName);
                }
            }
            else
            {
                if (CommandName.Expression == null)
                {
                    throw new ArgumentException(ActivityResources.CannotSpecifyBothCommandAndCommandName);
                }
            }
        }

        /// <summary>
        /// Checks if the script is blocked
        /// </summary>
        /// <param name="script"></param>
        private bool IsBlocked(string script)
        {
            string[] psUnsupportedConsoleApplications = new string[]
                {
                    "cmd",
                    "cmd.exe",
                    "diskpart",
                    "diskpart.exe",
                    "edit.com",
                    "netsh",
                    "netsh.exe",
                    "nslookup",
                    "nslookup.exe",
                    "powershell",
                    "powershell.exe",
                };

            foreach (string app in psUnsupportedConsoleApplications)
            {
                if (script.Equals(app, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        #region "Using variable utility"

        /// <summary>
        /// Remove the "Using" prefix for all UsingExpressionAsts that appear in the given script
        /// </summary>
        /// <param name="script">script text</param>
        /// <param name="allWorkflowVariables">all workflow variables/arguments that potentially can be referred by a using variable</param>
        /// <param name="usingVariables">names of the variables in the script that have the "Using" prefix</param>
        /// <returns>
        /// Return <para>script</para> if the script text is empty string or null
        /// Return <para>script</para> if there are errors when parsing the script text
        /// Return <para>script</para> if there is no UsingExpressionAst in the given script
        /// Return a new script text that has all the "Using" prefixes removed
        /// </returns>
        private static string RemoveUsingPrefix(string script, HashSet<string> allWorkflowVariables, out HashSet<string> usingVariables)
        {
            usingVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var usingAsts = GetUsingExpressionAsts(script);
            if (usingAsts == null || !usingAsts.Any()) { return script; }

            StringBuilder newScript = null;
            int startOffset = 0;
            foreach (Ast ast in usingAsts)
            {
                var usingAst = ast as UsingExpressionAst;
                if (usingAst == null) { continue; }

                VariableExpressionAst variableAst = UsingExpressionAst.ExtractUsingVariable(usingAst);
                if (variableAst == null) { continue; }

                if (newScript == null)
                {
                    newScript = new StringBuilder();
                }

                string varName = variableAst.VariablePath.UserPath;
                string varSign = variableAst.Splatted ? "@" : "$";
                bool needPrefix = allWorkflowVariables.Contains(varName);
                string newVar = needPrefix ? (varSign + VariablePrefix + varName) : (varSign + varName);

                // Add those variable names that potentially refer to workflow variables/arguments
                if (needPrefix && !usingVariables.Contains(varName))
                {
                    usingVariables.Add(varName);
                }

                newScript.Append(script.Substring(startOffset, variableAst.Extent.StartOffset - startOffset));
                newScript.Append(newVar);
                startOffset = variableAst.Extent.EndOffset;
            }

            if (newScript != null)
            {
                newScript.Append(script.Substring(startOffset));
                return newScript.ToString();
            }

            return script;
        }

        /// <summary>
        /// Get the UsingExpressionAsts out of a script
        /// </summary>
        /// <param name="script"></param>
        /// <returns>a list of UsingExpressionAsts ordered by the StartOffset</returns>
        private static IEnumerable<Ast> GetUsingExpressionAsts(string script)
        {
            if (String.IsNullOrEmpty(script))
            {
                return null;
            }

            ParseError[] errors;
            Token[] tokens;
            ScriptBlockAst scriptAst = Parser.ParseInput(script, out tokens, out errors);
            if (errors.Length != 0)
            {
                return null;
            }

            var list = scriptAst.FindAll(ast => ast is UsingExpressionAst, searchNestedScriptBlocks: true).ToList();
            if (list.Count > 1)
            {
                return list.OrderBy(a => a.Extent.StartOffset);
            }
            return list;
        }

        #endregion  "Using variable utility"

        /// <summary>
        /// Adds the PSActivity variable to the active runspace, which is of type InlineScriptContext.
        /// </summary>
        /// <param name="implementationContext">The ActivityImplementationContext returned by the call to GetCommand.</param>
        protected override void PrepareSession(ActivityImplementationContext implementationContext)
        {
            if (implementationContext.PSActivityEnvironment == null)
            {
                implementationContext.PSActivityEnvironment = new PSActivityEnvironment();
            }

            // Update the preference variables
            UpdatePreferenceVariables(implementationContext);
            System.Management.Automation.PowerShell session = implementationContext.PowerShellInstance;

            implementationContext.PSActivityEnvironment.Variables["UserName"] = System.Environment.UserName;

            string computerName = null;
            if (implementationContext.ConnectionInfo != null)
            {
                computerName = implementationContext.ConnectionInfo.ComputerName;
            }
            if (string.IsNullOrEmpty(computerName))
            {
                computerName = "localhost";
            }

            implementationContext.PSActivityEnvironment.Variables["ComputerName"] = computerName;
            implementationContext.PSActivityEnvironment.Variables["PSComputerName"] = computerName;

            string workflowCommandName = null;

            Dictionary<string, object> activityVariables = (Dictionary<string, object>)implementationContext.WorkflowContext;
            if (activityVariables != null && activityVariables.ContainsKey("ParameterDefaults"))
            {
                HostParameterDefaults defaults = activityVariables["ParameterDefaults"] as HostParameterDefaults;
                if (defaults != null)
                {
                    workflowCommandName = defaults.Parameters["WorkflowCommandName"] as string;
                }
            }

            if (string.IsNullOrEmpty(workflowCommandName))
            {
                workflowCommandName = "unknown";
            }

            implementationContext.PSActivityEnvironment.Variables["CommandName"] = workflowCommandName;

            // Populate the default variables
            InlineScriptContext inlineScriptContext = new InlineScriptContext(this);

            // Populate the activity variables
            foreach (KeyValuePair<string, object> entry in activityVariables)
            {
                if (String.Equals(entry.Key, "ParameterDefaults", StringComparison.OrdinalIgnoreCase))
                {
                    System.Diagnostics.Debug.Assert(entry.Value is HostParameterDefaults, "ParameterDefaults does not contain a HostParameterDefaults object");
                    inlineScriptContext.Variables[entry.Key] = ((HostParameterDefaults)entry.Value).Parameters;
                    continue;
                }
                inlineScriptContext.Variables[entry.Key] = entry.Value;
            }

            // Set the PowerShell session variables...            
            foreach (KeyValuePair<string, object> entry in activityVariables)
            {
                var value = entry.Value;

                if (String.Equals(entry.Key, "ParameterDefaults", StringComparison.OrdinalIgnoreCase))
                    continue;
                implementationContext.PSActivityEnvironment.Variables[entry.Key] = value;
            }
        }

        // InlineScript needs to handle these specially, since it might go through the PowerShell AddScript() API.
        // If the parameter "CommandName" is in use, we add the preference configuration to the command parameters,
        // otherwise, we add the preference configuration to the preference variable.
        // All other activities have this set automatically by the infrastructure via parameters.
        private void UpdatePreferenceVariables(ActivityImplementationContext implementationContext)
        {
            System.Management.Automation.PowerShell session = implementationContext.PowerShellInstance;
            System.Management.Automation.Runspaces.Command command = null;

            if (!_commandSpecified)
            {
                // "CommandName" and "Parameters" are in use
                command = session.Commands.Commands[0];
            }

            if (implementationContext.Verbose != null)
            {
                if (command != null)
                {
                    command.Parameters.Add("Verbose", implementationContext.Verbose);
                }
                else
                {
                    // Map the boolean / switch to an actual action preference
                    ActionPreference preference = ActionPreference.SilentlyContinue;

                    if (implementationContext.Verbose.Value)
                        preference = ActionPreference.Continue;

                    implementationContext.PSActivityEnvironment.Variables["VerbosePreference"] = preference;
                }
            }

            if (implementationContext.Debug != null)
            {
                if (command != null)
                {
                    command.Parameters.Add("Debug", implementationContext.Debug);
                }
                else
                {
                    // Map the boolean / switch to an actual action preference
                    ActionPreference preference = ActionPreference.SilentlyContinue;

                    if (implementationContext.Debug.Value)
                        preference = ActionPreference.Continue;

                    implementationContext.PSActivityEnvironment.Variables["DebugPreference"] = preference;
                }
            }

            if (implementationContext.WhatIf != null && command != null)
            {
                command.Parameters.Add("WhatIf", implementationContext.WhatIf);
            }

            if (implementationContext.ErrorAction != null)
            {
                if (command != null)
                {
                    command.Parameters.Add("ErrorAction", implementationContext.ErrorAction);
                }
                else
                {
                    implementationContext.PSActivityEnvironment.Variables["ErrorActionPreference"] = implementationContext.ErrorAction;
                }
            }

            if (implementationContext.WarningAction != null)
            {
                if (command != null)
                {
                    command.Parameters.Add("WarningAction", implementationContext.WarningAction);
                }
                else
                {
                    implementationContext.PSActivityEnvironment.Variables["WarningPreference"] = implementationContext.WarningAction;
                }
            }

            if (implementationContext.InformationAction != null)
            {
                if (command != null)
                {
                    command.Parameters.Add("InformationAction", implementationContext.InformationAction);
                }
                else
                {
                    implementationContext.PSActivityEnvironment.Variables["InformationPreference"] = implementationContext.InformationAction;
                }
            }

        }

        static CommandInfo _ci;
        static readonly object Syncroot = new object();
    }

    /// <summary>
    /// Defines the context information available to scripts running within the
    /// InlineScript activity. These are exposed through the $PSActivity automatic
    /// variable.
    /// </summary>
    public class InlineScriptContext
    {
        /// <summary>
        /// Creates a new InlineScriptContext
        /// </summary>
        /// <param name="current">The InlineScript activity being invoked</param>
        public InlineScriptContext(InlineScript current)
        {
            this.current = current;
            this.variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            this.current = null;
        }

        /// <summary>
        /// Gets the current InlineScript activity being invoked.
        /// </summary>
        //public InlineScript Current
        //{
        //    get { return current; }
        //}
        private InlineScript current;

        /// <summary>
        /// Gets the current variables and arguments that are in-scope for
        /// the current activity within its context in the workflow.
        /// </summary>
        public Dictionary<string, object> Variables
        {
            get { return variables; }
        }
        private Dictionary<string, object> variables;
    }

    /// <summary>
    /// Suspends the current workflow.
    /// </summary>
    public class Suspend : NativeActivity
    {
        /// <summary>
        /// Optional field used for resuming the workflow for a specific label.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Returns true if the activity can induce an idle.
        /// </summary>
        protected override bool CanInduceIdle { get { return true; } }

        /// <summary>
        /// Invokes the activity
        /// </summary>
        /// <param name="context">The activity context.</param>
        /// <returns>True if the given argument is set.</returns>
        protected override void Execute(NativeActivityContext context)
        {
            string bookmarkname = string.IsNullOrEmpty(this.Label) ?
                                                PSActivity.PSSuspendBookmarkPrefix :
                                                PSActivity.PSSuspendBookmarkPrefix + this.Label + "_";

            bookmarkname += Guid.NewGuid().ToString().Replace("-", "_");

            context.CreateBookmark(bookmarkname, BookmarkResumed);
        }

        private void BookmarkResumed(NativeActivityContext context, Bookmark bookmark, object value)
        {
        }
    }
}
