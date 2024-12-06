using Microsoft.CodeAnalysis;

namespace FUIAnalyzer
{
    internal static class Utility
    {
        /// <summary>
        /// 创建一个属性绑定规则
        /// </summary>
        /// <param name="id">规则id</param>
        /// <param name="messageFormat">消息</param>
        /// <param name="helpUrl">帮助链接</param>
        /// <returns></returns>
        internal static DiagnosticDescriptor CreateAttributeBindingRule(string id, string messageFormat, string helpUrl = "")
        {
            return new DiagnosticDescriptor(
                id: id,
                title: "InvalidBinding",
                messageFormat: messageFormat,
                category: "FUI",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                helpLinkUri: helpUrl
            );
        }
    }
}
