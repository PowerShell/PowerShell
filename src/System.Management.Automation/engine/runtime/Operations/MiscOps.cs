// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Internal.Host;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using Microsoft.PowerShell.Commands;
using Microsoft.PowerShell.Commands.Internal.Format;

// ReSharper disable UnusedMember.Global

namespace System.Management.Automation
{
    internal static class PipelineOps
    {
        private static CommandProcessorBase AddCommand(PipelineProcessor pipe,
                                                       CommandParameterInternal[] commandElements,
                                                       CommandBaseAst commandBaseAst,
                                                       CommandRedirection[] redirections,
                                                       ExecutionContext context)
        {
            var commandAst = commandBaseAst as CommandAst;
            var invocationToken = commandAst != null ? commandAst.InvocationOperator : TokenKind.Unknown;
            bool dotSource = invocationToken == TokenKind.Dot;
            SessionStateInternal commandSessionState = null;
            int commandIndex = 0;

            Diagnostics.Assert(commandElements[0].ArgumentSpecified && !commandElements[0].ParameterNameSpecified,
                "Compiler will pass first parameter as an argument.");
            var mi = PSObject.Base(commandElements[0].ArgumentValue) as PSModuleInfo;
            if (mi != null)
            {
                if (mi.ModuleType == ModuleType.Binary && mi.SessionState == null)
                {
                    throw InterpreterError.NewInterpreterException(null, typeof(RuntimeException),
                        null, "CantInvokeInBinaryModule", ParserStrings.CantInvokeInBinaryModule, mi.Name);
                }
                else if (mi.SessionState == null)
                {
                    throw InterpreterError.NewInterpreterException(null, typeof(RuntimeException),
                        null, "CantInvokeInNonImportedModule", ParserStrings.CantInvokeInNonImportedModule, mi.Name);
                }
                else if (((invocationToken == TokenKind.Ampersand) || (invocationToken == TokenKind.Dot)) && (mi.LanguageMode != context.LanguageMode))
                {
                    // Disallow FullLanguage "& (Get-Module MyModule) MyPrivateFn" from ConstrainedLanguage because it always
                    // runs "internal" origin and so has access to all functions, including non-exported functions.
                    // Otherwise we end up leaking non-exported functions that run in FullLanguage.
                    throw InterpreterError.NewInterpreterException(null, typeof(RuntimeException), null,
                        "CantInvokeCallOperatorAcrossLanguageBoundaries", ParserStrings.CantInvokeCallOperatorAcrossLanguageBoundaries);
                }

                commandSessionState = mi.SessionState.Internal;
                commandIndex += 1;
            }

            object command;
            IScriptExtent commandExtent;
            var cpiCommand = commandElements[commandIndex];
            if (cpiCommand.ParameterNameSpecified)
            {
                command = cpiCommand.ParameterText;
                commandExtent = cpiCommand.ParameterExtent;
                if (cpiCommand.ArgumentSpecified)
                {
                    // BUG: we've seen something like:
                    //     & (gmo Module) -foo: bar
                    // The command is -foo:, and bar is an argument, but both are in commandElements[commandIndex],
                    // so we won't add 'bar' as an argument.
                }
            }
            else
            {
                command = PSObject.Base(cpiCommand.ArgumentValue);
                commandExtent = cpiCommand.ArgumentExtent;
            }

            string invocationName = (dotSource) ? "." : invocationToken == TokenKind.Ampersand ? "&" : null;
            CommandProcessorBase commandProcessor;
            var scriptBlock = command as ScriptBlock;
            if (scriptBlock != null)
            {
                commandProcessor = CommandDiscovery.CreateCommandProcessorForScript(scriptBlock, context, !dotSource, commandSessionState);
            }
            else
            {
                var commandInfo = command as CommandInfo;
                if (commandInfo != null)
                {
                    commandProcessor = context.CommandDiscovery.LookupCommandProcessor(commandInfo, context.EngineSessionState.CurrentScope.ScopeOrigin, !dotSource, commandSessionState);
                }
                else
                {
                    var commandName = command as string ?? PSObject.ToStringParser(context, command);
                    invocationName ??= commandName;

                    if (string.IsNullOrEmpty(commandName))
                    {
                        throw InterpreterError.NewInterpreterException(
                            command,
                            typeof(RuntimeException),
                            commandExtent,
                            "BadExpression",
                            ParserStrings.BadExpression,
                            dotSource ? "." : "&");
                    }

                    try
                    {
                        // See if we need to resolve the command in a different session state
                        // as will be the case with modules...
                        // BUGBUG - this can be cleaned up by fixing the overload on execution context (but not easily.)
                        if (commandSessionState != null)
                        {
                            SessionStateInternal oldSessionState = context.EngineSessionState;
                            try
                            {
                                context.EngineSessionState = commandSessionState;
                                commandProcessor = context.CreateCommand(commandName, dotSource);
                            }
                            finally
                            {
                                context.EngineSessionState = oldSessionState;
                            }
                        }
                        else
                        {
                            commandProcessor = context.CreateCommand(commandName, dotSource);
                        }
                    }
                    catch (RuntimeException rte)
                    {
                        // CreateCommand doesn't have the context to set InvocationInfo properly
                        // so we'll do it here instead...
                        if (rte.ErrorRecord.InvocationInfo == null)
                        {
                            InvocationInfo invocationInfo = new InvocationInfo(null, commandExtent, context)
                            { InvocationName = invocationName };
                            rte.ErrorRecord.SetInvocationInfo(invocationInfo);
                        }

                        throw;
                    }
                }
            }

            InternalCommand cmd = commandProcessor.Command;
            commandProcessor.UseLocalScope = !dotSource &&
                                             (cmd is ScriptCommand || cmd is PSScriptCmdlet);

            bool isNativeCommand = commandProcessor is NativeCommandProcessor;
            for (int i = commandIndex + 1; i < commandElements.Length; ++i)
            {
                var cpi = commandElements[i];

                if (cpi.ParameterNameSpecified)
                {
                    // Skip adding the special -- parameter unless we're invoking a native command.
                    if (cpi.ParameterName.Equals("-", StringComparison.OrdinalIgnoreCase) && !isNativeCommand)
                    {
                        continue;
                    }
                }

                if (cpi.ArgumentToBeSplatted)
                {
                    foreach (var splattedCpi in Splat(cpi.ArgumentValue, cpi.ArgumentAst))
                    {
                        commandProcessor.AddParameter(splattedCpi);
                    }
                }
                else
                {
                    commandProcessor.AddParameter(cpi);
                }
            }

            string helpTarget;
            HelpCategory helpCategory;
            if (commandProcessor.IsHelpRequested(out helpTarget, out helpCategory))
            {
                commandProcessor = CommandProcessorBase.CreateGetHelpCommandProcessor(context, helpTarget, helpCategory);
            }

            commandProcessor.Command.InvocationExtent = commandBaseAst.Extent;
            commandProcessor.Command.MyInvocation.ScriptPosition = commandBaseAst.Extent;
            commandProcessor.Command.MyInvocation.InvocationName = invocationName;

            pipe.Add(commandProcessor);

            bool redirectedError = false;
            bool redirectedWarning = false;
            bool redirectedVerbose = false;
            bool redirectedDebug = false;
            bool redirectedInformation = false;
            if (redirections != null)
            {
                foreach (var redirection in redirections)
                {
                    redirection.Bind(pipe, commandProcessor, context);
                    switch (redirection.FromStream)
                    {
                        case RedirectionStream.Error:
                            redirectedError = true;
                            break;

                        case RedirectionStream.Warning:
                            redirectedWarning = true;
                            break;

                        case RedirectionStream.Verbose:
                            redirectedVerbose = true;
                            break;

                        case RedirectionStream.Debug:
                            redirectedDebug = true;
                            break;

                        case RedirectionStream.Information:
                            redirectedInformation = true;
                            break;

                        case RedirectionStream.All:
                            redirectedError = true;
                            redirectedWarning = true;
                            redirectedVerbose = true;
                            redirectedDebug = true;
                            redirectedInformation = true;
                            break;
                    }
                }
            }

            // Pipe redirection can also be specified via the ExecutionContext pipes.
            if (!redirectedError)
            {
                if (context.ShellFunctionErrorOutputPipe != null)
                {
                    commandProcessor.CommandRuntime.ErrorOutputPipe = context.ShellFunctionErrorOutputPipe;
                }
                else
                {
                    commandProcessor.CommandRuntime.ErrorOutputPipe.ExternalWriter = context.ExternalErrorOutput;
                }
            }

            if (!redirectedWarning && (context.ExpressionWarningOutputPipe != null))
            {
                commandProcessor.CommandRuntime.WarningOutputPipe = context.ExpressionWarningOutputPipe;
                redirectedWarning = true;
            }

            if (!redirectedVerbose && (context.ExpressionVerboseOutputPipe != null))
            {
                commandProcessor.CommandRuntime.VerboseOutputPipe = context.ExpressionVerboseOutputPipe;
                redirectedVerbose = true;
            }

            if (!redirectedDebug && (context.ExpressionDebugOutputPipe != null))
            {
                commandProcessor.CommandRuntime.DebugOutputPipe = context.ExpressionDebugOutputPipe;
                redirectedDebug = true;
            }

            if (!redirectedInformation && (context.ExpressionInformationOutputPipe != null))
            {
                commandProcessor.CommandRuntime.InformationOutputPipe = context.ExpressionInformationOutputPipe;
                redirectedInformation = true;
            }

            // Warning, Verbose, Debug should pick up any redirection information from its parent command runtime object.
            if (context.CurrentCommandProcessor != null && context.CurrentCommandProcessor.CommandRuntime != null)
            {
                if (!redirectedWarning &&
                    context.CurrentCommandProcessor.CommandRuntime.WarningOutputPipe != null)
                {
                    commandProcessor.CommandRuntime.WarningOutputPipe = context.CurrentCommandProcessor.CommandRuntime.WarningOutputPipe;
                }

                if (!redirectedVerbose &&
                    context.CurrentCommandProcessor.CommandRuntime.VerboseOutputPipe != null)
                {
                    commandProcessor.CommandRuntime.VerboseOutputPipe = context.CurrentCommandProcessor.CommandRuntime.VerboseOutputPipe;
                }

                if (!redirectedDebug &&
                    context.CurrentCommandProcessor.CommandRuntime.DebugOutputPipe != null)
                {
                    commandProcessor.CommandRuntime.DebugOutputPipe = context.CurrentCommandProcessor.CommandRuntime.DebugOutputPipe;
                }

                if (!redirectedInformation &&
                    context.CurrentCommandProcessor.CommandRuntime.InformationOutputPipe != null)
                {
                    commandProcessor.CommandRuntime.InformationOutputPipe = context.CurrentCommandProcessor.CommandRuntime.InformationOutputPipe;
                }
            }

            return commandProcessor;
        }

        internal static IEnumerable<CommandParameterInternal> Splat(object splattedValue, Ast splatAst)
        {
            splattedValue = PSObject.Base(splattedValue);

            var markUntrustedData = false;
            if (ExecutionContext.HasEverUsedConstrainedLanguage)
            {
                // If the value to be splatted is untrusted, then make sure sub-values held by it are
                // also marked as untrusted.
                markUntrustedData = ExecutionContext.IsMarkedAsUntrusted(splattedValue);
            }

            IDictionary splattedTable = splattedValue as IDictionary;
            if (splattedTable != null)
            {
                foreach (DictionaryEntry de in splattedTable)
                {
                    string parameterName = de.Key.ToString();
                    object parameterValue = de.Value;
                    string parameterText = GetParameterText(parameterName);

                    if (markUntrustedData)
                    {
                        ExecutionContext.MarkObjectAsUntrusted(parameterValue);
                    }

                    yield return CommandParameterInternal.CreateParameterWithArgument(
                        parameterAst: splatAst,
                        parameterName: parameterName,
                        parameterText: parameterText,
                        argumentAst: splatAst,
                        value: parameterValue,
                        spaceAfterParameter: false,
                        fromSplatting: true);
                }
            }
            else
            {
                IEnumerable enumerableValue = splattedValue as IEnumerable;
                if (enumerableValue != null)
                {
                    foreach (object obj in enumerableValue)
                    {
                        if (markUntrustedData)
                        {
                            ExecutionContext.MarkObjectAsUntrusted(obj);
                        }

                        yield return SplatEnumerableElement(obj, splatAst);
                    }
                }
                else
                {
                    yield return SplatEnumerableElement(splattedValue, splatAst);
                }
            }
        }

        private static CommandParameterInternal SplatEnumerableElement(object splattedArgument, Ast splatAst)
        {
            var psObject = splattedArgument as PSObject;
            if (psObject != null)
            {
                var prop = psObject.Properties[ScriptParameterBinderController.NotePropertyNameForSplattingParametersInArgs];
                var baseObj = psObject.BaseObject;
                if (prop != null && prop.Value is string && baseObj is string)
                {
                    return CommandParameterInternal.CreateParameter((string)prop.Value, (string)baseObj, splatAst);
                }
            }

            return CommandParameterInternal.CreateArgument(splattedArgument, splatAst);
        }

        private static string GetParameterText(string parameterName)
        {
            Diagnostics.Assert(parameterName != null, "caller makes sure the parameterName is not null");
            int endPosition = parameterName.Length;
            while ((endPosition > 0) && char.IsWhiteSpace(parameterName[endPosition - 1]))
            {
                endPosition--;
            }

            if (endPosition == 0 || parameterName[endPosition - 1] == ':')
            {
                return "-" + parameterName;
            }

            string parameterText;
            if (endPosition == parameterName.Length)
            {
                parameterText = "-" + parameterName + ":";
            }
            else
            {
                string whitespaces = parameterName.Substring(endPosition);
                parameterText = string.Concat("-", parameterName.AsSpan(0, endPosition), ":", whitespaces);
            }

            return parameterText;
        }

