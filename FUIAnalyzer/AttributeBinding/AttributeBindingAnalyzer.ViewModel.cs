using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using System.Linq;

namespace FUIAnalyzer.AttributeBinding
{
    public partial class AttributeBindingAnalyzer
    {
        /// <summary>
        /// 绑定的对象不是ObservableObject
        /// </summary>
        static readonly DiagnosticDescriptor BindingObjectNotObservableObjectRule = Utility.CreateAttributeBindingRule(
            RuleIds.BindingObjectNotObservableObjectRuleId,
            "Target object '{0}' not 'FUI.Bindable.ObservableObject'.");

        /// <summary>
        /// 绑定的对象参数数量不是1
        /// </summary>
        static readonly DiagnosticDescriptor BindingObjectArgsCountNotOneRule = Utility.CreateAttributeBindingRule(
            RuleIds.BindingObjectArgsCountNotOneRuleId,
            "Binding attribute args count must be 1, but got {0}.");

        /// <summary>
        /// 绑定对象规则
        /// </summary>
        static readonly DiagnosticDescriptor[] BindingObjectRules = new DiagnosticDescriptor[]
        {
            BindingObjectNotObservableObjectRule,
            BindingObjectArgsCountNotOneRule,
        };

        void AnalyzeClass(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is ClassDeclarationSyntax classDeclaration))
            {
                return;
            }

            var attributes = classDeclaration.AttributeLists.SelectMany((list) => list.Attributes);
            foreach (var attribute in attributes)
            {
                if (context.SemanticModel.GetTypeInfo(attribute).Type.IsType(typeof(FUI.BindingAttribute)))
                {
                    AnalyzeClassAttribute(context, classDeclaration, attribute);
                }
            }
        }

        /// <summary>
        /// 分析类型的绑定标签是否合法
        /// </summary>
        void AnalyzeClassAttribute(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclaration, AttributeSyntax attribute)
        {
            var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

            if (!(symbol is INamedTypeSymbol namedType))
            {
                return;
            }

            if (!namedType.InheritsFrom(typeof(FUI.Bindable.ObservableObject)))
            {
                var diagnostic = Diagnostic.Create(BindingObjectNotObservableObjectRule, attribute.GetLocation(), namedType.ToString());
                context.ReportDiagnostic(diagnostic);
                return;
            }

            var argsCount = attribute.ArgumentList?.Arguments.Count ?? 0;
            if(argsCount != 1)
            {
                var diagnostic = Diagnostic.Create(BindingObjectArgsCountNotOneRule, attribute.GetLocation(), argsCount);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
