/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Collections;
using System.Collections.ObjectModel;
using System.Management.Automation.Internal;
using System.Text;

namespace System.Management.Automation
{
    /// <summary>
    /// The parameter binder for native commands.
    /// </summary>
    /// 
    internal class NativeCommandParameterBinder : ParameterBinderBase
    {
        #region ctor

        /// <summary>
        /// Constructs a NativeCommandParameterBinder
        /// </summary>
        /// 
        /// <param name="command">
        /// The NativeCommand to bind to.
        /// </param>
        /// 
        /// <exception cref="ArgumentNullException">
        /// <paramref name="command"/>.Context is null
        /// </exception>
        internal NativeCommandParameterBinder(
            NativeCommand command) : base(command.MyInvocation, command.Context, command)
        {
            _nativeCommand = command;
        }
        #endregion ctor

        #region internal members

        #region Parameter binding

        /// <summary>
        /// Binds a parameter for a native command (application).
        /// </summary>
        /// <param name="name">
        ///     The name of the parameter to bind the value to. For applications
        ///     this just becomes another parameter...
        /// </param>
        /// <param name="value">
        ///     The value to bind to the parameter. It should be assumed by
        ///     derived classes that the proper type coercion has already taken
        ///     place and that any prerequisite metadata has been satisfied.
        /// </param>
        /// <param name="parameterMetadata"></param>
        internal override void BindParameter(string name, object value, CompiledCommandParameter parameterMetadata)
        {
            Diagnostics.Assert(false, "Unreachable code");

            throw new NotSupportedException();
        } // BindParameter

        internal override object GetDefaultParameterValue(string name)
        {
            return null;
        }

        internal void BindParameters(Collection<CommandParameterInternal> parameters)
        {
            bool sawVerbatimArgumentMarker = false;
            bool first = true;
            foreach (CommandParameterInternal parameter in parameters)
            {
                if (!first)
                {
                    _arguments.Append(' ');
                }
                first = false;

                if (parameter.ParameterNameSpecified)
                {
                    Diagnostics.Assert(parameter.ParameterText.IndexOf(' ') == -1, "Parameters cannot have whitespace");
                    _arguments.Append(parameter.ParameterText);

                    if (parameter.SpaceAfterParameter)
                    {
                        _arguments.Append(' ');
                    }
                }

                if (parameter.ArgumentSpecified)
                {
                    // If this is the verbatim argument marker, we don't pass it on to the native command.
                    // We do need to remember it though - we'll expand environment variables in subsequent args.
                    object argValue = parameter.ArgumentValue;
                    if (string.Equals("--%", argValue as string, StringComparison.OrdinalIgnoreCase))
                    {
                        sawVerbatimArgumentMarker = true;
                        continue;
                    }

                    if (argValue != AutomationNull.Value && argValue != UnboundParameter.Value)
                    {
                        // ArrayIsSingleArgumentForNativeCommand is true when a comma is used in the
                        // command line, e.g.
                        //    windbg  -k com:port=\\devbox\pipe\debug,pipe,resets=0,reconnect
                        // The parser produced an array of strings but marked the parameter so we
                        // can properly reconstruct the correct command line.
                        appendOneNativeArgument(Context, argValue,
                            parameter.ArrayIsSingleArgumentForNativeCommand ? ',' : ' ',
                            sawVerbatimArgumentMarker);
                    }
                }
            }
        }

        #endregion Parameter binding

        /// <summary>
        /// Gets the command arguments in string form
        /// </summary>
        ///
        internal String Arguments
        {
            get
            {
                return _arguments.ToString();
            }
        } // Arguments
        private readonly StringBuilder _arguments = new StringBuilder();

        #endregion internal members

        #region private members

        /// <summary>
        /// Stringize a non-IEnum argument to a native command, adding quotes
        /// and trailing spaces as appropriate. An array gets added as multiple arguments
        /// each of which will be stringized.
        /// </summary>
        /// <param name="context">Execution context instance</param>
        /// <param name="obj">The object to append</param>
        /// <param name="separator">A space or comma used when obj is enumerable</param>
        /// <param name="sawVerbatimArgumentMarker">true if the argument occurs after --%</param>
        private void appendOneNativeArgument(ExecutionContext context, object obj, char separator, bool sawVerbatimArgumentMarker)
        {
            IEnumerator list = LanguagePrimitives.GetEnumerator(obj);
            bool needSeparator = false;

            do
            {
                string arg;
                if (list == null)
                {
                    arg = PSObject.ToStringParser(context, obj);
                }
                else
                {
                    if (!ParserOps.MoveNext(context, null, list))
                    {
                        break;
                    }
                    arg = PSObject.ToStringParser(context, ParserOps.Current(null, list));
                }

                if (!String.IsNullOrEmpty(arg))
                {
                    if (needSeparator)
                    {
                        _arguments.Append(separator);
                    }
                    else
                    {
                        needSeparator = true;
                    }

                    if (sawVerbatimArgumentMarker)
                    {
                        arg = Environment.ExpandEnvironmentVariables(arg);
                        _arguments.Append(arg);
                    }
                    else
                    {
                        // We need to add quotes if the argument has unquoted spaces.  The
                        // quotes could appear anywhere inside the string, not just at the start,
                        // e.g.
                        //    $a = 'a"b c"d'
                        //    echoargs $a 'a"b c"d' a"b c"d
                        //
                        // The above should see 3 identical arguments in argv (the command line will
                        // actually have quotes in different places, but the Win32 command line=>argv parser
                        // erases those differences.
                        //
                        // We need to check quotes that the win32 argument parser checks which is currently 
                        // just the normal double quotes, no other special quotes.  Also note that mismatched
                        // quotes are supported.

                        bool needQuotes = false, followingBackslash = false;
                        int quoteCount = 0;
                        for (int i = 0; i < arg.Length; i++)
                        {
                            if (arg[i] == '"' && !followingBackslash)
                            {
                                quoteCount += 1;
                            }
                            else if (char.IsWhiteSpace(arg[i]) && (quoteCount % 2 == 0))
                            {
                                needQuotes = true;
                            }

                            followingBackslash = arg[i] == '\\';
                        }

                        if (needQuotes)
                        {
                            _arguments.Append('"');
                            _arguments.Append(arg);
                            _arguments.Append('"');
                        }
                        else
                        {
                            _arguments.Append(arg);
                        }
                    }
                }
            } while (list != null);
        }

        /// <summary>
        /// The native command to bind to
        /// </summary>
        private NativeCommand _nativeCommand;
        #endregion private members
    }
} // namespace System.Management.Automation
