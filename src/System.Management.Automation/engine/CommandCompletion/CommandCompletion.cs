// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Text.RegularExpressions;
#if LEGACYTELEMETRY
using Microsoft.PowerShell.Telemetry.Internal;
#endif

namespace System.Management.Automation
{
    /// <summary>
    /// Provides a set of possible completions for given input.
    /// </summary>
    public class CommandCompletion
    {
        /// <summary>
        /// Construct the result CompleteInput or TabExpansion2.
        /// </summary>
        public CommandCompletion(Collection<CompletionResult> matches, int currentMatchIndex, int replacementIndex, int replacementLength)
        {
            this.CompletionMatches = matches;
            this.CurrentMatchIndex = currentMatchIndex;
            this.ReplacementIndex = replacementIndex;
            this.ReplacementLength = replacementLength;
        }

        #region Fields and Properties

        /// <summary>
        /// Current index in <see cref="CompletionMatches"/>.
        /// </summary>
        public int CurrentMatchIndex { get; set; }

        /// <summary>
        /// Returns the starting replacement index from the original input.
        /// </summary>
        public int ReplacementIndex { get; set; }

        /// <summary>
        /// Returns the length of the text to replace from the original input.
        /// </summary>
        public int ReplacementLength { get; set; }

        /// <summary>
        /// Gets all the completion results.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Collection<CompletionResult> CompletionMatches { get; set; }

        internal static readonly IList<CompletionResult> EmptyCompletionResult = Array.Empty<CompletionResult>();
        private static readonly CommandCompletion s_emptyCommandCompletion = new CommandCompletion(
            new Collection<CompletionResult>(EmptyCompletionResult), -1, -1, -1);

        #endregion Fields and Properties

        #region public methods

        /// <summary>
        /// </summary>
        /// <param name="input"></param>
        /// <param name="cursorIndex"></param>
        /// <returns></returns>
        public static Tuple<Ast, Token[], IScriptPosition> MapStringInputToParsedInput(string input, int cursorIndex)
        {
            if (cursorIndex > input.Length)
            {
                throw PSTraceSource.NewArgumentException("cursorIndex");
            }

            Token[] tokens;
            ParseError[] errors;
            var ast = Parser.ParseInput(input, out tokens, out errors);

            IScriptPosition cursorPosition =
                ((InternalScriptPosition)ast.Extent.StartScriptPosition).CloneWithNewOffset(cursorIndex);
            return Tuple.Create<Ast, Token[], IScriptPosition>(ast, tokens, cursorPosition);
        }

        /// <summary>
        /// </summary>
        /// <param name="input">The input to complete.</param>
        /// <param name="cursorIndex">The index of the cursor in the input.</param>
        /// <param name="options">Optional options to configure how completion is performed.</param>
        /// <returns></returns>
        public static CommandCompletion CompleteInput(string input, int cursorIndex, Hashtable options)
        {
            if (input == null)
            {
                return s_emptyCommandCompletion;
            }

            var parsedInput = MapStringInputToParsedInput(input, cursorIndex);
            return CompleteInputImpl(parsedInput.Item1, parsedInput.Item2, parsedInput.Item3, options);
        }

        /// <summary>
        /// </summary>
        /// <param name="ast">Ast for pre-parsed input.</param>
        /// <param name="tokens">Tokens for pre-parsed input.</param>
        /// <param name="positionOfCursor"></param>
        /// <param name="options">Optional options to configure how completion is performed.</param>
        /// <returns></returns>
        public static CommandCompletion CompleteInput(Ast ast, Token[] tokens, IScriptPosition positionOfCursor, Hashtable options)
        {
            if (ast == null)
            {
                throw PSTraceSource.NewArgumentNullException("ast");
            }

            if (tokens == null)
            {
                throw PSTraceSource.NewArgumentNullException("tokens");
            }

            if (positionOfCursor == null)
            {
                throw PSTraceSource.NewArgumentNullException("positionOfCursor");
            }

            return CompleteInputImpl(ast, tokens, positionOfCursor, options);
        }

        /// <summary>
        /// Invokes the script function TabExpansion2.
        /// For legacy support, TabExpansion2 will indirectly call TabExpansion if it exists.
        /// </summary>
        /// <param name="input">The input script to complete.</param>
        /// <param name="cursorIndex">The offset in <paramref name="input"/> where completion is requested.</param>
        /// <param name="options">Optional parameter that specifies configurable options for completion.</param>
        /// <param name="powershell">The powershell to use to invoke the script function TabExpansion2.</param>
        /// <returns>A collection of completions with the replacement start and length.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "powershell")]
        public static CommandCompletion CompleteInput(string input, int cursorIndex, Hashtable options, PowerShell powershell)
        {
            if (input == null)
            {
                return s_emptyCommandCompletion;
            }

            if (cursorIndex > input.Length)
            {
                throw PSTraceSource.NewArgumentException("cursorIndex");
            }

            if (powershell == null)
            {
                throw PSTraceSource.NewArgumentNullException("powershell");
            }

            // If we are in a debugger stop, let the debugger do the command completion.
            var debugger = (powershell.Runspace != null) ? powershell.Runspace.Debugger : null;
            if ((debugger != null) && debugger.InBreakpoint)
            {
                return CompleteInputInDebugger(input, cursorIndex, options, debugger);
            }

            var remoteRunspace = powershell.Runspace as RemoteRunspace;
            if (remoteRunspace != null)
            {
                // If the runspace is not available to run commands then exit here because nested commands are not
                // supported on remote runspaces.
                if (powershell.IsNested || (remoteRunspace.RunspaceAvailability != RunspaceAvailability.Available))
                {
                    return s_emptyCommandCompletion;
                }

                // If it's in the nested prompt, the powershell instance is created by "PowerShell.Create(RunspaceMode.CurrentRunspace);".
                // In this case, the powershell._runspace is null but if we try to access the property "Runspace", it will create a new
                // local runspace - the default local runspace will be abandoned. So we check the powershell.IsChild first to make sure
                // not to access the property "Runspace" in this case - powershell.isChild will be set to true only in this case.
                if (!powershell.IsChild)
                {
                    CheckScriptCallOnRemoteRunspace(remoteRunspace);
                    if (remoteRunspace.GetCapabilities().Equals(Runspaces.RunspaceCapability.Default))
                    {
                        // Capability:
                        //      NamedPipeTransport (0x2)            -> If remoteMachine is Threshold or later
                        //      SupportsDisconnect (0x1)            -> If remoteMachine is Win8 or later
                        //      Default (0x0)                       -> If remoteMachine is Win7
                        // Remoting to a Win7 machine. Use the legacy tab completion function from V1/V2
                        int replacementIndex;
                        int replacementLength;

                        powershell.Commands.Clear();
                        var results = InvokeLegacyTabExpansion(powershell, input, cursorIndex, true, out replacementIndex, out replacementLength);
                        return new CommandCompletion(
                            new Collection<CompletionResult>(results ?? EmptyCompletionResult),
                            -1, replacementIndex, replacementLength);
                    }
                }
            }

            return CallScriptWithStringParameterSet(input, cursorIndex, options, powershell);
        }

