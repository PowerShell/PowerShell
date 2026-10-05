// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Text;

namespace System.Management.Automation
{
    /// <summary>
    /// This is the interface between the CommandProcessor and the various
    /// parameter binders required to bind parameters to a cmdlet.
    /// </summary>
    internal class CmdletParameterBinderController : ParameterBinderController
    {
        #region tracer

        [TraceSource("ParameterBinderController", "Controls the interaction between the command processor and the parameter binder(s).")]
        private static readonly PSTraceSource s_tracer = PSTraceSource.GetTracer("ParameterBinderController", "Controls the interaction between the command processor and the parameter binder(s).");

        #endregion tracer

        #region ctor

        /// <summary>
        /// Initializes the cmdlet parameter binder controller for
        /// the specified cmdlet and engine context.
        /// </summary>
        /// <param name="cmdlet">
        /// The cmdlet that the parameters will be bound to.
        /// </param>
        /// <param name="commandMetadata">
        /// The metadata about the cmdlet.
        /// </param>
        /// <param name="parameterBinder">
        /// The default parameter binder to use.
        /// </param>
        internal CmdletParameterBinderController(
            Cmdlet cmdlet,
            CommandMetadata commandMetadata,
            ParameterBinderBase parameterBinder)
            : base(
                cmdlet.MyInvocation,
                cmdlet.Context,
                parameterBinder)
        {
            if (cmdlet == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(cmdlet));
            }

            if (commandMetadata == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(commandMetadata));
            }

            this.Command = cmdlet;
            _commandRuntime = (MshCommandRuntime)cmdlet.CommandRuntime;
            _commandMetadata = commandMetadata;

            // Add the static parameter metadata to the bindable parameters
            // And add them to the unbound parameters list

