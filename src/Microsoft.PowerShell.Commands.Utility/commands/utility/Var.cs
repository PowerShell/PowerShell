// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Base class for all variable commands.
    /// Because -Scope is defined in VariableCommandBase, all derived commands must implement -Scope.
    /// </summary>
    public abstract class VariableCommandBase : PSCmdlet
    {
        #region Parameters

        /// <summary>
        /// Selects active scope to work with; used for all variable commands.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [ArgumentCompleter(typeof(ScopeArgumentCompleter))]
        public string Scope { get; set; }

        #endregion parameters

        /// <summary>
        /// The Include parameter for all the variable commands.
        /// </summary>
        protected string[] IncludeFilters
        {
            get
            {
                return _include;
            }

            set
            {
                value ??= Array.Empty<string>();

                _include = value;
            }
        }

        private string[] _include = Array.Empty<string>();

        /// <summary>
        /// The Exclude parameter for all the variable commands.
        /// </summary>
        protected string[] ExcludeFilters
        {
            get
            {
                return _exclude;
            }

            set
            {
                value ??= Array.Empty<string>();

                _exclude = value;
            }
        }

        private string[] _exclude = Array.Empty<string>();

        #region helpers

        /// <summary>
        /// Gets the matching variable for the specified name, using the
        /// Include, Exclude, and Scope parameters defined in the base class.
        /// </summary>
        /// <param name="name">
        /// The name or pattern of the variables to retrieve.
        /// </param>
        /// <param name="lookupScope">
        /// The scope to do the lookup in. If null or empty the normal scoping rules apply.
        /// </param>
        /// <param name="wasFiltered">
        /// True is returned if a variable exists of the given name but was filtered
        /// out via globbing, include, or exclude.
        /// </param>
        /// <param name="quiet">
        /// If true, don't report errors when trying to access private variables.
        /// </param>
        /// <returns>
        /// A collection of the variables matching the name, include, and exclude
        /// pattern in the specified scope.
        /// </returns>
        internal List<PSVariable> GetMatchingVariables(string name, string lookupScope, out bool wasFiltered, bool quiet)
        {
            wasFiltered = false;

            List<PSVariable> result = new();

            if (string.IsNullOrEmpty(name))
            {
                name = "*";
            }

            bool nameContainsWildcard = WildcardPattern.ContainsWildcardCharacters(name);

            // Now create the filters

            WildcardPattern nameFilter =
                WildcardPattern.Get(
                    name,
                    WildcardOptions.IgnoreCase);

            Collection<WildcardPattern> includeFilters =
                SessionStateUtilities.CreateWildcardsFromStrings(
                    _include,
                    WildcardOptions.IgnoreCase);

            Collection<WildcardPattern> excludeFilters =
                SessionStateUtilities.CreateWildcardsFromStrings(
                    _exclude,
                    WildcardOptions.IgnoreCase);

            if (!nameContainsWildcard)
            {
                // Filter the name here against the include and exclude so that
                // we can report if the name was filtered vs. there being no
                // variable existing of that name.

                bool isIncludeMatch =
                    SessionStateUtilities.MatchesAnyWildcardPattern(
                        name,
                        includeFilters,
                        true);

                bool isExcludeMatch =
                    SessionStateUtilities.MatchesAnyWildcardPattern(
                        name,
                        excludeFilters,
                        false);

                if (!isIncludeMatch || isExcludeMatch)
                {
                    wasFiltered = true;
                    return result;
                }
            }

            // First get the appropriate view of the variables. If no scope
            // is specified, flatten all scopes to produce a currently active
            // view.

            IDictionary<string, PSVariable> variableTable = null;
            if (string.IsNullOrEmpty(lookupScope))
            {
                variableTable = SessionState.Internal.GetVariableTable();
            }
            else
            {
                variableTable = SessionState.Internal.GetVariableTableAtScope(lookupScope);
            }

            CommandOrigin origin = MyInvocation.CommandOrigin;
            foreach (KeyValuePair<string, PSVariable> entry in variableTable)
            {
                bool isNameMatch = nameFilter.IsMatch(entry.Key);
                bool isIncludeMatch =
                    SessionStateUtilities.MatchesAnyWildcardPattern(
                        entry.Key,
                        includeFilters,
                        true);

                bool isExcludeMatch =
                    SessionStateUtilities.MatchesAnyWildcardPattern(
                        entry.Key,
                        excludeFilters,
                        false);

                if (isNameMatch)
                {
                    if (isIncludeMatch && !isExcludeMatch)
                    {
                        // See if the variable is visible
                        if (!SessionState.IsVisible(origin, entry.Value))
                        {
                            // In quiet mode, don't report private variable accesses unless they are specific matches...
                            if (quiet || nameContainsWildcard)
                            {
                                wasFiltered = true;
                                continue;
                            }
                            else
                            {
                                // Generate an error for elements that aren't visible...
                                try
                                {
                                    SessionState.ThrowIfNotVisible(origin, entry.Value);
                                }
                                catch (SessionStateException sessionStateException)
                                {
                                    WriteError(
                                        new ErrorRecord(
                                            sessionStateException.ErrorRecord,
                                            sessionStateException));
                                    // Only report the error once...
                                    wasFiltered = true;
                                    continue;
                                }
                            }
                        }

                        result.Add(entry.Value);
                    }
                    else
                    {
                        wasFiltered = true;
                    }
                }
                else
                {
                    if (nameContainsWildcard)
                    {
                        wasFiltered = true;
                    }
                }
            }

            return result;
        }
        #endregion helpers

    }

    /// <summary>
    /// Implements get-variable command.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Variable", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096711")]
    [OutputType(typeof(PSVariable))]
    public class GetVariableCommand : VariableCommandBase
    {
        #region parameters

        /// <summary>
        /// Name of the PSVariable.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string[] Name
        {
            get
            {
                return _name;
            }

            set
            {
                value ??= new string[] { "*" };

                _name = value;
            }
        }

        private string[] _name = new string[] { "*" };

        /// <summary>
        /// Output only the value(s) of the requested variable(s).
        /// </summary>
        [Parameter]
        public SwitchParameter ValueOnly
        {
            get
            {
                return _valueOnly;
            }

            set
            {
                _valueOnly = value;
            }
        }

        private bool _valueOnly;

        /// <summary>
        /// The Include parameter for all the variable commands.
        /// </summary>
        [Parameter]
        public string[] Include
        {
            get
            {
                return IncludeFilters;
            }

            set
            {
                IncludeFilters = value;
            }
        }

        /// <summary>
        /// The Exclude parameter for all the variable commands.
        /// </summary>
        [Parameter]
        public string[] Exclude
        {
            get
            {
                return ExcludeFilters;
            }

            set
            {
                ExcludeFilters = value;
            }
        }

        #endregion parameters

        /// <summary>
        /// Implements ProcessRecord() method for get-variable's command.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (string varName in _name)
            {
                bool wasFiltered = false;
                List<PSVariable> matchingVariables =
                    GetMatchingVariables(varName, Scope, out wasFiltered, /*quiet*/ false);

                matchingVariables.Sort(
                    static (PSVariable left, PSVariable right) => StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name));

                bool matchFound = false;
                foreach (PSVariable matchingVariable in matchingVariables)
                {
                    matchFound = true;
                    if (_valueOnly)
                    {
                        WriteObject(matchingVariable.Value);
                    }
                    else
                    {
                        WriteObject(matchingVariable);
                    }
                }

                if (!matchFound && !wasFiltered)
                {
                    ItemNotFoundException itemNotFound =
                        new(
                            varName,
                            "VariableNotFound",
                            SessionStateStrings.VariableNotFound);

                    WriteError(
                        new ErrorRecord(
                            itemNotFound.ErrorRecord,
                            itemNotFound));
                }
            }
        }
    }

    /// <summary>
    /// Class implementing new-variable command.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "Variable", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097121")]
    [OutputType(typeof(PSVariable))]
    public sealed class NewVariableCommand : VariableCommandBase
    {
        #region parameters

        /// <summary>
        /// Name of the PSVariable.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true, Mandatory = true)]
        public string Name { get; set; }

        /// <summary>
        /// Value of the PSVariable.
        /// </summary>
        [Parameter(Position = 1, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public object Value { get; set; }

        /// <summary>
        /// Description of the variable.
        /// </summary>
        [Parameter]
        public string Description { get; set; }

        /// <summary>
        /// The options for the variable to specify if the variable should
        /// be ReadOnly, Constant, and/or Private.
        /// </summary>
        [Parameter]
        public ScopedItemOptions Option { get; set; } = ScopedItemOptions.None;

        /// <summary>
        /// Specifies the visibility of the new variable...
        /// </summary>
        [Parameter]
        public SessionStateEntryVisibility Visibility
        {
            get
            {
                return (SessionStateEntryVisibility)_visibility;
            }

            set
            {
                _visibility = value;
            }
        }

        private SessionStateEntryVisibility? _visibility;

        /// <summary>
        /// Force the operation to make the best attempt at setting the variable.
        /// </summary>
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

        private bool _force;

        /// <summary>
        /// The variable object should be passed down the pipeline.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru
        {
            get
            {
                return _passThru;
            }

            set
            {
                _passThru = value;
            }
        }

        private bool _passThru;

        #endregion parameters

        /// <summary>
        /// Add objects received on the pipeline to an ArrayList of values, to
        /// take the place of the Value parameter if none was specified on the
        /// command line.
        /// </summary>
        protected override void ProcessRecord()
        {
            // If Force is not specified, see if the variable already exists
            // in the specified scope. If the scope isn't specified, then
            // check to see if it exists in the current scope.

            if (!Force)
            {
                PSVariable varFound = null;
                if (string.IsNullOrEmpty(Scope))
                {
                    varFound =
                        SessionState.PSVariable.GetAtScope(Name, "local");
                }
                else
                {
                    varFound =
                        SessionState.PSVariable.GetAtScope(Name, Scope);
                }

                if (varFound != null)
                {
                    SessionStateException sessionStateException =
                        new(
                            Name,
                            SessionStateCategory.Variable,
                            "VariableAlreadyExists",
                            SessionStateStrings.VariableAlreadyExists,
                            ErrorCategory.ResourceExists);

                    WriteError(
                        new ErrorRecord(
                            sessionStateException.ErrorRecord,
                            sessionStateException));
                    return;
                }
            }

            // Since the variable doesn't exist or -Force was specified,
            // Call should process to validate the set with the user.

            string action = VariableCommandStrings.NewVariableAction;

            string target = StringUtil.Format(VariableCommandStrings.NewVariableTarget, Name, Value);

            if (ShouldProcess(target, action))
            {
                PSVariable newVariable = new(Name, Value, Option);

                if (_visibility != null)
                {
                    newVariable.Visibility = (SessionStateEntryVisibility)_visibility;
                }

                if (Description != null)
                {
                    newVariable.Description = Description;
                }

                try
                {
                    if (string.IsNullOrEmpty(Scope))
                    {
                        SessionState.Internal.NewVariable(newVariable, Force);
                    }
                    else
                    {
                        SessionState.Internal.NewVariableAtScope(newVariable, Scope, Force);
                    }
                }
                catch (SessionStateException sessionStateException)
                {
                    WriteError(
                        new ErrorRecord(
                            sessionStateException.ErrorRecord,
                            sessionStateException));
                    return;
                }
                catch (PSArgumentException argException)
                {
                    WriteError(
                        new ErrorRecord(
                            argException.ErrorRecord,
                            argException));
                    return;
                }

                if (_passThru)
                {
                    WriteObject(newVariable);
                }
            }
        }
    }

    /// <summary>
    /// This class implements set-variable command.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "Variable", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096624")]
    [OutputType(typeof(PSVariable))]
    public sealed class SetVariableCommand : VariableCommandBase
    {
        #region parameters

        /// <summary>
        /// Name of the PSVariable(s) to set.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true, Mandatory = true)]
        public string[] Name { get; set; }

        /// <summary>
        /// Value of the PSVariable.
        /// </summary>
        [Parameter(Position = 1, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public object Value { get; set; } = AutomationNull.Value;

        /// <summary>
        /// The Include parameter for all the variable commands.
        /// </summary>
        [Parameter]
        public string[] Include
        {
            get
            {
                return IncludeFilters;
            }

            set
            {
                IncludeFilters = value;
            }
        }

        /// <summary>
        /// The Exclude parameter for all the variable commands.
        /// </summary>
        [Parameter]
        public string[] Exclude
        {
            get
            {
                return ExcludeFilters;
            }

            set
            {
                ExcludeFilters = value;
            }
        }

        /// <summary>
        /// Description of the variable.
        /// </summary>
        [Parameter]
        public string Description { get; set; }

        /// <summary>
        /// The options for the variable to specify if the variable should
        /// be ReadOnly, Constant, and/or Private.
        /// </summary>
        [Parameter]
        public ScopedItemOptions Option
        {
            get
            {
                return (ScopedItemOptions)_options;
            }

            set
            {
                _options = value;
            }
        }

        private ScopedItemOptions? _options;

        /// <summary>
        /// Force the operation to make the best attempt at setting the variable.
        /// </summary>
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

        private bool _force;

        /// <summary>
        /// Sets the visibility of the variable...
        /// </summary>
        [Parameter]
        public SessionStateEntryVisibility Visibility
        {
            get
            {
                return (SessionStateEntryVisibility)_visibility;
            }

            set
            {
                _visibility = value;
            }
        }

        private SessionStateEntryVisibility? _visibility;

        /// <summary>
        /// The variable object should be passed down the pipeline.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru
        {
            get
            {
                return _passThru;
            }

            set
            {
                _passThru = value;
            }
        }

        private bool _passThru;

        /// <summary>
        /// Gets whether we will append to the variable if it exists.
        /// </summary>
        [Parameter]
        public SwitchParameter Append { get; set; }

        private bool _nameIsFormalParameter;
        private bool _valueIsFormalParameter;
        #endregion parameters

        /// <summary>
        /// Checks to see if the name and value parameters were bound as formal parameters.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (Name != null && Name.Length > 0)
            {
                _nameIsFormalParameter = true;
            }

            if (Value != AutomationNull.Value)
            {
                _valueIsFormalParameter = true;
            }

            if (Append)
            {
                // create the list here and add to it if it has a value
                // but if they have more than one name, produce an error
                if (Name.Length != 1)
                {
                    ErrorRecord appendVariableError = new ErrorRecord(new InvalidOperationException(), "SetVariableAppend", ErrorCategory.InvalidOperation, Name);
                    appendVariableError.ErrorDetails = new ErrorDetails("SetVariableAppend");
                    appendVariableError.ErrorDetails.RecommendedAction = VariableCommandStrings.UseSingleVariable;
                    ThrowTerminatingError(appendVariableError);
                }

                _valueList = new List<object>();
                var currentValue = Context.SessionState.PSVariable.Get(Name[0]);
                if (currentValue is not null)
                {
                    if (currentValue.Value is IList<object> ilist)
                    {
                        _valueList.AddRange(ilist);
                    }
                    else
                    {
                        _valueList.Add(currentValue.Value);
                    }
                }
            }
        }

        /// <summary>
        /// If name and value are both specified as a formal parameters, then
        /// just ignore the incoming objects in ProcessRecord.
        /// If name is a formal parameter but the value is coming from the pipeline,
        /// then accumulate the values in the valueList and set the variable during
        /// EndProcessing().
        /// If name is not a formal parameter, then set
        /// the variable each time ProcessRecord is called.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (_nameIsFormalParameter && _valueIsFormalParameter)
            {
                if (Append)
                {
                    if (Value != AutomationNull.Value)
                    {
                        _valueList ??= new List<object>();

                        _valueList.Add(Value);
                    }
                }

                return;
            }

            if (_nameIsFormalParameter && !_valueIsFormalParameter)
            {
                if (Value != AutomationNull.Value)
                {
                    _valueList ??= new List<object>();

                    _valueList.Add(Value);
                }
            }
            else
            {
                SetVariable(Name, Value);
            }
        }

        private List<object> _valueList;

        /// <summary>
        /// Sets the variable if the name was specified as a formal parameter
        /// but the value came from the pipeline.
        /// </summary>
        protected override void EndProcessing()
        {
            if (_nameIsFormalParameter)
            {
                if (_valueIsFormalParameter)
                {
                    if (Append)
                    {
                        SetVariable(Name, _valueList);
                    }
                    else
                    {
                        SetVariable(Name, Value);
                    }
                }
                else
                {
                    if (_valueList != null)
                    {
                        if (_valueList.Count == 1)
                        {
                            SetVariable(Name, _valueList[0]);
                        }
                        else if (_valueList.Count == 0)
                        {
                            SetVariable(Name, AutomationNull.Value);
                        }
                        else
                        {
                            SetVariable(Name, _valueList.ToArray());
                        }
                    }
                    else
                    {
                        SetVariable(Name, AutomationNull.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the variables of the given names to the specified value.
        /// </summary>
        /// <param name="varNames">
        /// The name(s) of the variables to set.
        /// </param>
        /// <param name="varValue">
        /// The value to set the variable to.
        /// </param>
        private void SetVariable(string[] varNames, object varValue)
        {
            CommandOrigin origin = MyInvocation.CommandOrigin;

            foreach (string varName in varNames)
            {
                // First look for existing variables to set.

                List<PSVariable> matchingVariables = new();

                bool wasFiltered = false;

                if (!string.IsNullOrEmpty(Scope))
                {
                    // We really only need to find matches if the scope was specified.
                    // If the scope wasn't specified then we need to create the
                    // variable in the local scope.

                    matchingVariables =
                        GetMatchingVariables(varName, Scope, out wasFiltered, /* quiet */ false);
                }
                else
                {
                    // Since the scope wasn't specified, it doesn't matter if there
                    // is a variable in another scope, it only matters if there is a
                    // variable in the local scope.

                    matchingVariables =
                        GetMatchingVariables(
                            varName,
                            System.Management.Automation.StringLiterals.Local,
                            out wasFiltered,
                            false);
                }

                // We only want to create the variable if we are not filtering
                // the name.

                if (matchingVariables.Count == 0 &&
                    !wasFiltered)
                {
                    try
                    {
                        ScopedItemOptions newOptions = ScopedItemOptions.None;

                        if (!string.IsNullOrEmpty(Scope) &&
                            string.Equals("private", Scope, StringComparison.OrdinalIgnoreCase))
                        {
                            newOptions = ScopedItemOptions.Private;
                        }

                        if (_options != null)
                        {
                            newOptions |= (ScopedItemOptions)_options;
                        }

                        object newVarValue = varValue;
                        if (newVarValue == AutomationNull.Value)
                        {
                            newVarValue = null;
                        }

                        PSVariable varToSet =
                            new(
                                varName,
                                newVarValue,
                                newOptions);

                        Description ??= string.Empty;

                        varToSet.Description = Description;

                        // If visibility was specified, set it on the variable
                        if (_visibility != null)
                        {
                            varToSet.Visibility = Visibility;
                        }

                        string action = VariableCommandStrings.SetVariableAction;

                        string target = StringUtil.Format(VariableCommandStrings.SetVariableTarget, varName, newVarValue);

                        if (ShouldProcess(target, action))
                        {
                            object result = null;

                            if (string.IsNullOrEmpty(Scope))
                            {
                                result =
                                    SessionState.Internal.SetVariable(varToSet, Force, origin);
                            }
                            else
                            {
                                result =
                                    SessionState.Internal.SetVariableAtScope(varToSet, Scope, Force, origin);
                            }

                            if (_passThru && result != null)
                            {
                                WriteObject(result);
                            }
                        }
                    }
                    catch (SessionStateException sessionStateException)
                    {
                        WriteError(
                            new ErrorRecord(
                                sessionStateException.ErrorRecord,
                                sessionStateException));
                        continue;
                    }
                    catch (PSArgumentException argException)
                    {
                        WriteError(
                            new ErrorRecord(
                                argException.ErrorRecord,
                                argException));
                        continue;
                    }
                }
                else
                {
                    foreach (PSVariable matchingVariable in matchingVariables)
                    {
                        string action = VariableCommandStrings.SetVariableAction;

                        string target = StringUtil.Format(VariableCommandStrings.SetVariableTarget, matchingVariable.Name, varValue);

                        if (ShouldProcess(target, action))
                        {
                            object result = null;

                            try
                            {
                                // Since the variable existed in the specified scope, or
                                // in the local scope if no scope was specified, use
                                // the reference returned to set the variable properties.

                                // If we want to force setting over a readonly variable
                                // we have to temporarily mark the variable writable.

                                bool wasReadOnly = false;
                                if (Force &&
                                    (matchingVariable.Options & ScopedItemOptions.ReadOnly) != 0)
                                {
                                    matchingVariable.SetOptions(matchingVariable.Options & ~ScopedItemOptions.ReadOnly, true);
                                    wasReadOnly = true;
                                }

                                // Now change the value, options, or description
                                // and set the variable

                                if (varValue != AutomationNull.Value)
                                {
                                    matchingVariable.Value = varValue;

                                    if (Context.LanguageMode == PSLanguageMode.ConstrainedLanguage)
                                    {
                                        // In 'ConstrainedLanguage' we want to monitor untrusted values assigned to 'Global:' variables
                                        // and 'Script:' variables, because they may be set from 'ConstrainedLanguage' environment and
                                        // referenced within trusted script block, and thus result in security issues.
                                        // Here we are setting the value of an existing variable and don't know what scope this variable
                                        // is from, so we mark the value as untrusted, regardless of the scope.
                                        ExecutionContext.MarkObjectAsUntrusted(matchingVariable.Value);
                                    }
                                }

                                if (Description != null)
                                {
                                    matchingVariable.Description = Description;
                                }

                                if (_options != null)
                                {
                                    matchingVariable.Options = (ScopedItemOptions)_options;
                                }
                                else
                                {
                                    if (wasReadOnly)
                                    {
                                        matchingVariable.SetOptions(matchingVariable.Options | ScopedItemOptions.ReadOnly, true);
                                    }
                                }

                                // If visibility was specified, set it on the variable
                                if (_visibility != null)
                                {
                                    matchingVariable.Visibility = Visibility;
                                }

                                result = matchingVariable;
                            }
                            catch (SessionStateException sessionStateException)
                            {
                                WriteError(
                                    new ErrorRecord(
                                        sessionStateException.ErrorRecord,
                                        sessionStateException));
                                continue;
                            }
                            catch (PSArgumentException argException)
                            {
                                WriteError(
                                    new ErrorRecord(
                                        argException.ErrorRecord,
                                        argException));
                                continue;
                            }

                            if (_passThru && result != null)
                            {
                                WriteObject(result);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// The Remove-Variable cmdlet implementation.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "Variable", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097123")]
    public sealed class RemoveVariableCommand : VariableCommandBase
    {
        #region parameters

        /// <summary>
        /// Name of the PSVariable(s) to set.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true, Mandatory = true)]
        public string[] Name { get; set; }

        /// <summary>
        /// The Include parameter for all the variable commands.
        /// </summary>
        [Parameter]
        public string[] Include
        {
            get
            {
                return IncludeFilters;
            }

            set
            {
                IncludeFilters = value;
            }
        }

        /// <summary>
        /// The Exclude parameter for all the variable commands.
        /// </summary>
        [Parameter]
        public string[] Exclude
        {
            get
            {
                return ExcludeFilters;
            }

            set
            {
                ExcludeFilters = value;
            }
        }

        /// <summary>
        /// If true, the variable is removed even if it is ReadOnly.
        /// </summary>
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

        private bool _force;

        #endregion parameters

        /// <summary>
        /// Removes the matching variables from the specified scope.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Removal of variables only happens in the local scope if the
            // scope wasn't explicitly specified by the user.

            Scope ??= "local";

            foreach (string varName in Name)
            {
                // First look for existing variables to set.
                bool wasFiltered = false;

                List<PSVariable> matchingVariables =
                    GetMatchingVariables(varName, Scope, out wasFiltered, /* quiet */ false);

                if (matchingVariables.Count == 0 && !wasFiltered)
                {
                    // Since the variable wasn't found and no glob
                    // characters were specified, write an error.

                    ItemNotFoundException itemNotFound =
                        new(
                            varName,
                            "VariableNotFound",
                            SessionStateStrings.VariableNotFound);

                    WriteError(
                        new ErrorRecord(
                            itemNotFound.ErrorRecord,
                            itemNotFound));

                    continue;
                }

                foreach (PSVariable matchingVariable in matchingVariables)
                {
                    // Since the variable doesn't exist or -Force was specified,
                    // Call should process to validate the set with the user.

                    string action = VariableCommandStrings.RemoveVariableAction;

                    string target = StringUtil.Format(VariableCommandStrings.RemoveVariableTarget, matchingVariable.Name);

                    if (ShouldProcess(target, action))
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(Scope))
                            {
                                SessionState.Internal.RemoveVariable(matchingVariable, _force);
                            }
                            else
                            {
                                SessionState.Internal.RemoveVariableAtScope(matchingVariable, Scope, _force);
                            }
                        }
                        catch (SessionStateException sessionStateException)
                        {
                            WriteError(
                                new ErrorRecord(
                                    sessionStateException.ErrorRecord,
                                    sessionStateException));
                        }
                        catch (PSArgumentException argException)
                        {
                            WriteError(
                                new ErrorRecord(
                                    argException.ErrorRecord,
                                    argException));
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// This class implements set-variable command.
    /// </summary>
    [Cmdlet(VerbsCommon.Clear, "Variable", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096923")]
    [OutputType(typeof(PSVariable))]
    public sealed class ClearVariableCommand : VariableCommandBase
    {
        #region parameters

        /// <summary>
        /// Name of the PSVariable(s) to set.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true, Mandatory = true)]
        public string[] Name { get; set; }

        /// <summary>
        /// The Include parameter for all the variable commands.
        /// </summary>
        [Parameter]
        public string[] Include
        {
            get
            {
                return IncludeFilters;
            }

            set
            {
                IncludeFilters = value;
            }
        }

        /// <summary>
        /// The Exclude parameter for all the variable commands.
        /// </summary>
        [Parameter]
        public string[] Exclude
        {
            get
            {
                return ExcludeFilters;
            }

            set
            {
                ExcludeFilters = value;
            }
        }

        /// <summary>
        /// Force the operation to make the best attempt at clearing the variable.
        /// </summary>
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

        private bool _force;

        /// <summary>
        /// The variable object should be passed down the pipeline.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru
        {
            get
            {
                return _passThru;
            }

            set
            {
                _passThru = value;
            }
        }

        private bool _passThru;

        #endregion parameters

        /// <summary>
        /// The implementation of the Clear-Variable command.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (string varName in Name)
            {
                bool wasFiltered = false;

                List<PSVariable> matchingVariables =
                    GetMatchingVariables(varName, Scope, out wasFiltered, /* quiet */ false);

                if (matchingVariables.Count == 0 && !wasFiltered)
                {
                    // Since the variable wasn't found and no glob
                    // characters were specified, write an error.

                    ItemNotFoundException itemNotFound =
                        new(
                            varName,
                            "VariableNotFound",
                            SessionStateStrings.VariableNotFound);

                    WriteError(
                        new ErrorRecord(
                            itemNotFound.ErrorRecord,
                            itemNotFound));

                    continue;
                }

                foreach (PSVariable matchingVariable in matchingVariables)
                {
                    // Since the variable doesn't exist or -Force was specified,
                    // Call should process to validate the set with the user.

                    string action = VariableCommandStrings.ClearVariableAction;

                    string target = StringUtil.Format(VariableCommandStrings.ClearVariableTarget, matchingVariable.Name);

                    if (ShouldProcess(target, action))
                    {
                        PSVariable result = matchingVariable;

                        try
                        {
                            if (_force &&
                                (matchingVariable.Options & ScopedItemOptions.ReadOnly) != 0)
                            {
                                // Remove the ReadOnly bit to set the value and then reapply

                                matchingVariable.SetOptions(matchingVariable.Options & ~ScopedItemOptions.ReadOnly, true);

                                result = ClearValue(matchingVariable);

                                matchingVariable.SetOptions(matchingVariable.Options | ScopedItemOptions.ReadOnly, true);
                            }
                            else
                            {
                                result = ClearValue(matchingVariable);
                            }
                        }
                        catch (SessionStateException sessionStateException)
                        {
                            WriteError(
                                new ErrorRecord(
                                    sessionStateException.ErrorRecord,
                                    sessionStateException));
                            continue;
                        }
                        catch (PSArgumentException argException)
                        {
                            WriteError(
                                new ErrorRecord(
                                    argException.ErrorRecord,
                                    argException));
                            continue;
                        }

                        if (_passThru)
                        {
                            WriteObject(result);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clears the value of the variable using the PSVariable instance if the scope
        /// was specified or using standard variable lookup if the scope was not specified.
        /// </summary>
        /// <param name="matchingVariable">
        /// The variable that matched the name parameter(s).
        /// </param>
        private PSVariable ClearValue(PSVariable matchingVariable)
        {
            PSVariable result = matchingVariable;
            if (Scope != null)
            {
                matchingVariable.Value = null;
            }
            else
            {
                SessionState.PSVariable.Set(matchingVariable.Name, null);
                result = SessionState.PSVariable.Get(matchingVariable.Name);
            }

            return result;
        }
    }
}
