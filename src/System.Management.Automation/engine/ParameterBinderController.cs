// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation.Language;
using System.Text;

namespace System.Management.Automation
{
    /// <summary>
    /// The base class for the parameter binder controllers. This class and
    /// its derived classes control the interaction between the command processor
    /// and the parameter binder(s). It holds the state of the arguments and parameters.
    /// </summary>
    [DebuggerDisplay("InvocationInfo = {InvocationInfo}")]
    internal abstract class ParameterBinderController
    {
        #region ctor

        /// <summary>
        /// Constructs a parameter binder controller for the specified command
        /// in the specified engine context.
        /// </summary>
        /// <param name="invocationInfo">
        ///     The invocation information about the code being run.
        /// </param>
        /// <param name="context">
        ///     The engine context in which the command is being run.
        /// </param>
        /// <param name="parameterBinder">
        ///     The default parameter binder for the command.
        /// </param>
        internal ParameterBinderController(InvocationInfo invocationInfo, ExecutionContext context, ParameterBinderBase parameterBinder)
        {
            Diagnostics.Assert(invocationInfo != null, "Caller to verify invocationInfo is not null.");
            Diagnostics.Assert(parameterBinder != null, "Caller to verify parameterBinder is not null.");
            Diagnostics.Assert(context != null, "call to verify context is not null.");

            this.DefaultParameterBinder = parameterBinder;
            Context = context;
            InvocationInfo = invocationInfo;
        }

        #endregion ctor

        #region internal_members

        /// <summary>
        /// The engine context the command is running in.
        /// </summary>
        internal ExecutionContext Context { get; }

        /// <summary>
        /// Gets the parameter binder for the command.
        /// </summary>
        internal ParameterBinderBase DefaultParameterBinder { get; }

        /// <summary>
        /// The invocation information about the code being run.
        /// </summary>
        internal InvocationInfo InvocationInfo { get; }

        /// <summary>
        /// All the metadata associated with any of the parameters that
        /// are available from the command.
        /// </summary>
        internal MergedCommandParameterMetadata BindableParameters
        {
            get { return _bindableParameters; }
        }

        protected MergedCommandParameterMetadata _bindableParameters = new MergedCommandParameterMetadata();

        /// <summary>
        /// A list of the unbound parameters for the command.
        /// </summary>
        protected List<MergedCompiledCommandParameter> UnboundParameters { get; set; }

        /// <summary>
        /// A collection of the bound parameters for the command. The collection is
        /// indexed based on the name of the parameter.
        /// </summary>
        protected Dictionary<string, MergedCompiledCommandParameter> BoundParameters { get; } = new Dictionary<string, MergedCompiledCommandParameter>(StringComparer.OrdinalIgnoreCase);

        internal CommandLineParameters CommandLineParameters
        {
            get { return this.DefaultParameterBinder.CommandLineParameters; }
        }

        /// <summary>
        /// Set true if the default parameter binding is in use.
        /// </summary>
        protected bool DefaultParameterBindingInUse { get; set; } = false;

        // Set true if the default parameter values are applied

        /// <summary>
        /// A collection of bound default parameters.
        /// </summary>
        protected Collection<string> BoundDefaultParameters { get; } = new Collection<string>();

        // Keep record of the bound default parameters

        /// <summary>
        /// A collection of the unbound arguments.
        /// </summary>
        /// <value></value>
        protected Collection<CommandParameterInternal> UnboundArguments { get; set; } = new Collection<CommandParameterInternal>();

        internal void ClearUnboundArguments()
        {
            UnboundArguments.Clear();
        }

