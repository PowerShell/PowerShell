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
            string compositeArg = string.Empty;
            for (int i = 0; i < parameters.Count; i++)
            {
                CommandParameterInternal parameter = parameters[i];

                if (i < parameters.Count - 1)
                {
                    int? end = parameter?.ArgumentAst?.Extent?.EndOffset;
                    int? start = parameters[i + 1]?.ArgumentAst?.Extent?.StartOffset;
                    if (end is not null && start is not null && end == start)
                    {
                        // nibble the next argument value
                        compositeArg = parameter.ArgumentAst?.Extent?.Text + parameters[i + 1].ArgumentAst?.Extent?.Text;
                        var compositeArgumentValue = string.Format("{0}{1}", parameter.ArgumentValue, parameters[i + 1].ArgumentValue);
                        // skip the next "parameter"
                        // cook up a new extent for the the current parameter which is the composite of the two
                        var ast = Parser.ParseInput(compositeArgumentValue, out Token[] _, out ParseError[] _).Find(ast => ast is StringConstantExpressionAst, true);
                        var compositeParameter = CommandParameterInternal.CreateArgument(compositeArgumentValue, ast, false);
                        parameter = compositeParameter;
                        i++;
                    }
                }

                if (!first)
                {
                    _arguments.Append(' ');
                }

                first = false;

                if (parameter.ParameterNameSpecified)
                {
                    Diagnostics.Assert(!parameter.ParameterText.Contains(' '), "Parameters cannot have whitespace");
                    var globbedArgs = PossiblyGlobArg(Context, parameter.ParameterText, usedQuotes: false);
                    _arguments.Append(string.Join(' ', globbedArgs));
                    if (parameter.SpaceAfterParameter)
                    {
                        _arguments.Append(' ');
                    }

                    globbedArgs.ForEach(s => AddToArgumentList(parameter, s, false));
                }

                if (parameter.ArgumentSpecified)
                {
                    // If this is the verbatim argument marker, we don't pass it on to the native command.
                    // We do need to remember it though - we'll expand environment variables in subsequent args.
                    object argValue;
                    argValue =  parameter.ArgumentValue;
                    
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

                        AppendOneNativeArgument(Context, parameter, argValue, arrayLiteralAst, sawVerbatimArgumentMarker, usedQuotes, false);
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
                    // The value is not convertible send back Legacy
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
                            AddToArgumentList(parameter, arg);

                            // need to escape all trailing backslashes so the native command receives it correctly
                            // according to http://www.daviddeley.com/autohotkey/parameters/parameters.htm#WINCRULESDOC
                            _arguments.Append(arg);
                            for (int i = arg.Length - 1; i >= 0 && arg[i] == '\\'; i--)
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
                                    var globbedArgs = PossiblyGlobArg(Context, element, usedQuotes);
                                    _arguments.Append(string.Join(' ', globbedArgs));
                                    globbedArgs.ForEach(s => AddToArgumentList(parameter, s);
                                }

                                break;
                            }
                            else
                            {
                                var result = PossiblyGlobArg(Context, arg, usedQuotes);
                                foreach (string s in result)
                                {
                                    _arguments.Append(s);
                                    // NOT SURE
                                    if (argArrayAst is null)
                                    {
                                        _arguments.Append(' ');
                                    }

                                    AddToArgumentList(parameter, s?.Trim('"'));
                                }
                            }
                        }
                    }
                }
                else if (ArgumentPassingStyle != NativeArgumentPassingStyle.Legacy && currentObj != null)
                {
                    // add empty strings to arglist, but not nulls
                    AddToArgumentList(parameter, arg, false);
                }
            }
            while (list != null);
        }

        /// <summary>
        /// On Windows, just append <paramref name="arg"/>.
        /// On Unix, do globbing as appropriate, otherwise just append <paramref name="arg"/>.
        /// </summary>
        /// <param name="context">The engine context used in expansion.</param>
        /// <param name="arg">The argument that possibly needs expansion.</param>
        /// <param name="usedQuotes">True if the argument was a quoted string (single or double).</param>
        /// <returns>Returns the possibly globbed argument as a list of strings.</returns>
        private static List<string> PossiblyGlobArg(ExecutionContext context, string arg, bool usedQuotes)
        {
            StringBuilder globbedArg = new StringBuilder();
            List<string> globbedArgs = new List<string>();

#if UNIX
            var argExpanded = false;
            // On UNIX systems, we expand arguments containing wildcard expressions against
            // the file system just like bash, etc.
            if (!usedQuotes && WildcardPattern.ContainsWildcardCharacters(arg))
            {
                // See if the current working directory is a filesystem provider location
                // We won't do the expansion if it isn't since native commands can only access the file system.
                var cwdinfo = context.EngineSessionState.CurrentLocation;

                // If it's a filesystem location then expand the wildcards
                if (cwdinfo.Provider.Name.Equals(FileSystemProvider.ProviderName, StringComparison.OrdinalIgnoreCase))
                {
                    // On UNIX, paths starting with ~ or absolute paths are not normalized
                    bool normalizePath = arg.Length == 0 || !(arg[0] == '~' || arg[0] == '/');

                    // See if there are any matching paths otherwise just add the pattern as the argument
                    Collection<PSObject> paths = null;
                    try
                    {
                        paths = context.EngineSessionState.InvokeProvider.ChildItem.Get(arg, false);
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
                            var expandedPath = (path.BaseObject as FileSystemInfo).FullName;
                            if (normalizePath)
                            {
                                expandedPath = context.SessionState.Path.NormalizeRelativePath(expandedPath, cwdinfo.ProviderPath);
                            }
                            // If the path contains spaces, then add quotes around it.
                            if (NeedQuotes(expandedPath))
                            {
                                globbedArg.Append('"');
                                globbedArg.Append(expandedPath);
                                globbedArg.Append('"');
                            }
                            else
                            {
                                globbedArg.Append(expandedPath);
                            }

                            globbedArgs.Add(globbedArg.ToString());
                            globbedArg.Clear();
                            argExpanded = true;
                        }

                        if (argExpanded)
                        {
                            return globbedArgs;
                        }
                    }
                }
            }
            else if (!usedQuotes)
            {
                // Even if there are no wildcards, we still need to possibly
                // expand ~ into the filesystem provider home directory path
                ProviderInfo fileSystemProvider = context.EngineSessionState.GetSingleProvider(FileSystemProvider.ProviderName);
                string home = fileSystemProvider.Home;
                if (string.Equals(arg, "~"))
                {
                    globbedArgs.Add(home);
                    return globbedArgs;
                }
                else if (arg.StartsWith("~/", StringComparison.OrdinalIgnoreCase))
                {
                    var replacementString = string.Concat(home, arg.AsSpan(1));
                    globbedArgs.Add(replacementString);
                    return globbedArgs;
                }
            }
#endif // UNIX

            globbedArgs.Add(arg);
            return globbedArgs;
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
            if (arrayLiteralAst == null)
            {
                return " ";
            }

            // index points to the *next* element, so we're looking for space between
            // it and the previous element.

            var next = arrayLiteralAst.Elements[index];
            var prev = arrayLiteralAst.Elements[index - 1];

            var arrayExtent = arrayLiteralAst.Extent;
            var afterPrev = prev.Extent.EndOffset;
            var beforeNext = next.Extent.StartOffset - 1;

            if (afterPrev == beforeNext)
            {
                return ",";
            }

            var arrayText = arrayExtent.Text;
            afterPrev -= arrayExtent.StartOffset;
            beforeNext -= arrayExtent.StartOffset;

            if (arrayText[afterPrev] == ',')
            {
                return ", ";
            }

            if (arrayText[beforeNext] == ',')
            {
                return " ,";
            }

            return " , ";
        }

        /// <summary>
        /// The native command to bind to.
        /// </summary>
        private readonly NativeCommand _nativeCommand;

        #endregion private members
    }
}
