// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation.Runspaces;
using System.Runtime.CompilerServices;

using Microsoft.PowerShell.Commands;

namespace System.Management.Automation.Language
{
    internal enum ScopeType
    {
        Type,           // class or enum
        Method,         // class method
        Function,       // function
        ScriptBlock     // script or anonymous script block
    }

    internal class TypeLookupResult
    {
        public TypeLookupResult(TypeDefinitionAst type = null)
        {
            Type = type;
        }

        public TypeDefinitionAst Type { get; set; }

        public List<string> ExternalNamespaces { get; set; }

        public bool IsAmbiguous()
        {
            return (ExternalNamespaces != null && ExternalNamespaces.Count > 1);
        }
    }

    internal class Scope
    {
        internal Ast _ast;
        internal ScopeType _scopeType;
        /// <summary>
        /// TypeTable maps namespace (currently it's module name) to the types under this namespace.
        /// For the types defined in the current namespace (module) we use CURRENT_NAMESPACE as a namespace.
        /// </summary>
        private readonly Dictionary<string, TypeLookupResult> _typeTable;
        private readonly Dictionary<string, Ast> _variableTable;

        internal Scope(IParameterMetadataProvider ast, ScopeType scopeType)
        {
            _ast = (Ast)ast;
            _scopeType = scopeType;
            _typeTable = new Dictionary<string, TypeLookupResult>(StringComparer.OrdinalIgnoreCase);
            _variableTable = new Dictionary<string, Ast>(StringComparer.OrdinalIgnoreCase);
        }

        internal Scope(TypeDefinitionAst typeDefinition)
        {
            _ast = typeDefinition;
            _scopeType = ScopeType.Type;
            _typeTable = new Dictionary<string, TypeLookupResult>(StringComparer.OrdinalIgnoreCase);
            _variableTable = new Dictionary<string, Ast>(StringComparer.OrdinalIgnoreCase);
            foreach (var member in typeDefinition.Members)
            {
                var propertyMember = member as PropertyMemberAst;
                if (propertyMember != null)
                {
                    // Duplicate members are an error, but we catch that later after all types
                    // have been resolved.  We could report errors for properties here, but
                    // we couldn't compare methods because overloads can't be compared until types
                    // are resolved.
                    if (!_variableTable.ContainsKey(propertyMember.Name))
                    {
                        _variableTable.Add(propertyMember.Name, propertyMember);
                    }
                }
            }
        }

        internal void AddType(Parser parser, TypeDefinitionAst typeDefinitionAst)
        {
            TypeLookupResult result;
            if (_typeTable.TryGetValue(typeDefinitionAst.Name, out result))
            {
                if (result.ExternalNamespaces != null)
                {
                    // override external type by the type defined in the current namespace
                    result.ExternalNamespaces = null;
                    result.Type = typeDefinitionAst;
                }
                else
                {
                    parser.ReportError(typeDefinitionAst.Extent,
                        nameof(ParserStrings.MemberAlreadyDefined),
                        ParserStrings.MemberAlreadyDefined,
                        typeDefinitionAst.Name);
                }
            }
            else
            {
                _typeTable.Add(typeDefinitionAst.Name, new TypeLookupResult(typeDefinitionAst));
            }
        }

        internal void AddTypeFromUsingModule(Parser parser, TypeDefinitionAst typeDefinitionAst, PSModuleInfo moduleInfo)
        {
            TypeLookupResult result;
            if (_typeTable.TryGetValue(typeDefinitionAst.Name, out result))
            {
                if (result.ExternalNamespaces != null)
                {
                    // override external type by the type defined in the current namespace
                    result.ExternalNamespaces.Add(moduleInfo.Name);
                }
            }
            else
            {
                var newLookupEntry = new TypeLookupResult(typeDefinitionAst)
                {
                    ExternalNamespaces = new List<string>()
                };
                newLookupEntry.ExternalNamespaces.Add(moduleInfo.Name);
                _typeTable.Add(typeDefinitionAst.Name, newLookupEntry);
            }

            string fullName = SymbolResolver.GetModuleQualifiedName(moduleInfo.Name, typeDefinitionAst.Name);
            if (_typeTable.TryGetValue(fullName, out result))
            {
                parser.ReportError(typeDefinitionAst.Extent,
                    nameof(ParserStrings.MemberAlreadyDefined),
                    ParserStrings.MemberAlreadyDefined,
                    fullName);
            }
            else
            {
                _typeTable.Add(fullName, new TypeLookupResult(typeDefinitionAst));
            }
        }

