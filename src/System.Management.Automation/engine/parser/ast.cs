// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

//
// This file contains all of the publicly visible parts of the PowerShell abstract syntax tree.
// Any private/internal methods or properties are found in the file AstCompile.cs.
//

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.PowerShell;
using Microsoft.PowerShell.Commands;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

namespace System.Management.Automation.Language
{
    using KeyValuePair = Tuple<ExpressionAst, StatementAst>;
    using IfClause = Tuple<PipelineBaseAst, StatementBlockAst>;
    using SwitchClause = Tuple<ExpressionAst, StatementBlockAst>;
    using System.Runtime.CompilerServices;
    using System.Reflection.Emit;

#nullable enable

    internal interface ISupportsAssignment
    {
        IAssignableValue GetAssignableValue();
    }

    internal interface IAssignableValue
    {
        /// <summary>
        /// GetValue is only called for pre/post increment/decrement or for read/modify/write assignment operators (+=, -=, etc.)
        /// It returns the expressions that holds the value of the ast.  It may append the exprs or temps lists if the return
        /// value relies on temps and other expressions.
        /// </summary>
        Expression? GetValue(Compiler compiler, List<Expression> exprs, List<ParameterExpression> temps);

        /// <summary>
        /// SetValue is called to set the result of an assignment (=) or to write back the result of
        /// a pre/post increment/decrement.  It needs to use potentially cached temps if GetValue was called first.
        /// </summary>
        Expression SetValue(Compiler compiler, Expression rhs);
    }
#nullable restore

    internal interface IParameterMetadataProvider
    {
        bool HasAnyScriptBlockAttributes();

        RuntimeDefinedParameterDictionary GetParameterMetadata(bool automaticPositions, ref bool usesCmdletBinding);

        IEnumerable<Attribute> GetScriptBlockAttributes();

        IEnumerable<ExperimentalAttribute> GetExperimentalAttributes();

        bool UsesCmdletBinding();

        ReadOnlyCollection<ParameterAst> Parameters { get; }

        ScriptBlockAst Body { get; }

        #region Remoting/Invoke Command

        PowerShell GetPowerShell(ExecutionContext context, Dictionary<string, object> variables, bool isTrustedInput,
            bool filterNonUsingVariables, bool? createLocalScope, params object[] args);

        string GetWithInputHandlingForInvokeCommand();

        /// <summary>
        /// Return value is Tuple[paramText, scriptBlockText]
        /// </summary>
        Tuple<string, string> GetWithInputHandlingForInvokeCommandWithUsingExpression(Tuple<List<VariableExpressionAst>, string> usingVariablesTuple);

        #endregion Remoting/Invoke Command
    }

    /// <summary>
    /// The abstract base class for all PowerShell abstract syntax tree nodes.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
    public abstract class Ast
    {
        /// <summary>
        /// Initialize the common fields of an ast.
        /// </summary>
        /// <param name="extent">The extent of the expression.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        protected Ast(IScriptExtent extent)
        {
            if (extent == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(extent));
            }

            this.Extent = extent;
        }

        /// <summary>
        /// The extent in the source this ast represents.
        /// </summary>
        public IScriptExtent Extent { get; }

        /// <summary>
        /// The parent tree for this node.
        /// </summary>
        public Ast Parent { get; private set; }

        /// <summary>
        /// Visit the Ast using a visitor that can choose how the tree traversal is performed.  This visit method is
        /// for advanced uses of the visitor pattern where an <see cref="AstVisitor"/> is insufficient.
        /// </summary>
        /// <param name="astVisitor">The visitor.</param>
        /// <returns>Returns the value returned by the visitor.</returns>
        public object Visit(ICustomAstVisitor astVisitor)
        {
            if (astVisitor == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(astVisitor));
            }

