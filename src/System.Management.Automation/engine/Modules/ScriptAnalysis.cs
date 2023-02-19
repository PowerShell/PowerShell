// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management.Automation.Language;
using System.Text;
using System.Text.RegularExpressions;

namespace System.Management.Automation
{
    /// <summary>
    /// Class describing a PowerShell module...
    /// </summary>
    [Serializable]
    internal class ScriptAnalysis
    {
        internal static ScriptAnalysis Analyze(string path, ExecutionContext context)
        {
            ModuleIntrinsics.Tracer.WriteLine("Analyzing path: {0}", path);

            try
            {
                if (Utils.PathIsUnc(path) && (context.CurrentCommandProcessor.CommandRuntime != null))
                {
                    ProgressRecord analysisProgress = new ProgressRecord(0,
                        Modules.ScriptAnalysisPreparing,
                        string.Format(CultureInfo.InvariantCulture, Modules.ScriptAnalysisModule, path));
                    analysisProgress.RecordType = ProgressRecordType.Processing;

                    // Write the progress using a static source ID so that all
                    // analysis messages get single-threaded in the progress pane (rather than nesting).
                    context.CurrentCommandProcessor.CommandRuntime.WriteProgress(typeof(ScriptAnalysis).FullName.GetHashCode(), analysisProgress);
                }
            }
            catch (InvalidOperationException)
            {
                // This may be called when we are not allowed to write progress,
                // So eat the invalid operation
            }

            string scriptContent = ReadScript(path);

            ParseError[] errors;
            var moduleAst = (new Parser()).Parse(path, scriptContent, null, out errors, ParseMode.ModuleAnalysis);

            // Don't bother analyzing if there are syntax errors (we don't do semantic analysis which would
            // detect other errors that we also might choose to ignore, but it's slower.)
            if (errors.Length > 0)
                return null;

            ExportVisitor exportVisitor = new ExportVisitor(forCompletion: false);
            moduleAst.Visit(exportVisitor);

            var result = new ScriptAnalysis
            {
                DiscoveredClasses = exportVisitor.DiscoveredClasses,
                DiscoveredExports = exportVisitor.DiscoveredExports,
                DiscoveredAliases = new Dictionary<string, string>(),
                DiscoveredModules = exportVisitor.DiscoveredModules,
                DiscoveredCommandFilters = exportVisitor.DiscoveredCommandFilters,
                AddsSelfToPath = exportVisitor.AddsSelfToPath
            };

            if (result.DiscoveredCommandFilters.Count == 0)
            {
                result.DiscoveredCommandFilters.Add("*");
            }
            else
            {
                // Post-process aliases, as they are not exported by default
                List<WildcardPattern> patterns = new List<WildcardPattern>();
                foreach (string discoveredCommandFilter in result.DiscoveredCommandFilters)
                {
                    patterns.Add(WildcardPattern.Get(discoveredCommandFilter, WildcardOptions.IgnoreCase));
                }

                foreach (var pair in exportVisitor.DiscoveredAliases)
                {
                    string discoveredAlias = pair.Key;
                    if (SessionStateUtilities.MatchesAnyWildcardPattern(discoveredAlias, patterns, defaultValue: false))
                    {
                        result.DiscoveredAliases[discoveredAlias] = pair.Value;
                    }
                }
            }

            return result;
        }

        internal static string ReadScript(string path)
        {
            using (FileStream readerStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                Microsoft.Win32.SafeHandles.SafeFileHandle safeFileHandle = readerStream.SafeFileHandle;

                using (StreamReader scriptReader = new StreamReader(readerStream, Encoding.Default))
                {
                    return scriptReader.ReadToEnd();
                }
            }
        }

        internal List<string> DiscoveredExports { get; set; }

        internal Dictionary<string, string> DiscoveredAliases { get; set; }

        internal List<RequiredModuleInfo> DiscoveredModules { get; set; }

        internal List<string> DiscoveredCommandFilters { get; set; }

        internal bool AddsSelfToPath { get; set; }

        internal List<TypeDefinitionAst> DiscoveredClasses { get; set; }
    }

    // Defines the visitor that analyzes a script to determine its exports
    // and dependencies.
    internal class ExportVisitor : AstVisitor2
    {
        internal ExportVisitor(bool forCompletion)
        {
            _forCompletion = forCompletion;
            DiscoveredExports = new List<string>();
            DiscoveredFunctions = new Dictionary<string, FunctionDefinitionAst>(StringComparer.OrdinalIgnoreCase);
            DiscoveredAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            DiscoveredModules = new List<RequiredModuleInfo>();
            DiscoveredCommandFilters = new List<string>();
            DiscoveredClasses = new List<TypeDefinitionAst>();
        }

