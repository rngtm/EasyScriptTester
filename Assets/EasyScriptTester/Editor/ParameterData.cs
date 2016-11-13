///-----------------------------------
/// EasyScriptTester
/// @ 2016 RNGTM(https://github.com/rngtm)
///-----------------------------------
namespace EasyScriptTester
{
    using System;
    using System.Reflection;

    /// <summary>
    /// parameter data
    /// </summary>
    public class ParameterData
    {
        /// <summary>
        /// パラメーターに関する情報
        /// </summary>
        public ParameterInfo ParameterInfo { get; private set; }

        /// <summary>
        /// パラメーターの値
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ParameterData(ParameterInfo parameterInfo)
        {
            this.ParameterInfo = parameterInfo;
            this.Value = GetDefaultValue(parameterInfo.ParameterType);
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
    }
}