        /// <summary>
        /// Invokes the script function TabExpansion2.
        /// For legacy support, TabExpansion2 will indirectly call TabExpansion if it exists.
        /// </summary>
        /// <param name="ast">The ast for pre-parsed input.</param>
        /// <param name="tokens"></param>
        /// <param name="cursorPosition"></param>
        /// <param name="options">Optional options to configure how completion is performed.</param>
        /// <param name="powershell">The powershell to use to invoke the script function TabExpansion2.</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "powershell")]
        public static CommandCompletion CompleteInput(Ast ast, Token[] tokens, IScriptPosition cursorPosition, Hashtable options, PowerShell powershell)
        {
            if (ast == null)
            {
                throw PSTraceSource.NewArgumentNullException("ast");
            }

            if (tokens == null)
            {
                throw PSTraceSource.NewArgumentNullException("tokens");
            }

            if (cursorPosition == null)
            {
                throw PSTraceSource.NewArgumentNullException("cursorPosition");
            }

            if (powershell == null)
            {
                throw PSTraceSource.NewArgumentNullException("powershell");
            }

            // If we are in a debugger stop, let the debugger do the command completion.
            var debugger = (powershell.Runspace != null) ? powershell.Runspace.Debugger : null;
            if ((debugger != null) && debugger.InBreakpoint)
            {
                return CompleteInputInDebugger(ast, tokens, cursorPosition, options, debugger);
            }

            var remoteRunspace = powershell.Runspace as RemoteRunspace;
            if (remoteRunspace != null)
            {
                // If the runspace is not available to run commands then exit here because nested commands are not
                // supported on remote runspaces.
                if (powershell.IsNested || (remoteRunspace.RunspaceAvailability != RunspaceAvailability.Available))
                {
                    return s_emptyCommandCompletion;
                }

                if (!powershell.IsChild)
                {
                    CheckScriptCallOnRemoteRunspace(remoteRunspace);
                    if (remoteRunspace.GetCapabilities().Equals(Runspaces.RunspaceCapability.Default))
                    {
                        // Capability:
                        //      SupportsDisconnect (0x1)            -> If remoteMachine is Win8 or later
                        //      Default (0x0)                       -> If remoteMachine is Win7
                        // Remoting to a Win7 machine. Use the legacy tab completion function from V1/V2
                        int replacementIndex;
                        int replacementLength;

                        // When call the win7 TabExpansion script, the input should be the single current line
                        powershell.Commands.Clear();
                        var inputAndCursor = GetInputAndCursorFromAst(cursorPosition);
                        var results = InvokeLegacyTabExpansion(powershell, inputAndCursor.Item1, inputAndCursor.Item2, true, out replacementIndex, out replacementLength);
                        return new CommandCompletion(
                            new Collection<CompletionResult>(results ?? EmptyCompletionResult),
                            -1, replacementIndex + inputAndCursor.Item3, replacementLength);
                    }
                    else
                    {
                        // Call script on a remote win8 machine
                        // when call the win8 TabExpansion2 script, the input should be the whole script text
                        string input = ast.Extent.Text;
                        int cursorIndex = ((InternalScriptPosition)cursorPosition).Offset;
                        return CallScriptWithStringParameterSet(input, cursorIndex, options, powershell);
                    }
                }
            }

            return CallScriptWithAstParameterSet(ast, tokens, cursorPosition, options, powershell);
        }

        /// <summary>
        /// Get the next result, moving forward or backward.  Supports wraparound, so if there are any results at all,
        /// this method will never fail and never return null.
        /// </summary>
        /// <param name="forward">True if we should move forward through the list, false if backwards.</param>
        /// <returns>The next completion result, or null if no results.</returns>
        public CompletionResult GetNextResult(bool forward)
        {
            CompletionResult result = null;
            var count = CompletionMatches.Count;
            if (count > 0)
            {
                CurrentMatchIndex += forward ? 1 : -1;
                if (CurrentMatchIndex >= count)
                {
                    CurrentMatchIndex = 0;
                }
                else if (CurrentMatchIndex < 0)
                {
                    CurrentMatchIndex = count - 1;
                }

                result = CompletionMatches[CurrentMatchIndex];
            }

            return result;
        }

        #endregion public methods

        #region Internal methods

