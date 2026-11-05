// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.PowerShell.CmdletAnalyzers
{
    /// <summary>
    /// Encourages cmdlets to preserve engine control flow when handling exceptions.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EngineExceptionFilterAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PSCMD001";

        private static readonly DiagnosticDescriptor s_rule = new(
            DiagnosticId,
            "Preserve PowerShell engine exceptions",
            "Use 'catch (Exception e) when (!PSCmdlet.IsPowerShellControlFlowException(e))' to avoid swallowing PowerShell engine control flow",
            "Reliability",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Broad exception handlers in cmdlets must allow PowerShell engine control-flow exceptions to propagate.");

        private static readonly string[] s_engineExceptionTypes =
        [
            "System.Management.Automation.FlowControlException",
            "System.Management.Automation.PipelineStoppedException",
            "System.Management.Automation.ActionPreferenceStopException",
            "System.Management.Automation.HaltCommandException",
            "System.Management.Automation.RuntimeException",
            "System.Reflection.TargetInvocationException",
        ];

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(startContext =>
            {
                INamedTypeSymbol? cmdlet = startContext.Compilation.GetTypeByMetadataName("System.Management.Automation.Cmdlet");
                INamedTypeSymbol? psCmdlet = startContext.Compilation.GetTypeByMetadataName("System.Management.Automation.PSCmdlet");
                if (cmdlet is null || psCmdlet is null)
                {
                    return;
                }

                var engineTypes = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
                foreach (string name in s_engineExceptionTypes)
                {
                    INamedTypeSymbol? type = startContext.Compilation.GetTypeByMetadataName(name);
                    if (type is not null)
                    {
                        engineTypes.Add(type);
                    }
                }

                ImmutableArray<INamedTypeSymbol> exceptions = engineTypes.ToImmutable();
                startContext.RegisterSyntaxNodeAction(
                    nodeContext => AnalyzeCatch(nodeContext, cmdlet, psCmdlet, exceptions),
                    SyntaxKind.CatchClause);
            });
        }

        private static void AnalyzeCatch(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol cmdlet,
            INamedTypeSymbol psCmdlet,
            ImmutableArray<INamedTypeSymbol> engineTypes)
        {
            var clause = (CatchClauseSyntax)context.Node;
            ISymbol? enclosing = context.SemanticModel.GetEnclosingSymbol(clause.SpanStart, context.CancellationToken);
            if (!DerivesFrom(enclosing?.ContainingType, cmdlet))
            {
                return;
            }

            if (!CatchesEngineExceptions(clause, context, engineTypes)
                || RethrowsImmediately(clause)
                || HasEngineExceptionFilter(clause, context, psCmdlet))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(s_rule, clause.CatchKeyword.GetLocation()));
        }

        private static bool CatchesEngineExceptions(
            CatchClauseSyntax clause,
            SyntaxNodeAnalysisContext context,
            ImmutableArray<INamedTypeSymbol> engineTypes)
        {
            if (clause.Declaration is null)
            {
                return true;
            }

            ITypeSymbol? caughtType = context.SemanticModel.GetTypeInfo(
                clause.Declaration.Type, context.CancellationToken).Type;
            foreach (INamedTypeSymbol engineType in engineTypes)
            {
                if (DerivesFrom(engineType, caughtType) || DerivesFrom(caughtType, engineType))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool RethrowsImmediately(CatchClauseSyntax clause)
        {
            return clause.Block.Statements.Count == 1
                && clause.Block.Statements[0] is ThrowStatementSyntax { Expression: null };
        }

        private static bool HasEngineExceptionFilter(
            CatchClauseSyntax clause,
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol psCmdlet)
        {
            return clause.Declaration is not null && clause.Filter is not null
                && context.SemanticModel.GetDeclaredSymbol(clause.Declaration, context.CancellationToken) is ILocalSymbol exception
                && ExcludesEngineExceptions(clause.Filter.FilterExpression, context, psCmdlet, exception);
        }

        private static bool DerivesFrom(ITypeSymbol? type, ITypeSymbol? baseType)
        {
            if (baseType is null)
            {
                return false;
            }

            for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseType))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ExcludesEngineExceptions(
            ExpressionSyntax expression,
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol psCmdlet,
            ILocalSymbol exception)
        {
            expression = Unwrap(expression);
            if (expression is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.LogicalAndExpression))
            {
                return ExcludesEngineExceptions(binary.Left, context, psCmdlet, exception)
                    || ExcludesEngineExceptions(binary.Right, context, psCmdlet, exception);
            }

            if (expression is PrefixUnaryExpressionSyntax negation && negation.IsKind(SyntaxKind.LogicalNotExpression))
            {
                return IsEnginePredicate(negation.Operand, context, psCmdlet, exception);
            }

            if (expression is BinaryExpressionSyntax comparison && comparison.IsKind(SyntaxKind.EqualsExpression))
            {
                return (Unwrap(comparison.Right).IsKind(SyntaxKind.FalseLiteralExpression)
                        && IsEnginePredicate(comparison.Left, context, psCmdlet, exception))
                    || (Unwrap(comparison.Left).IsKind(SyntaxKind.FalseLiteralExpression)
                        && IsEnginePredicate(comparison.Right, context, psCmdlet, exception));
            }

            return false;
        }

        private static bool IsEnginePredicate(
            ExpressionSyntax expression,
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol psCmdlet,
            ILocalSymbol exception)
        {
            if (Unwrap(expression) is not InvocationExpressionSyntax invocation
                || invocation.ArgumentList.Arguments.Count != 1
                || context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method
                || !method.IsStatic
                || method.Name != "IsPowerShellControlFlowException"
                || !SymbolEqualityComparer.Default.Equals(method.ContainingType, psCmdlet))
            {
                return false;
            }

            ISymbol? argument = context.SemanticModel.GetSymbolInfo(
                Unwrap(invocation.ArgumentList.Arguments[0].Expression), context.CancellationToken).Symbol;
            return SymbolEqualityComparer.Default.Equals(argument, exception);
        }

        private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
        {
            while (expression is ParenthesizedExpressionSyntax parenthesized)
            {
                expression = parenthesized.Expression;
            }

            return expression;
        }
    }
}
