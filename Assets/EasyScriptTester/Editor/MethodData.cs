///-----------------------------------
/// EasyScriptTester
/// @ 2016 RNGTM(https://github.com/rngtm)
///-----------------------------------
namespace EasyScriptTester
{
    using System;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Method data
    /// </summary>
    public class MethodData
    {
        /// <summary>
        /// MethodInfo
        /// </summary>
        public MethodInfo MethodInfo { get; private set; }

        /// <summary>
        /// メソッドのパラメーターに関する情報
        /// </summary>
        public ParameterData[] Parameters { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MethodData(MethodInfo methodInfo)
        {
            this.MethodInfo = methodInfo;

            this.Parameters = methodInfo.GetParameters().Select(p =>
            {
                return new MethodData.ParameterData
                {
                    ParameterInfo = p,
                    Value = GetDefaultValue(p.ParameterType),
                };
            }).ToArray();
        }

        /// <summary>
        /// Typeの規定値を取得
        /// </summary>
        private static object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            return null;
        }

        public class ParameterData
        {
            public ParameterInfo ParameterInfo;
            public object Value;
        }

    }
}
