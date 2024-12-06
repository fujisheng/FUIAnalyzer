namespace FUIAnalyzer
{
    internal class RuleIds
    {
        /// <summary>
        /// 绑定目标不是FUI.IElement
        /// </summary>
        internal const string TargetNotElementRuleId = "FUI0001";

        /// <summary>
        /// 绑定目标属性不可绑定
        /// </summary>
        internal const string TargetPropertyNotBindableRuleId = "FUI0002";

        /// <summary>
        /// 转换器不是FUI.IConverter
        /// </summary>
        internal const string ConverterNotIConverterRuleId = "FUI0003";

        /// <summary>
        /// 属性到目标值无法转换
        /// </summary>
        internal const string PropertyToTargetWithoutConverterRuleId = "FUI0004";

        /// <summary>
        /// 属性到目标通过转换器无法转换
        /// </summary>
        internal const string PropertyToTargetWithConverterRuleId = "FUI0005";

        /// <summary>
        /// 目标字段字符串赋值必须使用nameof(xxx.yyy)
        /// </summary>
        internal const string TargetMustBeNameOfRuleId = "FUI0006";
    }
}