        /// <summary>
        /// A collection of the arguments that have been bound.
        /// </summary>
        protected Dictionary<string, CommandParameterInternal> BoundArguments { get; } = new Dictionary<string, CommandParameterInternal>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Reparses the unbound arguments using the parameter metadata of the
        /// specified parameter binder as the parsing guide.
        /// </summary>
        /// <exception cref="ParameterBindingException">
        /// If a parameter token is not matched with an argument and its not a bool or
        /// SwitchParameter.
        /// Or
        /// The name of the argument matches more than one parameter.
        /// </exception>
        protected void ReparseUnboundArguments()
        {
            Collection<CommandParameterInternal> result = new Collection<CommandParameterInternal>();

            for (int index = 0; index < UnboundArguments.Count; ++index)
            {
                CommandParameterInternal argument = UnboundArguments[index];

                // If the parameter name is not specified, or if it is specified _and_ there is an
                // argument, we have nothing to reparse for this argument.
                if (!argument.ParameterNameSpecified || argument.ArgumentSpecified)
                {
                    result.Add(argument);
                    continue;
                }

                Diagnostics.Assert(argument.ParameterNameSpecified && !argument.ArgumentSpecified,
                    "At this point, we only process parameters with no arguments");

                // Now check the argument name with the binder.

                string parameterName = argument.ParameterName;
                MergedCompiledCommandParameter matchingParameter =
                    _bindableParameters.GetMatchingParameter(
                        parameterName,
                        false,
                        true,
                        new InvocationInfo(this.InvocationInfo.MyCommand, argument.ParameterExtent));

                if (matchingParameter == null)
                {
                    // Since we couldn't find a match, just add the argument as it was
                    // and continue
                    result.Add(argument);
                    continue;
                }

                // Now that we know we have a single match for the parameter name,
                // see if we can figure out what the argument value for the parameter is.

                // If its a bool or switch parameter, then set the value to true and continue

                if (IsSwitchAndSetValue(parameterName, argument, matchingParameter.Parameter))
                {
                    result.Add(argument);
                    continue;
                }

                // Since it's not a bool or a SwitchParameter we need to check the next
                // argument.

                if (UnboundArguments.Count - 1 > index)
                {
                    CommandParameterInternal nextArgument = UnboundArguments[index + 1];

                    // Since the argument appears to be a valid parameter, check the
                    // next argument to see if it is the value for that parameter

                    if (nextArgument.ParameterNameSpecified)
                    {
                        // Since we have a valid parameter we need to see if the next argument is
                        // an argument value for that parameter or a parameter itself.

                        MergedCompiledCommandParameter nextMatchingParameter =
                            _bindableParameters.GetMatchingParameter(
                                nextArgument.ParameterName,
                                false,
                                true,
                                new InvocationInfo(this.InvocationInfo.MyCommand, nextArgument.ParameterExtent));

                        if ((nextMatchingParameter != null) || nextArgument.ParameterAndArgumentSpecified)
                        {
                            // Since the next argument is a valid parameter that means the current
                            // argument doesn't have a value

                            // It is an error to have an argument that is a parameter name
                            // but doesn't have a value

                            ParameterBindingException exception =
                                new ParameterBindingException(
                                    ErrorCategory.InvalidArgument,
                                    this.InvocationInfo,
                                    GetParameterErrorExtent(argument),
                                    matchingParameter.Parameter.Name,
                                    matchingParameter.Parameter.Type,
                                    null,
                                    ParameterBinderStrings.MissingArgument,
                                    "MissingArgument");

                            throw exception;
                        }

                        ++index;
                        argument.ParameterName = matchingParameter.Parameter.Name;
                        argument.SetArgumentValue(nextArgument.ArgumentAst, nextArgument.ParameterText);
                        result.Add(argument);
                        continue;
                    }

                    // The next argument appears to be the value for this parameter. Set the value,
                    // increment the index and continue

                    ++index;
                    argument.ParameterName = matchingParameter.Parameter.Name;
                    argument.SetArgumentValue(nextArgument.ArgumentAst, nextArgument.ArgumentValue);
                    result.Add(argument);
                }
                else
                {
                    // It is an error to have a argument that is a parameter name
                    // but doesn't have a value

                    ParameterBindingException exception =
                        new ParameterBindingException(
                            ErrorCategory.InvalidArgument,
                            this.InvocationInfo,
                            GetParameterErrorExtent(argument),
                            matchingParameter.Parameter.Name,
                            matchingParameter.Parameter.Type,
                            null,
                            ParameterBinderStrings.MissingArgument,
                            "MissingArgument");

                    throw exception;
                }
            }

            UnboundArguments = result;
        }

        protected void InitUnboundArguments(Collection<CommandParameterInternal> arguments)
        {
            // Add the passed in arguments to the unboundArguments collection
            Collection<CommandParameterInternal> paramsFromSplatting = null;
            foreach (CommandParameterInternal argument in arguments)
            {
                if (argument.FromHashtableSplatting)
                {
                    paramsFromSplatting ??= new Collection<CommandParameterInternal>();
                    paramsFromSplatting.Add(argument);
                }
                else
                {
                    UnboundArguments.Add(argument);
                }
            }

            // Move the arguments from hashtable splatting to the end of the unbound args list, so that
            // the explicitly specified named arguments can supersede those from a hashtable splatting.
            if (paramsFromSplatting != null)
            {
                foreach (CommandParameterInternal argument in paramsFromSplatting)
                {
                    UnboundArguments.Add(argument);
                }
            }
        }

        private static bool IsSwitchAndSetValue(
            string argumentName,
            CommandParameterInternal argument,
            CompiledCommandParameter matchingParameter)
        {
            bool result = false;

            if (matchingParameter.Type == typeof(SwitchParameter))
            {
                argument.ParameterName = argumentName;
                argument.SetArgumentValue(null, SwitchParameter.Present);
                result = true;
            }

            return result;
        }

        /// <summary>
        /// The argument looks like a parameter if it is a string
        /// and starts with a dash.
        /// </summary>
        /// <param name="arg">
        /// The argument to check.
        /// </param>
        /// <returns>
        /// True if the argument is a string and starts with a dash,
        /// or false otherwise.
        /// </returns>
        internal static bool ArgumentLooksLikeParameter(string arg)
        {
            bool result = false;

            if (!string.IsNullOrEmpty(arg))
            {
                result = arg[0].IsDash();
            }

            return result;
        }

