// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Management.Automation.Host;
using System.Text;
using System.Reflection;
using System.Management.Automation.Runspaces;

namespace System.Management.Automation.Language
{
    #region "AstArgumentPair"

    /// <summary>
    /// The types for AstParameterArgumentPair.
    /// </summary>
    internal enum AstParameterArgumentType
    {
        AstPair = 0,
        Switch = 1,
        Fake = 2,
        AstArray = 3,
        PipeObject = 4
    }

    /// <summary>
    /// The base class for parameter argument pair.
    /// </summary>
    internal abstract class AstParameterArgumentPair
    {
        /// <summary>
        /// The parameter Ast.
        /// </summary>
        public CommandParameterAst Parameter { get; protected set; }

        /// <summary>
        /// The argument type.
        /// </summary>
        public AstParameterArgumentType ParameterArgumentType { get; protected set; }

        /// <summary>
        /// Indicate if the parameter is specified.
        /// </summary>
        public bool ParameterSpecified { get; protected set; } = false;

        /// <summary>
        /// Indicate if the parameter is specified.
        /// </summary>
        public bool ArgumentSpecified { get; protected set; } = false;

        /// <summary>
        /// The parameter name.
        /// </summary>
        public string ParameterName { get; protected set; }

        /// <summary>
        /// The parameter text.
        /// </summary>
        public string ParameterText { get; protected set; }

        /// <summary>
        /// The argument type.
        /// </summary>
        public Type ArgumentType { get; protected set; }
    }

    /// <summary>
    /// Represent a parameter argument pair. The argument is a pipeline input object.
    /// </summary>
    internal sealed class PipeObjectPair : AstParameterArgumentPair
    {
        internal PipeObjectPair(string parameterName, Type pipeObjType)
        {
            if (parameterName == null)
                throw PSTraceSource.NewArgumentNullException("parameterName");

            Parameter = null;
            ParameterArgumentType = AstParameterArgumentType.PipeObject;
            ParameterSpecified = true;
            ArgumentSpecified = true;
            ParameterName = parameterName;
            ParameterText = parameterName;
            ArgumentType = pipeObjType;
        }
    }

    /// <summary>
    /// Represent a parameter argument pair. The argument is an array of ExpressionAst (remaining
    /// arguments)
    /// </summary>
    internal sealed class AstArrayPair : AstParameterArgumentPair
    {
        internal AstArrayPair(string parameterName, ICollection<ExpressionAst> arguments)
        {
            if (parameterName == null)
                throw PSTraceSource.NewArgumentNullException("parameterName");
            if (arguments == null || arguments.Count == 0)
                throw PSTraceSource.NewArgumentNullException("arguments");

            Parameter = null;
            ParameterArgumentType = AstParameterArgumentType.AstArray;
            ParameterSpecified = true;
            ArgumentSpecified = true;
            ParameterName = parameterName;
            ParameterText = parameterName;
            ArgumentType = typeof(Array);

            Argument = arguments.ToArray();
        }

        /// <summary>
        /// Get the argument.
        /// </summary>
        public ExpressionAst[] Argument { get; } = null;
    }

    /// <summary>
    /// Represent a parameter argument pair. The argument is a fake object.
    /// </summary>
    internal sealed class FakePair : AstParameterArgumentPair
    {
        internal FakePair(CommandParameterAst parameterAst)
        {
            if (parameterAst == null)
                throw PSTraceSource.NewArgumentNullException("parameterAst");

            Parameter = parameterAst;
            ParameterArgumentType = AstParameterArgumentType.Fake;
            ParameterSpecified = true;
            ArgumentSpecified = true;
            ParameterName = parameterAst.ParameterName;
            ParameterText = parameterAst.ParameterName;
            ArgumentType = typeof(object);
        }
    }

    /// <summary>
    /// Represent a parameter argument pair. The parameter is a switch parameter.
    /// </summary>
    internal sealed class SwitchPair : AstParameterArgumentPair
    {
        internal SwitchPair(CommandParameterAst parameterAst)
        {
            if (parameterAst == null)
                throw PSTraceSource.NewArgumentNullException("parameterAst");

            Parameter = parameterAst;
            ParameterArgumentType = AstParameterArgumentType.Switch;
            ParameterSpecified = true;
            ArgumentSpecified = true;
            ParameterName = parameterAst.ParameterName;
            ParameterText = parameterAst.ParameterName;
            ArgumentType = typeof(bool);
        }

        /// <summary>
        /// Get the argument.
        /// </summary>
        public bool Argument
        {
            get { return true; }
        }
    }

    /// <summary>
    /// Represent a parameter argument pair. It could be a pure argument (no parameter, only argument available);
    /// it could be a CommandParameterAst that contains its argument; it also could be a CommandParameterAst with
    /// another CommandParameterAst as the argument.
    /// </summary>
    internal sealed class AstPair : AstParameterArgumentPair
    {
        internal AstPair(CommandParameterAst parameterAst)
        {
            if (parameterAst == null || parameterAst.Argument == null)
                throw PSTraceSource.NewArgumentException("parameterAst");

            Parameter = parameterAst;
            ParameterArgumentType = AstParameterArgumentType.AstPair;
            ParameterSpecified = true;
            ArgumentSpecified = true;
            ParameterName = parameterAst.ParameterName;
            ParameterText = "-" + ParameterName + ":";
            ArgumentType = parameterAst.Argument.StaticType;

            ParameterContainsArgument = true;
            Argument = parameterAst.Argument;
        }

        internal AstPair(CommandParameterAst parameterAst, ExpressionAst argumentAst)
        {
            if (parameterAst != null && parameterAst.Argument != null)
                throw PSTraceSource.NewArgumentException("parameterAst");

            if (parameterAst == null && argumentAst == null)
                throw PSTraceSource.NewArgumentNullException("argumentAst");

            Parameter = parameterAst;
            ParameterArgumentType = AstParameterArgumentType.AstPair;
            ParameterSpecified = parameterAst != null;
            ArgumentSpecified = argumentAst != null;
            ParameterName = parameterAst != null ? parameterAst.ParameterName : null;
            ParameterText = parameterAst != null ? parameterAst.ParameterName : null;
            ArgumentType = argumentAst != null ? argumentAst.StaticType : null;

            ParameterContainsArgument = false;
            Argument = argumentAst;
        }

        internal AstPair(CommandParameterAst parameterAst, CommandElementAst argumentAst)
        {
            if (parameterAst != null && parameterAst.Argument != null)
                throw PSTraceSource.NewArgumentException("parameterAst");

            if (parameterAst == null || argumentAst == null)
                throw PSTraceSource.NewArgumentNullException("argumentAst");

            Parameter = parameterAst;
            ParameterArgumentType = AstParameterArgumentType.AstPair;
            ParameterSpecified = true;
            ArgumentSpecified = true;
            ParameterName = parameterAst.ParameterName;
            ParameterText = parameterAst.ParameterName;
            ArgumentType = typeof(string);

            ParameterContainsArgument = false;
            Argument = argumentAst;
            ArgumentIsCommandParameterAst = true;
        }

        /// <summary>
        /// Indicate if the argument is contained in the CommandParameterAst.
        /// </summary>
        public bool ParameterContainsArgument { get; } = false;

        /// <summary>
        /// Indicate if the argument is of type CommandParameterAst.
        /// </summary>
        public bool ArgumentIsCommandParameterAst { get; } = false;

        /// <summary>
        /// Get the argument.
        /// </summary>
        public CommandElementAst Argument { get; } = null;
    }

    #endregion "AstArgumentPair"

    /// <summary>
    /// Runs the PowerShell parameter binding algorithm against a CommandAst,
    /// returning information about which parameters were bound.
    /// </summary>
    public static class StaticParameterBinder
    {
        /// <summary>
        /// Bind a CommandAst to one of PowerShell's built-in commands.
        /// </summary>
        /// <param name="commandAst">The CommandAst that represents the command invocation.</param>
        /// <returns>The StaticBindingResult that represents the binding.</returns>
        public static StaticBindingResult BindCommand(CommandAst commandAst)
        {
            bool resolve = true;
            return BindCommand(commandAst, resolve);
        }

        /// <summary>
        /// Bind a CommandAst to the specified command.
        /// </summary>
        /// <param name="commandAst">The CommandAst that represents the command invocation.</param>
        /// <param name="resolve">Boolean to determine whether binding should be syntactic, or should attempt
        /// to resolve against an existing command.
        /// </param>
        /// <returns>The StaticBindingResult that represents the binding.</returns>
        public static StaticBindingResult BindCommand(CommandAst commandAst, bool resolve)
        {
            return BindCommand(commandAst, resolve, null);
        }