        /// <summary>
        /// Command completion while in debug break mode.
        /// </summary>
        /// <param name="input">The input script to complete.</param>
        /// <param name="cursorIndex">The offset in <paramref name="input"/> where completion is requested.</param>
        /// <param name="options">Optional parameter that specifies configurable options for completion.</param>
        /// <param name="debugger">Current debugger.</param>
        /// <returns>A collection of completions with the replacement start and length.</returns>
        internal static CommandCompletion CompleteInputInDebugger(string input, int cursorIndex, Hashtable options, Debugger debugger)
        {
            if (input == null)
            {
                return s_emptyCommandCompletion;
            }

            if (cursorIndex > input.Length)
            {
                throw PSTraceSource.NewArgumentException("cursorIndex");
            }

            if (debugger == null)
            {
                throw PSTraceSource.NewArgumentNullException("debugger");
            }

            Command cmd = new Command("TabExpansion2");
            cmd.Parameters.Add("InputScript", input);
            cmd.Parameters.Add("CursorColumn", cursorIndex);
            cmd.Parameters.Add("Options", options);

            return ProcessCompleteInputCommand(cmd, debugger);
        }

        /// <summary>
        /// Command completion while in debug break mode.
        /// </summary>
        /// <param name="ast">The ast for pre-parsed input.</param>
        /// <param name="tokens"></param>
        /// <param name="cursorPosition"></param>
        /// <param name="options">Optional options to configure how completion is performed.</param>
        /// <param name="debugger">Current debugger.</param>
        /// <returns>Command completion.</returns>
        internal static CommandCompletion CompleteInputInDebugger(Ast ast, Token[] tokens, IScriptPosition cursorPosition, Hashtable options, Debugger debugger)
        {
            if (ast == null)
            {
                throw PSTraceSource.NewArgumentNullException("ast");
            }

            if (tokens == null)
            {
                throw PSTraceSource.NewArgumentNullException("tokens");
            }

            if (cursorPosition == null)
            {
                throw PSTraceSource.NewArgumentNullException("cursorPosition");
            }

            if (debugger == null)
            {
                throw PSTraceSource.NewArgumentNullException("debugger");
            }

            // For remote debugging just pass string input.
            if ((debugger is RemoteDebugger) || debugger.IsPushed)
            {
                string input = ast.Extent.Text;
                int cursorIndex = ((InternalScriptPosition)cursorPosition).Offset;

                return CompleteInputInDebugger(input, cursorIndex, options, debugger);
            }

            Command cmd = new Command("TabExpansion2");
            cmd.Parameters.Add("Ast", ast);
            cmd.Parameters.Add("Tokens", tokens);
            cmd.Parameters.Add("PositionOfCursor", cursorPosition);
            cmd.Parameters.Add("Options", options);

            return ProcessCompleteInputCommand(cmd, debugger);
        }

        private static CommandCompletion ProcessCompleteInputCommand(
            Command cmd,
            Debugger debugger)
        {
            PSCommand command = new PSCommand(cmd);
            PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();

            debugger.ProcessCommand(command, output);

            if (output.Count == 1)
            {
                var commandCompletion = output[0].BaseObject as CommandCompletion;
                if (commandCompletion != null)
                {
                    return commandCompletion;
                }
            }

            return s_emptyCommandCompletion;
        }

        #endregion

        #region private methods

        private static void CheckScriptCallOnRemoteRunspace(RemoteRunspace remoteRunspace)
        {
            var remoteRunspaceInternal = remoteRunspace.RunspacePool.RemoteRunspacePoolInternal;
            if (remoteRunspaceInternal != null)
            {
                var transportManager = remoteRunspaceInternal.DataStructureHandler.TransportManager;
                if (transportManager != null && transportManager.TypeTable == null)
                {
                    // The remote runspace was created without a TypeTable instance.
                    // The tab completion results cannot be deserialized if the TypeTable is not available
                    throw PSTraceSource.NewInvalidOperationException(TabCompletionStrings.CannotDeserializeTabCompletionResult);
                }
            }
        }