        /// <summary>
        /// Reparses the arguments specified in the object[] and generates CommandParameterInternal instances
        /// based on whether the arguments look like parameters. The CommandParameterInternal instances then
        /// get added to the specified command processor.
        /// </summary>
        /// <param name="commandProcessor">
        /// The command processor instance to add the reparsed parameters to.
        /// </param>
        /// <param name="arguments">
        /// The arguments that require reparsing.
        /// </param>
        internal static void AddArgumentsToCommandProcessor(CommandProcessorBase commandProcessor, object[] arguments)
        {
            if ((arguments != null) && (arguments.Length > 0))
            {
                PSBoundParametersDictionary boundParameters = arguments[0] as PSBoundParametersDictionary;
                if ((boundParameters != null) && (arguments.Length == 1))
                {
                    // If they are supplying a dictionary of parameters, use those directly
                    foreach (KeyValuePair<string, object> boundParameter in boundParameters)
                    {
                        CommandParameterInternal param = CommandParameterInternal.CreateParameterWithArgument(
                            /*parameterAst*/null, boundParameter.Key, boundParameter.Key,
                            /*argumentAst*/null, boundParameter.Value, false);
                        commandProcessor.AddParameter(param);
                    }
                }
                else
                {
                    // Otherwise, we need to parse them ourselves
                    for (int argIndex = 0; argIndex < arguments.Length; ++argIndex)
                    {
                        CommandParameterInternal param;
                        string paramText = arguments[argIndex] as string;
                        if (ArgumentLooksLikeParameter(paramText))
                        {
                            // The argument looks like a parameter.
                            // Create a parameter with argument if the paramText is like this: -Path:c:\windows
                            // Combine it with the next argument if there is an argument, and the parameter ends in ':'.

                            var colonIndex = paramText.IndexOf(':');
                            if (colonIndex != -1 && colonIndex != paramText.Length - 1)
                            {
                                param = CommandParameterInternal.CreateParameterWithArgument(
                                    /*parameterAst*/null, paramText.Substring(1, colonIndex - 1), paramText,
                                    /*argumentAst*/null, paramText.AsSpan(colonIndex + 1).Trim().ToString(),
                                    false);
                            }
                            else if (argIndex == arguments.Length - 1 || paramText[paramText.Length - 1] != ':')
                            {
                                param = CommandParameterInternal.CreateParameter(
                                    paramText.Substring(1), paramText);
                            }
                            else
                            {
                                param = CommandParameterInternal.CreateParameterWithArgument(
                                    /*parameterAst*/null, paramText.Substring(1, paramText.Length - 2), paramText,
                                    /*argumentAst*/null, arguments[argIndex + 1],
                                    false);
                                argIndex++;
                            }
                        }
                        else
                        {
                            param = CommandParameterInternal.CreateArgument(arguments[argIndex]);
                        }

                        commandProcessor.AddParameter(param);
                    }
                }
            }
        }

        /// <summary>
        /// Bind the argument to the specified parameter.
        /// </summary>
        /// <param name="argument">
        /// The argument to be bound.
        /// </param>
        /// <param name="flags">
        /// The flags for type coercion, validation, and script block binding.
        /// </param>
        /// <returns>
        /// True if the parameter was successfully bound. False if <paramref name="flags"/> does not have the
        /// flag <see>ParameterBindingFlags.ShouldCoerceType</see> and the type does not match the parameter type.
        /// </returns>
        /// <exception cref="ParameterBindingException">
        /// If argument transformation fails.
        /// or
        /// The argument could not be coerced to the appropriate type for the parameter.
        /// or
        /// The parameter argument transformation, prerequisite, or validation failed.
        /// or
        /// If the binding to the parameter fails.
        /// or
        /// The parameter has already been bound.
        /// </exception>
        internal virtual bool BindParameter(
            CommandParameterInternal argument,
            ParameterBindingFlags flags)
        {
            bool result = false;

            MergedCompiledCommandParameter matchingParameter =
                BindableParameters.GetMatchingParameter(
                    argument.ParameterName,
                    (flags & ParameterBindingFlags.ThrowOnParameterNotFound) != 0,
                    true,
                    new InvocationInfo(this.InvocationInfo.MyCommand, argument.ParameterExtent));

            if (matchingParameter != null)
            {
                // Now check to make sure it hasn't already been
                // bound by looking in the boundParameters collection

                if (BoundParameters.ContainsKey(matchingParameter.Parameter.Name))
                {
                    ParameterBindingException bindingException =
                        new ParameterBindingException(
                            ErrorCategory.InvalidArgument,
                            this.InvocationInfo,
                            GetParameterErrorExtent(argument),
                            argument.ParameterName,
                            null,
                            null,
                            ParameterBinderStrings.ParameterAlreadyBound,
                            nameof(ParameterBinderStrings.ParameterAlreadyBound));

                    throw bindingException;
                }

                flags &= ~ParameterBindingFlags.DelayBindScriptBlock;
                result = BindParameter(_currentParameterSetFlag, argument, matchingParameter, flags);
            }

            return result;
        }

        /// <summary>
        /// Derived classes need to define the binding of multiple arguments.
        /// </summary>
        /// <param name="parameters">
        /// The arguments to be bound.
        /// </param>
        /// <returns>
        /// The arguments which are still not bound.
        /// </returns>
        internal virtual Collection<CommandParameterInternal> BindParameters(Collection<CommandParameterInternal> parameters)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Bind the argument to the specified parameter.
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
        /// Flags for type coercion and validation of the arguments.
        /// </param>
        /// <returns>
        /// True if the parameter was successfully bound. False if <paramref name="flags"/>
        /// specifies no type coercion and the type does not match the parameter type.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="parameter"/> or <paramref name="argument"/> is null.
        /// </exception>
        /// <exception cref="ParameterBindingException">
        /// If argument transformation fails.
        /// or
        /// The argument could not be coerced to the appropriate type for the parameter.
        /// or
        /// The parameter argument transformation, prerequisite, or validation failed.
        /// or
        /// If the binding to the parameter fails.
        /// </exception>
        internal virtual bool BindParameter(
            uint parameterSets,
            CommandParameterInternal argument,
            MergedCompiledCommandParameter parameter,
            ParameterBindingFlags flags)
        {
            bool result = false;

            switch (parameter.BinderAssociation)
            {
                case ParameterBinderAssociation.DeclaredFormalParameters:
                    result =
                        this.DefaultParameterBinder.BindParameter(
                            argument,
                            parameter.Parameter,
                            flags);
                    break;

                default:
                    Diagnostics.Assert(
                        false,
                        "Only the formal parameters are available for this type of command");
                    break;
            }

            if (result && ((flags & ParameterBindingFlags.IsDefaultValue) == 0))
            {
                UnboundParameters.Remove(parameter);
                BoundParameters.Add(parameter.Parameter.Name, parameter);
            }

            return result;
        }