        /// <summary>
        /// Bind a CommandAst to the specified command.
        /// </summary>
        /// <param name="commandAst">The CommandAst that represents the command invocation.</param>
        /// <param name="resolve">Boolean to determine whether binding should be syntactic, or should attempt
        /// to resolve against an existing command.
        /// </param>
        /// <param name="desiredParameters">
        ///     A string array that represents parameter names of interest. If any of these are specified,
        ///     then full binding is done.
        /// </param>
        /// <returns>The StaticBindingResult that represents the binding.</returns>
        public static StaticBindingResult BindCommand(CommandAst commandAst, bool resolve, string[] desiredParameters)
        {
            // If they specified any desired parameters, first quickly check if they are found
            if ((desiredParameters != null) && (desiredParameters.Length > 0))
            {
                bool possiblyHadDesiredParameter = false;
                foreach (CommandParameterAst commandParameter in commandAst.CommandElements.OfType<CommandParameterAst>())
                {
                    string actualParameterName = commandParameter.ParameterName;

                    foreach (string actualParameter in desiredParameters)
                    {
                        if (actualParameter.StartsWith(actualParameterName, StringComparison.OrdinalIgnoreCase))
                        {
                            possiblyHadDesiredParameter = true;
                            break;
                        }
                    }

                    if (possiblyHadDesiredParameter)
                    {
                        break;
                    }
                }

                // Quick exit if the desired parameter was not present
                if (!possiblyHadDesiredParameter)
                {
                    return null;
                }
            }

            if (!resolve)
            {
                return new StaticBindingResult(commandAst, null);
            }

            PseudoBindingInfo pseudoBinding = null;
            if (Runspace.DefaultRunspace == null)
            {
                // Handle static binding from a non-PowerShell / C# application
                // DefaultRunspace is a thread static field, so race condition will not happen because different threads will access different instances of "DefaultRunspace"
                if (s_bindCommandRunspace == null)
                {
                    // Create a mini runspace by remove the types and formats
                    InitialSessionState minimalState = InitialSessionState.CreateDefault2();
                    minimalState.Types.Clear();
                    minimalState.Formats.Clear();
                    s_bindCommandRunspace = RunspaceFactory.CreateRunspace(minimalState);
                    s_bindCommandRunspace.Open();
                }

                Runspace.DefaultRunspace = s_bindCommandRunspace;
                // Static binding always does argument binding (not argument or parameter completion).
                pseudoBinding = new PseudoParameterBinder().DoPseudoParameterBinding(commandAst, null, null, PseudoParameterBinder.BindingType.ArgumentBinding);
                Runspace.DefaultRunspace = null;
            }
            else
            {
                // Static binding always does argument binding (not argument or parameter completion).
                pseudoBinding = new PseudoParameterBinder().DoPseudoParameterBinding(commandAst, null, null, PseudoParameterBinder.BindingType.ArgumentBinding);
            }

            return new StaticBindingResult(commandAst, pseudoBinding);
        }

        [ThreadStatic]
        static Runspace s_bindCommandRunspace = null;
    }

    /// <summary>
    /// Represents the results of the PowerShell parameter binding process.
    /// </summary>
    public class StaticBindingResult
    {
        internal StaticBindingResult(CommandAst commandAst, PseudoBindingInfo bindingInfo)
        {
            BoundParameters = new Dictionary<string, ParameterBindingResult>(StringComparer.OrdinalIgnoreCase);
            BindingExceptions = new Dictionary<string, StaticBindingError>(StringComparer.OrdinalIgnoreCase);

            if (bindingInfo == null)
            {
                CreateBindingResultForSyntacticBind(commandAst);
            }
            else
            {
                CreateBindingResultForSuccessfulBind(commandAst, bindingInfo);
            }
        }