        private static CommandCompletion CallScriptWithStringParameterSet(string input, int cursorIndex, Hashtable options, PowerShell powershell)
        {
            try
            {
                powershell.Commands.Clear();
                powershell.AddCommand("TabExpansion2")
                    .AddArgument(input)
                    .AddArgument(cursorIndex)
                    .AddArgument(options);
                var results = powershell.Invoke();
                if (results == null)
                {
                    return s_emptyCommandCompletion;
                }

                if (results.Count == 1)
                {
                    var result = PSObject.Base(results[0]);
                    var commandCompletion = result as CommandCompletion;
                    if (commandCompletion != null)
                    {
                        return commandCompletion;
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                powershell.Commands.Clear();
            }

            return s_emptyCommandCompletion;
        }

        private static CommandCompletion CallScriptWithAstParameterSet(Ast ast, Token[] tokens, IScriptPosition cursorPosition, Hashtable options, PowerShell powershell)
        {
            try
            {
                powershell.Commands.Clear();
                powershell.AddCommand("TabExpansion2")
                    .AddArgument(ast)
                    .AddArgument(tokens)
                    .AddArgument(cursorPosition)
                    .AddArgument(options);
                var results = powershell.Invoke();
                if (results == null)
                {
                    return s_emptyCommandCompletion;
                }

                if (results.Count == 1)
                {
                    var result = PSObject.Base(results[0]);
                    var commandCompletion = result as CommandCompletion;
                    if (commandCompletion != null)
                    {
                        return commandCompletion;
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                powershell.Commands.Clear();
            }

            return s_emptyCommandCompletion;
        }

        // This is the start of the real implementation of autocomplete/intellisense/tab completion
        private static CommandCompletion CompleteInputImpl(Ast ast, Token[] tokens, IScriptPosition positionOfCursor, Hashtable options)
        {
            var sw = new Stopwatch();
            sw.Start();

            using (var powershell = PowerShell.Create(RunspaceMode.CurrentRunspace))
            {
                var context = LocalPipeline.GetExecutionContextFromTLS();

                bool cleanupModuleAnalysisAppDomain = context.TakeResponsibilityForModuleAnalysisAppDomain();

                try
                {
                    // First, check if a V1/V2 implementation of TabExpansion exists.  If so, the user had overridden
                    // the built-in version, so we should continue to use theirs.
                    int replacementIndex = -1;
                    int replacementLength = -1;
                    List<CompletionResult> results = null;

                    if (NeedToInvokeLegacyTabExpansion(powershell))
                    {
                        var inputAndCursor = GetInputAndCursorFromAst(positionOfCursor);
                        results = InvokeLegacyTabExpansion(powershell, inputAndCursor.Item1, inputAndCursor.Item2, false, out replacementIndex, out replacementLength);
                        replacementIndex += inputAndCursor.Item3;
                    }

                    if (results == null || results.Count == 0)
                    {
                        /* BROKEN code commented out, fix sometime
                        // If we were invoked from TabExpansion2, we want to "remove" TabExpansion2 and anything it calls
                        // from our results.  We do this by faking out the session so that TabExpansion2 isn't anywhere to be found.
                        MutableTuple tupleForFrameToSkipPast = null;
                        foreach (var stackEntry in context.Debugger.GetCallStack())
                        {
                            dynamic stackEntryAsPSObj = PSObject.AsPSObject(stackEntry);
                            if (stackEntryAsPSObj.Command.Equals("TabExpansion2", StringComparison.OrdinalIgnoreCase))
                            {
                                tupleForFrameToSkipPast = stackEntry.FunctionContext._localsTuple;
                                break;
                            }
                        }

                        SessionStateScope scopeToRestore = null;
                        if (tupleForFrameToSkipPast != null)
                        {
                            // Find this tuple in the scope stack.
                            scopeToRestore = context.EngineSessionState.CurrentScope;
                            var scope = context.EngineSessionState.CurrentScope;
                            while (scope != null && scope.LocalsTuple != tupleForFrameToSkipPast)
                            {
                                scope = scope.Parent;
                            }

                            if (scope != null)
                            {
                                context.EngineSessionState.CurrentScope = scope.Parent;
                            }
                        }

                        try
                        {
                        */
                        var completionAnalysis = new CompletionAnalysis(ast, tokens, positionOfCursor, options);
                        results = completionAnalysis.GetResults(powershell, out replacementIndex, out replacementLength);
                        /*
                        }
                        finally
                        {
                            if (scopeToRestore != null)
                            {
                                context.EngineSessionState.CurrentScope = scopeToRestore;
                            }
                        }
                        */
                    }

                    var completionResults = results ?? EmptyCompletionResult;
                    sw.Stop();
#if LEGACYTELEMETRY
                    TelemetryAPI.ReportTabCompletionTelemetry(sw.ElapsedMilliseconds, completionResults.Count,
                        completionResults.Count > 0 ? completionResults[0].ResultType : CompletionResultType.Text);
#endif
                    return new CommandCompletion(
                        new Collection<CompletionResult>(completionResults),
                        -1,
                        replacementIndex,
                        replacementLength);
                }
                finally
                {
                    if (cleanupModuleAnalysisAppDomain)
                    {
                        context.ReleaseResponsibilityForModuleAnalysisAppDomain();
                    }
                }
            }
        }

        private static Tuple<string, int, int> GetInputAndCursorFromAst(IScriptPosition cursorPosition)
        {
            var line = cursorPosition.Line;
            var cursor = cursorPosition.ColumnNumber - 1;
            var adjustment = cursorPosition.Offset - cursor;
            return Tuple.Create(line.Substring(0, cursor), cursor, adjustment);
        }

        private static bool NeedToInvokeLegacyTabExpansion(PowerShell powershell)
        {
            var executionContext = powershell.GetContextFromTLS();

            // We don't want command discovery to search unloaded modules for TabExpansion.
            var functionInfo = executionContext.EngineSessionState.GetFunction("TabExpansion");
            if (functionInfo != null)
                return true;

            var aliasInfo = executionContext.EngineSessionState.GetAlias("TabExpansion");
            if (aliasInfo != null)
                return true;

            return false;
        }

        private static List<CompletionResult> InvokeLegacyTabExpansion(PowerShell powershell, string input, int cursorIndex, bool remoteToWin7, out int replacementIndex, out int replacementLength)
        {
            List<CompletionResult> results = null;

            var legacyInput = (cursorIndex != input.Length) ? input.Substring(0, cursorIndex) : input;
            char quote;
            var lastword = LastWordFinder.FindLastWord(legacyInput, out replacementIndex, out quote);
            replacementLength = legacyInput.Length - replacementIndex;
            var helper = new PowerShellExecutionHelper(powershell);

            powershell.AddCommand("TabExpansion").AddArgument(legacyInput).AddArgument(lastword);

            Exception exceptionThrown;
            var oldResults = helper.ExecuteCurrentPowerShell(out exceptionThrown);
            if (oldResults != null)
            {
                results = new List<CompletionResult>();
                foreach (var oldResult in oldResults)
                {
                    var completionResult = PSObject.Base(oldResult) as CompletionResult;
                    if (completionResult == null)
                    {
                        var oldResultStr = oldResult.ToString();

                        // Add back the quotes we removed if the result isn't quoted
                        if (quote != '\0')
                        {
                            if (oldResultStr.Length > 2 && oldResultStr[0] != quote)
                            {
                                oldResultStr = quote + oldResultStr + quote;
                            }
                        }

                        completionResult = new CompletionResult(oldResultStr);
                    }

                    results.Add(completionResult);
                }
            }

            if (remoteToWin7 && (results == null || results.Count == 0))
            {
                string quoteStr = quote == '\0' ? string.Empty : quote.ToString();
                results = PSv2CompletionCompleter.PSv2GenerateMatchSetOfFiles(helper, lastword, replacementIndex == 0, quoteStr);
                var cmdletResults = PSv2CompletionCompleter.PSv2GenerateMatchSetOfCmdlets(helper, lastword, quoteStr, replacementIndex == 0);

                if (cmdletResults != null && cmdletResults.Count > 0)
                {
                    results.AddRange(cmdletResults);
                }
            }

            return results;
        }

        /// <summary>
        /// PSv2CompletionCompleter implements the algorithm we use to complete cmdlet/file names in PowerShell v2. This class
        /// exists for legacy purpose only. It is used only in a remote interactive session from Win8 to Win7. V3 and forward
        /// uses completely different completers.
        /// </summary>
        /// <remarks>
        /// The implementation of file name completion is completely different on V2 and V3 for remote scenarios. On PSv3, the
        /// CompletionResults are generated always on the target machine, and
        /// </remarks>
        private static class PSv2CompletionCompleter
        {
            private static readonly Regex s_cmdletTabRegex = new Regex(@"^[\w\*\?]+-[\w\*\?]*");
            private static readonly char[] s_charsRequiringQuotedString = "`&@'#{}()$,;|<> \t".ToCharArray();

            #region "Handle Command"

            /// <summary>
            /// Used when remoting from a win8 machine to a win7 machine.
            /// </summary>
            /// <param name="lastWord"></param>
            /// <param name="isSnapinSpecified"></param>
            /// <returns></returns>
            private static bool PSv2IsCommandLikeCmdlet(string lastWord, out bool isSnapinSpecified)
            {
                isSnapinSpecified = false;

                string[] cmdletParts = lastWord.Split(Utils.Separators.Backslash);
                if (cmdletParts.Length == 1)
                {
                    return s_cmdletTabRegex.IsMatch(lastWord);
                }

                if (cmdletParts.Length == 2)
                {
                    isSnapinSpecified = PSSnapInInfo.IsPSSnapinIdValid(cmdletParts[0]);
                    if (isSnapinSpecified)
                    {
                        return s_cmdletTabRegex.IsMatch(cmdletParts[1]);
                    }
                }

                return false;
            }

            private struct CommandAndName
            {
                internal readonly PSObject Command;
                internal readonly PSSnapinQualifiedName CommandName;

                internal CommandAndName(PSObject command, PSSnapinQualifiedName commandName)
                {
                    this.Command = command;
                    this.CommandName = commandName;
                }
            }

            /// <summary>
            /// Used when remoting from a win8 machine to a win7 machine. Complete command names.
            /// </summary>
            /// <param name="helper"></param>
            /// <param name="lastWord"></param>
            /// <param name="quote"></param>
            /// <param name="completingAtStartOfLine"></param>
            /// <returns></returns>
            internal static List<CompletionResult> PSv2GenerateMatchSetOfCmdlets(PowerShellExecutionHelper helper, string lastWord, string quote, bool completingAtStartOfLine)
            {
                var results = new List<CompletionResult>();
                bool isSnapinSpecified;

                if (!PSv2IsCommandLikeCmdlet(lastWord, out isSnapinSpecified))
                    return results;

                helper.CurrentPowerShell
                    .AddCommand("Get-Command")
                    .AddParameter("Name", lastWord + "*")
                    .AddCommand("Sort-Object")
                    .AddParameter("Property", "Name");

                Exception exceptionThrown;
                Collection<PSObject> commands = helper.ExecuteCurrentPowerShell(out exceptionThrown);

                if (commands != null && commands.Count > 0)
                {
                    // convert the PSObjects into strings
                    CommandAndName[] cmdlets = new CommandAndName[commands.Count];
                    // if the command causes cmdlets from multiple mshsnapin is returned,
                    // append the mshsnapin name to disambiguate the cmdlets.
                    for (int i = 0; i < commands.Count; ++i)
                    {
                        PSObject command = commands[i];
                        string cmdletFullName = CmdletInfo.GetFullName(command);
                        cmdlets[i] = new CommandAndName(command, PSSnapinQualifiedName.GetInstance(cmdletFullName));
                    }

                    if (isSnapinSpecified)
                    {
                        foreach (CommandAndName cmdlet in cmdlets)
                        {
                            AddCommandResult(cmdlet, true, completingAtStartOfLine, quote, results);
                        }
                    }
                    else
                    {
                        PrependSnapInNameForSameCmdletNames(cmdlets, completingAtStartOfLine, quote, results);
                    }
                }

                return results;
            }

            private static void AddCommandResult(CommandAndName commandAndName, bool useFullName, bool completingAtStartOfLine, string quote, List<CompletionResult> results)
            {
                Diagnostics.Assert(results != null, "Caller needs to make sure the result list is not null");

                string name = useFullName ? commandAndName.CommandName.FullName : commandAndName.CommandName.ShortName;
                string quotedFileName = AddQuoteIfNecessary(name, quote, completingAtStartOfLine);

                var commandType = SafeGetProperty<CommandTypes?>(commandAndName.Command, "CommandType");
                if (commandType == null)
                {
                    return;
                }

                string toolTip;
                string displayName = SafeGetProperty<string>(commandAndName.Command, "Name");

                if (commandType.Value == CommandTypes.Cmdlet || commandType.Value == CommandTypes.Application)
                {
                    toolTip = SafeGetProperty<string>(commandAndName.Command, "Definition");
                }
                else
                {
                    toolTip = displayName;
                }

                results.Add(new CompletionResult(quotedFileName, displayName, CompletionResultType.Command, toolTip));
            }

            private static void PrependSnapInNameForSameCmdletNames(CommandAndName[] cmdlets, bool completingAtStartOfLine, string quote, List<CompletionResult> results)
            {
                Diagnostics.Assert(cmdlets != null && cmdlets.Length > 0,
                    "HasMultiplePSSnapIns must be called with a non-empty collection of PSObject");

                int i = 0;
                bool previousMatched = false;
                while (true)
                {
                    CommandAndName commandAndName = cmdlets[i];

                    int lookAhead = i + 1;
                    if (lookAhead >= cmdlets.Length)
                    {
                        AddCommandResult(commandAndName, previousMatched, completingAtStartOfLine, quote, results);
                        break;
                    }

                    CommandAndName nextCommandAndName = cmdlets[lookAhead];

                    if (string.Compare(
                            commandAndName.CommandName.ShortName,
                            nextCommandAndName.CommandName.ShortName,
                            StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        AddCommandResult(commandAndName, true, completingAtStartOfLine, quote, results);
                        previousMatched = true;
                    }
                    else
                    {
                        AddCommandResult(commandAndName, previousMatched, completingAtStartOfLine, quote, results);
                        previousMatched = false;
                    }

                    i++;
                }
            }

            #endregion "Handle Command"

            #region "Handle File Names"

            internal static List<CompletionResult> PSv2GenerateMatchSetOfFiles(PowerShellExecutionHelper helper, string lastWord, bool completingAtStartOfLine, string quote)
            {
                var results = new List<CompletionResult>();

                // lastWord is treated as an PSPath. The match set includes those items that match that
                // path, namely, the union of:
                //  (S1) the sorted set of items matching the last word
                //  (S2) the sorted set of items matching the last word + *
                // If the last word contains no wildcard characters, then S1 is the empty set.  S1 is always
                // a subset of S2, but we want to present S1 first, then (S2 - S1) next.  The idea is that
                // if the user typed some wildcard characters, they'd prefer to see those matches before
                // all of the rest.

                // Determine if we need to quote the paths we parse

                lastWord = lastWord ?? string.Empty;
                bool isLastWordEmpty = string.IsNullOrEmpty(lastWord);
                bool lastCharIsStar = !isLastWordEmpty && lastWord.EndsWith("*", StringComparison.Ordinal);
                bool containsGlobChars = WildcardPattern.ContainsWildcardCharacters(lastWord);

                string wildWord = lastWord + "*";
                bool shouldFullyQualifyPaths = PSv2ShouldFullyQualifyPathsPath(helper, lastWord);

                // NTRAID#Windows Out Of Band Releases-927933-2006/03/13-JeffJon
                // Need to detect when the path is a provider-direct path and make sure
                // to remove the provider-qualifier when the resolved path is returned.
                bool isProviderDirectPath = lastWord.StartsWith(@"\\", StringComparison.Ordinal) ||
                                            lastWord.StartsWith("//", StringComparison.Ordinal);

                List<PathItemAndConvertedPath> s1 = null;
                List<PathItemAndConvertedPath> s2 = null;

                if (containsGlobChars && !isLastWordEmpty)
                {
                    s1 = PSv2FindMatches(
                            helper,
                            lastWord,
                            shouldFullyQualifyPaths);
                }

                if (!lastCharIsStar)
                {
                    s2 = PSv2FindMatches(
                            helper,
                            wildWord,
                            shouldFullyQualifyPaths);
                }

                IEnumerable<PathItemAndConvertedPath> combinedMatches = CombineMatchSets(s1, s2);

                if (combinedMatches != null)
                {
                    foreach (var combinedMatch in combinedMatches)
                    {
                        string combinedMatchPath = WildcardPattern.Escape(combinedMatch.Path);
                        string combinedMatchConvertedPath = WildcardPattern.Escape(combinedMatch.ConvertedPath);
                        string completionText = isProviderDirectPath ? combinedMatchConvertedPath : combinedMatchPath;

                        completionText = AddQuoteIfNecessary(completionText, quote, completingAtStartOfLine);

                        bool? isContainer = SafeGetProperty<bool?>(combinedMatch.Item, "PSIsContainer");
                        string childName = SafeGetProperty<string>(combinedMatch.Item, "PSChildName");
                        string toolTip = PowerShellExecutionHelper.SafeToString(combinedMatch.ConvertedPath);

                        if (isContainer != null && childName != null && toolTip != null)
                        {
                            CompletionResultType resultType = isContainer.Value
                                                                  ? CompletionResultType.ProviderContainer
                                                                  : CompletionResultType.ProviderItem;
                            results.Add(new CompletionResult(completionText, childName, resultType, toolTip));
                        }
                    }
                }

                return results;
            }

            private static string AddQuoteIfNecessary(string completionText, string quote, bool completingAtStartOfLine)
            {
                if (completionText.IndexOfAny(s_charsRequiringQuotedString) != -1)
                {
                    bool needAmpersand = quote.Length == 0 && completingAtStartOfLine;
                    string quoteInUse = quote.Length == 0 ? "'" : quote;
                    completionText = quoteInUse == "'" ? completionText.Replace("'", "''") : completionText;
                    completionText = quoteInUse + completionText + quoteInUse;
                    completionText = needAmpersand ? "& " + completionText : completionText;
                }
                else
                {
                    completionText = quote + completionText + quote;
                }

                return completionText;
            }

            private static IEnumerable<PathItemAndConvertedPath> CombineMatchSets(List<PathItemAndConvertedPath> s1, List<PathItemAndConvertedPath> s2)
            {
                if (s1 == null || s1.Count < 1)
                {
                    // only s2 contains results; which may be null or empty
                    return s2;
                }

                if (s2 == null || s2.Count < 1)
                {
                    // only s1 contains results
                    return s1;
                }

                // s1 and s2 contain results
                Diagnostics.Assert(s1 != null && s1.Count > 0, "s1 should have results");
                Diagnostics.Assert(s2 != null && s2.Count > 0, "if s1 has results, s2 must also");
                Diagnostics.Assert(s1.Count <= s2.Count, "s2 should always be larger than s1");

                var result = new List<PathItemAndConvertedPath>();

                // we need to remove from s2 those items in s1.  Since the results from FindMatches will be sorted,
                // just copy out the unique elements from s2 and s1.  We know that every element of S1 will be in S2,
                // so the result set will be S1 + (S2 - S1), which is the same size as S2.
                result.AddRange(s1);
                for (int i = 0, j = 0; i < s2.Count; ++i)
                {
                    if (j < s1.Count && string.Compare(s2[i].Path, s1[j].Path, StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        ++j;
                        continue;
                    }

                    result.Add(s2[i]);
                }

#if DEBUG
                Diagnostics.Assert(result.Count == s2.Count, "result should be the same size as s2, see the size comment above");
                for (int i = 0; i < s1.Count; ++i)
                {
                    string path = result[i].Path;
                    int j = result.FindLastIndex(item => item.Path == path);
                    Diagnostics.Assert(j == i, "elements of s1 should only come at the start of the results");
                }
#endif
                return result;
            }

            private static T SafeGetProperty<T>(PSObject psObject, string propertyName)
            {
                if (psObject == null)
                {
                    return default(T);
                }

                PSPropertyInfo property = psObject.Properties[propertyName];
                if (property == null)
                {
                    return default(T);
                }

                object propertyValue = property.Value;
                if (propertyValue == null)
                {
                    return default(T);
                }

                T returnValue;
                if (LanguagePrimitives.TryConvertTo(propertyValue, out returnValue))
                {
                    return returnValue;
                }

                return default(T);
            }

            private static bool PSv2ShouldFullyQualifyPathsPath(PowerShellExecutionHelper helper, string lastWord)
            {
                // These are special cases, as they represent cases where the user expects to
                // see the full path.
                if (lastWord.StartsWith("~", StringComparison.OrdinalIgnoreCase) ||
                    lastWord.StartsWith("\\", StringComparison.OrdinalIgnoreCase) ||
                    lastWord.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                helper.CurrentPowerShell
                    .AddCommand("Split-Path")
                    .AddParameter("Path", lastWord)
                    .AddParameter("IsAbsolute", true);

                bool isAbsolute = helper.ExecuteCommandAndGetResultAsBool();
                return isAbsolute;
            }

            private struct PathItemAndConvertedPath
            {
                internal readonly string Path;
                internal readonly PSObject Item;
                internal readonly string ConvertedPath;

                internal PathItemAndConvertedPath(string path, PSObject item, string convertedPath)
                {
                    this.Path = path;
                    this.Item = item;
                    this.ConvertedPath = convertedPath;
                }
            }

            private static List<PathItemAndConvertedPath> PSv2FindMatches(PowerShellExecutionHelper helper, string path, bool shouldFullyQualifyPaths)
            {
                Diagnostics.Assert(!string.IsNullOrEmpty(path), "path should have a value");
                var result = new List<PathItemAndConvertedPath>();

                Exception exceptionThrown;
                PowerShell powershell = helper.CurrentPowerShell;

                // It's OK to use script, since tab completion is useless when the remote Win7 machine is in nolanguage mode
                if (!shouldFullyQualifyPaths)
                {
                    powershell.AddScript(string.Format(
                        CultureInfo.InvariantCulture,
                        "& {{ trap {{ continue }} ; resolve-path {0} -Relative -WarningAction SilentlyContinue | ForEach-Object {{,($_,(get-item $_ -WarningAction SilentlyContinue),(convert-path $_ -WarningAction SilentlyContinue))}} }}",
                        path));
                }
                else
                {
                    powershell.AddScript(string.Format(
                        CultureInfo.InvariantCulture,
                        "& {{ trap {{ continue }} ; resolve-path {0} -WarningAction SilentlyContinue | ForEach-Object {{,($_,(get-item $_ -WarningAction SilentlyContinue),(convert-path $_ -WarningAction SilentlyContinue))}} }}",
                        path));
                }

                Collection<PSObject> paths = helper.ExecuteCurrentPowerShell(out exceptionThrown);
                if (paths == null || paths.Count == 0)
                {
                    return null;
                }

                foreach (PSObject t in paths)
                {
                    var pathsArray = t.BaseObject as IList;
                    if (pathsArray != null && pathsArray.Count == 3)
                    {
                        object objectPath = pathsArray[0];
                        PSObject item = pathsArray[1] as PSObject;
                        object convertedPath = pathsArray[1];

                        if (objectPath == null || item == null || convertedPath == null)
                        {
                            continue;
                        }

                        result.Add(new PathItemAndConvertedPath(
                                            PowerShellExecutionHelper.SafeToString(objectPath),
                                            item,
                                            PowerShellExecutionHelper.SafeToString(convertedPath)));
                    }
                }

                if (result.Count == 0)
                {
                    return null;
                }

                result.Sort(delegate (PathItemAndConvertedPath x, PathItemAndConvertedPath y)
                                {
                                    Diagnostics.Assert(x.Path != null && y.Path != null, "SafeToString always returns a non-null string");
                                    return string.Compare(x.Path, y.Path, StringComparison.CurrentCultureIgnoreCase);
                                });

                return result;
            }

            #endregion "Handle File Names"
        }

        /// <summary>
        /// LastWordFinder implements the algorithm we use to search for the last word in a line of input taken from the console.
        /// This class exists for legacy purposes only - V3 and forward uses a slightly different interface.
        /// </summary>
        private class LastWordFinder
        {
            internal static string FindLastWord(string sentence, out int replacementIndexOut, out char closingQuote)
            {
                return (new LastWordFinder(sentence)).FindLastWord(out replacementIndexOut, out closingQuote);
            }

            private LastWordFinder(string sentence)
            {
                _replacementIndex = 0;
                Diagnostics.Assert(sentence != null, "need to provide an instance");
                _sentence = sentence;
            }

            /// <summary>
            /// Locates the last "word" in a string of text.  A word is a conguous sequence of characters that are not
            /// whitespace, or a contiguous set grouped by single or double quotes.  Can be called by at most 1 thread at a time
            /// per LastWordFinder instance.
            /// </summary>
            /// <param name="replacementIndexOut">
            /// Receives the character index (from the front of the string) of the starting point of the located word, or 0 if
            /// the word starts at the beginning of the sentence.
            /// </param>
            /// <param name="closingQuote">
            /// Receives the quote character that would be needed to end the sentence with a balanced pair of quotes.  For
            /// instance, if sentence is "foo then " is returned, if sentence if "foo" then nothing is returned, if sentence is
            /// 'foo then ' is returned, if sentence is 'foo' then nothing is returned.
            /// </param>
            /// <returns>The last word located, or the empty string if no word could be found.</returns>
            private string FindLastWord(out int replacementIndexOut, out char closingQuote)
            {
                bool inSingleQuote = false;
                bool inDoubleQuote = false;

                ReplacementIndex = 0;

                for (_sentenceIndex = 0; _sentenceIndex < _sentence.Length; ++_sentenceIndex)
                {
                    Diagnostics.Assert(!(inSingleQuote && inDoubleQuote),
                        "Can't be in both single and double quotes");

                    char c = _sentence[_sentenceIndex];

                    // there are 3 possibilities:
                    // 1) a new sequence is starting,
                    // 2) a sequence is ending, or
                    // 3) a sequence is due to end on the next matching quote, end-of-sentence, or whitespace

                    if (c == '\'')
                    {
                        HandleQuote(ref inSingleQuote, ref inDoubleQuote, c);
                    }
                    else if (c == '"')
                    {
                        HandleQuote(ref inDoubleQuote, ref inSingleQuote, c);
                    }
                    else if (c == '`')
                    {
                        Consume(c);
                        if (++_sentenceIndex < _sentence.Length)
                        {
                            Consume(_sentence[_sentenceIndex]);
                        }
                    }
                    else if (IsWhitespace(c))
                    {
                        if (_sequenceDueToEnd)
                        {
                            // we skipped a quote earlier, now end that sequence

                            _sequenceDueToEnd = false;
                            if (inSingleQuote)
                            {
                                inSingleQuote = false;
                            }

                            if (inDoubleQuote)
                            {
                                inDoubleQuote = false;
                            }

                            ReplacementIndex = _sentenceIndex + 1;
                        }
                        else if (inSingleQuote || inDoubleQuote)
                        {
                            // a sequence is started and we're in quotes

                            Consume(c);
                        }
                        else
                        {
                            // no sequence is started, so ignore c

                            ReplacementIndex = _sentenceIndex + 1;
                        }
                    }
                    else
                    {
                        // a sequence is started and we're in it

                        Consume(c);
                    }
                }

                string result = new string(_wordBuffer, 0, _wordBufferIndex);

                closingQuote = inSingleQuote ? '\'' : inDoubleQuote ? '"' : '\0';
                replacementIndexOut = ReplacementIndex;
                return result;
            }

            private void HandleQuote(ref bool inQuote, ref bool inOppositeQuote, char c)
            {
                if (inOppositeQuote)
                {
                    // a sequence is started, and we're in it.
                    Consume(c);
                    return;
                }

                if (inQuote)
                {
                    if (_sequenceDueToEnd)
                    {
                        // I've ended a sequence and am starting another; don't consume c, update replacementIndex
                        ReplacementIndex = _sentenceIndex + 1;
                    }

                    _sequenceDueToEnd = !_sequenceDueToEnd;
                }
                else
                {
                    // I'm starting a sequence; don't consume c, update replacementIndex
                    inQuote = true;
                    ReplacementIndex = _sentenceIndex;
                }
            }

            private void Consume(char c)
            {
                Diagnostics.Assert(_wordBuffer != null, "wordBuffer is not initialized");
                Diagnostics.Assert(_wordBufferIndex < _wordBuffer.Length, "wordBufferIndex is out of range");

                _wordBuffer[_wordBufferIndex++] = c;
            }

            private int ReplacementIndex
            {
                get { return _replacementIndex; }

                set
                {
                    Diagnostics.Assert(value >= 0 && value < _sentence.Length + 1, "value out of range");

                    // when we set the replacement index, that means we're also resetting our word buffer. we know wordBuffer
                    // will never be longer than sentence.

                    _wordBuffer = new char[_sentence.Length];
                    _wordBufferIndex = 0;
                    _replacementIndex = value;
                }
            }

            private static bool IsWhitespace(char c)
            {
                return (c == ' ') || (c == '\x0009');
            }

            private readonly string _sentence;
            private char[] _wordBuffer;
            private int _wordBufferIndex;
            private int _replacementIndex;
            private int _sentenceIndex;
            private bool _sequenceDueToEnd;
        }

        #endregion private methods
    }
}