        /// <summary>
        /// This is used by <see cref="BindNamedParameters"/> to validate and bind a given named parameter.
        /// </summary>
        protected virtual void BindNamedParameter(
            uint parameterSets,
            CommandParameterInternal argument,
            MergedCompiledCommandParameter parameter)
        {
            BindParameter(parameterSets, argument, parameter, ParameterBindingFlags.ShouldCoerceType);
        }

        /// <summary>
        /// Bind the named parameters from the specified argument collection,
        /// for only the parameters in the specified parameter set.
        /// </summary>
        /// <param name="parameterSets">
        /// The parameter set used to bind the arguments.
        /// </param>
        /// <param name="arguments">
        /// The arguments that should be attempted to bind to the parameters of the specified parameter binder.
        /// </param>
        /// <exception cref="ParameterBindingException">
        /// if multiple parameters are found matching the name.
        /// or
        /// if no match could be found.
        /// or
        /// If argument transformation fails.
        /// or
        /// The argument could not be coerced to the appropriate type for the parameter.
        /// or
        /// The parameter argument transformation, prerequisite, or validation failed.
        /// or
        /// If the binding to the parameter fails.
        /// </exception>
        protected Collection<CommandParameterInternal> BindNamedParameters(uint parameterSets, Collection<CommandParameterInternal> arguments)
        {
            Collection<CommandParameterInternal> result = new Collection<CommandParameterInternal>();
            HashSet<string> boundExplicitNamedParams = null;

            foreach (CommandParameterInternal argument in arguments)
            {
                if (!argument.ParameterNameSpecified)
                {
                    result.Add(argument);
                    continue;
                }

                // We don't want to throw an exception yet because the parameter might be a positional argument,
                // or in case of a cmdlet or an advanced function, it might match up to a dynamic parameter.
                MergedCompiledCommandParameter parameter =
                    BindableParameters.GetMatchingParameter(
                        name: argument.ParameterName,
                        throwOnParameterNotFound: false,
                        tryExactMatching: true,
                        invocationInfo: new InvocationInfo(this.InvocationInfo.MyCommand, argument.ParameterExtent));

                // If the parameter is not in the specified parameter set, throw a binding exception
                if (parameter != null)
                {
                    string formalParamName = parameter.Parameter.Name;

                    if (argument.FromHashtableSplatting)
                    {
                        boundExplicitNamedParams ??= new HashSet<string>(
                            BoundParameters.Keys,
                            StringComparer.OrdinalIgnoreCase);

                        if (boundExplicitNamedParams.Contains(formalParamName))
                        {
                            // This named parameter from splatting is also explicitly specified by the user,
                            // which was successfully bound, so we ignore the one from splatting because it
                            // is superceded by the explicit one. For example:
                            //   $splat = @{ Path = $path1 }
                            //   dir @splat -Path $path2
                            continue;
                        }
                    }

                    // Now check to make sure it hasn't already been
                    // bound by looking in the boundParameters collection

                    if (BoundParameters.ContainsKey(formalParamName))
                    {
                        ParameterBindingException bindingException =
                            new ParameterBindingException(
                                ErrorCategory.InvalidArgument,
                                this.InvocationInfo,
                                GetParameterErrorExtent(argument),
                                argument.ParameterName,
                                null,
                                null,
                                ParameterBinderStrings.ParameterAlreadyBound,
                                nameof(ParameterBinderStrings.ParameterAlreadyBound));

                        throw bindingException;
                    }

                    BindNamedParameter(parameterSets, argument, parameter);
                }
                else if (argument.ParameterName.Equals(Parser.VERBATIM_PARAMETERNAME, StringComparison.Ordinal))
                {
                    // We sometimes send a magic parameter from a remote machine with the values referenced via
                    // a using expression ($using:x).  We then access these values via PSBoundParameters, so
                    // "bind" them here.
                    DefaultParameterBinder.CommandLineParameters.SetImplicitUsingParameters(argument.ArgumentValue);
                }
                else
                {
                    result.Add(argument);
                }
            }

            return result;
        }