        private void CreateBindingResultForSuccessfulBind(CommandAst commandAst, PseudoBindingInfo bindingInfo)
        {
            _bindingInfo = bindingInfo;

            // Check if there is exactly one parameter set valid. In that case,
            // ValidParameterSetFlags is exactly a power of two. Otherwise,
            // add to the binding exceptions.
            bool parameterSetSpecified = bindingInfo.ValidParameterSetsFlags != UInt32.MaxValue;
            bool remainingParameterSetIncludesDefault =
                (bindingInfo.DefaultParameterSetFlag != 0) &&
                ((bindingInfo.ValidParameterSetsFlags & bindingInfo.DefaultParameterSetFlag) ==
                bindingInfo.DefaultParameterSetFlag);

            // (x & (x -1 ) == 0) is a bit hack to determine if something is
            // exactly a power of two.
            bool onlyOneRemainingParameterSet =
                (bindingInfo.ValidParameterSetsFlags != 0) &&
                (bindingInfo.ValidParameterSetsFlags &
                        (bindingInfo.ValidParameterSetsFlags - 1)) == 0;

            if (parameterSetSpecified &&
                (!remainingParameterSetIncludesDefault) &&
                (!onlyOneRemainingParameterSet))
            {
                ParameterBindingException bindingException =
                    new ParameterBindingException(
                        ErrorCategory.InvalidArgument,
                        null,
                        null,
                        null,
                        null,
                        null,
                        ParameterBinderStrings.AmbiguousParameterSet,
                        "AmbiguousParameterSet");
                BindingExceptions.Add(commandAst.CommandElements[0].Extent.Text,
                    new StaticBindingError(commandAst.CommandElements[0], bindingException));
            }

            // Add error for duplicate parameters
            if (bindingInfo.DuplicateParameters != null)
            {
                foreach (AstParameterArgumentPair duplicateParameter in bindingInfo.DuplicateParameters)
                {
                    AddDuplicateParameterBindingException(duplicateParameter.Parameter);
                }
            }

            // Add error for parameters not found
            if (bindingInfo.ParametersNotFound != null)
            {
                foreach (CommandParameterAst parameterNotFound in bindingInfo.ParametersNotFound)
                {
                    ParameterBindingException bindingException =
                        new ParameterBindingException(
                            ErrorCategory.InvalidArgument,
                            null,
                            parameterNotFound.ErrorPosition,
                            parameterNotFound.ParameterName,
                            null,
                            null,
                            ParameterBinderStrings.NamedParameterNotFound,
                            "NamedParameterNotFound");
                    BindingExceptions.Add(parameterNotFound.ParameterName, new StaticBindingError(parameterNotFound, bindingException));
                }
            }

            // Add error for ambiguous parameters
            if (bindingInfo.AmbiguousParameters != null)
            {
                foreach (CommandParameterAst ambiguousParameter in bindingInfo.AmbiguousParameters)
                {
                    ParameterBindingException bindingException = bindingInfo.BindingExceptions[ambiguousParameter];
                    BindingExceptions.Add(ambiguousParameter.ParameterName, new StaticBindingError(ambiguousParameter, bindingException));
                }
            }

            // Add error for unbound positional parameters
            if (bindingInfo.UnboundArguments != null)
            {
                foreach (AstParameterArgumentPair unboundArgument in bindingInfo.UnboundArguments)
                {
                    AstPair argument = unboundArgument as AstPair;

                    ParameterBindingException bindingException =
                        new ParameterBindingException(
                            ErrorCategory.InvalidArgument,
                            null,
                            argument.Argument.Extent,
                            argument.Argument.Extent.Text,
                            null,
                            null,
                            ParameterBinderStrings.PositionalParameterNotFound,
                            "PositionalParameterNotFound");
                    BindingExceptions.Add(argument.Argument.Extent.Text, new StaticBindingError(argument.Argument, bindingException));
                }
            }

            // Process the bound parameters
            if (bindingInfo.BoundParameters != null)
            {
                foreach (KeyValuePair<string, MergedCompiledCommandParameter> item in bindingInfo.BoundParameters)
                {
                    CompiledCommandParameter parameter = item.Value.Parameter;
                    CommandElementAst value = null;
                    object constantValue = null;

                    // This is a single argument
                    AstPair argumentAstPair = bindingInfo.BoundArguments[item.Key] as AstPair;
                    if (argumentAstPair != null)
                    {
                        value = argumentAstPair.Argument;
                    }

                    // This is a parameter that took an argument, as well as ValueFromRemainingArguments.
                    // Merge the arguments into a single fake argument.
                    AstArrayPair argumentAstArrayPair = bindingInfo.BoundArguments[item.Key] as AstArrayPair;
                    if (argumentAstArrayPair != null)
                    {
                        List<ExpressionAst> arguments = new List<ExpressionAst>();
                        foreach (ExpressionAst expression in argumentAstArrayPair.Argument)
                        {
                            ArrayLiteralAst expressionArray = expression as ArrayLiteralAst;
                            if (expressionArray != null)
                            {
                                foreach (ExpressionAst newExpression in expressionArray.Elements)
                                {
                                    arguments.Add((ExpressionAst)newExpression.Copy());
                                }
                            }
                            else
                            {
                                arguments.Add((ExpressionAst)expression.Copy());
                            }
                        }

                        // Define the virtual extent and virtual ArrayLiteral.
                        IScriptExtent fakeExtent = arguments[0].Extent;
                        ArrayLiteralAst fakeArguments = new ArrayLiteralAst(fakeExtent, arguments);
                        value = fakeArguments;
                    }

                    // Special handling of switch parameters
                    if (parameter.Type == typeof(SwitchParameter))
                    {
                        if ((value != null) &&
                            (string.Equals("$false", value.Extent.Text, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }

                        constantValue = true;
                    }

                    // We got a parameter and a value
                    if ((value != null) || (constantValue != null))
                    {
                        BoundParameters.Add(item.Key, new ParameterBindingResult(parameter, value, constantValue));
                    }
                    else
                    {
                        bool takesValueFromPipeline = false;
                        foreach (ParameterSetSpecificMetadata parameterSet in parameter.GetMatchingParameterSetData(bindingInfo.ValidParameterSetsFlags))
                        {
                            if (parameterSet.ValueFromPipeline)
                            {
                                takesValueFromPipeline = true;
                                break;
                            }
                        }

                        if (!takesValueFromPipeline)
                        {
                            // We have a parameter with no value that isn't a switch parameter, or input parameter
                            ParameterBindingException bindingException =
                                new ParameterBindingException(
                                    ErrorCategory.InvalidArgument,
                                    null,
                                    commandAst.CommandElements[0].Extent,
                                    parameter.Name,
                                    parameter.Type,
                                    null,
                                    ParameterBinderStrings.MissingArgument,
                                    "MissingArgument");

                            BindingExceptions.Add(commandAst.CommandElements[0].Extent.Text,
                                new StaticBindingError(commandAst.CommandElements[0], bindingException));
                        }
                    }
                }
            }
        }

        private void AddDuplicateParameterBindingException(CommandParameterAst duplicateParameter)
        {
            if (duplicateParameter == null)
            {
                return;
            }

            ParameterBindingException bindingException =
                new ParameterBindingException(
                    ErrorCategory.InvalidArgument,
                    null,
                    duplicateParameter.ErrorPosition,
                    duplicateParameter.ParameterName,
                    null,
                    null,
                    ParameterBinderStrings.ParameterAlreadyBound,
                    nameof(ParameterBinderStrings.ParameterAlreadyBound));
            // if the duplicated Parameter Name appears more than twice, we will ignore as we already have similar bindingException.
            if (!BindingExceptions.ContainsKey(duplicateParameter.ParameterName))
            {
                BindingExceptions.Add(duplicateParameter.ParameterName, new StaticBindingError(duplicateParameter, bindingException));
            }
        }

        private PseudoBindingInfo _bindingInfo = null;

        private void CreateBindingResultForSyntacticBind(CommandAst commandAst)
        {
            bool foundCommand = false;

            CommandParameterAst currentParameter = null;
            int position = 0;
            ParameterBindingResult bindingResult = new ParameterBindingResult();

            foreach (CommandElementAst commandElement in commandAst.CommandElements)
            {
                // Skip the command name
                if (!foundCommand)
                {
                    foundCommand = true;
                    continue;
                }

                CommandParameterAst parameter = commandElement as CommandParameterAst;
                if (parameter != null)
                {
                    if (currentParameter != null)
                    {
                        // Assume it was a switch
                        AddSwitch(currentParameter.ParameterName, bindingResult);
                        ResetCurrentParameter(ref currentParameter, ref bindingResult);
                    }

                    // If this is an actual parameter, get its name.
                    string parameterName = parameter.ParameterName;
                    bindingResult.Value = parameter;

                    // If it's a parameter with argument, add them both to the dictionary
                    if (parameter.Argument != null)
                    {
                        bindingResult.Value = parameter.Argument;

                        AddBoundParameter(parameter, parameterName, bindingResult);
                        ResetCurrentParameter(ref currentParameter, ref bindingResult);
                    }
                    // Otherwise, it's just a parameter and the argument is to follow.
                    else
                    {
                        // Store our current parameter
                        currentParameter = parameter;
                    }
                }
                else
                {
                    // This isn't a parameter, it's a value for the previous parameter
                    if (currentParameter != null)
                    {
                        bindingResult.Value = commandElement;
                        AddBoundParameter(currentParameter, currentParameter.ParameterName, bindingResult);
                    }
                    else
                    {
                        // Assume positional
                        bindingResult.Value = commandElement;
                        AddBoundParameter(null, position.ToString(CultureInfo.InvariantCulture), bindingResult);
                        position++;
                    }

                    ResetCurrentParameter(ref currentParameter, ref bindingResult);
                }
            }

            // Catch any extra parameters at the end of the command
            if (currentParameter != null)
            {
                // Assume it was a switch
                AddSwitch(currentParameter.ParameterName, bindingResult);
            }
        }

        private void AddBoundParameter(CommandParameterAst parameter, string parameterName, ParameterBindingResult bindingResult)
        {
            if (BoundParameters.ContainsKey(parameterName))
            {
                AddDuplicateParameterBindingException(parameter);
            }
            else
            {
                BoundParameters.Add(parameterName, bindingResult);
            }
        }

        private static void ResetCurrentParameter(ref CommandParameterAst currentParameter, ref ParameterBindingResult bindingResult)
        {
            currentParameter = null;
            bindingResult = new ParameterBindingResult();
        }

        private void AddSwitch(string currentParameter, ParameterBindingResult bindingResult)
        {
            bindingResult.ConstantValue = true;
            AddBoundParameter(null, currentParameter, bindingResult);
        }

        /// <summary>
        /// </summary>
        public Dictionary<string, ParameterBindingResult> BoundParameters { get; }

        /// <summary>
        /// </summary>
        public Dictionary<string, StaticBindingError> BindingExceptions { get; }
    }

    /// <summary>
    /// Represents the binding of a parameter to its argument.
    /// </summary>
    public class ParameterBindingResult
    {
        internal ParameterBindingResult(CompiledCommandParameter parameter, CommandElementAst value, object constantValue)
        {
            this.Parameter = new ParameterMetadata(parameter);
            this.Value = value;
            this.ConstantValue = constantValue;
        }

        internal ParameterBindingResult()
        {
        }

        /// <summary>
        /// </summary>
        public ParameterMetadata Parameter { get; internal set; }

        /// <summary>
        /// </summary>
        public object ConstantValue
        {
            get { return _constantValue; }

            internal set
            {
                if (value != null)
                {
                    _constantValue = value;
                }
            }
        }

        private object _constantValue;

        /// <summary>
        /// </summary>
        public CommandElementAst Value
        {
            get { return _value; }

            internal set
            {
                _value = value;

                ConstantExpressionAst constantValueAst = value as ConstantExpressionAst;
                if (constantValueAst != null)
                {
                    this.ConstantValue = constantValueAst.Value;
                }
            }
        }

        private CommandElementAst _value;
    }

    /// <summary>
    /// Represents the exception generated by the static parameter binding process.
    /// </summary>
    public class StaticBindingError
    {
        /// <summary>
        /// Creates a StaticBindingException.
        /// </summary>
        /// <param name="commandElement">The element associated with the exception.</param>
        /// <param name="exception">The parameter binding exception that got raised.</param>
        internal StaticBindingError(CommandElementAst commandElement, ParameterBindingException exception)
        {
            this.CommandElement = commandElement;
            this.BindingException = exception;
        }

        /// <summary>
        /// The command element associated with the exception.
        /// </summary>
        public CommandElementAst CommandElement { get; private set; }

        /// <summary>
        /// The ParameterBindingException that this command element caused.
        /// </summary>
        public ParameterBindingException BindingException { get; private set; }
    }

    #region "PseudoBindingInfo"

    internal enum PseudoBindingInfoType
    {
        PseudoBindingFail = 0,
        PseudoBindingSucceed = 1,
    }

    internal sealed class PseudoBindingInfo
    {
        /// <summary>
        /// The pseudo binding succeeded.
        /// </summary>
        /// <param name="commandInfo"></param>
        /// <param name="validParameterSetsFlags"></param>
        /// <param name="defaultParameterSetFlag"></param>
        /// <param name="boundParameters"></param>
        /// <param name="unboundParameters"></param>
        /// <param name="boundArguments"></param>
        /// <param name="boundPositionalParameter"></param>
        /// <param name="allParsedArguments"></param>
        /// <param name="parametersNotFound"></param>
        /// <param name="ambiguousParameters"></param>
        /// <param name="bindingExceptions"></param>
        /// <param name="duplicateParameters"></param>
        /// <param name="unboundArguments"></param>
        internal PseudoBindingInfo(
            CommandInfo commandInfo,
            uint validParameterSetsFlags,
            uint defaultParameterSetFlag,
            Dictionary<string, MergedCompiledCommandParameter> boundParameters,
            List<MergedCompiledCommandParameter> unboundParameters,
            Dictionary<string, AstParameterArgumentPair> boundArguments,
            Collection<string> boundPositionalParameter,
            Collection<AstParameterArgumentPair> allParsedArguments,
            Collection<CommandParameterAst> parametersNotFound,
            Collection<CommandParameterAst> ambiguousParameters,
            Dictionary<CommandParameterAst, ParameterBindingException> bindingExceptions,
            Collection<AstParameterArgumentPair> duplicateParameters,
            Collection<AstParameterArgumentPair> unboundArguments)
        {
            CommandInfo = commandInfo;
            InfoType = PseudoBindingInfoType.PseudoBindingSucceed;
            ValidParameterSetsFlags = validParameterSetsFlags;
            DefaultParameterSetFlag = defaultParameterSetFlag;
            BoundParameters = boundParameters;
            UnboundParameters = unboundParameters;
            BoundArguments = boundArguments;
            BoundPositionalParameter = boundPositionalParameter;
            AllParsedArguments = allParsedArguments;
            ParametersNotFound = parametersNotFound;
            AmbiguousParameters = ambiguousParameters;
            BindingExceptions = bindingExceptions;
            DuplicateParameters = duplicateParameters;
            UnboundArguments = unboundArguments;
        }

        /// <summary>
        /// The pseudo binding failed with parameter set confliction.
        /// </summary>
        /// <param name="commandInfo"></param>
        /// <param name="defaultParameterSetFlag"></param>
        /// <param name="allParsedArguments"></param>
        /// <param name="unboundParameters"></param>
        internal PseudoBindingInfo(
            CommandInfo commandInfo,
            uint defaultParameterSetFlag,
            Collection<AstParameterArgumentPair> allParsedArguments,
            List<MergedCompiledCommandParameter> unboundParameters)
        {
            CommandInfo = commandInfo;
            InfoType = PseudoBindingInfoType.PseudoBindingFail;
            DefaultParameterSetFlag = defaultParameterSetFlag;
            AllParsedArguments = allParsedArguments;
            UnboundParameters = unboundParameters;
        }

        internal string CommandName
        {
            get { return CommandInfo.Name; }
        }

        internal CommandInfo CommandInfo { get; }

        internal PseudoBindingInfoType InfoType { get; }

        internal uint ValidParameterSetsFlags { get; }

        internal uint DefaultParameterSetFlag { get; }

        internal Dictionary<string, MergedCompiledCommandParameter> BoundParameters { get; }

        internal List<MergedCompiledCommandParameter> UnboundParameters { get; }

        internal Dictionary<string, AstParameterArgumentPair> BoundArguments { get; }

        internal Collection<AstParameterArgumentPair> UnboundArguments { get; }

        internal Collection<string> BoundPositionalParameter { get; }

        internal Collection<AstParameterArgumentPair> AllParsedArguments { get; }

        internal Collection<CommandParameterAst> ParametersNotFound { get; }

        internal Collection<CommandParameterAst> AmbiguousParameters { get; }

        internal Dictionary<CommandParameterAst, ParameterBindingException> BindingExceptions { get; }

        internal Collection<AstParameterArgumentPair> DuplicateParameters { get; }
    }

    #endregion "PseudoBindingInfo"

    internal class PseudoParameterBinder
    {
        /*
        /// <summary>
        /// Get the parameter binding metadata.
        /// </summary>
        /// <param name="possibleParameterSets"></param>
        /// <returns></returns>
        public Dictionary<ParameterMetadata, ExpressionAst> GetPseudoParameterBinding(out Collection<ParameterSetMetadata> possibleParameterSets)
        {
            ExecutionContext contextFromTls =
                System.Management.Automation.Runspaces.LocalPipeline.GetExecutionContextFromTLS();
            return GetPseudoParameterBinding(out possibleParameterSets, contextFromTls, null);
        }
        */

        internal enum BindingType
        {
            /// <summary>
            /// Caller is binding a parameter argument.
            /// </summary>
            ArgumentBinding = 0,

            /// <summary>
            /// Caller is performing completion on a parameter argument.
            /// </summary>
            ArgumentCompletion,

            /// <summary>
            /// Caller is performing completion on a parameter name.
            /// </summary>
            ParameterCompletion
        }

        /// <summary>
        /// Get the parameter binding metadata.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="pipeArgumentType">Indicate the type of the piped-in argument.</param>
        /// <param name="paramAstAtCursor">The CommandParameterAst the cursor is pointing at.</param>
        /// <param name="bindingType">Indicates whether pseudo binding is for argument binding, argument completion, or parameter completion.</param>
        /// <returns>PseudoBindingInfo.</returns>
        internal PseudoBindingInfo DoPseudoParameterBinding(CommandAst command, Type pipeArgumentType, CommandParameterAst paramAstAtCursor, BindingType bindingType)
        {
            if (command == null)
            {
                throw PSTraceSource.NewArgumentNullException("command");
            }

            // initialize/reset the private members
            InitializeMembers();
            _commandAst = command;
            _commandElements = command.CommandElements;
            Collection<AstParameterArgumentPair> unboundArguments = new Collection<AstParameterArgumentPair>();

            // analyze the command and reparse the arguments
            {
                ExecutionContext executionContext = LocalPipeline.GetExecutionContextFromTLS();
                if (executionContext != null)
                {
                    // WinBlue: 324316. This limits the interaction of pseudoparameterbinder with the actual host.
                    SetTemporaryDefaultHost(executionContext);

                    PSLanguageMode? previousLanguageMode = null;
                    try
                    {
                        // Tab expansion is called from a trusted function - we should apply ConstrainedLanguage if necessary.
                        if (executionContext.HasRunspaceEverUsedConstrainedLanguageMode)
                        {
                            previousLanguageMode = executionContext.LanguageMode;
                            executionContext.LanguageMode = PSLanguageMode.ConstrainedLanguage;
                        }

                        _bindingEffective = PrepareCommandElements(executionContext);
                    }
                    finally
                    {
                        if (previousLanguageMode.HasValue)
                        {
                            executionContext.LanguageMode = previousLanguageMode.Value;
                        }

                        RestoreHost(executionContext);
                    }
                }
            }

            if (_bindingEffective && (_isPipelineInputExpected || pipeArgumentType != null))
            {
                _pipelineInputType = pipeArgumentType;
            }

            _bindingEffective = ParseParameterArguments(paramAstAtCursor);

            if (_bindingEffective)
            {
                // named binding
                unboundArguments = BindNamedParameters();
                _bindingEffective = _currentParameterSetFlag != 0;

                // positional binding
                unboundArguments = BindPositionalParameter(
                    unboundArguments,
                    _currentParameterSetFlag,
                    _defaultParameterSetFlag,
                    bindingType);

                // VFRA/pipeline binding if the given command is a binary cmdlet or a script cmdlet
                if (!_function)
                {
                    unboundArguments = BindRemainingParameters(unboundArguments);
                    BindPipelineParameters();
                }

                // Update available parameter sets based on bound arguments
                // (x & (x -1 ) == 0) is a bit hack to determine if something is
                // exactly a power of two.
                bool parameterSetSpecified = (_currentParameterSetFlag != 0) &&
                    (_currentParameterSetFlag != UInt32.MaxValue);
                bool onlyOneRemainingParameterSet = (_currentParameterSetFlag != 0) &&
                    (_currentParameterSetFlag & (_currentParameterSetFlag - 1)) == 0;
                if ((bindingType != BindingType.ParameterCompletion) && parameterSetSpecified && (!onlyOneRemainingParameterSet))
                {
                    CmdletParameterBinderController.ResolveParameterSetAmbiguityBasedOnMandatoryParameters(
                        _boundParameters,
                        _unboundParameters,
                        null,
                        ref _currentParameterSetFlag,
                        null);
                }
            }

            // Binding failed
            if (!_bindingEffective)
            {
                // The command is not a cmdlet, not a script cmdlet, and not a function
                if (_bindableParameters == null)
                    return null;

                // get all bindable parameters
                _unboundParameters.Clear();
                _unboundParameters.AddRange(_bindableParameters.BindableParameters.Values);

                return new PseudoBindingInfo(
                    _commandInfo,
                    _defaultParameterSetFlag,
                    _arguments,
                    _unboundParameters);
            }

            return new PseudoBindingInfo(
                _commandInfo,
                _currentParameterSetFlag,
                _defaultParameterSetFlag,
                _boundParameters,
                _unboundParameters,
                _boundArguments,
                _boundPositionalParameter,
                _arguments,
                _parametersNotFound,
                _ambiguousParameters,
                _bindingExceptions,
                _duplicateParameters,
                unboundArguments
                );
        }

        /// <summary>
        /// Sets a temporary default host on the ExecutionContext.
        /// </summary>
        /// <param name="executionContext">ExecutionContext.</param>
        private void SetTemporaryDefaultHost(ExecutionContext executionContext)
        {
            if (executionContext.EngineHostInterface.IsHostRefSet)
            {
                // A temporary host is already set so we need to track and restore here, because
                // setting the host again will overwrite the current one.
                _restoreHost = executionContext.EngineHostInterface.ExternalHost;

                // Revert host back to its original state.
                executionContext.EngineHostInterface.RevertHostRef();
            }

            // Temporarily set host to default.
            executionContext.EngineHostInterface.SetHostRef(new Microsoft.PowerShell.DefaultHost(
                CultureInfo.CurrentCulture,
                CultureInfo.CurrentUICulture));
        }

        /// <summary>
        /// Restores original ExecutionContext host state.
        /// </summary>
        /// <param name="executionContext">ExecutionContext.</param>
        private void RestoreHost(ExecutionContext executionContext)
        {
            // Remove temporary host and revert to original.
            executionContext.EngineHostInterface.RevertHostRef();

            // Re-apply saved host if any.
            if (_restoreHost != null)
            {
                executionContext.EngineHostInterface.SetHostRef(_restoreHost);
                _restoreHost = null;
            }
        }

        // Host to restore.
        private PSHost _restoreHost;

        // command ast related states
        private CommandAst _commandAst;
        private ReadOnlyCollection<CommandElementAst> _commandElements;

        // binding related states
        private bool _function = false;
        private string _commandName = null;
        private CommandInfo _commandInfo = null;
        private uint _currentParameterSetFlag = uint.MaxValue;
        private uint _defaultParameterSetFlag = 0;
        private MergedCommandParameterMetadata _bindableParameters = null;
        private Dictionary<string, MergedCompiledCommandParameter> _boundParameters;
        private Dictionary<string, AstParameterArgumentPair> _boundArguments;
        private Collection<AstParameterArgumentPair> _arguments;
        private Collection<string> _boundPositionalParameter;
        private List<MergedCompiledCommandParameter> _unboundParameters;

        // tab expansion related states
        private Type _pipelineInputType = null;
        private bool _bindingEffective = true;
        private bool _isPipelineInputExpected = false;
        private Collection<CommandParameterAst> _parametersNotFound;
        private Collection<CommandParameterAst> _ambiguousParameters;
        private Collection<AstParameterArgumentPair> _duplicateParameters;
        private Dictionary<CommandParameterAst, ParameterBindingException> _bindingExceptions;

        /// <summary>
        /// Initialize collection/dictionary members when it's necessary.
        /// </summary>
        private void InitializeMembers()
        {
            // Initializing binding related members
            _function = false;
            _commandName = null;
            _currentParameterSetFlag = uint.MaxValue;
            _defaultParameterSetFlag = 0;
            _bindableParameters = null;

            // reuse the collections/dictionaries
            _arguments = _arguments ?? new Collection<AstParameterArgumentPair>();
            _boundParameters = _boundParameters ?? new Dictionary<string, MergedCompiledCommandParameter>(StringComparer.OrdinalIgnoreCase);
            _boundArguments = _boundArguments ?? new Dictionary<string, AstParameterArgumentPair>(StringComparer.OrdinalIgnoreCase);
            _unboundParameters = _unboundParameters ?? new List<MergedCompiledCommandParameter>();
            _boundPositionalParameter = _boundPositionalParameter ?? new Collection<string>();
            _bindingExceptions = _bindingExceptions ?? new Dictionary<CommandParameterAst, ParameterBindingException>();

            _arguments.Clear();
            _boundParameters.Clear();
            _unboundParameters.Clear();
            _boundArguments.Clear();
            _boundPositionalParameter.Clear();
            _bindingExceptions.Clear();

            // Initializing tab expansion related members
            _pipelineInputType = null;
            _bindingEffective = true;
            _isPipelineInputExpected = false;

            // reuse the collections
            _parametersNotFound = _parametersNotFound ?? new Collection<CommandParameterAst>();
            _ambiguousParameters = _ambiguousParameters ?? new Collection<CommandParameterAst>();
            _duplicateParameters = _duplicateParameters ?? new Collection<AstParameterArgumentPair>();

            _parametersNotFound.Clear();
            _ambiguousParameters.Clear();
            _duplicateParameters.Clear();
        }

        private bool PrepareCommandElements(ExecutionContext context)
        {
            int commandIndex = 0;
            bool dotSource = _commandAst.InvocationOperator == TokenKind.Dot;

            CommandProcessorBase processor = null;
            string commandName = null;
            try
            {
                processor = PrepareFromAst(context, out commandName) ?? context.CreateCommand(commandName, dotSource);
            }
            catch (RuntimeException)
            {
                // Failed to create the CommandProcessor;
                return false;
            }

            var commandProcessor = processor as CommandProcessor;
            var scriptProcessor = processor as ScriptCommandProcessorBase;
            bool implementsDynamicParameters = commandProcessor != null &&
                                               commandProcessor.CommandInfo.ImplementsDynamicParameters;

            var argumentsToGetDynamicParameters = implementsDynamicParameters
                                                      ? new List<object>(_commandElements.Count)
                                                      : null;
            if (commandProcessor != null || scriptProcessor != null)
            {
                // Pre-processing the arguments -- command arguments
                for (commandIndex++; commandIndex < _commandElements.Count; commandIndex++)
                {
                    var parameter = _commandElements[commandIndex] as CommandParameterAst;
                    if (parameter != null)
                    {
                        if (argumentsToGetDynamicParameters != null)
                        {
                            argumentsToGetDynamicParameters.Add(parameter.Extent.Text);
                        }

                        AstPair parameterArg = parameter.Argument != null
                            ? new AstPair(parameter)
                            : new AstPair(parameter, (ExpressionAst)null);

                        _arguments.Add(parameterArg);
                    }
                    else
                    {
                        var dash = _commandElements[commandIndex] as StringConstantExpressionAst;
                        if (dash != null && dash.Value.Trim().Equals("-", StringComparison.OrdinalIgnoreCase))
                        {
                            // "-" is represented by StringConstantExpressionAst. Most likely the user type a tab here,
                            // and we don't want it be treated as an argument
                            continue;
                        }

                        var expressionArgument = _commandElements[commandIndex] as ExpressionAst;
                        if (expressionArgument != null)
                        {
                            argumentsToGetDynamicParameters?.Add(expressionArgument.Extent.Text);

                            _arguments.Add(new AstPair(null, expressionArgument));
                        }
                    }
                }
            }

            if (commandProcessor != null)
            {
                _function = false;
                if (implementsDynamicParameters)
                {
                    ParameterBinderController.AddArgumentsToCommandProcessor(commandProcessor, argumentsToGetDynamicParameters.ToArray());
                    bool retryWithNoArgs = false, alreadyRetried = false;

                    do
                    {
                        CommandProcessorBase oldCurrentCommandProcessor = context.CurrentCommandProcessor;
                        try
                        {
                            context.CurrentCommandProcessor = commandProcessor;
                            commandProcessor.SetCurrentScopeToExecutionScope();
                            // Run method "BindCommandLineParametersNoValidation" to get all available parameters, including the dynamic
                            // parameters (some of them, not necessarily all. Since we don't do the actual binding, some dynamic parameters
                            // might not be retrieved).
                            if (!retryWithNoArgs)
                            {
                                // Win8 345299: First try with all unbounded arguments
                                commandProcessor.CmdletParameterBinderController.BindCommandLineParametersNoValidation(commandProcessor.arguments);
                            }
                            else
                            {
                                // Win8 345299: If the first try ended with ParameterBindingException, try again with no arguments
                                alreadyRetried = true;
                                commandProcessor.CmdletParameterBinderController.ClearUnboundArguments();
                                commandProcessor.CmdletParameterBinderController.BindCommandLineParametersNoValidation(new Collection<CommandParameterInternal>());
                            }
                        }
                        catch (ParameterBindingException e)
                        {
                            // Catch the parameter binding exception thrown when Reparsing the argument.
                            //   "MissingArgument" - a single parameter is matched, but no argument is present
                            //   "AmbiguousParameter" - multiple parameters are matched
                            // When such exceptions are caught, retry again without arguments, so as to get dynamic parameters
                            // based on the current provider
                            if (e.ErrorId == "MissingArgument" || e.ErrorId == "AmbiguousParameter")
                                retryWithNoArgs = true;
                        }
                        catch (Exception)
                        {
                        }
                        finally
                        {
                            context.CurrentCommandProcessor = oldCurrentCommandProcessor;
                            commandProcessor.RestorePreviousScope();
                        }
                    } while (retryWithNoArgs && !alreadyRetried);
                }
                // Get all bindable parameters and initialize the _unboundParameters
                _commandInfo = commandProcessor.CommandInfo;
                _commandName = commandProcessor.CommandInfo.Name;
                _bindableParameters = commandProcessor.CmdletParameterBinderController.BindableParameters;
                _defaultParameterSetFlag = commandProcessor.CommandInfo.CommandMetadata.DefaultParameterSetFlag;
            }
            else if (scriptProcessor != null)
            {
                _function = true;
                _commandInfo = scriptProcessor.CommandInfo;
                _commandName = scriptProcessor.CommandInfo.Name;
                _bindableParameters = scriptProcessor.ScriptParameterBinderController.BindableParameters;
                _defaultParameterSetFlag = 0;
            }
            else
            {
                // The command is not a function, cmdlet and script cmdlet
                return false;
            }

            _unboundParameters.AddRange(_bindableParameters.BindableParameters.Values);

            // Pre-processing the arguments -- pipeline input
            // Check if there is pipeline input
            CommandBaseAst preCmdBaseAst = null;
            var pipe = _commandAst.Parent as PipelineAst;
            Diagnostics.Assert(pipe != null, "CommandAst should has a PipelineAst parent");
            if (pipe.PipelineElements.Count > 1)
            {
                foreach (CommandBaseAst cmdBase in pipe.PipelineElements)
                {
                    if (cmdBase.GetHashCode() == _commandAst.GetHashCode())
                    {
                        _isPipelineInputExpected = preCmdBaseAst != null;
                        if (_isPipelineInputExpected)
                            _pipelineInputType = typeof(object);
                        break;
                    }

                    preCmdBaseAst = cmdBase;
                }
            }

            return true;
        }

        private CommandProcessorBase PrepareFromAst(ExecutionContext context, out string resolvedCommandName)
        {
            // Analyze the Ast
            var exportVisitor = new ExportVisitor(forCompletion: true);
            Ast ast = _commandAst;
            while (ast.Parent != null)
            {
                ast = ast.Parent;
            }

            ast.Visit(exportVisitor);

            CommandProcessorBase commandProcessor = null;

            resolvedCommandName = _commandAst.GetCommandName();
            if (resolvedCommandName != null)
            {
                string alias;
                int resolvedAliasCount = 0;

                while (exportVisitor.DiscoveredAliases.TryGetValue(resolvedCommandName, out alias))
                {
                    resolvedAliasCount += 1;
                    if (resolvedAliasCount > 5)
                        break;  // give up, assume it's recursive
                    resolvedCommandName = alias;
                }

                FunctionDefinitionAst functionDefinitionAst;
                if (exportVisitor.DiscoveredFunctions.TryGetValue(resolvedCommandName, out functionDefinitionAst))
                {
                    var scriptBlock = new ScriptBlock(functionDefinitionAst, functionDefinitionAst.IsFilter);
                    commandProcessor = CommandDiscovery.CreateCommandProcessorForScript(scriptBlock, context, true, context.EngineSessionState);
                }
            }

            return commandProcessor;
        }

        /// <summary>
        /// Parse the arguments to process switch parameters and parameters without a value
        /// specified. We always eat the error (such as parameter without value) and continue
        /// to do the binding.
        /// </summary>
        /// <param name="paramAstAtCursor">
        /// For parameter completion, if the cursor is pointing at a CommandParameterAst, we
        /// should not try exact matching for that CommandParameterAst. This is to handle the
        /// following case:
        ///     Add-Computer -domain(tab)
        /// Add-Computer has an alias "Domain" that can exactly match this partial input, but
        /// since the user is typing 'tab', the partial input 'domain' should not be considered
        /// as an exact match. In this case, we don't try exact matching when calling
        /// GetMatchingParameter(..) so as to preserve other possibilities.
        /// </param>
        private bool ParseParameterArguments(CommandParameterAst paramAstAtCursor)
        {
            if (!_bindingEffective)
                return _bindingEffective;

            var result = new Collection<AstParameterArgumentPair>();
            for (int index = 0; index < _arguments.Count; index++)
            {
                AstParameterArgumentPair argument = _arguments[index];
                if (!argument.ParameterSpecified || argument.ArgumentSpecified)
                {
                    // Add the positional/named arguments back
                    result.Add(argument);
                    continue;
                }

                Diagnostics.Assert(argument.ParameterSpecified && !argument.ArgumentSpecified,
                    "At this point, the parameters should have no arguments");

                // Now check the parameter name with the bindable parameters
                string parameterName = argument.ParameterName;
                MergedCompiledCommandParameter matchingParameter = null;

                try
                {
                    bool tryExactMatching = argument.Parameter != paramAstAtCursor;
                    matchingParameter = _bindableParameters.GetMatchingParameter(parameterName, false, tryExactMatching, null);
                }
                catch (ParameterBindingException e)
                {
                    // The parameterName is resolved to multiple parameters. The most possible scenario for this
                    // would be the user typing tab to complete a parameter. In this case, we can ignore this
                    // parameter safely.

                    // If the next item is a pure argument, we skip it so that it doesn't get bound
                    // positionally.
                    if (index < _arguments.Count - 1)
                    {
                        AstParameterArgumentPair nextArg = _arguments[index + 1];
                        if (!nextArg.ParameterSpecified && nextArg.ArgumentSpecified)
                        {
                            index++;
                        }
                    }

                    _ambiguousParameters.Add(argument.Parameter);
                    _bindingExceptions[argument.Parameter] = e;

                    continue;
                }

                if (matchingParameter == null)
                {
                    // The parameter cannot be found. The reason could be:
                    // 1. It's a bynamic parameter, and we cannot retrieve the ParameterMetadata for it
                    //    at this point, since it's pseudo binding.
                    // 2. The spelling of this parameter is wrong.
                    // We can simply ignore this parameter, but the issue is what to do with the argument
                    // following this parameter (if there is an argument following it). There are two cases:
                    // 1. This parameter is supposed to be a switch parameter. Then the argument following it
                    //    should NOT be ignored.
                    // 2. This parameter is supposed to take an argument. Then the following argument should
                    //    also be ignored
                    // We check the next item. If it's a pure argument, we give up the binding, because we don't
                    // know how to deal with it (ignore it? keep it?), and it will affect the accuracy of our
                    // parameter set resolution.
                    if (index < _arguments.Count - 1)
                    {
                        AstParameterArgumentPair nextArg = _arguments[index + 1];

                        // If the next item is a pure argument, we give up the pseudo binding.
                        if (!nextArg.ParameterSpecified && nextArg.ArgumentSpecified)
                        {
                            // Testing paramsAstAtCursor ensures we only give up during tab completion,
                            // otherwise we know this is a missing parameter.
                            if (paramAstAtCursor != null)
                            {
                                // Do not use the parsed arguments
                                _arguments = null;
                                return false;
                            }
                            else
                            {
                                // Otherwise, skip the next argument
                                index++;
                                _parametersNotFound.Add(argument.Parameter);
                                continue;
                            }
                        }
                    }

                    // If the next item is not a pure argument, or the current parameter is the last item,
                    // ignore this parameter and carry on with the binding
                    _parametersNotFound.Add(argument.Parameter);
                    continue;
                }

                // Check if it's SwitchParameter
                if (matchingParameter.Parameter.Type == typeof(SwitchParameter))
                {
                    SwitchPair newArg = new SwitchPair(argument.Parameter);
                    result.Add(newArg);
                    continue;
                }

                // It's not a switch parameter, we need to check the next argument
                if (index < _arguments.Count - 1)
                {
                    AstParameterArgumentPair nextArg = _arguments[index + 1];
                    if (nextArg.ParameterSpecified)
                    {
                        try
                        {
                            MergedCompiledCommandParameter nextMatchingParameter =
                                _bindableParameters.GetMatchingParameter(nextArg.ParameterName, false, true, null);
                            // The next parameter doesn't exist. We use it as an argument
                            if (nextMatchingParameter == null)
                            {
                                AstPair newArg = new AstPair(argument.Parameter, nextArg.Parameter);
                                result.Add(newArg);
                                index++;
                            }
                            else
                            {
                                // It's possible the user is typing tab for argument completion.
                                // We set a fake argument for the current parameter in this case.
                                FakePair newArg = new FakePair(argument.Parameter);
                                result.Add(newArg);
                            }
                        }
                        catch (ParameterBindingException)
                        {
                            // The next parameter name is ambiguous. We just set
                            // a fake argument for the current parameter.
                            FakePair newArg = new FakePair(argument.Parameter);
                            result.Add(newArg);
                        }
                    }
                    else
                    {
                        // The next item is a pure argument.
                        AstPair nextArgument = nextArg as AstPair;
                        Diagnostics.Assert(nextArgument != null, "the next item should be a pure argument here");
                        Diagnostics.Assert(nextArgument.ArgumentSpecified && !nextArgument.ArgumentIsCommandParameterAst, "the next item should be a pure argument here");

                        AstPair newArg = new AstPair(argument.Parameter, (ExpressionAst)nextArgument.Argument);
                        result.Add(newArg);
                        index++;
                    }
                }
                else
                {
                    // The current parameter is the last item. Set a fake argument for it
                    FakePair newArg = new FakePair(argument.Parameter);
                    result.Add(newArg);
                }
            }

            _arguments = result;
            return true;
        }

        private Collection<AstParameterArgumentPair> BindNamedParameters()
        {
            Collection<AstParameterArgumentPair> result = new Collection<AstParameterArgumentPair>();

            if (!_bindingEffective)
                return result;

            foreach (AstParameterArgumentPair argument in _arguments)
            {
                if (!argument.ParameterSpecified)
                {
                    result.Add(argument);
                    continue;
                }

                MergedCompiledCommandParameter parameter = null;
                try
                {
                    parameter = _bindableParameters.GetMatchingParameter(argument.ParameterName, false, true, null);
                }
                catch (ParameterBindingException)
                {
                    // The parameter name is ambiguous. It's not processed in ParseParameterArguments. Otherwise we
                    // should detect it early. So this argument comes from a CommandParameterAst with argument. We
                    // ignore it and carry on with our binding
                    _ambiguousParameters.Add(argument.Parameter);
                    continue;
                }

                if (parameter == null)
                {
                    // Cannot find a matching parameter. It's not processed in ParseParameterArguments. It comes from
                    // a CommandParameterAst with argument. We ignore it and carry on with our binding
                    _parametersNotFound.Add(argument.Parameter);
                    continue;
                }

                if (_boundParameters.ContainsKey(parameter.Parameter.Name))
                {
                    // This parameter is already bound. We ignore it and carry on with the binding.
                    _duplicateParameters.Add(argument);
                    continue;
                }

                // The parameter exists and is not bound yet. We assume the binding will always succeed.
                if (parameter.Parameter.ParameterSetFlags != 0)
                {
                    _currentParameterSetFlag &= parameter.Parameter.ParameterSetFlags;
                }

                _unboundParameters.Remove(parameter);

                if (!_boundParameters.ContainsKey(parameter.Parameter.Name))
                {
                    _boundParameters.Add(parameter.Parameter.Name, parameter);
                }

                if (!_boundArguments.ContainsKey(parameter.Parameter.Name))
                {
                    _boundArguments.Add(parameter.Parameter.Name, argument);
                }
            }

            return result;
        }

        private Collection<AstParameterArgumentPair> BindPositionalParameter(
            Collection<AstParameterArgumentPair> unboundArguments,
            uint validParameterSetFlags,
            uint defaultParameterSetFlag,
            BindingType bindingType)
        {
            Collection<AstParameterArgumentPair> result = new Collection<AstParameterArgumentPair>();

            if (_bindingEffective && unboundArguments.Count > 0)
            {
                List<AstParameterArgumentPair> unboundArgumentsCollection = new List<AstParameterArgumentPair>(unboundArguments);
                // Get the unbound positional parameters
                SortedDictionary<int, Dictionary<MergedCompiledCommandParameter, PositionalCommandParameter>> positionalParameterDictionary;
                try
                {
                    positionalParameterDictionary =
                        ParameterBinderController.EvaluateUnboundPositionalParameters(_unboundParameters, validParameterSetFlags);
                }
                catch (InvalidOperationException)
                {
                    // This exception is thrown because the binder found two positional parameters
                    // from the same parameter set with the same position defined. The parameter definition
                    // is ambiguous. We give up binding in this case
                    _bindingEffective = false;
                    return result;
                }

                // No positional parameter available
                if (positionalParameterDictionary.Count == 0)
                    return unboundArguments;

                int unboundArgumentsIndex = 0;
                foreach (Dictionary<MergedCompiledCommandParameter, PositionalCommandParameter> nextPositionalParameters in positionalParameterDictionary.Values)
                {
                    if (nextPositionalParameters.Count == 0)
                    {
                        continue;
                    }

                    AstParameterArgumentPair argument = GetNextPositionalArgument(
                        unboundArgumentsCollection,
                        result,
                        ref unboundArgumentsIndex);

                    if (argument == null)
                    {
                        break;
                    }

                    // The positional pseudo binding is processed in two different approaches for parameter completion and parameter argument completion.
                    // - For parameter completion, we do NOT honor the default parameter set, so we can preserve potential parameters as many as possible.
                    //   Example:
                    //           Where-Object PropertyA -<tab>
                    //   If the default parameter is honored, the completion results only contain EQ, because it's locked to the default set
                    //
                    // - For parameter argument completion, however, we want to honor the default parameter set some times, especially when the argument
                    //   can be bound to the positional parameter from the default set WITHOUT type coercion.
                    //   Example:
                    //           Set-Location c:\win<tab>
                    //   In this scenario, the user actually intends to use -Path implicitly, and we should not preserve the -LiteralPath. But if we fail
                    //   on the attempt with the (default set + no coercion), we should fall back to the (all valid set + with coercion) to preserve possibilities.
                    //   Example:
                    //           Add-Member notep<tab>
                    //   We need presever the -MemberType along with the -NotePropertyName in this case.
                    //
                    // So the algorithm for positional binding is:
                    // - With bindingType == ParameterCompletion
                    //   Skip the attempt with the default set, as well as the attempt with all sets but no coercion.
                    //   Do the positional binding with the (all valid set + with coercion) directly.
                    //
                    // - With bindingType == ArgumentCompletion  (parameter argument completion)
                    //   First try to do positional binding with (default set + no coercion)
                    //   If the first attempt fails, do positional binding with (all valid set + with coercion)
                    //
                    // - With bindingType == ArgumentBinding (parameter argument binding, no completion)
                    //   First try to do positional binding with (default set + no coercion)
                    //   If the first attempt fails, do positional binding with (all valid set + without coercion)
                    //   If the second attempt fails, do positional binding with (all valid set + with coercion)

                    bool aParameterGetBound = false;
                    if ((bindingType != BindingType.ParameterCompletion) && ((validParameterSetFlags & defaultParameterSetFlag) != 0))
                    {
                        // Default set, no coercion.
                        aParameterGetBound =
                            BindPseudoPositionalParameterInSet(
                                defaultParameterSetFlag,
                                nextPositionalParameters,
                                argument,
                                false);
                    }

                    if (!aParameterGetBound && (bindingType == BindingType.ArgumentBinding))
                    {
                        // All valid sets, no coercion.
                        aParameterGetBound =
                            BindPseudoPositionalParameterInSet(
                                validParameterSetFlags,
                                nextPositionalParameters,
                                argument,
                                false);
                    }

                    if (!aParameterGetBound)
                    {
                        // All valid sets, with coercion.
                        aParameterGetBound =
                            BindPseudoPositionalParameterInSet(
                                validParameterSetFlags,
                                nextPositionalParameters,
                                argument,
                                true);
                    }

                    if (!aParameterGetBound)
                    {
                        result.Add(argument);
                    }
                    else
                    {
                        // Update the parameter sets if necessary
                        if (validParameterSetFlags != _currentParameterSetFlag)
                        {
                            validParameterSetFlags = _currentParameterSetFlag;
                            ParameterBinderController.UpdatePositionalDictionary(positionalParameterDictionary, validParameterSetFlags);
                        }
                    }
                }

                for (int index = unboundArgumentsIndex; index < unboundArgumentsCollection.Count; ++index)
                {
                    result.Add(unboundArgumentsCollection[index]);
                }
            }

            return result;
        }

        private bool BindPseudoPositionalParameterInSet(
            uint validParameterSetFlag,
            Dictionary<MergedCompiledCommandParameter, PositionalCommandParameter> nextPositionalParameters,
            AstParameterArgumentPair argument,
            bool typeConversion)
        {
            bool bindingSuccessful = false;
            uint localParameterSetFlag = 0;
            foreach (PositionalCommandParameter parameter in nextPositionalParameters.Values)
            {
                foreach (ParameterSetSpecificMetadata parameterSetData in parameter.ParameterSetData)
                {
                    // Skip it if it's not in the specified parameter set
                    if ((validParameterSetFlag & parameterSetData.ParameterSetFlag) == 0 &&
                        !parameterSetData.IsInAllSets)
                    {
                        continue;
                    }

                    bool result = false;
                    string parameterName = parameter.Parameter.Parameter.Name;
                    Type parameterType = parameter.Parameter.Parameter.Type;
                    Type argumentType = argument.ArgumentType;

                    // 1. the argument type is not known(typeof(object)). we assume the binding always succeeds
                    // 2. the argument type is the same as parameter type, we assume the binding succeeds
                    // 3. the types are not the same, but we allow conversion, we assume the binding succeeds
                    // 4. the types are not the same, and conversion is not allowed, we assume the binding fails
                    if (argumentType == typeof(object))
                        bindingSuccessful = result = true;
                    else if (IsTypeEquivalent(argumentType, parameterType))
                        bindingSuccessful = result = true;
                    else if (typeConversion)
                        bindingSuccessful = result = true;

                    if (result)
                    {
                        localParameterSetFlag |= parameter.Parameter.Parameter.ParameterSetFlags;
                        _unboundParameters.Remove(parameter.Parameter);

                        if (!_boundParameters.ContainsKey(parameterName))
                        {
                            _boundParameters.Add(parameterName, parameter.Parameter);
                            _boundPositionalParameter.Add(parameterName);
                        }

                        if (!_boundArguments.ContainsKey(parameterName))
                        {
                            _boundArguments.Add(parameterName, argument);
                        }

                        break;
                    }
                }
            }

            // We preserve all possibilities
            if (bindingSuccessful && localParameterSetFlag != 0)
                _currentParameterSetFlag &= localParameterSetFlag;
            return bindingSuccessful;
        }

        private static bool IsTypeEquivalent(Type argType, Type paramType)
        {
            bool result = false;
            if (argType == paramType)
            {
                result = true;
            }
            else if (argType.IsSubclassOf(paramType))
            {
                result = true;
            }
            else if (argType == paramType.GetElementType())
            {
                result = true;
            }
            else if (argType.IsSubclassOf(typeof(Array)) && paramType.IsSubclassOf(typeof(Array)))
            {
                result = true;
            }

            return result;
        }

        private static AstParameterArgumentPair GetNextPositionalArgument(
            List<AstParameterArgumentPair> unboundArgumentsCollection,
            Collection<AstParameterArgumentPair> nonPositionalArguments,
            ref int unboundArgumentsIndex)
        {
            // Find the next positional parameter. An argument without the parameter being
            // specified is considered to be a positional argument
            AstParameterArgumentPair result = null;
            while (unboundArgumentsIndex < unboundArgumentsCollection.Count)
            {
                AstParameterArgumentPair argument = unboundArgumentsCollection[unboundArgumentsIndex++];
                if (!argument.ParameterSpecified)
                {
                    result = argument;
                    break;
                }

                nonPositionalArguments.Add(argument);
            }

            return result;
        }

        private Collection<AstParameterArgumentPair> BindRemainingParameters(Collection<AstParameterArgumentPair> unboundArguments)
        {
            bool result = false;
            uint localParameterSetFlag = 0;

            if (!_bindingEffective || unboundArguments.Count == 0)
                return unboundArguments;

            Collection<ExpressionAst> argList = new Collection<ExpressionAst>();
            foreach (AstParameterArgumentPair arg in unboundArguments)
            {
                AstPair realArg = arg as AstPair;
                Diagnostics.Assert(realArg != null && !realArg.ParameterSpecified && !realArg.ArgumentIsCommandParameterAst,
                    "all unbound arguments left should be pure ExpressionAst arguments");
                argList.Add((ExpressionAst)realArg.Argument);
            }

            var unboundParametersCopy = new List<MergedCompiledCommandParameter>(_unboundParameters);

            foreach (MergedCompiledCommandParameter unboundParam in unboundParametersCopy)
            {
                bool isInParameterSet = (unboundParam.Parameter.ParameterSetFlags & _currentParameterSetFlag) != 0 ||
                                        unboundParam.Parameter.IsInAllSets;
                if (!isInParameterSet)
                {
                    continue;
                }

                var parameterSetDataCollection = unboundParam.Parameter.GetMatchingParameterSetData(_currentParameterSetFlag);
                foreach (ParameterSetSpecificMetadata parameterSetData in parameterSetDataCollection)
                {
                    if (!parameterSetData.ValueFromRemainingArguments)
                    {
                        continue;
                    }

                    localParameterSetFlag |= unboundParam.Parameter.ParameterSetFlags;
                    string parameterName = unboundParam.Parameter.Name;
                    _unboundParameters.Remove(unboundParam);

                    if (!_boundParameters.ContainsKey(parameterName))
                    {
                        _boundParameters.Add(parameterName, unboundParam);
                    }

                    if (!_boundArguments.ContainsKey(parameterName))
                    {
                        _boundArguments.Add(parameterName, new AstArrayPair(parameterName, argList));
                        unboundArguments.Clear();
                    }

                    result = true;
                    break;
                }
            }

            if (result && localParameterSetFlag != 0)
                _currentParameterSetFlag &= localParameterSetFlag;

            return unboundArguments;
        }

        private void BindPipelineParameters()
        {
            bool result = false;
            uint localParameterSetFlag = 0;

            if (!_bindingEffective || !_isPipelineInputExpected)
            {
                return;
            }

            var unboundParametersCopy = new List<MergedCompiledCommandParameter>(_unboundParameters);

            foreach (MergedCompiledCommandParameter unboundParam in unboundParametersCopy)
            {
                if (!unboundParam.Parameter.IsPipelineParameterInSomeParameterSet)
                    continue;

                bool isInParameterSet = (unboundParam.Parameter.ParameterSetFlags & _currentParameterSetFlag) != 0 ||
                                        unboundParam.Parameter.IsInAllSets;
                if (!isInParameterSet)
                {
                    continue;
                }

                var parameterSetDataCollection = unboundParam.Parameter.GetMatchingParameterSetData(_currentParameterSetFlag);
                foreach (ParameterSetSpecificMetadata parameterSetData in parameterSetDataCollection)
                {
                    // We don't assume the 'ValueFromPipelineByPropertyName' parameters get bound
                    if (!parameterSetData.ValueFromPipeline)
                    {
                        continue;
                    }

                    localParameterSetFlag |= unboundParam.Parameter.ParameterSetFlags;
                    string parameterName = unboundParam.Parameter.Name;
                    _unboundParameters.Remove(unboundParam);

                    if (!_boundParameters.ContainsKey(parameterName))
                    {
                        _boundParameters.Add(parameterName, unboundParam);
                    }

                    if (!_boundArguments.ContainsKey(parameterName))
                    {
                        _boundArguments.Add(parameterName, new PipeObjectPair(parameterName, _pipelineInputType));
                    }

                    result = true;
                    break;
                }
            }

            if (result && localParameterSetFlag != 0)
                _currentParameterSetFlag &= localParameterSetFlag;
        }
    }
}
