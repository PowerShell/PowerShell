/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Activities;
using System.Activities.Expressions;
using Microsoft.PowerShell.Activities;
using Microsoft.PowerShell.Activities.Internal;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Collections.ObjectModel;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using CoreRunspaces = System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Xaml;
using System.Xml;
using System.Activities.XamlIntegration;
using Microsoft.PowerShell.Commands;
using Pipeline = Microsoft.PowerShell.Activities.Pipeline;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;

namespace Microsoft.PowerShell.Workflow
{
    /// <summary>
    /// Exception thrown to return from workflow similar to a function return.
    /// </summary>
    [SerializableAttribute]
    public class WorkflowReturnException : WorkflowTerminatedException
    {
        /// <summary>
        /// Generic constructor
        /// </summary>
        public WorkflowReturnException() : base()
        { }

        /// <summary>
        ///  Initializes a new WorkflowReturnException instance with a given message string.
        /// </summary>
        /// <param name="message">Exception message</param>
        public WorkflowReturnException(string message)
            : base(message)
        { }

        /// <summary>
        /// Initializes a new WorkflowReturnException instance with a message and inner exception.
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">Inner Exception</param>
        public WorkflowReturnException(string message, Exception innerException) :
            base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new WorkflowReturnException with serialization info and streaming context
        /// </summary>
        /// <param name="info">Serialization Info</param>
        /// <param name="context">Streaming context</param>
        protected WorkflowReturnException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        { }
    }

    internal class WorkflowInfoComparer : System.Collections.Generic.IEqualityComparer<WorkflowInfo>
    {
        #region IEqualityComparer<WorkflowInfo> Members

