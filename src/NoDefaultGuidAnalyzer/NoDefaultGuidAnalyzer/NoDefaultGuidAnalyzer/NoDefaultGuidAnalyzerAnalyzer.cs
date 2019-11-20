using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NoDefaultGuidAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NoDefaultGuidAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NoDefaultGuid";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Usage";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var invocationExpr = (InvocationExpressionSyntax)context.Node;

            if (!(invocationExpr.Expression is MemberAccessExpressionSyntax memberAccessExpr))
                return;

            if (memberAccessExpr.Name.ToString() != nameof(Guid.NewGuid))
                return;

            // Find if it's a System.Guid
            var memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpr).Symbol as IMethodSymbol;
            if (memberSymbol == null)
                return;

            var declaringTypeName = string.Format(
                "{0}.{1}",
                memberSymbol.ContainingType.ContainingNamespace.Name,
                memberSymbol.ContainingType.Name);

            var target = Type.GetType(declaringTypeName);
            var systemGuid = typeof(Guid);

            if (target == systemGuid)
            {
                var diagnostic = Diagnostic.Create(Rule, invocationExpr.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