        internal static void InvokePipeline(object input,
                                            bool ignoreInput,
                                            CommandParameterInternal[][] pipeElements,
                                            CommandBaseAst[] pipeElementAsts,
                                            CommandRedirection[][] commandRedirections,
                                            FunctionContext funcContext)
        {
            PipelineProcessor pipelineProcessor = new PipelineProcessor();
            ExecutionContext context = funcContext._executionContext;
            Pipe outputPipe = funcContext._outputPipe;

            try
            {
                context.Events?.ProcessPendingActions();

                if (input == AutomationNull.Value && !ignoreInput)
                {
                    // We have seen something like:
                    //    $e | measure-object
                    // And $e is AutomationNull.Value.  We want to ensure
                    // measure-object runs w/o sending anything through the pipe,
                    // so we'll turn the pipe into Out-Null | ...
                    // This cleanly avoids any problems with the pipeline processing
                    // code dealing with null/AutomationNull input going directly to
                    // the first command (e.g. Measure-Object).
                    AddNoopCommandProcessor(pipelineProcessor, context);
                }

                CommandProcessorBase commandProcessor = null;
                CommandRedirection[] commandRedirection = null;
                // If we add a json adapter, we need to modify the history string to include the adapter name.
                bool commandAdded = false;

                for (int i = 0; i < pipeElements.Length; i++)
                {
                    commandRedirection = commandRedirections?[i];
                    commandProcessor = AddCommand(pipelineProcessor, pipeElements[i], pipeElementAsts[i],
                                                  commandRedirection, context);
                    if (ExperimentalFeature.IsEnabled("PSNativeJsonAdapter") && commandProcessor.CommandInfo is ApplicationInfo && commandRedirection is null)
                    {
                        var applicationInfo = (ApplicationInfo)commandProcessor.CommandInfo;
                        // Handle the Json adapter if it is set.
                        if (applicationInfo.JsonAdapter != null)
                        {
                            Token[] tokenList;
                            ParseError[] errorList;
                            var ast = Parser.ParseInput(applicationInfo.JsonAdapter.Name, out tokenList, out errorList);
                            CommandBaseAst cmdAst = ast?.Find(a => a is CommandBaseAst, false) as CommandBaseAst;
                            CommandAst jsonCommandAst = ast?.Find(a => a is CommandAst, false) as CommandAst;
                            // Process the Json adapter and add it to the pipeline.
                            if (jsonCommandAst != null && errorList.Length == 0)
                            {
                                // We will attach the ast of the original native command
                                var commandParameters = new List<CommandParameterInternal>();
                                foreach (var commandElement in jsonCommandAst.CommandElements)
                                {
                                    var commandParameterAst = commandElement as CommandParameterAst;
                                    if (commandParameterAst != null)
                                    {
                                        commandParameters.Add(GetCommandParameter(commandParameterAst, true, context));
                                        continue;
                                    }

                                    var exprAst = (ExpressionAst)commandElement;
                                    var argument = Compiler.GetExpressionValue(exprAst, true, context);
                                    var splatting = (exprAst is VariableExpressionAst && ((VariableExpressionAst)exprAst).Splatted);
                                    commandParameters.Add(CommandParameterInternal.CreateArgument(argument, exprAst, splatting));
                                }
                                
                                // Attach the parameters of the original native command as arguments for the adapter.
                                // Output from the original native command will be piped to the adapter.
                                foreach (var commandElement in pipeElements[i])
                                {
                                    commandParameters.Add(commandElement);
                                }

                                commandProcessor = AddCommand(pipelineProcessor, commandParameters.ToArray(), cmdAst, commandRedirection, context);
                                commandAdded = true;
                            }
                        }
                    }
                }

                var cmdletInfo = commandProcessor?.CommandInfo as CmdletInfo;
                if (cmdletInfo?.ImplementingType == typeof(OutNullCommand))
                {
                    var commandsCount = pipelineProcessor.Commands.Count;
                    if (commandsCount == 1)
                    {
                        // Out-Null is the only command, bail without running anything
                        return;
                    }

                    // Out-Null is the last command, rewrite command before Out-Null to a null pipe, but
                    // only if it didn't redirect anything, e.g. `Get-Stuff > o.txt | Out-Null`
                    var nextToLastCommand = pipelineProcessor.Commands[commandsCount - 2];
                    if (!nextToLastCommand.CommandRuntime.OutputPipe.IsRedirected)
                    {
                        pipelineProcessor.Commands.RemoveAt(commandsCount - 1);
                        commandProcessor = nextToLastCommand;
                        nextToLastCommand.CommandRuntime.OutputPipe = new Pipe { NullPipe = true };
                    }
                }

                if (commandProcessor != null && !commandProcessor.CommandRuntime.OutputPipe.IsRedirected)
                {
                    pipelineProcessor.LinkPipelineSuccessOutput(outputPipe ?? new Pipe(new List<object>()));

                    // Fix up merge redirection bindings on last command processor.
                    if (commandRedirection != null)
                    {
                        foreach (CommandRedirection redirection in commandRedirection)
                        {
                            if (redirection is MergingRedirection)
                            {
                                redirection.Bind(pipelineProcessor, commandProcessor, context);
                            }
                        }
                    }
                }

                if (commandAdded)
                {
                    // We've added a json adapter, update the history string.
                    UpdateHistory(context, pipelineProcessor);
                }

                context.PushPipelineProcessor(pipelineProcessor);
                try
                {
                    pipelineProcessor.SynchronousExecuteEnumerate(input);
                }
                finally
                {
                    context.PopPipelineProcessor(false);
                }
            }
            finally
            {
                context.QuestionMarkVariableValue = !pipelineProcessor.ExecutionFailed;
                pipelineProcessor.Dispose();
            }
        }

        // This is best effort to change history.
        internal static void UpdateHistory(ExecutionContext context, PipelineProcessor pipelineProcessor)
        {
            try
            {
                var runningPipeline =  context.CurrentRunspace.GetCurrentlyRunningPipeline() as LocalPipeline;
                if (runningPipeline is null)
                {
                    return;
                }
                List<string> commands = new List<string>();
                foreach (var commandProcessor in pipelineProcessor.Commands)
                {
                    commands.Add(commandProcessor.Command.InvocationExtent.Text);
                }
                runningPipeline.HistoryString = string.Join(" | ", commands);
            }
            catch (Exception)
            {
                // Ignore any exception.
            }
        }

        internal static void InvokePipelineInBackground(
                                            PipelineBaseAst pipelineAst,
                                            FunctionContext funcContext)
        {
            PipelineProcessor pipelineProcessor = new PipelineProcessor();
            ExecutionContext context = funcContext._executionContext;
            Pipe outputPipe = funcContext._outputPipe;

            try
            {
                context.Events?.ProcessPendingActions();

                CommandProcessorBase commandProcessor = null;

                // For background jobs rewrite the pipeline as a Start-Job command
                var scriptblockBodyString = pipelineAst.Extent.Text;
                var pipelineOffset = pipelineAst.Extent.StartOffset;
                var variables = pipelineAst.FindAll(static x => x is VariableExpressionAst, true);

                // Minimize allocations by initializing the stringbuilder to the size of the source string + space for ${using:} * 2
                System.Text.StringBuilder updatedScriptblock = new System.Text.StringBuilder(scriptblockBodyString.Length + 18);
                int position = 0;

                // Prefix variables in the scriptblock with $using:
                foreach (var v in variables)
                {
                    var variableName = ((VariableExpressionAst)v).VariablePath.UserPath;

                    // Skip variables that don't exist
                    if (funcContext._executionContext.EngineSessionState.GetVariable(variableName) == null)
                    {
                        continue;
                    }

                    // Skip PowerShell magic variables
                    if (!Regex.Match(
                            variableName,
                            "^(global:){0,1}(PID|PSVersionTable|PSEdition|PSHOME|HOST|TRUE|FALSE|NULL)$",
                            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Success)
                    {
                        updatedScriptblock.Append(scriptblockBodyString.AsSpan(position, v.Extent.StartOffset - pipelineOffset - position));
                        updatedScriptblock.Append("${using:");
                        updatedScriptblock.Append(CodeGeneration.EscapeVariableName(variableName));
                        updatedScriptblock.Append('}');
                        position = v.Extent.EndOffset - pipelineOffset;
                    }
                }

                updatedScriptblock.Append(scriptblockBodyString.AsSpan(position));
                var sb = ScriptBlock.Create(updatedScriptblock.ToString());
                var commandInfo = new CmdletInfo("Start-Job", typeof(StartJobCommand));
                commandProcessor = context.CommandDiscovery.LookupCommandProcessor(commandInfo, CommandOrigin.Internal, false, context.EngineSessionState);

                var workingDirectoryParameter = CommandParameterInternal.CreateParameterWithArgument(
                    parameterAst: pipelineAst,
                    parameterName: "WorkingDirectory",
                    parameterText: null,
                    argumentAst: pipelineAst,
                    value: context.SessionState.Path.CurrentLocation.Path,
                    spaceAfterParameter: false);

                var scriptBlockParameter = CommandParameterInternal.CreateParameterWithArgument(
                    parameterAst: pipelineAst,
                    parameterName: "ScriptBlock",
                    parameterText: null,
                    argumentAst: pipelineAst,
                    value: sb,
                    spaceAfterParameter: false);

                commandProcessor.AddParameter(workingDirectoryParameter);
                commandProcessor.AddParameter(scriptBlockParameter);
                pipelineProcessor.Add(commandProcessor);
                pipelineProcessor.LinkPipelineSuccessOutput(outputPipe ?? new Pipe(new List<object>()));

                context.PushPipelineProcessor(pipelineProcessor);
                try
                {
                    pipelineProcessor.SynchronousExecuteEnumerate(AutomationNull.Value);
                }
                finally
                {
                    context.PopPipelineProcessor(false);
                }
            }
            finally
            {
                context.QuestionMarkVariableValue = !pipelineProcessor.ExecutionFailed;
                pipelineProcessor.Dispose();
            }
        }

        private static void AddNoopCommandProcessor(PipelineProcessor pipelineProcessor, ExecutionContext context)
        {
            var commandInfo = new CmdletInfo("Out-Null", typeof(OutNullCommand));
            var commandProcessor = context.CommandDiscovery.LookupCommandProcessor(commandInfo,
                                                                                   context.EngineSessionState.CurrentScope.ScopeOrigin,
                                                                                   useLocalScope: false,
                                                                                   sessionState: null);
            pipelineProcessor.Add(commandProcessor);
        }

        internal static object CheckAutomationNullInCommandArgument(object obj)
        {
            if (obj == AutomationNull.Value)
            {
                return null;
            }

            var objAsArray = obj as object[];
            return objAsArray != null ? CheckAutomationNullInCommandArgumentArray(objAsArray) : obj;
        }

        internal static object[] CheckAutomationNullInCommandArgumentArray(object[] objArray)
        {
            if (objArray != null)
            {
                for (int i = 0; i < objArray.Length; ++i)
                {
                    if (objArray[i] == AutomationNull.Value)
                    {
                        objArray[i] = null;
                    }
                }
            }

            return objArray;
        }

        internal static SteppablePipeline GetSteppablePipeline(PipelineAst pipelineAst, CommandOrigin commandOrigin, ScriptBlock scriptBlock, object[] args)
        {
            var pipelineProcessor = new PipelineProcessor();
            var commandTuples = new List<Tuple<CommandAst, List<CommandParameterInternal>, List<CommandRedirection>>>();

            ExecutionContext context = LocalPipeline.GetExecutionContextFromTLS();
            if (context == null)
            {
                // If ExecutionContext from TLS is null then we are not in powershell engine thread.
                string scriptText = scriptBlock.ToString();
                scriptText = ErrorCategoryInfo.Ellipsize(CultureInfo.CurrentUICulture, scriptText);

                PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                    ParserStrings.GetSteppablePipelineFromWrongThread,
                    scriptText);

                e.SetErrorId("GetSteppablePipelineFromWrongThread");
                throw e;
            }

            // We try binding the parameter when following conditions are satisfied:
            //   * Arguments are provided
            //   * The ScriptBlock has parameters
            // If the script block has no parameter, RuntimeDefinedParameters.Data will be set to RuntimeDefinedParameterDictionary.EmptyParameterArray
            bool useParameter = (args != null && args.Length > 0) &&
                                scriptBlock.RuntimeDefinedParameters.Data !=
                                RuntimeDefinedParameterDictionary.EmptyParameterArray;
            try
            {
                if (useParameter)
                {
                    // If any parameters are used, we create a new scope and bind the parameters.

                    var newScope = context.EngineSessionState.NewScope(false);
                    context.EngineSessionState.CurrentScope = newScope;
                    context.EngineSessionState.CurrentScope.ScopeOrigin = CommandOrigin.Internal;

                    var locals = MutableTuple.MakeTuple(Compiler.DottedLocalsTupleType,
                                                        Compiler.DottedLocalsNameIndexMap);
                    object[] remainingArgs =
                        ScriptBlock.BindArgumentsForScriptblockInvoke(
                            (RuntimeDefinedParameter[])scriptBlock.RuntimeDefinedParameters.Data,
                            args, context, false, null, locals);
                    locals.SetAutomaticVariable(AutomaticVariable.Args, remainingArgs, context);
                    newScope.LocalsTuple = locals;
                }

                // GetSteppablePipeline() is called on an arbitrary script block with the intention
                // of invoking it. So the trustworthiness is defined by the trustworthiness of the
                // script block's language mode.
                bool isTrusted = scriptBlock.LanguageMode == PSLanguageMode.FullLanguage;

                foreach (var commandAst in pipelineAst.PipelineElements.Cast<CommandAst>())
                {
                    var commandParameters = new List<CommandParameterInternal>();
                    foreach (var commandElement in commandAst.CommandElements)
                    {
                        var commandParameterAst = commandElement as CommandParameterAst;
                        if (commandParameterAst != null)
                        {
                            commandParameters.Add(GetCommandParameter(commandParameterAst, isTrusted, context));
                            continue;
                        }

                        var exprAst = (ExpressionAst)commandElement;
                        var argument = Compiler.GetExpressionValue(exprAst, isTrusted, context);
                        var splatting = (exprAst is VariableExpressionAst && ((VariableExpressionAst)exprAst).Splatted);
                        commandParameters.Add(CommandParameterInternal.CreateArgument(argument, exprAst, splatting));
                    }

                    var redirections = new List<CommandRedirection>();
                    foreach (var redirection in commandAst.Redirections)
                    {
                        redirections.Add(GetCommandRedirection(redirection, isTrusted, context));
                    }

                    commandTuples.Add(Tuple.Create(commandAst, commandParameters, redirections));
                }
            }
            finally
            {
                if (useParameter)
                {
                    context.EngineSessionState.RemoveScope(context.EngineSessionState.CurrentScope);
                }
            }

            foreach (var commandTuple in commandTuples)
            {
                var commandProcessor = AddCommand(pipelineProcessor, commandTuple.Item2.ToArray(), commandTuple.Item1, commandTuple.Item3.ToArray(), context);
                commandProcessor.Command.CommandOriginInternal = commandOrigin;
                commandProcessor.CommandScope.ScopeOrigin = commandOrigin;
                commandProcessor.Command.MyInvocation.CommandOrigin = commandOrigin;

                // For nicer error reporting, we want to make it look like errors in the steppable pipeline point back to
                // the caller of the proxy.  We don't want errors pointing to the script block created in the proxy.
                // Here we assume (in a safe way) that GetSteppablePipeline is called from script.  If that isn't the case,
                // we won't crash, but the error reporting might be a little misleading.
                var callStack = context.Debugger.GetCallStack().ToArray();
                if (callStack.Length > 0 && Regex.IsMatch(callStack[0].Position.Text, "GetSteppablePipeline", RegexOptions.IgnoreCase))
                {
                    var myInvocation = commandProcessor.Command.MyInvocation;
                    myInvocation.InvocationName = callStack[0].InvocationInfo.InvocationName;
                    if (callStack.Length > 1)
                    {
                        var displayPosition = callStack[1].Position;
                        if (displayPosition != null && displayPosition != PositionUtilities.EmptyExtent)
                        {
                            myInvocation.DisplayScriptPosition = displayPosition;
                        }
                    }
                }

                // Set the data stream merge properties based on ExecutionContext.
                if (context.CurrentCommandProcessor != null && context.CurrentCommandProcessor.CommandRuntime != null)
                {
                    commandProcessor.CommandRuntime.SetMergeFromRuntime(context.CurrentCommandProcessor.CommandRuntime);
                }
            }

            return new SteppablePipeline(context, pipelineProcessor);
        }

        private static CommandParameterInternal GetCommandParameter(CommandParameterAst commandParameterAst, bool isTrusted, ExecutionContext context)
        {
            var argumentAst = commandParameterAst.Argument;
            var errorPos = commandParameterAst.ErrorPosition;

            if (argumentAst == null)
            {
                return CommandParameterInternal.CreateParameter(commandParameterAst.ParameterName, errorPos.Text, commandParameterAst);
            }

            object argumentValue = Compiler.GetExpressionValue(argumentAst, isTrusted, context);
            bool spaceAfterParameter = (errorPos.EndLineNumber != argumentAst.Extent.StartLineNumber ||
                                        errorPos.EndColumnNumber != argumentAst.Extent.StartColumnNumber);
            return CommandParameterInternal.CreateParameterWithArgument(commandParameterAst, commandParameterAst.ParameterName,
                                                                        errorPos.Text, argumentAst, argumentValue,
                                                                        spaceAfterParameter);
        }

