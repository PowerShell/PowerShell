// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Provider;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using Microsoft.PowerShell;
using Microsoft.PowerShell.Cim;
using Microsoft.PowerShell.Commands;
using Microsoft.PowerShell.Commands.Internal.Format;

namespace System.Management.Automation
{
    /// <summary>
    /// </summary>
    public static class CompletionCompleters
    {
        static CompletionCompleters()
        {
            AppDomain.CurrentDomain.AssemblyLoad += UpdateTypeCacheOnAssemblyLoad;
        }

        private static void UpdateTypeCacheOnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            // Just null out the cache - we'll rebuild it the next time someone tries to complete a type.
            // We could rebuild it now, but we could be loading multiple assemblies (e.g. dependent assemblies)
            // and there is no sense in rebuilding anything until we're done loading all of the assemblies.
            Interlocked.Exchange(ref s_typeCache, null);
        }

        #region Command Names

        /// <summary>
        /// </summary>
        /// <param name="commandName"></param>
        /// <returns></returns>
        public static IEnumerable<CompletionResult> CompleteCommand(string commandName)
        {
            return CompleteCommand(commandName, null);
        }

        /// <summary>
        /// </summary>
        /// <param name="commandName"></param>
        /// <param name="moduleName"></param>
        /// <param name="commandTypes"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static IEnumerable<CompletionResult> CompleteCommand(string commandName, string moduleName, CommandTypes commandTypes = CommandTypes.All)
        {
            var runspace = Runspace.DefaultRunspace;
            if (runspace == null)
            {
                // No runspace, just return no results.
                return CommandCompletion.EmptyCompletionResult;
            }

            var helper = new PowerShellExecutionHelper(PowerShell.Create(RunspaceMode.CurrentRunspace));
            var executionContext = helper.CurrentPowerShell.Runspace.ExecutionContext;
            return CompleteCommand(new CompletionContext { WordToComplete = commandName, Helper = helper, ExecutionContext = executionContext }, moduleName, commandTypes);
        }

        internal static List<CompletionResult> CompleteCommand(CompletionContext context)
        {
            return CompleteCommand(context, null);
        }

        private static List<CompletionResult> CompleteCommand(CompletionContext context, string moduleName, CommandTypes types = CommandTypes.All)
        {
            var addAmpersandIfNecessary = IsAmpersandNeeded(context, false);

            string commandName = context.WordToComplete;
            string quote = HandleDoubleAndSingleQuote(ref commandName);

            List<CompletionResult> commandResults = null;

            if (commandName.IndexOfAny(Utils.Separators.DirectoryOrDrive) == -1)
            {
                // The name to complete is neither module qualified nor is it a relative/rooted file path.

                Ast lastAst = null;
                if (context.RelatedAsts != null && context.RelatedAsts.Count > 0)
                {
                    lastAst = context.RelatedAsts.Last();
                }

                commandResults = ExecuteGetCommandCommand(useModulePrefix: false);

                if (lastAst != null)
                {
                    // We need to add the wildcard to the end so the regex is built correctly.
                    commandName += "*";

                    // Search the asts for function definitions that we might be calling
                    var findFunctionsVisitor = new FindFunctionsVisitor();
                    while (lastAst.Parent != null)
                    {
                        lastAst = lastAst.Parent;
                    }

                    lastAst.Visit(findFunctionsVisitor);

                    WildcardPattern commandNamePattern = WildcardPattern.Get(commandName, WildcardOptions.IgnoreCase);
                    foreach (var defn in findFunctionsVisitor.FunctionDefinitions)
                    {
                        if (commandNamePattern.IsMatch(defn.Name)
                            && !commandResults.Any(cr => cr.CompletionText.Equals(defn.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            // Results found in the current script are prepended to show up at the top of the list.
                            commandResults.Insert(0, GetCommandNameCompletionResult(defn.Name, defn, addAmpersandIfNecessary, quote));
                        }
                    }
                }
            }
            else
            {
                // If there is a single \, we might be looking for a module/snapin qualified command
                var indexOfFirstColon = commandName.IndexOf(':');
                var indexOfFirstBackslash = commandName.IndexOf('\\');
                if (indexOfFirstBackslash > 0 && (indexOfFirstBackslash < indexOfFirstColon || indexOfFirstColon == -1))
                {
                    // First try the name before the backslash as a module name.
                    // Use the exact module name provided by the user
                    moduleName = commandName.Substring(0, indexOfFirstBackslash);
                    commandName = commandName.Substring(indexOfFirstBackslash + 1);

                    commandResults = ExecuteGetCommandCommand(useModulePrefix: true);
                }
            }

            return commandResults;

            List<CompletionResult> ExecuteGetCommandCommand(bool useModulePrefix)
            {
                var powershell = context.Helper
                    .AddCommandWithPreferenceSetting("Get-Command", typeof(GetCommandCommand))
                    .AddParameter("All")
                    .AddParameter("Name", commandName + "*");

                if (moduleName != null)
                {
                    powershell.AddParameter("Module", moduleName);
                }

                if (!types.Equals(CommandTypes.All))
                {
                    powershell.AddParameter("CommandType", types);
                }

                // Exception is ignored, the user simply does not get any completion results if the pipeline fails
                Exception exceptionThrown;
                var commandInfos = context.Helper.ExecuteCurrentPowerShell(out exceptionThrown);

                if (commandInfos == null || commandInfos.Count == 0)
                {
                    powershell.Commands.Clear();
                    powershell
                        .AddCommandWithPreferenceSetting("Get-Command", typeof(GetCommandCommand))
                        .AddParameter("All")
                        .AddParameter("Name", commandName)
                        .AddParameter("UseAbbreviationExpansion");

                    if (moduleName != null)
                    {
                        powershell.AddParameter("Module", moduleName);
                    }

                    if (!types.Equals(CommandTypes.All))
                    {
                        powershell.AddParameter("CommandType", types);
                    }

                    commandInfos = context.Helper.ExecuteCurrentPowerShell(out exceptionThrown);
                }

                List<CompletionResult> completionResults = null;

                if (commandInfos != null && commandInfos.Count > 1)
                {
                    // OrderBy is using stable sorting
                    var sortedCommandInfos = commandInfos.Order(new CommandNameComparer());
                    completionResults = MakeCommandsUnique(sortedCommandInfos, useModulePrefix, addAmpersandIfNecessary, quote);
                }
                else
                {
                    completionResults = MakeCommandsUnique(commandInfos, useModulePrefix, addAmpersandIfNecessary, quote);
                }

                return completionResults;
            }
        }

        private static readonly HashSet<string> s_keywordsToExcludeFromAddingAmpersand
            = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { nameof(TokenKind.InlineScript), nameof(TokenKind.Configuration) };

        internal static CompletionResult GetCommandNameCompletionResult(string name, object command, bool addAmpersandIfNecessary, string quote)
        {
            string syntax = name, listItem = name;

            var commandInfo = command as CommandInfo;
            if (commandInfo != null)
            {
                try
                {
                    listItem = commandInfo.Name;
                    // This may require parsing a script, which could fail in a number of different ways
                    // (syntax errors, security exceptions, etc.)  If so, the name is fine for the tooltip.
                    syntax = commandInfo.Syntax;
                }
                catch (Exception)
                {
                }
            }

            syntax = string.IsNullOrEmpty(syntax) ? name : syntax;
            bool needAmpersand;

            if (CompletionRequiresQuotes(name, false))
            {
                needAmpersand = quote == string.Empty && addAmpersandIfNecessary;
                string quoteInUse = quote == string.Empty ? "'" : quote;
                if (quoteInUse == "'")
                {
                    name = name.Replace("'", "''");
                }
                else
                {
                    name = name.Replace("`", "``");
                    name = name.Replace("$", "`$");
                }

                name = quoteInUse + name + quoteInUse;
            }
            else
            {
                needAmpersand = quote == string.Empty && addAmpersandIfNecessary &&
                                Tokenizer.IsKeyword(name) && !s_keywordsToExcludeFromAddingAmpersand.Contains(name);
                name = quote + name + quote;
            }

            // It's useless to call ForEach-Object (foreach) as the first command of a pipeline. For example:
            //     PS C:\> fore<tab>  --->   PS C:\> foreach   (expected, use as the keyword)
            //     PS C:\> fore<tab>  --->   PS C:\> & foreach (unexpected, ForEach-Object is seldom used as the first command of a pipeline)
            if (needAmpersand && name != SpecialVariables.@foreach)
            {
                name = "& " + name;
            }

            return new CompletionResult(name, listItem, CompletionResultType.Command, syntax);
        }

        internal static List<CompletionResult> MakeCommandsUnique(IEnumerable<PSObject> commandInfoPsObjs, bool includeModulePrefix, bool addAmpersandIfNecessary, string quote)
        {
            List<CompletionResult> results = new List<CompletionResult>();
            if (commandInfoPsObjs == null || !commandInfoPsObjs.Any())
            {
                return results;
            }

            var commandTable = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var psobj in commandInfoPsObjs)
            {
                object baseObj = PSObject.Base(psobj);
                string name = null;

                var commandInfo = baseObj as CommandInfo;
                if (commandInfo != null)
                {
                    // Skip the private commands
                    if (commandInfo.Visibility == SessionStateEntryVisibility.Private) { continue; }

                    name = commandInfo.Name;
                    if (includeModulePrefix && !string.IsNullOrEmpty(commandInfo.ModuleName))
                    {
                        // The command might be a prefixed commandInfo that we get by importing a module with the -Prefix parameter, for example:
                        //    FooModule.psm1: Get-Foo
                        //    import-module FooModule -Prefix PowerShell
                        //    --> command 'Get-PowerShellFoo' in the global session state (prefixed commandInfo)
                        //        command 'Get-Foo' in the module session state (un-prefixed commandInfo)
                        // in that case, we should not add the module name qualification because it doesn't work
                        if (string.IsNullOrEmpty(commandInfo.Prefix) || !ModuleCmdletBase.IsPrefixedCommand(commandInfo))
                        {
                            name = commandInfo.ModuleName + "\\" + commandInfo.Name;
                        }
                    }
                }
                else
                {
                    name = baseObj as string;
                    if (name == null) { continue; }
                }

                object value;
                if (!commandTable.TryGetValue(name, out value))
                {
                    commandTable.Add(name, baseObj);
                }
                else
                {
                    var list = value as List<object>;
                    if (list != null)
                    {
                        list.Add(baseObj);
                    }
                    else
                    {
                        list = new List<object> { value, baseObj };
                        commandTable[name] = list;
                    }
                }
            }

            List<CompletionResult> endResults = null;
            foreach (var keyValuePair in commandTable)
            {
                var commandList = keyValuePair.Value as List<object>;
                if (commandList != null)
                {
                    endResults ??= new List<CompletionResult>();

                    // The first command might be an un-prefixed commandInfo that we get by importing a module with the -Prefix parameter,
                    // in that case, we should add the module name qualification because if the module is not in the module path, calling
                    // 'Get-Foo' directly doesn't work
                    string completionName = keyValuePair.Key;
                    if (!includeModulePrefix)
                    {
                        var commandInfo = commandList[0] as CommandInfo;
                        if (commandInfo != null && !string.IsNullOrEmpty(commandInfo.Prefix))
                        {
                            Diagnostics.Assert(!string.IsNullOrEmpty(commandInfo.ModuleName), "the module name should exist if commandInfo.Prefix is not an empty string");
                            if (!ModuleCmdletBase.IsPrefixedCommand(commandInfo))
                            {
                                completionName = commandInfo.ModuleName + "\\" + completionName;
                            }
                        }
                    }

                    results.Add(GetCommandNameCompletionResult(completionName, commandList[0], addAmpersandIfNecessary, quote));

                    // For the other commands that are hidden, we need to disambiguate,
                    // but put these at the end as it's less likely any of the hidden
                    // commands are desired.  If we can't add anything to disambiguate,
                    // then we'll skip adding a completion result.
                    for (int index = 1; index < commandList.Count; index++)
                    {
                        var commandInfo = commandList[index] as CommandInfo;
                        Diagnostics.Assert(commandInfo != null, "Elements should always be CommandInfo");

                        if (commandInfo.CommandType == CommandTypes.Application)
                        {
                            endResults.Add(GetCommandNameCompletionResult(commandInfo.Definition, commandInfo, addAmpersandIfNecessary, quote));
                        }
                        else if (!string.IsNullOrEmpty(commandInfo.ModuleName))
                        {
                            var name = commandInfo.ModuleName + "\\" + commandInfo.Name;
                            endResults.Add(GetCommandNameCompletionResult(name, commandInfo, addAmpersandIfNecessary, quote));
                        }
                    }
                }
                else
                {
                    // The first command might be an un-prefixed commandInfo that we get by importing a module with the -Prefix parameter,
                    // in that case, we should add the module name qualification because if the module is not in the module path, calling
                    // 'Get-Foo' directly doesn't work
                    string completionName = keyValuePair.Key;
                    if (!includeModulePrefix)
                    {
                        var commandInfo = keyValuePair.Value as CommandInfo;
                        if (commandInfo != null && !string.IsNullOrEmpty(commandInfo.Prefix))
                        {
                            Diagnostics.Assert(!string.IsNullOrEmpty(commandInfo.ModuleName), "the module name should exist if commandInfo.Prefix is not an empty string");
                            if (!ModuleCmdletBase.IsPrefixedCommand(commandInfo))
                            {
                                completionName = commandInfo.ModuleName + "\\" + completionName;
                            }
                        }
                    }

                    results.Add(GetCommandNameCompletionResult(completionName, keyValuePair.Value, addAmpersandIfNecessary, quote));
                }
            }

            if (endResults != null && endResults.Count > 0)
            {
                results.AddRange(endResults);
            }

            return results;
        }

        private sealed class FindFunctionsVisitor : AstVisitor
        {
            internal readonly List<FunctionDefinitionAst> FunctionDefinitions = new List<FunctionDefinitionAst>();

            public override AstVisitAction VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst)
            {
                FunctionDefinitions.Add(functionDefinitionAst);
                return AstVisitAction.Continue;
            }
        }

        #endregion Command Names

        #region Module Names

        internal static List<CompletionResult> CompleteModuleName(CompletionContext context, bool loadedModulesOnly, bool skipEditionCheck = false)
        {
            var moduleName = context.WordToComplete ?? string.Empty;
            var result = new List<CompletionResult>();
            var quote = HandleDoubleAndSingleQuote(ref moduleName);

            if (!moduleName.EndsWith('*'))
            {
                moduleName += "*";
            }

            var powershell = context.Helper.AddCommandWithPreferenceSetting("Get-Module", typeof(GetModuleCommand)).AddParameter("Name", moduleName);
            if (!loadedModulesOnly)
            {
                powershell.AddParameter("ListAvailable", true);

                // -SkipEditionCheck should only be set or apply to -ListAvailable
                if (skipEditionCheck)
                {
                    powershell.AddParameter("SkipEditionCheck", true);
                }
            }

            Exception exceptionThrown;
            var psObjects = context.Helper.ExecuteCurrentPowerShell(out exceptionThrown);

            if (psObjects != null)
            {
                foreach (dynamic moduleInfo in psObjects)
                {
                    var completionText = moduleInfo.Name.ToString();
                    var listItemText = completionText;
                    var toolTip = "Description: " + moduleInfo.Description.ToString() + "\r\nModuleType: "
                                  + moduleInfo.ModuleType.ToString() + "\r\nPath: "
                                  + moduleInfo.Path.ToString();

                    if (CompletionRequiresQuotes(completionText, false))
                    {
                        var quoteInUse = quote == string.Empty ? "'" : quote;
                        if (quoteInUse == "'")
                            completionText = completionText.Replace("'", "''");
                        completionText = quoteInUse + completionText + quoteInUse;
                    }
                    else
                    {
                        completionText = quote + completionText + quote;
                    }

                    result.Add(new CompletionResult(completionText, listItemText, CompletionResultType.ParameterValue, toolTip));
                }
            }

            return result;
        }

        #endregion Module Names

        #region Command Parameters
        private static readonly string[] s_parameterNamesOfImportDSCResource = { "Name", "ModuleName", "ModuleVersion" };