        /// <summary>
        /// Binds the unbound arguments to positional parameters.
        /// </summary>
        /// <param name="unboundArguments">
        /// The unbound arguments to attempt to bind as positional arguments.
        /// </param>
        /// <param name="validParameterSets">
        /// The current parameter set flags that are valid.
        /// </param>
        /// <param name="defaultParameterSet">
        /// The parameter set to use to disambiguate parameters that have the same position
        /// </param>
        /// <param name="outgoingBindingException">
        /// Returns the underlying parameter binding exception if any was generated.
        /// </param>
        /// <returns>
        /// The remaining arguments that have not been bound.
        /// </returns>
        /// <remarks>
        /// It is assumed that the unboundArguments parameter has already been processed
        /// for this parameter binder. All named parameters have been paired with their
        /// values. Any arguments that don't have a name are considered positional and
        /// will be processed in this method.
        /// </remarks>
        /// <exception cref="ParameterBindingException">
        /// If multiple parameters were found for the same position in the specified
        /// parameter set.
        /// or
        /// If argument transformation fails.
        /// or
        /// The argument could not be coerced to the appropriate type for the parameter.
        /// or
        /// The parameter argument transformation, prerequisite, or validation failed.
        /// or
        /// If the binding to the parameter fails.
        /// </exception>
        internal Collection<CommandParameterInternal> BindPositionalParameters(
            Collection<CommandParameterInternal> unboundArguments,
            uint validParameterSets,
            uint defaultParameterSet,
            out ParameterBindingException outgoingBindingException
            )
        {
            Collection<CommandParameterInternal> result = new Collection<CommandParameterInternal>();
            outgoingBindingException = null;

            if (unboundArguments.Count > 0)
            {
                // Create a new collection to iterate over so that we can remove
                // unbound arguments while binding them.

                List<CommandParameterInternal> unboundArgumentsCollection = new List<CommandParameterInternal>(unboundArguments);

                // Get a sorted dictionary of the positional parameters with the position
                // as the key

                SortedDictionary<int, Dictionary<MergedCompiledCommandParameter, PositionalCommandParameter>> positionalParameterDictionary;

                try
                {
                    positionalParameterDictionary =
                        EvaluateUnboundPositionalParameters(UnboundParameters, _currentParameterSetFlag);
                }
                catch (InvalidOperationException)
                {
                    // The parameter set declaration is ambiguous so
                    // throw an exception.

                    ParameterBindingException bindingException =
                        new ParameterBindingException(
                            ErrorCategory.InvalidArgument,
                            this.InvocationInfo,
                            null,
                            null,
                            null,
                            null,
                            ParameterBinderStrings.AmbiguousPositionalParameterNoName,
                            "AmbiguousPositionalParameterNoName");

                    // This exception is thrown because the binder found two positional parameters
                    // from the same parameter set with the same position defined. This is not caused
                    // by introducing the default parameter binding.
                    throw bindingException;
                }

                if (positionalParameterDictionary.Count > 0)
                {
                    int unboundArgumentsIndex = 0;

                    foreach (Dictionary<MergedCompiledCommandParameter, PositionalCommandParameter> nextPositionalParameters in positionalParameterDictionary.Values)
                    {
                        // Only continue if there are parameters at the specified position. Parameters
                        // can be removed as the parameter set gets narrowed down.

                        if (nextPositionalParameters.Count == 0)
                        {
                            continue;
                        }

                        CommandParameterInternal argument = GetNextPositionalArgument(
                            unboundArgumentsCollection,
                            result,
                            ref unboundArgumentsIndex);

                        if (argument == null)
                        {
                            break;
                        }

                        // Bind first to defaultParameterSet without type coercion, then to
                        // other sets without type coercion, then to the defaultParameterSet with
                        // type coercion and finally to the other sets with type coercion.

                        bool aParameterWasBound = false;
                        if (defaultParameterSet != 0 && (validParameterSets & defaultParameterSet) != 0)
                        {
                            // Favor the default parameter set.
                            // First try without type coercion

                            aParameterWasBound =
                                BindPositionalParametersInSet(
                                    defaultParameterSet,
                                    nextPositionalParameters,
                                    argument,
                                    ParameterBindingFlags.DelayBindScriptBlock,
                                    out outgoingBindingException);
                        }

                        if (!aParameterWasBound)
                        {
                            // Try the non-default parameter sets
                            // without type coercion.

                            aParameterWasBound =
                                BindPositionalParametersInSet(
                                    validParameterSets,
                                    nextPositionalParameters,
                                    argument,
                                    ParameterBindingFlags.DelayBindScriptBlock,
                                    out outgoingBindingException);
                        }

                        if (!aParameterWasBound)
                        {
                            // Now try the default parameter set with type coercion
                            if (defaultParameterSet != 0 && (validParameterSets & defaultParameterSet) != 0)
                            {
                                // Favor the default parameter set.
                                // First try without type coercion

                                aParameterWasBound =
                                    BindPositionalParametersInSet(
                                        defaultParameterSet,
                                        nextPositionalParameters,
                                        argument,
                                        ParameterBindingFlags.ShouldCoerceType | ParameterBindingFlags.DelayBindScriptBlock,
                                        out outgoingBindingException);
                            }
                        }

                        if (!aParameterWasBound)
                        {
                            // Try the non-default parameter sets
                            // with type coercion.

                            aParameterWasBound =
                                BindPositionalParametersInSet(
                                    validParameterSets,
                                    nextPositionalParameters,
                                    argument,
                                    ParameterBindingFlags.ShouldCoerceType | ParameterBindingFlags.DelayBindScriptBlock,
                                    out outgoingBindingException);
                        }

                        if (!aParameterWasBound)
                        {
                            // Add the unprocessed argument to the results and continue
                            result.Add(argument);
                        }
                        else
                        {
                            // Update the parameter sets if necessary
                            if (validParameterSets != _currentParameterSetFlag)
                            {
                                validParameterSets = _currentParameterSetFlag;
                                UpdatePositionalDictionary(positionalParameterDictionary, validParameterSets);
                            }
                        }
                    }

                    // Now for any arguments that were not processed, add them to
                    // the result

                    for (int index = unboundArgumentsIndex; index < unboundArgumentsCollection.Count; ++index)
                    {
                        result.Add(unboundArgumentsCollection[index]);
                    }
                }
                else
                {
                    // Since no positional parameters were found, add the arguments
                    // to the result

                    result = unboundArguments;
                }
            }

            return result;
        }

