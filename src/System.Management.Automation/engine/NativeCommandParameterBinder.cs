// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation.Internal;
using System.Text;

using Microsoft.PowerShell.Commands;

namespace System.Management.Automation
{
    using Language;

    /// <summary>
    /// The parameter binder for native commands.
    /// </summary>
    internal class NativeCommandParameterBinder : ParameterBinderBase
    {
        #region ctor

        /// <summary>
        /// Constructs a NativeCommandParameterBinder.
        /// </summary>
        /// <param name="command">
        /// The NativeCommand to bind to.
        /// </param>
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
        }

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
                    Diagnostics.Assert(!parameter.ParameterText.Contains(' '), "Parameters cannot have whitespace");
                    PossiblyGlobArg(parameter.ParameterText, parameter, usedQuotes: false);

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
                        // ArrayLiteralAst is used to reconstruct the correct argument, e.g.
                        //    windbg  -k com:port=\\devbox\pipe\debug,pipe,resets=0,reconnect
                        // The parser produced an array of strings but marked the parameter so we
                        // can properly reconstruct the correct command line.
                        bool usedQuotes = false;
                        ArrayLiteralAst arrayLiteralAst = null;
                        switch (parameter?.ArgumentAst)
                        {
                            case StringConstantExpressionAst sce:
                                usedQuotes = sce.StringConstantType != StringConstantType.BareWord;
                                break;
                            case ExpandableStringExpressionAst ese:
                                usedQuotes = ese.StringConstantType != StringConstantType.BareWord;
                                break;
                            case ArrayLiteralAst ala:
                                arrayLiteralAst = ala;
                                break;
                        }