        public bool Equals(WorkflowInfo x, WorkflowInfo y)
        {
            return string.Equals(x.XamlDefinition, y.XamlDefinition, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(WorkflowInfo obj)
        {
            return obj.XamlDefinition.GetHashCode();
        }

        #endregion
    }

    /// <summary>
    /// Converts a PowerShell AST into a function that invokes the corresponding
    /// script as a workflow job.
    /// </summary>
    public sealed class AstToWorkflowConverter : IAstToWorkflowConverter
    {
        private static readonly PowerShellTraceSource Tracer = PowerShellTraceSourceFactory.GetTraceSource();

        /// <summary>
        /// Provides the opportunity for job converters to validate the semantics of
        /// the AST before compilation. This stage should be light-weight and as efficient
        /// as possible.
        /// </summary>
        /// <param name="ast">The PowerShell AST correpsponding to the job's definition.</param>
        /// <returns>A collection of PSParseErrors corresponding to any semantic issues in the AST.</returns>
        public List<ParseError> ValidateAst(FunctionDefinitionAst ast)
        {
            return AstToXamlConverter.Validate(ast);
        }

        /// <summary>
        /// Converts a PowerShell AST into a script block that represents
        /// the workflow to run.
        /// </summary>
        /// <param name="ast">The PowerShell AST correpsponding to the job's definition.</param>
        /// <param name="definingModule">The module that is defining this command (if any)</param>
        /// <returns>
        /// A PowerShell script block that invokes an underlying job,
        /// based on the definition provided by this script block.
        /// </returns>
        public List<WorkflowInfo> CompileWorkflows(ScriptBlockAst ast, PSModuleInfo definingModule)
        {
            ParseException parsingException = null;

            var result = CompileWorkflows(ast, definingModule, null, out parsingException, null);

            if (parsingException.Errors != null)
            {
                throw parsingException;
            }

            return result;
        }


        /// <summary>
        /// Converts a PowerShell AST into a script block that represents
        /// the workflow to run.
        /// </summary>
        /// <param name="ast">The PowerShell AST correpsponding to the job's definition.</param>
        /// <param name="definingModule">The module that is defining this command (if any)</param>
        /// <param name="rootWorkflowName">Only root Workflow will be compiled</param>
        /// <returns>
        /// A PowerShell script block that invokes an underlying job,
        /// based on the definition provided by this script block.
        /// </returns>
        public List<WorkflowInfo> CompileWorkflows(ScriptBlockAst ast, PSModuleInfo definingModule, string rootWorkflowName)
        {
            ParseException parsingException = null;

            var result = CompileWorkflows(ast, definingModule, null, out parsingException, rootWorkflowName);

            if (parsingException.Errors != null)
            {
                throw parsingException;
            }

            return result;
        }

        /// <summary>
        /// Converts a PowerShell AST into a script block that represents
        /// the workflow to run.
        /// </summary>
        /// <param name="ast">The PowerShell AST correpsponding to the job's definition.</param>
        /// <param name="definingModule">The module that is defining this command (if any)</param>
        /// <param name="initialSessionState">The initial session state of a runspace.</param>
        /// <param name="parsingErrors">parsing errors</param>
        /// <returns>
        /// A PowerShell script block that invokes an underlying job,
        /// based on the definition provided by this script block.
        /// </returns>
        public List<WorkflowInfo> CompileWorkflows(ScriptBlockAst ast, PSModuleInfo definingModule, InitialSessionState initialSessionState, out ParseException parsingErrors)
        {

            var result = CompileWorkflows(ast, definingModule, initialSessionState, out parsingErrors, null);

            if (parsingErrors.Errors != null)
            {
                throw parsingErrors;
            }

            return result;
        }

        /// <summary>
        /// Converts a PowerShell AST into a script block that represents
        /// the workflow to run.
        /// </summary>
        /// <param name="ast">The PowerShell AST correpsponding to the job's definition.</param>
        /// <param name="definingModule">The module that is defining this command (if any)</param>
        /// <param name="initialSessionState">The initial session state of a runspace.</param>
        /// <param name="parsingErrors">parsing errors</param>
        /// <param name="rootWorkflowName">Optional, once assigned, only root Workflow will be compiled</param>
        /// <returns>
        /// A PowerShell script block that invokes an underlying job,
        /// based on the definition provided by this script block.
        /// </returns>
        public List<WorkflowInfo> CompileWorkflows(ScriptBlockAst ast, PSModuleInfo definingModule, InitialSessionState initialSessionState, out ParseException parsingErrors, string rootWorkflowName)
        {
            return CompileWorkflowsImpl(ast, definingModule, initialSessionState, null, out parsingErrors, rootWorkflowName);
        }

        /// <summary>
        /// Converts a PowerShell AST into a script block that represents
        /// the workflow to run.
        /// </summary>
        /// <param name="ast">The PowerShell AST correpsponding to the job's definition.</param>
        /// <param name="definingModule">The module that is defining this command (if any).</param>
        /// <param name="initialSessionState">The initial session state of a runspace.</param>
        /// <param name="sourceLanguageMode">Language mode of source that is creating the workflow.</param>
        /// <param name="parsingErrors">Optional, once assigned, only root Workflow will be compiled.</param>
        /// <returns>
        /// A PowerShell script block that invokes an underlying job,
        /// based on the definition provided by this script block.
        /// </returns>
        public List<WorkflowInfo> CompileWorkflows(ScriptBlockAst ast, PSModuleInfo definingModule, InitialSessionState initialSessionState, PSLanguageMode? sourceLanguageMode, out ParseException parsingErrors)
        {
            return CompileWorkflowsImpl(ast, definingModule, initialSessionState, sourceLanguageMode, out parsingErrors, null);
        }

        /// <summary>
        /// Converts a PowerShell AST into a script block that represents
        /// the workflow to run.
        /// </summary>
        /// <param name="ast">The PowerShell AST correpsponding to the job's definition.</param>
        /// <param name="definingModule">The module that is defining this command (if any)</param>
        /// <param name="initialSessionState">The initial session state of a runspace.</param>
        /// <param name="sourceLanguageMode">Language mode of source that is creating the workflow.</param>
        /// <param name="parsingErrors">parsing errors</param>
        /// <param name="rootWorkflowName">Optional, once assigned, only root Workflow will be compiled</param>
        /// <returns>
        /// A PowerShell script block that invokes an underlying job,
        /// based on the definition provided by this script block.
        /// </returns>
        private List<WorkflowInfo> CompileWorkflowsImpl(ScriptBlockAst ast, PSModuleInfo definingModule, InitialSessionState initialSessionState, PSLanguageMode? sourceLanguageMode, out ParseException parsingErrors, string rootWorkflowName)
        {
            List<ParseError> errorList = new List<ParseError>();

            if (ast == null)
            {
                throw new PSArgumentNullException("ast");
            }

            // if user specifies rootWorkflowName, we will check if it exists in the given ast.
            if (rootWorkflowName != null)
            {
                var methods = ast.FindAll(a => a is FunctionDefinitionAst, true);
                bool isWFNameMatch = false;
                foreach (FunctionDefinitionAst method in methods)
                {
                    if (method.Name == rootWorkflowName)
                    {
                        isWFNameMatch = true;
                        break;
                    }
                }

                if (!isWFNameMatch)
                {
                    string error = String.Format(CultureInfo.InvariantCulture, ActivityResources.InvalidRootWorkflowName, rootWorkflowName);
                    throw new PSArgumentException(error);
                }
            }

            var dependencies = new Dictionary<FunctionDefinitionAst, DependencyGraphNode>();
            var scope = BuildSymbolTable(ast, null, dependencies);
            foreach (var scopeEntry in scope.functionDefinitions.Values)
            {
                AnalyzeFunctionBody(scopeEntry, scope, dependencies);
            }

            // Now do a topological sort.
            var outputList = new List<Scope.Entry>();
            var readyList = dependencies.Values.Where(n => n.outgoingCalls.Count == 0).Select(node => node.scopeEntry).ToList();

            while (readyList.Count > 0)
            {
                var entry = readyList[0];
                outputList.Add(entry);
                readyList.RemoveAt(0);

                var node = dependencies[entry.functionDefinition];
                foreach (var caller in node.incomingCallers)
                {
                    var nodeCaller = dependencies[caller.functionDefinition];
                    nodeCaller.outgoingCalls.Remove(entry);
                    if (nodeCaller.outgoingCalls.Count == 0)
                        readyList.Add(nodeCaller.scopeEntry);
                }
            }

            if (outputList.Count != dependencies.Count)
            {
                // There must be a cycle.  Workflows can't be recursive, so generate an error.
                var error = new ParseError(ast.Extent, "RecursiveWorkflowNotSupported", ActivityResources.RecursiveWorkflowNotSupported);
                errorList.Add(error);
            }

            Ast parentAst = ast;
            while (parentAst.Parent != null)
            {
                parentAst = parentAst.Parent;
            }
            var requirements = ((ScriptBlockAst)parentAst).ScriptRequirements;

            System.Management.Automation.PowerShell invoker;
            bool useCurrentRunspace = false;

            HashSet<string> processedActivityLibraries;
            Dictionary<string, Type> activityMap;
            var requiredAssemblies = new Collection<string>();

            if (requirements != null)
            {
                foreach (var reqAssembly in requirements.RequiredAssemblies)
                    requiredAssemblies.Add(reqAssembly);
            }

            if (initialSessionState != null)
            {
                invoker = System.Management.Automation.PowerShell.Create(initialSessionState);
                var scopeFromIss = GetScopeFromIss(initialSessionState, invoker, out processedActivityLibraries, out activityMap);

                // Add functionDefinitions, if any, from scopeFromIss to parent scope, so that they will be available for all FunctionDefintionAsts
                foreach(var entry in scopeFromIss.functionDefinitions)
                {
                    scope.functionDefinitions.Add(entry.Key, entry.Value);
                }

                // Add assemblies from initialSessionState to requiredAssemblies
                foreach (var ssae in initialSessionState.Assemblies)
                {
                    if (!string.IsNullOrEmpty(ssae.FileName))
                    {
                        requiredAssemblies.Add(ssae.FileName);
                    }
                    else if (!string.IsNullOrEmpty(ssae.Name))
                    {
                        requiredAssemblies.Add(ssae.Name);
                    }
                }
            }
            else
            {

                useCurrentRunspace = Runspace.CanUseDefaultRunspace;
                invoker = System.Management.Automation.PowerShell.Create(useCurrentRunspace
                                                                       ? RunspaceMode.CurrentRunspace
                                                                       : RunspaceMode.NewRunspace);

                activityMap = AstToXamlConverter.GetActivityMap(requiredAssemblies, out processedActivityLibraries);
            }

            var result = new List<WorkflowInfo>();
            try
            {
                foreach (var entry in outputList)
                {
                    var func = entry.functionDefinition;
                    if (!func.IsWorkflow)
                        continue;

                    try
                    {
                        entry.workflowInfo = CompileSingleWorkflow(entry.scope, func, scriptBlockTokenCache, definingModule, requiredAssemblies, activityMap, processedActivityLibraries, invoker, sourceLanguageMode, rootWorkflowName);
                        result.Add(entry.workflowInfo);
                    }
                    catch (ParseException e)
                    {
                        errorList.AddRange(e.Errors);                        
                    }
                }
            }
            finally
            {
                if (!useCurrentRunspace)
                {
                    invoker.Dispose();
                }
            }

            if (errorList.Count > 0)
            {
                parsingErrors = new ParseException(errorList.ToArray());
            }
            else
            {
                parsingErrors = new ParseException();
            }

            return result;
        }
        Dictionary<Ast, Token[]> scriptBlockTokenCache = new Dictionary<Ast, Token[]>();

        private static WorkflowInfo CompileSingleWorkflow(Scope scope,
                                                          FunctionDefinitionAst func,
                                                          Dictionary<Ast, Token[]> scriptBlockTokenCache,
                                                          PSModuleInfo definingModule,
                                                          IEnumerable<string> assemblyList,
                                                          Dictionary<string, Type> activityMap,
                                                          HashSet<string> processedActivityLibraries,
                                                          System.Management.Automation.PowerShell invoker,
                                                          PSLanguageMode? sourceLanguageMode = (PSLanguageMode?)null,
                                                          string rootWorkflowName = null)
        {
            Dictionary<string, ParameterAst> parameterValidation;
            WorkflowInfo[] calledWorkflows;
            Dictionary<string, string> referencedAssemblies;
            string workflowAttributes;

            var xaml = AstToXamlConverter.Convert(func, scope, definingModule, activityMap, processedActivityLibraries,
                                                  out parameterValidation, out calledWorkflows, out referencedAssemblies, out workflowAttributes,
                                                  assemblyList, invoker);

            // This step does two major things:
            // - it takes all of the dependent workflows and compiles them into in-memory dlls.
            // - it synthesizes the text for user-callable powershell function from the workflow XAML definition
            string modulePath = null;
            if (definingModule != null)
            {
                modulePath = definingModule.ModuleBase;
            }
            else if (!String.IsNullOrEmpty(func.Extent.File))
            {
                modulePath = Path.GetDirectoryName(func.Extent.File);
            }

            // Get the topmost parent AST for the workflow function.
            Ast parentAst = func;
            while (parentAst.Parent != null)
            {
                parentAst = parentAst.Parent;
            }

            // Pass either the workflow script file path if available or the full source otherwise.
            string scriptFile = parentAst.Extent.File;
            string scriptSource = string.IsNullOrEmpty(scriptFile) ? parentAst.Extent.StartScriptPosition.GetFullScript() : null;
            ReadOnlyCollection<AttributeAst> attributeAstCollection = (func.Body.ParamBlock != null) ? func.Body.ParamBlock.Attributes : null;
            var functionDefinition = ImportWorkflowCommand.CreateFunctionFromXaml(func.Name, xaml,
                                                                                  referencedAssemblies, calledWorkflows.Select(wfi => wfi.NestedXamlDefinition).ToArray(),
                                                                                  null, parameterValidation, modulePath, true, workflowAttributes,
                                                                                  scriptFile, scriptSource, rootWorkflowName, sourceLanguageMode, attributeAstCollection);

            var helpContent = func.GetHelpContent(scriptBlockTokenCache);
            if (helpContent != null)
            {
                functionDefinition = helpContent.GetCommentBlock() + functionDefinition;
            }
                
            var sb = ScriptBlock.Create(functionDefinition);
            sb.DebuggerHidden = true;

            var defnText = func.Body.Extent.Text;
            defnText = defnText.Substring(1, defnText.Length - 2);
            return new WorkflowInfo(func.Name, defnText, sb, xaml, calledWorkflows, definingModule);
        }

        static internal IEnumerable<string> GetRequiredAssembliesFromInitialSessionState(
            InitialSessionState initialSessionState,
            System.Management.Automation.PowerShell invoker)
        {          
            var getModuleCommand = new CmdletInfo("Get-Module", typeof(GetModuleCommand));
            invoker.Commands.Clear();
            invoker.AddCommand(getModuleCommand)
                .AddParameter("ErrorAction", ActionPreference.Ignore);
            var modules = invoker.Invoke<PSModuleInfo>();

            var modulesToProcess = new Stack<PSModuleInfo>(modules);
            while (modulesToProcess.Count > 0)
            {
                var module = modulesToProcess.Pop();

                foreach (var assem in module.RequiredAssemblies)
                {
                    yield return assem;
                }

                foreach (var nestedModule in module.NestedModules)
                {
                    modulesToProcess.Push(nestedModule);
                }
            }

            // All required assemblies have been loaded now, so we can iterate through
            // the app domain and match up assemblies from iss.
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                {
                    continue;
                }

                foreach (var ssae in initialSessionState.Assemblies)
                {
                    if (!string.IsNullOrEmpty(ssae.Name))
                    {
                        // Compare against full name and partial name
                        if (assembly.FullName.Equals(ssae.Name, StringComparison.OrdinalIgnoreCase)
                            || assembly.GetName().Name.Equals(ssae.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            yield return assembly.FullName;
                            continue;
                        }
                    }
                    if (!string.IsNullOrEmpty(ssae.FileName))
                    {
                        if (assembly.Location.Equals(ssae.FileName, StringComparison.OrdinalIgnoreCase))
                        {
                            yield return ssae.FileName;
                            continue;
                        }
                    }
                }
            }
        }

        internal static Scope GetScopeFromIss(InitialSessionState iss,
                                              System.Management.Automation.PowerShell invoker,
                                              out HashSet<string> processedActivityLibraries,
                                              out Dictionary<string, Type> activityMap)
        {
            var scope = new Scope
            {
                functionDefinitions = new Dictionary<string, Scope.Entry>(StringComparer.OrdinalIgnoreCase)
            };

            activityMap = AstToXamlConverter.GetActivityMap(GetRequiredAssembliesFromInitialSessionState(iss, invoker), out processedActivityLibraries);

            foreach (var sswe in iss.Commands.OfType<SessionStateWorkflowEntry>())
            {
                var issFn = AstToXamlConverter.GetScriptAsFunction(sswe.Name, sswe.Definition, isWorkflow: true);
                var entry = new Scope.Entry
                {
                    functionDefinition = issFn,
                    scope = new Scope
                    {
                        functionDefinitions = new Dictionary<string, Scope.Entry>(StringComparer.OrdinalIgnoreCase)
                    },
                    workflowInfo = (new AstToWorkflowConverter()).CompileWorkflow(sswe.Name, sswe.Definition, scope, processedActivityLibraries, activityMap, invoker)
                };
                scope.functionDefinitions.Add(sswe.Name, entry);
            }

            return scope;
        }

        /// <summary>
        /// Compile a single workflow from it's definition as a string.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="definition"></param>
        /// <param name="initialSessionState"></param>
        /// <returns></returns>
        public WorkflowInfo CompileWorkflow(string name, string definition, InitialSessionState initialSessionState)
        {
            if (name == null)
            {
                throw new PSArgumentNullException("name");
            }

            if (definition == null)
            {
                throw new PSArgumentNullException("definition");
            }

            if (initialSessionState == null)
            {
                throw new PSArgumentNullException("initialSessionState");
            }

            var fnDefn = AstToXamlConverter.GetScriptAsFunction(name, definition, isWorkflow: true);

            var invoker = System.Management.Automation.PowerShell.Create(initialSessionState);
            try
            {
                HashSet<string> processedActivityLibraries;
                Dictionary<string, Type> activityMap;

                var scope = GetScopeFromIss(initialSessionState, invoker, out processedActivityLibraries, out activityMap);
                return CompileSingleWorkflow(scope, fnDefn, scriptBlockTokenCache, null, null, activityMap, processedActivityLibraries, invoker);
            }
            finally
            {
                invoker.Dispose();
            }
        }

        internal WorkflowInfo CompileWorkflow(string name,
                                              string definition,
                                              Scope scope,
                                              HashSet<string> processedActivityLibraries,
                                              Dictionary<string, Type> activityMap,
                                              System.Management.Automation.PowerShell invoker)
        {
            var fnDefn = AstToXamlConverter.GetScriptAsFunction(name, definition, isWorkflow: true);
            return CompileSingleWorkflow(scope, fnDefn, scriptBlockTokenCache, null, null, activityMap, processedActivityLibraries, invoker);
        }

        /// <summary>
        /// Returns the parameters of the activity called by the <paramref name="commandAst"/>.
        /// </summary>
        /// <param name="commandAst">The ast representing the command called</param>
        /// <returns>The parameters with their corresponding types, or null if the parameters cannot be found.</returns>
        public static Dictionary<string, Type> GetActivityParameters(CommandAst commandAst)
        {
            CommandInfo command;

            Ast workflowRootAst = commandAst;
            while (!(workflowRootAst is FunctionDefinitionAst))
            {
                workflowRootAst = workflowRootAst.Parent;
            }
            var scope = new Scope
            {
                functionDefinitions = new Dictionary<string, Scope.Entry>(StringComparer.OrdinalIgnoreCase)
            };

            bool useCurrentRunspace = Runspace.CanUseDefaultRunspace;
            var invoker = System.Management.Automation.PowerShell.Create(useCurrentRunspace
                                                                   ? RunspaceMode.CurrentRunspace
                                                                   : RunspaceMode.NewRunspace);
            try
            {
                HashSet<string> processedActivityLibraries;
                var activityMap = AstToXamlConverter.GetActivityMap(null, out processedActivityLibraries);
                var converter = new AstToXamlConverter(null, (FunctionDefinitionAst)workflowRootAst, null, activityMap, processedActivityLibraries, true, scope, invoker);

                string commandName = commandAst.GetCommandName();
                AstToXamlConverter.ActivityKind activityKind = converter.ResolveActivityKindBasedOnCommandName(commandName, commandAst, out command, true);

                switch (activityKind)
                {
                case AstToXamlConverter.ActivityKind.InlineScript:
                    return AstToXamlConverter.GetAvailableProperties(typeof(InlineScript), null);
                    
                case AstToXamlConverter.ActivityKind.Persist:
                    // The checkpoint-workflow activity accepts no parameters
                    return null;

                case AstToXamlConverter.ActivityKind.Suspend:
                    // The suspend-workflow activity accepts only one optional parameter, the syntax is:
                    // Suspend-Workflow [-Label <string>]
                    return new Dictionary<string, Type>() {{ "Label", typeof(string) }};

                case AstToXamlConverter.ActivityKind.InvokeExpression:
                    // The real Invoke-Expression activity has the common parameters but the Language parameter is
                    // not available in the real activity.  To encourage the expected use of Invoke-Expression,
                    // we'll only return -Language and -Command.
                    return new Dictionary<string, Type>() {{"Language", typeof (string)}, {"Command", typeof (string)}};

                case AstToXamlConverter.ActivityKind.Delay:
                    return new Dictionary<string, Type> {{"Seconds", typeof (int)}, {"Milliseconds", typeof (int)}};

                case AstToXamlConverter.ActivityKind.NewObject:
                    return new Dictionary<string, Type> {{"TypeName", typeof (string)}};

                case AstToXamlConverter.ActivityKind.RegularCommand:
                    // If the command resolved to a script, the command metadata has all of it's parameters,                
                    if ((command != null) && (command.CommandType & (CommandTypes.ExternalScript | CommandTypes.Workflow | CommandTypes.Function | CommandTypes.Filter | CommandTypes.Configuration)) != 0)
                    {
                        return null;
                    }

                    CommandInfo resolvedCommand;
                    Type[] genericTypes;

                    // Find the activity for this name
                    Type activityType = converter.ResolveActivityType(commandName, commandAst, true, out resolvedCommand, out genericTypes);

                    if (activityType == null || resolvedCommand == null)
                    {
                        return null;
                    }

                    Dictionary<string, Type> availableProperties = AstToXamlConverter.GetAvailableProperties(activityType, null);
                    Dictionary<string, Type> virtualProperties = AstToXamlConverter.GetVirtualProperties(activityType, null);
                    if (virtualProperties != null)
                    {
                        foreach (KeyValuePair<string, Type> virtualProperty in virtualProperties)
                        {
                            availableProperties.Add(virtualProperty.Key, virtualProperty.Value);
                        }
                    }

                    return availableProperties;
                }
            }
            finally
            {
                if (!useCurrentRunspace)
                {
                    invoker.Dispose();
                }
            }

            return null;
        }

        #region Dependency graph

        class DependencyGraphNode
        {
            internal Scope.Entry scopeEntry;
            internal List<Scope.Entry> outgoingCalls = new List<Scope.Entry>();
            internal List<Scope.Entry> incomingCallers = new List<Scope.Entry>();
        }

        private static void AnalyzeFunctionBody(Scope.Entry defnEntry, Scope parentScope, Dictionary<FunctionDefinitionAst, DependencyGraphNode> dependencies)
        {
            var functionDefinitionAst = defnEntry.functionDefinition;
            var scriptBlockAst = functionDefinitionAst.Body;
            var currentNode = dependencies[functionDefinitionAst];
            var innerScope = BuildSymbolTable(scriptBlockAst, parentScope, dependencies);
            defnEntry.scope = innerScope;
            foreach (var commandAst in scriptBlockAst.FindAll(ast => true, searchNestedScriptBlocks: false).OfType<CommandAst>())
            {
                var commandName = commandAst.GetCommandName();
                if (!string.IsNullOrEmpty(commandName))
                {
                    var scopeEntry = innerScope.LookupCommand(commandName);
                    if (scopeEntry != null && !currentNode.outgoingCalls.Contains(scopeEntry))
                    {
                        currentNode.outgoingCalls.Add(scopeEntry);
                        dependencies[scopeEntry.functionDefinition].incomingCallers.Add(defnEntry);
                    }
                }
            }

            foreach (var scopeEntry in innerScope.functionDefinitions.Values)
            {
                AnalyzeFunctionBody(scopeEntry, innerScope, dependencies);
            }
        }

        static Scope BuildSymbolTable(ScriptBlockAst scriptBlockAst, Scope parentScope, Dictionary<FunctionDefinitionAst, DependencyGraphNode> dependencies)
        {
            var table = new Dictionary<string, Scope.Entry>(StringComparer.OrdinalIgnoreCase);
            var scope = new Scope { parent = parentScope, functionDefinitions = table };

            foreach (var defn in scriptBlockAst.FindAll(ast => true, searchNestedScriptBlocks: false).OfType<FunctionDefinitionAst>())
            {
                if (table.ContainsKey(defn.Name))
                {
                    var errorMsg = String.Format(CultureInfo.InvariantCulture, ActivityResources.FunctionRedefinitionNotAllowed, defn.Name);
                    var error = new ParseError(defn.Extent, "FunctionRedefinitionNotAllowed", errorMsg);
                    throw new ParseException(new [] {error});
                }
                var entry = new Scope.Entry {functionDefinition = defn};
                table.Add(defn.Name, entry);
                dependencies.Add(defn, new DependencyGraphNode {scopeEntry = entry});
            }

            return scope;
        }

        #endregion Dependency graph
    }

    internal class Scope
    {
        internal class Entry
        {
            internal WorkflowInfo workflowInfo;
            internal FunctionDefinitionAst functionDefinition;
            internal Scope scope;
        }

        internal Scope parent;
        internal Dictionary<string, Scope.Entry> functionDefinitions;

        internal Entry LookupCommand(string name)
        {
            Scope currentScope = this;

            while (currentScope != null)
            {
                Entry result;
                if (currentScope.functionDefinitions.TryGetValue(name, out result))
                {
                    return result;
                }
                currentScope = currentScope.parent;
            }

            return null;
        }
    }

    /// <summary>
    /// Converts a PowerShell AST into the workflow XAML that represents it.
    /// </summary>
    public class AstToXamlConverter : ICustomAstVisitor
    {
        /// <summary>
        /// Creates a new PowerShellXaml converter
        /// </summary>
        /// <param name="name">The name of the command being converted</param>
        /// <param name="workflowAstRoot">The AST that is the root of the PowerShell script to convert</param>
        /// <param name="definingModule">The module that is defining this command (if any)</param>
        /// <param name="activityMap">The dictionary mapping activities to their types.</param>
        /// <param name="processedActivityLibraries">A hashset of activity libraries that the workflow depends on</param>
        /// <param name="validateOnly">Only do validation.</param>
        /// <param name="scope">Scope chain used to resolve commands lexically</param>
        /// <param name="invoker"></param>
        internal AstToXamlConverter(string name,
                                    FunctionDefinitionAst workflowAstRoot,
                                    PSModuleInfo definingModule,
                                    Dictionary<string, Type> activityMap,
                                    HashSet<string> processedActivityLibraries,
                                    bool validateOnly,
                                    Scope scope,
                                    System.Management.Automation.PowerShell invoker)
        {
            this.name = name;
            this.scriptWorkflowAstRoot = workflowAstRoot;
            this.activityMap = activityMap;
            this.processedActivityLibraries = processedActivityLibraries;
            this.validateOnly = validateOnly;
            this.definingModule = definingModule;
            this.scope = scope;
            this.invoker = invoker;
        }

        static AstToXamlConverter()
        {
            PopulateActivityStaticMap();

            List<string> supportedCommonParameters = new List<string>() { "Verbose", "Debug", "ErrorAction", "WarningAction", "InformationAction" };
            ignoredParameters = new List<string>(Cmdlet.CommonParameters.Concat<string>(Cmdlet.OptionalCommonParameters));
            ignoredParameters.RemoveAll( item => supportedCommonParameters.Contains(item, StringComparer.OrdinalIgnoreCase) );
        }

        /// <summary>
        /// Any parameter validation attributes associated with this script block.
        /// </summary>
        internal Dictionary<string, ParameterAst> ParameterValidation
        {
            get { return parameterValidation; }
        }
        private Dictionary<string, ParameterAst> parameterValidation = new Dictionary<string, ParameterAst>(StringComparer.OrdinalIgnoreCase);
        
        private bool disableSymbolGeneration = false;
        private string name = null;
        private PSModuleInfo definingModule = null;
        private readonly Ast scriptWorkflowAstRoot;
        private int _currentIndentLevel;      
        private Scope scope;
        private System.Management.Automation.PowerShell invoker;

        // Remember the assemblies we've processed, as these should
        // correspond to the module names of commands we're processing.
        // If the user ever tries to call a command from a module that we've
        // processed - but that command is not found - then we generate an
        // error because that activity was probably intentionally excluded.
        private HashSet<string> processedActivityLibraries;
        private Dictionary<string, Type> activityMap;
        private static HashSet<string> staticProcessedActivityLibraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, Type> staticActivityMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        // Bool to identify if the workflow uses return / exit.
        // If so, we need to generate a try / catch statement to wrap this control flow.
        bool hasControlFlowException = false;

        // Indicate whether to merge error stream for a specific CommandAst or CommandExpressionAst
        private bool mergeErrorToOutput = false;

        // XAML elements
        const string xamlHeader = @"
<Activity
    x:Class=""Microsoft.PowerShell.DynamicActivities.{0}""
    xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
    xmlns:sad=""clr-namespace:System.Activities.Debugger;assembly=System.Activities""
    xmlns:local=""clr-namespace:Microsoft.PowerShell.DynamicActivities""
    xmlns:mva=""clr-namespace:Microsoft.VisualBasic.Activities;assembly=System.Activities""
    mva:VisualBasic.Settings=""Assembly references and imported namespaces serialized as XML namespaces"" 
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""";

        private const string xamlFooter = @"</Activity>";
        private const string AppendOutputTemplate = @" AppendOutput = ""True""";
        private const string GenericTypesKey = @"Activity-GenericTypes";
        private const string MemberTemplate = @"<Variable Name=""{0}"" x:TypeArguments=""{1}"" {2}/>";
        private const string M3PKeyForThrowStatement = @"__Microsoft.PowerShell.Activities.Throw";
        
        // PseudoCommands that only work in the script workflow
        // Please keep in sync with the System.Management.Automation.CompletionCompleter.PseudoCommands
        private const string CheckpointWorkflow = "Checkpoint-Workflow";
        private const string SuspendWorkflow = "Suspend-Workflow";

        private int namespaceCount = 0;
        private Dictionary<string, string> namespaces = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private List<string> namespaceDefinitions = new List<string>();
        private List<string> bodyElements = new List<string>();
        private Stack<VariableScope> scopes = new Stack<VariableScope>();
        private Stack<StorageVariable> resultVariables = new Stack<StorageVariable>();
        private Dictionary<string, VariableDefinition> members = new Dictionary<string, VariableDefinition>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> memberDefaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private bool isVisitingPipeline = false;
        private bool isVisitingIterativePipeline = false;

        /// <summary>
        /// Converts a PowerShell AST into the XAML that represents it, also returning the cmdlet attribute string
        /// for the workflow.
        /// </summary>
        /// <param name="ast">The PowerShell AST to convert</param>
        /// <param name="definingModule">The module that is defining this command (if any)</param>
        /// <param name="referencedAssemblies">The list of additional assemblies to search for workflow activities.</param>
        /// <param name="parameterValidation">Any parameter validation applied to the parameters in the provided AST.</param>
        /// <param name="nestedWorkflows">Any nested workflows required by this PowerShell AST.</param>
        /// <param name="requiredAssemblies">All assemblies, including provided at API or proiveded in workfow definition, required by this PowerShell Workflow.</param>
        /// <param name="workflowAttributes">The attribute string for the workflow if these is one.</param>
        public static string Convert(FunctionDefinitionAst ast,
                                     PSModuleInfo definingModule,
                                     List<string> referencedAssemblies,
                                     out Dictionary<string, ParameterAst> parameterValidation,
                                     out WorkflowInfo[] nestedWorkflows,
                                     out Dictionary<string, string> requiredAssemblies,
                                     out string workflowAttributes)
        {
            var scope = new Scope
                {
                    functionDefinitions = new Dictionary<string, Scope.Entry>(StringComparer.OrdinalIgnoreCase)
                };

            HashSet<string> processedActivityLibraries;
            var activityMap = AstToXamlConverter.GetActivityMap(referencedAssemblies, out processedActivityLibraries);

            bool useCurrentRunspace = Runspace.CanUseDefaultRunspace;
            var invoker = System.Management.Automation.PowerShell.Create(useCurrentRunspace
                                                                   ? RunspaceMode.CurrentRunspace
                                                                   : RunspaceMode.NewRunspace);

            try
            {
                return Convert(ast, scope, definingModule, activityMap, processedActivityLibraries, out parameterValidation, out nestedWorkflows,
                               out requiredAssemblies, out workflowAttributes, referencedAssemblies, invoker);
            }
            finally
            {
                if (!useCurrentRunspace)
                {
                    invoker.Dispose();
                }
            }
        }

        internal static FunctionDefinitionAst GetScriptAsFunction(string name, string definition, bool isWorkflow)
        {
            Token[] tokens;
            ParseError[] errors;
            var block = Parser.ParseInput(
                string.Format(CultureInfo.InvariantCulture, "{0} {1} {{ {2} }}",
                              isWorkflow ? "workflow" : "function", name, definition),
                out tokens, out errors);
            if (errors.Count() > 0)
            {
                throw new ParseException(errors);
            }
            return (FunctionDefinitionAst)block.EndBlock.Statements[0];            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="definition"></param>
        /// <param name="initialSessionState"></param>
        /// <returns></returns>
        public static string Convert(string name, string definition, InitialSessionState initialSessionState)
        {
            if (name == null)
            {
                throw new PSArgumentNullException("name");
            }

            if (definition == null)
            {
                throw new PSArgumentNullException("definition");
            }

            if (initialSessionState == null)
            {
                throw new PSArgumentNullException("initialSessionState");
            }

            var fnDefn = GetScriptAsFunction(name, definition, true);

            var invoker = System.Management.Automation.PowerShell.Create(initialSessionState);

            try
            {
                HashSet<string> processedActivityLibraries;
                Dictionary<string, Type> activityMap;

                var scope = AstToWorkflowConverter.GetScopeFromIss(initialSessionState, invoker, out processedActivityLibraries, out activityMap);

                Dictionary<string, ParameterAst> parameterValidation;
                WorkflowInfo[] nestedWorkflows;
                Dictionary<string, string> requiredAssemblies;
                string workflowAttributes;
                return Convert(fnDefn, scope, null, activityMap, processedActivityLibraries, out parameterValidation, out nestedWorkflows,
                               out requiredAssemblies, out workflowAttributes, null, invoker);
            }
            finally
            {
                invoker.Dispose();
            }
        }

        internal static string Convert(FunctionDefinitionAst ast,
                                       Scope scope,
                                       PSModuleInfo definingModule,
                                       Dictionary<string, Type> activityMap,
                                       HashSet<string> processedActivityLibraries,
                                       out Dictionary<string, ParameterAst> parameterValidation,
                                       out WorkflowInfo[] nestedWorkflows,
                                       out Dictionary<string, string> requiredAssemblies,
                                       out string workflowAttributes,
                                       IEnumerable<string> assemblyList,
                                       System.Management.Automation.PowerShell invoker)
        {
            AstToXamlConverter converter = new AstToXamlConverter(ast.Name, ast, definingModule, activityMap, processedActivityLibraries, false, scope, invoker);

            ast.Visit(converter);
            parameterValidation = converter.ParameterValidation;
            nestedWorkflows = converter.NestedWorkflows.ToArray();

            requiredAssemblies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (assemblyList != null)
            {

                foreach (string filePath in assemblyList)
                {

                    if (!string.IsNullOrEmpty(filePath))
                    {
                        string fileName;

                        // To avoid situation like "System.Management.Automation -> System.Management"
                        if (string.Equals(Path.GetExtension(filePath),".dll",StringComparison.OrdinalIgnoreCase))
                        {
                            fileName = Path.GetFileNameWithoutExtension(filePath);
                        }
                        else
                        {
                            fileName = filePath;
                        }

                        requiredAssemblies.Add(fileName, filePath);
                    }
                }
            }

            // Pop the attribute off of the stack
            workflowAttributes = converter.CmdletAttributeText;

            string result = converter.ToString().Trim();
            return result;
        }

        /// <summary>
        /// Validates a PowerShell AST as a valid workflow.
        /// </summary>
        /// <param name="ast">The PowerShell AST to convert</param>
        public static List<ParseError> Validate(FunctionDefinitionAst ast)
        {
            var scope = new Scope
            {
                functionDefinitions = new Dictionary<string, Scope.Entry>(StringComparer.OrdinalIgnoreCase)
            };

            bool useCurrentRunspace = Runspace.CanUseDefaultRunspace;
            var invoker = System.Management.Automation.PowerShell.Create(useCurrentRunspace
                                                                   ? RunspaceMode.CurrentRunspace
                                                                   : RunspaceMode.NewRunspace);

            try
            {
                // Guard access to private static variables.  IEnumerable use is not thread safe.
                lock (staticProcessedActivityLibraries)
                {
                    var converter = new AstToXamlConverter(null, ast, null, staticActivityMap, staticProcessedActivityLibraries, true, scope, invoker);

                    try
                    {
                        ast.Visit(converter);
                    }
                    catch (Exception)
                    {
                        // If we are reporting a parse error, catch all exceptions during validation
                        // as we probably tried to continue past a parse error. True code issues will
                        // be caught during final compilation.
                        if (converter.ParseErrors.Count == 0)
                        {
                            throw;
                        }
                    }

                    return converter.ParseErrors;
                }
            }
            finally
            {
                if (!useCurrentRunspace)
                {
                    invoker.Dispose();
                }
            }
        }

        /// <summary>
        /// Returns the XAML result of the AST compilation
        /// </summary>
        /// <returns>The XAML result of the AST compilation</returns>
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            // Add in the initial header
            string actualActivityName = string.Format(CultureInfo.InvariantCulture, "Activity_{0}", Math.Abs(scriptWorkflowAstRoot.ToString().GetHashCode()));
            string formattedXamlHeader = String.Format(
                CultureInfo.InvariantCulture,
                xamlHeader,
                actualActivityName);
            result.AppendLine(formattedXamlHeader);

            if (hasControlFlowException)
            {
                // Add exception namespace to list so we can use the friendly name
                GetFriendlyName(null, typeof(Microsoft.PowerShell.Workflow.WorkflowReturnException));
            }

            // Add in the namespaces
            foreach (string namespaceDeclaration in namespaceDefinitions)
            {
                result.AppendLine(namespaceDeclaration);
            }

            // Add in the defaults
            foreach (string memberName in memberDefaults.Keys)
            {
                result.AppendLine(String.Format(
                    CultureInfo.InvariantCulture, "    local:{0}.{1} = \"{2}\"",
                    actualActivityName,
                    memberName,
                    memberDefaults[memberName]));
            }

            // Close the Activity tag
            result.AppendLine("    >");

            IndentLevel();

            // Add the members
            if (members.Count > 0)
            {
                result.AppendLine(GetIndentedLine("<x:Members>"));

                IndentLevel();
                foreach (VariableDefinition member in members.Values)
                {
                    result.AppendLine(GetIndentedLine(member.XamlDefinition));
                }
                UnindentLevel();

                result.AppendLine(GetIndentedLine("</x:Members>"));
            }

            // Add the wrapping try / catch to support return and exit
            if (hasControlFlowException)
            {
                result.AppendLine(GetIndentedLine("<TryCatch>"));
                IndentLevel();
                result.AppendLine(GetIndentedLine("<TryCatch.Try>"));
                IndentLevel();
            }

            // Add the body elements: <Sequence> ... </Sequence>
            foreach (string element in bodyElements)
            {
                result.AppendLine(GetIndentedLine(element));
            }

            // Close up the wrapping try / catch to support return and exit
            if (hasControlFlowException)
            {
                string friendlyTypeName = GetConvertedTypeName(typeof(Microsoft.PowerShell.Workflow.WorkflowReturnException));
                UnindentLevel();
                result.AppendLine(GetIndentedLine("</TryCatch.Try>"));
                result.AppendLine(GetIndentedLine("<TryCatch.Catches>"));
                IndentLevel();
                result.AppendLine(GetIndentedLine(@"<Catch x:TypeArguments=""" + friendlyTypeName + @""">"));
                IndentLevel();
                result.AppendLine(GetIndentedLine(@"<ActivityAction x:TypeArguments=""" + friendlyTypeName + @""">"));
                IndentLevel();
                result.AppendLine(GetIndentedLine(@"<ActivityAction.Argument>"));
                IndentLevel();
                result.AppendLine(GetIndentedLine(@"<DelegateInArgument x:TypeArguments=""" + friendlyTypeName + @""" Name=""_"" />"));
                UnindentLevel();
                result.AppendLine(GetIndentedLine(@"</ActivityAction.Argument>"));
                UnindentLevel();
                result.AppendLine(GetIndentedLine(@"</ActivityAction>"));
                UnindentLevel();
                result.AppendLine(GetIndentedLine(@"</Catch>"));

                UnindentLevel();
                result.AppendLine(GetIndentedLine("</TryCatch.Catches>"));
                UnindentLevel();
                result.AppendLine(GetIndentedLine("</TryCatch>"));
            }

            UnindentLevel();
            result.AppendLine(GetIndentedLine(xamlFooter));

            return result.ToString();
        }

        /// <summary>
        /// Set to True if workflow conversion should be done in validation mode.
        /// </summary>
        internal bool ValidateOnly
        {
            get { return validateOnly; }
        }
        private bool validateOnly = false;

        /// <summary>
        /// Returns the list of errors found during validation / compilation
        /// </summary>
        internal List<ParseError> ParseErrors
        {
            get { return _parseErrors; }
        }
        List<ParseError> _parseErrors = new List<ParseError>();

        /// <summary>
        /// Returns all nested workflows used by this command
        /// </summary>
        internal HashSet<WorkflowInfo> NestedWorkflows
        {
            get { return nestedWorkflows; }
        }
        private HashSet<WorkflowInfo> nestedWorkflows = new HashSet<WorkflowInfo>(new WorkflowInfoComparer());

        private string GetIndentedLine(string line)
        {
            if (_currentIndentLevel > 0)
            {
                StringBuilder indentation = new StringBuilder();
                indentation.Append(' ', 4 * _currentIndentLevel);
                indentation.Append(line);

                line = indentation.ToString();
            }

            return line;
        }

        private void WriteLine(string line)
        {
            bodyElements.Add(GetIndentedLine(line));
        }

        // Convert a type name to something with namespaces shortened down to XML namespaces (i.e.: ns0:Dictionary)
        // Also adds the XML namespaces to the list of namespaces in the activity itself.
        private string GetConvertedTypeName(Type requiredType)
        {
            string convertedName = GetFriendlyName(null, requiredType);

            // Process generic arguments
            if (requiredType.IsGenericType)
            {
                convertedName += "(";

                Type[] genericArguments = requiredType.GetGenericArguments();
                string[] convertedGenericArguments = new string[genericArguments.Length];

                for (int counter = 0; counter < genericArguments.Length; counter++)
                {
                    convertedGenericArguments[counter] = GetConvertedTypeName(genericArguments[counter]);
                }

                convertedName += String.Join(", ", convertedGenericArguments);

                convertedName += ")";
            }

            return convertedName;
        }

        private string GetFriendlyName(string invocationName, Type requiredType)
        {
            // Generate an error if they're trying to use a parameter type / etc
            // from a dynamically-loaded assembly.
            if (String.IsNullOrEmpty(requiredType.Assembly.Location))
            {
                throw new NotSupportedException(
                    String.Format(CultureInfo.InvariantCulture, ActivityResources.TypeFromDynamicAssembly, requiredType.FullName));
            }

            string typeKey = requiredType.Namespace + "|" + requiredType.Assembly.FullName;

            // Add a namespace alias if required
            if (!namespaces.ContainsKey(typeKey))
            {
                string namespaceName = "ns" + namespaceCount;
                namespaces[typeKey] = namespaceName;

                namespaceDefinitions.Add(
                    String.Format(CultureInfo.InvariantCulture, @"    xmlns:ns{0}=""clr-namespace:{1};assembly={2}""",
                        namespaceCount, requiredType.Namespace, requiredType.Module.Name.Replace(".dll", "")));
                namespaceCount++;
            }

            string namespaceMapping = namespaces[typeKey];

            if (invocationName == null)
            {
                invocationName = requiredType.Name;
            }

            string friendlyName = GetNonGenericName(invocationName);

            if (typeof(DynamicActivity).IsAssignableFrom(requiredType))
            {
                friendlyName = friendlyName.Replace("Microsoft.PowerShell.DynamicActivities.", "");
                namespaceMapping = "local";
            }

            return String.Format(CultureInfo.InvariantCulture, "{0}:{1}", namespaceMapping, friendlyName);
        }

        private string GetNonGenericName(string genericName)
        {
            int genericIndex = genericName.IndexOf('`');
            if (genericIndex >= 0)
            {
                genericName = genericName.Substring(0, genericIndex);
            }
            return genericName;
        }

        private void IndentLevel()
        {
            ++_currentIndentLevel;
        }

        private void UnindentLevel()
        {
            --_currentIndentLevel;
            if (_currentIndentLevel < 0)
            {
                throw new InvalidOperationException();
            }
        }

        object ICustomAstVisitor.VisitErrorStatement(ErrorStatementAst errorStatementAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitErrorExpression(ErrorExpressionAst errorExpressionAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitScriptBlock(ScriptBlockAst scriptBlockAst)
        {
            ValidateScriptBlock(scriptBlockAst);

            // We've already processed a sequence
            if (bodyElements.Count != 0)
            {
                ReportError("CannotProcessMoreThanOneScriptBlock", ActivityResources.CannotProcessMoreThanOneScriptBlock, scriptBlockAst.Extent);
            }

            WriteLine("<Sequence>");
            IndentLevel();
            EnterScope();

            try
            {
                if (scriptBlockAst.ParamBlock != null)
                {
                    scriptBlockAst.ParamBlock.Visit(this);
                }

                // Initialize the 'result' reference parameter with empty collection, without this result parameter is generating 
                // wrong results if it is first used in a += operation in Powershell value activity.
                if(members.ContainsKey("result") && typeof(PSDataCollection<PSObject>).IsAssignableFrom(members["result"].Type))
                {
                    GeneratePowerShellValue(typeof(PSDataCollection<PSObject>), "@()", false, "result");
                }

                if (scriptBlockAst.EndBlock != null)
                {
                    scriptBlockAst.EndBlock.Visit(this);
                }
            }
            finally
            {
                DumpVariables("Sequence");
                LeaveScope();

                UnindentLevel();
                WriteLine("</Sequence>");
            }

            return null;
        }

        private void ValidateScriptBlock(ScriptBlockAst scriptBlockAst)
        {
            if (scriptBlockAst.DynamicParamBlock != null)
            {
                ReportError("DynamicParametersNotSupported", ActivityResources.DynamicParametersNotSupported, scriptBlockAst.DynamicParamBlock.Extent);
            }
            if (scriptBlockAst.BeginBlock != null)
            {
                ReportError("BeginNotSupported", ActivityResources.BeginProcessNotSupported, scriptBlockAst.BeginBlock.Extent);
            }
            if (scriptBlockAst.ProcessBlock != null)
            {
                ReportError("ProcessNotSupported", ActivityResources.BeginProcessNotSupported, scriptBlockAst.ProcessBlock.Extent);
            }
        }

        object ICustomAstVisitor.VisitTypeConstraint(TypeConstraintAst typeConstraintAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitAttribute(AttributeAst attributeAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitParameter(ParameterAst parameterAst)
        {
            Type parameterType = parameterAst.StaticType;
            string parameterName = parameterAst.Name.VariablePath.ToString();
            string actualParameterName;
            if (!IsValidMemberName(parameterName, out actualParameterName, true))
            {
                string error = String.Format(CultureInfo.InvariantCulture, ActivityResources.InvalidMemberName, parameterName);
                ReportError("MemberNameNotValid", error, parameterAst.Extent);
            }

            // If we've already seen the parameter, it's an error, but that error should have been reported by the parser.
            if (members.ContainsKey(parameterName))
            {
                return null;
            }

            // Store any parameter validation
            if ((parameterAst.Attributes.Count > 0) ||
                (parameterAst.DefaultValue != null))
            {
                if(! parameterValidation.ContainsKey(parameterName))
                {
                    parameterValidation[parameterName] = parameterAst;
                }
            }

            // If the parameter is not typed and they've given a default
            // value, then use the static type from the default.
            if (parameterType == typeof(System.Object))
            {
                if (parameterAst.DefaultValue != null)
                {
                    bool constrainedToObject = false;

                    // Check if it was constrained that way
                    if (parameterAst.Attributes.Count > 0)
                    {
                        foreach (AttributeBaseAst attribute in parameterAst.Attributes)
                        {
                            if (attribute is TypeConstraintAst)
                            {
                                constrainedToObject = true;
                                break;
                            }
                        }
                    }

                    if (!constrainedToObject)
                    {
                        parameterType = parameterAst.DefaultValue.StaticType;
                    }
                }
            }

            string memberTemplate = @"<x:Property Name=""{0}"" Type=""InArgument({1})"" />";

            // If it's a reference type, we need to generate an OutArgument
            if (parameterType == typeof(System.Management.Automation.PSReference))
            {
                // Unfortunately, you can't type constrain these. Determine the
                // type through flow analysis. (Good version TBD!)
                parameterType = DetectType(parameterName, true, scriptWorkflowAstRoot);
                memberTemplate = @"<x:Property Name=""{0}"" Type=""OutArgument({1})"" />";
            }

            // If they've specified a default value, save that so we can add it to the
            // workflow.
            if (parameterAst.DefaultValue != null)
            {
                bool areParameterAndDefaultCompatible = 
                    (parameterAst.StaticType == typeof(object)) ||
                    (parameterAst.StaticType == typeof(bool)) ||
                    (parameterAst.StaticType == parameterAst.DefaultValue.StaticType);

                bool isSupportedDefaultType =
                    areParameterAndDefaultCompatible &&
                    (
                        parameterAst.DefaultValue.StaticType.IsPrimitive ||
                        (parameterAst.DefaultValue.StaticType == typeof(string)) ||
                        parameterAst.StaticType == typeof(bool)
                    );

                if (!isSupportedDefaultType)
                {
                    ReportError("OnlySimpleParameterDefaultsSupported", ActivityResources.OnlySimpleParameterDefaultsSupported, parameterAst.DefaultValue.Extent);
                    return null;
                }

                ConstantExpressionAst parameterValue = parameterAst.DefaultValue as ConstantExpressionAst;

                if (parameterValue != null)
                {
                    this.memberDefaults[parameterName] = EncodeStringArgument(parameterValue.Value.ToString(), false);
                }
                else
                {
                    string valueText = parameterAst.DefaultValue.Extent.Text;

                    // Do some hand tweaking for booleans, which actually come as variable expressions
                    valueText = GetEquivalentVBTextForLiteralValue(parameterAst.StaticType, valueText);
                    if (valueText == null)
                    {
                        ReportError("OnlySimpleParameterDefaultsSupported", ActivityResources.OnlySimpleParameterDefaultsSupported, parameterAst.DefaultValue.Extent);
                        return null;
                    }

                    this.memberDefaults[parameterName] = EncodeStringArgument(valueText, false);
                }
            }

            string xamlDefinition = String.Format(CultureInfo.InvariantCulture, memberTemplate, parameterName, GetConvertedTypeName(parameterType));
            VariableDefinition member = new VariableDefinition() { Name = parameterName, Type = parameterType, XamlDefinition = xamlDefinition };
            members.Add(parameterName, member);

            return null;
        }

        private string GetEquivalentVBTextForLiteralValue(Type argumentType, string valueText)
        {
            string result = null;

            if (argumentType == typeof(bool))
            {
                if (String.Equals(valueText, "$true", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(valueText, "$false", StringComparison.OrdinalIgnoreCase))
                {
                    result = valueText.Substring(1);
                }
            }

            return result;
        }

        private bool IsValidMemberName(string name, out string actualVariableName, bool isParameter)
        {
            actualVariableName = name;
            if (name == null) { return false; }
            
            if (!isParameter)
            {
                // Allow the "WORKFLOW:" scope qualifier in nested scopes
                if (name.IndexOf(':') >= 0)
                {
                    if ((name.StartsWith("WORKFLOW:", StringComparison.OrdinalIgnoreCase)) &&
                        (scopes.Count > 1))
                    {
                        name = name.Remove(0, "WORKFLOW:".Length);
                        actualVariableName = name;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            // Alphabetic to start, alphabetic plus numbers, dash, and underscore for the rest.
            return Regex.IsMatch(name, "^[a-zA-Z][a-zA-Z0-9-_]*$");
        }

        private Type DetectType(string parameterName, bool isReference, Ast scriptRoot)
        {
            // Currently only does enough to detect simple derivation of [ref] types:
            //    $Variable.Value = Expression
            Func<Ast, bool> assignmentSearcher = (ast) =>
            {
                AssignmentStatementAst assignment = ast as AssignmentStatementAst;
                UnaryExpressionAst unaryExpression = ast as UnaryExpressionAst;

                if ((assignment == null) && (unaryExpression == null))
                {
                    return false;
                }

                // Check if this is a unary assignment
                if (unaryExpression != null)
                {
                    VariableExpressionAst referenceVariable = unaryExpression.Child as VariableExpressionAst;
                    if (referenceVariable != null && String.Equals(referenceVariable.VariablePath.UserPath, parameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                // Check if this is a regular assignment
                if (assignment == null) { return false; }

                if (isReference)
                {
                    MemberExpressionAst member = assignment.Left as MemberExpressionAst;
                    if (member == null) { return false; }

                    VariableExpressionAst variable = member.Expression as VariableExpressionAst;
                    if (variable == null) { return false; }

                    // See if it's the variable we're looking for
                    // Variable.Value = <Expression>
                    if (
                        (String.Equals(variable.VariablePath.ToString(), parameterName, StringComparison.OrdinalIgnoreCase)) &&
                        (String.Equals(member.Member.ToString(), "Value", StringComparison.OrdinalIgnoreCase))
                        )
                    {
                        CommandExpressionAst value = assignment.Right as CommandExpressionAst;
                        if (value == null) { return false; }

                        return true;
                    }
                }
                else
                {
                    // Capture $x = 10
                    VariableExpressionAst variableExpression = assignment.Left as VariableExpressionAst;
                    string detectedVariableName = null;

                    // Don't count PlusEquals for type detection, as we enforce that during assignment
                    // itself
                    if (assignment.Operator == TokenKind.PlusEquals)
                    {
                        return false;
                    }

                    if (variableExpression == null)
                    {
                        // Capture [int] $x = 10
                        ConvertExpressionAst convertExpression = assignment.Left as ConvertExpressionAst;
                        if (convertExpression != null)
                        {
                            variableExpression = convertExpression.Child as VariableExpressionAst;
                        }
                    }

                    if (variableExpression != null)
                    {
                        detectedVariableName = variableExpression.VariablePath.UserPath;
                    }
                    else
                    {
                        return false;
                    }

                    // See if it's the variable we're looking for
                    // Variable = <Expression>
                    string workingVariableName = detectedVariableName;
                    
                    // Allow the "WORKFLOW:" scope qualifier in nested scopes
                    if (workingVariableName.StartsWith("WORKFLOW:", StringComparison.OrdinalIgnoreCase))
                    {
                        workingVariableName = workingVariableName.Remove(0, "WORKFLOW:".Length);
                    }

                    if (String.Equals(workingVariableName, parameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Ignore assignments to parallel script blocks (Variable = [parallel()] { ... })
                        var value = assignment.Right as CommandExpressionAst;
                        if (value != null)
                        {
                            return true;
                        }

                        // Ignore statements like foreach -parallel, parallel and sequence block
                        return !(assignment.Right is ForEachStatementAst) && !(assignment.Right is BlockStatementAst);
                    }
                }

                return false;
            };

            List<Ast> resultNodes = scriptRoot.FindAll(assignmentSearcher, searchNestedScriptBlocks: true).ToList();

            // We couldn't detect the type. Assume PSDataCollection<PSObject>)
            if (resultNodes.Count == 0)
            {
                return typeof(PSDataCollection<PSObject>);
            }

            HashSet<Type> detectedTypes = new HashSet<Type>();
            foreach (Ast result in resultNodes)
            {
                AssignmentStatementAst assignmentStatement = result as AssignmentStatementAst;
                if (assignmentStatement == null)
                {
                    continue;
                }

                ConvertExpressionAst convertExpression = assignmentStatement.Left as ConvertExpressionAst;
                if (convertExpression != null)
                {
                    Type detectedType = convertExpression.StaticType;
                    detectedTypes.Add(detectedType);
                    continue;
                }

                PipelineAst invocationExpression = assignmentStatement.Right as PipelineAst;
                if (invocationExpression != null)
                {
                    if (invocationExpression.PipelineElements.Count == 1 && invocationExpression.PipelineElements[0] is CommandAst)
                    {
                        var commandAst = (CommandAst)invocationExpression.PipelineElements[0];
                        string commandName = commandAst.GetCommandName();
                        CommandInfo command;

                        bool searchSessionState = !this.ValidateOnly;
                        ActivityKind activityKind = ResolveActivityKindBasedOnCommandName(commandName, commandAst, out command, searchSessionState);

                        if (activityKind == ActivityKind.NewObject)
                        {
                            Dictionary<string, CommandArgumentInfo> parameters = GetAndResolveParameters(commandAst, true);

                            if (parameters.ContainsKey("TypeName"))
                            {
                                string paramValue = parameters["TypeName"].OriginalValue.ToString();
                                Type actualResultType = ResolveTypeFromParameterValue(commandAst, paramValue);

                                if (actualResultType != null)
                                {
                                    detectedTypes.Add(actualResultType);
                                }
                                continue;
                            }
                        }
                        else if (
                            // For generic activities, the CommandInfo should be resolved to null
                            activityKind == ActivityKind.RegularCommand && command == null)
                        {
                            // Skip this expensive step if we are in parse mode
                            if (!this.ValidateOnly)
                            {
                                CommandInfo unusedCommandInfo;
                                Type[] genericTypes;
                                Type activityType = ResolveActivityType(commandName, commandAst, false, out unusedCommandInfo, out genericTypes);

                                // By convention, the 'TResult' must be the last item in 'genericArgumentTypes'
                                Type[] genericArgumentTypes = null;
                                if (activityType != null && activityType.IsGenericType && genericTypes != null &&
                                    IsAssignableFromGenericType(typeof(Activity<>), activityType, out genericArgumentTypes))
                                {
                                    var genericTypeMap = GetGenericTypeMap(activityType, genericTypes);
                                    var actualResultType = GetActualPropertyType(genericArgumentTypes[genericArgumentTypes.Length - 1], genericTypeMap, "Result", commandAst.Extent);
                                    detectedTypes.Add(actualResultType);
                                    continue;
                                }
                            }
                        }
                    }

                    detectedTypes.Add(typeof(PSDataCollection<PSObject>));
                }
            }

            // We detected an unambiguous type
            if (detectedTypes.Count == 1)
            {
                return detectedTypes.ElementAt<Type>(0);
            }
            else if (detectedTypes.Contains(typeof(PSDataCollection<PSObject>)))
            {
                // When we see that a variable is storing the result of an activity call, 
                // we can't have the detected type be an Object, as Workflow doesn't allow that.
                // So we just return PSDataCollection<PSObject> in that case.
                return typeof(PSDataCollection<PSObject>);
            }
            else
            {
                // It was ambiguous, or of several types
                return typeof(Object);
            }
        }

        internal ActivityKind ResolveActivityKindBasedOnCommandName(string commandName, CommandAst commandAst, out CommandInfo command, bool searchSessionState)
        {
            command = null;

            if (String.IsNullOrEmpty(commandName))
            {
                return ActivityKind.RegularCommand;
            }

            // Check if this is InlineScript activity
            if (String.Equals(commandName, "InlineScript", StringComparison.OrdinalIgnoreCase))
            {
                return ActivityKind.InlineScript;
            }
            // Check if this is a persist activity
            else if (
                String.Equals(commandName, CheckpointWorkflow, StringComparison.OrdinalIgnoreCase) ||
                String.Equals(commandName, "persist", StringComparison.OrdinalIgnoreCase)
                )
            {
                return ActivityKind.Persist;
            }
            // Check if this is a suspend activity
            else if (String.Equals(commandName, SuspendWorkflow, StringComparison.OrdinalIgnoreCase))
            {
                return ActivityKind.Suspend;
            }
            // Check if this is inline XAML
            else if (
                String.Equals(commandName, "Invoke-Expression", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(commandName, "iex", StringComparison.OrdinalIgnoreCase)
                )
            {
                StaticBindingResult bindingResult = null;
                if (commandAst != null)
                {
                    bindingResult = StaticParameterBinder.BindCommand(commandAst, false, new string[] { "Language" });
                }

                // If they've specified the "-Language" parameter, invoke the
                // built-in Invoke-Expression support for inline XAML
                if ((bindingResult != null) && (bindingResult.BoundParameters.ContainsKey("Language")))
                {
                    return ActivityKind.InvokeExpression;
                }
                else
                {
                    if (searchSessionState)
                    {
                        command = ResolveCommand(commandName);
                    }

                    // Otherwise, use the Invoke-Expression activity
                    return ActivityKind.RegularCommand;
                }
            }
            // Check if this is a delay activity
            else if (
                String.Equals(commandName, "Start-Sleep", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(commandName, "sleep", StringComparison.OrdinalIgnoreCase)
                )
            {
                return ActivityKind.Delay;
            }
            // Check if this is a New-Object activity
            else if (String.Equals(commandName, "New-Object", StringComparison.OrdinalIgnoreCase))
            {
                if (searchSessionState)
                {
                    command = ResolveCommand(commandName);
                }

                return ActivityKind.NewObject;
            }
            // This is another command name
            else
            {
                var entry = scope.LookupCommand(commandName);
                if (entry != null)
                {
                    command = entry.workflowInfo;
                }

                if (searchSessionState && command == null)
                {
                    command = ResolveCommand(commandName);
                }

                return ActivityKind.RegularCommand;
            }
        }

        /// <summary>
        /// Block variable scope prefix like "$GLOBAL:" and "$SCRIPT:". In script workflow,
        /// the only valid scope prefix is "$WORKFLOW:". When generating expression for the
        /// PowerShellValue activity, we need to remove the $WORKFLOW part. Otherwise it will
        /// generate error during execution, becuase the prefix "WORKFLOW" is not actually 
        /// supported in the PowerShell.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private string GetPowerShellValueExpression(Ast expression)
        {
            Func<Ast, bool> variableSearcher = (ast) =>
            {
                var variableExpr = ast as VariableExpressionAst;
                if (variableExpr == null)
                {
                    return false;
                }
                
                string variableName = variableExpr.VariablePath.ToString();
                if (variableName.IndexOf(':') != -1 && !variableName.StartsWith("ENV:", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                return false;
            };

            List<Ast> resultNodes = expression.FindAll(variableSearcher, searchNestedScriptBlocks: true).ToList();
            if (resultNodes.Count == 0)
            {
                return expression.Extent.Text;
            }

            string valueExpression = expression.Extent.ToString();
            foreach (Ast node in resultNodes)
            {
                var variableNode = node as VariableExpressionAst;
                if (variableNode == null)
                {
                    continue;
                }

                string variableName = variableNode.VariablePath.ToString();
                string variableSign = variableNode.Splatted ? "@" : "$";
                if (variableName.IndexOf(':') != -1)
                {
                    if (!variableName.StartsWith("WORKFLOW:", StringComparison.OrdinalIgnoreCase))
                    {
                        ReportError("InvalidScopePrefixInWorkflow", ActivityResources.InvalidScopePrefixInWorkflow, variableNode.Extent);
                    }
                    else
                    {
                        string actualVariableName = variableName.Remove(0, "WORKFLOW:".Length);
                        string oldValue = variableSign + variableName;
                        string newValue = variableSign + actualVariableName;
                        valueExpression = valueExpression.Replace(oldValue, newValue);
                    }
                }
            }

            return valueExpression;
        }

        object ICustomAstVisitor.VisitTypeExpression(TypeExpressionAst typeExpressionAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst)
        {
            // Process top-level workflow to generate the callable function, nested functions must be handled by the caller.
            if (functionDefinitionAst != scriptWorkflowAstRoot) return null;

            // Generate the parameters applied to the function itself
            if (functionDefinitionAst.Parameters != null)
            {
                foreach (ParameterAst parameter in functionDefinitionAst.Parameters)
                {
                    parameter.Visit(this);
                }
            }

            // Extract the cmdlet binding attribute and save it to add to the generated function text...
            bool foundCmdletBinding = false;
            if (functionDefinitionAst.Body.ParamBlock != null && functionDefinitionAst.Body.ParamBlock.Attributes != null)
            {
                foreach (var attribute in functionDefinitionAst.Body.ParamBlock.Attributes)
                {
                    if (typeof(CmdletBindingAttribute) == attribute.TypeName.GetReflectionAttributeType())
                    {
                        bool error = false;

                        if (attribute.PositionalArguments.Count != 0)
                        {
                            ReportError("InvalidCmdletBindingAttribute", ActivityResources.InvalidCmdletBindingAttribute, functionDefinitionAst.Extent);
                            error = true;
                        }
                        else
                        {
                            foreach(NamedAttributeArgumentAst namedArg in attribute.NamedArguments)
                            {
                                if(!namedArg.ArgumentName.Equals("DefaultParameterSetName", StringComparison.OrdinalIgnoreCase) &&
                                   !namedArg.ArgumentName.Equals("ConfirmImpact", StringComparison.OrdinalIgnoreCase) &&
                                   !namedArg.ArgumentName.Equals("HelpUri", StringComparison.OrdinalIgnoreCase) &&
                                   !namedArg.ArgumentName.Equals("PositionalBinding", StringComparison.OrdinalIgnoreCase))
                                {
                                    ReportError("InvalidCmdletBindingAttribute", ActivityResources.InvalidCmdletBindingAttribute, functionDefinitionAst.Extent);
                                    error = true;
                                }
                            }
                        }

                        if (!error)
                        {
                            foundCmdletBinding = true;

                            if (String.IsNullOrEmpty(this.CmdletAttributeText))
                            {
                                this.CmdletAttributeText = attribute.ToString();
                            }
                            else
                            {
                                this.CmdletAttributeText += "\r\n" + attribute.ToString();
                            }
                        }
                    }

                    if ((typeof(OutputTypeAttribute) == attribute.TypeName.GetReflectionAttributeType()) ||
                        (typeof(AliasAttribute) == attribute.TypeName.GetReflectionAttributeType()))
                    {
                        if (String.IsNullOrEmpty(this.CmdletAttributeText))
                        {
                            this.CmdletAttributeText = attribute.ToString();
                        }
                        else
                        {
                            this.CmdletAttributeText += "\r\n" + attribute.ToString();
                        }
                    }
                }
            }

            if (!foundCmdletBinding)
            {
                if (String.IsNullOrEmpty(this.CmdletAttributeText))
                {
                    this.CmdletAttributeText = "[CmdletBinding()]";
                }
                else
                {
                    this.CmdletAttributeText += "\r\n[CmdletBinding()]";
                }
            }

            functionDefinitionAst.Body.Visit(this);

            return null;
        }

        /// <summary>
        /// Used to hold the CmdletBinding attribute string specified in the script workflow text.
        /// This needs to be propigated to the synthesized driver function .
        /// </summary>
        internal string CmdletAttributeText { get; set; }
        
        object ICustomAstVisitor.VisitParamBlock(ParamBlockAst paramBlockAst)
        {
            foreach (ParameterAst parameter in paramBlockAst.Parameters)
            {
                parameter.Visit(this);
            }

            return null;
        }

        object ICustomAstVisitor.VisitNamedBlock(NamedBlockAst namedBlockAst)
        {
            if (namedBlockAst.BlockKind != TokenKind.End)
            {
                ReportError("BeginProcessNotSupported", ActivityResources.BeginProcessNotSupported, namedBlockAst.Extent);
            }

            if (namedBlockAst.Traps != null)
            {
                foreach(TrapStatementAst ast in namedBlockAst.Traps)
                {
                    ast.Visit(this);
                }
            }

            DefineVariable("WorkflowCommandName", typeof(string), namedBlockAst.Extent, this.name);

            if (namedBlockAst.Statements != null)
            {
                foreach (StatementAst ast in namedBlockAst.Statements)
                {
                    ast.Visit(this);
                }
            }

            return null;
        }

        object ICustomAstVisitor.VisitStatementBlock(StatementBlockAst statementBlockAst)
        {
            // If the statement block is the body of a parallel block statement, we enclose each 
            // statement in a Try/Catch block, so that: 
            //   1. the activities generated for each statement will be executed sequentially.
            //   2. the terminating exception thrown from one statement will not terminate other statements that
            //      are running in parallel.
            var blockStatement = statementBlockAst.Parent as BlockStatementAst;
            bool needToEncloseStatementInTryCatchBlock = blockStatement != null
                && blockStatement.Kind.Text.Equals(TokenKind.Parallel.Text(), StringComparison.OrdinalIgnoreCase);

            foreach (StatementAst statement in statementBlockAst.Statements)
            {
                try
                {
                    if (needToEncloseStatementInTryCatchBlock)
                    {
                        // Start to add the Try/Catch block
                        AddTryCatchForParallelStart();

                        WriteLine("<Sequence>");
                        IndentLevel();
                    }
                    statement.Visit(this);
                }
                finally
                {
                    if (needToEncloseStatementInTryCatchBlock)
                    {
                        UnindentLevel();
                        WriteLine("</Sequence>");

                        // Finish the Try/Catch block
                        AddTryCatchForParallelEnd();
                    }
                }
            }

            return null;
        }

        object ICustomAstVisitor.VisitIfStatement(IfStatementAst ifStmtAst)
        {
            Collection<Tuple<PipelineBaseAst, StatementBlockAst>> ifClauses = new Collection<Tuple<PipelineBaseAst, StatementBlockAst>>();
            foreach (Tuple<PipelineBaseAst, StatementBlockAst> ifClause in ifStmtAst.Clauses)
            {
                ifClauses.Add(ifClause);
            }
            GenerateIf(ifClauses, ifStmtAst.ElseClause);

            return null;
        }

        private void GenerateIf(Collection<Tuple<PipelineBaseAst, StatementBlockAst>> ifClauses, StatementBlockAst elseClause)
        {
            Tuple<PipelineBaseAst, StatementBlockAst> ifClause = ifClauses[0];
            ifClauses.RemoveAt(0);

            // Generate a temporary variable for the if condition
            string tempVarName = GenerateUniqueVariableName("IfCondition");
            Type conditionType = DetectType(tempVarName, false, ifClause.Item1);
            DefineVariable(tempVarName, conditionType, ifClause.Item1.Extent, null);

            // Generate the assignment of the the clause to the temporary variable
            string conditionExpression = GetPowerShellValueExpression(ifClause.Item1);
            GenerateAssignment(tempVarName, ifClause.Item1.Extent, TokenKind.Equals, ifClause.Item1, conditionExpression);

            // Note that symbols are generated from the above GenerateAssignment call.
            WriteLine("<If>");
            IndentLevel();

            // Generate the "If"
            WriteLine("<If.Condition>");
            IndentLevel();

            // Convert the results of the condition to a boolean via PowerShellValue
            string boolFriendlyName = GetConvertedTypeName(typeof(bool));
            WriteLine(@"<InArgument x:TypeArguments=""" + boolFriendlyName + @""">");
            IndentLevel();

            GeneratePowerShellValue(typeof(bool), "$" + tempVarName, false, false);

            UnindentLevel();
            WriteLine("</InArgument>");

            UnindentLevel();
            WriteLine("</If.Condition>");

            // Generate the "Then"
            WriteLine("<If.Then>");
            IndentLevel();
            WriteLine("<Sequence>");
            IndentLevel();

            ifClause.Item2.Visit(this);

            UnindentLevel();
            WriteLine("</Sequence>");
            UnindentLevel();
            WriteLine("</If.Then>");

            // Generate the "Else"
            if ((ifClauses.Count > 0) || (elseClause != null))
            {
                WriteLine("<If.Else>");
                IndentLevel();
                
                WriteLine("<Sequence>");
                IndentLevel();

                // If we had an "ElseIf", then it's an "If" statement nested in an Else statement
                if (ifClauses.Count > 0)
                {
                    GenerateIf(ifClauses, elseClause);
                }
                else
                {
                    if (elseClause != null)
                    {
                        GenerateSymbolicInformation(elseClause.Extent);
                        elseClause.Visit(this);
                    }
                }

                UnindentLevel();
                WriteLine("</Sequence>");
                UnindentLevel();
                WriteLine("</If.Else>");
            }

            UnindentLevel();
            WriteLine("</If>");
        }

        object ICustomAstVisitor.VisitTrap(TrapStatementAst trapStatementAst)
        {
            ReportError("TrapNotSupported", ActivityResources.TrapNotSupported, trapStatementAst.Extent);
            return null;
        }

        object ICustomAstVisitor.VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst)
        {
            StatementAst right = null;
            string variableName = null;
            VariableExpressionAst leftExpressionAst = null;

            AttributedExpressionAst leftAttributeAst = assignmentStatementAst.Left as AttributedExpressionAst;
            if (leftAttributeAst != null)
            {
                leftExpressionAst = leftAttributeAst.Child as VariableExpressionAst;
            }
            else
            {
                leftExpressionAst = assignmentStatementAst.Left as VariableExpressionAst;
            }

            // This was neither a variable assignment, nor a strongly-typed variable
            if (leftExpressionAst == null)
            {
                ReportError("AssignmentNotSupported", ActivityResources.AssignmentNotSupported, assignmentStatementAst.Left.Extent);
                return null;
            }

            // check if the left-hand itself has any side effects
            string nameOfUnSupportedVariableFound = null;
            if (CheckIfExpressionHasUnsupportedVariableOrHasSideEffects(null, leftExpressionAst, out nameOfUnSupportedVariableFound))
            {
                if (!string.IsNullOrEmpty(nameOfUnSupportedVariableFound))
                {
                    string error = String.Format(CultureInfo.InvariantCulture,
                                                 ActivityResources.VariableNotSupportedInWorkflow,
                                                 nameOfUnSupportedVariableFound);
                    ReportError("VariableNotSupportedInWorkflow", error, leftExpressionAst.Extent);
                }
            }

            variableName = leftExpressionAst.VariablePath.ToString();
            right = assignmentStatementAst.Right;

            string expression = GetPowerShellValueExpression(assignmentStatementAst);
            GenerateAssignment(variableName, leftExpressionAst.Extent, assignmentStatementAst.Operator, right, expression);

            return null;
        }

        private void GenerateAssignment(string variableName, IScriptExtent errorExtent, TokenKind assignmentOperator, Ast value, string expression)
        {
            // Give a good error message specifically for environment variable names
            if (variableName.StartsWith("env:", StringComparison.OrdinalIgnoreCase))
            {
                ReportError("EnvironmentVariableAssignmentNotSupported", ActivityResources.EnvironmentVariableAssignmentNotSupported, errorExtent);
            }

            string actualVariableName;
            if (!IsValidMemberName(variableName, out actualVariableName, false))
            {
                // This was an error with variable scoping
                if (variableName.IndexOf(':') > 0)
                {
                    ReportError("WorkflowScopeOnlyValidInParallelOrSequenceBlock", ActivityResources.WorkflowScopeOnlyValidInParallelOrSequenceBlock, errorExtent);
                }
                else
                {
                    string error = String.Format(CultureInfo.InvariantCulture, ActivityResources.InvalidMemberName, variableName);
                    ReportError("VariableNameNotValid", error, errorExtent);
                }
            }

            // Give a good error message if they've used a reserved variable name
            if (IsReservedVariableName(actualVariableName))
            {
                Dictionary<string, Type> propsCanBeSet = GetAvailableProperties(typeof(SetPSWorkflowData), null);
                if (propsCanBeSet.ContainsKey(actualVariableName))
                {
                    string error = String.Format(CultureInfo.InvariantCulture, ActivityResources.VariableNameReserved, variableName);
                    ReportError("VariableNameReserved", error, errorExtent);
                }
                else
                {
                    string error = String.Format(CultureInfo.InvariantCulture, ActivityResources.VariableNameReadOnly, variableName);
                    ReportError("VariableNameReadOnly", error, errorExtent);
                }
            }

            // Check if the value itself contains side-effects. If so, generate an error.
            string nameOfUnSupportedVariableFound = null;
            if (CheckIfExpressionHasUnsupportedVariableOrHasSideEffects(variableName, value, out nameOfUnSupportedVariableFound))
            {
                if (string.IsNullOrEmpty(nameOfUnSupportedVariableFound))
                {
                    string errorTemplate = ActivityResources.CannotStoreResultsInUnsupportedElement;
                    ReportError("CannotStoreResultsInUnsupportedElement",
                                ActivityResources.CannotStoreResultsInUnsupportedElement, value.Extent);
                    return;
                }
                else
                {
                    string error = String.Format(CultureInfo.InvariantCulture,
                                                 ActivityResources.VariableNotSupportedInWorkflow,
                                                 nameOfUnSupportedVariableFound);
                    ReportError("VariableNotSupportedInWorkflow", error, value.Extent);
                }
            }

            Type variableType = null;
            if(members.ContainsKey(actualVariableName))
            {
                variableType = members[actualVariableName].Type;
            }

            if (variableType == null)
            {
                VariableDefinition variable = GetVariableDefinition(actualVariableName);
                if (variable != null)
                {
                    variableType = variable.Type;
                }
            }

            if (variableType == null)
            {
                variableType = DetectType(actualVariableName, false, scriptWorkflowAstRoot);
            }

            // Create the variable if it hasn't been created
            if ((!VariableDefinedInCurrentScope(variableName)) &&
                (!members.ContainsKey(actualVariableName)))
            {
                DefineVariable(variableName, variableType, errorExtent, null);
            }

            // Create the variable assignment. If this is of type PSActivity, then we can
            // set the variable as the result.
            if (LocalVariableAlreadyExisting())
            {
                string errorTemplate = ActivityResources.CannotStoreResultsInVariable;
                string error = String.Format(CultureInfo.InvariantCulture, errorTemplate, variableName, resultVariables.Peek().VariableName);

                ReportError("CannotStoreResultsInVariable", error, errorExtent);
                return;
            }

            // Should not override a data collecting variable
            if (IsDataAggregatingVariable(actualVariableName))
            {
                string errorMessage = String.Format(CultureInfo.InvariantCulture, ActivityResources.CannotUseDataCollectingVariable, actualVariableName);
                ReportError("CannotUseDataCollectingVariable", errorMessage, errorExtent);
            }

            bool isAggregatingVariable = false;
            if (assignmentOperator == TokenKind.PlusEquals)
            {
                isAggregatingVariable = true;
            }

            // Generate debug symbol information for variable assignments.
            GenerateSymbolicInformation(value.Extent);

            // Visit the right-hand side of the expression for activitities
            PipelineAst rightPipeline = value as PipelineAst;
            if (rightPipeline != null)
            {
                try
                {
                    EnterStorage(actualVariableName, isAggregatingVariable);
                    value.Visit(this);
                }
                finally
                {
                    LeaveStorage();
                }
            }
            else if (value is BlockStatementAst || value is ForEachStatementAst)
            {
                try
                {
                    EnterStorage(actualVariableName, true);
                    value.Visit(this);
                }
                finally
                {
                    LeaveStorage();
                }
            }
            else
            {
                // Support simple assignment expressions, such as hashtables, ranges, etc.
                // Specifically exclude subexpressions, as they are supported by InlineScript.
                CommandExpressionAst rightExpression = value as CommandExpressionAst;
                UnaryExpressionAst unaryExpression = value as UnaryExpressionAst;

                if ((
                    (rightExpression != null) && (!(rightExpression.Expression is SubExpressionAst)) && 
                    ((rightExpression.Expression is ConvertExpressionAst) || !(rightExpression.Expression is AttributedExpressionAst))
                    ) ||
                    (unaryExpression != null)
                    )
                {
                    try
                    {
                        EnterStorage(actualVariableName, false);

                        // We rewrite an assignment such as: "$x = $x + 1" to "$x = $x + 1; $x" so that
                        // PowerShell returns the new value after assignment. This is especially required
                        // for statements such as $x++, which normally have no output.
                        string assignmentExpression = null;

                        if (assignmentOperator != TokenKind.Equals)
                        {
                            // We rewrite an assignment such as: "$x = $x + 1" to "$x = $x + 1; $x" so that
                            // PowerShell returns the new value after assignment. This is required
                            // for statements such as $x++, which normally have no output.
                            assignmentExpression = expression + "; ,($" + actualVariableName + ")";
                        }
                        else
                        {
                            assignmentExpression = GetPowerShellValueExpression(rightExpression);
                        }

                        GeneratePowerShellValue(variableType, assignmentExpression, false, true);
                    }
                    finally
                    {
                        LeaveStorage();
                    }
                }
                else
                {
                    ReportError("CannotStoreResultsInUnsupportedElement", ActivityResources.CannotStoreResultsInUnsupportedElement, value.Extent);
                }
            }
        }

        private bool IsReservedVariableName(string variable)
        {
            PSWorkflowRuntimeVariable unused;
            return Enum.TryParse<PSWorkflowRuntimeVariable>(variable, true, out unused);
        }

        private void DefineVariable(string name, Type variableType, IScriptExtent extent, string defaultValue)
        {
            VariableScope scopeToUse = scopes.Peek();
            if (name.StartsWith("WORKFLOW:", StringComparison.OrdinalIgnoreCase))
            {
                scopeToUse = scopes.Last();
                name = name.Remove(0, "WORKFLOW:".Length);
            }

            // Check that it's not already defined
            foreach (VariableScope scope in scopes)
            {
                if (scope.Variables.ContainsKey(name))
                {
                    string errorMessage = String.Format(CultureInfo.InvariantCulture, ActivityResources.VariableAlreadyDefined, name);
                    ReportError("VariableAlreadyDefined", errorMessage, extent);
                }
            }

            string convertedTypeName = GetConvertedTypeName(variableType);

            string defaultValueTemplate = "Default = \"{0}\" ";
            string defaultValueElement = String.Empty;
            if (!String.IsNullOrEmpty(defaultValue))
            {
                defaultValueElement = String.Format(CultureInfo.InvariantCulture, defaultValueTemplate, EncodeStringArgument(defaultValue, false));
            }

            string xamlDefinition = String.Format(CultureInfo.InvariantCulture, MemberTemplate, name, convertedTypeName, defaultValueElement);
            VariableDefinition variable = new VariableDefinition() { Name = name, Type = variableType, XamlDefinition = xamlDefinition };

            scopeToUse.Variables[name] = variable;
        }

        private bool VariableDefined(string variableName)
        {
            if (variableName.StartsWith("WORKFLOW:", StringComparison.OrdinalIgnoreCase))
            {
                variableName = variableName.Remove(0, "WORKFLOW:".Length);
            }

            // Check that it's not already defined
            foreach (VariableScope scope in scopes)
            {
                if (scope.Variables.ContainsKey(variableName))
                {
                    return true;
                }
            }

            return false;
        }

        private bool VariableDefinedInCurrentScope(string variableName)
        {
            VariableScope scopeToCheck = scopes.Peek();

            if (variableName.StartsWith("WORKFLOW:", StringComparison.OrdinalIgnoreCase))
            {
                scopeToCheck = scopes.Last();
                variableName = variableName.Remove(0, "WORKFLOW:".Length);
            }

            return scopeToCheck.Variables.ContainsKey(variableName);
        }


        private VariableDefinition GetVariableDefinition(string variableName)
        {
            return (from scope in scopes
                    where scope.Variables.ContainsKey(variableName)
                    select scope.Variables[variableName]).FirstOrDefault();
        }

        private void DumpVariables(string scopeType)
        {
            VariableScope currentScope = scopes.Peek();

            if (currentScope.Variables.Count > 0)
            {
                WriteLine(String.Format(CultureInfo.InvariantCulture, "<{0}.Variables>", scopeType));
                IndentLevel();

                foreach (VariableDefinition variable in currentScope.Variables.Values)
                {
                    WriteLine(variable.XamlDefinition);
                }

                UnindentLevel();
                WriteLine(String.Format(CultureInfo.InvariantCulture, "</{0}.Variables>", scopeType));
            }
        }

        private void EnterScope()
        {
            VariableScope newScope = new VariableScope();
            scopes.Push(newScope);
        }

        private void LeaveScope()
        {
            scopes.Pop();
        }

        private void EnterStorage(string variable, bool isAggregatingVariable)
        {
            StorageVariable newStorage = new StorageVariable(variable, isAggregatingVariable);
            resultVariables.Push(newStorage);
        }

        private void LeaveStorage()
        {
            resultVariables.Pop();
        }

        
        private StorageVariable GetVariableToUse()
        {
            if (resultVariables != null && resultVariables.Count > 0)
            {
                return resultVariables.Peek();
            }

            return null;
        }

        private bool LocalVariableAlreadyExisting()
        {
            if (resultVariables != null && resultVariables.Count > 0)
            {
                return !resultVariables.Peek().IsAggregatingVariable;
            }
            return false;
        }

        private bool IsDataAggregatingVariable(string actualVariableName)
        {
            // Check that it's not already defined
            return resultVariables.Any(scope => scope.IsAggregatingVariable && scope.VariableName.Equals(actualVariableName, StringComparison.OrdinalIgnoreCase));
        }

        private string RemoveScriptBlockBraces(string expression)
        {
            string trimmedExpression = expression.Trim();

            if (trimmedExpression.StartsWith("{", StringComparison.OrdinalIgnoreCase) && 
                trimmedExpression.EndsWith("}", StringComparison.OrdinalIgnoreCase))
            {
                trimmedExpression = trimmedExpression.Remove(0, 1);
                trimmedExpression = trimmedExpression.Remove(trimmedExpression.Length - 1, 1);
            }

            return trimmedExpression;
        }


        private void GeneratePowerShellValue(Type outputType, string expression, bool isLiteral, string resultVariable)
        {
            if ((outputType == typeof(ScriptBlock)) ||
                (outputType == typeof(ScriptBlock[])))
            {
                expression = RemoveScriptBlockBraces(expression);
            }

            string convertedTypeName = GetConvertedTypeName(outputType);
            string powerShellValueFriendlyName = GetFriendlyName(null, typeof(PowerShellValue<string>));

            bool useDefaultInput = System.Text.RegularExpressions.Regex.IsMatch(expression, "\\$input", RegexOptions.IgnoreCase);

            String valueLine = null;

            valueLine = String.Format(CultureInfo.InvariantCulture,
                    @"<" + powerShellValueFriendlyName + @" x:TypeArguments=""{0}"" Expression=""{1}""",
                convertedTypeName,
                EncodeStringNonArgument(expression, isLiteral)
                );

            if(! String.IsNullOrEmpty(resultVariable))
            {
                valueLine += String.Format(CultureInfo.InvariantCulture, " Result=\"[{0}]\"", resultVariable);
            }

            if (useDefaultInput)
            {
                valueLine +=  " UseDefaultInput=\"true\"";
            }

            valueLine += " />";

            WriteLine(valueLine);
        }

        private void GeneratePowerShellValue(Type outputType, string expression, bool isLiteral, bool storeResults)
        {
            string resultVariable = null;

            if ((LocalVariableAlreadyExisting()) && (storeResults))
            {
                resultVariable = resultVariables.Peek().VariableName;
            }

            GeneratePowerShellValue(outputType, expression, isLiteral, resultVariable);
        }


        private bool CheckIfExpressionHasUnsupportedVariableOrHasSideEffects(string variableName, Ast root, out string nameOfUnSupportedVariableFound)
        {
            nameOfUnSupportedVariableFound = null;
            if (root == null) { return false; }

            ExpressionHasUnsupportedVariableOrSideEffectsVisitor visitor = new ExpressionHasUnsupportedVariableOrSideEffectsVisitor(variableName);
            root.Visit(visitor);
            nameOfUnSupportedVariableFound = visitor.NameOfUnsupportedVariableFound;

            return visitor.ExpressionHasSideEffects;
        }
        
        object ICustomAstVisitor.VisitPipeline(PipelineAst pipelineAst)
        {
            // If it's one command, generate the call alone
            if (pipelineAst.PipelineElements.Count == 1)
            {
                pipelineAst.PipelineElements[0].Visit(this);
            }
            else
            {
                // It's a pipeline. Look for any elements that use -DisplayName or -PipelineVariable, as they become
                // an inline foreach and simulate the -PipelineVariable concept in PowerShell.
                //
                // When used without Foreach -Sequence,
                //
                // A | B | C -PipelineVariable C2 | D -DisplayName D2 | E | F | G
                //
                // becomes
                //
                // foreach($C2 in A | B |C)
                // {
                //     foreach($D2 in $C2 | D)
                //     {
                //         $D2 | E | F | G
                //     }
                // }
                //
                // The pipeline variable becomes the foreach variable
                // The commands up to (and including) the command with the pipeline variable
                //  become the foreach condition
                // The commands after the command with the pipeline variable become the foreach
                //  body.
                //
                // When used with Foreach -Sequence, we create an iteration-style pipeline.
                // This captures output state in hashtable elements, and restores the state during
                // each iteration.
                //
                // A -PipelineVariable A2 | Foreach-Object -Sequence { Command1 } -PipelineVariable B1 |
                //    Foreach-Object -Sequence { Command2 } -PipelineVariable C1
                //
                // becomes
                //
                // foreach($a2 in A)
                // {
                //      $psPipelineResults = @( @{} )
                //      
                //      $psPipelineResults = foreach($item in $psPipelineResults)
                //      {
                //          foreach($output in Command1)
                //          {
                //              $result = $item.Clone()
                //              $result["B1"] = $output
                //              $result["_"] = $output
                //              $result
                //          }
                //       }
                //
                //      $psPipelineResults = foreach($item in $psPipelineResults)
                //      {
                //          foreach($output in Command2)
                //          {
                //              $B1 = $item["B1"]
                //              $PSItem = $item["_"]
                //
                //              $result = $item.Clone()
                //              $result["C1"] = $output
                //              $result["_"] = $output
                //              $result
                //          }
                //       }
                //
                //       foreach($item in $psPipelineResults) { $item["_"] }
                // }

                List<CommandBaseAst> prePipelineElements = new List<CommandBaseAst>();
                List<CommandBaseAst> postPipelineElements = new List<CommandBaseAst>();
                List<StatementAst> iterativePipelineElements = new List<StatementAst>();

                string currentPipelineVariable = null;
                string nonIterativePipelineVariable = null;
                List<string> iterativePipelineVariables = new List<string>();
                bool isIterativePipeline = false;
                bool isIterativeCommand = false;
                bool processedPipelineVariable = false;

                foreach (CommandBaseAst commandBase in pipelineAst.PipelineElements)
                {
                    // If this defines a DisplayName or PipelineVariable, then we need to split this
                    // pipeline into two segments.
                    CommandAst command = commandBase as CommandAst;
                    isIterativeCommand = false;

                    if (command != null)
                    {
                        StaticBindingResult bindingResult = StaticParameterBinder.BindCommand(command, false);
                        IScriptExtent errorExtent = null;

                        // Get the argument for the PipelineVariable parameter - last one wins.
                        currentPipelineVariable = null;
                        string boundParameter = null;
                        string[] parametersToCheck = new string[] { "DisplayName", "PV", "PipelineVariable" };

                        foreach (string parameterName in parametersToCheck)
                        {
                            if (GetArgumentAndExtentForParameter(parameterName, ref currentPipelineVariable, bindingResult, ref errorExtent))
                            {
                                // Ensure it wasn't treated like a switch statement
                                if (String.Equals(currentPipelineVariable, "-" + parameterName, StringComparison.OrdinalIgnoreCase))
                                {
                                    currentPipelineVariable = "";
                                }

                                boundParameter = parameterName;
                                
                                // DisplayName can also be used as a Activity.DisplayName in that case DisplayName param value is not treated as a PipelineVariable
                                if (String.Equals(parameterName, "DisplayName", StringComparison.OrdinalIgnoreCase) &&
                                    !Regex.IsMatch(currentPipelineVariable, "^[a-zA-Z][a-zA-Z0-9-_]*$"))
                                {
                                    currentPipelineVariable = null;
                                }
                            }
                        }

                        if (currentPipelineVariable != null)
                        {
                            if(String.IsNullOrEmpty(currentPipelineVariable))
                            {
                                string errorMsg = String.Format(CultureInfo.InvariantCulture, ActivityResources.MissingValueForParameter, boundParameter);
                                ReportError("MissingValueForParameter", errorMsg, errorExtent);
                                return null;
                            }

                            ValidateVariableName(currentPipelineVariable, errorExtent);
                        }

                        ScriptBlockExpressionAst sequenceParameter = null;
                        ScriptBlockExpressionAst beginParameter = null;
                        ScriptBlockExpressionAst endParameter = null;

                        // If this is Foreach-Object, check if it's using the "-Sequence" flag. If so, we have an iterative pipeline.
                        string commandName = command.GetCommandName();
                        IterativeCommands iterativeCommandType = IterativeCommands.None;
                        if (string.Equals("Foreach-Object", commandName, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals("%", commandName, StringComparison.OrdinalIgnoreCase))
                        {
                            if (bindingResult.BoundParameters.ContainsKey("Sequence"))
                            {
                                iterativeCommandType = IterativeCommands.ForEachSequence;

                                // Detect foreach-object in the wrong pipeline position (where there are no
                                // pre-pipeline elements)
                                if (prePipelineElements.Count == 0)
                                {
                                    ReportError("InvalidForeachSequencePipelinePosition", ActivityResources.InvalidForeachSequencePipelinePosition, command.Extent);
                                    return null;
                                }

                                isIterativeCommand = true;
                                isIterativePipeline = true;

                                List<string> supportedParameters = new List<string>() { "PipelineVariable", "PV", "DisplayName", "Begin", "Sequence", "End" };
                                foreach(string parameter in bindingResult.BoundParameters.Keys)
                                {
                                    if(! supportedParameters.Contains(parameter, StringComparer.OrdinalIgnoreCase))
                                    {
                                        ReportError("InvalidForeachSequenceParameter", ActivityResources.InvalidForeachSequenceParameter, command.Extent);
                                    }
                                }

                                // Get the argument for the Sequence parameter, if it exists.
                                sequenceParameter = bindingResult.BoundParameters["Sequence"].Value as ScriptBlockExpressionAst;
                                if ((sequenceParameter == null) || (sequenceParameter.ScriptBlock.EndBlock.Statements.Count == 0))
                                {
                                    string errorMsg = String.Format(CultureInfo.InvariantCulture, ActivityResources.MissingValueForParameter, "Sequence");
                                    ReportError("MissingValueForParameter", errorMsg, bindingResult.BoundParameters["Sequence"].Value.Extent);
                                    return null;
                                }

                                // Get the argument for the Begin parameter, if it exists.
                                if (bindingResult.BoundParameters.ContainsKey("Begin"))
                                {
                                    beginParameter = bindingResult.BoundParameters["Begin"].Value as ScriptBlockExpressionAst;
                                    if ((beginParameter == null) || (beginParameter.ScriptBlock.EndBlock.Statements.Count == 0))
                                    {
                                        string errorMsg = String.Format(CultureInfo.InvariantCulture, ActivityResources.MissingValueForParameter, "Begin");
                                        ReportError("MissingValueForParameter", errorMsg, bindingResult.BoundParameters["Begin"].Value.Extent);
                                        return null;
                                    }
                                }

                                // Get the argument for the End parameter, if it exists.
                                if (bindingResult.BoundParameters.ContainsKey("End"))
                                {
                                    endParameter = bindingResult.BoundParameters["End"].Value as ScriptBlockExpressionAst;
                                    if ((endParameter == null) || (endParameter.ScriptBlock.EndBlock.Statements.Count == 0))
                                    {
                                        string errorMsg = String.Format(CultureInfo.InvariantCulture, ActivityResources.MissingValueForParameter, "End");
                                        ReportError("MissingValueForParameter", errorMsg, bindingResult.BoundParameters["End"].Value.Extent);
                                        return null;
                                    }
                                }

                                // It's now an iterative pipeline, remember the nonIterativePipelineVariable as one
                                // that needs to be set
                                if ((!String.IsNullOrEmpty(nonIterativePipelineVariable)) &&
                                    (! iterativePipelineVariables.Contains(nonIterativePipelineVariable, StringComparer.OrdinalIgnoreCase)))
                                {
                                    iterativePipelineVariables.Add(nonIterativePipelineVariable);
                                }

                            }
                        }
                        else if ((string.Equals("Where-Object", commandName, StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals("?", commandName, StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals("where", commandName, StringComparison.OrdinalIgnoreCase)) &&
                                 (bindingResult.BoundParameters.ContainsKey("Sequence")))
                        {
                            iterativeCommandType = IterativeCommands.WhereSequence;

                            // Detect Where-Object with -Sequence in the wrong pipeline position (where there are no
                            // pre-pipeline elements)
                            if (prePipelineElements.Count == 0)
                            {
                                ReportError("InvalidWhereSequencePipelinePosition", ActivityResources.InvalidWhereSequencePipelinePosition, command.Extent);
                                return null;
                            }

                            isIterativeCommand = true;
                            isIterativePipeline = true;

                            // Check supported parameters
                            List<string> supportedParameters = new List<string>() { "PipelineVariable", "PV", "Sequence" };
                            foreach (string parameter in bindingResult.BoundParameters.Keys)
                            {
                                if (!supportedParameters.Contains(parameter, StringComparer.OrdinalIgnoreCase))
                                {
                                    ReportError("InvalidWhereSequenceParameter", ActivityResources.InvalidWhereSequenceParameter, command.Extent);
                                }
                            }

                            // Get Sequence parameter argument.
                            sequenceParameter = bindingResult.BoundParameters["Sequence"].Value as ScriptBlockExpressionAst;
                            if ((sequenceParameter == null) || (sequenceParameter.ScriptBlock.EndBlock.Statements.Count == 0))
                            {
                                string errorMsg = String.Format(CultureInfo.InvariantCulture, ActivityResources.MissingValueForParameter, "Sequence");
                                ReportError("MissingValueForParameter", errorMsg, bindingResult.BoundParameters["Sequence"].Value.Extent);
                                return null;
                            }

                            // This is now an interative pipeline, remember the nonIterativePipelineVariable as one that needs to be set.
                            if (!String.IsNullOrEmpty(nonIterativePipelineVariable) &&
                                !iterativePipelineVariables.Contains(nonIterativePipelineVariable, StringComparer.OrdinalIgnoreCase))
                            {
                                iterativePipelineVariables.Add(nonIterativePipelineVariable);
                            }
                        }

                        // Verify that once they've started an interative pipeline, all commands are
                        // Foreach-Object -Sequence or Where-Object -Sequence.
                        if (isIterativePipeline && (! isIterativeCommand))
                        {
                            ReportError("EntirePipelineMustUseForeachSequence", ActivityResources.EntirePipelineMustUseForeachSequence, command.Extent);
                            return null;
                        }

                        // If this is the first command with the pipelineVariable,
                        // it is now the pipeline variable for the first portion of
                        // the pipeline.
                        if ((currentPipelineVariable != null) && (! processedPipelineVariable))
                        {
                            nonIterativePipelineVariable = currentPipelineVariable;
                        }

                        // Generate the iterative pipeline element if we got one.
                        if (isIterativePipeline)
                        {
                            switch (iterativeCommandType)
                            {
                                case IterativeCommands.ForEachSequence:
                                    GenerateIterativePipelineElementForForEach(iterativePipelineElements, currentPipelineVariable, iterativePipelineVariables, beginParameter, sequenceParameter, endParameter);
                                    break;

                                case IterativeCommands.WhereSequence:
                                    GenerateIterativePipelineElementForWhere(iterativePipelineElements, currentPipelineVariable, iterativePipelineVariables, sequenceParameter);
                                    break;
                            }

                            if (! String.IsNullOrEmpty(currentPipelineVariable))
                            {
                                iterativePipelineVariables.Add(currentPipelineVariable);
                            }
                        }
                    }

                    // Otherwise, remember the non-iterative command.
                    // If we haven't processed the command with the pipeline variable yet, then it goes
                    // into the prePipeline. Otherwise, it goes into the post pipeline.
                    if(! isIterativePipeline)
                    {
                        if (! processedPipelineVariable)
                        {
                            prePipelineElements.Add((CommandBaseAst)commandBase.Copy());

                            if (nonIterativePipelineVariable != null)
                            {
                                processedPipelineVariable = true;
                            }
                        }
                        else
                        {
                            postPipelineElements.Add(commandBase);
                        }
                    }
                }

                // Generate the outputter if we had an iterative pipeline
                if (isIterativePipeline)
                {
                    string outputter = "foreach($item in $PSPipelineVariableContext) { $item['_'] }";
                    Token[] unusedTokens = null;
                    ParseError[] unusedParseErrors = null;
                    Ast pipelineElementAst = Parser.ParseInput(outputter, out unusedTokens, out unusedParseErrors);

                    iterativePipelineElements.AddRange(((ScriptBlockAst)pipelineElementAst).EndBlock.Statements);
                }

                // See if this is a generated pipeline segment used to support a pipeline variable.
                // If it is, don't process any further.
                bool isInGeneratedPipeline =
                    (pipelineAst.Parent) != null &&
                    (pipelineAst.Parent is ForEachStatementAst) &&
                    (pipelineAst.Parent.Parent == null);

                // If we got a pipeline variable, generate the appropriate style of pipeline.
                if ((!isInGeneratedPipeline) && (isIterativePipeline || processedPipelineVariable))
                {
                    bool savedSymbolGeneration = this.disableSymbolGeneration;

                    try
                    {
                        GenerateSymbolicInformation(pipelineAst.Extent);
                        this.disableSymbolGeneration = true;

                        // This is an iterative pipeline
                        if (isIterativePipeline)
                        {
                            if (isVisitingIterativePipeline)
                            {
                                ReportError("CannotNestIterativePipeline", ActivityResources.CannotNestIterativePipeline, pipelineAst.Extent);
                                return null;
                            }

                            try
                            {
                                this.isVisitingIterativePipeline = true;
                                GenerateIterativePipeline(prePipelineElements, iterativePipelineElements, nonIterativePipelineVariable);
                            }
                            finally
                            {
                                this.isVisitingIterativePipeline = false;
                            }
                        }
                        else
                        {
                            // This is a regular pipeline with a pipeline variable
                            GeneratePipelineVariablePipeline(pipelineAst, prePipelineElements, postPipelineElements, nonIterativePipelineVariable);
                        }
                    }
                    finally
                    {
                        this.disableSymbolGeneration = savedSymbolGeneration;
                    }
                }
                else
                {
                    // We didn't get a pipeline variable at all. It's just a regular pipeline.
                    PipelineAst completePipeline = new PipelineAst(pipelineAst.Extent, prePipelineElements);
                    GeneratePipelineCall(completePipeline);
                }
            }

            return null;
        }

        private void ValidateVariableName(string variableName, IScriptExtent errorExtent)
        {
            string actualVariableNameUnused;
            if (!IsValidMemberName(variableName, out actualVariableNameUnused, false))
            {
                // This was an error with variable scoping
                if (variableName.IndexOf(':') > 0)
                {
                    ReportError("WorkflowScopeOnlyValidInParallelOrSequenceBlock", ActivityResources.WorkflowScopeOnlyValidInParallelOrSequenceBlock, errorExtent);
                }
                else
                {
                    string error = String.Format(CultureInfo.InvariantCulture, ActivityResources.InvalidMemberName, variableName);
                    ReportError("VariableNameNotValid", error, errorExtent);
                }
            }
        }

        private void GenerateIterativePipelineElementForForEach(List<StatementAst> iterativePipelineElements, string currentPipelineVariable, List<string> iterativePipelineVariables,
            ScriptBlockExpressionAst beginParameter, ScriptBlockExpressionAst sequenceParameter, ScriptBlockExpressionAst endParameter)
        {
            // Define the variable that will be used to hold the pipeline iteration count
            if ((beginParameter != null) || (endParameter != null))
            {
                VariableDefinition pipelineIteration = GetVariableDefinition("PSPipelineIteration");
                if (pipelineIteration == null)
                {
                    DefineVariable("PSPipelineIteration", typeof(Int32), null, "0");
                }

                VariableDefinition pipelineCount = GetVariableDefinition("PSPipelineLength");
                if (pipelineIteration == null)
                {
                    DefineVariable("PSPipelineLength", typeof(Int32), null, "0");
                }
            }

            StringBuilder iterativePipelineElement = new StringBuilder();

            // Set up start of foreach loop to go over all output collected so far
            iterativePipelineElement.AppendLine("$PSPipelineIteration = 0");
            SetUpIterativePipelineLoop(iterativePipelineElement, iterativePipelineVariables);

            // If they specified "-Begin {}", then generate the script to invoke that code before repeated invocation of the -Sequence {}
            if (beginParameter != null)
            {
                iterativePipelineElement.AppendLine("    if($PSPipelineIteration -eq 0)");
                iterativePipelineElement.AppendLine("    {");
                iterativePipelineElement.AppendLine("        [System.Management.Automation.PSDataCollection[PSObject]] $PSSequenceOutput = $null");
                iterativePipelineElement.AppendLine("        $PSSequenceOutput = sequence " + beginParameter.Extent.Text);

                // Handle PSSequenceOutput output
                HandlePSSequenceOutput(iterativePipelineElement, currentPipelineVariable);

                iterativePipelineElement.AppendLine("    }");
            }

            // Now take all the output from invoking the -Sequence {} portion, and create new context frames to hold
            // the output. These get stored in PSPipelineVariableContext by the foreach loop above.
            iterativePipelineElement.AppendLine("    [System.Management.Automation.PSDataCollection[PSObject]] $PSSequenceOutput = $null");
            iterativePipelineElement.AppendLine("    $PSSequenceOutput = sequence " + sequenceParameter.Extent.Text);

            // Handle PSSequenceOutput output
            HandlePSSequenceOutput(iterativePipelineElement, currentPipelineVariable);

            // If they specified "-End {}", then generate the script to invoke that code after repeated invocation of the -Sequence {}
            if (endParameter != null)
            {
                iterativePipelineElement.AppendLine("    if($PSPipelineIteration -eq ($PSPipelineLength- 1))");
                iterativePipelineElement.AppendLine("    {");
                iterativePipelineElement.AppendLine("        [System.Management.Automation.PSDataCollection[PSObject]] $PSSequenceOutput = $null");
                iterativePipelineElement.AppendLine("        $PSSequenceOutput = sequence " + endParameter.Extent.Text);

                // Handle PSSequenceOutput output
                HandlePSSequenceOutput(iterativePipelineElement, currentPipelineVariable);

                iterativePipelineElement.AppendLine("    }");
            }

            // Finish iterative pipeline loop.
            FinishIterativePipelineLoop(iterativePipelineElement, iterativePipelineVariables, true);

            // Parse the iterative pipeline element and update the pipeline elements list.
            ParseAndUpdatePipelineElements(iterativePipelineElement, iterativePipelineElements);
        }

        private void GenerateIterativePipelineElementForWhere(List<StatementAst> iterativePipelineElements, string currentPipelineVariable, List<string> iterativePipelineVariables,
            ScriptBlockExpressionAst sequenceParameter)
        {
            StringBuilder iterativePipelineElement = new StringBuilder();

            // Set up start of foreach loop to go over all output collected so far
            SetUpIterativePipelineLoop(iterativePipelineElement, iterativePipelineVariables);

            // Generate output with the Where-Object command and provided sequence script block.
            iterativePipelineElement.AppendLine("    [System.Management.Automation.PSDataCollection[PSObject]] $PSSequenceOutput = $null");
            iterativePipelineElement.AppendLine("    $PSSequenceOutput = Where-Object -InputObject $PSItem -FilterScript " + sequenceParameter.Extent.Text);

            // Handle PSSequenceOutput output
            HandlePSSequenceOutput(iterativePipelineElement, currentPipelineVariable);

            // Finish the iterative pipeline loop.
            FinishIterativePipelineLoop(iterativePipelineElement, iterativePipelineVariables, false);

            // Parse the iterative pipeline element and update the pipeline elements list.
            ParseAndUpdatePipelineElements(iterativePipelineElement, iterativePipelineElements);
        }

        private void SetUpIterativePipelineLoop(StringBuilder iterativePipelineElement, List<string> iterativePipelineVariables)
        {
            // Use a foreach loop to go over all output collected so far
            iterativePipelineElement.AppendLine("$PSPipelineLength = @($PSPipelineVariableContext).Count");
            iterativePipelineElement.AppendLine("$PSPipelineVariableContext = foreach($PSPipelineItem in $PSPipelineVariableContext)");
            iterativePipelineElement.AppendLine("{");
            iterativePipelineElement.AppendLine("    $PSItem = $PSPipelineItem['_']");

            // Create virtual variables that represent the variables used in the iterative pipeline, with
            // their values taken from the context frame (PSPipelineItem).
            foreach (string previousPipelineVariable in iterativePipelineVariables)
            {
                iterativePipelineElement.AppendLine("    $" + previousPipelineVariable + " = $PSPipelineItem['" + previousPipelineVariable + "']");
            }
        }

        private void HandlePSSequenceOutput(StringBuilder iterativePipelineElement, string currentPipelineVariable)
        {
            // Take all the output from invoking -Sequence {} portion and create new context frames to hold
            // the output.  These are stored in PSPipelineVariableContext by the foreach loop above.
            iterativePipelineElement.AppendLine("    foreach($output in $PSSequenceOutput)");
            iterativePipelineElement.AppendLine("    {");
            iterativePipelineElement.AppendLine("        PowerShellValue[Object] -Expression '");
            iterativePipelineElement.AppendLine("        $PSPipelineItem = $PSPipelineItem.Clone();");
            iterativePipelineElement.AppendLine("        $PSPipelineItem[\"_\"] = $output;");
            iterativePipelineElement.AppendLine("        $PSPipelineItem[\"" + currentPipelineVariable + "\"] = $output;");
            iterativePipelineElement.AppendLine("        $PSPipelineItem'");
            iterativePipelineElement.AppendLine("    }");
        }

        private void FinishIterativePipelineLoop(StringBuilder iterativePipelineElement, List<string> iterativePipelineVariables, bool incrementIterationCount)
        {
            // Clean out the iterative pipeline variables so that they don't have values when the iteration
            // completes
            foreach (string previousPipelineVariable in iterativePipelineVariables)
            {
                iterativePipelineElement.AppendLine("    $" + previousPipelineVariable + " = $null");
            }

            // Finish script.
            if (incrementIterationCount)
            {
                iterativePipelineElement.AppendLine("    $PSPipelineIteration = $PSPipelineIteration + 1");
            }
            iterativePipelineElement.AppendLine("}");
        }

        private void ParseAndUpdatePipelineElements(StringBuilder iterativePipelineElement, List<StatementAst> iterativePipelineElements)
        {
            // We can't use the parse errors, since the parser thinks we are using workflow constructs
            // ("sequence") outside of a workflow context. We're only injecting validated pipeline variable names
            // and already-parsed text, so that is OK.
            Token[] unusedTokens = null;
            ParseError[] unusedParseErrors = null;
            Ast pipelineElementAst = Parser.ParseInput(iterativePipelineElement.ToString(), out unusedTokens, out unusedParseErrors);

            iterativePipelineElements.AddRange((((ScriptBlockAst)pipelineElementAst).EndBlock.Statements));
        }

        private static bool GetArgumentAndExtentForParameter(string key, ref string pipelineVariable,
            StaticBindingResult parameters, ref IScriptExtent errorExtent)
        {
            if (parameters.BoundParameters.ContainsKey(key))
            {
                ParameterBindingResult argument = parameters.BoundParameters[key];
                string stringConstantValue = argument.ConstantValue as string;

                if (stringConstantValue != null)
                {
                    pipelineVariable = stringConstantValue;
                }
                else
                {
                    pipelineVariable = argument.Value.Extent.Text;
                }

                errorExtent = argument.Value.Extent;
                return true;
            }

            return false;
        }

        private void GenerateIterativePipeline(List<CommandBaseAst> prePipelineElements, List<StatementAst> bodyElements, string pipelineVariable)
        {
            // Define the context variable that will be used to hold context frames
            VariableDefinition pipelineVariableContext = GetVariableDefinition("PSPipelineVariableContext");
            if (pipelineVariableContext == null)
            {
                DefineVariable("PSPipelineVariableContext", typeof(PSDataCollection<PSObject>), null, null);
            }

            // Generate the pipeline initializer
            string conditionExtent = "";
            foreach (var element in prePipelineElements)
            {
                if (!String.IsNullOrEmpty(conditionExtent))
                {
                    conditionExtent += " | ";
                }

                conditionExtent += element.Extent.Text;
            }

            StringBuilder iterativePipelineElement = new StringBuilder();
            iterativePipelineElement.AppendLine("[System.Management.Automation.PSDataCollection[PSObject]] $PSPipelineVariableContext = @( @{} )");

            iterativePipelineElement.AppendLine("[System.Management.Automation.PSDataCollection[PSObject]] $PSSequenceOutput = $null");
            iterativePipelineElement.AppendLine("$PSSequenceOutput = " + conditionExtent);
            iterativePipelineElement.AppendLine("$PSPipelineVariableContext = foreach($output in $PSSequenceOutput)");
            iterativePipelineElement.AppendLine("{");
            iterativePipelineElement.AppendLine("    PowerShellValue[Object] -Expression '");
            iterativePipelineElement.AppendLine("    $PSPipelineItem = @{};");
            iterativePipelineElement.AppendLine("    $PSPipelineItem[\"_\"] = $output;");
            iterativePipelineElement.AppendLine("    $PSPipelineItem[\"" + pipelineVariable + "\"] = $output;");
            iterativePipelineElement.AppendLine("    $PSPipelineItem'");
            iterativePipelineElement.AppendLine("}");

            // We can't use the parse errors, since the parser thinks we are using workflow constructs
            // ("sequence") outside of a workflow context. We're only injecting validated pipeline variable names
            // and already-parsed text, so that is OK.
            Token[] unusedTokens = null;
            ParseError[] unusedParseErrors = null;
            ScriptBlockAst pipelineElementAst = (ScriptBlockAst) Parser.ParseInput(iterativePipelineElement.ToString(), out unusedTokens, out unusedParseErrors);

            foreach (StatementAst statement in pipelineElementAst.EndBlock.Statements)
            {
                statement.Visit(this);
            }

            foreach (Ast bodyElement in bodyElements)
            {
                bodyElement.Visit(this);
            }
        }

        private void GeneratePipelineVariablePipeline(PipelineAst pipelineAst, List<CommandBaseAst> prePipelineElements, List<StatementAst> bodyElements, string pipelineVariable)
        {
            // If there's a storage variable, it now needs to become an aggregating variable
            var storageVariable = GetVariableToUse();
            if (storageVariable != null)
            {
                storageVariable.IsAggregatingVariable = true;
            }

            Token[] unusedTokens = null;
            ParseError[] unusedParseErrors = null;

            // Generate the extent for the variable in the foreach
            string extentText = "$" + pipelineVariable;
            Ast newAst = Parser.ParseInput(extentText, out unusedTokens, out unusedParseErrors);
            IScriptExtent variableExtent = newAst.Extent;

            newAst = Parser.ParseInput(extentText, out unusedTokens, out unusedParseErrors);
            IScriptExtent foreachBodyExtent = newAst.Extent;

            List<StatementAst> foreachBody = new List<StatementAst>();
            foreach (StatementAst bodyElement in bodyElements)
            {
                foreachBody.Add((StatementAst) bodyElement.Copy());
            }

            // Generate the AST and extent for the foreach condition
            string conditionExtent = "";
            foreach (var element in prePipelineElements)
            {
                if (!String.IsNullOrEmpty(conditionExtent))
                {
                    conditionExtent += " | ";
                }

                conditionExtent += element.Extent.Text;
            }
            newAst = Parser.ParseInput(conditionExtent, out unusedTokens, out unusedParseErrors);
            IScriptExtent foreachConditionExtent = newAst.Extent;

            // Generate the actual foreach AST, and visit it.
            ForEachStatementAst foreachStatement = new ForEachStatementAst(pipelineAst.Extent, null, ForEachFlags.None,
                new VariableExpressionAst(variableExtent, pipelineVariable, false),
                new PipelineAst(foreachConditionExtent, prePipelineElements),
                new StatementBlockAst(foreachBodyExtent, foreachBody, null));
            foreachStatement.Visit(this);
        }

        private void GeneratePipelineVariablePipeline(PipelineAst pipelineAst, List<CommandBaseAst> prePipelineElements, List<CommandBaseAst> postPipelineElements, string pipelineVariable)
        {
            Token[] unusedTokens = null;
            ParseError[] unusedParseErrors = null;

            // Generate the extent for the variable in the foreach
            string extentText = "$" + pipelineVariable;
            Ast newAst = Parser.ParseInput(extentText, out unusedTokens, out unusedParseErrors);
            IScriptExtent variableExtent = newAst.Extent;

            // Generate the AST and extent for the foreach body
            VariableExpressionAst expressionAst = new VariableExpressionAst(variableExtent, pipelineVariable, false);
            CommandExpressionAst commandExpression = new CommandExpressionAst(variableExtent, expressionAst, null);

            foreach (var element in postPipelineElements)
            {
                extentText += " | " + element.Extent.Text;
            }

            postPipelineElements.Insert(0, commandExpression);

            newAst = Parser.ParseInput(extentText, out unusedTokens, out unusedParseErrors);
            IScriptExtent foreachBodyExtent = newAst.Extent;

            List<StatementAst> bodyStatements = ((ScriptBlockAst)newAst).EndBlock.Statements.ToList<StatementAst>();
            GeneratePipelineVariablePipeline(pipelineAst, prePipelineElements, bodyStatements, pipelineVariable);
        }

        private void GeneratePipelineCall(PipelineAst pipelineAst)
        {
            GenerateSymbolicInformation(pipelineAst.Extent);

            // Create an assembly name reference
            string friendlyName = GetFriendlyName(null, typeof(Pipeline));
            string typeArguments = GetConvertedTypeName(typeof(PSDataCollection<PSObject>));

            // Generate the call to the activity
            var currentVariable = GetVariableToUse();
            string variableToUse = null;
            bool isAggregatingVariable = false;
            if (currentVariable != null)
            {
                variableToUse = currentVariable.VariableName;
                isAggregatingVariable = currentVariable.IsAggregatingVariable;
            }

            if (string.IsNullOrEmpty(variableToUse))
            {
                WriteLine("<" + friendlyName + ">");
            }
            else
            {
                string appendOutput = isAggregatingVariable ? AppendOutputTemplate : string.Empty;
                string pipelineCall = String.Format(CultureInfo.InvariantCulture, "<{0} Result=\"[{1}]\"{2}>", friendlyName, variableToUse, appendOutput);
                WriteLine(pipelineCall);
            }

            IndentLevel();
            WriteLine("<" + friendlyName + ".Activities>");
            IndentLevel();

            bool savedSymbolGeneration = this.disableSymbolGeneration;
            Stack<StorageVariable> savedStorage = this.resultVariables;
            try
            {
                this.disableSymbolGeneration = true;
                this.resultVariables = null;
                this.isVisitingPipeline = true;

                foreach (CommandBaseAst pipelineElement in pipelineAst.PipelineElements)
                {
                    pipelineElement.Visit(this);
                }
            }
            finally
            {
                this.isVisitingPipeline = false;
                this.disableSymbolGeneration = savedSymbolGeneration;
                this.resultVariables = savedStorage;
            }

            UnindentLevel();
            WriteLine("</" + friendlyName + ".Activities>");
            UnindentLevel();
            WriteLine("</" + friendlyName + ">");
        }

        object ICustomAstVisitor.VisitFileRedirection(FileRedirectionAst fileRedirectionAst)
        {
            ReportError("OnlySupportErrorStreamRedirection", ActivityResources.OnlySupportErrorStreamRedirection, fileRedirectionAst.Extent);
            return null;
        }

        object ICustomAstVisitor.VisitMergingRedirection(MergingRedirectionAst mergingRedirectionAst)
        {
            if (mergingRedirectionAst.FromStream != RedirectionStream.Error ||
                mergingRedirectionAst.ToStream != RedirectionStream.Output)
            {
                ReportError("OnlySupportErrorStreamRedirection", ActivityResources.OnlySupportErrorStreamRedirection, mergingRedirectionAst.Extent);
            }

            return null;
        }

        object ICustomAstVisitor.VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst)
        {
            VariableExpressionAst referenceVariable = unaryExpressionAst.Child as VariableExpressionAst;
            string expression = GetPowerShellValueExpression(unaryExpressionAst);

            switch (unaryExpressionAst.TokenKind)
            {
                case TokenKind.PlusPlus:
                case TokenKind.MinusMinus:
                case TokenKind.PostfixPlusPlus:
                case TokenKind.PostfixMinusMinus:
                    if (referenceVariable != null)
                    {
                        string variableName = referenceVariable.VariablePath.ToString();
                        GenerateAssignment(variableName, unaryExpressionAst.Extent, unaryExpressionAst.TokenKind, unaryExpressionAst, expression);
                    }
                    else
                    {
                        ReportError("OperatorRequiresVariable", ActivityResources.OperatorRequiresVariable, unaryExpressionAst.Extent);
                    }
                        
                    break;
                case TokenKind.Minus:
                case TokenKind.Plus:
                case TokenKind.Not:
                case TokenKind.Bnot:
                case TokenKind.Exclaim:
                case TokenKind.Comma:
                case TokenKind.Csplit:
                case TokenKind.Isplit:
                case TokenKind.Join:
                    var currentVariable = GetVariableToUse();
                    string variableToUse = null;
                    bool isAggregatingVariable = false;
                    if (currentVariable != null)
                    {
                        variableToUse = currentVariable.VariableName;
                        isAggregatingVariable = currentVariable.IsAggregatingVariable;
                    }

                    string writeOutputFriendlyName = GetFriendlyName(null, typeof(Microsoft.PowerShell.Utility.Activities.WriteOutput));
                    bool needWriteOutput = isAggregatingVariable || String.IsNullOrEmpty(variableToUse);
                    Type variableType = null;

                    if (needWriteOutput)
                    {
                        variableType = typeof(PSObject[]);
                        GenerateWriteOutputStart(writeOutputFriendlyName, variableToUse, unaryExpressionAst.Extent);
                    }
                    else
                    {
                        variableType = GetVariableDefinition(variableToUse).Type;
                    }

                    // Evaluate the expression
                    GeneratePowerShellValue(variableType, expression, false, true);

                    if (needWriteOutput)
                    {
                        GenerateWriteOutputEnd(writeOutputFriendlyName);
                    }

                    break;
                default:
                    throw new NotSupportedException();
            }

            return null;
        }

        object ICustomAstVisitor.VisitConvertExpression(ConvertExpressionAst convertExpressionAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitConstantExpression(ConstantExpressionAst constantExpressionAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitSubExpression(SubExpressionAst subExpressionAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitUsingExpression(UsingExpressionAst usingExpressionAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitCommand(CommandAst commandAst)
        {
            if (commandAst.InvocationOperator != TokenKind.Unknown)
            {
                ReportError("AlternateInvocationNotSupported", ActivityResources.AlternateInvocationNotSupported, commandAst.Extent);
            }

            if (commandAst.Redirections.Count > 0)
            {
                foreach (RedirectionAst redirection in commandAst.Redirections)
                {
                    redirection.Visit(this);
                }

                this.mergeErrorToOutput = true;
            }

            try
            {
                string commandName = commandAst.GetCommandName();
                CommandInfo command = null;

                bool searchSessionState = !this.ValidateOnly;
                ActivityKind activityKind = ResolveActivityKindBasedOnCommandName(commandName, commandAst, out command, searchSessionState);

                switch (activityKind)
                {
                    case ActivityKind.InlineScript:
                        ProcessInlineScriptAst(commandAst);
                        return null;

                    case ActivityKind.Persist:
                        GeneratePersist(commandAst);
                        return null;

                    case ActivityKind.Suspend:
                        GenerateSuspend(commandAst);
                        return null;

                    case ActivityKind.InvokeExpression:
                        GenerateInvokeExpression(commandAst);
                        return null;

                    case ActivityKind.Delay:
                        if (this.mergeErrorToOutput)
                        {
                            ReportError("CannotRedirectErrorStreamForStartSleep",
                                        ActivityResources.CannotRedirectErrorStreamForStartSleep,
                                        commandAst.Redirections[0].Extent);
                        }

                        GenerateStartSleep(commandAst);
                        return null;

                    case ActivityKind.NewObject:
                        if (this.mergeErrorToOutput)
                        {
                            ReportError("CannotRedirectErrorStreamForNewObject",
                                        ActivityResources.CannotRedirectErrorStreamForNewObject,
                                        commandAst.Redirections[0].Extent);
                        }

                        if (GetVariableToUse() == null)
                        {
                            ReportError("NewObjectMustBeAssigned",
                                        ActivityResources.NewObjectMustBeAssigned,
                                        commandAst.Extent);
                        }

                        GenerateNewObject(commandAst);
                        return null;

                    case ActivityKind.RegularCommand:

                        // If the command resolved to a workflow, generate that as a nested workflow
                        if ((command != null) && (command.CommandType == CommandTypes.Workflow))
                        {
                            WorkflowInfo nestedWorkflow = (WorkflowInfo)command;
                            CreateNestedWorkflow(nestedWorkflow, commandAst, commandAst.Extent);
                        }

                        GenerateCommandCall(commandAst, command);
                        return null;

                    default:
                        // Should not get to default
                        break;
                }
            }
            finally
            {
                this.mergeErrorToOutput = false;
            }

            throw new NotSupportedException();
        }

        private void GenerateCommandCall(CommandAst commandAst, CommandInfo command)
        {
            string commandName = commandAst.GetCommandName();

            var scopeEntry = scope.LookupCommand(commandName);

            // If the named command maps to a function with workflow binding, then we process that
            // function, and create a "workflow calling workflow" scenario.)
            if (scopeEntry != null)
            {
                if (scopeEntry.workflowInfo != null)
                {
                    IScriptExtent extent = null;
                    if(scopeEntry.functionDefinition != null)
                    {
                        extent = scopeEntry.functionDefinition.Extent;
                    }
                    CreateNestedWorkflow(scopeEntry.workflowInfo, commandAst, extent);
                    GenerateWorkflowCallingWorkflowCall(commandAst, scopeEntry.workflowInfo.NestedXamlDefinition, command);
                }
                else
                {
                    // If the named command maps to a regular PowerShell function, then we compile
                    // to an inline script.
                    // If we're in a pipeline, generate an error. The InlineScript wrapper we generate for these
                    // is not capable of passing along input.
                    if (this.isVisitingPipeline)
                    {
                        ReportError("ActivityNotSupportedInPipeline",
                            String.Format(CultureInfo.InvariantCulture, ActivityResources.ActivityNotSupportedInPipeline, commandName), commandAst.Extent);
                    }

                    // Although the function may be defined separately from the invocation,
                    // we generate the InlineScript of:
                    // function foo($bar) { ... }; foo "Test"
                    string functionDefinition = scopeEntry.functionDefinition.Extent.Text;
                    GenerateInlineScriptForFunctionCall(null, functionDefinition, commandAst);
                }
            }
            else if ((command != null) &&
                (command.CommandType == CommandTypes.Function) &&
                (((command.Module == null) || ((definingModule != null) && (String.Equals(command.Module.Path, definingModule.Path, StringComparison.OrdinalIgnoreCase))))))
            {
                // Extract functions from the session state, as long as they are not defined in a module
                // (as they probably have requirements that cannot be met).

                // If we're in a pipeline, generate an error. The InlineScript wrapper we generate for these
                // is not capable of passing along input.
                if (this.isVisitingPipeline)
                {
                    ReportError("ActivityNotSupportedInPipeline",
                        String.Format(CultureInfo.InvariantCulture, ActivityResources.ActivityNotSupportedInPipeline, commandName), commandAst.Extent);
                }

                GenerateInlineScriptForFunctionCall(command.Name, command.Definition, commandAst);
            }
            else
            {
                // Skip this expensive step during the parse phase
                if (!this.ValidateOnly)
                {
                    // If the command maps to an activity, then we compile it into that activity
                    bool activityFound = GenerateActivityCallFromCommand(commandName, commandAst);
                    if (!activityFound)
                    {
                        GenerateNativeCommandCallFromCommand(commandName, commandAst);
                    }
                }
            }
        }

        private void GenerateWorkflowCallingWorkflowCall(CommandAst commandAst, string xaml, CommandInfo command)
        {
            DynamicActivity result;
            using (StringReader xamlReader = new StringReader(xaml))
            {
                result = (DynamicActivity)System.Activities.XamlIntegration.ActivityXamlServices.Load(xamlReader);
            }

            ProcessAssembly(result.GetType().Assembly, processedActivityLibraries, activityMap);

            StaticBindingResult bindingResult = StaticParameterBinder.BindCommand(commandAst, false);
            Dictionary<string, CommandArgumentInfo> convertedParameters = ResolveParameters(bindingResult);

            // If we are calling a nested workflow and are also storing the result, we need to create
            // a new instance of the storage variable.
            var currentVariable = GetVariableToUse();
            string storageVariable = null;
            bool isAggregatingVariable = false;
            if (currentVariable != null)
            {
                storageVariable = currentVariable.VariableName;
                isAggregatingVariable = currentVariable.IsAggregatingVariable;
            }

            if (!String.IsNullOrEmpty(storageVariable) && !isAggregatingVariable)
            {
                string constructor = "New-Object -Type System.Management.Automation.PSDataCollection[PSObject]";
                GeneratePowerShellValue(typeof(System.Management.Automation.PSDataCollection<PSObject>), constructor, false, true);
            }

            // Create Inline Script for parameter validation of the inner workflow.
            string paramBlock = GetWorkflowParamBlock(command);

            bool inlineScriptGenerated = false;
            if (!string.IsNullOrEmpty(paramBlock))
            {
                StringBuilder actualParamBlock = AddAdditionalWorkflowParameters(paramBlock);

                // Add inline script parameters to ensure that validation is run locally
                Dictionary<string, CommandArgumentInfo> inlineScriptParameters = new Dictionary<string, CommandArgumentInfo>(StringComparer.OrdinalIgnoreCase);
                inlineScriptParameters.Add("PSComputerName", new CommandArgumentInfo { Value = "$null" });

                // Generate Inline Script
                GenerateInlineScriptForFunctionCall(command.Name, inlineScriptParameters, actualParamBlock.ToString(), commandAst);
                inlineScriptGenerated = true;
            }

            // Prevent symbols for this workflow call from being added twice.
            bool prevDisableSymbolGeneration = this.disableSymbolGeneration;
            try
            {
                this.disableSymbolGeneration = inlineScriptGenerated || this.disableSymbolGeneration;
                GenerateActivityCall(null, result.Name, result.GetType(), result, convertedParameters, bindingResult, null, commandAst.Extent);
            }
            finally
            {
                this.disableSymbolGeneration = prevDisableSymbolGeneration;
            }
        }

        private static StringBuilder AddAdditionalWorkflowParameters(string paramBlock)
        {
            const string additionalParameters =
                    @"
                    ,
                    [System.Collections.ArrayList] $Result,
                    [System.Collections.ArrayList] $PSError,
                    [System.Collections.ArrayList] $PSProgress,
                    [System.Collections.ArrayList] $PSVerbose,
                    [System.Collections.ArrayList] $PSDebug,
                    [System.Collections.ArrayList] $PSWarning,
                    [System.Collections.ArrayList] $PSInformation,
                    [Nullable[uint32]] $PSActionRetryCount,
                    [bool] $PSDisableSerialization,
                    [NUllable[uint32]] $PSActionRunningTimeoutSec,
                    [NUllable[uint32]] $PSActionRetryIntervalSec,
                    [string[]] $PSRequiredModules";

            // Remove the ")" from the end
            string newParamBlock = paramBlock.Substring(0, paramBlock.Length - 2);
            
            StringBuilder actualParamBlock = new StringBuilder();
            actualParamBlock.Append(newParamBlock);
            actualParamBlock.Append(additionalParameters);
            
            // Add the ")" at the end
            actualParamBlock.Append("\n)");

            return actualParamBlock;
        }

        private string GetWorkflowParamBlock(CommandInfo command)
        {
            var paramBlock = string.Empty;
            if (command != null)
            {
                WorkflowInfo workflowInfo = (WorkflowInfo)command;
                ScriptBlockAst sb;
                if (workflowInfo.ScriptBlock != null)
                {
                    sb = workflowInfo.ScriptBlock.Ast as ScriptBlockAst;
                    if (sb != null)
                    {
                        if (sb.BeginBlock != null && sb.BeginBlock.Statements != null &&
                            sb.BeginBlock.Statements.Count > 0)
                        {
                            FunctionDefinitionAst functionAst = sb.BeginBlock.Statements[0] as FunctionDefinitionAst;
                            if (functionAst != null)
                            {
                                if (functionAst.Body != null && functionAst.Body.ParamBlock != null)
                                {
                                    paramBlock = functionAst.Body.ParamBlock.ToString();
                                }
                            }
                        }
                    }
                }
            }
            return paramBlock;
        }

        private void GenerateInlineScriptForFunctionCall(string name, Dictionary<string, CommandArgumentInfo> parameters, string definition, CommandAst commandAst)
        {
            // We add a trap / break so that the InlineScript actually generates an exception when a terminating
            // error happens.
            string inlineScript = "trap { break }\r\n";
            string functionCall = AddUsingPrefixToWorkflowVariablesForFunctionCall(commandAst);

            if (!String.IsNullOrEmpty(name))
            {
                inlineScript += String.Format(
                    CultureInfo.InvariantCulture,
                    "function {0}\r\n{{\r\n{1}\r\n}}\r\n",
                    name, definition);
            }
            else
            {
                inlineScript += definition + "\r\n";
            }

            // Use the function name as the display name of the InlineScript activity
            string displayName = name ?? commandAst.GetCommandName();

            inlineScript += functionCall;
            GenerateInlineScript(inlineScript, displayName, parameters, commandAst.Extent, false);
        }

        private void GenerateInlineScriptForFunctionCall(string name, string definition, CommandAst commandAst)
        {
            GenerateInlineScriptForFunctionCall(name, null, definition, commandAst);
        }

        // Get all variable expressions, sort them and return
        private static IEnumerable<Ast> GetVariableExpressionAsts(CommandAst command)
        {
            var list = command.FindAll(ast => ast is VariableExpressionAst, searchNestedScriptBlocks: true).ToList();
            if (list.Count > 1)
            {
                return list.OrderBy(a => a.Extent.StartOffset);
            }
            return list;
        }

        private static string AddUsingPrefixToWorkflowVariablesForFunctionCall(CommandAst command)
        {
            string functionCall = command.Extent.Text;
            IEnumerable<Ast> variableExprs = GetVariableExpressionAsts(command);
            if (!variableExprs.Any()) { return functionCall; }

            StringBuilder newFunctionCall = null;
            int baseOffset = command.Extent.StartOffset;
            int commandTextRelativeStartOffset = 0;
            
            foreach (Ast ast in variableExprs)
            {
                var variableExpr = ast as VariableExpressionAst;
                if (variableExpr == null) { continue; }
                if (IsVariableFromUsingExpression(variableExpr, topParent: command)) { continue; }
                if (variableExpr.IsConstantVariable()) { continue; }

                string varName = variableExpr.VariablePath.UserPath;
                if (varName.IndexOf(':') >= 0)
                {
                    if (varName.StartsWith("WORKFLOW:", StringComparison.OrdinalIgnoreCase))
                    {
                        // If the variable has a 'workflow' scope prefix, we always add 'using' prefix to it
                        varName = varName.Remove(0, "WORKFLOW:".Length);
                    }
                    else
                    {
                        continue; // Something like $env:psmodulepath
                    }
                }
                else
                {
                    // We add 'using' prefix to all other variables except $input.
                    // $input doesn't need the 'using' prefix to work within an InlineScript:
                    //    PS\> workflow bar { 1..2 | inlinescript { $input } }
                    //    PS\> bar
                    //    1
                    //    2
                    if (String.Equals(varName, "Input", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                // Check if we need to include the using variable in a parenthesis
                bool needParen = false;
                var indexExprParent = variableExpr.Parent as IndexExpressionAst;
                if (indexExprParent != null && indexExprParent.Target == variableExpr)
                {
                    needParen = true;
                }
                else
                {
                    var memberExprParent = variableExpr.Parent as MemberExpressionAst;
                    if (memberExprParent != null && memberExprParent.Expression == variableExpr)
                    {
                        needParen = true;
                    }
                }

                // We need to alter the function call script by this point
                if (newFunctionCall == null)
                {
                    newFunctionCall = new StringBuilder();
                }

                string newVarName = (variableExpr.Splatted ? "@" : "$") + "using:" + varName;
                newVarName = needParen ? "(" + newVarName + ")" : newVarName;
                int variableRelativeStartOffset = variableExpr.Extent.StartOffset - baseOffset;
                int variableRelativeEndOffset = variableExpr.Extent.EndOffset - baseOffset;

                newFunctionCall.Append(functionCall.Substring(commandTextRelativeStartOffset, variableRelativeStartOffset - commandTextRelativeStartOffset));
                newFunctionCall.Append(newVarName);
                commandTextRelativeStartOffset = variableRelativeEndOffset;
            }

            if (newFunctionCall != null)
            {
                newFunctionCall.Append(functionCall.Substring(commandTextRelativeStartOffset));
                return newFunctionCall.ToString();
            }

            return functionCall;
        }

        private static bool IsVariableFromUsingExpression(VariableExpressionAst variableExpr, Ast topParent)
        {
            var parent = variableExpr.Parent;
            while (parent != topParent)
            {
                if (parent is UsingExpressionAst)
                {
                    return true;
                }
                parent = parent.Parent;
            }
            return false;
        }

        private void GenerateNativeCommandCallFromCommand(string commandName, CommandAst commandAst)
        {
            // If it parses like a command but isn't a regular PowerShell command, then
            // it's likely a native command. Convert this into an InlineScript call, since
            // we can't do anything intelligent about parameter parsing.
            // Use the command name as the display name of the InlineScript activity.
            string inlineScript = AddUsingPrefixToWorkflowVariablesForFunctionCall(commandAst);
            GenerateInlineScript(inlineScript, commandName, null, commandAst.Extent, false);
        }

        // If the named command is in any of the referenced DLLs, then we call it as an activity.
        // If the adjusted named command (i.e.: Get-Process -> GetProcessActivity) is in any of the
        // referenced DLLs, then we call it as an activity.
        private bool GenerateActivityCallFromCommand(string commandName, CommandAst commandAst)
        {
            CommandInfo resolvedCommand;
            Type[] genericTypes;

            // Find the activity for this name
            Type activityType = ResolveActivityType(commandName, commandAst, true, out resolvedCommand, out genericTypes);

            // If this didn't map to a specific command, generate an error that tells the user to use an
            // InlineScript
            if ((activityType == null) && (resolvedCommand == null))
            {
                // Do not report this error at validation time, as it will only ever work
                // at run-time.
                if (!ValidateOnly)
                {
                    string error = String.Format(CultureInfo.InvariantCulture, ActivityResources.CommandNotFound, commandName);
                    ReportError("CommandNotFound", error, commandAst.Extent);
                }
            }

            if (activityType == null) { return false; }

            // Get the parameters used
            StaticBindingResult syntacticResult = StaticParameterBinder.BindCommand(commandAst, false);
            StaticBindingResult bindingResult = StaticParameterBinder.BindCommand(commandAst, true);

            // See if there are any errors to report
            if (bindingResult.BindingExceptions != null)
            {
                foreach(KeyValuePair<string, StaticBindingError> bindingException in bindingResult.BindingExceptions)
                {
                    if (String.Equals("ParameterAlreadyBound",
                        bindingException.Value.BindingException.ErrorId, StringComparison.OrdinalIgnoreCase))
                    {
                        // throw Duplicate parameters error
                        ReportError("DuplicateParametersNotAllowed",
                                    string.Format(CultureInfo.CurrentCulture, ActivityResources.DuplicateParametersNotAllowed, bindingException.Key),
                                    bindingException.Value.CommandElement.Extent);
                    }
                    else if (String.Equals("NamedParameterNotFound",
                        bindingException.Value.BindingException.ErrorId, StringComparison.OrdinalIgnoreCase))
                    {
                        // Skip for now, handled during activity generation.
                    }
                    else
                    {
                        // throw the other error
                        ReportError(
                            bindingException.Value.BindingException.ErrorId,
                            bindingException.Value.BindingException.Message,
                            bindingException.Value.CommandElement.Extent);
                    }
                }
            }

            Dictionary<string, CommandArgumentInfo> convertedParameters = ResolveParameters(bindingResult);

            if (genericTypes != null && genericTypes.Length > 0)
            {
                var genericTypeArg = new CommandArgumentInfo() {Value = genericTypes};
                convertedParameters.Add(GenericTypesKey, genericTypeArg);
            }

            GenerateActivityCall(resolvedCommand, null, activityType, null, convertedParameters, bindingResult, syntacticResult, commandAst.Extent);
            return true;
        }

        /// <summary>
        /// Resolve the activity type based on the given command name
        /// </summary>
        /// <param name="commandName"></param>
        /// <param name="commandAst"></param>
        /// <param name="throwError">Indicate the error action</param>
        /// <param name="resolvedCommand">The CommandInfo instance resolved from the command name</param>
        /// <param name="genericTypes">If it is a generic activity, genericTypes contains the actual types in the order they get declared</param>
        /// <returns></returns>
        internal Type ResolveActivityType(string commandName, CommandAst commandAst, bool throwError, out CommandInfo resolvedCommand, out Type[] genericTypes)
        {
            genericTypes = null;

            int index = commandName.IndexOf('[');
            if (index != -1 && commandName.EndsWith("]", StringComparison.Ordinal))
            {
                string typeNames = commandName.Substring(index + 1, commandName.Length - index - 2);
                commandName = commandName.Substring(0, index).Trim();
                genericTypes = ResolveGenericTypes(typeNames, commandAst.CommandElements[0]);
            }

            // Resolve the command
            resolvedCommand = ResolveCommand(commandName);
            commandName = resolvedCommand != null ? resolvedCommand.Name : commandName;

            // Replace any dashes, and search for activities that match.
            string searchName = commandName.Replace("-", "");

            // Verify it isn't a built-in command
            CommandInfo unused = null;

            bool searchSessionState = !this.ValidateOnly;
            ActivityKind activityKind = ResolveActivityKindBasedOnCommandName(searchName, commandAst, out unused, searchSessionState);
            if (activityKind != ActivityKind.RegularCommand)
            {
                string error = String.Format(CultureInfo.InvariantCulture, ActivityResources.CommandHandledByKeyword, commandName, searchName);
                ReportError("CommandHandledByKeyword", error, commandAst.Extent);
            }

            if (genericTypes != null && genericTypes.Length > 0)
            {
                searchName += String.Format(CultureInfo.InvariantCulture, "`{0}", genericTypes.Length);
            }

            if (!activityMap.ContainsKey(searchName))
            {
                if ((resolvedCommand != null) && (resolvedCommand.CommandType != CommandTypes.Application))
                {
                    // Check if it was explicitly excluded.
                    // - We check for null module names, as this may have come from an activity in SMA (which is a snapin).
                    // - We check for Microsoft.PowerShell.Host, as that module has only two commands (that are not intended to be activities).
                    // - We check to see if this command comes from a module that has defined activities for OTHER commands.
                    string moduleName = resolvedCommand.ModuleName;
                    if ((String.IsNullOrEmpty(moduleName) ||
                        String.Equals("Microsoft.PowerShell.Host", moduleName, StringComparison.OrdinalIgnoreCase) ||
                        String.Equals("CimCmdlets", moduleName, StringComparison.OrdinalIgnoreCase) ||
                        processedActivityLibraries.Contains(moduleName)) && throwError)
                    {
                        string error = String.Format(CultureInfo.InvariantCulture, ActivityResources.CommandActivityExcluded, commandName);
                        ReportError("CommandActivityExcluded", error, commandAst.Extent);
                    }
                }

                return null;
            }

            // Find the activity for this name
            Type activityType = activityMap[searchName];

            // The name was cached, but the type is null. That means the command name
            // is ambiguous.
            if (activityType == null && throwError)
            {
                String errorMessage = String.Format(CultureInfo.InvariantCulture,
                    ActivityResources.AmbiguousCommand,
                    commandName);
                ReportError("AmbiguousCommand", errorMessage, commandAst.Extent);
            }

            return activityType;
        }

        private Type[] ResolveGenericTypes(string typeNames, Ast targetAst)
        {
            string[] names = typeNames.Split(',');
            var result = new List<Type>();

            foreach (string typeName in names)
            {
                try
                {
                    var outputType = LanguagePrimitives.ConvertTo<Type>(typeName.Trim());
                    result.Add(outputType);
                }
                catch (PSInvalidCastException)
                {
                    string error = String.Format(CultureInfo.InvariantCulture, ActivityResources.NewObjectCouldNotFindType, typeName);
                    ReportError("GenericTypeNotFound", error, targetAst.Extent);
                }
            }

            return result.ToArray();
        }

        private void CreateNestedWorkflow(WorkflowInfo command, CommandAst commandAst, IScriptExtent extent)
        {
            AddWorkflowCommonParameters(command, commandAst, extent);

            // Register the conversion of the outer workflow
            var entry = scope.LookupCommand(command.Name);
            if (entry == null)
            {
                entry = new Scope.Entry { workflowInfo = command, scope = scope };
                scope.functionDefinitions.Add(command.Name, entry);
            }

            nestedWorkflows.Add(command);

            foreach (var nested in command.WorkflowsCalled)
            {
                nestedWorkflows.Add(nested);
            }
        }

        private void AddWorkflowCommonParameters(WorkflowInfo workflowInfo, CommandAst commandAst, IScriptExtent extent)
        {
            // Verify it doesn't use any advanced validation
            foreach (string parameter in workflowInfo.Parameters.Keys)
            {
                // Skip validation on WF-common parameters
                if (parameter.StartsWith("PS", StringComparison.OrdinalIgnoreCase) || Cmdlet.CommonParameters.Contains(parameter))
                {
                    continue;
                }

                ParameterMetadata metadata = workflowInfo.Parameters[parameter];
                foreach (Attribute validationAttribute in metadata.Attributes)
                {
                    if ((validationAttribute is ValidateArgumentsAttribute) ||
                       (validationAttribute is AliasAttribute))
                    {
                        ReportError("ParameterValidationNotSupportedOnNestedWorkflows",
                            ActivityResources.ParameterValidationNotSupportedOnNestedWorkflows,
                            extent);
                    }
                }

            }


            if (this.ValidateOnly) { return; }


            string xaml = workflowInfo.XamlDefinition;
            string nestedXaml = workflowInfo.NestedXamlDefinition;

            // See if it's already been compiled to contain the wrapper properties. If so, just exit.
            // If we haven't compiled this workflow before (or it's never been called with the
            // ubiquitous parameters), then keep on processing.
            if (!String.IsNullOrEmpty(nestedXaml))
            {
                if (Regex.IsMatch(nestedXaml, "(<Activity.*x:Class=\"Microsoft.PowerShell.DynamicActivities.Activity_)(\\d+)_Nested(\"[^>]*>)"))
                {
                    return;
                }
            }

            // We can only add the common parameters to workflows that don't
            // depend on any others. We don't generate an error here, as these
            // nested workflows can still be useful without these common parameters.
            //
            // Given workflow A calling B, calling C, (...), calling Z, we can only support workflow
            // common parameters on Z. This is due to a technical limitation. When we analyze a workflow
            // to add emulation of workflow-common parameters, the API that does this tries to resolve all
            // of the activities and types in that workflow. Workflows that already call workflows (B, C,...)
            // rely on a temporary  DLL that we don't have access to (and can't supply to the
            // API that requires it).

            if (workflowInfo.WorkflowsCalled.Count > 0)
            {
                workflowInfo.NestedXamlDefinition = xaml;
                return;
            }

            // Read the XAML into an activity builder to determine what properties the
            // actual workflow has.
            ActivityBuilder activityBuilder = (ActivityBuilder)XamlServices.Load(
                ActivityXamlServices.CreateBuilderReader(
                    new XamlXmlReader(
                        new StringReader(xaml))));

            HashSet<string> existingProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < activityBuilder.Properties.Count; index++)
            {
                existingProperties.Add(activityBuilder.Properties[index].Name);
            }

            // Extract what parameters they used
            StaticBindingResult specifiedParameters = StaticParameterBinder.BindCommand(commandAst, false);
            List<string> parameterNames = specifiedParameters.BoundParameters.Keys.ToList<string>();

            // See what properties are on PSRemotingActivity
            PropertyInfo[] psRemotingProperties = typeof(PSRemotingActivity).GetProperties();
            bool usesPsRemotingProperties = false;

            // See if they use any PSRemoting properties
            foreach (PropertyInfo property in psRemotingProperties)
            {
                foreach (string parameterName in parameterNames)
                {
                    if (property.Name.StartsWith(parameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (! existingProperties.Contains(parameterName))
                        {
                            usesPsRemotingProperties = true;
                            break;
                        }
                    }
                }

                if (usesPsRemotingProperties) { break; }
            }

            PropertyInfo[] propertiesToUse;
            if (! usesPsRemotingProperties)
            {
                if (String.IsNullOrEmpty(workflowInfo.NestedXamlDefinition))
                {
                    workflowInfo.NestedXamlDefinition = xaml;

                    // Replace the name with _Nested
                    workflowInfo.NestedXamlDefinition = Regex.Replace(
                        workflowInfo.NestedXamlDefinition,
                        "(<Activity.*x:Class=\"Microsoft.PowerShell.DynamicActivities.Activity_)(\\d+)(\"[^>]*>)",
                        "$1$2_Nested$3",
                        RegexOptions.Singleline);

                    // Replace any parameter defaults
                    while (Regex.IsMatch(workflowInfo.NestedXamlDefinition,
                        "<Activity.*local:Activity_\\d+\\.[^>]*>", RegexOptions.Singleline))
                    {
                        workflowInfo.NestedXamlDefinition = Regex.Replace(
                            workflowInfo.NestedXamlDefinition,
                            "(<Activity.*local:Activity_)(\\d+)(\\.[^>]*>)",
                            "$1$2_Nested$3",
                            RegexOptions.Singleline);
                    }
                }

                // For nested workflows we want to be able to flow preference variables.  To do this we need
                // to include the Verbose, Debug, ErrorAction, WarningAction, and InformationAction parameters in the
                // nested Xaml definition.
                Collection<PropertyInfo> properties = new Collection<PropertyInfo>();
                foreach (var property in psRemotingProperties)
                {
                    if (property.Name.Equals("Verbose", StringComparison.OrdinalIgnoreCase) ||
                        property.Name.Equals("Debug", StringComparison.OrdinalIgnoreCase) ||
                        property.Name.Equals("ErrorAction", StringComparison.OrdinalIgnoreCase) ||
                        property.Name.Equals("WarningAction", StringComparison.OrdinalIgnoreCase) ||
                        property.Name.Equals("InformationAction", StringComparison.OrdinalIgnoreCase))
                    {
                        properties.Add(property);
                    }
                }

                if (properties.Count > 0)
                {
                    propertiesToUse = new PropertyInfo[properties.Count];
                    properties.CopyTo(propertiesToUse, 0);
                }
                else
                {
                    // If they don't use any "workflow calling workflow" common parameters,
                    // Just use the existing XAML
                    return;
                }
            }
            else
            {
                propertiesToUse = psRemotingProperties;
            }

            // Otherwise, crack open the XAML and create a new workflow that has all of the common
            // parameters they might use.
            activityBuilder.Name = activityBuilder.Name + "_Nested";

            // Sequence to set host defaults from the incoming parameters that were auto-generated
            // for the function..
            Sequence configureSequence = new Sequence()
            {
                Activities = { }
            };

            // Activity that holds the existing logic of the current activity
            Activity existingLogic = activityBuilder.Implementation;

            // Sequence to restore values back to the host defaults that were present when
            // we entered the function. Needs to be duplicated at the end of the existing logic
            // and also in the catch() clause.
            Sequence undoSequence = new Sequence() { Activities = { } };
            Sequence undoSequenceInCatch = new Sequence() { Activities = { } };

            Collection<Variable> savedHostDefaults = new Collection<Variable>();
            
            // Go through all the properties in PSRemotingActivity they use, and add them as
            // virtual properties to this activity
            foreach (PropertyInfo field in propertiesToUse)
            {
                // Don't generate wrapper properties for ones that are already defined
                if (existingProperties.Contains(field.Name)) { continue; }

                // See if it's an argument
                if (typeof(Argument).IsAssignableFrom(field.PropertyType))
                {
                    // Create a new property
                    DynamicActivityProperty newProperty = new DynamicActivityProperty();
                    newProperty.Name = field.Name;
                    newProperty.Type = field.PropertyType;
                    activityBuilder.Properties.Add(newProperty);

                    // Create a variable to hold the saved host default
                    savedHostDefaults.Add(
                        new Variable<Object>() { Name = "Saved_" + field.Name }
                    );

                    // Add the configuration of this property to the sequence that sets
                    // the host defaults.
                    IsArgumentSet isArgumentSetActivity = new IsArgumentSet();
                    ActivityBuilder.SetPropertyReference(isArgumentSetActivity, new ActivityPropertyReference { SourceProperty = field.Name, TargetProperty = "Argument" });
                    configureSequence.Activities.Add(
                        new If()
                        {
                            Condition = isArgumentSetActivity,
                            Then = new Sequence
                            {
                                Activities = {
                                    new GetPSWorkflowData<Object> {
                                            VariableToRetrieve = PSWorkflowRuntimeVariable.Other,
                                            OtherVariableName = field.Name,
                                            Result = new VisualBasic.Activities.VisualBasicReference<Object>("Saved_" + field.Name)
                                    },
                                    new SetPSWorkflowData()
                                    {
                                        OtherVariableName = field.Name,
                                        Value = new InArgument<Object>((System.Activities.Activity<Object>)new VisualBasic.Activities.VisualBasicValue<Object>(field.Name))
                                    }
                                }
                            }
                        }
                    );

                    // Add the restoration of this property to the undo sequence
                    isArgumentSetActivity = new IsArgumentSet();
                    ActivityBuilder.SetPropertyReference(isArgumentSetActivity, new ActivityPropertyReference { SourceProperty = field.Name, TargetProperty = "Argument" });
                    undoSequence.Activities.Add(
                        new If()
                        {
                            Condition = isArgumentSetActivity,
                            Then = new SetPSWorkflowData()
                            {
                                OtherVariableName = field.Name,
                                Value = new InArgument<Object>((System.Activities.Activity<Object>)new VisualBasic.Activities.VisualBasicValue<Object>("Saved_" + field.Name))
                            }
                        }
                    );

                    // And the version in the catch block
                    isArgumentSetActivity = new IsArgumentSet();
                    ActivityBuilder.SetPropertyReference(isArgumentSetActivity, new ActivityPropertyReference { SourceProperty = field.Name, TargetProperty = "Argument" });
                    undoSequenceInCatch.Activities.Add(
                        new If()
                        {
                            Condition = isArgumentSetActivity,
                            Then = new SetPSWorkflowData()
                            {
                                OtherVariableName = field.Name,
                                Value = new InArgument<Object>((System.Activities.Activity<Object>)new VisualBasic.Activities.VisualBasicValue<Object>("Saved_" + field.Name))
                            }
                        }
                    );
                }
            }

            // Wrapper commands to save the current host settings, change them based on the parameters that have
            // been passed in, and then restore them.
            Sequence hostSettingWrapper = new Sequence()
            {
                Activities = {
                    new TryCatch()
                    {
                        Try = new Sequence()
                        {
                            Activities = {
                                configureSequence,
                                existingLogic,
                                undoSequence
                            }
                        },
                        Catches = {
                            new Catch<Exception>()
                            {
                                Action = new ActivityAction<Exception>()
                                {
                                    Handler = new Sequence() {
                                        Activities = {
                                            undoSequenceInCatch,
                                            new Rethrow()
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            foreach (Variable savedHostDefault in savedHostDefaults)
            {
                hostSettingWrapper.Variables.Add(savedHostDefault);
            }

            activityBuilder.Implementation = hostSettingWrapper;

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;

            StringBuilder resultWriter = new StringBuilder();
            XmlWriter xmlWriter = XmlWriter.Create(resultWriter, settings);
            XamlSchemaContext xamlSchemaContext = new XamlSchemaContext();
            XamlXmlWriter xamlXmlWriter = new XamlXmlWriter(xmlWriter, xamlSchemaContext);
            XamlWriter xamlWriter = ActivityXamlServices.CreateBuilderWriter(xamlXmlWriter);
            XamlServices.Save(xamlWriter, activityBuilder);

            workflowInfo.NestedXamlDefinition = resultWriter.ToString();
        }

        private Dictionary<string, CommandInfo> resolvedCommands = new Dictionary<string, CommandInfo>(StringComparer.OrdinalIgnoreCase);

        private CommandInfo ResolveCommand(string commandName)
        {
            CommandInfo resolvedCommand = null;

            if (resolvedCommands.TryGetValue(commandName, out resolvedCommand))
            {
                return resolvedCommand;
            }

            var scopeEntry = scope.LookupCommand(commandName);
            if (scopeEntry != null)
            {
                resolvedCommands[commandName] = null;
                return null;
            }

            // Make sure we only call the cmdlet.
            var getCommandCommand = new CmdletInfo("Get-Command", typeof(GetCommandCommand));
            do
            {
                // Look in the session for the command name.
                invoker.Commands.Clear();
                invoker.AddCommand(getCommandCommand)
                    .AddParameter("Name", commandName)
                    .AddParameter("ErrorAction", ActionPreference.Ignore);
                var result = invoker.Invoke<CommandInfo>();

                // There was a result - this is a command from the session.
                // Resolve an aliases, and figure out the actual command name.
                if (result.Count > 0)
                {
                    foreach (CommandInfo currentResult in result)
                    {
                        string compareName = currentResult.Name;
                        string moduleQualifiedName = currentResult.ModuleName + "\\" + currentResult.Name;

                        if (currentResult.CommandType == CommandTypes.Application)
                        {
                            compareName = System.IO.Path.GetFileNameWithoutExtension(currentResult.Name);
                        }

                        if (String.Equals(commandName, currentResult.Name, StringComparison.OrdinalIgnoreCase) ||
                            String.Equals(commandName, moduleQualifiedName, StringComparison.OrdinalIgnoreCase) ||
                            String.Equals(commandName, compareName, StringComparison.OrdinalIgnoreCase))
                        {
                            resolvedCommand = currentResult;

                            if (resolvedCommand is AliasInfo)
                            {
                                commandName = resolvedCommand.Definition;
                            }
                            break;
                        }
                    }
                }

            // Also chase down aliases
            } while (resolvedCommand is AliasInfo);

            resolvedCommands[commandName] = resolvedCommand;
            return resolvedCommand;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability",
            "CA2001:AvoidCallingProblematicMethods",
            MessageId = "System.Reflection.Assembly.LoadFrom")]
        private static void PopulateActivityStaticMap()
        {
            string[] defaultAssemblies = {
                "Microsoft.PowerShell.Activities",
                "Microsoft.PowerShell.Core.Activities",
                "Microsoft.PowerShell.Diagnostics.Activities",
                "Microsoft.PowerShell.Management.Activities",
                "Microsoft.PowerShell.Security.Activities",
                "Microsoft.PowerShell.Utility.Activities",
                "Microsoft.WSMan.Management.Activities",
                "Microsoft.PowerShell.Workflow.ServiceCore"
            };

            // Add our default assemblies
            foreach (string assembly in defaultAssemblies)
            {
                Assembly loadedAssembly = null;

                try
                {
                    loadedAssembly = Assembly.Load(assembly);
                }
                catch (IOException)
                {
                    string newAssembly = assembly + ", Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
                    loadedAssembly = Assembly.Load(newAssembly);
                }

                ProcessAssembly(loadedAssembly, staticProcessedActivityLibraries, staticActivityMap);
            }
        }

        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom")]
        internal static Dictionary<string, Type> GetActivityMap(IEnumerable<string> assembliesToScan, out HashSet<string> processedActivityLibraries)
        {
            // Guard access to private static variables.  IEnumerable use is not thread safe.
            Dictionary<string, Type> activityMap = null;
            lock (staticProcessedActivityLibraries)
            {
                activityMap = new Dictionary<string, Type>(staticActivityMap, StringComparer.OrdinalIgnoreCase);

                if (assembliesToScan == null)
                {
                    processedActivityLibraries = new HashSet<string>(staticProcessedActivityLibraries, StringComparer.OrdinalIgnoreCase);
                    return activityMap;
                }

                processedActivityLibraries = new HashSet<string>(staticProcessedActivityLibraries, StringComparer.OrdinalIgnoreCase);
            }

            foreach (string assembly in assembliesToScan)
            {
                Assembly loadedAssembly = null;

                try
                {
                    // Try first from the GAC
                    loadedAssembly = Assembly.Load(assembly);
                }
                catch (IOException)
                {
                    try
                    {
                        // And second by path
                        loadedAssembly = Assembly.LoadFrom(assembly);
                    }
                    catch (IOException)
                    {
                        // Finally, by relative path
                        if (CoreRunspaces.Runspace.DefaultRunspace != null)
                        {
                            using (System.Management.Automation.PowerShell nestedPs =
                                System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace))
                            {
                                nestedPs.AddCommand("Get-Location");
                                PathInfo result = nestedPs.Invoke<PathInfo>()[0];

                                string currentLocation = result.ProviderPath;

                                try
                                {
                                    loadedAssembly = Assembly.LoadFrom(Path.Combine(currentLocation, assembly));
                                }
                                catch (IOException)
                                {
                                }
                            }
                        }
                    }
                }

                // Throw an error if we weren't able to load the assembly.
                if (loadedAssembly == null)
                {
                    string error = String.Format(CultureInfo.InvariantCulture, ActivityResources.CouldNotLoadRequiredAssembly, assembly);
                    throw new IOException(error);
                }

                ProcessAssembly(loadedAssembly, processedActivityLibraries, activityMap);
            }

            return activityMap;
        }

        private static void ProcessAssembly(Assembly activityAssembly, HashSet<string> processedActivityLibraries, Dictionary<string, Type> activityMap)
        {
            // Remember that we've processed this assembly (and therefore
            // the module).
            string assemblyName = activityAssembly.ManifestModule.Name;
            int activityIndex = assemblyName.IndexOf(".Activities.dll", StringComparison.OrdinalIgnoreCase);
            if(activityIndex > 0)
            {
                string moduleName = assemblyName.Substring(0, activityIndex);
                processedActivityLibraries.Add(moduleName);
            }

            bool isRunningInConstrainedLanguage = false;

            // If we're in ConstrainedLanguage mode, block inline XAML as it can't be verified.
            CoreRunspaces.Runspace currentRunspace = (CoreRunspaces.Runspace)CoreRunspaces.Runspace.DefaultRunspace;
            if (currentRunspace != null)
            {
                if (currentRunspace.SessionStateProxy.LanguageMode == PSLanguageMode.ConstrainedLanguage)
                {
                    isRunningInConstrainedLanguage = true;
                }
            }


            foreach (Type type in activityAssembly.GetTypes())
            {
                // Process activities exported by this assembly
                if (!typeof(System.Activities.Activity).IsAssignableFrom(type))
                {
                    continue;
                }

                // Ensure it derives from PSActivity if it's in constrained language,
                // as arbitrary workflow activities (like VisualBasicValue) may be
                // unsafe
                if (isRunningInConstrainedLanguage &&
                    (!typeof(Microsoft.PowerShell.Activities.PSActivity).IsAssignableFrom(type)))
                {
                    continue;
                }

                // If the activity map already contains an assembly with this name,
                // then set the value to NULL. During processing, we will recognize
                // this and throw an exception about this type name being ambiguous.
                if (activityMap.ContainsKey(type.Name))
                {
                    activityMap[type.Name] = null;
                }
                else
                {
                    activityMap[type.Name] = type;
                }

                // Update the activity map with the full name to support users specifying
                // an activity with full name.
                // If the activity map already contains an assembly with this full name, 
                // set the value to NULL as is done for Name key.
                if (activityMap.ContainsKey(type.FullName))
                {
                    activityMap[type.FullName] = null;
                }
                else
                {
                    activityMap[type.FullName] = type;
                }

                // Update the activity map with both the
                // short name and full name. By convention, activities generated from commands
                // get put in a namespace with ".Activities" after the module name. Remove the
                // ".Activities" bit so that module-qualified command lookups work - i.e.:
                // Microsoft.PowerShell.Utility\Write-Output
                activityMap[type.Namespace.Replace(".Activities", "") + "\\" + type.Name] = type;
            }
        }

        private void GeneratePersist(CommandAst commandAst)
        {
            // Verify they've used the correct syntax
            if (commandAst.CommandElements.Count > 1)
            {
                ReportError("CheckpointWorkflowSyntax", ActivityResources.CheckpointWorkflowSyntax, commandAst.Extent);
            }

            GenerateActivityCall(null, null, typeof(Microsoft.PowerShell.Activities.PSPersist), null, null, null, null, commandAst.Extent);
        }

        private void GenerateSuspend(CommandAst commandAst)
        {
            StaticBindingResult bindingResult = StaticParameterBinder.BindCommand(commandAst, false);

            // Verify they've used the correct syntax
            if (bindingResult.BoundParameters.Count == 0)
            {
                GenerateActivityCall(null, null, typeof(Microsoft.PowerShell.Activities.Suspend), null, null, null, null, commandAst.Extent);
                return;
            }

            if (bindingResult.BoundParameters.Count == 1 && bindingResult.BoundParameters.ContainsKey("Label"))
            {
                Object label = bindingResult.BoundParameters["Label"].ConstantValue;
                if (label == null)
                {
                    ReportError("SuspendWorkflowSyntax", ActivityResources.SuspendWorkflowSyntax, commandAst.Extent);
                    return;
                }

                CommandArgumentInfo argumentInfo = new CommandArgumentInfo();

                argumentInfo.Value = label.ToString();
                argumentInfo.IsLiteral = true;

                // Prepare the parameter used
                Dictionary<string, CommandArgumentInfo> finalParameters = new Dictionary<string, CommandArgumentInfo>(StringComparer.OrdinalIgnoreCase);
                finalParameters["Label"] = argumentInfo;

                GenerateActivityCall(null, null, typeof(Microsoft.PowerShell.Activities.Suspend), null, finalParameters, null, null, commandAst.Extent);

                return;
            }

            ReportError("SuspendWorkflowSyntax", ActivityResources.SuspendWorkflowSyntax, commandAst.Extent);
        }

        private void GenerateInvokeExpression(CommandAst commandAst)
        {
            StaticBindingResult bindingResult = StaticParameterBinder.BindCommand(commandAst, false);

            // Verify they've used the correct syntax
            if (bindingResult.BoundParameters.Count != 2)
            {
                ReportError("InvokeExpressionSyntaxTooManyParameters", ActivityResources.InvokeExpressionSyntax, commandAst.Extent);
                return;
            }

            // Validate the 'Language' parameter
            if (bindingResult.BoundParameters.ContainsKey("Language"))
            {
                ParameterBindingResult language = bindingResult.BoundParameters["Language"];
                Object constantLanguage = language.ConstantValue;
                if (constantLanguage == null)
                {
                    ReportError("InvokeExpressionSyntaxLanguageNotConstant", ActivityResources.InvokeExpressionSyntax, commandAst.Extent);
                    return;
                }

                if (!String.Equals("XAML", constantLanguage.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    ReportError("MustUseXamlLanguage", ActivityResources.MustUseXamlLanguage, language.Value.Extent);
                }
            }
            else
            {
                ReportError("InvokeExpressionSyntaxLanguageMissing", ActivityResources.InvokeExpressionSyntax, commandAst.Extent);
                return;
            }

            // Validate the 'Command' parameter
            if (bindingResult.BoundParameters.ContainsKey("Command"))
            {
                ParameterBindingResult command = bindingResult.BoundParameters["Command"];
                Object constantCommand = command.ConstantValue;
                if (constantCommand == null)
                {
                    ReportError("InvokeExpressionSyntaxCommandNotConstant", ActivityResources.InvokeExpressionSyntax, commandAst.Extent);
                    return;
                }

                // If we're in ConstrainedLanguage mode, block inline XAML as it can't be verified.
                CoreRunspaces.Runspace currentRunspace = (CoreRunspaces.Runspace) CoreRunspaces.Runspace.DefaultRunspace;
                if (currentRunspace != null)
                {
                    if (currentRunspace.SessionStateProxy.LanguageMode == PSLanguageMode.ConstrainedLanguage)
                    {
                        ReportError("InlineXamlNotSupported", ActivityResources.InlineXamlNotSupported, commandAst.Extent);
                        return;
                    }
                }

                string inlineXaml = constantCommand.ToString();
                bodyElements.Add(inlineXaml);
            }
            else
            {
                ReportError("InvokeExpressionSyntaxCommandMissing", ActivityResources.InvokeExpressionSyntax, commandAst.Extent);
                return;
            }
        }

        private void GenerateStartSleep(CommandAst commandAst)
        {
            CommandInfo command = ResolveCommand("Start-Sleep");
            Dictionary<string, CommandArgumentInfo> parameters = GetAndResolveParameters(commandAst, true);

            // Verify they've used the correct syntax
            if (parameters.Count != 1)
            {
                ReportError("StartSleepSyntaxTooManyParameters", ActivityResources.StartSleepSyntax, commandAst.Extent);
                return;
            }

            CommandArgumentInfo argumentInfo = new CommandArgumentInfo();

            if (parameters.ContainsKey("Seconds"))
            {
                // 'Seconds' parameter
                string paramValue = parameters["Seconds"].Value.ToString();
                if (paramValue == null)
                {
                    ReportError("StartSleepSyntaxCouldNotParseSeconds", ActivityResources.StartSleepSyntax, commandAst.Extent);
                    return;
                }

                int secondsInt = -1;
                if (Int32.TryParse(paramValue, out secondsInt))
                {
                    // Create literal TimeSpan argument type from seconds.
                    argumentInfo.Value = new System.TimeSpan(0, 0, secondsInt).ToString();
                    argumentInfo.IsLiteral = true;
                }
                else
                {
                    // Create expression argument with conversion from seconds to ticks for TimeSpan type.
                    argumentInfo.Value = String.Format(CultureInfo.InvariantCulture,
                        @"([Int64]({0}) * 10000000)", paramValue);

                    argumentInfo.IsLiteral = false;
                }
            }
            else if (parameters.ContainsKey("Milliseconds"))
            {
                // 'Milliseconds' parameter
                string paramValue = parameters["Milliseconds"].Value.ToString();
                if (paramValue == null)
                {
                    ReportError("StartSleepSyntaxCouldNotParseSeconds", ActivityResources.StartSleepSyntax, commandAst.Extent);
                    return;
                }

                int mSecondsInt = -1;
                if (Int32.TryParse(paramValue, out mSecondsInt))
                {
                    // Create literal TimeSpan argument type from milliseconds.
                    argumentInfo.Value = new System.TimeSpan(0, 0, 0, 0, mSecondsInt).ToString();
                    argumentInfo.IsLiteral = true;
                }
                else
                {
                    // Create expression argument with conversion from milliseconds to ticks for TimeSpan type.
                    argumentInfo.Value = String.Format(CultureInfo.InvariantCulture,
                        @"([Int64]({0}) * 10000)", paramValue);

                    argumentInfo.IsLiteral = false;
                }
            }
            else
            {
                ReportError("StartSleepSyntaxSecondsMissing", ActivityResources.StartSleepSyntax, commandAst.Extent);
                return;
            }

            // Prepare the parameter used
            Dictionary<string, CommandArgumentInfo> finalParameters = new Dictionary<string, CommandArgumentInfo>(StringComparer.OrdinalIgnoreCase);
            finalParameters["Duration"] = argumentInfo;

            GenerateActivityCall(null, null, typeof(System.Activities.Statements.Delay), null, finalParameters, null, null, commandAst.Extent);
        }

        private void GenerateNewObject(CommandAst commandAst)
        {
            CommandInfo command = ResolveCommand("New-Object");
            Dictionary<string, CommandArgumentInfo> parameters = GetAndResolveParameters(commandAst, true);

            // Verify they've used the correct syntax
            if ((parameters.Count == 0) || (parameters.Count > 2))
            {
                ReportError("NewObjectSyntaxTooManyParameters", ActivityResources.NewObjectSyntax, commandAst.Extent);
                return;
            }

            if (parameters.ContainsKey("TypeName"))
            {
                // 'TypeName' parameter
                string paramValue = parameters["TypeName"].OriginalValue.ToString();
                Type outputType = ResolveTypeFromParameterValue(commandAst, paramValue);
                if (outputType == null)
                {
                    // Error reporting already handled by ResolveTypeFromParameterValue
                    return;
                }

                string expression = GetPowerShellValueExpression(commandAst);
                GeneratePowerShellValue(outputType, expression, false, true);
            }
            else
            {
                ReportError("NewObjectSyntaxMissingTypeName", ActivityResources.NewObjectSyntax, commandAst.Extent);
            }
        }

        private Type ResolveTypeFromParameterValue(CommandAst commandAst, string paramValue)
        {
            Type outputType = null;

            try
            {
                outputType = LanguagePrimitives.ConvertTo<Type>(paramValue);
            }
            catch (PSInvalidCastException)
            {
                // Find the actual parameter AST
                Ast resultAst = commandAst;
                foreach (CommandElementAst commandElement in commandAst.CommandElements)
                {
                    var parameter = commandElement as CommandParameterAst;
                    if (parameter != null)
                    {
                        if (String.Equals("TypeName", parameter.ParameterName, StringComparison.OrdinalIgnoreCase))
                        {
                            resultAst = parameter;
                            break;
                        }
                    }
                }

                string error = String.Format(CultureInfo.InvariantCulture, ActivityResources.NewObjectCouldNotFindType, paramValue);
                ReportError("NewObjectCouldNotFindType", error, resultAst.Extent);
            }

            return outputType;
        }

        // We need to convert command arguments to their script representation
        // for the following scenario:
        //     Get-Process -Name Foo,Bar
        // When we call the Get-Process activity, we need to coerce its arguments
        // to the type expected by the parameter - for example, string[].
        // To do this, we generate basically the following XAML:
        //     <GetProcessActivity>
        //         <GetProcessActivity.Name>
        //             <PowerShellValue OutputType="String[]" Expression = " Foo, Bar " />
        //         </...>
        //     </...>
        //     Get-Process -Name $arguments
        // But since the arguments were retrieved in parameter parsing mode, we need
        // to quote strings (since they may be considered commands) before supplying
        // them as a PowerShell expression.
        private string ProcessCommandArgument(CommandElementAst argument)
        {
            if (argument == null)
                return null;
            string argumentValue = "";

            ArrayLiteralAst arrayLiteralArgument = argument as ArrayLiteralAst;

            if (arrayLiteralArgument != null)
            {
                foreach (ExpressionAst expression in arrayLiteralArgument.Elements)
                {
                    if (!String.IsNullOrEmpty(argumentValue))
                        argumentValue += ",";

                    argumentValue += ProcessCommandArgumentElement(expression);
                }
            }
            else
            {
                argumentValue = ProcessCommandArgumentElement(argument);
            }

            return argumentValue;
        }

        private string ProcessCommandArgumentElement(CommandElementAst argument)
        {
            // If it's a string constant, single quote it.
            StringConstantExpressionAst stringAst = argument as StringConstantExpressionAst;
            if (stringAst != null && stringAst.StringConstantType == StringConstantType.BareWord)
            {
                string bareContent = argument.Extent.Text.Replace("'", "''");
                bareContent = "'" + bareContent + "'";

                return bareContent;
            }

            // If it's an expandable string, double quote it
            ExpandableStringExpressionAst expandableString = argument as ExpandableStringExpressionAst;
            if (expandableString != null && expandableString.StringConstantType == StringConstantType.BareWord)
            {
                string bareContent = argument.Extent.Text.Replace("\"", "`\"");
                bareContent = "\"" + bareContent + "\"";

                return bareContent;
            }

            return argument.Extent.Text;
        }

        private void ProcessInlineScriptAst(CommandAst commandAst)
        {
            if (commandAst.CommandElements.Count == 1)
            {
                ReportError("InlineScriptSyntaxTooFewParameters", ActivityResources.InlineScriptSyntax, commandAst.Extent);
                return;
            }

            var scriptBlock = commandAst.CommandElements[1] as ScriptBlockExpressionAst;
            bool scriptBlockExists = scriptBlock != null;
            if (!scriptBlockExists)
            {
                ReportError("InlineScriptMissingStatementBlock", ActivityResources.InlineScriptSyntax, commandAst.Extent);
                return;
            }

            List<Ast> variableExprsWithWorkflowPrefix = ContainVariablesWithWorkflowPrefix(scriptBlock);
            if (variableExprsWithWorkflowPrefix.Any())
            {
                ReportError("CannotUseWorkflowPrefixInInlineScript", ActivityResources.CannotUseWorkflowPrefixInInlineScript, variableExprsWithWorkflowPrefix[0].Extent);
            }

            StaticBindingResult bindingResult = StaticParameterBinder.BindCommand(commandAst, false);
            
            // Remove the first positional element (the script) if there is one
            if (bindingResult.BoundParameters.ContainsKey("0"))
            {
                bindingResult.BoundParameters.Remove("0");
            }

            bool isCommandSpecified = bindingResult.BoundParameters.ContainsKey("Command");
            bool isCommandNameSpecified = bindingResult.BoundParameters.ContainsKey("CommandName");
            bool isParametersSpecified = bindingResult.BoundParameters.ContainsKey("Parameters");
            var sbExpr = commandAst.CommandElements[1] as ScriptBlockExpressionAst;

            IScriptExtent errorParameterExtent = null;
            if (isCommandSpecified)
            {
                errorParameterExtent = bindingResult.BoundParameters["Command"].Value.Extent;
            }
            else if (isCommandNameSpecified)
            {
                errorParameterExtent = bindingResult.BoundParameters["CommandName"].Value.Extent;
            }
            else if (isParametersSpecified)
            {
                errorParameterExtent = bindingResult.BoundParameters["Parameters"].Value.Extent;
            }

            if (errorParameterExtent != null)
            {
                ReportError("InlineScriptParameterNotSupported", ActivityResources.InlineScriptSyntax, errorParameterExtent);
            }

            Dictionary<string, CommandArgumentInfo> convertedParameters = ResolveParameters(bindingResult);
            String scriptToProcess = sbExpr.ScriptBlock.EndBlock.Extent.Text;
            GenerateInlineScript(scriptToProcess, null, convertedParameters, commandAst.Extent, true);
        }

        private List<Ast> ContainVariablesWithWorkflowPrefix(Ast scriptBlock)
        {
            Func<Ast, bool> variableExprWithWorkflowPrefixSearcher =
                (ast) =>
                    {
                        var variableExpr = ast as VariableExpressionAst;
                        if (variableExpr == null) { return false; }

                        return variableExpr.VariablePath.UserPath.StartsWith("WORKFLOW:", StringComparison.OrdinalIgnoreCase);
                    };

            return scriptBlock.FindAll(variableExprWithWorkflowPrefixSearcher, searchNestedScriptBlocks: true).ToList();
        }

        private const string PSRequiredModules = "PSRequiredModules";
        private void GenerateInlineScript(string inlineScript, string displayName, Dictionary<string, CommandArgumentInfo> parameters, IScriptExtent extent, bool isExplicit)
        {
            Dictionary<string, CommandArgumentInfo> arguments = new Dictionary<string, CommandArgumentInfo>(StringComparer.OrdinalIgnoreCase);
            var argumentInfo = new CommandArgumentInfo { Value = inlineScript, IsLiteral = true };
            arguments.Add("Command", argumentInfo);

            if (displayName != null)
            {
                var displayNameArgInfo = new CommandArgumentInfo { Value = displayName, IsLiteral = true };
                arguments.Add("DisplayName", displayNameArgInfo);
            }

            if (!isExplicit)
            {
                string moduleQualifier = GetModuleQualifier(inlineScript);
                if (moduleQualifier != null)
                {
                    CommandArgumentInfo requiredModuleArgument = new CommandArgumentInfo();
                    requiredModuleArgument.Value = string.Format(CultureInfo.InvariantCulture,
                        "'{0}'", moduleQualifier);
                    requiredModuleArgument.IsLiteral = false;
                    arguments[PSRequiredModules] = requiredModuleArgument;
                }
            }

            // Copy the parameters
            if (parameters != null)
            {
                foreach (string key in parameters.Keys)
                {
                    arguments.Add(key, parameters[key]);
                }
            }

            GenerateActivityCall(null, "InlineScript", typeof(InlineScript), null, arguments, null, null, extent);
        }

        private string GetModuleQualifier(string command)
        {
            if (string.IsNullOrEmpty(command))
                return null;

            int pos = command.IndexOf('\\');

            if (pos < 0)
                return null;

            System.Management.Automation.Language.Token[] tokens = null;
            System.Management.Automation.Language.ParseError[] errors = null;

            System.Management.Automation.Language.Parser.ParseInput(command, out tokens, out errors);
            if (tokens == null || errors == null || tokens.Length == 0 || errors.Length > 0)
                return null;

            command = tokens[0].Text;
            pos = command.IndexOf('\\');

            if (pos > 0 && pos < command.Length - 1)
                return command.Substring(0, pos);

            return null;
        }

        private void SetVariableArgumentValue(CommandArgumentInfo argument, ref object argumentValue)
        {
            System.Diagnostics.Debug.Assert(argumentValue != null, "argumentValue should not be null"); 
            
            if (argument != null)
            {
                argumentValue = "$" + argument.OriginalValue;
            }
            else if (argumentValue is CommandElementAst)
            {
                var cmdElement = argumentValue as CommandElementAst;
                argumentValue = "$" + cmdElement;
            }
        }

        private void GenerateActivityCall(
            CommandInfo commandInfo,
            string invocationName,
            Type activityType,
            DynamicActivity activityInstance,
            Dictionary<string, CommandArgumentInfo> arguments,
            StaticBindingResult argumentResolution,
            StaticBindingResult syntacticResolution,
            IScriptExtent extent
            )
        {
            bool isNestedWorkflow = activityInstance != null;

            // If we're in a pipeline, ensure that this activity derives from PipelineEnabledActivity
            if (this.isVisitingPipeline)
            {
                if ((activityType != null) && (! typeof(PipelineEnabledActivity).IsAssignableFrom(activityType)))
                {
                    string activityName = extent.ToString();
                    if (commandInfo != null)
                    {
                        activityName = commandInfo.Name;
                    }

                    ReportError("ActivityNotSupportedInPipeline",
                        String.Format(CultureInfo.InvariantCulture, ActivityResources.ActivityNotSupportedInPipeline, activityName), extent);
                }
            }

            // Generate symbolic information for this activity call.
            if (!String.Equals("SetPSWorkflowData", invocationName, StringComparison.OrdinalIgnoreCase))
            {
                GenerateSymbolicInformation(extent);
            }

            if ((arguments != null) && arguments.ContainsKey("Result"))
            {
                if(! this.ValidateOnly)
                {
                    ReportError("CannotSpecifyResultArgument", ActivityResources.CannotSpecifyResultArgument, extent);
                }
            }

            string attributes = string.Empty;
            if (string.Equals("InlineScript", invocationName))
            {
                if (arguments.ContainsKey("Command"))
                {
                    CommandArgumentInfo argument = arguments["Command"];
                    arguments.Remove("Command");
                    string argumentValue = argument.Value.ToString();
                    if (argumentValue == null)
                    {
                        argumentValue = string.Empty;
                    }
                    attributes = " Command=\"" +
                            EncodeStringArgument(argumentValue, false) +
                            "\"";
                }
            }

            Dictionary<string, string> propertiesToEmulate = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Check if the error stream is redirected for the current command
            if (this.mergeErrorToOutput)
            {
                // Check if the dynamic activity instance contains the properties we need
                Type parameterType = null;
                if (!IsParameterAvailable(activityType, activityInstance, "MergeErrorToOutput", false, extent, out parameterType))
                {
                    // If this is a workflow calling workflow, we'll emulate the property
                    if (isNestedWorkflow)
                    {
                        propertiesToEmulate.Add("MergeErrorToOutput", "[True]");
                    }
                    else
                    {
                        // Don't report on parameter errors from nested workflows, as they require compilation
                        if (!this.ValidateOnly)
                        {
                            ReportError("CannotMergeErrorToOutput",
                                String.Format(CultureInfo.InvariantCulture, ActivityResources.CannotMergeErrorToOutput, invocationName), extent);
                        }
                    }
                }
                else
                {
                    attributes += @" MergeErrorToOutput = ""True""";
                }
            }

            // Process the generic activity type
            Dictionary<string, Type> genericTypeMap = null;
            if (activityType.IsGenericType && arguments.ContainsKey(GenericTypesKey))
            {
                var genericTypes = (Type[]) arguments[GenericTypesKey].Value;
                var concatenatedTypeNames = new StringBuilder();
                foreach (Type type in genericTypes)
                {
                    string convertedTypeName = GetConvertedTypeName(type);
                    concatenatedTypeNames.Append(convertedTypeName);
                    concatenatedTypeNames.Append(", ");
                }
                concatenatedTypeNames.Remove(concatenatedTypeNames.Length - 2, 2);
                attributes += String.Format(CultureInfo.InvariantCulture, @" x:TypeArguments=""{0}""", concatenatedTypeNames);
                arguments.Remove(GenericTypesKey);

                genericTypeMap = GetGenericTypeMap(activityType, genericTypes);
            }

            // Create an assembly name reference
            string friendlyName = GetFriendlyName(invocationName, activityType);

            if ((commandInfo != null) && (invocationName == null))
            {
                invocationName = commandInfo.Name;
            }

            if (String.IsNullOrEmpty(invocationName))
            {
                invocationName = extent.Text;
            }

            // Generate the call to the activity

            // Capture the result if there is any output variable defined
            var currentVariable = GetVariableToUse();
            string variableToUse = null;
            bool isAggregatingVariable = false;
            if (currentVariable != null)
            {
                variableToUse = currentVariable.VariableName;
                isAggregatingVariable = currentVariable.IsAggregatingVariable;
            }
            VariableDefinition variableToUseDefinition = null;

            if (!String.IsNullOrEmpty(variableToUse))
            {
                variableToUseDefinition = GetVariableDefinition(variableToUse);
                variableToUseDefinition = variableToUseDefinition ?? members[variableToUse];
            }
            
            string appendOutput = String.Empty;
            string actualAggregatingVariable = String.Empty;
            bool needAggregationVariable = false;
            bool needConversionVariable = false;

            if (!String.IsNullOrEmpty(variableToUse))
            {
                if (activityType.Equals(typeof(System.Activities.Statements.Delay)))
                {
                    if (!isAggregatingVariable)
                    {
                        ReportError("CannotAssignStartSleepToVariable", ActivityResources.CannotAssignStartSleepToVariable, extent);
                    }
                    else
                    {
                        // ignore the storagescope variable for start-sleep
                        attributes = null;
                    }
                }
                else
                {
                    // Activities derived from Activity<TResult> have two Result properties, one from the Activity<TResult>
                    // the other from ActivityWithResult. So we cannot handle the property Result as a regular property.
                    Type[] genericArgumentTypes = null;
                    bool isActivityWithResult = activityType.IsGenericType && IsAssignableFromGenericType(typeof(Activity<>), activityType, out genericArgumentTypes);
                    Type activityOutputType = null;

                    // Check if the dynamic activity instance contains the properties we need
                    Type parameterType = null;
                    if (!IsParameterAvailable(activityType, activityInstance, "Result", isActivityWithResult, extent, out parameterType))
                    {
                        // If this is a workflow calling workflow, we'll emulate the property
                        if (isNestedWorkflow)
                        {
                            propertiesToEmulate.Add("Result", "[" + variableToUse + "]");
                        }
                        else
                        {
                            if (!this.ValidateOnly)
                            {
                                ReportError("ActivityDoesNotContainResultProperty",
                                    String.Format(CultureInfo.InvariantCulture, ActivityResources.ActivityDoesNotContainResultProperty, variableToUse, invocationName), extent);
                            }
                            else
                            {
                                return;
                            }
                        }
                    }

                    // Get the activity output type
                    if (parameterType != null)
                    {
                        activityOutputType = GetActualPropertyType(
                            isActivityWithResult ? genericArgumentTypes[genericArgumentTypes.Length - 1] : parameterType,
                            genericTypeMap, "Result", extent);
                    }

                    Type unused = null;

                    // Check if we need to make a temporary variable for either aggregation, or conversion.
                    if (isAggregatingVariable &&
                        !IsParameterAvailable(activityType, activityInstance, "AppendOutput", false, extent, out unused))
                    {
                        needAggregationVariable = true;
                    }
                    if ((variableToUseDefinition.Type != activityOutputType) && (! isNestedWorkflow))
                    {
                        needConversionVariable = true;
                    }

                    // If we need a temporary variable, create it.
                    if (needAggregationVariable || needConversionVariable)
                    {
                        Type tempVarType = isNestedWorkflow
                            ? typeof(PSDataCollection<PSObject>)
                            : activityOutputType;

                        // Define the temporary variable
                        string tempVarName = GenerateUniqueVariableName("__AggregatingTempVar");
                        DefineVariable(tempVarName, tempVarType, extent, null);

                        if (isNestedWorkflow)
                        {
                            // We are calling a nested workflow. we need to create a new instance of the storage variable.
                            try
                            {
                                EnterStorage(tempVarName, false);
                                string constructor = "New-Object -Type System.Management.Automation.PSDataCollection[PSObject]";
                                GeneratePowerShellValue(typeof(System.Management.Automation.PSDataCollection<PSObject>), constructor, false, true);
                            }
                            finally
                            {
                                LeaveStorage();
                            }
                        }

                        actualAggregatingVariable = variableToUse;
                        variableToUse = tempVarName;
                    }

                    if (isActivityWithResult)
                    {
                        appendOutput = String.Format(CultureInfo.InvariantCulture, " Result=\"[{0}]\"", variableToUse);
                    }
                    else
                    {
                        if (!propertiesToEmulate.ContainsKey("Result"))
                        {
                            arguments["Result"] = new CommandArgumentInfo() { Value = "$" + variableToUse };
                        }

                        if (isAggregatingVariable && !needAggregationVariable) { appendOutput = AppendOutputTemplate; }
                    }
                }
            }

            string getPSWorkflowDataFriendlyName = GetFriendlyName("GetPSWorkflowData", typeof(GetPSWorkflowData<Object>));
            string setPSWorkflowDataFriendlyName = GetFriendlyName("SetPSWorkflowData", typeof(SetPSWorkflowData));

            // If we have any properties to emulate, save the old values and set the new ones
            if (propertiesToEmulate.Count > 0)
            {
                WriteLine("<TryCatch>");
                IndentLevel();
                WriteLine("<TryCatch.Try>");
                IndentLevel();
                WriteLine("<Sequence>");

                IndentLevel();

                // Prepare the variables
                foreach (string propertyToEmulate in propertiesToEmulate.Keys)
                {
                    // Create variables for the saved values if we need them
                    string variableName = "PSSaved_" + propertyToEmulate;
                    if (!VariableDefinedInCurrentScope(variableName))
                    {
                        DefineVariable(variableName, typeof(Object), extent, null);
                    }

                    // Save the existing variable, and set the new one.
                    WriteLine(@"<" + getPSWorkflowDataFriendlyName + @" x:TypeArguments=""x:Object"" OtherVariableName=""" +
                        propertyToEmulate + @""" Result=""[" + variableName + @"]"" VariableToRetrieve=""Other"" />");
                    WriteLine(@"<" + setPSWorkflowDataFriendlyName + @" PSRemotingBehavior=""{x:Null}"" OtherVariableName=""" +
                        propertyToEmulate + @""" Value=""" + propertiesToEmulate[propertyToEmulate] + @""" />");
                }
            }

            WriteLine("<" + friendlyName + attributes + appendOutput + ">");
            IndentLevel();

            // Now process the arguments
            if (arguments != null)
            {
                // Attempt to add any parameter defaults if they have been supplied from preference variables
                UpdateArgumentsFromPreferenceVariables(activityType, arguments, activityInstance);
                var boundParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                List<string> argumentsToProcess = arguments.Keys.ToList<string>();
                if((argumentResolution != null) && (argumentResolution.BindingExceptions != null))
                {
                    argumentsToProcess.AddRange(argumentResolution.BindingExceptions.Keys);
                }

                foreach (string parameterNameIterator in argumentsToProcess)
                {
                    string parameterName = parameterNameIterator;

                    CommandArgumentInfo argument = null;
                    object argumentValue = null;
                    bool isLiteral = false;
                    bool wasSpecifiedAsSwitch = false;

                    if (arguments.ContainsKey(parameterName))
                    {
                        argument = arguments[parameterName];
                        argumentValue = argument.Value;
                        isLiteral = argument.IsLiteral;

                        // Detect a boolean that was used like a switch
                        if ((syntacticResolution != null) &&
                            ((string)argumentValue == "-" + parameterName) &&
                            (syntacticResolution.BoundParameters[parameterName].ConstantValue is bool))
                        {
                            argument = null;
                            argumentValue = syntacticResolution.BoundParameters[parameterName].ConstantValue;
                            isLiteral = true;
                            wasSpecifiedAsSwitch = true;
                        }
                    }
                    else if (syntacticResolution != null)
                    {
                        // This was not bound successfully, look in the syntactic
                        // version of the bound parameters.
                        argumentValue = syntacticResolution.BoundParameters[parameterName].Value;
                        isLiteral = (syntacticResolution.BoundParameters[parameterName].ConstantValue != null);
                    }

                    // If this is 'PipelineVariable', ignore this argument as it is implemented
                    // by workflow compilation itself.
                    if (String.Equals("PipelineVariable", parameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // If the argument value is NULL, then this was a switch parameter and
                    // we change it to $true
                    if (argumentValue == null)
                    {
                        argumentValue = "$true";
                    }

                    // If this is ErrorVariable or WarningVariable, switch it to PSError and PSWarning,
                    // respectively
                    if (String.Equals("ErrorVariable", parameterName, StringComparison.OrdinalIgnoreCase) ||
                        String.Equals("EV", parameterName, StringComparison.OrdinalIgnoreCase))

                    {
                        parameterName = "PSError";
                        SetVariableArgumentValue(argument, ref argumentValue);
                    }

                    if (String.Equals("WarningVariable", parameterName, StringComparison.OrdinalIgnoreCase) ||
                        String.Equals("WV", parameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        parameterName = "PSWarning";
                        SetVariableArgumentValue(argument, ref argumentValue);
                        isLiteral = true;
                    }

                    if (String.Equals("InformationVariable", parameterName, StringComparison.OrdinalIgnoreCase) ||
                        String.Equals("IV", parameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        parameterName = "PSInformation";
                        SetVariableArgumentValue(argument, ref argumentValue);
                        isLiteral = true;
                    }

                    Type propertyType = GetPropertyType(activityType, activityInstance, ref parameterName, extent);

                    // Flatten Nullable arguments down to their base
                    if ((propertyType != null) && propertyType.IsGenericType && (propertyType.GetGenericTypeDefinition().IsAssignableFrom(typeof(Nullable<>))))
                    {
                        propertyType = propertyType.GetGenericArguments()[0];
                    }

                    // If we can't find the property name, we may have done parameter avoidance and renamed the
                    // activity's parameter so that it didn't conflict with something in workflow. Try to
                    // undo it and try again.
                    if ((invocationName != null) && (propertyType == null))
                    {
                        // If it has a noun, trim it to include only the noun
                        string avoidanceQualifier = invocationName;
                        int nounIndex = avoidanceQualifier.IndexOf('-');
                        if (nounIndex >= 0)
                        {
                            avoidanceQualifier = avoidanceQualifier.Substring(nounIndex + 1);
                        }

                        // Try again
                        parameterName = avoidanceQualifier + parameterNameIterator;
                        propertyType = GetPropertyType(activityType, activityInstance, ref parameterName, extent);
                    }

                    // If we couldn't find the parameter, generate an error.
                    if (propertyType == null)
                    {
                        if ((invocationName != null) && (argumentResolution != null) && argumentResolution.BoundParameters.ContainsKey(parameterName))
                        {
                            StaticBindingError bindingException = argumentResolution.BindingExceptions[parameterName];
                            ReportError("CannotFindParameter", bindingException.BindingException.Message, extent);
                            return;
                        }
                        else
                        {
                            Dictionary<string, Type> availableProperties = GetAvailableProperties(activityType, activityInstance);

                            // Check if this is "Workflows calling workflows"
                            if ((activityInstance != null) && (parameterNameIterator.StartsWith("PS", StringComparison.OrdinalIgnoreCase)))
                            {
                                // Don't report on parameter errors from nested workflows, as they require compilation
                                if (!this.ValidateOnly)
                                {
                                    string error = String.Format(CultureInfo.InvariantCulture, ActivityResources.CouldNotFindParameterNameNested, parameterNameIterator);
                                    ReportError("CouldNotFindWorkflowCommonParameterNameNested", error, extent);
                                }

                                return;
                            }
                            else if (IsNotSupportedCommonParameter(parameterNameIterator))
                            {
                                string error = String.Format(CultureInfo.InvariantCulture, ActivityResources.CommonParameterNotSupported, parameterNameIterator, invocationName);
                                ReportError("CommonParameterNotSupported", error, extent);
                                return;
                            }
                            else if (String.Equals("ComputerName", parameterNameIterator, StringComparison.OrdinalIgnoreCase) &&
                                availableProperties.ContainsKey("PSComputerName"))
                            {
                                ReportError("RemotingHandledByPSComputerName", ActivityResources.RemotingHandledByPSComputerName, extent);
                                return;
                            }
                            else
                            {
                                // They've typed a parameter that we can't map. Since there is no "Get-Command -Syntax" option, dump out all
                                // of the properties for them.
                                List<string> completeProperties = new List<string>(availableProperties.Keys);

                                // If this is a workflow calling workflow, add in the virtual properties that don't exist, since they may not 
                                // have been actually compiled (if they were not used).
                                foreach(PropertyInfo psRemotingProperty in typeof(PSRemotingActivity).GetProperties())
                                {
                                    if (typeof(Argument).IsAssignableFrom(psRemotingProperty.PropertyType))
                                    {
                                        if (!completeProperties.Contains(psRemotingProperty.Name, StringComparer.OrdinalIgnoreCase))
                                        {
                                            completeProperties.Add(psRemotingProperty.Name);
                                        }
                                    }
                                }

                                string[] sortedParameters = completeProperties.ToArray();
                                Array.Sort(sortedParameters);

                                string supportedParametersString = String.Join(", ", sortedParameters);

                                // Check if this is one of our activities that shadow a cmdlet name
                                List<string> shadowedCmdlets = new List<string>() { "Invoke-Command" };
                                if (shadowedCmdlets.Contains(invocationName, StringComparer.OrdinalIgnoreCase))
                                {
                                    string error = String.Format(CultureInfo.InvariantCulture, ActivityResources.CouldNotFindParameterNameShadowedActivity, parameterNameIterator, supportedParametersString);
                                    ReportError("CouldNotFindParameterNameShadowedActivity", error, extent);
                                    return;
                                }
                                else
                                {
                                    // Don't report on parameter errors from nested workflows, as they require compilation
                                    if (!this.ValidateOnly)
                                    {
                                        string error = String.Format(CultureInfo.InvariantCulture, ActivityResources.CouldNotFindParameterName, parameterNameIterator, supportedParametersString);
                                        ReportError("CouldNotFindParameterName", error, extent);
                                    }

                                    return;
                                }
                            }
                        }
                    }
                    
                    // Check if multiple specified parameters can be resolved to the same actual parameter
                    if (!boundParameters.Contains(parameterName))
                    {
                        boundParameters.Add(parameterName);
                    }
                    else
                    {
                        string error = String.Format(CultureInfo.InvariantCulture, ActivityResources.DuplicateParametersNotAllowed, parameterName);
                        ReportError("DuplicateParametersNotAllowed", error, argument.ArgumentAst != null ? argument.ArgumentAst.Extent : extent);
                    }

                    Type actualPropertyType = GetActualPropertyType(propertyType, genericTypeMap, parameterName, extent);

                    // Detect calls to non-boolean properties that were specified like a switch
                    if ((actualPropertyType != typeof(bool)) && (actualPropertyType != typeof(SwitchParameter)))
                    {
                        if (wasSpecifiedAsSwitch)
                        {
                            string errorMsg = String.Format(CultureInfo.InvariantCulture, ActivityResources.MissingValueForParameter, parameterName);
                            ReportError("MissingValueForParameter", errorMsg, extent);
                        }
                    }

                    ScriptBlockExpressionAst argumentValueAsScriptBlock = null;
                    if (argument != null)
                    {
                        argumentValueAsScriptBlock = argument.ArgumentAst as ScriptBlockExpressionAst;
                    }

                    // See if this is a script block being bound to an activity - this is a container activity.
                    if(
                        argumentValueAsScriptBlock != null &&
                        (
                            typeof(Activity).IsAssignableFrom(propertyType) ||
                            typeof(Activity[]).IsAssignableFrom(propertyType) ||
                            (propertyType.IsGenericType && (typeof(Activity).IsAssignableFrom(propertyType.GetGenericArguments()[0])))
                        ))
                    {
                        bool previousDisableSymbolGeneration = this.disableSymbolGeneration;

                        WriteLine("<" + friendlyName + "." + parameterName + ">");
                        IndentLevel();

                        WriteLine("<Sequence>");
                        IndentLevel();

                        try
                        {
                            this.disableSymbolGeneration = true;

                            foreach (StatementAst statement in argumentValueAsScriptBlock.ScriptBlock.EndBlock.Statements)
                            {
                                statement.Visit(this);
                            }
                        }
                        finally
                        {
                            this.disableSymbolGeneration = previousDisableSymbolGeneration;

                            UnindentLevel();
                            WriteLine("</Sequence>");

                            UnindentLevel();
                            WriteLine("</" + friendlyName + "." + parameterName + ">");
                        }
                    }
                    // If the property type is not generic, then we have to in-line the string representation
                    // Or, if the incoming argument is of the exact type, and it's not a string (because then it could contain variable
                    // references etc), then use its string representation
                    else if ((!propertyType.IsGenericType) ||
                        ((!ContainsLanguageElements(argumentValue.ToString())) &&
                        (actualPropertyType == argumentValue.GetType())))
                    {
                        string valueToUse = argumentValue.ToString();
                        if ((argument != null) && (argument.OriginalValue != null))
                        {
                            valueToUse = argument.OriginalValue.ToString();
                        }

                        if (ContainsLanguageElements(argumentValue.ToString()))
                        {
                            string convertedText = GetEquivalentVBTextForLiteralValue(actualPropertyType, valueToUse);
                            if (String.IsNullOrEmpty(convertedText))
                            {
                                // If this is a direct property assignment, ensure it doesn't contain any PowerShell language
                                // elements.

                                IScriptExtent errorExtent = null;
                                Ast argumentAst = argumentValue as Ast;
                                if (argumentAst != null)
                                {
                                    errorExtent = argumentAst.Extent;
                                }
                                else
                                {
                                    errorExtent = argument.ArgumentAst.Extent;
                                }

                                string error = String.Format(CultureInfo.InvariantCulture, ActivityResources.PropertyDoesNotSupportPowerShellLanguage, parameterNameIterator);
                                ReportError("PropertyDoesNotSupportPowerShellLanguage", error, errorExtent);
                            }

                            valueToUse = convertedText;
                        }
                        else
                        {
                            // This will be injected directly into the XAML - escape it
                            if (propertyType.IsGenericType)
                            {
                                valueToUse = EncodeStringArgument(valueToUse, false);
                            }
                            else
                            {
                                valueToUse = EncodeStringArgumentLiteral(valueToUse, false);
                            }
                        }

                        // In the XAML processing sequence, provided values from a markup extension do not invoke additional value conversion. (http://msdn.microsoft.com/en-us/library/ms742135.aspx)
                        // So we use the markup extension to represent an empty string only if the property type is exactly System.String
                        if (String.Empty.Equals(valueToUse))
                        {
                            if (propertyType.Equals(typeof(string)))
                            {
                                valueToUse = "<x:Static Member=\"x:String.Empty\" />";
                            }
                            else
                            {
                                valueToUse = "[\"\"]";
                            }
                        }

                        WriteLine("<" + friendlyName + "." + parameterName + ">" + valueToUse + "</" + friendlyName + "." + parameterName + ">");
                    }
                    else
                    {
                        string argumentValueString = argumentValue.ToString();
                        string variableName = argumentValueString.Substring(1);

                        // If this is is an OutArgument or InOutArgument, they must supply a variable name.
                        // If they've supplied a variable name and the type matches, use that directly.
                        if ((propertyType.BaseType != null) &&
                            (typeof(InOutArgument).IsAssignableFrom(propertyType.BaseType) ||
                             typeof(OutArgument).IsAssignableFrom(propertyType.BaseType)))
                        {
                            if (argumentValueString.StartsWith("$", StringComparison.OrdinalIgnoreCase))
                            {
                                if(! (VariableDefined(variableName) || members.ContainsKey(variableName)))
                                {
                                    DefineVariable(variableName, actualPropertyType, extent, null);
                                }

                                WriteLine("<" + friendlyName + "." + parameterName + ">[" + variableName + "]</" + friendlyName + "." + parameterName + ">");
                            }
                            else
                            {
                                string error = String.Format(CultureInfo.InvariantCulture, ActivityResources.MustSupplyVariableReferenceForInOutArgument, parameterName);
                                ReportError("MustSupplyVariableReferenceForInOutArgument", error, extent);
                            }
                        }
                        else
                        {
                            // Otherwise, go through PowerShellValue to resolve variables, expressions, etc.
                            string propertyFriendlyName = GetConvertedTypeName(actualPropertyType);

                            WriteLine("<" + friendlyName + "." + parameterName + ">");
                            IndentLevel();

                            String argumentLine = String.Format(CultureInfo.InvariantCulture,
                                @"<{0} x:TypeArguments=""{1}"">",
                                GetFriendlyName(null, propertyType),
                                propertyFriendlyName);
                            WriteLine(argumentLine);
                            IndentLevel();

                            // Now make the call
                            string expression = argumentValue.ToString();
                            if (argumentValue is Ast)
                            {
                                expression = GetPowerShellValueExpression((Ast)argumentValue);
                            }
                            GeneratePowerShellValue(actualPropertyType, expression, isLiteral, false);

                            UnindentLevel();
                            WriteLine("</" + GetFriendlyName(null, propertyType) + ">");

                            UnindentLevel();
                            WriteLine("</" + friendlyName + "." + parameterName + ">");
                        }
                    }
                }
            }

            // Close the activity call
            UnindentLevel();
            WriteLine("</" + friendlyName + ">");

            // If we had any properties to emulate, restore the old values
            if (propertiesToEmulate.Count > 0)
            {
                string exceptionFriendlyName = GetFriendlyName(null, typeof(Exception));
                // Restore the variables if there was no error
                foreach(string propertyToEmulate in propertiesToEmulate.Keys)
                {
                    // Variable names from the saved values if we need them
                    string variableName = "PSSaved_" + propertyToEmulate;

                    WriteLine(@"<" + setPSWorkflowDataFriendlyName + @" PSRemotingBehavior=""{x:Null}"" OtherVariableName=""" +
                        propertyToEmulate + @""" Value=""[" + variableName + @"]"" />");
                }

                // Close the sequence and Try / Catch
                UnindentLevel();
                WriteLine("</Sequence>");
                UnindentLevel();
                WriteLine("</TryCatch.Try>");
                WriteLine("<TryCatch.Catches>");
                IndentLevel();
                WriteLine(@"<Catch x:TypeArguments=""" + exceptionFriendlyName + @""">");
                IndentLevel();
                WriteLine(@"<ActivityAction x:TypeArguments=""" + exceptionFriendlyName + @""">");
                IndentLevel();
                WriteLine("<Sequence>");
                IndentLevel();

                // Restore the variables in the Catch
                foreach(string propertyToEmulate in propertiesToEmulate.Keys)
                {
                    // Variable names from the saved values if we need them
                    string variableName = "PSSaved_" + propertyToEmulate;

                    WriteLine(@"<" + setPSWorkflowDataFriendlyName + @" PSRemotingBehavior=""{x:Null}"" OtherVariableName=""" +
                        propertyToEmulate + @""" Value=""[" + variableName + @"]"" />");
                }

                WriteLine("<Rethrow />");
                UnindentLevel();

                WriteLine("</Sequence>");
                UnindentLevel();
                WriteLine("</ActivityAction>");
                UnindentLevel();
                WriteLine("</Catch>");
                UnindentLevel();
                WriteLine("</TryCatch.Catches>");
                UnindentLevel();
                WriteLine("</TryCatch>");

            }

            // If we created an aggregation variable, emit its results into the actual storage variable
            if (needAggregationVariable)
            {
                string writeOutputFriendlyName = GetFriendlyName(null, typeof(Microsoft.PowerShell.Utility.Activities.WriteOutput));
                GenerateWriteOutputStart(writeOutputFriendlyName, actualAggregatingVariable, extent);

                GeneratePowerShellValue(typeof(PSObject[]), "$" + variableToUse, false, false);
                GenerateWriteOutputEnd(writeOutputFriendlyName);
            }

            // If we needed a conversion variable, use PowerShellValue to do the conversion
            if (needConversionVariable && (! needAggregationVariable))
            {
                // We rewrite an assignment such as: "$x += 1" to "$x = $x + 1; $x" so that
                // PowerShell returns the new value after assignment.
                string assignmentExpression = null;

                if (isAggregatingVariable)
                {
                    assignmentExpression = "$" + actualAggregatingVariable + " + $" + variableToUse;
                }
                else
                {
                    assignmentExpression = "$" + variableToUse;
                }

                GeneratePowerShellValue(variableToUseDefinition.Type, assignmentExpression, false, actualAggregatingVariable);
            }
        }

        // Map the generic argument type names to their actual types
        // For example:
        //   input: List<string> --- output: {T, string}
        private static Dictionary<string, Type> GetGenericTypeMap(Type activityType, Type[] genericTypeArray)
        {
            if (!activityType.IsGenericType)
            {
                return null;
            }

            var genericTypeMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            var activityTypeDefinition = activityType.GetGenericTypeDefinition();
            var genericParamTypes = activityTypeDefinition.GetGenericArguments();

            for (int i = 0; i < genericParamTypes.Length; i++)
            {
                genericTypeMap.Add(genericParamTypes[i].Name, genericTypeArray[i]);
            }

            return genericTypeMap;
        }

        private Type GetActualPropertyType(Type rawPropertyType, Dictionary<string, Type> genericTypeMap, string parameterName, IScriptExtent extent)
        {
            Type[] unusedGenericArgumentTypes;
            bool isActivityArgument =
                IsAssignableFromGenericType(typeof (InArgument<>), rawPropertyType, out unusedGenericArgumentTypes) ||
                IsAssignableFromGenericType(typeof (InOutArgument<>), rawPropertyType, out unusedGenericArgumentTypes) ||
                IsAssignableFromGenericType(typeof (OutArgument<>), rawPropertyType, out unusedGenericArgumentTypes);

            Type actualPropertyType = isActivityArgument ? rawPropertyType.GetGenericArguments()[0] : rawPropertyType;

            if (actualPropertyType.IsGenericParameter)
            {
                // If it's in process of the compilation
                //   --- 'ResolveGenericParameterType' throws if we cannot find the actual type of the specified generic parameter
                // If it's only for validation
                //   --- 'ResolveGenericParameterType' returns null if we cannot find the actual type of the specified generic parameter,
                //       in that case, we use the "actualPropertyType" directly
                Type parameterType = ResolveGenericParameterType(actualPropertyType, genericTypeMap, parameterName, extent);
                actualPropertyType = !this.validateOnly 
                                         ? parameterType 
                                         : (parameterType ?? actualPropertyType);
            }
            else if (actualPropertyType.IsGenericType && actualPropertyType.ContainsGenericParameters)
            {
                // If it's in process of the compilation
                //   --- 'ResolveTypeWithGenericParameters' throws if we cannot resolve the specified generic type
                // If it's only for validation
                //   --- 'ResolveTypeWithGenericParameters' returns null if we cannot resolve the specified generic type,
                //       in that case, we use the "actualPropertyType" directly
                Type resolvedPropertyType = ResolveTypeWithGenericParameters(actualPropertyType, genericTypeMap, parameterName, extent);
                actualPropertyType = !this.validateOnly
                                         ? resolvedPropertyType
                                         : (resolvedPropertyType ?? actualPropertyType);
            }

            return actualPropertyType;
        }

        // Return the actual type for the generic parameter.
        // If "this.ValidateOnly == true", this method returns null when no actual type can be found for the generic parameter
        // If "this.ValidateOnly == false", this method throws when no actual type can be found for the generic parameter
        private Type ResolveGenericParameterType(Type genericParameter, Dictionary<string, Type> genericTypeMap, string parameterName, IScriptExtent extent)
        {
            Type actualParameterType = null;
            if (genericTypeMap == null || !genericTypeMap.TryGetValue(genericParameter.Name, out actualParameterType))
            {
                string errorMsg = String.Format(CultureInfo.InvariantCulture, ActivityResources.GenericParameterTypeNotFound, genericParameter.Name, parameterName);
                ReportError("GenericParameterTypeNotFound", errorMsg, extent);
            }

            return actualParameterType;
        }

        // Resolve the specified generic type that contains generic parameters
        // If "this.ValidateOnly == true", this method returns null when the resolution fails
        // If "this.ValidateOnly == false", this method throws when the resolution fails
        private Type ResolveTypeWithGenericParameters(Type genericPropertyType, Dictionary<string, Type> genericTypeMap, string parameterName, IScriptExtent extent)
        {
            Type[] genericArguments = genericPropertyType.GetGenericArguments();
            var resolvedArgumentTypes = new List<Type>(genericArguments.Length);

            foreach (Type argument in genericArguments)
            {
                if (argument.IsGenericParameter)
                {
                    Type resolvedParameterType = ResolveGenericParameterType(argument, genericTypeMap, parameterName, extent);
                    // It's for validation only, and we cannot find the actual type of the specified generic parameter
                    if (resolvedParameterType == null) { return null; }
                    resolvedArgumentTypes.Add(resolvedParameterType);
                }
                else if (argument.IsGenericType && argument.ContainsGenericParameters)
                {
                    Type resolvedGenericArgument = ResolveTypeWithGenericParameters(argument, genericTypeMap, parameterName, extent);
                    if (resolvedGenericArgument == null) { return null; }
                    resolvedArgumentTypes.Add(resolvedGenericArgument);
                }
                else
                {
                    resolvedArgumentTypes.Add(argument);
                }
            }

            Type resolvedGenericPropertyType = null;
            try
            {
                Type genericTypeDefinition = genericPropertyType.GetGenericTypeDefinition();
                resolvedGenericPropertyType = genericTypeDefinition.MakeGenericType(resolvedArgumentTypes.ToArray());
            }
            catch
            {
                string errorMsg = String.Format(CultureInfo.InvariantCulture, ActivityResources.GenericPropertyTypeNotResolved, genericPropertyType.Name, parameterName);
                ReportError("GenericParameterTypeNotFound", errorMsg, extent);
            }

            return resolvedGenericPropertyType;
        }

        private bool IsParameterAvailable(Type activityType, DynamicActivity activityInstance, string parameterName, bool ignoreAmbiguousMatch, IScriptExtent extent, out Type propertyType)
        {
            propertyType = null;
            try
            {
                propertyType = GetPropertyType(activityType, activityInstance, ref parameterName, extent);
                return propertyType != null;
            }
            catch (AmbiguousMatchException)
            {
                // ignore the AmbiguousMatchException. The Activity<TResult> has two Result parameters.
                if (ignoreAmbiguousMatch)
                {
                    return true;
                }

                string errorMsg = String.Format(CultureInfo.InvariantCulture, ActivityResources.AmbiguousPropertiesFound, parameterName);
                ReportError("AmbiguousParametersFound", errorMsg, extent);
                return false;
            }
        }

        private static bool IsAssignableFromGenericType(Type parentType, Type fromType, out Type[] genericArgumentTypes)
        {
            genericArgumentTypes = null;
            if (!fromType.IsGenericType) { return false; }

            // Check the interfaces
            if (CheckInterface(parentType, fromType, out genericArgumentTypes)) { return true; }

            // No match in interfaces, then we check the class hierarchy
            return CheckClass(parentType, fromType, out genericArgumentTypes);
        }

        private static bool CheckInterface(Type parentType, Type fromType, out Type[] genericArgumentTypes)
        {
            genericArgumentTypes = null;
            var interfaceTypes = fromType.GetInterfaces();
            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType)
                {
                    if (it.GetGenericTypeDefinition() == parentType)
                    {
                        genericArgumentTypes = it.GetGenericArguments();
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool CheckClass(Type parentType, Type fromType, out Type[] genericArgumentTypes)
        {
            genericArgumentTypes = null;
            if (fromType.GetGenericTypeDefinition() == parentType)
            {
                genericArgumentTypes = fromType.GetGenericArguments();
                return true;
            }

            Type baseType = fromType.BaseType;
            if (baseType == null || !baseType.IsGenericType) return false;

            return CheckClass(parentType, baseType, out genericArgumentTypes);
        }

        private bool ContainsLanguageElements(string input)
        {
            if (String.IsNullOrEmpty(input))
            {
                return false;
            }

            if (input[0] == '\'' && input[input.Length - 1] == '\'')
            {
                return false;
            }

            char[] languageElementIdentifiers = { '`', '$', '(', '@' };
            if (input.IndexOfAny(languageElementIdentifiers) >= 0)
            {
                return true;
            }

            return false;
        }

        private void GenerateSymbolicInformation(IScriptExtent extent)
        {
            // Generate the symbolic metadata for this command call
            if (! this.disableSymbolGeneration)
            {
                string position = String.Format(CultureInfo.InvariantCulture, "{0}:{1}:{2}", extent.StartLineNumber, extent.StartColumnNumber, this.name);

                // Stop storing results into the storage variable
                Stack<StorageVariable> savedStorage = this.resultVariables;
                bool savedMergeErrorToOutput = this.mergeErrorToOutput;

                try
                {
                    this.resultVariables = null;
                    this.mergeErrorToOutput = false;

                    Dictionary<string, CommandArgumentInfo> setHostValueArguments = new Dictionary<string, CommandArgumentInfo>(StringComparer.OrdinalIgnoreCase);
                    setHostValueArguments["OtherVariableName"] = new CommandArgumentInfo { Value = "Position", IsLiteral = true };
                    setHostValueArguments["Value"] = new CommandArgumentInfo { Value = position, IsLiteral = true };

                    GenerateActivityCall(null, "SetPSWorkflowData", typeof(SetPSWorkflowData), null, setHostValueArguments, null, null, extent);
                }
                finally
                {
                    this.resultVariables = savedStorage;
                    this.mergeErrorToOutput = savedMergeErrorToOutput;
                }
            }
        }

        // The list of parameters that need to be ignored.
        static List<string> ignoredParameters;
        
        private static bool IsNotSupportedCommonParameter(string parameterName)
        {
            return ignoredParameters.Contains(parameterName, StringComparer.OrdinalIgnoreCase);
        }

        private void UpdateArgumentsFromPreferenceVariables(Type activityType, Dictionary<string, CommandArgumentInfo> arguments, DynamicActivity activityInstance)
        {
            string preferenceVariable = null;

            if (typeof(PSActivity).IsAssignableFrom(activityType))
            {
                // Process any of the ubiquitous parameters for PSActivity

                preferenceVariable = "DebugPreference";
                if (VariableDefined(preferenceVariable) && (!arguments.ContainsKey(preferenceVariable)))
                {
                    SetDebugPreferenceArg(arguments, preferenceVariable);
                }

                preferenceVariable = "ErrorActionPreference";
                if (VariableDefined(preferenceVariable) && (!arguments.ContainsKey(preferenceVariable)))
                {
                    SetErrorActionPreferenceArg(arguments, preferenceVariable);
                }

                preferenceVariable = "VerbosePreference";
                if (VariableDefined(preferenceVariable) && (!arguments.ContainsKey(preferenceVariable)))
                {
                    SetVerbosePreferenceArg(arguments, preferenceVariable);
                }

                preferenceVariable = "WarningPreference";
                if (VariableDefined(preferenceVariable) && (!arguments.ContainsKey(preferenceVariable)))
                {
                    SetWarningActionPreferenceArg(arguments, preferenceVariable);
                }

                preferenceVariable = "InformationPreference";
                if (VariableDefined(preferenceVariable) && (!arguments.ContainsKey(preferenceVariable)))
                {
                    SetInformationActionPreferenceArg(arguments, preferenceVariable);
                }

                preferenceVariable = "WhatIfPreference";
                if (VariableDefined(preferenceVariable) && (!arguments.ContainsKey(preferenceVariable)))
                {
                    CommandArgumentInfo preferenceVariableInfo = new CommandArgumentInfo();
                    preferenceVariableInfo.Value = "$" + preferenceVariable;
                    arguments["WhatIf"] = preferenceVariableInfo;
                }
            }
            else if (typeof(DynamicActivity).IsAssignableFrom(activityType) && (activityInstance != null))
            {
                // Process ubiquitous parameters for DynamicActivity if supported.

                preferenceVariable = "VerbosePreference";
                if (VariableDefined(preferenceVariable) && (!arguments.ContainsKey(preferenceVariable)) && activityInstance.Properties.Contains("Verbose"))
                {
                    SetVerbosePreferenceArg(arguments, preferenceVariable);
                }

                preferenceVariable = "DebugPreference";
                if (VariableDefined(preferenceVariable) && (!arguments.ContainsKey(preferenceVariable)) && activityInstance.Properties.Contains("Debug"))
                {
                    SetDebugPreferenceArg(arguments, preferenceVariable);
                }

                preferenceVariable = "ErrorActionPreference";
                if (VariableDefined(preferenceVariable) && (!arguments.ContainsKey(preferenceVariable)) && activityInstance.Properties.Contains("ErrorAction"))
                {
                    SetErrorActionPreferenceArg(arguments, preferenceVariable);
                }

                preferenceVariable = "WarningPreference";
                if (VariableDefined(preferenceVariable) && (!arguments.ContainsKey(preferenceVariable)) && activityInstance.Properties.Contains("WarningAction"))
                {
                    SetWarningActionPreferenceArg(arguments, preferenceVariable);
                }

                preferenceVariable = "InformationPreference";
                if (VariableDefined(preferenceVariable) && (!arguments.ContainsKey(preferenceVariable)) && activityInstance.Properties.Contains("InformationAction"))
                {
                    SetInformationActionPreferenceArg(arguments, preferenceVariable);
                }
            }

            // Process any of the ubiquitous parameters for PSRemotingActivity
            if (typeof(PSRemotingActivity).IsAssignableFrom(activityType))
            {
                // PSSessionApplicationName
                preferenceVariable = "PSSessionApplicationName";
                if (VariableDefined(preferenceVariable) && (!arguments.ContainsKey(preferenceVariable)))
                {
                    CommandArgumentInfo preferenceVariableInfo = new CommandArgumentInfo();
                    preferenceVariableInfo.Value = "$" + preferenceVariable;
                    arguments["PSApplicationName"] = preferenceVariableInfo;
                }

                // PSSessionConfigurationName
                preferenceVariable = "PSSessionConfigurationName";
                if (VariableDefined(preferenceVariable) && (!arguments.ContainsKey(preferenceVariable)))
                {
                    CommandArgumentInfo preferenceVariableInfo = new CommandArgumentInfo();
                    preferenceVariableInfo.Value = "$" + preferenceVariable;
                    arguments["PSConfigurationName"] = preferenceVariableInfo;
                }

                // PSSessionOption
                preferenceVariable = "PSSessionOption";
                if (VariableDefined(preferenceVariable) && (!arguments.ContainsKey(preferenceVariable)))
                {
                    CommandArgumentInfo preferenceVariableInfo = new CommandArgumentInfo();
                    preferenceVariableInfo.Value = "$" + preferenceVariable;
                    arguments["PSSessionOption"] = preferenceVariableInfo;
                }
            }
        }

        private static void SetVerbosePreferenceArg(Dictionary<string, CommandArgumentInfo> arguments, string preferenceVariable)
        {
            CommandArgumentInfo preferenceVariableInfo = new CommandArgumentInfo();
            preferenceVariableInfo.Value = "if($" + preferenceVariable + " -eq 'Continue') { $true } else { $false }";
            arguments["Verbose"] = preferenceVariableInfo;
        }

        private static void SetDebugPreferenceArg(Dictionary<string, CommandArgumentInfo> arguments, string preferenceVariable)
        {
            CommandArgumentInfo preferenceVariableInfo = new CommandArgumentInfo();
            preferenceVariableInfo.Value = "if($" + preferenceVariable + " -eq 'Continue') { $true } else { $false }";
            arguments["Debug"] = preferenceVariableInfo;
        }

        private static void SetErrorActionPreferenceArg(Dictionary<string, CommandArgumentInfo> arguments, string preferenceVariable)
        {
            SetSimpleActionPreferenceArg("ErrorAction", arguments, preferenceVariable);
        }

        private static void SetWarningActionPreferenceArg(Dictionary<string, CommandArgumentInfo> arguments, string preferenceVariable)
        {
            SetSimpleActionPreferenceArg("WarningAction", arguments, preferenceVariable);
        }

        private static void SetInformationActionPreferenceArg(Dictionary<string, CommandArgumentInfo> arguments, string preferenceVariable)
        {
            SetSimpleActionPreferenceArg("InformationAction", arguments, preferenceVariable);
        }

        private static void SetSimpleActionPreferenceArg(string preference, Dictionary<string, CommandArgumentInfo> arguments, string preferenceVariable)
        {
            CommandArgumentInfo preferenceVariableInfo = new CommandArgumentInfo();
            preferenceVariableInfo.Value = "$" + preferenceVariable;
            arguments[preference] = preferenceVariableInfo;
        }


        private Type GetPropertyType(Type activityType, DynamicActivity activityInstance, ref string parameterName, IScriptExtent extent)
        {
            Dictionary<string, Type> mergedProperties = GetAvailableProperties(activityType, activityInstance);
            Type resultType = null;

            // Map their case-insensitive property lookup to the actual property
            // name, and do minimal substring matching.
            List<string> parameterMatches = new List<string>();
            foreach(string propertyName in mergedProperties.Keys)
            {
                // Check if it is an exact match. If so, return immediately.
                if(String.Equals(propertyName, parameterName, StringComparison.OrdinalIgnoreCase))
                {
                    resultType = mergedProperties[propertyName];
                    parameterName = propertyName;
                    return resultType;
                }

                // Check if it is a substring match. If so, remember which parameters have
                // matched and store the result type of the last one.
                if(propertyName.StartsWith(parameterName, StringComparison.OrdinalIgnoreCase))
                {
                    resultType = mergedProperties[propertyName];
                    parameterMatches.Add("-" + propertyName);
                }
            }

            // Verify there were not multiple substring matches
            if (parameterMatches.Count > 1)
            {
                string combinedMatches = String.Join(" ", parameterMatches);
                string error = String.Format(CultureInfo.InvariantCulture, ActivityResources.AmbiguousParameter, parameterName, combinedMatches);
                ReportError("AmbiguousParameter", error, extent);
            }
            else if(parameterMatches.Count == 1)
            {
                parameterName = parameterMatches[0].Substring(1);
            }

            return resultType;
        }

        internal static Dictionary<string, Type> GetAvailableProperties(Type activityType, DynamicActivity activityInstance)
        {
            Dictionary<string, Type> mergedProperties = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            // If this is a dynamic activity, pull the info from activityInstance
            if (activityInstance != null)
            {
                for (int index = 0; index < activityInstance.Properties.Count; index++)
                {
                    string propertyName = activityInstance.Properties[index].Name;
                    Type propertyType = activityInstance.Properties[index].Type;
                    mergedProperties[propertyName] = propertyType;
                }
            }
            else
            {
                // This is a concrete activity
                foreach (PropertyInfo property in activityType.GetProperties(BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.Instance))
                {
                    mergedProperties[property.Name] = property.PropertyType;
                }
            }

            // Remove the "ID" property, as it is in the Activity base class and will be avoided
            // by cmdlets
            if (mergedProperties.ContainsKey("Id"))
            {
                mergedProperties.Remove("Id");
            }

            return mergedProperties;
        }

        // Gets properties understood / handled by workflow compilation but not by the activity itself
        internal static Dictionary<string, Type> GetVirtualProperties(Type activityType, DynamicActivity activityInstance)
        {
            if (String.Equals(activityType.FullName, "Microsoft.PowerShell.Core.Activities.ForEachObject", StringComparison.OrdinalIgnoreCase))
            {
                return new Dictionary<string, Type> { { "PipelineVariable", typeof(string) } };
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Utility to encode an expression so it can be inserted in the generated XAML.
        /// </summary>
        /// <param name="bareContent">The expression to encode</param>
        /// <param name="isLiteral">
        /// If true, the string should be wrapped in quotes to it works
        /// like a literal expression.
        /// </param>
        /// <returns></returns>
        private string EncodeStringArgument(string bareContent, bool isLiteral)
        {
            // First, do XAML encoding
            bareContent = EncodeStringNonArgument(bareContent, isLiteral);

            // Since this is an argument, escape content (i.e.: '[')  to prevent
            // the expression from looking like a VB value
            var literal = new System.Activities.Expressions.Literal<string>(bareContent);
            string literalEncoded = literal.ConvertToString(null);
            bareContent = literalEncoded;

            return bareContent;
        }

        // Encodes a string so that it can be used directly in XAML.
        private string EncodeStringNonArgument(string bareContent, bool isLiteral)
        {
            bareContent = EncodeStringArgumentLiteral(bareContent, isLiteral);

            // Escape markup extensions
            if (bareContent.StartsWith("{", StringComparison.OrdinalIgnoreCase))
            {
                bareContent = "{}" + bareContent;
            }

            return bareContent;
        }

        // Encodes a string so that it can be used directly in XAML.
        private string EncodeStringArgumentLiteral(string bareContent, bool isLiteral)
        {
            StringBuilder encodedString = new StringBuilder(bareContent.Length);
            System.Xml.XmlWriterSettings settings = new System.Xml.XmlWriterSettings();
            settings.ConformanceLevel = System.Xml.ConformanceLevel.Fragment;
            using (var writer = System.Xml.XmlWriter.Create(encodedString, settings))
            {
                writer.WriteString(bareContent);
            }

            bareContent = encodedString.ToString();

            bareContent = bareContent.Replace("\r", "&#xD;");
            bareContent = bareContent.Replace("\n", "&#xA;");
            bareContent = bareContent.Replace(@"""", "&quot;");

            // If the target to assign to is an argument, it expects
            // an expression so re-write
            //    hello
            // as
            //    'hello'
            if (isLiteral)
            {
                bareContent = bareContent.Replace("'", "''");
                bareContent = "'" + bareContent + "'";
            }

            return bareContent;
        }

        private Dictionary<string, CommandArgumentInfo> GetAndResolveParameters(CommandAst commandAst, bool searchSessionState)
        {
            StaticBindingResult bindingResult = StaticParameterBinder.BindCommand(commandAst, searchSessionState);
            Dictionary<string, CommandArgumentInfo> convertedParameters = ResolveParameters(bindingResult);

            return convertedParameters;
        }

        private Dictionary<string, CommandArgumentInfo> ResolveParameters(StaticBindingResult parameters)
        {
            Dictionary<string, CommandArgumentInfo> convertedParameters = new Dictionary<string, CommandArgumentInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (string parameterName in parameters.BoundParameters.Keys)
            {
                CommandArgumentInfo argumentInfo = new CommandArgumentInfo();
                argumentInfo.Value = ProcessCommandArgument(parameters.BoundParameters[parameterName].Value);
                argumentInfo.ArgumentAst = parameters.BoundParameters[parameterName].Value;

                if(argumentInfo.Value != null)
                {
                    StringConstantExpressionAst stringAst = parameters.BoundParameters[parameterName].Value as StringConstantExpressionAst;
                    if (stringAst != null)
                    {
                        argumentInfo.OriginalValue = stringAst.Value;
                    }
                    else
                    {
                        argumentInfo.OriginalValue = parameters.BoundParameters[parameterName].Value.Extent.Text;
                    }
                }
                convertedParameters.Add(parameterName, argumentInfo);
            }

            return convertedParameters;
        }

        object ICustomAstVisitor.VisitCommandExpression(CommandExpressionAst commandExpressionAst)
        {
            // If this is a unary expression, it will update the variable in-place. Generate an assignment
            // statement.
            if (commandExpressionAst.Expression is UnaryExpressionAst)
            {
                return commandExpressionAst.Expression.Visit(this);
            }

            // If it is a parenthesized expression, visit what's in the parens
            ParenExpressionAst parenExpression = commandExpressionAst.Expression as ParenExpressionAst;
            if (parenExpression != null)
            {
                return parenExpression.Pipeline.Visit(this);
            }

            // If this is a method call, throw an error that they should use Inline Script instead.
            if (commandExpressionAst.Expression is InvokeMemberExpressionAst)
            {
                ReportError("MethodInvocationNotSupported", ActivityResources.MethodInvocationNotSupported, commandExpressionAst.Expression.Extent);
            }

            // If this is a sub expression, throw an error that they should use Inline Script instead.
            if (commandExpressionAst.Expression is SubExpressionAst)
            {
                ReportError("SubExpressionNotSupported", ActivityResources.SubExpressionNotSupported, commandExpressionAst.Expression.Extent);
            }

            // If this is an attributed expression, throw an error that they should use attribute expression only when declaring parameters for the script workflow
            if ((commandExpressionAst.Expression is AttributedExpressionAst) &&
                (! (commandExpressionAst.Expression is ConvertExpressionAst)))
            {
                ReportError("AttributedExpressionNotSupported", ActivityResources.AttributedExpressionNotSupported, commandExpressionAst.Expression.Extent);
            }

            if (commandExpressionAst.Redirections.Count > 0)
            {
                foreach (RedirectionAst redirection in commandExpressionAst.Redirections)
                {
                    redirection.Visit(this);
                }

                this.mergeErrorToOutput = true;
            }

            // They've done a simple variable reference.

            // Ensure it has no side-effects, as we aren't capturing them.
            string nameOfUnSupportedVariableFound = null;
            if (CheckIfExpressionHasUnsupportedVariableOrHasSideEffects(null, commandExpressionAst, out nameOfUnSupportedVariableFound))
            {
                if (string.IsNullOrEmpty(nameOfUnSupportedVariableFound))
                {
                    string errorTemplate = ActivityResources.CannotStoreResultsInUnsupportedElement;
                    ReportError("CannotStoreResultsInUnsupportedElement",
                                ActivityResources.CannotStoreResultsInUnsupportedElement,
                                commandExpressionAst.Expression.Extent);
                }
                else
                {
                    string error = String.Format(CultureInfo.InvariantCulture,
                                                 ActivityResources.VariableNotSupportedInWorkflow,
                                                 nameOfUnSupportedVariableFound);
                    ReportError("VariableNotSupportedInWorkflow", error, commandExpressionAst.Expression.Extent);
                }
            }

            try
            {
                var currentVariable = GetVariableToUse();
                string variableToUse = null;
                bool isAggregatingVariable = false;
                if (currentVariable != null)
                {
                    variableToUse = currentVariable.VariableName;
                    isAggregatingVariable = currentVariable.IsAggregatingVariable;
                }

                // They're accessing the $input variable. Convert it to a simple call to Write-Output, which will
                // pick up the input stream from the parameter defaults.
                // We ignore error redirection in this case
                VariableExpressionAst inputVariable = commandExpressionAst.Expression as VariableExpressionAst;
                string writeOutputFriendlyName = GetFriendlyName(null, typeof(Microsoft.PowerShell.Utility.Activities.WriteOutput));
                if (inputVariable != null)
                {
                    if (String.Equals("input", inputVariable.VariablePath.UserPath, StringComparison.OrdinalIgnoreCase))
                    {
                        if (String.IsNullOrEmpty(variableToUse))
                        {
                            WriteLine(String.Format(CultureInfo.InvariantCulture, "<{0} UseDefaultInput=\"true\" />", writeOutputFriendlyName));
                        }
                        else
                        {
                            string appendOutput = isAggregatingVariable ? AppendOutputTemplate : string.Empty;
                            WriteLine(String.Format(CultureInfo.InvariantCulture, "<{0} UseDefaultInput=\"true\" Result = \"[{1}]\"{2} />", writeOutputFriendlyName, variableToUse, appendOutput));
                        }
                        return null;
                    }
                }

                // If we aren't storing an assignment, then add a call to Write-Output so that
                // it can be sent to the output stream.
                bool isOutputExpression = isAggregatingVariable || String.IsNullOrEmpty(variableToUse);

                // Generate the outer call to WriteOutput
                Type variableType = null;
                if (isOutputExpression)
                {
                    // Otherwise, make the following value the input object of the
                    // Write-Output activity.
                    variableType = typeof(PSObject[]);
                    GenerateWriteOutputStart(writeOutputFriendlyName, variableToUse, commandExpressionAst.Extent);
                }
                else
                {
                    variableType = GetVariableDefinition(variableToUse).Type;
                }

                // Evaluate the expression
                string expression = GetPowerShellValueExpression(commandExpressionAst.Expression);
                GeneratePowerShellValue(variableType, expression, false, true);

                // Complete the outer call to WriteObject
                if (isOutputExpression)
                {
                    GenerateWriteOutputEnd(writeOutputFriendlyName);
                }
            }
            finally
            {
                this.mergeErrorToOutput = false;
            }

            return null;
        }

        private void GenerateWriteOutputStart(string writeOutputFriendlyName, string storageScopeVariable, IScriptExtent extent)
        {
            GenerateSymbolicInformation(extent);

            Type inputType = typeof(PSObject[]);
            string convertedTypeName = GetConvertedTypeName(inputType);

            if (String.IsNullOrEmpty(storageScopeVariable))
            {
                WriteLine("<" + writeOutputFriendlyName + ">");
            }
            else
            {
                WriteLine(String.Format(CultureInfo.InvariantCulture, "<{0} Result = \"[{1}]\"{2} >", writeOutputFriendlyName, storageScopeVariable, AppendOutputTemplate));
            }
            IndentLevel();

            WriteLine("<" + writeOutputFriendlyName + ".NoEnumerate>[System.Management.Automation.SwitchParameter.Present]</" + writeOutputFriendlyName + ".NoEnumerate>");
            WriteLine("<" + writeOutputFriendlyName + ".InputObject>");
            IndentLevel();

            // Assign the value to the InputObject argument
            string template = @"<InArgument x:TypeArguments=""{0}"">";
            WriteLine(String.Format(CultureInfo.InvariantCulture, template, convertedTypeName));
            IndentLevel();
        }

        private void GenerateWriteOutputEnd(string writeOutputFriendlyName)
        {
            UnindentLevel();

            WriteLine("</InArgument>");
            UnindentLevel();

            WriteLine("</" + writeOutputFriendlyName + ".InputObject>");
            UnindentLevel();

            WriteLine("</" + writeOutputFriendlyName + ">");
        }

        object ICustomAstVisitor.VisitCommandParameter(CommandParameterAst commandParameterAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitSwitchStatement(SwitchStatementAst switchStatementAst)
        {
            Type switchType = typeof(object);

            // Figure out the type of the switch statement by examining its expression (first), or
            // its clauses (second). We need a strongly-typed switch statement, as workflow uses
            // a hashtable lookup to determine condition hits.
            ExpressionAst conditionExpressionAst = switchStatementAst.Condition.GetPureExpression();
            if (conditionExpressionAst != null)
            {
                switchType = conditionExpressionAst.StaticType;
            }

            // Elevate ints to doubles so that the WF switch can handle the conversion
            if ((switchType == typeof(int)) ||
                (switchType == typeof(Int32)) ||
                (switchType == typeof(UInt32)))
            {
                switchType = typeof(double);
            }

            if ((switchType == typeof(object)) ||
                (!IsSupportedSwitchExpressionType(switchType))
                )
            {
                switchType = null;

                foreach (Tuple<ExpressionAst, StatementBlockAst> switchClause in switchStatementAst.Clauses)
                {
                    // Verify they've typed a literal (i.e.: integer, string)
                    ConstantExpressionAst key = switchClause.Item1 as ConstantExpressionAst;
                    if (key == null)
                    {
                        ReportError("SwitchOnlySupportsConstantExpression", ActivityResources.SwitchOnlySupportsConstantExpression, switchClause.Item1.Extent);
                        return null;
                    }

                    if (switchType == null)
                    {
                        switchType = key.StaticType;
                    }
                    else
                    {
                        // Validate they only have one key type
                        if (key.StaticType != switchType)
                        {
                            ReportError("SwitchClauseMustBeOfSameType", ActivityResources.SwitchClauseMustBeOfSameType, key.Extent);
                        }
                    }
                }
            }

            if (switchType == null)
            {
                switchType = typeof(object);
            }

            // Generic flag errors
            if (
                ((switchStatementAst.Flags & SwitchFlags.Regex) == SwitchFlags.Regex) ||
                ((switchStatementAst.Flags & SwitchFlags.Wildcard) == SwitchFlags.Wildcard) ||
                ((switchStatementAst.Flags & SwitchFlags.File) == SwitchFlags.File) ||
                ((switchStatementAst.Flags & SwitchFlags.Parallel) == SwitchFlags.Parallel))
            {
                ReportError("SwitchFlagNotSupported", ActivityResources.SwitchFlagNotSupported, switchStatementAst.Extent);
            }

            // Validate any parameters to the switch statement
            if ((switchType == typeof(string)) || (switchType == typeof(char)))
            {
                if ((switchStatementAst.Flags & SwitchFlags.CaseSensitive) != SwitchFlags.CaseSensitive)
                {
                    ReportError("SwitchCaseSensitive", ActivityResources.SwitchCaseSensitive, switchStatementAst.Extent);
                }
            }

            GenerateSymbolicInformation(switchStatementAst.Extent);

            // Generate a temporary variable for the switch condition
            string tempVarName = GenerateUniqueVariableName("SwitchCondition");
            Type conditionType = DetectType(tempVarName, false, switchStatementAst.Condition);
            DefineVariable(tempVarName, conditionType, switchStatementAst.Condition.Extent, null);

            // Generate the assignment of the the clause to the temporary variable
            string conditionExpression = GetPowerShellValueExpression(switchStatementAst.Condition);
            GenerateAssignment(tempVarName, switchStatementAst.Condition.Extent, TokenKind.Equals, switchStatementAst.Condition, conditionExpression);


            string switchTypeFriendlyName = GetConvertedTypeName(switchType);
            WriteLine(@"<Switch x:TypeArguments=""" + switchTypeFriendlyName + @""">");
            IndentLevel();

            WriteLine("<Switch.Expression>");
            IndentLevel();

            WriteLine(@"<InArgument x:TypeArguments=""" + switchTypeFriendlyName + @""">");
            IndentLevel();

            // Evaluate the switch expression
            string errorMessage = ActivityResources.SwitchEnumerationNotSupported;
            string switchExpressionTemplate = "$switchCondition = ${0}; if(@($switchCondition).Count -gt 1) {{ throw '{1}' }}; $switchCondition";
            string switchConditionExpression = String.Format(CultureInfo.InvariantCulture, switchExpressionTemplate, tempVarName, errorMessage);
            GeneratePowerShellValue(switchType, switchConditionExpression, false, false);

            UnindentLevel();
            WriteLine("</InArgument>");

            UnindentLevel();
            WriteLine("</Switch.Expression>");

            if (switchStatementAst.Default != null)
            {
                WriteLine("<Switch.Default>");
                IndentLevel();

                WriteLine("<Sequence>");
                IndentLevel();

                switchStatementAst.Default.Visit(this);

                UnindentLevel();
                WriteLine("</Sequence>");

                UnindentLevel();
                WriteLine("</Switch.Default>");
            }

            foreach (Tuple<ExpressionAst, StatementBlockAst> switchClause in switchStatementAst.Clauses)
            {
                // Verify they've typed a literal (i.e.: integer, string)
                ConstantExpressionAst key = switchClause.Item1 as ConstantExpressionAst;
                if (key == null)
                {
                    ReportError("SwitchOnlySupportsConstantExpression", ActivityResources.SwitchOnlySupportsConstantExpression, switchClause.Item1.Extent);
                    return null;
                }
                string switchKey = key.Value.ToString();

                WriteLine(@"<Sequence x:Key=""" + EncodeStringNonArgument(switchKey, false) + @""">");
                IndentLevel();

                switchClause.Item2.Visit(this);

                UnindentLevel();
                WriteLine("</Sequence>");
            }

            UnindentLevel();
            WriteLine("</Switch>");

            return null;
        }

        private bool IsSupportedSwitchExpressionType(Type switchType)
        {
            bool isSupportedSwitchExpressionType =
                switchType.IsPrimitive ||
                (switchType == typeof(string));

            return isSupportedSwitchExpressionType;
        }

        object ICustomAstVisitor.VisitDataStatement(DataStatementAst dataStatementAst)
        {
            ReportError("DataSectionNotSupported", ActivityResources.DataSectionNotSupported, dataStatementAst.Extent);
            return null;
        }

        object ICustomAstVisitor.VisitForEachStatement(ForEachStatementAst forEachStatementAst)
        {
            if (!String.IsNullOrEmpty(forEachStatementAst.Label))
            {
                ReportError("LoopLabelNotSupported", ActivityResources.LoopLabelNotSupported, forEachStatementAst.Extent);
            }

            GenerateSymbolicInformation(forEachStatementAst.Extent);

            // Generate a temporary variable for the foreach condition
            string tempVarName = GenerateUniqueVariableName("ForeachCondition");
            Type conditionType = DetectType(tempVarName, false, forEachStatementAst.Condition);
            DefineVariable(tempVarName, conditionType, forEachStatementAst.Condition.Extent, null);

            // Generate the assignment of the the clause to the temporary variable
            string conditionExpression = GetPowerShellValueExpression(forEachStatementAst.Condition);
            GenerateAssignment(tempVarName, forEachStatementAst.Condition.Extent, TokenKind.Equals, forEachStatementAst.Condition, conditionExpression);

            // Clear the storage variable if there is one
            if(forEachStatementAst.Parent is AssignmentStatementAst)
            {
                StorageVariable currentVariable = GetVariableToUse();
                GeneratePowerShellValue(conditionType, "$null", false, currentVariable.VariableName);
            }

            // Check if this is a parallel foreach. If so, update the activity name.
            string activityName = "ForEach";
            bool isParallel = false;

            if ((forEachStatementAst.Flags & ForEachFlags.Parallel) == ForEachFlags.Parallel)
            {
                activityName = GetFriendlyName(null, typeof(ThrottledParallelForEach<Object>));
                isParallel = true;
            }

            WriteLine("<" + activityName + @" x:TypeArguments=""x:Object"">");
            IndentLevel();

            WriteLine("<" + activityName + ".Values>");
            IndentLevel();

            Type resultType = typeof(IEnumerable<object>);
            string convertedTypeName = GetConvertedTypeName(resultType);

            WriteLine(@"<InArgument x:TypeArguments=""" + convertedTypeName + @""">");
            IndentLevel();

            // Evaluate the expression for the ForEach values. We force this to evaluate as a list so that we
            // can be guaranteed to iterate over it.
            string expression = "$foreachIterator = $" + tempVarName + "; if($null -eq $foreachIterator) { ,@() } else { ,@($foreachIterator) }";
            GeneratePowerShellValue(resultType, expression, false, false);

            UnindentLevel();
            WriteLine("</InArgument>");

            UnindentLevel();
            WriteLine("</" + activityName + ".Values>");

            // Generate throttle limit if specified
            if (isParallel && (forEachStatementAst.ThrottleLimit != null))
            {
                WriteLine("<" + activityName + ".ThrottleLimit>");
                IndentLevel();

                resultType = typeof(int);
                convertedTypeName = GetConvertedTypeName(resultType);

                WriteLine(@"<InArgument x:TypeArguments=""" + convertedTypeName + @""">");
                IndentLevel();

                GeneratePowerShellValue(typeof(int), forEachStatementAst.ThrottleLimit.Extent.Text, false, false);

                UnindentLevel();
                WriteLine("</InArgument>");

                UnindentLevel();
                WriteLine("</" + activityName + ".ThrottleLimit>");
            }

            // Generate the index variable
            WriteLine(@"<ActivityAction x:TypeArguments=""x:Object"">");
            IndentLevel();

            WriteLine("<ActivityAction.Argument>");
            IndentLevel();

            // Convert their variable name to the original case if needed
            string variableName = forEachStatementAst.Variable.VariablePath.ToString();
            VariableDefinition existingVariableDefinition = GetVariableDefinition(variableName);
            if (existingVariableDefinition != null)
            {
                variableName = existingVariableDefinition.Name;
            }

            WriteLine(@"<DelegateInArgument x:TypeArguments=""x:Object"" Name=""" + variableName + @""" />");

            UnindentLevel();
            WriteLine("</ActivityAction.Argument>");

            // Generate the Try/Catch block to capture all terminating errors
            // so that that terminating error won't terminate other branches
            if (isParallel)
            {
                AddTryCatchForParallelStart();
            }

            // And the statement body
            WriteLine("<Sequence>");
            IndentLevel();
            
            if (isParallel)
            {
                EnterScope();
            }

            try
            {
                forEachStatementAst.Body.Visit(this);
            }
            finally
            {
                if (isParallel)
                {
                    DumpVariables("Sequence");
                    LeaveScope();
                }

                UnindentLevel();
                WriteLine("</Sequence>");

                // Finish the Try/Catch block that is to capture all terminating errors
                if (isParallel)
                {
                    AddTryCatchForParallelEnd();
                }

                UnindentLevel();
                WriteLine(@"</ActivityAction>");

                UnindentLevel();
                WriteLine("</" + activityName + ">");
            }

            return null;
        }

        object ICustomAstVisitor.VisitDoWhileStatement(DoWhileStatementAst doWhileStatementAst)
        {
            if (!String.IsNullOrEmpty(doWhileStatementAst.Label))
            {
                ReportError("LoopLabelNotSupported", ActivityResources.LoopLabelNotSupported, doWhileStatementAst.Extent);
            }

            GenerateSymbolicInformation(doWhileStatementAst.Extent);
            GenerateLoop(doWhileStatementAst.Condition, doWhileStatementAst.Body, null, "DoWhile", false);
            return null;
        }

        object ICustomAstVisitor.VisitForStatement(ForStatementAst forStatementAst)
        {
            if (!String.IsNullOrEmpty(forStatementAst.Label))
            {
                ReportError("LoopLabelNotSupported", ActivityResources.LoopLabelNotSupported, forStatementAst.Extent);
            }

            // We compile a 'for' statement into a while statement.
            // for(<initializer>; <condition>; <iterator>) { <statements> }
            // Converts to:
            // <initializer>
            // while(<condition>) { <statements>; <iterator> }

            GenerateSymbolicInformation(forStatementAst.Extent);

            // Visit the initializer
            if (forStatementAst.Initializer != null)
            {
                forStatementAst.Initializer.Visit(this);
            }

            // Generate the loop
            GenerateLoop(forStatementAst.Condition, forStatementAst.Body, forStatementAst.Iterator, "While", false);

            return null;
        }

        object ICustomAstVisitor.VisitWhileStatement(WhileStatementAst whileStatementAst)
        {
            if (!String.IsNullOrEmpty(whileStatementAst.Label))
            {
                ReportError("LoopLabelNotSupported", ActivityResources.LoopLabelNotSupported, whileStatementAst.Extent);
            }

            GenerateSymbolicInformation(whileStatementAst.Extent);
            GenerateLoop(whileStatementAst.Condition, whileStatementAst.Body, null, "While", false);
            return null;
        }

        private void GenerateLoop(PipelineBaseAst condition, StatementBlockAst body, PipelineBaseAst iterator, string whileType, bool isUntil)
        {
            // Check if the condition itself contains side-effects. If so, generate an error.
            string nameOfUnSupportedVariableFound;
            if ((condition != null) && CheckIfExpressionHasUnsupportedVariableOrHasSideEffects(null, condition, out nameOfUnSupportedVariableFound))
            {
                if (string.IsNullOrEmpty(nameOfUnSupportedVariableFound))
                {
                    string errorTemplate = ActivityResources.ConditionsCannotHaveSideEffects;
                    ReportError("ConditionsCannotHaveSideEffects", errorTemplate, condition.Extent);
                }
                else
                {
                    string error = String.Format(CultureInfo.InvariantCulture,
                                                 ActivityResources.VariableNotSupportedInWorkflow,
                                                 nameOfUnSupportedVariableFound);
                    ReportError("VariableNotSupportedInWorkflow", error, condition.Extent);
                }
            }

            // Check if the condition contains a command call. If so, generate an error
            if ((condition != null) && AstContainsCommandCall(condition, null))
            {
                string errorTemplate = ActivityResources.ConditionsCannotInvokeActivities;
                ReportError("ConditionsCannotInvokeActivities", errorTemplate, condition.Extent);
            }

            WriteLine(String.Format(CultureInfo.InvariantCulture, "<{0}>", whileType));
            IndentLevel();

            WriteLine(String.Format(CultureInfo.InvariantCulture, "<{0}.Condition>", whileType));
            IndentLevel();

            // Get a default condition if needed
            string conditionExpression = null;
            if (condition == null)
            {
                conditionExpression = "$true";
            }
            else
            {
                conditionExpression = GetPowerShellValueExpression(condition);
            }

            // If this is an 'until' loop, negate the condition
            if (isUntil)
            {
                conditionExpression = "-not (" + conditionExpression + ")";
            }

            // Evaluate the PowerShell value of the condition
            GeneratePowerShellValue(typeof(bool), conditionExpression, false, false);

            UnindentLevel();
            WriteLine(String.Format(CultureInfo.InvariantCulture, "</{0}.Condition>", whileType));

            WriteLine("<Sequence>");
            IndentLevel();

            body.Visit(this);
            if (iterator != null)
            {
                iterator.Visit(this);
            }

            UnindentLevel();
            WriteLine("</Sequence>");

            UnindentLevel();
            WriteLine(String.Format(CultureInfo.InvariantCulture, "</{0}>", whileType));
        }

        private static bool AstContainsCommandCall(PipelineBaseAst condition, HashSet<string> allowedCommands)
        {
            Func<Ast, bool> searcher = (ast) =>
            {
                CommandAst command = ast as CommandAst;
                if (command == null)
                {
                    return false;
                }

                if (allowedCommands == null)
                    return true;

                var commandName = command.GetCommandName();
                return commandName == null || !allowedCommands.Contains(commandName);
            };

            Ast result = condition.Find(searcher, searchNestedScriptBlocks: true);
            return result != null;
        }

        object ICustomAstVisitor.VisitCatchClause(CatchClauseAst catchClauseAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitTryStatement(TryStatementAst tryStatementAst)
        {
            GenerateSymbolicInformation(tryStatementAst.Extent);

            string exceptionToRethrowName = GenerateUniqueVariableName("__ExceptionToRethrow");
            string exceptionTypeFriendlyName = GetConvertedTypeName(typeof(Exception));

            // In Workflow, try / catch / finally acts unlike any other common language.
            // The Finally block is only processed if the try block is fully processed,
            // or one of its catch clauses are fully processed.
            // To work around this, we need to wrap the try / catch in another try / catch
            // and rethrow it in the finally clause if one is there.
            if (tryStatementAst.Finally != null)
            {
                // Create the exceptionToRethrow variable
                if (!VariableDefinedInCurrentScope(exceptionToRethrowName))
                {
                    DefineVariable(exceptionToRethrowName, typeof(Exception), tryStatementAst.Finally.Extent, null);
                }

                WriteLine("<TryCatch>");
                IndentLevel();
                WriteLine("<TryCatch.Try>");
                IndentLevel();
            }

            // Generate the original try
            WriteLine("<TryCatch>");
            IndentLevel();
            WriteLine("<TryCatch.Try>");
            IndentLevel();

            // Generate an inner Try/Catch that can unwrap the RuntimeExceptions that we
            // get from remoting.
            WriteLine("<TryCatch>");
            IndentLevel();
            WriteLine("<TryCatch.Try>");

            IndentLevel();
            WriteLine("<Sequence>");
            IndentLevel();

            tryStatementAst.Body.Visit(this);

            UnindentLevel();
            WriteLine("</Sequence>");
            UnindentLevel();
            WriteLine("</TryCatch.Try>");
            WriteLine("<TryCatch.Catches>");
            IndentLevel();
            Type runtimeExceptionType = typeof(RuntimeException);
            string runtimeExceptionTypeFriendlyName = GetConvertedTypeName(runtimeExceptionType);
            WriteLine(@"<Catch x:TypeArguments=""" + runtimeExceptionTypeFriendlyName + @""">");
            IndentLevel();
            WriteLine(@"<ActivityAction x:TypeArguments=""" + runtimeExceptionTypeFriendlyName + @""">");
            IndentLevel();
            WriteLine(@"<ActivityAction.Argument>");
            IndentLevel();
            WriteLine(@"<DelegateInArgument x:TypeArguments=""" + runtimeExceptionTypeFriendlyName + @""" Name=""__RuntimeException"" />");
            UnindentLevel();
            WriteLine(@"</ActivityAction.Argument>");
            WriteLine(@"<If Condition=""[__RuntimeException.InnerException IsNot Nothing]"">");
            IndentLevel();
            WriteLine(@"<If.Then>");
            IndentLevel();
            WriteLine(@"<Throw Exception=""[__RuntimeException.InnerException]"" />");
            UnindentLevel();
            WriteLine(@"</If.Then>");
            WriteLine(@"<If.Else>");
            IndentLevel();
            WriteLine(@"<Rethrow />");
            UnindentLevel();
            WriteLine(@"</If.Else>");
            UnindentLevel();
            WriteLine(@"</If>");
            UnindentLevel();
            WriteLine("</ActivityAction>");
            UnindentLevel();
            WriteLine(@"</Catch>");

            UnindentLevel();
            WriteLine("</TryCatch.Catches>");

            UnindentLevel();
            WriteLine("</TryCatch>");

            UnindentLevel();
            WriteLine("</TryCatch.Try>");

            // Generate the catches
            if (tryStatementAst.CatchClauses.Count > 0)
            {
                WriteLine("<TryCatch.Catches>");
                IndentLevel();
                string friendlyTypeName = string.Empty;

                if (hasControlFlowException)
                {
                    friendlyTypeName = GetConvertedTypeName(typeof(Microsoft.PowerShell.Workflow.WorkflowReturnException));
                    WriteLine(@"<Catch x:TypeArguments=""" + friendlyTypeName + @""">");
                    IndentLevel();
                    WriteLine(@"<ActivityAction x:TypeArguments=""" + friendlyTypeName + @""">");
                    IndentLevel();
                    WriteLine(@"<ActivityAction.Argument>");
                    IndentLevel();
                    WriteLine(@"<DelegateInArgument x:TypeArguments=""" + friendlyTypeName + @""" Name=""_"" />");
                    UnindentLevel();
                    WriteLine(@"</ActivityAction.Argument>");
                    WriteLine(@"<Rethrow />");
                    UnindentLevel();
                    WriteLine("</ActivityAction>");
                    UnindentLevel();
                    WriteLine(@"</Catch>");
                }

                foreach (CatchClauseAst catchClause in tryStatementAst.CatchClauses)
                {
                    List<Type> catchTypes = new List<Type>();
                    
                    if (catchClause.IsCatchAll)
                    {
                        catchTypes.Add(typeof(System.Exception));
                    }
                    else
                    {
                        foreach (TypeConstraintAst catchType in catchClause.CatchTypes)
                        {
                            Type reflectionType = catchType.TypeName.GetReflectionType();
                            if (reflectionType != null)
                            {
                                catchTypes.Add(reflectionType);
                            }
                            else
                            {
                                string error = String.Format(CultureInfo.InvariantCulture, ActivityResources.NewObjectCouldNotFindType, catchType.TypeName);
                                ReportError("ExceptionTypeNotFound", error, tryStatementAst.Extent);
                            }
                        }
                    }

                    foreach (Type actualType in catchTypes)
                    {
                        friendlyTypeName = GetConvertedTypeName(actualType);

                        WriteLine(@"<Catch x:TypeArguments=""" + friendlyTypeName + @""">");
                        IndentLevel();
                        WriteLine(@"<ActivityAction x:TypeArguments=""" + friendlyTypeName + @""">");
                        IndentLevel();
                        WriteLine(@"<ActivityAction.Argument>");
                        IndentLevel();
                        WriteLine(@"<DelegateInArgument x:TypeArguments=""" + friendlyTypeName + @""" Name=""_"" />");
                        UnindentLevel();
                        WriteLine(@"</ActivityAction.Argument>");
                        

                        WriteLine("<Sequence>");
                        IndentLevel();

                        catchClause.Body.Visit(this);

                        UnindentLevel();
                        WriteLine("</Sequence>");
                        UnindentLevel();

                        WriteLine("</ActivityAction>");
                        UnindentLevel();
                        WriteLine(@"</Catch>");
                    }
                }

                UnindentLevel();
                WriteLine("</TryCatch.Catches>");
            }

            // Generate the finally
            if (tryStatementAst.Finally != null)
            {
                // Close the original try / catch. Since we're bringing the original
                // finally into a new catch statement, we need to sythesize a dummy one here.
                if (tryStatementAst.CatchClauses.Count == 0)
                {
                    WriteLine("<TryCatch.Finally><Sequence /></TryCatch.Finally>");
                }

                UnindentLevel();
                WriteLine("</TryCatch>");

                // Now add our catch statement to emulate a real finally block
                UnindentLevel();
                WriteLine("</TryCatch.Try>");
                WriteLine("<TryCatch.Catches>");
                IndentLevel();

                WriteLine(@"<Catch x:TypeArguments=""" + exceptionTypeFriendlyName + @""">");
                IndentLevel();
                WriteLine(@"<ActivityAction x:TypeArguments=""" + exceptionTypeFriendlyName + @""">");
                IndentLevel();
                // Store the variable we caught
                WriteLine(@"<ActivityAction.Argument>");
                IndentLevel();
                WriteLine(@"<DelegateInArgument Name=""__UnhandledException"" x:TypeArguments=""" + exceptionTypeFriendlyName + @""" />");
                UnindentLevel();
                WriteLine(@"</ActivityAction.Argument>");

                WriteLine(@"<Assign>");
                IndentLevel();
                WriteLine(@"<Assign.To>");
                IndentLevel();
                WriteLine(@"<OutArgument  x:TypeArguments=""" + exceptionTypeFriendlyName + @""">[" + exceptionToRethrowName + @"]</OutArgument>");
                UnindentLevel();
                WriteLine(@"</Assign.To>");
                WriteLine(@"<Assign.Value>");
                IndentLevel();
                WriteLine(@"<InArgument  x:TypeArguments=""" + exceptionTypeFriendlyName + @""">[__UnhandledException]</InArgument>");
                UnindentLevel();
                WriteLine(@"</Assign.Value>");
                UnindentLevel();
                WriteLine(@"</Assign>");

                UnindentLevel();
                WriteLine("</ActivityAction>");
                UnindentLevel();
                WriteLine(@"</Catch>");
                UnindentLevel();
                WriteLine("</TryCatch.Catches>");

                WriteLine("<TryCatch.Finally>");
                IndentLevel();
                WriteLine("<Sequence>");
                IndentLevel();

                tryStatementAst.Finally.Visit(this);

                // Rethrow any exception we may have eaten
                WriteLine(@"<If Condition=""[" + exceptionToRethrowName + @" IsNot Nothing]"">");
                IndentLevel();
                WriteLine("<If.Then>");
                IndentLevel();
                WriteLine(@"<Throw Exception=""[" + exceptionToRethrowName + @"]"" />");
                UnindentLevel();
                WriteLine("</If.Then>");
                UnindentLevel();
                WriteLine("</If>");

                UnindentLevel();
                WriteLine("</Sequence>");
                UnindentLevel();
                WriteLine("</TryCatch.Finally>");
            }

            UnindentLevel();
            WriteLine("</TryCatch>");

            return null;
        }

        object ICustomAstVisitor.VisitBreakStatement(BreakStatementAst breakStatementAst)
        {
            ReportError("BreakContinueNotSupported", ActivityResources.BreakContinueNotSupported, breakStatementAst.Extent);
            return null;
        }

        object ICustomAstVisitor.VisitContinueStatement(ContinueStatementAst continueStatementAst)
        {
            ReportError("BreakContinueNotSupported", ActivityResources.BreakContinueNotSupported, continueStatementAst.Extent);
            return null;
        }

        object ICustomAstVisitor.VisitReturnStatement(ReturnStatementAst returnStatementAst)
        {
            if (returnStatementAst.Pipeline != null)
            {
                returnStatementAst.Pipeline.Visit(this);
            }

            // Throw a hard-coded exception here. Our global exception handler catches this and exits quietly.
            WriteLine(@"<Throw Exception=""[New Microsoft.PowerShell.Workflow.WorkflowReturnException()]"" />");
            this.hasControlFlowException = true;

            return null;
        }

        object ICustomAstVisitor.VisitExitStatement(ExitStatementAst exitStatementAst)
        {
            // Throw a hard-coded exception here. Our global exception handler catches this and exits quietly.
            WriteLine(@"<Throw Exception=""[New Microsoft.PowerShell.Workflow.WorkflowReturnException()]"" />");
            this.hasControlFlowException = true;

            return null;
        }

        object ICustomAstVisitor.VisitThrowStatement(ThrowStatementAst throwStatementAst)
        {
            GenerateSymbolicInformation(throwStatementAst.Extent);

            Type exceptionType = typeof(System.Exception);
            bool isInParallelBlock = IsThrowStatementInParallelBlock(throwStatementAst);

            if (throwStatementAst.IsRethrow)
            {
                if (isInParallelBlock)
                {
                    // Define the temp variable
                    string rethrowExceptionName = GenerateUniqueVariableName("__RethrowException");
                    if (!VariableDefinedInCurrentScope(rethrowExceptionName))
                    {
                        DefineVariable(rethrowExceptionName, exceptionType, throwStatementAst.Extent, null);
                    }

                    try
                    {
                        // Generate the exception and assign it to the 'rethrowExceptionName' variable
                        EnterStorage(rethrowExceptionName, false);
                        GeneratePowerShellValue(exceptionType, "$_", false, true);
                    }
                    finally
                    {
                        LeaveStorage();
                    }

                    // Check if the exception is thrown by the 'Throw' statement. If not, add the special key-value pair to 
                    // the 'Data' property. Then we re-throw the exception.
                    GenerateXamlForThrowStatement(rethrowExceptionName, isRethrow: true);
                }
                else
                {
                    WriteLine("<Rethrow />");
                }
            }
            else
            {
                if (throwStatementAst.Pipeline == null)
                {
                    ReportError("ReasonRequiredInThrowStatement", ActivityResources.ReasonRequiredInThrowStatement, throwStatementAst.Extent);
                    return null;
                }

                // Verify it's not a pipeline with commands
                HashSet<string> allowedCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "New-Object", "new" };
                if (AstContainsCommandCall(throwStatementAst.Pipeline, allowedCommands))
                {
                    string errorTemplate = ActivityResources.ThrowStatementCannotInvokeActivities;
                    ReportError("ThrowStatementCannotInvokeActivities", errorTemplate, throwStatementAst.Pipeline.Extent);
                }
                
                if (isInParallelBlock)
                {
                    // Define the temp variable
                    string throwExceptionName = GenerateUniqueVariableName("__ThrowException");
                    if (!VariableDefinedInCurrentScope(throwExceptionName))
                    {
                        DefineVariable(throwExceptionName, exceptionType, throwStatementAst.Pipeline.Extent, null);
                    }

                    try
                    {
                        // Generate the exception and assign it to the 'throwExceptionName' variable
                        EnterStorage(throwExceptionName, false);
                        string expression = GetPowerShellValueExpression(throwStatementAst.Pipeline);
                        GeneratePowerShellValue(exceptionType, expression, false, true);
                    }
                    finally
                    {
                        LeaveStorage();
                    }

                    // Check if the exception is thrown by the 'Throw' statement. If not, add the special key-value pair to 
                    // the 'Data' property. Then we throw the exception.
                    GenerateXamlForThrowStatement(throwExceptionName, isRethrow: false);
                }
                else
                {
                    string exceptionTypeFriendlyName = GetConvertedTypeName(exceptionType);

                    WriteLine("<Throw>");
                    IndentLevel();

                    WriteLine("<Throw.Exception>");
                    IndentLevel();

                    WriteLine(@"<InArgument x:TypeArguments=""" + exceptionTypeFriendlyName + @""">");
                    IndentLevel();

                    string expression = GetPowerShellValueExpression(throwStatementAst.Pipeline);
                    GeneratePowerShellValue(exceptionType, expression, false, false);

                    UnindentLevel();
                    WriteLine("</InArgument>");

                    UnindentLevel();
                    WriteLine("</Throw.Exception>");

                    UnindentLevel();
                    WriteLine("</Throw>");
                }
            }

            return null;
        }

        /// <summary>
        /// Check if the 'Throw' statement is in a parallel block
        /// </summary>
        /// <param name="throwStatementAst"></param>
        /// <returns></returns>
        private static bool IsThrowStatementInParallelBlock(ThrowStatementAst throwStatementAst)
        {
            var parent = throwStatementAst.Parent;
            while (parent != null)
            {
                var blockStatementAst = parent as BlockStatementAst;
                if (blockStatementAst != null)
                {
                    if (blockStatementAst.Kind.Kind == TokenKind.Parallel)
                    {
                        return true;
                    }
                }
                else
                {
                    var forEachStatementAst = parent as ForEachStatementAst;
                    if (forEachStatementAst != null && (forEachStatementAst.Flags & ForEachFlags.Parallel) == ForEachFlags.Parallel)
                    {
                        return true;
                    }
                }

                parent = parent.Parent;
            }

            return false;
        }

        /// <summary>
        /// Generate the XAML for the 'Throw' statement
        /// </summary>
        /// <param name="variableName"></param>
        /// <param name="isRethrow"></param>
        private void GenerateXamlForThrowStatement(string variableName, bool isRethrow)
        {
            // Add a special key value pair to the Data property of the exception, so that we know it's thrown by the 'Throw' statement
            WriteLine(@"<If Condition=""[Not " + variableName + @".Data.Contains(&quot;" + M3PKeyForThrowStatement + @"&quot;)]"">");
            IndentLevel();
            WriteLine(@"<If.Then>");
            IndentLevel();

            AddOrRemoveSpecialKey(variableName, addKey: true);
            
            UnindentLevel();
            WriteLine(@"</If.Then>");
            UnindentLevel();
            WriteLine(@"</If>");

            if (isRethrow)
            {
                WriteLine(@"<Rethrow />");
            }
            else
            {
                WriteLine(@"<Throw Exception=""[" + variableName + @"]"" />");
            }
        }

        /// <summary>
        /// Add or remove the special key-value pair to/from the exception
        /// </summary>
        /// <param name="variableName"></param>
        /// <param name="addKey"></param>
        private void AddOrRemoveSpecialKey(string variableName, bool addKey)
        {
            string iDictionaryTypeFriendlyName = GetConvertedTypeName(typeof(System.Collections.IDictionary));
            string objectTypeFriendlyName = GetConvertedTypeName(typeof(object));
            string method = addKey ? "Add" : "Remove";

            WriteLine(@"<InvokeMethod MethodName=""" + method + @""">");
            IndentLevel();
            WriteLine(@"<InvokeMethod.TargetObject>");
            IndentLevel();
            WriteLine(@"<InArgument x:TypeArguments=""" + iDictionaryTypeFriendlyName + @""">[" + variableName + @".Data]</InArgument>");
            UnindentLevel();
            WriteLine(@"</InvokeMethod.TargetObject>");
            WriteLine(@"<InArgument x:TypeArguments=""" + objectTypeFriendlyName + @""">[""" + M3PKeyForThrowStatement + @"""]</InArgument>");

            if (addKey)
            {
                WriteLine(@"<InArgument x:TypeArguments=""" + objectTypeFriendlyName + @""">[""""]</InArgument>");
            }

            UnindentLevel();
            WriteLine(@"</InvokeMethod>");
        }

        /// <summary>
        /// Start to add the Try/Catch block for the parallel statement/block
        /// </summary>
        private void AddTryCatchForParallelStart()
        {
            WriteLine("<TryCatch>");
            IndentLevel();
            WriteLine("<TryCatch.Try>");
            IndentLevel();
        }

        /// <summary>
        /// Finish the Try/Catch block for the parallel statement/block
        /// </summary>
        private void AddTryCatchForParallelEnd()
        {
            UnindentLevel();
            WriteLine("</TryCatch.Try>");
            WriteLine("<TryCatch.Catches>");
            IndentLevel();

            string exceptionTypeFriendlyName = GetConvertedTypeName(typeof(Exception));
            string writeErrorFriendlyName = GetFriendlyName(null, typeof(Microsoft.PowerShell.Utility.Activities.WriteError));

            WriteLine(@"<Catch x:TypeArguments=""" + exceptionTypeFriendlyName + @""">");
            IndentLevel();
            WriteLine(@"<ActivityAction x:TypeArguments=""" + exceptionTypeFriendlyName + @""">");
            IndentLevel();
            WriteLine(@"<ActivityAction.Argument>");
            IndentLevel();
            WriteLine(@"<DelegateInArgument x:TypeArguments=""" + exceptionTypeFriendlyName + @""" Name=""__UnhandledException"" />");
            UnindentLevel();
            WriteLine(@"</ActivityAction.Argument>");
            WriteLine(@"<If Condition=""[__UnhandledException.Data.Contains(&quot;" + M3PKeyForThrowStatement + @"&quot;)]"">");
            IndentLevel();
            WriteLine(@"<If.Then>");
            IndentLevel();

            WriteLine(@"<Sequence>");
            IndentLevel();
            AddOrRemoveSpecialKey("__UnhandledException", addKey: false); // Remove the special key-value pair
            WriteLine(@"<Rethrow />"); // Rethrow the exception if it was thrown by the 'Throw' statement
            UnindentLevel();
            WriteLine(@"</Sequence>");

            UnindentLevel();
            WriteLine(@"</If.Then>");
            WriteLine(@"<If.Else>");
            IndentLevel();
            WriteLine(@"<" + writeErrorFriendlyName + @" Exception=""[__UnhandledException]"" />"); // Otherwise, write to the error stream
            UnindentLevel();
            WriteLine(@"</If.Else>");
            UnindentLevel();
            WriteLine(@"</If>");
            UnindentLevel();
            WriteLine("</ActivityAction>");
            UnindentLevel();
            WriteLine(@"</Catch>");

            UnindentLevel();
            WriteLine("</TryCatch.Catches>");
            UnindentLevel();
            WriteLine("</TryCatch>");
        }

        object ICustomAstVisitor.VisitVariableExpression(VariableExpressionAst variableExpressionAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitDoUntilStatement(DoUntilStatementAst doUntilStatementAst)
        {
            if (!String.IsNullOrEmpty(doUntilStatementAst.Label))
            {
                ReportError("LoopLabelNotSupported", ActivityResources.LoopLabelNotSupported, doUntilStatementAst.Extent);
            }

            GenerateSymbolicInformation(doUntilStatementAst.Extent);
            GenerateLoop(doUntilStatementAst.Condition, doUntilStatementAst.Body, null, "DoWhile", true);
            return null;
        }

        object ICustomAstVisitor.VisitMemberExpression(MemberExpressionAst memberExpressionAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitInvokeMemberExpression(InvokeMemberExpressionAst invokeMemberExpressionAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitArrayExpression(ArrayExpressionAst arrayExpressionAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitHashtable(HashtableAst hashtableAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitParenExpression(ParenExpressionAst parenExpressionAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitIndexExpression(IndexExpressionAst indexExpressionAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitBlockStatement(BlockStatementAst blockStatementAst)
        {
            GenerateSymbolicInformation(blockStatementAst.Extent);

            bool isParallelBlock = String.Equals(blockStatementAst.Kind.Text, TokenKind.Parallel.Text(), StringComparison.OrdinalIgnoreCase);
            string containerType = isParallelBlock ? "Parallel" : "Sequence";
            WriteLine("<" + containerType + ">");
            IndentLevel();
            EnterScope();

            try
            {
                blockStatementAst.Body.Visit(this);
            }
            finally
            {
                DumpVariables(containerType);
                LeaveScope();

                UnindentLevel();
                WriteLine("</" + containerType + ">");
            }

            return null;
        }

        object ICustomAstVisitor.VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst)
        {
            throw new NotSupportedException();
        }

        object ICustomAstVisitor.VisitNamedAttributeArgument(NamedAttributeArgumentAst namedAttributeArgumentAst)
        {
            throw new NotSupportedException();
        }

        private void ReportError(string errorId, string errorText, IScriptExtent extent)
        {
            _parseErrors.Add(new ParseError(extent, errorId, errorText));

            if (!this.ValidateOnly)
            {
                throw new ParseException(_parseErrors.ToArray());
            }
        }

        private int uniqueVariableDisambiguator = 0;
        private string GenerateUniqueVariableName(string basename)
        {
            return basename + "_" + (this.uniqueVariableDisambiguator++).ToString(CultureInfo.InvariantCulture);
        }

        internal enum ActivityKind
        {
            /// <summary>
            /// Delay activity
            /// </summary>
            Delay = 0,

            /// <summary>
            /// An InlineScript activity
            /// </summary>
            InlineScript = 1,

            /// <summary>
            /// Xaml injection activity. inline XAML
            /// </summary>
            InvokeExpression = 2,

            /// <summary>
            /// New-Object activity
            /// </summary>
            NewObject = 3,

            /// <summary>
            /// Persist activity
            /// </summary>
            Persist = 4,

            /// <summary>
            /// Other command activity
            /// </summary>
            RegularCommand = 5,

            /// <summary>
            /// Suspend activity
            /// </summary>
            Suspend = 6,
        }

        private enum IterativeCommands
        {
            None = 0,
            ForEachSequence,
            WhereSequence
        }
    }

    class CommandArgumentInfo
    {
        internal object Value { get; set; }
        internal object OriginalValue { get; set; }
        internal Ast ArgumentAst { get; set; }
        internal bool IsLiteral { get; set; }
    }

    class VariableScope
    {
        internal VariableScope()
        {
            Variables = new Dictionary<string, VariableDefinition>(StringComparer.OrdinalIgnoreCase);
        }

        internal Dictionary<string, VariableDefinition> Variables { get; set; }
    }

    class StorageVariable
    {
        internal StorageVariable(string variableName, bool isAggregatingVariable)
        {
            if (string.IsNullOrEmpty(variableName))
            {
                // caller needs to make sure the argument is not null or empty
                throw new PSArgumentNullException(variableName);
            }

            IsAggregatingVariable = isAggregatingVariable;
            VariableName = variableName;
        }

        /// <summary>
        /// This indicates if the variable is one that will aggregate results from a parallel/sequence/foreach block.
        /// For example:
        ///     workflow bar { $a = parallel { Get-Process -Name powershell; Get-Service -Name Dhcp } }
        /// $a here will contain all results generated from the parallel block, including a process object "powershell" 
        /// and a service object "Dhcp". We call $a an aggregating variable.
        /// </summary>
        internal bool IsAggregatingVariable { get; set; }
        internal string VariableName { get; private set; }
    }

    class VariableDefinition
    {
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal string Name { get; set; }
        internal string XamlDefinition { get; set; }
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Type Type { get; set; }
    }

    /// <summary>
    /// Used by Script->Xaml converter to give compile time error
    /// for variables that are not supported in a workflow context.
    /// </summary>
    internal enum PSWorkflowUnsupportedVariable
    {
        Args = 0,
        Error = 1,
        MyInvocation = 2,
        PSBoundParameters = 3,
        PSCmdlet = 4,
        PSCommandPath = 5,
        PSDefaultParameterValues = 6,
        PSScriptRoot = 7,
        StackTrace = 8,
        PID = 9
    }
    
    internal class ExpressionHasUnsupportedVariableOrSideEffectsVisitor : AstVisitor
    {
        internal bool ExpressionHasSideEffects { get; set; }
        internal string NameOfUnsupportedVariableFound { get; set; }

        string variableName;

        internal ExpressionHasUnsupportedVariableOrSideEffectsVisitor(string variableName)
        {
            ExpressionHasSideEffects = false;
            this.variableName = variableName;
        }

        public override AstVisitAction VisitBlockStatement(BlockStatementAst blockStatementAst)
        {
            return AstVisitAction.SkipChildren;
        }

        public override AstVisitAction VisitForEachStatement(ForEachStatementAst forEachStatementAst)
        {
            return AstVisitAction.SkipChildren;
        }

        // Stop visiting when we hit a command (like InlineScript)
        public override AstVisitAction VisitCommand(CommandAst commandAst)
        {
            return AstVisitAction.SkipChildren;
        }

        // Check if this is an assignment to another variable
        public override AstVisitAction VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst)
        {
            if (assignmentStatementAst != null)
            {
                VariableExpressionAst leftExpressionAst = assignmentStatementAst.Left as VariableExpressionAst;
                if (leftExpressionAst != null)
                {
                    string targetVariableName = leftExpressionAst.VariablePath.ToString();
                    if (!String.Equals(variableName, targetVariableName, StringComparison.OrdinalIgnoreCase))
                    {
                        ExpressionHasSideEffects = true;
                        return AstVisitAction.StopVisit;
                    }
                }
            }

            return base.VisitAssignmentStatement(assignmentStatementAst);
        }

        public override AstVisitAction VisitVariableExpression(VariableExpressionAst variableExpressionAst)
        {
            // ignore patterns like $env:
            if (variableExpressionAst.VariablePath.IsVariable)
            {
                // dont allow patterns like "pscmdlet","workflow:pscmdlet"
                string tempVariableName = variableExpressionAst.VariablePath.ToString();
                int indexOfColon = tempVariableName.IndexOf(':');
                if (indexOfColon != -1)
                {
                    tempVariableName = tempVariableName.Substring(indexOfColon + 1);
                }

                PSWorkflowUnsupportedVariable unused;
                if (Enum.TryParse<PSWorkflowUnsupportedVariable>(tempVariableName, true, out unused))
                {
                    ExpressionHasSideEffects = true;
                    NameOfUnsupportedVariableFound = tempVariableName;

                    return AstVisitAction.StopVisit;
                }
            }
            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst)
        {
            // ignore cases like "-bnot 100"
            switch (unaryExpressionAst.TokenKind)
            {
                case TokenKind.Minus:
                case TokenKind.Plus:
                case TokenKind.Not:
                case TokenKind.Bnot:
                case TokenKind.Exclaim:
                case TokenKind.Comma:
                case TokenKind.Csplit:
                case TokenKind.Isplit:
                case TokenKind.Join:
                    return AstVisitAction.SkipChildren;
            }

            if (unaryExpressionAst.Child is VariableExpressionAst)
            {
                // If needed, check if it assigned to another variable
                if (String.IsNullOrEmpty(variableName))
                {
                    ExpressionHasSideEffects = true;
                    return AstVisitAction.StopVisit;
                }

                VariableExpressionAst referenceVariable = unaryExpressionAst.Child as VariableExpressionAst;
                if (!String.Equals(variableName, referenceVariable.VariablePath.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    ExpressionHasSideEffects = true;
                    return AstVisitAction.StopVisit;
                }
            }

            return base.VisitUnaryExpression(unaryExpressionAst);
        }
    }
}


