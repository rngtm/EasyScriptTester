///-----------------------------------
/// EasyScriptTester
/// @ 2016 RNGTM(https://github.com/rngtm)
///-----------------------------------
namespace EasyScriptTester
{
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
            this.Parameters = methodInfo.GetParameters()
                .Select(p => new ParameterData(p))
                .ToArray();
        }
    }
}
