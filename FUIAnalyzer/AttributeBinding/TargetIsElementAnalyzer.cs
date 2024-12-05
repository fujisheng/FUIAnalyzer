using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using System.Collections.Immutable;
using System.Linq;

namespace FUIAnalyzer.AttributeBinding
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TargetIsElementAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "FUI[001]";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";
        static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
        }

        void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is AttributeSyntax attribute))
            {
                return;
            }

            var attributeType = context.SemanticModel.GetTypeInfo(attribute).Type as INamedTypeSymbol;
            if (!attributeType.IsType(typeof(FUI.BindingAttribute)))
            {
                return;
            }

            if (!(attribute.Parent is PropertyDeclarationSyntax property))
            {
                return;
            }

            var propertyType = context.SemanticModel.GetTypeInfo(property.Type).Type;
            var converterInfo = GetConverterType(context, attribute);
            var targetPropertyType = GetTargetPropertyType(context, attribute);

            if (converterInfo == default)
            {
                if (!propertyType.InheritsFromOrEquals(targetPropertyType))
                {
                    Report(context, attributeType);
                }
            }
            else
            {
                if (!propertyType.InheritsFromOrEquals(converterInfo.sourceType))
                {
                    Report(context, attributeType);
                }

                if (!targetPropertyType.InheritsFromOrEquals(converterInfo.targetType))
                {
                    Report(context, attributeType);
                }
            }
        }

        void Report(SyntaxNodeAnalysisContext context, INamedTypeSymbol namedTypeSymbol)
        {
            var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

            context.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// 获取目标绑定属性的值类型
        /// </summary>
        INamedTypeSymbol GetTargetPropertyType(SyntaxNodeAnalysisContext context, AttributeSyntax attribute)
        {
            var targetArgs = attribute.ArgumentList.Arguments
                .FirstOrDefault((item) => item.Expression is InvocationExpressionSyntax invocation
                && invocation.Expression.ToString() == "nameof");

            if (targetArgs == null)
            {
                return null;
            }

            var targetInvocationArgs = targetArgs.Expression as InvocationExpressionSyntax;
            var memberAccess = targetInvocationArgs.ArgumentList.Arguments[0].ChildNodes().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
            if (memberAccess == null)
            {
                return null;
            }

            var typeInfo = context.SemanticModel.GetTypeInfo(memberAccess);

            foreach (var @interface in typeInfo.Type.AllInterfaces)
            {
                if (@interface.IsGenericType && @interface.ToString().StartsWith("FUI.Bindable.IBindableProperty"))
                {
                    return @interface.TypeArguments[0] as INamedTypeSymbol;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取绑定的转换器源类型和目标类型
        /// </summary>
        (INamedTypeSymbol sourceType, INamedTypeSymbol targetType) GetConverterType(SyntaxNodeAnalysisContext context, AttributeSyntax attribute)
        {
            var converterArgs = attribute.ArgumentList.Arguments
                .FirstOrDefault((item) => item.Expression is InvocationExpressionSyntax invocation
                && invocation.Expression.ToString() == "typeof");

            if (converterArgs == null)
            {
                return default;
            }

            var typeInfo = context.SemanticModel.GetTypeInfo(converterArgs.Expression);
            foreach(var @interface in typeInfo.Type.AllInterfaces)
            {
                if (@interface.IsGenericType && @interface.ToString().StartsWith("FUI.IValueConverter"))
                {
                    return (@interface.TypeArguments[0] as INamedTypeSymbol, @interface.TypeArguments[1] as INamedTypeSymbol);
                }
            }

            return default;
        }
    }
}
