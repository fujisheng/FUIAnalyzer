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
        #region Rules
        /// <summary>
        /// 绑定目标组件非FUI.IElement
        /// </summary>
        static readonly DiagnosticDescriptor TargetNotElementRule = Utility.CreateAttributeBindingRule(
            RuleIds.TargetNotElementRuleId,
            "Target '{0}' not 'FUI.IElement'");

        /// <summary>
        /// 绑定目标属性非 FUI.Bindable.BindableProperty
        /// </summary>
        static readonly DiagnosticDescriptor TargetPropertyNotBindableRule = Utility.CreateAttributeBindingRule(
            RuleIds.TargetPropertyNotBindableRuleId,
            "Target property '{0}' not 'FUI.Bindable.BindableProperty<>'.");

        /// <summary>
        /// 设置的转换器非 FUI.IValueConverter
        /// </summary>
        static readonly DiagnosticDescriptor ConverterNotIConverterRule = Utility.CreateAttributeBindingRule(
            RuleIds.ConverterNotIConverterRuleId,
            "Converter '{0}' not 'FUI.IValueConverter<,>'.");

        /// <summary>
        /// 没有转换器的时候 源属性无法转换成目标属性值类型
        /// </summary>
        static readonly DiagnosticDescriptor PropertyToTargetWithoutConverterRule = Utility.CreateAttributeBindingRule(
            RuleIds.PropertyToTargetWithoutConverterRuleId,
            "Can not convert property type '{0}' to target value type '{1}', Please consider using a 'ValueConverter' or changing the property type.");

        /// <summary>
        /// 有转换器的时候 源属性无法转换成转换器源类型 或 转换器目标类型无法转换成目标属性值类型
        /// </summary>
        static readonly DiagnosticDescriptor PropertyToTargetWithConverterRule = Utility.CreateAttributeBindingRule(
            RuleIds.PropertyToTargetWithConverterRuleId,
            "Can not convert property type '{0}' to converter source type '{1}' or converter target type '{2}' to target value type '{3}'.");

        /// <summary>
        /// 设置目标的时候必须通过nameof来赋值  以防止直接用字符串赋值而无法解析到对应的类型信息
        /// </summary>
        static readonly DiagnosticDescriptor TargetMustBeNameOfRule = Utility.CreateAttributeBindingRule(
            RuleIds.TargetMustBeNameOfRuleId,
            "The target must be assigned a value using 'nameof(Element.Property)'.");
        #endregion

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create
            (TargetNotElementRule, 
            TargetPropertyNotBindableRule, 
            ConverterNotIConverterRule, 
            PropertyToTargetWithoutConverterRule,
            PropertyToTargetWithConverterRule,
            TargetMustBeNameOfRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
        }

        void AnalyzeProperty(SyntaxNodeAnalysisContext context)
        {
            if(!(context.Node is PropertyDeclarationSyntax property))
            {
                return;
            }

            var attribute = property.AttributeLists.SelectMany((list) => list.Attributes)
                .FirstOrDefault((a) => context.SemanticModel.GetTypeInfo(a).Type.IsType(typeof(FUI.BindingAttribute)));

            if(attribute == null)
            {
                return;
            }

            //解析Binding标签  判断是否合法
            var propertyType = context.SemanticModel.GetTypeInfo(property.Type).Type;
            var converterInfo = GetConverterType(context, attribute);
            var targetPropertyType = GetTargetPropertyType(context, attribute);

            //如果为空则返回
            if(propertyType == null || targetPropertyType == null)
            {
                return;
            }

            if (converterInfo == default)
            {
                //如果没有转换器且属性类型无法转换成目标值类型
                if (!propertyType.InheritsFromOrEquals(targetPropertyType))
                {
                    var diagnostic = Diagnostic.Create(PropertyToTargetWithoutConverterRule, attribute.GetLocation(), propertyType, targetPropertyType);
                    context.ReportDiagnostic(diagnostic);
                }
            }
            else
            {
                //如果有转换器，但是无法将属性类型转换成转换器源类型，或无法将转换器目标类型转换成绑定目标值类型
                if (!propertyType.InheritsFromOrEquals(converterInfo.sourceType) 
                    || !converterInfo.targetType.InheritsFromOrEquals(targetPropertyType))
                {
                    var diagnostic = Diagnostic.Create(PropertyToTargetWithConverterRule, attribute.GetLocation(), propertyType, converterInfo.sourceType, converterInfo.targetType, targetPropertyType);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        /// <summary>
        /// 获取目标绑定属性的值类型
        /// </summary>
        INamedTypeSymbol GetTargetPropertyType(SyntaxNodeAnalysisContext context, AttributeSyntax attribute)
        {
            //找到nameof
            var targetArgs = attribute.ArgumentList.Arguments
                .FirstOrDefault((item) => item.Expression is InvocationExpressionSyntax invocation
                && invocation.Expression.ToString() == "nameof");

            //如果没有找到则报错
            if (targetArgs == null)
            {
                var diagnostic = Diagnostic.Create(TargetMustBeNameOfRule, attribute.GetLocation());
                context.ReportDiagnostic(diagnostic);
                return null;
            }

            //找到nameof 里面的成员访问
            var targetInvocationArgs = targetArgs.Expression as InvocationExpressionSyntax;
            var memberAccess = targetInvocationArgs.ArgumentList.Arguments[0].ChildNodes().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
            if (memberAccess == null)
            {
                return null;
            }

            //判断目标类型是否是IElement
            var targetTypeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression);
            if(targetTypeInfo.Type.AllInterfaces.FirstOrDefault((item) => item.ToString().StartsWith("FUI.IElement")) == null)
            {
                var diagnostic = Diagnostic.Create(TargetNotElementRule, memberAccess.Expression.GetLocation(), targetTypeInfo.Type);
                context.ReportDiagnostic(diagnostic);
                return null;
            }

            //判断目标成员类型是否是BindableProperty
            var targetPropertyType = context.SemanticModel.GetTypeInfo(memberAccess.Name);
            var @interface = targetPropertyType.Type.AllInterfaces.FirstOrDefault(item => item.IsGenericType && item.ToString().StartsWith("FUI.Bindable.IBindableProperty"));
            if(@interface == null)
            {
                var diagnostic = Diagnostic.Create(TargetPropertyNotBindableRule, memberAccess.Name.GetLocation(), targetPropertyType.Type);
                context.ReportDiagnostic(diagnostic);
                return null;
            }

            //获取目标成员值类型
            var targetValueType = @interface.TypeArguments[0] as INamedTypeSymbol;
            return targetValueType;
        }

        /// <summary>
        /// 获取绑定的转换器源类型和目标类型
        /// </summary>
        (INamedTypeSymbol sourceType, INamedTypeSymbol targetType) GetConverterType(SyntaxNodeAnalysisContext context, AttributeSyntax attribute)
        {
            //找到typeof
            var converterTypeOf = attribute.ArgumentList.Arguments
                .FirstOrDefault((item) => item.Expression is TypeOfExpressionSyntax);

            if (converterTypeOf == null)
            {
                return default;
            }

            //找到对应的类型
            var typeofExpression = converterTypeOf.Expression as TypeOfExpressionSyntax;
            var typeInfo = context.SemanticModel.GetTypeInfo(typeofExpression.Type);

            //判断是否继承自IValueConverter<>
            var interfaces = typeInfo.Type.AllInterfaces.FirstOrDefault(item => item.IsGenericType && item.ToString().StartsWith("FUI.IValueConverter"));

            //如果不继承 则报错
            if(interfaces == null)
            {
                var diagnostic = Diagnostic.Create(ConverterNotIConverterRule, typeofExpression.Type.GetLocation(), typeInfo.Type);
                context.ReportDiagnostic(diagnostic);
                return default;
            }

            //返回其sourcesType和targetType
            var sourceType = interfaces.TypeArguments[0] as INamedTypeSymbol;
            var targetType = interfaces.TypeArguments[1] as INamedTypeSymbol;
            return (sourceType, targetType);
        }
    }
}