        private static CommandRedirection GetCommandRedirection(RedirectionAst redirectionAst, bool isTrusted, ExecutionContext context)
        {
            var fileRedirection = redirectionAst as FileRedirectionAst;
            if (fileRedirection != null)
            {
                object fileName = Compiler.GetExpressionValue(fileRedirection.Location, isTrusted, context);
                return new FileRedirection(fileRedirection.FromStream, fileRedirection.Append, fileName.ToString());
            }

            var mergingRedirectionAst = (MergingRedirectionAst)redirectionAst;
            return new MergingRedirection(mergingRedirectionAst.FromStream, mergingRedirectionAst.ToStream);
        }

        internal static object PipelineResult(List<object> resultList)
        {
            var resultCount = resultList.Count;
            if (resultCount == 0)
            {
                return AutomationNull.Value;
            }

            var result = resultCount == 1 ? resultList[0] : resultList.ToArray();
            // Clear the array list so that we don't write the results of the pipe when flushing the pipe.
            resultList.Clear();
            return result;
        }

        internal static void FlushPipe(Pipe oldPipe, List<object> resultList)
        {
            for (int i = 0; i < resultList.Count; i++)
            {
                oldPipe.Add(resultList[i]);
            }
        }

        internal static void ClearPipe(List<object> resultList)
        {
            resultList.Clear();
        }

        internal static ExitException GetExitException(object exitCodeObj)
        {
            int exitCode = 0;

            try
            {
                if (!LanguagePrimitives.IsNull(exitCodeObj))
                {
                    exitCode = ParserOps.ConvertTo<int>(exitCodeObj, PositionUtilities.EmptyExtent);
                }
            }
            catch (Exception) // ignore non-severe exceptions
            {
            }

            return new ExitException(exitCode);
        }

        internal static void CheckForInterrupts(ExecutionContext context)
        {
            context.Events?.ProcessPendingActions();

            if (context.CurrentPipelineStopping)
            {
                throw new PipelineStoppedException();
            }
        }

        // This is to work around a DLR problem with gotos in try/catch to the end of a lambda.
        internal static void Nop() { }
    }

    #region Redirections

    internal abstract class CommandRedirection
    {
        protected CommandRedirection(RedirectionStream from)
        {
            this.FromStream = from;
        }

        internal RedirectionStream FromStream { get; }

        internal abstract void Bind(PipelineProcessor pipelineProcessor, CommandProcessorBase commandProcessor, ExecutionContext context);

        internal void UnbindForExpression(FunctionContext funcContext, Pipe[] pipes)
        {
            if (pipes == null)
            {
                // The pipes can be null if there was an exception (ideally we'd just call unbind
                // from a fault, but that isn't supported in a clr dynamic method.
                return;
            }

            var context = funcContext._executionContext;
            switch (FromStream)
            {
                case RedirectionStream.All:
                    funcContext._outputPipe = pipes[(int)RedirectionStream.Output];
                    context.ShellFunctionErrorOutputPipe = pipes[(int)RedirectionStream.Error];
                    context.ExpressionWarningOutputPipe = pipes[(int)RedirectionStream.Warning];
                    context.ExpressionVerboseOutputPipe = pipes[(int)RedirectionStream.Verbose];
                    context.ExpressionDebugOutputPipe = pipes[(int)RedirectionStream.Debug];
                    context.ExpressionInformationOutputPipe = pipes[(int)RedirectionStream.Information];
                    break;
                case RedirectionStream.Output:
                    funcContext._outputPipe = pipes[(int)RedirectionStream.Output];
                    break;
                case RedirectionStream.Error:
                    context.ShellFunctionErrorOutputPipe = pipes[(int)FromStream];
                    break;
                case RedirectionStream.Warning:
                    context.ExpressionWarningOutputPipe = pipes[(int)FromStream];
                    break;
                case RedirectionStream.Verbose:
                    context.ExpressionVerboseOutputPipe = pipes[(int)FromStream];
                    break;
                case RedirectionStream.Debug:
                    context.ExpressionDebugOutputPipe = pipes[(int)FromStream];
                    break;
                case RedirectionStream.Information:
                    context.ExpressionInformationOutputPipe = pipes[(int)FromStream];
                    break;
            }
        }
    }

    internal class MergingRedirection : CommandRedirection
    {
        internal MergingRedirection(RedirectionStream from, RedirectionStream to)
            : base(from)
        {
            if (to != RedirectionStream.Output)
            {
                throw InterpreterError.NewInterpreterException(to, typeof(ArgumentException),
                                               null, "RedirectionStreamCanOnlyMergeToOutputStream",
                                               ParserStrings.RedirectionStreamCanOnlyMergeToOutputStream);
            }

            // this.ToStream = to;
        }

        public override string ToString()
        {
            return FromStream == RedirectionStream.All
                       ? "*>&1"
                       : string.Create(CultureInfo.InvariantCulture, $"{(int)FromStream}>&1");
        }

        // private RedirectionStream ToStream { get; set; }

        // Handle merging redirections for commands, like:
        //   dir 2>&1
        // A more realistic example:
        //   dir 2>&1 > out
        internal override void Bind(PipelineProcessor pipelineProcessor, CommandProcessorBase commandProcessor, ExecutionContext context)
        {
            Pipe pipe = commandProcessor.CommandRuntime.OutputPipe;

            switch (FromStream)
            {
                case RedirectionStream.All:
                    commandProcessor.CommandRuntime.ErrorMergeTo = MshCommandRuntime.MergeDataStream.Output;
                    commandProcessor.CommandRuntime.WarningOutputPipe = pipe;
                    commandProcessor.CommandRuntime.VerboseOutputPipe = pipe;
                    commandProcessor.CommandRuntime.DebugOutputPipe = pipe;
                    commandProcessor.CommandRuntime.InformationOutputPipe = pipe;
                    break;
                case RedirectionStream.Output:
                    break;
                case RedirectionStream.Error:
                    commandProcessor.CommandRuntime.ErrorMergeTo = MshCommandRuntime.MergeDataStream.Output;
                    break;
                case RedirectionStream.Warning:
                    commandProcessor.CommandRuntime.WarningOutputPipe = pipe;
                    break;
                case RedirectionStream.Verbose:
                    commandProcessor.CommandRuntime.VerboseOutputPipe = pipe;
                    break;
                case RedirectionStream.Debug:
                    commandProcessor.CommandRuntime.DebugOutputPipe = pipe;
                    break;
                case RedirectionStream.Information:
                    commandProcessor.CommandRuntime.InformationOutputPipe = pipe;
                    break;
            }
        }

        // Handle merging redirections for expressions, like:
        //   $(write-error) 2>&1
        // A more realistic example:
        //   $(write-error) 2>&1 > out
        internal Pipe[] BindForExpression(ExecutionContext context, FunctionContext funcContext)
        {
            Pipe[] oldPipes = new Pipe[(int)RedirectionStream.Information + 1];
            Pipe pipe = funcContext._outputPipe;

            // We set the redirection pipe directly in Context because there is no command processor
            // (which indirectly does the same thing as this code.)

            switch (FromStream)
            {
                case RedirectionStream.All:
                    oldPipes[(int)RedirectionStream.Output] = funcContext._outputPipe;
                    oldPipes[(int)RedirectionStream.Error] = context.ShellFunctionErrorOutputPipe;
                    context.ShellFunctionErrorOutputPipe = pipe;
                    oldPipes[(int)RedirectionStream.Warning] = context.ExpressionWarningOutputPipe;
                    context.ExpressionWarningOutputPipe = pipe;
                    oldPipes[(int)RedirectionStream.Verbose] = context.ExpressionVerboseOutputPipe;
                    context.ExpressionVerboseOutputPipe = pipe;
                    oldPipes[(int)RedirectionStream.Debug] = context.ExpressionDebugOutputPipe;
                    context.ExpressionDebugOutputPipe = pipe;
                    oldPipes[(int)RedirectionStream.Information] = context.ExpressionInformationOutputPipe;
                    context.ExpressionInformationOutputPipe = pipe;
                    break;
                case RedirectionStream.Output:
                    oldPipes[(int)RedirectionStream.Output] = funcContext._outputPipe;
                    break;
                case RedirectionStream.Error:
                    oldPipes[(int)FromStream] = context.ShellFunctionErrorOutputPipe;
                    context.ShellFunctionErrorOutputPipe = pipe;
                    break;
                case RedirectionStream.Warning:
                    oldPipes[(int)FromStream] = context.ExpressionWarningOutputPipe;
                    context.ExpressionWarningOutputPipe = pipe;
                    break;
                case RedirectionStream.Verbose:
                    oldPipes[(int)FromStream] = context.ExpressionVerboseOutputPipe;
                    context.ExpressionVerboseOutputPipe = pipe;
                    break;
                case RedirectionStream.Debug:
                    oldPipes[(int)FromStream] = context.ExpressionDebugOutputPipe;
                    context.ExpressionDebugOutputPipe = pipe;
                    break;
                case RedirectionStream.Information:
                    oldPipes[(int)FromStream] = context.ExpressionInformationOutputPipe;
                    context.ExpressionInformationOutputPipe = pipe;
                    break;
            }

            return oldPipes;
        }
    }