        /// <summary>
        /// This method only updates the collections contained in the dictionary, not the dictionary
        /// itself to contain only the parameters that are in the specified parameter set.
        /// </summary>
        /// <param name="positionalParameterDictionary">
        /// The sorted dictionary of positional parameters.
        /// </param>
        /// <param name="validParameterSets">
        /// Valid parameter sets
        /// </param>
        internal static void UpdatePositionalDictionary(
            SortedDictionary<int, Dictionary<MergedCompiledCommandParameter, PositionalCommandParameter>> positionalParameterDictionary,
            uint validParameterSets)
        {
            foreach (Dictionary<MergedCompiledCommandParameter, PositionalCommandParameter> parameterCollection in positionalParameterDictionary.Values)
            {
                Collection<MergedCompiledCommandParameter> paramToRemove = new Collection<MergedCompiledCommandParameter>();

                foreach (PositionalCommandParameter positionalParameter in parameterCollection.Values)
                {
                    Collection<ParameterSetSpecificMetadata> parameterSetData = positionalParameter.ParameterSetData;

                    for (int index = parameterSetData.Count - 1; index >= 0; --index)
                    {
                        if ((parameterSetData[index].ParameterSetFlag & validParameterSets) == 0 &&
                            !parameterSetData[index].IsInAllSets)
                        {
                            // The parameter is not in the valid parameter sets so remove it from the collection.
                            parameterSetData.RemoveAt(index);
                        }
                    }

                    if (parameterSetData.Count == 0)
                    {
                        paramToRemove.Add(positionalParameter.Parameter);
                    }
                }

                // Now remove all the parameters that no longer have parameter set data
                foreach (MergedCompiledCommandParameter removeParam in paramToRemove)
                {
                    parameterCollection.Remove(removeParam);
                }
            }
        }