        internal TypeLookupResult LookupType(TypeName typeName)
        {
            if (typeName.AssemblyName != null)
            {
                return null;
            }

            TypeLookupResult typeLookupResult;
            _typeTable.TryGetValue(typeName.Name, out typeLookupResult);
            return typeLookupResult;
        }

        public Ast LookupVariable(VariablePath variablePath)
        {
            Ast variabledefinition;
            _variableTable.TryGetValue(variablePath.UserPath, out variabledefinition);
            return variabledefinition;
        }
    }

    internal class SymbolTable
    {
        internal readonly List<Scope> _scopes;
        internal readonly Parser _parser;

        internal SymbolTable(Parser parser)
        {
            _scopes = new List<Scope>();
            _parser = parser;
        }

        internal void AddTypesInScope(Ast ast)
        {
            // On entering a scope, we first add all the types defined in this scope (but not any nested scopes)
            // This way, we can support types that refer to each other, e.g.
            //     class C1 { [C2]$x }
            //     class C2 { [C1]$c1 }

            var types = ast.FindAll(static x => x is TypeDefinitionAst, searchNestedScriptBlocks: false);
            foreach (var type in types)
            {
                AddType((TypeDefinitionAst)type);
            }
        }

        internal void EnterScope(IParameterMetadataProvider ast, ScopeType scopeType)
        {
            var scope = new Scope(ast, scopeType);
            _scopes.Add(scope);
            AddTypesInScope((Ast)ast);
        }

        internal void EnterScope(TypeDefinitionAst typeDefinition)
        {
            var scope = new Scope(typeDefinition);
            _scopes.Add(scope);
            AddTypesInScope(typeDefinition);
        }

        internal void LeaveScope()
        {
            Diagnostics.Assert(_scopes.Count > 0, "Scope stack can't be empty when leaving a scope");
            _scopes.RemoveAt(_scopes.Count - 1);
        }

        /// <summary>
        /// Add Type to the symbol Table.
        /// </summary>
        /// <param name="typeDefinitionAst"></param>
        public void AddType(TypeDefinitionAst typeDefinitionAst)
        {
            _scopes[_scopes.Count - 1].AddType(_parser, typeDefinitionAst);
        }

        /// <summary>
        /// Add Type from the different module to the symbol Table.
        /// </summary>
        /// <param name="typeDefinitionAst"></param>
        /// <param name="moduleInfo"></param>
        public void AddTypeFromUsingModule(TypeDefinitionAst typeDefinitionAst, PSModuleInfo moduleInfo)
        {
            _scopes[_scopes.Count - 1].AddTypeFromUsingModule(_parser, typeDefinitionAst, moduleInfo);
        }

        public TypeLookupResult LookupType(TypeName typeName)
        {
            TypeLookupResult result = null;
            for (int i = _scopes.Count - 1; i >= 0; i--)
            {
                result = _scopes[i].LookupType(typeName);
                if (result != null)
                    break;
            }

            return result;
        }

        public Ast LookupVariable(VariablePath variablePath)
        {
            Ast result = null;
            for (int i = _scopes.Count - 1; i >= 0; i--)
            {
                result = _scopes[i].LookupVariable(variablePath);
                if (result != null)
                    break;
            }

            return result;
        }

        /// <summary>
        /// Return the most deep typeDefinitionAst in the current context.
        /// </summary>
        /// <returns>TypeDefinitionAst or null, if currently not in type definition.</returns>
        public TypeDefinitionAst GetCurrentTypeDefinitionAst()
        {
            for (int i = _scopes.Count - 1; i >= 0; i--)
            {
                TypeDefinitionAst ast = _scopes[i]._ast as TypeDefinitionAst;
                if (ast != null)
                {
                    return ast;
                }
            }

            return null;
        }

        public bool IsInMethodScope()
        {
            return _scopes[_scopes.Count - 1]._scopeType == ScopeType.Method;
        }
    }

    internal sealed class SymbolResolver : AstVisitor2, IAstPostVisitHandler
    {
        private readonly SymbolResolvePostActionVisitor _symbolResolvePostActionVisitor;
        internal readonly SymbolTable _symbolTable;
        internal readonly Parser _parser;
        internal readonly TypeResolutionState _typeResolutionState;

