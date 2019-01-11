// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PowerShell.Commands;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation.Internal;
using System.Text;

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
                    Diagnostics.Assert(parameter.ParameterText.IndexOf(' ') == -1, "Parameters cannot have whitespace");
                    PossiblyGlobArg(parameter.ParameterText, usedQuotes: false);

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

                        appendOneNativeArgument(Context, argValue,
                            arrayLiteralAst, sawVerbatimArgumentMarker, usedQuotes);
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

        #endregion internal members

        #region private members

        /// <summary>
        /// Stringize a non-IEnum argument to a native command, adding quotes
        /// and trailing spaces as appropriate. An array gets added as multiple arguments
        /// each of which will be stringized.
        /// </summary>
        /// <param name="context">Execution context instance.</param>
        /// <param name="obj">The object to append.</param>
        /// <param name="argArrayAst">If the argument was an array literal, the Ast, otherwise null.</param>
        /// <param name="sawVerbatimArgumentMarker">True if the argument occurs after --%.</param>
        /// <param name="usedQuotes">True if the argument was a quoted string (single or double).</param>
        private void appendOneNativeArgument(ExecutionContext context, object obj, ArrayLiteralAst argArrayAst, bool sawVerbatimArgumentMarker, bool usedQuotes)
        {
            IEnumerator list = LanguagePrimitives.GetEnumerator(obj);

            Diagnostics.Assert(argArrayAst == null
                || obj is object[] && ((object[])obj).Length == argArrayAst.Elements.Count,
                "array argument and ArrayLiteralAst differ in number of elements");

            int currentElement = -1;
            string separator = string.Empty;
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

                    currentElement += 1;
                    if (currentElement != 0)
                    {
                        separator = GetEnumerableArgSeparator(argArrayAst, currentElement);
                    }
                }

                if (!string.IsNullOrEmpty(arg))
                {
                    _arguments.Append(separator);

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
                        // quotes are supported
                        if (NeedQuotes(arg))
                        {
                            _arguments.Append('"');
                            // need to escape all trailing backslashes so the native command receives it correctly
                            // according to http://www.daviddeley.com/autohotkey/parameters/parameters.htm#WINCRULESDOC
                            _arguments.Append(arg);
                            for (int i = arg.Length-1; i >= 0 && arg[i] == '\\'; i--)
                            {
                                _arguments.Append('\\');
                            }

                            _arguments.Append('"');
                        }
                        else
                        {
                            PossiblyGlobArg(arg, usedQuotes);
                        }
                    }
                }
            } while (list != null);
        }

        /// <summary>
        /// On Windows, just append <paramref name="arg"/>.
        /// On Unix, do globbing as appropriate, otherwise just append <paramref name="arg"/>.
        /// </summary>
        /// <param name="arg">The argument that possibly needs expansion.</param>
        /// <param name="usedQuotes">True if the argument was a quoted string (single or double).</param>
        private void PossiblyGlobArg(string arg, bool usedQuotes)
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
                                _arguments.Append("\"");
                                _arguments.Append(expandedPath);
                                _arguments.Append("\"");
                            }
                            else
                            {
                                _arguments.Append(expandedPath);
                            }

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
                    argExpanded = true;
                }
                else if (arg.StartsWith("~/", StringComparison.OrdinalIgnoreCase))
                {
                    var replacementString = home + arg.Substring(1);
                    _arguments.Append(replacementString);
                    argExpanded = true;
                }
            }
#endif // UNIX

            if (!argExpanded)
            {
                _arguments.Append(arg);
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
        private NativeCommand _nativeCommand;
#endregion private members
    }
}
