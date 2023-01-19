// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;

namespace System.Management.Automation
{
    // A visitor to walk an AST and validate that it can be converted to PowerShell.
    internal class ScriptBlockToPowerShellChecker : AstVisitor
    {
        private readonly HashSet<string> _validVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal ScriptBlockAst ScriptBeingConverted { get; set; }

        internal bool UsesParameter { get; private set; }

        internal bool HasUsingExpr { get; private set; }

        public override AstVisitAction VisitParameter(ParameterAst parameterAst)
        {
            if (parameterAst.Name.VariablePath.IsAnyLocal())
            {
                _validVariables.Add(parameterAst.Name.VariablePath.UnqualifiedPath);
            }

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitPipeline(PipelineAst pipelineAst)
        {
            if (pipelineAst.PipelineElements[0] is CommandExpressionAst)
            {
                // If the first element is a CommandExpression, this pipeline should be the value
                // of a parameter. We want to avoid a scriptblock that contains only a pure expression.
                // The check "pipelineAst.Parent.Parent == ScriptBeingConverted" guarantees we throw
                // error on that kind of scriptblock.

                // Disallow pure expressions at the "top" level, but allow them otherwise.
                // We want to catch:
                //     1 | echo
                // But we don't want to error out on:
                //     echo $(1)
                // See the comment in VisitCommand on why it's safe to check Parent.Parent, we
                // know that we have at least:
                //     * a NamedBlockAst (the end block)
                //     * a ScriptBlockAst (the ast we're comparing to)
                if (pipelineAst.GetPureExpression() == null || pipelineAst.Parent.Parent == ScriptBeingConverted)
                {
                    ThrowError(
                        new ScriptBlockToPowerShellNotSupportedException(
                            "CantConvertPipelineStartsWithExpression", null,
                            AutomationExceptions.CantConvertPipelineStartsWithExpression),
                        pipelineAst);
                }
            }

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitCommand(CommandAst commandAst)
        {
            if (commandAst.InvocationOperator == TokenKind.Dot)
            {
                ThrowError(
                    new ScriptBlockToPowerShellNotSupportedException(
                        "CantConvertWithDotSourcing", null, AutomationExceptions.CantConvertWithDotSourcing),
                    commandAst);
            }

            // Up front checking ensures that we have a simple script block,
            // so we can safely assume that the parents are:
            //     * a PipelineAst
            //     * a NamedBlockAst (the end block)
            //     * a ScriptBlockAst (the ast we're comparing to)
            // If that isn't the case, the conversion isn't allowed.  It
            // is also safe to assume that we have at least 3 parents, a script block can't be simpler.
            if (commandAst.Parent.Parent.Parent != ScriptBeingConverted)
            {
                ThrowError(
                    new ScriptBlockToPowerShellNotSupportedException(
                        "CantConvertWithCommandInvocations", null, AutomationExceptions.CantConvertWithCommandInvocations),
                    commandAst);
            }

            if (commandAst.CommandElements[0] is ScriptBlockExpressionAst)
            {
                ThrowError(
                    new ScriptBlockToPowerShellNotSupportedException(
                        "CantConvertWithScriptBlockInvocation", null,
                        AutomationExceptions.CantConvertWithScriptBlockInvocation),
                    commandAst);
            }

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitMergingRedirection(MergingRedirectionAst redirectionAst)
        {
            if (redirectionAst.ToStream != RedirectionStream.Output)
            {
                ThrowError(
                    new ScriptBlockToPowerShellNotSupportedException(
                        "CanConvertOneOutputErrorRedir", null, AutomationExceptions.CanConvertOneOutputErrorRedir),
                    redirectionAst);
            }

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitFileRedirection(FileRedirectionAst redirectionAst)
        {
            ThrowError(
                new ScriptBlockToPowerShellNotSupportedException(
                    "CanConvertOneOutputErrorRedir", null, AutomationExceptions.CanConvertOneOutputErrorRedir),
                redirectionAst);

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitVariableExpression(VariableExpressionAst variableExpressionAst)
        {
            bool usesParameterReference = this.UsesParameter;
            bool ok = variableExpressionAst.IsSafeVariableReference(_validVariables, ref usesParameterReference);
            if (usesParameterReference != this.UsesParameter)
            {
                this.UsesParameter = usesParameterReference;
            }

            if (!ok)
            {
                ThrowError(new ScriptBlockToPowerShellNotSupportedException(
                               "CantConvertWithUndeclaredVariables",
                               null,
                               AutomationExceptions.CantConvertWithUndeclaredVariables,
                               variableExpressionAst.VariablePath),
                           variableExpressionAst);
            }

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst)
        {
            ThrowError(new ScriptBlockToPowerShellNotSupportedException(
                           "CantConvertWithScriptBlocks",
                           null,
                           AutomationExceptions.CantConvertWithScriptBlocks),
                       scriptBlockExpressionAst);

            return AstVisitAction.SkipChildren;
        }

        public override AstVisitAction VisitUsingExpression(UsingExpressionAst usingExpressionAst)
        {
            // A using expression is always allowed, it simply gets re-written to be a parameter
            HasUsingExpr = true;

            // Skip the children - the expression is evaluated before sending to the remote machine,
            // so it doesn't matter what we might find in the children.
            return AstVisitAction.SkipChildren;
        }

        internal static void ThrowError(ScriptBlockToPowerShellNotSupportedException ex, Ast ast)
        {
            InterpreterError.UpdateExceptionErrorRecordPosition(ex, ast.Extent);
            throw ex;
        }
    }

    internal sealed class UsingExpressionAstSearcher : AstSearcher
    {
        internal static IEnumerable<Ast> FindAllUsingExpressions(Ast ast)
        {
            Diagnostics.Assert(ast != null, "caller to verify arguments");

            var searcher = new UsingExpressionAstSearcher(
                callback: astParam => astParam is UsingExpressionAst,
                stopOnFirst: false,
                searchNestedScriptBlocks: true);
            ast.InternalVisit(searcher);
            return searcher.Results;
        }

        private UsingExpressionAstSearcher(Func<Ast, bool> callback, bool stopOnFirst, bool searchNestedScriptBlocks)
            : base(callback, stopOnFirst, searchNestedScriptBlocks)
        {
        }

        public override AstVisitAction VisitFunctionDefinition(FunctionDefinitionAst ast)
        {
            // Skip the workflow. We are not interested in the UsingExpressions in a workflow
            if (ast.IsWorkflow)
            {
                return AstVisitAction.SkipChildren;
            }

            return CheckScriptBlock(ast);
        }
    }

    /// <summary>
    /// Converts a ScriptBlock to a PowerShell object by traversing the
    /// given Ast.
    /// </summary>
    internal sealed class ScriptBlockToPowerShellConverter
    {
        private readonly PowerShell _powershell;
        private ExecutionContext _context;
        private Dictionary<string, object> _usingValueMap;
        private bool? _createLocalScope;

        private ScriptBlockToPowerShellConverter()
        {
            _powershell = PowerShell.Create();
        }

        internal static PowerShell Convert(ScriptBlockAst body,
                                           ReadOnlyCollection<ParameterAst> functionParameters,
                                           bool isTrustedInput,
                                           ExecutionContext context,
                                           Dictionary<string, object> variables,
                                           bool filterNonUsingVariables,
                                           bool? createLocalScope,
                                           object[] args)
        {
            ExecutionContext.CheckStackDepth();

            args ??= Array.Empty<object>();

            // Perform validations on the ScriptBlock.  GetSimplePipeline can allow for more than one
            // pipeline if the first parameter is true, but Invoke-Command doesn't yet support multiple
            // pipelines in a PowerShell (it just grabs the last command directly.)  The rest of this
            // code properly supports multiple pipelines, so it should just work to change the false to true
            // if/when Invoke-Command can support multiple pipelines.
            string errorId;
            string errorMsg;
            body.GetSimplePipeline(true, out errorId, out errorMsg);
            if (errorId != null)
            {
                throw new ScriptBlockToPowerShellNotSupportedException(errorId, null, errorMsg);
            }

            var checker = new ScriptBlockToPowerShellChecker { ScriptBeingConverted = body };
            if (functionParameters != null)
            {
                foreach (var parameter in functionParameters)
                {
                    parameter.InternalVisit(checker);
                }
            }

            body.InternalVisit(checker);

            // When the context is null (or they haven't supplied any variables), throw, but only if we really need the
            // context (basically, if we have some variable reference to resolve).
            if (context == null && (checker.HasUsingExpr || checker.UsesParameter) && (variables == null))
            {
                throw new PSInvalidOperationException(AutomationExceptions.CantConvertScriptBlockWithNoContext);
            }

            try
            {
                var converter = new ScriptBlockToPowerShellConverter { _context = context, _createLocalScope = createLocalScope };

                if (checker.HasUsingExpr)
                {
                    converter._usingValueMap = GetUsingValues(body, isTrustedInput, context, variables, filterNonUsingVariables).Item1;
                }

                if (checker.UsesParameter)
                {
                    // If any parameters are used, we create a new scope and bind the parameters.

                    var newScope = context.EngineSessionState.NewScope(false);
                    context.EngineSessionState.CurrentScope = newScope;
                    context.EngineSessionState.CurrentScope.ScopeOrigin = CommandOrigin.Internal;

                    var locals =
                        MutableTuple.MakeTuple(Compiler.DottedLocalsTupleType, Compiler.DottedLocalsNameIndexMap);

                    // Get the parameter metadata for the script block.
                    // If 'functionParameters' is not null, then the ScriptBlockAst is actually the body of a FunctionDefinitionAst, and it doesn't have a ParamBlock.
                    // If 'functionParameters' is null, then the ScriptBlockAst may have parameters defined in its ParamBlock.
                    bool usesCmdletBinding = false;
                    var parameters = functionParameters != null
                                         ? Compiler.GetParameterMetaData(functionParameters, true, ref usesCmdletBinding)
                                         : ((IParameterMetadataProvider)body).GetParameterMetadata(true, ref usesCmdletBinding);
                    object[] remainingArgs = ScriptBlock.BindArgumentsForScriptblockInvoke(
                        (RuntimeDefinedParameter[])parameters.Data, args, context, false, null, locals);
                    locals.SetAutomaticVariable(AutomaticVariable.Args, remainingArgs, context);
                    newScope.LocalsTuple = locals;
                }

                foreach (var pipeline in body.EndBlock.Statements.OfType<PipelineAst>())
                {
                    converter._powershell.AddStatement();
                    converter.ConvertPipeline(pipeline, isTrustedInput);
                }

                return converter._powershell;
            }
            finally
            {
                if (checker.UsesParameter)
                {
                    context.EngineSessionState.RemoveScope(context.EngineSessionState.CurrentScope);
                }
            }
        }

        /// <summary>
        /// Get using values as dictionary for the Foreach-Object parallel cmdlet.
        /// Ignore any using expressions that are associated with inner nested Foreach-Object parallel calls,
        /// since they are only effective in the nested call scope and not the current outer scope.
        /// </summary>
        /// <param name = "scriptBlock">Scriptblock to search.</param>
        /// <param name = "isTrustedInput">True when input is trusted.</param>
        /// <param name = "context">Execution context.</param>
        /// <returns>Dictionary of using variable map.</returns>
        internal static Dictionary<string, object> GetUsingValuesForEachParallel(
            ScriptBlock scriptBlock,
            bool isTrustedInput,
            ExecutionContext context)
        {
            // Using variables for Foreach-Object -Parallel use are restricted to be within the 
            // Foreach-Object -Parallel call scope. This will filter the using variable map to variables 
            // only within the current (outer) Foreach-Object -Parallel call scope.
            var usingAsts = UsingExpressionAstSearcher.FindAllUsingExpressions(scriptBlock.Ast).ToList();
            UsingExpressionAst usingAst = null;
            var usingValueMap = new Dictionary<string, object>(usingAsts.Count);
            Version oldStrictVersion = null;
            try
            {
                if (context != null)
                {
                    oldStrictVersion = context.EngineSessionState.CurrentScope.StrictModeVersion;
                    context.EngineSessionState.CurrentScope.StrictModeVersion = PSVersionInfo.PSVersion;
                }

                for (int i = 0; i < usingAsts.Count; ++i)
                {
                    usingAst = (UsingExpressionAst)usingAsts[i];
                    if (IsInForeachParallelCallingScope(scriptBlock.Ast, usingAst))
                    {
                        var value = Compiler.GetExpressionValue(usingAst.SubExpression, isTrustedInput, context);
                        string usingAstKey = PsUtils.GetUsingExpressionKey(usingAst);
                        usingValueMap.TryAdd(usingAstKey, value);
                    }
                }
            }
            catch (RuntimeException rte)
            {
                if (rte.ErrorRecord.FullyQualifiedErrorId.Equals("VariableIsUndefined", StringComparison.Ordinal))
                {
                    throw InterpreterError.NewInterpreterException(
                        targetObject: null, 
                        exceptionType: typeof(RuntimeException),
                        errorPosition: usingAst.Extent, 
                        resourceIdAndErrorId: "UsingVariableIsUndefined",
                        resourceString: AutomationExceptions.UsingVariableIsUndefined, 
                        args: rte.ErrorRecord.TargetObject);
                }
            }
            finally
            {
                if (context != null)
                {
                    context.EngineSessionState.CurrentScope.StrictModeVersion = oldStrictVersion;
                }
            }

            return usingValueMap;
        }

        // List of Foreach-Object command names and aliases.
        // TODO: Look into using SessionState.Internal.GetAliasTable() to find all user created aliases.
        //       But update Alias command logic to maintain reverse table that lists all aliases mapping
        //       to a single command definition, for performance.
        private static readonly string[] forEachNames = new string[]
        {
            "ForEach-Object",
            "foreach",
            "%"
        };

        private static bool FindForEachInCommand(CommandAst commandAst)
        {
            // Command name is always the first element in the CommandAst.
            //  e.g., 'foreach -parallel {}'
            var commandNameElement = (commandAst.CommandElements.Count > 0) ? commandAst.CommandElements[0] : null;
            if (commandNameElement is StringConstantExpressionAst commandName)
            {
                bool found = false;
                foreach (var foreachName in forEachNames)
                {
                    if (commandName.Value.Equals(foreachName, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    // Verify this is foreach-object with parallel parameter set.
                    var bindingResult = StaticParameterBinder.BindCommand(commandAst);
                    if (bindingResult.BoundParameters.ContainsKey("Parallel"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Walks the using Ast to verify it is used within a foreach-object -parallel command
        /// and parameter set scope, and not from within a nested foreach-object -parallel call.
        /// </summary>
        /// <param name="scriptblockAst">Scriptblock Ast containing this using Ast</param>
        /// <param name="usingAst">Using Ast to check.</param>
        /// <returns>True if using expression is in current call scope.</returns>
        private static bool IsInForeachParallelCallingScope(
            Ast scriptblockAst,
            UsingExpressionAst usingAst)
        {
            Diagnostics.Assert(usingAst != null, "usingAst argument cannot be null.");

            /*
                Example:
                $Test1 = "Hello"
                1 | ForEach-Object -Parallel { 
                   $using:Test1
                   $Test2 = "Goodbye"
                   1 | ForEach-Object -Parallel {
                       $using:Test1    # Invalid using scope
                       $using:Test2    # Valid using scope
                   }
                }
            */

            // Search up the parent Ast chain for 'Foreach-Object -Parallel' commands.
            Ast currentParent = usingAst.Parent;
            while (currentParent != scriptblockAst)
            {
                // Look for Foreach-Object outer commands
                if (currentParent is CommandAst commandAst && 
                    FindForEachInCommand(commandAst))
                {
                    // Using Ast is outside the invoking foreach scope.
                    return false;
                }

                currentParent = currentParent.Parent;
            }

            return true;
        }

        /// <summary>
        /// Get using values in the dictionary form.
        /// </summary>
        internal static Dictionary<string, object> GetUsingValuesAsDictionary(ScriptBlock scriptBlock, bool isTrustedInput, ExecutionContext context, Dictionary<string, object> variables)
        {
            return GetUsingValues(scriptBlock.Ast, isTrustedInput, context, variables, false).Item1;
        }

        /// <summary>
        /// Get using values in the array form.
        /// </summary>
        internal static object[] GetUsingValuesAsArray(ScriptBlock scriptBlock, bool isTrustedInput, ExecutionContext context, Dictionary<string, object> variables)
        {
            return GetUsingValues(scriptBlock.Ast, isTrustedInput, context, variables, false).Item2;
        }

        /// <summary>
        /// Collect values for UsingExpressions, in the form of a dictionary and an array.
        ///  - The dictionary form is used when the remote server is PSv5 and later version for handling UsingExpression in Invoke-Command/Start-Job
        ///  - The array form is used when the remote server is PSv3 and PSv4 for handling UsingExpression in Invoke-Command.
        /// </summary>
        /// <remarks>
        /// We still keep the array-form using values because we want to avoid any breaking changes when running Invoke-Command
        /// targeting PSv3 or PSv4 remote end -- if UsingExpressions are all in the same scope, then we still pass an array of using
        /// values to the remote end; otherwise, we will handle UsingExpression as if the remote end is PSv2.
        /// </remarks>
        /// <returns>
        /// A tuple of the dictionary-form and the array-form using values.
        /// If the array-form using value is null, then there are UsingExpressions used in different scopes.
        /// </returns>
        private static Tuple<Dictionary<string, object>, object[]> GetUsingValues(
            Ast body,
            bool isTrustedInput,
            ExecutionContext context,
            Dictionary<string, object> variables,
            bool filterNonUsingVariables)
        {
            Diagnostics.Assert(context != null || variables != null, "can't retrieve variables with no context and no variables");

            var usingAsts = UsingExpressionAstSearcher.FindAllUsingExpressions(body).ToList();
            var usingValueArray = new object[usingAsts.Count];
            var usingValueMap = new Dictionary<string, object>(usingAsts.Count);
            HashSet<string> usingVarNames = (variables != null && filterNonUsingVariables) ? new HashSet<string>() : null;

            // Used to check if the PSv3/PSv4 way of handling UsingExpression can continue to be used.
            bool hasUsingExprInDifferentScope = false;
            ScriptBlockAst sbClosestToPreUsingExpr = null;

            UsingExpressionAst usingAst = null;
            Version oldStrictVersion = null;
            try
            {
                if (context != null)
                {
                    oldStrictVersion = context.EngineSessionState.CurrentScope.StrictModeVersion;
                    context.EngineSessionState.CurrentScope.StrictModeVersion = PSVersionInfo.PSVersion;
                }

                for (int i = 0; i < usingAsts.Count; ++i)
                {
                    usingAst = (UsingExpressionAst)usingAsts[i];
                    object value = null;

                    // This happens only when GetUsingValues gets called outside the ScriptBlockToPowerShellConverter class
                    if (!hasUsingExprInDifferentScope && HasUsingExpressionsInDifferentScopes(usingAst, body, ref sbClosestToPreUsingExpr))
                    {
                        // If there are UsingExpressions in different scopes, the array-form using values will not be useful
                        // even if the remote end is PSv3 or PSv4, because the way we handle using expression in PSv3 and PSv4
                        // doesn't support UsingExpression in different scopes. In this case, we will set the array-form using
                        // value to be null before return.
                        //
                        // Note that this check only affect array-form using value. In PSv5, we change the way to handle UsingExpression
                        // on both client and server sides. The dictionary-form using values is used and UsingExpression in different
                        // scope is supported.
                        hasUsingExprInDifferentScope = true;
                    }

                    if (variables != null)
                    {
                        if (!(usingAst.SubExpression is VariableExpressionAst variableAst))
                        {
                            throw InterpreterError.NewInterpreterException(null, typeof(RuntimeException),
                                usingAst.Extent, "CantGetUsingExpressionValueWithSpecifiedVariableDictionary", AutomationExceptions.CantGetUsingExpressionValueWithSpecifiedVariableDictionary, usingAst.Extent.Text);
                        }

                        string varName = variableAst.VariablePath.UserPath;
                        if (varName != null && variables.TryGetValue(varName, out value) && usingVarNames != null)
                        {
                            usingVarNames.Add(varName);
                        }
                    }
                    else
                    {
                        value = Compiler.GetExpressionValue(usingAst.SubExpression, isTrustedInput, context);
                    }

                    // Collect UsingExpression value as an array
                    usingValueArray[i] = value;

                    // Collect UsingExpression value as a dictionary
                    string usingAstKey = PsUtils.GetUsingExpressionKey(usingAst);
                    usingValueMap.TryAdd(usingAstKey, value);
                }
            }
            catch (RuntimeException rte)
            {
                if (rte.ErrorRecord.FullyQualifiedErrorId.Equals("VariableIsUndefined", StringComparison.Ordinal))
                {
                    throw InterpreterError.NewInterpreterException(null, typeof(RuntimeException),
                        usingAst.Extent, "UsingVariableIsUndefined", AutomationExceptions.UsingVariableIsUndefined, rte.ErrorRecord.TargetObject);
                }
                else if (rte.ErrorRecord.FullyQualifiedErrorId.Equals("CantGetUsingExpressionValueWithSpecifiedVariableDictionary", StringComparison.Ordinal))
                {
                    throw;
                }
            }
            finally
            {
                if (context != null)
                {
                    context.EngineSessionState.CurrentScope.StrictModeVersion = oldStrictVersion;
                }
            }

            if (usingVarNames != null)
            {
                string[] keys = variables.Keys.ToArray();
                foreach (string key in keys)
                {
                    if (!usingVarNames.Contains(key))
                    {
                        variables.Remove(key);
                    }
                }
            }

            if (hasUsingExprInDifferentScope)
            {
                usingValueArray = null;
            }

            return Tuple.Create(usingValueMap, usingValueArray);
        }

        /// <summary>
        /// Check if the given UsingExpression is in a different scope from the previous UsingExpression that we analyzed.
        /// </summary>
        /// <remarks>
        /// Note that the value of <paramref name="usingExpr"/> is retrieved by calling 'UsingExpressionAstSearcher.FindAllUsingExpressions'.
        /// So <paramref name="usingExpr"/> is guaranteed not inside a workflow.
        /// </remarks>
        /// <param name="usingExpr">The UsingExpression to analyze.</param>
        /// <param name="topLevelParent">The top level Ast, should be either ScriptBlockAst or FunctionDefinitionAst.</param>
        /// <param name="sbClosestToPreviousUsingExpr">The ScriptBlockAst that represents the scope of the previously analyzed UsingExpressions.</param>
        private static bool HasUsingExpressionsInDifferentScopes(UsingExpressionAst usingExpr, Ast topLevelParent, ref ScriptBlockAst sbClosestToPreviousUsingExpr)
        {
            Diagnostics.Assert(topLevelParent is ScriptBlockAst || topLevelParent is FunctionDefinitionAst,
                "the top level parent should be either a ScriptBlockAst or FunctionDefinitionAst");

            // Scan up the parents of a UsingExpression to check if it's in a nested function/filter/ScriptBlock
            Ast current = usingExpr;
            do
            {
                current = current.Parent;

                var sbAst = current as ScriptBlockAst;
                if (sbAst != null)
                {
                    // We find the closest parent ScriptBlockAst of the current UsingExpression, which represents the scope
                    // that the current UsingExpression is in.

                    if (sbClosestToPreviousUsingExpr == null)
                    {
                        // The current UsingExpression is the first one to be analyzed.
                        sbClosestToPreviousUsingExpr = sbAst;
                        return false;
                    }

                    if (sbAst == sbClosestToPreviousUsingExpr)
                    {
                        // The current UsingExpression is in the same scope as the previous UsingExpression we analyzed.
                        return false;
                    }

                    // The current UsingExpression is in a different scope from the previous UsingExpression we analyzed.
                    return true;
                }

                var funcAst = current as FunctionDefinitionAst;
                if (funcAst != null)
                {
                    // The parent chain of the current UsingExpression reaches a FunctionDefinitionAst, then the UsingExpression
                    // must be in 'Parameters' property of this FunctionDefinitionAst.
                    // In this case, the 'Body' of this FunctionDefinitionAst represents the scope that the UsingExpression is in.

                    if (sbClosestToPreviousUsingExpr == null)
                    {
                        // The current UsingExpression is the first one to be analyzed.
                        sbClosestToPreviousUsingExpr = funcAst.Body;
                        return false;
                    }

                    if (funcAst.Body == sbClosestToPreviousUsingExpr)
                    {
                        // The current UsingExpression is in the same scope as the previous UsingExpression we analyzed.
                        return false;
                    }

                    // The current UsingExpression is in a different scope from the previous UsingExpression we analyzed.
                    return true;
                }
            } while (current != topLevelParent);

            Diagnostics.Assert(false, "Unreachable Code. Top level parent is eitehr ScriptBlockAst or FunctionDefinitionAst, so it should return within the loop for sure.");
            // I don't think it's reachable, but if it happens, just assume there are UsingExpressions in different scopes.
            return true;
        }

        private void ConvertPipeline(PipelineAst pipelineAst, bool isTrustedInput)
        {
            foreach (var command in pipelineAst.PipelineElements)
            {
                ConvertCommand((CommandAst)command, isTrustedInput);
            }
        }

        private void ConvertCommand(CommandAst commandAst, bool isTrustedInput)
        {
            // First need command name.
            var commandName = GetCommandName(commandAst.CommandElements[0], isTrustedInput);

            var command = new Command(commandName, isScript: false, useLocalScope: _createLocalScope);

            // Handle redirections, if any (there can really be just 0 or 1).
            if (commandAst.Redirections.Count > 0)
            {
                Diagnostics.Assert(commandAst.Redirections.Count == 1, "only 1 kind of redirection is supported");
                Diagnostics.Assert(commandAst.Redirections[0] is MergingRedirectionAst, "unexpected redirection type");

                PipelineResultTypes fromType;
                switch (commandAst.Redirections[0].FromStream)
                {
                    case RedirectionStream.Error:
                        fromType = PipelineResultTypes.Error;
                        break;

                    case RedirectionStream.Warning:
                        fromType = PipelineResultTypes.Warning;
                        break;

                    case RedirectionStream.Verbose:
                        fromType = PipelineResultTypes.Verbose;
                        break;

                    case RedirectionStream.Debug:
                        fromType = PipelineResultTypes.Debug;
                        break;

                    case RedirectionStream.Information:
                        fromType = PipelineResultTypes.Information;
                        break;

                    case RedirectionStream.All:
                        fromType = PipelineResultTypes.All;
                        break;

                    default:
                        // Default to Error->Output to be compatible with V2.
                        fromType = PipelineResultTypes.Error;
                        break;
                }

                command.MergeMyResults(fromType, toResult: PipelineResultTypes.Output);
            }

            _powershell.AddCommand(command);

            // Now the parameters and arguments.
            foreach (var ast in commandAst.CommandElements.Skip(1))
            {
                var exprAst = ast as ExpressionAst;
                if (exprAst != null)
                {
                    VariableExpressionAst variableAst = null;

                    var usingExprAst = ast as UsingExpressionAst;
                    if (usingExprAst != null)
                    {
                        string usingAstKey = PsUtils.GetUsingExpressionKey(usingExprAst);
                        object usingValue = _usingValueMap[usingAstKey];
                        variableAst = usingExprAst.SubExpression as VariableExpressionAst;
                        if (variableAst != null && variableAst.Splatted)
                        {
                            // Support the splatting of a dictionary
                            var parameters = usingValue as System.Collections.IDictionary;
                            if (parameters != null)
                            {
                                _powershell.AddParameters(parameters);
                            }
                            else
                            {
                                // Support the splatting of an array
                                var arguments = usingValue as System.Collections.IEnumerable;
                                if (arguments != null)
                                {
                                    foreach (object argument in arguments)
                                    {
                                        _powershell.AddArgument(argument);
                                    }
                                }
                                else
                                {
                                    // Splat the object directly.
                                    _powershell.AddArgument(usingValue);
                                }
                            }
                        }
                        else
                        {
                            _powershell.AddArgument(usingValue);
                        }

                        continue;
                    }

                    variableAst = ast as VariableExpressionAst;
                    if (variableAst != null && variableAst.Splatted)
                    {
                        GetSplattedVariable(variableAst);
                    }
                    else
                    {
                        var constantExprAst = ast as ConstantExpressionAst;
                        object argument;
                        if (constantExprAst != null
                            && (LanguagePrimitives.IsNumeric(LanguagePrimitives.GetTypeCode(constantExprAst.StaticType))
                            || constantExprAst.StaticType == typeof(System.Numerics.BigInteger)))
                        {
                            var commandArgumentText = constantExprAst.Extent.Text;
                            argument = constantExprAst.Value;
                            if (!commandArgumentText.Equals(constantExprAst.Value.ToString(), StringComparison.Ordinal))
                            {
                                // The wrapped number will actually return a PSObject which could end holding a reference to
                                // a typetable, making the object runspace specific.  We should find a better way to avoid
                                // any possibility of sharing problems, though this is unlikely to cause problems.
                                argument = ParserOps.WrappedNumber(argument, commandArgumentText);
                            }
                        }
                        else
                        {
                            if (!isTrustedInput)
                            {
                                try
                                {
                                    argument = GetSafeValueVisitor.GetSafeValue(exprAst, _context, GetSafeValueVisitor.SafeValueContext.GetPowerShell);
                                }
                                catch (System.Exception)
                                {
                                    throw new ScriptBlockToPowerShellNotSupportedException(
                                        "CantConvertWithDynamicExpression",
                                        null,
                                        AutomationExceptions.CantConvertWithDynamicExpression,
                                        exprAst.Extent.Text);
                                }
                            }
                            else
                            {
                                argument = GetExpressionValue(exprAst, isTrustedInput);
                            }
                        }

                        _powershell.AddArgument(argument);
                    }
                }
                else
                {
                    AddParameter((CommandParameterAst)ast, isTrustedInput);
                }
            }
        }

        private string GetCommandName(CommandElementAst commandNameAst, bool isTrustedInput)
        {
            var exprAst = commandNameAst as ExpressionAst;
            string commandName;
            if (exprAst != null)
            {
                var value = GetExpressionValue(exprAst, isTrustedInput);
                if (value == null)
                {
                    ScriptBlockToPowerShellChecker.ThrowError(
                        new ScriptBlockToPowerShellNotSupportedException(
                            "CantConvertWithScriptBlockInvocation", null, AutomationExceptions.CantConvertWithScriptBlockInvocation),
                        exprAst);
                }

                if (value is CommandInfo)
                {
                    commandName = ((CommandInfo)value).Name;
                }
                else
                {
                    commandName = value as string;
                }
            }
            else
            {
                // If this assertion fires, the command name is determined incorrectly.
                Diagnostics.Assert(commandNameAst is CommandParameterAst, "Unexpected element not handled correctly.");
                commandName = commandNameAst.Extent.Text;
            }

            if (string.IsNullOrWhiteSpace(commandName))
            {
                // TODO: could use a better error here
                throw new ScriptBlockToPowerShellNotSupportedException(
                    "CantConvertWithScriptBlockInvocation",
                    null,
                    AutomationExceptions.CantConvertWithScriptBlockInvocation);
            }

            return commandName;
        }

        private void GetSplattedVariable(VariableExpressionAst variableAst)
        {
            if (_context == null)
            {
                throw new PSInvalidOperationException(AutomationExceptions.CantConvertScriptBlockWithNoContext);
            }

            // Process the contents of a splatted variable into the arguments for this
            // command. If the variable contains a hashtable, distribute the key/value pairs
            // If it's an enumerable, then distribute the values as $args and finally
            // if it's a scalar, then the effect is equivalent to $var
            object splattedValue = _context.GetVariableValue(variableAst.VariablePath);
            foreach (var splattedParameter in PipelineOps.Splat(splattedValue, variableAst))
            {
                CommandParameter publicParameter = CommandParameter.FromCommandParameterInternal(splattedParameter);
                _powershell.AddParameter(publicParameter);
            }
        }

        private object GetExpressionValue(ExpressionAst exprAst, bool isTrustedInput)
        {
            // be sure that there's a context at hand
            if (_context == null)
            {
                var rs = RunspaceFactory.CreateRunspace(InitialSessionState.Create());
                rs.Open();
                _context = rs.ExecutionContext;
            }

            if (!isTrustedInput) // if it's not trusted, call the safe value visitor
            {
                return GetSafeValueVisitor.GetSafeValue(exprAst, _context, GetSafeValueVisitor.SafeValueContext.GetPowerShell);
            }

            return Compiler.GetExpressionValue(exprAst, isTrustedInput, _context, _usingValueMap);
        }

        private void AddParameter(CommandParameterAst commandParameterAst, bool isTrustedInput)
        {
            string nameSuffix;
            object argument;
            if (commandParameterAst.Argument != null)
            {
                var arg = commandParameterAst.Argument;
                var errorPos = commandParameterAst.ErrorPosition;
                bool spaceAfterParameter = (errorPos.EndLineNumber != arg.Extent.StartLineNumber ||
                                            errorPos.EndColumnNumber != arg.Extent.StartColumnNumber);
                nameSuffix = spaceAfterParameter ? ": " : ":";

                argument = GetExpressionValue(commandParameterAst.Argument, isTrustedInput);
            }
            else
            {
                nameSuffix = string.Empty;
                argument = null;
            }

            // first character in parameter name must be a dash
            _powershell.AddParameter(
                string.Create(CultureInfo.InvariantCulture, $"-{commandParameterAst.ParameterName}{nameSuffix}"),
                argument);
        }
    }
}