        [ThreadStatic]
        private static PowerShell t_usingStatementResolvePowerShell;

        private static PowerShell UsingStatementResolvePowerShell
        {
            get
            {
                // The goal is to re-use runspaces, because creating runspace is an expensive part in creating PowerShell instance.
                if (t_usingStatementResolvePowerShell == null)
                {
                    if (Runspace.DefaultRunspace != null)
                    {
                        t_usingStatementResolvePowerShell = PowerShell.Create(RunspaceMode.CurrentRunspace);
                    }
                    else
                    {
                        // Create empty iss and populate only commands, that we want to use.
                        InitialSessionState iss = InitialSessionState.Create();
                        iss.Commands.Add(new SessionStateCmdletEntry("Get-Module", typeof(GetModuleCommand), null));
                        var sessionStateProviderEntry = new SessionStateProviderEntry(FileSystemProvider.ProviderName, typeof(FileSystemProvider), null);
                        var snapin = PSSnapInReader.ReadEnginePSSnapIns().FirstOrDefault(static snapIn => snapIn.Name.Equals("Microsoft.PowerShell.Core", StringComparison.OrdinalIgnoreCase));
                        sessionStateProviderEntry.SetPSSnapIn(snapin);
                        iss.Providers.Add(sessionStateProviderEntry);
                        t_usingStatementResolvePowerShell = PowerShell.Create(iss);
                    }
                }
                else if (Runspace.DefaultRunspace != null && t_usingStatementResolvePowerShell.Runspace != Runspace.DefaultRunspace)
                {
                    t_usingStatementResolvePowerShell = PowerShell.Create(RunspaceMode.CurrentRunspace);
                }

                return t_usingStatementResolvePowerShell;
            }
        }

        private SymbolResolver(Parser parser, TypeResolutionState typeResolutionState)
        {
            _symbolTable = new SymbolTable(parser);
            _parser = parser;
            _typeResolutionState = typeResolutionState;
            _symbolResolvePostActionVisitor = new SymbolResolvePostActionVisitor { _symbolResolver = this };
        }

        internal static void ResolveSymbols(Parser parser, ScriptBlockAst scriptBlockAst)
        {
            Diagnostics.Assert(scriptBlockAst.Parent == null, "Can only resolve starting from the root");

            var usingState = scriptBlockAst.UsingStatements.Count > 0
                ? new TypeResolutionState(TypeOps.GetNamespacesForTypeResolutionState(scriptBlockAst.UsingStatements), TypeResolutionState.emptyAssemblies)
                : TypeResolutionState.GetDefaultUsingState(null);
            var resolver = new SymbolResolver(parser, usingState);
            resolver._symbolTable.EnterScope(scriptBlockAst, ScopeType.ScriptBlock);
            scriptBlockAst.Visit(resolver);
            resolver._symbolTable.LeaveScope();

            Diagnostics.Assert(resolver._symbolTable._scopes.Count == 0, "Somebody missed removing a scope");
        }