        static ExportVisitor()
        {
            var nameParam = new ParameterInfo { name = "Name", position = 0 };
            var valueParam = new ParameterInfo { name = "Value", position = 1 };
            var aliasParameterInfo = new ParameterBindingInfo { parameterInfo = new[] { nameParam, valueParam } };

            var functionParam = new ParameterInfo { name = "Function", position = -1 };
            var cmdletParam = new ParameterInfo { name = "Cmdlet", position = -1 };
            var aliasParam = new ParameterInfo { name = "Alias", position = -1 };
            var ipmoParameterInfo = new ParameterBindingInfo { parameterInfo = new[] { nameParam, functionParam, cmdletParam, aliasParam } };

            functionParam = new ParameterInfo { name = "Function", position = 0 };
            var exportModuleMemberInfo = new ParameterBindingInfo { parameterInfo = new[] { functionParam, cmdletParam, aliasParam } };

            s_parameterBindingInfoTable = new Dictionary<string, ParameterBindingInfo>(StringComparer.OrdinalIgnoreCase)
            {
                {"New-Alias",                                      aliasParameterInfo},
                {@"Microsoft.PowerShell.Utility\New-Alias",        aliasParameterInfo},
                {"Set-Alias",                                      aliasParameterInfo},
                {@"Microsoft.PowerShell.Utility\Set-Alias",        aliasParameterInfo},
                {"nal",                                            aliasParameterInfo},
                {"sal",                                            aliasParameterInfo},
                {"Import-Module",                                  ipmoParameterInfo},
                {@"Microsoft.PowerShell.Core\Import-Module",       ipmoParameterInfo},
                {"ipmo",                                           ipmoParameterInfo},
                {"Export-ModuleMember",                            exportModuleMemberInfo},
                {@"Microsoft.PowerShell.Core\Export-ModuleMember", exportModuleMemberInfo}
            };
        }

        private readonly bool _forCompletion;

        internal List<string> DiscoveredExports { get; set; }

        internal List<RequiredModuleInfo> DiscoveredModules { get; set; }

        internal Dictionary<string, FunctionDefinitionAst> DiscoveredFunctions { get; set; }

        internal Dictionary<string, string> DiscoveredAliases { get; set; }

        internal List<string> DiscoveredCommandFilters { get; set; }

        internal bool AddsSelfToPath { get; set; }

        internal List<TypeDefinitionAst> DiscoveredClasses { get; set; }

        public override AstVisitAction VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst)
        {
            DiscoveredClasses.Add(typeDefinitionAst);
            return _forCompletion ? AstVisitAction.Continue : AstVisitAction.SkipChildren;
        }

