// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
#if LEGACYTELEMETRY
using System.Diagnostics;
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
                throw PSTraceSource.NewArgumentException(nameof(cursorIndex));
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
            if (input == null || input.Length == 0)
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
                throw PSTraceSource.NewArgumentNullException(nameof(ast));
            }

            if (tokens == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(tokens));
            }

            if (positionOfCursor == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(positionOfCursor));
            }

            if (ast.Extent.Text.Length == 0)
            {
                return s_emptyCommandCompletion;
            }

            return CompleteInputImpl(ast, tokens, positionOfCursor, options);
        }

        /// <summary>
        /// Invokes the script function TabExpansion2.
        /// </summary>
        /// <param name="input">The input script to complete.</param>
        /// <param name="cursorIndex">The offset in <paramref name="input"/> where completion is requested.</param>
        /// <param name="options">Optional parameter that specifies configurable options for completion.</param>
        /// <param name="powershell">The powershell to use to invoke the script function TabExpansion2.</param>
        /// <returns>A collection of completions with the replacement start and length.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "powershell")]
        public static CommandCompletion CompleteInput(string input, int cursorIndex, Hashtable options, PowerShell powershell)
        {
            if (input == null || input.Length == 0)
            {
                return s_emptyCommandCompletion;
            }

            if (cursorIndex > input.Length)
            {
                throw PSTraceSource.NewArgumentException(nameof(cursorIndex));
            }

            if (powershell == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(powershell));
            }

            // If we are in a debugger stop, let the debugger do the command completion.
            var debugger = powershell.Runspace?.Debugger;
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

                    // TabExpansion2 script is not available prior to PSv3.
                    if (remoteRunspace.GetCapabilities().Equals(Runspaces.RunspaceCapability.Default))
                    {
                        return s_emptyCommandCompletion;
                    }
                }
            }

            return CallScriptWithStringParameterSet(input, cursorIndex, options, powershell);
        }

        /// <summary>
        /// Invokes the script function TabExpansion2.
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
                throw PSTraceSource.NewArgumentNullException(nameof(ast));
            }

            if (tokens == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(tokens));
            }

            if (cursorPosition == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(cursorPosition));
            }

            if (powershell == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(powershell));
            }

            if (ast.Extent.Text.Length == 0)
            {
                return s_emptyCommandCompletion;
            }

            // If we are in a debugger stop, let the debugger do the command completion.
            var debugger = powershell.Runspace?.Debugger;
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

                    // TabExpansion2 script is not available prior to PSv3.
                    if (remoteRunspace.GetCapabilities().Equals(Runspaces.RunspaceCapability.Default))
                    {
                        return s_emptyCommandCompletion;
                    }

                    // When calling the TabExpansion2 script, the input should be the whole script text
                    string input = ast.Extent.Text;
                    int cursorIndex = ((InternalScriptPosition)cursorPosition).Offset;
                    return CallScriptWithStringParameterSet(input, cursorIndex, options, powershell);
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
                throw PSTraceSource.NewArgumentException(nameof(cursorIndex));
            }

            if (debugger == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(debugger));
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
                throw PSTraceSource.NewArgumentNullException(nameof(ast));
            }

            if (tokens == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(tokens));
            }

            if (cursorPosition == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(cursorPosition));
            }

            if (debugger == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(debugger));
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
#if LEGACYTELEMETRY
            // We could start collecting telemetry at a later date.
            // We will leave the #if to remind us that we did this once.
            var sw = new Stopwatch();
            sw.Start();
#endif
            using (var powershell = PowerShell.Create(RunspaceMode.CurrentRunspace))
            {
                var context = LocalPipeline.GetExecutionContextFromTLS();

                int replacementIndex = -1;
                int replacementLength = -1;
                List<CompletionResult> results = null;

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

#if LEGACYTELEMETRY
                // no telemetry here. We don't capture tab completion performance.
                sw.Stop();
                TelemetryAPI.ReportTabCompletionTelemetry(sw.ElapsedMilliseconds, completionResults.Count,
                    completionResults.Count > 0 ? completionResults[0].ResultType : CompletionResultType.Text);
#endif
                return new CommandCompletion(
                    new Collection<CompletionResult>(completionResults),
                    -1,
                    replacementIndex,
                    replacementLength);
            }
        }

        #endregion private methods
    }
}