            if (commandMetadata.ImplementsDynamicParameters)
            {
                // ReplaceMetadata makes a copy for us, so we can use that collection as is.
                this.UnboundParameters = this.BindableParameters.ReplaceMetadata(commandMetadata.StaticCommandParameterMetadata);
            }
            else
            {
                _bindableParameters = commandMetadata.StaticCommandParameterMetadata;

                // Must make a copy of the list because we'll modify it.
                this.UnboundParameters = new List<MergedCompiledCommandParameter>(_bindableParameters.BindableParameters.Values);
            }
        }

        #endregion ctor

        #region helper_methods

        /// <summary>
        /// Binds the specified command-line parameters to the target.
        /// </summary>
        /// <param name="arguments">
        /// Parameters to the command.
        /// </param>
        /// <exception cref="ParameterBindingException">
        /// If any parameters fail to bind,
        /// or
        /// If any mandatory parameters are missing.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If there is an error generating the metadata for dynamic parameters.
        /// </exception>
        internal void BindCommandLineParameters(Collection<CommandParameterInternal> arguments)
        {
            s_tracer.WriteLine("Argument count: {0}", arguments.Count);

            BindCommandLineParametersNoValidation(arguments);

            // Is pipeline input expected?
            bool isPipelineInputExpected = !(_commandRuntime.IsClosed && _commandRuntime.InputPipe.Empty);

            int validParameterSetCount;

            if (!isPipelineInputExpected)
            {
                // Since pipeline input is not expected, ensure that we have a single
                // parameter set and that all the mandatory
                // parameters for the working parameter set are specified, or prompt

                validParameterSetCount = ValidateParameterSets(false, true);
            }
            else
            {
                // Use ValidateParameterSets to get the number of valid parameter
                // sets.

                // NTRAID#Windows Out Of Band Releases-2005/11/07-923917-JonN
                validParameterSetCount = ValidateParameterSets(true, false);
            }

            // If the parameter set is determined and the default parameters are not used
            // we try the default parameter binding again because it may contain some mandatory
            // parameters
            if (validParameterSetCount == 1 && !DefaultParameterBindingInUse)
            {
                ApplyDefaultParameterBinding("Mandatory Checking", false);
            }

            // If there are multiple valid parameter sets and we are expecting pipeline inputs,
            // we should filter out those parameter sets that cannot take pipeline inputs anymore.
            if (validParameterSetCount > 1 && isPipelineInputExpected)
            {
                uint filteredValidParameterSetFlags = FilterParameterSetsTakingNoPipelineInput();
                if (filteredValidParameterSetFlags != _currentParameterSetFlag)
                {
                    _currentParameterSetFlag = filteredValidParameterSetFlags;
                    // The valid parameter set flag is narrowed down, we get the new validParameterSetCount
                    validParameterSetCount = ValidateParameterSets(true, false);
                }
            }

            using (ParameterBinderBase.bindingTracer.TraceScope(
                "MANDATORY PARAMETER CHECK on cmdlet [{0}]",
                _commandMetadata.Name))
            {
                try
                {
                    // The missingMandatoryParameters out parameter is used for error reporting when binding from the pipeline.
                    // We're not binding from the pipeline here, and if a mandatory non-pipeline parameter is missing, it will
                    // be prompted for, or an exception will be raised, so we can ignore the missingMandatoryParameters out parameter.
                    Collection<MergedCompiledCommandParameter> missingMandatoryParameters;

                    // We shouldn't prompt for mandatory parameters if this command is private.
                    bool promptForMandatoryParameters = (Command.CommandInfo.Visibility == SessionStateEntryVisibility.Public);
                    HandleUnboundMandatoryParameters(validParameterSetCount, true, promptForMandatoryParameters, isPipelineInputExpected, out missingMandatoryParameters);

                    if (DefaultParameterBinder is ScriptParameterBinder)
                    {
                        BindUnboundScriptParameters();
                    }
                }
                catch (ParameterBindingException pbex)
                {
                    if (!DefaultParameterBindingInUse)
                    {
                        throw;
                    }

                    ThrowElaboratedBindingException(pbex);
                }
            }

            // If there is no more expected input, ensure there is a single
            // parameter set selected

            if (!isPipelineInputExpected)
            {
                VerifyParameterSetSelected();
            }

            // Set the prepipeline parameter set flags so that they can be restored
            // between each pipeline object.

            _prePipelineProcessingParameterSetFlags = _currentParameterSetFlag;
        }

        /// <summary>
        /// Binds the unbound arguments to parameters but does not
        /// perform mandatory parameter validation or parameter set validation.
        /// </summary>
        internal void BindCommandLineParametersNoValidation(Collection<CommandParameterInternal> arguments)
        {
            var psCompiledScriptCmdlet = this.Command as PSScriptCmdlet;
            psCompiledScriptCmdlet?.PrepareForBinding(this);

            InitUnboundArguments(arguments);
            CommandMetadata cmdletMetadata = _commandMetadata;
            // Clear the warningSet at the beginning.
            _warningSet.Clear();
            // Parse $PSDefaultParameterValues to get all valid <parameter, value> pairs
            _allDefaultParameterValuePairs = this.GetDefaultParameterValuePairs(true);
            // Set to false at the beginning
            DefaultParameterBindingInUse = false;
            // Clear the bound default parameters at the beginning
            BoundDefaultParameters.Clear();

            // Reparse the arguments based on the merged metadata
            ReparseUnboundArguments();

            using (ParameterBinderBase.bindingTracer.TraceScope(
                "BIND NAMED cmd line args [{0}]",
                _commandMetadata.Name))
            {
                // Bind the actual arguments
                UnboundArguments = BindNamedParameters(_currentParameterSetFlag, this.UnboundArguments);
            }

            ParameterBindingException reportedBindingException;
            ParameterBindingException currentBindingException;

            using (ParameterBinderBase.bindingTracer.TraceScope(
                "BIND POSITIONAL cmd line args [{0}]",
                _commandMetadata.Name))
            {
                // Now that we know the parameter set, bind the positional parameters
                UnboundArguments =
                    BindPositionalParameters(
                        UnboundArguments,
                        _currentParameterSetFlag,
                        cmdletMetadata.DefaultParameterSetFlag,
                        out currentBindingException);

                reportedBindingException = currentBindingException;
            }

            // Try applying the default parameter binding after POSITIONAL BIND so that the default parameter
            // values can influence the parameter set selection earlier than the default parameter set.
            ApplyDefaultParameterBinding("POSITIONAL BIND", false);

            // We need to make sure there is at least one valid parameter set. Its
            // OK to allow more than one as long as one of them takes pipeline input.

            // NTRAID#Windows Out Of Band Releases-2006/02/14-928660-JonN
            // Pipeline input fails to bind to pipeline enabled parameter
            // second parameter changed from true to false
            ValidateParameterSets(true, false);

            // Always get the dynamic parameters as there may be mandatory parameters there

            // Now try binding the dynamic parameters
            HandleCommandLineDynamicParameters(out currentBindingException);

            // Try binding the default parameters again. After dynamic binding, new parameter metadata are
            // included, so it's possible a previously unsuccessful binding will succeed.
            ApplyDefaultParameterBinding("DYNAMIC BIND", true);

            // If this generated an exception (but we didn't have one from the non-dynamic
            // parameters, report on this one.
            reportedBindingException ??= currentBindingException;

            // If the cmdlet implements a ValueFromRemainingArguments parameter (VarArgs)
            // bind the unbound arguments to that parameter.
            HandleRemainingArguments();

            VerifyArgumentsProcessed(reportedBindingException);
        }

        /// <summary>
        /// Process all valid parameter sets, and filter out those that don't take any pipeline input.
        /// </summary>
        /// <returns>
        /// The new valid parameter set flags
        /// </returns>
        private uint FilterParameterSetsTakingNoPipelineInput()
        {
            uint parameterSetsTakingPipeInput = 0;
            bool findPipeParameterInAllSets = false;

            foreach (KeyValuePair<MergedCompiledCommandParameter, DelayedScriptBlockArgument> entry in _delayBindScriptBlocks)
            {
                parameterSetsTakingPipeInput |= entry.Key.Parameter.ParameterSetFlags;
            }

            foreach (MergedCompiledCommandParameter parameter in UnboundParameters)
            {
                // If a parameter doesn't take pipeline input at all, we can skip it
                if (!parameter.Parameter.IsPipelineParameterInSomeParameterSet)
                {
                    continue;
                }

                var matchingParameterSetMetadata =
                    parameter.Parameter.GetMatchingParameterSetData(_currentParameterSetFlag);

                foreach (ParameterSetSpecificMetadata parameterSetMetadata in matchingParameterSetMetadata)
                {
                    if (parameterSetMetadata.ValueFromPipeline || parameterSetMetadata.ValueFromPipelineByPropertyName)
                    {
                        if (parameterSetMetadata.ParameterSetFlag == 0 && parameterSetMetadata.IsInAllSets)
                        {
                            // The parameter takes pipeline input and is in all sets, we don't change the _currentParameterSetFlag
                            parameterSetsTakingPipeInput = 0;
                            findPipeParameterInAllSets = true;
                            break;
                        }
                        else
                        {
                            parameterSetsTakingPipeInput |= parameterSetMetadata.ParameterSetFlag;
                        }
                    }
                }

                if (findPipeParameterInAllSets)
                    break;
            }

            // If parameterSetsTakingPipeInput is 0, then no parameter set from the _currentParameterSetFlag can take piped objects.
            // Then we just leave what it was, and the pipeline binding deal with the error later
            if (parameterSetsTakingPipeInput != 0)
                return _currentParameterSetFlag & parameterSetsTakingPipeInput;
            else
                return _currentParameterSetFlag;
        }

        /// <summary>
        /// Apply the binding for the default parameter defined by the user.
        /// </summary>
        /// <param name="bindingStage">
        /// Dictate which binding stage this default binding happens
        /// </param>
        /// <param name="isDynamic">
        /// Special operation needed if the default binding happens at the dynamic binding stage
        /// </param>
        /// <returns></returns>
        private void ApplyDefaultParameterBinding(string bindingStage, bool isDynamic)
        {
            if (!_useDefaultParameterBinding)
            {
                return;
            }

            if (isDynamic)
            {
                // Get user defined default parameter value pairs again, so that the
                // dynamic parameter value pairs could be involved.
                _allDefaultParameterValuePairs = GetDefaultParameterValuePairs(false);
            }

            Dictionary<MergedCompiledCommandParameter, object> qualifiedParameterValuePairs = GetQualifiedParameterValuePairs(_currentParameterSetFlag, _allDefaultParameterValuePairs);
            if (qualifiedParameterValuePairs != null)
            {
                bool isSuccess = false;
                using (ParameterBinderBase.bindingTracer.TraceScope(
                    "BIND DEFAULT <parameter, value> pairs after [{0}] for [{1}]",
                    bindingStage, _commandMetadata.Name))
                {
                    isSuccess = BindDefaultParameters(_currentParameterSetFlag, qualifiedParameterValuePairs);
                    if (isSuccess && !DefaultParameterBindingInUse)
                    {
                        DefaultParameterBindingInUse = true;
                    }
                }

                s_tracer.WriteLine("BIND DEFAULT after [{0}] result [{1}]", bindingStage, isSuccess);
            }

            return;
        }

        /// <summary>
        /// Bind the default parameter value pairs.
        /// </summary>
        /// <param name="validParameterSetFlag">ValidParameterSetFlag.</param>
        /// <param name="defaultParameterValues">Default value pairs.</param>
        /// <returns>
        /// true if there is at least one default parameter bound successfully
        /// false if there is no default parameter bound successfully
        /// </returns>
        private bool BindDefaultParameters(uint validParameterSetFlag, Dictionary<MergedCompiledCommandParameter, object> defaultParameterValues)
        {
            bool ret = false;
            foreach (var pair in defaultParameterValues)
            {
                MergedCompiledCommandParameter parameter = pair.Key;
                object argumentValue = pair.Value;
                string parameterName = parameter.Parameter.Name;

                try
                {
                    ScriptBlock scriptBlockArg = argumentValue as ScriptBlock;
                    if (scriptBlockArg != null)
                    {
                        // Get the current binding state, and pass it to the ScriptBlock as the argument
                        // The 'arg' includes HashSet properties 'BoundParameters', 'BoundPositionalParameters',
                        // 'BoundDefaultParameters', and 'LastBindingStage'. So the user can set value
                        // to a parameter depending on the current binding state.
                        PSObject arg = WrapBindingState();
                        Collection<PSObject> results = scriptBlockArg.Invoke(arg);
                        if (results == null || results.Count == 0)
                        {
                            continue;
                        }
                        else if (results.Count == 1)
                        {
                            argumentValue = results[0];
                        }
                        else
                        {
                            argumentValue = results;
                        }
                    }

                    CommandParameterInternal bindableArgument =
                        CommandParameterInternal.CreateParameterWithArgument(
                           /*parameterAst*/null, parameterName, "-" + parameterName + ":",
                           /*argumentAst*/null, argumentValue, false);

                    bool bindResult =
                            BindParameter(
                                validParameterSetFlag,
                                bindableArgument,
                                parameter,
                                ParameterBindingFlags.ShouldCoerceType | ParameterBindingFlags.DelayBindScriptBlock);

                    if (bindResult && !ret)
                    {
                        ret = true;
                    }

                    if (bindResult)
                    {
                        BoundDefaultParameters.Add(parameterName);
                    }
                }
                catch (ParameterBindingException ex)
                {
                    // We don't want the failures in default binding affect the command line binding,
                    // so we write out a warning and ignore this binding failure
                    if (!_warningSet.Contains(_commandMetadata.Name + Separator + parameterName))
                    {
                        string message = string.Format(CultureInfo.InvariantCulture,
                            ParameterBinderStrings.FailToBindDefaultParameter,
                            LanguagePrimitives.IsNull(argumentValue) ? "null" : argumentValue.ToString(),
                            parameterName, ex.Message);
                        _commandRuntime.WriteWarning(message);
                        _warningSet.Add(_commandMetadata.Name + Separator + parameterName);
                    }

                    continue;
                }
            }

            return ret;
        }

        /// <summary>
        /// Wrap up current binding state to provide more information to the user.
        /// </summary>
        /// <returns></returns>
        private PSObject WrapBindingState()
        {
            HashSet<string> boundParameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> boundPositionalParameterNames =
                this.DefaultParameterBinder.CommandLineParameters.CopyBoundPositionalParameters();
            HashSet<string> boundDefaultParameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string paramName in BoundParameters.Keys)
            {
                boundParameterNames.Add(paramName);
            }

            foreach (string paramName in BoundDefaultParameters)
            {
                boundDefaultParameterNames.Add(paramName);
            }

            PSObject result = new PSObject();
            result.Properties.Add(new PSNoteProperty("BoundParameters", boundParameterNames));
            result.Properties.Add(new PSNoteProperty("BoundPositionalParameters", boundPositionalParameterNames));
            result.Properties.Add(new PSNoteProperty("BoundDefaultParameters", boundDefaultParameterNames));

            return result;
        }

        /// <summary>
        /// Get all qualified default parameter value pairs based on the
        /// given currentParameterSetFlag.
        /// </summary>
        /// <param name="currentParameterSetFlag"></param>
        /// <param name="availableParameterValuePairs"></param>
        /// <returns>Null if no qualified pair found.</returns>
        private Dictionary<MergedCompiledCommandParameter, object> GetQualifiedParameterValuePairs(
            uint currentParameterSetFlag,
            Dictionary<MergedCompiledCommandParameter, object> availableParameterValuePairs)
        {
            if (availableParameterValuePairs == null)
            {
                return null;
            }

            Dictionary<MergedCompiledCommandParameter, object> result = new Dictionary<MergedCompiledCommandParameter, object>();

            uint possibleParameterFlag = uint.MaxValue;
            foreach (var pair in availableParameterValuePairs)
            {
                MergedCompiledCommandParameter param = pair.Key;
                if ((param.Parameter.ParameterSetFlags & currentParameterSetFlag) == 0 && !param.Parameter.IsInAllSets)
                {
                    continue;
                }

                if (BoundArguments.ContainsKey(param.Parameter.Name))
                {
                    continue;
                }

                // check if this param's set conflicts with other possible params.
                if (param.Parameter.ParameterSetFlags != 0)
                {
                    possibleParameterFlag &= param.Parameter.ParameterSetFlags;
                    if (possibleParameterFlag == 0)
                    {
                        return null;
                    }
                }

                result.Add(param, pair.Value);
            }

            if (result.Count > 0)
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Get the aliases of the current cmdlet.
        /// </summary>
        /// <returns></returns>
        private List<string> GetAliasOfCurrentCmdlet()
        {
            var results = Context.SessionState.Internal.GetAliasesByCommandName(_commandMetadata.Name).ToList();

            return results.Count > 0 ? results : null;
        }

        /// <summary>
        /// Check if the passed-in aliasName matches an alias name in _aliasList.
        /// </summary>
        /// <param name="aliasName"></param>
        /// <returns></returns>
        private bool MatchAnyAlias(string aliasName)
        {
            if (_aliasList == null)
            {
                return false;
            }

            bool result = false;
            WildcardPattern aliasPattern = WildcardPattern.Get(aliasName, WildcardOptions.IgnoreCase);
            foreach (string alias in _aliasList)
            {
                if (aliasPattern.IsMatch(alias))
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        internal IDictionary DefaultParameterValues { get; set; }
        /// <summary>
        /// Get all available default parameter value pairs.
        /// </summary>
        /// <returns>Return the available parameter value pairs. Otherwise return null.</returns>
        private Dictionary<MergedCompiledCommandParameter, object> GetDefaultParameterValuePairs(bool needToGetAlias)
        {
            if (DefaultParameterValues == null)
            {
                _useDefaultParameterBinding = false;
                return null;
            }

            var availablePairs = new Dictionary<MergedCompiledCommandParameter, object>();

            if (needToGetAlias && DefaultParameterValues.Count > 0)
            {
                // Get all aliases of the current cmdlet
                _aliasList = GetAliasOfCurrentCmdlet();
            }

            // Set flag to true by default
            _useDefaultParameterBinding = true;

            string currentCmdletName = _commandMetadata.Name;

            IDictionary<string, MergedCompiledCommandParameter> bindableParameters = BindableParameters.BindableParameters;
            IDictionary<string, MergedCompiledCommandParameter> bindableAlias = BindableParameters.AliasedParameters;

            // Contains parameters that are set with different values by settings in $PSDefaultParameterValues.
            // We should ignore those settings and write out a warning
            var parametersToRemove = new HashSet<MergedCompiledCommandParameter>();
            var wildcardDefault = new Dictionary<string, object>();
            // Contains keys that are in bad format. For every bad format key, we should write out a warning message
            // the first time we encounter it, and remove it from the $PSDefaultParameterValues
            var keysToRemove = new List<object>();

            foreach (DictionaryEntry entry in DefaultParameterValues)
            {
                if (entry.Key is not string key)
                {
                    continue;
                }

                key = key.Trim();
                string cmdletName = null;
                string parameterName = null;

                // The key is not in valid format
                if (!DefaultParameterDictionary.CheckKeyIsValid(key, ref cmdletName, ref parameterName))
                {
                    if (key.Equals("Disabled", StringComparison.OrdinalIgnoreCase) &&
                        LanguagePrimitives.IsTrue(entry.Value))
                    {
                        _useDefaultParameterBinding = false;
                        return null;
                    }
                    // Write out a warning message if the key is not 'Disabled'
                    if (!key.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
                    {
                        keysToRemove.Add(entry.Key);
                    }

                    continue;
                }

                Diagnostics.Assert(cmdletName != null && parameterName != null, "The cmdletName and parameterName should be set in CheckKeyIsValid");

                if (WildcardPattern.ContainsWildcardCharacters(key))
                {
                    wildcardDefault.Add(cmdletName + Separator + parameterName, entry.Value);
                    continue;
                }

                // Continue to process this entry only if the specified cmdletName is the name
                // of the current cmdlet, or is an alias name of the current cmdlet.
                if (!cmdletName.Equals(currentCmdletName, StringComparison.OrdinalIgnoreCase) && !MatchAnyAlias(cmdletName))
                {
                    continue;
                }

                GetDefaultParameterValuePairsHelper(
                    cmdletName, parameterName, entry.Value,
                    bindableParameters, bindableAlias,
                    availablePairs, parametersToRemove);
            }

            foreach (KeyValuePair<string, object> wildcard in wildcardDefault)
            {
                string key = wildcard.Key;

                string cmdletName = key.Substring(0, key.IndexOf(Separator, StringComparison.OrdinalIgnoreCase));
                string parameterName = key.Substring(key.IndexOf(Separator, StringComparison.OrdinalIgnoreCase) + Separator.Length);

                WildcardPattern cmdletPattern = WildcardPattern.Get(cmdletName, WildcardOptions.IgnoreCase);
                // Continue to process this entry only if the cmdletName matches the name of the current
                // cmdlet, or matches an alias name of the current cmdlet
                if (!cmdletPattern.IsMatch(currentCmdletName) && !MatchAnyAlias(cmdletName))
                {
                    continue;
                }

                if (!WildcardPattern.ContainsWildcardCharacters(parameterName))
                {
                    GetDefaultParameterValuePairsHelper(
                        cmdletName, parameterName, wildcard.Value,
                        bindableParameters, bindableAlias,
                        availablePairs, parametersToRemove);

                    continue;
                }

                WildcardPattern parameterPattern = MemberMatch.GetNamePattern(parameterName);
                var matches = new List<MergedCompiledCommandParameter>();

                foreach (KeyValuePair<string, MergedCompiledCommandParameter> entry in bindableParameters)
                {
                    if (parameterPattern.IsMatch(entry.Key))
                    {
                        matches.Add(entry.Value);
                    }
                }

                foreach (KeyValuePair<string, MergedCompiledCommandParameter> entry in bindableAlias)
                {
                    if (parameterPattern.IsMatch(entry.Key))
                    {
                        matches.Add(entry.Value);
                    }
                }

                if (matches.Count > 1)
                {
                    // The parameterPattern matches more than one parameters, so we write out a warning message and ignore this setting
                    if (!_warningSet.Contains(cmdletName + Separator + parameterName))
                    {
                        _commandRuntime.WriteWarning(
                            string.Format(CultureInfo.InvariantCulture, ParameterBinderStrings.MultipleParametersMatched, parameterName));
                        _warningSet.Add(cmdletName + Separator + parameterName);
                    }

                    continue;
                }

                if (matches.Count == 1)
                {
                    if (!availablePairs.ContainsKey(matches[0]))
                    {
                        availablePairs.Add(matches[0], wildcard.Value);
                        continue;
                    }

                    if (!wildcard.Value.Equals(availablePairs[matches[0]]))
                    {
                        if (!_warningSet.Contains(cmdletName + Separator + parameterName))
                        {
                            _commandRuntime.WriteWarning(
                                string.Format(CultureInfo.InvariantCulture, ParameterBinderStrings.DifferentValuesAssignedToSingleParameter, parameterName));
                            _warningSet.Add(cmdletName + Separator + parameterName);
                        }

                        parametersToRemove.Add(matches[0]);
                    }
                }
            }

            if (keysToRemove.Count > 0)
            {
                var keysInError = new StringBuilder();
                foreach (object badFormatKey in keysToRemove)
                {
                    if (DefaultParameterValues.Contains(badFormatKey))
                        DefaultParameterValues.Remove(badFormatKey);

                    keysInError.Append(badFormatKey.ToString() + ", ");
                }

                keysInError.Remove(keysInError.Length - 2, 2);
                var multipleKeys = keysToRemove.Count > 1;
                string formatString = multipleKeys
                                            ? ParameterBinderStrings.MultipleKeysInBadFormat
                                            : ParameterBinderStrings.SingleKeyInBadFormat;
                _commandRuntime.WriteWarning(
                    string.Format(CultureInfo.InvariantCulture, formatString, keysInError));
            }

            foreach (MergedCompiledCommandParameter param in parametersToRemove)
            {
                availablePairs.Remove(param);
            }

            if (availablePairs.Count > 0)
            {
                return availablePairs;
            }

            return null;
        }

        /// <summary>
        /// A helper method for GetDefaultParameterValuePairs.
        /// </summary>
        /// <param name="cmdletName"></param>
        /// <param name="paramName"></param>
        /// <param name="paramValue"></param>
        /// <param name="bindableParameters"></param>
        /// <param name="bindableAlias"></param>
        /// <param name="result"></param>
        /// <param name="parametersToRemove"></param>
        private void GetDefaultParameterValuePairsHelper(
            string cmdletName, string paramName, object paramValue,
            IDictionary<string, MergedCompiledCommandParameter> bindableParameters,
            IDictionary<string, MergedCompiledCommandParameter> bindableAlias,
            Dictionary<MergedCompiledCommandParameter, object> result,
            HashSet<MergedCompiledCommandParameter> parametersToRemove)
        {
            // No exception should be thrown if we cannot find a match for the 'paramName',
            // because the 'paramName' could be a dynamic parameter name, and this dynamic parameter
            // hasn't been introduced at the current stage.
            bool writeWarning = false;
            MergedCompiledCommandParameter matchParameter;
            object resultObject;
            if (bindableParameters.TryGetValue(paramName, out matchParameter))
            {
                if (!result.TryGetValue(matchParameter, out resultObject))
                {
                    result.Add(matchParameter, paramValue);
                    return;
                }

                if (!paramValue.Equals(resultObject))
                {
                    writeWarning = true;
                    parametersToRemove.Add(matchParameter);
                }
            }
            else
            {
                if (bindableAlias.TryGetValue(paramName, out matchParameter))
                {
                    if (!result.TryGetValue(matchParameter, out resultObject))
                    {
                        result.Add(matchParameter, paramValue);
                        return;
                    }

                    if (!paramValue.Equals(resultObject))
                    {
                        writeWarning = true;
                        parametersToRemove.Add(matchParameter);
                    }
                }
            }

            if (writeWarning && !_warningSet.Contains(cmdletName + Separator + paramName))
            {
                _commandRuntime.WriteWarning(
                    string.Format(CultureInfo.InvariantCulture, ParameterBinderStrings.DifferentValuesAssignedToSingleParameter, paramName));
                _warningSet.Add(cmdletName + Separator + paramName);
            }
        }

        /// <summary>
        /// Verify if all arguments from the command line are bound.
        /// </summary>
        /// <param name="originalBindingException">
        /// Previous binding exceptions that possibly causes the failure
        /// </param>
        private void VerifyArgumentsProcessed(ParameterBindingException originalBindingException)
        {
            // Now verify that all the arguments that were passed in were processed.

            if (UnboundArguments.Count > 0)
            {
                ParameterBindingException bindingException;
                CommandParameterInternal parameter = UnboundArguments[0];

                // Get the argument type that was specified

                Type specifiedType = null;
                object argumentValue = parameter.ArgumentValue;
                if (argumentValue != null && argumentValue != UnboundParameter.Value)
                {
                    specifiedType = argumentValue.GetType();
                }

                if (parameter.ParameterNameSpecified)
                {
                    bindingException =
                        new ParameterBindingException(
                            ErrorCategory.InvalidArgument,
                            this.Command.MyInvocation,
                            GetParameterErrorExtent(parameter),
                            parameter.ParameterName,
                            null,
                            specifiedType,
                            ParameterBinderStrings.NamedParameterNotFound,
                            "NamedParameterNotFound");
                }
                else
                {
                    // If this was a positional parameter, and we have the original exception,
                    // report on the original error
                    if (originalBindingException != null)
                    {
                        bindingException = originalBindingException;
                    }
                    // Otherwise, give a generic error.
                    else
                    {
                        string argument = StringLiterals.DollarNull;
                        if (parameter.ArgumentValue != null)
                        {
                            try
                            {
                                argument = parameter.ArgumentValue.ToString();
                            }
                            catch (Exception e)
                            {
                                bindingException =
                                    new ParameterBindingArgumentTransformationException(
                                        e,
                                        ErrorCategory.InvalidData,
                                        this.InvocationInfo,
                                        null,
                                        null,
                                        null,
                                        parameter.ArgumentValue.GetType(),
                                        ParameterBinderStrings.ParameterArgumentTransformationErrorMessageOnly,
                                        "ParameterArgumentTransformationErrorMessageOnly",
                                        e.Message);

                                if (!DefaultParameterBindingInUse)
                                {
                                    throw bindingException;
                                }
                                else
                                {
                                    ThrowElaboratedBindingException(bindingException);
                                }
                            }
                        }

                        bindingException =
                            new ParameterBindingException(
                                ErrorCategory.InvalidArgument,
                                this.Command.MyInvocation,
                                null,
                                argument,
                                null,
                                specifiedType,
                                ParameterBinderStrings.PositionalParameterNotFound,
                                "PositionalParameterNotFound");
                    }
                }

                if (!DefaultParameterBindingInUse)
                {
                    throw bindingException;
                }
                else
                {
                    ThrowElaboratedBindingException(bindingException);
                }
            }
        }

        /// <summary>
        /// Verifies that a single parameter set is selected and throws an exception if
        /// one of there are multiple and one of them is not the default parameter set.
        /// </summary>
        private void VerifyParameterSetSelected()
        {
            // Now verify that a parameter set has been selected if any parameter sets
            // were defined.

            if (this.BindableParameters.ParameterSetCount > 1)
            {
                if (_currentParameterSetFlag == uint.MaxValue)
                {
                    if ((_currentParameterSetFlag &
                         _commandMetadata.DefaultParameterSetFlag) != 0 &&
                         _commandMetadata.DefaultParameterSetFlag != uint.MaxValue)
                    {
                        ParameterBinderBase.bindingTracer.WriteLine(
                            "{0} valid parameter sets, using the DEFAULT PARAMETER SET: [{0}]",
                            this.BindableParameters.ParameterSetCount.ToString(),
                            _commandMetadata.DefaultParameterSetName);

                        _currentParameterSetFlag =
                            _commandMetadata.DefaultParameterSetFlag;
                    }
                    else
                    {
                        ParameterBinderBase.bindingTracer.TraceError(
                            "ERROR: {0} valid parameter sets, but NOT DEFAULT PARAMETER SET.",
                            this.BindableParameters.ParameterSetCount);

                        // Throw an exception for ambiguous parameter set
                        ThrowAmbiguousParameterSetException(_currentParameterSetFlag, BindableParameters);
                    }
                }
            }
        }

        /// <summary>
        /// Restores the specified parameter to the original value.
        /// </summary>
        /// <param name="argumentToBind">
        /// The argument containing the value to restore.
        /// </param>
        /// <param name="parameter">
        /// The metadata for the parameter to restore.
        /// </param>
        /// <returns>
        /// True if the parameter was restored correctly, or false otherwise.
        /// </returns>
        private bool RestoreParameter(CommandParameterInternal argumentToBind, MergedCompiledCommandParameter parameter)
        {
            switch (parameter.BinderAssociation)
            {
                case ParameterBinderAssociation.DeclaredFormalParameters:
                    DefaultParameterBinder.BindParameter(argumentToBind.ParameterName, argumentToBind.ArgumentValue, parameter.Parameter);
                    break;

                case ParameterBinderAssociation.CommonParameters:
                    CommonParametersBinder.BindParameter(argumentToBind.ParameterName, argumentToBind.ArgumentValue, parameter.Parameter);
                    break;

                case ParameterBinderAssociation.ShouldProcessParameters:
                    Diagnostics.Assert(
                        _commandMetadata.SupportsShouldProcess,
                        "The metadata for the ShouldProcessParameters should only be available if the command supports ShouldProcess");

                    ShouldProcessParametersBinder.BindParameter(argumentToBind.ParameterName, argumentToBind.ArgumentValue, parameter.Parameter);
                    break;

                case ParameterBinderAssociation.PagingParameters:
                    Diagnostics.Assert(
                        _commandMetadata.SupportsPaging,
                        "The metadata for the PagingParameters should only be available if the command supports paging");

                    PagingParametersBinder.BindParameter(argumentToBind.ParameterName, argumentToBind.ArgumentValue, parameter.Parameter);
                    break;

                case ParameterBinderAssociation.TransactionParameters:
                    Diagnostics.Assert(
                        _commandMetadata.SupportsTransactions,
                        "The metadata for the TransactionParameters should only be available if the command supports Transactions");

                    TransactionParametersBinder.BindParameter(argumentToBind.ParameterName, argumentToBind.ArgumentValue, parameter.Parameter);
                    break;

                case ParameterBinderAssociation.DynamicParameters:
                    Diagnostics.Assert(
                        _commandMetadata.ImplementsDynamicParameters,
                        "The metadata for the dynamic parameters should only be available if the command supports IDynamicParameters");

                    _dynamicParameterBinder?.BindParameter(argumentToBind.ParameterName, argumentToBind.ArgumentValue, parameter.Parameter);

                    break;
            }

            return true;
        }

        /// <summary>
        /// Validate the given named parameter against the specified parameter set,
        /// and then bind the argument to the parameter.
        /// </summary>
        protected override void BindNamedParameter(
            uint parameterSets,
            CommandParameterInternal argument,
            MergedCompiledCommandParameter parameter)
        {
            if ((parameter.Parameter.ParameterSetFlags & parameterSets) == 0 &&
                !parameter.Parameter.IsInAllSets)
            {
                string parameterSetName = BindableParameters.GetParameterSetName(parameterSets);

                ParameterBindingException bindingException =
                    new ParameterBindingException(
                        ErrorCategory.InvalidArgument,
                        this.Command.MyInvocation,
                        errorPosition: null,
                        argument.ParameterName,
                        parameterType: null,
                        typeSpecified: null,
                        ParameterBinderStrings.ParameterNotInParameterSet,
                        "ParameterNotInParameterSet",
                        parameterSetName);

                // Might be caused by default parameter binding
                if (!DefaultParameterBindingInUse)
                {
                    throw bindingException;
                }
                else
                {
                    ThrowElaboratedBindingException(bindingException);
                }
            }

            try
            {
                BindParameter(parameterSets, argument, parameter,
                    ParameterBindingFlags.ShouldCoerceType | ParameterBindingFlags.DelayBindScriptBlock);
            }
            catch (ParameterBindingException pbex)
            {
                if (!DefaultParameterBindingInUse)
                {
                    throw;
                }

                ThrowElaboratedBindingException(pbex);
            }
        }

        /// <summary>
        /// Determines if a ScriptBlock can be bound directly to the type of the specified parameter.
        /// </summary>
        /// <param name="parameter">
        /// The metadata of the parameter to check the type of.
        /// </param>
        /// <returns>
        /// true if the parameter type is Object, ScriptBlock, derived from ScriptBlock, a
        /// collection of ScriptBlocks, a collection of Objects, or a collection of types derived from
        /// ScriptBlock.
        /// False otherwise.
        /// </returns>
        private static bool IsParameterScriptBlockBindable(MergedCompiledCommandParameter parameter)
        {
            bool result = false;

            Type parameterType = parameter.Parameter.Type;

            do // false loop
            {
                if (parameterType == typeof(object))
                {
                    result = true;
                    break;
                }

                if (parameterType == typeof(ScriptBlock))
                {
                    result = true;
                    break;
                }

                if (parameterType.IsSubclassOf(typeof(ScriptBlock)))
                {
                    result = true;
                    break;
                }

                ParameterCollectionTypeInformation parameterCollectionTypeInfo = parameter.Parameter.CollectionTypeInformation;
                if (parameterCollectionTypeInfo.ParameterCollectionType != ParameterCollectionType.NotCollection)
                {
                    if (parameterCollectionTypeInfo.ElementType == typeof(object))
                    {
                        result = true;
                        break;
                    }

                    if (parameterCollectionTypeInfo.ElementType == typeof(ScriptBlock))
                    {
                        result = true;
                        break;
                    }

                    if (parameterCollectionTypeInfo.ElementType.IsSubclassOf(typeof(ScriptBlock)))
                    {
                        result = true;
                        break;
                    }
                }
            } while (false);

            s_tracer.WriteLine("IsParameterScriptBlockBindable: result = {0}", result);
            return result;
        }

        /// <summary>
        /// Binds the specified argument to the specified parameter using the appropriate
        /// parameter binder. If the argument is of type ScriptBlock and the parameter takes
        /// pipeline input, then the ScriptBlock is saved off in the delay-bind ScriptBlock
        /// container for further processing of pipeline input and is not bound as the argument
        /// to the parameter.
        /// </summary>
        /// <param name="parameterSets">
        /// The parameter set used to bind the arguments.
        /// </param>
        /// <param name="argument">
        /// The argument to be bound.
        /// </param>
        /// <param name="parameter">
        /// The metadata for the parameter to bind the argument to.
        /// </param>
        /// <param name="flags">
        /// Flags for type coercion, validation, and script block binding.
        ///
        /// ParameterBindingFlags.DelayBindScriptBlock:
        /// If set, arguments that are of type ScriptBlock where the parameter is not of type ScriptBlock,
        /// Object, or PSObject will be stored for execution during pipeline input and not bound as
        /// an argument to the parameter.
        /// </param>
        /// <returns>
        /// True if the parameter was successfully bound. False if <paramref name="flags"/>
        /// has the flag <see cref="ParameterBindingFlags.ShouldCoerceType"/> set and the type does not match the parameter type.
        /// </returns>
        internal override bool BindParameter(
            uint parameterSets,
            CommandParameterInternal argument,
            MergedCompiledCommandParameter parameter,
            ParameterBindingFlags flags)
        {
            // Now we need to check to see if the argument value is
            // a ScriptBlock.  If it is and the parameter type is
            // not ScriptBlock and not Object, then we need to delay
            // binding until a pipeline object is provided to invoke
            // the ScriptBlock.

            // Note: we haven't yet determined that only a single parameter
            // set is valid, so we have to take a best guess on pipeline input
            // based on the current valid parameter sets.

            bool continueWithBinding = true;

            if ((flags & ParameterBindingFlags.DelayBindScriptBlock) != 0 &&
                parameter.Parameter.DoesParameterSetTakePipelineInput(parameterSets) &&
                argument.ArgumentSpecified)
            {
                object argumentValue = argument.ArgumentValue;
                if ((argumentValue is ScriptBlock || argumentValue is DelayedScriptBlockArgument) &&
                    !IsParameterScriptBlockBindable(parameter))
                {
                    // Now check to see if the command expects to have pipeline input.
                    // If not, we should throw an exception now to inform the
                    // user with more information than they would get if it was
                    // considered an unbound mandatory parameter.

                    if (_commandRuntime.IsClosed && _commandRuntime.InputPipe.Empty)
                    {
                        ParameterBindingException bindingException =
                            new ParameterBindingException(
                                    ErrorCategory.MetadataError,
                                    this.Command.MyInvocation,
                                    GetErrorExtent(argument),
                                    parameter.Parameter.Name,
                                    parameter.Parameter.Type,
                                    null,
                                    ParameterBinderStrings.ScriptBlockArgumentNoInput,
                                    "ScriptBlockArgumentNoInput");

                        throw bindingException;
                    }

                    ParameterBinderBase.bindingTracer.WriteLine(
                        "Adding ScriptBlock to delay-bind list for parameter '{0}'",
                        parameter.Parameter.Name);

                    // We need to delay binding of this argument to the parameter

                    DelayedScriptBlockArgument delayedArg = argumentValue as DelayedScriptBlockArgument ??
                                                            new DelayedScriptBlockArgument { _argument = argument, _parameterBinder = this };
                    if (!_delayBindScriptBlocks.ContainsKey(parameter))
                    {
                        _delayBindScriptBlocks.Add(parameter, delayedArg);
                    }

                    // We treat the parameter as bound, but really the
                    // script block gets run for each pipeline object and
                    // the result is bound.

                    if (parameter.Parameter.ParameterSetFlags != 0)
                    {
                        _currentParameterSetFlag &= parameter.Parameter.ParameterSetFlags;
                    }

                    UnboundParameters.Remove(parameter);

                    BoundParameters[parameter.Parameter.Name] = parameter;
                    BoundArguments[parameter.Parameter.Name] = argument;

                    if (DefaultParameterBinder.RecordBoundParameters &&
                        !DefaultParameterBinder.CommandLineParameters.ContainsKey(parameter.Parameter.Name))
                    {
                        DefaultParameterBinder.CommandLineParameters.Add(parameter.Parameter.Name, delayedArg);
                    }

                    continueWithBinding = false;
                }
            }

            bool result = false;
            if (continueWithBinding)
            {
                try
                {
                    result = BindParameter(argument, parameter, flags);
                }
                catch (Exception e)
                {
                    bool rethrow = true;
                    if ((flags & ParameterBindingFlags.ShouldCoerceType) == 0)
                    {
                        // Attributes are used to do type coercion and result in various exceptions.
                        // We assume that if we aren't trying to do type coercion, we should avoid
                        // propagating type conversion exceptions.
                        while (e != null)
                        {
                            if (e is PSInvalidCastException)
                            {
                                rethrow = false;
                                break;
                            }

                            e = e.InnerException;
                        }
                    }

                    if (rethrow)
                    {
                        throw;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Binds the specified argument to the specified parameter using the appropriate
        /// parameter binder.
        /// </summary>
        /// <param name="argument">
        /// The argument to be bound.
        /// </param>
        /// <param name="parameter">
        /// The metadata for the parameter to bind the argument to.
        /// </param>
        /// <param name="flags">
        /// Flags for type coercion and validation.
        /// </param>
        /// <returns>
        /// True if the parameter was successfully bound. False if <paramref name="flags"/>
        /// has the flag <see cref="ParameterBindingFlags.ShouldCoerceType"/> set and the type does not match the parameter type.
        /// </returns>
        private bool BindParameter(
            CommandParameterInternal argument,
            MergedCompiledCommandParameter parameter,
            ParameterBindingFlags flags)
        {
            bool result = false;

            switch (parameter.BinderAssociation)
            {
                case ParameterBinderAssociation.DeclaredFormalParameters:
                    result =
                        DefaultParameterBinder.BindParameter(
                            argument,
                            parameter.Parameter,
                            flags);
                    break;

                case ParameterBinderAssociation.CommonParameters:
                    result =
                        CommonParametersBinder.BindParameter(
                            argument,
                            parameter.Parameter,
                            flags);
                    break;

                case ParameterBinderAssociation.ShouldProcessParameters:
                    Diagnostics.Assert(
                        _commandMetadata.SupportsShouldProcess,
                        "The metadata for the ShouldProcessParameters should only be available if the command supports ShouldProcess");

                    result =
                        ShouldProcessParametersBinder.BindParameter(
                            argument,
                            parameter.Parameter,
                            flags);
                    break;

                case ParameterBinderAssociation.PagingParameters:
                    Diagnostics.Assert(
                        _commandMetadata.SupportsPaging,
                        "The metadata for the PagingParameters should only be available if the command supports paging");

                    result =
                        PagingParametersBinder.BindParameter(
                            argument,
                            parameter.Parameter,
                            flags);
                    break;

                case ParameterBinderAssociation.TransactionParameters:
                    Diagnostics.Assert(
                        _commandMetadata.SupportsTransactions,
                        "The metadata for the TransactionsParameters should only be available if the command supports transactions");

                    result =
                        TransactionParametersBinder.BindParameter(
                            argument,
                            parameter.Parameter,
                            flags);
                    break;

                case ParameterBinderAssociation.DynamicParameters:
                    Diagnostics.Assert(
                        _commandMetadata.ImplementsDynamicParameters,
                        "The metadata for the dynamic parameters should only be available if the command supports IDynamicParameters");

                    if (_dynamicParameterBinder != null)
                    {
                        result =
                            _dynamicParameterBinder.BindParameter(
                                argument,
                                parameter.Parameter,
                                flags);
                    }

                    break;
            }

            if (result && ((flags & ParameterBindingFlags.IsDefaultValue) == 0))
            {
                // Update the current valid parameter set flags

                if (parameter.Parameter.ParameterSetFlags != 0)
                {
                    _currentParameterSetFlag &= parameter.Parameter.ParameterSetFlags;
                }

                UnboundParameters.Remove(parameter);

                if (!BoundParameters.ContainsKey(parameter.Parameter.Name))
                {
                    BoundParameters.Add(parameter.Parameter.Name, parameter);
                }

                if (!BoundArguments.ContainsKey(parameter.Parameter.Name))
                {
                    BoundArguments.Add(parameter.Parameter.Name, argument);
                }

                if (parameter.Parameter.ObsoleteAttribute != null &&
                    (flags & ParameterBindingFlags.IsDefaultValue) == 0 &&
                    !BoundObsoleteParameterNames.Contains(parameter.Parameter.Name))
                {
                    string obsoleteWarning = string.Format(
                        CultureInfo.InvariantCulture,
                        ParameterBinderStrings.UseOfDeprecatedParameterWarning,
                        parameter.Parameter.Name,
                        parameter.Parameter.ObsoleteAttribute.Message);
                    var warningRecord = new WarningRecord(ParameterBinderBase.FQIDParameterObsolete, obsoleteWarning);

                    BoundObsoleteParameterNames.Add(parameter.Parameter.Name);

                    ObsoleteParameterWarningList ??= new List<WarningRecord>();

                    ObsoleteParameterWarningList.Add(warningRecord);
                }
            }

            return result;
        }

        /// <summary>
        /// Binds the remaining arguments to an unbound ValueFromRemainingArguments parameter (Varargs)
        /// </summary>
        /// <exception cref="ParameterBindingException">
        /// If there was an error binding the arguments to the parameters.
        /// </exception>
        private void HandleRemainingArguments()
        {
            if (UnboundArguments.Count > 0)
            {
                // Find the parameters that take the remaining args, if there are more
                // than one and the parameter set has not been defined, this is an error

                MergedCompiledCommandParameter varargsParameter = null;

                foreach (MergedCompiledCommandParameter parameter in UnboundParameters)
                {
                    ParameterSetSpecificMetadata parameterSetData = parameter.Parameter.GetParameterSetData(_currentParameterSetFlag);

                    if (parameterSetData == null)
                    {
                        continue;
                    }

                    // If the parameter takes the remaining arguments, bind them.

                    if (parameterSetData.ValueFromRemainingArguments)
                    {
                        if (varargsParameter != null)
                        {
                            ParameterBindingException bindingException =
                                new ParameterBindingException(
                                        ErrorCategory.MetadataError,
                                        this.Command.MyInvocation,
                                        null,
                                        parameter.Parameter.Name,
                                        parameter.Parameter.Type,
                                        null,
                                        ParameterBinderStrings.AmbiguousParameterSet,
                                        "AmbiguousParameterSet");

                            // Might be caused by the default parameter binding
                            if (!DefaultParameterBindingInUse)
                            {
                                throw bindingException;
                            }
                            else
                            {
                                ThrowElaboratedBindingException(bindingException);
                            }
                        }

                        varargsParameter = parameter;
                    }
                }

                if (varargsParameter != null)
                {
                    using (ParameterBinderBase.bindingTracer.TraceScope(
                        "BIND REMAININGARGUMENTS cmd line args to param: [{0}]",
                        varargsParameter.Parameter.Name))
                    {
                        // Accumulate the unbound arguments in to an list and then bind it to the parameter

                        List<object> valueFromRemainingArguments = new List<object>();

                        foreach (CommandParameterInternal argument in UnboundArguments)
                        {
                            if (argument.ParameterNameSpecified)
                            {
                                Diagnostics.Assert(!string.IsNullOrEmpty(argument.ParameterText), "Don't add a null argument");
                                valueFromRemainingArguments.Add(argument.ParameterText);
                            }

                            if (argument.ArgumentSpecified)
                            {
                                object argumentValue = argument.ArgumentValue;
                                if (argumentValue != AutomationNull.Value && argumentValue != UnboundParameter.Value)
                                {
                                    valueFromRemainingArguments.Add(argumentValue);
                                }
                            }
                        }

                        // If there are multiple arguments, it's not clear how best to represent the extent as the extent
                        // may be disjoint, as in 'echo a -verbose b', we have 'a' and 'b' in UnboundArguments.
                        var argumentAst = UnboundArguments.Count == 1 ? UnboundArguments[0].ArgumentAst : null;
                        var cpi = CommandParameterInternal.CreateParameterWithArgument(
                            /*parameterAst*/null, varargsParameter.Parameter.Name, "-" + varargsParameter.Parameter.Name + ":",
                            argumentAst, valueFromRemainingArguments, false);

                        // To make all of the following work similarly (the first is handled elsewhere, but second and third are
                        // handled here):
                        //     Set-ClusterOwnerNode -Owners foo,bar
                        //     Set-ClusterOwnerNode foo bar
                        //     Set-ClusterOwnerNode foo,bar
                        // we unwrap our List, but only if there is a single argument which is a collection.
                        if (valueFromRemainingArguments.Count == 1 && LanguagePrimitives.IsObjectEnumerable(valueFromRemainingArguments[0]))
                        {
                            cpi.SetArgumentValue(UnboundArguments[0].ArgumentAst, valueFromRemainingArguments[0]);
                        }

                        try
                        {
                            BindParameter(cpi, varargsParameter, ParameterBindingFlags.ShouldCoerceType);
                        }
                        catch (ParameterBindingException pbex)
                        {
                            if (!DefaultParameterBindingInUse)
                            {
                                throw;
                            }
                            else
                            {
                                ThrowElaboratedBindingException(pbex);
                            }
                        }

                        UnboundArguments.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// Determines if the cmdlet supports dynamic parameters. If it does,
        /// the dynamic parameter bindable object is retrieved and the unbound
        /// arguments are bound to it.
        /// </summary>
        /// <param name="outgoingBindingException">
        /// Returns the underlying parameter binding exception if any was generated.
        /// </param>
        /// <exception cref="MetadataException">
        /// If there was an error compiling the parameter metadata.
        /// </exception>
        /// <exception cref="ParameterBindingException">
        /// If there was an error binding the arguments to the parameters.
        /// </exception>
        private void HandleCommandLineDynamicParameters(out ParameterBindingException outgoingBindingException)
        {
            outgoingBindingException = null;

            if (_commandMetadata.ImplementsDynamicParameters && this.Command is IDynamicParameters dynamicParameterCmdlet)
            {
                using (ParameterBinderBase.bindingTracer.TraceScope(
                    "BIND cmd line args to DYNAMIC parameters."))
                    s_tracer.WriteLine("The Cmdlet supports the dynamic parameter interface");
                if (_dynamicParameterBinder == null)
                {
                    s_tracer.WriteLine("Getting the bindable object from the Cmdlet");

                    // Now get the dynamic parameter bindable object.
                    object dynamicParamBindableObject = null;

                    try
                    {
                        dynamicParamBindableObject = dynamicParameterCmdlet.GetDynamicParameters();
                    }
                    catch (ParameterBindingException e)
                    {
                        outgoingBindingException = e;
                    }
                    catch (Exception e) // Catch-all OK, this is a third-party callout
                    {
                        if (e is ProviderInvocationException)
                        {
                            throw;
                        }

                        ParameterBindingException bindingException =
                            new ParameterBindingException(
                                e,
                                ErrorCategory.InvalidArgument,
                                this.Command.MyInvocation,
                                null,
                                null,
                                null,
                                null,
                                ParameterBinderStrings.GetDynamicParametersException,
                                "GetDynamicParametersException",
                                e.Message);

                        // This exception is caused because failure happens when retrieving the dynamic parameters,
                        // this is not caused by introducing the default parameter binding.
                        throw bindingException;
                    }

                    if (dynamicParamBindableObject == null)
                    {
                        if (_dynamicParameterBinder == null)
                        {
                            s_tracer.WriteLine("No dynamic parameter object or RuntimeDefinedParameters were returned from the Cmdlet");
                            return;
                        }else
                        {
                            s_tracer.WriteLine("RuntimeDefinedParameters were just-in-time bound from the output pipeline of the dynamicparam block");
                        }
                    }
                    else
                    {
                        ParameterBinderBase.bindingTracer.WriteLine(
                            "DYNAMIC parameter object: [{0}]",
                            dynamicParamBindableObject.GetType());

                        // Now merge the metadata with other metadata for the command

                        MergeStaticAndDynamicParameterMetadata(dynamicParamBindableObject);
                    }
                }
                BindDynamicParameters(out outgoingBindingException);
            }
        }

        internal void MergeStaticAndDynamicParameterMetadata(object dynamicParamBindableObject)
        {
            InternalParameterMetadata dynamicParameterMetadata;
            if (dynamicParamBindableObject is RuntimeDefinedParameterDictionary runtimeParamDictionary)
            {
                // Generate the type metadata for the runtime-defined parameters
                dynamicParameterMetadata =
                    InternalParameterMetadata.Get(runtimeParamDictionary, true, true);
                s_tracer.WriteLine("Creating a new {0} for the returned RuntimeDefinedParameterDictionary", nameof(RuntimeDefinedParameterBinder));
                _dynamicParameterBinder =
                    new RuntimeDefinedParameterBinder(
                        runtimeParamDictionary,
                        this.Command,
                        this.CommandLineParameters);
            }
            else
            {
                // Generate the type metadata or retrieve it from the cache
                Type objectType = dynamicParamBindableObject.GetType();
                dynamicParameterMetadata =
                    InternalParameterMetadata.Get(objectType, Context, true);

                // Create the parameter binder for the dynamic parameter object
                s_tracer.WriteLine("Creating a new {0} for the returned object type [{1}]", nameof(ReflectionParameterBinder), objectType.FullName);

                _dynamicParameterBinder =
                    new ReflectionParameterBinder(
                        dynamicParamBindableObject,
                        this.Command,
                        this.CommandLineParameters);
            }
            var dynamicParams = BindableParameters.AddMetadataForBinder(
                                                    dynamicParameterMetadata,
                                                    ParameterBinderAssociation.DynamicParameters);
            foreach (var param in dynamicParams)
            {
                UnboundParameters.Add(param);
            }
            // Now set the parameter set flags for the new type metadata.
            _commandMetadata.DefaultParameterSetFlag =
                                                this.BindableParameters.GenerateParameterSetMappingFromMetadata(_commandMetadata.DefaultParameterSetName);
        }

        internal void BindDynamicParameters(out ParameterBindingException outgoingBindingException)
        {
            outgoingBindingException = null;
            if (UnboundArguments.Count > 0)
            {
                using (ParameterBinderBase.bindingTracer.TraceScope(
                        "BIND NAMED args to DYNAMIC parameters"))
                {
                    // Try to bind the unbound arguments as static parameters to the
                    // dynamic parameter object.

                    ReparseUnboundArguments();

                    UnboundArguments = BindNamedParameters(_currentParameterSetFlag, UnboundArguments);
                }

                using (ParameterBinderBase.bindingTracer.TraceScope(
                        "BIND POSITIONAL args to DYNAMIC parameters"))
                {
                    UnboundArguments =
                        BindPositionalParameters(
                        UnboundArguments,
                        _currentParameterSetFlag,
                        _commandMetadata.DefaultParameterSetFlag,
                        out outgoingBindingException);
                }
            }
        }

        /// <summary>
        /// This method determines if the unbound mandatory parameters take pipeline input or
        /// if we can use the default parameter set.  If all the unbound mandatory parameters
        /// take pipeline input and the default parameter set is valid, then the default parameter
        /// set is set as the current parameter set and processing can continue.  If there are
        /// more than one valid parameter sets and the unbound mandatory parameters are not
        /// consistent across parameter sets or there is no default parameter set then a
        /// ParameterBindingException is thrown with an errorId of AmbiguousParameterSet.
        /// </summary>
        /// <param name="validParameterSetCount">
        /// The number of valid parameter sets.
        /// </param>
        /// <param name="isPipelineInputExpected">
        /// True if the pipeline is open to receive input.
        /// </param>
        /// <exception cref="ParameterBindingException">
        /// If there are multiple valid parameter sets and the missing mandatory parameters are
        /// not consistent across parameter sets, or there is no default parameter set.
        /// </exception>
        [SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode", Justification = "Consider Simplifying it.")]
        private Collection<MergedCompiledCommandParameter> GetMissingMandatoryParameters(
            int validParameterSetCount,
            bool isPipelineInputExpected)
        {
            Collection<MergedCompiledCommandParameter> result = new Collection<MergedCompiledCommandParameter>();

            uint defaultParameterSet = _commandMetadata.DefaultParameterSetFlag;
            uint commandMandatorySets = 0;

            Dictionary<uint, ParameterSetPromptingData> promptingData = new Dictionary<uint, ParameterSetPromptingData>();

            bool missingAMandatoryParameter = false;
            bool missingAMandatoryParameterInAllSet = false;

            // See if any of the unbound parameters are mandatory

            foreach (MergedCompiledCommandParameter parameter in UnboundParameters)
            {
                // If a parameter is never mandatory, we can skip lots of work here.
                if (!parameter.Parameter.IsMandatoryInSomeParameterSet)
                {
                    continue;
                }

                var matchingParameterSetMetadata = parameter.Parameter.GetMatchingParameterSetData(_currentParameterSetFlag);

                uint parameterMandatorySets = 0;
                bool thisParameterMissing = false;

                foreach (ParameterSetSpecificMetadata parameterSetMetadata in matchingParameterSetMetadata)
                {
                    uint newMandatoryParameterSetFlag = NewParameterSetPromptingData(promptingData, parameter, parameterSetMetadata, defaultParameterSet, isPipelineInputExpected);

                    if (newMandatoryParameterSetFlag != 0)
                    {
                        missingAMandatoryParameter = true;
                        thisParameterMissing = true;

                        if (newMandatoryParameterSetFlag != uint.MaxValue)
                        {
                            parameterMandatorySets |= (_currentParameterSetFlag & newMandatoryParameterSetFlag);
                            commandMandatorySets |= (_currentParameterSetFlag & parameterMandatorySets);
                        }
                        else
                        {
                            missingAMandatoryParameterInAllSet = true;
                        }
                    }
                }

                // We are not expecting pipeline input
                if (!isPipelineInputExpected)
                {
                    // The parameter is mandatory so we need to prompt for it
                    if (thisParameterMissing)
                    {
                        result.Add(parameter);
                        continue;
                    }
                    // The parameter was not mandatory in any parameter set
                }
            }

            if (missingAMandatoryParameter && isPipelineInputExpected)
            {
                if (commandMandatorySets == 0)
                {
                    commandMandatorySets = _currentParameterSetFlag;
                }

                if (missingAMandatoryParameterInAllSet)
                {
                    uint availableParameterSetFlags = this.BindableParameters.AllParameterSetFlags;
                    if (availableParameterSetFlags == 0)
                    {
                        availableParameterSetFlags = uint.MaxValue;
                    }

                    commandMandatorySets = (_currentParameterSetFlag & availableParameterSetFlags);
                }

                // First we need to see if there are multiple valid parameter sets, and if one is
                // the default parameter set, and it is not missing any mandatory parameters, then
                // use the default parameter set.

                if (validParameterSetCount > 1 &&
                    defaultParameterSet != 0 &&
                    (defaultParameterSet & commandMandatorySets) == 0 &&
                    (defaultParameterSet & _currentParameterSetFlag) != 0)
                {
                    // If no other set takes pipeline input, then latch on to the default set

                    uint setThatTakesPipelineInput = 0;
                    foreach (ParameterSetPromptingData promptingSetData in promptingData.Values)
                    {
                        if ((promptingSetData.ParameterSet & _currentParameterSetFlag) != 0 &&
                            (promptingSetData.ParameterSet & defaultParameterSet) == 0 &&
                            !promptingSetData.IsAllSet)
                        {
                            if (promptingSetData.PipelineableMandatoryParameters.Count > 0)
                            {
                                setThatTakesPipelineInput = promptingSetData.ParameterSet;
                                break;
                            }
                        }
                    }

                    if (setThatTakesPipelineInput == 0)
                    {
                        // Old algorithm starts
                        // // latch on to the default parameter set
                        // commandMandatorySets = defaultParameterSet;
                        // _currentParameterSetFlag = defaultParameterSet;
                        // Command.SetParameterSetName(CurrentParameterSetName);
                        // Old algorithm ends

                        // At this point, we have the following information:
                        //  1. There are unbound mandatory parameter(s)
                        //  2. No unbound mandatory parameter is in AllSet
                        //  3. All unbound mandatory parameters don't take pipeline input
                        //  4. Default parameter set is valid
                        //  5. Default parameter set doesn't contain unbound mandatory parameters
                        //
                        // We ignore those parameter sets that contain unbound mandatory parameters, but leave
                        // all other parameter sets remain valid. The other parameter sets contains the default
                        // parameter set and have one characteristic: NONE of them contain unbound mandatory parameters
                        //
                        // Comparing to the old algorithm, we keep more possible parameter sets here, but
                        // we need to prioritize the default parameter set for pipeline binding, so as NOT to
                        // make breaking changes. This is to handle the following scenario:
                        //                               Old Algorithm              New Algorithm (without prioritizing default)      New Algorithm (with prioritizing default)
                        //  Remaining Parameter Sets       A(default)               A(default), B                                     A(default), B
                        //        Pipeline parameter       P1(string)               A: P1(string); B: P2(System.DateTime)             A: P1(string); B: P2(System.DateTime)
                        //   Pipeline parameter type       P1:By Value              P1:By Value; P2:By Value                          P1:By Value; P2:By Value
                        //            Pipeline input       $a (System.DateTime)     $a (System.DateTime)                              $a (System.DateTime)
                        //   Pipeline binding result       P1 --> $a.ToString()     P2 --> $a                                         P1 --> $a.ToString()
                        //     Pipeline binding type       ByValueWithCoercion      ByValueWithoutCoercion                            ByValueWithCoercion

                        commandMandatorySets = _currentParameterSetFlag & (~commandMandatorySets);
                        _currentParameterSetFlag = commandMandatorySets;

                        if (_currentParameterSetFlag == defaultParameterSet)
                            Command.SetParameterSetName(CurrentParameterSetName);
                        else
                            _parameterSetToBePrioritizedInPipelineBinding = defaultParameterSet;
                    }
                }
                // We need to analyze the prompting data that was gathered to determine what parameter
                // set to use, which parameters need prompting for, and which parameters take pipeline input.

                int commandMandatorySetsCount = ValidParameterSetCount(commandMandatorySets);
                if (commandMandatorySetsCount == 0)
                {
                    ThrowAmbiguousParameterSetException(_currentParameterSetFlag, BindableParameters);
                }
                else if (commandMandatorySetsCount == 1)
                {
                    // Since we have only one valid parameter set, add all
                    foreach (ParameterSetPromptingData promptingSetData in promptingData.Values)
                    {
                        if ((promptingSetData.ParameterSet & commandMandatorySets) != 0 ||
                            promptingSetData.IsAllSet)
                        {
                            foreach (MergedCompiledCommandParameter mandatoryParameter in promptingSetData.NonpipelineableMandatoryParameters.Keys)
                            {
                                result.Add(mandatoryParameter);
                            }
                        }
                    }
                }
                else if (_parameterSetToBePrioritizedInPipelineBinding == 0)
                {
                    // We have more than one valid parameter set.  Need to figure out which one to
                    // use.
                    // First we need to process the default parameter set if it can fill its parameters
                    // from the pipeline.

                    bool latchOnToDefault = false;
                    if (defaultParameterSet != 0 && (commandMandatorySets & defaultParameterSet) != 0)
                    {
                        // Determine if another set could be satisfied by pipeline input - that is, it
                        // has mandatory pipeline input parameters but no mandatory command-line only parameters.
                        bool anotherSetTakesPipelineInput = false;
                        foreach (ParameterSetPromptingData paramPromptingData in promptingData.Values)
                        {
                            if (!paramPromptingData.IsAllSet &&
                                !paramPromptingData.IsDefaultSet &&
                                paramPromptingData.PipelineableMandatoryParameters.Count > 0 &&
                                paramPromptingData.NonpipelineableMandatoryParameters.Count == 0)
                            {
                                anotherSetTakesPipelineInput = true;
                                break;
                            }
                        }

                        // Determine if another set takes pipeline input by property name
                        bool anotherSetTakesPipelineInputByPropertyName = false;
                        foreach (ParameterSetPromptingData paramPromptingData in promptingData.Values)
                        {
                            if (!paramPromptingData.IsAllSet &&
                                !paramPromptingData.IsDefaultSet &&
                                paramPromptingData.PipelineableMandatoryByPropertyNameParameters.Count > 0)
                            {
                                anotherSetTakesPipelineInputByPropertyName = true;
                                break;
                            }
                        }

                        // See if we should pick the default set if it can bind strongly to the incoming objects
                        ParameterSetPromptingData defaultSetPromptingData;
                        if (promptingData.TryGetValue(defaultParameterSet, out defaultSetPromptingData))
                        {
                            bool defaultSetTakesPipelineInput = defaultSetPromptingData.PipelineableMandatoryParameters.Count > 0;
                            bool defaultSetTakesPipelineInputByPropertyName = defaultSetPromptingData.PipelineableMandatoryByPropertyNameParameters.Count > 0;

                            if (defaultSetTakesPipelineInputByPropertyName && !anotherSetTakesPipelineInputByPropertyName)
                            {
                                latchOnToDefault = true;
                            }
                            else if (defaultSetTakesPipelineInput && !anotherSetTakesPipelineInput)
                            {
                                latchOnToDefault = true;
                            }
                        }

                        if (!latchOnToDefault)
                        {
                            // If only the all set takes pipeline input then latch on to the
                            // default set

                            if (!anotherSetTakesPipelineInput)
                            {
                                latchOnToDefault = true;
                            }
                        }

                        if (!latchOnToDefault)
                        {
                            // Need to see if there are nonpipelineable mandatory parameters in the
                            // all set.

                            ParameterSetPromptingData allSetPromptingData;
                            if (promptingData.TryGetValue(uint.MaxValue, out allSetPromptingData))
                            {
                                if (allSetPromptingData.NonpipelineableMandatoryParameters.Count > 0)
                                {
                                    latchOnToDefault = true;
                                }
                            }
                        }

                        if (latchOnToDefault)
                        {
                            // latch on to the default parameter set
                            commandMandatorySets = defaultParameterSet;
                            _currentParameterSetFlag = defaultParameterSet;
                            Command.SetParameterSetName(CurrentParameterSetName);

                            // Add all missing mandatory parameters that don't take pipeline input
                            foreach (ParameterSetPromptingData promptingSetData in promptingData.Values)
                            {
                                if ((promptingSetData.ParameterSet & commandMandatorySets) != 0 ||
                                    promptingSetData.IsAllSet)
                                {
                                    foreach (MergedCompiledCommandParameter mandatoryParameter in promptingSetData.NonpipelineableMandatoryParameters.Keys)
                                    {
                                        result.Add(mandatoryParameter);
                                    }
                                }
                            }
                        }
                    }

                    if (!latchOnToDefault)
                    {
                        // When we select a mandatory set to latch on, we should try to preserve other parameter sets that contain no mandatory parameters or contain only common mandatory parameters
                        // as much as possible, so as to support the binding for the following scenarios:
                        //
                        // (1) Scenario 1:
                        // Valid parameter sets when it comes to the mandatory checking: A, B
                        // Mandatory parameters in A, B:
                        // Set      Nonpipelineable-Mandatory-InSet         Pipelineable-Mandatory-InSet       Common-Nonpipelineable-Mandatory       Common-Pipelineable-Mandatory
                        // A        N/A                                     N/A                                N/A                                    AllParam (of type DateTime)
                        // B        N/A                                     ParamB (of type TimeSpan)          N/A                                    AllParam (of type DateTime)
                        //
                        // Piped-in object: Get-Date
                        //
                        // (2) Scenario 2:
                        // Valid parameter sets when it comes to the mandatory checking: A, B, C, Default
                        // Mandatory parameters in A, B, C and Default:
                        // Set      Nonpipelineable-Mandatory-InSet         Pipelineable-Mandatory-InSet       Common-Nonpipelineable-Mandatory       Common-Pipelineable-Mandatory
                        // A        N/A                                     N/A                                N/A                                    AllParam (of type DateTime)
                        // B        N/A                                     ParamB (of type TimeSpan)          N/A                                    AllParam (of type DateTime)
                        // C        N/A                                     N/A                                N/A                                    AllParam (of type DateTime)
                        // Default  N/A                                     N/A                                N/A                                    AllParam (of type DateTime)
                        //
                        // Piped-in object: Get-Date
                        //
                        // Before the fix, the mandatory checking will resolve the parameter set to be B in both scenario 1 and 2, which will fail in the subsequent pipeline binding.
                        // After the fix, the parameter set "A" in the scenario 1 and the set "A", "C", "Default" in the scenario 2 will be preserved, and the subsequent pipeline binding will succeed.
                        //
                        // (3) Scenario 3:
                        // Valid parameter sets when it comes to the mandatory checking: A, B, C
                        // Mandatory parameters in A, B and C:
                        // Set      Nonpipelineable-Mandatory-InSet         Pipelineable-Mandatory-InSet       Pipelineable-Nonmandatory-InSet       Common-Nonpipelineable-Mandatory       Common-Pipelineable-Mandatory       Common-Pipelineable-Nonmandatory
                        // A        N/A                                     ParamA (of type TimeSpan)          N/A                                   N/A                                    N/A                                 N/A
                        // B        ParamB-1                                N/A                                ParamB-2 (of type string[])           N/A                                    N/A                                 N/A
                        // C        N/A                                     N/A                                ParamC (of type DateTime)             N/A                                    N/A                                 N/A
                        //
                        // (4) Scenario 4:
                        // Valid parameter sets when it comes to the mandatory checking: A, B, C, Default
                        // Mandatory parameters in A, B, C and Default:
                        // Set      Nonpipelineable-Mandatory-InSet         Pipelineable-Mandatory-InSet       Pipelineable-Nonmandatory-InSet       Common-Nonpipelineable-Mandatory       Common-Pipelineable-Mandatory       Common-Pipelineable-Nonmandatory
                        // A        N/A                                     ParamA (of type TimeSpan)          N/A                                   N/A                                    N/A                                 AllParam (of type DateTime)
                        // B        ParamB-1                                N/A                                ParamB-2 (of type string[])           N/A                                    N/A                                 AllParam (of type DateTime)
                        // C        N/A                                     N/A                                N/A                                   N/A                                    N/A                                 AllParam (of type DateTime)
                        // Default  N/A                                     N/A                                N/A                                   N/A                                    N/A                                 AllParam (of type DateTime)
                        //
                        // Piped-in object: Get-Date
                        //
                        // Before the fix, the mandatory checking will resolve the parameter set to be A in both scenario 3 and 4, which will fail in the subsequent pipeline binding.
                        // After the fix, the parameter set "C" in the scenario 1 and the set "C" and "Default" in the scenario 2 will be preserved, and the subsequent pipeline binding will succeed.
                        //
                        // Examples:
                        // (1) Scenario 1
                        // Function Get-Cmdlet
                        // {
                        //       [CmdletBinding()]
                        //       param(
                        //          [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
                        //          [System.DateTime]
                        //          $Date,
                        //          [Parameter(ParameterSetName="computer")]
                        //          [Parameter(ParameterSetName="session")]
                        //          $ComputerName,
                        //          [Parameter(ParameterSetName="session", Mandatory=$true, ValueFromPipeline=$true)]
                        //          [System.TimeSpan]
                        //          $TimeSpan
                        //       )
                        //
                        //      Process
                        //      {
                        //         Write-Output $PsCmdlet.ParameterSetName
                        //      }
                        // }
                        //
                        // PS:\> Get-Date | Get-Cmdlet
                        // PS:\> computer
                        //
                        // (2) Scenario 2
                        //
                        // Function Get-Cmdlet
                        // {
                        //       [CmdletBinding(DefaultParameterSetName="computer")]
                        //       param(
                        //          [Parameter(ParameterSetName="new")]
                        //          $NewName,
                        //          [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
                        //          [System.DateTime]
                        //          $Date,
                        //          [Parameter(ParameterSetName="computer")]
                        //          [Parameter(ParameterSetName="session")]
                        //          $ComputerName,
                        //          [Parameter(ParameterSetName="session", Mandatory=$true, ValueFromPipeline=$true)]
                        //          [System.TimeSpan]
                        //          $TimeSpan
                        //       )
                        //
                        //      Process
                        //      {
                        //         Write-Output $PsCmdlet.ParameterSetName
                        //      }
                        // }
                        //
                        // PS:\> Get-Date | Get-Cmdlet
                        // PS:\> computer
                        //
                        // (3) Scenario 3
                        //
                        // Function Get-Cmdlet
                        // {
                        //        [CmdletBinding()]
                        //        param(
                        //           [Parameter(ParameterSetName="network", Mandatory=$true, ValueFromPipeline=$true)]
                        //           [TimeSpan]
                        //           $network,
                        //
                        //           [Parameter(ParameterSetName="computer", ValueFromPipelineByPropertyName=$true)]
                        //           [string[]]
                        //           $ComputerName,
                        //
                        //           [Parameter(ParameterSetName="computer", Mandatory=$true)]
                        //           [switch]
                        //           $DisableComputer,
                        //
                        //           [Parameter(ParameterSetName="session", ValueFromPipeline=$true)]
                        //           [DateTime]
                        //           $Date
                        //        )
                        //
                        //        Process
                        //        {
                        //           Write-Output $PsCmdlet.ParameterSetName
                        //        }
                        // }
                        //
                        // PS:\> Get-Date | Get-Cmdlet
                        // PS:\> session
                        //
                        // (4) Scenario 4
                        //
                        // Function Get-Cmdlet
                        // {
                        //       [CmdletBinding(DefaultParameterSetName="server")]
                        //       param(
                        //          [Parameter(ParameterSetName="network", Mandatory=$true, ValueFromPipeline=$true)]
                        //          [TimeSpan]
                        //          $network,
                        //
                        //          [Parameter(ParameterSetName="computer", ValueFromPipelineByPropertyName=$true)]
                        //          [string[]]
                        //          $ComputerName,
                        //
                        //          [Parameter(ParameterSetName="computer", Mandatory=$true)]
                        //          [switch]
                        //          $DisableComputer,
                        //
                        //          [Parameter(ParameterSetName="session")]
                        //          [Parameter(ParameterSetName="server")]
                        //          [string]
                        //          $Param,
                        //
                        //          [Parameter(ValueFromPipeline=$true)]
                        //          [DateTime]
                        //          $Date
                        //       )

                        //      Process
                        //      {
                        //         Write-Output $PsCmdlet.ParameterSetName
                        //      }
                        // }
                        //
                        // PS:\> Get-Date | Get-Cmdlet
                        // PS:\> server
                        //

                        uint setThatTakesPipelineInputByValue = 0;
                        uint setThatTakesPipelineInputByPropertyName = 0;

                        // Find the single set that takes pipeline input by value
                        bool foundSetThatTakesPipelineInputByValue = false;
                        bool foundMultipleSetsThatTakesPipelineInputByValue = false;
                        foreach (ParameterSetPromptingData promptingSetData in promptingData.Values)
                        {
                            if ((promptingSetData.ParameterSet & commandMandatorySets) != 0 &&
                                !promptingSetData.IsAllSet)
                            {
                                if (promptingSetData.PipelineableMandatoryByValueParameters.Count > 0)
                                {
                                    if (foundSetThatTakesPipelineInputByValue)
                                    {
                                        foundMultipleSetsThatTakesPipelineInputByValue = true;
                                        setThatTakesPipelineInputByValue = 0;
                                        break;
                                    }

                                    setThatTakesPipelineInputByValue = promptingSetData.ParameterSet;
                                    foundSetThatTakesPipelineInputByValue = true;
                                }
                            }
                        }

                        // Find the single set that takes pipeline input by property name
                        bool foundSetThatTakesPipelineInputByPropertyName = false;
                        bool foundMultipleSetsThatTakesPipelineInputByPropertyName = false;
                        foreach (ParameterSetPromptingData promptingSetData in promptingData.Values)
                        {
                            if ((promptingSetData.ParameterSet & commandMandatorySets) != 0 &&
                                    !promptingSetData.IsAllSet)
                            {
                                if (promptingSetData.PipelineableMandatoryByPropertyNameParameters.Count > 0)
                                {
                                    if (foundSetThatTakesPipelineInputByPropertyName)
                                    {
                                        foundMultipleSetsThatTakesPipelineInputByPropertyName = true;
                                        setThatTakesPipelineInputByPropertyName = 0;
                                        break;
                                    }

                                    setThatTakesPipelineInputByPropertyName = promptingSetData.ParameterSet;
                                    foundSetThatTakesPipelineInputByPropertyName = true;
                                }
                            }
                        }

                        // If we have one or the other, we can latch onto that set without difficulty
                        uint uniqueSetThatTakesPipelineInput = 0;
                        if (foundSetThatTakesPipelineInputByValue && foundSetThatTakesPipelineInputByPropertyName &&
                            (setThatTakesPipelineInputByValue == setThatTakesPipelineInputByPropertyName))
                        {
                            uniqueSetThatTakesPipelineInput = setThatTakesPipelineInputByValue;
                        }

                        if (foundSetThatTakesPipelineInputByValue ^ foundSetThatTakesPipelineInputByPropertyName)
                        {
                            uniqueSetThatTakesPipelineInput = foundSetThatTakesPipelineInputByValue ?
                                setThatTakesPipelineInputByValue : setThatTakesPipelineInputByPropertyName;
                        }

                        if (uniqueSetThatTakesPipelineInput != 0)
                        {
                            // latch on to the set that takes pipeline input
                            commandMandatorySets = uniqueSetThatTakesPipelineInput;
                            uint otherMandatorySetsToBeIgnored = 0;
                            bool chosenMandatorySetContainsNonpipelineableMandatoryParameters = false;

                            // Add all missing mandatory parameters that don't take pipeline input
                            foreach (ParameterSetPromptingData promptingSetData in promptingData.Values)
                            {
                                if ((promptingSetData.ParameterSet & commandMandatorySets) != 0 ||
                                    promptingSetData.IsAllSet)
                                {
                                    if (!promptingSetData.IsAllSet)
                                    {
                                        chosenMandatorySetContainsNonpipelineableMandatoryParameters =
                                            promptingSetData.NonpipelineableMandatoryParameters.Count > 0;
                                    }

                                    foreach (MergedCompiledCommandParameter mandatoryParameter in promptingSetData.NonpipelineableMandatoryParameters.Keys)
                                    {
                                        result.Add(mandatoryParameter);
                                    }
                                }
                                else
                                {
                                    otherMandatorySetsToBeIgnored |= promptingSetData.ParameterSet;
                                }
                            }

                            // Preserve potential parameter sets as much as possible
                            PreservePotentialParameterSets(uniqueSetThatTakesPipelineInput,
                                                           otherMandatorySetsToBeIgnored,
                                                           chosenMandatorySetContainsNonpipelineableMandatoryParameters);
                        }
                        else
                        {
                            // Now if any valid parameter sets have nonpipelineable mandatory parameters we have
                            // an error
                            bool foundMissingParameters = false;
                            uint setsThatContainNonpipelineableMandatoryParameter = 0;
                            foreach (ParameterSetPromptingData promptingSetData in promptingData.Values)
                            {
                                if ((promptingSetData.ParameterSet & commandMandatorySets) != 0 ||
                                     promptingSetData.IsAllSet)
                                {
                                    if (promptingSetData.NonpipelineableMandatoryParameters.Count > 0)
                                    {
                                        foundMissingParameters = true;
                                        if (!promptingSetData.IsAllSet)
                                        {
                                            setsThatContainNonpipelineableMandatoryParameter |= promptingSetData.ParameterSet;
                                        }
                                    }
                                }
                            }

                            if (foundMissingParameters)
                            {
                                // As a last-ditch effort, bind to the set that takes pipeline input by value
                                if (setThatTakesPipelineInputByValue != 0)
                                {
                                    // latch on to the set that takes pipeline input
                                    commandMandatorySets = setThatTakesPipelineInputByValue;
                                    uint otherMandatorySetsToBeIgnored = 0;
                                    bool chosenMandatorySetContainsNonpipelineableMandatoryParameters = false;

                                    // Add all missing mandatory parameters that don't take pipeline input
                                    foreach (ParameterSetPromptingData promptingSetData in promptingData.Values)
                                    {
                                        if ((promptingSetData.ParameterSet & commandMandatorySets) != 0 ||
                                            promptingSetData.IsAllSet)
                                        {
                                            if (!promptingSetData.IsAllSet)
                                            {
                                                chosenMandatorySetContainsNonpipelineableMandatoryParameters =
                                                    promptingSetData.NonpipelineableMandatoryParameters.Count > 0;
                                            }

                                            foreach (MergedCompiledCommandParameter mandatoryParameter in promptingSetData.NonpipelineableMandatoryParameters.Keys)
                                            {
                                                result.Add(mandatoryParameter);
                                            }
                                        }
                                        else
                                        {
                                            otherMandatorySetsToBeIgnored |= promptingSetData.ParameterSet;
                                        }
                                    }

                                    // Preserve potential parameter sets as much as possible
                                    PreservePotentialParameterSets(setThatTakesPipelineInputByValue,
                                                                   otherMandatorySetsToBeIgnored,
                                                                   chosenMandatorySetContainsNonpipelineableMandatoryParameters);
                                }
                                else
                                {
                                    if ((!foundMultipleSetsThatTakesPipelineInputByValue) &&
                                       (!foundMultipleSetsThatTakesPipelineInputByPropertyName))
                                    {
                                        ThrowAmbiguousParameterSetException(_currentParameterSetFlag, BindableParameters);
                                    }

                                    // Remove the data set that contains non-pipelineable mandatory parameters, since we are not
                                    // prompting for them and they will not be bound later.
                                    // If no data set left, throw ambiguous parameter set exception
                                    if (setsThatContainNonpipelineableMandatoryParameter != 0)
                                    {
                                        IgnoreOtherMandatoryParameterSets(setsThatContainNonpipelineableMandatoryParameter);
                                        if (_currentParameterSetFlag == 0)
                                        {
                                            ThrowAmbiguousParameterSetException(_currentParameterSetFlag, BindableParameters);
                                        }

                                        if (ValidParameterSetCount(_currentParameterSetFlag) == 1)
                                        {
                                            Command.SetParameterSetName(CurrentParameterSetName);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Preserve potential parameter sets as much as possible.
        /// </summary>
        /// <param name="chosenMandatorySet">The mandatory set we choose to latch on.</param>
        /// <param name="otherMandatorySetsToBeIgnored">Other mandatory parameter sets to be ignored.</param>
        /// <param name="chosenSetContainsNonpipelineableMandatoryParameters">Indicate if the chosen mandatory set contains any non-pipelineable mandatory parameters.</param>
        private void PreservePotentialParameterSets(uint chosenMandatorySet, uint otherMandatorySetsToBeIgnored, bool chosenSetContainsNonpipelineableMandatoryParameters)
        {
            // If the chosen set contains nonpipelineable mandatory parameters, then we set it as the only valid parameter set since we will prompt for those mandatory parameters
            if (chosenSetContainsNonpipelineableMandatoryParameters)
            {
                _currentParameterSetFlag = chosenMandatorySet;
                Command.SetParameterSetName(CurrentParameterSetName);
            }
            else
            {
                // Otherwise, we additionally preserve those valid parameter sets that contain no mandatory parameter, or contain only the common mandatory parameters
                IgnoreOtherMandatoryParameterSets(otherMandatorySetsToBeIgnored);
                Command.SetParameterSetName(CurrentParameterSetName);

                if (_currentParameterSetFlag != chosenMandatorySet)
                {
                    _parameterSetToBePrioritizedInPipelineBinding = chosenMandatorySet;
                }
            }
        }

        /// <summary>
        /// Update _currentParameterSetFlag to ignore the specified mandatory sets.
        /// </summary>
        /// <remarks>
        /// This method is used only when we try to preserve parameter sets during the mandatory parameter checking.
        /// In cases where this method is used, there must be at least one parameter set declared.
        /// </remarks>
        /// <param name="otherMandatorySetsToBeIgnored">The mandatory parameter sets to be ignored.</param>
        private void IgnoreOtherMandatoryParameterSets(uint otherMandatorySetsToBeIgnored)
        {
            if (otherMandatorySetsToBeIgnored == 0)
                return;

            if (_currentParameterSetFlag == uint.MaxValue)
            {
                // We cannot update the _currentParameterSetFlag to remove some parameter sets directly when it's AllSet as that will get it to an incorrect state.
                uint availableParameterSets = this.BindableParameters.AllParameterSetFlags;
                Diagnostics.Assert(availableParameterSets != 0, "At least one parameter set must be declared");
                _currentParameterSetFlag = availableParameterSets & (~otherMandatorySetsToBeIgnored);
            }
            else
            {
                _currentParameterSetFlag &= (~otherMandatorySetsToBeIgnored);
            }
        }

        private static uint NewParameterSetPromptingData(
            Dictionary<uint, ParameterSetPromptingData> promptingData,
            MergedCompiledCommandParameter parameter,
            ParameterSetSpecificMetadata parameterSetMetadata,
            uint defaultParameterSet,
            bool pipelineInputExpected)
        {
            uint parameterMandatorySets = 0;
            uint parameterSetFlag = parameterSetMetadata.ParameterSetFlag;
            if (parameterSetFlag == 0)
            {
                parameterSetFlag = uint.MaxValue;
            }

            bool isDefaultSet = (defaultParameterSet != 0) && ((defaultParameterSet & parameterSetFlag) != 0);

            bool isMandatory = false;
            if (parameterSetMetadata.IsMandatory)
            {
                parameterMandatorySets |= parameterSetFlag;
                isMandatory = true;
            }

            bool isPipelineable = false;
            if (pipelineInputExpected)
            {
                if (parameterSetMetadata.ValueFromPipeline || parameterSetMetadata.ValueFromPipelineByPropertyName)
                {
                    isPipelineable = true;
                }
            }

            if (isMandatory)
            {
                ParameterSetPromptingData promptingDataForSet;
                if (!promptingData.TryGetValue(parameterSetFlag, out promptingDataForSet))
                {
                    promptingDataForSet = new ParameterSetPromptingData(parameterSetFlag, isDefaultSet);
                    promptingData.Add(parameterSetFlag, promptingDataForSet);
                }

                if (isPipelineable)
                {
                    promptingDataForSet.PipelineableMandatoryParameters[parameter] = parameterSetMetadata;

                    if (parameterSetMetadata.ValueFromPipeline)
                    {
                        promptingDataForSet.PipelineableMandatoryByValueParameters[parameter] = parameterSetMetadata;
                    }

                    if (parameterSetMetadata.ValueFromPipelineByPropertyName)
                    {
                        promptingDataForSet.PipelineableMandatoryByPropertyNameParameters[parameter] = parameterSetMetadata;
                    }
                }
                else
                {
                    promptingDataForSet.NonpipelineableMandatoryParameters[parameter] = parameterSetMetadata;
                }
            }

            return parameterMandatorySets;
        }

        /// <summary>
        /// Ensures that only one parameter set is valid or throws an appropriate exception.
        /// </summary>
        /// <param name="prePipelineInput">
        /// If true, it is acceptable to have multiple valid parameter sets as long as one
        /// of those parameter sets take pipeline input.
        /// </param>
        /// <param name="setDefault">
        /// If true, the default parameter set will be selected if there is more than
        /// one valid parameter set and one is the default set.
        /// If false, the count of valid parameter sets will be returned but no error
        /// will occur and the default parameter set will not be used.
        /// </param>
        /// <returns>
        /// The number of valid parameter sets.
        /// </returns>
        /// <exception cref="ParameterBindingException">
        /// If the more than one or zero parameter sets were resolved from the named
        /// parameters.
        /// </exception>
        private int ValidateParameterSets(bool prePipelineInput, bool setDefault)
        {
            // Compute how many parameter sets are still valid
            int validParameterSetCount = ValidParameterSetCount(_currentParameterSetFlag);

            if (validParameterSetCount == 0 && _currentParameterSetFlag != uint.MaxValue)
            {
                ThrowAmbiguousParameterSetException(_currentParameterSetFlag, BindableParameters);
            }
            else if (validParameterSetCount > 1)
            {
                uint defaultParameterSetFlag = _commandMetadata.DefaultParameterSetFlag;
                bool hasDefaultSetDefined = defaultParameterSetFlag != 0;

                bool validSetIsAllSet = _currentParameterSetFlag == uint.MaxValue;
                bool validSetIsDefault = _currentParameterSetFlag == defaultParameterSetFlag;

                // If no default parameter set is defined and the valid set is the "all" set
                // then use the all set.

                if (validSetIsAllSet && !hasDefaultSetDefined)
                {
                    // The current parameter set flags are valid.
                    // Note: this is the same as having a single valid parameter set flag.
                    validParameterSetCount = 1;
                }
                // If the valid parameter set is the default parameter set, or if the default
                // parameter set has been defined and one of the valid parameter sets is
                // the default parameter set, then use the default parameter set.
                else if (!prePipelineInput &&
                    validSetIsDefault ||
                    (hasDefaultSetDefined && (_currentParameterSetFlag & defaultParameterSetFlag) != 0))
                {
                    // NTRAID#Windows Out Of Band Releases-2006/02/14-928660-JonN
                    // Set currentParameterSetName regardless of setDefault
                    string currentParameterSetName = BindableParameters.GetParameterSetName(defaultParameterSetFlag);
                    Command.SetParameterSetName(currentParameterSetName);
                    if (setDefault)
                    {
                        _currentParameterSetFlag = _commandMetadata.DefaultParameterSetFlag;
                        validParameterSetCount = 1;
                    }
                }
                // There are multiple valid parameter sets but at least one parameter set takes
                // pipeline input
                else if (prePipelineInput &&
                    AtLeastOneUnboundValidParameterSetTakesPipelineInput(_currentParameterSetFlag))
                {
                    // We haven't fixated on a valid parameter set yet, but will wait for pipeline input to
                    // determine which parameter set to use.
                }
                else
                {
                    int resolvedParameterSetCount = ResolveParameterSetAmbiguityBasedOnMandatoryParameters();
                    if (resolvedParameterSetCount != 1)
                    {
                        ThrowAmbiguousParameterSetException(_currentParameterSetFlag, BindableParameters);
                    }

                    validParameterSetCount = resolvedParameterSetCount;
                }
            }
            else // validParameterSetCount == 1
            {
                // If the valid parameter set is the "all" set, and a default set was defined,
                // then set the current parameter set to the default set.

                if (_currentParameterSetFlag == uint.MaxValue)
                {
                    // Since this is the "all" set, default the parameter set count to the
                    // number of parameter sets that were defined for the cmdlet or 1 if
                    // none were defined.

                    validParameterSetCount =
                        (this.BindableParameters.ParameterSetCount > 0) ?
                            this.BindableParameters.ParameterSetCount : 1;

                    if (prePipelineInput &&
                        AtLeastOneUnboundValidParameterSetTakesPipelineInput(_currentParameterSetFlag))
                    {
                        // Don't fixate on the default parameter set yet. Wait until after
                        // we have processed pipeline input.
                    }
                    else if (_commandMetadata.DefaultParameterSetFlag != 0)
                    {
                        if (setDefault)
                        {
                            _currentParameterSetFlag = _commandMetadata.DefaultParameterSetFlag;
                            validParameterSetCount = 1;
                        }
                    }
                    // NTRAID#Windows Out Of Band Releases-2005/11/07-923917-JonN
                    else if (validParameterSetCount > 1)
                    {
                        int resolvedParameterSetCount = ResolveParameterSetAmbiguityBasedOnMandatoryParameters();
                        if (resolvedParameterSetCount != 1)
                        {
                            ThrowAmbiguousParameterSetException(_currentParameterSetFlag, BindableParameters);
                        }

                        validParameterSetCount = resolvedParameterSetCount;
                    }
                }

                Command.SetParameterSetName(CurrentParameterSetName);
            }

            return validParameterSetCount;
        }

        private int ResolveParameterSetAmbiguityBasedOnMandatoryParameters()
        {
            return ResolveParameterSetAmbiguityBasedOnMandatoryParameters(this.BoundParameters, this.UnboundParameters, this.BindableParameters, ref _currentParameterSetFlag, Command);
        }

        internal static int ResolveParameterSetAmbiguityBasedOnMandatoryParameters(
            Dictionary<string, MergedCompiledCommandParameter> boundParameters,
            ICollection<MergedCompiledCommandParameter> unboundParameters,
            MergedCommandParameterMetadata bindableParameters,
            ref uint _currentParameterSetFlag,
            Cmdlet command
            )
        {
            uint remainingParameterSetsWithNoMandatoryUnboundParameters = _currentParameterSetFlag;

            IEnumerable<ParameterSetSpecificMetadata> allParameterSetMetadatas = boundParameters.Values
                .Concat(unboundParameters)
                .SelectMany(static p => p.Parameter.ParameterSetData.Values);
            uint allParameterSetFlags = 0;
            foreach (ParameterSetSpecificMetadata parameterSetMetadata in allParameterSetMetadatas)
            {
                allParameterSetFlags |= parameterSetMetadata.ParameterSetFlag;
            }

            remainingParameterSetsWithNoMandatoryUnboundParameters &= allParameterSetFlags;

            Diagnostics.Assert(
                ValidParameterSetCount(remainingParameterSetsWithNoMandatoryUnboundParameters) > 1,
                "This method should only be called when there is an ambiguity wrt parameter sets");

            IEnumerable<ParameterSetSpecificMetadata> parameterSetMetadatasForUnboundMandatoryParameters = unboundParameters
                .SelectMany(static p => p.Parameter.ParameterSetData.Values)
                .Where(static p => p.IsMandatory);
            foreach (ParameterSetSpecificMetadata parameterSetMetadata in parameterSetMetadatasForUnboundMandatoryParameters)
            {
                remainingParameterSetsWithNoMandatoryUnboundParameters &= (~parameterSetMetadata.ParameterSetFlag);
            }

            int finalParameterSetCount = ValidParameterSetCount(remainingParameterSetsWithNoMandatoryUnboundParameters);
            if (finalParameterSetCount == 1)
            {
                _currentParameterSetFlag = remainingParameterSetsWithNoMandatoryUnboundParameters;

                if (command != null)
                {
                    string currentParameterSetName = bindableParameters.GetParameterSetName(_currentParameterSetFlag);
                    command.SetParameterSetName(currentParameterSetName);
                }

                return finalParameterSetCount;
            }

            return -1;
        }

        private void ThrowAmbiguousParameterSetException(uint parameterSetFlags, MergedCommandParameterMetadata bindableParameters)
        {
            ParameterBindingException bindingException =
                new ParameterBindingException(
                    ErrorCategory.InvalidArgument,
                    this.Command.MyInvocation,
                    null,
                    null,
                    null,
                    null,
                    ParameterBinderStrings.AmbiguousParameterSet,
                    "AmbiguousParameterSet");

            // Trace the parameter sets still active
            uint currentParameterSet = 1;

            while (parameterSetFlags != 0)
            {
                uint currentParameterSetActive = parameterSetFlags & 0x1;

                if (currentParameterSetActive == 1)
                {
                    string parameterSetName = bindableParameters.GetParameterSetName(currentParameterSet);
                    if (!string.IsNullOrEmpty(parameterSetName))
                    {
                        ParameterBinderBase.bindingTracer.WriteLine("Remaining valid parameter set: {0}", parameterSetName);
                    }
                }

                parameterSetFlags >>= 1;
                currentParameterSet <<= 1;
            }

            if (!DefaultParameterBindingInUse)
            {
                throw bindingException;
            }
            else
            {
                ThrowElaboratedBindingException(bindingException);
            }
        }

        /// <summary>
        /// Determines if there are any unbound parameters that take pipeline input
        /// for the specified parameter sets.
        /// </summary>
        /// <param name="validParameterSetFlags">
        /// The parameter sets that should be checked for each unbound parameter to see
        /// if it accepts pipeline input.
        /// </param>
        /// <returns>
        /// True if there is at least one parameter that takes pipeline input for the
        /// specified parameter sets, or false otherwise.
        /// </returns>
        private bool AtLeastOneUnboundValidParameterSetTakesPipelineInput(uint validParameterSetFlags)
        {
            bool result = false;

            // Loop through all the unbound parameters to see if there are any
            // that take pipeline input for the specified parameter sets.

            foreach (MergedCompiledCommandParameter parameter in UnboundParameters)
            {
                if (parameter.Parameter.DoesParameterSetTakePipelineInput(validParameterSetFlags))
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Checks for unbound mandatory parameters. If any are found, an exception is thrown.
        /// </summary>
        /// <param name="missingMandatoryParameters">
        /// Returns the missing mandatory parameters, if any.
        /// </param>
        /// <returns>
        /// True if there are no unbound mandatory parameters. False if there are unbound mandatory parameters.
        /// </returns>
        internal bool HandleUnboundMandatoryParameters(out Collection<MergedCompiledCommandParameter> missingMandatoryParameters)
        {
            return HandleUnboundMandatoryParameters(
                ValidParameterSetCount(_currentParameterSetFlag),
                false,
                false,
                false,
                out missingMandatoryParameters);
        }

        /// <summary>
        /// Checks for unbound mandatory parameters. If any are found and promptForMandatory is true,
        /// the user will be prompted for the missing mandatory parameters.
        /// </summary>
        /// <param name="validParameterSetCount">
        /// The number of valid parameter sets.
        /// </param>
        /// <param name="processMissingMandatory">
        /// If true, unbound mandatory parameters will be processed via user prompting (if allowed by promptForMandatory).
        /// If false, unbound mandatory parameters will cause false to be returned.
        /// </param>
        /// <param name="promptForMandatory">
        /// If true, unbound mandatory parameters will cause the user to be prompted. If false, unbound
        /// mandatory parameters will cause an exception to be thrown.
        /// </param>
        /// <param name="isPipelineInputExpected">
        /// If true, then only parameters that don't take pipeline input will be prompted for.
        /// If false, any mandatory parameter that has not been specified will be prompted for.
        /// </param>
        /// <param name="missingMandatoryParameters">
        /// Returns the missing mandatory parameters, if any.
        /// </param>
        /// <returns>
        /// True if there are no unbound mandatory parameters. False if there are unbound mandatory parameters
        /// and promptForMandatory if false.
        /// </returns>
        /// <exception cref="ParameterBindingException">
        /// If prompting didn't result in a value for the parameter (only when <paramref name="promptForMandatory"/> is true.)
        /// </exception>
        internal bool HandleUnboundMandatoryParameters(
            int validParameterSetCount,
            bool processMissingMandatory,
            bool promptForMandatory,
            bool isPipelineInputExpected,
            out Collection<MergedCompiledCommandParameter> missingMandatoryParameters)
        {
            bool result = true;

            missingMandatoryParameters = GetMissingMandatoryParameters(validParameterSetCount, isPipelineInputExpected);

            if (missingMandatoryParameters.Count > 0)
            {
                if (processMissingMandatory)
                {
                    // If the host interface wasn't specified or we were instructed not to prmopt, then throw
                    // an exception instead
                    if ((Context.EngineHostInterface == null) || (!promptForMandatory))
                    {
                        Diagnostics.Assert(
                            Context.EngineHostInterface != null,
                            "The EngineHostInterface should never be null");

                        ParameterBinderBase.bindingTracer.WriteLine(
                            "ERROR: host does not support prompting for missing mandatory parameters");

                        string missingParameters = BuildMissingParamsString(missingMandatoryParameters);

                        ParameterBindingException bindingException =
                            new ParameterBindingException(
                                ErrorCategory.InvalidArgument,
                                this.Command.MyInvocation,
                                null,
                                missingParameters,
                                null,
                                null,
                                ParameterBinderStrings.MissingMandatoryParameter,
                                "MissingMandatoryParameter");

                        throw bindingException;
                    }

                    // Create a collection to store the prompt descriptions of unbound mandatory parameters

                    Collection<FieldDescription> fieldDescriptionList = CreatePromptDataStructures(missingMandatoryParameters);

                    Dictionary<string, PSObject> parameters =
                        PromptForMissingMandatoryParameters(
                            fieldDescriptionList,
                            missingMandatoryParameters);

                    using (ParameterBinderBase.bindingTracer.TraceScope(
                        "BIND PROMPTED mandatory parameter args"))
                    {
                        // Now bind any parameters that were retrieved.

                        foreach (KeyValuePair<string, PSObject> entry in parameters)
                        {
                            var argument =
                                CommandParameterInternal.CreateParameterWithArgument(
                                /*parameterAst*/null, entry.Key, "-" + entry.Key + ":",
                                /*argumentAst*/null, entry.Value,
                                false);

                            // Ignore the result since any failure should cause an exception
                            result =
                                BindParameter(argument, ParameterBindingFlags.ShouldCoerceType | ParameterBindingFlags.ThrowOnParameterNotFound);

                            Diagnostics.Assert(
                                result,
                                "Any error in binding the parameter with type coercion should result in an exception");
                        }

                        result = true;
                    }
                }
                else
                {
                    result = false;
                }
            }

            return result;
        }

        private Dictionary<string, PSObject> PromptForMissingMandatoryParameters(
            Collection<FieldDescription> fieldDescriptionList,
            Collection<MergedCompiledCommandParameter> missingMandatoryParameters)
        {
            Dictionary<string, PSObject> parameters = null;

            Exception error = null;

            // Prompt
            try
            {
                ParameterBinderBase.bindingTracer.WriteLine(
                    "PROMPTING for missing mandatory parameters using the host");
                string msg = ParameterBinderStrings.PromptMessage;
                InvocationInfo invoInfo = Command.MyInvocation;
                string caption = StringUtil.Format(ParameterBinderStrings.PromptCaption,
                    invoInfo.MyCommand.Name,
                    invoInfo.PipelinePosition);

                parameters = Context.EngineHostInterface.UI.Prompt(caption, msg, fieldDescriptionList);
            }
            catch (NotImplementedException notImplemented)
            {
                error = notImplemented;
            }
            catch (HostException hostException)
            {
                error = hostException;
            }
            catch (PSInvalidOperationException invalidOperation)
            {
                error = invalidOperation;
            }

            if (error != null)
            {
                ParameterBinderBase.bindingTracer.WriteLine(
                    "ERROR: host does not support prompting for missing mandatory parameters");

                string missingParameters = BuildMissingParamsString(missingMandatoryParameters);

                ParameterBindingException bindingException =
                    new ParameterBindingException(
                        ErrorCategory.InvalidArgument,
                        this.Command.MyInvocation,
                        null,
                        missingParameters,
                        null,
                        null,
                        ParameterBinderStrings.MissingMandatoryParameter,
                        "MissingMandatoryParameter");

                throw bindingException;
            }

            if ((parameters == null) || (parameters.Count == 0))
            {
                ParameterBinderBase.bindingTracer.WriteLine(
                    "ERROR: still missing mandatory parameters after PROMPTING");

                string missingParameters = BuildMissingParamsString(missingMandatoryParameters);

                ParameterBindingException bindingException =
                    new ParameterBindingException(
                        ErrorCategory.InvalidArgument,
                        this.Command.MyInvocation,
                        null,
                        missingParameters,
                        null,
                        null,
                        ParameterBinderStrings.MissingMandatoryParameter,
                        "MissingMandatoryParameter");

                throw bindingException;
            }

            return parameters;
        }

        internal static string BuildMissingParamsString(Collection<MergedCompiledCommandParameter> missingMandatoryParameters)
        {
            StringBuilder missingParameters = new StringBuilder();

            foreach (MergedCompiledCommandParameter missingParameter in missingMandatoryParameters)
            {
                missingParameters.Append(CultureInfo.InvariantCulture, $" {missingParameter.Parameter.Name}");
            }

            return missingParameters.ToString();
        }

        private Collection<FieldDescription> CreatePromptDataStructures(
            Collection<MergedCompiledCommandParameter> missingMandatoryParameters)
        {
            StringBuilder usedHotKeys = new StringBuilder();
            Collection<FieldDescription> fieldDescriptionList = new Collection<FieldDescription>();

            // See if any of the unbound parameters are mandatory

            foreach (MergedCompiledCommandParameter parameter in missingMandatoryParameters)
            {
                ParameterSetSpecificMetadata parameterSetMetadata =
                    parameter.Parameter.GetParameterSetData(_currentParameterSetFlag);

                FieldDescription fDesc = new FieldDescription(parameter.Parameter.Name);

                string helpInfo = null;

                try
                {
                    helpInfo = parameterSetMetadata.GetHelpMessage(Command);
                }
                catch (InvalidOperationException)
                {
                }
                catch (ArgumentException)
                {
                }

                if (!string.IsNullOrEmpty(helpInfo))
                {
                    fDesc.HelpMessage = helpInfo;
                }

                fDesc.SetParameterType(parameter.Parameter.Type);
                fDesc.Label = BuildLabel(parameter.Parameter.Name, usedHotKeys);

                foreach (ValidateArgumentsAttribute vaAttr in parameter.Parameter.ValidationAttributes)
                {
                    fDesc.Attributes.Add(vaAttr);
                }

                foreach (ArgumentTransformationAttribute arAttr in parameter.Parameter.ArgumentTransformationAttributes)
                {
                    fDesc.Attributes.Add(arAttr);
                }

                fDesc.IsMandatory = true;

                fieldDescriptionList.Add(fDesc);
            }

            return fieldDescriptionList;
        }

        /// <summary>
        /// Creates a label with a Hotkey from <paramref name="parameterName"/>. The Hotkey is
        /// <paramref name="parameterName"/>'s first capital character not in <paramref name="usedHotKeys"/>.
        /// If <paramref name="parameterName"/> does not have any capital character, the first lower
        ///  case character is used. The Hotkey is preceded by an ampersand in the label.
        /// </summary>
        /// <param name="parameterName">
        /// The parameter name from which the Hotkey is created
        /// </param>
        /// <param name="usedHotKeys">
        /// A list of used HotKeys
        /// </param>
        /// <returns>
        /// A label made from parameterName with a HotKey indicated by an ampersand
        /// </returns>
        private static string BuildLabel(string parameterName, StringBuilder usedHotKeys)
        {
            Diagnostics.Assert(!string.IsNullOrEmpty(parameterName), "parameterName is not set");
            const char hotKeyPrefix = '&';
            bool built = false;
            StringBuilder label = new StringBuilder(parameterName);
            string usedHotKeysStr = usedHotKeys.ToString();

            for (int i = 0; i < parameterName.Length; i++)
            {
                // try Upper case
                if (char.IsUpper(parameterName[i]) && usedHotKeysStr.Contains(parameterName[i]))
                {
                    label.Insert(i, hotKeyPrefix);
                    usedHotKeys.Append(parameterName[i]);
                    built = true;
                    break;
                }
            }

            if (!built)
            {
                // try Lower case
                for (int i = 0; i < parameterName.Length; i++)
                {
                    if (char.IsLower(parameterName[i]) && usedHotKeysStr.Contains(parameterName[i]))
                    {
                        label.Insert(i, hotKeyPrefix);
                        usedHotKeys.Append(parameterName[i]);
                        built = true;
                        break;
                    }
                }
            }

            if (!built)
            {
                // try non-letters
                for (int i = 0; i < parameterName.Length; i++)
                {
                    if (!char.IsLetter(parameterName[i]) && usedHotKeysStr.Contains(parameterName[i]))
                    {
                        label.Insert(i, hotKeyPrefix);
                        usedHotKeys.Append(parameterName[i]);
                        built = true;
                        break;
                    }
                }
            }

            if (!built)
            {
                // use first char
                label.Insert(0, hotKeyPrefix);
            }

            return label.ToString();
        }

        /// <summary>
        /// Gets the parameter set name for the current parameter set.
        /// </summary>
        internal string CurrentParameterSetName
        {
            get
            {
                string currentParameterSetName = BindableParameters.GetParameterSetName(_currentParameterSetFlag);
                s_tracer.WriteLine("CurrentParameterSetName = {0}", currentParameterSetName);
                return currentParameterSetName;
            }
        }

        /// <summary>
        /// Binds the specified object or its properties to parameters
        /// that accept pipeline input.
        /// </summary>
        /// <param name="inputToOperateOn">
        /// The pipeline object to bind.
        /// </param>
        /// <returns>
        /// True if the pipeline input was bound successfully or there was nothing
        /// to bind, or false if there was an error.
        /// </returns>
        internal bool BindPipelineParameters(PSObject inputToOperateOn)
        {
            bool result;

            try
            {
                using (ParameterBinderBase.bindingTracer.TraceScope(
                    "BIND PIPELINE object to parameters: [{0}]",
                    _commandMetadata.Name))
                {
                    // First run any of the delay bind ScriptBlocks and bind the
                    // result to the appropriate parameter.

                    bool thereWasSomethingToBind;
                    bool invokeScriptResult = InvokeAndBindDelayBindScriptBlock(inputToOperateOn, out thereWasSomethingToBind);

                    bool continueBindingAfterScriptBlockProcessing = !thereWasSomethingToBind || invokeScriptResult;

                    bool bindPipelineParametersResult = false;

                    if (continueBindingAfterScriptBlockProcessing)
                    {
                        // If any of the parameters in the parameter set which are not yet bound
                        // accept pipeline input, process the input object and bind to those
                        // parameters

                        bindPipelineParametersResult = BindPipelineParametersPrivate(inputToOperateOn);
                    }

                    // We are successful at binding the pipeline input if there was a ScriptBlock to
                    // run and it ran successfully or if we successfully bound a parameter based on
                    // the pipeline input.

                    result = (thereWasSomethingToBind && invokeScriptResult) || bindPipelineParametersResult;
                }
            }
            catch (ParameterBindingException)
            {
                // Reset the default values
                // This prevents the last pipeline object from being bound during EndProcessing
                // if it failed some post binding verification step.
                this.RestoreDefaultParameterValues(ParametersBoundThroughPipelineInput);

                // Let the parameter binding errors propagate out
                throw;
            }

            try
            {
                // Now make sure we have latched on to a single parameter set.
                VerifyParameterSetSelected();
            }
            catch (ParameterBindingException)
            {
                // Reset the default values
                // This prevents the last pipeline object from being bound during EndProcessing
                // if it failed some post binding verification step.
                this.RestoreDefaultParameterValues(ParametersBoundThroughPipelineInput);

                throw;
            }

            if (!result)
            {
                // Reset the default values
                // This prevents the last pipeline object from being bound during EndProcessing
                // if it failed some post binding verification step.
                this.RestoreDefaultParameterValues(ParametersBoundThroughPipelineInput);
            }

            return result;
        }

        /// <summary>
        /// Binds the pipeline parameters using the specified input and parameter set.
        /// </summary>
        /// <param name="inputToOperateOn">
        /// The pipeline input to be bound to the parameters.
        /// </param>
        /// <exception cref="ParameterBindingException">
        /// If argument transformation fails.
        /// or
        /// The argument could not be coerced to the appropriate type for the parameter.
        /// or
        /// The parameter argument transformation, prerequisite, or validation failed.
        /// or
        /// If the binding to the parameter fails.
        /// or
        /// If there is a failure resetting values prior to binding from the pipeline
        /// </exception>
        /// <remarks>
        /// The algorithm for binding the pipeline object is as follows. If any
        /// step is successful true gets returned immediately.
        ///
        /// - If parameter supports ValueFromPipeline
        ///     - attempt to bind input value without type coercion
        /// - If parameter supports ValueFromPipelineByPropertyName
        ///     - attempt to bind the value of the property with the matching name without type coercion
        ///
        /// Now see if we have a single valid parameter set and reset the validParameterSets flags as
        /// necessary. If there are still multiple valid parameter sets, then we need to use TypeDistance
        /// to determine which parameters to do type coercion binding on.
        ///
        /// - If parameter supports ValueFromPipeline
        ///     - attempt to bind input value using type coercion
        /// - If parameter support ValueFromPipelineByPropertyName
        ///     - attempt to bind the vlue of the property with the matching name using type coercion
        /// </remarks>
        private bool BindPipelineParametersPrivate(PSObject inputToOperateOn)
        {
            if (ParameterBinderBase.bindingTracer.IsEnabled)
            {
                ConsolidatedString dontuseInternalTypeNames;
                ParameterBinderBase.bindingTracer.WriteLine(
                    "PIPELINE object TYPE = [{0}]",
                    inputToOperateOn == null || inputToOperateOn == AutomationNull.Value
                        ? "null"
                        : ((dontuseInternalTypeNames = inputToOperateOn.InternalTypeNames).Count > 0 && dontuseInternalTypeNames[0] != null)
                              ? dontuseInternalTypeNames[0]
                              : inputToOperateOn.BaseObject.GetType().FullName);

                ParameterBinderBase.bindingTracer.WriteLine("RESTORING pipeline parameter's original values");
            }

            bool result = false;

            // Reset the default values

            this.RestoreDefaultParameterValues(ParametersBoundThroughPipelineInput);

            // Now clear the parameter names from the previous pipeline input

            ParametersBoundThroughPipelineInput.Clear();

            // Now restore the parameter set flags

            _currentParameterSetFlag = _prePipelineProcessingParameterSetFlags;
            uint validParameterSets = _currentParameterSetFlag;
            bool needToPrioritizeOneSpecificParameterSet = _parameterSetToBePrioritizedInPipelineBinding != 0;
            int steps = needToPrioritizeOneSpecificParameterSet ? 2 : 1;

            if (needToPrioritizeOneSpecificParameterSet)
            {
                // _parameterSetToBePrioritizedInPipelineBinding is set, so we are certain that the specified parameter set must be valid,
                // and it's not the only valid parameter set.
                Diagnostics.Assert((_currentParameterSetFlag & _parameterSetToBePrioritizedInPipelineBinding) != 0, "_parameterSetToBePrioritizedInPipelineBinding should be valid if it's set");
                validParameterSets = _parameterSetToBePrioritizedInPipelineBinding;
            }

            for (int i = 0; i < steps; i++)
            {
                for (CurrentlyBinding currentlyBinding = CurrentlyBinding.ValueFromPipelineNoCoercion; currentlyBinding <= CurrentlyBinding.ValueFromPipelineByPropertyNameWithCoercion; ++currentlyBinding)
                {
                    // The parameterBoundForCurrentlyBindingState will be true as long as there is one parameter gets bound, even if it belongs to AllSet
                    bool parameterBoundForCurrentlyBindingState =
                        BindUnboundParametersForBindingState(
                            inputToOperateOn,
                            currentlyBinding,
                            validParameterSets);

                    if (parameterBoundForCurrentlyBindingState)
                    {
                        // Now validate the parameter sets again and update the valid sets.
                        // No need to validate the parameter sets and update the valid sets when dealing with the prioritized parameter set,
                        // this is because the prioritized parameter set is a single set, and when binding succeeds, _currentParameterSetFlag
                        // must be equal to the specific prioritized parameter set.
                        if (!needToPrioritizeOneSpecificParameterSet || i == 1)
                        {
                            ValidateParameterSets(true, true);
                            validParameterSets = _currentParameterSetFlag;
                        }

                        result = true;
                    }
                }

                // Update the validParameterSets after the binding attempt for the prioritized parameter set
                if (needToPrioritizeOneSpecificParameterSet && i == 0)
                {
                    // If the prioritized set can be bound successfully, there is no need to do the second round binding
                    if (_currentParameterSetFlag == _parameterSetToBePrioritizedInPipelineBinding)
                    {
                        break;
                    }

                    validParameterSets = _currentParameterSetFlag & (~_parameterSetToBePrioritizedInPipelineBinding);
                }
            }

            // Now make sure we only have one valid parameter set
            // Note, this will throw if we have more than one.

            ValidateParameterSets(false, true);

            if (!DefaultParameterBindingInUse)
            {
                ApplyDefaultParameterBinding("PIPELINE BIND", false);
            }

            return result;
        }

        private bool BindUnboundParametersForBindingState(
            PSObject inputToOperateOn,
            CurrentlyBinding currentlyBinding,
            uint validParameterSets)
        {
            bool aParameterWasBound = false;

            // First check to see if the default parameter set has been defined and if it
            // is still valid.

            uint defaultParameterSetFlag = _commandMetadata.DefaultParameterSetFlag;

            if (defaultParameterSetFlag != 0 && (validParameterSets & defaultParameterSetFlag) != 0)
            {
                // Since we have a default parameter set and it is still valid, give preference to the
                // parameters in the default set.

                aParameterWasBound =
                    BindUnboundParametersForBindingStateInParameterSet(
                        inputToOperateOn,
                        currentlyBinding,
                        defaultParameterSetFlag);

                if (!aParameterWasBound)
                {
                    validParameterSets &= ~(defaultParameterSetFlag);
                }
            }

            if (!aParameterWasBound)
            {
                // Since nothing was bound for the default parameter set, try all
                // the other parameter sets that are still valid.

                aParameterWasBound =
                    BindUnboundParametersForBindingStateInParameterSet(
                        inputToOperateOn,
                        currentlyBinding,
                        validParameterSets);
            }

            s_tracer.WriteLine("aParameterWasBound = {0}", aParameterWasBound);
            return aParameterWasBound;
        }

        private bool BindUnboundParametersForBindingStateInParameterSet(
            PSObject inputToOperateOn,
            CurrentlyBinding currentlyBinding,
            uint validParameterSets)
        {
            bool aParameterWasBound = false;

            // For all unbound parameters in the parameter set, see if we can bind
            // from the input object directly from pipeline without type coercion.
            //
            // We loop the unbound parameters in reversed order, so that we can move
            // items from the unboundParameters collection to the boundParameters
            // collection as we process, without the need to make a copy of the
            // unboundParameters collection.
            //
            // We used to make a copy of UnboundParameters and loop from the head of the
            // list. Now we are processing the unbound parameters from the end of the list.
            // This change should NOT be a breaking change. The 'validParameterSets' in
            // this method never changes, so no matter we start from the head or the end of
            // the list, every unbound parameter in the list that takes pipeline input and
            // satisfy the 'validParameterSets' will be bound. If parameters from more than
            // one sets got bound, then "parameter set cannot be resolved" error will be thrown,
            // which is expected.

            for (int i = UnboundParameters.Count - 1; i >= 0; i--)
            {
                var parameter = UnboundParameters[i];

                // if the parameter is never a pipeline parameter, don't consider it
                if (!parameter.Parameter.IsPipelineParameterInSomeParameterSet)
                    continue;

                // if the parameter is not in the specified parameter set, don't consider it
                if ((validParameterSets & parameter.Parameter.ParameterSetFlags) == 0 &&
                    !parameter.Parameter.IsInAllSets)
                {
                    continue;
                }

                // Get the appropriate parameter set data
                var parameterSetData = parameter.Parameter.GetMatchingParameterSetData(validParameterSets);

                bool bindResult = false;

                foreach (ParameterSetSpecificMetadata parameterSetMetadata in parameterSetData)
                {
                    // In the first phase we try to bind the value from the pipeline without
                    // type coercion

                    if (currentlyBinding == CurrentlyBinding.ValueFromPipelineNoCoercion &&
                        parameterSetMetadata.ValueFromPipeline)
                    {
                        bindResult = BindValueFromPipeline(inputToOperateOn, parameter, ParameterBindingFlags.None);
                    }
                    // In the next phase we try binding the value from the pipeline by matching
                    // the property name
                    else if (currentlyBinding == CurrentlyBinding.ValueFromPipelineByPropertyNameNoCoercion &&
                        parameterSetMetadata.ValueFromPipelineByPropertyName &&
                        inputToOperateOn != null)
                    {
                        bindResult = BindValueFromPipelineByPropertyName(inputToOperateOn, parameter, ParameterBindingFlags.None);
                    }
                    // The third step is to attempt to bind the value from the pipeline with
                    // type coercion.
                    else if (currentlyBinding == CurrentlyBinding.ValueFromPipelineWithCoercion &&
                        parameterSetMetadata.ValueFromPipeline)
                    {
                        bindResult = BindValueFromPipeline(inputToOperateOn, parameter, ParameterBindingFlags.ShouldCoerceType);
                    }
                    // The final step is to attempt to bind the value from the pipeline by matching
                    // the property name
                    else if (currentlyBinding == CurrentlyBinding.ValueFromPipelineByPropertyNameWithCoercion &&
                        parameterSetMetadata.ValueFromPipelineByPropertyName &&
                        inputToOperateOn != null)
                    {
                        bindResult = BindValueFromPipelineByPropertyName(inputToOperateOn, parameter, ParameterBindingFlags.ShouldCoerceType);
                    }

                    if (bindResult)
                    {
                        aParameterWasBound = true;
                        break;
                    }
                }
            }

            return aParameterWasBound;
        }

        private bool BindValueFromPipeline(
            PSObject inputToOperateOn,
            MergedCompiledCommandParameter parameter,
            ParameterBindingFlags flags)
        {
            bool bindResult = false;

            // Attempt binding the value from the pipeline
            // without type coercion

            ParameterBinderBase.bindingTracer.WriteLine(
                ((flags & ParameterBindingFlags.ShouldCoerceType) != 0) ?
                    "Parameter [{0}] PIPELINE INPUT ValueFromPipeline WITH COERCION" :
                    "Parameter [{0}] PIPELINE INPUT ValueFromPipeline NO COERCION",
                parameter.Parameter.Name);

            ParameterBindingException parameterBindingException = null;
            try
            {
                bindResult = BindPipelineParameter(inputToOperateOn, parameter, flags);
            }
            catch (ParameterBindingArgumentTransformationException e)
            {
                PSInvalidCastException invalidCast;
                if (e.InnerException is ArgumentTransformationMetadataException)
                {
                    invalidCast = e.InnerException.InnerException as PSInvalidCastException;
                }
                else
                {
                    invalidCast = e.InnerException as PSInvalidCastException;
                }

                if (invalidCast == null)
                {
                    parameterBindingException = e;
                }
                // Just ignore and continue;
                bindResult = false;
            }
            catch (ParameterBindingValidationException e)
            {
                parameterBindingException = e;
            }
            catch (ParameterBindingParameterDefaultValueException e)
            {
                parameterBindingException = e;
            }
            catch (ParameterBindingException)
            {
                // Just ignore and continue;
                bindResult = false;
            }

            if (parameterBindingException != null)
            {
                if (!DefaultParameterBindingInUse)
                {
                    throw parameterBindingException;
                }
                else
                {
                    ThrowElaboratedBindingException(parameterBindingException);
                }
            }

            return bindResult;
        }

        private bool BindValueFromPipelineByPropertyName(
            PSObject inputToOperateOn,
            MergedCompiledCommandParameter parameter,
            ParameterBindingFlags flags)
        {
            bool bindResult = false;

            ParameterBinderBase.bindingTracer.WriteLine(
                ((flags & ParameterBindingFlags.ShouldCoerceType) != 0) ?
                    "Parameter [{0}] PIPELINE INPUT ValueFromPipelineByPropertyName WITH COERCION" :
                    "Parameter [{0}] PIPELINE INPUT ValueFromPipelineByPropertyName NO COERCION",
                parameter.Parameter.Name);

            PSMemberInfo member = inputToOperateOn.Properties[parameter.Parameter.Name];

            if (member == null)
            {
                // Since a member matching the name of the parameter wasn't found,
                // check the aliases.

                foreach (string alias in parameter.Parameter.Aliases)
                {
                    member = inputToOperateOn.Properties[alias];

                    if (member != null)
                    {
                        break;
                    }
                }
            }

            if (member != null)
            {
                ParameterBindingException parameterBindingException = null;
                try
                {
                    bindResult =
                        BindPipelineParameter(
                            member.Value,
                            parameter,
                            flags);
                }
                catch (ParameterBindingArgumentTransformationException e)
                {
                    parameterBindingException = e;
                }
                catch (ParameterBindingValidationException e)
                {
                    parameterBindingException = e;
                }
                catch (ParameterBindingParameterDefaultValueException e)
                {
                    parameterBindingException = e;
                }
                catch (ParameterBindingException)
                {
                    // Just ignore and continue;
                    bindResult = false;
                }

                if (parameterBindingException != null)
                {
                    if (!DefaultParameterBindingInUse)
                    {
                        throw parameterBindingException;
                    }
                    else
                    {
                        ThrowElaboratedBindingException(parameterBindingException);
                    }
                }
            }

            return bindResult;
        }

        /// <summary>
        /// Used for defining the state of the binding state machine.
        /// </summary>
        private enum CurrentlyBinding
        {
            ValueFromPipelineNoCoercion = 0,
            ValueFromPipelineByPropertyNameNoCoercion = 1,
            ValueFromPipelineWithCoercion = 2,
            ValueFromPipelineByPropertyNameWithCoercion = 3
        }

        /// <summary>
        /// Invokes any delay bind script blocks and binds the resulting value
        /// to the appropriate parameter.
        /// </summary>
        /// <param name="inputToOperateOn">
        /// The input to the script block.
        /// </param>
        /// <param name="thereWasSomethingToBind">
        /// Returns True if there was a ScriptBlock to invoke and bind, or false if there
        /// are no ScriptBlocks to invoke.
        /// </param>
        /// <returns>
        /// True if the binding succeeds, or false otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// if <paramref name="inputToOperateOn"/> is null.
        /// </exception>
        /// <exception cref="ParameterBindingException">
        /// If execution of the script block throws an exception or if it doesn't produce
        /// any output.
        /// </exception>
        private bool InvokeAndBindDelayBindScriptBlock(PSObject inputToOperateOn, out bool thereWasSomethingToBind)
        {
            thereWasSomethingToBind = false;
            bool result = true;

            // NOTE: we are not doing backup and restore of default parameter
            // values here.  It is not needed because each script block will be
            // invoked and each delay bind parameter bound for each pipeline object.
            // This is unlike normal pipeline object processing which may bind
            // different parameters depending on the type of the incoming pipeline
            // object.

            // Loop through each of the delay bind script blocks and invoke them.
            // Bind the result to the associated parameter

            foreach (KeyValuePair<MergedCompiledCommandParameter, DelayedScriptBlockArgument> delayedScriptBlock in _delayBindScriptBlocks)
            {
                thereWasSomethingToBind = true;

                CommandParameterInternal argument = delayedScriptBlock.Value._argument;
                MergedCompiledCommandParameter parameter = delayedScriptBlock.Key;

                ScriptBlock script = argument.ArgumentValue as ScriptBlock;

                Diagnostics.Assert(
                    script != null,
                    "An argument should only be put in the delayBindScriptBlocks collection if it is a ScriptBlock");

                Collection<PSObject> output = null;

                Exception error = null;
                using (ParameterBinderBase.bindingTracer.TraceScope(
                    "Invoking delay-bind ScriptBlock"))
                {
                    if (delayedScriptBlock.Value._parameterBinder == this)
                    {
                        try
                        {
                            output = script.DoInvoke(inputToOperateOn, inputToOperateOn, Array.Empty<object>());
                            delayedScriptBlock.Value._evaluatedArgument = output;
                        }
                        catch (RuntimeException runtimeException)
                        {
                            error = runtimeException;
                        }
                    }
                    else
                    {
                        output = delayedScriptBlock.Value._evaluatedArgument;
                    }
                }

                if (error != null)
                {
                    ParameterBindingException bindingException =
                        new ParameterBindingException(
                            error,
                            ErrorCategory.InvalidArgument,
                            this.Command.MyInvocation,
                            GetErrorExtent(argument),
                            parameter.Parameter.Name,
                            null,
                            null,
                            ParameterBinderStrings.ScriptBlockArgumentInvocationFailed,
                            "ScriptBlockArgumentInvocationFailed",
                            error.Message);

                    throw bindingException;
                }

                if (output == null || output.Count == 0)
                {
                    ParameterBindingException bindingException =
                        new ParameterBindingException(
                            null,
                            ErrorCategory.InvalidArgument,
                            this.Command.MyInvocation,
                            GetErrorExtent(argument),
                            parameter.Parameter.Name,
                            null,
                            null,
                            ParameterBinderStrings.ScriptBlockArgumentNoOutput,
                            "ScriptBlockArgumentNoOutput");

                    throw bindingException;
                }

                // Check the output.  If it is only a single value, just pass the single value,
                // if not, pass in the whole collection.

                object newValue = output;
                if (output.Count == 1)
                {
                    newValue = output[0];
                }

                // Create a new CommandParameterInternal for the output of the script block.
                var newArgument = CommandParameterInternal.CreateParameterWithArgument(
                    argument.ParameterAst, argument.ParameterName, "-" + argument.ParameterName + ":",
                    argument.ArgumentAst, newValue,
                    false);

                if (!BindParameter(newArgument, parameter, ParameterBindingFlags.ShouldCoerceType))
                {
                    result = false;
                }
            }

            return result;
        }

        /// <summary>
        /// Determines the number of valid parameter sets based on the valid parameter
        /// set flags.
        /// </summary>
        /// <param name="parameterSetFlags">
        /// The valid parameter set flags.
        /// </param>
        /// <returns>
        /// The number of valid parameter sets in the parameterSetFlags.
        /// </returns>
        private static int ValidParameterSetCount(uint parameterSetFlags)
        {
            int result = 0;

            if (parameterSetFlags == uint.MaxValue)
            {
                result = 1;
            }
            else
            {
                while (parameterSetFlags != 0)
                {
                    result += (int)(parameterSetFlags & 0x1);
                    parameterSetFlags >>= 1;
                }
            }

            return result;
        }

        #endregion helper_methods

        #region private_members

        /// <summary>
        /// This method gets a backup of the default value of a parameter.
        /// Derived classes may override this method to get the default parameter
        /// value in a different way.
        /// </summary>
        /// <param name="name">
        /// The name of the parameter to get the default value of.
        /// </param>
        /// <returns>
        /// The value of the parameter specified by name.
        /// </returns>
        /// <exception cref="ParameterBindingParameterDefaultValueException">
        /// If the parameter binder encounters an error getting the default value.
        /// </exception>
        internal object GetDefaultParameterValue(string name)
        {
            MergedCompiledCommandParameter matchingParameter =
                BindableParameters.GetMatchingParameter(
                    name,
                    false,
                    true,
                    null);

            object result = null;

            try
            {
                switch (matchingParameter.BinderAssociation)
                {
                    case ParameterBinderAssociation.DeclaredFormalParameters:
                        result = DefaultParameterBinder.GetDefaultParameterValue(name);
                        break;

                    case ParameterBinderAssociation.CommonParameters:
                        result = CommonParametersBinder.GetDefaultParameterValue(name);
                        break;

                    case ParameterBinderAssociation.ShouldProcessParameters:
                        result = ShouldProcessParametersBinder.GetDefaultParameterValue(name);
                        break;

                    case ParameterBinderAssociation.DynamicParameters:
                        if (_dynamicParameterBinder != null)
                        {
                            result = _dynamicParameterBinder.GetDefaultParameterValue(name);
                        }

                        break;
                }
            }
            catch (GetValueException getValueException)
            {
                ParameterBindingParameterDefaultValueException bindingError =
                    new ParameterBindingParameterDefaultValueException(
                        getValueException,
                        ErrorCategory.ReadError,
                        this.Command.MyInvocation,
                        null,
                        name,
                        null,
                        null,
                        "ParameterBinderStrings",
                        "GetDefaultValueFailed",
                        getValueException.Message);

                throw bindingError;
            }

            return result;
        }

        /// <summary>
        /// Gets or sets the command that this parameter binder controller
        /// will bind parameters to.
        /// </summary>
        internal Cmdlet Command { get; }

        #region DefaultParameterBindingStructures

        /// <summary>
        /// The separator used in GetDefaultParameterValuePairs function.
        /// </summary>
        private const string Separator = ":::";

        // Hold all aliases of the current cmdlet
        private List<string> _aliasList;
        // Method GetDefaultParameterValuePairs() will be invoked twice, one time before the Named Bind,
        // one time after Dynamic Bind. We don't want the same warning message to be written out twice.
        // Put the key(in case the key format is invalid), or cmdletName+separator+parameterName(in case
        // setting resolves to multiple parameters or multiple different values are assigned to the same
        // parameter) in warningSet when the corresponding warnings are written out, so they won't get
        // written out the second time GetDefaultParameterValuePairs() is called.
        private readonly HashSet<string> _warningSet = new HashSet<string>();

        // Hold all user defined default parameter values
        private Dictionary<MergedCompiledCommandParameter, object> _allDefaultParameterValuePairs;
        private bool _useDefaultParameterBinding = true;

        #endregion DefaultParameterBindingStructures

        private uint _parameterSetToBePrioritizedInPipelineBinding = 0;

        /// <summary>
        /// The cmdlet metadata.
        /// </summary>
        private readonly CommandMetadata _commandMetadata;

        /// <summary>
        /// THe command runtime object for this cmdlet.
        /// </summary>
        private readonly MshCommandRuntime _commandRuntime;

        /// <summary>
        /// Keep the obsolete parameter warnings generated from parameter binding.
        /// </summary>
        internal List<WarningRecord> ObsoleteParameterWarningList { get; private set; }

        /// <summary>
        /// Keep names of the parameters for which we have generated obsolete warning messages.
        /// </summary>
        private HashSet<string> BoundObsoleteParameterNames
        {
            get
            {
                return _boundObsoleteParameterNames ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private HashSet<string> _boundObsoleteParameterNames;

        /// <summary>
        /// The parameter binder for the dynamic parameters. Currently this
        /// can be either a ReflectionParameterBinder or a RuntimeDefinedParameterBinder.
        /// </summary>
        private ParameterBinderBase _dynamicParameterBinder;

        /// <summary>
        /// The parameter binder for the ShouldProcess parameters.
        /// </summary>
        internal ReflectionParameterBinder ShouldProcessParametersBinder
        {
            get
            {
                if (_shouldProcessParameterBinder == null)
                {
                    // Construct a new instance of the should process parameters object
                    ShouldProcessParameters shouldProcessParameters = new ShouldProcessParameters(_commandRuntime);

                    // Create reflection binder for this object

                    _shouldProcessParameterBinder =
                        new ReflectionParameterBinder(
                            shouldProcessParameters,
                            this.Command,
                            this.CommandLineParameters);
                }

                return _shouldProcessParameterBinder;
            }
        }

        private ReflectionParameterBinder _shouldProcessParameterBinder;

        /// <summary>
        /// The parameter binder for the Paging parameters.
        /// </summary>
        internal ReflectionParameterBinder PagingParametersBinder
        {
            get
            {
                if (_pagingParameterBinder == null)
                {
                    // Construct a new instance of the should process parameters object
                    PagingParameters pagingParameters = new PagingParameters(_commandRuntime);

                    // Create reflection binder for this object

                    _pagingParameterBinder =
                        new ReflectionParameterBinder(
                            pagingParameters,
                            this.Command,
                            this.CommandLineParameters);
                }

                return _pagingParameterBinder;
            }
        }

        private ReflectionParameterBinder _pagingParameterBinder;

        /// <summary>
        /// The parameter binder for the Transactions parameters.
        /// </summary>
        internal ReflectionParameterBinder TransactionParametersBinder
        {
            get
            {
                if (_transactionParameterBinder == null)
                {
                    // Construct a new instance of the transactions parameters object
                    TransactionParameters transactionParameters = new TransactionParameters(_commandRuntime);

                    // Create reflection binder for this object

                    _transactionParameterBinder =
                        new ReflectionParameterBinder(
                            transactionParameters,
                            this.Command,
                            this.CommandLineParameters);
                }

                return _transactionParameterBinder;
            }
        }

        private ReflectionParameterBinder _transactionParameterBinder;

        /// <summary>
        /// The parameter binder for the CommonParameters.
        /// </summary>
        internal ReflectionParameterBinder CommonParametersBinder
        {
            get
            {
                if (_commonParametersBinder == null)
                {
                    // Construct a new instance of the user feedback parameters object
                    CommonParameters commonParameters = new CommonParameters(_commandRuntime);

                    // Create reflection binder for this object

                    _commonParametersBinder =
                        new ReflectionParameterBinder(
                            commonParameters,
                            this.Command,
                            this.CommandLineParameters);
                }

                return _commonParametersBinder;
            }
        }

        private ReflectionParameterBinder _commonParametersBinder;

        private sealed class DelayedScriptBlockArgument
        {
            // Remember the parameter binder so we know when to invoke the script block
            // and when to use the evaluated argument.
            internal CmdletParameterBinderController _parameterBinder;
            internal CommandParameterInternal _argument;
            internal Collection<PSObject> _evaluatedArgument;

            public override string ToString()
            {
                return _argument.ArgumentValue.ToString();
            }
        }

        /// <summary>
        /// This dictionary is used to contain the arguments that were passed in as ScriptBlocks
        /// but the parameter isn't a ScriptBlock. So we have to wait to bind the parameter
        /// until there is a pipeline object available to invoke the ScriptBlock with.
        /// </summary>
        private readonly Dictionary<MergedCompiledCommandParameter, DelayedScriptBlockArgument> _delayBindScriptBlocks =
            new Dictionary<MergedCompiledCommandParameter, DelayedScriptBlockArgument>();

        /// <summary>
        /// A collection of the default values of the parameters.
        /// </summary>
        private readonly Dictionary<string, CommandParameterInternal> _defaultParameterValues =
            new Dictionary<string, CommandParameterInternal>(StringComparer.OrdinalIgnoreCase);

        #endregion private_members

        /// <summary>
        /// Binds the specified value to the specified parameter.
        /// </summary>
        /// <param name="parameterValue">
        /// The value to bind to the parameter
        /// </param>
        /// <param name="parameter">
        /// The parameter to bind the value to.
        /// </param>
        /// <param name="flags">
        /// Parameter binding flags for type coercion and validation.
        /// </param>
        /// <returns>
        /// True if the parameter was successfully bound. False if <paramref name="flags"/>
        /// specifies no coercion and the type does not match the parameter type.
        /// </returns>
        /// <exception cref="ParameterBindingParameterDefaultValueException">
        /// If the parameter binder encounters an error getting the default value.
        /// </exception>
        private bool BindPipelineParameter(
            object parameterValue,
            MergedCompiledCommandParameter parameter,
            ParameterBindingFlags flags)
        {
            bool result = false;

            if (parameterValue != AutomationNull.Value)
            {
                s_tracer.WriteLine("Adding PipelineParameter name={0}; value={1}",
                                 parameter.Parameter.Name, parameterValue ?? "null");

                // Backup the default value
                BackupDefaultParameter(parameter);

                // Now bind the new value
                CommandParameterInternal param = CommandParameterInternal.CreateParameterWithArgument(
                    /*parameterAst*/null, parameter.Parameter.Name, "-" + parameter.Parameter.Name + ":",
                    /*argumentAst*/null, parameterValue,
                    false);

                flags &= ~ParameterBindingFlags.DelayBindScriptBlock;
                result = BindParameter(_currentParameterSetFlag, param, parameter, flags);

                if (result)
                {
                    // Now make sure to remember that the default value needs to be restored
                    // if we get another pipeline object
                    ParametersBoundThroughPipelineInput.Add(parameter);
                }
            }

            return result;
        }

        protected override void SaveDefaultScriptParameterValue(string name, object value)
        {
            _defaultParameterValues.Add(name,
                CommandParameterInternal.CreateParameterWithArgument(
                    /*parameterAst*/null, name, "-" + name + ":",
                    /*argumentAst*/null, value,
                    false));
        }

        /// <summary>
        /// Backs up the specified parameter value by calling the GetDefaultParameterValue
        /// abstract method.
        ///
        /// This method is called when binding a parameter value that came from a pipeline
        /// object.
        /// </summary>
        /// <exception cref="ParameterBindingParameterDefaultValueException">
        /// If the parameter binder encounters an error getting the default value.
        /// </exception>
        private void BackupDefaultParameter(MergedCompiledCommandParameter parameter)
        {
            if (!_defaultParameterValues.ContainsKey(parameter.Parameter.Name))
            {
                object defaultParameterValue = GetDefaultParameterValue(parameter.Parameter.Name);
                _defaultParameterValues.Add(
                    parameter.Parameter.Name,
                    CommandParameterInternal.CreateParameterWithArgument(
                        /*parameterAst*/null, parameter.Parameter.Name, "-" + parameter.Parameter.Name + ":",
                        /*argumentAst*/null, defaultParameterValue,
                        false));
            }
        }

        /// <summary>
        /// Replaces the values of the parameters with their initial value for the
        /// parameters specified.
        /// </summary>
        /// <param name="parameters">
        /// The parameters that should have their default values restored.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="parameters"/> is null.
        /// </exception>
        private void RestoreDefaultParameterValues(IEnumerable<MergedCompiledCommandParameter> parameters)
        {
            if (parameters == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(parameters));
            }

            // Get all the matching arguments from the defaultParameterValues collection
            // and bind those that had parameters that were bound via pipeline input

            foreach (MergedCompiledCommandParameter parameter in parameters)
            {
                if (parameter == null)
                {
                    continue;
                }

                CommandParameterInternal argumentToBind = null;

                // If the argument was found then bind it to the parameter
                // and manage the bound and unbound parameter list

                if (_defaultParameterValues.TryGetValue(parameter.Parameter.Name, out argumentToBind))
                {
                    // Don't go through the normal binding routine to run data generation,
                    // type coercion, validation, or prerequisites since we know the
                    // type is already correct, and we don't want data generation to
                    // run when resetting the default value.

                    Exception error = null;
                    try
                    {
                        // We shouldn't have to coerce the type here so its
                        // faster to pass false

                        bool bindResult = RestoreParameter(argumentToBind, parameter);

                        Diagnostics.Assert(
                            bindResult,
                            "Restoring the default value should not require type coercion");
                    }
                    catch (SetValueException setValueException)
                    {
                        error = setValueException;
                    }

                    if (error != null)
                    {
                        Type specifiedType = argumentToBind.ArgumentValue?.GetType();
                        ParameterBindingException bindingException =
                            new ParameterBindingException(
                                error,
                                ErrorCategory.WriteError,
                                this.InvocationInfo,
                                GetErrorExtent(argumentToBind),
                                parameter.Parameter.Name,
                                parameter.Parameter.Type,
                                specifiedType,
                                ParameterBinderStrings.ParameterBindingFailed,
                                "ParameterBindingFailed",
                                error.Message);
                        throw bindingException;
                    }

                    // Since the parameter was returned to its original value,
                    // ensure that it is not in the boundParameters list but
                    // is in the unboundParameters list

                    BoundParameters.Remove(parameter.Parameter.Name);

                    if (!UnboundParameters.Contains(parameter))
                    {
                        UnboundParameters.Add(parameter);
                    }

                    BoundArguments.Remove(parameter.Parameter.Name);
                }
                else
                {
                    // Since the parameter was not reset, ensure that the parameter
                    // is in the bound parameters list and not in the unbound
                    // parameters list

                    if (!BoundParameters.ContainsKey(parameter.Parameter.Name))
                    {
                        BoundParameters.Add(parameter.Parameter.Name, parameter);
                    }

                    // Ensure the parameter is not in the unboundParameters list

                    UnboundParameters.Remove(parameter);
                }
            }
        }
    }

    /// <summary>
    /// A versionable hashtable, so the caching of UserInput -> ParameterBindingResult will work.
    /// </summary>
    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Justification = "DefaultParameterDictionary will only be used for $PSDefaultParameterValues.")]
    public sealed class DefaultParameterDictionary : Hashtable
    {
        private bool _isChanged;

        /// <summary>
        /// Check to see if the hashtable has been changed since last check.
        /// </summary>
        /// <returns>True for changed; false for not changed.</returns>
        public bool ChangeSinceLastCheck()
        {
            bool ret = _isChanged;
            _isChanged = false;
            return ret;
        }

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DefaultParameterDictionary()
            : base(StringComparer.OrdinalIgnoreCase)
        {
            _isChanged = true;
        }

        /// <summary>
        /// Constructor takes a hash table.
        /// </summary>
        /// <remarks>
        /// Check for the keys' formats and make it versionable
        /// </remarks>
        /// <param name="dictionary">A hashtable instance.</param>
        public DefaultParameterDictionary(IDictionary dictionary)
            : this()
        {
            if (dictionary == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(dictionary));
            }
            // Contains keys that are in bad format. For every bad format key, we should write out a warning message
            // the first time we encounter it, and remove it from the $PSDefaultParameterValues
            var keysInBadFormat = new List<object>();

            foreach (DictionaryEntry entry in dictionary)
            {
                var entryKey = entry.Key as string;
                if (entryKey != null)
                {
                    string key = entryKey.Trim();
                    string cmdletName = null;
                    string parameterName = null;
                    bool isSpecialKey = false; // The key is 'Disabled'

                    // The key is not with valid format
                    if (!CheckKeyIsValid(key, ref cmdletName, ref parameterName))
                    {
                        isSpecialKey = key.Equals("Disabled", StringComparison.OrdinalIgnoreCase);
                        if (!isSpecialKey)
                        {
                            keysInBadFormat.Add(entryKey);
                            continue;
                        }
                    }

                    Diagnostics.Assert(isSpecialKey || (cmdletName != null && parameterName != null), "The cmdletName and parameterName should be set in CheckKeyIsValid");
                    if (keysInBadFormat.Count == 0 && !base.ContainsKey(key))
                    {
                        base.Add(key, entry.Value);
                    }
                }
                else
                {
                    keysInBadFormat.Add(entry.Key);
                }
            }

            var keysInError = new StringBuilder();
            foreach (object badFormatKey in keysInBadFormat)
            {
                keysInError.Append(badFormatKey.ToString() + ", ");
            }

            if (keysInError.Length > 0)
            {
                keysInError.Remove(keysInError.Length - 2, 2);
                string resourceString = keysInBadFormat.Count > 1
                                            ? ParameterBinderStrings.MultipleKeysInBadFormat
                                            : ParameterBinderStrings.SingleKeyInBadFormat;
                throw PSTraceSource.NewInvalidOperationException(resourceString, keysInError);
            }
        }

        #endregion Constructor

        /// <summary>
        /// Override Contains.
        /// </summary>
        public override bool Contains(object key)
        {
            return this.ContainsKey(key);
        }

        /// <summary>
        /// Override ContainsKey.
        /// </summary>
        public override bool ContainsKey(object key)
        {
            if (key == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(key));
            }

            if (key is not string strKey)
            {
                return false;
            }

            string keyAfterTrim = strKey.Trim();
            return base.ContainsKey(keyAfterTrim);
        }

        /// <summary>
        /// Override the Add to check for key's format and make it versionable.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public override void Add(object key, object value)
        {
            AddImpl(key, value, isSelfIndexing: false);
        }

        /// <summary>
        /// Actual implementation for Add.
        /// </summary>
        private void AddImpl(object key, object value, bool isSelfIndexing)
        {
            if (key == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(key));
            }

            if (key is not string strKey)
            {
                throw PSTraceSource.NewArgumentException(nameof(key), ParameterBinderStrings.StringValueKeyExpected, key, key.GetType().FullName);
            }

            string keyAfterTrim = strKey.Trim();
            string cmdletName = null;
            string parameterName = null;

            if (base.ContainsKey(keyAfterTrim))
            {
                if (isSelfIndexing)
                {
                    _isChanged = true;
                    base[keyAfterTrim] = value;
                    return;
                }

                throw PSTraceSource.NewArgumentException(nameof(key), ParameterBinderStrings.KeyAlreadyAdded, key);
            }

            if (!CheckKeyIsValid(keyAfterTrim, ref cmdletName, ref parameterName))
            {
                // The key is not in valid format
                if (!keyAfterTrim.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
                {
                    throw PSTraceSource.NewInvalidOperationException(ParameterBinderStrings.SingleKeyInBadFormat, key);
                }
            }

            _isChanged = true;
            base.Add(keyAfterTrim, value);
        }

        /// <summary>
        /// Override the indexing to check for key's format and make it versionable.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override object this[object key]
        {
            get
            {
                if (key == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(key));
                }

                if (key is not string strKey)
                {
                    return null;
                }

                string keyAfterTrim = strKey.Trim();
                return base[keyAfterTrim];
            }

            set
            {
                AddImpl(key, value, isSelfIndexing: true);
            }
        }

        /// <summary>
        /// Override the Remove to make it versionable.
        /// </summary>
        /// <param name="key">Key.</param>
        public override void Remove(object key)
        {
            if (key == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(key));
            }

            if (key is not string strKey)
            {
                return;
            }

            string keyAfterTrim = strKey.Trim();
            if (base.ContainsKey(keyAfterTrim))
            {
                base.Remove(keyAfterTrim);
                _isChanged = true;
            }
        }

        /// <summary>
        /// Override the Clear to make it versionable.
        /// </summary>
        public override void Clear()
        {
            base.Clear();
            _isChanged = true;
        }

        #region KeyValidation

        /// <summary>
        /// Check if the key is in valid format. If it is, get the cmdlet name and parameter name.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cmdletName"></param>
        /// <param name="parameterName"></param>
        /// <returns>Return true if the key is valid, false if not.</returns>
        internal static bool CheckKeyIsValid(string key, ref string cmdletName, ref string parameterName)
        {
            if (key == string.Empty)
            {
                return false;
            }

            // The index returned should point to the separator or a character that is before the separator
            int index = GetValueToken(0, key, ref cmdletName, true);
            if (index == -1)
            {
                return false;
            }

            // The index returned should point to the first non-whitespace character, and it should be the separator
            index = SkipWhiteSpace(index, key);
            if (index == -1 || key[index] != ':')
            {
                return false;
            }

            // The index returned should point to the first non-whitespace character after the separator
            index = SkipWhiteSpace(index + 1, key);
            if (index == -1)
            {
                return false;
            }

            // The index returned should point to the last character in key
            index = GetValueToken(index, key, ref parameterName, false);
            if (index == -1 || index != key.Length)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get the cmdlet name and the parameter name.
        /// </summary>
        /// <param name="index">Point to a non-whitespace character.</param>
        /// <param name="key">The key to iterate over.</param>
        /// <param name="name"></param>
        /// <param name="getCmdletName">Specify whether to get the cmdlet name or parameter name.</param>
        /// <returns>
        /// For cmdletName:
        /// When the name is enclosed by quotes, the index returned should be the index of the character right after the second quote;
        /// When the name is not enclosed by quotes, the index returned should be the index of the separator;
        ///
        /// For parameterName:
        /// When the name is enclosed by quotes, the index returned should be the index of the second quote plus 1 (the length of the key if the key is in a valid format);
        /// When the name is not enclosed by quotes, the index returned should be the length of the key.
        /// </returns>
        private static int GetValueToken(int index, string key, ref string name, bool getCmdletName)
        {
            char quoteChar = '\0';
            if (key[index].IsSingleQuote() || key[index].IsDoubleQuote())
            {
                quoteChar = key[index];
                index++;
            }

            StringBuilder builder = new StringBuilder(string.Empty);
            for (; index < key.Length; index++)
            {
                if (quoteChar != '\0')
                {
                    if ((quoteChar.IsSingleQuote() && key[index].IsSingleQuote()) ||
                        (quoteChar.IsDoubleQuote() && key[index].IsDoubleQuote()))
                    {
                        name = builder.ToString().Trim();
                        // Make the index point to the character right after the quote
                        return name.Length == 0 ? -1 : index + 1;
                    }

                    builder.Append(key[index]);
                    continue;
                }

                if (getCmdletName)
                {
                    if (key[index] != ':')
                    {
                        builder.Append(key[index]);
                        continue;
                    }

                    name = builder.ToString().Trim();
                    return name.Length == 0 ? -1 : index;
                }
                else
                {
                    builder.Append(key[index]);
                }
            }

            if (!getCmdletName && quoteChar == '\0')
            {
                name = builder.ToString().Trim();
                Diagnostics.Assert(name.Length > 0, "name should not be empty at this point");
                return index;
            }

            return -1;
        }

        /// <summary>
        /// Skip whitespace characters.
        /// </summary>
        /// <param name="index">Start index.</param>
        /// <param name="key">The string to iterate over.</param>
        /// <returns>
        /// Return -1 if we reach the end of the key, otherwise return the index of the first
        /// non-whitespace character we encounter.
        /// </returns>
        private static int SkipWhiteSpace(int index, string key)
        {
            for (; index < key.Length; index++)
            {
                if (key[index].IsWhitespace() || key[index] == '\r' || key[index] == '\n')
                    continue;
                return index;
            }

            return -1;
        }

        #endregion KeyValidation
    }
}
