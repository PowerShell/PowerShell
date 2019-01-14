// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Internal;

namespace System.Management.Automation
{
    /// <summary>
    /// This is the interface between the ScriptCommandProcessor and the
    /// parameter binders required to bind parameters to a shell function.
    /// </summary>
    internal class ScriptParameterBinderController : ParameterBinderController
    {
        #region ctor

        /// <summary>
        /// Initializes the cmdlet parameter binder controller for
        /// the specified cmdlet and engine context.
        /// </summary>
        /// <param name="script">
        /// The script that contains the parameter metadata.
        /// </param>
        /// <param name="invocationInfo">
        /// The invocation information about the code being run.
        /// </param>
        /// <param name="context">
        /// The engine context the cmdlet is run in.
        /// </param>
        /// <param name="command">
        /// The command that the parameters will be bound to.
        /// </param>
        /// <param name="localScope">
        /// The scope that the parameter binder will use to set parameters.
        /// </param>
        internal ScriptParameterBinderController(
            ScriptBlock script,
            InvocationInfo invocationInfo,
            ExecutionContext context,
            InternalCommand command,
            SessionStateScope localScope)
            : base(invocationInfo, context, new ScriptParameterBinder(script, invocationInfo, context, command, localScope))
        {
            this.DollarArgs = new List<object>();

            // Add the script parameter metadata to the bindable parameters
            // And add them to the unbound parameters list

            if (script.HasDynamicParameters)
            {
                UnboundParameters = this.BindableParameters.ReplaceMetadata(script.ParameterMetadata);
            }
            else
            {
                _bindableParameters = script.ParameterMetadata;
                UnboundParameters = new List<MergedCompiledCommandParameter>(_bindableParameters.BindableParameters.Values);
            }
        }

        #endregion ctor

        /// <summary>
        /// Holds the set of parameters that were not bound to any argument (i.e $args)
        /// </summary>
        internal List<object> DollarArgs { get; private set; }

        /// <summary>
        /// Binds the command line parameters for shell functions/filters/scripts/scriptblocks.
        /// </summary>
        /// <param name="arguments">
        ///     The arguments to be bound.
        /// </param>
        /// <returns>
        /// True if binding was successful or false otherwise.
        /// </returns>
        internal void BindCommandLineParameters(Collection<CommandParameterInternal> arguments)
        {
            // Add the passed in arguments to the unboundArguments collection

            foreach (CommandParameterInternal argument in arguments)
            {
                UnboundArguments.Add(argument);
            }

            ReparseUnboundArguments();

            // To support named parameters you just have un-comment the following line
            UnboundArguments = BindParameters(UnboundArguments);

            ParameterBindingException parameterBindingError;
            UnboundArguments =
                BindPositionalParameters(
                    UnboundArguments,
                    uint.MaxValue,
                    uint.MaxValue,
                    out parameterBindingError);

            try
            {
                this.DefaultParameterBinder.RecordBoundParameters = false;

                // If there are any unbound parameters that have default values, then
                // set those default values.
                BindUnboundScriptParameters();

                // If there are any unbound arguments, stick them into $args
                HandleRemainingArguments(UnboundArguments);
            }
            finally
            {
                this.DefaultParameterBinder.RecordBoundParameters = true;
            }

            return;
        }

        /// <summary>
        /// Passes the binding directly through to the parameter binder.
        /// It does no verification against metadata.
        /// </summary>
        /// <param name="argument">
        /// The name and value of the variable to bind.
        /// </param>
        /// <param name="flags">
        /// Ignored.
        /// </param>
        /// <returns>
        /// True if the parameter was successfully bound. Any error condition
        /// produces an exception.
        /// </returns>
        internal override bool BindParameter(CommandParameterInternal argument, ParameterBindingFlags flags)
        {
            // Just pass the binding straight through.  No metadata to verify the parameter against.
            DefaultParameterBinder.BindParameter(argument.ParameterName, argument.ArgumentValue, parameterMetadata: null);
            return true;
        }

