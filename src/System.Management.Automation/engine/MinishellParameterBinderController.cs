// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Management.Automation.Language;
using System.Xml;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// This is the interface between the NativeCommandProcessor and the
    /// parameter binders required to bind parameters to a minishell.
    /// </summary>
    internal sealed class MinishellParameterBinderController : NativeCommandParameterBinderController
    {
        #region ctor

        /// <summary>
        /// Initializes the parameter binder controller for
        /// the specified native command and engine context.
        /// </summary>
        /// <param name="command">
        /// The command that the parameters will be bound to.
        /// </param>
        internal MinishellParameterBinderController(
            NativeCommand command)
            : base(command)
        {
            InputFormat = NativeCommandIOFormat.Xml;
            OutputFormat = NativeCommandIOFormat.Text;
        }

        #endregion ctor

        /// <summary>
        /// Value of input format. This property should be read after binding of parameters.
        /// </summary>
        internal NativeCommandIOFormat InputFormat { get; private set; }

        /// <summary>
        /// Value of output format. This property should be read after binding of parameters.
        /// </summary>
        internal NativeCommandIOFormat OutputFormat { get; private set; }

        /// <summary>
        /// IF true, child minishell is invoked with no-window.
        /// </summary>
        internal bool NonInteractive { get; private set; }

        /// <summary>
        /// Binds the specified parameters to the native command.
        /// </summary>
        /// <param name="parameters">
        /// The parameters to bind.
        /// </param>
        /// <param name="outputRedirected">
        /// true if minishell output is redirected.
        /// </param>
        /// <param name="hostName">
        /// name of the calling host.
        /// </param>
        /// <remarks>
        /// For any parameters that do not have a name, they are added to the command
        /// line arguments for the command
        /// </remarks>
        internal Collection<CommandParameterInternal> BindParameters(Collection<CommandParameterInternal> parameters, bool outputRedirected, string hostName)
        {
            MinishellParameters seen = 0;
            string inputFormat = null;
            string outputFormat = null;
            for (int i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];

                if (parameter.ParameterNameSpecified)
                {
                    var parameterName = parameter.ParameterName;

                    if (CommandParameter.StartsWith(parameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        HandleSeenParameter(ref seen, MinishellParameters.Command, CommandParameter);

                        // Value must be specified for -Command parameter.
                        if (i + 1 >= parameters.Count)
                        {
                            throw NewParameterBindingException(null, ErrorCategory.InvalidArgument, CommandParameter,
                                                               typeof(ScriptBlock), null,
                                                               NativeCP.NoValueForCommandParameter,
                                                               "NoValueForCommandParameter");
                        }

                        i += 1;

                        // Value of -Command parameter must be scriptblock
                        var scriptBlockArgument = parameters[i];
                        var argumentValue = PSObject.Base(scriptBlockArgument.ArgumentValue);
                        if (!scriptBlockArgument.ArgumentSpecified || argumentValue is not ScriptBlock)
                        {
                            throw NewParameterBindingException(null, ErrorCategory.InvalidArgument, CommandParameter,
                                                               typeof(ScriptBlock), argumentValue.GetType(),
                                                               NativeCP.IncorrectValueForCommandParameter,
                                                               "IncorrectValueForCommandParameter");
                        }

                        // Replace the parameters with -EncodedCommand <base64 encoded scriptblock>
                        parameters[i - 1] = CommandParameterInternal.CreateParameter(EncodedCommandParameter, "-" + EncodedCommandParameter, parameter.ParameterAst);
                        string encodedScript = StringToBase64Converter.StringToBase64String(argumentValue.ToString());
                        parameters[i] = CommandParameterInternal.CreateArgument(encodedScript, scriptBlockArgument.ArgumentAst);
                    }
                    else if (InputFormatParameter.StartsWith(parameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        HandleSeenParameter(ref seen, MinishellParameters.InputFormat, InputFormatParameter);

                        // Value for -Inputformat must be specified
                        if (i + 1 >= parameters.Count)
                        {
                            throw NewParameterBindingException(null, ErrorCategory.InvalidArgument, InputFormatParameter,
                                                               typeof(string), null,
                                                               NativeCP.NoValueForInputFormatParameter,
                                                               "NoValueForInputFormatParameter");
                        }

                        // Update the argument (partial arguments are allowed)
                        i += 1;
                        var inputFormatArg = parameters[i];
                        inputFormat = ProcessFormatParameterValue(InputFormatParameter, inputFormatArg.ArgumentValue);
                        parameters[i - 1] = CommandParameterInternal.CreateParameter(InputFormatParameter, "-" + InputFormatParameter, parameter.ParameterAst);
                        parameters[i] = CommandParameterInternal.CreateArgument(inputFormat, inputFormatArg.ArgumentAst);
                    }
                    else if (OutputFormatParameter.StartsWith(parameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        HandleSeenParameter(ref seen, MinishellParameters.OutputFormat, OutputFormatParameter);

                        // Value for -Inputformat must be specified
                        if (i + 1 >= parameters.Count)
                        {
                            throw NewParameterBindingException(null, ErrorCategory.InvalidArgument, OutputFormatParameter,
                                                               typeof(string), null,
                                                               NativeCP.NoValueForOutputFormatParameter,
                                                               "NoValueForInputFormatParameter");
                        }

                        // Update the argument (partial arguments are allowed)
                        i += 1;
                        var outputFormatArg = parameters[i];
                        outputFormat = ProcessFormatParameterValue(OutputFormatParameter, outputFormatArg.ArgumentValue);
                        parameters[i - 1] = CommandParameterInternal.CreateParameter(OutputFormatParameter, "-" + OutputFormatParameter, parameter.ParameterAst);
                        parameters[i] = CommandParameterInternal.CreateArgument(outputFormat, outputFormatArg.ArgumentAst);
                    }
                    else if (ArgsParameter.StartsWith(parameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        HandleSeenParameter(ref seen, MinishellParameters.Arguments, ArgsParameter);

                        // Value for -Args parameter must be specified
                        if (i + 1 >= parameters.Count)
                        {
                            throw NewParameterBindingException(null, ErrorCategory.InvalidArgument, ArgsParameter,
                                                               typeof(string), null, NativeCP.NoValuesSpecifiedForArgs,
                                                               "NoValuesSpecifiedForArgs");
                        }

                        // Get the encoded value for -args parameter
                        i += 1;
                        var argsArg = parameters[i];
                        var encodedArgs = ConvertArgsValueToEncodedString(argsArg.ArgumentValue);
                        parameters[i - 1] = CommandParameterInternal.CreateParameter(EncodedArgsParameter, "-" + EncodedArgsParameter, parameter.ParameterAst);
                        // NOTE: do not pass the ArgumentAst; it will fail validation in BindParameters if there
                        // are multiple arguments (array) but encodedArgs is an encoded string.
                        parameters[i] = CommandParameterInternal.CreateArgument(encodedArgs);
                    }
                }
                else
                {
                    // -Command is positional parameter. Bind first scriptblock to it, others are errors.
                    var scriptBlockArgument = parameters[i];
                    var argumentValue = PSObject.Base(scriptBlockArgument.ArgumentValue);
                    if (argumentValue is ScriptBlock)
                    {
                        HandleSeenParameter(ref seen, MinishellParameters.Command, CommandParameter);

                        // Replace the argument with -EncodedCommand <base64 encoded scriptblock>
                        string encodedScript = StringToBase64Converter.StringToBase64String(argumentValue.ToString());
                        parameters[i] = CommandParameterInternal.CreateParameterWithArgument(
                            parameter.ArgumentAst, EncodedCommandParameter, "-" + EncodedCommandParameter,
                            parameter.ArgumentAst, encodedScript,
                            spaceAfterParameter: true);
                    }
                }
            }

            // Add InputFormat and OutputFormat parameter if not specified
            if (inputFormat == null)
            {
                // For minishell default input format is xml
                parameters.Add(CommandParameterInternal.CreateParameter(InputFormatParameter, "-" + InputFormatParameter));
                parameters.Add(CommandParameterInternal.CreateArgument(XmlFormatValue));
                inputFormat = XmlFormatValue;
            }

            if (outputFormat == null)
            {
                // If output is redirected, output format should be xml
                outputFormat = outputRedirected ? XmlFormatValue : TextFormatValue;
                parameters.Add(CommandParameterInternal.CreateParameter(OutputFormatParameter, "-" + OutputFormatParameter));
                parameters.Add(CommandParameterInternal.CreateArgument(outputFormat));
            }

            // Set the output and input format class variable
            InputFormat = XmlFormatValue.StartsWith(inputFormat, StringComparison.OrdinalIgnoreCase)
                ? NativeCommandIOFormat.Xml
                : NativeCommandIOFormat.Text;
            OutputFormat = XmlFormatValue.StartsWith(outputFormat, StringComparison.OrdinalIgnoreCase)
                ? NativeCommandIOFormat.Xml
                : NativeCommandIOFormat.Text;

            // Note if a minishell is invoked from a non-console host, we need to
            // pass -nonInteractive flag. Our console host's name is "ConsoleHost".
            // Correct check would be see if current host has access to console and
            // pass noninteractive flag if doesn't.
            if (string.IsNullOrEmpty(hostName) || !hostName.Equals("ConsoleHost", StringComparison.OrdinalIgnoreCase))
            {
                NonInteractive = true;
                parameters.Insert(0, CommandParameterInternal.CreateParameter(NonInteractiveParameter, "-" + NonInteractiveParameter));
            }

            ((NativeCommandParameterBinder)DefaultParameterBinder).BindParameters(parameters);

            Diagnostics.Assert(s_emptyReturnCollection.Count == 0, "This list shouldn't be used for anything as it's shared.");

            return s_emptyReturnCollection;
        }

        private static readonly Collection<CommandParameterInternal> s_emptyReturnCollection = new Collection<CommandParameterInternal>();

        internal const string CommandParameter = "command";
        internal const string EncodedCommandParameter = "encodedCommand";
        internal const string ArgsParameter = "args";
        internal const string EncodedArgsParameter = "encodedarguments";
        internal const string InputFormatParameter = "inputFormat";
        internal const string OutputFormatParameter = "outputFormat";
        internal const string XmlFormatValue = "xml";
        internal const string TextFormatValue = "text";
        internal const string NonInteractiveParameter = "noninteractive";

        [Flags]
        private enum MinishellParameters
        {
            Command = 0x01,
            Arguments = 0x02,
            InputFormat = 0x04,
            OutputFormat = 0x08
        }

        /// <summary>
        /// Handles error handling if some parameter is specified more than once.
        /// </summary>
        private void HandleSeenParameter(ref MinishellParameters seen, MinishellParameters parameter, string parameterName)
        {
            if ((seen & parameter) == parameter)
            {
                throw NewParameterBindingException(null, ErrorCategory.InvalidArgument,
                                                   "-" + parameterName, null, null,
                                                   NativeCP.ParameterSpecifiedAlready,
                                                   "ParameterSpecifiedAlready",
                                                   parameterName);
            }
            else
            {
                seen |= parameter;
            }
        }

        /// <summary>
        /// This function processes the value for -inputFormat and -outputFormat parameter of minishell.
        /// </summary>
        /// <param name="parameterName">Name of the parameter for error messages. Value should be -inputFormat or -outputFormat.</param>
        /// <param name="value">Value to process.</param>
        /// <returns>Processed value.</returns>
        private string
        ProcessFormatParameterValue(string parameterName, object value)
        {
            string fpValue;
            try
            {
                fpValue = (string)LanguagePrimitives.ConvertTo(value, typeof(string), CultureInfo.InvariantCulture);
            }
            catch (PSInvalidCastException ex)
            {
                throw NewParameterBindingException(ex, ErrorCategory.InvalidArgument, parameterName,
                                                   typeof(string), value.GetType(),
                                                   NativeCP.StringValueExpectedForFormatParameter,
                                                   "StringValueExpectedForFormatParameter", parameterName);
            }

            if (XmlFormatValue.StartsWith(fpValue, StringComparison.OrdinalIgnoreCase))
            {
                return XmlFormatValue;
            }

            if (TextFormatValue.StartsWith(fpValue, StringComparison.OrdinalIgnoreCase))
            {
                return TextFormatValue;
            }

            throw NewParameterBindingException(null, ErrorCategory.InvalidArgument, parameterName,
                typeof(string), value.GetType(),
                NativeCP.IncorrectValueForFormatParameter,
                "IncorrectValueForFormatParameter",
                fpValue, parameterName);
        }

        /// <summary>
        /// Converts value of args parameter in to an encoded string.
        /// </summary>
        private static string ConvertArgsValueToEncodedString(object value)
        {
            ArrayList list = ConvertArgsValueToArrayList(value);

            // Serialize the list
            StringWriter stringWriter = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            // When (if) switching to XmlTextWriter.Create remember the OmitXmlDeclaration difference
            XmlWriter xmlWriter = XmlWriter.Create(stringWriter);
            Serializer serializer = new Serializer(xmlWriter);
            serializer.Serialize(list);
            serializer.Done();
            xmlWriter.Flush();
            string result = stringWriter.ToString();

            // convert result to encoded string
            return StringToBase64Converter.StringToBase64String(result);
        }

        /// <summary>
        /// Converts the value of -args parameter received from
        /// parser in to an arraylist.
        /// </summary>
        private static ArrayList ConvertArgsValueToArrayList(object value)
        {
            ArrayList results = new ArrayList();
            IEnumerator list = LanguagePrimitives.GetEnumerator(value);
            if (list == null)
            {
                results.Add(value);
            }
            else
            {
                while (list.MoveNext())
                {
                    results.Add(list.Current);
                }
            }

            return results;
        }

        private ParameterBindingException NewParameterBindingException(
            Exception innerException,
            ErrorCategory errorCategory,
            string parameterName,
            Type parameterType,
            Type typeSpecified,
            string resourceString,
            string errorId,
            params object[] args)
        {
            return new ParameterBindingException(
                innerException,
                errorCategory,
                this.InvocationInfo,
                null,
                parameterName,
                parameterType,
                typeSpecified,
                resourceString,
                errorId,
                args);
        }
    }
}