        internal static List<CompletionResult> CompleteCommandParameter(CompletionContext context)
        {
            string partialName = null;
            bool withColon = false;
            CommandAst commandAst = null;
            List<CompletionResult> result = new List<CompletionResult>();

            // Find the parameter ast, it will be near or at the end
            CommandParameterAst parameterAst = null;
            DynamicKeywordStatementAst keywordAst = null;
            for (int i = context.RelatedAsts.Count - 1; i >= 0; i--)
            {
                keywordAst ??= context.RelatedAsts[i] as DynamicKeywordStatementAst;
                parameterAst = (context.RelatedAsts[i] as CommandParameterAst);
                if (parameterAst != null) break;
            }

            if (parameterAst != null)
            {
                keywordAst = parameterAst.Parent as DynamicKeywordStatementAst;
            }

            // If parent is DynamicKeywordStatementAst - 'Import-DscResource',
            // then customize the auto completion results
            if (keywordAst != null && string.Equals(keywordAst.Keyword.Keyword, "Import-DscResource", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(context.WordToComplete) && context.WordToComplete.StartsWith('-'))
            {
                var lastAst = context.RelatedAsts.Last();
                var wordToMatch = string.Concat(context.WordToComplete.AsSpan(1), "*");
                var pattern = WildcardPattern.Get(wordToMatch, WildcardOptions.IgnoreCase);
                var parameterNames = keywordAst.CommandElements.Where(static ast => ast is CommandParameterAst).Select(static ast => (ast as CommandParameterAst).ParameterName);
                foreach (var parameterName in s_parameterNamesOfImportDSCResource)
                {
                    if (pattern.IsMatch(parameterName) && !parameterNames.Contains(parameterName, StringComparer.OrdinalIgnoreCase))
                    {
                        string tooltip = "[String] " + parameterName;
                        result.Add(new CompletionResult("-" + parameterName, parameterName, CompletionResultType.ParameterName, tooltip));
                    }
                }

                if (result.Count > 0)
                {
                    context.ReplacementLength = context.WordToComplete.Length;
                    context.ReplacementIndex = lastAst.Extent.StartOffset;
                }

                return result;
            }

            bool bindPositionalParameters = true;
            if (parameterAst != null)
            {
                // Parent must be a command
                commandAst = (CommandAst)parameterAst.Parent;
                partialName = parameterAst.ParameterName;
                withColon = context.WordToComplete.EndsWith(':');
            }
            else
            {
                // No CommandParameterAst is found. It could be a StringConstantExpressionAst "-"
                if (!(context.RelatedAsts[context.RelatedAsts.Count - 1] is StringConstantExpressionAst dashAst))
                    return result;
                if (!dashAst.Value.Trim().Equals("-", StringComparison.OrdinalIgnoreCase))
                    return result;

                // Parent must be a command
                commandAst = (CommandAst)dashAst.Parent;
                partialName = string.Empty;

                // If the user tries to tab complete a new parameter in front of a positional argument like: dir -<Tab> C:\
                // the user may want to add the parameter name so we don't want to bind positional arguments
                if (commandAst is not null)
                {
                    foreach (var element in commandAst.CommandElements)
                    {
                        if (element.Extent.StartOffset > context.TokenAtCursor.Extent.StartOffset)
                        {
                            bindPositionalParameters = element is CommandParameterAst;
                            break;
                        }
                    }
                }
            }

            PseudoBindingInfo pseudoBinding = new PseudoParameterBinder()
                                                .DoPseudoParameterBinding(commandAst, null, parameterAst, PseudoParameterBinder.BindingType.ParameterCompletion, bindPositionalParameters);
            // The command cannot be found or it's not a cmdlet, not a script cmdlet, not a function.
            // Try completing as if it the parameter is a command argument for native command completion.
            if (pseudoBinding == null)
            {
                return CompleteCommandArgument(context);
            }

            switch (pseudoBinding.InfoType)
            {
                case PseudoBindingInfoType.PseudoBindingFail:
                    // The command is a cmdlet or script cmdlet. Binding failed
                    result = GetParameterCompletionResults(partialName, uint.MaxValue, pseudoBinding.UnboundParameters, withColon);
                    break;
                case PseudoBindingInfoType.PseudoBindingSucceed:
                    // The command is a cmdlet or script cmdlet. Binding succeeded.
                    result = GetParameterCompletionResults(partialName, pseudoBinding, parameterAst, withColon);
                    break;
            }

            if (result.Count == 0)
            {
                result = pseudoBinding.CommandName.Equals("Set-Location", StringComparison.OrdinalIgnoreCase)
                             ? new List<CompletionResult>(CompleteFilename(context, containerOnly: true, extension: null))
                             : new List<CompletionResult>(CompleteFilename(context));
            }

            return result;
        }

        /// <summary>
        /// Get the parameter completion results when the pseudo binding was successful.
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="bindingInfo"></param>
        /// <param name="parameterAst"></param>
        /// <param name="withColon"></param>
        /// <returns></returns>
        private static List<CompletionResult> GetParameterCompletionResults(string parameterName, PseudoBindingInfo bindingInfo, CommandParameterAst parameterAst, bool withColon)
        {
            Diagnostics.Assert(bindingInfo.InfoType.Equals(PseudoBindingInfoType.PseudoBindingSucceed), "The pseudo binding should succeed");
            List<CompletionResult> result = new List<CompletionResult>();

            if (parameterName == string.Empty)
            {
                result = GetParameterCompletionResults(
                    parameterName,
                    bindingInfo.ValidParameterSetsFlags,
                    bindingInfo.UnboundParameters,
                    withColon);
                return result;
            }

            if (bindingInfo.ParametersNotFound.Count > 0)
            {
                // The parameter name cannot be matched to any parameter
                if (bindingInfo.ParametersNotFound.Any(pAst => parameterAst.GetHashCode() == pAst.GetHashCode()))
                {
                    return result;
                }
            }

            if (bindingInfo.AmbiguousParameters.Count > 0)
            {
                // The parameter name is ambiguous. It's ignored in the pseudo binding, and we should search in the UnboundParameters
                if (bindingInfo.AmbiguousParameters.Any(pAst => parameterAst.GetHashCode() == pAst.GetHashCode()))
                {
                    result = GetParameterCompletionResults(
                        parameterName,
                        bindingInfo.ValidParameterSetsFlags,
                        bindingInfo.UnboundParameters,
                        withColon);
                }

                return result;
            }

            if (bindingInfo.DuplicateParameters.Count > 0)
            {
                // The parameter name is resolved to a parameter that is already bound. We search it in the BoundParameters
                if (bindingInfo.DuplicateParameters.Any(pAst => parameterAst.GetHashCode() == pAst.Parameter.GetHashCode()))
                {
                    result = GetParameterCompletionResults(
                        parameterName,
                        bindingInfo.ValidParameterSetsFlags,
                        bindingInfo.BoundParameters.Values,
                        withColon);
                }

                return result;
            }

            // The parameter should be bound in the pseudo binding during the named binding
            string matchedParameterName = null;
            foreach (KeyValuePair<string, AstParameterArgumentPair> entry in bindingInfo.BoundArguments)
            {
                switch (entry.Value.ParameterArgumentType)
                {
                    case AstParameterArgumentType.AstPair:
                        {
                            AstPair pair = (AstPair)entry.Value;
                            if (pair.ParameterSpecified && pair.Parameter.GetHashCode() == parameterAst.GetHashCode())
                            {
                                matchedParameterName = entry.Key;
                            }
                            else if (pair.ArgumentIsCommandParameterAst && pair.Argument.GetHashCode() == parameterAst.GetHashCode())
                            {
                                // The parameter name cannot be resolved to a parameter
                                return result;
                            }
                        }

                        break;
                    case AstParameterArgumentType.Fake:
                        {
                            FakePair pair = (FakePair)entry.Value;
                            if (pair.ParameterSpecified && pair.Parameter.GetHashCode() == parameterAst.GetHashCode())
                            {
                                matchedParameterName = entry.Key;
                            }
                        }

                        break;
                    case AstParameterArgumentType.Switch:
                        {
                            SwitchPair pair = (SwitchPair)entry.Value;
                            if (pair.ParameterSpecified && pair.Parameter.GetHashCode() == parameterAst.GetHashCode())
                            {
                                matchedParameterName = entry.Key;
                            }
                        }

                        break;
                    case AstParameterArgumentType.AstArray:
                    case AstParameterArgumentType.PipeObject:
                        break;
                }

                if (matchedParameterName != null)
                    break;
            }

            if (matchedParameterName is null)
            {
                // The pseudo binder has skipped a parameter
                // This will happen when completing parameters for commands with dynamic parameters.
                result = GetParameterCompletionResults(
                    parameterName,
                    bindingInfo.ValidParameterSetsFlags,
                    bindingInfo.UnboundParameters,
                    withColon);
                return result;
            }

            MergedCompiledCommandParameter param = bindingInfo.BoundParameters[matchedParameterName];

            WildcardPattern pattern = WildcardPattern.Get(parameterName + "*", WildcardOptions.IgnoreCase);
            string parameterType = "[" + ToStringCodeMethods.Type(param.Parameter.Type, dropNamespaces: true) + "] ";
            string colonSuffix = withColon ? ":" : string.Empty;
            if (pattern.IsMatch(matchedParameterName))
            {
                string completionText = "-" + matchedParameterName + colonSuffix;
                string tooltip = parameterType + matchedParameterName;
                result.Add(new CompletionResult(completionText, matchedParameterName, CompletionResultType.ParameterName, tooltip));
            }
            else
            {
                // Process alias when there is partial input
                foreach (var alias in param.Parameter.Aliases)
                {
                    if (pattern.IsMatch(alias))
                    {
                        result.Add(new CompletionResult(
                            $"-{alias}{colonSuffix}",
                            alias,
                            CompletionResultType.ParameterName,
                            parameterType + alias));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Get the parameter completion results by using the given valid parameter sets and available parameters.
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="validParameterSetFlags"></param>
        /// <param name="parameters"></param>
        /// <param name="withColon"></param>
        /// <returns></returns>
        private static List<CompletionResult> GetParameterCompletionResults(
            string parameterName,
            uint validParameterSetFlags,
            IEnumerable<MergedCompiledCommandParameter> parameters,
            bool withColon)
        {
            var result = new List<CompletionResult>();
            var commonParamResult = new List<CompletionResult>();
            var pattern = WildcardPattern.Get(parameterName + "*", WildcardOptions.IgnoreCase);
            var colonSuffix = withColon ? ":" : string.Empty;

            bool addCommonParameters = true;
            foreach (MergedCompiledCommandParameter param in parameters)
            {
                bool inParameterSet = (param.Parameter.ParameterSetFlags & validParameterSetFlags) != 0 || param.Parameter.IsInAllSets;
                if (!inParameterSet)
                    continue;

                string name = param.Parameter.Name;
                string type = "[" + ToStringCodeMethods.Type(param.Parameter.Type, dropNamespaces: true) + "] ";
                bool isCommonParameter = Cmdlet.CommonParameters.Contains(name, StringComparer.OrdinalIgnoreCase);
                List<CompletionResult> listInUse = isCommonParameter ? commonParamResult : result;

                if (pattern.IsMatch(name))
                {
                    // Then using functions to back dynamic keywords, we don't necessarily
                    // want all of the parameters to be shown to the user. Those that are marked
                    // DontShow will not be displayed. Also, if any of the parameters have
                    // don't show set, we won't show any of the common parameters either.
                    bool showToUser = true;
                    var compiledAttributes = param.Parameter.CompiledAttributes;
                    if (compiledAttributes != null && compiledAttributes.Count > 0)
                    {
                        foreach (var attr in compiledAttributes)
                        {
                            var pattr = attr as ParameterAttribute;
                            if (pattr != null && pattr.DontShow)
                            {
                                showToUser = false;
                                addCommonParameters = false;
                                break;
                            }
                        }
                    }

                    if (showToUser)
                    {
                        string completionText = "-" + name + colonSuffix;
                        string tooltip = type + name;
                        listInUse.Add(new CompletionResult(completionText, name, CompletionResultType.ParameterName,
                                                           tooltip));
                    }
                }
                else if (parameterName != string.Empty)
                {
                    // Process alias when there is partial input
                    foreach (var alias in param.Parameter.Aliases)
                    {
                        if (pattern.IsMatch(alias))
                        {
                            listInUse.Add(new CompletionResult(
                                $"-{alias}{colonSuffix}",
                                alias,
                                CompletionResultType.ParameterName,
                                type + alias));
                        }
                    }
                }
            }

            // Add the common parameters to the results if expected.
            if (addCommonParameters)
            {
                result.AddRange(commonParamResult);
            }

            return result;
        }

        /// <summary>
        /// Get completion results for operators that start with <paramref name="wordToComplete"/>
        /// </summary>
        /// <param name="wordToComplete">The starting text of the operator to complete.</param>
        /// <returns>A list of completion results.</returns>
        public static List<CompletionResult> CompleteOperator(string wordToComplete)
        {
            if (wordToComplete.StartsWith('-'))
            {
                wordToComplete = wordToComplete.Substring(1);
            }

            return (from op in Tokenizer._operatorText
                    where op.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase)
                    orderby op
                    select new CompletionResult("-" + op, op, CompletionResultType.ParameterName, GetOperatorDescription(op))).ToList();
        }

        private static string GetOperatorDescription(string op)
        {
            return ResourceManagerCache.GetResourceString(typeof(CompletionCompleters).Assembly,
                                                          "System.Management.Automation.resources.TabCompletionStrings",
                                                          op + "OperatorDescription");
        }

        #endregion Command Parameters

        #region Command Arguments

        internal static List<CompletionResult> CompleteCommandArgument(CompletionContext context)
        {
            CommandAst commandAst = null;
            List<CompletionResult> result = new List<CompletionResult>();

            // Find the expression ast. It should be at the end if there is one
            ExpressionAst expressionAst = null;
            MemberExpressionAst secondToLastMemberAst = null;
            Ast lastAst = context.RelatedAsts.Last();

            expressionAst = lastAst as ExpressionAst;
            if (expressionAst != null)
            {
                if (expressionAst.Parent is CommandAst)
                {
                    commandAst = (CommandAst)expressionAst.Parent;

                    if (expressionAst is ErrorExpressionAst && expressionAst.Extent.Text.EndsWith(','))
                    {
                        context.WordToComplete = string.Empty;
                        // BUGBUG context.CursorPosition = expressionAst.Extent.StartScriptPosition;
                    }
                    else if (commandAst.CommandElements.Count == 1 || context.WordToComplete == string.Empty)
                    {
                        expressionAst = null;
                    }
                    else if (commandAst.CommandElements.Count > 2)
                    {
                        var length = commandAst.CommandElements.Count;
                        var index = 1;

                        for (; index < length; index++)
                        {
                            if (commandAst.CommandElements[index] == expressionAst)
                                break;
                        }

                        CommandElementAst secondToLastAst = null;
                        if (index > 1)
                        {
                            secondToLastAst = commandAst.CommandElements[index - 1];
                            secondToLastMemberAst = secondToLastAst as MemberExpressionAst;
                        }

                        var partialPathAst = expressionAst as StringConstantExpressionAst;
                        if (partialPathAst != null && secondToLastAst != null &&
                            partialPathAst.StringConstantType == StringConstantType.BareWord &&
                            secondToLastAst.Extent.EndLineNumber == partialPathAst.Extent.StartLineNumber &&
                            secondToLastAst.Extent.EndColumnNumber == partialPathAst.Extent.StartColumnNumber &&
                            partialPathAst.Value.AsSpan().IndexOfAny('\\', '/') == 0)
                        {
                            var secondToLastStringConstantAst = secondToLastAst as StringConstantExpressionAst;
                            var secondToLastExpandableStringAst = secondToLastAst as ExpandableStringExpressionAst;
                            var secondToLastArrayAst = secondToLastAst as ArrayLiteralAst;
                            var secondToLastParamAst = secondToLastAst as CommandParameterAst;

                            if (secondToLastStringConstantAst != null || secondToLastExpandableStringAst != null)
                            {
                                var fullPath = ConcatenateStringPathArguments(secondToLastAst, partialPathAst.Value, context);
                                expressionAst = secondToLastStringConstantAst != null
                                                    ? (ExpressionAst)secondToLastStringConstantAst
                                                    : (ExpressionAst)secondToLastExpandableStringAst;

                                context.ReplacementIndex = ((InternalScriptPosition)secondToLastAst.Extent.StartScriptPosition).Offset;
                                context.ReplacementLength += ((InternalScriptPosition)secondToLastAst.Extent.EndScriptPosition).Offset - context.ReplacementIndex;
                                context.WordToComplete = fullPath;
                                // context.CursorPosition = secondToLastAst.Extent.StartScriptPosition;
                            }
                            else if (secondToLastArrayAst != null)
                            {
                                // Handle cases like: dir -Path .\cd, 'a b'\new<tab>
                                var lastArrayElement = secondToLastArrayAst.Elements.LastOrDefault();
                                var fullPath = ConcatenateStringPathArguments(lastArrayElement, partialPathAst.Value, context);
                                if (fullPath != null)
                                {
                                    expressionAst = secondToLastArrayAst;

                                    context.ReplacementIndex = ((InternalScriptPosition)lastArrayElement.Extent.StartScriptPosition).Offset;
                                    context.ReplacementLength += ((InternalScriptPosition)lastArrayElement.Extent.EndScriptPosition).Offset - context.ReplacementIndex;
                                    context.WordToComplete = fullPath;
                                }
                            }
                            else if (secondToLastParamAst != null)
                            {
                                // Handle cases like: dir -Path: .\cd, 'a b'\new<tab> || dir -Path: 'a b'\new<tab>
                                var fullPath = ConcatenateStringPathArguments(secondToLastParamAst.Argument, partialPathAst.Value, context);
                                if (fullPath != null)
                                {
                                    expressionAst = secondToLastParamAst.Argument;

                                    context.ReplacementIndex = ((InternalScriptPosition)secondToLastParamAst.Argument.Extent.StartScriptPosition).Offset;
                                    context.ReplacementLength += ((InternalScriptPosition)secondToLastParamAst.Argument.Extent.EndScriptPosition).Offset - context.ReplacementIndex;
                                    context.WordToComplete = fullPath;
                                }
                                else
                                {
                                    var arrayArgAst = secondToLastParamAst.Argument as ArrayLiteralAst;
                                    if (arrayArgAst != null)
                                    {
                                        var lastArrayElement = arrayArgAst.Elements.LastOrDefault();
                                        fullPath = ConcatenateStringPathArguments(lastArrayElement, partialPathAst.Value, context);
                                        if (fullPath != null)
                                        {
                                            expressionAst = arrayArgAst;

                                            context.ReplacementIndex = ((InternalScriptPosition)lastArrayElement.Extent.StartScriptPosition).Offset;
                                            context.ReplacementLength += ((InternalScriptPosition)lastArrayElement.Extent.EndScriptPosition).Offset - context.ReplacementIndex;
                                            context.WordToComplete = fullPath;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (expressionAst.Parent is ArrayLiteralAst && expressionAst.Parent.Parent is CommandAst)
                {
                    commandAst = (CommandAst)expressionAst.Parent.Parent;

                    if (commandAst.CommandElements.Count == 1 || context.WordToComplete == string.Empty)
                    {
                        // dir -Path a.txt, b.txt <tab>
                        expressionAst = null;
                    }
                    else
                    {
                        // dir -Path a.txt, b.txt c<tab>
                        expressionAst = (ExpressionAst)expressionAst.Parent;
                    }
                }
                else if (expressionAst.Parent is ArrayLiteralAst && expressionAst.Parent.Parent is CommandParameterAst)
                {
                    // Handle scenarios such as
                    //      dir -Path: a.txt, <tab> || dir -Path: a.txt, b.txt <tab>
                    commandAst = (CommandAst)expressionAst.Parent.Parent.Parent;
                    if (context.WordToComplete == string.Empty)
                    {
                        // dir -Path: a.txt, b.txt <tab>
                        expressionAst = null;
                    }
                    else
                    {
                        // dir -Path: a.txt, b<tab>
                        expressionAst = (ExpressionAst)expressionAst.Parent;
                    }
                }
                else if (expressionAst.Parent is CommandParameterAst && expressionAst.Parent.Parent is CommandAst)
                {
                    commandAst = (CommandAst)expressionAst.Parent.Parent;
                    if (expressionAst is ErrorExpressionAst && expressionAst.Extent.Text.EndsWith(','))
                    {
                        // dir -Path: a.txt,<tab>
                        context.WordToComplete = string.Empty;
                        // context.CursorPosition = expressionAst.Extent.StartScriptPosition;
                    }
                    else if (context.WordToComplete == string.Empty)
                    {
                        // Handle scenario like this: Set-ExecutionPolicy -Scope:CurrentUser <tab>
                        expressionAst = null;
                    }
                }
            }
            else
            {
                var paramAst = lastAst as CommandParameterAst;
                if (paramAst != null)
                {
                    commandAst = paramAst.Parent as CommandAst;
                }
                else
                {
                    commandAst = lastAst as CommandAst;
                }
            }

            if (commandAst == null)
            {
                // We don't know if this could be expanded into anything interesting
                return result;
            }

            PseudoBindingInfo pseudoBinding = new PseudoParameterBinder()
                                                .DoPseudoParameterBinding(commandAst, null, null, PseudoParameterBinder.BindingType.ArgumentCompletion);

            do
            {
                // The command cannot be found, or it's NOT a cmdlet, NOT a script cmdlet and NOT a function
                if (pseudoBinding == null)
                    break;

                bool parsedArgumentsProvidesMatch = false;

                if (pseudoBinding.AllParsedArguments != null && pseudoBinding.AllParsedArguments.Count > 0)
                {
                    ArgumentLocation argLocation;
                    bool treatAsExpression = false;

                    if (expressionAst != null)
                    {
                        treatAsExpression = true;
                        var dashExp = expressionAst as StringConstantExpressionAst;
                        if (dashExp != null && dashExp.Value.Trim().Equals("-", StringComparison.OrdinalIgnoreCase))
                        {
                            // "-" is represented as StringConstantExpressionAst. Most likely the user is typing a <tab>
                            // after it, so in the pseudo binder, we ignore it to avoid treating it as an argument.
                            // for example:
                            //      Get-Content -Path "-<tab>  -->  Get-Content -Path ".\-patt.txt"
                            treatAsExpression = false;
                        }
                    }

                    if (treatAsExpression)
                    {
                        argLocation = FindTargetArgumentLocation(
                            pseudoBinding.AllParsedArguments, expressionAst);
                    }
                    else
                    {
                        argLocation = FindTargetArgumentLocation(
                            pseudoBinding.AllParsedArguments, context.TokenAtCursor ?? context.TokenBeforeCursor);
                    }

                    if (argLocation != null)
                    {
                        context.PseudoBindingInfo = pseudoBinding;
                        switch (pseudoBinding.InfoType)
                        {
                            case PseudoBindingInfoType.PseudoBindingSucceed:
                                result = GetArgumentCompletionResultsWithSuccessfulPseudoBinding(context, argLocation, commandAst);
                                break;
                            case PseudoBindingInfoType.PseudoBindingFail:
                                result = GetArgumentCompletionResultsWithFailedPseudoBinding(context, argLocation, commandAst);
                                break;
                        }

                        parsedArgumentsProvidesMatch = true;
                    }
                }

                if (!parsedArgumentsProvidesMatch)
                {
                    int index = 0;
                    CommandElementAst prevElem = null;
                    if (expressionAst != null)
                    {
                        foreach (CommandElementAst eleAst in commandAst.CommandElements)
                        {
                            if (eleAst.GetHashCode() == expressionAst.GetHashCode())
                                break;
                            prevElem = eleAst;
                            index++;
                        }
                    }
                    else
                    {
                        var token = context.TokenAtCursor ?? context.TokenBeforeCursor;
                        foreach (CommandElementAst eleAst in commandAst.CommandElements)
                        {
                            if (eleAst.Extent.StartOffset > token.Extent.EndOffset)
                                break;
                            prevElem = eleAst;
                            index++;
                        }
                    }

                    // positional argument with position 0
                    if (index == 1)
                    {
                        CompletePositionalArgument(
                            pseudoBinding.CommandName,
                            commandAst,
                            context,
                            result,
                            pseudoBinding.UnboundParameters,
                            pseudoBinding.DefaultParameterSetFlag,
                            uint.MaxValue,
                            0);
                    }
                    else
                    {
                        if (prevElem is CommandParameterAst && ((CommandParameterAst)prevElem).Argument == null)
                        {
                            var paramName = ((CommandParameterAst)prevElem).ParameterName;
                            var pattern = WildcardPattern.Get(paramName + "*", WildcardOptions.IgnoreCase);
                            foreach (MergedCompiledCommandParameter param in pseudoBinding.UnboundParameters)
                            {
                                if (pattern.IsMatch(param.Parameter.Name))
                                {
                                    ProcessParameter(pseudoBinding.CommandName, commandAst, context, result, param);
                                    break;
                                }

                                var isAliasMatch = false;
                                foreach (string alias in param.Parameter.Aliases)
                                {
                                    if (pattern.IsMatch(alias))
                                    {
                                        isAliasMatch = true;
                                        ProcessParameter(pseudoBinding.CommandName, commandAst, context, result, param);
                                        break;
                                    }
                                }

                                if (isAliasMatch)
                                    break;
                            }
                        }
                    }
                }
            } while (false);

            // Indicate if the current argument completion falls into those pre-defined cases and
            // has been processed already.
            bool hasBeenProcessed = false;
            if (result.Count > 0 && result[result.Count - 1].Equals(CompletionResult.Null))
            {
                result.RemoveAt(result.Count - 1);
                hasBeenProcessed = true;

                if (result.Count > 0)
                    return result;
            }

            // Handle some special cases such as:
            //    & "get-comm<tab> --> & "Get-Command"
            //    & "sa<tab>       --> & ".\sa[v].txt"
            if (expressionAst == null && !hasBeenProcessed &&
                commandAst.CommandElements.Count == 1 &&
                commandAst.InvocationOperator != TokenKind.Unknown &&
                context.WordToComplete != string.Empty)
            {
                // Use literal path after Ampersand
                var tryCmdletCompletion = false;
                var clearLiteralPathsKey = TurnOnLiteralPathOption(context);

                if (context.WordToComplete.Contains('-'))
                {
                    tryCmdletCompletion = true;
                }

                try
                {
                    var fileCompletionResults = new List<CompletionResult>(CompleteFilename(context));
                    if (tryCmdletCompletion)
                    {
                        // It's actually command name completion, other than argument completion
                        var cmdletCompletionResults = CompleteCommand(context);
                        if (cmdletCompletionResults != null && cmdletCompletionResults.Count > 0)
                        {
                            fileCompletionResults.AddRange(cmdletCompletionResults);
                        }
                    }

                    return fileCompletionResults;
                }
                finally
                {
                    if (clearLiteralPathsKey)
                        context.Options.Remove("LiteralPaths");
                }
            }

            if (expressionAst is StringConstantExpressionAst)
            {
                var pathAst = (StringConstantExpressionAst)expressionAst;
                // Handle static member completion: echo [int]::<tab>
                var shareMatch = Regex.Match(pathAst.Value, @"^(\[[\w\d\.]+\]::[\w\d\*]*)$");
                if (shareMatch.Success)
                {
                    int fakeReplacementIndex, fakeReplacementLength;
                    var input = shareMatch.Groups[1].Value;
                    var completionParameters = CommandCompletion.MapStringInputToParsedInput(input, input.Length);
                    var completionAnalysis = new CompletionAnalysis(completionParameters.Item1, completionParameters.Item2, completionParameters.Item3, context.Options);
                    var ret = completionAnalysis.GetResults(
                        context.Helper.CurrentPowerShell,
                        out fakeReplacementIndex,
                        out fakeReplacementLength);

                    if (ret != null && ret.Count > 0)
                    {
                        string prefix = string.Concat(TokenKind.LParen.Text(), input.AsSpan(0, fakeReplacementIndex));
                        foreach (CompletionResult entry in ret)
                        {
                            string completionText = prefix + entry.CompletionText;
                            if (entry.ResultType.Equals(CompletionResultType.Property))
                                completionText += TokenKind.RParen.Text();
                            result.Add(new CompletionResult(completionText, entry.ListItemText, entry.ResultType,
                                                            entry.ToolTip));
                        }

                        return result;
                    }
                }

                // Handle member completion with wildcard: echo $a.*<tab>
                if (pathAst.Value.Contains('*') && secondToLastMemberAst != null &&
                    secondToLastMemberAst.Extent.EndLineNumber == pathAst.Extent.StartLineNumber &&
                    secondToLastMemberAst.Extent.EndColumnNumber == pathAst.Extent.StartColumnNumber)
                {
                    var memberName = pathAst.Value.EndsWith('*')
                                         ? pathAst.Value
                                         : pathAst.Value + "*";
                    var targetExpr = secondToLastMemberAst.Expression;
                    if (IsSplattedVariable(targetExpr))
                    {
                        // It's splatted variable, and the member completion is not useful
                        return result;
                    }

                    var memberAst = secondToLastMemberAst.Member as StringConstantExpressionAst;
                    if (memberAst != null)
                    {
                        memberName = memberAst.Value + memberName;
                    }

                    CompleteMemberHelper(false, memberName, targetExpr, context, result);
                    if (result.Count > 0)
                    {
                        context.ReplacementIndex =
                            ((InternalScriptPosition)secondToLastMemberAst.Expression.Extent.EndScriptPosition).Offset + 1;
                        if (memberAst != null)
                            context.ReplacementLength += memberAst.Value.Length;
                        return result;
                    }
                }

                // Treat it as the file name completion
                // Handle this scenario: & 'c:\a b'\<tab>
                string fileName = pathAst.Value;
                if (commandAst.InvocationOperator != TokenKind.Unknown && fileName.AsSpan().IndexOfAny('\\', '/') == 0 &&
                    commandAst.CommandElements.Count == 2 && commandAst.CommandElements[0] is StringConstantExpressionAst &&
                    commandAst.CommandElements[0].Extent.EndLineNumber == expressionAst.Extent.StartLineNumber &&
                    commandAst.CommandElements[0].Extent.EndColumnNumber == expressionAst.Extent.StartColumnNumber)
                {
                    if (pseudoBinding != null)
                    {
                        // CommandElements[0] is resolved to a command
                        return result;
                    }
                    else
                    {
                        var constantAst = (StringConstantExpressionAst)commandAst.CommandElements[0];
                        fileName = constantAst.Value + fileName;
                        context.ReplacementIndex = ((InternalScriptPosition)constantAst.Extent.StartScriptPosition).Offset;
                        context.ReplacementLength += ((InternalScriptPosition)constantAst.Extent.EndScriptPosition).Offset - context.ReplacementIndex;
                        context.WordToComplete = fileName;
                        // commandAst.InvocationOperator != TokenKind.Unknown, so we should use literal path
                        var clearLiteralPathKey = TurnOnLiteralPathOption(context);

                        try
                        {
                            return new List<CompletionResult>(CompleteFilename(context));
                        }
                        finally
                        {
                            if (clearLiteralPathKey)
                                context.Options.Remove("LiteralPaths");
                        }
                    }
                }
            }

            // The default argument completion: file path completion, command name completion('WordToComplete' is not empty and contains a dash).
            // If the current argument completion has been process already, we don't go through the default argument completion anymore.
            if (!hasBeenProcessed)
            {
                var commandName = commandAst.GetCommandName();
                var customCompleter = GetCustomArgumentCompleter(
                    "NativeArgumentCompleters",
                    new[] { commandName, Path.GetFileName(commandName), Path.GetFileNameWithoutExtension(commandName) },
                    context);
                if (customCompleter != null)
                {
                    if (InvokeScriptArgumentCompleter(
                        customCompleter,
                        new object[] { context.WordToComplete, commandAst, context.CursorPosition.Offset },
                        result))
                    {
                        return result;
                    }
                }

                var clearLiteralPathKey = false;
                if (pseudoBinding == null)
                {
                    // the command could be a native command such as notepad.exe, we use literal path in this case
                    clearLiteralPathKey = TurnOnLiteralPathOption(context);
                }

                try
                {
                    result = new List<CompletionResult>(CompleteFilename(context));
                }
                finally
                {
                    if (clearLiteralPathKey)
                        context.Options.Remove("LiteralPaths");
                }

                // The word to complete contains a dash and it's not the first character. We try command names in this case.
                if (context.WordToComplete.IndexOf('-') > 0)
                {
                    var commandResults = CompleteCommand(context);
                    if (commandResults != null)
                        result.AddRange(commandResults);
                }
            }

            return result;
        }

        internal static string ConcatenateStringPathArguments(CommandElementAst stringAst, string partialPath, CompletionContext completionContext)
        {
            var constantPathAst = stringAst as StringConstantExpressionAst;
            if (constantPathAst != null)
            {
                string quote = string.Empty;
                switch (constantPathAst.StringConstantType)
                {
                    case StringConstantType.SingleQuoted:
                        quote = "'";
                        break;
                    case StringConstantType.DoubleQuoted:
                        quote = "\"";
                        break;
                    default:
                        break;
                }

                return quote + constantPathAst.Value + partialPath + quote;
            }
            else
            {
                var expandablePathAst = stringAst as ExpandableStringExpressionAst;
                string fullPath = null;
                if (expandablePathAst != null &&
                    IsPathSafelyExpandable(expandableStringAst: expandablePathAst,
                                           extraText: partialPath,
                                           executionContext: completionContext.ExecutionContext,
                                           expandedString: out fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }

        /// <summary>
        /// Get the argument completion results when the pseudo binding was not successful.
        /// </summary>
        private static List<CompletionResult> GetArgumentCompletionResultsWithFailedPseudoBinding(
            CompletionContext context,
            ArgumentLocation argLocation,
            CommandAst commandAst)
        {
            List<CompletionResult> result = new List<CompletionResult>();

            PseudoBindingInfo bindingInfo = context.PseudoBindingInfo;
            if (argLocation.IsPositional)
            {
                CompletePositionalArgument(
                    bindingInfo.CommandName,
                    commandAst,
                    context,
                    result,
                    bindingInfo.UnboundParameters,
                    bindingInfo.DefaultParameterSetFlag,
                    uint.MaxValue,
                    argLocation.Position);
            }
            else
            {
                string paramName = argLocation.Argument.ParameterName;
                WildcardPattern pattern = WildcardPattern.Get(paramName + "*", WildcardOptions.IgnoreCase);
                foreach (MergedCompiledCommandParameter param in bindingInfo.UnboundParameters)
                {
                    if (pattern.IsMatch(param.Parameter.Name))
                    {
                        ProcessParameter(bindingInfo.CommandName, commandAst, context, result, param);
                        break;
                    }

                    bool isAliasMatch = false;
                    foreach (string alias in param.Parameter.Aliases)
                    {
                        if (pattern.IsMatch(alias))
                        {
                            isAliasMatch = true;
                            ProcessParameter(bindingInfo.CommandName, commandAst, context, result, param);
                            break;
                        }
                    }

                    if (isAliasMatch)
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Get the argument completion results when the pseudo binding was successful.
        /// </summary>
        private static List<CompletionResult> GetArgumentCompletionResultsWithSuccessfulPseudoBinding(
            CompletionContext context,
            ArgumentLocation argLocation,
            CommandAst commandAst)
        {
            PseudoBindingInfo bindingInfo = context.PseudoBindingInfo;
            Diagnostics.Assert(bindingInfo.InfoType.Equals(PseudoBindingInfoType.PseudoBindingSucceed), "Caller needs to make sure the pseudo binding was successful");
            List<CompletionResult> result = new List<CompletionResult>();

            if (argLocation.IsPositional && argLocation.Argument == null)
            {
                AstPair lastPositionalArg;
                AstParameterArgumentPair targetPositionalArg =
                    FindTargetPositionalArgument(
                        bindingInfo.AllParsedArguments,
                        argLocation.Position,
                        out lastPositionalArg);

                if (targetPositionalArg != null)
                    argLocation.Argument = targetPositionalArg;
                else
                {
                    if (lastPositionalArg != null)
                    {
                        bool lastPositionalGetBound = false;
                        Collection<string> parameterNames = new Collection<string>();

                        foreach (KeyValuePair<string, AstParameterArgumentPair> entry in bindingInfo.BoundArguments)
                        {
                            // positional argument
                            if (!entry.Value.ParameterSpecified)
                            {
                                var arg = (AstPair)entry.Value;
                                if (arg.Argument.GetHashCode() == lastPositionalArg.Argument.GetHashCode())
                                {
                                    lastPositionalGetBound = true;
                                    break;
                                }
                            }
                            else if (entry.Value.ParameterArgumentType.Equals(AstParameterArgumentType.AstArray))
                            {
                                // check if the positional argument would be bound to a "ValueFromRemainingArgument" parameter
                                var arg = (AstArrayPair)entry.Value;
                                if (arg.Argument.Any(exp => exp.GetHashCode() == lastPositionalArg.Argument.GetHashCode()))
                                {
                                    parameterNames.Add(entry.Key);
                                }
                            }
                        }

                        if (parameterNames.Count > 0)
                        {
                            // parameter should be in BoundParameters
                            foreach (string param in parameterNames)
                            {
                                MergedCompiledCommandParameter parameter = bindingInfo.BoundParameters[param];
                                ProcessParameter(bindingInfo.CommandName, commandAst, context, result, parameter, bindingInfo.BoundArguments);
                            }

                            return result;
                        }
                        else if (!lastPositionalGetBound)
                        {
                            // last positional argument was not bound, then positional argument 'tab' wants to
                            // expand will not get bound either
                            return result;
                        }
                    }

                    CompletePositionalArgument(
                        bindingInfo.CommandName,
                        commandAst,
                        context,
                        result,
                        bindingInfo.UnboundParameters,
                        bindingInfo.DefaultParameterSetFlag,
                        bindingInfo.ValidParameterSetsFlags,
                        argLocation.Position,
                        bindingInfo.BoundArguments);

                    return result;
                }
            }

            if (argLocation.Argument != null)
            {
                Collection<string> parameterNames = new Collection<string>();
                foreach (KeyValuePair<string, AstParameterArgumentPair> entry in bindingInfo.BoundArguments)
                {
                    if (entry.Value.ParameterArgumentType.Equals(AstParameterArgumentType.PipeObject))
                        continue;

                    if (entry.Value.ParameterArgumentType.Equals(AstParameterArgumentType.AstArray) && !argLocation.Argument.ParameterSpecified)
                    {
                        var arrayArg = (AstArrayPair)entry.Value;
                        var target = (AstPair)argLocation.Argument;
                        if (arrayArg.Argument.Any(exp => exp.GetHashCode() == target.Argument.GetHashCode()))
                        {
                            parameterNames.Add(entry.Key);
                        }
                    }
                    else if (entry.Value.GetHashCode() == argLocation.Argument.GetHashCode())
                    {
                        parameterNames.Add(entry.Key);
                    }
                }

                if (parameterNames.Count > 0)
                {
                    // those parameters should be in BoundParameters
                    foreach (string param in parameterNames)
                    {
                        MergedCompiledCommandParameter parameter = bindingInfo.BoundParameters[param];
                        ProcessParameter(bindingInfo.CommandName, commandAst, context, result, parameter, bindingInfo.BoundArguments);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Get the positional argument completion results based on the position it's in the command line.
        /// </summary>
        private static void CompletePositionalArgument(
            string commandName,
            CommandAst commandAst,
            CompletionContext context,
            List<CompletionResult> result,
            IEnumerable<MergedCompiledCommandParameter> parameters,
            uint defaultParameterSetFlag,
            uint validParameterSetFlags,
            int position,
            Dictionary<string, AstParameterArgumentPair> boundArguments = null)
        {
            bool isProcessedAsPositional = false;
            bool isDefaultParameterSetValid = defaultParameterSetFlag != 0 &&
                                              (defaultParameterSetFlag & validParameterSetFlags) != 0;
            MergedCompiledCommandParameter positionalParam = null;

            MergedCompiledCommandParameter bestMatchParam = null;
            ParameterSetSpecificMetadata bestMatchSet = null;

            // Finds the parameter with the position closest to the specified position
            foreach (MergedCompiledCommandParameter param in parameters)
            {
                bool isInParameterSet = (param.Parameter.ParameterSetFlags & validParameterSetFlags) != 0 || param.Parameter.IsInAllSets;
                if (!isInParameterSet)
                {
                    continue;
                }

                var parameterSetDataCollection = param.Parameter.GetMatchingParameterSetData(validParameterSetFlags);

                foreach (ParameterSetSpecificMetadata parameterSetData in parameterSetDataCollection)
                {
                    // in the first pass, we skip the remaining argument ones
                    if (parameterSetData.ValueFromRemainingArguments)
                    {
                        continue;
                    }

                    // Check the position
                    int positionInParameterSet = parameterSetData.Position;

                    if (positionInParameterSet < position)
                    {
                        // The parameter is not positional (position == int.MinValue), or its position is lower than what we want.
                        continue;
                    }

                    if (bestMatchSet is null
                        || bestMatchSet.Position > positionInParameterSet
                        || (isDefaultParameterSetValid && positionInParameterSet == bestMatchSet.Position && defaultParameterSetFlag == parameterSetData.ParameterSetFlag))
                    {
                        bestMatchParam = param;
                        bestMatchSet = parameterSetData;
                        if (positionInParameterSet == position)
                        {
                            break;
                        }
                    }
                }
            }

            if (bestMatchParam is not null)
            {
                if (isDefaultParameterSetValid)
                {
                    if (bestMatchSet.ParameterSetFlag == defaultParameterSetFlag)
                    {
                        ProcessParameter(commandName, commandAst, context, result, bestMatchParam, boundArguments);
                        isProcessedAsPositional = result.Count > 0;
                    }
                    else
                    {
                        positionalParam ??= bestMatchParam;
                    }
                }
                else
                {
                    isProcessedAsPositional = true;
                    ProcessParameter(commandName, commandAst, context, result, bestMatchParam, boundArguments);
                }
            }

            if (!isProcessedAsPositional && positionalParam != null)
            {
                isProcessedAsPositional = true;
                ProcessParameter(commandName, commandAst, context, result, positionalParam, boundArguments);
            }

            if (!isProcessedAsPositional)
            {
                foreach (MergedCompiledCommandParameter param in parameters)
                {
                    bool isInParameterSet = (param.Parameter.ParameterSetFlags & validParameterSetFlags) != 0 || param.Parameter.IsInAllSets;
                    if (!isInParameterSet)
                        continue;

                    var parameterSetDataCollection = param.Parameter.GetMatchingParameterSetData(validParameterSetFlags);
                    foreach (ParameterSetSpecificMetadata parameterSetData in parameterSetDataCollection)
                    {
                        // in the second pass, we check the remaining argument ones
                        if (parameterSetData.ValueFromRemainingArguments)
                        {
                            ProcessParameter(commandName, commandAst, context, result, param, boundArguments);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Process a parameter to get the argument completion results.
        /// </summary>
        /// <remarks>
        /// If the argument completion falls into these pre-defined cases:
        ///   1. The matching parameter is declared with ValidateSetAttribute
        ///   2. The matching parameter is of type Enum
        ///   3. The matching parameter is of type SwitchParameter
        ///   4. Falls into the native command argument completion
        /// a null instance of CompletionResult is added to the end of the
        /// "result" list, to indicate that this particular argument completion
        /// has been processed already. If the "result" list is still empty, we
        /// will not go through the default argument completion steps anymore.
        /// </remarks>
        private static void ProcessParameter(
            string commandName,
            CommandAst commandAst,
            CompletionContext context,
            List<CompletionResult> result,
            MergedCompiledCommandParameter parameter,
            Dictionary<string, AstParameterArgumentPair> boundArguments = null)
        {
            CompletionResult fullMatch = null;
            Type parameterType = GetEffectiveParameterType(parameter.Parameter.Type);

            if (parameterType.IsArray)
            {
                parameterType = parameterType.GetElementType();
            }

            foreach (ValidateArgumentsAttribute att in parameter.Parameter.ValidationAttributes)
            {
                if (att is ValidateSetAttribute setAtt)
                {
                    RemoveLastNullCompletionResult(result);

                    string wordToComplete = context.WordToComplete ?? string.Empty;
                    string quote = HandleDoubleAndSingleQuote(ref wordToComplete);

                    var pattern = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase);
                    var setList = new List<string>();

                    foreach (string value in setAtt.ValidValues)
                    {
                        if (value == string.Empty)
                        {
                            continue;
                        }

                        if (wordToComplete.Equals(value, StringComparison.OrdinalIgnoreCase))
                        {
                            string completionText = quote == string.Empty ? value : quote + value + quote;
                            fullMatch = new CompletionResult(completionText, value, CompletionResultType.ParameterValue, value);
                            continue;
                        }

                        if (pattern.IsMatch(value))
                        {
                            setList.Add(value);
                        }
                    }

                    if (fullMatch != null)
                    {
                        result.Add(fullMatch);
                    }

                    setList.Sort();
                    foreach (string entry in setList)
                    {
                        string realEntry = entry;
                        string completionText = entry;
                        if (quote == string.Empty)
                        {
                            if (CompletionRequiresQuotes(entry, false))
                            {
                                realEntry = CodeGeneration.EscapeSingleQuotedStringContent(entry);
                                completionText = "'" + realEntry + "'";
                            }
                        }
                        else
                        {
                            if (quote.Equals("'", StringComparison.OrdinalIgnoreCase))
                            {
                                realEntry = CodeGeneration.EscapeSingleQuotedStringContent(entry);
                            }

                            completionText = quote + realEntry + quote;
                        }

                        result.Add(new CompletionResult(completionText, entry, CompletionResultType.ParameterValue, entry));
                    }

                    result.Add(CompletionResult.Null);
                    return;
                }
            }

            if (parameterType.IsEnum)
            {
                RemoveLastNullCompletionResult(result);

                IEnumerable enumValues = LanguagePrimitives.EnumSingleTypeConverter.GetEnumValues(parameterType);

                // Exclude values not accepted by ValidateRange-attributes
                foreach (ValidateArgumentsAttribute att in parameter.Parameter.ValidationAttributes)
                {
                    if (att is ValidateRangeAttribute rangeAtt)
                    {
                        enumValues = rangeAtt.GetValidatedElements(enumValues);
                    }
                }

                string wordToComplete = context.WordToComplete ?? string.Empty;
                string quote = HandleDoubleAndSingleQuote(ref wordToComplete);

                var pattern = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase);
                var enumList = new List<string>();

                foreach (Enum value in enumValues)
                {
                    string name = value.ToString();
                    if (wordToComplete.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        string completionText = quote == string.Empty ? name : quote + name + quote;
                        fullMatch = new CompletionResult(completionText, name, CompletionResultType.ParameterValue, name);
                        continue;
                    }

                    if (pattern.IsMatch(name))
                    {
                        enumList.Add(name);
                    }
                }

                if (fullMatch != null)
                {
                    result.Add(fullMatch);
                }

                enumList.Sort();
                result.AddRange(from entry in enumList
                                let completionText = quote == string.Empty ? entry : quote + entry + quote
                                select new CompletionResult(completionText, entry, CompletionResultType.ParameterValue, entry));

                result.Add(CompletionResult.Null);
                return;
            }

            if (parameterType.Equals(typeof(SwitchParameter)))
            {
                RemoveLastNullCompletionResult(result);

                if (context.WordToComplete == string.Empty || context.WordToComplete.Equals("$", StringComparison.Ordinal))
                {
                    result.Add(new CompletionResult("$true", "$true", CompletionResultType.ParameterValue, "$true"));
                    result.Add(new CompletionResult("$false", "$false", CompletionResultType.ParameterValue, "$false"));
                }

                result.Add(CompletionResult.Null);
                return;
            }

            NativeCommandArgumentCompletion(commandName, parameter.Parameter, result, commandAst, context, boundArguments);
        }

        private static IEnumerable<PSTypeName> NativeCommandArgumentCompletion_InferTypesOfArgument(
            Dictionary<string, AstParameterArgumentPair> boundArguments,
            CommandAst commandAst,
            CompletionContext context,
            string parameterName)
        {
            if (boundArguments == null)
            {
                yield break;
            }

            AstParameterArgumentPair astParameterArgumentPair;
            if (!boundArguments.TryGetValue(parameterName, out astParameterArgumentPair))
            {
                yield break;
            }

            Ast argumentAst = null;
            switch (astParameterArgumentPair.ParameterArgumentType)
            {
                case AstParameterArgumentType.AstPair:
                    {
                        AstPair astPair = (AstPair)astParameterArgumentPair;
                        argumentAst = astPair.Argument;
                    }

                    break;

                case AstParameterArgumentType.PipeObject:
                    {
                        var pipelineAst = commandAst.Parent as PipelineAst;
                        if (pipelineAst != null)
                        {
                            int i;
                            for (i = 0; i < pipelineAst.PipelineElements.Count; i++)
                            {
                                if (pipelineAst.PipelineElements[i] == commandAst)
                                    break;
                            }

                            if (i != 0)
                            {
                                argumentAst = pipelineAst.PipelineElements[i - 1];
                            }
                        }
                    }

                    break;

                default:
                    break;
            }

            if (argumentAst == null)
            {
                yield break;
            }

            ExpressionAst argumentExpressionAst = argumentAst as ExpressionAst;
            if (argumentExpressionAst == null)
            {
                CommandExpressionAst argumentCommandExpressionAst = argumentAst as CommandExpressionAst;
                if (argumentCommandExpressionAst != null)
                {
                    argumentExpressionAst = argumentCommandExpressionAst.Expression;
                }
            }

            object argumentValue;
            if (argumentExpressionAst != null && SafeExprEvaluator.TrySafeEval(argumentExpressionAst, context.ExecutionContext, out argumentValue))
            {
                if (argumentValue != null)
                {
                    IEnumerable enumerable = LanguagePrimitives.GetEnumerable(argumentValue) ??
                                             new object[] { argumentValue };
                    foreach (var element in enumerable)
                    {
                        if (element == null)
                        {
                            continue;
                        }

                        PSObject pso = PSObject.AsPSObject(element);
                        if ((pso.TypeNames.Count > 0) && (!(pso.TypeNames[0].Equals(pso.BaseObject.GetType().FullName, StringComparison.OrdinalIgnoreCase))))
                        {
                            yield return new PSTypeName(pso.TypeNames[0]);
                        }

                        if (pso.BaseObject is not PSCustomObject)
                        {
                            yield return new PSTypeName(pso.BaseObject.GetType());
                        }
                    }

                    yield break;
                }
            }

            foreach (PSTypeName typeName in AstTypeInference.InferTypeOf(argumentAst, context.TypeInferenceContext, TypeInferenceRuntimePermissions.AllowSafeEval))
            {
                yield return typeName;
            }
        }

        internal static IList<string> NativeCommandArgumentCompletion_ExtractSecondaryArgument(
            Dictionary<string, AstParameterArgumentPair> boundArguments,
            string parameterName)
        {
            List<string> result = new List<string>();

            if (boundArguments == null)
            {
                return result;
            }

            AstParameterArgumentPair argumentValue;
            if (!boundArguments.TryGetValue(parameterName, out argumentValue))
            {
                return result;
            }

            switch (argumentValue.ParameterArgumentType)
            {
                case AstParameterArgumentType.AstPair:
                    {
                        var value = (AstPair)argumentValue;
                        if (value.Argument is StringConstantExpressionAst)
                        {
                            var argument = (StringConstantExpressionAst)value.Argument;
                            result.Add(argument.Value);
                        }
                        else if (value.Argument is ArrayLiteralAst)
                        {
                            var argument = (ArrayLiteralAst)value.Argument;
                            foreach (ExpressionAst entry in argument.Elements)
                            {
                                var entryAsString = entry as StringConstantExpressionAst;
                                if (entryAsString != null)
                                {
                                    result.Add(entryAsString.Value);
                                }
                                else
                                {
                                    result.Clear();
                                    break;
                                }
                            }
                        }

                        break;
                    }
                case AstParameterArgumentType.AstArray:
                    {
                        var value = (AstArrayPair)argumentValue;
                        var argument = value.Argument;

                        foreach (ExpressionAst entry in argument)
                        {
                            var entryAsString = entry as StringConstantExpressionAst;
                            if (entryAsString != null)
                            {
                                result.Add(entryAsString.Value);
                            }
                            else
                            {
                                result.Clear();
                                break;
                            }
                        }

                        break;
                    }
                default:
                    break;
            }

            return result;
        }

        private static void NativeCommandArgumentCompletion(
            string commandName,
            CompiledCommandParameter parameter,
            List<CompletionResult> result,
            CommandAst commandAst,
            CompletionContext context,
            Dictionary<string, AstParameterArgumentPair> boundArguments = null)
        {
            string parameterName = parameter.Name;

            // Fall back to the commandAst command name if a command name is not found. This can be caused by a script block or AST with the matching function definition being passed to CompleteInput
            // This allows for editors and other tools using CompleteInput with Script/AST definitions to get values from RegisteredArgumentCompleters to better match the console experience.
            // See issue https://github.com/PowerShell/PowerShell/issues/10567
            string actualCommandName = string.IsNullOrEmpty(commandName)
                        ? commandAst.GetCommandName()
                        : commandName;

            if (string.IsNullOrEmpty(actualCommandName))
            {
                return;
            }

            string parameterFullName = $"{actualCommandName}:{parameterName}";

            ScriptBlock customCompleter = GetCustomArgumentCompleter(
                "CustomArgumentCompleters",
                new[] { parameterFullName, parameterName },
                context);

            if (customCompleter != null)
            {
                if (InvokeScriptArgumentCompleter(
                    customCompleter,
                    commandName, parameterName, context.WordToComplete, commandAst, context,
                    result))
                {
                    return;
                }
            }

            var argumentCompleterAttribute = parameter.CompiledAttributes.OfType<ArgumentCompleterAttribute>().FirstOrDefault();
            if (argumentCompleterAttribute != null)
            {
                try
                {
                    var completer = argumentCompleterAttribute.CreateArgumentCompleter();

                    if (completer != null)
                    {
                        var customResults = completer.CompleteArgument(commandName, parameterName,
                            context.WordToComplete, commandAst, GetBoundArgumentsAsHashtable(context));
                        if (customResults != null)
                        {
                            result.AddRange(customResults);
                            result.Add(CompletionResult.Null);
                            return;
                        }
                    }
                    else
                    {
                        if (InvokeScriptArgumentCompleter(
                            argumentCompleterAttribute.ScriptBlock,
                            commandName, parameterName, context.WordToComplete, commandAst, context,
                            result))
                        {
                            return;
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            var argumentCompletionsAttribute = parameter.CompiledAttributes.OfType<ArgumentCompletionsAttribute>().FirstOrDefault();
            if (argumentCompletionsAttribute != null)
            {
                var customResults = argumentCompletionsAttribute.CompleteArgument(commandName, parameterName,
                        context.WordToComplete, commandAst, GetBoundArgumentsAsHashtable(context));
                if (customResults != null)
                {
                    result.AddRange(customResults);
                    result.Add(CompletionResult.Null);
                    return;
                }
            }

            switch (commandName)
            {
                case "Get-Command":
                    {
                        if (parameterName.Equals("Module", StringComparison.OrdinalIgnoreCase))
                        {
                            NativeCompletionGetCommand(context, /* moduleName: */ null, parameterName, result);
                            break;
                        }

                        if (parameterName.Equals("Name", StringComparison.OrdinalIgnoreCase))
                        {
                            var moduleNames = NativeCommandArgumentCompletion_ExtractSecondaryArgument(boundArguments, "Module");

                            if (moduleNames.Count > 0)
                            {
                                foreach (string module in moduleNames)
                                {
                                    NativeCompletionGetCommand(context, module, parameterName, result);
                                }
                            }
                            else
                            {
                                NativeCompletionGetCommand(context, /* moduleName: */ null, parameterName, result);
                            }

                            break;
                        }

                        if (parameterName.Equals("ParameterType", StringComparison.OrdinalIgnoreCase))
                        {
                            NativeCompletionTypeName(context, result);
                            break;
                        }

                        break;
                    }
                case "Show-Command":
                    {
                        NativeCompletionGetHelpCommand(context, parameterName, /* isHelpRelated: */ false, result);
                        break;
                    }
                case "help":
                case "Get-Help":
                    {
                        NativeCompletionGetHelpCommand(context, parameterName, /* isHelpRelated: */ true, result);
                        break;
                    }
                case "Save-Help":
                    {
                        if (parameterName.Equals("Module", StringComparison.OrdinalIgnoreCase))
                        {
                            CompleteModule(context, result);
                        }
                        break;
                    }
                case "Update-Help":
                    {
                        if (parameterName.Equals("Module", StringComparison.OrdinalIgnoreCase))
                        {
                            CompleteModule(context, result);
                        }
                        break;
                    }
                case "Invoke-Expression":
                    {
                        if (parameterName.Equals("Command", StringComparison.OrdinalIgnoreCase))
                        {
                            var commandResults = CompleteCommand(context);
                            if (commandResults != null)
                                result.AddRange(commandResults);
                        }

                        break;
                    }
                case "Clear-EventLog":
                case "Get-EventLog":
                case "Limit-EventLog":
                case "Remove-EventLog":
                case "Write-EventLog":
                    {
                        NativeCompletionEventLogCommands(context, parameterName, result);
                        break;
                    }
                case "Get-Job":
                case "Receive-Job":
                case "Remove-Job":
                case "Stop-Job":
                case "Wait-Job":
                case "Suspend-Job":
                case "Resume-Job":
                    {
                        NativeCompletionJobCommands(context, parameterName, result);
                        break;
                    }
                case "Disable-ScheduledJob":
                case "Enable-ScheduledJob":
                case "Get-ScheduledJob":
                case "Unregister-ScheduledJob":
                    {
                        NativeCompletionScheduledJobCommands(context, parameterName, result);
                        break;
                    }
                case "Get-Module":
                    {
                        bool loadedModulesOnly = boundArguments == null || !boundArguments.ContainsKey("ListAvailable");
                        bool skipEditionCheck = !loadedModulesOnly && boundArguments.ContainsKey("SkipEditionCheck");
                        NativeCompletionModuleCommands(context, parameterName, result, loadedModulesOnly, skipEditionCheck: skipEditionCheck);
                        break;
                    }
                case "Remove-Module":
                    {
                        NativeCompletionModuleCommands(context, parameterName, result, loadedModulesOnly: true);
                        break;
                    }
                case "Import-Module":
                    {
                        bool skipEditionCheck = boundArguments != null && boundArguments.ContainsKey("SkipEditionCheck");
                        NativeCompletionModuleCommands(context, parameterName, result, isImportModule: true, skipEditionCheck: skipEditionCheck);
                        break;
                    }
                case "Debug-Process":
                case "Get-Process":
                case "Stop-Process":
                case "Wait-Process":
                case "Enter-PSHostProcess":
                    {
                        NativeCompletionProcessCommands(context, parameterName, result);
                        break;
                    }
                case "Get-PSDrive":
                case "Remove-PSDrive":
                    {
                        if (parameterName.Equals("PSProvider", StringComparison.OrdinalIgnoreCase))
                        {
                            NativeCompletionProviderCommands(context, parameterName, result);
                        }
                        else if (parameterName.Equals("Name", StringComparison.OrdinalIgnoreCase))
                        {
                            var psProviders = NativeCommandArgumentCompletion_ExtractSecondaryArgument(boundArguments, "PSProvider");
                            if (psProviders.Count > 0)
                            {
                                foreach (string psProvider in psProviders)
                                {
                                    NativeCompletionDriveCommands(context, psProvider, parameterName, result);
                                }
                            }
                            else
                            {
                                NativeCompletionDriveCommands(context, /* psProvider: */ null, parameterName, result);
                            }
                        }

                        break;
                    }
                case "New-PSDrive":
                    {
                        NativeCompletionProviderCommands(context, parameterName, result);
                        break;
                    }
                case "Get-PSProvider":
                    {
                        NativeCompletionProviderCommands(context, parameterName, result);
                        break;
                    }
                case "Get-Service":
                case "Start-Service":
                case "Restart-Service":
                case "Resume-Service":
                case "Set-Service":
                case "Stop-Service":
                case "Suspend-Service":
                    {
                        NativeCompletionServiceCommands(context, parameterName, result);
                        break;
                    }
                case "Clear-Variable":
                case "Get-Variable":
                case "Remove-Variable":
                case "Set-Variable":
                    {
                        NativeCompletionVariableCommands(context, parameterName, result);
                        break;
                    }
                case "Get-Alias":
                    {
                        NativeCompletionAliasCommands(context, parameterName, result);
                        break;
                    }
                case "Get-TraceSource":
                case "Set-TraceSource":
                case "Trace-Command":
                    {
                        NativeCompletionTraceSourceCommands(context, parameterName, result);
                        break;
                    }
                case "Push-Location":
                case "Set-Location":
                    {
                        NativeCompletionSetLocationCommand(context, parameterName, result);
                        break;
                    }
                case "Move-Item":
                case "Copy-Item":
                    {
                        NativeCompletionCopyMoveItemCommand(context, parameterName, result);
                        break;
                    }
                case "New-Item":
                    {
                        NativeCompletionNewItemCommand(context, parameterName, result);
                        break;
                    }
                case "ForEach-Object":
                    {
                        if (parameterName.Equals("MemberName", StringComparison.OrdinalIgnoreCase))
                        {
                            NativeCompletionMemberName(context, result, commandAst, boundArguments?[parameterName], propertiesOnly: false);
                        }

                        break;
                    }
                case "Group-Object":
                case "Measure-Object":
                case "Sort-Object":
                case "Where-Object":
                    {
                        if (parameterName.Equals("Property", StringComparison.OrdinalIgnoreCase))
                        {
                            NativeCompletionMemberName(context, result, commandAst, boundArguments?[parameterName]);
                        }
                        else if (parameterName.Equals("Value", StringComparison.OrdinalIgnoreCase)
                            && boundArguments?["Property"] is AstPair pair && pair.Argument is StringConstantExpressionAst stringAst)
                        {
                            NativeCompletionMemberValue(context, result, commandAst, stringAst.Value);
                        }

                        break;
                    }
                case "Format-Custom":
                case "Format-List":
                case "Format-Table":
                case "Format-Wide":
                    {
                        if (parameterName.Equals("Property", StringComparison.OrdinalIgnoreCase))
                        {
                            NativeCompletionMemberName(context, result, commandAst, boundArguments?[parameterName]);
                        }
                        else if (parameterName.Equals("View", StringComparison.OrdinalIgnoreCase))
                        {
                            NativeCompletionFormatViewName(context, boundArguments, result, commandAst, commandName);
                        }

                        break;
                    }
                case "Select-Object":
                    {
                        if (parameterName.Equals("Property", StringComparison.OrdinalIgnoreCase)
                         || parameterName.Equals("ExcludeProperty", StringComparison.OrdinalIgnoreCase)
                         || parameterName.Equals("ExpandProperty", StringComparison.OrdinalIgnoreCase))
                        {
                            NativeCompletionMemberName(context, result, commandAst, boundArguments?[parameterName]);
                        }

                        break;
                    }

                case "New-Object":
                    {
                        if (parameterName.Equals("TypeName", StringComparison.OrdinalIgnoreCase))
                        {
                            NativeCompletionTypeName(context, result);
                        }

                        break;
                    }

                case "Get-CimClass":
                case "Get-CimInstance":
                case "Get-CimAssociatedInstance":
                case "Invoke-CimMethod":
                case "New-CimInstance":
                case "Register-CimIndicationEvent":
                case "Set-CimInstance":
                    {
                        // Avoids completion for parameters that expect a hashtable.
                        if (parameterName.Equals("Arguments", StringComparison.OrdinalIgnoreCase)
                            || (parameterName.Equals("Property", StringComparison.OrdinalIgnoreCase) && !commandName.Equals("Get-CimInstance")))
                        {
                            break;
                        }

                        HashSet<string> excludedValues = null;
                        if (parameterName.Equals("Property", StringComparison.OrdinalIgnoreCase) && boundArguments["Property"] is AstPair pair)
                        {
                            excludedValues = GetParameterValues(pair, context.CursorPosition.Offset);
                        }

                        NativeCompletionCimCommands(parameterName, boundArguments, result, commandAst, context, excludedValues, commandName);
                        break;
                    }

                default:
                    {
                        NativeCompletionPathArgument(context, parameterName, result);
                        break;
                    }
            }
        }

        private static Hashtable GetBoundArgumentsAsHashtable(CompletionContext context)
        {
            var result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            if (context.PseudoBindingInfo != null)
            {
                var boundArguments = context.PseudoBindingInfo.BoundArguments;
                if (boundArguments != null)
                {
                    foreach (var boundArgument in boundArguments)
                    {
                        var astPair = boundArgument.Value as AstPair;
                        if (astPair != null)
                        {
                            var parameterAst = astPair.Argument as CommandParameterAst;
                            var exprAst = parameterAst != null
                                              ? parameterAst.Argument
                                              : astPair.Argument as ExpressionAst;
                            object value;
                            if (exprAst != null && SafeExprEvaluator.TrySafeEval(exprAst, context.ExecutionContext, out value))
                            {
                                result[boundArgument.Key] = value;
                            }

                            continue;
                        }

                        var switchPair = boundArgument.Value as SwitchPair;
                        if (switchPair != null)
                        {
                            result[boundArgument.Key] = switchPair.Argument;
                            continue;
                        }
                        // Ignored:
                        //     AstArrayPair - only used for ValueFromRemainingArguments, not that useful for tab completion
                        //     FakePair - missing argument, not that useful
                        //     PipeObjectPair - no actual argument, makes for a poor api
                    }
                }
            }

            return result;
        }

        private static ScriptBlock GetCustomArgumentCompleter(
            string optionKey,
            IEnumerable<string> keys,
            CompletionContext context)
        {
            ScriptBlock scriptBlock;
            var options = context.Options;
            if (options != null)
            {
                var customCompleters = options[optionKey] as Hashtable;
                if (customCompleters != null)
                {
                    foreach (var key in keys)
                    {
                        if (customCompleters.ContainsKey(key))
                        {
                            scriptBlock = customCompleters[key] as ScriptBlock;
                            if (scriptBlock != null)
                                return scriptBlock;
                        }
                    }
                }
            }

            var registeredCompleters = optionKey.Equals("NativeArgumentCompleters", StringComparison.OrdinalIgnoreCase)
                ? context.NativeArgumentCompleters
                : context.CustomArgumentCompleters;

            if (registeredCompleters != null)
            {
                foreach (var key in keys)
                {
                    if (registeredCompleters.TryGetValue(key, out scriptBlock))
                    {
                        return scriptBlock;
                    }
                }
            }

            return null;
        }

        private static bool InvokeScriptArgumentCompleter(
            ScriptBlock scriptBlock,
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            CompletionContext context,
            List<CompletionResult> resultList)
        {
            bool result = InvokeScriptArgumentCompleter(
                scriptBlock,
                new object[] { commandName, parameterName, wordToComplete, commandAst, GetBoundArgumentsAsHashtable(context) },
                resultList);
            if (result)
            {
                resultList.Add(CompletionResult.Null);
            }

            return result;
        }

        private static bool InvokeScriptArgumentCompleter(
            ScriptBlock scriptBlock,
            object[] argumentsToCompleter,
            List<CompletionResult> result)
        {
            Collection<PSObject> customResults = null;
            try
            {
                customResults = scriptBlock.Invoke(argumentsToCompleter);
            }
            catch (Exception)
            {
            }

            if (customResults == null || customResults.Count == 0)
            {
                return false;
            }

            foreach (var customResult in customResults)
            {
                var resultAsCompletion = customResult.BaseObject as CompletionResult;
                if (resultAsCompletion != null)
                {
                    result.Add(resultAsCompletion);
                    continue;
                }

                var resultAsString = customResult.ToString();
                result.Add(new CompletionResult(resultAsString));
            }

            return true;
        }

        // All the methods for native command argument completion will add a null instance of the type CompletionResult to the end of the
        // "result" list, to indicate that this particular argument completion has fallen into one of the native command argument completion methods,
        // and has been processed already. So if the "result" list is still empty afterward, we will not go through the default argument completion anymore.
        #region Native Command Argument Completion

        private static void RemoveLastNullCompletionResult(List<CompletionResult> result)
        {
            if (result.Count > 0 && result[result.Count - 1].Equals(CompletionResult.Null))
            {
                result.RemoveAt(result.Count - 1);
            }
        }

        private static void NativeCompletionCimCommands(
            string parameter,
            Dictionary<string, AstParameterArgumentPair> boundArguments,
            List<CompletionResult> result,
            CommandAst commandAst,
            CompletionContext context,
            HashSet<string> excludedValues,
            string commandName)
        {
            if (boundArguments != null)
            {
                AstParameterArgumentPair astParameterArgumentPair;
                if ((boundArguments.TryGetValue("ComputerName", out astParameterArgumentPair)
                     || boundArguments.TryGetValue("CimSession", out astParameterArgumentPair))
                    && astParameterArgumentPair != null)
                {
                    switch (astParameterArgumentPair.ParameterArgumentType)
                    {
                        case AstParameterArgumentType.PipeObject:
                        case AstParameterArgumentType.Fake:
                            break;

                        default:
                            return; // we won't tab-complete remote class names
                    }
                }
            }

            RemoveLastNullCompletionResult(result);
            if (parameter.Equals("Namespace", StringComparison.OrdinalIgnoreCase))
            {
                NativeCompletionCimNamespace(result, context);
                result.Add(CompletionResult.Null);
                return;
            }

            string pseudoboundCimNamespace = NativeCommandArgumentCompletion_ExtractSecondaryArgument(boundArguments, "Namespace").FirstOrDefault();
            if (parameter.Equals("ClassName", StringComparison.OrdinalIgnoreCase))
            {
                NativeCompletionCimClassName(pseudoboundCimNamespace, result, context);
                result.Add(CompletionResult.Null);
                return;
            }

            bool gotInstance = false;
            IEnumerable<PSTypeName> cimClassTypeNames = null;
            string pseudoboundClassName = NativeCommandArgumentCompletion_ExtractSecondaryArgument(boundArguments, "ClassName").FirstOrDefault();
            if (pseudoboundClassName != null)
            {
                gotInstance = false;
                var tmp = new List<PSTypeName>();
                tmp.Add(new PSTypeName(typeof(CimInstance).FullName + "#" + (pseudoboundCimNamespace ?? "root/cimv2") + "/" + pseudoboundClassName));
                cimClassTypeNames = tmp;
            }
            else if (boundArguments != null && boundArguments.ContainsKey("InputObject"))
            {
                gotInstance = true;
                cimClassTypeNames = NativeCommandArgumentCompletion_InferTypesOfArgument(boundArguments, commandAst, context, "InputObject");
            }

            if (cimClassTypeNames != null)
            {
                foreach (PSTypeName typeName in cimClassTypeNames)
                {
                    if (TypeInferenceContext.ParseCimCommandsTypeName(typeName, out pseudoboundCimNamespace, out pseudoboundClassName))
                    {
                        if (parameter.Equals("ResultClassName", StringComparison.OrdinalIgnoreCase))
                        {
                            NativeCompletionCimAssociationResultClassName(pseudoboundCimNamespace, pseudoboundClassName, result, context);
                        }
                        else if (parameter.Equals("MethodName", StringComparison.OrdinalIgnoreCase))
                        {
                            NativeCompletionCimMethodName(pseudoboundCimNamespace, pseudoboundClassName, !gotInstance, result, context);
                        }
                        else if (parameter.Equals("Arguments", StringComparison.OrdinalIgnoreCase))
                        {
                            string pseudoboundMethodName = NativeCommandArgumentCompletion_ExtractSecondaryArgument(boundArguments, "MethodName").FirstOrDefault();
                            NativeCompletionCimMethodArgumentName(pseudoboundCimNamespace, pseudoboundClassName, pseudoboundMethodName, excludedValues, result, context);
                        }
                        else if (parameter.Equals("Property", StringComparison.OrdinalIgnoreCase))
                        {
                            bool includeReadOnly = !commandName.Equals("Set-CimInstance", StringComparison.OrdinalIgnoreCase);
                            NativeCompletionCimPropertyName(pseudoboundCimNamespace, pseudoboundClassName, includeReadOnly, excludedValues, result, context);
                        }
                    }
                }

                result.Add(CompletionResult.Null);
            }
        }

        private static readonly ConcurrentDictionary<string, IEnumerable<string>> s_cimNamespaceAndClassNameToAssociationResultClassNames =
            new ConcurrentDictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);

        private static IEnumerable<string> NativeCompletionCimAssociationResultClassName_GetResultClassNames(
            string cimNamespaceOfSource,
            string cimClassNameOfSource)
        {
            StringBuilder safeClassName = new StringBuilder();
            foreach (char c in cimClassNameOfSource)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    safeClassName.Append(c);
                }
            }

            List<string> resultClassNames = new List<string>();
            using (var cimSession = CimSession.Create(null))
            {
                CimClass cimClass = cimSession.GetClass(cimNamespaceOfSource ?? "root/cimv2", cimClassNameOfSource);
                while (cimClass != null)
                {
                    string query = string.Format(
                        CultureInfo.InvariantCulture,
                        "associators of {{{0}}} WHERE SchemaOnly",
                        cimClass.CimSystemProperties.ClassName);

                    resultClassNames.AddRange(
                        cimSession.QueryInstances(cimNamespaceOfSource ?? "root/cimv2", "WQL", query)
                            .Select(static associationInstance => associationInstance.CimSystemProperties.ClassName));

                    cimClass = cimClass.CimSuperClass;
                }
            }

            resultClassNames.Sort(StringComparer.OrdinalIgnoreCase);

            return resultClassNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static void NativeCompletionCimAssociationResultClassName(
            string pseudoboundNamespace,
            string pseudoboundClassName,
            List<CompletionResult> result,
            CompletionContext context)
        {
            if (string.IsNullOrWhiteSpace(pseudoboundClassName))
            {
                return;
            }

            IEnumerable<string> resultClassNames = s_cimNamespaceAndClassNameToAssociationResultClassNames.GetOrAdd(
                (pseudoboundNamespace ?? "root/cimv2") + ":" + pseudoboundClassName,
                _ => NativeCompletionCimAssociationResultClassName_GetResultClassNames(pseudoboundNamespace, pseudoboundClassName));

            WildcardPattern resultClassNamePattern = WildcardPattern.Get(context.WordToComplete + "*", WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant);
            result.AddRange(resultClassNames
                .Where(resultClassNamePattern.IsMatch)
                .Select(x => new CompletionResult(x, x, CompletionResultType.Type, string.Create(CultureInfo.InvariantCulture, $"{pseudoboundClassName} -> {x}"))));
        }

        private static void NativeCompletionCimMethodName(
            string pseudoboundNamespace,
            string pseudoboundClassName,
            bool staticMethod,
            List<CompletionResult> result,
            CompletionContext context)
        {
            if (string.IsNullOrWhiteSpace(pseudoboundClassName))
            {
                return;
            }

            CimClass cimClass;
            using (var cimSession = CimSession.Create(null))
            {
                cimClass = cimSession.GetClass(pseudoboundNamespace ?? "root/cimv2", pseudoboundClassName);
            }

            WildcardPattern methodNamePattern = WildcardPattern.Get(context.WordToComplete + "*", WildcardOptions.CultureInvariant | WildcardOptions.IgnoreCase);
            List<CompletionResult> localResults = new List<CompletionResult>();
            foreach (CimMethodDeclaration methodDeclaration in cimClass.CimClassMethods)
            {
                string methodName = methodDeclaration.Name;
                if (!methodNamePattern.IsMatch(methodName))
                {
                    continue;
                }

                bool currentMethodIsStatic = methodDeclaration.Qualifiers.Any(static q => q.Name.Equals("Static", StringComparison.OrdinalIgnoreCase));
                if ((currentMethodIsStatic && !staticMethod) || (!currentMethodIsStatic && staticMethod))
                {
                    continue;
                }

                StringBuilder tooltipText = new StringBuilder();
                tooltipText.Append(methodName);
                tooltipText.Append('(');
                bool gotFirstParameter = false;
                foreach (var methodParameter in methodDeclaration.Parameters)
                {
                    bool outParameter = methodParameter.Qualifiers.Any(static q => q.Name.Equals("Out", StringComparison.OrdinalIgnoreCase));

                    if (!gotFirstParameter)
                    {
                        gotFirstParameter = true;
                    }
                    else
                    {
                        tooltipText.Append(", ");
                    }

                    if (outParameter)
                    {
                        tooltipText.Append("[out] ");
                    }

                    tooltipText.Append(CimInstanceAdapter.CimTypeToTypeNameDisplayString(methodParameter.CimType));
                    tooltipText.Append(' ');
                    tooltipText.Append(methodParameter.Name);

                    if (outParameter)
                    {
                        continue;
                    }
                }

                tooltipText.Append(')');

                localResults.Add(new CompletionResult(methodName, methodName, CompletionResultType.Method, tooltipText.ToString()));
            }

            result.AddRange(localResults.OrderBy(static x => x.ListItemText, StringComparer.OrdinalIgnoreCase));
        }

        private static void NativeCompletionCimMethodArgumentName(
            string pseudoboundNamespace,
            string pseudoboundClassName,
            string pseudoboundMethodName,
            HashSet<string> excludedParameters,
            List<CompletionResult> result,
            CompletionContext context)
        {
            if (string.IsNullOrWhiteSpace(pseudoboundClassName) || string.IsNullOrWhiteSpace(pseudoboundMethodName))
            {
                return;
            }

            CimClass cimClass;
            using (var cimSession = CimSession.Create(null))
            {
                using var options = new CimOperationOptions();
                options.Flags |= CimOperationFlags.LocalizedQualifiers;
                cimClass = cimSession.GetClass(pseudoboundNamespace ?? "root/cimv2", pseudoboundClassName, options);
            }

            var methodParameters = cimClass.CimClassMethods[pseudoboundMethodName]?.Parameters;
            if (methodParameters is null)
            {
                return;
            }

            foreach (var parameter in methodParameters)
            {
                if ((string.IsNullOrEmpty(context.WordToComplete) || parameter.Name.StartsWith(context.WordToComplete, StringComparison.OrdinalIgnoreCase))
                        && (excludedParameters is null || !excludedParameters.Contains(parameter.Name))
                        && parameter.Qualifiers["In"]?.Value is true)
                {
                    string parameterDescription = parameter.Qualifiers["Description"]?.Value as string ?? string.Empty;
                    string toolTip = $"[{CimInstanceAdapter.CimTypeToTypeNameDisplayString(parameter.CimType)}] {parameterDescription}";
                    result.Add(new CompletionResult(parameter.Name, parameter.Name, CompletionResultType.Property, toolTip));
                }
            }
        }

        private static void NativeCompletionCimPropertyName(
            string pseudoboundNamespace,
            string pseudoboundClassName,
            bool includeReadOnly,
            HashSet<string> excludedProperties,
            List<CompletionResult> result,
            CompletionContext context)
        {
            if (string.IsNullOrWhiteSpace(pseudoboundClassName))
            {
                return;
            }

            CimClass cimClass;
            using (var cimSession = CimSession.Create(null))
            {
                using var options = new CimOperationOptions();
                options.Flags |= CimOperationFlags.LocalizedQualifiers;
                cimClass = cimSession.GetClass(pseudoboundNamespace ?? "root/cimv2", pseudoboundClassName, options);
            }

            foreach (var property in cimClass.CimClassProperties)
            {
                bool isReadOnly = (property.Flags & CimFlags.ReadOnly) != 0;
                if ((!isReadOnly || (isReadOnly && includeReadOnly))
                    && (string.IsNullOrEmpty(context.WordToComplete) || property.Name.StartsWith(context.WordToComplete, StringComparison.OrdinalIgnoreCase))
                    && (excludedProperties is null || !excludedProperties.Contains(property.Name)))
                {
                    string propertyDescription = property.Qualifiers["Description"]?.Value as string ?? string.Empty;
                    string accessString = isReadOnly ? "{ get; }" : "{ get; set; }";
                    string toolTip = $"[{CimInstanceAdapter.CimTypeToTypeNameDisplayString(property.CimType)}] {accessString} {propertyDescription}";
                    result.Add(new CompletionResult(property.Name, property.Name, CompletionResultType.Property, toolTip));
                }
            }
        }

        private static readonly ConcurrentDictionary<string, IEnumerable<string>> s_cimNamespaceToClassNames =
            new ConcurrentDictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);

        private static IEnumerable<string> NativeCompletionCimClassName_GetClassNames(string targetNamespace)
        {
            List<string> result = new List<string>();
            using (CimSession cimSession = CimSession.Create(null))
            {
                using (var operationOptions = new CimOperationOptions { ClassNamesOnly = true })
                    foreach (CimClass cimClass in cimSession.EnumerateClasses(targetNamespace, null, operationOptions))
                        using (cimClass)
                        {
                            string className = cimClass.CimSystemProperties.ClassName;
                            result.Add(className);
                        }
            }

            return result;
        }

        private static void NativeCompletionCimClassName(
            string pseudoBoundNamespace,
            List<CompletionResult> result,
            CompletionContext context)
        {
            string targetNamespace = pseudoBoundNamespace ?? "root/cimv2";

            List<string> regularClasses = new List<string>();
            List<string> systemClasses = new List<string>();

            IEnumerable<string> allClasses = s_cimNamespaceToClassNames.GetOrAdd(
                targetNamespace,
                NativeCompletionCimClassName_GetClassNames);
            WildcardPattern classNamePattern = WildcardPattern.Get(context.WordToComplete + "*", WildcardOptions.CultureInvariant | WildcardOptions.IgnoreCase);

            foreach (string className in allClasses)
            {
                if (context.Helper.CancelTabCompletion)
                {
                    break;
                }

                if (!classNamePattern.IsMatch(className))
                {
                    continue;
                }

                if (className.Length > 0 && className[0] == '_')
                {
                    systemClasses.Add(className);
                }
                else
                {
                    regularClasses.Add(className);
                }
            }

            regularClasses.Sort(StringComparer.OrdinalIgnoreCase);
            systemClasses.Sort(StringComparer.OrdinalIgnoreCase);
            result.AddRange(
                regularClasses.Concat(systemClasses)
                    .Select(className => new CompletionResult(className, className, CompletionResultType.Type, targetNamespace + ":" + className)));
        }

        private static void NativeCompletionCimNamespace(
            List<CompletionResult> result,
            CompletionContext context)
        {
            string containerNamespace = "root";
            string prefixOfChildNamespace = string.Empty;
            if (!string.IsNullOrEmpty(context.WordToComplete))
            {
                int lastSlashOrBackslash = context.WordToComplete.AsSpan().LastIndexOfAny('\\', '/');
                if (lastSlashOrBackslash != (-1))
                {
                    containerNamespace = context.WordToComplete.Substring(0, lastSlashOrBackslash);
                    prefixOfChildNamespace = context.WordToComplete.Substring(lastSlashOrBackslash + 1);
                }
            }

            List<CompletionResult> namespaceResults = new List<CompletionResult>();
            WildcardPattern childNamespacePattern = WildcardPattern.Get(prefixOfChildNamespace + "*", WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant);
            using (CimSession cimSession = CimSession.Create(null))
            {
                foreach (CimInstance namespaceInstance in cimSession.EnumerateInstances(containerNamespace, "__Namespace"))
                    using (namespaceInstance)
                    {
                        if (context.Helper.CancelTabCompletion)
                        {
                            break;
                        }

                        CimProperty namespaceNameProperty = namespaceInstance.CimInstanceProperties["Name"];
                        if (namespaceNameProperty == null)
                        {
                            continue;
                        }

                        if (!(namespaceNameProperty.Value is string childNamespace))
                        {
                            continue;
                        }

                        if (!childNamespacePattern.IsMatch(childNamespace))
                        {
                            continue;
                        }

                        namespaceResults.Add(new CompletionResult(
                                                 containerNamespace + "/" + childNamespace,
                                                 childNamespace,
                                                 CompletionResultType.Namespace,
                                                 containerNamespace + "/" + childNamespace));
                    }
            }

            result.AddRange(namespaceResults.OrderBy(static x => x.ListItemText, StringComparer.OrdinalIgnoreCase));
        }

        private static void NativeCompletionGetCommand(CompletionContext context, string moduleName, string paramName, List<CompletionResult> result)
        {
            if (!string.IsNullOrEmpty(paramName) && paramName.Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                RemoveLastNullCompletionResult(result);

                // Available commands
                var commandResults = CompleteCommand(context, moduleName);
                if (commandResults != null)
                    result.AddRange(commandResults);

                // Consider files only if the -Module parameter is not present
                if (moduleName == null)
                {
                    // ps1 files and directories. We only complete the files with .ps1 extension for Get-Command, because the -Syntax
                    // may only works on files with .ps1 extension
                    var ps1Extension = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { StringLiterals.PowerShellScriptFileExtension };
                    var moduleFilesResults = new List<CompletionResult>(CompleteFilename(context, /* containerOnly: */ false, ps1Extension));
                    if (moduleFilesResults.Count > 0)
                        result.AddRange(moduleFilesResults);
                }

                result.Add(CompletionResult.Null);
            }
            else if (!string.IsNullOrEmpty(paramName) && paramName.Equals("Module", StringComparison.OrdinalIgnoreCase))
            {
                CompleteModule(context, result);
            }
        }

        private static void CompleteModule(CompletionContext context, List<CompletionResult> result)
        {
            RemoveLastNullCompletionResult(result);

            var modules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var moduleResults = CompleteModuleName(context, loadedModulesOnly: true);
            if (moduleResults != null)
            {
                foreach (CompletionResult moduleResult in moduleResults)
                {
                    if (!modules.Contains(moduleResult.ToolTip))
                    {
                        modules.Add(moduleResult.ToolTip);
                        result.Add(moduleResult);
                    }
                }
            }

            moduleResults = CompleteModuleName(context, loadedModulesOnly: false);
            if (moduleResults != null)
            {
                foreach (CompletionResult moduleResult in moduleResults)
                {
                    if (!modules.Contains(moduleResult.ToolTip))
                    {
                        modules.Add(moduleResult.ToolTip);
                        result.Add(moduleResult);
                    }
                }
            }

            result.Add(CompletionResult.Null);
        }

        private static void NativeCompletionGetHelpCommand(CompletionContext context, string paramName, bool isHelpRelated, List<CompletionResult> result)
        {
            if (!string.IsNullOrEmpty(paramName) && paramName.Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                RemoveLastNullCompletionResult(result);

                // Available commands
                const CommandTypes commandTypes = CommandTypes.Cmdlet | CommandTypes.Function | CommandTypes.Alias | CommandTypes.ExternalScript | CommandTypes.Configuration;
                var commandResults = CompleteCommand(context, /* moduleName: */ null, commandTypes);
                if (commandResults != null)
                    result.AddRange(commandResults);

                // ps1 files and directories
                var ps1Extension = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { StringLiterals.PowerShellScriptFileExtension };
                var fileResults = new List<CompletionResult>(CompleteFilename(context, /* containerOnly: */ false, ps1Extension));
                if (fileResults.Count > 0)
                    result.AddRange(fileResults);

                if (isHelpRelated)
                {
                    // Available topics
                    var helpTopicResults = CompleteHelpTopics(context);
                    if (helpTopicResults != null)
                        result.AddRange(helpTopicResults);
                }

                result.Add(CompletionResult.Null);
            }
        }

        private static void NativeCompletionEventLogCommands(CompletionContext context, string paramName, List<CompletionResult> result)
        {
            if (!string.IsNullOrEmpty(paramName) && paramName.Equals("LogName", StringComparison.OrdinalIgnoreCase))
            {
                RemoveLastNullCompletionResult(result);

                var logName = context.WordToComplete ?? string.Empty;
                var quote = HandleDoubleAndSingleQuote(ref logName);

                if (!logName.EndsWith('*'))
                {
                    logName += "*";
                }

                var pattern = WildcardPattern.Get(logName, WildcardOptions.IgnoreCase);

                var powerShellExecutionHelper = context.Helper;
                var powershell = powerShellExecutionHelper.AddCommandWithPreferenceSetting("Microsoft.PowerShell.Management\\Get-EventLog").AddParameter("LogName", "*");

                Exception exceptionThrown;
                var psObjects = powerShellExecutionHelper.ExecuteCurrentPowerShell(out exceptionThrown);

                if (psObjects != null)
                {
                    foreach (dynamic eventLog in psObjects)
                    {
                        var completionText = eventLog.Log.ToString();
                        var listItemText = completionText;

                        if (CompletionRequiresQuotes(completionText, false))
                        {
                            var quoteInUse = quote == string.Empty ? "'" : quote;
                            if (quoteInUse == "'")
                                completionText = completionText.Replace("'", "''");
                            completionText = quoteInUse + completionText + quoteInUse;
                        }
                        else
                        {
                            completionText = quote + completionText + quote;
                        }

                        if (pattern.IsMatch(listItemText))
                        {
                            result.Add(new CompletionResult(completionText, listItemText, CompletionResultType.ParameterValue, listItemText));
                        }
                    }
                }

                result.Add(CompletionResult.Null);
            }
        }

        private static void NativeCompletionJobCommands(CompletionContext context, string paramName, List<CompletionResult> result)
        {
            if (string.IsNullOrEmpty(paramName))
                return;

            var wordToComplete = context.WordToComplete ?? string.Empty;
            var quote = HandleDoubleAndSingleQuote(ref wordToComplete);

            if (!wordToComplete.EndsWith('*'))
            {
                wordToComplete += "*";
            }

            var pattern = WildcardPattern.Get(wordToComplete, WildcardOptions.IgnoreCase);

            var paramIsName = paramName.Equals("Name", StringComparison.OrdinalIgnoreCase);
            var (parameterName, value) = paramIsName ? ("Name", wordToComplete) : ("IncludeChildJob", (object)true);
            var powerShellExecutionHelper = context.Helper;
            powerShellExecutionHelper.AddCommandWithPreferenceSetting("Get-Job", typeof(GetJobCommand)).AddParameter(parameterName, value);

            Exception exceptionThrown;
            var psObjects = powerShellExecutionHelper.ExecuteCurrentPowerShell(out exceptionThrown);
            if (psObjects == null)
                return;

            if (paramName.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                RemoveLastNullCompletionResult(result);

                foreach (dynamic psJob in psObjects)
                {
                    var completionText = psJob.Id.ToString();
                    if (pattern.IsMatch(completionText))
                    {
                        var listItemText = completionText;
                        completionText = quote + completionText + quote;
                        result.Add(new CompletionResult(completionText, listItemText, CompletionResultType.ParameterValue, listItemText));
                    }
                }

                result.Add(CompletionResult.Null);
            }
            else if (paramName.Equals("InstanceId", StringComparison.OrdinalIgnoreCase))
            {
                RemoveLastNullCompletionResult(result);

                foreach (dynamic psJob in psObjects)
                {
                    var completionText = psJob.InstanceId.ToString();
                    if (pattern.IsMatch(completionText))
                    {
                        var listItemText = completionText;
                        completionText = quote + completionText + quote;
                        result.Add(new CompletionResult(completionText, listItemText, CompletionResultType.ParameterValue, listItemText));
                    }
                }

                result.Add(CompletionResult.Null);
            }
            else if (paramIsName)
            {
                RemoveLastNullCompletionResult(result);

                foreach (dynamic psJob in psObjects)
                {
                    var completionText = psJob.Name;
                    var listItemText = completionText;

                    if (CompletionRequiresQuotes(completionText, false))
                    {
                        var quoteInUse = quote == string.Empty ? "'" : quote;
                        if (quoteInUse == "'")
                            completionText = completionText.Replace("'", "''");
                        completionText = quoteInUse + completionText + quoteInUse;
                    }
                    else
                    {
                        completionText = quote + completionText + quote;
                    }

                    result.Add(new CompletionResult(completionText, listItemText, CompletionResultType.ParameterValue, listItemText));
                }

                result.Add(CompletionResult.Null);
            }
        }

        private static void NativeCompletionScheduledJobCommands(CompletionContext context, string paramName, List<CompletionResult> result)
        {
            if (string.IsNullOrEmpty(paramName))
                return;

            var wordToComplete = context.WordToComplete ?? string.Empty;
            var quote = HandleDoubleAndSingleQuote(ref wordToComplete);

            if (!wordToComplete.EndsWith('*'))
            {
                wordToComplete += "*";
            }

            var pattern = WildcardPattern.Get(wordToComplete, WildcardOptions.IgnoreCase);

            var powerShellExecutionHelper = context.Helper;
            if (paramName.Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                powerShellExecutionHelper.AddCommandWithPreferenceSetting("PSScheduledJob\\Get-ScheduledJob").AddParameter("Name", wordToComplete);
            }
            else
            {
                powerShellExecutionHelper.AddCommandWithPreferenceSetting("PSScheduledJob\\Get-ScheduledJob");
            }

            Exception exceptionThrown;
            var psObjects = powerShellExecutionHelper.ExecuteCurrentPowerShell(out exceptionThrown);
            if (psObjects == null)
                return;

            if (paramName.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                RemoveLastNullCompletionResult(result);

                foreach (dynamic psJob in psObjects)
                {
                    var completionText = psJob.Id.ToString();
                    if (pattern.IsMatch(completionText))
                    {
                        var listItemText = completionText;
                        completionText = quote + completionText + quote;
                        result.Add(new CompletionResult(completionText, listItemText, CompletionResultType.ParameterValue, listItemText));
                    }
                }

                result.Add(CompletionResult.Null);
            }
            else if (paramName.Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                RemoveLastNullCompletionResult(result);

                foreach (dynamic psJob in psObjects)
                {
                    var completionText = psJob.Name;
                    var listItemText = completionText;

                    if (CompletionRequiresQuotes(completionText, false))
                    {
                        var quoteInUse = quote == string.Empty ? "'" : quote;
                        if (quoteInUse == "'")
                            completionText = completionText.Replace("'", "''");
                        completionText = quoteInUse + completionText + quoteInUse;
                    }
                    else
                    {
                        completionText = quote + completionText + quote;
                    }

                    result.Add(new CompletionResult(completionText, listItemText, CompletionResultType.ParameterValue, listItemText));
                }

                result.Add(CompletionResult.Null);
            }
        }

        private static void NativeCompletionModuleCommands(
            CompletionContext context,
            string paramName,
            List<CompletionResult> result,
            bool loadedModulesOnly = false,
            bool isImportModule = false,
            bool skipEditionCheck = false)
        {
            if (string.IsNullOrEmpty(paramName))
            {
                return;
            }

            if (paramName.Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                RemoveLastNullCompletionResult(result);

                if (isImportModule)
                {
                    var moduleExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            {   StringLiterals.PowerShellScriptFileExtension,
                                StringLiterals.PowerShellModuleFileExtension,
                                StringLiterals.PowerShellDataFileExtension,
                                StringLiterals.PowerShellNgenAssemblyExtension,
                                StringLiterals.PowerShellILAssemblyExtension,
                                StringLiterals.PowerShellILExecutableExtension,
                                StringLiterals.PowerShellCmdletizationFileExtension
                            };
                    var moduleFilesResults = new List<CompletionResult>(CompleteFilename(context, containerOnly: false, moduleExtensions));
                    if (moduleFilesResults.Count > 0)
                        result.AddRange(moduleFilesResults);

                    var assemblyOrModuleName = context.WordToComplete;
                    if (assemblyOrModuleName.IndexOfAny(Utils.Separators.DirectoryOrDrive) != -1)
                    {
                        // The partial input is a path, then we don't iterate modules under $ENV:PSModulePath
                        return;
                    }
                }

                var moduleResults = CompleteModuleName(context, loadedModulesOnly, skipEditionCheck);
                if (moduleResults != null && moduleResults.Count > 0)
                    result.AddRange(moduleResults);

                result.Add(CompletionResult.Null);
            }
            else if (paramName.Equals("Assembly", StringComparison.OrdinalIgnoreCase))
            {
                RemoveLastNullCompletionResult(result);

                var moduleExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".dll" };
                var moduleFilesResults = new List<CompletionResult>(CompleteFilename(context, /* containerOnly: */ false, moduleExtensions));
                if (moduleFilesResults.Count > 0)
                    result.AddRange(moduleFilesResults);

                result.Add(CompletionResult.Null);
            }
        }

        private static void NativeCompletionProcessCommands(CompletionContext context, string paramName, List<CompletionResult> result)
        {
            if (string.IsNullOrEmpty(paramName))
                return;

            var wordToComplete = context.WordToComplete ?? string.Empty;
            var quote = HandleDoubleAndSingleQuote(ref wordToComplete);

            if (!wordToComplete.EndsWith('*'))
            {
                wordToComplete += "*";
            }

            var powerShellExecutionHelper = context.Helper;
            if (paramName.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                powerShellExecutionHelper.AddCommandWithPreferenceSetting("Microsoft.PowerShell.Management\\Get-Process");
            }
            else
            {
                powerShellExecutionHelper.AddCommandWithPreferenceSetting("Microsoft.PowerShell.Management\\Get-Process").AddParameter("Name", wordToComplete);
            }

            Exception exceptionThrown;
            var psObjects = powerShellExecutionHelper.ExecuteCurrentPowerShell(out exceptionThrown);
            if (psObjects == null)
                return;

            if (paramName.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                RemoveLastNullCompletionResult(result);

                var pattern = WildcardPattern.Get(wordToComplete, WildcardOptions.IgnoreCase);
                foreach (dynamic process in psObjects)
                {
                    var processId = process.Id.ToString();
                    if (pattern.IsMatch(processId))
                    {
                        var processName = process.Name;

                        var idAndName = $"{processId} - {processName}";
                        processId = quote + processId + quote;
                        result.Add(new CompletionResult(processId, idAndName, CompletionResultType.ParameterValue, idAndName));
                    }
                }

                result.Add(CompletionResult.Null);
            }
            else if (paramName.Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                RemoveLastNullCompletionResult(result);

                var uniqueSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (dynamic process in psObjects)
                {
                    var completionText = process.Name;
                    var listItemText = completionText;

                    if (uniqueSet.Contains(completionText))
                        continue;

                    uniqueSet.Add(completionText);
                    if (CompletionRequiresQuotes(completionText, false))
                    {
                        var quoteInUse = quote == string.Empty ? "'" : quote;
                        if (quoteInUse == "'")
                            completionText = completionText.Replace("'", "''");
                        completionText = quoteInUse + completionText + quoteInUse;
                    }
                    else
                    {
                        completionText = quote + completionText + quote;
                    }

                    // on macOS, system processes names will be empty if PowerShell isn't run as `sudo`
                    if (string.IsNullOrEmpty(listItemText))
                    {
                        continue;
                    }

                    result.Add(new CompletionResult(completionText, listItemText, CompletionResultType.ParameterValue, listItemText));
                }

                result.Add(CompletionResult.Null);
            }
        }

        private static void NativeCompletionProviderCommands(CompletionContext context, string paramName, List<CompletionResult> result)
        {
            if (string.IsNullOrEmpty(paramName) || !paramName.Equals("PSProvider", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            RemoveLastNullCompletionResult(result);

            var providerName = context.WordToComplete ?? string.Empty;
            var quote = HandleDoubleAndSingleQuote(ref providerName);

            if (!providerName.EndsWith('*'))
            {
                providerName += "*";
            }

            var powerShellExecutionHelper = context.Helper;
            powerShellExecutionHelper.AddCommandWithPreferenceSetting("Microsoft.PowerShell.Management\\Get-PSProvider").AddParameter("PSProvider", providerName);
            var psObjects = powerShellExecutionHelper.ExecuteCurrentPowerShell(out _);
            if (psObjects == null)
                return;

            foreach (dynamic providerInfo in psObjects)
            {
                var completionText = providerInfo.Name;
                var listItemText = completionText;

                if (CompletionRequiresQuotes(completionText, false))
                {
                    var quoteInUse = quote == string.Empty ? "'" : quote;
                    if (quoteInUse == "'")
                        completionText = completionText.Replace("'", "''");
                    completionText = quoteInUse + completionText + quoteInUse;
                }
                else
                {
                    completionText = quote + completionText + quote;
                }

                result.Add(new CompletionResult(completionText, listItemText, CompletionResultType.ParameterValue, listItemText));
            }

            result.Add(CompletionResult.Null);
        }

        private static void NativeCompletionDriveCommands(CompletionContext context, string psProvider, string paramName, List<CompletionResult> result)
        {
            if (string.IsNullOrEmpty(paramName) || !paramName.Equals("Name", StringComparison.OrdinalIgnoreCase))
                return;

            RemoveLastNullCompletionResult(result);

            var wordToComplete = context.WordToComplete ?? string.Empty;
            var quote = HandleDoubleAndSingleQuote(ref wordToComplete);

            if (!wordToComplete.EndsWith('*'))
            {
                wordToComplete += "*";
            }

            var powerShellExecutionHelper = context.Helper;
            var powershell = powerShellExecutionHelper
                .AddCommandWithPreferenceSetting("Microsoft.PowerShell.Management\\Get-PSDrive")
                .AddParameter("Name", wordToComplete);
            if (psProvider != null)
                powershell.AddParameter("PSProvider", psProvider);

            var psObjects = powerShellExecutionHelper.ExecuteCurrentPowerShell(out _);
            if (psObjects != null)
            {
                foreach (dynamic driveInfo in psObjects)
                {
                    var completionText = driveInfo.Name;
                    var listItemText = completionText;

                    if (CompletionRequiresQuotes(completionText, false))
                    {
                        var quoteInUse = quote == string.Empty ? "'" : quote;
                        if (quoteInUse == "'")
                            completionText = completionText.Replace("'", "''");
                        completionText = quoteInUse + completionText + quoteInUse;
                    }
                    else
                    {
                        completionText = quote + completionText + quote;
                    }

                    result.Add(new CompletionResult(completionText, listItemText, CompletionResultType.ParameterValue, listItemText));
                }
            }

            result.Add(CompletionResult.Null);
        }

        private static void NativeCompletionServiceCommands(CompletionContext context, string paramName, List<CompletionResult> result)
        {
            if (string.IsNullOrEmpty(paramName))
                return;

            var wordToComplete = context.WordToComplete ?? string.Empty;
            var quote = HandleDoubleAndSingleQuote(ref wordToComplete);

            if (!wordToComplete.EndsWith('*'))
            {
                wordToComplete += "*";
            }

            Exception exceptionThrown;
            var powerShellExecutionHelper = context.Helper;
            if (paramName.Equals("DisplayName", StringComparison.OrdinalIgnoreCase))
            {
                RemoveLastNullCompletionResult(result);

                powerShellExecutionHelper
                    .AddCommandWithPreferenceSetting("Microsoft.PowerShell.Management\\Get-Service")
                    .AddParameter("DisplayName", wordToComplete)
                    .AddCommandWithPreferenceSetting("Microsoft.PowerShell.Utility\\Sort-Object")
                    .AddParameter("Property", "DisplayName");
                var psObjects = powerShellExecutionHelper.ExecuteCurrentPowerShell(out exceptionThrown);
                if (psObjects != null)
                {
                    foreach (dynamic serviceInfo in psObjects)
                    {
                        var completionText = serviceInfo.DisplayName;
                        var listItemText = completionText;

                        if (CompletionRequiresQuotes(completionText, false))
                        {
                            var quoteInUse = quote == string.Empty ? "'" : quote;
                            if (quoteInUse == "'")
                                completionText = completionText.Replace("'", "''");
                            completionText = quoteInUse + completionText + quoteInUse;
                        }
                        else
                        {
                            completionText = quote + completionText + quote;
                        }

                        result.Add(new CompletionResult(completionText, listItemText, CompletionResultType.ParameterValue, listItemText));
                    }
                }

                result.Add(CompletionResult.Null);
            }
            else if (paramName.Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                RemoveLastNullCompletionResult(result);

                powerShellExecutionHelper.AddCommandWithPreferenceSetting("Microsoft.PowerShell.Management\\Get-Service").AddParameter("Name", wordToComplete);
                var psObjects = powerShellExecutionHelper.ExecuteCurrentPowerShell(out exceptionThrown);
                if (psObjects != null)
                {
                    foreach (dynamic serviceInfo in psObjects)
                    {
                        var completionText = serviceInfo.Name;
                        var listItemText = completionText;

                        if (CompletionRequiresQuotes(completionText, false))
                        {
                            var quoteInUse = quote == string.Empty ? "'" : quote;
                            if (quoteInUse == "'")
                                completionText = completionText.Replace("'", "''");
                            completionText = quoteInUse + completionText + quoteInUse;
                        }
                        else
                        {
                            completionText = quote + completionText + quote;
                        }

                        result.Add(new CompletionResult(completionText, listItemText, CompletionResultType.ParameterValue, listItemText));
                    }
                }

                result.Add(CompletionResult.Null);
            }
        }

        private static void NativeCompletionVariableCommands(CompletionContext context, string paramName, List<CompletionResult> result)
        {
            if (string.IsNullOrEmpty(paramName) || !paramName.Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            RemoveLastNullCompletionResult(result);

            var variableName = context.WordToComplete ?? string.Empty;
            var quote = HandleDoubleAndSingleQuote(ref variableName);
            if (!variableName.EndsWith('*'))
            {
                variableName += "*";
            }

            var powerShellExecutionHelper = context.Helper;
            var powershell = powerShellExecutionHelper.AddCommandWithPreferenceSetting("Microsoft.PowerShell.Utility\\Get-Variable").AddParameter("Name", variableName);
            var psObjects = powerShellExecutionHelper.ExecuteCurrentPowerShell(out _);
            if (psObjects == null)
                return;

            foreach (dynamic variable in psObjects)
            {
                var effectiveQuote = quote;
                var completionText = variable.Name;
                var listItemText = completionText;

                // Handle special characters ? and * in variable names
                if (completionText.IndexOfAny(Utils.Separators.StarOrQuestion) != -1)
                {
                    effectiveQuote = "'";
                    completionText = completionText.Replace("?", "`?");
                    completionText = completionText.Replace("*", "`*");
                }

                if (!completionText.Equals("$", StringComparison.Ordinal) && CompletionRequiresQuotes(completionText, false))
                {
                    var quoteInUse = effectiveQuote == string.Empty ? "'" : effectiveQuote;
                    if (quoteInUse == "'")
                        completionText = completionText.Replace("'", "''");
                    completionText = quoteInUse + completionText + quoteInUse;
                }
                else
                {
                    completionText = effectiveQuote + completionText + effectiveQuote;
                }

                result.Add(new CompletionResult(completionText, listItemText, CompletionResultType.ParameterValue, listItemText));
            }

            result.Add(CompletionResult.Null);
        }

        private static void NativeCompletionAliasCommands(CompletionContext context, string paramName, List<CompletionResult> result)
        {
            if (string.IsNullOrEmpty(paramName) ||
                (!paramName.Equals("Definition", StringComparison.OrdinalIgnoreCase) &&
                 !paramName.Equals("Name", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            RemoveLastNullCompletionResult(result);

            var powerShellExecutionHelper = context.Helper;
            if (paramName.Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                var commandName = context.WordToComplete ?? string.Empty;
                var quote = HandleDoubleAndSingleQuote(ref commandName);

                if (!commandName.EndsWith('*'))
                {
                    commandName += "*";
                }

                Exception exceptionThrown;
                var powershell = powerShellExecutionHelper.AddCommandWithPreferenceSetting("Microsoft.PowerShell.Utility\\Get-Alias").AddParameter("Name", commandName);
                var psObjects = powerShellExecutionHelper.ExecuteCurrentPowerShell(out exceptionThrown);
                if (psObjects != null)
                {
                    foreach (dynamic aliasInfo in psObjects)
                    {
                        var completionText = aliasInfo.Name;
                        var listItemText = completionText;

                        if (CompletionRequiresQuotes(completionText, false))
                        {
                            var quoteInUse = quote == string.Empty ? "'" : quote;
                            if (quoteInUse == "'")
                                completionText = completionText.Replace("'", "''");
                            completionText = quoteInUse + completionText + quoteInUse;
                        }
                        else
                        {
                            completionText = quote + completionText + quote;
                        }

                        result.Add(new CompletionResult(completionText, listItemText, CompletionResultType.ParameterValue, listItemText));
                    }
                }
            }
            else
            {
                // Complete for the parameter Definition
                // Available commands
                const CommandTypes commandTypes = CommandTypes.Cmdlet | CommandTypes.Function | CommandTypes.ExternalScript | CommandTypes.Configuration;
                var commandResults = CompleteCommand(context, /* moduleName: */ null, commandTypes);
                if (commandResults != null && commandResults.Count > 0)
                    result.AddRange(commandResults);

                // The parameter Definition takes a file
                var fileResults = new List<CompletionResult>(CompleteFilename(context));
                if (fileResults.Count > 0)
                    result.AddRange(fileResults);
            }

            result.Add(CompletionResult.Null);
        }

        private static void NativeCompletionTraceSourceCommands(CompletionContext context, string paramName, List<CompletionResult> result)
        {
            if (string.IsNullOrEmpty(paramName) || !paramName.Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            RemoveLastNullCompletionResult(result);

            var traceSourceName = context.WordToComplete ?? string.Empty;
            var quote = HandleDoubleAndSingleQuote(ref traceSourceName);

            if (!traceSourceName.EndsWith('*'))
            {
                traceSourceName += "*";
            }

            var powerShellExecutionHelper = context.Helper;
            var powershell = powerShellExecutionHelper.AddCommandWithPreferenceSetting("Microsoft.PowerShell.Utility\\Get-TraceSource").AddParameter("Name", traceSourceName);
            Exception exceptionThrown;
            var psObjects = powerShellExecutionHelper.ExecuteCurrentPowerShell(out exceptionThrown);
            if (psObjects == null)
                return;

            foreach (dynamic trace in psObjects)
            {
                var completionText = trace.Name;
                var listItemText = completionText;

                if (CompletionRequiresQuotes(completionText, false))
                {
                    var quoteInUse = quote == string.Empty ? "'" : quote;
                    if (quoteInUse == "'")
                        completionText = completionText.Replace("'", "''");
                    completionText = quoteInUse + completionText + quoteInUse;
                }
                else
                {
                    completionText = quote + completionText + quote;
                }

                result.Add(new CompletionResult(completionText, listItemText, CompletionResultType.ParameterValue, listItemText));
            }

            result.Add(CompletionResult.Null);
        }

        private static void NativeCompletionSetLocationCommand(CompletionContext context, string paramName, List<CompletionResult> result)
        {
            if (string.IsNullOrEmpty(paramName) ||
                (!paramName.Equals("Path", StringComparison.OrdinalIgnoreCase) &&
                 !paramName.Equals("LiteralPath", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            RemoveLastNullCompletionResult(result);

            context.WordToComplete ??= string.Empty;
            var clearLiteralPath = false;
            if (paramName.Equals("LiteralPath", StringComparison.OrdinalIgnoreCase))
            {
                clearLiteralPath = TurnOnLiteralPathOption(context);
            }

            try
            {
                var fileNameResults = CompleteFilename(context, containerOnly: true, extension: null);
                if (fileNameResults != null)
                    result.AddRange(fileNameResults);
            }
            finally
            {
                if (clearLiteralPath)
                    context.Options.Remove("LiteralPaths");
            }

            result.Add(CompletionResult.Null);
        }

        /// <summary>
        /// Provides completion results for NewItemCommand.
        /// </summary>
        /// <param name="context">Completion context.</param>
        /// <param name="paramName">Name of the parameter whose value needs completion.</param>
        /// <param name="result">List of completion suggestions.</param>
        private static void NativeCompletionNewItemCommand(CompletionContext context, string paramName, List<CompletionResult> result)
        {
            if (string.IsNullOrEmpty(paramName))
            {
                return;
            }

            var executionContext = context.ExecutionContext;

            var boundArgs = GetBoundArgumentsAsHashtable(context);
            var providedPath = boundArgs["Path"] as string ?? executionContext.SessionState.Path.CurrentLocation.Path;

            ProviderInfo provider;
            executionContext.LocationGlobber.GetProviderPath(providedPath, out provider);

            var isFileSystem = provider != null &&
                               provider.Name.Equals(FileSystemProvider.ProviderName, StringComparison.OrdinalIgnoreCase);

            // AutoComplete only if filesystem provider.
            if (isFileSystem)
            {
                if (paramName.Equals("ItemType", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(context.WordToComplete))
                    {
                        WildcardPattern patternEvaluator = WildcardPattern.Get(context.WordToComplete + "*", WildcardOptions.IgnoreCase);

                        if (patternEvaluator.IsMatch("file"))
                        {
                            result.Add(new CompletionResult("File"));
                        }
                        else if (patternEvaluator.IsMatch("directory"))
                        {
                            result.Add(new CompletionResult("Directory"));
                        }
                        else if (patternEvaluator.IsMatch("symboliclink"))
                        {
                            result.Add(new CompletionResult("SymbolicLink"));
                        }
                        else if (patternEvaluator.IsMatch("junction"))
                        {
                            result.Add(new CompletionResult("Junction"));
                        }
                        else if (patternEvaluator.IsMatch("hardlink"))
                        {
                            result.Add(new CompletionResult("HardLink"));
                        }
                    }
                    else
                    {
                        result.Add(new CompletionResult("File"));
                        result.Add(new CompletionResult("Directory"));
                        result.Add(new CompletionResult("SymbolicLink"));
                        result.Add(new CompletionResult("Junction"));
                        result.Add(new CompletionResult("HardLink"));
                    }

                    result.Add(CompletionResult.Null);
                }
            }
        }

        private static void NativeCompletionCopyMoveItemCommand(CompletionContext context, string paramName, List<CompletionResult> result)
        {
            if (string.IsNullOrEmpty(paramName))
            {
                return;
            }

            if (paramName.Equals("LiteralPath", StringComparison.OrdinalIgnoreCase) || paramName.Equals("Path", StringComparison.OrdinalIgnoreCase))
            {
                NativeCompletionPathArgument(context, paramName, result);
            }
            else if (paramName.Equals("Destination", StringComparison.OrdinalIgnoreCase))
            {
                // The parameter Destination for Move-Item and Copy-Item takes literal path
                RemoveLastNullCompletionResult(result);

                context.WordToComplete ??= string.Empty;
                var clearLiteralPath = TurnOnLiteralPathOption(context);

                try
                {
                    var fileNameResults = CompleteFilename(context);
                    if (fileNameResults != null)
                        result.AddRange(fileNameResults);
                }
                finally
                {
                    if (clearLiteralPath)
                        context.Options.Remove("LiteralPaths");
                }

                result.Add(CompletionResult.Null);
            }
        }

        private static void NativeCompletionPathArgument(CompletionContext context, string paramName, List<CompletionResult> result)
        {
            if (string.IsNullOrEmpty(paramName) ||
                (!paramName.Equals("LiteralPath", StringComparison.OrdinalIgnoreCase) &&
                (!paramName.Equals("Path", StringComparison.OrdinalIgnoreCase)) &&
                (!paramName.Equals("FilePath", StringComparison.OrdinalIgnoreCase))))
            {
                return;
            }

            RemoveLastNullCompletionResult(result);

            context.WordToComplete ??= string.Empty;
            var clearLiteralPath = false;
            if (paramName.Equals("LiteralPath", StringComparison.OrdinalIgnoreCase))
            {
                clearLiteralPath = TurnOnLiteralPathOption(context);
            }

            try
            {
                var fileNameResults = CompleteFilename(context);
                if (fileNameResults != null)
                    result.AddRange(fileNameResults);
            }
            finally
            {
                if (clearLiteralPath)
                    context.Options.Remove("LiteralPaths");
            }

            result.Add(CompletionResult.Null);
        }

        private static IEnumerable<PSTypeName> GetInferenceTypes(CompletionContext context, CommandAst commandAst)
        {
            // Command is something like where-object/foreach-object/format-list/etc. where there is a parameter that is a property name
            // and we want member names based on the input object, which is either the parameter InputObject, or comes from the pipeline.
            if (commandAst.Parent is not PipelineAst pipelineAst)
            {
                return null;
            }

            int i;
            for (i = 0; i < pipelineAst.PipelineElements.Count; i++)
            {
                if (pipelineAst.PipelineElements[i] == commandAst)
                {
                    break;
                }
            }

            IEnumerable<PSTypeName> prevType = null;
            if (i == 0)
            {
                // based on a type of the argument which is binded to 'InputObject' parameter.
                AstParameterArgumentPair pair;
                if (!context.PseudoBindingInfo.BoundArguments.TryGetValue("InputObject", out pair)
                    || !pair.ArgumentSpecified)
                {
                    return null;
                }

                var astPair = pair as AstPair;
                if (astPair == null || astPair.Argument == null)
                {
                    return null;
                }

                prevType = AstTypeInference.InferTypeOf(astPair.Argument, context.TypeInferenceContext, TypeInferenceRuntimePermissions.AllowSafeEval);
            }
            else
            {
                // based on OutputTypeAttribute() of the first cmdlet in pipeline.
                prevType = AstTypeInference.InferTypeOf(pipelineAst.PipelineElements[i - 1], context.TypeInferenceContext, TypeInferenceRuntimePermissions.AllowSafeEval);
            }

            return prevType;
        }

        private static void NativeCompletionMemberName(CompletionContext context, List<CompletionResult> result, CommandAst commandAst, AstParameterArgumentPair parameterInfo, bool propertiesOnly = true)
        {
            IEnumerable<PSTypeName> prevType = TypeInferenceVisitor.GetInferredEnumeratedTypes(GetInferenceTypes(context, commandAst));
            if (prevType is not null)
            {
                HashSet<string> excludedMembers = null;
                if (parameterInfo is AstPair pair)
                {
                    excludedMembers = GetParameterValues(pair, context.CursorPosition.Offset);
                }

                Func<object, bool> filter = propertiesOnly ? IsPropertyMember : null;
                CompleteMemberByInferredType(context.TypeInferenceContext, prevType, result, context.WordToComplete + "*", filter, isStatic: false, excludedMembers, addMethodParenthesis: false);
            }

            result.Add(CompletionResult.Null);
        }

        private static void NativeCompletionMemberValue(CompletionContext context, List<CompletionResult> result, CommandAst commandAst, string propertyName)
        {
            string wordToComplete = context.WordToComplete.Trim('"', '\'');
            IEnumerable<PSTypeName> prevTypes = GetInferenceTypes(context, commandAst);
            if (prevTypes is not null)
            {
                foreach (var type in prevTypes)
                {
                    if (type.Type is null)
                    {
                        continue;
                    }

                    PropertyInfo property = type.Type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (property is not null && property.PropertyType.IsEnum)
                    {
                        foreach (var value in property.PropertyType.GetEnumNames())
                        {
                            if (value.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
                            {
                                result.Add(new CompletionResult(value, value, CompletionResultType.ParameterValue, value));
                            }
                        }

                        break;
                    }
                }
            }

            result.Add(CompletionResult.Null);
        }

        /// <summary>
        /// Returns all string values bound to a parameter except the one the cursor is currently at.
        /// </summary>
        private static HashSet<string>GetParameterValues(AstPair parameter, int cursorOffset)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var parameterValues = parameter.Argument.FindAll(ast => !(cursorOffset >= ast.Extent.StartOffset && cursorOffset <= ast.Extent.EndOffset) && ast is StringConstantExpressionAst, searchNestedScriptBlocks: false);
            foreach (Ast ast in parameterValues)
            {
                result.Add(ast.Extent.Text);
            }

            return result;
        }

        private static void NativeCompletionFormatViewName(
            CompletionContext context,
            Dictionary<string, AstParameterArgumentPair> boundArguments,
            List<CompletionResult> result,
            CommandAst commandAst,
            string commandName)
        {
            IEnumerable<PSTypeName> prevType = NativeCommandArgumentCompletion_InferTypesOfArgument(boundArguments, commandAst, context, "InputObject");

            if (prevType is not null)
            {
                string[] inferTypeNames = prevType.Select(t => t.Name).ToArray();
                CompleteFormatViewByInferredType(context, inferTypeNames, result, commandName);
            }

            result.Add(CompletionResult.Null);
        }

        private static void NativeCompletionTypeName(CompletionContext context, List<CompletionResult> result)
        {
            var wordToComplete = context.WordToComplete;
            var isQuoted = wordToComplete.Length > 0 && (wordToComplete[0].IsSingleQuote() || wordToComplete[0].IsDoubleQuote());
            string prefix = string.Empty;
            string suffix = string.Empty;
            if (isQuoted)
            {
                prefix = suffix = wordToComplete.Substring(0, 1);

                var endQuoted = (wordToComplete.Length > 1) && wordToComplete[wordToComplete.Length - 1] == wordToComplete[0];
                wordToComplete = wordToComplete.Substring(1, wordToComplete.Length - (endQuoted ? 2 : 1));
            }

            if (wordToComplete.Contains('['))
            {
                var cursor = (InternalScriptPosition)context.CursorPosition;
                cursor = cursor.CloneWithNewOffset(cursor.Offset - context.TokenAtCursor.Extent.StartOffset - (isQuoted ? 1 : 0));
                var fullTypeName = Parser.ScanType(wordToComplete, ignoreErrors: true);
                var typeNameToComplete = CompletionAnalysis.FindTypeNameToComplete(fullTypeName, cursor);
                if (typeNameToComplete == null)
                    return;

                var openBrackets = 0;
                var closeBrackets = 0;
                foreach (char c in wordToComplete)
                {
                    if (c == '[') openBrackets += 1;
                    else if (c == ']') closeBrackets += 1;
                }

                wordToComplete = typeNameToComplete.FullName;
                var typeNameText = fullTypeName.Extent.Text;
                if (!isQuoted)
                {
                    // We need to add quotes - the square bracket messes up parsing the argument
                    prefix = suffix = "'";
                }

                if (closeBrackets < openBrackets)
                {
                    suffix = suffix.Insert(0, new string(']', (openBrackets - closeBrackets)));
                }

                if (isQuoted && closeBrackets == openBrackets)
                {
                    // Already quoted, and has matching [].  We can give a better Intellisense experience
                    // if we only replace the minimum.
                    context.ReplacementIndex = typeNameToComplete.Extent.StartOffset + context.TokenAtCursor.Extent.StartOffset + 1;
                    context.ReplacementLength = wordToComplete.Length;
                    prefix = suffix = string.Empty;
                }
                else
                {
                    prefix += typeNameText.Substring(0, typeNameToComplete.Extent.StartOffset);
                    suffix = suffix.Insert(0, typeNameText.Substring(typeNameToComplete.Extent.EndOffset));
                }
            }

            context.WordToComplete = wordToComplete;

            var typeResults = CompleteType(context, prefix, suffix);
            if (typeResults != null)
            {
                result.AddRange(typeResults);
            }

            result.Add(CompletionResult.Null);
        }

        #endregion Native Command Argument Completion

        /// <summary>
        /// Find the positional argument at the specific position from the parsed argument list.
        /// </summary>
        /// <param name="parsedArguments"></param>
        /// <param name="position"></param>
        /// <param name="lastPositionalArgument"></param>
        /// <returns>
        /// If the command line after the [tab] will not be truncated, the return value could be non-null: Get-Cmdlet [tab] abc
        /// If the command line after the [tab] is truncated, the return value will always be null
        /// </returns>
        private static AstPair FindTargetPositionalArgument(Collection<AstParameterArgumentPair> parsedArguments, int position, out AstPair lastPositionalArgument)
        {
            int index = 0;
            lastPositionalArgument = null;
            foreach (AstParameterArgumentPair pair in parsedArguments)
            {
                if (!pair.ParameterSpecified && index == position)
                    return (AstPair)pair;
                else if (!pair.ParameterSpecified)
                {
                    index++;
                    lastPositionalArgument = (AstPair)pair;
                }
            }

            // Cannot find an existing positional argument at 'position'
            return null;
        }

        /// <summary>
        /// Find the location where 'tab' is typed based on the line and column.
        /// </summary>
        private static ArgumentLocation FindTargetArgumentLocation(Collection<AstParameterArgumentPair> parsedArguments, Token token)
        {
            int position = 0;
            AstParameterArgumentPair prevArg = null;
            foreach (AstParameterArgumentPair pair in parsedArguments)
            {
                switch (pair.ParameterArgumentType)
                {
                    case AstParameterArgumentType.AstPair:
                        {
                            var arg = (AstPair)pair;
                            if (arg.ParameterSpecified)
                            {
                                // Named argument
                                if (arg.Parameter.Extent.StartOffset > token.Extent.StartOffset)
                                {
                                    // case: Get-Cmdlet <tab> -Param abc
                                    return GenerateArgumentLocation(prevArg, position);
                                }

                                if ((token.Kind == TokenKind.Parameter && token.Extent.StartOffset == arg.Parameter.Extent.StartOffset)
                                    || (token.Extent.StartOffset > arg.Argument.Extent.StartOffset && token.Extent.EndOffset < arg.Argument.Extent.EndOffset))
                                {
                                    // case 1: Get-Cmdlet -Param <tab> abc
                                    // case 2: dir -Path .\abc.txt, <tab> -File
                                    return new ArgumentLocation() { Argument = arg, IsPositional = false, Position = -1 };
                                }
                            }
                            else
                            {
                                // Positional argument
                                if (arg.Argument.Extent.StartOffset > token.Extent.StartOffset)
                                {
                                    // case: Get-Cmdlet <tab> abc
                                    return GenerateArgumentLocation(prevArg, position);
                                }

                                position++;
                            }

                            prevArg = arg;
                        }

                        break;
                    case AstParameterArgumentType.Fake:
                    case AstParameterArgumentType.Switch:
                        {
                            if (pair.Parameter.Extent.StartOffset > token.Extent.StartOffset)
                            {
                                return GenerateArgumentLocation(prevArg, position);
                            }

                            prevArg = pair;
                        }

                        break;
                    case AstParameterArgumentType.AstArray:
                    case AstParameterArgumentType.PipeObject:
                        Diagnostics.Assert(false, "parsed arguments should not contain AstArray and PipeObject");
                        break;
                }
            }

            // The 'tab' should be typed after the last argument
            return GenerateArgumentLocation(prevArg, position);
        }

        /// <summary>
        /// </summary>
        /// <param name="prev">The argument that is right before the 'tab' location.</param>
        /// <param name="position">The number of positional arguments before the 'tab' location.</param>
        /// <returns></returns>
        private static ArgumentLocation GenerateArgumentLocation(AstParameterArgumentPair prev, int position)
        {
            // Tab is typed before the first argument
            if (prev == null)
            {
                return new ArgumentLocation() { Argument = null, IsPositional = true, Position = 0 };
            }

            switch (prev.ParameterArgumentType)
            {
                case AstParameterArgumentType.AstPair:
                case AstParameterArgumentType.Switch:
                    if (!prev.ParameterSpecified)
                        return new ArgumentLocation() { Argument = null, IsPositional = true, Position = position };

                    return prev.Parameter.Extent.Text.EndsWith(':')
                        ? new ArgumentLocation() { Argument = prev, IsPositional = false, Position = -1 }

                        : new ArgumentLocation() { Argument = null, IsPositional = true, Position = position };
                case AstParameterArgumentType.Fake:
                    return new ArgumentLocation() { Argument = prev, IsPositional = false, Position = -1 };
                default:
                    Diagnostics.Assert(false, "parsed arguments should not contain AstArray and PipeObject");
                    return null;
            }
        }

        /// <summary>
        /// Find the location where 'tab' is typed based on the expressionAst.
        /// </summary>
        /// <param name="parsedArguments"></param>
        /// <param name="expAst"></param>
        /// <returns></returns>
        private static ArgumentLocation FindTargetArgumentLocation(Collection<AstParameterArgumentPair> parsedArguments, ExpressionAst expAst)
        {
            Diagnostics.Assert(expAst != null, "Caller needs to make sure expAst is not null");
            int position = 0;
            foreach (AstParameterArgumentPair pair in parsedArguments)
            {
                switch (pair.ParameterArgumentType)
                {
                    case AstParameterArgumentType.AstPair:
                        {
                            AstPair arg = (AstPair)pair;
                            if (arg.ArgumentIsCommandParameterAst)
                                continue;

                            if (arg.ParameterContainsArgument && arg.Argument == expAst)
                            {
                                return new ArgumentLocation() { IsPositional = false, Position = -1, Argument = arg };
                            }

                            if (arg.Argument.GetHashCode() == expAst.GetHashCode())
                            {
                                return arg.ParameterSpecified ?
                                    new ArgumentLocation() { IsPositional = false, Position = -1, Argument = arg } :
                                    new ArgumentLocation() { IsPositional = true, Position = position, Argument = arg };
                            }

                            if (!arg.ParameterSpecified)
                                position++;
                        }

                        break;
                    case AstParameterArgumentType.Fake:
                    case AstParameterArgumentType.Switch:
                        // FakePair and SwitchPair contains no ExpressionAst
                        break;
                    case AstParameterArgumentType.AstArray:
                    case AstParameterArgumentType.PipeObject:
                        Diagnostics.Assert(false, "parsed arguments should not contain AstArray and PipeObject arguments");
                        break;
                }
            }

            // We should be able to find the ExpAst from the parsed argument list, if all parameters was specified correctly.
            // We may try to complete something incorrect
            // ls -Recurse -QQQ qwe<+tab>
            return null;
        }

        private sealed class ArgumentLocation
        {
            internal bool IsPositional { get; set; }

            internal int Position { get; set; }

            internal AstParameterArgumentPair Argument { get; set; }
        }

        #endregion Command Arguments

        #region Filenames

        /// <summary>
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public static IEnumerable<CompletionResult> CompleteFilename(string fileName)
        {
            var runspace = Runspace.DefaultRunspace;
            if (runspace == null)
            {
                // No runspace, just return no results.
                return CommandCompletion.EmptyCompletionResult;
            }

            var helper = new PowerShellExecutionHelper(PowerShell.Create(RunspaceMode.CurrentRunspace));
            var executionContext = helper.CurrentPowerShell.Runspace.ExecutionContext;
            return CompleteFilename(new CompletionContext { WordToComplete = fileName, Helper = helper, ExecutionContext = executionContext });
        }

        internal static IEnumerable<CompletionResult> CompleteFilename(CompletionContext context)
        {
            return CompleteFilename(context, containerOnly: false, extension: null);
        }

        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        internal static IEnumerable<CompletionResult> CompleteFilename(CompletionContext context, bool containerOnly, HashSet<string> extension)
        {
            var wordToComplete = context.WordToComplete;
            var quote = HandleDoubleAndSingleQuote(ref wordToComplete);

            // First, try to match \\server\share
            // support both / and \ when entering UNC paths for typing convenience (#17111)
            var shareMatch = Regex.Match(wordToComplete, @"^(?:\\\\|//)([^\\/]+)(?:\\|/)([^\\/]*)$");
            if (shareMatch.Success)
            {
                // Only match share names, no filenames.
                var server = shareMatch.Groups[1].Value;
                var sharePattern = WildcardPattern.Get(shareMatch.Groups[2].Value + "*", WildcardOptions.IgnoreCase);
                var ignoreHidden = context.GetOption("IgnoreHiddenShares", @default: false);
                var shares = GetFileShares(server, ignoreHidden);
                if (shares.Count == 0)
                {
                    return CommandCompletion.EmptyCompletionResult;
                }

                var shareResults = new List<CompletionResult>(shares.Count);
                foreach (var share in shares)
                {
                    if (sharePattern.IsMatch(share))
                    {
                        string shareFullPath = "\\\\" + server + "\\" + share;
                        if (quote != string.Empty)
                        {
                            shareFullPath = quote + shareFullPath + quote;
                        }

                        shareResults.Add(new CompletionResult(shareFullPath, shareFullPath, CompletionResultType.ProviderContainer, shareFullPath));
                    }
                }

                return shareResults;
            }

            string filter;
            string basePath;
            int providerSeparatorIndex = -1;
            bool defaultRelativePath = false;
            bool inputUsedHomeChar = false;

            if (string.IsNullOrEmpty(wordToComplete))
            {
                filter = "*";
                basePath = ".";
                defaultRelativePath = true;
            }
            else
            {
                providerSeparatorIndex = wordToComplete.IndexOf("::", StringComparison.Ordinal);
                int pathStartOffset = providerSeparatorIndex == -1 ? 0 : providerSeparatorIndex + 2;
                inputUsedHomeChar = pathStartOffset + 2 <= wordToComplete.Length
                    && wordToComplete[pathStartOffset] is '~'
                    && wordToComplete[pathStartOffset + 1] is '/' or '\\';

                // This simple analysis is quick but doesn't handle scenarios where a separator character is not actually a separator
                // For example "\" or ":" in *nix filenames. This is only a problem if it appears to be the last separator though.
                int lastSeparatorIndex = wordToComplete.LastIndexOfAny(Utils.Separators.DirectoryOrDrive);
                if (lastSeparatorIndex == -1)
                {
                    // Input is a simple word with no path separators like: "Program Files"
                    filter = $"{wordToComplete}*";
                    basePath = ".";
                    defaultRelativePath = true;
                }
                else
                {
                    if (lastSeparatorIndex + 1 == wordToComplete.Length)
                    {
                        // Input ends with a separator like: "./", "filesystem::" or "C:"
                        filter = "*";
                        basePath = wordToComplete;
                    }
                    else
                    {
                        // Input contains a separator, but doesn't end with one like: "C:\Program Fil" or "Registry::HKEY_LOC"
                        filter = $"{wordToComplete.Substring(lastSeparatorIndex + 1)}*";
                        basePath = wordToComplete.Substring(0, lastSeparatorIndex + 1);
                    }

                    if (!inputUsedHomeChar && basePath[0] is not '/' and not '\\')
                    {
                        defaultRelativePath = !context.ExecutionContext.LocationGlobber.IsAbsolutePath(wordToComplete, out _);
                    }
                }
            }

            StringConstantType stringType;
            switch (quote)
            {
                case "":
                    stringType = StringConstantType.BareWord;
                    break;

                case "\"":
                    stringType = StringConstantType.DoubleQuoted;
                    break;

                default:
                    stringType = StringConstantType.SingleQuoted;
                    break;
            }

            var useLiteralPath = context.GetOption("LiteralPaths", @default: false);
            if (useLiteralPath)
            {
                basePath = EscapePath(basePath, stringType, useLiteralPath, out _);
            }

            _ = context.Helper
                .AddCommandWithPreferenceSetting("Microsoft.PowerShell.Management\\Resolve-Path")
                .AddParameter("Path", basePath);

            var resolvedPaths = context.Helper.ExecuteCurrentPowerShell(out _);
            if (resolvedPaths is null || resolvedPaths.Count == 0)
            {
                return CommandCompletion.EmptyCompletionResult;
            }

            var useRelativePath = context.GetOption("RelativePaths", @default: defaultRelativePath);
            if (useRelativePath && providerSeparatorIndex != -1)
            {
                // User must have requested relative paths but that's not valid with provider paths.
                return CommandCompletion.EmptyCompletionResult;
            }

            var resolvedProvider = ((PathInfo)resolvedPaths[0].BaseObject).Provider;
            string providerPrefix;
            if (providerSeparatorIndex == -1)
            {
                providerPrefix = string.Empty;
            }
            else if (providerSeparatorIndex == resolvedProvider.Name.Length)
            {
                providerPrefix = $"{resolvedProvider.Name}::";
            }
            else
            {
                providerPrefix = $"{resolvedProvider.ModuleName}\\{resolvedProvider.Name}::";
            }

            List<CompletionResult> results;
            switch (resolvedProvider.Name)
            {
                case FileSystemProvider.ProviderName:
                    results = GetFileSystemProviderResults(
                        context,
                        resolvedProvider,
                        resolvedPaths,
                        filter,
                        extension,
                        containerOnly,
                        useRelativePath,
                        useLiteralPath,
                        inputUsedHomeChar,
                        providerPrefix,
                        stringType);
                    break;

                default:
                    results = GetDefaultProviderResults(
                        context,
                        resolvedProvider,
                        resolvedPaths,
                        filter,
                        containerOnly,
                        useRelativePath,
                        useLiteralPath,
                        inputUsedHomeChar,
                        providerPrefix,
                        stringType);
                    break;
            }

            return results.OrderBy(x => x.ToolTip);
        }

        /// <summary>
        /// Helper method for generating path completion results for the file system provider.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="provider"></param>
        /// <param name="resolvedPaths"></param>
        /// <param name="filterText"></param>
        /// <param name="includedExtensions"></param>
        /// <param name="containersOnly"></param>
        /// <param name="relativePaths"></param>
        /// <param name="literalPaths"></param>
        /// <param name="inputUsedHome"></param>
        /// <param name="providerPrefix"></param>
        /// <param name="stringType"></param>
        /// <returns></returns>
        private static List<CompletionResult> GetFileSystemProviderResults(
            CompletionContext context,
            ProviderInfo provider,
            Collection<PSObject> resolvedPaths,
            string filterText,
            HashSet<string> includedExtensions,
            bool containersOnly,
            bool relativePaths,
            bool literalPaths,
            bool inputUsedHome,
            string providerPrefix,
            StringConstantType stringType)
        {
#if DEBUG
            Diagnostics.Assert(provider.Name.Equals(FileSystemProvider.ProviderName), "Provider should be filesystem provider.");
#endif
            var enumerationOptions = _enumerationOptions;
            var results = new List<CompletionResult>();
            string homePath = inputUsedHome && !string.IsNullOrEmpty(provider.Home) ? provider.Home : null;

            WildcardPattern wildcardFilter;
            if (WildcardPattern.ContainsRangeWildcard(filterText))
            {
                wildcardFilter = WildcardPattern.Get(filterText, WildcardOptions.IgnoreCase);
                filterText = "*";
            }
            else
            {
                wildcardFilter = null;
            }

            foreach (var item in resolvedPaths)
            {
                var pathInfo = (PathInfo)item.BaseObject;
                var dirInfo = new DirectoryInfo(pathInfo.ProviderPath);

                bool baseQuotesNeeded = false;
                string basePath;
                if (!relativePaths)
                {
                    if (pathInfo.Drive is null)
                    {
                        basePath = dirInfo.FullName;
                    }
                    else
                    {
                        int stringStartIndex = pathInfo.Drive.Root.EndsWith(provider.ItemSeparator) && pathInfo.Drive.Root.Length > 1
                            ? pathInfo.Drive.Root.Length - 1
                            : pathInfo.Drive.Root.Length;

                        basePath = pathInfo.Drive.VolumeSeparatedByColon
                            ? string.Concat(pathInfo.Drive.Name, ":", dirInfo.FullName.AsSpan(stringStartIndex))
                            : string.Concat(pathInfo.Drive.Name, dirInfo.FullName.AsSpan(stringStartIndex));
                    }

                    basePath = basePath.EndsWith(provider.ItemSeparator)
                        ? providerPrefix + basePath
                        : providerPrefix + basePath + provider.ItemSeparator;
                    basePath = RebuildPathWithVars(basePath, homePath, stringType, literalPaths, out baseQuotesNeeded);
                }
                else
                {
                    basePath = null;
                }
                IEnumerable <FileSystemInfo> fileSystemObjects = containersOnly
                    ? dirInfo.EnumerateDirectories(filterText, enumerationOptions)
                    : dirInfo.EnumerateFileSystemInfos(filterText, enumerationOptions);

                foreach (var entry in fileSystemObjects)
                {
                    bool isContainer = entry.Attributes.HasFlag(FileAttributes.Directory);
                    if (!isContainer && includedExtensions is not null && !includedExtensions.Contains(entry.Extension))
                    {
                        continue;
                    }

                    var entryName = entry.Name;
                    if (wildcardFilter is not null && !wildcardFilter.IsMatch(entryName))
                    {
                        continue;
                    }

                    if (basePath is null)
                    {
                        basePath = context.ExecutionContext.EngineSessionState.NormalizeRelativePath(
                            entry.FullName,
                            context.ExecutionContext.SessionState.Internal.CurrentLocation.ProviderPath);
                        if (!basePath.StartsWith($"..{provider.ItemSeparator}", StringComparison.Ordinal))
                        {
                            basePath = $".{provider.ItemSeparator}{basePath}";
                        }

                        basePath = basePath.Remove(basePath.Length - entry.Name.Length);
                        basePath = RebuildPathWithVars(basePath, homePath, stringType, literalPaths, out baseQuotesNeeded);
                    }

                    var resultType = isContainer
                        ? CompletionResultType.ProviderContainer
                        : CompletionResultType.ProviderItem;
                    
                    bool leafQuotesNeeded;
                    var completionText = NewPathCompletionText(
                        basePath,
                        EscapePath(entryName, stringType, literalPaths, out leafQuotesNeeded),
                        stringType,
                        containsNestedExpressions: false,
                        forceQuotes: baseQuotesNeeded || leafQuotesNeeded,
                        addAmpersand: false);
                    results.Add(new CompletionResult(completionText, entryName, resultType, entry.FullName));
                }
            }

            return results;
        }

        /// <summary>
        /// Helper method for generating path completion results standard providers that don't need any special treatment.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="provider"></param>
        /// <param name="resolvedPaths"></param>
        /// <param name="filterText"></param>
        /// <param name="containersOnly"></param>
        /// <param name="relativePaths"></param>
        /// <param name="literalPaths"></param>
        /// <param name="inputUsedHome"></param>
        /// <param name="providerPrefix"></param>
        /// <param name="stringType"></param>
        /// <returns></returns>
        private static List<CompletionResult> GetDefaultProviderResults(
            CompletionContext context,
            ProviderInfo provider,
            Collection<PSObject> resolvedPaths,
            string filterText,
            bool containersOnly,
            bool relativePaths,
            bool literalPaths,
            bool inputUsedHome,
            string providerPrefix,
            StringConstantType stringType)
        {
            string homePath = inputUsedHome && !string.IsNullOrEmpty(provider.Home)
                ? provider.Home
                : null;

            var pattern = WildcardPattern.Get(filterText, WildcardOptions.IgnoreCase);
            var results = new List<CompletionResult>();

            foreach (var item in resolvedPaths)
            {
                var pathInfo = (PathInfo)item.BaseObject;
                string baseTooltip = pathInfo.ProviderPath.Equals(string.Empty, StringComparison.Ordinal)
                    ? pathInfo.Path
                    : pathInfo.ProviderPath;
                if (baseTooltip[^1] is not '\\' and not '/' and not ':')
                {
                    baseTooltip += provider.ItemSeparator;
                }

                _ = context.Helper.CurrentPowerShell
                    .AddCommandWithPreferenceSetting("Microsoft.PowerShell.Management\\Get-ChildItem")
                    .AddParameter("LiteralPath", pathInfo.Path);

                bool hadErrors;
                var childItemOutput = context.Helper.ExecuteCurrentPowerShell(out _, out hadErrors);

                var childrenInfoTable = new Dictionary<string, bool>(childItemOutput.Count);
                var childNameList = new List<string>(childItemOutput.Count);

                if (hadErrors)
                {
                    // Get-ChildItem failed to get some items (Access denied or something)
                    // Save relevant info and try again to get just the names.
                    foreach (dynamic child in childItemOutput)
                    {
                        childrenInfoTable.Add(GetChildNameFromPsObject(child, provider.ItemSeparator), child.PSIsContainer);
                    }

                    _ = context.Helper.CurrentPowerShell
                        .AddCommandWithPreferenceSetting("Microsoft.PowerShell.Management\\Get-ChildItem")
                        .AddParameter("LiteralPath", pathInfo.Path)
                        .AddParameter("Name");
                    childItemOutput = context.Helper.ExecuteCurrentPowerShell(out _);
                    foreach (var child in childItemOutput)
                    {
                        var childName = (string)child.BaseObject;
                        childNameList.Add(childName);
                    }
                }
                else
                {
                    foreach (dynamic child in childItemOutput)
                    {
                        var childName = GetChildNameFromPsObject(child, provider.ItemSeparator);
                        childrenInfoTable.Add(childName, child.PSIsContainer);
                        childNameList.Add(childName);
                    }
                }

                if (childNameList.Count == 0)
                {
                    return results;
                }

                string basePath = providerPrefix.Length > 0
                    ? string.Concat(providerPrefix, pathInfo.Path.AsSpan(providerPrefix.Length))
                    : pathInfo.Path;
                if (basePath[^1] is not '\\' and not '/' and not ':')
                {
                    basePath += provider.ItemSeparator;
                }

                if (relativePaths)
                {
                    basePath = context.ExecutionContext.EngineSessionState.NormalizeRelativePath(
                        basePath + childNameList[0], context.ExecutionContext.SessionState.Internal.CurrentLocation.ProviderPath);
                    if (!basePath.StartsWith($"..{provider.ItemSeparator}", StringComparison.Ordinal))
                    {
                        basePath = $".{provider.ItemSeparator}{basePath}";
                    }

                    basePath = basePath.Remove(basePath.Length - childNameList[0].Length);
                }

                bool baseQuotesNeeded;
                basePath = RebuildPathWithVars(basePath, homePath, stringType, literalPaths, out baseQuotesNeeded);

                foreach (var childName in childNameList)
                {
                    if (!pattern.IsMatch(childName))
                    {
                        continue;
                    }

                    CompletionResultType resultType;
                    if (childrenInfoTable.TryGetValue(childName, out bool isContainer))
                    {
                        if (containersOnly && !isContainer)
                        {
                            continue;
                        }

                        resultType = isContainer
                            ? CompletionResultType.ProviderContainer
                            : CompletionResultType.ProviderItem;
                    }
                    else
                    {
                        resultType = CompletionResultType.Text;
                    }

                    bool leafQuotesNeeded;
                    var completionText = NewPathCompletionText(
                        basePath,
                        EscapePath(childName, stringType, literalPaths, out leafQuotesNeeded),
                        stringType,
                        containsNestedExpressions: false,
                        forceQuotes: baseQuotesNeeded || leafQuotesNeeded,
                        addAmpersand: false);
                    results.Add(new CompletionResult(completionText, childName, resultType, baseTooltip + childName));
                }
            }

            return results;
        }

        private static string GetChildNameFromPsObject(dynamic psObject, char separator)
        {
            if (((PSObject)psObject).BaseObject is string result)
            {
                // The "Get-ChildItem" call for this provider returned a string that we assume is the child name.
                // This is what the SCCM provider returns.
                return result;
            }

            string childName = psObject.PSChildName;
            if (childName is not null)
            {
                return childName;
            }

            // Some providers (Like the variable provider) don't include a PSChildName property
            // so we get the child name from the path instead.
            childName = psObject.PSPath ?? string.Empty;
            int ProviderSeparatorIndex = childName.IndexOf("::", StringComparison.Ordinal);
            childName = childName.Substring(ProviderSeparatorIndex + 2);
            int indexOfName = childName.LastIndexOf(separator);
            if (indexOfName == -1 || indexOfName + 1 == childName.Length)
            {
                return childName;
            }

            return childName.Substring(indexOfName + 1);
        }

        /// <summary>
        /// Takes a path and rebuilds it with the specified variable replacements.
        /// Also escapes special characters as needed.
        /// </summary>
        private static string RebuildPathWithVars(
            string path,
            string homePath,
            StringConstantType stringType,
            bool literalPath,
            out bool quotesAreNeeded)
        {
            var sb = new StringBuilder(path.Length);
            int homeIndex = string.IsNullOrEmpty(homePath)
                ? -1
                : path.IndexOf(homePath, StringComparison.OrdinalIgnoreCase);
            quotesAreNeeded = false;
            bool useSingleQuoteEscapeRules = stringType is StringConstantType.SingleQuoted or StringConstantType.BareWord;

            for (int i = 0; i < path.Length; i++)
            {
                if (i == homeIndex)
                {
                    _ = sb.Append('~');
                    i += homePath.Length - 1;
                    continue;
                }

                EscapeCharIfNeeded(sb, path, i, stringType, literalPath, useSingleQuoteEscapeRules, ref quotesAreNeeded);
                _ = sb.Append(path[i]);
            }

            return sb.ToString();
        }

        private static string EscapePath(string path, StringConstantType stringType, bool literalPath, out bool quotesAreNeeded)
        {
            var sb = new StringBuilder(path.Length);
            bool useSingleQuoteEscapeRules = stringType is StringConstantType.SingleQuoted or StringConstantType.BareWord;
            quotesAreNeeded = false;

            for (int i = 0; i < path.Length; i++)
            {
                EscapeCharIfNeeded(sb, path, i, stringType, literalPath, useSingleQuoteEscapeRules, ref quotesAreNeeded);
                _ = sb.Append(path[i]);
            }

            return sb.ToString();
        }

        private static void EscapeCharIfNeeded(
            StringBuilder sb,
            string path,
            int index,
            StringConstantType stringType,
            bool literalPath,
            bool useSingleQuoteEscapeRules,
            ref bool quotesAreNeeded)
        {
            switch (path[index])
            {
                case '#':
                case '-':
                case '@':
                    if (index == 0 && stringType == StringConstantType.BareWord)
                    {
                        // Chars that would start a new token when used as the first char in a bareword argument.
                        quotesAreNeeded = true;
                    }
                    break;

                case ' ':
                case ',':
                case ';':
                case '(':
                case ')':
                case '{':
                case '}':
                case '|':
                case '&':
                    if (stringType == StringConstantType.BareWord)
                    {
                        // Chars that would start a new token when used anywhere in a bareword argument.
                        quotesAreNeeded = true;
                    }
                    break;

                case '[':
                case ']':
                    if (!literalPath)
                    {
                        // Wildcard characters that need to be escaped.
                        int backtickCount;
                        if (useSingleQuoteEscapeRules)
                        {
                            backtickCount = 1;
                        }
                        else
                        {
                            backtickCount = sb[^1] == '`' ? 4 : 2;
                        }

                        _ = sb.Append('`', backtickCount);
                        quotesAreNeeded = true;
                    }
                    break;

                case '`':
                    // Literal backtick needs to be escaped to not be treated as an escape character
                    if (useSingleQuoteEscapeRules)
                    {
                        if (!literalPath)
                        {
                            _ = sb.Append('`');
                        }
                    }
                    else
                    {
                        int backtickCount = !literalPath && sb[^1] == '`' ? 3 : 1;
                        _ = sb.Append('`', backtickCount);
                    }

                    if (stringType is StringConstantType.BareWord or StringConstantType.DoubleQuoted)
                    {
                        quotesAreNeeded = true;
                    }
                    break;

                case '$':
                    // $ needs to be escaped so following chars are not parsed as a variable/subexpression
                    if (!useSingleQuoteEscapeRules)
                    {
                        _ = sb.Append('`');
                    }

                    if (stringType is StringConstantType.BareWord or StringConstantType.DoubleQuoted)
                    {
                        quotesAreNeeded = true;
                    }
                    break;

                default:
                    // Handle all the different quote types
                    if (useSingleQuoteEscapeRules && path[index].IsSingleQuote())
                    {
                        _ = sb.Append('\'');
                        quotesAreNeeded = true;
                    }
                    else if (!useSingleQuoteEscapeRules && path[index].IsDoubleQuote())
                    {
                        _ = sb.Append('`');
                        quotesAreNeeded = true;
                    }
                    break;
            }
        }

        private static string NewPathCompletionText(string parent, string leaf, StringConstantType stringType, bool containsNestedExpressions, bool forceQuotes, bool addAmpersand)
        {
            string result;
            if (stringType == StringConstantType.SingleQuoted)
            {
                result = addAmpersand ? $"& '{parent}{leaf}'" : $"'{parent}{leaf}'";
            }
            else if (stringType == StringConstantType.DoubleQuoted)
            {
                result = addAmpersand ? $"& \"{parent}{leaf}\"" : $"\"{parent}{leaf}\"";
            }
            else
            {
                if (forceQuotes)
                {
                    if (containsNestedExpressions)
                    {
                        result = addAmpersand ? $"& \"{parent}{leaf}\"" : $"\"{parent}{leaf}\"";
                    }
                    else
                    {
                        result = addAmpersand ? $"& '{parent}{leaf}'" : $"'{parent}{leaf}'";
                    }
                }
                else
                {
                    result = string.Concat(parent, leaf);
                }
            }

            return result;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHARE_INFO_1
        {
            public string netname;
            public int type;
            public string remark;
        }

        private static readonly System.IO.EnumerationOptions _enumerationOptions = new System.IO.EnumerationOptions
        {
            MatchCasing = MatchCasing.CaseInsensitive,
            AttributesToSkip = 0 // Default is to skip Hidden and System files, so we clear this to retain existing behavior
        };

        internal static List<string> GetFileShares(string machine, bool ignoreHidden)
        {
#if UNIX
            return new List<string>();
#else
            nint shBuf = nint.Zero;
            uint numEntries = 0;
            uint totalEntries;
            uint resumeHandle = 0;
            int result = Interop.Windows.NetShareEnum(
                machine,
                level: 1,
                out shBuf,
                Interop.Windows.MAX_PREFERRED_LENGTH,
                out numEntries,
                out totalEntries,
                ref resumeHandle);

            var shares = new List<string>();
            if (result == Interop.Windows.ERROR_SUCCESS || result == Interop.Windows.ERROR_MORE_DATA)
            {
                for (int i = 0; i < numEntries; ++i)
                {
                    nint curInfoPtr = shBuf + (Marshal.SizeOf<SHARE_INFO_1>() * i);
                    SHARE_INFO_1 shareInfo = Marshal.PtrToStructure<SHARE_INFO_1>(curInfoPtr);

                    if ((shareInfo.type & Interop.Windows.STYPE_MASK) != Interop.Windows.STYPE_DISKTREE)
                    {
                        continue;
                    }

                    if (ignoreHidden && shareInfo.netname.EndsWith('$'))
                    {
                        continue;
                    }

                    shares.Add(shareInfo.netname);
                }
            }

            return shares;
#endif
        }

        private static bool CheckFileExtension(string path, HashSet<string> extension)
        {
            if (extension == null || extension.Count == 0)
                return true;

            var ext = System.IO.Path.GetExtension(path);
            return ext == null || extension.Contains(ext);
        }

        #endregion Filenames

        #region Variable

        /// <summary>
        /// </summary>
        /// <param name="variableName"></param>
        /// <returns></returns>
        public static IEnumerable<CompletionResult> CompleteVariable(string variableName)
        {
            var runspace = Runspace.DefaultRunspace;
            if (runspace == null)
            {
                // No runspace, just return no results.
                return CommandCompletion.EmptyCompletionResult;
            }

            var helper = new PowerShellExecutionHelper(PowerShell.Create(RunspaceMode.CurrentRunspace));
            var executionContext = helper.CurrentPowerShell.Runspace.ExecutionContext;
            return CompleteVariable(new CompletionContext { WordToComplete = variableName, Helper = helper, ExecutionContext = executionContext });
        }

        private static readonly string[] s_variableScopes = new string[] { "Global:", "Local:", "Script:", "Private:" };

        private static readonly char[] s_charactersRequiringQuotes = new char[] {
            '-', '`', '&', '@', '\'', '"', '#', '{', '}', '(', ')', '$', ',', ';', '|', '<', '>', ' ', '.', '\\', '/', '\t', '^',
        };

        internal static List<CompletionResult> CompleteVariable(CompletionContext context)
        {
            HashSet<string> hashedResults = new(StringComparer.OrdinalIgnoreCase);
            List<CompletionResult> results = new();
            List<CompletionResult> tempResults = new();

            var wordToComplete = context.WordToComplete;
            var colon = wordToComplete.IndexOf(':');

            var lastAst = context.RelatedAsts?[^1];
            var variableAst = lastAst as VariableExpressionAst;
            var prefix = variableAst != null && variableAst.Splatted ? "@" : "$";
            bool tokenAtCursorUsedBraces = context.TokenAtCursor is not null && context.TokenAtCursor.Text.StartsWith("${");

            // Look for variables in the input (e.g. parameters, etc.) before checking session state - these
            // variables might not exist in session state yet.
            var wildcardPattern = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase);
            if (lastAst is not null)
            {
                Ast parent = lastAst.Parent;
                var findVariablesVisitor = new FindVariablesVisitor
                {
                    CompletionVariableAst = lastAst,
                    StopSearchOffset = lastAst.Extent.StartOffset,
                    Context = context.TypeInferenceContext
                };
                while (parent != null)
                {
                    if (parent is IParameterMetadataProvider)
                    {
                        findVariablesVisitor.Top = parent;
                        parent.Visit(findVariablesVisitor);
                    }

                    parent = parent.Parent;
                }

                foreach (string varName in findVariablesVisitor.FoundVariables)
                {
                    if (!wildcardPattern.IsMatch(varName))
                    {
                        continue;
                    }

                    VariableInfo varInfo = findVariablesVisitor.VariableInfoTable[varName];
                    PSTypeName varType = varInfo.LastDeclaredConstraint ?? varInfo.LastAssignedType;
                    string toolTip;
                    if (varType is null)
                    {
                        toolTip = varName;
                    }
                    else
                    {
                        toolTip = varType.Type is not null
                            ? StringUtil.Format("[{0}]${1}", ToStringCodeMethods.Type(varType.Type, dropNamespaces: true), varName)
                            : varType.Name;
                    }

                    var completionText = !tokenAtCursorUsedBraces && varName.IndexOfAny(s_charactersRequiringQuotes) == -1
                        ? prefix + varName
                        : prefix + "{" + varName + "}";
                    AddUniqueVariable(hashedResults, results, completionText, varName, toolTip);
                }
            }

            if (colon == -1)
            {
                var allVariables = context.ExecutionContext.SessionState.Internal.GetVariableTable();
                foreach (var key in allVariables.Keys)
                {
                    if (wildcardPattern.IsMatch(key))
                    {
                        var variable = allVariables[key];
                        var name = variable.Name;
                        var value = variable.Value;
                        var toolTip = value is null
                            ? key
                            : StringUtil.Format("[{0}]${1}", ToStringCodeMethods.Type(value.GetType(), dropNamespaces: true), key);
                        var completionText = !tokenAtCursorUsedBraces && name.IndexOfAny(s_charactersRequiringQuotes) == -1
                            ? prefix + name
                            : prefix + "{" + name + "}";
                        AddUniqueVariable(hashedResults, tempResults, completionText, key, key);
                    }
                }

                if (tempResults.Count > 0)
                {
                    results.AddRange(tempResults.OrderBy(item => item.ListItemText, StringComparer.OrdinalIgnoreCase));
                    tempResults.Clear();
                }
            }
            else
            {
                string provider = wordToComplete.Substring(0, colon + 1);
                string pattern;
                if (s_variableScopes.Contains(provider, StringComparer.OrdinalIgnoreCase))
                {
                    pattern = string.Concat("variable:", wordToComplete.AsSpan(colon + 1), "*");
                }
                else
                {
                    pattern = wordToComplete + "*";
                }

                var powerShellExecutionHelper = context.Helper;
                powerShellExecutionHelper
                    .AddCommandWithPreferenceSetting("Microsoft.PowerShell.Management\\Get-Item").AddParameter("Path", pattern)
                    .AddCommandWithPreferenceSetting("Microsoft.PowerShell.Utility\\Sort-Object").AddParameter("Property", "Name");

                var psobjs = powerShellExecutionHelper.ExecuteCurrentPowerShell(out _);

                if (psobjs is not null)
                {
                    foreach (dynamic psobj in psobjs)
                    {
                        var name = psobj.Name as string;
                        if (!string.IsNullOrEmpty(name))
                        {
                            var tooltip = name;
                            var variable = PSObject.Base(psobj) as PSVariable;
                            if (variable != null)
                            {
                                var value = variable.Value;
                                if (value != null)
                                {
                                    tooltip = StringUtil.Format("[{0}]${1}",
                                                                ToStringCodeMethods.Type(value.GetType(),
                                                                                         dropNamespaces: true), name);
                                }
                            }

                            var completedName = !tokenAtCursorUsedBraces && name.IndexOfAny(s_charactersRequiringQuotes) == -1
                                                    ? prefix + provider + name
                                                    : prefix + "{" + provider + name + "}";
                            AddUniqueVariable(hashedResults, results, completedName, name, tooltip);
                        }
                    }
                }
            }

            if (colon == -1 && "env".StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
            {
                var envVars = Environment.GetEnvironmentVariables();
                foreach (var key in envVars.Keys)
                {
                    var name = "env:" + key;
                    var completedName = !tokenAtCursorUsedBraces && name.IndexOfAny(s_charactersRequiringQuotes) == -1
                        ? prefix + name
                        : prefix + "{" + name + "}";
                    AddUniqueVariable(hashedResults, tempResults, completedName, name, "[string]" + name);
                }

                results.AddRange(tempResults.OrderBy(item => item.ListItemText, StringComparer.OrdinalIgnoreCase));
                tempResults.Clear();
            }

            // Return variables already in session state first, because we can sometimes give better information,
            // like the variables type.
            foreach (var specialVariable in s_specialVariablesCache.Value)
            {
                if (wildcardPattern.IsMatch(specialVariable))
                {
                    var completedName = !tokenAtCursorUsedBraces && specialVariable.IndexOfAny(s_charactersRequiringQuotes) == -1
                                            ? prefix + specialVariable
                                            : prefix + "{" + specialVariable + "}";

                    AddUniqueVariable(hashedResults, results, completedName, specialVariable, specialVariable);
                }
            }

            if (colon == -1)
            {
                var allDrives = context.ExecutionContext.SessionState.Drive.GetAll();
                foreach (var drive in allDrives)
                {
                    if (drive.Name.Length < 2
                        || !wildcardPattern.IsMatch(drive.Name)
                        || !drive.Provider.ImplementingType.IsAssignableTo(typeof(IContentCmdletProvider)))
                    {
                        continue;
                    }

                    var completedName = !tokenAtCursorUsedBraces && drive.Name.IndexOfAny(s_charactersRequiringQuotes) == -1
                        ? prefix + drive.Name + ":"
                        : prefix + "{" + drive.Name + ":}";
                    var tooltip = string.IsNullOrEmpty(drive.Description)
                        ? drive.Name
                        : drive.Description;
                    AddUniqueVariable(hashedResults, tempResults, completedName, drive.Name, tooltip);
                }

                if (tempResults.Count > 0)
                {
                    results.AddRange(tempResults.OrderBy(item => item.ListItemText, StringComparer.OrdinalIgnoreCase));
                }

                foreach (var scope in s_variableScopes)
                {
                    if (wildcardPattern.IsMatch(scope))
                    {
                        var completedName = !tokenAtCursorUsedBraces && scope.IndexOfAny(s_charactersRequiringQuotes) == -1
                            ? prefix + scope
                            : prefix + "{" + scope + "}";
                        AddUniqueVariable(hashedResults, results, completedName, scope, scope);
                    }
                }
            }

            return results;
        }

        private static void AddUniqueVariable(HashSet<string> hashedResults, List<CompletionResult> results, string completionText, string listItemText, string tooltip)
        {
            if (hashedResults.Add(completionText))
            {
                results.Add(new CompletionResult(completionText, listItemText, CompletionResultType.Variable, tooltip));
            }
        }

        private static readonly HashSet<string> s_varModificationCommands = new(StringComparer.OrdinalIgnoreCase)
        {
            "New-Variable",
            "nv",
            "Set-Variable",
            "set",
            "sv"
        };

        private static readonly string[] s_varModificationParameters = new string[]
        {
            "Name",
            "Value"
        };

        private static readonly string[] s_outVarParameters = new string[]
        {
            "ErrorVariable",
            "ev",
            "WarningVariable",
            "wv",
            "InformationVariable",
            "iv",
            "OutVariable",
            "ov",

        };

        private static readonly string[] s_pipelineVariableParameters = new string[]
        {
            "PipelineVariable",
            "pv"
        };

        private static readonly HashSet<string> s_localScopeCommandNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft.PowerShell.Core\\ForEach-Object",
            "ForEach-Object",
            "foreach",
            "%",
            "Microsoft.PowerShell.Core\\Where-Object",
            "Where-Object",
            "where",
            "?",
            "BeforeAll",
            "BeforeEach"
        };

        private sealed class VariableInfo
        {
            internal PSTypeName LastDeclaredConstraint;
            internal PSTypeName LastAssignedType;
        }

        private sealed class FindVariablesVisitor : AstVisitor
        {
            internal Ast Top;
            internal Ast CompletionVariableAst;
            internal readonly List<string> FoundVariables = new();
            internal readonly Dictionary<string, VariableInfo> VariableInfoTable = new(StringComparer.OrdinalIgnoreCase);
            internal int StopSearchOffset;
            internal TypeInferenceContext Context;

            private static PSTypeName GetInferredVarTypeFromAst(Ast ast)
            {
                PSTypeName type;
                switch (ast)
                {
                    case ConstantExpressionAst constant:
                        type = new PSTypeName(constant.StaticType);
                        break;

                    case ExpandableStringExpressionAst:
                        type = new PSTypeName(typeof(string));
                        break;

                    case ConvertExpressionAst convertExpression:
                        type = new PSTypeName(convertExpression.Type.TypeName);
                        break;

                    case HashtableAst:
                        type = new PSTypeName(typeof(Hashtable));
                        break;

                    case ArrayExpressionAst:
                    case ArrayLiteralAst:
                        type = new PSTypeName(typeof(object[]));
                        break;

                    case ScriptBlockExpressionAst:
                        type = new PSTypeName(typeof(ScriptBlock));
                        break;

                    default:
                        type = null;
                        break;
                }

                return type;
            }

            private void SaveVariableInfo(string variableName, PSTypeName variableType, bool isConstraint)
            {
                if (VariableInfoTable.TryGetValue(variableName, out VariableInfo varInfo))
                {
                    if (isConstraint)
                    {
                        varInfo.LastDeclaredConstraint = variableType;
                    }
                    else
                    {
                        varInfo.LastAssignedType = variableType;
                    }
                }
                else
                {
                    varInfo = isConstraint
                        ? new VariableInfo() { LastDeclaredConstraint = variableType }
                        : new VariableInfo() { LastAssignedType = variableType };
                    VariableInfoTable.Add(variableName, varInfo);
                    FoundVariables.Add(variableName);
                }
            }

            public override AstVisitAction DefaultVisit(Ast ast)
            {
                if (ast.Extent.StartOffset > StopSearchOffset)
                {
                    return AstVisitAction.StopVisit;
                }

                return AstVisitAction.Continue;
            }

            public override AstVisitAction VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst)
            {
                if (assignmentStatementAst.Extent.StartOffset > StopSearchOffset)
                {
                    return AstVisitAction.StopVisit;
                }

                if (assignmentStatementAst.Left is ConvertExpressionAst convertExpression)
                {
                    if (convertExpression.Child is VariableExpressionAst variableExpression)
                    {
                        if (variableExpression == CompletionVariableAst || s_specialVariablesCache.Value.Contains(variableExpression.VariablePath.UserPath))
                        {
                            return AstVisitAction.Continue;
                        }

                        SaveVariableInfo(variableExpression.VariablePath.UserPath, new PSTypeName(convertExpression.Type.TypeName), isConstraint: true);
                    }
                }
                else if (assignmentStatementAst.Left is VariableExpressionAst variableExpression)
                {
                    if (variableExpression == CompletionVariableAst || s_specialVariablesCache.Value.Contains(variableExpression.VariablePath.UserPath))
                    {
                        return AstVisitAction.Continue;
                    }

                    PSTypeName lastAssignedType;
                    if (assignmentStatementAst.Right is CommandExpressionAst commandExpression)
                    {
                        lastAssignedType = GetInferredVarTypeFromAst(commandExpression.Expression);
                    }
                    else
                    {
                        lastAssignedType = null;
                    }

                    SaveVariableInfo(variableExpression.VariablePath.UserPath, lastAssignedType, isConstraint: false);
                }

                return AstVisitAction.Continue;
            }

            public override AstVisitAction VisitCommand(CommandAst commandAst)
            {
                if (commandAst.Extent.StartOffset > StopSearchOffset)
                {
                    return AstVisitAction.StopVisit;
                }

                var commandName = commandAst.GetCommandName();
                if (commandName is not null && s_varModificationCommands.Contains(commandName))
                {
                    StaticBindingResult bindingResult = StaticParameterBinder.BindCommand(commandAst, resolve: false, s_varModificationParameters);
                    if (bindingResult is not null
                        && bindingResult.BoundParameters.TryGetValue("Name", out ParameterBindingResult variableName))
                    {
                        var nameValue = variableName.ConstantValue as string;
                        if (nameValue is not null)
                        {
                            PSTypeName variableType;
                            if (bindingResult.BoundParameters.TryGetValue("Value", out ParameterBindingResult variableValue))
                            {
                                variableType = GetInferredVarTypeFromAst(variableValue.Value);
                            }
                            else
                            {
                                variableType = null;
                            }

                            SaveVariableInfo(nameValue, variableType, isConstraint: false);
                        }
                    }
                }

                var bindResult = StaticParameterBinder.BindCommand(commandAst, resolve: false);
                if (bindResult is not null)
                {
                    foreach (var parameterName in s_outVarParameters)
                    {
                        if (bindResult.BoundParameters.TryGetValue(parameterName, out ParameterBindingResult outVarBind))
                        {
                            var varName = outVarBind.ConstantValue as string;
                            if (varName is not null)
                            {
                                SaveVariableInfo(varName, new PSTypeName(typeof(ArrayList)), isConstraint: false);
                            }
                        }
                    }

                    if (commandAst.Parent is PipelineAst pipeline && pipeline.Extent.EndOffset > CompletionVariableAst.Extent.StartOffset)
                    {
                        foreach (var parameterName in s_pipelineVariableParameters)
                        {
                            if (bindResult.BoundParameters.TryGetValue(parameterName, out ParameterBindingResult pipeVarBind))
                            {
                                var varName = pipeVarBind.ConstantValue as string;
                                if (varName is not null)
                                {
                                    var inferredTypes = AstTypeInference.InferTypeOf(commandAst, Context, TypeInferenceRuntimePermissions.AllowSafeEval);
                                    PSTypeName varType = inferredTypes.Count == 0
                                        ? null
                                        : inferredTypes[0];
                                    SaveVariableInfo(varName, varType, isConstraint: false);
                                }
                            }
                        }
                    }
                }

                return AstVisitAction.Continue;
            }

            public override AstVisitAction VisitParameter(ParameterAst parameterAst)
            {
                if (parameterAst.Extent.StartOffset > StopSearchOffset)
                {
                    return AstVisitAction.StopVisit;
                }

                VariableExpressionAst variableExpression = parameterAst.Name;
                if (variableExpression == CompletionVariableAst)
                {
                    return AstVisitAction.Continue;
                }

                SaveVariableInfo(variableExpression.VariablePath.UserPath, new PSTypeName(parameterAst.StaticType), isConstraint: true);

                return AstVisitAction.Continue;
            }

            public override AstVisitAction VisitForEachStatement(ForEachStatementAst forEachStatementAst)
            {
                if (forEachStatementAst.Extent.StartOffset > StopSearchOffset || forEachStatementAst.Variable == CompletionVariableAst)
                {
                    return AstVisitAction.StopVisit;
                }

                SaveVariableInfo(forEachStatementAst.Variable.VariablePath.UserPath, variableType: null, isConstraint: false);
                return AstVisitAction.Continue;
            }

            public override AstVisitAction VisitAttribute(AttributeAst attributeAst)
            {
                // Attributes can't assign values to variables so they aren't interesting.
                return AstVisitAction.SkipChildren;
            }

            public override AstVisitAction VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst)
            {
                return functionDefinitionAst != Top ? AstVisitAction.SkipChildren : AstVisitAction.Continue;
            }

            public override AstVisitAction VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst)
            {
                if (scriptBlockExpressionAst == Top)
                {
                    return AstVisitAction.Continue;
                }

                Ast parent = scriptBlockExpressionAst.Parent;
                // This loop checks if the scriptblock is used as a command, or an argument for a command, eg: ForEach-Object -Process {$Var1 = "Hello"}, {Var2 = $true}
                while (true)
                {
                    if (parent is CommandAst cmdAst)
                    {
                        string cmdName = cmdAst.GetCommandName();
                        return s_localScopeCommandNames.Contains(cmdName)
                            || (cmdAst.CommandElements[0] is ScriptBlockExpressionAst && cmdAst.InvocationOperator == TokenKind.Dot)
                            ? AstVisitAction.Continue
                            : AstVisitAction.SkipChildren;
                    }

                    if (parent is not CommandExpressionAst and not PipelineAst and not StatementBlockAst and not ArrayExpressionAst and not ArrayLiteralAst)
                    {
                        return AstVisitAction.SkipChildren;
                    }

                    parent = parent.Parent;
                }
            }

            public override AstVisitAction VisitDataStatement(DataStatementAst dataStatementAst)
            {
                if (dataStatementAst.Extent.StartOffset >= StopSearchOffset)
                {
                    return AstVisitAction.StopVisit;
                }

                if (dataStatementAst.Variable is not null)
                {
                    SaveVariableInfo(dataStatementAst.Variable, variableType: null, isConstraint: false);
                }

                return AstVisitAction.SkipChildren;
            }
        }

        private static readonly Lazy<SortedSet<string>> s_specialVariablesCache = new Lazy<SortedSet<string>>(BuildSpecialVariablesCache);

        private static SortedSet<string> BuildSpecialVariablesCache()
        {
            var result = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var member in typeof(SpecialVariables).GetFields(BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (member.FieldType.Equals(typeof(string)))
                {
                    result.Add((string)member.GetValue(null));
                }
            }

            return result;
        }

        internal static PSTypeName GetLastDeclaredTypeConstraint(VariableExpressionAst variableAst, TypeInferenceContext typeInferenceContext)
        {
            Ast parent = variableAst.Parent;
            var findVariablesVisitor = new FindVariablesVisitor()
            {
                CompletionVariableAst = variableAst,
                StopSearchOffset = variableAst.Extent.StartOffset,
                Context = typeInferenceContext
            };
            while (parent != null)
            {
                if (parent is IParameterMetadataProvider)
                {
                    findVariablesVisitor.Top = parent;
                    parent.Visit(findVariablesVisitor);
                }

                if (findVariablesVisitor.VariableInfoTable.TryGetValue(variableAst.VariablePath.UserPath, out VariableInfo varInfo)
                    && varInfo.LastDeclaredConstraint is not null)
                {
                    return varInfo.LastDeclaredConstraint;
                }

                parent = parent.Parent;
            }

            return null;
        }

        #endregion Variables

        #region Comments

        internal static List<CompletionResult> CompleteComment(CompletionContext context, ref int replacementIndex, ref int replacementLength)
        {
            if (context.WordToComplete.StartsWith("<#", StringComparison.Ordinal))
            {
                return CompleteCommentHelp(context, ref replacementIndex, ref replacementLength);
            }

            // Complete #requires statements
            if (context.WordToComplete.StartsWith("#requires ", StringComparison.OrdinalIgnoreCase))
            {
                return CompleteRequires(context, ref replacementIndex, ref replacementLength);
            }

            var results = new List<CompletionResult>();

            // Complete the history entries
            Match matchResult = Regex.Match(context.WordToComplete, @"^#([\w\-]*)$");
            if (!matchResult.Success)
            {
                return results;
            }

            string wordToComplete = matchResult.Groups[1].Value;
            Collection<PSObject> psobjs;

            int entryId;
            if (Regex.IsMatch(wordToComplete, @"^[0-9]+$") && LanguagePrimitives.TryConvertTo(wordToComplete, out entryId))
            {
                context.Helper.AddCommandWithPreferenceSetting("Get-History", typeof(GetHistoryCommand)).AddParameter("Id", entryId);
                psobjs = context.Helper.ExecuteCurrentPowerShell(out _);

                if (psobjs != null && psobjs.Count == 1)
                {
                    var historyInfo = PSObject.Base(psobjs[0]) as HistoryInfo;
                    if (historyInfo != null)
                    {
                        var commandLine = historyInfo.CommandLine;
                        if (!string.IsNullOrEmpty(commandLine))
                        {
                            // var tooltip = "Id: " + historyInfo.Id + "\n" +
                            //               "ExecutionStatus: " + historyInfo.ExecutionStatus + "\n" +
                            //               "StartExecutionTime: " + historyInfo.StartExecutionTime + "\n" +
                            //               "EndExecutionTime: " + historyInfo.EndExecutionTime + "\n";
                            // Use the commandLine as the Tooltip in case the commandLine is multiple lines of scripts
                            results.Add(new CompletionResult(commandLine, commandLine, CompletionResultType.History, commandLine));
                        }
                    }
                }

                return results;
            }

            wordToComplete = "*" + wordToComplete + "*";
            context.Helper.AddCommandWithPreferenceSetting("Get-History", typeof(GetHistoryCommand));

            psobjs = context.Helper.ExecuteCurrentPowerShell(out _);
            var pattern = WildcardPattern.Get(wordToComplete, WildcardOptions.IgnoreCase);

            if (psobjs != null)
            {
                for (int index = psobjs.Count - 1; index >= 0; index--)
                {
                    var psobj = psobjs[index];
                    if (!(PSObject.Base(psobj) is HistoryInfo historyInfo)) continue;

                    var commandLine = historyInfo.CommandLine;
                    if (!string.IsNullOrEmpty(commandLine) && pattern.IsMatch(commandLine))
                    {
                        // var tooltip = "Id: " + historyInfo.Id + "\n" +
                        //               "ExecutionStatus: " + historyInfo.ExecutionStatus + "\n" +
                        //               "StartExecutionTime: " + historyInfo.StartExecutionTime + "\n" +
                        //               "EndExecutionTime: " + historyInfo.EndExecutionTime + "\n";
                        // Use the commandLine as the Tooltip in case the commandLine is multiple lines of scripts
                        results.Add(new CompletionResult(commandLine, commandLine, CompletionResultType.History, commandLine));
                    }
                }
            }

            return results;
        }

        private static List<CompletionResult> CompleteRequires(CompletionContext context, ref int replacementIndex, ref int replacementLength)
        {
            var results = new List<CompletionResult>();

            int cursorIndex = context.CursorPosition.ColumnNumber - 1;
            string lineToCursor = context.CursorPosition.Line.Substring(0, cursorIndex);

            // RunAsAdministrator must be the last parameter in a Requires statement so no completion if the cursor is after the parameter.
            if (lineToCursor.Contains(" -RunAsAdministrator", StringComparison.OrdinalIgnoreCase))
            {
                return results;
            }

            // Regex to find parameter like " -Parameter1" or " -"
            MatchCollection hashtableKeyMatches = Regex.Matches(lineToCursor, @"\s+-([A-Za-z]+|$)");
            if (hashtableKeyMatches.Count == 0)
            {
                return results;
            }

            Group currentParameterMatch = hashtableKeyMatches[^1].Groups[1];

            // Complete the parameter if the cursor is at a parameter
            if (currentParameterMatch.Index + currentParameterMatch.Length == cursorIndex)
            {
                string currentParameterPrefix = currentParameterMatch.Value;

                replacementIndex = context.CursorPosition.Offset - currentParameterPrefix.Length;
                replacementLength = currentParameterPrefix.Length;

                // Produce completions for all parameters that begin with the prefix we've found,
                // but which haven't already been specified in the line we need to complete
                foreach (KeyValuePair<string, string> parameter in s_requiresParameters)
                {
                    if (parameter.Key.StartsWith(currentParameterPrefix, StringComparison.OrdinalIgnoreCase)
                        && !context.CursorPosition.Line.Contains($" -{parameter.Key}", StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(new CompletionResult(parameter.Key, parameter.Key, CompletionResultType.ParameterName, parameter.Value));
                    }
                }

                return results;
            }

            // Regex to find parameter values (any text that appears after various delimiters)
            hashtableKeyMatches = Regex.Matches(lineToCursor, @"(\s+|,|;|{|\""|'|=)(\w+|$)");
            string currentValue;
            if (hashtableKeyMatches.Count == 0)
            {
                currentValue = string.Empty;
            }
            else
            {
                currentValue = hashtableKeyMatches[^1].Groups[2].Value;
            }

            replacementIndex = context.CursorPosition.Offset - currentValue.Length;
            replacementLength = currentValue.Length;

            // Complete PSEdition parameter values
            if (currentParameterMatch.Value.Equals("PSEdition", StringComparison.OrdinalIgnoreCase))
            {
                foreach (KeyValuePair<string, string> psEditionEntry in s_requiresPSEditions)
                {
                    if (psEditionEntry.Key.StartsWith(currentValue, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(new CompletionResult(psEditionEntry.Key, psEditionEntry.Key, CompletionResultType.ParameterValue, psEditionEntry.Value));
                    }
                }

                return results;
            }

            // Complete Modules module specification values
            if (currentParameterMatch.Value.Equals("Modules", StringComparison.OrdinalIgnoreCase))
            {
                int hashtableStart = lineToCursor.LastIndexOf("@{");
                int hashtableEnd = lineToCursor.LastIndexOf('}');

                bool insideHashtable = hashtableStart != -1 && (hashtableEnd == -1 || hashtableEnd < hashtableStart);

                // If not inside a hashtable, try to complete a module simple name
                if (!insideHashtable)
                {
                    context.WordToComplete = currentValue;
                    return CompleteModuleName(context, true);
                }

                string hashtableString = lineToCursor.Substring(hashtableStart);

                // Regex to find hashtable keys with or without quotes
                hashtableKeyMatches = Regex.Matches(hashtableString, @"(@{|;)\s*(?:'|\""|\w*)\w*");

                // Build the list of keys we might want to complete, based on what's already been provided
                var moduleSpecKeysToComplete = new HashSet<string>(s_requiresModuleSpecKeys.Keys);
                bool sawModuleNameLast = false;
                foreach (Match existingHashtableKeyMatch in hashtableKeyMatches)
                {
                    string existingHashtableKey = existingHashtableKeyMatch.Value.TrimStart(s_hashtableKeyPrefixes);

                    if (string.IsNullOrEmpty(existingHashtableKey))
                    {
                        continue;
                    }

                    // Remove the existing key we just saw
                    moduleSpecKeysToComplete.Remove(existingHashtableKey);

                    // We need to remember later if we saw "ModuleName" as the last hashtable key, for completions
                    if (sawModuleNameLast = existingHashtableKey.Equals("ModuleName", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // "RequiredVersion" is mutually exclusive with "ModuleVersion" and "MaximumVersion"
                    if (existingHashtableKey.Equals("ModuleVersion", StringComparison.OrdinalIgnoreCase)
                        || existingHashtableKey.Equals("MaximumVersion", StringComparison.OrdinalIgnoreCase))
                    {
                        moduleSpecKeysToComplete.Remove("RequiredVersion");
                        continue;
                    }

                    if (existingHashtableKey.Equals("RequiredVersion", StringComparison.OrdinalIgnoreCase))
                    {
                        moduleSpecKeysToComplete.Remove("ModuleVersion");
                        moduleSpecKeysToComplete.Remove("MaximumVersion");
                        continue;
                    }
                }

                Group lastHashtableKeyPrefixGroup = hashtableKeyMatches[^1].Groups[0];

                // If we're not completing a key for the hashtable, try to complete module names, but nothing else
                bool completingHashtableKey = lastHashtableKeyPrefixGroup.Index + lastHashtableKeyPrefixGroup.Length == hashtableString.Length;
                if (!completingHashtableKey)
                {
                    if (sawModuleNameLast)
                    {
                        context.WordToComplete = currentValue;
                        return CompleteModuleName(context, true);
                    }

                    return results;
                }

                // Now try to complete hashtable keys
                foreach (string moduleSpecKey in moduleSpecKeysToComplete)
                {
                    if (moduleSpecKey.StartsWith(currentValue, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(new CompletionResult(moduleSpecKey, moduleSpecKey, CompletionResultType.ParameterValue, s_requiresModuleSpecKeys[moduleSpecKey]));
                    }
                }
            }

            return results;
        }

        private static readonly IReadOnlyDictionary<string, string> s_requiresParameters = new SortedList<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Modules", "Specifies PowerShell modules that the script requires." },
            { "PSEdition", "Specifies a PowerShell edition that the script requires." },
            { "RunAsAdministrator", "Specifies that PowerShell must be running as administrator on Windows." },
            { "Version", "Specifies the minimum version of PowerShell that the script requires." },
        };

        private static readonly IReadOnlyDictionary<string, string> s_requiresPSEditions = new SortedList<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Core", "Specifies that the script requires PowerShell Core to run." },
            { "Desktop", "Specifies that the script requires Windows PowerShell to run." },
        };

        private static readonly IReadOnlyDictionary<string, string> s_requiresModuleSpecKeys = new SortedList<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ModuleName", "Required. Specifies the module name." },
            { "GUID", "Optional. Specifies the GUID of the module." },
            { "ModuleVersion", "Specifies a minimum acceptable version of the module." },
            { "RequiredVersion", "Specifies an exact, required version of the module." },
            { "MaximumVersion", "Specifies the maximum acceptable version of the module." },
        };

        private static readonly char[] s_hashtableKeyPrefixes = new[]
        {
            '@',
            '{',
            ';',
            '"',
            '\'',
            ' ',
        };

        private static List<CompletionResult> CompleteCommentHelp(CompletionContext context, ref int replacementIndex, ref int replacementLength)
        {
            // Finds comment keywords like ".DESCRIPTION"
            MatchCollection usedKeywords = Regex.Matches(context.TokenAtCursor.Text, @"(?<=^\s*\.)\w*", RegexOptions.Multiline);
            if (usedKeywords.Count == 0)
            {
                return null;
            }

            // Last keyword at or before the cursor
            Match lineKeyword = null;
            for (int i = usedKeywords.Count - 1; i >= 0; i--)
            {
                Match keyword = usedKeywords[i];
                if (context.CursorPosition.Offset >= keyword.Index + context.TokenAtCursor.Extent.StartOffset)
                {
                    lineKeyword = keyword;
                    break;
                }
            }

            if (lineKeyword is null)
            {
                return null;
            }

            // Cursor is within or at the start/end of the keyword
            if (context.CursorPosition.Offset <= lineKeyword.Index + lineKeyword.Length + context.TokenAtCursor.Extent.StartOffset)
            {
                replacementIndex = context.TokenAtCursor.Extent.StartOffset + lineKeyword.Index;
                replacementLength = lineKeyword.Value.Length;

                var validKeywords = new HashSet<String>(s_commentHelpKeywords.Keys, StringComparer.OrdinalIgnoreCase);
                foreach (Match keyword in usedKeywords)
                {
                    if (keyword == lineKeyword || s_commentHelpAllowedDuplicateKeywords.Contains(keyword.Value))
                    {
                        continue;
                    }

                    validKeywords.Remove(keyword.Value);
                }

                var result = new List<CompletionResult>();
                foreach (string keyword in validKeywords)
                {
                    if (keyword.StartsWith(lineKeyword.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(new CompletionResult(keyword, keyword, CompletionResultType.Keyword, s_commentHelpKeywords[keyword]));
                    }
                }

                return result.Count > 0 ? result : null;
            }

            // Finds the argument for the keyword (any characters following the keyword, ignoring leading/trailing whitespace). For example "C:\New folder"
            Match keywordArgument = Regex.Match(context.CursorPosition.Line, @"(?<=^\s*\.\w+\s+)\S.*(?<=\S)");
            int lineStartIndex = lineKeyword.Index - context.CursorPosition.Line.IndexOf(lineKeyword.Value) + context.TokenAtCursor.Extent.StartOffset;
            int argumentIndex = keywordArgument.Success ? keywordArgument.Index : context.CursorPosition.ColumnNumber - 1;

            replacementIndex = lineStartIndex + argumentIndex;
            replacementLength = keywordArgument.Value.Length;

            if (lineKeyword.Value.Equals("PARAMETER", StringComparison.OrdinalIgnoreCase))
            {
                return CompleteCommentParameterValue(context, keywordArgument.Value);
            }

            if (lineKeyword.Value.Equals("FORWARDHELPTARGETNAME", StringComparison.OrdinalIgnoreCase))
            {
                var result = new List<CompletionResult>(CompleteCommand(keywordArgument.Value, "*", CommandTypes.All));
                return result.Count > 0 ? result : null;
            }

            if (lineKeyword.Value.Equals("FORWARDHELPCATEGORY", StringComparison.OrdinalIgnoreCase))
            {
                var result = new List<CompletionResult>();
                foreach (string category in s_commentHelpForwardCategories)
                {
                    if (category.StartsWith(keywordArgument.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(new CompletionResult(category));
                    }
                }
                return result.Count > 0 ? result : null;
            }

            if (lineKeyword.Value.Equals("REMOTEHELPRUNSPACE", StringComparison.OrdinalIgnoreCase))
            {
                var result = new List<CompletionResult>();
                foreach (CompletionResult variable in CompleteVariable(keywordArgument.Value))
                {
                    // ListItemText is used because it excludes the "$" as expected by REMOTEHELPRUNSPACE.
                    result.Add(new CompletionResult(variable.ListItemText, variable.ListItemText, variable.ResultType, variable.ToolTip));
                }
                return result.Count > 0 ? result : null;
            }

            if (lineKeyword.Value.Equals("EXTERNALHELP", StringComparison.OrdinalIgnoreCase))
            {
                context.WordToComplete = keywordArgument.Value;
                var result = new List<CompletionResult>(CompleteFilename(context, containerOnly: false, (new HashSet<string>() { ".xml" })));
                return result.Count > 0 ? result : null;
            }

            return null;
        }

        private static readonly IReadOnlyDictionary<string, string> s_commentHelpKeywords = new SortedList<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "SYNOPSIS", "A brief description of the function or script. This keyword can be used only once in each topic." },
            { "DESCRIPTION", "A detailed description of the function or script. This keyword can be used only once in each topic." },
            { "PARAMETER", ".PARAMETER  <Parameter-Name>\nThe description of a parameter. Add a .PARAMETER keyword for each parameter in the function or script syntax." },
            { "EXAMPLE", "A sample command that uses the function or script, optionally followed by sample output and a description. Repeat this keyword for each example." },
            { "INPUTS", "The .NET types of objects that can be piped to the function or script. You can also include a description of the input objects." },
            { "OUTPUTS", "The .NET type of the objects that the cmdlet returns. You can also include a description of the returned objects." },
            { "NOTES", "Additional information about the function or script." },
            { "LINK", "The name of a related topic. Repeat the .LINK keyword for each related topic. The .Link keyword content can also include a URI to an online version of the same help topic." },
            { "COMPONENT", "The name of the technology or feature that the function or script uses, or to which it is related." },
            { "ROLE", "The name of the user role for the help topic." },
            { "FUNCTIONALITY", "The keywords that describe the intended use of the function." },
            { "FORWARDHELPTARGETNAME", ".FORWARDHELPTARGETNAME <Command-Name>\nRedirects to the help topic for the specified command." },
            { "FORWARDHELPCATEGORY", ".FORWARDHELPCATEGORY <Category>\nSpecifies the help category of the item in .ForwardHelpTargetName" },
            { "REMOTEHELPRUNSPACE", ".REMOTEHELPRUNSPACE <PSSession-variable>\nSpecifies a session that contains the help topic. Enter a variable that contains a PSSession object." },
            { "EXTERNALHELP", ".EXTERNALHELP <XML Help File>\nThe .ExternalHelp keyword is required when a function or script is documented in XML files." }
        };

        private static readonly HashSet<string> s_commentHelpAllowedDuplicateKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            "PARAMETER",
            "EXAMPLE",
            "LINK"
        };

        private static readonly string[] s_commentHelpForwardCategories = new string[]
        {
            "Alias",
            "Cmdlet",
            "HelpFile",
            "Function",
            "Provider",
            "General",
            "FAQ",
            "Glossary",
            "ScriptCommand",
            "ExternalScript",
            "Filter",
            "All"
        };

        private static FunctionDefinitionAst GetCommentHelpFunctionTarget(CompletionContext context)
        {
            if (context.TokenAtCursor.Kind != TokenKind.Comment)
            {
                return null;
            }

            Ast lastAst = context.RelatedAsts[^1];
            Ast firstAstAfterComment = lastAst.Find(ast => ast.Extent.StartOffset >= context.TokenAtCursor.Extent.EndOffset && ast is not NamedBlockAst, searchNestedScriptBlocks: false);

            // Comment-based help can apply to a following function definition if it starts within 2 lines
            int commentEndLine = context.TokenAtCursor.Extent.EndLineNumber + 2;

            if (lastAst is NamedBlockAst)
            {
                // Helpblock before function inside advanced function
                if (firstAstAfterComment is not null
                    && firstAstAfterComment.Extent.StartLineNumber <= commentEndLine
                    && firstAstAfterComment is FunctionDefinitionAst outerHelpFunctionDefAst)
                {
                    return outerHelpFunctionDefAst;
                }

                // Helpblock inside function
                if (lastAst.Parent.Parent is FunctionDefinitionAst innerHelpFunctionDefAst)
                {
                    return innerHelpFunctionDefAst;
                }
            }

            if (lastAst is ScriptBlockAst)
            {
                // Helpblock before function
                if (firstAstAfterComment is not null
                    && firstAstAfterComment.Extent.StartLineNumber <= commentEndLine
                    && firstAstAfterComment is FunctionDefinitionAst statement)
                {
                    return statement;
                }

                // Advanced function with help inside
                if (lastAst.Parent is FunctionDefinitionAst advFuncDefAst)
                {
                    return advFuncDefAst;
                }
            }

            return null;
        }

        private static List<CompletionResult> CompleteCommentParameterValue(CompletionContext context, string wordToComplete)
        {
            FunctionDefinitionAst foundFunction = GetCommentHelpFunctionTarget(context);

            ReadOnlyCollection<ParameterAst> foundParameters = null;
            if (foundFunction is not null)
            {
                foundParameters = foundFunction.Parameters ?? foundFunction.Body.ParamBlock?.Parameters;
            }
            else if (context.RelatedAsts[^1] is ScriptBlockAst scriptAst)
            {
                // The helpblock is for a script file
                foundParameters = scriptAst.ParamBlock?.Parameters;
            }

            if (foundParameters is null || foundParameters.Count == 0)
            {
                return null;
            }

            var parametersToShow = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ParameterAst parameter in foundParameters)
            {
                if (parameter.Name.VariablePath.UserPath.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
                {
                    parametersToShow.Add(parameter.Name.VariablePath.UserPath);
                }
            }

            MatchCollection usedParameters = Regex.Matches(context.TokenAtCursor.Text, @"(?<=^\s*\.parameter\s+)\w.*(?<=\S)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            foreach (Match parameter in usedParameters)
            {
                if (wordToComplete.Equals(parameter.Value, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                parametersToShow.Remove(parameter.Value);
            }

            var result = new List<CompletionResult>();
            foreach (string parameter in parametersToShow)
            {
                result.Add(new CompletionResult(parameter));
            }

            return result.Count > 0 ? result : null;
        }

        #endregion Comments

        #region Members

        // List of extension methods <MethodName, Signature>
        private static readonly List<Tuple<string, string>> s_extensionMethods =
            new List<Tuple<string, string>>
                {
                    new Tuple<string, string>("Where", "Where({ expression } [, mode [, numberToReturn]])"),
                    new Tuple<string, string>("ForEach", "ForEach(expression [, arguments...])")
                };

        // List of DSC collection-value variables
        private static readonly HashSet<string> s_dscCollectionVariables =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SelectedNodes", "AllNodes" };

        internal static List<CompletionResult> CompleteMember(CompletionContext context, bool @static, ref int replacementLength)
        {
            // If we get here, we know that either:
            //   * the cursor appeared after a member access token ('.' or '::').
            //   * the parent of the ast on the cursor was a member expression.
            //
            // In the first case, we have 2 possibilities:
            //   * the last ast is an error ast because no member name was entered and we were in expression context
            //   * the last ast is a string constant, with something like:   echo $foo.

            var results = new List<CompletionResult>();
            var lastAst = context.RelatedAsts.Last();
            var memberName = "*";
            Ast memberNameCandidateAst = null;
            ExpressionAst targetExpr = null;

            if (lastAst is MemberExpressionAst LastAstAsMemberExpression)
            {
                // If the cursor is not inside the member name in the member expression, assume
                // that the user had incomplete input, but the parser got lucky and succeeded parsing anyway.
                if (context.TokenAtCursor is not null && context.TokenAtCursor.Extent.StartOffset >= LastAstAsMemberExpression.Member.Extent.StartOffset)
                {
                    memberNameCandidateAst = LastAstAsMemberExpression.Member;
                }

                targetExpr = LastAstAsMemberExpression.Expression;
                // Handles scenario where the cursor is after the member access token but before the text
                // like: "".<Tab>Le
                // which completes the member using the partial text after the cursor.
                if (LastAstAsMemberExpression.Member is StringConstantExpressionAst stringExpression && stringExpression.Extent.StartOffset <= context.CursorPosition.Offset)
                {
                    memberName = $"{stringExpression.Value}*";
                }
            }
            else
            {
                memberNameCandidateAst = lastAst;
            }

            if (memberNameCandidateAst is StringConstantExpressionAst memberNameAst)
            {
                // Make sure to correctly handle: echo $foo.
                if (!memberNameAst.Value.Equals(".", StringComparison.OrdinalIgnoreCase) && !memberNameAst.Value.Equals("::", StringComparison.OrdinalIgnoreCase))
                {
                    memberName = memberNameAst.Value + "*";
                }
            }
            else if (lastAst is not ErrorExpressionAst && targetExpr == null)
            {
                // I don't think we can complete anything interesting
                return results;
            }

            if (lastAst.Parent is CommandAst commandAst)
            {
                int i;
                for (i = commandAst.CommandElements.Count - 1; i >= 0; --i)
                {
                    if (commandAst.CommandElements[i] == lastAst)
                    {
                        break;
                    }
                }

                var nextToLastAst = commandAst.CommandElements[i - 1];
                var nextToLastExtent = nextToLastAst.Extent;
                var lastExtent = lastAst.Extent;
                if (nextToLastExtent.EndLineNumber == lastExtent.StartLineNumber &&
                    nextToLastExtent.EndColumnNumber == lastExtent.StartColumnNumber)
                {
                    targetExpr = nextToLastAst as ExpressionAst;
                }
            }
            else if (lastAst.Parent is MemberExpressionAst parentAsMemberExpression)
            {
                if (lastAst is ErrorExpressionAst)
                {
                    // Handles scenarios like $PSVersionTable.PSVersi<tab>.Major.
                    // where the cursor is moved back to a previous member expression while
                    // there's an incomplete member expression at the end
                    targetExpr = parentAsMemberExpression;
                    do
                    {
                        if (targetExpr is MemberExpressionAst memberExpression)
                        {
                            targetExpr = memberExpression.Expression;
                        }
                        else
                        {
                            break;
                        }
                    } while (targetExpr.Extent.EndOffset >= context.CursorPosition.Offset);

                    if (targetExpr.Parent != parentAsMemberExpression
                        && targetExpr.Parent is MemberExpressionAst memberAst
                        && memberAst.Member is StringConstantExpressionAst stringExpression
                        && stringExpression.Extent.StartOffset <= context.CursorPosition.Offset)
                    {
                        memberName = $"{stringExpression.Value}*";
                    }
                }
                // If 'targetExpr' has already been set, we should skip this step. This is for some member completion
                // cases in VSCode, where we may add a new statement in the middle of existing statements as follows:
                //     $xml = New-Object Xml
                //     $xml.
                //     $xml.Save("C:\data.xml")
                // In this example, we add $xml. between two existing statements, and the 'lastAst' in this case is
                // a MemberExpressionAst '$xml.$xml', whose parent is still a MemberExpressionAst '$xml.$xml.Save'.
                // But here we DO NOT want to re-assign 'targetExpr' to be '$xml.$xml'. 'targetExpr' in this case
                // should be '$xml'.
                else
                {
                    targetExpr ??= parentAsMemberExpression.Expression;
                }
            }
            else if (lastAst.Parent is BinaryExpressionAst binaryExpression && context.TokenAtCursor.Kind.Equals(TokenKind.Multiply))
            {
                if (binaryExpression.Left is MemberExpressionAst memberExpression)
                {
                    targetExpr = memberExpression.Expression;
                    if (memberExpression.Member is StringConstantExpressionAst stringExpression)
                    {
                        memberName = $"{stringExpression.Value}*";
                    }
                }
            }
            else if (lastAst.Parent is ErrorStatementAst errorStatement)
            {
                // Handles switches like:
                // switch ($x)
                // {
                //     'RandomString'.<tab>
                //     { }
                // }
                Ast astBeforeMemberAccessToken = null;
                for (int i = errorStatement.Bodies.Count - 1; i >= 0; i--)
                {
                    astBeforeMemberAccessToken = errorStatement.Bodies[i];
                    if (astBeforeMemberAccessToken.Extent.EndOffset < lastAst.Extent.EndOffset)
                    {
                        break;
                    }
                }

                if (astBeforeMemberAccessToken is ExpressionAst expression)
                {
                    targetExpr = expression;
                }
            }

            if (targetExpr == null)
            {
                // Not sure what we have, but we're not looking for members.
                return results;
            }

            if (IsSplattedVariable(targetExpr))
            {
                // It's splatted variable, member expansion is not useful
                return results;
            }

            CompleteMemberHelper(@static, memberName, targetExpr, context, results);

            if (results.Count == 0)
            {
                PSTypeName[] inferredTypes = null;

                if (@static)
                {
                    var typeExpr = targetExpr as TypeExpressionAst;
                    if (typeExpr != null)
                    {
                        inferredTypes = new[] { new PSTypeName(typeExpr.TypeName) };
                    }
                }
                else
                {
                    inferredTypes = AstTypeInference.InferTypeOf(targetExpr, context.TypeInferenceContext, TypeInferenceRuntimePermissions.AllowSafeEval).ToArray();
                }

                if (!@static && inferredTypes.Length == 1 && inferredTypes[0].Name.Equals("System.Void", StringComparison.OrdinalIgnoreCase))
                {
                    return results;
                }

                if (inferredTypes != null && inferredTypes.Length > 0)
                {
                    // Use inferred types if we have any
                    CompleteMemberByInferredType(context.TypeInferenceContext, inferredTypes, results, memberName, filter: null, isStatic: @static);
                }
                else
                {
                    // Handle special DSC collection variables to complete the extension methods 'Where' and 'ForEach'
                    // e.g. Configuration foo { node $AllNodes.<tab> --> $AllNodes.Where(
                    var variableAst = targetExpr as VariableExpressionAst;
                    var memberExprAst = targetExpr as MemberExpressionAst;
                    bool shouldAddExtensionMethods = false;

                    // We complete against extension methods 'Where' and 'ForEach' for the following DSC variables
                    // $SelectedNodes, $AllNodes, $ConfigurationData.AllNodes
                    if (variableAst != null)
                    {
                        // Handle $SelectedNodes and $AllNodes
                        var variablePath = variableAst.VariablePath;
                        if (variablePath.IsVariable && s_dscCollectionVariables.Contains(variablePath.UserPath) && IsInDscContext(variableAst))
                        {
                            shouldAddExtensionMethods = true;
                        }
                    }
                    else if (memberExprAst != null)
                    {
                        // Handle $ConfigurationData.AllNodes
                        var member = memberExprAst.Member as StringConstantExpressionAst;
                        if (IsConfigurationDataVariable(memberExprAst.Expression) && member != null &&
                            string.Equals("AllNodes", member.Value, StringComparison.OrdinalIgnoreCase) &&
                            IsInDscContext(memberExprAst))
                        {
                            shouldAddExtensionMethods = true;
                        }
                    }

                    if (shouldAddExtensionMethods)
                    {
                        CompleteExtensionMethods(memberName, results);
                    }
                }

                if (results.Count == 0)
                {
                    // Handle '$ConfigurationData' specially to complete 'AllNodes' for it
                    if (IsConfigurationDataVariable(targetExpr) && IsInDscContext(targetExpr))
                    {
                        var pattern = WildcardPattern.Get(memberName, WildcardOptions.IgnoreCase);
                        if (pattern.IsMatch("AllNodes"))
                        {
                            results.Add(new CompletionResult("AllNodes", "AllNodes", CompletionResultType.Property, "AllNodes"));
                        }
                    }
                }
            }

            if (memberName != "*" && results.Count > 0)
            {
                // -1 because membername always has a trailing wildcard *
                replacementLength = memberName.Length - 1;
            }

            return results;
        }

        internal static List<CompletionResult> CompleteComparisonOperatorValues(CompletionContext context, ExpressionAst operatorLeftValue)
        {
            var result = new List<CompletionResult>();
            var resolvedTypes = new List<Type>();

            if (SafeExprEvaluator.TrySafeEval(operatorLeftValue, context.ExecutionContext, out object value) && value is not null)
            {
                resolvedTypes.Add(value.GetType());
            }
            else
            {
                var inferredTypes = AstTypeInference.InferTypeOf(operatorLeftValue, context.TypeInferenceContext, TypeInferenceRuntimePermissions.AllowSafeEval);
                foreach (var type in inferredTypes)
                {
                    if (type.Type is not null)
                    {
                        resolvedTypes.Add(type.Type);
                    }
                }
            }

            foreach (var type in resolvedTypes)
            {
                if (type.IsEnum)
                {
                    foreach (var name in type.GetEnumNames())
                    {
                        if (name.StartsWith(context.WordToComplete, StringComparison.OrdinalIgnoreCase))
                        {
                            result.Add(new CompletionResult($"'{name}'", name, CompletionResultType.ParameterValue, name));
                        }
                    }

                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Complete members against extension methods 'Where' and 'ForEach'
        /// </summary>
        private static void CompleteExtensionMethods(string memberName, List<CompletionResult> results, bool addMethodParenthesis = true)
        {
            var pattern = WildcardPattern.Get(memberName, WildcardOptions.IgnoreCase);
            CompleteExtensionMethods(pattern, results, addMethodParenthesis);
        }

        /// <summary>
        /// Complete members against extension methods 'Where' and 'ForEach' based on the given pattern.
        /// </summary>
        private static void CompleteExtensionMethods(WildcardPattern pattern, List<CompletionResult> results, bool addMethodParenthesis)
        {
            foreach (var member in s_extensionMethods)
            {
                if (pattern.IsMatch(member.Item1))
                {
                    string completionText = addMethodParenthesis ? $"{member.Item1}(" : member.Item1;
                    results.Add(new CompletionResult(completionText, member.Item1, CompletionResultType.Method, member.Item2));
                }
            }
        }

        /// <summary>
        /// Verify if an expression Ast is representing the $ConfigurationData variable.
        /// </summary>
        private static bool IsConfigurationDataVariable(ExpressionAst targetExpr)
        {
            var variableExpr = targetExpr as VariableExpressionAst;
            if (variableExpr != null)
            {
                var varPath = variableExpr.VariablePath;
                if (varPath.IsVariable &&
                    varPath.UserPath.Equals("ConfigurationData", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Verify if an expression Ast is within a configuration definition.
        /// </summary>
        private static bool IsInDscContext(ExpressionAst expression)
        {
            return Ast.GetAncestorAst<ConfigurationDefinitionAst>(expression) != null;
        }

        internal static List<CompletionResult> CompleteIndexExpression(CompletionContext context, ExpressionAst indexTarget)
        {
            var result = new List<CompletionResult>();
            object value;
            if (SafeExprEvaluator.TrySafeEval(indexTarget, context.ExecutionContext, out value)
                && value is not null
                && PSObject.Base(value) is IDictionary dictionary)
            {
                foreach (var key in dictionary.Keys)
                {
                    if (key is string keyAsString && keyAsString.StartsWith(context.WordToComplete, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(new CompletionResult($"'{keyAsString}'", keyAsString, CompletionResultType.Property, keyAsString));
                    }
                }
            }
            else
            {
                var inferredTypes = AstTypeInference.InferTypeOf(indexTarget, context.TypeInferenceContext, TypeInferenceRuntimePermissions.AllowSafeEval);
                foreach (var type in inferredTypes)
                {
                    if (type is PSSyntheticTypeName synthetic)
                    {
                        foreach (var member in synthetic.Members)
                        {
                            if (member.Name.StartsWith(context.WordToComplete, StringComparison.OrdinalIgnoreCase))
                            {
                                result.Add(new CompletionResult($"'{member.Name}'", member.Name, CompletionResultType.Property, member.Name));
                            }
                        }
                    }
                }
            }
            return result;
        }

        private static void CompleteFormatViewByInferredType(CompletionContext context, string[] inferredTypeNames, List<CompletionResult> results, string commandName)
        {
            var typeInfoDB = context.TypeInferenceContext.ExecutionContext.FormatDBManager.GetTypeInfoDataBase();

            if (typeInfoDB is null)
            {
                return;
            }

            Type controlBodyType = commandName switch
            {
                "Format-Table" => typeof(TableControlBody),
                "Format-List" => typeof(ListControlBody),
                "Format-Wide" => typeof(WideControlBody),
                "Format-Custom" => typeof(ComplexControlBody),
                _ => null
            };

            Diagnostics.Assert(controlBodyType is not null, "This should never happen unless a new Format-* cmdlet is added");

            var wordToComplete = context.WordToComplete;
            var quote = HandleDoubleAndSingleQuote(ref wordToComplete);
            WildcardPattern viewPattern = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase);

            var uniqueNames = new HashSet<string>();
            foreach (ViewDefinition viewDefinition in typeInfoDB.viewDefinitionsSection.viewDefinitionList)
            {
                if (viewDefinition?.appliesTo is not null && controlBodyType == viewDefinition.mainControl.GetType())
                {
                    foreach (TypeOrGroupReference applyTo in viewDefinition.appliesTo.referenceList)
                    {
                        foreach (string inferredTypeName in inferredTypeNames)
                        {
                            // We use 'StartsWith()' because 'applyTo.Name' can look like "System.Diagnostics.Process#IncludeUserName".
                            if (applyTo.name.StartsWith(inferredTypeName, StringComparison.OrdinalIgnoreCase)
                                && uniqueNames.Add(viewDefinition.name)
                                && viewPattern.IsMatch(viewDefinition.name))
                            {
                                string completionText = viewDefinition.name;
                                // If the string is quoted or if it contains characters that need quoting, quote it in single quotes
                                if (quote != string.Empty || viewDefinition.name.IndexOfAny(s_charactersRequiringQuotes) != -1)
                                {
                                    completionText = "'" + completionText.Replace("'", "''") + "'";
                                }

                                results.Add(new CompletionResult(completionText, viewDefinition.name, CompletionResultType.Text, viewDefinition.name));
                            }
                        }
                    }
                }
            }
        }

        internal static void CompleteMemberByInferredType(
            TypeInferenceContext context,
            IEnumerable<PSTypeName> inferredTypes,
            List<CompletionResult> results,
            string memberName,
            Func<object, bool> filter,
            bool isStatic,
            HashSet<string> excludedMembers = null,
            bool addMethodParenthesis = true,
            bool ignoreTypesWithoutDefaultConstructor = false)
        {
            bool extensionMethodsAdded = false;
            HashSet<string> typeNameUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            WildcardPattern memberNamePattern = WildcardPattern.Get(memberName, WildcardOptions.IgnoreCase);
            foreach (var psTypeName in inferredTypes)
            {
                if (!typeNameUsed.Add(psTypeName.Name)
                    || (ignoreTypesWithoutDefaultConstructor && psTypeName.Type is not null && psTypeName.Type.GetConstructor(Type.EmptyTypes) is null && !psTypeName.Type.IsInterface))
                {
                    continue;
                }

                if (ignoreTypesWithoutDefaultConstructor && psTypeName.TypeDefinitionAst is not null)
                {
                    bool foundConstructor = false;
                    bool foundDefaultConstructor = false;
                    foreach (var member in psTypeName.TypeDefinitionAst.Members)
                    {
                        if (member is FunctionMemberAst methodDefinition && methodDefinition.IsConstructor)
                        {
                            foundConstructor = true;
                            if (methodDefinition.Parameters.Count == 0)
                            {
                                foundDefaultConstructor = true;
                                break;
                            }
                        }
                    }
                    if (foundConstructor && !foundDefaultConstructor)
                    {
                        continue;
                    }
                }

                var members = context.GetMembersByInferredType(psTypeName, isStatic, filter);
                foreach (var member in members)
                {
                    AddInferredMember(member, memberNamePattern, results, excludedMembers, addMethodParenthesis);
                }

                // Check if we need to complete against the extension methods 'Where' and 'ForEach'
                if (!extensionMethodsAdded && psTypeName.Type != null && IsStaticTypeEnumerable(psTypeName.Type))
                {
                    // Complete extension methods 'Where' and 'ForEach' for Enumerable types
                    extensionMethodsAdded = true;
                    CompleteExtensionMethods(memberNamePattern, results, addMethodParenthesis);
                }
            }

            if (results.Count > 0)
            {
                // Sort the results
                var powerShellExecutionHelper = context.Helper;
                powerShellExecutionHelper
                    .AddCommandWithPreferenceSetting("Microsoft.PowerShell.Utility\\Sort-Object")
                    .AddParameter("Property", new[] { "ResultType", "ListItemText" })
                    .AddParameter("Unique");
                var sortedResults = powerShellExecutionHelper.ExecuteCurrentPowerShell(out _, results);
                results.Clear();
                results.AddRange(sortedResults.Select(static psobj => PSObject.Base(psobj) as CompletionResult));
            }
        }

        private static void AddInferredMember(object member, WildcardPattern memberNamePattern, List<CompletionResult> results, HashSet<string> excludedMembers, bool addMethodParenthesis)
        {
            string memberName = null;
            bool isMethod = false;
            Func<string> getToolTip = null;
            var propertyInfo = member as PropertyInfo;
            if (propertyInfo != null)
            {
                memberName = propertyInfo.Name;
                getToolTip = () => ToStringCodeMethods.Type(propertyInfo.PropertyType) + " " + memberName
                    + " { " + (propertyInfo.GetGetMethod() != null ? "get; " : string.Empty)
                    + (propertyInfo.GetSetMethod() != null ? "set; " : string.Empty) + "}";
            }

            var fieldInfo = member as FieldInfo;
            if (fieldInfo != null)
            {
                memberName = fieldInfo.Name;
                getToolTip = () => ToStringCodeMethods.Type(fieldInfo.FieldType) + " " + memberName;
            }

            var methodCacheEntry = member as DotNetAdapter.MethodCacheEntry;
            if (methodCacheEntry != null)
            {
                memberName = methodCacheEntry[0].method.Name;
                isMethod = true;
                getToolTip = () => string.Join('\n', methodCacheEntry.methodInformationStructures.Select(static m => m.methodDefinition));
            }

            var psMemberInfo = member as PSMemberInfo;
            if (psMemberInfo != null)
            {
                memberName = psMemberInfo.Name;
                isMethod = member is PSMethodInfo;
                getToolTip = psMemberInfo.ToString;
            }

            var cimProperty = member as CimPropertyDeclaration;
            if (cimProperty != null)
            {
                memberName = cimProperty.Name;
                isMethod = false;
                getToolTip = () => GetCimPropertyToString(cimProperty);
            }

            if (member is MemberAst memberAst)
            {
                if (memberAst is CompilerGeneratedMemberFunctionAst)
                {
                    memberName = "new";
                    isMethod = true;
                }
                else if (memberAst is FunctionMemberAst functionMember)
                {
                    memberName = functionMember.IsConstructor ? "new" : functionMember.Name;
                    isMethod = true;
                }
                else
                {
                    memberName = memberAst.Name;
                    isMethod = false;
                }
                getToolTip = memberAst.GetTooltip;
            }

            if (memberName == null || !memberNamePattern.IsMatch(memberName) || (excludedMembers is not null && excludedMembers.Contains(memberName)))
            {
                return;
            }

            var completionResultType = isMethod ? CompletionResultType.Method : CompletionResultType.Property;
            string completionText;
            if (isMethod && addMethodParenthesis)
            {
                completionText = $"{memberName}(";
            }
            else if (memberName.IndexOfAny(s_charactersRequiringQuotes) != -1)
            {
                completionText = $"'{memberName}'";
            }
            else
            {
                completionText = memberName;
            }

            results.Add(new CompletionResult(completionText, memberName, completionResultType, getToolTip()));
        }

        private static string GetCimPropertyToString(CimPropertyDeclaration cimProperty)
        {
            string type;
            switch (cimProperty.CimType)
            {
                case Microsoft.Management.Infrastructure.CimType.DateTime:
                case Microsoft.Management.Infrastructure.CimType.Instance:
                case Microsoft.Management.Infrastructure.CimType.Reference:
                case Microsoft.Management.Infrastructure.CimType.DateTimeArray:
                case Microsoft.Management.Infrastructure.CimType.InstanceArray:
                case Microsoft.Management.Infrastructure.CimType.ReferenceArray:
                    type = "CimInstance#" + cimProperty.CimType.ToString();
                    break;

                default:
                    type = ToStringCodeMethods.Type(CimConverter.GetDotNetType(cimProperty.CimType));
                    break;
            }

            bool isReadOnly = ((cimProperty.Flags & CimFlags.ReadOnly) == CimFlags.ReadOnly);
            return type + " " + cimProperty.Name + " { get; " + (isReadOnly ? "}" : "set; }");
        }

        private static bool IsWriteablePropertyMember(object member)
        {
            var propertyInfo = member as PropertyInfo;
            if (propertyInfo != null)
            {
                return propertyInfo.CanWrite;
            }

            var psPropertyInfo = member as PSPropertyInfo;
            if (psPropertyInfo != null)
            {
                return psPropertyInfo.IsSettable;
            }

            if (member is PropertyMemberAst)
            {
                // Properties in PowerShell classes are always writeable
                return true;
            }

            return false;
        }

        internal static bool IsPropertyMember(object member)
        {
            return member is PropertyInfo
                   || member is FieldInfo
                   || member is PSPropertyInfo
                   || member is CimPropertyDeclaration
                   || member is PropertyMemberAst;
        }

        private static bool IsMemberHidden(object member)
        {
            var psMemberInfo = member as PSMemberInfo;
            if (psMemberInfo != null)
                return psMemberInfo.IsHidden;

            var memberInfo = member as MemberInfo;
            if (memberInfo != null)
                return memberInfo.GetCustomAttributes(typeof(HiddenAttribute), false).Length > 0;

            var propertyMemberAst = member as PropertyMemberAst;
            if (propertyMemberAst != null)
                return propertyMemberAst.IsHidden;

            var functionMemberAst = member as FunctionMemberAst;
            if (functionMemberAst != null)
                return functionMemberAst.IsHidden;

            return false;
        }

        private static bool IsConstructor(object member)
        {
            var psMethod = member as PSMethod;
            if (psMethod != null)
            {
                var methodCacheEntry = psMethod.adapterData as DotNetAdapter.MethodCacheEntry;
                if (methodCacheEntry != null)
                {
                    return methodCacheEntry.methodInformationStructures[0].method.IsConstructor;
                }
            }

            return false;
        }

        #endregion Members

        #region Types

        private abstract class TypeCompletionBase
        {
            internal abstract CompletionResult GetCompletionResult(string keyMatched, string prefix, string suffix);

            internal abstract CompletionResult GetCompletionResult(string keyMatched, string prefix, string suffix, string namespaceToRemove);

            internal static string RemoveBackTick(string typeName)
            {
                var backtick = typeName.LastIndexOf('`');
                return backtick == -1 ? typeName : typeName.Substring(0, backtick);
            }
        }

        /// <summary>
        /// In OneCore PS, there is no way to retrieve all loaded assemblies. But we have the type catalog dictionary
        /// which contains the full type names of all available CoreCLR .NET types. We can extract the necessary info
        /// from the full type names to make type name auto-completion work.
        /// This type represents a non-generic type for type name completion. It only contains information that can be
        /// inferred from the full type name.
        /// </summary>
        private class TypeCompletionInStringFormat : TypeCompletionBase
        {
            /// <summary>
            /// Get the full type name of the type represented by this instance.
            /// </summary>
            internal string FullTypeName;

            /// <summary>
            /// Get the short type name of the type represented by this instance.
            /// </summary>
            internal string ShortTypeName
            {
                get
                {
                    if (_shortTypeName == null)
                    {
                        int lastDotIndex = FullTypeName.LastIndexOf('.');
                        int lastPlusIndex = FullTypeName.LastIndexOf('+');
                        _shortTypeName = lastPlusIndex != -1
                                           ? FullTypeName.Substring(lastPlusIndex + 1)
                                           : FullTypeName.Substring(lastDotIndex + 1);
                    }

                    return _shortTypeName;
                }
            }

            private string _shortTypeName;

            /// <summary>
            /// Get the namespace of the type represented by this instance.
            /// </summary>
            internal string Namespace
            {
                get
                {
                    if (_namespace == null)
                    {
                        int lastDotIndex = FullTypeName.LastIndexOf('.');
                        _namespace = FullTypeName.Substring(0, lastDotIndex);
                    }

                    return _namespace;
                }
            }

            private string _namespace;

            /// <summary>
            /// Construct the CompletionResult based on the information of this instance.
            /// </summary>
            internal override CompletionResult GetCompletionResult(string keyMatched, string prefix, string suffix)
            {
                return GetCompletionResult(keyMatched, prefix, suffix, null);
            }

            /// <summary>
            /// Construct the CompletionResult based on the information of this instance.
            /// </summary>
            internal override CompletionResult GetCompletionResult(string keyMatched, string prefix, string suffix, string namespaceToRemove)
            {
                string completion = string.IsNullOrEmpty(namespaceToRemove)
                                        ? FullTypeName
                                        : FullTypeName.Substring(namespaceToRemove.Length + 1);

                string listItem = ShortTypeName;
                string tooltip = FullTypeName;

                return new CompletionResult(prefix + completion + suffix, listItem, CompletionResultType.Type, tooltip);
            }
        }

        /// <summary>
        /// In OneCore PS, there is no way to retrieve all loaded assemblies. But we have the type catalog dictionary
        /// which contains the full type names of all available CoreCLR .NET types. We can extract the necessary info
        /// from the full type names to make type name auto-completion work.
        /// This type represents a generic type for type name completion. It only contains information that can be
        /// inferred from the full type name.
        /// </summary>
        private sealed class GenericTypeCompletionInStringFormat : TypeCompletionInStringFormat
        {
            /// <summary>
            /// Get the number of generic type arguments required by the type represented by this instance.
            /// </summary>
            private int GenericArgumentCount
            {
                get
                {
                    if (_genericArgumentCount == 0)
                    {
                        var backtick = FullTypeName.LastIndexOf('`');
                        var argCount = FullTypeName.Substring(backtick + 1);
                        _genericArgumentCount = LanguagePrimitives.ConvertTo<int>(argCount);
                    }

                    return _genericArgumentCount;
                }
            }

            private int _genericArgumentCount = 0;

            /// <summary>
            /// Construct the CompletionResult based on the information of this instance.
            /// </summary>
            internal override CompletionResult GetCompletionResult(string keyMatched, string prefix, string suffix)
            {
                return GetCompletionResult(keyMatched, prefix, suffix, null);
            }

            /// <summary>
            /// Construct the CompletionResult based on the information of this instance.
            /// </summary>
            internal override CompletionResult GetCompletionResult(string keyMatched, string prefix, string suffix, string namespaceToRemove)
            {
                string fullNameWithoutBacktip = RemoveBackTick(FullTypeName);
                string completion = string.IsNullOrEmpty(namespaceToRemove)
                                        ? fullNameWithoutBacktip
                                        : fullNameWithoutBacktip.Substring(namespaceToRemove.Length + 1);

                string typeName = RemoveBackTick(ShortTypeName);
                var listItem = typeName + "<>";

                var tooltip = new StringBuilder();
                tooltip.Append(fullNameWithoutBacktip);
                tooltip.Append('[');

                for (int i = 0; i < GenericArgumentCount; i++)
                {
                    if (i != 0) tooltip.Append(", ");
                    tooltip.Append(GenericArgumentCount == 1
                                       ? "T"
                                       : string.Create(CultureInfo.InvariantCulture, $"T{i + 1}"));
                }

                tooltip.Append(']');

                return new CompletionResult(prefix + completion + suffix, listItem, CompletionResultType.Type, tooltip.ToString());
            }
        }

        /// <summary>
        /// This type represents a non-generic type for type name completion. It contains the actual type instance.
        /// </summary>
        private class TypeCompletion : TypeCompletionBase
        {
            internal Type Type;

            protected string GetTooltipPrefix()
            {
                if (typeof(Delegate).IsAssignableFrom(Type))
                    return "Delegate ";
                if (Type.IsInterface)
                    return "Interface ";
                if (Type.IsClass)
                    return "Class ";
                if (Type.IsEnum)
                    return "Enum ";
                if (typeof(ValueType).IsAssignableFrom(Type))
                    return "Struct ";

                return string.Empty; // what other interesting types are there?
            }

            internal override CompletionResult GetCompletionResult(string keyMatched, string prefix, string suffix)
            {
                return GetCompletionResult(keyMatched, prefix, suffix, null);
            }

            internal override CompletionResult GetCompletionResult(string keyMatched, string prefix, string suffix, string namespaceToRemove)
            {
                string completion = ToStringCodeMethods.Type(Type, false, keyMatched);

                // If the completion included a namespace and ToStringCodeMethods.Type found
                // an accelerator, then just use the type's FullName instead because the user
                // probably didn't want the accelerator.
                if (keyMatched.Contains('.') && !completion.Contains('.'))
                {
                    completion = Type.FullName;
                }

                if (!string.IsNullOrEmpty(namespaceToRemove) && completion.Equals(Type.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    // Remove the namespace only if the completion text contains namespace
                    completion = completion.Substring(namespaceToRemove.Length + 1);
                }

                string listItem = Type.Name;
                string tooltip = GetTooltipPrefix() + Type.FullName;

                return new CompletionResult(prefix + completion + suffix, listItem, CompletionResultType.Type, tooltip);
            }
        }

        /// <summary>
        /// This type represents a generic type for type name completion. It contains the actual type instance.
        /// </summary>
        private sealed class GenericTypeCompletion : TypeCompletion
        {
            internal override CompletionResult GetCompletionResult(string keyMatched, string prefix, string suffix)
            {
                return GetCompletionResult(keyMatched, prefix, suffix, null);
            }

            internal override CompletionResult GetCompletionResult(string keyMatched, string prefix, string suffix, string namespaceToRemove)
            {
                string fullNameWithoutBacktip = RemoveBackTick(Type.FullName);
                string completion = string.IsNullOrEmpty(namespaceToRemove)
                                        ? fullNameWithoutBacktip
                                        : fullNameWithoutBacktip.Substring(namespaceToRemove.Length + 1);

                string typeName = RemoveBackTick(Type.Name);
                var listItem = typeName + "<>";

                var tooltip = new StringBuilder();
                tooltip.Append(GetTooltipPrefix());
                tooltip.Append(fullNameWithoutBacktip);
                tooltip.Append('[');
                var genericParameters = Type.GetGenericArguments();
                for (int i = 0; i < genericParameters.Length; i++)
                {
                    if (i != 0) tooltip.Append(", ");
                    tooltip.Append(genericParameters[i].Name);
                }

                tooltip.Append(']');

                return new CompletionResult(prefix + completion + suffix, listItem, CompletionResultType.Type, tooltip.ToString());
            }
        }

        /// <summary>
        /// This type represents a namespace for namespace completion.
        /// </summary>
        private sealed class NamespaceCompletion : TypeCompletionBase
        {
            internal string Namespace;

            internal override CompletionResult GetCompletionResult(string keyMatched, string prefix, string suffix)
            {
                var listItemText = Namespace;
                var dotIndex = listItemText.LastIndexOf('.');
                if (dotIndex != -1)
                {
                    listItemText = listItemText.Substring(dotIndex + 1);
                }

                return new CompletionResult(prefix + Namespace + suffix, listItemText, CompletionResultType.Namespace, "Namespace " + Namespace);
            }

            internal override CompletionResult GetCompletionResult(string keyMatched, string prefix, string suffix, string namespaceToRemove)
            {
                return GetCompletionResult(keyMatched, prefix, suffix);
            }
        }

        private sealed class TypeCompletionMapping
        {
            // The Key is the string we'll be searching on.  It could complete to various things.
            internal string Key;
            internal List<TypeCompletionBase> Completions = new List<TypeCompletionBase>();
        }

        private static TypeCompletionMapping[][] s_typeCache;

        private static TypeCompletionMapping[][] InitializeTypeCache()
        {
            #region Process_TypeAccelerators

            var entries = new Dictionary<string, TypeCompletionMapping>(StringComparer.OrdinalIgnoreCase);
            foreach (var type in TypeAccelerators.Get)
            {
                TypeCompletionMapping entry;
                var typeCompletionInstance = new TypeCompletion { Type = type.Value };

                if (entries.TryGetValue(type.Key, out entry))
                {
                    // Check if this accelerator type is already included in the mapping entry referenced by the same key.
                    Type acceleratorType = type.Value;
                    bool typeAlreadyIncluded = entry.Completions.Any(
                        item =>
                            {
                                var typeCompletion = item as TypeCompletion;
                                return typeCompletion != null && typeCompletion.Type == acceleratorType;
                            });

                    // If it's already included, skip it.
                    // This may happen when an accelerator name is the same as the short name of the type it represents,
                    // and aslo that type has more than one accelerator names. For example:
                    //    "float"  -> System.Single
                    //    "single" -> System.Single
                    if (typeAlreadyIncluded) { continue; }

                    // If this accelerator type is not included in the mapping entry, add it in.
                    // This may happen when an accelerator name happens to be the short name of a different type (rare case).
                    entry.Completions.Add(typeCompletionInstance);
                }
                else
                {
                    entries.Add(type.Key, new TypeCompletionMapping { Key = type.Key, Completions = { typeCompletionInstance } });
                }

                // If the full type name has already been included, then we know for sure that the short type name has also been included.
                string fullTypeName = type.Value.FullName;
                if (entries.ContainsKey(fullTypeName)) { continue; }

                // Otherwise, add the mapping from full type name to the type
                entries.Add(fullTypeName, new TypeCompletionMapping { Key = fullTypeName, Completions = { typeCompletionInstance } });

                // If the short type name is the same as the accelerator name, then skip it to avoid duplication.
                string shortTypeName = type.Value.Name;
                if (type.Key.Equals(shortTypeName, StringComparison.OrdinalIgnoreCase)) { continue; }

                // Otherwise, add a new mapping entry, or put the TypeCompletion instance in the existing mapping entry.
                // For example, this may happen if both System.TimeoutException and System.ServiceProcess.TimeoutException
                // are in the TypeAccelerator cache.
                if (!entries.TryGetValue(shortTypeName, out entry))
                {
                    entry = new TypeCompletionMapping { Key = shortTypeName };
                    entries.Add(shortTypeName, entry);
                }

                entry.Completions.Add(typeCompletionInstance);
            }

            #endregion Process_TypeAccelerators

            #region Process_CoreCLR_TypeCatalog

            // In CoreCLR, we have namespace-qualified type names of all available .NET Core types stored in TypeCatalog.
            // Populate the type completion cache using the namespace-qualified type names.
            foreach (string fullTypeName in ClrFacade.AvailableDotNetTypeNames)
            {
                var typeCompInString = new TypeCompletionInStringFormat { FullTypeName = fullTypeName };
                HandleNamespace(entries, typeCompInString.Namespace);
                HandleType(entries, fullTypeName, typeCompInString.ShortTypeName, null);
            }

            #endregion Process_CoreCLR_TypeCatalog

            #region Process_LoadedAssemblies

            foreach (Assembly assembly in ClrFacade.GetAssemblies())
            {
                // Ignore the assemblies that are already covered by the type catalog
                if (ClrFacade.AvailableDotNetAssemblyNames.Contains(assembly.FullName)) { continue; }

                try
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        // Ignore non-public types
                        if (!TypeResolver.IsPublic(type)) { continue; }

                        HandleNamespace(entries, type.Namespace);
                        HandleType(entries, type.FullName, type.Name, type);
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                }
            }

            #endregion Process_LoadedAssemblies

            var grouping = entries.Values.GroupBy(static t => t.Key.Count(c => c == '.')).OrderBy(static g => g.Key).ToArray();
            var localTypeCache = new TypeCompletionMapping[grouping.Last().Key + 1][];
            foreach (var group in grouping)
            {
                localTypeCache[group.Key] = group.ToArray();
            }

            Interlocked.Exchange(ref s_typeCache, localTypeCache);
            return localTypeCache;
        }

        /// <summary>
        /// Handle namespace when initializing the type cache.
        /// </summary>
        /// <param name="entryCache">The TypeCompletionMapping dictionary.</param>
        /// <param name="namespace">The namespace.</param>
        private static void HandleNamespace(Dictionary<string, TypeCompletionMapping> entryCache, string @namespace)
        {
            if (string.IsNullOrEmpty(@namespace))
            {
                return;
            }

            int dotIndex = 0;
            while (dotIndex != -1)
            {
                dotIndex = @namespace.IndexOf('.', dotIndex + 1);
                string subNamespace = dotIndex != -1
                                        ? @namespace.Substring(0, dotIndex)
                                        : @namespace;

                TypeCompletionMapping entry;
                if (!entryCache.TryGetValue(subNamespace, out entry))
                {
                    entry = new TypeCompletionMapping
                    {
                        Key = subNamespace,
                        Completions = { new NamespaceCompletion { Namespace = subNamespace } }
                    };
                    entryCache.Add(subNamespace, entry);
                }
                else if (!entry.Completions.OfType<NamespaceCompletion>().Any())
                {
                    entry.Completions.Add(new NamespaceCompletion { Namespace = subNamespace });
                }
            }
        }

        /// <summary>
        /// Handle a type when initializing the type cache.
        /// </summary>
        /// <param name="entryCache">The TypeCompletionMapping dictionary.</param>
        /// <param name="fullTypeName">The full type name.</param>
        /// <param name="shortTypeName">The short type name.</param>
        /// <param name="actualType">The actual type object. It may be null if we are handling type information from the CoreCLR TypeCatalog.</param>
        private static void HandleType(Dictionary<string, TypeCompletionMapping> entryCache, string fullTypeName, string shortTypeName, Type actualType)
        {
            if (string.IsNullOrEmpty(fullTypeName)) { return; }

            TypeCompletionBase typeCompletionBase = null;
            var backtick = fullTypeName.LastIndexOf('`');
            var plusChar = fullTypeName.LastIndexOf('+');

            bool isGenericTypeDefinition = backtick != -1;
            bool isNested = plusChar != -1;

            if (isGenericTypeDefinition)
            {
                // Nested generic types aren't useful for completion.
                if (isNested) { return; }

                typeCompletionBase = actualType != null
                                         ? (TypeCompletionBase)new GenericTypeCompletion { Type = actualType }

                                         : new GenericTypeCompletionInStringFormat { FullTypeName = fullTypeName };

                // Remove the backtick, we only want 1 generic in our results for types like Func or Action.
                fullTypeName = fullTypeName.Substring(0, backtick);
                shortTypeName = shortTypeName.Substring(0, shortTypeName.LastIndexOf('`'));
            }
            else
            {
                typeCompletionBase = actualType != null
                                         ? (TypeCompletionBase)new TypeCompletion { Type = actualType }

                                         : new TypeCompletionInStringFormat { FullTypeName = fullTypeName };
            }

            // If the full type name has already been included, then we know for sure that the short type
            // name and the accelerator type names (if there are any) have also been included.
            TypeCompletionMapping entry;
            if (!entryCache.TryGetValue(fullTypeName, out entry))
            {
                entry = new TypeCompletionMapping
                {
                    Key = fullTypeName,
                    Completions = { typeCompletionBase }
                };
                entryCache.Add(fullTypeName, entry);

                // Add a new mapping entry, or put the TypeCompletion instance in the existing mapping entry of the shortTypeName.
                // For example, this may happen to System.ServiceProcess.TimeoutException when System.TimeoutException is already in the cache.
                if (!entryCache.TryGetValue(shortTypeName, out entry))
                {
                    entry = new TypeCompletionMapping { Key = shortTypeName };
                    entryCache.Add(shortTypeName, entry);
                }

                entry.Completions.Add(typeCompletionBase);
            }
        }

        internal static List<CompletionResult> CompleteNamespace(CompletionContext context, string prefix = "", string suffix = "")
        {
            var localTypeCache = s_typeCache ?? InitializeTypeCache();
            var results = new List<CompletionResult>();
            var wordToComplete = context.WordToComplete;
            var dots = wordToComplete.Count(static c => c == '.');
            if (dots >= localTypeCache.Length || localTypeCache[dots] == null)
            {
                return results;
            }

            var pattern = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase);

            foreach (var entry in localTypeCache[dots].Where(e => e.Completions.OfType<NamespaceCompletion>().Any() && pattern.IsMatch(e.Key)))
            {
                foreach (var completion in entry.Completions)
                {
                    results.Add(completion.GetCompletionResult(entry.Key, prefix, suffix));
                }
            }

            results.Sort(static (c1, c2) => string.Compare(c1.ListItemText, c2.ListItemText, StringComparison.OrdinalIgnoreCase));
            return results;
        }

        /// <summary>
        /// Complete a typename.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static IEnumerable<CompletionResult> CompleteType(string typeName)
        {
            // When completing types, we don't care about the runspace, types are visible across the appdomain
            var powershell = (Runspace.DefaultRunspace == null)
                                 ? PowerShell.Create()
                                 : PowerShell.Create(RunspaceMode.CurrentRunspace);

            var helper = new PowerShellExecutionHelper(powershell);
            var executionContext = helper.CurrentPowerShell.Runspace.ExecutionContext;
            return CompleteType(new CompletionContext { WordToComplete = typeName, Helper = helper, ExecutionContext = executionContext });
        }

        internal static List<CompletionResult> CompleteType(CompletionContext context, string prefix = "", string suffix = "")
        {
            var localTypeCache = s_typeCache ?? InitializeTypeCache();

            var results = new List<CompletionResult>();
            var completionTextSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var wordToComplete = context.WordToComplete;
            var dots = wordToComplete.Count(static c => c == '.');
            if (dots >= localTypeCache.Length || localTypeCache[dots] == null)
            {
                return results;
            }

            var pattern = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase);

            foreach (var entry in localTypeCache[dots].Where(e => pattern.IsMatch(e.Key)))
            {
                foreach (var completion in entry.Completions)
                {
                    string namespaceToRemove = GetNamespaceToRemove(context, completion);
                    var completionResult = completion.GetCompletionResult(entry.Key, prefix, suffix, namespaceToRemove);

                    // We might get the same completion result twice. For example, the type cache has:
                    //    DscResource->System.Management.Automation.DscResourceAttribute (from accelerator)
                    //    DscResourceAttribute->System.Management.Automation.DscResourceAttribute (from short type name)
                    // input '[DSCRes' can match both of them, but they actually resolves to the same completion text 'DscResource'.
                    if (!completionTextSet.Contains(completionResult.CompletionText))
                    {
                        results.Add(completionResult);
                        completionTextSet.Add(completionResult.CompletionText);
                    }
                }
            }

            // this is a temporary fix. Only the type defined in the same script get complete. Need to use using Module when that is available.
            if (context.RelatedAsts != null && context.RelatedAsts.Count > 0)
            {
                var scriptBlockAst = (ScriptBlockAst)context.RelatedAsts[0];
                var typeAsts = scriptBlockAst.FindAll(static ast => ast is TypeDefinitionAst, false).Cast<TypeDefinitionAst>();
                foreach (var typeAst in typeAsts.Where(ast => pattern.IsMatch(ast.Name)))
                {
                    string toolTipPrefix = string.Empty;
                    if (typeAst.IsInterface)
                        toolTipPrefix = "Interface ";
                    else if (typeAst.IsClass)
                        toolTipPrefix = "Class ";
                    else if (typeAst.IsEnum)
                        toolTipPrefix = "Enum ";

                    results.Add(new CompletionResult(prefix + typeAst.Name + suffix, typeAst.Name, CompletionResultType.Type, toolTipPrefix + typeAst.Name));
                }
            }

            results.Sort(static (c1, c2) => string.Compare(c1.ListItemText, c2.ListItemText, StringComparison.OrdinalIgnoreCase));
            return results;
        }

        private static string GetNamespaceToRemove(CompletionContext context, TypeCompletionBase completion)
        {
            if (completion is NamespaceCompletion || context.RelatedAsts == null || context.RelatedAsts.Count == 0)
            {
                return null;
            }

            var typeCompletion = completion as TypeCompletion;
            string typeNameSpace = typeCompletion != null
                                       ? typeCompletion.Type.Namespace
                                       : ((TypeCompletionInStringFormat)completion).Namespace;

            var scriptBlockAst = (ScriptBlockAst)context.RelatedAsts[0];
            var matchingNsStates = scriptBlockAst.UsingStatements.Where(s =>
                 s.UsingStatementKind == UsingStatementKind.Namespace
                 && typeNameSpace != null
                 && typeNameSpace.StartsWith(s.Name.Value, StringComparison.OrdinalIgnoreCase));

            string ns = string.Empty;
            foreach (var nsState in matchingNsStates)
            {
                if (nsState.Name.Extent.Text.Length > ns.Length)
                {
                    ns = nsState.Name.Extent.Text;
                }
            }

            return ns;
        }

        #endregion Types

        #region Help Topics

        internal static List<CompletionResult> CompleteHelpTopics(CompletionContext context)
        {
            var results = new List<CompletionResult>();
            string userHelpDir = HelpUtils.GetUserHomeHelpSearchPath();
            string appHelpDir = Utils.GetApplicationBase(Utils.DefaultPowerShellShellID);
            string currentCulture = CultureInfo.CurrentCulture.Name;

            //search for help files for the current culture + en-US as fallback
            var searchPaths = new string[]
            {
                Path.Combine(userHelpDir, currentCulture),
                Path.Combine(appHelpDir, currentCulture),
                Path.Combine(userHelpDir, "en-US"),
                Path.Combine(appHelpDir, "en-US")
            }.Distinct();

            string wordToComplete = context.WordToComplete + "*";
            try
            {
                var wildcardPattern = WildcardPattern.Get(wordToComplete, WildcardOptions.IgnoreCase);

                foreach (var dir in searchPaths)
                {
                    var currentDir = new DirectoryInfo(dir);
                    if (currentDir.Exists)
                    {
                        foreach (var file in currentDir.EnumerateFiles("about_*.help.txt"))
                        {
                            if (wildcardPattern.IsMatch(file.Name))
                            {
                                string topicName = file.Name.Substring(0, file.Name.LastIndexOf(".help.txt"));
                                results.Add(new CompletionResult(topicName));
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return results;
        }

        #endregion Help Topics

        #region Statement Parameters

        internal static List<CompletionResult> CompleteStatementFlags(TokenKind kind, string wordToComplete)
        {
            switch (kind)
            {
                case TokenKind.Switch:

                    Diagnostics.Assert(!string.IsNullOrEmpty(wordToComplete) && wordToComplete[0].IsDash(), "the word to complete should start with '-'");
                    wordToComplete = wordToComplete.Substring(1);
                    bool withColon = wordToComplete.EndsWith(':');
                    wordToComplete = withColon ? wordToComplete.Remove(wordToComplete.Length - 1) : wordToComplete;

                    string enumString = LanguagePrimitives.EnumSingleTypeConverter.EnumValues(typeof(SwitchFlags));
                    string separator = CultureInfo.CurrentUICulture.TextInfo.ListSeparator;
                    string[] enumArray = enumString.Split(separator, StringSplitOptions.RemoveEmptyEntries);

                    var pattern = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase);
                    var enumList = new List<string>();
                    var result = new List<CompletionResult>();
                    CompletionResult fullMatch = null;

                    foreach (string value in enumArray)
                    {
                        if (value.Equals(SwitchFlags.None.ToString(), StringComparison.OrdinalIgnoreCase)) { continue; }

                        if (wordToComplete.Equals(value, StringComparison.OrdinalIgnoreCase))
                        {
                            string completionText = withColon ? "-" + value + ":" : "-" + value;
                            fullMatch = new CompletionResult(completionText, value, CompletionResultType.ParameterName, value);
                            continue;
                        }

                        if (pattern.IsMatch(value))
                        {
                            enumList.Add(value);
                        }
                    }

                    if (fullMatch != null)
                    {
                        result.Add(fullMatch);
                    }

                    enumList.Sort();
                    result.AddRange(from entry in enumList
                                    let completionText = withColon ? "-" + entry + ":" : "-" + entry
                                    select new CompletionResult(completionText, entry, CompletionResultType.ParameterName, entry));

                    return result;

                default:
                    break;
            }

            return null;
        }

        #endregion Statement Parameters

        #region Hashtable Keys

        /// <summary>
        /// Generate auto complete results for hashtable key within a Dynamickeyword.
        /// Results are generated based on properties of a DynamicKeyword matches given identifier.
        /// For example, following "D" matches "DestinationPath"
        ///
        ///     Configuration
        ///     {
        ///         File
        ///         {
        ///             D^
        ///         }
        ///     }
        /// </summary>
        /// <param name="completionContext"></param>
        /// <param name="ast"></param>
        /// <param name="hashtableAst"></param>
        /// <returns></returns>
        internal static List<CompletionResult> CompleteHashtableKeyForDynamicKeyword(
            CompletionContext completionContext,
            DynamicKeywordStatementAst ast,
            HashtableAst hashtableAst)
        {
            Diagnostics.Assert(ast.Keyword != null, "DynamicKeywordStatementAst.Keyword can never be null");
            List<CompletionResult> results = null;
            var dynamicKeywordProperties = ast.Keyword.Properties;
            var memberPattern = completionContext.WordToComplete + "*";

            //
            // Capture all existing properties in hashtable
            //
            var propertiesName = new List<string>();
            int cursorOffset = completionContext.CursorPosition.Offset;
            foreach (var keyValueTuple in hashtableAst.KeyValuePairs)
            {
                var propName = keyValueTuple.Item1 as StringConstantExpressionAst;
                // Exclude the property name at cursor
                if (propName != null && propName.Extent.EndOffset != cursorOffset)
                {
                    propertiesName.Add(propName.Value);
                }
            }

            if (dynamicKeywordProperties.Count > 0)
            {
                // Excludes existing properties in the hashtable statement
                var tempProperties = dynamicKeywordProperties.Where(p => !propertiesName.Contains(p.Key, StringComparer.OrdinalIgnoreCase));
                if (tempProperties != null && tempProperties.Any())
                {
                    results = new List<CompletionResult>();
                    // Filter by name
                    var wildcardPattern = WildcardPattern.Get(memberPattern, WildcardOptions.IgnoreCase);
                    var matchedResults = tempProperties.Where(p => wildcardPattern.IsMatch(p.Key));
                    if (matchedResults == null || !matchedResults.Any())
                    {
                        // Fallback to all non-exist properties in the hashtable statement
                        matchedResults = tempProperties;
                    }

                    foreach (var p in matchedResults)
                    {
                        string psTypeName = LanguagePrimitives.ConvertTypeNameToPSTypeName(p.Value.TypeConstraint);
                        if (psTypeName == "[]" || string.IsNullOrEmpty(psTypeName))
                        {
                            psTypeName = "[" + p.Value.TypeConstraint + "]";
                        }

                        if (string.Equals(psTypeName, "[MSFT_Credential]", StringComparison.OrdinalIgnoreCase))
                        {
                            psTypeName = "[pscredential]";
                        }

                        results.Add(new CompletionResult(
                            p.Key + " = ",
                            p.Key,
                            CompletionResultType.Property,
                            psTypeName));
                    }
                }
            }

            return results;
        }

        private static PSTypeName GetNestedHashtableKeyType(TypeInferenceContext typeContext, PSTypeName parentType, IList<string> nestedKeys)
        {
            var currentType = parentType;
            // The nestedKeys list should have the outer most key as the last element, and the inner most key as the first element
            // If we fail to resolve the type of any key we return null
            for (int i = nestedKeys.Count - 1; i >= 0; i--)
            {
                if (currentType is null)
                {
                    return null;
                }

                var typeMembers = typeContext.GetMembersByInferredType(currentType, false, null);
                currentType = null;
                foreach (var member in typeMembers)
                {
                    if (member is PropertyInfo propertyInfo)
                    {
                        if (propertyInfo.Name.Equals(nestedKeys[i], StringComparison.OrdinalIgnoreCase))
                        {
                            currentType = new PSTypeName(propertyInfo.PropertyType);
                            break;
                        }
                    }
                    else if (member is PropertyMemberAst memberAst && memberAst.Name.Equals(nestedKeys[i], StringComparison.OrdinalIgnoreCase))
                    {
                        if (memberAst.PropertyType is null)
                        {
                            return null;
                        }
                        else
                        {
                            if (memberAst.PropertyType.TypeName is ArrayTypeName arrayType)
                            {
                                currentType = new PSTypeName(arrayType.ElementType);
                            }
                            else
                            {
                                currentType = new PSTypeName(memberAst.PropertyType.TypeName);
                            }
                        }

                        break;
                    }
                }
            }

            return currentType;
        }

        internal static List<CompletionResult> CompleteHashtableKey(CompletionContext completionContext, HashtableAst hashtableAst)
        {
            Ast previousAst = hashtableAst;
            Ast parentAst = hashtableAst.Parent;
            string parameterName = null;
            var nestedHashtableKeys = new List<string>();

            // This loop determines if it's a nested hashtable and what the outermost hashtable is used for (Dynamic keyword, command argument, etc.)
            // Note this also considers hashtables with arrays of hashtables to be nested to support scenarios like this:
            // class Level1
            // {
            //     [Level2[]] $Prop1
            // }
            // class Level2
            // {
            //     [string] $Prop2
            // }
            // [Level1] @{
            //     Prop1 = @(
            //         @{Prop2="Hello"}
            //         @{Pro<Tab>}
            //     )
            // }
            while (parentAst is not null)
            {
                switch (parentAst)
                {
                    case HashtableAst parentTable:
                        foreach (var pair in parentTable.KeyValuePairs)
                        {
                            if (pair.Item2 == previousAst)
                            {
                                // Try to get the value of the hashtable key in the nested hashtable.
                                // If we fail to get the value then return early because we can't generate any useful completions
                                if (SafeExprEvaluator.TrySafeEval(pair.Item1, completionContext.ExecutionContext, out object value))
                                {
                                    if (value is not string stringValue)
                                    {
                                        return null;
                                    }

                                    nestedHashtableKeys.Add(stringValue);
                                    break;
                                }
                                else
                                {
                                    return null;
                                }
                            }
                        }
                        break;

                    case DynamicKeywordStatementAst dynamicKeyword:
                        return CompleteHashtableKeyForDynamicKeyword(completionContext, dynamicKeyword, hashtableAst);

                    case CommandParameterAst cmdParam:
                        parameterName = cmdParam.ParameterName;
                        parentAst = cmdParam.Parent;
                        goto ExitWhileLoop;

                    case AssignmentStatementAst assignment:
                        if (assignment.Left is MemberExpressionAst or ConvertExpressionAst)
                        {
                            parentAst = assignment.Left;
                        }
                        goto ExitWhileLoop;

                    case CommandAst:
                    case ConvertExpressionAst:
                    case UsingStatementAst:
                        goto ExitWhileLoop;

                    case CommandExpressionAst:
                    case PipelineAst:
                    case StatementBlockAst:
                    case ArrayExpressionAst:
                    case ArrayLiteralAst:
                        break;

                    default:
                        return null;
                }

                previousAst = parentAst;
                parentAst = parentAst.Parent;
            }

            ExitWhileLoop:

            bool hashtableIsNested = nestedHashtableKeys.Count > 0;
            int cursorOffset = completionContext.CursorPosition.Offset;
            string wordToComplete = completionContext.WordToComplete;
            var excludedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // Filters out keys that have already been defined in the hashtable, except the one the cursor is at
            foreach (var keyPair in hashtableAst.KeyValuePairs)
            {
                if (!(cursorOffset >= keyPair.Item1.Extent.StartOffset && cursorOffset <= keyPair.Item1.Extent.EndOffset))
                {
                    excludedKeys.Add(keyPair.Item1.Extent.Text);
                }
            }

            if (parentAst is UsingStatementAst usingStatement)
            {
                if (hashtableIsNested || usingStatement.UsingStatementKind != UsingStatementKind.Module)
                {
                    return null;
                }

                var result = new List<CompletionResult>();
                foreach (var key in s_requiresModuleSpecKeys.Keys)
                {
                    if (excludedKeys.Contains(key)
                        || (wordToComplete is not null && !key.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
                        || (key.Equals("RequiredVersion") && (excludedKeys.Contains("ModuleVersion") || excludedKeys.Contains("MaximumVersion")))
                        || ((key.Equals("ModuleVersion") || key.Equals("MaximumVersion")) && excludedKeys.Contains("RequiredVersion")))
                    {
                        continue;
                    }
                    result.Add(new CompletionResult(key, key, CompletionResultType.Property, s_requiresModuleSpecKeys[key]));
                }

                return result;
            }

            if (parentAst is MemberExpressionAst or ConvertExpressionAst)
            {
                IEnumerable<PSTypeName> inferredTypes;
                if (hashtableIsNested)
                {
                    var nestedType = GetNestedHashtableKeyType(
                        completionContext.TypeInferenceContext,
                        AstTypeInference.InferTypeOf(parentAst, completionContext.TypeInferenceContext, TypeInferenceRuntimePermissions.AllowSafeEval)[0],
                        nestedHashtableKeys);
                    if (nestedType is null)
                    {
                        return null;
                    }

                    inferredTypes = TypeInferenceVisitor.GetInferredEnumeratedTypes(new PSTypeName[] { nestedType });
                }
                else
                {
                    inferredTypes = TypeInferenceVisitor.GetInferredEnumeratedTypes(
                        AstTypeInference.InferTypeOf(parentAst, completionContext.TypeInferenceContext, TypeInferenceRuntimePermissions.AllowSafeEval));
                }

                var result = new List<CompletionResult>();
                CompleteMemberByInferredType(
                    completionContext.TypeInferenceContext,
                    inferredTypes,
                    result,
                    wordToComplete + "*",
                    IsWriteablePropertyMember,
                    isStatic: false,
                    excludedKeys,
                    ignoreTypesWithoutDefaultConstructor: true);
                return result;
            }

            if (parentAst is CommandAst commandAst)
            {
                var binding = new PseudoParameterBinder().DoPseudoParameterBinding(commandAst, null, null, bindingType: PseudoParameterBinder.BindingType.ArgumentCompletion);
                if (binding is null)
                {
                    return null;
                }

                if (parameterName is null)
                {
                    foreach (var boundArg in binding.BoundArguments)
                    {
                        if (boundArg.Value is AstPair pair && pair.Argument == previousAst)
                        {
                            parameterName = boundArg.Key;
                        }
                        else if (boundArg.Value is AstArrayPair arrayPair && arrayPair.Argument.Contains(previousAst))
                        {
                            parameterName = boundArg.Key;
                        }
                    }
                }

                if (parameterName is not null)
                {
                    List<CompletionResult> results;
                    if (parameterName.Equals("GroupBy", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!hashtableIsNested)
                        {
                            switch (binding.CommandName)
                            {
                                case "Format-Table":
                                case "Format-List":
                                case "Format-Wide":
                                case "Format-Custom":
                                    return GetSpecialHashTableKeyMembers(excludedKeys, wordToComplete, "Expression", "FormatString", "Label");
                            }
                        }

                        return null;
                    }

                    if (parameterName.Equals("Property", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!hashtableIsNested)
                        {
                            switch (binding.CommandName)
                            {
                                case "New-Object":
                                    var inferredType = AstTypeInference.InferTypeOf(commandAst, completionContext.TypeInferenceContext, TypeInferenceRuntimePermissions.AllowSafeEval);
                                    results = new List<CompletionResult>();
                                    CompleteMemberByInferredType(
                                        completionContext.TypeInferenceContext, inferredType,
                                        results, completionContext.WordToComplete + "*", IsWriteablePropertyMember, isStatic: false, excludedKeys);
                                    return results;
                                case "Select-Object":
                                    return GetSpecialHashTableKeyMembers(excludedKeys, wordToComplete, "Name", "Expression");
                                case "Sort-Object":
                                    return GetSpecialHashTableKeyMembers(excludedKeys, wordToComplete, "Expression", "Ascending", "Descending");
                                case "Group-Object":
                                    return GetSpecialHashTableKeyMembers(excludedKeys, wordToComplete, "Expression");
                                case "Format-Table":
                                    return GetSpecialHashTableKeyMembers(excludedKeys, wordToComplete, "Expression", "FormatString", "Label", "Width", "Alignment");
                                case "Format-List":
                                    return GetSpecialHashTableKeyMembers(excludedKeys, wordToComplete, "Expression", "FormatString", "Label");
                                case "Format-Wide":
                                    return GetSpecialHashTableKeyMembers(excludedKeys, wordToComplete, "Expression", "FormatString");
                                case "Format-Custom":
                                    return GetSpecialHashTableKeyMembers(excludedKeys, wordToComplete, "Expression", "Depth");
                                case "Set-CimInstance":
                                case "New-CimInstance":
                                    results = new List<CompletionResult>();
                                    NativeCompletionCimCommands(parameterName, binding.BoundArguments, results, commandAst, completionContext, excludedKeys, binding.CommandName);
                                    // this method adds a null CompletionResult to the list but we don't want that here.
                                    if (results.Count > 1)
                                    {
                                        results.RemoveAt(results.Count - 1);
                                        return results;
                                    }
                                    return null;
                            }
                            return null;
                        }
                    }

                    if (parameterName.Equals("FilterHashtable", StringComparison.OrdinalIgnoreCase))
                    {
                        switch (binding.CommandName)
                        {
                            case "Get-WinEvent":
                                if (nestedHashtableKeys.Count == 1
                                    && nestedHashtableKeys[0].Equals("SuppressHashFilter", StringComparison.OrdinalIgnoreCase)
                                    && hashtableAst.Parent.Parent.Parent is HashtableAst)
                                {
                                    return GetSpecialHashTableKeyMembers(excludedKeys, wordToComplete, "LogName", "ProviderName", "Path", "Keywords", "ID", "Level",
                                    "StartTime", "EndTime", "UserID", "Data");
                                }
                                else if (!hashtableIsNested)
                                {
                                    return GetSpecialHashTableKeyMembers(excludedKeys, wordToComplete, "LogName", "ProviderName", "Path", "Keywords", "ID", "Level",
                                    "StartTime", "EndTime", "UserID", "Data", "SuppressHashFilter");
                                }

                                return null;
                        }
                    }

                    if (parameterName.Equals("Arguments", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!hashtableIsNested)
                        {
                            switch (binding.CommandName)
                            {
                                case "Invoke-CimMethod":
                                    results = new List<CompletionResult>();
                                    NativeCompletionCimCommands(parameterName, binding.BoundArguments, results, commandAst, completionContext, excludedKeys, binding.CommandName);
                                    // this method adds a null CompletionResult to the list but we don't want that here.
                                    if (results.Count > 1)
                                    {
                                        results.RemoveAt(results.Count - 1);
                                        return results;
                                    }
                                    return null;
                            }
                        }
                        return null;
                    }

                    IEnumerable<PSTypeName> inferredTypes;
                    if (hashtableIsNested)
                    {
                        var nestedType = GetNestedHashtableKeyType(
                            completionContext.TypeInferenceContext,
                            new PSTypeName(binding.BoundParameters[parameterName].Parameter.Type),
                            nestedHashtableKeys);
                        if (nestedType is null)
                        {
                            return null;
                        }
                        inferredTypes = TypeInferenceVisitor.GetInferredEnumeratedTypes(new PSTypeName[] { nestedType });
                    }
                    else
                    {
                        inferredTypes = TypeInferenceVisitor.GetInferredEnumeratedTypes(new PSTypeName[] { new PSTypeName(binding.BoundParameters[parameterName].Parameter.Type) });
                    }

                    results = new List<CompletionResult>();
                    CompleteMemberByInferredType(
                        completionContext.TypeInferenceContext,
                        inferredTypes,
                        results,
                        $"{wordToComplete}*",
                        IsWriteablePropertyMember,
                        isStatic: false,
                        excludedKeys,
                        ignoreTypesWithoutDefaultConstructor: true);
                    return results;
                }
            }
            else if (!hashtableIsNested && parentAst is AssignmentStatementAst assignment && assignment.Left is VariableExpressionAst assignmentVar)
            {
                var firstSplatUse = completionContext.RelatedAsts[0].Find(
                    currentAst =>
                        currentAst.Extent.StartOffset > hashtableAst.Extent.EndOffset
                        && currentAst is VariableExpressionAst splatVar
                        && splatVar.Splatted
                        && splatVar.VariablePath.UserPath.Equals(assignmentVar.VariablePath.UserPath, StringComparison.OrdinalIgnoreCase),
                    searchNestedScriptBlocks: true) as VariableExpressionAst;

                if (firstSplatUse is not null && firstSplatUse.Parent is CommandAst command)
                {
                    var binding = new PseudoParameterBinder()
                        .DoPseudoParameterBinding(
                            command,
                            pipeArgumentType: null,
                            paramAstAtCursor: null,
                            PseudoParameterBinder.BindingType.ParameterCompletion);

                    if (binding is null)
                    {
                        return null;
                    }

                    var results = new List<CompletionResult>();
                    foreach (var parameter in binding.UnboundParameters)
                    {
                        if (!excludedKeys.Contains(parameter.Parameter.Name)
                            && (wordToComplete is null || parameter.Parameter.Name.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase)))
                        {
                            results.Add(new CompletionResult(parameter.Parameter.Name, parameter.Parameter.Name, CompletionResultType.ParameterName, $"[{parameter.Parameter.Type.Name}]"));
                        }
                    }

                    if (results.Count > 0)
                    {
                        return results;
                    }
                }
            }

            return null;
        }

        private static List<CompletionResult> GetSpecialHashTableKeyMembers(HashSet<string> excludedKeys, string wordToComplete, params string[] keys)
        {
            var result = new List<CompletionResult>();
            foreach (string key in keys)
            {
                if ((string.IsNullOrEmpty(wordToComplete) || key.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase)) && !excludedKeys.Contains(key))
                {
                    result.Add(new CompletionResult(key, key, CompletionResultType.Property, key));
                }
            }

            if (result.Count == 0)
            {
                return null;
            }

            return result;
        }

        #endregion Hashtable Keys

        #region Helpers

        internal static bool IsPathSafelyExpandable(ExpandableStringExpressionAst expandableStringAst, string extraText, ExecutionContext executionContext, out string expandedString)
        {
            expandedString = null;
            // Expand the string if its type is DoubleQuoted or BareWord
            var constType = expandableStringAst.StringConstantType;
            if (constType == StringConstantType.DoubleQuotedHereString) { return false; }

            Diagnostics.Assert(
                constType == StringConstantType.BareWord ||
                (constType == StringConstantType.DoubleQuoted && expandableStringAst.Extent.Text[0].IsDoubleQuote()),
                "the string to be expanded should be either BareWord or DoubleQuoted");

            var varValues = new List<string>();
            foreach (ExpressionAst nestedAst in expandableStringAst.NestedExpressions)
            {
                if (!(nestedAst is VariableExpressionAst variableAst)) { return false; }

                string strValue = CombineVariableWithPartialPath(variableAst, null, executionContext);
                if (strValue != null)
                {
                    varValues.Add(strValue);
                }
                else
                {
                    return false;
                }
            }

            var formattedString = string.Format(CultureInfo.InvariantCulture, expandableStringAst.FormatExpression, varValues.ToArray());
            string quote = (constType == StringConstantType.DoubleQuoted) ? "\"" : string.Empty;

            expandedString = quote + formattedString + extraText + quote;
            return true;
        }

        internal static string CombineVariableWithPartialPath(VariableExpressionAst variableAst, string extraText, ExecutionContext executionContext)
        {
            var varPath = variableAst.VariablePath;
            if (!varPath.IsVariable && !varPath.DriveName.Equals("env", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (varPath.UnqualifiedPath.Equals(SpecialVariables.PSScriptRoot, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(variableAst.Extent.File))
            {
                return Path.GetDirectoryName(variableAst.Extent.File) + extraText;
            }

            try
            {
                // We check the strict mode inside GetVariableValue
                object value = VariableOps.GetVariableValue(varPath, executionContext, variableAst);
                var strValue = (value == null) ? string.Empty : value as string;

                if (strValue == null)
                {
                    object baseObj = PSObject.Base(value);
                    if (baseObj is string || baseObj?.GetType()?.IsPrimitive is true)
                    {
                        strValue = LanguagePrimitives.ConvertTo<string>(value);
                    }
                }

                if (strValue != null)
                {
                    return strValue + extraText;
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        internal static string HandleDoubleAndSingleQuote(ref string wordToComplete)
        {
            string quote = string.Empty;

            if (!string.IsNullOrEmpty(wordToComplete) && (wordToComplete[0].IsSingleQuote() || wordToComplete[0].IsDoubleQuote()))
            {
                char frontQuote = wordToComplete[0];
                int length = wordToComplete.Length;

                if (length == 1)
                {
                    wordToComplete = string.Empty;
                    quote = frontQuote.IsSingleQuote() ? "'" : "\"";
                }
                else if (length > 1)
                {
                    if ((wordToComplete[length - 1].IsDoubleQuote() && frontQuote.IsDoubleQuote()) || (wordToComplete[length - 1].IsSingleQuote() && frontQuote.IsSingleQuote()))
                    {
                        wordToComplete = wordToComplete.Substring(1, length - 2);
                        quote = frontQuote.IsSingleQuote() ? "'" : "\"";
                    }
                    else if (!wordToComplete[length - 1].IsDoubleQuote() && !wordToComplete[length - 1].IsSingleQuote())
                    {
                        wordToComplete = wordToComplete.Substring(1);
                        quote = frontQuote.IsSingleQuote() ? "'" : "\"";
                    }
                }
            }

            return quote;
        }

        internal static bool IsSplattedVariable(Ast targetExpr)
        {
            if (targetExpr is VariableExpressionAst && ((VariableExpressionAst)targetExpr).Splatted)
            {
                // It's splatted variable, member expansion is not useful
                return true;
            }

            return false;
        }

        internal static void CompleteMemberHelper(
            bool @static,
            string memberName,
            ExpressionAst targetExpr,
            CompletionContext context,
            List<CompletionResult> results)
        {
            object value;
            if (SafeExprEvaluator.TrySafeEval(targetExpr, context.ExecutionContext, out value) && value != null)
            {
                if (targetExpr is ArrayExpressionAst && value is not object[])
                {
                    // When the array contains only one element, the evaluation result would be that element. We wrap it into an array
                    value = new[] { value };
                }

                // Instead of Get-Member, we access the members directly and send as input to the pipe.
                var powerShellExecutionHelper = context.Helper;
                powerShellExecutionHelper
                    .AddCommandWithPreferenceSetting("Microsoft.PowerShell.Core\\Where-Object")
                    .AddParameter("Property", "Name")
                    .AddParameter("Like")
                    .AddParameter("Value", memberName)
                    .AddCommandWithPreferenceSetting("Microsoft.PowerShell.Utility\\Sort-Object")
                    .AddParameter("Property", new object[] { "MemberType", "Name" });

                IEnumerable members;
                if (@static)
                {
                    if (!(PSObject.Base(value) is Type type))
                    {
                        return;
                    }

                    members = PSObject.DotNetStaticAdapter.BaseGetMembers<PSMemberInfo>(type);
                }
                else
                {
                    members = PSObject.AsPSObject(value).Members;
                }

                var sortedMembers = powerShellExecutionHelper.ExecuteCurrentPowerShell(out _, members);

                foreach (var member in sortedMembers)
                {
                    var memberInfo = (PSMemberInfo)PSObject.Base(member);
                    if (memberInfo.IsHidden)
                    {
                        continue;
                    }

                    var completionText = memberInfo.Name;

                    // Handle scenarios like this: $aa | add-member 'a b' 23; $aa.a<tab>
                    if (completionText.IndexOfAny(s_charactersRequiringQuotes) != -1)
                    {
                        completionText = completionText.Replace("'", "''");
                        completionText = "'" + completionText + "'";
                    }

                    var isMethod = memberInfo is PSMethodInfo;
                    if (isMethod)
                    {
                        var isSpecial = (memberInfo is PSMethod) && ((PSMethod)memberInfo).IsSpecial;
                        if (isSpecial)
                            continue;
                        completionText += '(';
                    }

                    string tooltip = memberInfo.ToString();
                    if (tooltip.Contains("),", StringComparison.Ordinal))
                    {
                        var overloads = tooltip.Split("),", StringSplitOptions.RemoveEmptyEntries);
                        var newTooltip = new StringBuilder();
                        foreach (var overload in overloads)
                        {
                            newTooltip.Append(overload.Trim() + ")\r\n");
                        }

                        newTooltip.Remove(newTooltip.Length - 3, 3);
                        tooltip = newTooltip.ToString();
                    }

                    results.Add(
                        new CompletionResult(completionText, memberInfo.Name,
                                             isMethod ? CompletionResultType.Method : CompletionResultType.Property,
                                             tooltip));
                }

                var dictionary = PSObject.Base(value) as IDictionary;
                if (dictionary != null)
                {
                    var pattern = WildcardPattern.Get(memberName, WildcardOptions.IgnoreCase);
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        if (!(entry.Key is string key))
                            continue;

                        if (pattern.IsMatch(key))
                        {
                            // Handle scenarios like this: $hashtable["abc#d"] = 100; $hashtable.ab<tab>
                            if (key.IndexOfAny(s_charactersRequiringQuotes) != -1)
                            {
                                key = key.Replace("'", "''");
                                key = "'" + key + "'";
                            }

                            results.Add(new CompletionResult(key, key, CompletionResultType.Property, key));
                        }
                    }
                }

                if (!@static && IsValueEnumerable(PSObject.Base(value)))
                {
                    // Complete extension methods 'Where' and 'ForEach' for Enumerable values
                    CompleteExtensionMethods(memberName, results);
                }
            }
        }

        /// <summary>
        /// Check if a value is treated as Enumerable in powershell.
        /// </summary>
        private static bool IsValueEnumerable(object value)
        {
            object baseValue = PSObject.Base(value);

            if (baseValue == null || baseValue is string || baseValue is PSObject ||
                baseValue is IDictionary || baseValue is System.Xml.XmlNode)
            {
                return false;
            }

            if (baseValue is IEnumerable || baseValue is IEnumerator || baseValue is DataTable)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if a strong type is treated as Enumerable in powershell.
        /// </summary>
        private static bool IsStaticTypeEnumerable(Type type)
        {
            if (type.Equals(typeof(string)) || typeof(IDictionary).IsAssignableFrom(type) || typeof(System.Xml.XmlNode).IsAssignableFrom(type))
            {
                return false;
            }

            if (typeof(IEnumerable).IsAssignableFrom(type) || typeof(IEnumerator).IsAssignableFrom(type))
            {
                return true;
            }

            return false;
        }

        private static bool CompletionRequiresQuotes(string completion, bool escape)
        {
            // If the tokenizer sees the completion as more than two tokens, or if there is some error, then
            // some form of quoting is necessary (if it's a variable, we'd need ${}, filenames would need [], etc.)

            Language.Token[] tokens;
            ParseError[] errors;
            Language.Parser.ParseInput(completion, out tokens, out errors);

            char[] charToCheck = escape ? new[] { '$', '[', ']', '`' } : new[] { '$', '`' };

            // Expect no errors and 2 tokens (1 is for our completion, the other is eof)
            // Or if the completion is a keyword, we ignore the errors
            bool requireQuote = !(errors.Length == 0 && tokens.Length == 2);
            if ((!requireQuote && tokens[0] is StringToken) ||
                (tokens.Length == 2 && (tokens[0].TokenFlags & TokenFlags.Keyword) != 0))
            {
                requireQuote = false;
                var value = tokens[0].Text;
                if (value.IndexOfAny(charToCheck) != -1)
                    requireQuote = true;
            }

            return requireQuote;
        }

        private static bool ProviderSpecified(string path)
        {
            var index = path.IndexOf(':');
            return index != -1 && index + 1 < path.Length && path[index + 1] == ':';
        }

        private static Type GetEffectiveParameterType(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type);
            return underlying ?? type;
        }

        /// <summary>
        /// Turn on the "LiteralPaths" option.
        /// </summary>
        /// <param name="completionContext"></param>
        /// <returns>
        /// Indicate whether the "LiteralPaths" option needs to be removed after operation
        /// </returns>
        private static bool TurnOnLiteralPathOption(CompletionContext completionContext)
        {
            bool clearLiteralPathsKey = false;

            if (completionContext.Options == null)
            {
                completionContext.Options = new Hashtable { { "LiteralPaths", true } };
                clearLiteralPathsKey = true;
            }
            else if (!completionContext.Options.ContainsKey("LiteralPaths"))
            {
                // Dont escape '[',']','`' when the file name is treated as command name
                completionContext.Options.Add("LiteralPaths", true);
                clearLiteralPathsKey = true;
            }

            return clearLiteralPathsKey;
        }

        /// <summary>
        /// Return whether we need to add ampersand when it's necessary.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="defaultChoice"></param>
        /// <returns></returns>
        internal static bool IsAmpersandNeeded(CompletionContext context, bool defaultChoice)
        {
            if (context.RelatedAsts != null && !string.IsNullOrEmpty(context.WordToComplete))
            {
                var lastAst = context.RelatedAsts.Last();
                var parent = lastAst.Parent as CommandAst;

                if (parent != null && parent.CommandElements.Count == 1 &&
                    ((!defaultChoice && parent.InvocationOperator == TokenKind.Unknown) ||
                     (defaultChoice && parent.InvocationOperator != TokenKind.Unknown)))
                {
                    // - When the default choice is NOT to add ampersand, we only return true
                    //   when the invocation operator is NOT specified.
                    // - When the default choice is to add ampersand, we only return false
                    //   when the invocation operator is specified.
                    defaultChoice = !defaultChoice;
                }
            }

            return defaultChoice;
        }

        private sealed class ItemPathComparer : IComparer<PSObject>
        {
            public int Compare(PSObject x, PSObject y)
            {
                var xPathInfo = PSObject.Base(x) as PathInfo;
                var xFileInfo = PSObject.Base(x) as IO.FileSystemInfo;
                var xPathStr = PSObject.Base(x) as string;

                var yPathInfo = PSObject.Base(y) as PathInfo;
                var yFileInfo = PSObject.Base(y) as IO.FileSystemInfo;
                var yPathStr = PSObject.Base(y) as string;

                string xPath = null, yPath = null;

                if (xPathInfo != null)
                    xPath = xPathInfo.ProviderPath;
                else if (xFileInfo != null)
                    xPath = xFileInfo.FullName;
                else if (xPathStr != null)
                    xPath = xPathStr;

                if (yPathInfo != null)
                    yPath = yPathInfo.ProviderPath;
                else if (yFileInfo != null)
                    yPath = yFileInfo.FullName;
                else if (yPathStr != null)
                    yPath = yPathStr;

                if (string.IsNullOrEmpty(xPath) || string.IsNullOrEmpty(yPath))
                    Diagnostics.Assert(false, "Base object of item PSObject should be either PathInfo or FileSystemInfo");

                return string.Compare(xPath, yPath, StringComparison.CurrentCultureIgnoreCase);
            }
        }

        private sealed class CommandNameComparer : IComparer<PSObject>
        {
            public int Compare(PSObject x, PSObject y)
            {
                string xName = null;
                string yName = null;

                object xObj = PSObject.Base(x);
                object yObj = PSObject.Base(y);

                var xCommandInfo = xObj as CommandInfo;
                xName = xCommandInfo != null ? xCommandInfo.Name : xObj as string;

                var yCommandInfo = yObj as CommandInfo;
                yName = yCommandInfo != null ? yCommandInfo.Name : yObj as string;

                if (xName == null || yName == null)
                    Diagnostics.Assert(false, "Base object of Command PSObject should be either CommandInfo or string");

                return string.Compare(xName, yName, StringComparison.OrdinalIgnoreCase);
            }
        }

        #endregion Helpers
    }

    /// <summary>
    /// This class is very similar to the restricted language checker, but it is meant to allow more things, yet still
    /// be considered "safe", at least in the sense that tab completion can rely on it to not do bad things.  The primary
    /// use is for intellisense where you don't want to run arbitrary code, but you do want to know the values
    /// of various expressions so you can get the members.
    /// </summary>
    internal class SafeExprEvaluator : ICustomAstVisitor2
    {
        internal static bool TrySafeEval(ExpressionAst ast, ExecutionContext executionContext, out object value)
        {
            if (!(bool)ast.Accept(new SafeExprEvaluator()))
            {
                value = null;
                return false;
            }

            try
            {
                // ConstrainedLanguage has already been applied as necessary when we construct CompletionContext
                Diagnostics.Assert(!(executionContext.HasRunspaceEverUsedConstrainedLanguageMode && executionContext.LanguageMode != PSLanguageMode.ConstrainedLanguage),
                                   "If the runspace has ever used constrained language mode, then the current language mode should already be set to constrained language");

                // We're passing 'true' here for isTrustedInput, because SafeExprEvaluator ensures that the AST
                // has no dangerous side-effects such as arbitrary expression evaluation. It does require variable
                // access and a few other minor things, which staples of tab completion:
                //
                // $t = Get-Process
                // $t[0].MainModule.<TAB>
                //
                value = Compiler.GetExpressionValue(ast, true, executionContext);
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        public object VisitErrorStatement(ErrorStatementAst errorStatementAst) { return false; }

        public object VisitErrorExpression(ErrorExpressionAst errorExpressionAst) { return false; }

        public object VisitScriptBlock(ScriptBlockAst scriptBlockAst) { return false; }

        public object VisitParamBlock(ParamBlockAst paramBlockAst) { return false; }

        public object VisitNamedBlock(NamedBlockAst namedBlockAst) { return false; }

        public object VisitTypeConstraint(TypeConstraintAst typeConstraintAst) { return false; }

        public object VisitAttribute(AttributeAst attributeAst) { return false; }

        public object VisitNamedAttributeArgument(NamedAttributeArgumentAst namedAttributeArgumentAst) { return false; }

        public object VisitParameter(ParameterAst parameterAst) { return false; }

        public object VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst) { return false; }

        public object VisitIfStatement(IfStatementAst ifStmtAst) { return false; }

        public object VisitTrap(TrapStatementAst trapStatementAst) { return false; }

        public object VisitSwitchStatement(SwitchStatementAst switchStatementAst) { return false; }

        public object VisitDataStatement(DataStatementAst dataStatementAst) { return false; }

        public object VisitForEachStatement(ForEachStatementAst forEachStatementAst) { return false; }

        public object VisitDoWhileStatement(DoWhileStatementAst doWhileStatementAst) { return false; }

        public object VisitForStatement(ForStatementAst forStatementAst) { return false; }

        public object VisitWhileStatement(WhileStatementAst whileStatementAst) { return false; }

        public object VisitCatchClause(CatchClauseAst catchClauseAst) { return false; }

        public object VisitTryStatement(TryStatementAst tryStatementAst) { return false; }

        public object VisitBreakStatement(BreakStatementAst breakStatementAst) { return false; }

        public object VisitContinueStatement(ContinueStatementAst continueStatementAst) { return false; }

        public object VisitReturnStatement(ReturnStatementAst returnStatementAst) { return false; }

        public object VisitExitStatement(ExitStatementAst exitStatementAst) { return false; }

        public object VisitThrowStatement(ThrowStatementAst throwStatementAst) { return false; }

        public object VisitDoUntilStatement(DoUntilStatementAst doUntilStatementAst) { return false; }

        public object VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst) { return false; }

        // REVIEW: we could relax this to allow specific commands
        public object VisitCommand(CommandAst commandAst) { return false; }

        public object VisitCommandExpression(CommandExpressionAst commandExpressionAst) { return false; }

        public object VisitCommandParameter(CommandParameterAst commandParameterAst) { return false; }

        public object VisitFileRedirection(FileRedirectionAst fileRedirectionAst) { return false; }

        public object VisitMergingRedirection(MergingRedirectionAst mergingRedirectionAst) { return false; }

        public object VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst) { return false; }

        public object VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst) { return false; }

        public object VisitBlockStatement(BlockStatementAst blockStatementAst) { return false; }

        public object VisitInvokeMemberExpression(InvokeMemberExpressionAst invokeMemberExpressionAst) { return false; }

        public object VisitUsingExpression(UsingExpressionAst usingExpressionAst) { return false; }

        public object VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst) { return false; }

        public object VisitPropertyMember(PropertyMemberAst propertyMemberAst) { return false; }

        public object VisitFunctionMember(FunctionMemberAst functionMemberAst) { return false; }

        public object VisitBaseCtorInvokeMemberExpression(BaseCtorInvokeMemberExpressionAst baseCtorInvokeMemberExpressionAst) { return false; }

        public object VisitUsingStatement(UsingStatementAst usingStatementAst) { return false; }

        public object VisitDynamicKeywordStatement(DynamicKeywordStatementAst dynamicKeywordStatementAst) { return false; }

        public object VisitPipelineChain(PipelineChainAst pipelineChainAst) { return false; }

        public object VisitConfigurationDefinition(ConfigurationDefinitionAst configurationDefinitionAst)
        {
            return configurationDefinitionAst.Body.Accept(this);
        }

        public object VisitStatementBlock(StatementBlockAst statementBlockAst)
        {
            if (statementBlockAst.Traps != null) return false;
            // REVIEW: we could relax this to allow multiple statements
            if (statementBlockAst.Statements.Count > 1) return false;
            var pipeline = statementBlockAst.Statements.FirstOrDefault();
            return pipeline != null && (bool)pipeline.Accept(this);
        }

        public object VisitPipeline(PipelineAst pipelineAst)
        {
            var expr = pipelineAst.GetPureExpression();
            return expr != null && (bool)expr.Accept(this);
        }

        public object VisitTernaryExpression(TernaryExpressionAst ternaryExpressionAst)
        {
            return (bool)ternaryExpressionAst.Condition.Accept(this) &&
                   (bool)ternaryExpressionAst.IfTrue.Accept(this) &&
                   (bool)ternaryExpressionAst.IfFalse.Accept(this);
        }

        public object VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst)
        {
            return (bool)binaryExpressionAst.Left.Accept(this) && (bool)binaryExpressionAst.Right.Accept(this);
        }

        public object VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst)
        {
            return (bool)unaryExpressionAst.Child.Accept(this);
        }

        public object VisitConvertExpression(ConvertExpressionAst convertExpressionAst)
        {
            return (bool)convertExpressionAst.Child.Accept(this);
        }

        public object VisitConstantExpression(ConstantExpressionAst constantExpressionAst)
        {
            return true;
        }

        public object VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst)
        {
            return true;
        }

        public object VisitSubExpression(SubExpressionAst subExpressionAst)
        {
            return subExpressionAst.SubExpression.Accept(this);
        }

        public object VisitVariableExpression(VariableExpressionAst variableExpressionAst)
        {
            return true;
        }

        public object VisitTypeExpression(TypeExpressionAst typeExpressionAst)
        {
            return true;
        }

        public object VisitMemberExpression(MemberExpressionAst memberExpressionAst)
        {
            return (bool)memberExpressionAst.Expression.Accept(this) && (bool)memberExpressionAst.Member.Accept(this);
        }

        public object VisitIndexExpression(IndexExpressionAst indexExpressionAst)
        {
            return (bool)indexExpressionAst.Target.Accept(this) && (bool)indexExpressionAst.Index.Accept(this);
        }

        public object VisitArrayExpression(ArrayExpressionAst arrayExpressionAst)
        {
            return arrayExpressionAst.SubExpression.Accept(this);
        }

        public object VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst)
        {
            return arrayLiteralAst.Elements.All(e => (bool)e.Accept(this));
        }

        public object VisitHashtable(HashtableAst hashtableAst)
        {
            foreach (var keyValuePair in hashtableAst.KeyValuePairs)
            {
                if (!(bool)keyValuePair.Item1.Accept(this))
                    return false;
                if (!(bool)keyValuePair.Item2.Accept(this))
                    return false;
            }

            return true;
        }

        public object VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst)
        {
            return true;
        }

        public object VisitParenExpression(ParenExpressionAst parenExpressionAst)
        {
            return parenExpressionAst.Pipeline.Accept(this);
        }
    }

    /// <summary>
    /// Completes with the property names of the InputObject.
    /// </summary>
    internal class PropertyNameCompleter : IArgumentCompleter
    {
        private readonly string _parameterNameOfInput;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyNameCompleter"/> class.
        /// </summary>
        public PropertyNameCompleter()
        {
            _parameterNameOfInput = "InputObject";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyNameCompleter"/> class.
        /// </summary>
        /// <param name="parameterNameOfInput">The name of the property of the input object for which to complete with property names.</param>
        public PropertyNameCompleter(string parameterNameOfInput)
        {
            _parameterNameOfInput = parameterNameOfInput;
        }

        IEnumerable<CompletionResult> IArgumentCompleter.CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            if (commandAst.Parent is not PipelineAst pipelineAst)
            {
                return null;
            }

            int i;
            for (i = 0; i < pipelineAst.PipelineElements.Count; i++)
            {
                if (pipelineAst.PipelineElements[i] == commandAst)
                {
                    break;
                }
            }

            var typeInferenceContext = new TypeInferenceContext();
            IEnumerable<PSTypeName> prevType;
            if (i == 0)
            {
                var parameterAst = (CommandParameterAst)commandAst.Find(ast => ast is CommandParameterAst cpa && cpa.ParameterName == "PropertyName", false);
                var pseudoBinding = new PseudoParameterBinder().DoPseudoParameterBinding(commandAst, null, parameterAst, PseudoParameterBinder.BindingType.ParameterCompletion);
                if (!pseudoBinding.BoundArguments.TryGetValue(_parameterNameOfInput, out var pair) || !pair.ArgumentSpecified)
                {
                    return null;
                }

                if (pair is AstPair astPair && astPair.Argument != null)
                {
                    prevType = AstTypeInference.InferTypeOf(astPair.Argument, typeInferenceContext, TypeInferenceRuntimePermissions.AllowSafeEval);
                }

                return null;
            }
            else
            {
                prevType = AstTypeInference.InferTypeOf(pipelineAst.PipelineElements[i - 1], typeInferenceContext, TypeInferenceRuntimePermissions.AllowSafeEval);
            }

            var result = new List<CompletionResult>();

            CompletionCompleters.CompleteMemberByInferredType(typeInferenceContext, prevType, result, wordToComplete + "*", filter: CompletionCompleters.IsPropertyMember, isStatic: false);
            return result;
        }
    }
}