        /// <summary>
        /// Binds the specified parameters to the shell function.
        /// </summary>
        /// <param name="arguments">
        /// The arguments to bind.
        /// </param>
        internal override Collection<CommandParameterInternal> BindParameters(Collection<CommandParameterInternal> arguments)
        {
            Collection<CommandParameterInternal> result = new Collection<CommandParameterInternal>();

            foreach (CommandParameterInternal argument in arguments)
            {
                if (!argument.ParameterNameSpecified)
                {
                    result.Add(argument);
                    continue;
                }

                // We don't want to throw an exception yet because
                // the parameter might be a positional argument

                MergedCompiledCommandParameter parameter =
                    BindableParameters.GetMatchingParameter(
                        argument.ParameterName,
                        false, true,
                        new InvocationInfo(this.InvocationInfo.MyCommand, argument.ParameterExtent));

                // If the parameter is not in the specified parameter set,
                // throw a binding exception

                if (parameter != null)
                {
                    // Now check to make sure it hasn't already been
                    // bound by looking in the boundParameters collection

                    if (BoundParameters.ContainsKey(parameter.Parameter.Name))
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

                    BindParameter(uint.MaxValue, argument, parameter, ParameterBindingFlags.ShouldCoerceType);
                }
                else if (argument.ParameterName.Equals(Language.Parser.VERBATIM_PARAMETERNAME, StringComparison.Ordinal))
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
        /// Takes the remaining arguments that haven't been bound, and binds
        /// them to $args.
        /// </summary>
        /// <param name="arguments">
        ///     The remaining unbound arguments.
        /// </param>
        /// <remarks>
        /// An array containing the values that were bound to $args.
        /// </remarks>
        private void HandleRemainingArguments(Collection<CommandParameterInternal> arguments)
        {
            List<object> args = new List<object>();

            foreach (CommandParameterInternal parameter in arguments)
            {
                object argValue = parameter.ArgumentSpecified ? parameter.ArgumentValue : null;

                // Proper automatic proxy generation requires the ability to prevent unbound arguments
                // in the proxy from binding to positional parameters in the proxied command.  We use
                // a special key ("$args") when splatting @CommandLineArguments to package up $args.
                // This special key is not created automatically because it is useful to splat @args,
                // just not in the automatically generated proxy.
                //
                // Example usage:
                //
                //   function foo { param($a, $b) $a; $b; $args }
                //   function foo_proxy { param($a) ; $CommandLineArguments.Add('$args', $args); foo @CommandLineArguments }
                //   foo_proxy 1 2 3
                //
                // Then in foo, $a=1, $b=, $args=2,3
                //
                // Here, we want $b in foo to be unbound because the proxy doesn't have $b (an Exchange scenario.)
                // So we pass $args (2,3) in the special entry in @CommandLineArguments.  If we had instead written:
                //
                //   function foo_proxy { param($a) ; foo @CommandLineArguments @args }
                //   foo_proxy 1 2 3
                //
                // Then in foo, $a=1, $b=2, $args=3
                //
                // Note that the name $args is chosen to be:
                //   * descriptive
                //   * obscure (it can't be a property/field name in C#, and is an unlikely variable in script)
                // So we shouldn't have any real conflict.  Note that if someone actually puts ${$args} in their
                // param block, then the value will be bound and we won't have an unbound argument for "$args" here.
                if (parameter.ParameterAndArgumentSpecified &&
                    parameter.ParameterName.Equals("$args", StringComparison.OrdinalIgnoreCase))
                {
                    // $args is normally an object[], but because this feature is accessible from script, it's possible
                    // for it to contain anything.
                    if (argValue is object[])
                    {
                        args.AddRange(argValue as object[]);
                    }
                    else
                    {
                        args.Add(argValue);
                    }

                    continue;
                }

                if (parameter.ParameterNameSpecified)
                {
                    // Add a property to the string so we can tell the difference between:
                    //    foo -abc
                    //    foo "-abc"
                    // This is important when splatting, we reconstruct the parameter if the
                    // value is splatted.
                    var parameterText = new PSObject(new String(parameter.ParameterText.ToCharArray()));
                    if (parameterText.Properties[NotePropertyNameForSplattingParametersInArgs] == null)
                    {
                        var noteProperty = new PSNoteProperty(NotePropertyNameForSplattingParametersInArgs,
                                                              parameter.ParameterName)
                        { IsHidden = true };
                        parameterText.Properties.Add(noteProperty);
                    }

                    args.Add(parameterText);
                }

                if (parameter.ArgumentSpecified)
                {
                    args.Add(argValue);
                }
            }

            object[] argsArray = args.ToArray();

            DefaultParameterBinder.BindParameter(SpecialVariables.Args, argsArray, parameterMetadata: null);

            DollarArgs.AddRange(argsArray);

            return;
        }

        internal const string NotePropertyNameForSplattingParametersInArgs = "<CommandParameterName>";
    }
}