        public override AstVisitAction VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst)
        {
            _symbolTable.EnterScope(typeDefinitionAst);
            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst)
        {
            _symbolTable.EnterScope(scriptBlockExpressionAst.ScriptBlock, ScopeType.ScriptBlock);
            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst)
        {
            if (functionDefinitionAst.Parent is not FunctionMemberAst)
            {
                _symbolTable.EnterScope(functionDefinitionAst.Body, ScopeType.Function);
            }

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitPropertyMember(PropertyMemberAst propertyMemberAst)
        {
            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitFunctionMember(FunctionMemberAst functionMemberAst)
        {
            _symbolTable.EnterScope(functionMemberAst.Body, ScopeType.Method);
            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst)
        {
            if (_symbolTable.IsInMethodScope())
            {
                var targets = assignmentStatementAst.GetAssignmentTargets().ToArray();
                foreach (var expressionAst in targets)
                {
                    var expression = expressionAst;
                    var variableExpressionAst = expression as VariableExpressionAst;
                    while (variableExpressionAst == null && expression != null)
                    {
                        var convertExpressionAst = expression as ConvertExpressionAst;
                        if (convertExpressionAst != null)
                        {
                            expression = convertExpressionAst.Child;
                            variableExpressionAst = convertExpressionAst.Child as VariableExpressionAst;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (variableExpressionAst != null && variableExpressionAst.VariablePath.IsVariable)
                    {
                        var ast = _symbolTable.LookupVariable(variableExpressionAst.VariablePath);
                        var propertyMember = ast as PropertyMemberAst;
                        if (propertyMember != null)
                        {
                            if (propertyMember.IsStatic)
                            {
                                var typeAst = _symbolTable.GetCurrentTypeDefinitionAst();
                                Diagnostics.Assert(typeAst != null, "Method scopes can exist only inside type definitions.");

                                string typeString = string.Format(CultureInfo.InvariantCulture, "[{0}]::", typeAst.Name);
                                _parser.ReportError(variableExpressionAst.Extent,
                                    nameof(ParserStrings.MissingTypeInStaticPropertyAssignment),
                                    ParserStrings.MissingTypeInStaticPropertyAssignment,
                                    typeString,
                                    propertyMember.Name);
                            }
                            else
                            {
                                _parser.ReportError(variableExpressionAst.Extent,
                                    nameof(ParserStrings.MissingThis),
                                    ParserStrings.MissingThis,
                                    "$this.",
                                    propertyMember.Name);
                            }
                        }
                    }
                }
                // TODO: static look for alias and function.
            }

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitTypeExpression(TypeExpressionAst typeExpressionAst)
        {
            DispatchTypeName(typeExpressionAst.TypeName, genericArgumentCount: 0, isAttribute: false);
            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitTypeConstraint(TypeConstraintAst typeConstraintAst)
        {
            DispatchTypeName(typeConstraintAst.TypeName, genericArgumentCount: 0, isAttribute: false);
            return AstVisitAction.Continue;
        }

        /// <summary>
        /// Resolves using module to a collection of PSModuleInfos. Doesn't throw.
        /// PSModuleInfo objects are returned in the right order: i.e. if multiply versions of the module
        /// is presented on the system and user didn't specify version, we will return all of them, but newer one would go first.
        /// </summary>
        /// <param name="usingStatementAst">Using statement.</param>
        /// <param name="exception">If exception happens, return exception object.</param>
        /// <param name="wildcardCharactersUsed">
        /// True if in the module name uses wildcardCharacter.
        /// We don't want to resolve any wild-cards in using module.
        /// </param>
        /// <param name="isConstant">True if module hashtable contains constant value (it's our requirement).</param>
        /// <returns>Modules, if can resolve it. null if any problems happens.</returns>
        private Collection<PSModuleInfo> GetModulesFromUsingModule(UsingStatementAst usingStatementAst, out Exception exception, out bool wildcardCharactersUsed, out bool isConstant)
        {
            exception = null;
            wildcardCharactersUsed = false;
            isConstant = true;

            // fullyQualifiedName can be string or hashtable
            object fullyQualifiedName;
            if (usingStatementAst.ModuleSpecification != null)
            {
                object resultObject;
                if (!IsConstantValueVisitor.IsConstant(usingStatementAst.ModuleSpecification, out resultObject, forAttribute: false, forRequires: true))
                {
                    isConstant = false;
                    return null;
                }

                var hashtable = resultObject as System.Collections.Hashtable;
                var ms = new ModuleSpecification();
                exception = ModuleSpecification.ModuleSpecificationInitHelper(ms, hashtable);
                if (exception != null)
                {
                    return null;
                }

                if (WildcardPattern.ContainsWildcardCharacters(ms.Name))
                {
                    wildcardCharactersUsed = true;
                    return null;
                }

                fullyQualifiedName = ms;
            }
            else
            {
                string fullyQualifiedNameStr = usingStatementAst.Name.Value;

                if (WildcardPattern.ContainsWildcardCharacters(fullyQualifiedNameStr))
                {
                    wildcardCharactersUsed = true;
                    return null;
                }

                // case 1: relative path. Relative for file in the same folder should include .\ or ./
                bool isPath = fullyQualifiedNameStr.Contains('\\') || fullyQualifiedNameStr.Contains('/');
                if (isPath && !LocationGlobber.IsAbsolutePath(fullyQualifiedNameStr))
                {
                    string rootPath = Path.GetDirectoryName(_parser._fileName);
                    if (rootPath != null)
                    {
                        fullyQualifiedNameStr = Path.Combine(rootPath, fullyQualifiedNameStr);
                    }
                }

                // case 2: Module by name
                // case 3: Absolute Path
                // We don't need to do anything for these cases, FullyQualifiedName already handle it.

                fullyQualifiedName = fullyQualifiedNameStr;
            }

            var commandInfo = new CmdletInfo("Get-Module", typeof(GetModuleCommand));
            // TODO(sevoroby): we should consider an async call with cancellation here.
            UsingStatementResolvePowerShell.Commands.Clear();
            try
            {
                return UsingStatementResolvePowerShell.AddCommand(commandInfo)
                    .AddParameter("FullyQualifiedName", fullyQualifiedName)
                    .AddParameter("ListAvailable", true)
                    .Invoke<PSModuleInfo>();
            }
            catch (Exception e)
            {
                exception = e;
                return null;
            }
        }

        public override AstVisitAction VisitUsingStatement(UsingStatementAst usingStatementAst)
        {
            if (usingStatementAst.UsingStatementKind == UsingStatementKind.Module)
            {
                Exception exception;
                bool wildcardCharactersUsed;
                bool isConstant;
                var moduleInfo = GetModulesFromUsingModule(usingStatementAst, out exception, out wildcardCharactersUsed, out isConstant);
                if (!isConstant)
                {
                    _parser.ReportError(usingStatementAst.Extent,
                        nameof(ParserStrings.RequiresArgumentMustBeConstant),
                        ParserStrings.RequiresArgumentMustBeConstant);
                }
                else if (exception != null)
                {
                    // we re-using RequiresModuleInvalid string, semantic is very similar so it's fine to do that.
                    _parser.ReportError(usingStatementAst.Extent,
                        nameof(ParserStrings.RequiresModuleInvalid),
                        ParserStrings.RequiresModuleInvalid,
                        exception.Message);
                }
                else if (wildcardCharactersUsed)
                {
                    _parser.ReportError(usingStatementAst.Extent,
                        nameof(ParserStrings.WildCardModuleNameError),
                        ParserStrings.WildCardModuleNameError);
                }
                else if (moduleInfo != null && moduleInfo.Count > 0)
                {
                    // it's ok, if we get more then one module. They are already sorted in the right order
                    // we just need to use the first one

                    // We must add the same objects (in sense of object refs) to usingStatementAst typeTable and to symbolTable.
                    // Later, this same TypeDefinitionAsts would be used in DefineTypes(), by the module, where it was imported from at compile time.
                    var exportedTypes = usingStatementAst.DefineImportedModule(moduleInfo[0]);
                    foreach (var typePairs in exportedTypes)
                    {
                        _symbolTable.AddTypeFromUsingModule(typePairs.Value, moduleInfo[0]);
                    }
                }
                else
                {
                    // if there is no exception, but we didn't find the module then it's not present
                    string moduleText = usingStatementAst.Name != null ? usingStatementAst.Name.Value : usingStatementAst.ModuleSpecification.Extent.Text;
                    _parser.ReportError(usingStatementAst.Extent,
                        nameof(ParserStrings.ModuleNotFoundDuringParse),
                        ParserStrings.ModuleNotFoundDuringParse,
                        moduleText);
                }
            }

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitAttribute(AttributeAst attributeAst)
        {
            DispatchTypeName(attributeAst.TypeName, genericArgumentCount: 0, isAttribute: true);
            return AstVisitAction.Continue;
        }

        private bool DispatchTypeName(ITypeName type, int genericArgumentCount, bool isAttribute)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
            var typeName = type as TypeName;
            if (typeName != null)
            {
                return VisitTypeName(typeName, genericArgumentCount, isAttribute);
            }
            else
            {
                var arrayTypeName = type as ArrayTypeName;
                if (arrayTypeName != null)
                {
                    return VisitArrayTypeName(arrayTypeName);
                }
                else
                {
                    var genericTypeName = type as GenericTypeName;
                    if (genericTypeName != null)
                    {
                        return VisitGenericTypeName(genericTypeName);
                    }
                }
            }

            return false;
        }

        private bool VisitArrayTypeName(ArrayTypeName arrayTypeName)
        {
            bool resolved = DispatchTypeName(arrayTypeName.ElementType, genericArgumentCount: 0, isAttribute: false);
            if (resolved)
            {
                var resolvedType = arrayTypeName.GetReflectionType();
                TypeCache.Add(arrayTypeName, _typeResolutionState, resolvedType);
            }

            return resolved;
        }

        private bool VisitTypeName(TypeName typeName, int genericArgumentCount, bool isAttribute)
        {
            var classDefn = _symbolTable.LookupType(typeName);

            if (classDefn != null && classDefn.IsAmbiguous())
            {
                _parser.ReportError(typeName.Extent,
                    nameof(ParserStrings.AmbiguousTypeReference),
                    ParserStrings.AmbiguousTypeReference,
                    typeName.Name,
                    GetModuleQualifiedName(classDefn.ExternalNamespaces[0], typeName.Name),
                    GetModuleQualifiedName(classDefn.ExternalNamespaces[1], typeName.Name));
            }
            else if (classDefn != null && genericArgumentCount == 0)
            {
                typeName.SetTypeDefinition(classDefn.Type);
            }
            else
            {
                Exception e;
                TypeResolutionState trs = genericArgumentCount > 0 || isAttribute
                    ? new TypeResolutionState(_typeResolutionState, genericArgumentCount, isAttribute)
                    : _typeResolutionState;

                var type = TypeResolver.ResolveTypeNameWithContext(typeName, out e, null, trs);
                if (type == null)
                {
                    if (_symbolTable.GetCurrentTypeDefinitionAst() != null)
                    {
                        // [ordered] is an attribute, but it's looks like a type constraint.
                        if (!typeName.FullName.Equals(LanguagePrimitives.OrderedAttribute, StringComparison.OrdinalIgnoreCase))
                        {
                            string errorId;
                            string errorMsg;
                            if (isAttribute)
                            {
                                errorId = nameof(ParserStrings.CustomAttributeTypeNotFound);
                                errorMsg = ParserStrings.CustomAttributeTypeNotFound;
                            }
                            else
                            {
                                errorId = nameof(ParserStrings.TypeNotFound);
                                errorMsg = ParserStrings.TypeNotFound;
                            }

                            _parser.ReportError(typeName.Extent, errorId, errorMsg, typeName.Name);
                        }
                    }
                }
                else
                {
                    ((ISupportsTypeCaching)typeName).CachedType = type;
                    return true;
                }
            }

            return false;
        }

        private bool VisitGenericTypeName(GenericTypeName genericTypeName)
        {
            var foundType = TypeCache.Lookup(genericTypeName, _typeResolutionState);
            if (foundType != null)
            {
                ((ISupportsTypeCaching)genericTypeName).CachedType = foundType;
                return true;
            }

            bool resolved = true;
            resolved &= DispatchTypeName(genericTypeName.TypeName, genericTypeName.GenericArguments.Count, isAttribute: false);
            foreach (var typeArg in genericTypeName.GenericArguments)
            {
                resolved &= DispatchTypeName(typeArg, genericArgumentCount: 0, isAttribute: false);
            }

            if (resolved)
            {
                var resolvedType = genericTypeName.GetReflectionType();
                TypeCache.Add(genericTypeName, _typeResolutionState, resolvedType);
            }

            return resolved;
        }

        public void PostVisit(Ast ast)
        {
            ast.Accept(_symbolResolvePostActionVisitor);
        }

        internal static string GetModuleQualifiedName(string namespaceName, string typeName)
        {
            const char NAMESPACE_SEPARATOR = '.';
            return namespaceName + NAMESPACE_SEPARATOR + typeName;
        }
    }

    internal class SymbolResolvePostActionVisitor : DefaultCustomAstVisitor2
    {
        internal SymbolResolver _symbolResolver;

        public override object VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst)
        {
            if (functionDefinitionAst.Parent is not FunctionMemberAst)
            {
                _symbolResolver._symbolTable.LeaveScope();
            }

            return null;
        }

        public override object VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst)
        {
            _symbolResolver._symbolTable.LeaveScope();
            return null;
        }

        public override object VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst)
        {
            _symbolResolver._symbolTable.LeaveScope();
            return null;
        }

        public override object VisitFunctionMember(FunctionMemberAst functionMemberAst)
        {
            _symbolResolver._symbolTable.LeaveScope();
            return null;
        }
    }
}