                        AppendOneNativeArgument(Context, parameter, argValue, arrayLiteralAst, sawVerbatimArgumentMarker, usedQuotes);
                    }
                }
            }
        }

        #endregion Parameter binding

        /// <summary>
        /// Gets the command arguments in string form.
        /// </summary>
        internal string Arguments
        {
            get
            {
                return _arguments.ToString();
            }
        }

        private readonly StringBuilder _arguments = new StringBuilder();

        internal string[] ArgumentList
        {
            get
            {
                return _argumentList.ToArray();
            }
        }

        /// <summary>
        /// Add an argument to the ArgumentList.
        /// We may need to construct the argument out of the parameter text and the argument
        /// in the case that we have a parameter that appears as "-switch:value".
        /// </summary>
        /// <param name="parameter">The parameter associated with the operation.</param>
        /// <param name="argument">The value used with parameter.</param>
        internal void AddToArgumentList(CommandParameterInternal parameter, string argument)
        {
            if (parameter.ParameterNameSpecified && parameter.ParameterText.EndsWith(":"))
            {
                if (argument != parameter.ParameterText)
                {
                    // Only combine the text and argument if there was no space after the parameter,
                    // otherwise, add the parameter and arguments as separate elements.
                    if (parameter.SpaceAfterParameter)
                    {
                        _argumentList.Add(parameter.ParameterText);
                        _argumentList.Add(argument);
                    }
                    else
                    {
                        _argumentList.Add(parameter.ParameterText + argument);
                    }
                }
            }
            else
            {
                _argumentList.Add(argument);
            }
        }

        private readonly List<string> _argumentList = new List<string>();

        /// <summary>
        /// Gets a value indicating whether to use an ArgumentList or string for arguments when invoking a native executable.
        /// </summary>
        internal NativeArgumentPassingStyle ArgumentPassingStyle
        {
            get
            {
                try
                {
                    var preference = LanguagePrimitives.ConvertTo<NativeArgumentPassingStyle>(
                        Context.GetVariableValue(SpecialVariables.NativeArgumentPassingVarPath, NativeArgumentPassingStyle.Standard));
                    return preference;
                }
                catch
                {
                    // The value is not convertable send back Legacy
                    return NativeArgumentPassingStyle.Legacy;
                }
            }
        }

        #endregion internal members

        #region private members

        /// <summary>
        /// Stringize a non-IEnum argument to a native command, adding quotes
        /// and trailing spaces as appropriate. An array gets added as multiple arguments
        /// each of which will be stringized.
        /// </summary>
        /// <param name="context">Execution context instance.</param>
        /// <param name="parameter">The parameter associated with the operation.</param>
        /// <param name="obj">The object to append.</param>
        /// <param name="argArrayAst">If the argument was an array literal, the Ast, otherwise null.</param>
        /// <param name="sawVerbatimArgumentMarker">True if the argument occurs after --%.</param>
        /// <param name="usedQuotes">True if the argument was a quoted string (single or double).</param>
        private void AppendOneNativeArgument(ExecutionContext context, CommandParameterInternal parameter, object obj, ArrayLiteralAst argArrayAst, bool sawVerbatimArgumentMarker, bool usedQuotes)
        {
            IEnumerator list = LanguagePrimitives.GetEnumerator(obj);

            Diagnostics.Assert((argArrayAst == null) || (obj is object[] && ((object[])obj).Length == argArrayAst.Elements.Count), "array argument and ArrayLiteralAst differ in number of elements");

            int currentElement = -1;
            string separator = string.Empty;
            do
            {
                string arg;
                object currentObj;
                if (list == null)
                {
                    arg = PSObject.ToStringParser(context, obj);
                    currentObj = obj;
                }
                else
                {
                    if (!ParserOps.MoveNext(context, null, list))
                    {
                        break;
                    }

                    currentObj = ParserOps.Current(null, list);
                    arg = PSObject.ToStringParser(context, currentObj);

                    currentElement += 1;
                    if (currentElement != 0)
                    {
                        separator = GetEnumerableArgSeparator(argArrayAst, currentElement);
                    }
                }

                if (!string.IsNullOrEmpty(arg))
                {
                    // Only add the separator to the argument string rather than adding a separator to the ArgumentList.
                    _arguments.Append(separator);

                    if (sawVerbatimArgumentMarker)
                    {
                        arg = Environment.ExpandEnvironmentVariables(arg);
                        _arguments.Append(arg);

                        // we need to split the argument on spaces
                        _argumentList.AddRange(arg.Split(' ', StringSplitOptions.RemoveEmptyEntries));
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
                        // quotes are supported
                        if (NeedQuotes(arg))
                        {
                            _arguments.Append('"');
                            string text = arg;

                            // handle a scriptblock here
                            if (ExperimentalFeature.IsEnabled(ExperimentalFeature.PSNativeScriptBlockArgumentFeatureName) && parameter.ArgumentValue is ScriptBlock)
                            {
                                ScriptBlock sb = (ScriptBlock)parameter.ArgumentValue;
                                text = sb.Ast.Extent.Text;
                            }

                            AddToArgumentList(parameter, text);

                            // need to escape all trailing backslashes so the native command receives it correctly
                            // according to http://www.daviddeley.com/autohotkey/parameters/parameters.htm#WINCRULESDOC
                            _arguments.Append(text);
                            for (int i = text.Length - 1; i >= 0 && text[i] == '\\'; i--)
                            {
                                _arguments.Append('\\');
                            }

                            _arguments.Append('"');
                        }
                        else
                        {
                            if (argArrayAst != null && ArgumentPassingStyle != NativeArgumentPassingStyle.Legacy)
                            {
                                // We have a literal array, so take the extent, break it on spaces and add them to the argument list.
                                foreach (string element in argArrayAst.Extent.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                                {
                                    PossiblyGlobArg(element, parameter, usedQuotes);
                                }

                                break;
                            }
                            else
                            {
                                PossiblyGlobArg(arg, parameter, usedQuotes);
                            }
                        }
                    }
                }
                else if (ArgumentPassingStyle != NativeArgumentPassingStyle.Legacy && currentObj != null)
                {
                    // add empty strings to arglist, but not nulls
                    AddToArgumentList(parameter, arg);
                }
            }
            while (list != null);
        }

        /// <summary>
        /// On Windows, just append <paramref name="arg"/>.
        /// On Unix, do globbing as appropriate, otherwise just append <paramref name="arg"/>.
        /// </summary>
        /// <param name="arg">The argument that possibly needs expansion.</param>
        /// <param name="parameter">The parameter associated with the operation.</param>
        /// <param name="usedQuotes">True if the argument was a quoted string (single or double).</param>
        private void PossiblyGlobArg(string arg, CommandParameterInternal parameter, bool usedQuotes)
        {
            var argExpanded = false;

#if UNIX
            // On UNIX systems, we expand arguments containing wildcard expressions against
            // the file system just like bash, etc.
            if (!usedQuotes && WildcardPattern.ContainsWildcardCharacters(arg))
            {
                // See if the current working directory is a filesystem provider location
                // We won't do the expansion if it isn't since native commands can only access the file system.
                var cwdinfo = Context.EngineSessionState.CurrentLocation;

                // If it's a filesystem location then expand the wildcards
                if (cwdinfo.Provider.Name.Equals(FileSystemProvider.ProviderName, StringComparison.OrdinalIgnoreCase))
                {
                    // On UNIX, paths starting with ~ or absolute paths are not normalized
                    bool normalizePath = arg.Length == 0 || !(arg[0] == '~' || arg[0] == '/');

                    // See if there are any matching paths otherwise just add the pattern as the argument
                    Collection<PSObject> paths = null;
                    try
                    {
                        paths = Context.EngineSessionState.InvokeProvider.ChildItem.Get(arg, false);
                    }
                    catch
                    {
                        // Fallthrough will append the pattern unchanged.
                    }

                    // Expand paths, but only from the file system.
                    if (paths?.Count > 0 && paths.All(p => p.BaseObject is FileSystemInfo))
                    {
                        var sep = string.Empty;
                        foreach (var path in paths)
                        {
                            _arguments.Append(sep);
                            sep = " ";
                            var expandedPath = (path.BaseObject as FileSystemInfo).FullName;
                            if (normalizePath)
                            {
                                expandedPath =
                                    Context.SessionState.Path.NormalizeRelativePath(expandedPath, cwdinfo.ProviderPath);
                            }
                            // If the path contains spaces, then add quotes around it.
                            if (NeedQuotes(expandedPath))
                            {
                                _arguments.Append('"');
                                _arguments.Append(expandedPath);
                                _arguments.Append('"');
                            }
                            else
                            {
                                _arguments.Append(expandedPath);
                            }

                            AddToArgumentList(parameter, expandedPath);
                            argExpanded = true;
                        }
                    }
                }
            }
            else if (!usedQuotes)
            {
                // Even if there are no wildcards, we still need to possibly
                // expand ~ into the filesystem provider home directory path
                ProviderInfo fileSystemProvider = Context.EngineSessionState.GetSingleProvider(FileSystemProvider.ProviderName);
                string home = fileSystemProvider.Home;
                if (string.Equals(arg, "~"))
                {
                    _arguments.Append(home);
                    AddToArgumentList(parameter, home);
                    argExpanded = true;
                }
                else if (arg.StartsWith("~/", StringComparison.OrdinalIgnoreCase))
                {
                    var replacementString = string.Concat(home, arg.AsSpan(1));
                    _arguments.Append(replacementString);
                    AddToArgumentList(parameter, replacementString);
                    argExpanded = true;
                }
            }
#endif // UNIX

            if (!argExpanded)
            {
                if (ExperimentalFeature.IsEnabled(ExperimentalFeature.PSNativeScriptBlockArgumentFeatureName) && parameter.ArgumentValue is ScriptBlock)
                {
                    ScriptBlock sb = (ScriptBlock)parameter.ArgumentValue;
                    _arguments.Append(sb.Ast.Extent.Text);
                    AddToArgumentList(parameter, sb.Ast.Extent.Text);
                }
                else
                {
                    _arguments.Append(arg);
                    AddToArgumentList(parameter, arg);
                }
            }
        }

        /// <summary>
        /// Check to see if the string contains spaces and therefore must be quoted.
        /// </summary>
        /// <param name="stringToCheck">The string to check for spaces.</param>
        internal static bool NeedQuotes(string stringToCheck)
        {
            bool needQuotes = false, followingBackslash = false;
            int quoteCount = 0;
            for (int i = 0; i < stringToCheck.Length; i++)
            {
                if (stringToCheck[i] == '"' && !followingBackslash)
                {
                    quoteCount += 1;
                }
                else if (char.IsWhiteSpace(stringToCheck[i]) && (quoteCount % 2 == 0))
                {
                    needQuotes = true;
                }

                followingBackslash = stringToCheck[i] == '\\';
            }

            return needQuotes;
        }

        private static string GetEnumerableArgSeparator(ArrayLiteralAst arrayLiteralAst, int index)
        {
            if (arrayLiteralAst == null) return " ";

            // index points to the *next* element, so we're looking for space between
            // it and the previous element.

            var next = arrayLiteralAst.Elements[index];
            var prev = arrayLiteralAst.Elements[index - 1];

            var arrayExtent = arrayLiteralAst.Extent;
            var afterPrev = prev.Extent.EndOffset;
            var beforeNext = next.Extent.StartOffset - 1;

            if (afterPrev == beforeNext) return ",";

            var arrayText = arrayExtent.Text;
            afterPrev -= arrayExtent.StartOffset;
            beforeNext -= arrayExtent.StartOffset;

            if (arrayText[afterPrev] == ',') return ", ";
            if (arrayText[beforeNext] == ',') return " ,";
            return " , ";
        }

        /// <summary>
        /// The native command to bind to.
        /// </summary>
        private readonly NativeCommand _nativeCommand;

        #endregion private members
    }
}