            return this.Accept(astVisitor);
        }

        /// <summary>
        /// Visit each node in the Ast, calling the methods in <paramref name="astVisitor"/> for each node in the ast.
        /// </summary>
        /// <param name="astVisitor">The visitor.</param>
        public void Visit(AstVisitor astVisitor)
        {
            if (astVisitor == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(astVisitor));
            }

            this.InternalVisit(astVisitor);
        }

        /// <summary>
        /// Traverse the entire Ast, returning all nodes in the tree for which <paramref name="predicate"/> returns true.
        /// </summary>
        /// <param name="predicate">The predicate function.</param>
        /// <param name="searchNestedScriptBlocks">Search nested functions and script block expressions.</param>
        /// <returns>A possibly empty collection of matching Ast nodes.</returns>
        public IEnumerable<Ast> FindAll(Func<Ast, bool> predicate, bool searchNestedScriptBlocks)
        {
            if (predicate == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(predicate));
            }

            return AstSearcher.FindAll(this, predicate, searchNestedScriptBlocks);
        }

        /// <summary>
        /// Traverse the entire Ast, returning the first node in the tree for which <paramref name="predicate"/> returns true.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        /// <param name="searchNestedScriptBlocks">Search nested functions and script block expressions.</param>
        /// <returns>The first matching node, or null if there is no match.</returns>
        public Ast Find(Func<Ast, bool> predicate, bool searchNestedScriptBlocks)
        {
            if (predicate == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(predicate));
            }

            return AstSearcher.FindFirst(this, predicate, searchNestedScriptBlocks);
        }

        /// <summary>
        /// Formats the ast and returns a string.
        /// </summary>
        public override string ToString()
        {
            return Extent.Text;
        }

        /// <summary>
        /// Duplicates the AST, allowing it to be composed into other ASTs.
        /// </summary>
        /// <returns>A copy of the AST, with the link to the previous parent removed.</returns>
        public abstract Ast Copy();

        /// <summary>
        /// Constructs the resultant object from the AST and returns it if it is safe.
        /// </summary>
        /// <returns>The object represented by the AST as a safe object.</returns>
        /// <exception cref="InvalidOperationException">
        /// If <paramref name="extent"/> is deemed unsafe
        /// </exception>
        public object SafeGetValue()
        {
            return SafeGetValue(skipHashtableSizeCheck: false);
        }

        /// <summary>
        /// Constructs the resultant object from the AST and returns it if it is safe.
        /// </summary>
        /// <param name="skipHashtableSizeCheck">Set to skip hashtable limit validation.</param>
        /// <returns>The object represented by the AST as a safe object.</returns>
        /// <exception cref="InvalidOperationException">
        /// If <paramref name="extent"/> is deemed unsafe.
        /// </exception>
        public object SafeGetValue(bool skipHashtableSizeCheck)
        {
            try
            {
                ExecutionContext context = null;
                if (System.Management.Automation.Runspaces.Runspace.DefaultRunspace != null)
                {
                    context = System.Management.Automation.Runspaces.Runspace.DefaultRunspace.ExecutionContext;
                }

                return GetSafeValueVisitor.GetSafeValue(this, context, skipHashtableSizeCheck ? GetSafeValueVisitor.SafeValueContext.SkipHashtableSizeCheck : GetSafeValueVisitor.SafeValueContext.Default);
            }
            catch
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, AutomationExceptions.CantConvertWithDynamicExpression, this.Extent.Text));
            }
        }

        /// <summary>
        /// Copy a collection of AST elements.
        /// </summary>
        /// <typeparam name="T">The actual AST type</typeparam>
        /// <param name="elements">Collection of ASTs.</param>
        /// <returns></returns>
        internal static T[] CopyElements<T>(ReadOnlyCollection<T> elements) where T : Ast
        {
            if (elements == null || elements.Count == 0) { return null; }

            var result = new T[elements.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (T)elements[i].Copy();
            }

            return result;
        }

        /// <summary>
        /// Copy a single AST element.
        /// </summary>
        /// <typeparam name="T">The actual AST type</typeparam>
        /// <param name="element">An AST instance.</param>
        /// <returns></returns>
        internal static T CopyElement<T>(T element) where T : Ast
        {
            if (element == null) { return null; }

            return (T)element.Copy();
        }

        // Should be protected AND internal, but C# doesn't support that.
        internal void SetParents<T>(ReadOnlyCollection<T> children)
            where T : Ast
        {
            for (int index = 0; index < children.Count; index++)
            {
                var child = children[index];
                SetParent(child);
            }
        }

        // Should be protected AND internal, but C# doesn't support that.
        internal void SetParents<T1, T2>(ReadOnlyCollection<Tuple<T1, T2>> children)
            where T1 : Ast
            where T2 : Ast
        {
            for (int index = 0; index < children.Count; index++)
            {
                var child = children[index];
                SetParent(child.Item1);
                SetParent(child.Item2);
            }
        }

        // Should be protected AND internal, but C# doesn't support that.
        internal void SetParent(Ast child)
        {
            if (child.Parent != null)
            {
                throw new InvalidOperationException(ParserStrings.AstIsReused);
            }

            Diagnostics.Assert(child.Parent == null, "Parent can only be set once");
            child.Parent = this;
        }

        internal void ClearParent()
        {
            this.Parent = null;
        }

        internal abstract object Accept(ICustomAstVisitor visitor);

        internal abstract AstVisitAction InternalVisit(AstVisitor visitor);

        internal static readonly PSTypeName[] EmptyPSTypeNameArray = Array.Empty<PSTypeName>();

        internal bool IsInWorkflow()
        {
            // Scan up the AST's parents, looking for a script block that is either
            // a workflow, or has a job definition attribute.
            // Stop scanning when we encounter a FunctionDefinitionAst
            Ast current = this;
            bool stopScanning = false;

            while (current != null && !stopScanning)
            {
                ScriptBlockAst scriptBlock = current as ScriptBlockAst;
                if (scriptBlock != null)
                {
                    // See if this uses the workflow keyword
                    FunctionDefinitionAst functionDefinition = scriptBlock.Parent as FunctionDefinitionAst;
                    if ((functionDefinition != null))
                    {
                        stopScanning = true;
                        if (functionDefinition.IsWorkflow) { return true; }
                    }
                }

                CommandAst commandAst = current as CommandAst;
                if (commandAst != null &&
                    string.Equals(TokenKind.InlineScript.Text(), commandAst.GetCommandName(), StringComparison.OrdinalIgnoreCase) &&
                    this != commandAst)
                {
                    return false;
                }

                current = current.Parent;
            }

            return false;
        }

        internal bool HasSuspiciousContent { get; set; }

        #region Search Ancestor Ast

        internal static ConfigurationDefinitionAst GetAncestorConfigurationDefinitionAstAndDynamicKeywordStatementAst(
            Ast ast,
            out DynamicKeywordStatementAst keywordAst)
        {
            ConfigurationDefinitionAst configAst = null;
            keywordAst = GetAncestorAst<DynamicKeywordStatementAst>(ast);
            configAst = (keywordAst != null) ? GetAncestorAst<ConfigurationDefinitionAst>(keywordAst) : GetAncestorAst<ConfigurationDefinitionAst>(ast);
            return configAst;
        }

        internal static HashtableAst GetAncestorHashtableAst(Ast ast, out Ast lastChildOfHashtable)
        {
            HashtableAst hashtableAst = null;
            lastChildOfHashtable = null;
            while (ast != null)
            {
                hashtableAst = ast as HashtableAst;
                if (hashtableAst != null)
                    break;
                lastChildOfHashtable = ast;
                ast = ast.Parent;
            }

            return hashtableAst;
        }

        internal static TypeDefinitionAst GetAncestorTypeDefinitionAst(Ast ast)
        {
            TypeDefinitionAst typeDefinitionAst = null;

            while (ast != null)
            {
                typeDefinitionAst = ast as TypeDefinitionAst;
                if (typeDefinitionAst != null)
                    break;

                // Nested function isn't really a member of the type so stop looking
                // Anonymous script blocks are though
                var functionDefinitionAst = ast as FunctionDefinitionAst;
                if (functionDefinitionAst != null && functionDefinitionAst.Parent is not FunctionMemberAst)
                    break;
                ast = ast.Parent;
            }

            return typeDefinitionAst;
        }

        /// <summary>
        /// Get ancestor Ast of the given type of the given ast.
        /// </summary>
        /// <param name="ast"></param>
        /// <returns></returns>
        internal static T GetAncestorAst<T>(Ast ast) where T : Ast
        {
            T targetAst = null;
            var parent = ast;
            while (parent != null)
            {
                targetAst = parent as T;
                if (targetAst != null)
                    break;
                parent = parent.Parent;
            }

            return targetAst;
        }

        #endregion
    }

    // A dummy class to hold an extent for open/close curlies so we can step in the debugger.
    // This Ast is never produced by the parser, only from the compiler.
    internal class SequencePointAst : Ast
    {
        public SequencePointAst(IScriptExtent extent)
            : base(extent)
        {
        }

        /// <summary>
        /// Copy the SequencePointAst instance.
        /// </summary>
        public override Ast Copy()
        {
            Diagnostics.Assert(false, "code should be unreachable");
            return null;
        }

        internal override object Accept(ICustomAstVisitor visitor)
        {
            Diagnostics.Assert(false, "code should be unreachable");
            return null;
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            Diagnostics.Assert(false, "code should be unreachable");
            return visitor.CheckForPostAction(this, AstVisitAction.Continue);
        }
    }

    /// <summary>
    /// A placeholder statement used when there are syntactic errors in the source script.
    /// </summary>
    public class ErrorStatementAst : PipelineBaseAst
    {
        internal ErrorStatementAst(IScriptExtent extent, IEnumerable<Ast> nestedAsts = null)
            : base(extent)
        {
            if (nestedAsts != null && nestedAsts.Any())
            {
                NestedAst = new ReadOnlyCollection<Ast>(nestedAsts.ToArray());
                SetParents(NestedAst);
            }
        }

        internal ErrorStatementAst(IScriptExtent extent, Token kind, IEnumerable<Ast> nestedAsts = null)
            : base(extent)
        {
            if (kind == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(kind));
            }

            Kind = kind;
            if (nestedAsts != null && nestedAsts.Any())
            {
                NestedAst = new ReadOnlyCollection<Ast>(nestedAsts.ToArray());
                SetParents(NestedAst);
            }
        }

        internal ErrorStatementAst(IScriptExtent extent, Token kind, IEnumerable<KeyValuePair<string, Tuple<Token, Ast>>> flags, IEnumerable<Ast> conditions, IEnumerable<Ast> bodies)
            : base(extent)
        {
            if (kind == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(kind));
            }

            Kind = kind;
            if (flags != null && flags.Any())
            {
                Flags = new Dictionary<string, Tuple<Token, Ast>>(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, Tuple<Token, Ast>> entry in flags)
                {
                    if (Flags.ContainsKey(entry.Key))
                        continue;

                    Flags.Add(entry.Key, entry.Value);
                    if (entry.Value.Item2 != null)
                    {
                        SetParent(entry.Value.Item2);
                    }
                }
            }

            if (conditions != null && conditions.Any())
            {
                Conditions = new ReadOnlyCollection<Ast>(conditions.ToArray());
                SetParents(Conditions);
            }

            if (bodies != null && bodies.Any())
            {
                Bodies = new ReadOnlyCollection<Ast>(bodies.ToArray());
                SetParents(Bodies);
            }
        }

        /// <summary>
        /// Indicate the kind of the ErrorStatement. e.g. Kind == Switch means that this error statment is generated
        /// when parsing a switch statement.
        /// </summary>
        public Token Kind { get; }

        /// <summary>
        /// The flags specified and their value. The value is null if it's not specified.
        /// e.g. switch -regex -file c:\demo.txt  --->   regex -- null
        ///                                              file  -- { c:\demo.txt }
        /// </summary>
        /// TODO, Changing this to an IDictionary because ReadOnlyDictionary is available only in .NET 4.5
        /// This is a temporary workaround and will be fixed later. Tracked by Win8: 354135
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public Dictionary<string, Tuple<Token, Ast>> Flags { get; }

        /// <summary>
        /// The conditions specified.
        /// </summary>
        public ReadOnlyCollection<Ast> Conditions { get; }

        /// <summary>
        /// The bodies specified.
        /// </summary>
        public ReadOnlyCollection<Ast> Bodies { get; }

        /// <summary>
        /// Sometimes a valid ast is parsed successfully within the extent that this error statement represents.  Those
        /// asts are contained in this collection.  This collection may contain other error asts.  This collection may
        /// be null when no asts were successfully constructed within the extent of this error ast.
        /// </summary>
        public ReadOnlyCollection<Ast> NestedAst { get; }

        /// <summary>
        /// Copy the ErrorStatementAst instance.
        /// </summary>
        public override Ast Copy()
        {
            if (this.Kind == null)
            {
                var newNestedAst = CopyElements(this.NestedAst);
                return new ErrorStatementAst(this.Extent, newNestedAst);
            }
            else if (Flags != null || Conditions != null || Bodies != null)
            {
                var newConditions = CopyElements(this.Conditions);
                var newBodies = CopyElements(this.Bodies);
                Dictionary<string, Tuple<Token, Ast>> newFlags = null;

                if (this.Flags != null)
                {
                    newFlags = new Dictionary<string, Tuple<Token, Ast>>(StringComparer.OrdinalIgnoreCase);
                    foreach (KeyValuePair<string, Tuple<Token, Ast>> entry in this.Flags)
                    {
                        var newAst = CopyElement(entry.Value.Item2);
                        newFlags.Add(entry.Key, new Tuple<Token, Ast>(entry.Value.Item1, newAst));
                    }
                }

                return new ErrorStatementAst(this.Extent, this.Kind, newFlags, newConditions, newBodies);
            }
            else
            {
                var newNestedAst = CopyElements(this.NestedAst);
                return new ErrorStatementAst(this.Extent, this.Kind, newNestedAst);
            }
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitErrorStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitErrorStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue && NestedAst != null)
            {
                for (int index = 0; index < NestedAst.Count; index++)
                {
                    var ast = NestedAst[index];
                    action = ast.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            if (action == AstVisitAction.Continue && Flags != null)
            {
                foreach (var tuple in Flags.Values)
                {
                    if (tuple.Item2 == null)
                        continue;

                    action = tuple.Item2.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            if (action == AstVisitAction.Continue && Conditions != null)
            {
                for (int index = 0; index < Conditions.Count; index++)
                {
                    var ast = Conditions[index];
                    action = ast.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            if (action == AstVisitAction.Continue && Bodies != null)
            {
                for (int index = 0; index < Bodies.Count; index++)
                {
                    var ast = Bodies[index];
                    action = ast.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// A placeholder expression used when there are syntactic errors in the source script.
    /// </summary>
    public class ErrorExpressionAst : ExpressionAst
    {
        internal ErrorExpressionAst(IScriptExtent extent, IEnumerable<Ast> nestedAsts = null)
            : base(extent)
        {
            if (nestedAsts != null && nestedAsts.Any())
            {
                NestedAst = new ReadOnlyCollection<Ast>(nestedAsts.ToArray());
                SetParents(NestedAst);
            }
        }

        /// <summary>
        /// Sometimes a valid ast is parsed successfully within the extent that this error expression represents.  Those
        /// asts are contained in this collection.  This collection may contain other error asts.  This collection may
        /// be null when no asts were successfully constructed within the extent of this error ast.
        /// </summary>
        public ReadOnlyCollection<Ast> NestedAst { get; }

        /// <summary>
        /// Copy the ErrorExpressionAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newNestedAst = CopyElements(this.NestedAst);
            return new ErrorExpressionAst(this.Extent, newNestedAst);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitErrorExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitErrorExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue && NestedAst != null)
            {
                for (int index = 0; index < NestedAst.Count; index++)
                {
                    var ast = NestedAst[index];
                    action = ast.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    #region Script Blocks

    /// <summary>
    /// </summary>
    public class ScriptRequirements
    {
        internal static readonly ReadOnlyCollection<PSSnapInSpecification> EmptySnapinCollection =
            Utils.EmptyReadOnlyCollection<PSSnapInSpecification>();

        internal static readonly ReadOnlyCollection<string> EmptyAssemblyCollection =
            Utils.EmptyReadOnlyCollection<string>();

        internal static readonly ReadOnlyCollection<ModuleSpecification> EmptyModuleCollection =
            Utils.EmptyReadOnlyCollection<ModuleSpecification>();

        internal static readonly ReadOnlyCollection<string> EmptyEditionCollection =
            Utils.EmptyReadOnlyCollection<string>();

        /// <summary>
        /// The application id this script requires, specified like:
        ///     <code>#requires -Shellid Shell</code>
        /// If no application id has been specified, this property is null.
        /// </summary>
        public string RequiredApplicationId { get; internal set; }

        /// <summary>
        /// The PowerShell version this script requires, specified like:
        ///     <code>#requires -Version 3</code>
        /// If no version has been specified, this property is null.
        /// </summary>
        public Version RequiredPSVersion { get; internal set; }

        /// <summary>
        /// The PowerShell Edition this script requires, specified like:
        ///     <code>#requires -PSEdition Desktop</code>
        /// If no PSEdition has been specified, this property is an empty collection.
        /// </summary>
        public ReadOnlyCollection<string> RequiredPSEditions { get; internal set; }

        /// <summary>
        /// The modules this script requires, specified like:
        ///     <code>#requires -Module NetAdapter</code>
        ///     <code>#requires -Module @{Name="NetAdapter"; Version="1.0.0.0"}</code>
        /// If no modules are required, this property is an empty collection.
        /// </summary>
        public ReadOnlyCollection<ModuleSpecification> RequiredModules { get; internal set; }

        /// <summary>
        /// The snapins this script requires, specified like:
        ///     <code>#requires -PSSnapin Snapin</code>
        ///     <code>#requires -PSSnapin Snapin -Version 2</code>
        /// If no snapins are required, this property is an empty collection.
        /// </summary>
        public ReadOnlyCollection<PSSnapInSpecification> RequiresPSSnapIns { get; internal set; }

        /// <summary>
        /// The assemblies this script requires, specified like:
        ///     <code>#requires -Assembly path\to\foo.dll</code>
        ///     <code>#requires -Assembly "System.Management.Automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"</code>
        /// If no assemblies are required, this property is an empty collection.
        /// </summary>
        public ReadOnlyCollection<string> RequiredAssemblies { get; internal set; }

        /// <summary>
        /// Specifies if this script requires elevated privileges, specified like:
        ///     <code>#requires -RunAsAdministrator</code>
        /// If nothing is specified, this property is false.
        /// </summary>
        public bool IsElevationRequired { get; internal set; }
    }

    /// <summary>
    /// A ScriptBlockAst is the root ast node for a complete script.
    /// </summary>
    public class ScriptBlockAst : Ast, IParameterMetadataProvider
    {
        private static readonly ReadOnlyCollection<AttributeAst> s_emptyAttributeList =
            Utils.EmptyReadOnlyCollection<AttributeAst>();

        private static readonly ReadOnlyCollection<UsingStatementAst> s_emptyUsingStatementList =
            Utils.EmptyReadOnlyCollection<UsingStatementAst>();

        internal bool HadErrors { get; set; }

        internal bool IsConfiguration { get; private set; }

        internal bool PostParseChecksPerformed { get; set; }

        /// <summary>
        /// Construct a ScriptBlockAst that uses explicitly named begin/process/end blocks.
        /// </summary>
        /// <param name="extent">The extent of the script block.</param>
        /// <param name="usingStatements">The list of using statments, may be null.</param>
        /// <param name="attributes">The set of attributes for the script block.</param>
        /// <param name="paramBlock">The ast for the param block, may be null.</param>
        /// <param name="beginBlock">The ast for the begin block, may be null.</param>
        /// <param name="processBlock">The ast for the process block, may be null.</param>
        /// <param name="endBlock">The ast for the end block, may be null.</param>
        /// <param name="dynamicParamBlock">The ast for the dynamicparam block, may be null.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public ScriptBlockAst(IScriptExtent extent,
                              IEnumerable<UsingStatementAst> usingStatements,
                              IEnumerable<AttributeAst> attributes,
                              ParamBlockAst paramBlock,
                              NamedBlockAst beginBlock,
                              NamedBlockAst processBlock,
                              NamedBlockAst endBlock,
                              NamedBlockAst dynamicParamBlock)
            : base(extent)
        {
            SetUsingStatements(usingStatements);

            if (attributes != null)
            {
                this.Attributes = new ReadOnlyCollection<AttributeAst>(attributes.ToArray());
                SetParents(Attributes);
            }
            else
            {
                this.Attributes = s_emptyAttributeList;
            }

            if (paramBlock != null)
            {
                this.ParamBlock = paramBlock;
                SetParent(paramBlock);
            }

            if (beginBlock != null)
            {
                this.BeginBlock = beginBlock;
                SetParent(beginBlock);
            }

            if (processBlock != null)
            {
                this.ProcessBlock = processBlock;
                SetParent(processBlock);
            }

            if (endBlock != null)
            {
                this.EndBlock = endBlock;
                SetParent(endBlock);
            }

            if (dynamicParamBlock != null)
            {
                this.DynamicParamBlock = dynamicParamBlock;
                SetParent(dynamicParamBlock);
            }
        }

        /// <summary>
        /// Construct a ScriptBlockAst that uses explicitly named begin/process/end blocks.
        /// </summary>
        /// <param name="extent">The extent of the script block.</param>
        /// <param name="usingStatements">The list of using statments, may be null.</param>
        /// <param name="paramBlock">The ast for the param block, may be null.</param>
        /// <param name="beginBlock">The ast for the begin block, may be null.</param>
        /// <param name="processBlock">The ast for the process block, may be null.</param>
        /// <param name="endBlock">The ast for the end block, may be null.</param>
        /// <param name="dynamicParamBlock">The ast for the dynamicparam block, may be null.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public ScriptBlockAst(IScriptExtent extent,
                              IEnumerable<UsingStatementAst> usingStatements,
                              ParamBlockAst paramBlock,
                              NamedBlockAst beginBlock,
                              NamedBlockAst processBlock,
                              NamedBlockAst endBlock,
                              NamedBlockAst dynamicParamBlock)
            : this(extent, usingStatements, null, paramBlock, beginBlock, processBlock, endBlock, dynamicParamBlock)
        {
        }

        /// <summary>
        /// Construct a ScriptBlockAst that uses explicitly named begin/process/end blocks.
        /// </summary>
        /// <param name="extent">The extent of the script block.</param>
        /// <param name="paramBlock">The ast for the param block, may be null.</param>
        /// <param name="beginBlock">The ast for the begin block, may be null.</param>
        /// <param name="processBlock">The ast for the process block, may be null.</param>
        /// <param name="endBlock">The ast for the end block, may be null.</param>
        /// <param name="dynamicParamBlock">The ast for the dynamicparam block, may be null.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public ScriptBlockAst(IScriptExtent extent,
                              ParamBlockAst paramBlock,
                              NamedBlockAst beginBlock,
                              NamedBlockAst processBlock,
                              NamedBlockAst endBlock,
                              NamedBlockAst dynamicParamBlock)
            : this(extent, null, paramBlock, beginBlock, processBlock, endBlock, dynamicParamBlock)
        {
        }

        /// <summary>
        /// Construct a ScriptBlockAst that does not use explicitly named blocks.
        /// </summary>
        /// <param name="extent">The extent of the script block.</param>
        /// <param name="usingStatements">The list of using statments, may be null.</param>
        /// <param name="paramBlock">The ast for the param block, may be null.</param>
        /// <param name="statements">
        /// The statements that go in the end block if <paramref name="isFilter"/> is false, or the
        /// process block if <paramref name="isFilter"/> is true.
        /// </param>
        /// <param name="isFilter">True if the script block is a filter, false if it is a function or workflow.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="statements"/> is null.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public ScriptBlockAst(IScriptExtent extent, List<UsingStatementAst> usingStatements, ParamBlockAst paramBlock, StatementBlockAst statements, bool isFilter)
            : this(extent, usingStatements, null, paramBlock, statements, isFilter, false)
        {
        }

        /// <summary>
        /// Construct a ScriptBlockAst that does not use explicitly named blocks.
        /// </summary>
        /// <param name="extent">The extent of the script block.</param>
        /// <param name="paramBlock">The ast for the param block, may be null.</param>
        /// <param name="statements">
        /// The statements that go in the end block if <paramref name="isFilter"/> is false, or the
        /// process block if <paramref name="isFilter"/> is true.
        /// </param>
        /// <param name="isFilter">True if the script block is a filter, false if it is a function or workflow.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="statements"/> is null.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public ScriptBlockAst(IScriptExtent extent, ParamBlockAst paramBlock, StatementBlockAst statements, bool isFilter)
            : this(extent, null, null, paramBlock, statements, isFilter, false)
        {
        }

        /// <summary>
        /// Construct a ScriptBlockAst that does not use explicitly named blocks.
        /// </summary>
        /// <param name="extent">The extent of the script block.</param>
        /// <param name="paramBlock">The ast for the param block, may be null.</param>
        /// <param name="statements">
        /// The statements that go in the end block if <paramref name="isFilter"/> is false, or the
        /// process block if <paramref name="isFilter"/> is true.
        /// </param>
        /// <param name="isFilter">True if the script block is a filter, false if it is a function or workflow.</param>
        /// <param name="isConfiguration">True if the script block is a configuration.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="statements"/> is null.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public ScriptBlockAst(IScriptExtent extent, ParamBlockAst paramBlock, StatementBlockAst statements, bool isFilter, bool isConfiguration)
            : this(extent, null, null, paramBlock, statements, isFilter, isConfiguration)
        {
        }

        /// <summary>
        /// Construct a ScriptBlockAst that does not use explicitly named blocks.
        /// </summary>
        /// <param name="extent">The extent of the script block.</param>
        /// <param name="usingStatements">The list of using statments, may be null.</param>
        /// <param name="paramBlock">The ast for the param block, may be null.</param>
        /// <param name="statements">
        /// The statements that go in the end block if <paramref name="isFilter"/> is false, or the
        /// process block if <paramref name="isFilter"/> is true.
        /// </param>
        /// <param name="isFilter">True if the script block is a filter, false if it is a function or workflow.</param>
        /// <param name="isConfiguration">True if the script block is a configuration.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="statements"/> is null.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public ScriptBlockAst(IScriptExtent extent, IEnumerable<UsingStatementAst> usingStatements, ParamBlockAst paramBlock, StatementBlockAst statements, bool isFilter, bool isConfiguration)
            : this(extent, usingStatements, null, paramBlock, statements, isFilter, isConfiguration)
        {
        }

        /// <summary>
        /// Construct a ScriptBlockAst that does not use explicitly named blocks.
        /// </summary>
        /// <param name="extent">The extent of the script block.</param>
        /// <param name="attributes">The attributes for the script block.</param>
        /// <param name="paramBlock">The ast for the param block, may be null.</param>
        /// <param name="statements">
        /// The statements that go in the end block if <paramref name="isFilter"/> is false, or the
        /// process block if <paramref name="isFilter"/> is true.
        /// </param>
        /// <param name="isFilter">True if the script block is a filter, false if it is a function or workflow.</param>
        /// <param name="isConfiguration">True if the script block is a configuration.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="statements"/> is null.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public ScriptBlockAst(IScriptExtent extent, IEnumerable<AttributeAst> attributes, ParamBlockAst paramBlock, StatementBlockAst statements, bool isFilter, bool isConfiguration)
            : this(extent, null, attributes, paramBlock, statements, isFilter, isConfiguration)
        {
        }

        /// <summary>
        /// Construct a ScriptBlockAst that does not use explicitly named blocks.
        /// </summary>
        /// <param name="extent">The extent of the script block.</param>
        /// <param name="usingStatements">The list of using statments, may be null.</param>
        /// <param name="attributes">The attributes for the script block.</param>
        /// <param name="paramBlock">The ast for the param block, may be null.</param>
        /// <param name="statements">
        /// The statements that go in the end block if <paramref name="isFilter"/> is false, or the
        /// process block if <paramref name="isFilter"/> is true.
        /// </param>
        /// <param name="isFilter">True if the script block is a filter, false if it is a function or workflow.</param>
        /// <param name="isConfiguration">True if the script block is a configuration.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="statements"/> is null.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public ScriptBlockAst(IScriptExtent extent, IEnumerable<UsingStatementAst> usingStatements, IEnumerable<AttributeAst> attributes, ParamBlockAst paramBlock, StatementBlockAst statements, bool isFilter, bool isConfiguration)
            : base(extent)
        {
            SetUsingStatements(usingStatements);

            if (attributes != null)
            {
                this.Attributes = new ReadOnlyCollection<AttributeAst>(attributes.ToArray());
                SetParents(Attributes);
            }
            else
            {
                this.Attributes = s_emptyAttributeList;
            }

            if (statements == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(statements));
            }

            if (paramBlock != null)
            {
                this.ParamBlock = paramBlock;
                SetParent(paramBlock);
            }

            if (isFilter)
            {
                this.ProcessBlock = new NamedBlockAst(statements.Extent, TokenKind.Process, statements, true);
                SetParent(ProcessBlock);
            }
            else
            {
                this.EndBlock = new NamedBlockAst(statements.Extent, TokenKind.End, statements, true);
                this.IsConfiguration = isConfiguration;
                SetParent(EndBlock);
            }
        }

        private void SetUsingStatements(IEnumerable<UsingStatementAst> usingStatements)
        {
            if (usingStatements != null)
            {
                this.UsingStatements = new ReadOnlyCollection<UsingStatementAst>(usingStatements.ToArray());
                SetParents(UsingStatements);
            }
            else
            {
                this.UsingStatements = s_emptyUsingStatementList;
            }
        }

        /// <summary>
        /// The asts for attributes (such as [DscLocalConfigurationManager()]) used before the scriptblock.
        /// This property is never null.
        /// </summary>
        public ReadOnlyCollection<AttributeAst> Attributes { get; }

        /// <summary>
        /// The asts for any using statements.  This property is never null.
        /// Elements of the collection are instances of either <see cref="UsingStatementAst"/>
        /// or (only in error cases) <see cref="ErrorStatementAst"/>.
        /// </summary>
        public ReadOnlyCollection<UsingStatementAst> UsingStatements { get; private set; }

        /// <summary>
        /// The ast representing the parameters for a script block, or null if no param block was specified.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Param")]
        public ParamBlockAst ParamBlock { get; }

        /// <summary>
        /// The ast representing the begin block for a script block, or null if no begin block was specified.
        /// </summary>
        public NamedBlockAst BeginBlock { get; }

        /// <summary>
        /// The ast representing the process block for a script block, or null if no process block was specified.
        /// </summary>
        public NamedBlockAst ProcessBlock { get; }

        /// <summary>
        /// The ast representing the end block for a script block, or null if no end block was specified.
        /// </summary>
        public NamedBlockAst EndBlock { get; }

        /// <summary>
        /// The ast representing the dynamicparam block for a script block, or null if no dynamicparam block was specified.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Param")]
        public NamedBlockAst DynamicParamBlock { get; }

        /// <summary>
        /// All of the parsed information from any #requires in the script, or null if #requires was not used.
        /// This property is only set for the top level script block (where <see cref="Ast.Parent"/>) is null.
        /// </summary>
        public ScriptRequirements ScriptRequirements { get; internal set; }

        /// <summary>
        /// Return the help content, if any, for the script block.
        /// </summary>
        public CommentHelpInfo GetHelpContent()
        {
            Dictionary<Ast, Token[]> scriptBlockTokenCache = new Dictionary<Ast, Token[]>();
            var commentTokens = HelpCommentsParser.GetHelpCommentTokens(this, scriptBlockTokenCache);
            if (commentTokens != null)
            {
                return HelpCommentsParser.GetHelpContents(commentTokens.Item1, commentTokens.Item2);
            }

            return null;
        }

        /// <summary>
        /// Convert the ast into a script block that can be invoked.
        /// </summary>
        /// <returns>The compiled script block.</returns>
        /// <exception cref="ParseException">
        /// Thrown if there are any semantic errors in the ast.
        /// </exception>
        public ScriptBlock GetScriptBlock()
        {
            if (!PostParseChecksPerformed)
            {
                Parser parser = new Parser();
                // we call PerformPostParseChecks on root ScriptBlockAst, to obey contract of SymbolResolver.
                // It needs to be run from the top of the tree.
                // It's ok to report an error from a different part of AST in this case.
                var root = GetRootScriptBlockAst();
                root.PerformPostParseChecks(parser);
                if (parser.ErrorList.Count > 0)
                {
                    throw new ParseException(parser.ErrorList.ToArray());
                }
            }

            if (HadErrors)
            {
                throw new PSInvalidOperationException();
            }

            return new ScriptBlock(this, isFilter: false);
        }

        private ScriptBlockAst GetRootScriptBlockAst()
        {
            ScriptBlockAst rootScriptBlockAst = this;
            ScriptBlockAst parent;
            while ((parent = Ast.GetAncestorAst<ScriptBlockAst>(rootScriptBlockAst.Parent)) != null)
            {
                rootScriptBlockAst = parent;
            }

            return rootScriptBlockAst;
        }

        /// <summary>
        /// Copy the ScriptBlockAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newParamBlock = CopyElement(this.ParamBlock);
            var newBeginBlock = CopyElement(this.BeginBlock);
            var newProcessBlock = CopyElement(this.ProcessBlock);
            var newEndBlock = CopyElement(this.EndBlock);
            var newDynamicParamBlock = CopyElement(this.DynamicParamBlock);
            var newAttributes = CopyElements(this.Attributes);
            var newUsingStatements = CopyElements(this.UsingStatements);

            var scriptBlockAst = new ScriptBlockAst(this.Extent, newUsingStatements, newAttributes, newParamBlock, newBeginBlock, newProcessBlock,
                                                    newEndBlock, newDynamicParamBlock)
            {
                IsConfiguration = this.IsConfiguration,
                ScriptRequirements = this.ScriptRequirements
            };
            return scriptBlockAst;
        }

        internal string ToStringForSerialization()
        {
            string result = this.ToString();
            if (Parent != null)
            {
                // Parent is FunctionDefinitionAst or ScriptBlockExpressionAst
                // The extent includes curlies which we want to exclude.
                Diagnostics.Assert(result[0] == '{' && result[result.Length - 1] == '}',
                    "There is an incorrect assumption about the extent.");
                result = result.Substring(1, result.Length - 2);
            }

            return result;
        }

        internal string ToStringForSerialization(Tuple<List<VariableExpressionAst>, string> usingVariablesTuple, int initialStartOffset, int initialEndOffset)
        {
            Diagnostics.Assert(usingVariablesTuple.Item1 != null && usingVariablesTuple.Item1.Count > 0 && !string.IsNullOrEmpty(usingVariablesTuple.Item2),
                               "Caller makes sure the value passed in is not null or empty");
            Diagnostics.Assert(initialStartOffset < initialEndOffset && initialStartOffset >= this.Extent.StartOffset && initialEndOffset <= this.Extent.EndOffset,
                               "Caller makes sure the section is within the ScriptBlockAst");

            List<VariableExpressionAst> usingVars = usingVariablesTuple.Item1; // A list of using variables
            string newParams = usingVariablesTuple.Item2; // The new parameters are separated by the comma

            // astElements contains
            //  -- UsingVariable
            //  -- ParamBlockAst
            var astElements = new List<Ast>(usingVars);
            if (ParamBlock != null)
            {
                astElements.Add(ParamBlock);
            }

            int indexOffset = this.Extent.StartOffset;
            int startOffset = initialStartOffset - indexOffset;
            int endOffset = initialEndOffset - indexOffset;

            string script = this.ToString();
            var newScript = new StringBuilder();

            foreach (var ast in astElements.OrderBy(static ast => ast.Extent.StartOffset))
            {
                int astStartOffset = ast.Extent.StartOffset - indexOffset;
                int astEndOffset = ast.Extent.EndOffset - indexOffset;

                // Skip the ast that is before the section that we care about
                if (astStartOffset < startOffset) { continue; }
                // We are done processing the section that we care about
                if (astStartOffset >= endOffset) { break; }

                var varAst = ast as VariableExpressionAst;
                if (varAst != null)
                {
                    string varName = varAst.VariablePath.UserPath;
                    string varSign = varAst.Splatted ? "@" : "$";
                    string newVarName = varSign + UsingExpressionAst.UsingPrefix + varName;

                    newScript.Append(script.AsSpan(startOffset, astStartOffset - startOffset));
                    newScript.Append(newVarName);
                    startOffset = astEndOffset;
                }
                else
                {
                    var paramAst = ast as ParamBlockAst;
                    Diagnostics.Assert(paramAst != null, "The elements in astElements are either ParamBlockAst or VariableExpressionAst");

                    int currentOffset;
                    if (paramAst.Parameters.Count == 0)
                    {
                        currentOffset = astEndOffset - 1;
                    }
                    else
                    {
                        var firstParam = paramAst.Parameters[0];
                        currentOffset = firstParam.Attributes.Count == 0 ? firstParam.Name.Extent.StartOffset - indexOffset : firstParam.Attributes[0].Extent.StartOffset - indexOffset;
                        newParams += ",\n";
                    }

                    newScript.Append(script.AsSpan(startOffset, currentOffset - startOffset));
                    newScript.Append(newParams);
                    startOffset = currentOffset;
                }
            }

            newScript.Append(script.AsSpan(startOffset, endOffset - startOffset));
            string result = newScript.ToString();

            if (Parent != null && initialStartOffset == this.Extent.StartOffset && initialEndOffset == this.Extent.EndOffset)
            {
                // Parent is FunctionDefinitionAst or ScriptBlockExpressionAst
                // The extent includes curlies which we want to exclude.
                Diagnostics.Assert(result[0] == '{' && result[result.Length - 1] == '}',
                    "There is an incorrect assumption about the extent.");
                result = result.Substring(1, result.Length - 2);
            }

            return result;
        }

        internal void PerformPostParseChecks(Parser parser)
        {
            bool etwEnabled = ParserEventSource.Log.IsEnabled();
            if (etwEnabled) ParserEventSource.Log.ResolveSymbolsStart();

            SymbolResolver.ResolveSymbols(parser, this);
            if (etwEnabled)
            {
                ParserEventSource.Log.ResolveSymbolsStop();
                ParserEventSource.Log.SemanticChecksStart();
            }

            SemanticChecks.CheckAst(parser, this);
            if (etwEnabled) ParserEventSource.Log.SemanticChecksStop();

            Diagnostics.Assert(PostParseChecksPerformed, "Post parse checks not set during semantic checks");
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitScriptBlock(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitScriptBlock(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);

            var visitor2 = visitor as AstVisitor2;
            if (visitor2 != null)
            {
                if (action == AstVisitAction.Continue)
                {
                    foreach (var usingStatement in UsingStatements)
                    {
                        action = usingStatement.InternalVisit(visitor2);
                        if (action != AstVisitAction.Continue)
                        {
                            break;
                        }
                    }
                }

                if (action == AstVisitAction.Continue)
                {
                    foreach (var attr in Attributes)
                    {
                        action = attr.InternalVisit(visitor2);
                        if (action != AstVisitAction.Continue)
                        {
                            break;
                        }
                    }
                }
            }

            if (action == AstVisitAction.Continue && ParamBlock != null)
                action = ParamBlock.InternalVisit(visitor);
            if (action == AstVisitAction.Continue && DynamicParamBlock != null)
                action = DynamicParamBlock.InternalVisit(visitor);
            if (action == AstVisitAction.Continue && BeginBlock != null)
                action = BeginBlock.InternalVisit(visitor);
            if (action == AstVisitAction.Continue && ProcessBlock != null)
                action = ProcessBlock.InternalVisit(visitor);
            if (action == AstVisitAction.Continue && EndBlock != null)
                action = EndBlock.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        #region IParameterMetadataProvider implementation

        bool IParameterMetadataProvider.HasAnyScriptBlockAttributes()
        {
            return Attributes.Count > 0 || ParamBlock != null && ParamBlock.Attributes.Count > 0;
        }

        RuntimeDefinedParameterDictionary IParameterMetadataProvider.GetParameterMetadata(bool automaticPositions, ref bool usesCmdletBinding)
        {
            if (ParamBlock != null)
            {
                return Compiler.GetParameterMetaData(ParamBlock.Parameters, automaticPositions, ref usesCmdletBinding);
            }

            return new RuntimeDefinedParameterDictionary { Data = RuntimeDefinedParameterDictionary.EmptyParameterArray };
        }

        IEnumerable<Attribute> IParameterMetadataProvider.GetScriptBlockAttributes()
        {
            for (int index = 0; index < Attributes.Count; index++)
            {
                var attributeAst = Attributes[index];
                yield return Compiler.GetAttribute(attributeAst);
            }

            if (ParamBlock != null)
            {
                for (int index = 0; index < ParamBlock.Attributes.Count; index++)
                {
                    var attributeAst = ParamBlock.Attributes[index];
                    yield return Compiler.GetAttribute(attributeAst);
                }
            }
        }

        IEnumerable<ExperimentalAttribute> IParameterMetadataProvider.GetExperimentalAttributes()
        {
            for (int index = 0; index < Attributes.Count; index++)
            {
                AttributeAst attributeAst = Attributes[index];
                ExperimentalAttribute expAttr = GetExpAttributeHelper(attributeAst);
                if (expAttr != null) { yield return expAttr; }
            }

            if (ParamBlock != null)
            {
                for (int index = 0; index < ParamBlock.Attributes.Count; index++)
                {
                    var attributeAst = ParamBlock.Attributes[index];
                    var expAttr = GetExpAttributeHelper(attributeAst);
                    if (expAttr != null) { yield return expAttr; }
                }
            }

            static ExperimentalAttribute GetExpAttributeHelper(AttributeAst attributeAst)
            {
                AttributeAst potentialExpAttr = null;
                string expAttrTypeName = typeof(ExperimentalAttribute).FullName;
                string attrAstTypeName = attributeAst.TypeName.Name;

                if (TypeAccelerators.Get.TryGetValue(attrAstTypeName, out Type attrType) && attrType == typeof(ExperimentalAttribute))
                {
                    potentialExpAttr = attributeAst;
                }
                else if (expAttrTypeName.EndsWith(attrAstTypeName, StringComparison.OrdinalIgnoreCase))
                {
                    // Handle two cases:
                    //   1. declare the attribute using full type name;
                    //   2. declare the attribute using partial type name due to 'using namespace'.
                    int expAttrLength = expAttrTypeName.Length;
                    int attrAstLength = attrAstTypeName.Length;
                    if (expAttrLength == attrAstLength || expAttrTypeName[expAttrLength - attrAstLength - 1] == '.')
                    {
                        potentialExpAttr = attributeAst;
                    }
                }

                if (potentialExpAttr != null)
                {
                    try
                    {
                        return Compiler.GetAttribute(potentialExpAttr) as ExperimentalAttribute;
                    }
                    catch (Exception)
                    {
                        // catch all and assume it's not a declaration of ExperimentalAttribute
                    }
                }

                return null;
            }
        }

        ReadOnlyCollection<ParameterAst> IParameterMetadataProvider.Parameters
        {
            get { return (ParamBlock != null) ? this.ParamBlock.Parameters : null; }
        }

        ScriptBlockAst IParameterMetadataProvider.Body { get { return this; } }

        #region PowerShell Conversion

        PowerShell IParameterMetadataProvider.GetPowerShell(ExecutionContext context, Dictionary<string, object> variables, bool isTrustedInput,
            bool filterNonUsingVariables, bool? createLocalScope, params object[] args)
        {
            ExecutionContext.CheckStackDepth();
            return ScriptBlockToPowerShellConverter.Convert(this, null, isTrustedInput, context, variables, filterNonUsingVariables, createLocalScope, args);
        }

        string IParameterMetadataProvider.GetWithInputHandlingForInvokeCommand()
        {
            return GetWithInputHandlingForInvokeCommandImpl(null);
        }

        Tuple<string, string> IParameterMetadataProvider.GetWithInputHandlingForInvokeCommandWithUsingExpression(
            Tuple<List<VariableExpressionAst>, string> usingVariablesTuple)
        {
            string additionalNewParams = usingVariablesTuple.Item2;
            string scriptBlockText = GetWithInputHandlingForInvokeCommandImpl(usingVariablesTuple);

            string paramText = null;
            if (ParamBlock == null)
            {
                paramText = "param(" + additionalNewParams + ")" + Environment.NewLine;
            }

            return new Tuple<string, string>(paramText, scriptBlockText);
        }

        private string GetWithInputHandlingForInvokeCommandImpl(Tuple<List<VariableExpressionAst>, string> usingVariablesTuple)
        {
            // do not add "$input |" to complex pipelines
            string unused1;
            string unused2;
            var pipelineAst = GetSimplePipeline(false, out unused1, out unused2);
            if (pipelineAst == null)
            {
                return (usingVariablesTuple == null)
                    ? this.ToStringForSerialization()
                    : this.ToStringForSerialization(usingVariablesTuple, this.Extent.StartOffset, this.Extent.EndOffset);
            }

            // do not add "$input |" to pipelines beginning with an expression
            if (pipelineAst.PipelineElements[0] is CommandExpressionAst)
            {
                return (usingVariablesTuple == null)
                    ? this.ToStringForSerialization()
                    : this.ToStringForSerialization(usingVariablesTuple, this.Extent.StartOffset, this.Extent.EndOffset);
            }

            // do not add "$input |" to commands that reference $input in their arguments
            if (AstSearcher.IsUsingDollarInput(this))
            {
                return (usingVariablesTuple == null)
                    ? this.ToStringForSerialization()
                    : this.ToStringForSerialization(usingVariablesTuple, this.Extent.StartOffset, this.Extent.EndOffset);
            }

            // all checks above failed - change script into "$input | <original script>"
            var sb = new StringBuilder();
            if (ParamBlock != null)
            {
                string paramText = (usingVariablesTuple == null)
                    ? ParamBlock.ToString()
                    : this.ToStringForSerialization(usingVariablesTuple, ParamBlock.Extent.StartOffset, ParamBlock.Extent.EndOffset);
                sb.Append(paramText);
            }

            sb.Append("$input |");
            string pipelineText = (usingVariablesTuple == null)
                ? pipelineAst.ToString()
                : this.ToStringForSerialization(usingVariablesTuple, pipelineAst.Extent.StartOffset, pipelineAst.Extent.EndOffset);
            sb.Append(pipelineText);

            return sb.ToString();
        }

        #endregion PowerShell Conversion

        bool IParameterMetadataProvider.UsesCmdletBinding()
        {
            bool usesCmdletBinding = false;

            if (ParamBlock != null)
            {
                usesCmdletBinding = this.ParamBlock.Attributes.Any(static attribute => typeof(CmdletBindingAttribute) == attribute.TypeName.GetReflectionAttributeType());
                if (!usesCmdletBinding)
                {
                    usesCmdletBinding = ParamBlockAst.UsesCmdletBinding(ParamBlock.Parameters);
                }
            }

            return usesCmdletBinding;
        }

        #endregion IParameterMetadataProvider implementation

        internal PipelineAst GetSimplePipeline(bool allowMultiplePipelines, out string errorId, out string errorMsg)
        {
            if (BeginBlock != null || ProcessBlock != null || DynamicParamBlock != null)
            {
                errorId = "CanConvertOneClauseOnly";
                errorMsg = AutomationExceptions.CanConvertOneClauseOnly;
                return null;
            }

            if (EndBlock == null || EndBlock.Statements.Count < 1)
            {
                errorId = "CantConvertEmptyPipeline";
                errorMsg = AutomationExceptions.CantConvertEmptyPipeline;
                return null;
            }

            if (EndBlock.Traps != null && EndBlock.Traps.Count > 0)
            {
                errorId = "CantConvertScriptBlockWithTrap";
                errorMsg = AutomationExceptions.CantConvertScriptBlockWithTrap;
                return null;
            }

            // Make sure all statements are pipelines.
            if (EndBlock.Statements.Any(ast => ast is not PipelineAst))
            {
                errorId = "CanOnlyConvertOnePipeline";
                errorMsg = AutomationExceptions.CanOnlyConvertOnePipeline;
                return null;
            }

            if (EndBlock.Statements.Count != 1 && !allowMultiplePipelines)
            {
                errorId = "CanOnlyConvertOnePipeline";
                errorMsg = AutomationExceptions.CanOnlyConvertOnePipeline;
                return null;
            }

            errorId = null;
            errorMsg = null;
            return EndBlock.Statements[0] as PipelineAst;
        }
    }

    /// <summary>
    /// The ast representing the param statement in a script block.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Param")]
    public class ParamBlockAst : Ast
    {
        private static readonly ReadOnlyCollection<AttributeAst> s_emptyAttributeList =
            Utils.EmptyReadOnlyCollection<AttributeAst>();

        private static readonly ReadOnlyCollection<ParameterAst> s_emptyParameterList =
            Utils.EmptyReadOnlyCollection<ParameterAst>();

        /// <summary>
        /// Construct the ast for a param statement of a script block.
        /// </summary>
        /// <param name="extent">The extent of the param statement, from any possible attributes to the closing paren.</param>
        /// <param name="attributes">The attributes (such as [cmdletbinding()]) specified on the param statement.  May be null.</param>
        /// <param name="parameters">The parameters to the script block.  May be null.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        public ParamBlockAst(IScriptExtent extent, IEnumerable<AttributeAst> attributes, IEnumerable<ParameterAst> parameters)
            : base(extent)
        {
            if (attributes != null)
            {
                this.Attributes = new ReadOnlyCollection<AttributeAst>(attributes.ToArray());
                SetParents(Attributes);
            }
            else
            {
                this.Attributes = s_emptyAttributeList;
            }

            if (parameters != null)
            {
                this.Parameters = new ReadOnlyCollection<ParameterAst>(parameters.ToArray());
                SetParents(Parameters);
            }
            else
            {
                this.Parameters = s_emptyParameterList;
            }
        }

        /// <summary>
        /// The asts for attributes (such as [cmdletbinding()]) used before the param keyword.
        /// </summary>
        public ReadOnlyCollection<AttributeAst> Attributes { get; }

        /// <summary>
        /// The asts for the parameters of the param statement.
        /// </summary>
        public ReadOnlyCollection<ParameterAst> Parameters { get; }

        /// <summary>
        /// Copy the ParamBlockAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newAttributes = CopyElements(this.Attributes);
            var newParameters = CopyElements(this.Parameters);
            return new ParamBlockAst(this.Extent, newAttributes, newParameters);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitParamBlock(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitParamBlock(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < Attributes.Count; index++)
                {
                    var attributeAst = Attributes[index];
                    action = attributeAst.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < Parameters.Count; index++)
                {
                    var paramAst = Parameters[index];
                    action = paramAst.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        internal static bool UsesCmdletBinding(IEnumerable<ParameterAst> parameters)
        {
            bool usesCmdletBinding = false;
            foreach (var parameter in parameters)
            {
                usesCmdletBinding = parameter.Attributes.Any(attribute => (attribute.TypeName.GetReflectionAttributeType() != null) &&
                                                                          attribute.TypeName.GetReflectionAttributeType() == typeof(ParameterAttribute));
                if (usesCmdletBinding)
                {
                    break;
                }
            }

            return usesCmdletBinding;
        }
    }

    /// <summary>
    /// The ast representing a begin, process, end, or dynamicparam block in a scriptblock.  This ast is used even
    /// when the block is unnamed, in which case the block is either an end block (for functions) or process block (for filters).
    /// </summary>
    public class NamedBlockAst : Ast
    {
        /// <summary>
        /// Construct the ast for a begin, process, end, or dynamic param block.
        /// </summary>
        /// <param name="extent">
        /// The extent of the block.  If <paramref name="unnamed"/> is false, the extent includes
        /// the keyword through the closing curly, otherwise the extent is the as the extent of <paramref name="statementBlock"/>.
        /// </param>
        /// <param name="blockName">
        /// The kind of block, must be one of:
        /// <list type="bullet">
        /// <item><see cref="TokenKind.Begin"/></item>
        /// <item><see cref="TokenKind.Process"/></item>
        /// <item><see cref="TokenKind.End"/></item>
        /// <item><see cref="TokenKind.Dynamicparam"/></item>
        /// </list>
        /// </param>
        /// <param name="statementBlock">The ast for the statements in this named block.</param>
        /// <param name="unnamed">True if the block was not explicitly named.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="statementBlock"/> is null.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// If <paramref name="blockName"/> is not one of the valid kinds for a named block,
        /// or if <paramref name="unnamed"/> is <see langword="true"/> and <paramref name="blockName"/> is neither
        /// <see cref="TokenKind.Process"/> nor <see cref="TokenKind.End"/>.
        /// </exception>
        public NamedBlockAst(IScriptExtent extent, TokenKind blockName, StatementBlockAst statementBlock, bool unnamed)
            : base(extent)
        {
            // Validate the block name.  If the block is unnamed, it must be an End block (for a function)
            // or Process block (for a filter).
            if (!blockName.HasTrait(TokenFlags.ScriptBlockBlockName)
                || (unnamed && (blockName == TokenKind.Begin || blockName == TokenKind.Dynamicparam)))
            {
                throw PSTraceSource.NewArgumentException(nameof(blockName));
            }

            if (statementBlock == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(statementBlock));
            }

            this.Unnamed = unnamed;
            this.BlockKind = blockName;
            var statements = statementBlock.Statements;
            this.Statements = statements;
            for (int index = 0; index < statements.Count; index++)
            {
                var stmt = statements[index];
                stmt.ClearParent();
            }

            SetParents(statements);

            var traps = statementBlock.Traps;
            if (traps != null && traps.Count > 0)
            {
                this.Traps = traps;
                for (int index = 0; index < traps.Count; index++)
                {
                    var trap = traps[index];
                    trap.ClearParent();
                }

                SetParents(traps);
            }

            if (!unnamed)
            {
                var statementsExtent = statementBlock.Extent as InternalScriptExtent;
                if (statementsExtent != null)
                {
                    this.OpenCurlyExtent = new InternalScriptExtent(statementsExtent.PositionHelper, statementsExtent.StartOffset, statementsExtent.StartOffset + 1);
                    this.CloseCurlyExtent = new InternalScriptExtent(statementsExtent.PositionHelper, statementsExtent.EndOffset - 1, statementsExtent.EndOffset);
                }
            }
        }

        /// <summary>
        /// For a function/filter that did not explicitly name the end/process block (which is quite common),
        /// this property will return true.
        /// </summary>
        public bool Unnamed { get; }

        /// <summary>
        /// The kind of block, always one of:
        /// <list type="bullet">
        /// <item><see cref="TokenKind.Begin"/></item>
        /// <item><see cref="TokenKind.Process"/></item>
        /// <item><see cref="TokenKind.End"/></item>
        /// <item><see cref="TokenKind.Dynamicparam"/></item>
        /// </list>
        /// </summary>
        public TokenKind BlockKind { get; }

        /// <summary>
        /// The asts for all of the statements represented by this statement block.  This property is never null.
        /// </summary>
        public ReadOnlyCollection<StatementAst> Statements { get; }

        /// <summary>
        /// The asts for all of the trap statements specified by this statement block, or null if no trap statements were
        /// specified in this block.
        /// </summary>
        public ReadOnlyCollection<TrapStatementAst> Traps { get; }

        /// <summary>
        /// Copy the NamedBlockAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newTraps = CopyElements(this.Traps);
            var newStatements = CopyElements(this.Statements);
            var statementBlockExtent = this.Extent;

            if (this.OpenCurlyExtent != null && this.CloseCurlyExtent != null)
            {
                // For explicitly named block, the Extent of the StatementBlockAst is not
                // the same as the Extent of the NamedBlockAst. In this case, reconstruct
                // the Extent of the StatementBlockAst from openExtent/closeExtent.
                var openExtent = (InternalScriptExtent)this.OpenCurlyExtent;
                var closeExtent = (InternalScriptExtent)this.CloseCurlyExtent;
                statementBlockExtent = new InternalScriptExtent(openExtent.PositionHelper, openExtent.StartOffset, closeExtent.EndOffset);
            }

            var statementBlock = new StatementBlockAst(statementBlockExtent, newStatements, newTraps);
            return new NamedBlockAst(this.Extent, this.BlockKind, statementBlock, this.Unnamed);
        }

        // Used by the debugger for command breakpoints
        internal IScriptExtent OpenCurlyExtent { get; }

        internal IScriptExtent CloseCurlyExtent { get; }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitNamedBlock(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitNamedBlock(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = StatementBlockAst.InternalVisit(visitor, Traps, Statements, action);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast representing a named attribute argument.  For example, in <c>[Parameter(Mandatory=$true)]</c>, this ast
    /// represents <c>Mandatory=$true</c>.
    /// </summary>
    public class NamedAttributeArgumentAst : Ast
    {
        /// <summary>
        /// Construct the ast for a named attribute argument.
        /// </summary>
        /// <param name="extent">
        /// The extent of the named attribute argument, starting with the name, ending with the expression, or if the expression
        /// is omitted from the source, then ending at the end of the name.
        /// </param>
        /// <param name="argumentName">The name of the argument specified.  May not be null or empty.</param>
        /// <param name="argument">The argument expression.  May not be null even if the expression is omitted from the source.</param>
        /// <param name="expressionOmitted">
        /// True when an explicit argument is not provided in the source, e.g. <c>[Parameter(Mandatory)]</c>.  In this case,
        /// an ast for the argument expression must still be provided.
        /// </param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/>, <paramref name="argumentName"/>, or <paramref name="argument"/> is null, or if
        /// <paramref name="argumentName"/> is an empty string.
        /// </exception>
        public NamedAttributeArgumentAst(IScriptExtent extent, string argumentName, ExpressionAst argument, bool expressionOmitted)
            : base(extent)
        {
            if (string.IsNullOrEmpty(argumentName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(argumentName));
            }

            if (argument == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(argument));
            }

            this.Argument = argument;
            SetParent(argument);
            this.ArgumentName = argumentName;
            this.ExpressionOmitted = expressionOmitted;
        }

        /// <summary>
        /// The named argument specified by this ast, is never null or empty.
        /// </summary>
        public string ArgumentName { get; }

        /// <summary>
        /// The ast of the value of the argument specified by this ast.  This property is never null.
        /// </summary>
        public ExpressionAst Argument { get; }

        /// <summary>
        /// If the source omitted an expression, this returns true, otherwise false.  This allows a caller to distinguish
        /// the difference between <c>[Parameter(Mandatory)]</c> and <c>[Parameter(Mandatory=$true)]</c>
        /// </summary>
        public bool ExpressionOmitted { get; }

        /// <summary>
        /// Copy the NamedAttributeArgumentAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newArgument = CopyElement(this.Argument);
            return new NamedAttributeArgumentAst(this.Extent, this.ArgumentName, newArgument, this.ExpressionOmitted);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitNamedAttributeArgument(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitNamedAttributeArgument(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Argument.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// An abstract base class representing attributes that accept optional arguments, e.g. <c>[Parameter()]</c>, as well as
    /// type constraints, such as <c>[int]</c>.
    /// </summary>
    public abstract class AttributeBaseAst : Ast
    {
        /// <summary>
        /// Initialize the common fields for an attribute.
        /// </summary>
        /// <param name="extent">The extent of the attribute, from the opening '[' to the closing ']'.</param>
        /// <param name="typeName">The type named by the attribute.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="typeName"/> is null.
        /// </exception>
        protected AttributeBaseAst(IScriptExtent extent, ITypeName typeName)
            : base(extent)
        {
            if (typeName == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(typeName));
            }

            this.TypeName = typeName;
        }

        /// <summary>
        /// The type name for the attribute.  This property is never null.
        /// </summary>
        public ITypeName TypeName { get; }

        internal abstract Attribute GetAttribute();
    }

    /// <summary>
    /// The ast representing an attribute with optional positional and named arguments.
    /// </summary>
    public class AttributeAst : AttributeBaseAst
    {
        private static readonly ReadOnlyCollection<ExpressionAst> s_emptyPositionalArguments =
            Utils.EmptyReadOnlyCollection<ExpressionAst>();

        private static readonly ReadOnlyCollection<NamedAttributeArgumentAst> s_emptyNamedAttributeArguments =
            Utils.EmptyReadOnlyCollection<NamedAttributeArgumentAst>();

        /// <summary>
        /// Construct an attribute ast.
        /// </summary>
        /// <param name="extent">The extent of the attribute from opening '[' to closing ']'.</param>
        /// <param name="namedArguments">The named arguments, may be null.</param>
        /// <param name="positionalArguments">The positional arguments, may be null.</param>
        /// <param name="typeName">The attribute name.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="typeName"/> is null.
        /// </exception>
        public AttributeAst(IScriptExtent extent,
                            ITypeName typeName,
                            IEnumerable<ExpressionAst> positionalArguments,
                            IEnumerable<NamedAttributeArgumentAst> namedArguments)
            : base(extent, typeName)
        {
            if (positionalArguments != null)
            {
                this.PositionalArguments = new ReadOnlyCollection<ExpressionAst>(positionalArguments.ToArray());
                SetParents(PositionalArguments);
            }
            else
            {
                this.PositionalArguments = s_emptyPositionalArguments;
            }

            if (namedArguments != null)
            {
                this.NamedArguments = new ReadOnlyCollection<NamedAttributeArgumentAst>(namedArguments.ToArray());
                SetParents(NamedArguments);
            }
            else
            {
                this.NamedArguments = s_emptyNamedAttributeArguments;
            }
        }

        /// <summary>
        /// The asts for the attribute arguments specified positionally.
        /// </summary>
        public ReadOnlyCollection<ExpressionAst> PositionalArguments { get; }

        /// <summary>
        /// The asts for the named attribute arguments.
        /// </summary>
        public ReadOnlyCollection<NamedAttributeArgumentAst> NamedArguments { get; }

        /// <summary>
        /// Copy the AttributeAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newPositionalArguments = CopyElements(this.PositionalArguments);
            var newNamedArguments = CopyElements(this.NamedArguments);
            return new AttributeAst(this.Extent, this.TypeName, newPositionalArguments, newNamedArguments);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitAttribute(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitAttribute(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < PositionalArguments.Count; index++)
                {
                    var expressionAst = PositionalArguments[index];
                    action = expressionAst.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < NamedArguments.Count; index++)
                {
                    var namedAttributeArgumentAst = NamedArguments[index];
                    action = namedAttributeArgumentAst.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        internal override Attribute GetAttribute()
        {
            return Compiler.GetAttribute(this);
        }
    }

    /// <summary>
    /// The ast representing a type constraint, which is simply a typename with no arguments.
    /// </summary>
    public class TypeConstraintAst : AttributeBaseAst
    {
        /// <summary>
        /// Construct a type constraint from a possibly not yet resolved typename.
        /// </summary>
        /// <param name="extent">The extent of the constraint, from the opening '[' to the closing ']'.</param>
        /// <param name="typeName">The type for the constraint.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="typeName"/> is null.
        /// </exception>
        public TypeConstraintAst(IScriptExtent extent, ITypeName typeName)
            : base(extent, typeName)
        {
        }

        /// <summary>
        /// Construct a type constraint from a <see cref="Type"/>.
        /// </summary>
        /// <param name="extent">The extent of the constraint, from the opening '[' to the closing ']'.</param>
        /// <param name="type">The type for the constraint.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="type"/> is null.
        /// </exception>
        public TypeConstraintAst(IScriptExtent extent, Type type)
            : base(extent, new ReflectionTypeName(type))
        {
        }

        /// <summary>
        /// Copy the TypeConstraintAst instance.
        /// </summary>
        public override Ast Copy()
        {
            return new TypeConstraintAst(this.Extent, this.TypeName);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitTypeConstraint(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitTypeConstraint(this);
            return visitor.CheckForPostAction(this, action == AstVisitAction.SkipChildren ? AstVisitAction.Continue : action);
        }

        #endregion Visitors

        internal override Attribute GetAttribute()
        {
            return Compiler.GetAttribute(this);
        }
    }

    /// <summary>
    /// The ast representing a parameter to a script.  Parameters may appear in one of 2 places, either just after the
    /// name of the function, e.g. <c>function foo($a){}</c> or in a param statement, e.g. <c>param($a)</c>.
    /// </summary>
    public class ParameterAst : Ast
    {
        private static readonly ReadOnlyCollection<AttributeBaseAst> s_emptyAttributeList =
            Utils.EmptyReadOnlyCollection<AttributeBaseAst>();

        /// <summary>
        /// Construct a parameter ast from the name, attributes, and default value.
        /// </summary>
        /// <param name="extent">The extent of the parameter, including the attributes and default if specified.</param>
        /// <param name="name">The name of the variable.</param>
        /// <param name="attributes">The attributes, or null if no attributes were specified.</param>
        /// <param name="defaultValue">The default value of the parameter, or null if no default value was specified.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="name"/> is null.
        /// </exception>
        public ParameterAst(IScriptExtent extent,
                            VariableExpressionAst name,
                            IEnumerable<AttributeBaseAst> attributes,
                            ExpressionAst defaultValue)
            : base(extent)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            if (attributes != null)
            {
                this.Attributes = new ReadOnlyCollection<AttributeBaseAst>(attributes.ToArray());
                SetParents(Attributes);
            }
            else
            {
                this.Attributes = s_emptyAttributeList;
            }

            this.Name = name;
            SetParent(name);
            if (defaultValue != null)
            {
                this.DefaultValue = defaultValue;
                SetParent(defaultValue);
            }
        }

        /// <summary>
        /// The asts for any attributes or type constraints specified on the parameter.
        /// </summary>
        public ReadOnlyCollection<AttributeBaseAst> Attributes { get; }

        /// <summary>
        /// The variable path for the parameter.  This property is never null.
        /// </summary>
        public VariableExpressionAst Name { get; }

        /// <summary>
        /// The ast for the default value of the parameter, or null if no default value was specified.
        /// </summary>
        public ExpressionAst DefaultValue { get; }

        /// <summary>
        /// Returns the type of the parameter.  If the parameter is constrained to be a specific type, that type is returned,
        /// otherwise <c>typeof(object)</c> is returned.
        /// </summary>
        public Type StaticType
        {
            get
            {
                Type type = null;
                var typeConstraint = Attributes.OfType<TypeConstraintAst>().FirstOrDefault();
                if (typeConstraint != null)
                {
                    type = typeConstraint.TypeName.GetReflectionType();
                }

                return type ?? typeof(object);
            }
        }

        /// <summary>
        /// Copy the ParameterAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newName = CopyElement(this.Name);
            var newAttributes = CopyElements(this.Attributes);
            var newDefaultValue = CopyElement(this.DefaultValue);
            return new ParameterAst(this.Extent, newName, newAttributes, newDefaultValue);
        }

        internal string GetTooltip()
        {
            var typeConstraint = Attributes.OfType<TypeConstraintAst>().FirstOrDefault();
            var type = typeConstraint != null ? typeConstraint.TypeName.FullName : "object";
            return type + " " + Name.VariablePath.UserPath;
        }

        /// <summary>
        /// Get the text that represents this ParameterAst based on the $using variables passed in.
        /// A parameter name cannot be a using variable, but its default value could contain any number of UsingExpressions, for example:
        ///     function bar ($x = (Get-X @using:defaultSettings.Parameters)) { ... }
        /// This method goes through the ParameterAst text and replace each $using variable with its new synthetic name (remove the $using prefix).
        /// This method is used when we call Invoke-Command targeting a PSv2 remote machine. In that case, we might need to call this method
        /// to process the script block text, since $using prefix cannot be recognized by PSv2.
        /// </summary>
        /// <param name="orderedUsingVar">A sorted enumerator of using variable asts, ascendingly sorted based on StartOffSet.</param>
        /// <returns>
        /// The text of the ParameterAst with $using variable being replaced with a new variable name.
        /// </returns>
        internal string GetParamTextWithDollarUsingHandling(IEnumerator<VariableExpressionAst> orderedUsingVar)
        {
            int indexOffset = Extent.StartOffset;
            int startOffset = 0;
            int endOffset = Extent.EndOffset - Extent.StartOffset;

            string paramText = ToString();
            if (orderedUsingVar.Current == null && !orderedUsingVar.MoveNext())
            {
                return paramText;
            }

            var newParamText = new StringBuilder();
            do
            {
                var varAst = orderedUsingVar.Current;
                int astStartOffset = varAst.Extent.StartOffset - indexOffset;
                int astEndOffset = varAst.Extent.EndOffset - indexOffset;

                // Skip the VariableAst that is before section we care about
                if (astStartOffset < startOffset) { continue; }
                // We are done processing the current ParameterAst
                if (astStartOffset >= endOffset) { break; }

                string varName = varAst.VariablePath.UserPath;
                string varSign = varAst.Splatted ? "@" : "$";
                string newVarName = varSign + UsingExpressionAst.UsingPrefix + varName;

                newParamText.Append(paramText.AsSpan(startOffset, astStartOffset - startOffset));
                newParamText.Append(newVarName);
                startOffset = astEndOffset;
            } while (orderedUsingVar.MoveNext());

            if (startOffset == 0)
            {
                // Nothing changed within the ParameterAst text
                return paramText;
            }

            newParamText.Append(paramText.AsSpan(startOffset, endOffset - startOffset));
            return newParamText.ToString();
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitParameter(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitParameter(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < Attributes.Count; index++)
                {
                    var attributeAst = Attributes[index];
                    action = attributeAst.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            if (action == AstVisitAction.Continue)
            {
                action = Name.InternalVisit(visitor);
            }

            if (action == AstVisitAction.Continue && DefaultValue != null)
            {
                action = DefaultValue.InternalVisit(visitor);
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    #endregion Script Blocks

    #region Statements

    /// <summary>
    /// The ast representing a block of statements.  The block of statements could be part of a script block or some other
    /// statement such as an if statement or while statement.
    /// </summary>
    public class StatementBlockAst : Ast
    {
        private static readonly ReadOnlyCollection<StatementAst> s_emptyStatementCollection = Utils.EmptyReadOnlyCollection<StatementAst>();

        /// <summary>
        /// Construct a statement block.
        /// </summary>
        /// <param name="extent">The extent of the statement block.  If curly braces are part of the statement block (and
        /// not some other ast like in a script block), then the curly braces are included in the extent, otherwise the
        /// extent runs from the first statement or trap to the last statement or trap.</param>
        /// <param name="statements">The (possibly null) collection of statements.</param>
        /// <param name="traps">The (possibly null) collection of trap statements.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        public StatementBlockAst(IScriptExtent extent, IEnumerable<StatementAst> statements, IEnumerable<TrapStatementAst> traps)
            : base(extent)
        {
            if (statements != null)
            {
                this.Statements = new ReadOnlyCollection<StatementAst>(statements.ToArray());
                SetParents(Statements);
            }
            else
            {
                this.Statements = s_emptyStatementCollection;
            }

            if (traps != null && traps.Any())
            {
                this.Traps = new ReadOnlyCollection<TrapStatementAst>(traps.ToArray());
                SetParents(Traps);
            }
        }

        /// <summary>
        /// The asts for all of the statements represented by this statement block.  This property is never null.
        /// </summary>
        public ReadOnlyCollection<StatementAst> Statements { get; }

        /// <summary>
        /// The asts for all of the trap statements specified by this statement block, or null if no trap statements were
        /// specified in this block.
        /// </summary>
        public ReadOnlyCollection<TrapStatementAst> Traps { get; }

        /// <summary>
        /// Copy the StatementBlockAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newStatements = CopyElements(this.Statements);
            var newTraps = CopyElements(this.Traps);

            return new StatementBlockAst(this.Extent, newStatements, newTraps);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitStatementBlock(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitStatementBlock(this);
            return visitor.CheckForPostAction(this, InternalVisit(visitor, Traps, Statements, action));
        }

        internal static AstVisitAction InternalVisit(AstVisitor visitor,
                                                     ReadOnlyCollection<TrapStatementAst> traps,
                                                     ReadOnlyCollection<StatementAst> statements,
                                                     AstVisitAction action)
        {
            if (action == AstVisitAction.SkipChildren)
                return AstVisitAction.Continue;

            if (action == AstVisitAction.Continue && traps != null)
            {
                for (int index = 0; index < traps.Count; index++)
                {
                    var trapAst = traps[index];
                    action = trapAst.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            if (action == AstVisitAction.Continue && statements != null)
            {
                for (int index = 0; index < statements.Count; index++)
                {
                    var statementAst = statements[index];
                    action = statementAst.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            return action;
        }

        #endregion Visitors
    }

    /// <summary>
    /// An abstract base class for any statement like an if statement or while statement.
    /// </summary>
    public abstract class StatementAst : Ast
    {
        /// <summary>
        /// Initialize the common fields of a statement.
        /// </summary>
        /// <param name="extent">The extent of the statement.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        protected StatementAst(IScriptExtent extent)
            : base(extent)
        {
        }
    }

    /// <summary>
    /// Specifies type attributes.
    /// </summary>
    [Flags]
    public enum TypeAttributes
    {
        /// <summary>No attributes specified.</summary>
        None = 0x00,

        /// <summary>The type specifies a class.</summary>
        Class = 0x01,

        /// <summary>The type specifies an interface.</summary>
        Interface = 0x02,

        /// <summary>The type specifies an enum.</summary>
        Enum = 0x04,
    }

    /// <summary>
    /// The ast representing a type definition including attributes, base class and
    /// implemented interfaces, plus it's members.
    /// </summary>
    public class TypeDefinitionAst : StatementAst
    {
        private static readonly ReadOnlyCollection<AttributeAst> s_emptyAttributeList =
            Utils.EmptyReadOnlyCollection<AttributeAst>();

        private static readonly ReadOnlyCollection<MemberAst> s_emptyMembersCollection =
            Utils.EmptyReadOnlyCollection<MemberAst>();

        private static readonly ReadOnlyCollection<TypeConstraintAst> s_emptyBaseTypesCollection =
            Utils.EmptyReadOnlyCollection<TypeConstraintAst>();

        /// <summary>
        /// Construct a type definition.
        /// </summary>
        /// <param name="extent">The extent of the type definition, from any attributes to the closing curly brace.</param>
        /// <param name="name">The name of the type.</param>
        /// <param name="attributes">The attributes, or null if no attributes were specified.</param>
        /// <param name="members">The members, or null if no members were specified.</param>
        /// <param name="typeAttributes">The attributes (like class or interface) of the type.</param>
        /// <param name="baseTypes">Base class and implemented interfaces for the type.</param>
        public TypeDefinitionAst(IScriptExtent extent, string name, IEnumerable<AttributeAst> attributes, IEnumerable<MemberAst> members, TypeAttributes typeAttributes, IEnumerable<TypeConstraintAst> baseTypes)
            : base(extent)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            if (attributes != null && attributes.Any())
            {
                Attributes = new ReadOnlyCollection<AttributeAst>(attributes.ToArray());
                SetParents(Attributes);
            }
            else
            {
                Attributes = s_emptyAttributeList;
            }

            if (members != null && members.Any())
            {
                Members = new ReadOnlyCollection<MemberAst>(members.ToArray());
                SetParents(Members);
            }
            else
            {
                Members = s_emptyMembersCollection;
            }

            if (baseTypes != null && baseTypes.Any())
            {
                BaseTypes = new ReadOnlyCollection<TypeConstraintAst>(baseTypes.ToArray());
                SetParents(BaseTypes);
            }
            else
            {
                BaseTypes = s_emptyBaseTypesCollection;
            }

            this.Name = name;
            this.TypeAttributes = typeAttributes;
        }

        /// <summary>
        /// The name of the type.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The asts for the custom attributes specified on the type.  This property is never null.
        /// </summary>
        public ReadOnlyCollection<AttributeAst> Attributes { get; }

        /// <summary>
        /// The asts for the base types. This property is never null.
        /// </summary>
        public ReadOnlyCollection<TypeConstraintAst> BaseTypes { get; }

        /// <summary>
        /// The asts for the members of the type.  This property is never null.
        /// </summary>
        public ReadOnlyCollection<MemberAst> Members { get; }

        /// <summary>
        /// The type attributes (like class or interface) of the type.
        /// </summary>
        public TypeAttributes TypeAttributes { get; }

        /// <summary>
        /// Returns true if the type defines an enum.
        /// </summary>
        public bool IsEnum { get { return (TypeAttributes & TypeAttributes.Enum) == TypeAttributes.Enum; } }

        /// <summary>
        /// Returns true if the type defines a class.
        /// </summary>
        public bool IsClass { get { return (TypeAttributes & TypeAttributes.Class) == TypeAttributes.Class; } }

        /// <summary>
        /// Returns true if the type defines an interface.
        /// </summary>
        public bool IsInterface { get { return (TypeAttributes & TypeAttributes.Interface) == TypeAttributes.Interface; } }

        internal Type Type
        {
            get
            {
                return _type;
            }

            set
            {
                // The assert may seem a little bit confusing.
                // It's because RuntimeType is not a public class and I don't want to use Name string in assert.
                // The basic idea is that Type field should go thru 3 stages:
                // 1. null
                // 2. TypeBuilder
                // 3. RuntimeType
                // We also allow wipe type (assign to null), because there could be errors.
                Diagnostics.Assert(value == null || _type == null || _type is TypeBuilder, "Type must be assigned only once to RuntimeType");
                _type = value;
            }
        }

        /// <summary>
        /// Copy the TypeDefinitionAst.
        /// </summary>
        public override Ast Copy()
        {
            return new TypeDefinitionAst(Extent, Name, CopyElements(Attributes), CopyElements(Members), TypeAttributes, CopyElements(BaseTypes));
        }

        private Type _type;

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            var visitor2 = visitor as ICustomAstVisitor2;
            return visitor2?.VisitTypeDefinition(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = AstVisitAction.Continue;
            var visitor2 = visitor as AstVisitor2;
            if (visitor2 != null)
            {
                action = visitor2.VisitTypeDefinition(this);
                if (action == AstVisitAction.SkipChildren)
                    return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            }

            // REVIEW: should old visitors completely skip the attributes and
            // bodies of methods, or should they get a chance to see them.  If
            // we want to skip them, the code below needs to move up into the
            // above test 'visitor2 != null'.
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < Attributes.Count; index++)
                {
                    var attribute = Attributes[index];
                    action = attribute.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue)
                    {
                        break;
                    }
                }
            }

            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < BaseTypes.Count; index++)
                {
                    var baseTypes = BaseTypes[index];
                    action = baseTypes.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue)
                    {
                        break;
                    }
                }
            }

            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < Members.Count; index++)
                {
                    var member = Members[index];
                    action = member.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue)
                    {
                        break;
                    }
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The kind of using statement.
    /// </summary>
    public enum UsingStatementKind
    {
        /// <summary>
        /// A parse time reference to an assembly.
        /// </summary>
        Assembly = 0,

        /// <summary>
        /// A parse time command alias.
        /// </summary>
        Command = 1,

        /// <summary>
        /// A parse time reference or alias to a module.
        /// </summary>
        Module = 2,

        /// <summary>
        /// A parse time statement that allows specifying types without their full namespace.
        /// </summary>
        Namespace = 3,

        /// <summary>
        /// A parse time type alias (type accelerator).
        /// </summary>
        Type = 4,
    }

    /// <summary>
    /// The ast representing a using statement.
    /// </summary>
    public class UsingStatementAst : StatementAst
    {
        /// <summary>
        /// Construct a simple using statement (one that is not a form of an alias).
        /// </summary>
        /// <param name="extent">The extent of the using statement including the using keyword.</param>
        /// <param name="kind">
        /// The kind of using statement, cannot be <see cref="System.Management.Automation.Language.UsingStatementKind.Command"/>
        /// or <see cref="System.Management.Automation.Language.UsingStatementKind.Type"/>
        /// </param>
        /// <param name="name">The item (assembly, module, or namespace) being used.</param>
        public UsingStatementAst(IScriptExtent extent, UsingStatementKind kind, StringConstantExpressionAst name)
            : base(extent)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            if (kind == UsingStatementKind.Command || kind == UsingStatementKind.Type)
            {
                throw PSTraceSource.NewArgumentException(nameof(kind));
            }

            UsingStatementKind = kind;
            Name = name;

            SetParent(Name);
        }

        /// <summary>
        /// Construct a simple (one that is not a form of an alias) using module statement with module specification as hashtable.
        /// </summary>
        /// <param name="extent">The extent of the using statement including the using keyword.</param>
        /// <param name="moduleSpecification">HashtableAst that describes <see cref="Microsoft.PowerShell.Commands.ModuleSpecification"/> object.</param>
        public UsingStatementAst(IScriptExtent extent, HashtableAst moduleSpecification)
            : base(extent)
        {
            if (moduleSpecification == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(moduleSpecification));
            }

            UsingStatementKind = UsingStatementKind.Module;
            ModuleSpecification = moduleSpecification;

            SetParent(moduleSpecification);
        }

        /// <summary>
        /// Construct a using statement that aliases an item.
        /// </summary>
        /// <param name="extent">The extent of the using statement including the using keyword.</param>
        /// <param name="kind">
        /// The kind of using statement, cannot be <see cref="System.Management.Automation.Language.UsingStatementKind.Assembly"/>.
        /// </param>
        /// <param name="aliasName">The name of the alias.</param>
        /// <param name="resolvedAliasAst">The item being aliased.</param>
        public UsingStatementAst(IScriptExtent extent, UsingStatementKind kind, StringConstantExpressionAst aliasName,
                                 StringConstantExpressionAst resolvedAliasAst)
            : base(extent)
        {
            if (aliasName == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(aliasName));
            }

            if (resolvedAliasAst == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(resolvedAliasAst));
            }

            if (kind == UsingStatementKind.Assembly)
            {
                throw PSTraceSource.NewArgumentException(nameof(kind));
            }

            UsingStatementKind = kind;
            Name = aliasName;
            Alias = resolvedAliasAst;

            SetParent(Name);
            SetParent(Alias);
        }

        /// <summary>
        /// Construct a using module statement that aliases an item with module specification as hashtable.
        /// </summary>
        /// <param name="extent">The extent of the using statement including the using keyword.</param>
        /// <param name="aliasName">The name of the alias.</param>
        /// <param name="moduleSpecification">The module being aliased. Hashtable that describes <see cref="Microsoft.PowerShell.Commands.ModuleSpecification"/></param>
        public UsingStatementAst(IScriptExtent extent, StringConstantExpressionAst aliasName, HashtableAst moduleSpecification)
            : base(extent)
        {
            if (moduleSpecification == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(moduleSpecification));
            }

            UsingStatementKind = UsingStatementKind.Module;
            ModuleSpecification = moduleSpecification;
            Name = aliasName;

            SetParent(moduleSpecification);
        }

        /// <summary>
        /// The kind of using statement.
        /// </summary>
        public UsingStatementKind UsingStatementKind { get; }

        /// <summary>
        /// When <see cref="Alias"/> is null or <see cref="ModuleSpecification"/> is null, the item being used, otherwise the alias name.
        /// </summary>
        public StringConstantExpressionAst Name { get; }

        /// <summary>
        /// The name of the item being aliased.
        /// This property is mutually exclusive with <see cref="ModuleSpecification"/> property.
        /// </summary>
        public StringConstantExpressionAst Alias { get; }

        /// <summary>
        /// Hashtable that can be converted to <see cref="Microsoft.PowerShell.Commands.ModuleSpecification"/>. Only for 'using module' case, otherwise null.
        /// This property is mutually exclusive with <see cref="Alias"/> property.
        /// </summary>
        public HashtableAst ModuleSpecification { get; }

        /// <summary>
        /// ModuleInfo about used module. Only for 'using module' case, otherwise null.
        /// </summary>
        internal PSModuleInfo ModuleInfo { get; private set; }

        /// <summary>
        /// Copy the UsingStatementAst.
        /// </summary>
        public override Ast Copy()
        {
            var copy = Alias != null
                ? new UsingStatementAst(Extent, UsingStatementKind, Name, Alias)
                : new UsingStatementAst(Extent, UsingStatementKind, Name);
            copy.ModuleInfo = ModuleInfo;

            return copy;
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            var visitor2 = visitor as ICustomAstVisitor2;
            return visitor2?.VisitUsingStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = AstVisitAction.Continue;
            var visitor2 = visitor as AstVisitor2;
            if (visitor2 != null)
            {
                action = visitor2.VisitUsingStatement(this);
                if (action != AstVisitAction.Continue)
                    return visitor.CheckForPostAction(this, action);
            }

            if (Name != null)
                action = Name.InternalVisit(visitor);

            if (action != AstVisitAction.Continue)
                return visitor.CheckForPostAction(this, action);

            if (ModuleSpecification != null)
                action = ModuleSpecification.InternalVisit(visitor);

            if (action != AstVisitAction.Continue)
                return visitor.CheckForPostAction(this, action);

            if (Alias != null)
                action = Alias.InternalVisit(visitor);

            return visitor.CheckForPostAction(this, action);
        }

        #endregion

        /// <summary>
        /// Define imported module and all type definitions imported by this using statement.
        /// </summary>
        /// <param name="moduleInfo"></param>
        /// <returns>Return ExportedTypeTable for this module.</returns>
        internal ReadOnlyDictionary<string, TypeDefinitionAst> DefineImportedModule(PSModuleInfo moduleInfo)
        {
            var types = moduleInfo.GetExportedTypeDefinitions();
            ModuleInfo = moduleInfo;
            return types;
        }

        /// <summary>
        /// Is UsingStatementKind Module or Assembly.
        /// </summary>
        /// <returns>True, if it is.</returns>
        internal bool IsUsingModuleOrAssembly()
        {
            return UsingStatementKind == UsingStatementKind.Assembly || UsingStatementKind == UsingStatementKind.Module;
        }
    }

    /// <summary>
    /// An abstract base class for type members.
    /// </summary>
    public abstract class MemberAst : Ast
    {
        /// <summary>
        /// Initialize the common fields of a type member.
        /// </summary>
        /// <param name="extent">The extent of the type member.</param>
        protected MemberAst(IScriptExtent extent) : base(extent)
        {
        }

        /// <summary>
        /// The name of the member.  This property is never null.
        /// </summary>
        public abstract string Name { get; }

        internal abstract string GetTooltip();
    }

    /// <summary>
    /// The attributes for a property.
    /// </summary>
    [Flags]
    public enum PropertyAttributes
    {
        /// <summary>No attributes specified.</summary>
        None = 0x00,

        /// <summary>The property is public.</summary>
        Public = 0x01,

        /// <summary>The property is private.</summary>
        Private = 0x02,

        /// <summary>The property is static.</summary>
        Static = 0x10,

        /// <summary>The property is a literal.</summary>
        Literal = 0x20,

        /// <summary>The property is a hidden.</summary>
        Hidden = 0x40,
    }

    /// <summary>
    /// The ast for a property.
    /// </summary>
    public class PropertyMemberAst : MemberAst
    {
        private static readonly ReadOnlyCollection<AttributeAst> s_emptyAttributeList =
            Utils.EmptyReadOnlyCollection<AttributeAst>();

        /// <summary>
        /// Construct a property member.
        /// </summary>
        /// <param name="extent">The extent of the property starting with any custom attributes.</param>
        /// <param name="name">The name of the property.</param>
        /// <param name="propertyType">The ast for the type of the property - may be null.</param>
        /// <param name="attributes">The custom attributes for the property.</param>
        /// <param name="propertyAttributes">The attributes (like public or static) for the property.</param>
        /// <param name="initialValue">The initial value of the property (may be null).</param>
        public PropertyMemberAst(IScriptExtent extent, string name, TypeConstraintAst propertyType, IEnumerable<AttributeAst> attributes, PropertyAttributes propertyAttributes, ExpressionAst initialValue)
            : base(extent)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            if ((propertyAttributes & (PropertyAttributes.Private | PropertyAttributes.Public)) ==
                (PropertyAttributes.Private | PropertyAttributes.Public))
            {
                throw PSTraceSource.NewArgumentException(nameof(propertyAttributes));
            }

            Name = name;
            if (propertyType != null)
            {
                PropertyType = propertyType;
                SetParent(PropertyType);
            }

            if (attributes != null)
            {
                this.Attributes = new ReadOnlyCollection<AttributeAst>(attributes.ToArray());
                SetParents(Attributes);
            }
            else
            {
                this.Attributes = s_emptyAttributeList;
            }

            PropertyAttributes = propertyAttributes;
            InitialValue = initialValue;

            if (InitialValue != null)
            {
                SetParent(InitialValue);
            }
        }

        /// <summary>
        /// The name of the property.
        /// </summary>
        public override string Name { get; }

        /// <summary>
        /// The ast for the type of the property.  This property may be null if no type was specified.
        /// </summary>
        public TypeConstraintAst PropertyType { get; }

        /// <summary>
        /// The custom attributes of the property.  This property is never null.
        /// </summary>
        public ReadOnlyCollection<AttributeAst> Attributes { get; }

        /// <summary>
        /// The attributes (like public or static) of the property.
        /// </summary>
        public PropertyAttributes PropertyAttributes { get; }

        /// <summary>
        /// The ast for the initial value of the property.  This property may be null if no initial value was specified.
        /// </summary>
        public ExpressionAst InitialValue { get; }

        /// <summary>
        /// Return true if the property is public.
        /// </summary>
        public bool IsPublic { get { return (PropertyAttributes & PropertyAttributes.Public) != 0; } }

        /// <summary>
        /// Return true if the property is private.
        /// </summary>
        public bool IsPrivate { get { return (PropertyAttributes & PropertyAttributes.Private) != 0; } }

        /// <summary>
        /// Return true if the property is hidden.
        /// </summary>
        public bool IsHidden { get { return (PropertyAttributes & PropertyAttributes.Hidden) != 0; } }

        /// <summary>
        /// Return true if the property is static.
        /// </summary>
        public bool IsStatic { get { return (PropertyAttributes & PropertyAttributes.Static) != 0; } }

        /// <summary>
        /// Copy the PropertyMemberAst.
        /// </summary>
        public override Ast Copy()
        {
            var newPropertyType = CopyElement(PropertyType);
            var newAttributes = CopyElements(Attributes);
            var newInitialValue = CopyElement(InitialValue);
            return new PropertyMemberAst(Extent, Name, newPropertyType, newAttributes, PropertyAttributes, newInitialValue);
        }

        internal override string GetTooltip()
        {
            var type = PropertyType != null ? PropertyType.TypeName.FullName : "object";
            return IsStatic
                ? "static " + type + " " + Name
                : type + " " + Name;
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            var visitor2 = visitor as ICustomAstVisitor2;
            return visitor2?.VisitPropertyMember(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = AstVisitAction.Continue;
            var visitor2 = visitor as AstVisitor2;
            if (visitor2 != null)
            {
                action = visitor2.VisitPropertyMember(this);
                if (action == AstVisitAction.SkipChildren)
                    return visitor.CheckForPostAction(this, AstVisitAction.Continue);

                if (action == AstVisitAction.Continue && PropertyType != null)
                    action = PropertyType.InternalVisit(visitor);

                if (action == AstVisitAction.Continue)
                {
                    for (int index = 0; index < Attributes.Count; index++)
                    {
                        var attributeAst = Attributes[index];
                        action = attributeAst.InternalVisit(visitor);
                        if (action != AstVisitAction.Continue) break;
                    }
                }

                if (action == AstVisitAction.Continue && InitialValue != null)
                    action = InitialValue.InternalVisit(visitor);
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// Flags for a method.
    /// </summary>
    [Flags]
    public enum MethodAttributes
    {
        /// <summary>No flags specified.</summary>
        None = 0x00,

        /// <summary>The method is public.</summary>
        Public = 0x01,

        /// <summary>The method is private.</summary>
        Private = 0x02,

        /// <summary>The method is static.</summary>
        Static = 0x10,

        /// <summary>The property is a hidden.</summary>
        Hidden = 0x40,
    }

    /// <summary>
    /// The ast for a method.
    /// </summary>
    public class FunctionMemberAst : MemberAst, IParameterMetadataProvider
    {
        private static readonly ReadOnlyCollection<AttributeAst> s_emptyAttributeList =
            Utils.EmptyReadOnlyCollection<AttributeAst>();

        private static readonly ReadOnlyCollection<ParameterAst> s_emptyParameterList =
            Utils.EmptyReadOnlyCollection<ParameterAst>();

        private readonly FunctionDefinitionAst _functionDefinitionAst;

        /// <summary>
        /// Construct a member function.
        /// </summary>
        /// <param name="extent">The extent of the method starting from any attributes to the closing curly.</param>
        /// <param name="functionDefinitionAst">The main body of the method.</param>
        /// <param name="returnType">The return type of the method, may be null.</param>
        /// <param name="attributes">The custom attributes for the function.</param>
        /// <param name="methodAttributes">The method attributes like public or static.</param>
        public FunctionMemberAst(IScriptExtent extent, FunctionDefinitionAst functionDefinitionAst, TypeConstraintAst returnType, IEnumerable<AttributeAst> attributes, MethodAttributes methodAttributes)
            : base(extent)
        {
            if (functionDefinitionAst == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(functionDefinitionAst));
            }

            if ((methodAttributes & (MethodAttributes.Private | MethodAttributes.Public)) ==
                (MethodAttributes.Private | MethodAttributes.Public))
            {
                throw PSTraceSource.NewArgumentException(nameof(methodAttributes));
            }

            if (returnType != null)
            {
                ReturnType = returnType;
                SetParent(returnType);
            }

            if (attributes != null)
            {
                this.Attributes = new ReadOnlyCollection<AttributeAst>(attributes.ToArray());
                SetParents(Attributes);
            }
            else
            {
                this.Attributes = s_emptyAttributeList;
            }

            _functionDefinitionAst = functionDefinitionAst;
            SetParent(functionDefinitionAst);
            MethodAttributes = methodAttributes;
        }

        /// <summary>
        /// The name of the method.  This property is never null.
        /// </summary>
        public override string Name { get { return _functionDefinitionAst.Name; } }

        /// <summary>
        /// The attributes specified on the method.  This property is never null.
        /// </summary>
        public ReadOnlyCollection<AttributeAst> Attributes { get; }

        /// <summary>
        /// The ast representing the return type for the method.  This property may be null if no return type was specified.
        /// </summary>
        public TypeConstraintAst ReturnType { get; }

        /// <summary>
        /// The parameters specified immediately after the function name.  This property is never null.
        /// </summary>
        public ReadOnlyCollection<ParameterAst> Parameters
        {
            get { return _functionDefinitionAst.Parameters ?? s_emptyParameterList; }
        }

        /// <summary>
        /// The body of the function.  This property is never null.
        /// </summary>
        public ScriptBlockAst Body { get { return _functionDefinitionAst.Body; } }

        /// <summary>
        /// Method attribute flags.
        /// </summary>
        public MethodAttributes MethodAttributes { get; }

        /// <summary>
        /// Returns true if the method is public.
        /// </summary>
        public bool IsPublic { get { return (MethodAttributes & MethodAttributes.Public) != 0; } }

        /// <summary>
        /// Returns true if the method is public.
        /// </summary>
        public bool IsPrivate { get { return (MethodAttributes & MethodAttributes.Private) != 0; } }

        /// <summary>
        /// Returns true if the method is hidden.
        /// </summary>
        public bool IsHidden { get { return (MethodAttributes & MethodAttributes.Hidden) != 0; } }

        /// <summary>
        /// Returns true if the method is static.
        /// </summary>
        public bool IsStatic { get { return (MethodAttributes & MethodAttributes.Static) != 0; } }

        /// <summary>
        /// Returns true if the method is a constructor.
        /// </summary>
        public bool IsConstructor
        {
            get { return Name.Equals(((TypeDefinitionAst)Parent).Name, StringComparison.OrdinalIgnoreCase); }
        }

        internal IScriptExtent NameExtent { get { return _functionDefinitionAst.NameExtent; } }

        /// <summary>
        /// Copy a function member ast.
        /// </summary>
        public override Ast Copy()
        {
            var newDefn = CopyElement(_functionDefinitionAst);
            var newReturnType = CopyElement(ReturnType);
            var newAttributes = CopyElements(Attributes);

            return new FunctionMemberAst(Extent, newDefn, newReturnType, newAttributes, MethodAttributes);
        }

        internal override string GetTooltip()
        {
            var sb = new StringBuilder();
            if (IsStatic)
            {
                sb.Append("static ");
            }

            sb.Append(IsReturnTypeVoid() ? "void" : ReturnType.TypeName.FullName);
            sb.Append(' ');
            sb.Append(Name);
            sb.Append('(');
            for (int i = 0; i < Parameters.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(Parameters[i].GetTooltip());
            }

            sb.Append(')');
            return sb.ToString();
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            var visitor2 = visitor as ICustomAstVisitor2;
            return visitor2?.VisitFunctionMember(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = AstVisitAction.Continue;
            var visitor2 = visitor as AstVisitor2;
            if (visitor2 != null)
            {
                action = visitor2.VisitFunctionMember(this);
                if (action == AstVisitAction.SkipChildren)
                    return visitor.CheckForPostAction(this, AstVisitAction.Continue);
                if (action == AstVisitAction.Continue)
                {
                    for (int index = 0; index < Attributes.Count; index++)
                    {
                        var attributeAst = Attributes[index];
                        action = attributeAst.InternalVisit(visitor);
                        if (action != AstVisitAction.Continue) break;
                    }
                }

                if (action == AstVisitAction.Continue && ReturnType != null)
                    action = ReturnType.InternalVisit(visitor);

                if (action == AstVisitAction.Continue)
                    action = _functionDefinitionAst.InternalVisit(visitor);
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        #region IParameterMetadataProvider implementation

        bool IParameterMetadataProvider.HasAnyScriptBlockAttributes()
        {
            return ((IParameterMetadataProvider)_functionDefinitionAst).HasAnyScriptBlockAttributes();
        }

        ReadOnlyCollection<ParameterAst> IParameterMetadataProvider.Parameters
        {
            get { return ((IParameterMetadataProvider)_functionDefinitionAst).Parameters; }
        }

        RuntimeDefinedParameterDictionary IParameterMetadataProvider.GetParameterMetadata(bool automaticPositions, ref bool usesCmdletBinding)
        {
            return ((IParameterMetadataProvider)_functionDefinitionAst).GetParameterMetadata(automaticPositions, ref usesCmdletBinding);
        }

        IEnumerable<Attribute> IParameterMetadataProvider.GetScriptBlockAttributes()
        {
            return ((IParameterMetadataProvider)_functionDefinitionAst).GetScriptBlockAttributes();
        }

        IEnumerable<ExperimentalAttribute> IParameterMetadataProvider.GetExperimentalAttributes()
        {
            return ((IParameterMetadataProvider)_functionDefinitionAst).GetExperimentalAttributes();
        }

        bool IParameterMetadataProvider.UsesCmdletBinding()
        {
            return ((IParameterMetadataProvider)_functionDefinitionAst).UsesCmdletBinding();
        }

        PowerShell IParameterMetadataProvider.GetPowerShell(ExecutionContext context, Dictionary<string, object> variables, bool isTrustedInput,
            bool filterNonUsingVariables, bool? createLocalScope, params object[] args)
        {
            // OK?  I think this isn't reachable
            throw new NotSupportedException();
        }

        string IParameterMetadataProvider.GetWithInputHandlingForInvokeCommand()
        {
            // OK?  I think this isn't reachable
            throw new NotSupportedException();
        }

        Tuple<string, string> IParameterMetadataProvider.GetWithInputHandlingForInvokeCommandWithUsingExpression(Tuple<List<VariableExpressionAst>, string> usingVariablesTuple)
        {
            throw new NotImplementedException();
        }

        #endregion IParameterMetadataProvider implementation

        #region Internal helpers
        internal bool IsReturnTypeVoid()
        {
            if (ReturnType == null)
                return true;
            var typeName = ReturnType.TypeName as TypeName;
            return typeName != null && typeName.IsType(typeof(void));
        }

        internal Type GetReturnType()
        {
            return ReturnType == null ? typeof(void) : ReturnType.TypeName.GetReflectionType();
        }
        #endregion
    }

    internal enum SpecialMemberFunctionType
    {
        None,
        DefaultConstructor,
        StaticConstructor,
    }

    internal class CompilerGeneratedMemberFunctionAst : MemberAst, IParameterMetadataProvider
    {
        internal CompilerGeneratedMemberFunctionAst(IScriptExtent extent, TypeDefinitionAst definingType, SpecialMemberFunctionType type)
            : base(extent)
        {
            StatementAst statement = null;
            if (type == SpecialMemberFunctionType.DefaultConstructor)
            {
                var invokeMemberAst = new BaseCtorInvokeMemberExpressionAst(extent, extent, Array.Empty<ExpressionAst>());
                statement = new CommandExpressionAst(extent, invokeMemberAst, null);
            }

            Body = new ScriptBlockAst(extent, null, new StatementBlockAst(extent, statement == null ? null : new[] { statement }, null), false);
            this.SetParent(Body);
            definingType.SetParent(this);
            DefiningType = definingType;
            Type = type;
        }

        public override string Name
        {
            // This is fine for now, but if we add non-constructors, the name will be wrong.
            get { return DefiningType.Name; }
        }

        internal TypeDefinitionAst DefiningType { get; }

        internal SpecialMemberFunctionType Type { get; }

        internal override string GetTooltip()
        {
            return DefiningType.Name + " new()";
        }

        public override Ast Copy()
        {
            return new CompilerGeneratedMemberFunctionAst(Extent, (TypeDefinitionAst)DefiningType.Copy(), Type);
        }

        internal override object Accept(ICustomAstVisitor visitor)
        {
            Diagnostics.Assert(false, "code should be unreachable");
            return null;
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            Diagnostics.Assert(false, "code should be unreachable");
            return AstVisitAction.Continue;
        }

        public bool HasAnyScriptBlockAttributes()
        {
            return ((IParameterMetadataProvider)Body).HasAnyScriptBlockAttributes();
        }

        public RuntimeDefinedParameterDictionary GetParameterMetadata(bool automaticPositions, ref bool usesCmdletBinding)
        {
            return new RuntimeDefinedParameterDictionary { Data = RuntimeDefinedParameterDictionary.EmptyParameterArray };
        }

        public IEnumerable<Attribute> GetScriptBlockAttributes()
        {
            return ((IParameterMetadataProvider)Body).GetScriptBlockAttributes();
        }

        public IEnumerable<ExperimentalAttribute> GetExperimentalAttributes()
        {
            return ((IParameterMetadataProvider)Body).GetExperimentalAttributes();
        }

        public bool UsesCmdletBinding()
        {
            return false;
        }

        public ReadOnlyCollection<ParameterAst> Parameters { get { return null; } }

        public ScriptBlockAst Body { get; }

        public PowerShell GetPowerShell(ExecutionContext context, Dictionary<string, object> variables, bool isTrustedInput,
            bool filterNonUsingVariables, bool? createLocalScope, params object[] args)
        {
            Diagnostics.Assert(false, "code should be unreachable");
            return null;
        }

        public string GetWithInputHandlingForInvokeCommand()
        {
            Diagnostics.Assert(false, "code should be unreachable");
            return null;
        }

        public Tuple<string, string> GetWithInputHandlingForInvokeCommandWithUsingExpression(Tuple<List<VariableExpressionAst>, string> usingVariablesTuple)
        {
            Diagnostics.Assert(false, "code should be unreachable");
            return null;
        }
    }

    /// <summary>
    /// The ast that represents a function or filter definition.  The function is always named.
    /// </summary>
    public class FunctionDefinitionAst : StatementAst, IParameterMetadataProvider
    {
        /// <summary>
        /// Construct a function definition.
        /// </summary>
        /// <param name="extent">
        /// The extent of the function definition, starting with the function or filter keyword, ending at the closing curly.
        /// </param>
        /// <param name="isFilter">True if the filter keyword was used.</param>
        /// <param name="isWorkflow">True if the workflow keyword was used.</param>
        /// <param name="name">The name of the function.</param>
        /// <param name="parameters">
        /// The parameters specified after the function name.  This does not include parameters specified with a param statement.
        /// </param>
        /// <param name="body">The body of the function/filter.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/>, <paramref name="name"/>, or <paramref name="body"/> is null, or
        /// if <paramref name="name"/> is an empty string.
        /// </exception>
        public FunctionDefinitionAst(IScriptExtent extent,
                                     bool isFilter,
                                     bool isWorkflow,
                                     string name,
                                     IEnumerable<ParameterAst> parameters,
                                     ScriptBlockAst body)
            : base(extent)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            if (body == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(body));
            }

            if (isFilter && isWorkflow)
            {
                throw PSTraceSource.NewArgumentException(nameof(isFilter));
            }

            this.IsFilter = isFilter;
            this.IsWorkflow = isWorkflow;

            this.Name = name;
            if (parameters != null && parameters.Any())
            {
                this.Parameters = new ReadOnlyCollection<ParameterAst>(parameters.ToArray());
                SetParents(Parameters);
            }

            this.Body = body;
            SetParent(body);
        }

        internal FunctionDefinitionAst(IScriptExtent extent,
                                       bool isFilter,
                                       bool isWorkflow,
                                       Token functionNameToken,
                                       IEnumerable<ParameterAst> parameters,
                                       ScriptBlockAst body)
            : this(extent,
                   isFilter,
                   isWorkflow,
                   (functionNameToken.Kind == TokenKind.Generic) ? ((StringToken)functionNameToken).Value : functionNameToken.Text,
                   parameters,
                   body)
        {
            NameExtent = functionNameToken.Extent;
        }

        /// <summary>
        /// If true, the filter keyword was used.
        /// </summary>
        public bool IsFilter { get; }

        /// <summary>
        /// If true, the workflow keyword was used.
        /// </summary>
        public bool IsWorkflow { get; }

        /// <summary>
        /// The name of the function or filter.  This property is never null or empty.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The parameters specified immediately after the function name, or null if no parameters were specified.
        /// <para>It is possible that this property may have a value and <see cref="ScriptBlockAst.ParamBlock"/> to also have a
        /// value.  Normally this is not allowed in a valid script, but in one rare case it is allowed:</para>
        /// <c>function foo() { param($a) }</c>
        /// <para>
        /// In this example, the parameters specified after the function name must be empty or the script is not valid.
        /// </para>
        /// </summary>
        public ReadOnlyCollection<ParameterAst> Parameters { get; }

        /// <summary>
        /// The body of the function.  This property is never null.
        /// </summary>
        public ScriptBlockAst Body { get; }

        internal IScriptExtent NameExtent { get; private set; }

        /// <summary>
        /// Return the help content, if any, for the function.
        /// </summary>
        public CommentHelpInfo GetHelpContent()
        {
            Dictionary<Ast, Token[]> scriptBlockTokenCache = new Dictionary<Ast, Token[]>();
            var commentTokens = HelpCommentsParser.GetHelpCommentTokens(this, scriptBlockTokenCache);
            if (commentTokens != null)
            {
                return HelpCommentsParser.GetHelpContents(commentTokens.Item1, commentTokens.Item2);
            }

            return null;
        }

        /// <summary>
        /// Return the help content, if any, for the function.
        /// Use this overload when parsing multiple functions within a single scope.
        /// </summary>
        /// <param name="scriptBlockTokenCache">A dictionary that the parser will use to
        /// map AST nodes to their respective tokens. The parser uses this to improve performance
        /// while repeatedly parsing the parent script blocks of a function (since the parent
        /// script blocks may contain help comments related to this function.
        /// To conserve memory, clear / null-out this cache when done with repeated parsing.</param>
        /// <returns></returns>
        public CommentHelpInfo GetHelpContent(Dictionary<Ast, Token[]> scriptBlockTokenCache)
        {
            if (scriptBlockTokenCache == null)
            {
                throw new ArgumentNullException(nameof(scriptBlockTokenCache));
            }

            var commentTokens = HelpCommentsParser.GetHelpCommentTokens(this, scriptBlockTokenCache);
            if (commentTokens != null)
            {
                return HelpCommentsParser.GetHelpContents(commentTokens.Item1, commentTokens.Item2);
            }

            return null;
        }

        /// <summary>
        /// Copy the FunctionDefinitionAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newParameters = CopyElements(this.Parameters);
            var newBody = CopyElement(this.Body);

            return new FunctionDefinitionAst(this.Extent, this.IsFilter, this.IsWorkflow, this.Name, newParameters, newBody) { NameExtent = this.NameExtent };
        }

        internal string GetParamTextFromParameterList(Tuple<List<VariableExpressionAst>, string> usingVariablesTuple = null)
        {
            Diagnostics.Assert(Parameters != null, "Caller makes sure that Parameters is not null before calling this method.");

            string additionalNewUsingParams = null;
            IEnumerator<VariableExpressionAst> orderedUsingVars = null;
            if (usingVariablesTuple != null)
            {
                Diagnostics.Assert(
                    usingVariablesTuple.Item1 != null && usingVariablesTuple.Item1.Count > 0 && !string.IsNullOrEmpty(usingVariablesTuple.Item2),
                    "Caller makes sure the value passed in is not null or empty.");
                orderedUsingVars = usingVariablesTuple.Item1.OrderBy(static varAst => varAst.Extent.StartOffset).GetEnumerator();
                additionalNewUsingParams = usingVariablesTuple.Item2;
            }

            var sb = new StringBuilder("param(");
            string separator = string.Empty;

            if (additionalNewUsingParams != null)
            {
                // Add the $using variable parameters if necessary
                sb.Append(additionalNewUsingParams);
                separator = ", ";
            }

            for (int i = 0; i < Parameters.Count; i++)
            {
                var param = Parameters[i];
                sb.Append(separator);
                sb.Append(orderedUsingVars != null
                              ? param.GetParamTextWithDollarUsingHandling(orderedUsingVars)
                              : param.ToString());
                separator = ", ";
            }

            sb.Append(')');
            sb.Append(Environment.NewLine);

            return sb.ToString();
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitFunctionDefinition(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitFunctionDefinition(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
            {
                if (Parameters != null)
                {
                    for (int index = 0; index < Parameters.Count; index++)
                    {
                        var param = Parameters[index];
                        action = param.InternalVisit(visitor);
                        if (action != AstVisitAction.Continue) break;
                    }
                }

                if (action == AstVisitAction.Continue)
                    action = Body.InternalVisit(visitor);
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        #region IParameterMetadataProvider implementation

        bool IParameterMetadataProvider.HasAnyScriptBlockAttributes()
        {
            return ((IParameterMetadataProvider)Body).HasAnyScriptBlockAttributes();
        }

        RuntimeDefinedParameterDictionary IParameterMetadataProvider.GetParameterMetadata(bool automaticPositions, ref bool usesCmdletBinding)
        {
            if (Parameters != null)
            {
                return Compiler.GetParameterMetaData(Parameters, automaticPositions, ref usesCmdletBinding);
            }

            if (Body.ParamBlock != null)
            {
                return Compiler.GetParameterMetaData(Body.ParamBlock.Parameters, automaticPositions, ref usesCmdletBinding);
            }

            return new RuntimeDefinedParameterDictionary { Data = RuntimeDefinedParameterDictionary.EmptyParameterArray };
        }

        IEnumerable<Attribute> IParameterMetadataProvider.GetScriptBlockAttributes()
        {
            return ((IParameterMetadataProvider)Body).GetScriptBlockAttributes();
        }

        IEnumerable<ExperimentalAttribute> IParameterMetadataProvider.GetExperimentalAttributes()
        {
            return ((IParameterMetadataProvider)Body).GetExperimentalAttributes();
        }

        ReadOnlyCollection<ParameterAst> IParameterMetadataProvider.Parameters
        {
            get { return Parameters ?? (Body.ParamBlock?.Parameters); }
        }

        PowerShell IParameterMetadataProvider.GetPowerShell(ExecutionContext context, Dictionary<string, object> variables, bool isTrustedInput,
            bool filterNonUsingVariables, bool? createLocalScope, params object[] args)
        {
            ExecutionContext.CheckStackDepth();
            return ScriptBlockToPowerShellConverter.Convert(this.Body, this.Parameters, isTrustedInput, context, variables, filterNonUsingVariables, createLocalScope, args);
        }

        string IParameterMetadataProvider.GetWithInputHandlingForInvokeCommand()
        {
            string result = ((IParameterMetadataProvider)Body).GetWithInputHandlingForInvokeCommand();
            return Parameters == null ? result : (GetParamTextFromParameterList() + result);
        }

        Tuple<string, string> IParameterMetadataProvider.GetWithInputHandlingForInvokeCommandWithUsingExpression(
            Tuple<List<VariableExpressionAst>, string> usingVariablesTuple)
        {
            Tuple<string, string> result =
                ((IParameterMetadataProvider)Body).GetWithInputHandlingForInvokeCommandWithUsingExpression(usingVariablesTuple);

            if (Parameters == null)
            {
                return result;
            }

            string paramText = GetParamTextFromParameterList(usingVariablesTuple);
            return new Tuple<string, string>(paramText, result.Item2);
        }

        bool IParameterMetadataProvider.UsesCmdletBinding()
        {
            bool usesCmdletBinding = false;
            if (Parameters != null)
            {
                usesCmdletBinding = ParamBlockAst.UsesCmdletBinding(Parameters);
            }
            else if (Body.ParamBlock != null)
            {
                usesCmdletBinding = ((IParameterMetadataProvider)Body).UsesCmdletBinding();
            }

            return usesCmdletBinding;
        }

        #endregion IParameterMetadataProvider implementation
    }

    /// <summary>
    /// The ast that represents an if statement.
    /// </summary>
    public class IfStatementAst : StatementAst
    {
        /// <summary>
        /// Construct an if statement.
        /// </summary>
        /// <param name="extent">
        /// The extent of the statement, starting with the if keyword, ending at the closing curly of the last clause.
        /// </param>
        /// <param name="clauses">
        /// A non-empty collection of pairs of condition expressions and statement blocks.
        /// </param>
        /// <param name="elseClause">The else clause, or null if no clause was specified.</param>
        /// <exception cref="PSArgumentNullException">If <paramref name="extent"/> is null.</exception>
        /// <exception cref="PSArgumentException">If <paramref name="clauses"/> is null or empty.</exception>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public IfStatementAst(IScriptExtent extent, IEnumerable<IfClause> clauses, StatementBlockAst elseClause)
            : base(extent)
        {
            if (clauses == null || !clauses.Any())
            {
                throw PSTraceSource.NewArgumentException(nameof(clauses));
            }

            this.Clauses = new ReadOnlyCollection<IfClause>(clauses.ToArray());
            SetParents(Clauses);

            if (elseClause != null)
            {
                this.ElseClause = elseClause;
                SetParent(elseClause);
            }
        }

        /// <summary>
        /// The asts representing a pair of (condition,statements) that are tested, in sequence until the first condition
        /// tests true, in which case it's statements are executed, otherwise the <see cref="ElseClause"/>, if any, is
        /// executed.  This property is never null and always has at least 1 value.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public ReadOnlyCollection<IfClause> Clauses { get; }

        /// <summary>
        /// The ast for the else clause, or null if no else clause is specified.
        /// </summary>
        public StatementBlockAst ElseClause { get; }

        /// <summary>
        /// Copy the IfStatementAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newClauses = new List<IfClause>(this.Clauses.Count);
            for (int i = 0; i < this.Clauses.Count; i++)
            {
                var clause = this.Clauses[i];
                var newCondition = CopyElement(clause.Item1);
                var newStatementBlock = CopyElement(clause.Item2);

                newClauses.Add(Tuple.Create(newCondition, newStatementBlock));
            }

            var newElseClause = CopyElement(this.ElseClause);
            return new IfStatementAst(this.Extent, newClauses, newElseClause);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitIfStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitIfStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < Clauses.Count; index++)
                {
                    var ifClause = Clauses[index];
                    action = ifClause.Item1.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                    action = ifClause.Item2.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            if (action == AstVisitAction.Continue && ElseClause != null)
            {
                action = ElseClause.InternalVisit(visitor);
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast representing the data statement.
    /// </summary>
    public class DataStatementAst : StatementAst
    {
        private static readonly ExpressionAst[] s_emptyCommandsAllowed = Array.Empty<ExpressionAst>();

        /// <summary>
        /// Construct a data statement.
        /// </summary>
        /// <param name="extent">The extent of the data statement, extending from the data keyword to the closing curly brace.</param>
        /// <param name="variableName">The name of the variable, if specified, otherwise null.</param>
        /// <param name="commandsAllowed">The list of commands allowed in the data statement, if specified, otherwise null.</param>
        /// <param name="body">The body of the data statement.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="body"/> is null.
        /// </exception>
        public DataStatementAst(IScriptExtent extent,
                                string variableName,
                                IEnumerable<ExpressionAst> commandsAllowed,
                                StatementBlockAst body)
            : base(extent)
        {
            if (body == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(body));
            }

            if (string.IsNullOrWhiteSpace(variableName))
            {
                variableName = null;
            }

            this.Variable = variableName;
            if (commandsAllowed != null && commandsAllowed.Any())
            {
                this.CommandsAllowed = new ReadOnlyCollection<ExpressionAst>(commandsAllowed.ToArray());
                SetParents(CommandsAllowed);
                this.HasNonConstantAllowedCommand = CommandsAllowed.Any(static ast => ast is not StringConstantExpressionAst);
            }
            else
            {
                this.CommandsAllowed = new ReadOnlyCollection<ExpressionAst>(s_emptyCommandsAllowed);
            }

            this.Body = body;
            SetParent(body);
        }

        /// <summary>
        /// The name of the variable this data statement sets, or null if no variable name was specified.
        /// </summary>
        public string Variable { get; }

        /// <summary>
        /// The asts naming the commands allowed to execute in this data statement.
        /// </summary>
        public ReadOnlyCollection<ExpressionAst> CommandsAllowed { get; }

        /// <summary>
        /// The ast for the body of the data statement.  This property is never null.
        /// </summary>
        public StatementBlockAst Body { get; }

        /// <summary>
        /// Copy the DataStatementAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newCommandsAllowed = CopyElements(this.CommandsAllowed);
            var newBody = CopyElement(this.Body);
            return new DataStatementAst(this.Extent, this.Variable, newCommandsAllowed, newBody);
        }

        internal bool HasNonConstantAllowedCommand { get; }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitDataStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitDataStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Body.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        #region Code Generation Details

        internal int TupleIndex { get; set; } = VariableAnalysis.Unanalyzed;

        #endregion Code Generation Details
    }

    #region Looping Statements

    /// <summary>
    /// An abstract base class for statements that have labels such as a while statement or a switch statement.
    /// </summary>
    public abstract class LabeledStatementAst : StatementAst
    {
        /// <summary>
        /// Initialize the properties common to labeled statements.
        /// </summary>
        /// <param name="extent">The extent of the statement.</param>
        /// <param name="label">The optionally null label for the statement.</param>
        /// <param name="condition">The optionally null pipeline for the condition test of the statement.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        protected LabeledStatementAst(IScriptExtent extent, string label, PipelineBaseAst condition)
            : base(extent)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                label = null;
            }

            this.Label = label;
            if (condition != null)
            {
                this.Condition = condition;
                SetParent(condition);
            }
        }

        /// <summary>
        /// The label name if specified, otherwise null.
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// The ast for the condition that is tested on each iteration of the loop, or the condition tested on a switch.
        /// This property may be null if the statement is a <see cref="ForStatementAst"/>, otherwise it is never null.
        /// </summary>
        public PipelineBaseAst Condition { get; }
    }

    /// <summary>
    /// An abstract base class for looping statements including a the do/while statement, the do/until statement,
    /// the foreach statement, the for statement, and the while statement.
    /// </summary>
    public abstract class LoopStatementAst : LabeledStatementAst
    {
        /// <summary>
        /// Initialize the properties common to all loop statements.
        /// </summary>
        /// <param name="extent">The extent of the statement.</param>
        /// <param name="label">The optionally null label for the statement.</param>
        /// <param name="condition">The optionally null pipeline for the condition test of the statement.</param>
        /// <param name="body">The body of the statement.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="body"/> is null.
        /// </exception>
        protected LoopStatementAst(IScriptExtent extent, string label, PipelineBaseAst condition, StatementBlockAst body)
            : base(extent, label, condition)
        {
            if (body == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(body));
            }

            this.Body = body;
            SetParent(body);
        }

        /// <summary>
        /// The body of a loop statement.  This property is never null.
        /// </summary>
        public StatementBlockAst Body { get; }
    }

    /// <summary>
    /// Flags that are specified on a foreach statement.  Values may be or'ed together, not all invalid combinations
    /// of flags are detected.
    /// </summary>
    [Flags]
    public enum ForEachFlags
    {
        /// <summary>
        /// No flags specified.
        /// </summary>
        None = 0x00,

        /// <summary>
        /// The -parallel flag.
        /// </summary>
        Parallel = 0x01,

        // If any flags are added that impact evaluation of items during the foreach statement, then
        // a binder (and caching strategy) needs to be added similar to SwitchClauseEvalBinder.
    }

    /// <summary>
    /// The ast representing the foreach statement.
    /// </summary>
    public class ForEachStatementAst : LoopStatementAst
    {
        /// <summary>
        /// Construct a foreach statement.
        /// </summary>
        /// <param name="extent">
        /// The extent of the statement, starting from the optional label or the foreach keyword and ending at the closing curly brace.
        /// </param>
        /// <param name="label">The optionally null label.</param>
        /// <param name="flags">Any flags that affect how the foreach statement is processed.</param>
        /// <param name="variable">The variable set on each iteration of the loop.</param>
        /// <param name="expression">The pipeline generating values to iterate through.</param>
        /// <param name="body">The body to execute for each element written from pipeline.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/>, <paramref name="expression"/>, or <paramref name="variable"/> is null.
        /// </exception>
        public ForEachStatementAst(IScriptExtent extent,
                                   string label,
                                   ForEachFlags flags,
                                   VariableExpressionAst variable,
                                   PipelineBaseAst expression,
                                   StatementBlockAst body)
            : base(extent, label, expression, body)
        {
            if (expression == null || variable == null)
            {
                throw PSTraceSource.NewArgumentNullException(expression == null ? "expression" : "variablePath");
            }

            this.Flags = flags;
            this.Variable = variable;
            SetParent(variable);
        }

        /// <summary>
        /// Construct a foreach statement.
        /// </summary>
        /// <param name="extent">
        /// The extent of the statement, starting from the optional label or the foreach keyword and ending at the closing curly brace.
        /// </param>
        /// <param name="label">The optionally null label.</param>
        /// <param name="flags">Any flags that affect how the foreach statement is processed.</param>
        /// <param name="throttleLimit">The limit to be obeyed during parallel processing, if any.</param>
        /// <param name="variable">The variable set on each iteration of the loop.</param>
        /// <param name="expression">The pipeline generating values to iterate through.</param>
        /// <param name="body">The body to execute for each element written from pipeline.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/>, <paramref name="expression"/>, or <paramref name="variable"/> is null.
        /// </exception>
        public ForEachStatementAst(IScriptExtent extent,
                                   string label,
                                   ForEachFlags flags,
                                   ExpressionAst throttleLimit,
                                   VariableExpressionAst variable,
                                   PipelineBaseAst expression,
                                   StatementBlockAst body)
            : this(extent, label, flags, variable, expression, body)
        {
            this.ThrottleLimit = throttleLimit;
            if (throttleLimit != null)
            {
                SetParent(throttleLimit);
            }
        }

        /// <summary>
        /// The name of the variable set for each item as the loop iterates.  This property is never null.
        /// </summary>
        public VariableExpressionAst Variable { get; }

        /// <summary>
        /// The limit to be obeyed during parallel processing, if any.
        /// </summary>
        public ExpressionAst ThrottleLimit { get; }

        /// <summary>
        /// The flags, if any specified on the foreach statement.
        /// </summary>
        public ForEachFlags Flags { get; }

        /// <summary>
        /// Copy the ForEachStatementAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newVariable = CopyElement(this.Variable);
            var newExpression = CopyElement(this.Condition);
            var newBody = CopyElement(this.Body);

            if (this.ThrottleLimit != null)
            {
                var newThrottleLimit = CopyElement(this.ThrottleLimit);
                return new ForEachStatementAst(this.Extent, this.Label, this.Flags, newThrottleLimit,
                                               newVariable, newExpression, newBody);
            }

            return new ForEachStatementAst(this.Extent, this.Label, this.Flags, newVariable, newExpression, newBody);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitForEachStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitForEachStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Variable.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Condition.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Body.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast for a for statement.
    /// </summary>
    public class ForStatementAst : LoopStatementAst
    {
        /// <summary>
        /// Construct a for statement.
        /// </summary>
        /// <param name="extent">The extent of the statement, from the label or for keyword to the closing curly.</param>
        /// <param name="label">The optionally null label.</param>
        /// <param name="initializer">The optionally null initialization expression executed before the loop.</param>
        /// <param name="condition">The optionally null condition expression tested on each iteration of the loop.</param>
        /// <param name="iterator">The optionally null iteration expression executed after each iteration of the loop.</param>
        /// <param name="body">The body executed on each iteration of the loop.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        public ForStatementAst(IScriptExtent extent,
                               string label,
                               PipelineBaseAst initializer,
                               PipelineBaseAst condition,
                               PipelineBaseAst iterator,
                               StatementBlockAst body)
            : base(extent, label, condition, body)
        {
            if (initializer != null)
            {
                this.Initializer = initializer;
                SetParent(initializer);
            }

            if (iterator != null)
            {
                this.Iterator = iterator;
                SetParent(iterator);
            }
        }

        /// <summary>
        /// The ast for the initialization expression of a for statement, or null if none was specified.
        /// </summary>
        public PipelineBaseAst Initializer { get; }

        /// <summary>
        /// The ast for the iteration expression of a for statement, or null if none was specified.
        /// </summary>
        public PipelineBaseAst Iterator { get; }

        /// <summary>
        /// Copy the ForStatementAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newInitializer = CopyElement(this.Initializer);
            var newCondition = CopyElement(this.Condition);
            var newIterator = CopyElement(this.Iterator);
            var newBody = CopyElement(this.Body);

            return new ForStatementAst(this.Extent, this.Label, newInitializer, newCondition, newIterator, newBody);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitForStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitForStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue && Initializer != null)
                action = Initializer.InternalVisit(visitor);
            if (action == AstVisitAction.Continue && Condition != null)
                action = Condition.InternalVisit(visitor);
            if (action == AstVisitAction.Continue && Iterator != null)
                action = Iterator.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Body.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast that represents the do/while statement.
    /// </summary>
    public class DoWhileStatementAst : LoopStatementAst
    {
        /// <summary>
        /// Construct a do/while statement.
        /// </summary>
        /// <param name="extent">The extent of the do/while statment from the label or do keyword to the closing curly brace.</param>
        /// <param name="label">The optionally null label.</param>
        /// <param name="condition">The condition tested on each iteration of the loop.</param>
        /// <param name="body">The body executed on each iteration of the loop.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="condition"/> is null.
        /// </exception>
        public DoWhileStatementAst(IScriptExtent extent, string label, PipelineBaseAst condition, StatementBlockAst body)
            : base(extent, label, condition, body)
        {
            if (condition == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(condition));
            }
        }

        /// <summary>
        /// Copy the DoWhileStatementAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newCondition = CopyElement(this.Condition);
            var newBody = CopyElement(this.Body);
            return new DoWhileStatementAst(this.Extent, this.Label, newCondition, newBody);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitDoWhileStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitDoWhileStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Condition.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Body.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast that represents a do/until statement.
    /// </summary>
    public class DoUntilStatementAst : LoopStatementAst
    {
        /// <summary>
        /// Construct a do/until statement.
        /// </summary>
        /// <param name="extent">The extent of the statement, from the label or do keyword to the closing curly brace.</param>
        /// <param name="label">The optionally null label.</param>
        /// <param name="condition">The condition tested on each iteration of the loop.</param>
        /// <param name="body">The body executed on each iteration of the loop.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="condition"/> is null.
        /// </exception>
        public DoUntilStatementAst(IScriptExtent extent, string label, PipelineBaseAst condition, StatementBlockAst body)
            : base(extent, label, condition, body)
        {
            if (condition == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(condition));
            }
        }

        /// <summary>
        /// Copy the DoUntilStatementAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newCondition = CopyElement(this.Condition);
            var newBody = CopyElement(this.Body);
            return new DoUntilStatementAst(this.Extent, this.Label, newCondition, newBody);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitDoUntilStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitDoUntilStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Condition.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Body.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast for a while statement.
    /// </summary>
    public class WhileStatementAst : LoopStatementAst
    {
        /// <summary>
        /// Construct a while statement.
        /// </summary>
        /// <param name="extent">The extent of the statement, from the label or while keyword to the closing curly brace.</param>
        /// <param name="label">The optionally null label.</param>
        /// <param name="condition">The condition tested on each iteration of the loop.</param>
        /// <param name="body">The body executed on each iteration of the loop.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="condition"/> is null.
        /// </exception>
        public WhileStatementAst(IScriptExtent extent, string label, PipelineBaseAst condition, StatementBlockAst body)
            : base(extent, label, condition, body)
        {
            if (condition == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(condition));
            }
        }

        /// <summary>
        /// Copy the WhileStatementAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newCondition = CopyElement(this.Condition);
            var newBody = CopyElement(this.Body);
            return new WhileStatementAst(this.Extent, this.Label, newCondition, newBody);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitWhileStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitWhileStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Condition.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Body.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// Flags that are specified on a switch statement.  Values may be or'ed together, not all invalid combinations
    /// of flags are detected.
    /// </summary>
    [Flags]
    public enum SwitchFlags
    {
        /// <summary>
        /// No flags specified.
        /// </summary>
        None = 0x00,

        /// <summary>
        /// The -file flag.
        /// </summary>
        File = 0x01,

        /// <summary>
        /// The -regex flag.
        /// </summary>
        Regex = 0x02,

        /// <summary>
        /// The -wildcard flag.
        /// </summary>
        Wildcard = 0x04,

        /// <summary>
        /// The -exact flag.
        /// </summary>
        Exact = 0x08,

        /// <summary>
        /// The -casesensitive flag.
        /// </summary>
        CaseSensitive = 0x10,

        /// <summary>
        /// The -parallel flag.
        /// </summary>
        Parallel = 0x20,

        // If any flags are added that influence evaluation of switch elements,
        // then the caching strategy in SwitchClauseEvalBinder needs to be updated,
        // and possibly its _binderCache.
    }

    /// <summary>
    /// The ast that represents a switch statement.
    /// </summary>
    public class SwitchStatementAst : LabeledStatementAst
    {
        private static readonly SwitchClause[] s_emptyClauseArray = Array.Empty<SwitchClause>();

        /// <summary>
        /// Construct a switch statement.
        /// </summary>
        /// <param name="extent">The extent of the statement, from the label or switch keyword to the closing curly.</param>
        /// <param name="label">The optionally null label.</param>
        /// <param name="condition">The expression being switched upon.</param>
        /// <param name="flags">Any flags that affect how the <paramref name="condition"/> is tested.</param>
        /// <param name="clauses">
        /// A possibly null or empty collection of conditions and block of statements to execute if the condition matches.
        /// </param>
        /// <param name="default">The default clause to execute if no clauses match.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// If <paramref name="default"/> and <paramref name="clauses"/> are both null or empty.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public SwitchStatementAst(IScriptExtent extent,
                                  string label,
                                  PipelineBaseAst condition,
                                  SwitchFlags flags,
                                  IEnumerable<SwitchClause> clauses,
                                  StatementBlockAst @default)
            : base(extent, label, condition)
        {
            if ((clauses == null || !clauses.Any()) && @default == null)
            {
                // Must specify either clauses or default.  If neither, just complain about clauses as that's the most likely
                // invalid argument.
                throw PSTraceSource.NewArgumentException(nameof(clauses));
            }

            this.Flags = flags;
            this.Clauses = new ReadOnlyCollection<SwitchClause>(
                (clauses != null && clauses.Any()) ? clauses.ToArray() : s_emptyClauseArray);
            SetParents(Clauses);
            if (@default != null)
            {
                this.Default = @default;
                SetParent(@default);
            }
        }

        /// <summary>
        /// The flags, if any specified on the switch statement.
        /// </summary>
        public SwitchFlags Flags { get; }

        /// <summary>
        /// A possibly empty collection of conditions and statement blocks representing the cases of the switch statement.
        /// If the collection is empty, the default clause is not null.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public ReadOnlyCollection<SwitchClause> Clauses { get; }

        /// <summary>
        /// The ast for the default of the switch statement, or null if no default block was specified.
        /// </summary>
        public StatementBlockAst Default { get; }

        /// <summary>
        /// Copy the SwitchStatementAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newCondition = CopyElement(this.Condition);
            var newDefault = CopyElement(this.Default);

            List<SwitchClause> newClauses = null;
            if (this.Clauses.Count > 0)
            {
                newClauses = new List<SwitchClause>(this.Clauses.Count);
                for (int i = 0; i < this.Clauses.Count; i++)
                {
                    var clause = this.Clauses[i];
                    var newSwitchItem1 = CopyElement(clause.Item1);
                    var newSwitchItem2 = CopyElement(clause.Item2);

                    newClauses.Add(Tuple.Create(newSwitchItem1, newSwitchItem2));
                }
            }

            return new SwitchStatementAst(this.Extent, this.Label, newCondition, this.Flags, newClauses, newDefault);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitSwitchStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitSwitchStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Condition.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < Clauses.Count; index++)
                {
                    var switchClauseAst = Clauses[index];
                    action = switchClauseAst.Item1.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                    action = switchClauseAst.Item2.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            if (action == AstVisitAction.Continue && Default != null)
                action = Default.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    #endregion Looping Statements

    #region Exception Handling Statements

    /// <summary>
    /// The ast that represents a single catch as part of a try statement.
    /// </summary>
    public class CatchClauseAst : Ast
    {
        private static readonly ReadOnlyCollection<TypeConstraintAst> s_emptyCatchTypes =
            Utils.EmptyReadOnlyCollection<TypeConstraintAst>();

        /// <summary>
        /// Construct a catch clause.
        /// </summary>
        /// <param name="extent">The extent of the catch, from the catch keyword to the closing curly brace.</param>
        /// <param name="catchTypes">The collection of types caught by this catch clause, may be null if all types are caught.</param>
        /// <param name="body">The body of the catch clause.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="body"/> is null.
        /// </exception>
        public CatchClauseAst(IScriptExtent extent, IEnumerable<TypeConstraintAst> catchTypes, StatementBlockAst body)
            : base(extent)
        {
            if (body == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(body));
            }

            if (catchTypes != null)
            {
                this.CatchTypes = new ReadOnlyCollection<TypeConstraintAst>(catchTypes.ToArray());
                SetParents(CatchTypes);
            }
            else
            {
                this.CatchTypes = s_emptyCatchTypes;
            }

            this.Body = body;
            SetParent(body);
        }

        /// <summary>
        /// A possibly empty collection of types caught by this catch block.  If the collection is empty, the catch handler
        /// catches all exceptions.
        /// </summary>
        public ReadOnlyCollection<TypeConstraintAst> CatchTypes { get; }

        /// <summary>
        /// Returns true if this handler handles any kind of exception.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "CatchAll")]
        public bool IsCatchAll { get { return CatchTypes.Count == 0; } }

        /// <summary>
        /// The body of the catch block.  This property is never null.
        /// </summary>
        public StatementBlockAst Body { get; }

        /// <summary>
        /// Copy the CatchClauseAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newCatchTypes = CopyElements(this.CatchTypes);
            var newBody = CopyElement(this.Body);
            return new CatchClauseAst(this.Extent, newCatchTypes, newBody);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitCatchClause(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitCatchClause(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            for (int index = 0; index < CatchTypes.Count; index++)
            {
                var catchType = CatchTypes[index];
                if (action != AstVisitAction.Continue) break;
                action = catchType.InternalVisit(visitor);
            }

            if (action == AstVisitAction.Continue)
                action = Body.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast that represents a try statement.
    /// </summary>
    public class TryStatementAst : StatementAst
    {
        private static readonly ReadOnlyCollection<CatchClauseAst> s_emptyCatchClauses =
            Utils.EmptyReadOnlyCollection<CatchClauseAst>();

        /// <summary>
        /// Construct a try statement ast.
        /// </summary>
        /// <param name="extent">
        /// The extent of the try statement, from the try keyword to the closing curly of the last catch or finally.
        /// </param>
        /// <param name="body">The region of guarded code.</param>
        /// <param name="catchClauses">The list of catch clauses, may be null.</param>
        /// <param name="finally">The finally clause, may be null.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="body"/> is null.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// If <paramref name="catchClauses"/> is null or is an empty collection and <paramref name="finally"/> is also
        /// null, then an exception is also raised as the try block must have a finally or at least one catch.
        /// </exception>
        public TryStatementAst(IScriptExtent extent,
                               StatementBlockAst body,
                               IEnumerable<CatchClauseAst> catchClauses,
                               StatementBlockAst @finally)
            : base(extent)
        {
            if (body == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(body));
            }

            if ((catchClauses == null || !catchClauses.Any()) && @finally == null)
            {
                // If no catches and no finally, just complain about catchClauses as that's the most likely invalid argument.
                throw PSTraceSource.NewArgumentException(nameof(catchClauses));
            }

            this.Body = body;
            SetParent(body);
            if (catchClauses != null)
            {
                this.CatchClauses = new ReadOnlyCollection<CatchClauseAst>(catchClauses.ToArray());
                SetParents(CatchClauses);
            }
            else
            {
                this.CatchClauses = s_emptyCatchClauses;
            }

            if (@finally != null)
            {
                this.Finally = @finally;
                SetParent(@finally);
            }
        }

        /// <summary>
        /// The body of the try statement.  This property is never null.
        /// </summary>
        public StatementBlockAst Body { get; }

        /// <summary>
        /// A collection of catch clauses, which is empty if there are no catches.
        /// </summary>
        public ReadOnlyCollection<CatchClauseAst> CatchClauses { get; }

        /// <summary>
        /// The ast for the finally block, or null if no finally block was specified, in which case <see cref="CatchClauses"/>
        /// is a non-null, non-empty collection.
        /// </summary>
        public StatementBlockAst Finally { get; }

        /// <summary>
        /// Copy the TryStatementAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newBody = CopyElement(this.Body);
            var newCatchClauses = CopyElements(this.CatchClauses);
            var newFinally = CopyElement(this.Finally);

            return new TryStatementAst(this.Extent, newBody, newCatchClauses, newFinally);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitTryStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitTryStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Body.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < CatchClauses.Count; index++)
                {
                    var catchClause = CatchClauses[index];
                    action = catchClause.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            if (action == AstVisitAction.Continue && Finally != null)
                action = Finally.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast that represents the trap statement.
    /// </summary>
    public class TrapStatementAst : StatementAst
    {
        /// <summary>
        /// Construct a trap statement.
        /// </summary>
        /// <param name="extent">
        /// The extent of the trap statement, starting with the trap keyword and ending with the closing curly of the body.
        /// </param>
        /// <param name="trapType">The type handled by the trap statement, may be null if all exceptions are trapped.</param>
        /// <param name="body">The handler for the error.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="body"/> is null.
        /// </exception>
        public TrapStatementAst(IScriptExtent extent, TypeConstraintAst trapType, StatementBlockAst body)
            : base(extent)
        {
            if (body == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(body));
            }

            if (trapType != null)
            {
                this.TrapType = trapType;
                SetParent(trapType);
            }

            this.Body = body;
            SetParent(body);
        }

        /// <summary>
        /// The ast for the type trapped by this trap block, or null if no type was specified.
        /// </summary>
        public TypeConstraintAst TrapType { get; }

        /// <summary>
        /// The body for the trap block.  This property is never null.
        /// </summary>
        public StatementBlockAst Body { get; }

        /// <summary>
        /// Copy the TrapStatementAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newTrapType = CopyElement(this.TrapType);
            var newBody = CopyElement(this.Body);
            return new TrapStatementAst(this.Extent, newTrapType, newBody);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitTrap(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitTrap(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue && TrapType != null)
                action = TrapType.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Body.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    #endregion Exception Handling Statements

    #region Flow Control Statements

    /// <summary>
    /// The ast representing the break statement.
    /// </summary>
    public class BreakStatementAst : StatementAst
    {
        /// <summary>
        /// Construct a break statement ast.
        /// </summary>
        /// <param name="extent">The extent of the statement, including the break keyword and the optional label.</param>
        /// <param name="label">The optional label expression.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        public BreakStatementAst(IScriptExtent extent, ExpressionAst label)
            : base(extent)
        {
            if (label != null)
            {
                this.Label = label;
                SetParent(label);
            }
        }

        /// <summary>
        /// The expression or label to break to, or null if no label was specified.
        /// </summary>
        public ExpressionAst Label { get; }

        /// <summary>
        /// Copy the BreakStatementAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newLabel = CopyElement(this.Label);
            return new BreakStatementAst(this.Extent, newLabel);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitBreakStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitBreakStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue && Label != null)
                action = Label.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast representing the continue statement.
    /// </summary>
    public class ContinueStatementAst : StatementAst
    {
        /// <summary>
        /// Construct a continue statement.
        /// </summary>
        /// <param name="extent">The extent of the statement including the optional label.</param>
        /// <param name="label">The optional label expression.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        public ContinueStatementAst(IScriptExtent extent, ExpressionAst label)
            : base(extent)
        {
            if (label != null)
            {
                this.Label = label;
                SetParent(label);
            }
        }

        /// <summary>
        /// The expression or label to continue to, or null if no label was specified.
        /// </summary>
        public ExpressionAst Label { get; }

        /// <summary>
        /// Copy the ContinueStatementAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newLabel = CopyElement(this.Label);
            return new ContinueStatementAst(this.Extent, newLabel);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitContinueStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitContinueStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue && Label != null)
                action = Label.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast representing the return statement.
    /// </summary>
    public class ReturnStatementAst : StatementAst
    {
        /// <summary>
        /// Construct a return statement.
        /// </summary>
        /// <param name="extent">The extent of the statement including the optional return value.</param>
        /// <param name="pipeline">The optional return value.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        public ReturnStatementAst(IScriptExtent extent, PipelineBaseAst pipeline)
            : base(extent)
        {
            if (pipeline != null)
            {
                this.Pipeline = pipeline;
                SetParent(pipeline);
            }
        }

        /// <summary>
        /// The pipeline specified in the return statement, or null if none was specified.
        /// </summary>
        public PipelineBaseAst Pipeline { get; }

        /// <summary>
        /// Copy the ReturnStatementAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newPipeline = CopyElement(this.Pipeline);
            return new ReturnStatementAst(this.Extent, newPipeline);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitReturnStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitReturnStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue && Pipeline != null)
                action = Pipeline.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast representing the exit statement.
    /// </summary>
    public class ExitStatementAst : StatementAst
    {
        /// <summary>
        /// Construct an exit statement.
        /// </summary>
        /// <param name="extent">The extent of the exit statement including the optional exit value.</param>
        /// <param name="pipeline">The optional exit value.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        public ExitStatementAst(IScriptExtent extent, PipelineBaseAst pipeline)
            : base(extent)
        {
            if (pipeline != null)
            {
                this.Pipeline = pipeline;
                SetParent(pipeline);
            }
        }

        /// <summary>
        /// The pipeline specified in the exit statement, or null if none was specified.
        /// </summary>
        public PipelineBaseAst Pipeline { get; }

        /// <summary>
        /// Copy the ExitStatementAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newPipeline = CopyElement(this.Pipeline);
            return new ExitStatementAst(this.Extent, newPipeline);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitExitStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitExitStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue && Pipeline != null)
                action = Pipeline.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast representing the throw statement.
    /// </summary>
    public class ThrowStatementAst : StatementAst
    {
        /// <summary>
        /// Construct a throw statement.
        /// </summary>
        /// <param name="extent">The extent of the throw statement, including the optional value to throw.</param>
        /// <param name="pipeline">The optional value to throw.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        public ThrowStatementAst(IScriptExtent extent, PipelineBaseAst pipeline)
            : base(extent)
        {
            if (pipeline != null)
            {
                this.Pipeline = pipeline;
                SetParent(pipeline);
            }
        }

        /// <summary>
        /// The pipeline specified in the throw statement, or null if none was specified.
        /// </summary>
        public PipelineBaseAst Pipeline { get; }

        /// <summary>
        /// If the throw statement is a rethrow.  In PowerShell, a throw statement need not throw anything.  Such
        /// a throw statement throws a new exception if it does not appear lexically withing a catch, otherwise
        /// it rethrows the caught exception.  Examples:
        /// <c>
        ///     if ($true) { throw } # not a rethrow
        ///     try { foo } catch { throw } # rethrow
        ///     try { foo } catch { . { throw } } # rethrow
        ///     try { foo } catch { function foo { throw } } # rethrow
        ///     try { foo } finally { throw } # not a rethrow
        /// </c>
        /// </summary>
        public bool IsRethrow
        {
            get
            {
                if (Pipeline != null)
                    return false;

                var parent = Parent;
                while (parent != null)
                {
                    if (parent is CatchClauseAst)
                    {
                        return true;
                    }

                    parent = parent.Parent;
                }

                return false;
            }
        }

        /// <summary>
        /// Copy the ThrowStatementAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newPipeline = CopyElement(this.Pipeline);
            return new ThrowStatementAst(this.Extent, newPipeline);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitThrowStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitThrowStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue && Pipeline != null)
                action = Pipeline.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// An AST representing a syntax element chainable with '&amp;&amp;' or '||'.
    /// </summary>
    public abstract class ChainableAst : PipelineBaseAst
    {
        /// <summary>
        /// Initializes a new instance of the new chainable AST with the given extent.
        /// </summary>
        /// <param name="extent">The script extent of the AST.</param>
        protected ChainableAst(IScriptExtent extent) : base(extent)
        {
        }
    }

    /// <summary>
    /// A command-oriented flow-controlled pipeline chain.
    /// E.g. <c>npm build &amp;&amp; npm test</c> or <c>Get-Content -Raw ./file.txt || "default"</c>.
    /// </summary>
    public class PipelineChainAst : ChainableAst
    {
        /// <summary>
        /// Initializes a new instance of the new statement chain AST from two statements and an operator.
        /// </summary>
        /// <param name="extent">The extent of the chained statement.</param>
        /// <param name="lhsChain">The pipeline or pipeline chain to the left of the operator.</param>
        /// <param name="rhsPipeline">The pipeline to the right of the operator.</param>
        /// <param name="chainOperator">The operator used.</param>
        /// <param name="background">True when this chain has been invoked with the background operator, false otherwise.</param>
        public PipelineChainAst(
            IScriptExtent extent,
            ChainableAst lhsChain,
            PipelineAst rhsPipeline,
            TokenKind chainOperator,
            bool background = false)
            : base(extent)
        {
            if (lhsChain == null)
            {
                throw new ArgumentNullException(nameof(lhsChain));
            }

            if (rhsPipeline == null)
            {
                throw new ArgumentNullException(nameof(rhsPipeline));
            }

            if (chainOperator != TokenKind.AndAnd && chainOperator != TokenKind.OrOr)
            {
                throw new ArgumentException(nameof(chainOperator));
            }

            LhsPipelineChain = lhsChain;
            RhsPipeline = rhsPipeline;
            Operator = chainOperator;
            Background = background;

            SetParent(LhsPipelineChain);
            SetParent(RhsPipeline);
        }

        /// <summary>
        /// Gets the left hand pipeline in the chain.
        /// </summary>
        public ChainableAst LhsPipelineChain { get; }

        /// <summary>
        /// Gets the right hand pipeline in the chain.
        /// </summary>
        public PipelineAst RhsPipeline { get; }

        /// <summary>
        /// Gets the chaining operator used.
        /// </summary>
        public TokenKind Operator { get; }

        /// <summary>
        /// Gets a flag that indicates whether this chain has been invoked with the background operator.
        /// </summary>
        public bool Background { get; }

        /// <summary>
        /// Create a copy of this Ast.
        /// </summary>
        /// <returns>
        /// A fresh copy of this PipelineChainAst instance.
        /// </returns>
        public override Ast Copy()
        {
            return new PipelineChainAst(Extent, CopyElement(LhsPipelineChain), CopyElement(RhsPipeline), Operator, Background);
        }

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return (visitor as ICustomAstVisitor2)?.VisitPipelineChain(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            AstVisitAction action = AstVisitAction.Continue;

            // Can only visit new AST type if using AstVisitor2
            if (visitor is AstVisitor2 visitor2)
            {
                action = visitor2.VisitPipelineChain(this);
                if (action == AstVisitAction.SkipChildren)
                {
                    return visitor.CheckForPostAction(this, AstVisitAction.Continue);
                }
            }

            if (action == AstVisitAction.Continue)
            {
                action = LhsPipelineChain.InternalVisit(visitor);
            }

            if (action == AstVisitAction.Continue)
            {
                action = RhsPipeline.InternalVisit(visitor);
            }

            return visitor.CheckForPostAction(this, action);
        }
    }

    #endregion Flow Control Statements

    #region Pipelines

    /// <summary>
    /// An abstract base class for statements that include command invocations, pipelines, expressions, and assignments.
    /// Any statement that does not begin with a keyword is derives from PipelineBastAst.
    /// </summary>
    public abstract class PipelineBaseAst : StatementAst
    {
        /// <summary>
        /// Initialize the common parts of a PipelineBaseAst.
        /// </summary>
        /// <param name="extent">The extent of the statement.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        protected PipelineBaseAst(IScriptExtent extent)
            : base(extent)
        {
        }

        /// <summary>
        /// If the pipeline represents a pure expression, the expression is returned, otherwise null is returned.
        /// </summary>
        public virtual ExpressionAst GetPureExpression()
        {
            return null;
        }
    }

    /// <summary>
    /// The ast that represents a PowerShell pipeline, e.g. <c>gci -re . *.cs | select-string Foo</c> or <c> 65..90 | % { [char]$_ }</c>.
    /// A pipeline must have at least 1 command.  The first command may be an expression or a command invocation.
    /// </summary>
    public class PipelineAst : ChainableAst
    {
        /// <summary>
        /// Construct a pipeline from a collection of commands.
        /// </summary>
        /// <param name="extent">The extent of the pipeline.</param>
        /// <param name="pipelineElements">The collection of commands representing the pipeline.</param>
        /// <param name="background">Indicates that this pipeline should be run in the background.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// If <paramref name="pipelineElements"/> is null or is an empty collection.
        /// </exception>
        public PipelineAst(IScriptExtent extent, IEnumerable<CommandBaseAst> pipelineElements, bool background)
            : base(extent)
        {
            if (pipelineElements == null || !pipelineElements.Any())
            {
                throw PSTraceSource.NewArgumentException(nameof(pipelineElements));
            }

            this.Background = background;
            this.PipelineElements = new ReadOnlyCollection<CommandBaseAst>(pipelineElements.ToArray());
            SetParents(PipelineElements);
        }

        /// <summary>
        /// Construct a pipeline from a collection of commands.
        /// </summary>
        /// <param name="extent">The extent of the pipeline.</param>
        /// <param name="pipelineElements">The collection of commands representing the pipeline.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// If <paramref name="pipelineElements"/> is null or is an empty collection.
        /// </exception>
        public PipelineAst(IScriptExtent extent, IEnumerable<CommandBaseAst> pipelineElements) : this(extent, pipelineElements, background: false)
        {
        }

        /// <summary>
        /// Construct a pipeline from a single command.
        /// </summary>
        /// <param name="extent">The extent of the pipeline (which should be the extent of the command).</param>
        /// <param name="commandAst">The command for the pipeline.</param>
        /// <param name="background">Indicates that this pipeline should be run in the background.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="commandAst"/> is null.
        /// </exception>
        public PipelineAst(IScriptExtent extent, CommandBaseAst commandAst, bool background)
            : base(extent)
        {
            if (commandAst == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(commandAst));
            }

            this.Background = background;
            this.PipelineElements = new ReadOnlyCollection<CommandBaseAst>(new CommandBaseAst[] { commandAst });
            SetParent(commandAst);
        }

        /// <summary>
        /// Construct a pipeline from a single command.
        /// </summary>
        /// <param name="extent">The extent of the pipeline (which should be the extent of the command).</param>
        /// <param name="commandAst">The command for the pipeline.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="commandAst"/> is null.
        /// </exception>
        public PipelineAst(IScriptExtent extent, CommandBaseAst commandAst) : this(extent, commandAst, background: false)
        {
        }

        /// <summary>
        /// A non-null, non-empty collection of commands that represent the pipeline.
        /// </summary>
        public ReadOnlyCollection<CommandBaseAst> PipelineElements { get; }

        /// <summary>
        /// Indicates that this pipeline should be run in the background.
        /// </summary>
        public bool Background { get; internal set; }

        /// <summary>
        /// If the pipeline represents a pure expression, the expression is returned, otherwise null is returned.
        /// </summary>
        public override ExpressionAst GetPureExpression()
        {
            if (PipelineElements.Count != 1)
            {
                return null;
            }

            CommandExpressionAst expr = PipelineElements[0] as CommandExpressionAst;
            if (expr != null && expr.Redirections.Count == 0)
            {
                return expr.Expression;
            }

            return null;
        }

        /// <summary>
        /// Copy the PipelineAst instance.
        /// </summary>
        /// <returns>A fresh copy of this PipelineAst instance.</returns>
        public override Ast Copy()
        {
            var newPipelineElements = CopyElements(this.PipelineElements);
            return new PipelineAst(this.Extent, newPipelineElements, this.Background);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitPipeline(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitPipeline(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < PipelineElements.Count; index++)
                {
                    var commandAst = PipelineElements[index];
                    action = commandAst.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// An abstract base class for the components of a <see cref="CommandAst"/>.
    /// </summary>
    public abstract class CommandElementAst : Ast
    {
        /// <summary>
        /// Initialize the common fields of a comment element.
        /// </summary>
        /// <param name="extent">The extent of the command element.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        protected CommandElementAst(IScriptExtent extent)
            : base(extent)
        {
        }
    }

    /// <summary>
    /// The ast that represents a parameter to a command, e.g. <c>dir -Path a*</c>, this class represent '-Path', and
    /// in <c>dir -Path:a*</c>, this class represents '-Path:a*'.
    ///
    /// In the first case, the argument 'a*' is not represented by this class because the parser can't know until runtime
    /// if the argument is positional or if -Path accepts an argument.  In the later case, the argument 'a*' always
    /// belongs to the parameter -Path.
    /// </summary>
    public class CommandParameterAst : CommandElementAst
    {
        /// <summary>
        /// Construct a command parameter.
        /// </summary>
        /// <param name="extent">
        /// The extent of the parameter, starting from the dash character, ending at the end of the parameter name, or else
        /// at the end of the optional argument.
        /// </param>
        /// <param name="parameterName">
        /// The parameter name, without the leading dash and without the trailing colon, if a colon was used.
        /// </param>
        /// <param name="argument">
        /// If the parameter includes an argument with the syntax like <c>-Path:a*</c>, then the expression for 'a*' is
        /// passed as the argument.  An argument is not required.
        /// </param>
        /// <param name="errorPosition">
        /// The extent to use for error reporting when parameter binding fails with this parameter.  If <paramref name="argument"/>
        /// is null, this extent is the same as <paramref name="extent"/>, otherwise it is the extent of the parameter token
        /// itself.
        /// </param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="parameterName"/>is null, or if <paramref name="parameterName"/>
        /// is an empty string.
        /// </exception>
        public CommandParameterAst(IScriptExtent extent, string parameterName, ExpressionAst argument, IScriptExtent errorPosition)
            : base(extent)
        {
            if (errorPosition == null || string.IsNullOrEmpty(parameterName))
            {
                throw PSTraceSource.NewArgumentNullException(errorPosition == null ? "errorPosition" : "parameterName");
            }

            this.ParameterName = parameterName;
            if (argument != null)
            {
                this.Argument = argument;
                SetParent(argument);
            }

            this.ErrorPosition = errorPosition;
        }

        /// <summary>
        /// The name of the parameter.  This value does not include a leading dash, and in the case that an argument
        /// is specified, no trailing colon is included either.  This property is never null or empty.
        /// </summary>
        public string ParameterName { get; }

        /// <summary>
        /// The ast for the argument if specified (e.g. -Path:-abc, then the argument is the ast for '-ast'), otherwise null
        /// if no argument was specified.
        /// </summary>
        public ExpressionAst Argument { get; }

        /// <summary>
        /// The error position to use when parameter binding fails.  This extent does not include the argument if one was
        /// specified, which means this extent is often the same as <see cref="Ast.Extent"/>.
        /// </summary>
        public IScriptExtent ErrorPosition { get; }

        /// <summary>
        /// Copy the CommandParameterAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newArgument = CopyElement(this.Argument);
            return new CommandParameterAst(this.Extent, this.ParameterName, newArgument, this.ErrorPosition);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitCommandParameter(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitCommandParameter(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (Argument != null && action == AstVisitAction.Continue)
            {
                action = Argument.InternalVisit(visitor);
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// An abstract base class for a command and an expression wrapper that allows an expression as a command in a pipeline.
    /// </summary>
    public abstract class CommandBaseAst : StatementAst
    {
        private static readonly ReadOnlyCollection<RedirectionAst> s_emptyRedirections =
            Utils.EmptyReadOnlyCollection<RedirectionAst>();

        internal const int MaxRedirections = (int)RedirectionStream.Information + 1;

        /// <summary>
        /// Initialize the common fields of a command.
        /// </summary>
        /// <param name="extent">The extent of the command.</param>
        /// <param name="redirections">The redirections for the command, may be null.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        protected CommandBaseAst(IScriptExtent extent, IEnumerable<RedirectionAst> redirections)
            : base(extent)
        {
            if (redirections != null)
            {
                this.Redirections = new ReadOnlyCollection<RedirectionAst>(redirections.ToArray());
                SetParents(Redirections);
            }
            else
            {
                this.Redirections = s_emptyRedirections;
            }
        }

        /// <summary>
        /// The possibly empty collection of redirections for this command.
        /// </summary>
        public ReadOnlyCollection<RedirectionAst> Redirections { get; }
    }

    /// <summary>
    /// The ast for a command invocation, e.g. <c>dir *.ps1</c>.
    /// </summary>
    public class CommandAst : CommandBaseAst
    {
        /// <summary>
        /// Construct a command invocation.
        /// </summary>
        /// <param name="extent">
        /// The extent of the command, starting with either the optional invocation operator '&amp;' or '.' or the command name
        /// and ending with the last command element.
        /// </param>
        /// <param name="commandElements">The elements of the command (command name, parameters and expressions.).</param>
        /// <param name="invocationOperator">The invocation operator that was used, if any.</param>
        /// <param name="redirections">The redirections for the command, may be null.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// If <paramref name="commandElements"/> is null or is an empty collection.
        /// </exception>
        public CommandAst(IScriptExtent extent,
                          IEnumerable<CommandElementAst> commandElements,
                          TokenKind invocationOperator,
                          IEnumerable<RedirectionAst> redirections)
            : base(extent, redirections)
        {
            if (commandElements == null || !commandElements.Any())
            {
                throw PSTraceSource.NewArgumentException(nameof(commandElements));
            }

            if (invocationOperator != TokenKind.Dot && invocationOperator != TokenKind.Ampersand && invocationOperator != TokenKind.Unknown)
            {
                throw PSTraceSource.NewArgumentException(nameof(invocationOperator));
            }

            this.CommandElements = new ReadOnlyCollection<CommandElementAst>(commandElements.ToArray());
            SetParents(CommandElements);
            this.InvocationOperator = invocationOperator;
        }

        /// <summary>
        /// A non-empty collection of command elements.  This property is never null.
        /// </summary>
        public ReadOnlyCollection<CommandElementAst> CommandElements { get; }

        /// <summary>
        /// The invocation operator (either <see cref="TokenKind.Dot"/> or <see cref="TokenKind.Ampersand"/>) if one was specified,
        /// otherwise the value is <see cref="TokenKind.Unknown"/>.
        /// </summary>
        public TokenKind InvocationOperator { get; }

        /// <summary>
        /// <para>Returns the name of the command invoked by this ast.</para>
        /// <para>This command name may not be known statically, in which case null is returned.</para>
        /// <para>
        /// For example, if the command name is in a variable: <example>&amp; $foo</example>, then the parser cannot know which command is executed.
        /// Similarly, if the command is being invoked in a module: <example>&amp; (gmo SomeModule) Bar</example>, then the parser does not know the
        /// command name is Bar because the parser can't determine that the expression <code>(gmo SomeModule)</code> returns a module instead
        /// of a string.
        /// </para>
        /// </summary>
        /// <returns>The command name, if known, null otherwise.</returns>
        public string GetCommandName()
        {
            var name = CommandElements[0] as StringConstantExpressionAst;
            return name?.Value;
        }

        /// <summary>
        /// If this command was synthesized out of a dynamic keyword, this property will point to the DynamicKeyword
        /// data structure that was used to create this command.
        /// </summary>
        public DynamicKeyword DefiningKeyword { get; set; }

        /// <summary>
        /// Copy the CommandAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newCommandElements = CopyElements(this.CommandElements);
            var newRedirections = CopyElements(this.Redirections);
            return new CommandAst(this.Extent, newCommandElements, this.InvocationOperator, newRedirections)
            {
                DefiningKeyword = this.DefiningKeyword
            };
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitCommand(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitCommand(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < CommandElements.Count; index++)
                {
                    var commandElementAst = CommandElements[index];
                    action = commandElementAst.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < Redirections.Count; index++)
                {
                    var redirection = Redirections[index];
                    if (action == AstVisitAction.Continue)
                    {
                        action = redirection.InternalVisit(visitor);
                    }
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast representing an expression when the expression is used as the first command of a pipeline.
    /// </summary>
    public class CommandExpressionAst : CommandBaseAst
    {
        /// <summary>
        /// Construct a command that wraps an expression.
        /// </summary>
        /// <param name="extent">The extent of the expression.</param>
        /// <param name="expression">The expression being wrapped.</param>
        /// <param name="redirections">The redirections for the command, may be null.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="expression"/> is null.
        /// </exception>
        public CommandExpressionAst(IScriptExtent extent,
                                    ExpressionAst expression,
                                    IEnumerable<RedirectionAst> redirections)
            : base(extent, redirections)
        {
            if (expression == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(expression));
            }

            this.Expression = expression;
            SetParent(expression);
        }

        /// <summary>
        /// The ast for the expression that is or starts a pipeline.  This property is never null.
        /// </summary>
        public ExpressionAst Expression { get; }

        /// <summary>
        /// Copy the CommandExpressionAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newExpression = CopyElement(this.Expression);
            var newRedirections = CopyElements(this.Redirections);
            return new CommandExpressionAst(this.Extent, newExpression, newRedirections);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitCommandExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitCommandExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Expression.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < Redirections.Count; index++)
                {
                    var redirection = Redirections[index];
                    if (action == AstVisitAction.Continue)
                    {
                        action = redirection.InternalVisit(visitor);
                    }
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// An abstract base class representing both file redirections and merging redirections.
    /// </summary>
    public abstract class RedirectionAst : Ast
    {
        /// <summary>
        /// Initialize the common fields in a redirection.
        /// </summary>
        /// <param name="extent">The extent of the redirection.</param>
        /// <param name="from">The stream to read from.</param>
        protected RedirectionAst(IScriptExtent extent, RedirectionStream from)
            : base(extent)
        {
            this.FromStream = from;
        }

        /// <summary>
        /// The stream to read objects from.  Objects are either merged with another stream, or written to a file.
        /// </summary>
        public RedirectionStream FromStream { get; }
    }

    /// <summary>
    /// The stream number that is redirected.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public enum RedirectionStream
    {
        /// <summary>
        /// All streams, used when the redirection token uses '*' as the stream number.
        /// </summary>
        All = 0,

        /// <summary>
        /// The normal output stream.
        /// </summary>
        Output = 1,

        /// <summary>
        /// The error stream.
        /// </summary>
        Error = 2,

        /// <summary>
        /// The warning stream.
        /// </summary>
        Warning = 3,

        /// <summary>
        /// The verbose stream.
        /// </summary>
        Verbose = 4,

        /// <summary>
        /// The debug stream.
        /// </summary>
        Debug = 5,

        /// <summary>
        /// The information stream.
        /// </summary>
        Information = 6
    }

    /// <summary>
    /// The ast representing a redirection that merges 2 streams, e.g. <c>dir 2>&amp;1</c>
    /// </summary>
    public class MergingRedirectionAst : RedirectionAst
    {
        /// <summary>
        /// Construct a merging redirection.
        /// </summary>
        /// <param name="extent">The extent of the redirection.</param>
        /// <param name="from">The stream to read from.</param>
        /// <param name="to">The stream to write to - must always be <see cref="RedirectionStream.Output"/></param>
        /// <exception cref="PSArgumentNullException">If <paramref name="extent"/> is null.</exception>
        public MergingRedirectionAst(IScriptExtent extent, RedirectionStream from, RedirectionStream to)
            : base(extent, from)
        {
            this.ToStream = to;
        }

        /// <summary>
        /// The stream that results will be written to.
        /// </summary>
        public RedirectionStream ToStream { get; }

        /// <summary>
        /// Copy the MergingRedirectionAst instance.
        /// </summary>
        public override Ast Copy()
        {
            return new MergingRedirectionAst(this.Extent, this.FromStream, this.ToStream);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitMergingRedirection(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitMergingRedirection(this);
            return visitor.CheckForPostAction(this, action == AstVisitAction.SkipChildren ? AstVisitAction.Continue : action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast representing a redirection to a file, e.g. <c>dir > out.txt</c>, the '> out.txt' is represented by this ast.
    /// </summary>
    public class FileRedirectionAst : RedirectionAst
    {
        /// <summary>
        /// Construct a redirection to a file.
        /// </summary>
        /// <param name="extent">
        /// The extent of the redirection, starting with the redirection operator and including the file.
        /// </param>
        /// <param name="stream">
        /// The stream being redirected.
        /// </param>
        /// <param name="file">
        /// The optional location to redirect to.  Merging operators may not specify a file, the other redirection
        /// operators must specify a location.
        /// </param>
        /// <param name="append">
        /// True if the file is being appended, false otherwise.
        /// </param>
        /// <exception cref="PSArgumentNullException">If <paramref name="extent"/> is null.</exception>
        /// <exception cref="PSArgumentException">If <paramref name="file"/> is null.</exception>
        public FileRedirectionAst(IScriptExtent extent, RedirectionStream stream, ExpressionAst file, bool append)
            : base(extent, stream)
        {
            if (file == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(file));
            }

            this.Location = file;
            SetParent(file);
            this.Append = append;
        }

        /// <summary>
        /// The ast for the location to redirect to.
        /// </summary>
        public ExpressionAst Location { get; }

        /// <summary>
        /// True if the file is appended, false otherwise.
        /// </summary>
        public bool Append { get; }

        /// <summary>
        /// Copy the FileRedirectionAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newFile = CopyElement(this.Location);
            return new FileRedirectionAst(this.Extent, this.FromStream, newFile, this.Append);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitFileRedirection(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitFileRedirection(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Location.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    #endregion Pipelines

    /// <summary>
    /// The ast that represents an assignment statement, e.g. <c>$x = 42</c>.
    /// </summary>
    public class AssignmentStatementAst : PipelineBaseAst
    {
        /// <summary>
        /// Construct an assignment statement.
        /// </summary>
        /// <param name="extent">The extent of the assignment statement.</param>
        /// <param name="left">The value being assigned.</param>
        /// <param name="operator">The assignment operator, e.g. '=' or '+='.</param>
        /// <param name="right">The value to assign.</param>
        /// <param name="errorPosition">The position to report an error if an error occurs at runtime.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/>, <paramref name="left"/>, <paramref name="right"/>,
        /// or <paramref name="errorPosition"/> is null.
        /// </exception>
        public AssignmentStatementAst(IScriptExtent extent, ExpressionAst left, TokenKind @operator, StatementAst right, IScriptExtent errorPosition)
            : base(extent)
        {
            if (left == null || right == null || errorPosition == null)
            {
                throw PSTraceSource.NewArgumentNullException(left == null ? "left" : right == null ? "right" : "errorPosition");
            }

            if ((@operator.GetTraits() & TokenFlags.AssignmentOperator) == 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(@operator));
            }

            // If the assignment is just an expression and the expression is not backgrounded then
            // remove the pipeline wrapping the expression.
            var pipelineAst = right as PipelineAst;
            if (pipelineAst != null && !pipelineAst.Background)
            {
                if (pipelineAst.PipelineElements.Count == 1)
                {
                    var commandExpressionAst = pipelineAst.PipelineElements[0] as CommandExpressionAst;

                    if (commandExpressionAst != null)
                    {
                        right = commandExpressionAst;
                        right.ClearParent();
                    }
                }
            }

            this.Operator = @operator;
            this.Left = left;
            SetParent(left);
            this.Right = right;
            SetParent(right);
            this.ErrorPosition = errorPosition;
        }

        /// <summary>
        /// The ast for the location being assigned.  This property is never null.
        /// </summary>
        public ExpressionAst Left { get; }

        /// <summary>
        /// The operator for token assignment (such as =, +=, -=, etc.).  The value is always some assignment operator.
        /// </summary>
        public TokenKind Operator { get; }

        /// <summary>
        /// The ast for the value to assign.  This property is never null.
        /// </summary>
        public StatementAst Right { get; }

        /// <summary>
        /// The position to report at runtime if there is an error during assignment.  This property is never null.
        /// </summary>
        public IScriptExtent ErrorPosition { get; }

        /// <summary>
        /// Copy the AssignmentStatementAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newLeft = CopyElement(this.Left);
            var newRight = CopyElement(this.Right);
            return new AssignmentStatementAst(this.Extent, newLeft, this.Operator, newRight, this.ErrorPosition);
        }

        /// <summary>
        /// Return all of the expressions assigned by the assignment statement.  Typically
        /// it's just a variable expression, but if <see cref="Left"/> is an <see cref="ArrayLiteralAst"/>,
        /// then all of the elements are assigned.
        /// </summary>
        /// <returns>All of the expressions assigned by the assignment statement.</returns>
        public IEnumerable<ExpressionAst> GetAssignmentTargets()
        {
            var arrayExpression = Left as ArrayLiteralAst;
            if (arrayExpression != null)
            {
                foreach (var element in arrayExpression.Elements)
                {
                    yield return element;
                }

                yield break;
            }

            yield return Left;
        }
        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitAssignmentStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitAssignmentStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Left.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Right.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// Defines types of configuration document.
    /// </summary>
    public enum ConfigurationType
    {
        /// <summary>
        /// Resource configuration.
        /// </summary>
        Resource = 0,

        /// <summary>
        /// Meta configuration.
        /// </summary>
        Meta = 1
    }

    /// <summary>
    /// The ast represents the DSC configuration statement.
    /// </summary>
    public class ConfigurationDefinitionAst : StatementAst
    {
        /// <summary>
        /// Construct a configuration statement.
        /// </summary>
        /// <param name="extent">
        /// The extent of the expression, starting with the attribute and ending after the expression being attributed.
        /// </param>
        /// <param name="body"><see cref="ScriptBlockExpressionAst"/> of the configuration statement.</param>
        /// <param name="type">The type of the configuration.</param>
        /// <param name="instanceName">The configuration name expression.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/>, <paramref name="body"/>, or <paramref name="instanceName"/> is null.
        /// </exception>
        public ConfigurationDefinitionAst(IScriptExtent extent,
            ScriptBlockExpressionAst body,
            ConfigurationType type,
            ExpressionAst instanceName) : base(extent)
        {
            if (extent == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(extent));
            }

            if (body == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(body));
            }

            if (instanceName == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(instanceName));
            }

            this.Body = body;
            SetParent(body);
            this.ConfigurationType = type;
            this.InstanceName = instanceName;
            SetParent(instanceName);
        }

        /// <summary>
        /// This ast represents configuration body script block.
        /// This property is never null.
        /// </summary>
        public ScriptBlockExpressionAst Body { get; }

        /// <summary>
        /// The configuration type.
        /// </summary>
        public ConfigurationType ConfigurationType { get; }

        /// <summary>
        /// The name of the configuration instance,
        /// For example, Instance name of 'configuration test { ...... }' is 'test'
        /// This property is never null.
        /// </summary>
        public ExpressionAst InstanceName { get; }

        /// <summary>
        /// Duplicates the <see cref="ConfigurationDefinitionAst"/>, allowing it to be composed into other ASTs.
        /// </summary>
        /// <returns>A copy of the <see cref="ConfigurationDefinitionAst"/>, with the link to the previous parent removed.</returns>
        public override Ast Copy()
        {
            ScriptBlockExpressionAst body = CopyElement(Body);
            ExpressionAst instanceName = CopyElement(InstanceName);
            return new ConfigurationDefinitionAst(Extent, body, ConfigurationType, instanceName)
            {
                LCurlyToken = this.LCurlyToken,
                ConfigurationToken = this.ConfigurationToken,
                CustomAttributes = this.CustomAttributes?.Select(static e => (AttributeAst)e.Copy())
            };
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            var visitor2 = visitor as ICustomAstVisitor2;
            return visitor2?.VisitConfigurationDefinition(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = AstVisitAction.Continue;
            var visitor2 = visitor as AstVisitor2;
            if (visitor2 != null)
            {
                action = visitor2.VisitConfigurationDefinition(this);
                if (action == AstVisitAction.SkipChildren)
                    return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            }

            if (action == AstVisitAction.Continue)
            {
                action = InstanceName.InternalVisit(visitor);
            }

            if (action == AstVisitAction.Continue)
            {
                Body.InternalVisit(visitor);
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        #region Internal methods/properties

        internal Token LCurlyToken { get; set; }

        internal Token ConfigurationToken { get; set; }

        internal IEnumerable<AttributeAst> CustomAttributes { get; set; }

        /// <summary>
        /// A dynamic keyword may also define additional keywords in the child scope
        /// of the scriptblock. This collection will contain those keywords.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        internal List<DynamicKeyword> DefinedKeywords { get; set; }

        /// <summary>
        /// Generate ast that defines a function for this <see cref="ConfigurationDefinitionAst"/> object.
        /// </summary>
        /// <returns>
        /// The <see cref="PipelineAst"/> that defines a function for this <see cref="ConfigurationDefinitionAst"/> object
        /// </returns>
        internal PipelineAst GenerateSetItemPipelineAst()
        {
            // **************************
            // Now construct the AST to call the function that will build the actual object.
            // This is composed of a call to command with the signature
            //    function PSDesiredStateConfiguration\\Configuration
            //    {
            //      param (
            //          ResourceModuleTuplesToImport,      # list of required resource-module tuples
            //          $OutputPath = ".",      # output path where the MOF will be placed
            //          $Name,                  # name of the configuration == name of the wrapper function
            //          [scriptblock]
            //              $Body,              # the body of the configuration
            //          [hashtable]
            //              $ArgsToBody,        # the argument values to pass to the body scriptblock
            //          [hashtable]
            //              $ConfigurationData, # a collection of property bags describe the configuration environment
            //          [string]
            //              $InstanceName = ""  # THe name of the configuration instance being created.
            //          [boolean]
            //               $IsMetaConfig = $false # the configuration to generated is a meta configuration
            //      )
            //   }

            var cea = new Collection<CommandElementAst>
            {
                new StringConstantExpressionAst(this.Extent,
                                                @"PSDesiredStateConfiguration\Configuration",
                                                StringConstantType.BareWord),
                new CommandParameterAst(LCurlyToken.Extent, "ArgsToBody", new VariableExpressionAst(LCurlyToken.Extent, "toBody", false), LCurlyToken.Extent),
                new CommandParameterAst(LCurlyToken.Extent, "Name", (ExpressionAst)InstanceName.Copy(), LCurlyToken.Extent)
            };

            ///////////////////////////
            // get import parameters
            var bodyStatements = Body.ScriptBlock.EndBlock.Statements;
            var resourceModulePairsToImport = new List<Tuple<string[], ModuleSpecification[], Version>>();
            var resourceBody = (from stm in bodyStatements where !IsImportCommand(stm, resourceModulePairsToImport) select (StatementAst)stm.Copy()).ToList();

            cea.Add(new CommandParameterAst(PositionUtilities.EmptyExtent, "ResourceModuleTuplesToImport", new ConstantExpressionAst(PositionUtilities.EmptyExtent, resourceModulePairsToImport), PositionUtilities.EmptyExtent));

            var scriptBlockBody = new ScriptBlockAst(Body.Extent,
                CustomAttributes?.Select(static att => (AttributeAst)att.Copy()).ToList(),
                null,
                new StatementBlockAst(Body.Extent, resourceBody, null),
                false, false);

            var scriptBlockExp = new ScriptBlockExpressionAst(Body.Extent, scriptBlockBody);

            // then the configuration scriptblock as -Body
            cea.Add(new CommandParameterAst(LCurlyToken.Extent, "Body", scriptBlockExp, LCurlyToken.Extent));

            cea.Add(new CommandParameterAst(LCurlyToken.Extent, "Outputpath", new VariableExpressionAst(LCurlyToken.Extent, "OutputPath", false), LCurlyToken.Extent));

            cea.Add(new CommandParameterAst(LCurlyToken.Extent, "ConfigurationData", new VariableExpressionAst(LCurlyToken.Extent, "ConfigurationData", false), LCurlyToken.Extent));

            cea.Add(new CommandParameterAst(LCurlyToken.Extent, "InstanceName", new VariableExpressionAst(LCurlyToken.Extent, "InstanceName", false), LCurlyToken.Extent));

            //
            // copy the configuration parameter to the new function parameter
            // the new set-item created function will have below parameters
            //    [cmdletbinding()]
            //    param(
            //            [string]
            //                $InstanceName,
            //            [string[]]
            //                $DependsOn,
            //            [string]
            //                $OutputPath,
            //            [hashtable]
            //            [Microsoft.PowerShell.DesiredStateConfiguration.ArgumentToConfigurationDataTransformation()]
            //               $ConfigurationData
            //        )
            //
            var attribAsts =
                ConfigurationBuildInParameterAttribAsts.Select(static attribAst => (AttributeAst)attribAst.Copy()).ToList();

            var paramAsts = ConfigurationBuildInParameters.Select(static paramAst => (ParameterAst)paramAst.Copy()).ToList();

            // the parameters defined in the configuration keyword will be combined with above parameters
            // it will be used to construct $ArgsToBody in the set-item created function boby using below statement
            //         $toBody = @{}+$PSBoundParameters
            //         $toBody.Remove(""OutputPath"")
            //         $toBody.Remove(""ConfigurationData"")
            //         $ConfigurationData = $psboundparameters[""ConfigurationData""]
            //         $Outputpath = $psboundparameters[""Outputpath""]
            if (Body.ScriptBlock.ParamBlock != null)
            {
                paramAsts.AddRange(Body.ScriptBlock.ParamBlock.Parameters.Select(static parameterAst => (ParameterAst)parameterAst.Copy()));
            }

            var paramBlockAst = new ParamBlockAst(this.Extent, attribAsts, paramAsts);

            var cmdAst = new CommandAst(this.Extent, cea, TokenKind.Unknown, null);

            var pipeLineAst = new PipelineAst(this.Extent, cmdAst, background: false);
            var funcStatements = ConfigurationExtraParameterStatements.Select(static statement => (StatementAst)statement.Copy()).ToList();
            funcStatements.Add(pipeLineAst);
            var statmentBlockAst = new StatementBlockAst(this.Extent, funcStatements, null);

            var funcBody = new ScriptBlockAst(Body.Extent,
                CustomAttributes?.Select(static att => (AttributeAst)att.Copy()).ToList(),
                paramBlockAst, statmentBlockAst, false, true);
            var funcBodyExp = new ScriptBlockExpressionAst(this.Extent, funcBody);

            #region "Construct Set-Item pipeline"

            // **************************
            // Now construct the AST to call the set-item cmdlet that will create the function
            // it will do set-item -Path function:\ConfigurationNameExpr -Value funcBody

            // create function:\confignameexpression
            var funcDriveStrExpr = new StringConstantExpressionAst(this.Extent, @"function:\",
                                                                   StringConstantType.BareWord);
            var funcPathStrExpr = new BinaryExpressionAst(this.Extent, funcDriveStrExpr,
                                                          TokenKind.Plus,
                                                          (ExpressionAst)InstanceName.Copy(),
                                                          this.Extent);

            var setItemCmdElements = new Collection<CommandElementAst>
            {
                new StringConstantExpressionAst(this.Extent, @"set-item",
                                                StringConstantType.BareWord),
                new CommandParameterAst(this.Extent, "Path",
                                        funcPathStrExpr,
                                        this.Extent),
                new CommandParameterAst(this.Extent, "Value", funcBodyExp,
                                        this.Extent)
            };

            // then the configuration scriptblock as function body

            var setItemCmdlet = new CommandAst(this.Extent, setItemCmdElements, TokenKind.Unknown, null);
            #endregion

            var returnPipelineAst = new PipelineAst(this.Extent, setItemCmdlet, background: false);

            SetParent(returnPipelineAst);

            return returnPipelineAst;
        }

        #endregion

        #region static fields/methods

        /// <summary>
        /// </summary>
        /// <param name="stmt"></param>
        /// <param name="resourceModulePairsToImport">Item1 - ResourceName, Item2 - ModuleName, Item3 - ModuleVersion.</param>
        /// <returns></returns>
        private static bool IsImportCommand(StatementAst stmt, List<Tuple<string[], ModuleSpecification[], Version>> resourceModulePairsToImport)
        {
            var dkwsAst = stmt as DynamicKeywordStatementAst;
            if (dkwsAst == null || !dkwsAst.Keyword.Keyword.Equals("Import-DscResource", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var commandAst = new CommandAst(dkwsAst.Extent, CopyElements(dkwsAst.CommandElements), TokenKind.Unknown, null);

            StaticBindingResult bindingResult = StaticParameterBinder.BindCommand(commandAst, false);
            ParameterBindingResult moduleNames = null;
            ParameterBindingResult resourceNames = null;
            ParameterBindingResult moduleVersion = null;
            const string nameParam = "Name";
            const string moduleNameParam = "ModuleName";
            const string moduleVersionParam = "ModuleVersion";

            // we are only interested in Name and ModuleName parameter
            foreach (var entry in bindingResult.BoundParameters)
            {
                string paramName = entry.Key;
                var paramValue = entry.Value;

                if ((paramName.Length <= nameParam.Length) && (paramName.AsSpan().Equals(nameParam.AsSpan(0, paramName.Length), StringComparison.OrdinalIgnoreCase)))
                {
                    resourceNames = paramValue;
                }
                // Since both parameters -ModuleName and -ModuleVersion has same start string i.e. Module so we will try to resolve it to -ModuleName
                // if user specifies like -Module
                if ((paramName.Length <= moduleNameParam.Length) && (paramName.AsSpan().Equals(moduleNameParam.AsSpan(0, paramName.Length), StringComparison.OrdinalIgnoreCase)))
                {
                    moduleNames = paramValue;
                }
                else if ((paramName.Length <= moduleVersionParam.Length) && (paramName.AsSpan().Equals(moduleVersionParam.AsSpan(0, paramName.Length), StringComparison.OrdinalIgnoreCase)))
                {
                    moduleVersion = paramValue;
                }
            }

            string[] resourceNamesTyped = new[] { "*" };
            ModuleSpecification[] moduleNamesTyped = null;
            Version moduleVersionTyped = null;

            if (resourceNames != null)
            {
                object resourceNameEvaluated;
                IsConstantValueVisitor.IsConstant(resourceNames.Value, out resourceNameEvaluated, true, true);
                resourceNamesTyped = LanguagePrimitives.ConvertTo<string[]>(resourceNameEvaluated);
            }

            if (moduleNames != null)
            {
                object moduleNameEvaluated;
                IsConstantValueVisitor.IsConstant(moduleNames.Value, out moduleNameEvaluated, true, true);
                moduleNamesTyped = LanguagePrimitives.ConvertTo<ModuleSpecification[]>(moduleNameEvaluated);
            }

            if (moduleVersion != null)
            {
                object moduleVersionEvaluated;
                IsConstantValueVisitor.IsConstant(moduleVersion.Value, out moduleVersionEvaluated, true, true);
                if (moduleVersionEvaluated is double)
                {
                    // this happens in case -ModuleVersion 1.0, then use extent text for that.
                    // The better way to do it would be define static binding API against CommandInfo, that holds information about parameter types.
                    // This way, we can avoid this ugly special-casing and say that -ModuleVersion has type [System.Version].
                    moduleVersionEvaluated = moduleVersion.Value.Extent.Text;
                }

                moduleVersionTyped = LanguagePrimitives.ConvertTo<Version>(moduleVersionEvaluated);

                // Use -ModuleVersion <Version> only in the case, if -ModuleName specified.
                // Override ModuleName versions.
                if (moduleNamesTyped != null && moduleNamesTyped.Length == 1)
                {
                    for (int i = 0; i < moduleNamesTyped.Length; i++)
                    {
                        moduleNamesTyped[i] = new ModuleSpecification(new Hashtable()
                        {
                            {"ModuleName", moduleNamesTyped[i].Name},
                            {"ModuleVersion", moduleVersionTyped}
                        });
                    }
                }
            }

            resourceModulePairsToImport.Add(new Tuple<string[], ModuleSpecification[], Version>(resourceNamesTyped, moduleNamesTyped, moduleVersionTyped));
            return true;
        }

        private const string ConfigurationBuildInParametersStr = @"
                        [cmdletbinding()]
                        param(
                            [string]
                                $InstanceName,
                            [string[]]
                                $DependsOn,
                            [PSCredential]
                                $PsDscRunAsCredential,
                            [string]
                                $OutputPath,
                            [hashtable]
                            [Microsoft.PowerShell.DesiredStateConfiguration.ArgumentToConfigurationDataTransformation()]
                               $ConfigurationData
                        )";

        private static IEnumerable<ParameterAst> ConfigurationBuildInParameters
        {
            get
            {
                if (s_configurationBuildInParameters == null)
                {
                    s_configurationBuildInParameters = new List<ParameterAst>();

                    Token[] tokens;
                    ParseError[] errors;
                    var sba = Parser.ParseInput(ConfigurationBuildInParametersStr, out tokens, out errors);
                    if (sba != null)
                    {
                        foreach (var parameterAst in sba.ParamBlock.Parameters)
                        {
                            s_configurationBuildInParameters.Add((ParameterAst)parameterAst.Copy());
                        }
                    }
                }

                return s_configurationBuildInParameters;
            }
        }

        private static List<ParameterAst> s_configurationBuildInParameters;

        private static IEnumerable<AttributeAst> ConfigurationBuildInParameterAttribAsts
        {
            get
            {
                if (s_configurationBuildInParameterAttrAsts == null)
                {
                    s_configurationBuildInParameterAttrAsts = new List<AttributeAst>();

                    Token[] tokens;
                    ParseError[] errors;
                    var sba = Parser.ParseInput(ConfigurationBuildInParametersStr, out tokens, out errors);
                    if (sba != null)
                    {
                        if (s_configurationBuildInParameters == null)
                        {
                            s_configurationBuildInParameters = new List<ParameterAst>();

                            foreach (var parameterAst in sba.ParamBlock.Parameters)
                            {
                                s_configurationBuildInParameters.Add((ParameterAst)parameterAst.Copy());
                            }
                        }

                        foreach (var attribAst in sba.ParamBlock.Attributes)
                        {
                            s_configurationBuildInParameterAttrAsts.Add((AttributeAst)attribAst.Copy());
                        }
                    }
                }

                return s_configurationBuildInParameterAttrAsts;
            }
        }

        private static List<AttributeAst> s_configurationBuildInParameterAttrAsts;

        private static IEnumerable<StatementAst> ConfigurationExtraParameterStatements
        {
            get
            {
                if (s_configurationExtraParameterStatements == null)
                {
                    s_configurationExtraParameterStatements = new List<StatementAst>();
                    Token[] tokens;
                    ParseError[] errors;
                    var sba = Parser.ParseInput(@"
                        Import-Module Microsoft.PowerShell.Management -Verbose:$false
                        Import-Module PSDesiredStateConfiguration -Verbose:$false
                        $toBody = @{}+$PSBoundParameters
                        $toBody.Remove(""OutputPath"")
                        $toBody.Remove(""ConfigurationData"")
                        $ConfigurationData = $psboundparameters[""ConfigurationData""]
                        $Outputpath = $psboundparameters[""Outputpath""]", out tokens, out errors);
                    if (sba != null)
                    {
                        foreach (var statementAst in sba.EndBlock.Statements)
                        {
                            s_configurationExtraParameterStatements.Add((StatementAst)statementAst.Copy());
                        }
                    }
                }

                return s_configurationExtraParameterStatements;
            }
        }

        private static List<StatementAst> s_configurationExtraParameterStatements;
        #endregion

    }

    /// <summary>
    /// The ast represents the DynamicKeyword statement.
    /// </summary>
    public class DynamicKeywordStatementAst : StatementAst
    {
        /// <summary>
        /// Construct a DynamicKeyword statement.
        /// </summary>
        /// <param name="extent">
        /// The extent of the expression, starting with the attribute and ending after the expression being attributed.
        /// </param>
        /// <param name="commandElements">A collection of <see cref="CommandElementAst"/> used to invoke <see cref="DynamicKeyword"/> specific command.</param>
        /// <exception cref="PSArgumentException">
        /// If <paramref name="commandElements"/> is null or empty.
        /// </exception>
        public DynamicKeywordStatementAst(IScriptExtent extent,
            IEnumerable<CommandElementAst> commandElements) : base(extent)
        {
            if (commandElements == null || !commandElements.Any())
            {
                throw PSTraceSource.NewArgumentException(nameof(commandElements));
            }

            this.CommandElements = new ReadOnlyCollection<CommandElementAst>(commandElements.ToArray());
            SetParents(CommandElements);
        }

        /// <summary>
        /// A non-empty collection of command elements represent the content of the
        /// DynamicKeyword.
        /// It may represents a command, such as “Import-DSCResource”,
        /// or DSC resources, then CommandElements includes:
        ///   (1) Keyword Name
        ///   (2) InstanceName
        ///   (3) Body, could be ScriptBlockExpressionAst (for Node keyword) or a HashtableAst (remaining)
        ///
        /// This property is never null and never empty.
        /// </summary>
        public ReadOnlyCollection<CommandElementAst> CommandElements { get; }

        /// <summary>
        /// Duplicates the <see cref="DynamicKeywordStatementAst"/>, allowing it to be composed into other ASTs.
        /// </summary>
        /// <returns>A copy of the <see cref="DynamicKeywordStatementAst"/>, with the link to the previous parent removed.</returns>
        public override Ast Copy()
        {
            IEnumerable<CommandElementAst> commandElements = CopyElements(CommandElements);
            return new DynamicKeywordStatementAst(Extent, commandElements)
            {
                Keyword = this.Keyword,
                LCurly = this.LCurly,
                FunctionName = this.FunctionName,
                InstanceName = CopyElement(this.InstanceName),
                OriginalInstanceName = CopyElement(this.OriginalInstanceName),
                BodyExpression = CopyElement(this.BodyExpression),
                ElementName = this.ElementName,
            };
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            var visitor2 = visitor as ICustomAstVisitor2;
            return visitor2?.VisitDynamicKeywordStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = AstVisitAction.Continue;
            var visitor2 = visitor as AstVisitor2;
            if (visitor2 != null)
            {
                action = visitor2.VisitDynamicKeywordStatement(this);
                if (action == AstVisitAction.SkipChildren)
                    return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            }

            if (action == AstVisitAction.Continue)
            {
                foreach (CommandElementAst elementAst in CommandElements)
                {
                    action = elementAst.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue)
                        break;
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        #region Internal Properties/Methods

        internal DynamicKeyword Keyword
        {
            get
            {
                return _keyword;
            }

            set
            {
                _keyword = value.Copy();
            }
        }

        private DynamicKeyword _keyword;

        internal Token LCurly { get; set; }

        internal Token FunctionName { get; set; }

        internal ExpressionAst InstanceName { get; set; }

        internal ExpressionAst OriginalInstanceName { get; set; }

        internal ExpressionAst BodyExpression { get; set; }

        internal string ElementName { get; set; }

        private PipelineAst _commandCallPipelineAst;

        internal PipelineAst GenerateCommandCallPipelineAst()
        {
            if (_commandCallPipelineAst != null)
                return _commandCallPipelineAst;

            /////////////////////////////////////////////////////////////////////////
            //
            // Now construct the AST to call the function that defines implements the keywords logic. There are
            // two different types of ASTs that may be generated, depending on the settings in the DynamicKeyword object.
            // The first type uses a fixed set of 4 arguments and has the command with the signature
            //  function moduleName\keyword
            //  {
            //      param (
            //          $KeywordData,       # the DynamicKeyword object processed by this rule.
            //          $Name,              # the value of the name expression syntax element. This may be null for keywords that don't take a name
            //          $Value,             # the body of the keyword - either a hashtable or a scriptblock
            //          $SourceMetadata     # a string containing the original source line information so that errors
            //      )
            //      # function logic...
            //  }
            //
            // The second type, where the DirectCall flag is set to true, simply calls the command directly.
            // In the original source, the keyword body will be a property collection where the allowed properties
            // in the collection correspond to the parameters on the actual called function.
            //
            var cea = new Collection<CommandElementAst>();

            //
            // First add the name of the command to call. If a module name has been provided
            // then use the module-qualified form of the command name..
            //
            if (string.IsNullOrEmpty(Keyword.ImplementingModule))
            {
                cea.Add(
                    new StringConstantExpressionAst(
                        FunctionName.Extent,
                        FunctionName.Text,
                        StringConstantType.BareWord));
            }
            else
            {
                cea.Add(
                    new StringConstantExpressionAst(
                        FunctionName.Extent,
                        Keyword.ImplementingModule + '\\' + FunctionName.Text,
                        StringConstantType.BareWord));
            }

            ExpressionAst expr = BodyExpression;
            HashtableAst hashtable = expr as HashtableAst;
            if (Keyword.DirectCall)
            {
                // If this keyword takes a name, then add it as the parameter -InstanceName
                if (Keyword.NameMode != DynamicKeywordNameMode.NoName)
                {
                    cea.Add(
                        new CommandParameterAst(
                            FunctionName.Extent,
                            "InstanceName",
                            InstanceName,
                            FunctionName.Extent));
                }

                //
                // If it's a direct call keyword, then we just unravel the properties
                // in the hash literal expression and map them to parameters.
                // We've already checked to make sure that they're all valid names.
                //
                if (hashtable != null)
                {
                    bool isHashtableValid = true;
                    //
                    // If it's a hash table, validate that only valid members have been specified.
                    //
                    foreach (var keyValueTuple in hashtable.KeyValuePairs)
                    {
                        var propName = keyValueTuple.Item1 as StringConstantExpressionAst;
                        if (propName == null)
                        {
                            isHashtableValid = false;
                            break;
                        }
                        else
                        {
                            if (!Keyword.Properties.ContainsKey(propName.Value))
                            {
                                isHashtableValid = false;
                                break;
                            }
                        }

                        if (keyValueTuple.Item2 is ErrorStatementAst)
                        {
                            isHashtableValid = false;
                            break;
                        }
                    }

                    if (isHashtableValid)
                    {
                        // Construct the real parameters if Hashtable is valid
                        foreach (var keyValueTuple in hashtable.KeyValuePairs)
                        {
                            var propName = (StringConstantExpressionAst)keyValueTuple.Item1;
                            ExpressionAst propValue = new SubExpressionAst(
                                FunctionName.Extent,
                                new StatementBlockAst(
                                    FunctionName.Extent,
                                    new StatementAst[] { (StatementAst)keyValueTuple.Item2.Copy() }, null));

                            cea.Add(
                                new CommandParameterAst(
                                    FunctionName.Extent,
                                    propName.Value,
                                    propValue,
                                    LCurly.Extent));
                        }
                    }
                    else
                    {
                        // Construct a fake parameter with the HashtableAst to be its value, so that
                        // tab completion on the property names would work.
                        cea.Add(
                            new CommandParameterAst(
                                FunctionName.Extent,
                                "InvalidPropertyHashtable",
                                hashtable,
                                LCurly.Extent));
                    }
                }
            }
            else
            {
                //
                // Add the -KeywordData parameter using the expression
                //  ([type]("System.Management.Automation.Language.DynamicKeyword"))::GetKeyword(name)
                // To invoke the method to get the keyword data object for that keyword.
                //
                var indexExpr = new InvokeMemberExpressionAst(
                    FunctionName.Extent,
                    new TypeExpressionAst(
                        FunctionName.Extent,
                        new TypeName(
                            FunctionName.Extent,
                            typeof(System.Management.Automation.Language.DynamicKeyword).FullName)),
                    new StringConstantExpressionAst(
                        FunctionName.Extent,
                        "GetKeyword",
                        StringConstantType.BareWord),
                    new List<ExpressionAst>
                        {
                            new StringConstantExpressionAst(
                                FunctionName.Extent,
                                FunctionName.Text,
                                StringConstantType.BareWord)
                        },
                    true);

                cea.Add(
                    new CommandParameterAst(
                        FunctionName.Extent,
                        "KeywordData",
                        indexExpr,
                        LCurly.Extent));

                //
                // Add the -Name parameter
                //
                cea.Add(
                    new CommandParameterAst(
                        FunctionName.Extent,
                        "Name",
                        InstanceName,
                        LCurly.Extent));

                //
                // Add the -Value parameter
                //
                cea.Add(
                    new CommandParameterAst(
                        LCurly.Extent,
                        "Value",
                        expr,
                        LCurly.Extent));

                //
                // Add the -SourceMetadata parameter
                //
                string sourceMetadata = FunctionName.Extent.File
                                        + "::" + FunctionName.Extent.StartLineNumber
                                        + "::" + FunctionName.Extent.StartColumnNumber
                                        + "::" + FunctionName.Extent.Text;
                cea.Add(
                    new CommandParameterAst(
                        LCurly.Extent, "SourceMetadata",
                        new StringConstantExpressionAst(
                            FunctionName.Extent,
                            sourceMetadata,
                            StringConstantType.BareWord),
                        LCurly.Extent));
            }

            //
            // Build the final statement - a pipeline containing a single element with is a CommandAst
            // containing the command we've built up.
            //
            var cmdAst = new CommandAst(FunctionName.Extent, cea, TokenKind.Unknown, null);
            cmdAst.DefiningKeyword = Keyword;
            _commandCallPipelineAst = new PipelineAst(FunctionName.Extent, cmdAst, background: false);
            return _commandCallPipelineAst;
        }

        #endregion
    }

    #endregion Statements

    #region Expressions

    /// <summary>
    /// An abstract base class that represents all PowerShell expressions.
    /// </summary>
    public abstract class ExpressionAst : CommandElementAst
    {
        /// <summary>
        /// Initialize the fields common to all expressions.
        /// </summary>
        /// <param name="extent">The extent of the expression.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        protected ExpressionAst(IScriptExtent extent)
            : base(extent)
        {
        }

        /// <summary>
        /// By default, the static type of an expression is unknown and hence we assume <c>typeof(object)</c>.
        /// </summary>
        public virtual Type StaticType { get { return typeof(object); } }

        /// <summary>
        /// Determine if the results of ParenExpression/SubExpression should be preserved in case of exception.
        /// </summary>
        /// <remarks>
        /// We should preserve the partial output in case of exception only if the SubExpression/ParenExpression meets following conditions:
        ///  1. the SubExpr/ParenExpr is the first expression, and the only element in a pipeline
        ///  2. the pipeline's parent is a StatementBlockAst or NamedBlockAst. e.g. $(1; throw 2) OR if (true) { $(1; throw 2) }
        /// </remarks>
        /// <returns></returns>
        internal virtual bool ShouldPreserveOutputInCaseOfException()
        {
            var parenExpr = this as ParenExpressionAst;
            var subExpr = this as SubExpressionAst;

            if (parenExpr == null && subExpr == null)
            {
                PSTraceSource.NewInvalidOperationException();
            }

            if (!(this.Parent is CommandExpressionAst commandExpr))
            {
                return false;
            }

            var pipelineAst = commandExpr.Parent as PipelineAst;
            if (pipelineAst == null || pipelineAst.PipelineElements.Count > 1)
            {
                return false;
            }

            var parenExpressionAst = pipelineAst.Parent as ParenExpressionAst;
            if (parenExpressionAst != null)
            {
                return parenExpressionAst.ShouldPreserveOutputInCaseOfException();
            }
            else
            {
                return (pipelineAst.Parent is StatementBlockAst || pipelineAst.Parent is NamedBlockAst);
            }
        }
    }

    /// <summary>
    /// The ast representing a ternary expression, e.g. <c>$a ? 1 : 2</c>.
    /// </summary>
    public class TernaryExpressionAst : ExpressionAst
    {
        /// <summary>
        /// Initializes a new instance of the a ternary expression.
        /// </summary>
        /// <param name="extent">The extent of the expression.</param>
        /// <param name="condition">The condition operand.</param>
        /// <param name="ifTrue">The if clause.</param>
        /// <param name="ifFalse">The else clause.</param>
        public TernaryExpressionAst(IScriptExtent extent, ExpressionAst condition, ExpressionAst ifTrue, ExpressionAst ifFalse)
            : base(extent)
        {
            Condition = condition ?? throw PSTraceSource.NewArgumentNullException(nameof(condition));
            IfTrue = ifTrue ?? throw PSTraceSource.NewArgumentNullException(nameof(ifTrue));
            IfFalse = ifFalse ?? throw PSTraceSource.NewArgumentNullException(nameof(ifFalse));

            SetParent(Condition);
            SetParent(IfTrue);
            SetParent(IfFalse);
        }

        /// <summary>
        /// Gets the ast for the condition of the ternary expression. The property is never null.
        /// </summary>
        public ExpressionAst Condition { get; }

        /// <summary>
        /// Gets the ast for the if-operand of the ternary expression. The property is never null.
        /// </summary>
        public ExpressionAst IfTrue { get; }

        /// <summary>
        /// Gets the ast for the else-operand of the ternary expression. The property is never null.
        /// </summary>
        public ExpressionAst IfFalse { get; }

        /// <summary>
        /// Copy the TernaryExpressionAst instance.
        /// </summary>
        /// <return>
        /// Retirns a copy of the ast.
        /// </return>
        public override Ast Copy()
        {
            ExpressionAst newCondition = CopyElement(this.Condition);
            ExpressionAst newIfTrue = CopyElement(this.IfTrue);
            ExpressionAst newIfFalse = CopyElement(this.IfFalse);
            return new TernaryExpressionAst(this.Extent, newCondition, newIfTrue, newIfFalse);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            if (visitor is ICustomAstVisitor2 visitor2)
            {
                return visitor2.VisitTernaryExpression(this);
            }

            return null;
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = AstVisitAction.Continue;
            if (visitor is AstVisitor2 visitor2)
            {
                action = visitor2.VisitTernaryExpression(this);
                if (action == AstVisitAction.SkipChildren)
                {
                    return visitor.CheckForPostAction(this, AstVisitAction.Continue);
                }
            }

            if (action == AstVisitAction.Continue)
            {
                action = Condition.InternalVisit(visitor);
            }

            if (action == AstVisitAction.Continue)
            {
                action = IfTrue.InternalVisit(visitor);
            }

            if (action == AstVisitAction.Continue)
            {
                action = IfFalse.InternalVisit(visitor);
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast representing a binary expression, e.g. <c>$a + $b</c>.
    /// </summary>
    public class BinaryExpressionAst : ExpressionAst
    {
        /// <summary>
        /// Initializes a new instance of the binary expression.
        /// </summary>
        /// <param name="extent">The extent of the expression.</param>
        /// <param name="left">The left hand operand.</param>
        /// <param name="operator">The binary operator.</param>
        /// <param name="right">The right hand operand.</param>
        /// <param name="errorPosition">
        /// The position to report if an error occurs at runtime while evaluating the binary operation.
        /// </param>
        /// <exception cref="PSArgumentException">
        /// If <paramref name="operator"/> is not a valid binary operator.
        /// </exception>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/>, <paramref name="left"/>, <paramref name="right"/>,
        /// or <paramref name="errorPosition"/> is null.
        /// </exception>
        public BinaryExpressionAst(IScriptExtent extent, ExpressionAst left, TokenKind @operator, ExpressionAst right, IScriptExtent errorPosition)
            : base(extent)
        {
            if ((@operator.GetTraits() & TokenFlags.BinaryOperator) == 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(@operator));
            }

            if (left == null || right == null || errorPosition == null)
            {
                throw PSTraceSource.NewArgumentNullException(left == null ? "left" : right == null ? "right" : "errorPosition");
            }

            this.Left = left;
            SetParent(left);
            this.Operator = @operator;
            this.Right = right;
            SetParent(right);
            this.ErrorPosition = errorPosition;
        }

        /// <summary>
        /// The operator token kind.  The value returned is always a binary operator.
        /// </summary>
        public TokenKind Operator { get; }

        /// <summary>
        /// The ast for the left hand side of the binary expression.  The property is never null.
        /// </summary>
        public ExpressionAst Left { get; }

        /// <summary>
        /// The ast for the right hand side of the binary expression.  The property is never null.
        /// </summary>
        public ExpressionAst Right { get; }

        /// <summary>
        /// The position to report an error if an error occurs at runtime.  The property is never null.
        /// </summary>
        public IScriptExtent ErrorPosition { get; }

        /// <summary>
        /// Copy the BinaryExpressionAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newLeft = CopyElement(this.Left);
            var newRight = CopyElement(this.Right);
            return new BinaryExpressionAst(this.Extent, newLeft, this.Operator, newRight, this.ErrorPosition);
        }

        /// <summary>
        /// The result type of the operation.  For most binary operators, the type is unknown until runtime, but
        /// xor always results in <c>typeof(bool)</c>.
        /// </summary>
        public override Type StaticType
        {
            get
            {
                switch (Operator)
                {
                    case TokenKind.Xor:
                    case TokenKind.And:
                    case TokenKind.Or:
                    case TokenKind.Is:
                        return typeof(bool);
                }

                return typeof(object);
            }
        }

        internal static readonly PSTypeName[] BoolTypeNameArray = new PSTypeName[] { new PSTypeName(typeof(bool)) };

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitBinaryExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitBinaryExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Left.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Right.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast representing an expression with a unary operator.
    /// </summary>
    public class UnaryExpressionAst : ExpressionAst
    {
        /// <summary>
        /// Construct a unary expression.
        /// </summary>
        /// <param name="extent">The extent of the expression, including the operator (which may be prefix or postfix.).</param>
        /// <param name="tokenKind">The unary operator token kind for the operation.</param>
        /// <param name="child">The expression that the unary operator is applied to.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="child"/> is null.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// If <paramref name="tokenKind"/> is not a valid unary operator.
        /// </exception>
        public UnaryExpressionAst(IScriptExtent extent, TokenKind tokenKind, ExpressionAst child)
            : base(extent)
        {
            if ((tokenKind.GetTraits() & TokenFlags.UnaryOperator) == 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(tokenKind));
            }

            if (child == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(child));
            }

            this.TokenKind = tokenKind;
            this.Child = child;
            SetParent(child);
        }

        /// <summary>
        /// The operator token for the unary expression.  The value returned is always a unary operator.
        /// </summary>
        public TokenKind TokenKind { get; }

        /// <summary>
        /// The child expression the unary operator is applied to.  The property is never null.
        /// </summary>
        public ExpressionAst Child { get; }

        /// <summary>
        /// Copy the UnaryExpressionAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newChild = CopyElement(this.Child);
            return new UnaryExpressionAst(this.Extent, this.TokenKind, newChild);
        }

        /// <summary>
        /// Returns <c>typeof(bool)</c> if the unary operator is a logical negation, otherwise returns <c>typeof(object)</c>.
        /// </summary>
        public override Type StaticType
        {
            get
            {
                return (TokenKind == TokenKind.Not || TokenKind == TokenKind.Exclaim)
                    ? typeof(bool)
                    : typeof(object);
            }
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitUnaryExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitUnaryExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Child.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast that represents a scriptblock with a keyword name. This is normally allowed only for script workflow.
    /// e.g. <c>parallel { ... }</c> or <c>sequence { ... }</c>.
    /// </summary>
    public class BlockStatementAst : StatementAst
    {
        /// <summary>
        /// Construct a keyword block expression.
        /// </summary>
        /// <param name="extent"></param>
        /// <param name="kind"></param>
        /// <param name="body"></param>
        public BlockStatementAst(IScriptExtent extent, Token kind, StatementBlockAst body)
            : base(extent)
        {
            if (kind == null || body == null)
            {
                throw PSTraceSource.NewArgumentNullException(kind == null ? "kind" : "body");
            }

            if (kind.Kind != TokenKind.Sequence && kind.Kind != TokenKind.Parallel)
            {
                throw PSTraceSource.NewArgumentException(nameof(kind));
            }

            this.Kind = kind;
            this.Body = body;
            SetParent(body);
        }

        /// <summary>
        /// The scriptblockexpression that has a keyword applied to it. This property is nerver null.
        /// </summary>
        public StatementBlockAst Body { get; }

        /// <summary>
        /// The keyword name.
        /// </summary>
        public Token Kind { get; }

        /// <summary>
        /// Copy the BlockStatementAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newBody = CopyElement(this.Body);
            return new BlockStatementAst(this.Extent, this.Kind, newBody);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitBlockStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitBlockStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Body.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast that represents an expression with an attribute.  This is normally allowed only on parameters or variables
    /// being assigned, e.g. <c>[Parameter()]$PassThru</c> or <c>[ValidateScript({$true})$abc = 42</c>.
    /// </summary>
    public class AttributedExpressionAst : ExpressionAst, ISupportsAssignment, IAssignableValue
    {
        /// <summary>
        /// Construct an attributed expression.
        /// </summary>
        /// <param name="extent">
        /// The extent of the expression, starting with the attribute and ending after the expression being attributed.
        /// </param>
        /// <param name="attribute">The attribute being applied to <paramref name="child"/></param>
        /// <param name="child">The expression being attributed by <paramref name="attribute"/></param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/>, <paramref name="attribute"/>, or <paramref name="child"/> is null.
        /// </exception>
        public AttributedExpressionAst(IScriptExtent extent, AttributeBaseAst attribute, ExpressionAst child)
            : base(extent)
        {
            if (attribute == null || child == null)
            {
                throw PSTraceSource.NewArgumentNullException(attribute == null ? "attribute" : "child");
            }

            this.Attribute = attribute;
            SetParent(attribute);
            this.Child = child;
            SetParent(child);
        }

        /// <summary>
        /// The expression that has an attribute or type constraint applied to it.  This property is never null.
        /// </summary>
        public ExpressionAst Child { get; }

        /// <summary>
        /// The attribute or type constraint for this expression.  This property is never null.
        /// </summary>
        public AttributeBaseAst Attribute { get; }

        /// <summary>
        /// Copy the AttributedExpressionAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newAttribute = CopyElement(this.Attribute);
            var newChild = CopyElement(this.Child);
            return new AttributedExpressionAst(this.Extent, newAttribute, newChild);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitAttributedExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitAttributedExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Attribute.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Child.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        #region Code Generation Details

        private ISupportsAssignment GetActualAssignableAst()
        {
            ExpressionAst child = this;
            var childAttributeAst = child as AttributedExpressionAst;
            while (childAttributeAst != null)
            {
                child = childAttributeAst.Child;
                childAttributeAst = child as AttributedExpressionAst;
            }

            // Semantic checks ensure this cast succeeds
            return (ISupportsAssignment)child;
        }

        private List<AttributeBaseAst> GetAttributes()
        {
            var attributes = new List<AttributeBaseAst>();
            var childAttributeAst = this;
            while (childAttributeAst != null)
            {
                attributes.Add(childAttributeAst.Attribute);
                childAttributeAst = childAttributeAst.Child as AttributedExpressionAst;
            }

            attributes.Reverse();
            return attributes;
        }

        IAssignableValue ISupportsAssignment.GetAssignableValue()
        {
            return this;
        }

        Expression IAssignableValue.GetValue(Compiler compiler, List<Expression> exprs, List<ParameterExpression> temps)
        {
            return (Expression)this.Accept(compiler);
        }

        Expression IAssignableValue.SetValue(Compiler compiler, Expression rhs)
        {
            var attributes = GetAttributes();
            var assignableValue = GetActualAssignableAst().GetAssignableValue();

            if (!(assignableValue is VariableExpressionAst variableExpr))
            {
                return assignableValue.SetValue(compiler, Compiler.ConvertValue(rhs, attributes));
            }

            return Compiler.CallSetVariable(Expression.Constant(variableExpr.VariablePath), rhs, Expression.Constant(attributes.ToArray()));
        }

        #endregion Code Generation Details
    }

    /// <summary>
    /// The ast that represents a cast expression, e.g. <c>[wmiclass]"Win32_Process"</c>.
    /// </summary>
    public class ConvertExpressionAst : AttributedExpressionAst, ISupportsAssignment
    {
        /// <summary>
        /// Construct a cast expression.
        /// </summary>
        /// <param name="extent">
        /// The extent of the expression, starting with the type literal and ending after the expression being converted.
        /// </param>
        /// <param name="typeConstraint">The type to convert to.</param>
        /// <param name="child">The expression being converted.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/>, <paramref name="typeConstraint"/>, or <paramref name="child"/> is null.
        /// </exception>
        public ConvertExpressionAst(IScriptExtent extent, TypeConstraintAst typeConstraint, ExpressionAst child)
            : base(extent, typeConstraint, child)
        {
        }

        /// <summary>
        /// The type to convert to.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public TypeConstraintAst Type { get { return (TypeConstraintAst)Attribute; } }

        /// <summary>
        /// Copy the ConvertExpressionAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newTypeConstraint = CopyElement(this.Type);
            var newChild = CopyElement(this.Child);
            return new ConvertExpressionAst(this.Extent, newTypeConstraint, newChild);
        }

        /// <summary>
        /// The static type produced after the cast is normally the type named by <see cref="Type"/>, but in some cases
        /// it may not be, in which, <see cref="Object"/> is assumed.
        /// </summary>
        public override Type StaticType
        {
            get { return this.Type.TypeName.GetReflectionType() ?? typeof(object); }
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitConvertExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitConvertExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Type.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Child.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        #region Code Generation Details

        IAssignableValue ISupportsAssignment.GetAssignableValue()
        {
            var varExpr = Child as VariableExpressionAst;
            if (varExpr != null && varExpr.TupleIndex >= 0)
            {
                // In the common case of a single cast on the lhs of an assignment, we may have saved the type of the
                // variable in the mutable tuple, so conversions will get generated elsewhere, and we can just use
                // the child as the assignable value.
                return varExpr;
            }

            return this;
        }

        internal bool IsRef()
        {
            return Type.TypeName.Name.Equals("ref", StringComparison.OrdinalIgnoreCase);
        }
        #endregion Code Generation Details
    }

    /// <summary>
    /// The ast that represents accessing a member as a property, e.g. <c>$x.Length</c> or <c>[int]::MaxValue</c>.
    /// Most often this is a simple property access, but methods can also be access in this manner, returning an object
    /// that supports invoking that member.
    /// </summary>
    public class MemberExpressionAst : ExpressionAst, ISupportsAssignment
    {
        /// <summary>
        /// Construct an ast to reference a property.
        /// </summary>
        /// <param name="extent">
        /// The extent of the expression, starting with the expression before the operator '.' or '::' and ending after
        /// membername or expression naming the member.
        /// </param>
        /// <param name="expression">The expression before the member access operator '.' or '::'.</param>
        /// <param name="member">The name or expression naming the member to access.</param>
        /// <param name="static">True if the '::' operator was used, false if '.' is used.
        /// True if the member access is for a static member, using '::', false if accessing a member on an instance using '.'.
        /// </param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/>, <paramref name="expression"/>, or <paramref name="member"/> is null.
        /// </exception>
        public MemberExpressionAst(IScriptExtent extent, ExpressionAst expression, CommandElementAst member, bool @static)
            : base(extent)
        {
            if (expression == null || member == null)
            {
                throw PSTraceSource.NewArgumentNullException(expression == null ? "expression" : "member");
            }

            this.Expression = expression;
            SetParent(expression);
            this.Member = member;
            SetParent(member);
            this.Static = @static;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemberExpressionAst"/> class.
        /// </summary>
        /// <param name="extent">
        /// The extent of the expression, starting with the expression before the operator '.', '::' or '?.' and ending after
        /// membername or expression naming the member.
        /// </param>
        /// <param name="expression">The expression before the member access operator '.', '::' or '?.'.</param>
        /// <param name="member">The name or expression naming the member to access.</param>
        /// <param name="static">True if the '::' operator was used, false if '.' or '?.' is used.</param>
        /// <param name="nullConditional">True if '?.' used.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/>, <paramref name="expression"/>, or <paramref name="member"/> is null.
        /// </exception>
        public MemberExpressionAst(IScriptExtent extent, ExpressionAst expression, CommandElementAst member, bool @static, bool nullConditional)
            : this(extent, expression, member, @static)
        {
            this.NullConditional = nullConditional;
        }

        /// <summary>
        /// The expression that produces the value to retrieve the member from.  This property is never null.
        /// </summary>
        public ExpressionAst Expression { get; }

        /// <summary>
        /// The name of the member to retrieve.  This property is never null.
        /// </summary>
        public CommandElementAst Member { get; }

        /// <summary>
        /// True if the member to return is static, false if the member is an instance member.
        /// </summary>
        public bool Static { get; }

        /// <summary>
        /// Gets a value indicating true if the operator used is ?. or ?[].
        /// </summary>
        public bool NullConditional { get; protected set; }

        /// <summary>
        /// Copy the MemberExpressionAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newExpression = CopyElement(this.Expression);
            var newMember = CopyElement(this.Member);
            return new MemberExpressionAst(this.Extent, newExpression, newMember, this.Static, this.NullConditional);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitMemberExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitMemberExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Expression.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Member.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        IAssignableValue ISupportsAssignment.GetAssignableValue()
        {
            return new MemberAssignableValue { MemberExpression = this };
        }
    }

    /// <summary>
    /// The ast that represents the invocation of a method, e.g. <c>$sb.Append('abc')</c> or <c>[math]::Sign($i)</c>.
    /// </summary>
    public class InvokeMemberExpressionAst : MemberExpressionAst, ISupportsAssignment
    {
        /// <summary>
        /// Construct an instance of a method invocation expression.
        /// </summary>
        /// <param name="extent">
        /// The extent of the expression, starting with the expression before the invocation operator and ending with the
        /// closing paren after the arguments.
        /// </param>
        /// <param name="expression">The expression before the invocation operator ('.', '::').</param>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arguments">The arguments to pass to the method.</param>
        /// <param name="static">
        /// True if the invocation is for a static method, using '::', false if invoking a method on an instance using '.'.
        /// </param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        public InvokeMemberExpressionAst(IScriptExtent extent, ExpressionAst expression, CommandElementAst method, IEnumerable<ExpressionAst> arguments, bool @static)
            : base(extent, expression, method, @static)
        {
            if (arguments != null && arguments.Any())
            {
                this.Arguments = new ReadOnlyCollection<ExpressionAst>(arguments.ToArray());
                SetParents(Arguments);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeMemberExpressionAst"/> class.
        /// </summary>
        /// <param name="extent">
        /// The extent of the expression, starting with the expression before the invocation operator and ending with the
        /// closing paren after the arguments.
        /// </param>
        /// <param name="expression">The expression before the invocation operator ('.', '::' or '?.').</param>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arguments">The arguments to pass to the method.</param>
        /// <param name="static">
        /// True if the invocation is for a static method, using '::', false if invoking a method on an instance using '.' or '?.'.
        /// </param>
        /// <param name="nullConditional">True if the operator used is '?.'.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        public InvokeMemberExpressionAst(IScriptExtent extent, ExpressionAst expression, CommandElementAst method, IEnumerable<ExpressionAst> arguments, bool @static, bool nullConditional)
            : this(extent, expression, method, arguments, @static)
        {
            this.NullConditional = nullConditional;
        }

        /// <summary>
        /// The non-empty collection of arguments to pass when invoking the method, or null if no arguments were specified.
        /// </summary>
        public ReadOnlyCollection<ExpressionAst> Arguments { get; }

        /// <summary>
        /// Copy the InvokeMemberExpressionAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newExpression = CopyElement(this.Expression);
            var newMethod = CopyElement(this.Member);
            var newArguments = CopyElements(this.Arguments);
            return new InvokeMemberExpressionAst(this.Extent, newExpression, newMethod, newArguments, this.Static, this.NullConditional);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitInvokeMemberExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitInvokeMemberExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = this.InternalVisitChildren(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        internal AstVisitAction InternalVisitChildren(AstVisitor visitor)
        {
            var action = Expression.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Member.InternalVisit(visitor);
            if (action == AstVisitAction.Continue && Arguments != null)
            {
                for (int index = 0; index < Arguments.Count; index++)
                {
                    var arg = Arguments[index];
                    action = arg.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            return action;
        }

        #endregion Visitors

        IAssignableValue ISupportsAssignment.GetAssignableValue()
        {
            return new InvokeMemberAssignableValue { InvokeMemberExpressionAst = this };
        }
    }

    /// <summary>
    /// The ast that represents the invocation of a base ctor method from PS class instance ctor, e.g. <c>class B : A{ B() : base() {} }</c>.
    /// </summary>
    public class BaseCtorInvokeMemberExpressionAst : InvokeMemberExpressionAst
    {
        /// <summary>
        /// Construct an instance of a base ctor invocation expression.
        /// </summary>
        /// <param name="baseKeywordExtent">
        /// The extent of the base keyword, i.e. for
        /// <c>class B : A { B() : base(100) {} }</c>
        /// it will be "base".
        /// Can be empty extent (i.e. for implicit base ctor call).
        /// </param>
        /// <param name="baseCallExtent">
        /// The extent of the base ctor call expression, i.e. for
        /// <c>class B : A { B() : base(100) {} }</c>
        /// it will be "base(100)"
        /// Can be empty extent (i.e. for implicit base ctor call).
        /// </param>
        /// <param name="arguments">The arguments to pass to the ctor.</param>
        public BaseCtorInvokeMemberExpressionAst(IScriptExtent baseKeywordExtent, IScriptExtent baseCallExtent, IEnumerable<ExpressionAst> arguments)
            : base(
                baseCallExtent,
                new VariableExpressionAst(baseKeywordExtent, "this", false),
                new StringConstantExpressionAst(baseKeywordExtent, ".ctor", StringConstantType.BareWord),
                arguments,
                @static: false)
        {
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            AstVisitAction action = AstVisitAction.Continue;
            var visitor2 = visitor as AstVisitor2;
            if (visitor2 != null)
            {
                action = visitor2.VisitBaseCtorInvokeMemberExpression(this);
                if (action == AstVisitAction.SkipChildren)
                    return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            }

            if (action == AstVisitAction.Continue)
                action = this.InternalVisitChildren(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        internal override object Accept(ICustomAstVisitor visitor)
        {
            var visitor2 = visitor as ICustomAstVisitor2;
            return visitor2?.VisitBaseCtorInvokeMemberExpression(this);
        }
    }

    /// <summary>
    /// The name and attributes of a type.
    /// </summary>
#nullable enable
    public interface ITypeName
    {
        /// <summary>
        /// The full name of the type, including any namespace and assembly name.
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// The name of the type, including any namespace, but not including the assembly name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The name of the assembly, if specified, otherwise null.
        /// </summary>
        string? AssemblyName { get; }

        /// <summary>
        /// Returns true if the type names an array, false otherwise.
        /// </summary>
        bool IsArray { get; }

        /// <summary>
        /// Returns true if the type names a closed generic type (has generic type arguments), false otherwise.
        /// </summary>
        bool IsGeneric { get; }

        /// <summary>
        /// Returns the <see cref="System.Type"/> that this typename represents, if such a type exists, null otherwise.
        /// </summary>
        Type? GetReflectionType();

        /// <summary>
        /// Assuming the typename is an attribute, returns the <see cref="System.Type"/> that this typename represents.
        /// By convention, the typename may omit the suffix "Attribute".  Lookup will attempt to resolve the type as is,
        /// and if that fails, the suffix "Attribute" will be appended.
        /// </summary>
        Type? GetReflectionAttributeType();

        /// <summary>
        /// The extent of the typename.
        /// </summary>
        IScriptExtent Extent { get; }
    }
#nullable restore

#nullable enable
    internal interface ISupportsTypeCaching
    {
        Type? CachedType { get; set; }
    }
#nullable restore

    /// <summary>
    /// A simple type that is not an array or does not have generic arguments.
    /// </summary>
    public sealed class TypeName : ITypeName, ISupportsTypeCaching
    {
        internal readonly string _name;
        internal Type _type;
        internal readonly IScriptExtent _extent;
        internal TypeDefinitionAst _typeDefinitionAst;

        /// <summary>
        /// Construct a simple typename.
        /// </summary>
        /// <param name="extent">The extent of the typename.</param>
        /// <param name="name">The name of the type.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="name"/> is null or the empty string.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// If <paramref name="name"/> contains characters that are only allowed in a generic or array typename.
        /// </exception>
        public TypeName(IScriptExtent extent, string name)
        {
            if (extent == null || string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentNullException(extent == null ? "extent" : "name");
            }

            var c = name[0];
            if (c == '[' || c == ']' || c == ',')
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            int backtick = name.IndexOf('`');
            if (backtick != -1)
            {
                name = name.Replace("``", "`");
            }

            this._extent = extent;
            this._name = name;
        }

        /// <summary>
        /// Construct a typename with an assembly specification.
        /// </summary>
        /// <param name="extent">The extent of the typename.</param>
        /// <param name="name">The name of the type.</param>
        /// <param name="assembly">The assembly the type belongs to.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null or if <paramref name="name"/> or <paramref name="assembly"/> is null or the empty string.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// If <paramref name="name"/> contains characters that are only allowed in a generic or array typename.
        /// </exception>
        public TypeName(IScriptExtent extent, string name, string assembly)
            : this(extent, name)
        {
            if (string.IsNullOrEmpty(assembly))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(assembly));
            }

            AssemblyName = assembly;
        }

        /// <summary>
        /// Returns the full name of the type.
        /// </summary>
        public string FullName { get { return AssemblyName != null ? _name + "," + AssemblyName : _name; } }

        /// <summary>
        /// Returns the name of the type, w/o any assembly name if one was specified.
        /// </summary>
        public string Name { get { return _name; } }

        /// <summary>
        /// The name of the assembly, if specified, otherwise null.
        /// </summary>
        public string AssemblyName { get; internal set; }

        /// <summary>
        /// Always returns false, array typenames are instances of <see cref="ArrayTypeName"/>.
        /// </summary>
        public bool IsArray { get { return false; } }

        /// <summary>
        /// Always returns false, generic typenames are instances of <see cref="GenericTypeName"/>.
        /// </summary>
        public bool IsGeneric { get { return false; } }

        /// <summary>
        /// The extent of the typename.
        /// </summary>
        public IScriptExtent Extent { get { return _extent; } }

        internal bool HasDefaultCtor()
        {
            if (_typeDefinitionAst == null)
            {
                Type reflectionType = GetReflectionType();
                if (reflectionType == null)
                {
                    // we are pessimistic about default ctor presence.
                    return false;
                }

                return reflectionType.HasDefaultCtor();
            }

            bool hasExplicitCtor = false;
            foreach (var member in _typeDefinitionAst.Members)
            {
                var function = member as FunctionMemberAst;
                if (function != null)
                {
                    if (function.IsConstructor)
                    {
                        // TODO: add check for default values, once default values for parameters supported
                        if (function.Parameters.Count == 0)
                        {
                            return true;
                        }

                        hasExplicitCtor = true;
                    }
                }
            }
            // implicit ctor is default as well
            return !hasExplicitCtor;
        }

        /// <summary>
        /// Get the <see cref="Type"/> from a typename.
        /// </summary>
        /// <returns>
        /// The <see cref="Type"/> if possible, null otherwise.  Null may be returned for valid typenames if the assembly
        /// containing the type has not been loaded.
        /// </returns>
        public Type GetReflectionType()
        {
            if (_type == null)
            {
                Exception e;
                Type type = _typeDefinitionAst != null ? _typeDefinitionAst.Type : TypeResolver.ResolveTypeName(this, out e);
                if (type != null)
                {
                    try
                    {
                        var unused = type.TypeHandle;
                    }
                    catch (NotSupportedException)
                    {
                        // If the value of 'type' comes from typeBuilder.AsType(), then the value of 'type.GetTypeInfo()'
                        // is actually the TypeBuilder instance itself. This is the same on both FullCLR and CoreCLR.
                        Diagnostics.Assert(_typeDefinitionAst != null, "_typeDefinitionAst can never be null");
                        return type;
                    }

                    Interlocked.CompareExchange(ref _type, type, null);
                }
            }

            return _type;
        }

        /// <summary>
        /// Returns the <see cref="Type"/> this type represents, assuming the type is an attribute.  The suffix
        /// "Attribute" may be appended, if necessary, to resolve the type.
        /// </summary>
        /// <returns>
        /// The <see cref="Type"/> if possible, null otherwise.  Null may be returned for valid typenames if the assembly
        /// containing the type has not been loaded.
        /// </returns>
        public Type GetReflectionAttributeType()
        {
            var result = GetReflectionType();
            if (result == null || !typeof(Attribute).IsAssignableFrom(result))
            {
                var attrTypeName = new TypeName(_extent, FullName + "Attribute");
                result = attrTypeName.GetReflectionType();
                if (result != null && !typeof(Attribute).IsAssignableFrom(result))
                {
                    result = null;
                }
            }

            return result;
        }

        internal void SetTypeDefinition(TypeDefinitionAst typeDefinitionAst)
        {
            Diagnostics.Assert(_typeDefinitionAst == null, "Class definition is already set and cannot be changed");
            Diagnostics.Assert(_type == null, "Cannot set class definition if type is already resolved");
            _typeDefinitionAst = typeDefinitionAst;
        }

        /// <summary>
        /// Simply return the <see cref="FullName"/> of the type.
        /// </summary>
        public override string ToString()
        {
            return FullName;
        }

        /// <summary/>
        public override bool Equals(object obj)
        {
            if (!(obj is TypeName other))
                return false;

            if (!_name.Equals(other._name, StringComparison.OrdinalIgnoreCase))
                return false;

            if (AssemblyName == null)
                return other.AssemblyName == null;

            if (other.AssemblyName == null)
                return false;

            return AssemblyName.Equals(other.AssemblyName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary/>
        public override int GetHashCode()
        {
            var stringComparer = StringComparer.OrdinalIgnoreCase;
            var nameHashCode = stringComparer.GetHashCode(_name);
            if (AssemblyName == null)
                return nameHashCode;

            return Utils.CombineHashCodes(nameHashCode, stringComparer.GetHashCode(AssemblyName));
        }

        /// <summary>
        /// Check if the type names a <see cref="System.Type"/>, false otherwise.
        /// </summary>
        /// <param name="type">The given <see cref="System.Type"/></param>
        /// <returns>Returns true if the type names a <see cref="System.Type"/>, false otherwise.</returns>
        /// <remarks>
        ///  This helper function is now used to check 'Void' type only;
        ///  Other types may not work, for example, 'int'
        /// </remarks>
        internal bool IsType(Type type)
        {
            string fullTypeName = type.FullName;
            if (fullTypeName.Equals(Name, StringComparison.OrdinalIgnoreCase))
                return true;
            int lastDotIndex = fullTypeName.LastIndexOf('.');
            if (lastDotIndex >= 0)
            {
                return fullTypeName.AsSpan(lastDotIndex + 1).Equals(Name, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        Type ISupportsTypeCaching.CachedType
        {
            get { return _type; }

            set { _type = value; }
        }
    }

    /// <summary>
    /// Represent a closed generic type including its arguments.
    /// </summary>
    public sealed class GenericTypeName : ITypeName, ISupportsTypeCaching
    {
        private string _cachedFullName;
        private Type _cachedType;

        /// <summary>
        /// Construct a generic type name.
        /// </summary>
        /// <param name="extent">The extent of the generic typename.</param>
        /// <param name="genericTypeName">
        /// The name of the generic class.  The name does not need to include the backtick and number of expected arguments,
        /// (e.g. <c>System.Collections.Generic.Dictionary`2</c>, but the backtick and number be included.
        /// </param>
        /// <param name="genericArguments">
        /// The list of typenames that represent the arguments to the generic type named by <paramref name="genericTypeName"/>.
        /// </param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="genericTypeName"/> is null.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// If <paramref name="genericArguments"/> is null or if <paramref name="genericArguments"/> is an empty collection.
        /// </exception>
        public GenericTypeName(IScriptExtent extent, ITypeName genericTypeName, IEnumerable<ITypeName> genericArguments)
        {
            if (genericTypeName == null || extent == null)
            {
                throw PSTraceSource.NewArgumentNullException(extent == null ? "extent" : "genericTypeName");
            }

            if (genericArguments == null)
            {
                throw PSTraceSource.NewArgumentException(nameof(genericArguments));
            }

            Extent = extent;
            this.TypeName = genericTypeName;
            this.GenericArguments = new ReadOnlyCollection<ITypeName>(genericArguments.ToArray());

            if (this.GenericArguments.Count == 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(genericArguments));
            }
        }

        /// <summary>
        /// Return the typename, using PowerShell syntax for generic type arguments.
        /// </summary>
        public string FullName
        {
            get
            {
                if (_cachedFullName == null)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(TypeName.Name);
                    sb.Append('[');
                    bool first = true;
                    for (int index = 0; index < GenericArguments.Count; index++)
                    {
                        ITypeName typename = GenericArguments[index];
                        if (!first)
                        {
                            sb.Append(',');
                        }

                        first = false;
                        sb.Append(typename.FullName);
                    }

                    sb.Append(']');
                    var assemblyName = TypeName.AssemblyName;
                    if (assemblyName != null)
                    {
                        sb.Append(',');
                        sb.Append(assemblyName);
                    }

                    Interlocked.CompareExchange(ref _cachedFullName, sb.ToString(), null);
                }

                return _cachedFullName;
            }
        }

        /// <summary>
        /// The name of the type, including any namespace, but not including the assembly name, using PowerShell syntax for generic type arguments.
        /// </summary>
        public string Name
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(TypeName.Name);
                sb.Append('[');
                bool first = true;
                for (int index = 0; index < GenericArguments.Count; index++)
                {
                    ITypeName typename = GenericArguments[index];
                    if (!first)
                    {
                        sb.Append(',');
                    }

                    first = false;
                    sb.Append(typename.Name);
                }

                sb.Append(']');
                return sb.ToString();
            }
        }

        /// <summary>
        /// The name of the assembly, if specified, otherwise null.
        /// </summary>
        public string AssemblyName { get { return TypeName.AssemblyName; } }

        /// <summary>
        /// Always returns false because this class does not represent arrays.
        /// </summary>
        public bool IsArray { get { return false; } }

        /// <summary>
        /// Always returns true because this class represents generics.
        /// </summary>
        public bool IsGeneric { get { return true; } }

        /// <summary>
        /// The typename that specifies the generic class.
        /// </summary>
        public ITypeName TypeName { get; }

        /// <summary>
        /// The generic arguments for this typename.
        /// </summary>
        public ReadOnlyCollection<ITypeName> GenericArguments { get; }

        /// <summary>
        /// The extent of the typename.
        /// </summary>
        public IScriptExtent Extent { get; }

        /// <summary>
        /// Returns the <see cref="System.Type"/> that this typename represents, if such a type exists, null otherwise.
        /// </summary>
        public Type GetReflectionType()
        {
            if (_cachedType == null)
            {
                Type generic = GetGenericType(TypeName.GetReflectionType());

                if (generic != null && generic.ContainsGenericParameters)
                {
                    var argumentList = new List<Type>();
                    foreach (var arg in GenericArguments)
                    {
                        var type = arg.GetReflectionType();
                        if (type == null)
                            return null;
                        argumentList.Add(type);
                    }

                    try
                    {
                        var type = generic.MakeGenericType(argumentList.ToArray());
                        try
                        {
                            // We don't need the TypeHandle, but it's a good indication that a type doesn't
                            // support reflection which we need for many things.  So if this throws, we
                            // won't cache the type.  This situation arises while defining a type that
                            // refers to itself somehow (e.g. returns an instance of this type, array of
                            // this type, or this type as a generic parameter.)
                            // By not caching, we'll eventually get the real reflection capable type after
                            // the type is fully defined.
                            var unused = type.TypeHandle;
                        }
                        catch (NotSupportedException)
                        {
                            return type;
                        }

                        Interlocked.CompareExchange(ref _cachedType, type, null);
                    }
                    catch (Exception)
                    {
                        // We don't want to throw exception from GetReflectionType
                    }
                }
            }

            return _cachedType;
        }

        /// <summary>
        /// Get the actual generic type if it's necessary.
        /// </summary>
        /// <param name="generic"></param>
        /// <returns></returns>
        internal Type GetGenericType(Type generic)
        {
            if (generic == null || !generic.ContainsGenericParameters)
            {
                if (!TypeName.FullName.Contains('`'))
                {
                    var newTypeName = new TypeName(Extent,
                        string.Format(CultureInfo.InvariantCulture, "{0}`{1}", TypeName.FullName, GenericArguments.Count));
                    generic = newTypeName.GetReflectionType();
                }
            }

            return generic;
        }

        /// <summary>
        /// Returns the <see cref="Type"/> this type represents, assuming the type is an attribute.  The suffix
        /// "Attribute" may be appended, if necessary, to resolve the type.
        /// </summary>
        /// <returns>
        /// The <see cref="Type"/> if possible, null otherwise.  Null may be returned for valid typenames if the assembly
        /// containing the type has not been loaded.
        /// </returns>
        public Type GetReflectionAttributeType()
        {
            Type type = GetReflectionType();
            if (type == null)
            {
                Type generic = TypeName.GetReflectionAttributeType();
                if (generic == null || !generic.ContainsGenericParameters)
                {
                    if (!TypeName.FullName.Contains('`'))
                    {
                        var newTypeName = new TypeName(Extent,
                            string.Format(CultureInfo.InvariantCulture, "{0}Attribute`{1}", TypeName.FullName, GenericArguments.Count));
                        generic = newTypeName.GetReflectionType();
                    }
                }

                if (generic != null && generic.ContainsGenericParameters)
                {
                    type = generic.MakeGenericType((from arg in GenericArguments select arg.GetReflectionType()).ToArray());
                    Interlocked.CompareExchange(ref _cachedType, type, null);
                }
            }

            return type;
        }

        /// <summary>
        /// Simply return the <see cref="FullName"/> of the type.
        /// </summary>
        public override string ToString()
        {
            return FullName;
        }

        /// <summary/>
        public override bool Equals(object obj)
        {
            if (!(obj is GenericTypeName other))
                return false;

            if (!TypeName.Equals(other.TypeName))
                return false;

            if (GenericArguments.Count != other.GenericArguments.Count)
                return false;

            var count = GenericArguments.Count;
            for (int i = 0; i < count; i++)
            {
                if (!GenericArguments[i].Equals(other.GenericArguments[i]))
                    return false;
            }

            return true;
        }

        /// <summary/>
        public override int GetHashCode()
        {
            int hash = TypeName.GetHashCode();
            var count = GenericArguments.Count;
            for (int i = 0; i < count; i++)
            {
                hash = Utils.CombineHashCodes(hash, GenericArguments[i].GetHashCode());
            }

            return hash;
        }

        Type ISupportsTypeCaching.CachedType
        {
            get { return _cachedType; }

            set { _cachedType = value; }
        }
    }

    /// <summary>
    /// Represents the name of an array type including the dimensions.
    /// </summary>
    public sealed class ArrayTypeName : ITypeName, ISupportsTypeCaching
    {
        private string _cachedFullName;
        private Type _cachedType;

        /// <summary>
        /// Construct an ArrayTypeName.
        /// </summary>
        /// <param name="extent">The extent of the array typename.</param>
        /// <param name="elementType">The name of the element type.</param>
        /// <param name="rank">The number of dimensions in the array.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="elementType"/> is null.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// If <paramref name="rank"/> is 0 or negative.
        /// </exception>
        public ArrayTypeName(IScriptExtent extent, ITypeName elementType, int rank)
        {
            if (extent == null || elementType == null)
            {
                throw PSTraceSource.NewArgumentNullException(extent == null ? "extent" : "name");
            }

            if (rank <= 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(rank));
            }

            Extent = extent;
            this.Rank = rank;
            this.ElementType = elementType;
        }

        private string GetName(bool includeAssemblyName)
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
                sb.Append(ElementType.Name);
                sb.Append('[');
                if (Rank > 1)
                {
                    sb.Append(',', Rank - 1);
                }

                sb.Append(']');
                if (includeAssemblyName)
                {
                    var assemblyName = ElementType.AssemblyName;
                    if (assemblyName != null)
                    {
                        sb.Append(',');
                        sb.Append(assemblyName);
                    }
                }
            }
            catch (InsufficientExecutionStackException)
            {
                throw new ScriptCallDepthException();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Return the typename, using PowerShell syntax for the array dimensions.
        /// </summary>
        public string FullName
        {
            get
            {
                if (_cachedFullName == null)
                {
                    Interlocked.CompareExchange(ref _cachedFullName, GetName(includeAssemblyName: true), null);
                }

                return _cachedFullName;
            }
        }

        /// <summary>
        /// The name of the type, including any namespace, but not including the assembly name, using PowerShell syntax for the array dimensions.
        /// </summary>
        public string Name
        {
            get { return GetName(includeAssemblyName: false); }
        }

        /// <summary>
        /// The name of the assembly, if specified, otherwise null.
        /// </summary>
        public string AssemblyName { get { return ElementType.AssemblyName; } }

        /// <summary>
        /// Returns true always as this class represents arrays.
        /// </summary>
        public bool IsArray { get { return true; } }

        /// <summary>
        /// Returns false always as this class never represents generics.
        /// </summary>
        public bool IsGeneric { get { return false; } }

        /// <summary>
        /// The element type of the array.
        /// </summary>
        public ITypeName ElementType { get; }

        /// <summary>
        /// The rank of the array.
        /// </summary>
        public int Rank { get; }

        /// <summary>
        /// The extent of the typename.
        /// </summary>
        public IScriptExtent Extent { get; }

        /// <summary>
        /// Returns the <see cref="System.Type"/> that this typename represents, if such a type exists, null otherwise.
        /// </summary>
        public Type GetReflectionType()
        {
            try
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
                if (_cachedType == null)
                {
                    Type elementType = ElementType.GetReflectionType();
                    if (elementType != null)
                    {
                        Type type = Rank == 1 ? elementType.MakeArrayType() : elementType.MakeArrayType(Rank);
                        try
                        {
                            // We don't need the TypeHandle, but it's a good indication that a type doesn't
                            // support reflection which we need for many things.  So if this throws, we
                            // won't cache the type.  This situation arises while defining a type that
                            // refers to itself somehow (e.g. returns an instance of this type, array of
                            // this type, or this type as a generic parameter.)
                            // By not caching, we'll eventually get the real reflection capable type after
                            // the type is fully defined.
                            var unused = type.TypeHandle;
                        }
                        catch (NotSupportedException)
                        {
                            return type;
                        }

                        Interlocked.CompareExchange(ref _cachedType, type, null);
                    }
                }
            }
            catch (InsufficientExecutionStackException)
            {
                throw new ScriptCallDepthException();
            }

            return _cachedType;
        }

        /// <summary>
        /// Always return null, arrays can never be an attribute.
        /// </summary>
        public Type GetReflectionAttributeType()
        {
            return null;
        }

        /// <summary>
        /// Simply return the <see cref="FullName"/> of the type.
        /// </summary>
        public override string ToString()
        {
            return FullName;
        }

        /// <summary/>
        public override bool Equals(object obj)
        {
            if (!(obj is ArrayTypeName other))
                return false;

            return ElementType.Equals(other.ElementType) && Rank == other.Rank;
        }

        /// <summary/>
        public override int GetHashCode()
        {
            return Utils.CombineHashCodes(ElementType.GetHashCode(), Rank.GetHashCode());
        }

        Type ISupportsTypeCaching.CachedType
        {
            get { return _cachedType; }

            set { _cachedType = value; }
        }
    }

    /// <summary>
    /// A class that allows a <see cref="System.Type"/> to be used directly in the PowerShell ast.
    /// </summary>
    public sealed class ReflectionTypeName : ITypeName, ISupportsTypeCaching
    {
        private readonly Type _type;

        /// <summary>
        /// Construct a typename from a <see cref="System.Type"/>.
        /// </summary>
        /// <param name="type">The type to wrap.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="type"/> is null.
        /// </exception>
        public ReflectionTypeName(Type type)
        {
            if (type == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(type));
            }

            _type = type;
        }

        /// <summary>
        /// Returns the typename in PowerShell syntax.
        /// </summary>
        public string FullName { get { return ToStringCodeMethods.Type(_type); } }

        /// <summary>
        /// Returns the typename in PowerShell syntax.
        /// </summary>
        public string Name { get { return FullName; } }

        /// <summary>
        /// The name of the assembly.
        /// </summary>
        public string AssemblyName { get { return _type.Assembly.FullName; } }

        /// <summary>
        /// Returns true if the type is an array, false otherwise.
        /// </summary>
        public bool IsArray { get { return _type.IsArray; } }

        /// <summary>
        /// Returns true if the type is a generic, false otherwise.
        /// </summary>
        public bool IsGeneric { get { return _type.IsGenericType; } }

        /// <summary>
        /// The extent of the typename.
        /// </summary>
        public IScriptExtent Extent { get { return PositionUtilities.EmptyExtent; } }

        /// <summary>
        /// Returns the <see cref="System.Type"/> for this typename.  Never returns null.
        /// </summary>
        public Type GetReflectionType()
        {
            return _type;
        }

        /// <summary>
        /// Assuming the typename is an attribute, returns the <see cref="System.Type"/> that this typename represents.
        /// </summary>
        public Type GetReflectionAttributeType()
        {
            return _type;
        }

        /// <summary>
        /// Simply return the <see cref="FullName"/> of the type.
        /// </summary>
        public override string ToString()
        {
            return FullName;
        }

        /// <summary/>
        public override bool Equals(object obj)
        {
            if (!(obj is ReflectionTypeName other))
                return false;
            return _type == other._type;
        }

        /// <summary/>
        public override int GetHashCode()
        {
            return _type.GetHashCode();
        }

        Type ISupportsTypeCaching.CachedType
        {
            get { return _type; }

            set { throw new InvalidOperationException(); }
        }
    }

    /// <summary>
    /// The ast that represents a type literal expression, e.g. <c>[int]</c>.
    /// </summary>
    public class TypeExpressionAst : ExpressionAst
    {
        /// <summary>
        /// Construct a type literal expression.
        /// </summary>
        /// <param name="extent">The extent of the typename, including the opening and closing square braces.</param>
        /// <param name="typeName">The typename for the constructed ast.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="typeName"/> is null.
        /// </exception>
        public TypeExpressionAst(IScriptExtent extent, ITypeName typeName)
            : base(extent)
        {
            if (typeName == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(typeName));
            }

            this.TypeName = typeName;
        }

        /// <summary>
        /// The name of the type.  This property is never null.
        /// </summary>
        public ITypeName TypeName { get; }

        /// <summary>
        /// Copy the TypeExpressionAst instance.
        /// </summary>
        public override Ast Copy()
        {
            return new TypeExpressionAst(this.Extent, this.TypeName);
        }

        /// <summary>
        /// The static type of a type literal is always <c>typeof(Type)</c>.
        /// </summary>
        public override Type StaticType { get { return typeof(Type); } }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitTypeExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitTypeExpression(this);
            return visitor.CheckForPostAction(this, (action == AstVisitAction.SkipChildren ? AstVisitAction.Continue : action));
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast representing a variable reference, either normal references, e.g. <c>$true</c>, or splatted references
    /// <c>@PSBoundParameters</c>.
    /// </summary>
    public class VariableExpressionAst : ExpressionAst, ISupportsAssignment, IAssignableValue
    {
        /// <summary>
        /// Construct a variable reference.
        /// </summary>
        /// <param name="extent">The extent of the variable.</param>
        /// <param name="variableName">
        /// The name of the variable.  A leading '$' or '@' is not removed, those characters are assumed to be part of
        /// the variable name.
        /// </param>
        /// <param name="splatted">True if splatting, like <c>@PSBoundParameters</c>, false otherwise, like <c>$false</c></param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="variableName"/> is null, or if <paramref name="variableName"/>
        /// is an empty string.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public VariableExpressionAst(IScriptExtent extent, string variableName, bool splatted)
            : base(extent)
        {
            if (string.IsNullOrEmpty(variableName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(variableName));
            }

            this.VariablePath = new VariablePath(variableName);
            this.Splatted = splatted;
        }

        /// <summary>
        /// Construct a variable reference from a token.  Used from the parser.
        /// </summary>
        internal VariableExpressionAst(VariableToken token)
            : this(token.Extent, token.VariablePath, (token.Kind == TokenKind.SplattedVariable))
        {
        }

        /// <summary>
        /// Construct a variable reference with an existing VariablePath (rather than construct a new one.)
        /// </summary>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="variablePath"/> is null.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public VariableExpressionAst(IScriptExtent extent, VariablePath variablePath, bool splatted)
            : base(extent)
        {
            if (variablePath == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(variablePath));
            }

            this.VariablePath = variablePath;
            this.Splatted = splatted;
        }

        /// <summary>
        /// The name of the variable.  This property is never null.
        /// </summary>
        public VariablePath VariablePath { get; }

        /// <summary>
        /// True if splatting syntax was used, false otherwise.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public bool Splatted { get; }

        /// <summary>
        /// Check if the variable is one of $true, $false and $null.
        /// </summary>
        /// <returns>
        /// True if it is a constant variable
        /// </returns>
        public bool IsConstantVariable()
        {
            if (this.VariablePath.IsVariable)
            {
                string name = this.VariablePath.UnqualifiedPath;
                if (name.Equals(SpecialVariables.True, StringComparison.OrdinalIgnoreCase) ||
                    name.Equals(SpecialVariables.False, StringComparison.OrdinalIgnoreCase) ||
                    name.Equals(SpecialVariables.Null, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Copy the VariableExpressionAst instance.
        /// </summary>
        public override Ast Copy()
        {
            return new VariableExpressionAst(this.Extent, this.VariablePath, this.Splatted);
        }

        internal bool IsSafeVariableReference(HashSet<string> validVariables, ref bool usesParameter)
        {
            bool ok = false;

            if (this.VariablePath.IsAnyLocal())
            {
                var varName = this.VariablePath.UnqualifiedPath;
                if (((validVariables != null) && validVariables.Contains(varName)) ||
                    varName.Equals(SpecialVariables.Args, StringComparison.OrdinalIgnoreCase))
                {
                    ok = true;
                    usesParameter = true;
                }
                else
                {
                    ok = !this.Splatted
                         && this.IsConstantVariable();
                }
            }

            return ok;
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitVariableExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitVariableExpression(this);
            return visitor.CheckForPostAction(this, (action == AstVisitAction.SkipChildren ? AstVisitAction.Continue : action));
        }

        #endregion Visitors

        #region Code Generation Details

        internal int TupleIndex { get; set; } = VariableAnalysis.Unanalyzed;

        internal bool Automatic { get; set; }

        internal bool Assigned { get; set; }

        IAssignableValue ISupportsAssignment.GetAssignableValue()
        {
            return this;
        }

        Expression IAssignableValue.GetValue(Compiler compiler, List<Expression> exprs, List<ParameterExpression> temps)
        {
            return (Expression)compiler.VisitVariableExpression(this);
        }

        Expression IAssignableValue.SetValue(Compiler compiler, Expression rhs)
        {
            if (this.VariablePath.IsVariable && this.VariablePath.UnqualifiedPath.Equals(SpecialVariables.Null, StringComparison.OrdinalIgnoreCase))
            {
                return rhs;
            }

            IEnumerable<PropertyInfo> tupleAccessPath;
            bool localInTuple;
            Type targetType = GetVariableType(compiler, out tupleAccessPath, out localInTuple);

            // Value types must be copied on assignment (if they are mutable), boxed or not.  This preserves language
            // semantics from V1/V2, and might be slightly more natural for a dynamic language.
            // To generate good code, we assume any object or PSObject could be a boxed value type, and generate a dynamic
            // site to handle copying as necessary.  Given this assumption, here are the possibilities:
            //
            //     - rhs is reference type
            //         * just convert to lhs type
            //     - rhs is value type
            //         * lhs type is value type, copy made by simple assignment
            //         * lhs type is object, copy is made by boxing, also handled in simple assignment
            //     - rhs is boxed value type
            //         * lhs type is value type, copy is made by unboxing conversion
            //         * lhs type is object/psobject, copy must be made dynamically (boxed value type can't be known statically)
            var rhsType = rhs.Type;
            if (localInTuple &&
                (targetType == typeof(object) || targetType == typeof(PSObject)) &&
                (rhsType == typeof(object) || rhsType == typeof(PSObject)))
            {
                rhs = DynamicExpression.Dynamic(PSVariableAssignmentBinder.Get(), typeof(object), rhs);
            }

            rhs = rhs.Convert(targetType);

            if (!localInTuple)
            {
                return Compiler.CallSetVariable(Expression.Constant(VariablePath), rhs);
            }

            Expression lhs = compiler.LocalVariablesParameter;
            foreach (var property in tupleAccessPath)
            {
                lhs = Expression.Property(lhs, property);
            }

            return Expression.Assign(lhs, rhs);
        }

        internal Type GetVariableType(Compiler compiler, out IEnumerable<PropertyInfo> tupleAccessPath, out bool localInTuple)
        {
            Type targetType = null;
            localInTuple = TupleIndex >= 0 &&
                                (compiler.Optimize || TupleIndex < (int)AutomaticVariable.NumberOfAutomaticVariables);
            tupleAccessPath = null;
            if (localInTuple)
            {
                tupleAccessPath = MutableTuple.GetAccessPath(compiler.LocalVariablesTupleType, TupleIndex);
                targetType = tupleAccessPath.Last().PropertyType;
            }
            else
            {
                targetType = typeof(object);
            }

            return targetType;
        }

        #endregion Code Generation Details
    }

    /// <summary>
    /// The ast representing constant values, such as numbers.  Constant values mean truly constant, as in, the value is
    /// always the same.  Expandable strings with variable references (e.g. <c>"$val"</c>) or sub-expressions
    /// (e.g. <c>"$(1)"</c>) are not considered constant.
    /// </summary>
    public class ConstantExpressionAst : ExpressionAst
    {
        /// <summary>
        /// Construct a constant expression.
        /// </summary>
        /// <param name="extent">The extent of the constant.</param>
        /// <param name="value">The value of the constant.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        public ConstantExpressionAst(IScriptExtent extent, object value)
            : base(extent)
        {
            this.Value = value;
        }

        internal ConstantExpressionAst(NumberToken token)
            : base(token.Extent)
        {
            this.Value = token.Value;
        }

        /// <summary>
        /// The value of the constant.  This property is null only if the expression represents the null constant.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Copy the ConstantExpressionAst instance.
        /// </summary>
        public override Ast Copy()
        {
            return new ConstantExpressionAst(this.Extent, this.Value);
        }

        /// <summary>
        /// The static type of a constant is whatever type the value is, or if null, then assume it's <c>typeof(object)</c>.
        /// </summary>
        public override Type StaticType
        {
            get { return Value != null ? Value.GetType() : typeof(object); }
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitConstantExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitConstantExpression(this);
            return visitor.CheckForPostAction(this, (action == AstVisitAction.SkipChildren ? AstVisitAction.Continue : action));
        }

        #endregion Visitors
    }

    /// <summary>
    /// The kind of string constant.
    /// </summary>
    public enum StringConstantType
    {
        /// <summary>
        /// A string enclosed in single quotes, e.g. <c>'some text'</c>.
        /// </summary>
        SingleQuoted,

        /// <summary>
        /// A here string enclosed in single quotes, e.g. <c> @'
        /// a here string
        /// '@
        /// </c>
        /// </summary>
        SingleQuotedHereString,

        /// <summary>
        /// A string enclosed in double quotes, e.g. <c>"some text"</c>.
        /// </summary>
        DoubleQuoted,

        /// <summary>
        /// A here string enclosed in double quotes, e.g. <c> @"
        /// a here string
        /// "@
        /// </c>
        /// </summary>
        DoubleQuotedHereString,

        /// <summary>
        /// A string like token not enclosed in any quotes.  This usually includes a command name or command argument.
        /// </summary>
        BareWord
    }

    /// <summary>
    /// The ast that represents a constant string expression that is always constant.  This includes both single and
    /// double quoted strings, but the double quoted strings will not be scanned for variable references and sub-expressions.
    /// If expansion of the string is required, use <see cref="ExpandableStringExpressionAst"/>.
    /// </summary>
    public class StringConstantExpressionAst : ConstantExpressionAst
    {
        /// <summary>
        /// Construct a string constant expression.
        /// </summary>
        /// <param name="extent">The extent of the string constant, including quotes.</param>
        /// <param name="value">The value of the string.</param>
        /// <param name="stringConstantType">The type of string.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="value"/> is null.
        /// </exception>
        public StringConstantExpressionAst(IScriptExtent extent, string value, StringConstantType stringConstantType)
            : base(extent, value)
        {
            if (value == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(value));
            }

            this.StringConstantType = stringConstantType;
        }

        internal StringConstantExpressionAst(StringToken token)
            : base(token.Extent, token.Value)
        {
            this.StringConstantType = MapTokenKindToStringConstantKind(token);
        }

        /// <summary>
        /// The type of string.
        /// </summary>
        public StringConstantType StringConstantType { get; }

        /// <summary>
        /// The value of the string, not including the quotes used.
        /// </summary>
        public new string Value { get { return (string)base.Value; } }

        /// <summary>
        /// Copy the StringConstantExpressionAst instance.
        /// </summary>
        public override Ast Copy()
        {
            return new StringConstantExpressionAst(this.Extent, this.Value, this.StringConstantType);
        }

        /// <summary>
        /// The type of a StringConstantExpressionAst is always <c>typeof(string)</c>.
        /// </summary>
        public override Type StaticType
        {
            get { return typeof(string); }
        }

        internal static StringConstantType MapTokenKindToStringConstantKind(Token token)
        {
            switch (token.Kind)
            {
                case TokenKind.StringExpandable:
                    return StringConstantType.DoubleQuoted;
                case TokenKind.HereStringLiteral:
                    return StringConstantType.SingleQuotedHereString;
                case TokenKind.HereStringExpandable:
                    return StringConstantType.DoubleQuotedHereString;
                case TokenKind.StringLiteral:
                    return StringConstantType.SingleQuoted;
                case TokenKind.Generic:
                    return StringConstantType.BareWord;
            }

            throw PSTraceSource.NewInvalidOperationException();
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitStringConstantExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitStringConstantExpression(this);
            return visitor.CheckForPostAction(this, (action == AstVisitAction.SkipChildren ? AstVisitAction.Continue : action));
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast that represents a double quoted string (here string or normal string) and can have nested variable
    /// references or sub-expressions, e.g. <c>"Name: $name`nAge: $([DateTime]::Now.Year - $dob.Year)"</c>.
    /// </summary>
    public class ExpandableStringExpressionAst : ExpressionAst
    {
        /// <summary>
        /// Construct an expandable string.  The value is scanned for nested variable references and expressions
        /// which are evaluated at runtime when this ast is compiled.
        /// </summary>
        /// <param name="extent">The extent of the string.</param>
        /// <param name="value">The unexpanded value of the string.</param>
        /// <param name="type">The kind of string, must be one of<list>
        /// <see cref="System.Management.Automation.Language.StringConstantType.DoubleQuoted"/>
        /// <see cref="System.Management.Automation.Language.StringConstantType.DoubleQuotedHereString"/>
        /// <see cref="System.Management.Automation.Language.StringConstantType.BareWord"/>
        /// </list></param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="value"/> or <paramref name="extent"/> is null.
        /// </exception>
        public ExpandableStringExpressionAst(IScriptExtent extent,
                                             string value,
                                             StringConstantType type)
            : base(extent)
        {
            if (value == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(value));
            }

            if (type != StringConstantType.DoubleQuoted && type != StringConstantType.DoubleQuotedHereString
                && type != StringConstantType.BareWord)
            {
                throw PSTraceSource.NewArgumentException(nameof(type));
            }

            var ast = Language.Parser.ScanString(value);
            var expandableStringAst = ast as ExpandableStringExpressionAst;
            if (expandableStringAst != null)
            {
                this.FormatExpression = expandableStringAst.FormatExpression;
                this.NestedExpressions = expandableStringAst.NestedExpressions;
            }
            else
            {
                // We always compile to a format expression.  In the rare case that some external code (this can't happen
                // internally) passes in a string that doesn't require any expansion, we still need to generate code that
                // works.  This is slow as the ast is a string constant, but it should be rare.
                this.FormatExpression = "{0}";
                this.NestedExpressions = new ReadOnlyCollection<ExpressionAst>(new[] { ast });
            }

            // Set parents properly
            for (int i = 0; i < this.NestedExpressions.Count; i++)
            {
                this.NestedExpressions[i].ClearParent();
            }

            SetParents(this.NestedExpressions);

            this.Value = value;
            this.StringConstantType = type;
        }

        /// <summary>
        /// Construct an expandable string expression from a string token.  Used from the parser after parsing
        /// the nested tokens.  This method is internal mainly so we can avoid validating <paramref name="formatString"/>.
        /// </summary>
        internal ExpandableStringExpressionAst(Token token, string value, string formatString, IEnumerable<ExpressionAst> nestedExpressions)
            : this(token.Extent, value, formatString,
                   StringConstantExpressionAst
                        .MapTokenKindToStringConstantKind(token),
                   nestedExpressions)
        {
        }

        private ExpandableStringExpressionAst(IScriptExtent extent, string value, string formatString,
                                              StringConstantType type, IEnumerable<ExpressionAst> nestedExpressions)
            : base(extent)
        {
            Diagnostics.Assert(nestedExpressions != null && nestedExpressions.Any(), "Must specify non-empty expressions.");

            this.FormatExpression = formatString;
            this.Value = value;
            this.StringConstantType = type;
            this.NestedExpressions = new ReadOnlyCollection<ExpressionAst>(nestedExpressions.ToArray());
            SetParents(NestedExpressions);
        }

        /// <summary>
        /// The value of string, not including the quote characters and without any variables replaced.
        /// This property is never null.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// The type of string.
        /// </summary>
        public StringConstantType StringConstantType { get; }

        /// <summary>
        /// A non-empty collection of expressions contained within the string.  The nested expressions are always either
        /// instances of <see cref="VariableExpressionAst"/> or <see cref="SubExpressionAst"/>.
        /// </summary>
        public ReadOnlyCollection<ExpressionAst> NestedExpressions { get; }

        /// <summary>
        /// Copy the ExpandableStringExpressionAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newNestedExpressions = CopyElements(this.NestedExpressions);
            return new ExpandableStringExpressionAst(this.Extent, this.Value, this.FormatExpression, this.StringConstantType, newNestedExpressions);
        }

        /// <summary>
        /// The type of a StringConstantExpressionAst is always <c>typeof(string)</c>.
        /// </summary>
        public override Type StaticType
        {
            get { return typeof(string); }
        }

        /// <summary>
        /// The format expression needed to execute this ast.  It is generated by the scanner, it is not provided by clients.
        /// </summary>
        internal string FormatExpression { get; }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitExpandableStringExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitExpandableStringExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue && NestedExpressions != null)
            {
                for (int index = 0; index < NestedExpressions.Count; index++)
                {
                    var exprAst = NestedExpressions[index];
                    action = exprAst.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast that represents an anonymous script block expression, e.g. <c>{ dir }</c>.
    /// </summary>
    public class ScriptBlockExpressionAst : ExpressionAst
    {
        /// <summary>
        /// Construct a script block expression.
        /// </summary>
        /// <param name="extent">The extent of the script block, from the opening curly brace to the closing curly brace.</param>
        /// <param name="scriptBlock">The script block.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="scriptBlock"/> is null.
        /// </exception>
        public ScriptBlockExpressionAst(IScriptExtent extent, ScriptBlockAst scriptBlock)
            : base(extent)
        {
            if (scriptBlock == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(scriptBlock));
            }

            this.ScriptBlock = scriptBlock;
            SetParent(scriptBlock);
        }

        /// <summary>
        /// The ast for the scriptblock that this ast represent.  This property is never null.
        /// </summary>
        public ScriptBlockAst ScriptBlock { get; }

        /// <summary>
        /// Copy the ScriptBlockExpressionAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newScriptBlock = CopyElement(this.ScriptBlock);
            return new ScriptBlockExpressionAst(this.Extent, newScriptBlock);
        }

        /// <summary>
        /// The result of a <see cref="ScriptBlockExpressionAst"/> is always <c>typeof(<see cref="ScriptBlock"/></c>).
        /// </summary>
        public override Type StaticType
        {
            get { return typeof(ScriptBlock); }
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitScriptBlockExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitScriptBlockExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = ScriptBlock.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast that represents an array literal expression, e.g. <c>1,2,3</c>.  An array expression, e.g. <c>@(dir)</c>,
    /// is represented by <see cref="ArrayExpressionAst"/>.  An array literal expression can be constructed from a single
    /// element, as happens with the unary comma operator, e.g. <c>,4</c>.
    /// </summary>
    public class ArrayLiteralAst : ExpressionAst, ISupportsAssignment
    {
        /// <summary>
        /// Construct an array literal expression.
        /// </summary>
        /// <param name="extent">The extent of all of the elements.</param>
        /// <param name="elements">The collection of asts that represent the array literal.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// If <paramref name="elements"/> is null or is an empty collection.
        /// </exception>
        public ArrayLiteralAst(IScriptExtent extent, IList<ExpressionAst> elements)
            : base(extent)
        {
            if (elements == null || elements.Count == 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(elements));
            }

            this.Elements = new ReadOnlyCollection<ExpressionAst>(elements);
            SetParents(Elements);
        }

        /// <summary>
        /// The non-empty collection of asts of the elements of the array.
        /// </summary>
        public ReadOnlyCollection<ExpressionAst> Elements { get; }

        /// <summary>
        /// Copy the ArrayLiteralAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newElements = CopyElements(this.Elements);
            return new ArrayLiteralAst(this.Extent, newElements);
        }

        /// <summary>
        /// The result of an <see cref="ArrayLiteralAst"/> is always <c>typeof(object[])</c>.
        /// </summary>
        public override Type StaticType { get { return typeof(object[]); } }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitArrayLiteral(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitArrayLiteral(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < Elements.Count; index++)
                {
                    var element = Elements[index];
                    action = element.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        IAssignableValue ISupportsAssignment.GetAssignableValue()
        {
            return new ArrayAssignableValue { ArrayLiteral = this };
        }
    }

    /// <summary>
    /// The ast that represents a hash literal, e.g. <c>@{a = 1}</c>.
    /// </summary>
    public class HashtableAst : ExpressionAst
    {
        private static readonly ReadOnlyCollection<KeyValuePair> s_emptyKeyValuePairs = Utils.EmptyReadOnlyCollection<KeyValuePair>();

        /// <summary>
        /// Construct a hash literal ast.
        /// </summary>
        /// <param name="extent">The extent of the literal, from '@{' to the closing '}'.</param>
        /// <param name="keyValuePairs">The optionally null or empty list of key/value pairs.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> is null.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public HashtableAst(IScriptExtent extent, IEnumerable<KeyValuePair> keyValuePairs)
            : base(extent)
        {
            if (keyValuePairs != null)
            {
                this.KeyValuePairs = new ReadOnlyCollection<KeyValuePair>(keyValuePairs.ToArray());
                SetParents(KeyValuePairs);
            }
            else
            {
                this.KeyValuePairs = s_emptyKeyValuePairs;
            }
        }

        /// <summary>
        /// The pairs of key names and asts for values used to construct the hash table.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public ReadOnlyCollection<KeyValuePair> KeyValuePairs { get; }

        /// <summary>
        /// Copy the HashtableAst instance.
        /// </summary>
        public override Ast Copy()
        {
            List<KeyValuePair> newKeyValuePairs = null;
            if (this.KeyValuePairs.Count > 0)
            {
                newKeyValuePairs = new List<KeyValuePair>(this.KeyValuePairs.Count);
                for (int i = 0; i < this.KeyValuePairs.Count; i++)
                {
                    var keyValuePair = this.KeyValuePairs[i];
                    var newKey = CopyElement(keyValuePair.Item1);
                    var newValue = CopyElement(keyValuePair.Item2);
                    newKeyValuePairs.Add(Tuple.Create(newKey, newValue));
                }
            }

            return new HashtableAst(this.Extent, newKeyValuePairs);
        }

        /// <summary>
        /// The result type of a <see cref="HashtableAst"/> is always <c>typeof(<see cref="Hashtable"/>)</c>.
        /// </summary>
        public override Type StaticType { get { return typeof(Hashtable); } }

        // Indicates that this ast was constructed as part of a schematized object instead of just a plain hash literal.
        internal bool IsSchemaElement { get; set; }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitHashtable(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitHashtable(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < KeyValuePairs.Count; index++)
                {
                    var keyValuePairAst = KeyValuePairs[index];
                    action = keyValuePairAst.Item1.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                    action = keyValuePairAst.Item2.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast that represents an array expression, e.g. <c>@(1)</c>.  The array literal (e.g. <c>1,2,3</c>) is
    /// represented by <see cref="ArrayLiteralAst"/>.
    /// </summary>
    public class ArrayExpressionAst : ExpressionAst
    {
        /// <summary>
        /// Construct an expression that forces the result to be an array.
        /// </summary>
        /// <param name="extent">The extent of the expression, including the opening '@(' and closing ')'.</param>
        /// <param name="statementBlock">The statements executed as part of the expression.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="statementBlock"/> is null.
        /// </exception>
        public ArrayExpressionAst(IScriptExtent extent, StatementBlockAst statementBlock)
            : base(extent)
        {
            if (statementBlock == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(statementBlock));
            }

            this.SubExpression = statementBlock;
            SetParent(statementBlock);
        }

        /// <summary>
        /// The expression/statements represented by this sub-expression.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public StatementBlockAst SubExpression { get; }

        /// <summary>
        /// Copy the ArrayExpressionAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newStatementBlock = CopyElement(this.SubExpression);
            return new ArrayExpressionAst(this.Extent, newStatementBlock);
        }

        /// <summary>
        /// The result of an ArrayExpressionAst is always <c>typeof(object[])</c>.
        /// </summary>
        public override Type StaticType { get { return typeof(object[]); } }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitArrayExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitArrayExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = SubExpression.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast that represents an expression (or pipeline) that is enclosed in parentheses, e.g. <c>(1)</c> or <c>(dir)</c>
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Paren")]
    public class ParenExpressionAst : ExpressionAst, ISupportsAssignment
    {
        /// <summary>
        /// Construct a parenthesized expression.
        /// </summary>
        /// <param name="extent">The extent of the expression, including the opening and closing parentheses.</param>
        /// <param name="pipeline">The pipeline (or expression) enclosed in parentheses.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="pipeline"/> is null.
        /// </exception>
        public ParenExpressionAst(IScriptExtent extent, PipelineBaseAst pipeline)
            : base(extent)
        {
            if (pipeline == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(pipeline));
            }

            this.Pipeline = pipeline;
            SetParent(pipeline);
        }

        /// <summary>
        /// The pipeline (which is frequently but not always an expression) for this parenthesized expression.
        /// This property is never null.
        /// </summary>
        public PipelineBaseAst Pipeline { get; }

        /// <summary>
        /// Copy the ParenExpressionAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newPipeline = CopyElement(this.Pipeline);
            return new ParenExpressionAst(this.Extent, newPipeline);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitParenExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitParenExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Pipeline.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        IAssignableValue ISupportsAssignment.GetAssignableValue()
        {
            return ((ISupportsAssignment)Pipeline.GetPureExpression()).GetAssignableValue();
        }
    }

    /// <summary>
    /// The ast that represents a subexpression, e.g. <c>$(1)</c>.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
    public class SubExpressionAst : ExpressionAst
    {
        /// <summary>
        /// Construct a subexpression.
        /// </summary>
        /// <param name="extent">The extent of the expression.</param>
        /// <param name="statementBlock"></param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="statementBlock"/> is null.
        /// </exception>
        public SubExpressionAst(IScriptExtent extent, StatementBlockAst statementBlock)
            : base(extent)
        {
            if (statementBlock == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(statementBlock));
            }

            this.SubExpression = statementBlock;
            SetParent(statementBlock);
        }

        /// <summary>
        /// The expression/statements represented by this sub-expression.  This property is never null.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public StatementBlockAst SubExpression { get; }

        /// <summary>
        /// Copy the SubExpressionAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newStatementBlock = CopyElement(this.SubExpression);
            return new SubExpressionAst(this.Extent, newStatementBlock);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitSubExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitSubExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = SubExpression.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast that represents a "using" expression, e.g. <c>$using:pshome</c>
    /// </summary>
    public class UsingExpressionAst : ExpressionAst
    {
        /// <summary>
        /// Construct a using expression.
        /// </summary>
        /// <param name="extent">The extent of the using expression.</param>
        /// <param name="expressionAst">The sub-expression of the using expression.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/> or <paramref name="expressionAst"/> is null.
        /// </exception>
        public UsingExpressionAst(IScriptExtent extent, ExpressionAst expressionAst)
            : base(extent)
        {
            if (expressionAst == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(expressionAst));
            }

            RuntimeUsingIndex = -1;
            this.SubExpression = expressionAst;
            SetParent(SubExpression);
        }

        /// <summary>
        /// The expression represented by this using expression.  This property is never null.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public ExpressionAst SubExpression { get; }

        // Used from code gen to get the value from a well known location.
        internal int RuntimeUsingIndex
        {
            get;
            set;
        }

        /// <summary>
        /// Copy the UsingExpressionAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newExpression = CopyElement(this.SubExpression);
            var newUsingExpression = new UsingExpressionAst(this.Extent, newExpression);
            newUsingExpression.RuntimeUsingIndex = this.RuntimeUsingIndex;
            return newUsingExpression;
        }

        #region UsingExpression Utilities

        internal const string UsingPrefix = "__using_";

        /// <summary>
        /// Get the underlying "using variable" from a UsingExpressionAst.
        /// </summary>
        /// <param name="usingExpressionAst">
        /// A UsingExpressionAst
        /// </param>
        /// <returns>
        /// The underlying VariableExpressionAst of the UsingExpression
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "We want to get the underlying variable only for the UsingExpressionAst.")]
        public static VariableExpressionAst ExtractUsingVariable(UsingExpressionAst usingExpressionAst)
        {
            if (usingExpressionAst == null)
            {
                throw new ArgumentNullException(nameof(usingExpressionAst));
            }

            return ExtractUsingVariableImpl(usingExpressionAst);
        }

        /// <summary>
        /// A UsingExpressionAst must contains a VariableExpressionAst.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private static VariableExpressionAst ExtractUsingVariableImpl(ExpressionAst expression)
        {
            var usingExpr = expression as UsingExpressionAst;
            VariableExpressionAst variableExpr;

            if (usingExpr != null)
            {
                variableExpr = usingExpr.SubExpression as VariableExpressionAst;
                if (variableExpr != null)
                {
                    return variableExpr;
                }

                return ExtractUsingVariableImpl(usingExpr.SubExpression);
            }

            var indexExpr = expression as IndexExpressionAst;
            if (indexExpr != null)
            {
                variableExpr = indexExpr.Target as VariableExpressionAst;
                if (variableExpr != null)
                {
                    return variableExpr;
                }

                return ExtractUsingVariableImpl(indexExpr.Target);
            }

            var memberExpr = expression as MemberExpressionAst;
            if (memberExpr != null)
            {
                variableExpr = memberExpr.Expression as VariableExpressionAst;
                if (variableExpr != null)
                {
                    return variableExpr;
                }

                return ExtractUsingVariableImpl(memberExpr.Expression);
            }

            Diagnostics.Assert(false, "We should always be able to get a VariableExpressionAst from a UsingExpressionAst");
            return null;
        }

        #endregion UsingExpression Utilities

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitUsingExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitUsingExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = SubExpression.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    /// <summary>
    /// The ast that represents an index expression, e.g. <c>$a[0]</c>.
    /// </summary>
    public class IndexExpressionAst : ExpressionAst, ISupportsAssignment
    {
        /// <summary>
        /// Construct an ast for an index expression.
        /// </summary>
        /// <param name="extent">The extent of the expression.</param>
        /// <param name="target">The expression being indexed.</param>
        /// <param name="index">The index expression.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/>, <paramref name="target"/>, or <paramref name="index"/> is null.
        /// </exception>
        public IndexExpressionAst(IScriptExtent extent, ExpressionAst target, ExpressionAst index)
            : base(extent)
        {
            if (target == null || index == null)
            {
                throw PSTraceSource.NewArgumentNullException(target == null ? "target" : "index");
            }

            this.Target = target;
            SetParent(target);
            this.Index = index;
            SetParent(index);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexExpressionAst"/> class.
        /// </summary>
        /// <param name="extent">The extent of the expression.</param>
        /// <param name="target">The expression being indexed.</param>
        /// <param name="index">The index expression.</param>
        /// <param name="nullConditional">Access the index only if the target is not null.</param>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="extent"/>, <paramref name="target"/>, or <paramref name="index"/> is null.
        /// </exception>
        public IndexExpressionAst(IScriptExtent extent, ExpressionAst target, ExpressionAst index, bool nullConditional)
            : this(extent, target, index)
        {
            this.NullConditional = nullConditional;
        }

        /// <summary>
        /// Return the ast for the expression being indexed.  This value is never null.
        /// </summary>
        public ExpressionAst Target { get; }

        /// <summary>
        /// Return the ast for the index expression.  This value is never null.
        /// </summary>
        public ExpressionAst Index { get; }

        /// <summary>
        /// Gets a value indicating whether ?[] operator is being used.
        /// </summary>
        public bool NullConditional { get; }

        /// <summary>
        /// Copy the IndexExpressionAst instance.
        /// </summary>
        public override Ast Copy()
        {
            var newTarget = CopyElement(this.Target);
            var newIndex = CopyElement(this.Index);
            return new IndexExpressionAst(this.Extent, newTarget, newIndex, this.NullConditional);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitIndexExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitIndexExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Target.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Index.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        IAssignableValue ISupportsAssignment.GetAssignableValue()
        {
            return new IndexAssignableValue { IndexExpressionAst = this };
        }
    }

    #endregion Expressions

    #region Help

    /// <summary>
    /// The help content specified via help comments for a given script or script function.
    /// </summary>
    public sealed class CommentHelpInfo
    {
        /// <summary>
        /// The help content of the .SYNOPSIS section, if specified, otherwise null.
        /// </summary>
        public string Synopsis { get; internal set; }

        /// <summary>
        /// The help content of the .DESCRIPTION section, if specified, otherwise null.
        /// </summary>
        public string Description { get; internal set; }

        /// <summary>
        /// The help content of the .NOTES section, if specified, otherwise null.
        /// </summary>
        public string Notes { get; internal set; }

        /// <summary>
        /// The help content for each parameter where help content is specified.  The
        /// key is the parameter name, the value is the help content.
        /// </summary>
        /// TODO, Changing this to an IDictionary because ReadOnlyDictionary is available only in .NET 4.5
        /// This is a temporary workaround and will be fixed later. Tracked by Win8: 354135
        public IDictionary<string, string> Parameters { get; internal set; }

        /// <summary>
        /// The help content from all of the specified .LINK sections.
        /// </summary>
        public ReadOnlyCollection<string> Links { get; internal set; }

        /// <summary>
        /// The help content from all of the specified .EXAMPLE sections.
        /// </summary>
        public ReadOnlyCollection<string> Examples { get; internal set; }

        /// <summary>
        /// The help content from all of the specified .INPUT sections.
        /// </summary>
        public ReadOnlyCollection<string> Inputs { get; internal set; }

        /// <summary>
        /// The help content from all of the specified .OUTPUT sections.
        /// </summary>
        public ReadOnlyCollection<string> Outputs { get; internal set; }

        /// <summary>
        /// The help content of the .COMPONENT section, if specified, otherwise null.
        /// </summary>
        public string Component { get; internal set; }

        /// <summary>
        /// The help content of the .ROLE section, if specified, otherwise null.
        /// </summary>
        public string Role { get; internal set; }

        /// <summary>
        /// The help content of the .FUNCTIONALITY section, if specified, otherwise null.
        /// </summary>
        public string Functionality { get; internal set; }

        /// <summary>
        /// The help content of the .FORWARDHELPTARGETNAME section, if specified, otherwise null.
        /// </summary>
        public string ForwardHelpTargetName { get; internal set; }

        /// <summary>
        /// The help content of the .FORWARDHELPCATEGORY section, if specified, otherwise null.
        /// </summary>
        public string ForwardHelpCategory { get; internal set; }

        /// <summary>
        /// The help content of the .REMOTEHELPRUNSPACE section, if specified, otherwise null.
        /// </summary>
        public string RemoteHelpRunspace { get; internal set; }

        /// <summary>
        /// The help content of the .MAMLHELPFILE section, if specified, otherwise null.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Maml")]
        public string MamlHelpFile { get; internal set; }

        /// <summary>
        /// Returns the help info as a comment block.
        /// </summary>
        public string GetCommentBlock()
        {
            var sb = new StringBuilder();

            sb.AppendLine("<#");
            if (!string.IsNullOrEmpty(Synopsis))
            {
                sb.AppendLine(".SYNOPSIS");
                sb.AppendLine(Synopsis);
            }

            if (!string.IsNullOrEmpty(Description))
            {
                sb.AppendLine(".DESCRIPTION");
                sb.AppendLine(Description);
            }

            foreach (var parameter in Parameters)
            {
                sb.Append(".PARAMETER ");
                sb.AppendLine(parameter.Key);
                sb.AppendLine(parameter.Value);
            }

            for (int index = 0; index < Inputs.Count; index++)
            {
                var input = Inputs[index];
                sb.AppendLine(".INPUTS");
                sb.AppendLine(input);
            }

            for (int index = 0; index < Outputs.Count; index++)
            {
                var output = Outputs[index];
                sb.AppendLine(".OUTPUTS");
                sb.AppendLine(output);
            }

            if (!string.IsNullOrEmpty(Notes))
            {
                sb.AppendLine(".NOTES");
                sb.AppendLine(Notes);
            }

            for (int index = 0; index < Examples.Count; index++)
            {
                var example = Examples[index];
                sb.AppendLine(".EXAMPLE");
                sb.AppendLine(example);
            }

            for (int index = 0; index < Links.Count; index++)
            {
                var link = Links[index];
                sb.AppendLine(".LINK");
                sb.AppendLine(link);
            }

            if (!string.IsNullOrEmpty(ForwardHelpTargetName))
            {
                sb.Append(".FORWARDHELPTARGETNAME ");
                sb.AppendLine(ForwardHelpTargetName);
            }

            if (!string.IsNullOrEmpty(ForwardHelpCategory))
            {
                sb.Append(".FORWARDHELPCATEGORY ");
                sb.AppendLine(ForwardHelpCategory);
            }

            if (!string.IsNullOrEmpty(RemoteHelpRunspace))
            {
                sb.Append(".REMOTEHELPRUNSPACE ");
                sb.AppendLine(RemoteHelpRunspace);
            }

            if (!string.IsNullOrEmpty(Component))
            {
                sb.AppendLine(".COMPONENT");
                sb.AppendLine(Component);
            }

            if (!string.IsNullOrEmpty(Role))
            {
                sb.AppendLine(".ROLE");
                sb.AppendLine(Role);
            }

            if (!string.IsNullOrEmpty(Functionality))
            {
                sb.AppendLine(".FUNCTIONALITY");
                sb.AppendLine(Functionality);
            }

            if (!string.IsNullOrEmpty(MamlHelpFile))
            {
                sb.Append(".EXTERNALHELP ");
                sb.AppendLine(MamlHelpFile);
            }

            sb.AppendLine("#>");
            return sb.ToString();
        }
    }

    #endregion Help
}