    internal class FileRedirection : CommandRedirection, IDisposable
    {
        internal FileRedirection(RedirectionStream from, bool appending, string file)
            : base(from)
        {
            this.File = file;
            this.Appending = appending;
        }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}> {1}",
                FromStream == RedirectionStream.All
                    ? "*"
                    : ((int)FromStream).ToString(CultureInfo.InvariantCulture),
                File);
        }

        internal string File { get; }

        internal bool Appending { get; }

        private PipelineProcessor PipelineProcessor { get; set; }

        // Handle binding file redirection for commands, like:
        //    dir > out
        internal override void Bind(PipelineProcessor pipelineProcessor, CommandProcessorBase commandProcessor, ExecutionContext context)
        {
            Pipe pipe = GetRedirectionPipe(context, pipelineProcessor);

            switch (FromStream)
            {
                case RedirectionStream.All:
                    // Since a temp output pipe is going to be used, we should pass along the error and warning variable list.
                    // Normally, context.CurrentCommandProcessor will not be null. But in legacy DRTs from ParserTest.cs,
                    // a scriptblock may be invoked through 'DoInvokeReturnAsIs' using .NET reflection. In that case,
                    // context.CurrentCommandProcessor will be null. We don't try passing along variable lists in such case.
                    context.CurrentCommandProcessor?.CommandRuntime.OutputPipe.SetVariableListForTemporaryPipe(pipe);

                    commandProcessor.CommandRuntime.OutputPipe = pipe;
                    commandProcessor.CommandRuntime.ErrorOutputPipe = pipe;
                    commandProcessor.CommandRuntime.WarningOutputPipe = pipe;
                    commandProcessor.CommandRuntime.VerboseOutputPipe = pipe;
                    commandProcessor.CommandRuntime.DebugOutputPipe = pipe;
                    commandProcessor.CommandRuntime.InformationOutputPipe = pipe;
                    break;
                case RedirectionStream.Output:
                    // Since a temp output pipe is going to be used, we should pass along the error and warning variable list.
                    context.CurrentCommandProcessor?.CommandRuntime.OutputPipe.SetVariableListForTemporaryPipe(pipe);

                    commandProcessor.CommandRuntime.OutputPipe = pipe;
                    break;
                case RedirectionStream.Error:
                    commandProcessor.CommandRuntime.ErrorOutputPipe = pipe;
                    break;
                case RedirectionStream.Warning:
                    commandProcessor.CommandRuntime.WarningOutputPipe = pipe;
                    break;
                case RedirectionStream.Verbose:
                    commandProcessor.CommandRuntime.VerboseOutputPipe = pipe;
                    break;
                case RedirectionStream.Debug:
                    commandProcessor.CommandRuntime.DebugOutputPipe = pipe;
                    break;
                case RedirectionStream.Information:
                    commandProcessor.CommandRuntime.InformationOutputPipe = pipe;
                    break;
            }
        }

        // Handle binding file redirections for expressions, like:
        //     $(write-error blah) 2> out
        internal Pipe[] BindForExpression(FunctionContext funcContext)
        {
            var context = funcContext._executionContext;
            // GetRedirectionPipe can throw if the filename specified can't be written to.  In that case,
            // oldPipes is null, and when unbinding, there is nothing to do.
            Pipe pipe = GetRedirectionPipe(context, null);
            var oldPipes = new Pipe[(int)RedirectionStream.Information + 1];

            switch (FromStream)
            {
                case RedirectionStream.All:
                    oldPipes[(int)RedirectionStream.Output] = funcContext._outputPipe;
                    oldPipes[(int)RedirectionStream.Error] = context.ShellFunctionErrorOutputPipe;
                    oldPipes[(int)RedirectionStream.Warning] = context.ExpressionWarningOutputPipe;
                    oldPipes[(int)RedirectionStream.Verbose] = context.ExpressionVerboseOutputPipe;
                    oldPipes[(int)RedirectionStream.Debug] = context.ExpressionDebugOutputPipe;
                    oldPipes[(int)RedirectionStream.Information] = context.ExpressionInformationOutputPipe;

                    // Since a temp output pipe is going to be used, we should pass along the error and warning variable list.
                    funcContext._outputPipe.SetVariableListForTemporaryPipe(pipe);
                    funcContext._outputPipe = pipe;
                    context.ShellFunctionErrorOutputPipe = pipe;
                    context.ExpressionWarningOutputPipe = pipe;
                    context.ExpressionVerboseOutputPipe = pipe;
                    context.ExpressionDebugOutputPipe = pipe;
                    context.ExpressionInformationOutputPipe = pipe;
                    break;
                case RedirectionStream.Output:
                    oldPipes[(int)RedirectionStream.Output] = funcContext._outputPipe;
                    // Since a temp output pipe is going to be used, we should pass along the error and warning variable list.
                    funcContext._outputPipe.SetVariableListForTemporaryPipe(pipe);
                    funcContext._outputPipe = pipe;
                    break;
                case RedirectionStream.Error:
                    oldPipes[(int)FromStream] = context.ShellFunctionErrorOutputPipe;
                    context.ShellFunctionErrorOutputPipe = pipe;
                    break;
                case RedirectionStream.Warning:
                    oldPipes[(int)FromStream] = context.ExpressionWarningOutputPipe;
                    context.ExpressionWarningOutputPipe = pipe;
                    break;
                case RedirectionStream.Verbose:
                    oldPipes[(int)FromStream] = context.ExpressionVerboseOutputPipe;
                    context.ExpressionVerboseOutputPipe = pipe;
                    break;
                case RedirectionStream.Debug:
                    oldPipes[(int)FromStream] = context.ExpressionDebugOutputPipe;
                    context.ExpressionDebugOutputPipe = pipe;
                    break;
                case RedirectionStream.Information:
                    oldPipes[(int)FromStream] = context.ExpressionInformationOutputPipe;
                    context.ExpressionInformationOutputPipe = pipe;
                    break;
            }

            return oldPipes;
        }

        internal Pipe GetRedirectionPipe(ExecutionContext context, PipelineProcessor parentPipelineProcessor)
        {
            if (string.IsNullOrWhiteSpace(File))
            {
                return new Pipe { NullPipe = true };
            }

            CommandProcessorBase commandProcessor = context.CreateCommand("out-file", false);
            Diagnostics.Assert(commandProcessor != null, "CreateCommand returned null");

            // Previously, we mandated Unicode encoding here
            // Now, We can take what ever has been set if PSDefaultParameterValues
            // Unicode is still the default, but now may be overridden

            var cpi = CommandParameterInternal.CreateParameterWithArgument(
                /*parameterAst*/null, "Filepath", "-Filepath:",
                /*argumentAst*/null, File,
                false);
            commandProcessor.AddParameter(cpi);

            if (this.Appending)
            {
                cpi = CommandParameterInternal.CreateParameterWithArgument(
                    /*parameterAst*/null, "Append", "-Append:",
                    /*argumentAst*/null, true,
                    false);
                commandProcessor.AddParameter(cpi);
            }

            PipelineProcessor = new PipelineProcessor();
            PipelineProcessor.Add(commandProcessor);

            try
            {
                PipelineProcessor.StartStepping(true);
            }
            catch (RuntimeException rte)
            {
                // If it's just wrapping an argument exception, build a new exception that
                // is more specific tp the redirection operation...
                if (rte.ErrorRecord.Exception is System.ArgumentException)
                {
                    throw InterpreterError.NewInterpreterExceptionWithInnerException(null,
                        typeof(RuntimeException), null, "RedirectionFailed", ParserStrings.RedirectionFailed,
                            rte.ErrorRecord.Exception, File, rte.ErrorRecord.Exception.Message);
                }

                throw;
            }

            // I think this is only necessary for calling Dispose on the commands in the redirection pipe.
            parentPipelineProcessor?.AddRedirectionPipe(PipelineProcessor);

            return new Pipe(context, PipelineProcessor);
        }

        /// <summary>
        /// After file redirection is done, we need to call 'DoComplete' on the pipeline processor,
        /// so that 'EndProcessing' of Out-File can be called to wrap up the file write operation.
        /// </summary>
        /// <remarks>
        /// 'StartStepping' is called after creating the pipeline processor.
        /// 'Step' is called when an object is added to the pipe created with the pipeline processor.
        /// </remarks>
        internal void CallDoCompleteForExpression()
        {
            // The pipe returned from 'GetRedirectionPipe' could be a NullPipe
            PipelineProcessor?.DoComplete();
        }

        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                PipelineProcessor?.Dispose();
            }

            _disposed = true;
        }
    }

    #endregion Redirections

    internal static class FunctionOps
    {
        internal static void DefineFunction(ExecutionContext context,
                                            FunctionDefinitionAst functionDefinitionAst,
                                            ScriptBlockExpressionWrapper scriptBlockExpressionWrapper)
        {
            try
            {
                ScriptBlock scriptBlock = scriptBlockExpressionWrapper.GetScriptBlock(
                    context, functionDefinitionAst.IsFilter);

                var expAttribute = scriptBlock.ExperimentalAttribute;
                if (expAttribute == null || expAttribute.ToShow)
                {
                    context.EngineSessionState.SetFunctionRaw(functionDefinitionAst.Name,
                        scriptBlock, context.EngineSessionState.CurrentScope.ScopeOrigin);
                }
            }
            catch (Exception exception)
            {
                if (exception is not RuntimeException rte)
                {
                    throw ExceptionHandlingOps.ConvertToRuntimeException(exception, functionDefinitionAst.Extent);
                }

                InterpreterError.UpdateExceptionErrorRecordPosition(rte, functionDefinitionAst.Extent);
                throw;
            }
        }
    }

    internal class ScriptBlockExpressionWrapper
    {
        private ScriptBlock _scriptBlock;
        private readonly IParameterMetadataProvider _ast;

        internal ScriptBlockExpressionWrapper(IParameterMetadataProvider ast)
        {
            _ast = ast;
        }

        internal ScriptBlock GetScriptBlock(ExecutionContext context, bool isFilter)
        {
            // We always clone the result, even when creating a new script block, so that the cached
            // value doesn't hold on to any session state.
            Diagnostics.Assert(_scriptBlock == null || _scriptBlock.SessionStateInternal == null,
                "Cached script block should not hold on to session state");

            var result = (_scriptBlock ??= new ScriptBlock(_ast, isFilter)).Clone();
            result.SessionStateInternal = context.EngineSessionState;
            return result;
        }
    }

    internal static class ByRefOps
    {
        /// <summary>
        /// There is no way to directly work with ByRef type in the expression tree, so we turn to reflection in this case.
        /// </summary>
        internal static object GetByRefPropertyValue(object target, PropertyInfo property)
        {
            return property.GetValue(target);
        }
    }

    internal static class HashtableOps
    {
        internal static void AddKeyValuePair(IDictionary hashtable, object key, object value, IScriptExtent errorExtent)
        {
            key = PSObject.Base(key);
            if (key == null)
            {
                throw InterpreterError.NewInterpreterException(hashtable, typeof(RuntimeException), errorExtent,
                                                               "InvalidNullKey", ParserStrings.InvalidNullKey);
            }

            if (hashtable.Contains(key))
            {
                // convert the key to a string for the error message, trimming if it's to long...
                // we pass a null context here because we're not too interested in $OFS.
                string errorKeyString = PSObject.ToStringParser(null, key);

                if (errorKeyString.Length > 40)
                {
                    errorKeyString = errorKeyString.Substring(0, 40) + PSObjectHelper.Ellipsis;
                }

                throw InterpreterError.NewInterpreterException(hashtable, typeof(RuntimeException), errorExtent,
                    "DuplicateKeyInHashLiteral", ParserStrings.DuplicateKeyInHashLiteral, errorKeyString);
            }

            hashtable.Add(key, value);
        }

        internal static object Add(IDictionary lvalDict, IDictionary rvalDict)
        {
            IDictionary newDictionary;
            if (lvalDict is OrderedDictionary)
            {
                // If the left is ordered, assume they want orderedness preserved.
                newDictionary = new OrderedDictionary(StringComparer.CurrentCultureIgnoreCase);
            }
            else
            {
                newDictionary = new Hashtable(StringComparer.CurrentCultureIgnoreCase);
            }

            // Add key and values from left hand side...
            foreach (object key in lvalDict.Keys)
            {
                newDictionary.Add(key, lvalDict[key]);
            }

            // and the right-hand side
            foreach (object key in rvalDict.Keys)
            {
                newDictionary.Add(key, rvalDict[key]);
            }

            return newDictionary;
        }
    }

    internal static class ExceptionHandlingOps
    {
        internal class CatchAll { }

        /// <summary>
        /// Represent a handler search result.
        /// </summary>
        private sealed class HandlerSearchResult
        {
            internal HandlerSearchResult()
            {
                Handler = -1;
                Rank = int.MaxValue;
                ExceptionToPass = null;
                ErrorRecordToPass = null;
            }

            internal int Handler;
            internal int Rank;
            internal Exception ExceptionToPass;
            internal ErrorRecord ErrorRecordToPass;
        }

        /// <summary>
        /// Rank the exception types based on how specific they are.
        /// Smaller ranking number indicates more specific exception type.
        /// </summary>
        /// <remarks>
        /// The ranking number for each type represent how many other
        /// types from the array derive from it.
        /// For example, 0 means no other types in the array derive from
        /// the corresponding type, while 3 means there are 3 other types
        /// in the array actually derive from the corresponding type.
        /// 'CatchAll' is considered to be derived by all exception types.
        /// </remarks>
        private static int[] RankExceptionTypes(Type[] types)
        {
            int[] ranks = new int[types.Length];
            int length = types.Length;

            // If 'CatchAll' is specified, it must be the last catch block.
            // Handle it specially. This can save a few iterations in the
            // 'for' loop below, and also avoid some type comparisons.
            if (types[length - 1].Equals(typeof(CatchAll)))
            {
                ranks[length - 1] = length - 1;
                length -= 1;
            }

            // For each type check if it's a sub-class of any types after it.
            // The ordering of the type array guarantees the more specific type comes first.
            for (int i = 0; i < length - 1; i++)
            {
                for (int j = i + 1; j < length; j++)
                {
                    if (types[i].IsSubclassOf(types[j]))
                        ranks[j]++;
                }
            }

            return ranks;
        }

        /// <summary>
        /// Search for handler by the exception type and process the found result.
        /// </summary>
        private static void FindAndProcessHandler(Type[] types, int[] ranks,
                                                  HandlerSearchResult current,
                                                  Exception exception,
                                                  ErrorRecord errorRecord)
        {
            Diagnostics.Assert(current != null, "Caller makes sure 'current' is not null.");
            int handler = FindMatchingHandlerByType(exception.GetType(), types);

            // If no handler was found, return without changing the current result.
            if (handler == -1)
            {
                return;
            }

            // New handler was found.
            //  - If new-rank is less than current-rank -- meaning the new handler is more specific,
            //    then we update the current result with it.
            //  - If new-rank is more than current-rank -- meaning the new handler is less specific,
            //    then we do NOT change the current result.
            //  - If new-rank is equal to current-rank, we do NOT change the current result UNLESS the
            //    current handler is catch-all. (This is to keep the original behavior -- prefer to use
            //    the later found exception as the exception-to-pass-in if all exceptions result in the
            //    catch-all handler.
            int rank = ranks[handler];
            if (rank < current.Rank ||
                (rank == current.Rank && types[current.Handler].Equals(typeof(CatchAll)))
               )
            {
                current.Handler = handler;
                current.Rank = rank;
                current.ExceptionToPass = exception;
                current.ErrorRecordToPass = errorRecord;
            }
        }

        /// <summary>
        /// Find the matching handler for the caught exception.
        /// </summary>
        internal static int FindMatchingHandler(MutableTuple tuple, RuntimeException rte, Type[] types, ExecutionContext context)
        {
            bool continueToSearch = false;
            int[] ranks = RankExceptionTypes(types);
            var current = new HandlerSearchResult();

            do
            {
                // Always assume no need to repeat the search for another iteration
                continueToSearch = false;
                // The 'ErrorRecord' of the current RuntimeException would be passed to $_
                ErrorRecord errorRecordToPass = rte.ErrorRecord;

                Exception inner = rte.InnerException;
                if (inner != null)
                {
                    FindAndProcessHandler(types, ranks, current, inner, errorRecordToPass);
                }

                // If no handler was found (rank = int.MaxValue), or if the handler we found was not
                // the most specific one, then look again, this time using the outer exception.
                // If we found a handler, but not one of the most specific ones (rank != 0), there may
                // be a more specific handler that catches outer but not inner exception.
                if (current.Rank > 0)
                {
                    FindAndProcessHandler(types, ranks, current, rte, errorRecordToPass);
                }

                // If we still didn't find one of the most specific handlers (rank != 0), we'll try unwrapping a few other of our exceptions:
                //     ActionPreferenceStopException - to cover '-ea stop'
                //         try { gci nosuchfile -ea stop } catch [System.Management.Automation.ItemNotFoundException] { 'caught' }
                //     CmdletInvocationException - to cover cmdlets like Invoke-Expression
                if (current.Rank > 0)
                {
                    var apse = rte as ActionPreferenceStopException;
                    if (apse != null)
                    {
                        var exceptionToPass = apse.ErrorRecord.Exception;

                        // If it's again a RuntimeException, we repeat the search using it
                        rte = exceptionToPass as RuntimeException;
                        if (rte != null)
                        {
                            continueToSearch = true;
                        }
                        else if (exceptionToPass != null)
                        {
                            FindAndProcessHandler(types, ranks, current, exceptionToPass, errorRecordToPass);
                        }
                    }
                    else if (rte is CmdletInvocationException && inner != null)
                    {
                        if (inner.InnerException != null)
                        {
                            FindAndProcessHandler(types, ranks, current, inner.InnerException, errorRecordToPass);
                        }
                    }
                }
            } while (continueToSearch);

            if (current.Handler != -1)
            {
                var errorRecord = new ErrorRecord(current.ErrorRecordToPass, current.ExceptionToPass);
                tuple.SetAutomaticVariable(AutomaticVariable.Underbar, errorRecord, context);
            }

            return current.Handler;
        }

        /// <summary>
        /// Find the matching handler by the exception type.
        /// </summary>
        private static int FindMatchingHandlerByType(Type exceptionType, Type[] types)
        {
            int i;

            // pass 1 - exact match (this pass isn't needed for catch handlers because the ordering
            // guarantees more specific handlers come first.)
            for (i = 0; i < types.Length; ++i)
            {
                if (exceptionType.Equals(types[i]))
                    return i;
            }

            // pass 2 - subclass
            for (i = 0; i < types.Length; ++i)
            {
                if (exceptionType.IsSubclassOf(types[i]))
                    return i;
            }

            // pass 3 - untyped catchall handler...
            //   if there is more than one (can only happen with traps), return the first.
            //   it might be nice to enforce a single default in strict mode.
            for (i = 0; i < types.Length; ++i)
            {
                if (types[i].Equals(typeof(CatchAll)))
                    return i;
            }

            return -1;
        }

        internal static bool SuspendStoppingPipeline(ExecutionContext context)
        {
            var localPipeline = (LocalPipeline)context.CurrentRunspace.GetCurrentlyRunningPipeline();
            return SuspendStoppingPipelineImpl(localPipeline);
        }

        internal static void RestoreStoppingPipeline(ExecutionContext context, bool oldIsStopping)
        {
            var localPipeline = (LocalPipeline)context.CurrentRunspace.GetCurrentlyRunningPipeline();
            RestoreStoppingPipelineImpl(localPipeline, oldIsStopping);
        }

        internal static bool SuspendStoppingPipelineImpl(LocalPipeline localPipeline)
        {
            if (localPipeline is not null)
            {
                bool oldIsStopping = localPipeline.Stopper.IsStopping;
                localPipeline.Stopper.IsStopping = false;
                return oldIsStopping;
            }

            return false;
        }

        internal static void RestoreStoppingPipelineImpl(LocalPipeline localPipeline, bool oldIsStopping)
        {
            if (localPipeline is not null)
            {
                localPipeline.Stopper.IsStopping = oldIsStopping;
            }
        }

        internal static void CheckActionPreference(FunctionContext funcContext, Exception exception)
        {
            if (exception is TargetInvocationException)
            {
                // Always unwrap TargetInvocationException.
                exception = exception.InnerException;
            }

            var rte = exception as RuntimeException;
            if (rte == null)
            {
                rte = ConvertToRuntimeException(exception, funcContext.CurrentPosition);
            }
            else
            {
                InterpreterError.UpdateExceptionErrorRecordPosition(rte, funcContext.CurrentPosition);
            }

            // Update the history id if needed to associate the exception with the right history item.
            InterpreterError.UpdateExceptionErrorRecordHistoryId(rte, funcContext._executionContext);

            var context = funcContext._executionContext;
            var outputPipe = funcContext._outputPipe;

            var extent = rte.ErrorRecord.InvocationInfo.ScriptPosition;
            SetErrorVariables(extent, rte, context, outputPipe);

            // set $? to false indicating an error
            context.QuestionMarkVariableValue = false;

            ActionPreference preference = GetErrorActionPreference(context);

            // If the exception was not rethrown and we are not currently
            // handling an exception, then the exception is new, and we
            // can break on it if requested.
            if (!rte.WasRethrown &&
                context.CurrentExceptionBeingHandled == null &&
                preference == ActionPreference.Break)
            {
                context.Debugger?.Break(rte);
            }

            // Item2 in the trap tuples is the action (script) for the trap.
            // A null action script is only used to indicate when exceptions
            // should be thrown up to a higher level, and doesn't count as an
            // actual trap handler in the function context.
            bool anyTrapHandlers = funcContext._traps.Count > 0 && funcContext._traps[funcContext._traps.Count - 1].Item2 != null;

            if (anyTrapHandlers)
            {
                // update the action preference according to how the exception is
                // handled in the trap statement(s).
                preference = ProcessTraps(funcContext, rte);
            }
            else if (ExceptionCannotBeStoppedContinuedOrIgnored(rte, context))
            {
                throw rte;
            }
            else if (preference == ActionPreference.Inquire && !rte.SuppressPromptInInterpreter)
            {
                preference = InquireForActionPreference(rte.Message, context);
            }

            if ((preference == ActionPreference.SilentlyContinue) ||
                (preference == ActionPreference.Ignore))
            {
                return;
            }

            if (preference == ActionPreference.Stop)
            {
                // The interpreter prompt CommandBaseStrings:InquireHalt
                // should be suppressed when this flag is set.  This will be set
                // when this prompt has already occurred and Break was chosen,
                // or for ActionPreferenceStopException in all cases.
                rte.SuppressPromptInInterpreter = true;

                throw rte;
            }

            if (!anyTrapHandlers && rte.WasThrownFromThrowStatement)
            {
                throw rte;
            }

            if (!ReportErrorRecord(extent, rte, context))
            {
                throw rte;
            }
        }

        private static ActionPreference ProcessTraps(FunctionContext funcContext,
                                                     RuntimeException rte)
        {
            int handler = -1;
            Exception exception = null;
            Exception inner = rte.InnerException;

            var types = funcContext._traps.Last().Item1;
            var handlers = funcContext._traps.Last().Item2;

            if (inner != null)
            {
                handler = FindMatchingHandlerByType(inner.GetType(), types);
                exception = inner;
            }

            // If no handler was found, or if the handler we found was the catch all handler,
            // then look again, this time using the outer exception.  If, when looking with the inner,
            // we found the catch all, there may be a handler that catches outer but not inner.
            if (handler == -1 || types[handler].Equals(typeof(CatchAll)))
            {
                int outerHandler = FindMatchingHandlerByType(rte.GetType(), types);
                if (outerHandler != handler)
                {
                    handler = outerHandler;
                    exception = rte;
                }
            }

            if (handler != -1)
            {
                Diagnostics.Assert(exception != null, "Exception object can't be null.");

                var context = funcContext._executionContext;

                try
                {
                    ErrorRecord err = rte.ErrorRecord;
                    // CurrentCommandProcessor is normally not null, but it is null
                    // when executing some unit tests through reflection.
                    context.CurrentCommandProcessor?.ForgetScriptException();

                    try
                    {
                        // Invoke the trap statement body, passing in the exception...
                        var locals = MutableTuple.MakeTuple(funcContext._traps.Last().Item3[handler], Compiler.DottedLocalsNameIndexMap);
                        // Copy automatic variables into the new scope (not necessarily required because dynamic scoping
                        // would find them in the parent scope, but internal code might avoid dynamic lookup so copy
                        // to be safe.

                        Diagnostics.Assert(AutomaticVariable.Underbar == 0, "Code below relies on this assertion being true.");
                        locals.SetAutomaticVariable(AutomaticVariable.Underbar, new ErrorRecord(err, exception), context);
                        for (int i = 1; i < (int)AutomaticVariable.NumberOfAutomaticVariables; ++i)
                        {
                            locals.SetValue(i, funcContext._localsTuple.GetValue(i));
                        }

                        var newScope = context.EngineSessionState.NewScope(false);
                        context.EngineSessionState.CurrentScope = newScope;
                        newScope.LocalsTuple = locals;

                        var trapFuncContext = new FunctionContext
                        {
                            _file = funcContext._file,
                            _scriptBlock = funcContext._scriptBlock,
                            _sequencePoints = funcContext._sequencePoints,
                            _debuggerHidden = funcContext._debuggerHidden,
                            _debuggerStepThrough = funcContext._debuggerStepThrough,
                            _executionContext = funcContext._executionContext,
                            _boundBreakpoints = funcContext._boundBreakpoints,
                            _outputPipe = funcContext._outputPipe,
                            _breakPoints = funcContext._breakPoints,
                            _localsTuple = locals,
                        };

                        handlers[handler](trapFuncContext);
                    }
                    catch (TargetInvocationException tie)
                    {
                        throw tie.InnerException;
                    }
                    finally
                    {
                        context.EngineSessionState.RemoveScope(context.EngineSessionState.CurrentScope);
                    }

                    return ExceptionHandlingOps.QueryForAction(rte, exception.Message, context);
                }
                catch (ContinueException)
                {
                    // Just continue on to the next statement.
                    return ActionPreference.SilentlyContinue;
                }
                catch (BreakException)
                {
                    // Terminate this block of statements.
                    return ActionPreference.Stop;
                }
                finally
                {
                    // The questionmark variable will always be false when we process a trap, so
                    // set it to false to ensure it didn't change as a result of anything done
                    // inside the trap
                    context.QuestionMarkVariableValue = false;
                }
            }

            return ActionPreference.Stop;
        }

        /// <summary>
        /// Gets the current error action preference value.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <returns>The preference the user selected.</returns>
        /// <remarks>
        /// Error action is decided by error action preference. If preference is inquire, we will
        /// prompt user for their preference.
        /// </remarks>
        internal static ActionPreference GetErrorActionPreference(ExecutionContext context)
        {
            return context.GetEnumPreference(
                SpecialVariables.ErrorActionPreferenceVarPath,
                ActionPreference.Continue,
                out _);
        }

        /// <summary>
        /// Determine if we should continue or not after an error or exception.
        /// </summary>
        /// <param name="rte">The RuntimeException which was reported.</param>
        /// <param name="message">The message to display.</param>
        /// <param name="context">The execution context.</param>
        /// <returns>The preference the user selected.</returns>
        /// <remarks>
        /// Error action is decided by error action preference. If preference is inquire, we will
        /// prompt user for their preference.
        /// </remarks>
        internal static ActionPreference QueryForAction(RuntimeException rte, string message, ExecutionContext context)
        {
            // 906264 "$ErrorActionPreference="Inquire" prevents original non-terminating error from being reported to $error"
            ActionPreference preference =
                context.GetEnumPreference(
                    SpecialVariables.ErrorActionPreferenceVarPath,
                    ActionPreference.Continue,
                    out _);

            if (preference != ActionPreference.Inquire || rte.SuppressPromptInInterpreter)
                return preference;

            return InquireForActionPreference(message, context);
        }

        /// <summary>
        /// This is a helper function for prompting for user preference.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context">The execution context.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method will allow user to enter suspend mode.
        /// </remarks>
        internal static ActionPreference InquireForActionPreference(string message, ExecutionContext context)
        {
            InternalHostUserInterface ui = (InternalHostUserInterface)context.EngineHostInterface.UI;

            Collection<ChoiceDescription> choices = new Collection<ChoiceDescription>();

            string continueLabel = ParserStrings.ContinueLabel;
            string continueHelpMsg = ParserStrings.ContinueHelpMessage;
            string silentlyContinueLabel = ParserStrings.SilentlyContinueLabel;
            string silentlyContinueHelpMsg = ParserStrings.SilentlyContinueHelpMessage;
            string breakLabel = ParserStrings.BreakLabel;
            string breakHelpMsg = ParserStrings.BreakHelpMessage;
            string suspendLabel = ParserStrings.SuspendLabel;
            string suspendHelpMsg = StringUtil.Format(ParserStrings.SuspendHelpMessage);

            choices.Add(new ChoiceDescription(continueLabel, continueHelpMsg));
            choices.Add(new ChoiceDescription(silentlyContinueLabel, silentlyContinueHelpMsg));
            choices.Add(new ChoiceDescription(breakLabel, breakHelpMsg));
            choices.Add(new ChoiceDescription(suspendLabel, suspendHelpMsg));

            string caption = ParserStrings.ExceptionActionPromptCaption;

            bool oldQuestionMarkVariableValue = context.QuestionMarkVariableValue;

            int choice;
            while ((choice = ui.PromptForChoice(caption, message, choices, 0)) == 3)
            {
                context.EngineHostInterface.EnterNestedPrompt();
            }

            context.QuestionMarkVariableValue = oldQuestionMarkVariableValue;

            if (choice == 0)
                return ActionPreference.Continue;

            if (choice == 1)
                return ActionPreference.SilentlyContinue;

            return ActionPreference.Stop;
        }

        /// <summary>
        /// Set error variables like $error and $stacktrace.
        /// </summary>
        /// <param name="extent"></param>
        /// <param name="rte"></param>
        /// <param name="context">The execution context.</param>
        /// <param name="outputPipe">The output pipe of the statement.</param>
        internal static void SetErrorVariables(IScriptExtent extent, RuntimeException rte, ExecutionContext context, Pipe outputPipe)
        {
            string stack = null;
            Exception e = rte;

            int i = 0;
            while (e != null && i++ < 10)
            {
                if (!string.IsNullOrEmpty(e.StackTrace))
                {
                    stack = e.StackTrace;
                }

                e = e.InnerException;
            }

            context.SetVariable(SpecialVariables.StackTraceVarPath, stack);

            Diagnostics.Assert(rte.ErrorRecord != null, "The runtime exception's error record was null");
            InterpreterError.UpdateExceptionErrorRecordPosition(rte, extent);
            ErrorRecord errRec = rte.ErrorRecord.WrapException(rte);

            if (rte is not PipelineStoppedException)
            {
                outputPipe?.AppendVariableList(VariableStreamKind.Error, errRec);

                context.AppendDollarError(errRec);
            }
        }

        internal static bool ExceptionCannotBeStoppedContinuedOrIgnored(RuntimeException rte, ExecutionContext context)
        {
            return context.PropagateExceptionsToEnclosingStatementBlock
                   || context.ShellFunctionErrorOutputPipe == null
                   || context.CurrentPipelineStopping
                   || rte.SuppressPromptInInterpreter
                   || rte is PipelineStoppedException;
        }

        /// <summary>
        /// Report error into error pipe.
        /// </summary>
        /// <param name="extent"></param>
        /// <param name="rte">The runtime error to report.</param>
        /// <param name="context">The execution context.</param>
        /// <returns>True if it was able to report the error.</returns>
        internal static bool ReportErrorRecord(IScriptExtent extent, RuntimeException rte, ExecutionContext context)
        {
            if (context.ShellFunctionErrorOutputPipe == null)
                return false;

            Diagnostics.Assert(rte.ErrorRecord != null, "The runtime exception's error record was null");

            if (rte.ErrorRecord.InvocationInfo == null && extent != null && extent != PositionUtilities.EmptyExtent)
                rte.ErrorRecord.SetInvocationInfo(new InvocationInfo(null, extent, context));
            PSObject errorWrap = PSObject.AsPSObject(new ErrorRecord(rte.ErrorRecord, rte));

            errorWrap.WriteStream = WriteStreamType.Error;

            // If this is an error pipe for a hosting application (i.e.: no downstream cmdlet),
            // and we are logging, then create a temporary PowerShell to log the error.
            if (context.InternalHost.UI.IsTranscribing)
            {
                context.InternalHost.UI.TranscribeError(context, rte.ErrorRecord.InvocationInfo, errorWrap);
            }

            context.ShellFunctionErrorOutputPipe.Add(errorWrap);

            // set the value of $? here in case it is reset in error reporting.
            context.QuestionMarkVariableValue = false;

            return true;
        }

        internal static RuntimeException ConvertToException(object result, IScriptExtent extent, bool rethrow)
        {
            result = PSObject.Base(result);

            RuntimeException runtimeException = result as RuntimeException;
            if (runtimeException != null)
            {
                InterpreterError.UpdateExceptionErrorRecordPosition(runtimeException, extent);
                runtimeException.WasThrownFromThrowStatement = true;
                runtimeException.WasRethrown = rethrow;
                return runtimeException;
            }

            ErrorRecord er = result as ErrorRecord;
            if (er != null)
            {
                runtimeException = new RuntimeException(er.ToString(), er.Exception, er) { WasThrownFromThrowStatement = true, WasRethrown = rethrow };
                InterpreterError.UpdateExceptionErrorRecordPosition(runtimeException, extent);

                return runtimeException;
            }

            Exception exception = result as Exception;
            if (exception != null)
            {
                er = new ErrorRecord(exception, exception.Message, ErrorCategory.OperationStopped, null);
                runtimeException = new RuntimeException(exception.Message, exception, er) { WasThrownFromThrowStatement = true, WasRethrown = rethrow };
                InterpreterError.UpdateExceptionErrorRecordPosition(runtimeException, extent);
                return runtimeException;
            }

            string message = LanguagePrimitives.IsNull(result)
                ? "ScriptHalted"
                : ParserOps.ConvertTo<string>(result, PositionUtilities.EmptyExtent);
            exception = new RuntimeException(message, null);

            er = new ErrorRecord(exception, message, ErrorCategory.OperationStopped, null);
            runtimeException = new RuntimeException(message, exception, er) { WasThrownFromThrowStatement = true, WasRethrown = rethrow };
            runtimeException.SetTargetObject(result);
            InterpreterError.UpdateExceptionErrorRecordPosition(runtimeException, extent);

            return runtimeException;
        }

        internal static RuntimeException ConvertToRuntimeException(Exception exception, IScriptExtent extent)
        {
            RuntimeException runtimeException = exception as RuntimeException;
            if (runtimeException == null)
            {
                var icer = exception as IContainsErrorRecord;
                var er = icer != null
                             ? icer.ErrorRecord
                             : new ErrorRecord(exception, exception.GetType().FullName, ErrorCategory.OperationStopped, null);
                runtimeException = new RuntimeException(exception.Message, exception, er);
            }

            InterpreterError.UpdateExceptionErrorRecordPosition(runtimeException, extent);
            return runtimeException;
        }

        internal static void ConvertToArgumentConversionException(Exception exception, string parameterName, object argument, string method, Type toType)
        {
            throw new MethodException(
                "MethodArgumentConversionInvalidCastArgument", exception,
                ExtendedTypeSystem.MethodArgumentConversionException, parameterName, argument, method, toType, exception.Message);
        }

        internal static void ConvertToMethodInvocationException(Exception exception, Type typeToThrow, string methodName, int numArgs, MemberInfo memberInfo = null)
        {
            if (exception is TargetInvocationException)
            {
                exception = exception.InnerException;
            }

            // Win8: 178063. Allow flow control related exceptions for PowerShell hosting API
            if ((exception is FlowControlException ||
                exception is ScriptCallDepthException ||
                exception is PipelineStoppedException) &&
                ((memberInfo == null) || ((memberInfo.DeclaringType != typeof(PowerShell)) && (memberInfo.DeclaringType != typeof(Pipeline)))))
            {
                return;
            }

            if (typeToThrow == typeof(MethodException))
            {
                if (exception is MethodException)
                    return;

                throw new MethodInvocationException(
                        exception.GetType().Name,
                        exception,
                        ExtendedTypeSystem.MethodInvocationException,
                        methodName, numArgs, exception.Message);
            }

            if (methodName.StartsWith("set_", StringComparison.Ordinal) || methodName.StartsWith("get_", StringComparison.Ordinal))
            {
                methodName = methodName.Substring(4);
            }

            if (typeToThrow == typeof(GetValueInvocationException))
            {
                if (exception is GetValueException)
                    return;

                throw new GetValueInvocationException(
                    "ExceptionWhenGetting",
                    exception,
                    ExtendedTypeSystem.ExceptionWhenGetting,
                    methodName, exception.Message);
            }

            Diagnostics.Assert(typeToThrow == typeof(SetValueInvocationException),
                               "caller to verify exception is expected type");
            if (exception is SetValueException)
                return;

            throw new SetValueInvocationException(
                "ExceptionWhenSetting",
                exception,
                ExtendedTypeSystem.ExceptionWhenSetting,
                methodName, exception.Message);
        }
    }

    internal static class TypeOps
    {
        internal static Type ResolveTypeName(ITypeName typeName, IScriptExtent errorPos)
        {
            Exception exception;
            var result = TypeResolver.ResolveITypeName(typeName, out exception);

            if (result == null)
            {
                if (exception != null)
                {
                    if (exception is InvalidCastException &&
                        exception.InnerException != null &&
                        exception.InnerException is TypeResolver.AmbiguousTypeException)
                    {
                        throw exception;
                    }

                    throw InterpreterError.NewInterpreterException(typeName, typeof(RuntimeException), errorPos,
                                                                   "TypeNotFoundWithMessage",
                                                                   ParserStrings.TypeNotFoundWithMessage,
                                                                   typeName.FullName, exception.Message);
                }

                // For better error messages, figure out exactly which type we couldn't resolve.
                // We recurse and relying on one of the recursive calls to throw, or if none do,
                // then we just throw on the top level typeName.

                var genericTypeName = typeName as GenericTypeName;
                if (genericTypeName != null)
                {
                    var generic = genericTypeName.GetGenericType(ResolveTypeName(genericTypeName.TypeName, errorPos));
                    var typeArgs = (from arg in genericTypeName.GenericArguments select ResolveTypeName(arg, errorPos)).ToArray();

                    try
                    {
                        if (generic != null && generic.ContainsGenericParameters)
                            generic.MakeGenericType(typeArgs);
                    }
                    catch (Exception e)
                    {
                        throw InterpreterError.NewInterpreterException(typeName, typeof(RuntimeException), errorPos,
                                                                       "TypeNotFoundWithMessage",
                                                                       ParserStrings.TypeNotFoundWithMessage,
                                                                       typeName.FullName, e.Message);
                    }
                }

                var arrayTypeName = typeName as ArrayTypeName;

                if (arrayTypeName != null)
                {
                    ResolveTypeName(arrayTypeName.ElementType, errorPos);
                }

                throw InterpreterError.NewInterpreterException(typeName, typeof(RuntimeException), errorPos,
                                                               "TypeNotFound", ParserStrings.TypeNotFound,
                                                               typeName.FullName);
            }

            return result;
        }

        internal static bool IsInstance(object left, object right)
        {
            object lval = PSObject.Base(left);
            object rval = PSObject.Base(right);

            Type rType = rval as Type;

            if (rType == null)
            {
                rType = ParserOps.ConvertTo<Type>(rval, null);

                if (rType == null)
                {
                    // "the right operand of '-is' must be a type"
                    throw InterpreterError.NewInterpreterException(rval, typeof(RuntimeException),
                        null, "IsOperatorRequiresType", ParserStrings.IsOperatorRequiresType);
                }
            }

            if (rType == typeof(PSCustomObject) && lval is PSObject)
            {
                Diagnostics.Assert(rType.IsInstanceOfType(((PSObject)lval).ImmediateBaseObject), "Unexpect PSObject");
                return true;
            }

            if (rType.Equals(typeof(PSObject)) && left is PSObject)
            {
                return true;
            }

            return rType.IsInstanceOfType(lval);
        }

        internal static object AsOperator(object left, Type type)
        {
            if (type == null)
            {
                throw InterpreterError.NewInterpreterException(null, typeof(RuntimeException), null,
                                                               "AsOperatorRequiresType", ParserStrings.AsOperatorRequiresType);
            }

            // We figure out the exception instead of just executing a conversion because we can avoid an exception which is quite expensive,
            // and people using -as don't expect it to be expensive.
            bool debase;

            // ConstrainedLanguage note - Calls to this conversion are done at runtime, so conversions are not cached.
            var conversion = LanguagePrimitives.FigureConversion(left, type, out debase);
            if (conversion.Rank == ConversionRank.None)
            {
                return null;
            }

            try
            {
                if (debase)
                {
                    return conversion.Invoke(PSObject.Base(left), type, false, (PSObject)left,
                        NumberFormatInfo.InvariantInfo, null);
                }

                return conversion.Invoke(left, type, false, null, NumberFormatInfo.InvariantInfo, null);
            }
            catch (PSInvalidCastException)
            {
                return null;
            }
        }

        internal static string[] GetNamespacesForTypeResolutionState(IEnumerable<UsingStatementAst> usingAsts)
        {
            var usedSystem = false;
            var namespaces = new List<string>();

            foreach (var usingStmt in usingAsts)
            {
                if (usingStmt.UsingStatementKind == UsingStatementKind.Namespace)
                {
                    if (!usedSystem && usingStmt.Name.Value.Equals("System", StringComparison.OrdinalIgnoreCase))
                    {
                        usedSystem = true;
                    }

                    namespaces.Add(usingStmt.Name.Value);
                }
            }

            if (!usedSystem)
            {
                namespaces.Insert(0, "System");
            }

            return namespaces.ToArray();
        }

        /// <summary>
        /// Add types to the current scope.
        /// This method called at runtime after types are created at compile time.
        /// This method should be called for every ScriptBlockAst that defines types.
        ///
        /// I.e.
        ///
        /// class C1 {}
        /// function foo { class C2 {} }
        /// 1..10 | ForEach-Object { foo }
        ///
        /// DefinePowerShellTypes() would be called for two TypeDefinitionAsts at the same time and Types for C1 and C2 would be created at the same assembly.
        /// AddPowerShellTypesToTheScope() would be called for root script first and then for foo\C2, once we call function foo.
        /// Note that AddPowerShellTypesToTheScope() would be call on every foo call, 10 times.
        ///
        /// This method also should be called for 'using module' statements. Then added types would have a different name.
        /// </summary>
        /// <param name="types"></param>
        /// <param name="context"></param>
        internal static void AddPowerShellTypesToTheScope(Dictionary<string, TypeDefinitionAst> types, ExecutionContext context)
        {
            var trs = context.EngineSessionState.CurrentScope.TypeResolutionState;

            foreach (var t in types)
            {
                Diagnostics.Assert(t.Value.Type != null, "TypeDefinitionAst.Type cannot be null");
                context.EngineSessionState.CurrentScope.AddType(t.Key, t.Value.Type);
            }

            context.EngineSessionState.CurrentScope.TypeResolutionState = trs.CloneWithAddTypesDefined(types.Keys);
        }

        /// <summary>
        /// Capture session state for methods defined in PowerShell types, so they know what context to use.
        /// </summary>
        /// <param name="types"></param>
        internal static void InitPowerShellTypesAtRuntime(TypeDefinitionAst[] types)
        {
            foreach (var t in types)
            {
                Diagnostics.Assert(t.Type != null, "TypeDefinitionAst.Type cannot be null");
                if (t.IsClass)
                {
                    if (t.Type.IsDefined(typeof(NoRunspaceAffinityAttribute), inherit: true))
                    {
                        // Skip the initialization for session state affinity.
                        continue;
                    }

                    var helperType = t.Type.Assembly.GetType(t.Type.FullName + "_<staticHelpers>");
                    Diagnostics.Assert(helperType != null, "no corresponding " + t.Type.FullName + "_<staticHelpers> type found");
                    foreach (var p in helperType.GetFields(BindingFlags.Static | BindingFlags.NonPublic))
                    {
                        var field = p.GetValue(null);
                        // field can be one of two types: SessionStateKeeper or ScriptBlockMemberMethodWrapper
                        var methodWrapper = field as ScriptBlockMemberMethodWrapper;
                        if (methodWrapper != null)
                        {
                            methodWrapper.InitAtRuntime();
                        }
                        else
                        {
                            ((SessionStateKeeper)field).RegisterRunspace();
                        }
                    }
                }
            }
        }

        internal static void SetCurrentTypeResolutionState(TypeResolutionState trs, ExecutionContext context)
        {
            context.EngineSessionState.CurrentScope.TypeResolutionState = trs;
        }

        internal static void SetAssemblyDefiningPSTypes(FunctionContext functionContext, Assembly assembly)
        {
            functionContext._scriptBlock.AssemblyDefiningPSTypes = assembly;
        }
    }

    internal static class SwitchOps
    {
        internal static bool ConditionSatisfiedWildcard(bool caseSensitive,
                                                        object condition,
                                                        string str,
                                                        ExecutionContext context)
        {
            WildcardPattern wildcard = condition as WildcardPattern;
            if (wildcard != null)
            {
                // If case sensitivity doesn't agree between the existing wildcard pattern and the switch mode,
                // make a new wildcard pattern that agrees with the switch.
                if (((wildcard.Options & WildcardOptions.IgnoreCase) == 0) != caseSensitive)
                {
                    WildcardOptions options = caseSensitive ? WildcardOptions.None : WildcardOptions.IgnoreCase;
                    wildcard = WildcardPattern.Get(wildcard.Pattern, options);
                }
            }
            else
            {
                WildcardOptions options = caseSensitive ? WildcardOptions.None : WildcardOptions.IgnoreCase;
                wildcard = WildcardPattern.Get(PSObject.ToStringParser(context, condition), options);
            }

            return wildcard.IsMatch(str);
        }

        internal static bool ConditionSatisfiedRegex(bool caseSensitive,
                                                     object condition,
                                                     IScriptExtent errorPosition,
                                                     string str,
                                                     ExecutionContext context)
        {
            string pattern;

            RegexOptions options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;

            try
            {
                Match m;

                Regex regex = condition as Regex;
                // Check if the regex agrees with the switch w.r.t. case sensitivity, if not,
                // we must build a new regex.
                if (regex != null && (((regex.Options & RegexOptions.IgnoreCase) != 0) != caseSensitive))
                {
                    m = regex.Match(str);
                }
                else
                {
                    pattern = PSObject.ToStringParser(context, condition);
                    m = Regex.Match(str, pattern, options);

                    if (m.Success && m.Groups.Count > 0)
                    {
                        // We used the static regex method for it's caching ability, but
                        // we need the group names now.  Fortunately constructing another regex
                        // isn't slow because it should be in the cache still.
                        regex = new Regex(pattern, options);
                    }
                }

                if (m.Success)
                {
                    GroupCollection groups = m.Groups;

                    if (groups.Count > 0)
                    {
                        Diagnostics.Assert(regex != null, "Logic above ensures regex is not null.");

                        Hashtable h = new Hashtable(StringComparer.CurrentCultureIgnoreCase);

                        foreach (string groupName in regex.GetGroupNames())
                        {
                            Group g = groups[groupName];
                            if (g.Success)
                            {
                                int keyInt;

                                if (Int32.TryParse(groupName, out keyInt))
                                    h.Add(keyInt, g.ToString());
                                else
                                    h.Add(groupName, g.ToString());
                            }
                        }

                        context.SetVariable(SpecialVariables.MatchesVarPath, h);
                    }
                }

                return m.Success;
            }
            catch (ArgumentException ae)
            {
                // ErrorSkipping: Add this error to parser
                pattern = PSObject.ToStringParser(context, condition);
                throw InterpreterError.NewInterpreterExceptionWithInnerException(pattern, typeof(RuntimeException),
                    errorPosition, "InvalidRegularExpression", ParserStrings.InvalidRegularExpression, ae, pattern);
            }
        }

        internal static string ResolveFilePath(IScriptExtent errorExtent, object obj, ExecutionContext context)
        {
            try
            {
                FileInfo file = obj as FileInfo;
                string filePath = file != null ? file.FullName : PSObject.ToStringParser(context, obj);

                if (string.IsNullOrEmpty(filePath))
                {
                    throw InterpreterError.NewInterpreterException(filePath,
                        typeof(RuntimeException), errorExtent, "InvalidFilenameOption", ParserStrings.InvalidFilenameOption);
                }

                ProviderInfo provider;
                SessionState sessionState = new SessionState(context.EngineSessionState);

                Collection<string> filePaths =
                    sessionState.Path.GetResolvedProviderPathFromPSPath(filePath, out provider);

                // Make sure that the path is in the file system - that's all we can handle currently...
                if (!provider.NameEquals(context.ProviderNames.FileSystem))
                {
                    // "The current provider ({0}) cannot open a file"
                    throw InterpreterError.NewInterpreterException(filePath, typeof(RuntimeException), errorExtent,
                                                                   "FileOpenError", ParserStrings.FileOpenError,
                                                                   provider.FullName);
                }

                // Make sure at least one file was found...
                if (filePaths == null || filePaths.Count < 1)
                {
                    // "No files matching '{0}' were found.."
                    throw InterpreterError.NewInterpreterException(filePath, typeof(RuntimeException), errorExtent,
                                                                   "FileNotFound", ParserStrings.FileNotFound, filePath);
                }

                if (filePaths.Count > 1)
                {
                    // "The path resolved to more than one file; can only process one file at a time."
                    throw InterpreterError.NewInterpreterException(filePaths, typeof(RuntimeException), errorExtent,
                                                                   "AmbiguousPath", ParserStrings.AmbiguousPath);
                }

                return filePaths[0];
            }
            catch (RuntimeException rte)
            {
                // Add the invocation info to this command...
                if (rte.ErrorRecord != null && rte.ErrorRecord.InvocationInfo == null)
                    rte.ErrorRecord.SetInvocationInfo(new InvocationInfo(null, errorExtent, context));
                throw;
            }
        }
    }

    /// <summary>
    /// Controls the matching behaviour of the Where() operator.
    /// </summary>
    public enum WhereOperatorSelectionMode
    {
        /// <summary>
        /// Return all matches.
        /// </summary>
        Default = 0,
        /// <summary>
        /// Stop processing after the first match.
        /// </summary>
        First = 1,
        /// <summary>
        /// Return the last matching element.
        /// </summary>
        Last = 2,       // return last match
        /// <summary>
        /// Skip until the condition is true, then return the rest.
        /// </summary>
        SkipUntil = 3,
        /// <summary>
        /// Return elements until the condition is true then skip the rest.
        /// </summary>
        Until = 4,
        /// <summary>
        /// Return an array of two elements, first index is matched elements, second index is the remaining elements.
        /// </summary>
        Split = 5,
    }

    internal static class EnumerableOps
    {
        /// <summary>
        /// Implements the Where(expression) operation on collections.
        /// </summary>
        /// <param name="enumerator">The enumerator over the collection to search.</param>
        /// <param name="expressionSB">
        /// A ScriptBlock where its result is treated as a boolean, or null to
        /// return all collection objects with WhereOperatorSelectionMode.
        /// </param>
        /// <param name="selectionMode">
        /// Sets the WhereOperatorSelectionMode for operator, defaults to All.
        /// This is of type object to allow either enum values or strings to be passed.
        /// </param>
        /// <param name="numberToReturn">The number of elements to return.</param>
        /// <returns></returns>
        internal static object Where(IEnumerator enumerator, ScriptBlock expressionSB, WhereOperatorSelectionMode selectionMode, int numberToReturn)
        {
            Diagnostics.Assert(enumerator != null, "The Where() operator should never receive a null enumerator value from the runtime.");

            if (numberToReturn < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numberToReturn), numberToReturn, ParserStrings.NumberToReturnMustBeGreaterThanZero);
            }

            var context = Runspace.DefaultRunspace.ExecutionContext;

            // Optimization to speed up the case where there is no condition expression
            // Useful when using selection mode and number to return to do fast list
            // slicing.
            if (expressionSB == null)
            {
                if (selectionMode == WhereOperatorSelectionMode.Default)
                {
                    throw new InvalidOperationException(ParserStrings.EmptyExpressionRequiresANonDefaultMode);
                }

                var rest = new List<object>();
                object current = null;

                int index = 0;
                if (numberToReturn == 0)
                {
                    numberToReturn = 1;
                }

                // Skip the first N elements and return the rest
                if (selectionMode == WhereOperatorSelectionMode.SkipUntil)
                {
                    while (index < numberToReturn && MoveNext(null, enumerator))
                    {
                        index++;
                    }
                    while (MoveNext(context, enumerator))
                    {
                        rest.Add(Current(enumerator));
                    }

                    return rest.ToArray();
                }

                // Return the last N elements
                if (selectionMode == WhereOperatorSelectionMode.Last)
                {
                    while (MoveNext(context, enumerator))
                    {
                        current = Current(enumerator);
                        if (numberToReturn > 1)
                        {
                            rest.Add(current);
                            if (rest.Count > numberToReturn)
                            {
                                rest.RemoveAt(0);
                            }
                        }
                    }

                    if (numberToReturn == 1)
                    {
                        return new object[] { current };
                    }

                    return rest.ToArray();
                }

                object[] first = new object[numberToReturn];
                while (MoveNext(context, enumerator))
                {
                    current = Current(enumerator);
                    first[index++] = current;
                    if (index >= numberToReturn)
                    {
                        // Return the first N elements
                        if (selectionMode == WhereOperatorSelectionMode.First || selectionMode == WhereOperatorSelectionMode.Until)
                        {
                            return first;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                // Return a array of two elements, the first element is the first N elements,
                // the second element is the remainder of the input
                if (selectionMode == WhereOperatorSelectionMode.Split)
                {
                    while (MoveNext(context, enumerator))
                    {
                        var e = Current(enumerator);
                        rest.Add(e);
                    }

                    return new object[] { first, rest.ToArray() };
                }

                return first;
            }

            Collection<PSObject> matches = new Collection<PSObject>();
            Collection<PSObject> notMatched = null;
            if (selectionMode == WhereOperatorSelectionMode.Split)
            {
                notMatched = new Collection<PSObject>();
            }

            var resultCollection = new List<object>();
            Pipe outputPipe = new Pipe(resultCollection);
            bool returnTheRest = false;

            while (MoveNext(context, enumerator))
            {
                var ie = Current(enumerator);

                if (returnTheRest)
                {
                    matches.Add(ie == null ? null : PSObject.AsPSObject(ie));
                    if (numberToReturn > 0 && matches.Count >= numberToReturn)
                    {
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }

                resultCollection.Clear();
                expressionSB.InvokeWithPipeImpl(false, null, null, ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe, ie, AutomationNull.Value, AutomationNull.Value, outputPipe, null);
                bool elementMatched = LanguagePrimitives.IsTrue(resultCollection);

                if (elementMatched)
                {
                    if (selectionMode == WhereOperatorSelectionMode.Until)
                    {
                        break;
                    }
                    else if (selectionMode == WhereOperatorSelectionMode.Last)
                    {
                        if (numberToReturn == 0)
                        {
                            numberToReturn = 1;
                        }

                        if (matches.Count < numberToReturn)
                        {
                            matches.Add(ie == null ? null : PSObject.AsPSObject(ie));
                        }
                        else
                        {
                            if (numberToReturn == 1)
                            {
                                matches[0] = ie == null ? null : PSObject.AsPSObject(ie);
                            }
                            else
                            {
                                // Maintains a sliding window
                                matches.RemoveAt(0);
                                matches.Add(ie == null ? null : PSObject.AsPSObject(ie));
                            }
                        }
                    }
                    else if (selectionMode == WhereOperatorSelectionMode.SkipUntil)
                    {
                        matches.Add(ie == null ? null : PSObject.AsPSObject(ie));
                        returnTheRest = true;
                    }
                    else
                    {
                        matches.Add(ie == null ? null : PSObject.AsPSObject(ie));
                    }

                    if (selectionMode != WhereOperatorSelectionMode.Last)
                    {
                        if (numberToReturn == 0 && selectionMode == WhereOperatorSelectionMode.First)
                        {
                            break;
                        }

                        // If number to return is not 0, First and Any have identical behaviour
                        if (numberToReturn != 0 && numberToReturn == matches.Count)
                        {
                            break;
                        }
                    }
                }
                else if (selectionMode == WhereOperatorSelectionMode.Until)
                {
                    // no match so in the until case, we add the value until the count is reached
                    matches.Add(ie == null ? null : PSObject.AsPSObject(ie));
                    if (numberToReturn > 0 && matches.Count >= numberToReturn)
                    {
                        break;
                    }
                }
                else if (selectionMode == WhereOperatorSelectionMode.Split)
                {
                    // If in split mode, record both matched and noteMatched elements.
                    notMatched.Add(ie == null ? null : PSObject.AsPSObject(ie));
                }
            }

            // If split was specified, return both sets of objects
            if (selectionMode == WhereOperatorSelectionMode.Split)
            {
                // We may have stopped looping before processing the whole collection because
                // reached the max number of matching elements to return. In that case,
                // add remaining elements to the notMatched collection.
                while (MoveNext(context, enumerator))
                {
                    var ie = Current(enumerator);
                    notMatched.Add(ie == null ? null : PSObject.AsPSObject(ie));
                }

                return new object[] { matches, notMatched };
            }

            return matches;
        }

        /// <summary>
        /// Implements the ForEach() operator.
        /// </summary>
        /// <param name="enumerator">The collection to operate over.</param>
        /// <param name="expression"></param>
        /// <param name="arguments">
        /// </param>
        /// <returns>An object array containing the results of the expression evaluation.</returns>
        internal static object ForEach(IEnumerator enumerator, object expression, object[] arguments)
        {
            Diagnostics.Assert(enumerator != null, "The ForEach() operator should never receive a null enumerator value from the runtime.");
            Diagnostics.Assert(arguments != null, "The ForEach() operator should never receive a null value for the 'arguments' parameter from the runtime.");

            ArgumentNullException.ThrowIfNull(expression);

            var context = Runspace.DefaultRunspace.ExecutionContext;

            // If expression argument is a .Net type then convert the collection to that type
            // if the target type is a collection or array, then the result will be a collection of exactly
            // that type. If the target type is not a collection type then return a generic collection of that type.
            Type targetType = expression as Type;
            if (targetType != null)
            {
                dynamic resultCollection = null;

                if (targetType.GetInterface("System.Collections.ICollection") != null)
                {
                    // If the target type is an array, accumulate all the elements
                    // then use the PowerShell type converter to turn it into an array
                    // of the correct type.
                    if (targetType.IsArray)
                    {
                        var list = new List<object>();
                        while (MoveNext(null, enumerator))
                        {
                            object current = Current(enumerator);
                            list.Add(current);
                        }

                        return LanguagePrimitives.ConvertTo(list, targetType, CultureInfo.InvariantCulture);
                    }

                    // If it's a generic type then make sure it only has one type argument
                    if (targetType.IsGenericType)
                    {
                        Type[] ta = targetType.GetGenericArguments();
                        if (ta.Length != 1)
                        {
                            throw InterpreterError.NewInterpreterException(expression, typeof(RuntimeException),
                                null, "ForEachBadGenericConversionTypeSpecified", ParserStrings.ForEachBadGenericConversionTypeSpecified, ParserOps.ConvertTo<string>(targetType, null));
                        }

                        resultCollection = PSObject.AsPSObject(Activator.CreateInstance(targetType));
                        while (MoveNext(context, enumerator))
                        {
                            object current = Current(enumerator);
                            // Let the PSObject method invocation mechanism take care of
                            // any required conversions, etc.
                            resultCollection.Add(current);
                        }
                    }
                }
                else
                {
                    // Target is not a collection so return a Collection<targetType>
                    Type resultCollectionType = typeof(Collection<>).MakeGenericType(targetType);
                    resultCollection = PSObject.AsPSObject(Activator.CreateInstance(resultCollectionType));

                    while (MoveNext(context, enumerator))
                    {
                        object current = Current(enumerator);
                        // Let the PSObject method invocation mechanism take care of
                        // any required conversions, etc.
                        resultCollection.Add(current);
                    }
                }

                if (resultCollection == null)
                {
                    throw InterpreterError.NewInterpreterException(expression, typeof(RuntimeException),
                        null, "ForEachTypeConversionFailed", ParserStrings.ForEachTypeConversionFailed, ParserOps.ConvertTo<string>(targetType, null));
                }

                return resultCollection;
            }

            // If the expression is a script block, it will be executed in the current scope
            // once on each element.
            var result = new Collection<PSObject>();
            ScriptBlock sb = expression as ScriptBlock;
            if (sb != null)
            {
                if (sb.HasCleanBlock)
                {
                    throw new PSNotSupportedException(ParserStrings.ForEachNotSupportCleanBlock);
                }

                Pipe outputPipe = new Pipe(result);
                if (sb.HasBeginBlock)
                {
                    sb.InvokeWithPipeImpl(ScriptBlockClauseToInvoke.Begin, false, null, null, ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe, AutomationNull.Value, AutomationNull.Value, AutomationNull.Value, outputPipe, null, arguments);
                }

                ScriptBlockClauseToInvoke processClause = (sb.HasProcessBlock) ? ScriptBlockClauseToInvoke.Process : ScriptBlockClauseToInvoke.End;
                object ie = null;
                while (MoveNext(context, enumerator))
                {
                    ie = Current(enumerator);
                    if (ie != AutomationNull.Value)
                    {
                        sb.InvokeWithPipeImpl(processClause, false, null, null, ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe, ie, AutomationNull.Value, AutomationNull.Value, outputPipe, null, arguments);
                    }
                }

                if (processClause == ScriptBlockClauseToInvoke.Process && sb.HasEndBlock)
                {
                    // $_ has the same value as it did in the last iteration of the process loop
                    sb.InvokeWithPipeImpl(ScriptBlockClauseToInvoke.End, false, null, null, ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe, ie, AutomationNull.Value, AutomationNull.Value, outputPipe, null, arguments);
                }
            }
            else
            {
                // Deal with member gets, sets and invokes
                string name = ParserOps.ConvertTo<string>(expression, null);
                var numArgs = arguments.Length;
                var languageMode = context.LanguageMode;

                while (MoveNext(context, enumerator))
                {
                    object current = Current(enumerator);
                    object basedCurrent = PSObject.Base(current);
                    Hashtable ht = basedCurrent as Hashtable;
                    if (ht != null)
                    {
                        // special case hashtables since we don't want to hit a method name
                        switch (numArgs)
                        {
                            case 0:
                                // No args so do a set
                                object element = ht[name];
                                result.Add(element != null ? PSObject.AsPSObject(element) : null);
                                break;

                            case 1:
                                // 1 args so set as a scalar
                                ht[name] = arguments[0];
                                break;

                            default:
                                // more than one arg, just assign as is
                                ht[name] = arguments;
                                break;
                        }
                    }
                    else
                    {
                        // handle the null case with PowerShell semantics:
                        // - retrieving a property on null adds a null to the result set
                        // - setting a property on null or trying to invoke a method is an error
                        if (current == null)
                        {
                            if (arguments.Length == 0)
                            {
                                result.Add(null);
                            }
                            else
                            {
                                var nullRefException = new NullReferenceException();
                                throw new MethodInvocationException(
                                    nullRefException.GetType().Name,
                                    nullRefException,
                                    ExtendedTypeSystem.MethodInvocationException,
                                    name, arguments.Length, nullRefException.Message);
                            }

                            continue;
                        }

                        var ie = PSObject.AsPSObject(current);
                        if (ie != AutomationNull.Value)
                        {
                            PSMemberInfo member = ie.Members[name];

                            // If the property was not found, check strict mode...
                            if (member == null)
                            {
                                if (context.IsStrictVersion(2))
                                {
                                    throw InterpreterError.NewInterpreterException(null, typeof(RuntimeException),
                                        null, "PropertyNotFoundStrict", ParserStrings.PropertyNotFoundStrict, name);
                                }

                                if (numArgs == 0)
                                {
                                    result.Add(null);
                                    continue;
                                }
                                else
                                {
                                    throw InterpreterError.NewInterpreterException(ie, typeof(NullReferenceException), null, "ForEachNonexistentMemberReference",
                                        ParserStrings.ForEachNonexistentMemberReference, name);
                                }
                            }

                            var method = member as PSMethodInfo;
                            if (method != null)
                            {
                                // It's a method so check language modes to see if it's allowed.

                                // Cannot invoke a method in RestrictedLanguage mode
                                if (languageMode == PSLanguageMode.RestrictedLanguage)
                                {
                                    throw InterpreterError.NewInterpreterException(current, typeof(PSInvalidOperationException),
                                        null, "NoMethodInvocationInRestrictedLanguageMode", InternalCommandStrings.NoMethodInvocationInRestrictedLanguageMode);
                                }

                                // In constrained language mode, can only execute methods on certain types.
                                if (languageMode == PSLanguageMode.ConstrainedLanguage)
                                {
                                    if (!CoreTypes.Contains(basedCurrent.GetType()))
                                    {
                                        throw InterpreterError.NewInterpreterException(current, typeof(PSInvalidOperationException),
                                            null, "MethodInvocationNotSupportedInConstrainedLanguage", ParserStrings.InvokeMethodConstrainedLanguage);
                                    }
                                }

                                result.Add(PSObject.AsPSObject(method.Invoke(arguments)));
                            }
                            else
                            {
                                var property = member as PSPropertyInfo;

                                switch (numArgs)
                                {
                                    case 0:
                                        // No args: do a get
                                        result.Add(PSObject.AsPSObject(property.Value));
                                        break;

                                    case 1:
                                        // 1 arg: set as a scalar
                                        property.Value = arguments[0];
                                        break;

                                    default:
                                        // more than one arg, just assign as is
                                        property.Value = arguments;
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        internal static object SlicingIndex(object target, IEnumerator indexes, Func<object, object, object> indexer)
        {
            var fakeEnumerator = indexes as NonEnumerableObjectEnumerator;
            if (fakeEnumerator != null)
            {
                // We have a non-enumerable object, we're trying to slice index with it.  It really should have
                // been a single index, so we don't want to return an array, we just want to return the indexed value.
                return indexer(target, fakeEnumerator.GetNonEnumerableObject());
            }

            var result = new List<object>();
            while (MoveNext(null, indexes))
            {
                var value = indexer(target, Current(indexes));
                if (value != AutomationNull.Value)
                {
                    result.Add(value);
                }
            }

            return result.ToArray();
        }

        private static void FlattenResults(object o, List<object> result)
        {
            var e = LanguagePrimitives.GetEnumerator(o);
            if (e != null)
            {
                while (e.MoveNext())
                {
                    o = e.Current;
                    if (o != AutomationNull.Value)
                    {
                        result.Add(o);
                    }
                }
            }
            else
            {
                result.Add(o);
            }
        }

        private static void PropertyGetterWorker(CallSite<Func<CallSite, object, object>> getMemberBinderSite,
                                                 IEnumerator enumerator,
                                                 ExecutionContext context,
                                                 List<object> result)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
            while (MoveNext(context, enumerator))
            {
                var current = Current(enumerator);
                var o = getMemberBinderSite.Target.Invoke(getMemberBinderSite, current);
                if (o != AutomationNull.Value)
                {
                    FlattenResults(o, result);
                }
                else
                {
                    // Recurse through collections if current didn't have the property.
                    var nestedEnumerator = LanguagePrimitives.GetEnumerator(current);
                    if (nestedEnumerator != null)
                    {
                        PropertyGetterWorker(getMemberBinderSite, nestedEnumerator, context, result);
                    }
                }
            }
        }

        internal static object PropertyGetter(PSGetMemberBinder binder, IEnumerator enumerator)
        {
            var getMemberBinderSite = CallSite<Func<CallSite, object, object>>.Create(binder);
            var result = new List<object>();
            var context = LocalPipeline.GetExecutionContextFromTLS();

            PropertyGetterWorker(getMemberBinderSite, enumerator, context, result);

            if (result.Count == 1)
            {
                return result[0];
            }

            if (result.Count == 0)
            {
                if (context.IsStrictVersion(2))
                {
                    throw InterpreterError.NewInterpreterException(null, typeof(RuntimeException),
                        null, "PropertyNotFoundStrict", ParserStrings.PropertyNotFoundStrict, binder.Name);
                }

                return null;
            }

            return result.ToArray();
        }

        private static void MethodInvokerWorker(CallSite invokeMemberSite,
                                                IEnumerator enumerator,
                                                object[] args,
                                                ExecutionContext context,
                                                List<object> result,
                                                ref bool foundMethod)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
            while (MoveNext(context, enumerator))
            {
                var current = Current(enumerator);

                try
                {
                    // The following 2 lines contain quite a bit of magic.  We know that invokeMemberSite is a CallSite,
                    // but we don't know the exact delegate type so we can't use the usual code site.Target.Invoke.
                    // The Target could be an unbounded number of different types - but we do know is that it will have
                    // a delegate member named Target and we want to invoke that delegate.
                    // We do know it will have a signature like:
                    //     Func<CallSite, object, <unknown number of argument types>, object>
                    // Because we don't know the number of arguments, we can use DynamicInvoke to call the delegate.
                    dynamic site = invokeMemberSite;
                    object o = site.Target.DynamicInvoke(args.Prepend(current).Prepend(invokeMemberSite).ToArray());

                    // If we get here, we successfully called one method, so set the flag so we don't report a MissingMethodException.
                    // If there was a method, but it raised an exception, it doesn't matter that we aren't setting this flag, we'll
                    // be reporting the method's exception anyway, not a MissingMethodException.
                    foundMethod = true;

                    // void methods return AutomationNull.Value, so don't add it
                    if (o != AutomationNull.Value)
                    {
                        FlattenResults(o, result);
                    }
                }
                catch (TargetInvocationException tie)
                {
                    // If we tried to invoke a method that didn't exist, then we'll try enumerating the object and call the method on it's members.
                    RuntimeException rte = tie.InnerException as RuntimeException;
                    if (rte != null && rte.ErrorRecord.FullyQualifiedErrorId.Equals(ParserOps.MethodNotFoundErrorId, StringComparison.Ordinal))
                    {
                        var nestedEnumerator = LanguagePrimitives.GetEnumerator(current);
                        if (nestedEnumerator != null)
                        {
                            MethodInvokerWorker(invokeMemberSite, nestedEnumerator, args, context, result, ref foundMethod);
                            continue;
                        }
                    }

                    // Always unwrap the TargetInvocationException - we are called via a delegate already and anything we throw
                    // will get wrapped in a new TargetInvocationException.
                    throw tie.InnerException;
                }
            }
        }

        // Call some method(s) named by binder on all objects from enumerator - applied recursively if an object is itself enumerable
        // and doesn't have the method.
        // We don't necessarily call the same method on each object, just the same named method.
        internal static object MethodInvoker(PSInvokeMemberBinder binder,
                                             Type delegateType,
                                             IEnumerator enumerator,
                                             object[] args,
                                             Type typeForMessage)
        {
            var invokeMemberSite = CallSite.Create(delegateType, binder);
            var result = new List<object>();
            var context = LocalPipeline.GetExecutionContextFromTLS();

            bool foundMethod = false;
            MethodInvokerWorker(invokeMemberSite, enumerator, args, context, result, ref foundMethod);

            if (result.Count == 1)
            {
                return result[0];
            }

            if (!foundMethod)
            {
                // We must have had an empty collection - throw an error.
                throw InterpreterError.NewInterpreterException(null, typeof(RuntimeException), null,
                                                               ParserOps.MethodNotFoundErrorId,
                                                               ParserStrings.MethodNotFound, typeForMessage.FullName,
                                                               binder.Name);
            }

            if (result.Count == 0)
            {
                // All void methods - don't return a value.
                return AutomationNull.Value;
            }

            return result.ToArray();
        }

        internal static object Multiply(IEnumerator enumerator, uint times)
        {
            var fakeEnumerator = enumerator as NonEnumerableObjectEnumerator;
            if (fakeEnumerator != null)
            {
                // We have a non-enumerable object, we're trying to multiply something to it.  Generate an error
                // (or on the off chance that there is an implicit op, call that).
                return ParserOps.ImplicitOp(fakeEnumerator.GetNonEnumerableObject(),
                                            times,
                                            "op_Multiply", null, "*");
            }

            var originalList = new List<object>();
            while (MoveNext(null, enumerator))
            {
                originalList.Add(Current(enumerator));
            }

            if (originalList.Count == 0)
            {
#pragma warning disable CA1825 // Avoid zero-length array allocations
                // Don't use Array.Empty<object>(); always return a new instance.
                return new object[0];
#pragma warning restore CA1825 // Avoid zero-length array allocations
            }

            return ArrayOps.Multiply(originalList.ToArray(), times);
        }

        internal static IEnumerator GetEnumerator(IEnumerable enumerable)
        {
            try
            {
                return enumerable.GetEnumerator();
            }
            catch (RuntimeException)
            {
                // Just rethrow runtime exceptions...
                throw;
            }
            catch (Exception e)
            {
                throw new ExtendedTypeSystemException(
                    "ExceptionInGetEnumerator",
                    e,
                    ExtendedTypeSystem.EnumerationException,
                    e.Message);
            }
        }

        // Sometimes we need to pretend something is enumerable when it isn't.  So we wrap the object in a collection and enumerate that.
        // But sometimes we need to behave differently when an object is enumerable or not.  For example:
        //
        //     $o -eq $o
        //
        // If $o is enumerable, this expression will always return $null because we search for values in the LHS that match the RHS.
        // If $o is not enumerable, this expression returns $true.
        //
        // The solution is to pretend the object is enumerable, return a real but custom enumerator.  In places that don't care
        // about semantics (e.g. when writing to the pipe), the enumerator will work just fine.  In places where we care about
        // language semantics, we can check the type of the enumerator and use the non-enumerable semantics instead.
        internal class NonEnumerableObjectEnumerator : IEnumerator
        {
            internal static IEnumerator Create(object obj)
            {
                return new NonEnumerableObjectEnumerator
                {
                    _obj = obj,
                    _realEnumerator = (new[] { obj }).GetEnumerator()
                };
            }

            private object _obj;
            private IEnumerator _realEnumerator;

            bool IEnumerator.MoveNext()
            {
                return _realEnumerator.MoveNext();
            }

            void IEnumerator.Reset()
            {
                _realEnumerator.Reset();
            }

            object IEnumerator.Current
            {
                get { return _realEnumerator.Current; }
            }

            internal object GetNonEnumerableObject()
            {
                return _obj;
            }
        }

        internal static IEnumerator GetCOMEnumerator(object obj)
        {
            object targetValue = PSObject.Base(obj);
            try
            {
                var enumerator = (targetValue as IEnumerable)?.GetEnumerator();
                if (enumerator != null)
                {
                    return enumerator;
                }
            }
            catch (Exception)
            {
            }

            return targetValue as IEnumerator ?? NonEnumerableObjectEnumerator.Create(obj);
        }

        internal static IEnumerator GetGenericEnumerator<T>(IEnumerable<T> enumerable)
        {
            try
            {
                return enumerable.GetEnumerator();
            }
            catch (RuntimeException)
            {
                // Just rethrow runtime exceptions...
                throw;
            }
            catch (Exception e)
            {
                throw new ExtendedTypeSystemException(
                    "ExceptionInGetEnumerator",
                    e,
                    ExtendedTypeSystem.EnumerationException,
                    e.Message);
            }
        }

        /// <summary>
        /// A routine used to advance an enumerator and catch errors that might occur
        /// performing the operation.
        /// </summary>
        /// <param name="context">The execution context used to see if the pipeline is stopping.</param>
        /// <param name="enumerator">THe enumerator to advance.</param>
        /// <exception cref="RuntimeException">An error occurred moving to the next element in the enumeration.</exception>
        /// <returns>True if the move succeeded.</returns>
        internal static bool MoveNext(ExecutionContext context, IEnumerator enumerator)
        {
            try
            {
                // Check to see if we're stopping...
                if (context != null && context.CurrentPipelineStopping)
                    throw new PipelineStoppedException();

                return enumerator.MoveNext();
            }
            catch (RuntimeException)
            {
                throw;
            }
            catch (FlowControlException)
            {
                throw;
            }
            catch (ScriptCallDepthException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw InterpreterError.NewInterpreterExceptionWithInnerException(enumerator, typeof(RuntimeException),
                    null, "BadEnumeration", ParserStrings.BadEnumeration, e, e.Message);
            }
        }

        /// <summary>
        /// Wrapper caller for enumerator.Current - handles and republishes errors...
        /// </summary>
        /// <param name="enumerator">The enumerator to read from.</param>
        /// <returns></returns>
        internal static object Current(IEnumerator enumerator)
        {
            try
            {
                return enumerator.Current;
            }
            catch (RuntimeException)
            {
                throw;
            }
            catch (ScriptCallDepthException)
            {
                throw;
            }
            catch (FlowControlException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw InterpreterError.NewInterpreterExceptionWithInnerException(enumerator, typeof(RuntimeException),
                    null, "BadEnumeration", ParserStrings.BadEnumeration, e, e.Message);
            }
        }

        internal static object AddFakeEnumerable(NonEnumerableObjectEnumerator fakeEnumerator, object rhs)
        {
            // We have a non-enumerable object, we're trying to add something to it.  Generate an error
            // (or on the off chance that there is an implicit op, call that).
            var fakeEnumerator2 = rhs as NonEnumerableObjectEnumerator;
            return ParserOps.ImplicitOp(fakeEnumerator.GetNonEnumerableObject(),
                                        fakeEnumerator2 != null ? fakeEnumerator2.GetNonEnumerableObject() : rhs,
                                        "op_Addition", null, "+");
        }

        internal static object AddEnumerable(ExecutionContext context, IEnumerator lhs, IEnumerator rhs)
        {
            var fakeEnumerator = lhs as NonEnumerableObjectEnumerator;
            if (fakeEnumerator != null)
            {
                return AddFakeEnumerable(fakeEnumerator, rhs);
            }

            var result = new List<object>();

            while (MoveNext(context, lhs))
            {
                result.Add(Current(lhs));
            }

            while (MoveNext(context, rhs))
            {
                result.Add(Current(rhs));
            }

            return result.ToArray();
        }

        internal static object AddObject(ExecutionContext context, IEnumerator lhs, object rhs)
        {
            var fakeEnumerator = lhs as NonEnumerableObjectEnumerator;
            if (fakeEnumerator != null)
            {
                return AddFakeEnumerable(fakeEnumerator, rhs);
            }

            var result = new List<object>();

            while (MoveNext(context, lhs))
            {
                result.Add(Current(lhs));
            }

            result.Add(rhs);

            return result.ToArray();
        }

        internal static object Compare(IEnumerator enumerator, object valueToCompareTo, Func<object, object, bool> compareDelegate)
        {
            var fakeEnumerator = enumerator as NonEnumerableObjectEnumerator;
            if (fakeEnumerator != null)
            {
                return compareDelegate(fakeEnumerator.GetNonEnumerableObject(), valueToCompareTo) ? Boxed.True : Boxed.False;
            }

            var resultArray = new List<object>();
            while (MoveNext(null, enumerator))
            {
                object val = Current(enumerator);
                if (compareDelegate(val, valueToCompareTo))
                {
                    resultArray.Add(val);
                }
            }

            return resultArray.ToArray();
        }

        internal static void WriteEnumerableToPipe(IEnumerator enumerator, Pipe pipe, ExecutionContext context, bool dispose)
        {
            try
            {
                while (MoveNext(context, enumerator))
                {
                    pipe.Add(Current(enumerator));
                }
            }
            finally
            {
                if (dispose)
                {
                    var disposable = enumerator as IDisposable;
                    disposable?.Dispose();
                }
            }
        }

        internal static object[] ToArray(IEnumerator enumerator)
        {
            var result = new List<object>();
            while (MoveNext(null, enumerator))
            {
                result.Add(Current(enumerator));
            }

            return result.ToArray();
        }

        internal static object[] GetSlice(IList list, int startIndex)
        {
            int countElements = list.Count - startIndex;
            object[] result = new object[countElements];

            int i = startIndex;
            int j = 0;
            while (j < countElements)
            {
                result[j++] = list[i++];
            }

            return result;
        }
    }

    internal static class MemberInvocationLoggingOps
    {
        private static readonly Lazy<bool> DumpLogAMSIContent = new Lazy<bool>(
            () => {
                object result = Environment.GetEnvironmentVariable("__PSDumpAMSILogContent");
                if (result != null && LanguagePrimitives.TryConvertTo(result, out int value))
                {
                    return value == 1;
                }
                return false;
            }
        );

        private static string ArgumentToString(object arg)
        {
            object baseObj = PSObject.Base(arg);
            if (baseObj is null)
            {
                // The argument is null or AutomationNull.Value.
                return "null";
            }

            // The comparisons below are ordered by the likelihood of arguments being of those types.
            if (baseObj is string str)
            {
                return str;
            }

            // Special case some types to call 'ToString' on the object. For the rest, we return its
            // full type name to avoid calling a potentially expensive 'ToString' implementation.
            Type baseType = baseObj.GetType();
            if (baseType.IsEnum || baseType.IsPrimitive
                || baseType == typeof(Guid)
                || baseType == typeof(Uri)
                || baseType == typeof(Version)
                || baseType == typeof(SemanticVersion)
                || baseType == typeof(BigInteger)
                || baseType == typeof(decimal))
            {
                return baseObj.ToString();
            }

            return baseType.FullName;
        }

        internal static void LogMemberInvocation(string targetName, string name, object[] args)
        {
            try
            {
                var contentName = "PowerShellMemberInvocation";
                var argsBuilder = new Text.StringBuilder();

                for (int i = 0; i < args.Length; i++)
                {
                    string value = ArgumentToString(args[i]);

                    if (i > 0)
                    {
                        argsBuilder.Append(", ");
                    }

                    argsBuilder.Append($"<{value}>");
                }

                string content = $"<{targetName}>.{name}({argsBuilder})";

                if (DumpLogAMSIContent.Value)
                {
                    Console.WriteLine("\n=== Amsi notification report content ===");
                    Console.WriteLine(content);
                }

                var success = AmsiUtils.ReportContent(
                    name: contentName,
                    content: content);

                if (DumpLogAMSIContent.Value)
                {
                    Console.WriteLine($"=== Amsi notification report success: {success} ===");
                }
            }
            catch (PSSecurityException)
            {
                // ReportContent() will throw PSSecurityException if AMSI detects malware, which 
                // must be propagated.
                throw;
            }
            catch (Exception ex)
            {
                if (DumpLogAMSIContent.Value)
                {
                    Console.WriteLine($"!!! Amsi notification report exception: {ex} !!!");
                }
            }
        }
    }
}
