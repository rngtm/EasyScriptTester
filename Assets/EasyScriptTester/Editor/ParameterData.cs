///-----------------------------------
/// EasyScriptTester
/// @ 2016 RNGTM(https://github.com/rngtm)
///-----------------------------------
namespace EasyScriptTester
{
    using System;
    using System.Reflection;
    using UnityEditor;
    using UnityEditorInternal;

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

        public ReorderableList ReorderableList { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ParameterData(ParameterInfo parameterInfo)
        {
            this.ParameterInfo = parameterInfo;
            var parameterType = this.ParameterInfo.ParameterType;
            if (parameterType.IsArray)
            {
                var headerLabel = parameterInfo.Name + " : " + parameterType.Name;
                this.ReorderableList = CustomUI.CreateReorderableList(headerLabel, parameterType);
            }
            else
            {
                this.Value = GetDefaultValue(parameterInfo.ParameterType);
            }
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