        // Capture simple function definitions
        public override AstVisitAction VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst)
        {
            // Nested functions are ignored for the purposes of exports, but are still
            // recorded for command/parameter completion.

            // function Foo-Bar { ... }

            var functionName = functionDefinitionAst.Name;
            DiscoveredFunctions[functionName] = functionDefinitionAst;
            ModuleIntrinsics.Tracer.WriteLine("Discovered function definition: {0}", functionName);

            // Check if they've defined any aliases
            // function Foo-Bar { [Alias("Alias1", "...")] param() ... }

            var functionBody = functionDefinitionAst.Body;
            if ((functionBody.ParamBlock != null) && (functionBody.ParamBlock.Attributes != null))
            {
                foreach (AttributeAst attribute in functionBody.ParamBlock.Attributes)
                {
                    if (attribute.TypeName.GetReflectionAttributeType() == typeof(AliasAttribute))
                    {
                        foreach (ExpressionAst aliasAst in attribute.PositionalArguments)
                        {
                            var aliasExpression = aliasAst as StringConstantExpressionAst;
                            if (aliasExpression != null)
                            {
                                string alias = aliasExpression.Value;

                                DiscoveredAliases[alias] = functionName;
                                ModuleIntrinsics.Tracer.WriteLine("Function defines alias: {0} = {1}", alias, functionName);
                            }
                        }
                    }
                }
            }

            if (_forCompletion)
            {
                if (Ast.GetAncestorAst<ScriptBlockAst>(functionDefinitionAst).Parent == null)
                {
                    DiscoveredExports.Add(functionName);
                }

                return AstVisitAction.Continue;
            }

            DiscoveredExports.Add(functionName);
            return AstVisitAction.SkipChildren;
        }

        // Capture modules that add themselves to the path (so they generally package their functionality
        // as loose PS1 files)
        public override AstVisitAction VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst)
        {
            // $env:PATH += "";$psScriptRoot""
            if (string.Equals("$env:PATH", assignmentStatementAst.Left.ToString(), StringComparison.OrdinalIgnoreCase) &&
                Regex.IsMatch(assignmentStatementAst.Right.ToString(), "\\$psScriptRoot", RegexOptions.IgnoreCase))
            {
                ModuleIntrinsics.Tracer.WriteLine("Module adds itself to the path.");
                AddsSelfToPath = true;
            }

            return AstVisitAction.SkipChildren;
        }

        // We skip a bunch of random statements because we can't really be accurate detecting functions/classes etc. that
        // are conditionally defined.
        public override AstVisitAction VisitIfStatement(IfStatementAst ifStmtAst) { return AstVisitAction.SkipChildren; }

        public override AstVisitAction VisitDataStatement(DataStatementAst dataStatementAst) { return AstVisitAction.SkipChildren; }

        public override AstVisitAction VisitForEachStatement(ForEachStatementAst forEachStatementAst) { return AstVisitAction.SkipChildren; }

        public override AstVisitAction VisitForStatement(ForStatementAst forStatementAst) { return AstVisitAction.SkipChildren; }

        public override AstVisitAction VisitDoUntilStatement(DoUntilStatementAst doUntilStatementAst) { return AstVisitAction.SkipChildren; }

        public override AstVisitAction VisitDoWhileStatement(DoWhileStatementAst doWhileStatementAst) { return AstVisitAction.SkipChildren; }

        public override AstVisitAction VisitWhileStatement(WhileStatementAst whileStatementAst) { return AstVisitAction.SkipChildren; }

        public override AstVisitAction VisitInvokeMemberExpression(InvokeMemberExpressionAst methodCallAst) { return AstVisitAction.SkipChildren; }

        public override AstVisitAction VisitSwitchStatement(SwitchStatementAst switchStatementAst) { return AstVisitAction.SkipChildren; }

        public override AstVisitAction VisitTernaryExpression(TernaryExpressionAst ternaryExpressionAst) { return AstVisitAction.SkipChildren; }

        // Visit one the other variations:
        //  - Dotting scripts
        //  - Setting aliases
        //  - Importing modules
        //  - Exporting module members
        public override AstVisitAction VisitCommand(CommandAst commandAst)
        {
            string commandName =
                commandAst.GetCommandName() ??
                GetSafeValueVisitor.GetSafeValue(commandAst.CommandElements[0], null, GetSafeValueVisitor.SafeValueContext.ModuleAnalysis) as string;

            if (commandName == null)
                return AstVisitAction.SkipChildren;

            // They are trying to dot a script
            if (commandAst.InvocationOperator == TokenKind.Dot)
            {
                // . Foo-Bar4.ps1
                // . $psScriptRoot\Foo-Bar.ps1 -Bing Baz
                // . ""$psScriptRoot\Support Files\Foo-Bar2.ps1"" -Bing Baz
                // . '$psScriptRoot\Support Files\Foo-Bar3.ps1' -Bing Baz

                DiscoveredModules.Add(
                    new RequiredModuleInfo { Name = commandName, CommandsToPostFilter = new List<string>() });
                ModuleIntrinsics.Tracer.WriteLine("Module dots {0}", commandName);
            }

            // They are setting an alias.
            if (string.Equals(commandName, "New-Alias", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(commandName, "Microsoft.PowerShell.Utility\\New-Alias", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(commandName, "Set-Alias", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(commandName, "Microsoft.PowerShell.Utility\\Set-Alias", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(commandName, "nal", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(commandName, "sal", StringComparison.OrdinalIgnoreCase))
            {
                // Set-Alias Foo-Bar5 Foo-Bar
                // Set-Alias -Name Foo-Bar6 -Value Foo-Bar
                // sal Foo-Bar7 Foo-Bar
                // sal -Value Foo-Bar -Name Foo-Bar8

                var boundParameters = DoPseudoParameterBinding(commandAst, commandName);

                var name = boundParameters["Name"] as string;
                if (!string.IsNullOrEmpty(name))
                {
                    var value = boundParameters["Value"] as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        // These aren't stored in DiscoveredExports, as they are only
                        // exported after the user calls Export-ModuleMember.
                        DiscoveredAliases[name] = value;
                        ModuleIntrinsics.Tracer.WriteLine("Module defines alias: {0} = {1}", name, value);
                    }
                }

                return AstVisitAction.SkipChildren;
            }

            // They are importing a module
            if (string.Equals(commandName, "Import-Module", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(commandName, "ipmo", StringComparison.OrdinalIgnoreCase))
            {
                // Import-Module Module1
                // Import-Module Module2 -Function Foo-Module2*, Foo-Module2Second* -Cmdlet Foo-Module2Cmdlet,Foo-Module2Cmdlet*
                // Import-Module Module3 -Function Foo-Module3Command1, Foo-Module3Command2
                // Import-Module Module4,
                //    Module5
                // Import-Module -Name Module6,
                //    Module7 -Global

                var boundParameters = DoPseudoParameterBinding(commandAst, commandName);

                List<string> commandsToPostFilter = new List<string>();

                Action<string> onEachCommand = importedCommandName => commandsToPostFilter.Add(importedCommandName);

                // Process any exports from the module that we determine from
                // the -Function, -Cmdlet, or -Alias parameters
                ProcessCmdletArguments(boundParameters["Function"], onEachCommand);
                ProcessCmdletArguments(boundParameters["Cmdlet"], onEachCommand);
                ProcessCmdletArguments(boundParameters["Alias"], onEachCommand);

                // Now, go through all of the discovered modules on Import-Module
                // and register them for deeper investigation.
                Action<string> onEachModule = moduleName =>
                {
                    ModuleIntrinsics.Tracer.WriteLine("Discovered module import: {0}", moduleName);
                    DiscoveredModules.Add(
                        new RequiredModuleInfo
                        {
                            Name = moduleName,
                            CommandsToPostFilter = commandsToPostFilter
                        });
                };
                ProcessCmdletArguments(boundParameters["Name"], onEachModule);

                return AstVisitAction.SkipChildren;
            }

            // They are exporting a module member
            if (string.Equals(commandName, "Export-ModuleMember", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(commandName, "Microsoft.PowerShell.Core\\Export-ModuleMember", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(commandName, "$script:ExportModuleMember", StringComparison.OrdinalIgnoreCase))
            {
                // Export-ModuleMember *
                // Export-ModuleMember Exported-UnNamedModuleMember
                // Export-ModuleMember -Function Exported-FunctionModuleMember1, Exported-FunctionModuleMember2 -Cmdlet Exported-CmdletModuleMember `
                //    -Alias Exported-AliasModuleMember
                // & $script:ExportModuleMember -Function (...)

                var boundParameters = DoPseudoParameterBinding(commandAst, commandName);

                Action<string> onEachFunction = exportedCommandName =>
                {
                    DiscoveredCommandFilters.Add(exportedCommandName);
                    ModuleIntrinsics.Tracer.WriteLine("Discovered explicit export: {0}", exportedCommandName);

                    // If the export doesn't contain wildcards, then add it to the
                    // discovered commands as well. It is likely that they created
                    // the command dynamically
                    if ((!WildcardPattern.ContainsWildcardCharacters(exportedCommandName)) &&
                        (!DiscoveredExports.Contains(exportedCommandName)))
                    {
                        DiscoveredExports.Add(exportedCommandName);
                    }
                };
                ProcessCmdletArguments(boundParameters["Function"], onEachFunction);
                ProcessCmdletArguments(boundParameters["Cmdlet"], onEachFunction);

                Action<string> onEachAlias = exportedAlias =>
                {
                    DiscoveredCommandFilters.Add(exportedAlias);

                    // If the export doesn't contain wildcards, then add it to the
                    // discovered commands as well. It is likely that they created
                    // the command dynamically
                    if (!WildcardPattern.ContainsWildcardCharacters(exportedAlias))
                    {
                        DiscoveredAliases[exportedAlias] = null;
                    }
                };
                ProcessCmdletArguments(boundParameters["Alias"], onEachAlias);

                return AstVisitAction.SkipChildren;
            }

            // They are exporting a module member using our advanced 'public' function
            // that we've presented in many demos
            if ((string.Equals(commandName, "public", StringComparison.OrdinalIgnoreCase)) &&
                (commandAst.CommandElements.Count > 2))
            {
                // public function Publicly-ExportedFunction
                // public alias Publicly-ExportedAlias
                string publicCommandName = commandAst.CommandElements[2].ToString().Trim();
                DiscoveredExports.Add(publicCommandName);
                DiscoveredCommandFilters.Add(publicCommandName);
            }

            return AstVisitAction.SkipChildren;
        }

        private void ProcessCmdletArguments(object value, Action<string> onEachArgument)
        {
            if (value == null) return;

            var commandName = value as string;
            if (commandName != null)
            {
                onEachArgument(commandName);
                return;
            }

            var names = value as object[];
            if (names != null)
            {
                foreach (var n in names)
                {
                    // This is slightly more permissive than what would really happen with parameter binding
                    // in that it would allow arrays of arrays in ways that don't actually work
                    ProcessCmdletArguments(n, onEachArgument);
                }
            }
        }

        // This method does parameter binding for a very limited set of scenarios, specifically
        // for New-Alias, Set-Alias, Import-Module, and Export-ModuleMember.  It might not even
        // correctly handle these cmdlets if new parameters are added.
        //
        // It also only populates the bound parameters for a limited set of parameters needed
        // for module analysis.
        private static Hashtable DoPseudoParameterBinding(CommandAst commandAst, string commandName)
        {
            var result = new Hashtable(StringComparer.OrdinalIgnoreCase);

            var parameterBindingInfo = s_parameterBindingInfoTable[commandName].parameterInfo;

            int positionsBound = 0;

            for (int i = 1; i < commandAst.CommandElements.Count; i++)
            {
                var element = commandAst.CommandElements[i];
                var specifiedParameter = element as CommandParameterAst;
                if (specifiedParameter != null)
                {
                    bool boundParameter = false;
                    var specifiedParamName = specifiedParameter.ParameterName;
                    foreach (var parameterInfo in parameterBindingInfo)
                    {
                        if (parameterInfo.name.StartsWith(specifiedParamName, StringComparison.OrdinalIgnoreCase))
                        {
                            if (parameterInfo.position != -1)
                            {
                                positionsBound |= 1 << parameterInfo.position;
                            }

                            var argumentAst = specifiedParameter.Argument;
                            if (argumentAst == null)
                            {
                                argumentAst = commandAst.CommandElements[i] as ExpressionAst;
                                if (argumentAst != null)
                                {
                                    i += 1;
                                }
                            }

                            if (argumentAst != null)
                            {
                                boundParameter = true;
                                result[parameterInfo.name] =
                                    GetSafeValueVisitor.GetSafeValue(argumentAst, null, GetSafeValueVisitor.SafeValueContext.ModuleAnalysis);
                            }

                            break;
                        }
                    }

                    if (boundParameter || specifiedParameter.Argument != null)
                    {
                        continue;
                    }

                    if (!"PassThru".StartsWith(specifiedParamName, StringComparison.OrdinalIgnoreCase) &&
                        !"Force".StartsWith(specifiedParamName, StringComparison.OrdinalIgnoreCase) &&
                        !"Confirm".StartsWith(specifiedParamName, StringComparison.OrdinalIgnoreCase) &&
                        !"Global".StartsWith(specifiedParamName, StringComparison.OrdinalIgnoreCase) &&
                        !"AsCustomObject".StartsWith(specifiedParamName, StringComparison.OrdinalIgnoreCase) &&
                        !"Verbose".StartsWith(specifiedParamName, StringComparison.OrdinalIgnoreCase) &&
                        !"Debug".StartsWith(specifiedParamName, StringComparison.OrdinalIgnoreCase) &&
                        !"DisableNameChecking".StartsWith(specifiedParamName, StringComparison.OrdinalIgnoreCase) &&
                        !"NoClobber".StartsWith(specifiedParamName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Named parameter, skip the argument (except for specific switch parameters
                        i += 1;
                    }
                }
                else
                {
                    // Positional argument, find which position we want to bind
                    int pos = 0;
                    for (; pos < 10; pos++)
                    {
                        if ((positionsBound & (1 << pos)) == 0)
                            break;
                    }

                    positionsBound |= 1 << pos;

                    // Now see if we care (we probably do, but if the user did something odd, like specify too many, then we don't really)
                    foreach (var parameterInfo in parameterBindingInfo)
                    {
                        if (parameterInfo.position == pos)
                        {
                            result[parameterInfo.name] = GetSafeValueVisitor.GetSafeValue(
                                commandAst.CommandElements[i], null,
                                GetSafeValueVisitor.SafeValueContext.ModuleAnalysis);
                        }
                    }
                }
            }

            return result;
        }

        private static readonly Dictionary<string, ParameterBindingInfo> s_parameterBindingInfoTable;

        private sealed class ParameterBindingInfo
        {
            internal ParameterInfo[] parameterInfo;
        }

        private struct ParameterInfo
        {
            internal string name;
            internal int position;
        }
    }

    // Class to keep track of modules we need to import, and commands that should
    // be filtered out of them.
    [Serializable]
    internal class RequiredModuleInfo
    {
        internal string Name { get; set; }

        internal List<string> CommandsToPostFilter { get; set; }
    }
}