        private bool BindPositionalParametersInSet(
            uint validParameterSets,
            Dictionary<MergedCompiledCommandParameter, PositionalCommandParameter> nextPositionalParameters,
            CommandParameterInternal argument,
            ParameterBindingFlags flags,
            out ParameterBindingException bindingException
            )
        {
            bool result = false;
            bindingException = null;

            foreach (PositionalCommandParameter parameter in nextPositionalParameters.Values)
            {
                foreach (ParameterSetSpecificMetadata parameterSetData in parameter.ParameterSetData)
                {
                    // if the parameter is not in the specified parameter set, don't consider it

                    if ((validParameterSets & parameterSetData.ParameterSetFlag) == 0 &&
                        !parameterSetData.IsInAllSets)
                    {
                        continue;
                    }

                    bool bindResult = false;
                    string parameterName = parameter.Parameter.Parameter.Name;

                    ParameterBindingException parameterBindingExceptionToThrown = null;
                    try
                    {
                        CommandParameterInternal bindableArgument =
                            CommandParameterInternal.CreateParameterWithArgument(
                                /*parameterAst*/null, parameterName, "-" + parameterName + ":",
                                argument.ArgumentAst, argument.ArgumentValue,
                                false);

                        bindResult =
                            BindParameter(
                                validParameterSets,
                                bindableArgument,
                                parameter.Parameter,
                                flags);
                    }
                    catch (ParameterBindingArgumentTransformationException pbex)
                    {
                        parameterBindingExceptionToThrown = pbex;
                    }
                    catch (ParameterBindingValidationException pbex)
                    {
                        if (pbex.SwallowException)
                        {
                            // Just ignore and continue
                            bindResult = false;
                            bindingException = pbex;
                        }
                        else
                        {
                            parameterBindingExceptionToThrown = pbex;
                        }
                    }
                    catch (ParameterBindingParameterDefaultValueException pbex)
                    {
                        parameterBindingExceptionToThrown = pbex;
                    }
                    catch (ParameterBindingException e)
                    {
                        // Just ignore and continue;
                        bindResult = false;
                        bindingException = e;
                    }

                    if (parameterBindingExceptionToThrown != null)
                    {
                        if (!DefaultParameterBindingInUse)
                        {
                            throw parameterBindingExceptionToThrown;
                        }
                        else
                        {
                            ThrowElaboratedBindingException(parameterBindingExceptionToThrown);
                        }
                    }

                    if (bindResult)
                    {
                        result = true;
                        this.CommandLineParameters.MarkAsBoundPositionally(parameterName);
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Generate elaborated binding exception so that the user will know the default binding might cause the failure.
        /// </summary>
        /// <param name="pbex"></param>
        protected void ThrowElaboratedBindingException(ParameterBindingException pbex)
        {
            if (pbex == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(pbex));
            }

            Diagnostics.Assert(pbex.ErrorRecord != null, "ErrorRecord should not be null in a ParameterBindingException");

            // Original error message
            string oldMsg = pbex.Message;
            // Default parameters get bound so far
            StringBuilder defaultParamsGetBound = new StringBuilder();
            foreach (string paramName in BoundDefaultParameters)
            {
                defaultParamsGetBound.Append(CultureInfo.InvariantCulture, $" -{paramName}");
            }

            string resourceString = ParameterBinderStrings.DefaultBindingErrorElaborationSingle;
            if (BoundDefaultParameters.Count > 1)
            {
                resourceString = ParameterBinderStrings.DefaultBindingErrorElaborationMultiple;
            }

            ParameterBindingException newBindingException =
                new ParameterBindingException(
                    pbex.InnerException,
                    pbex,
                    resourceString,
                    oldMsg, defaultParamsGetBound);

            throw newBindingException;
        }

        private static CommandParameterInternal GetNextPositionalArgument(
            List<CommandParameterInternal> unboundArgumentsCollection,
            Collection<CommandParameterInternal> nonPositionalArguments,
            ref int unboundArgumentsIndex)
        {
            // Find the next positional argument
            // An argument without a name is considered to be positional since
            // we are assuming the unboundArguments have been reparsed using
            // the merged metadata from this parameter binder controller.

            CommandParameterInternal result = null;
            while (unboundArgumentsIndex < unboundArgumentsCollection.Count)
            {
                CommandParameterInternal argument = unboundArgumentsCollection[unboundArgumentsIndex++];

                if (!argument.ParameterNameSpecified)
                {
                    result = argument;
                    break;
                }

                nonPositionalArguments.Add(argument);

                // Now check to see if the next argument needs to be consumed as well.

                if (unboundArgumentsCollection.Count - 1 >= unboundArgumentsIndex)
                {
                    argument = unboundArgumentsCollection[unboundArgumentsIndex];

                    if (!argument.ParameterNameSpecified)
                    {
                        // Since the next argument doesn't appear to be a parameter name
                        // consume it as well.

                        nonPositionalArguments.Add(argument);
                        unboundArgumentsIndex++;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the unbound positional parameters in a sorted dictionary in the order of their
        /// positions.
        /// </summary>
        /// <returns>
        /// The sorted dictionary of MergedCompiledCommandParameter metadata with the position
        /// as the key.
        /// </returns>
        internal static SortedDictionary<int, Dictionary<MergedCompiledCommandParameter, PositionalCommandParameter>> EvaluateUnboundPositionalParameters(
            ICollection<MergedCompiledCommandParameter> unboundParameters, uint validParameterSetFlag)
        {
            SortedDictionary<int, Dictionary<MergedCompiledCommandParameter, PositionalCommandParameter>> result =
                new SortedDictionary<int, Dictionary<MergedCompiledCommandParameter, PositionalCommandParameter>>();

            if (unboundParameters.Count > 0)
            {
                // Loop through the unbound parameters and find a parameter in the specified parameter set
                // that has a position greater than or equal to the positionalParameterIndex

                foreach (MergedCompiledCommandParameter parameter in unboundParameters)
                {
                    bool isInParameterSet = (parameter.Parameter.ParameterSetFlags & validParameterSetFlag) != 0 || parameter.Parameter.IsInAllSets;

                    if (isInParameterSet)
                    {
                        var parameterSetDataCollection = parameter.Parameter.GetMatchingParameterSetData(validParameterSetFlag);
                        foreach (ParameterSetSpecificMetadata parameterSetData in parameterSetDataCollection)
                        {
                            // Skip ValueFromRemainingArguments parameters
                            if (parameterSetData.ValueFromRemainingArguments)
                            {
                                continue;
                            }

                            // Check the position in the parameter set

                            int positionInParameterSet = parameterSetData.Position;

                            if (positionInParameterSet == int.MinValue)
                            {
                                // The parameter is not positional so go to the next one
                                continue;
                            }

                            AddNewPosition(result, positionInParameterSet, parameter, parameterSetData);
                        }
                    }
                }
            }

            return result;
        }

        private static void AddNewPosition(
            SortedDictionary<int, Dictionary<MergedCompiledCommandParameter, PositionalCommandParameter>> result,
            int positionInParameterSet,
            MergedCompiledCommandParameter parameter,
            ParameterSetSpecificMetadata parameterSetData)
        {
            Dictionary<MergedCompiledCommandParameter, PositionalCommandParameter> positionalCommandParameters;
            if (result.TryGetValue(positionInParameterSet, out positionalCommandParameters))
            {
                // Check to see if any of the other parameters in this position are in the same parameter set.
                if (ContainsPositionalParameterInSet(positionalCommandParameters, parameter, parameterSetData.ParameterSetFlag))
                {
                    // Multiple parameters were found with the same
                    // position. This means the parameter set is ambiguous.

                    // positional parameter could not be resolved

                    // We throw InvalidOperationException, which the
                    // caller will catch and throw a more
                    // appropriate exception.
                    throw PSTraceSource.NewInvalidOperationException();
                }

                PositionalCommandParameter positionalCommandParameter;
                if (!positionalCommandParameters.TryGetValue(parameter, out positionalCommandParameter))
                {
                    positionalCommandParameter = new PositionalCommandParameter(parameter);
                    positionalCommandParameters.Add(parameter, positionalCommandParameter);
                }

                positionalCommandParameter.ParameterSetData.Add(parameterSetData);
            }
            else
            {
                Dictionary<MergedCompiledCommandParameter, PositionalCommandParameter> newPositionDictionary =
                    new Dictionary<MergedCompiledCommandParameter, PositionalCommandParameter>();

                PositionalCommandParameter newPositionalParameter = new PositionalCommandParameter(parameter);
                newPositionalParameter.ParameterSetData.Add(parameterSetData);
                newPositionDictionary.Add(parameter, newPositionalParameter);

                result.Add(positionInParameterSet, newPositionDictionary);
            }
        }

        private static bool ContainsPositionalParameterInSet(
            Dictionary<MergedCompiledCommandParameter, PositionalCommandParameter> positionalCommandParameters,
            MergedCompiledCommandParameter parameter,
            uint parameterSet)
        {
            bool result = false;

            foreach (KeyValuePair<MergedCompiledCommandParameter, PositionalCommandParameter> pair in positionalCommandParameters)
            {
                // It's OK to have the same parameter
                if (pair.Key == parameter)
                {
                    continue;
                }

                foreach (ParameterSetSpecificMetadata parameterSetData in pair.Value.ParameterSetData)
                {
                    if ((parameterSetData.ParameterSetFlag & parameterSet) != 0 ||
                        parameterSetData.ParameterSetFlag == parameterSet)
                    {
                        result = true;
                        break;
                    }
                }

                if (result)
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Keeps track of the parameters that get bound through pipeline input, so that their
        /// previous values can be restored before the next pipeline input comes.
        /// </summary>
        internal Collection<MergedCompiledCommandParameter> ParametersBoundThroughPipelineInput { get; } = new Collection<MergedCompiledCommandParameter>();

        /// <summary>
        /// For any unbound parameters, this method checks to see if the
        /// parameter has a default value specified, and evaluates the expression
        /// (if the expression is not constant) and binds the result to the parameter.
        /// If not, we bind null to the parameter (which may go through type coercion).
        /// </summary>
        internal void BindUnboundScriptParameters()
        {
            foreach (MergedCompiledCommandParameter parameter in UnboundParameters)
            {
                BindUnboundScriptParameterWithDefaultValue(parameter);
            }
        }

        /// <summary>
        /// If the parameter binder might use the value more than once, this it can save the value to avoid
        /// re-evaluating complicated expressions.
        /// </summary>
        protected virtual void SaveDefaultScriptParameterValue(string name, object value)
        {
            // By default, parameter binders don't need to remember the value, the exception being the cmdlet parameter binder.
        }

        /// <summary>
        /// Bind the default value for an unbound parameter to script (used by both the script binder
        /// and the cmdlet binder).
        /// </summary>
        internal void BindUnboundScriptParameterWithDefaultValue(MergedCompiledCommandParameter parameter)
        {
            ScriptParameterBinder spb = (ScriptParameterBinder)this.DefaultParameterBinder;
            ScriptBlock script = spb.Script;
            RuntimeDefinedParameter runtimeDefinedParameter;
            if (script.RuntimeDefinedParameters.TryGetValue(parameter.Parameter.Name, out runtimeDefinedParameter))
            {
                bool oldRecordParameters = spb.RecordBoundParameters;
                try
                {
                    spb.RecordBoundParameters = false;

                    // We may pass a magic parameter from the remote end with the values for the using expressions.
                    // In this case, we want to use those values to evaluate the default value. e.g. param($a = $using:date)
                    System.Collections.IDictionary implicitUsingParameters = null;
                    if (DefaultParameterBinder.CommandLineParameters != null)
                    {
                        implicitUsingParameters = DefaultParameterBinder.CommandLineParameters.GetImplicitUsingParameters();
                    }

                    object result = spb.GetDefaultScriptParameterValue(runtimeDefinedParameter, implicitUsingParameters);
                    SaveDefaultScriptParameterValue(parameter.Parameter.Name, result);
                    CommandParameterInternal argument = CommandParameterInternal.CreateParameterWithArgument(
                        /*parameterAst*/null, parameter.Parameter.Name, "-" + parameter.Parameter.Name + ":",
                        /*argumentAst*/null, result,
                        false);
                    ParameterBindingFlags flags = ParameterBindingFlags.IsDefaultValue;
                    // Only coerce explicit values.  We default to null, which isn't always convertible.
                    if (runtimeDefinedParameter.IsSet)
                    {
                        flags |= ParameterBindingFlags.ShouldCoerceType;
                    }

                    BindParameter(uint.MaxValue, argument, parameter, flags);
                }
                finally
                {
                    spb.RecordBoundParameters = oldRecordParameters;
                }
            }
        }

        internal uint _currentParameterSetFlag = uint.MaxValue;
        internal uint _prePipelineProcessingParameterSetFlags = uint.MaxValue;

        protected IScriptExtent GetErrorExtent(CommandParameterInternal cpi)
        {
            var result = cpi.ErrorExtent;
            if (result == PositionUtilities.EmptyExtent)
                result = InvocationInfo.ScriptPosition;
            // Can't use this assertion - we don't have useful positions when invoked via PowerShell API
            // Diagnostics.Assert(result != PositionUtilities.EmptyExtent, "We are missing a valid position somewhere");
            return result;
        }

        protected IScriptExtent GetParameterErrorExtent(CommandParameterInternal cpi)
        {
            var result = cpi.ParameterExtent;
            if (result == PositionUtilities.EmptyExtent)
                result = InvocationInfo.ScriptPosition;
            // Can't use this assertion - we don't have useful positions when invoked via PowerShell API
            // Diagnostics.Assert(result != PositionUtilities.EmptyExtent, "We are missing a valid position somewhere");
            return result;
        }

        #endregion internal_members
    }
}
