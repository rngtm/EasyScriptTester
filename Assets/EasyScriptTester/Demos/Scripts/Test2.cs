///-----------------------------------
/// EasyScriptTester
/// @ 2016 RNGTM(https://github.com/rngtm)
///-----------------------------------
namespace EasyScriptTester.Demos
{
    using UnityEngine;

    /// <summary>
    /// Demo用のクラス
    /// </summary>
    public static class Test2
    {
        static void Test(int[] x, int y)
        {
            for (int i = 0; i < x.Length; i++)
            {
                Debug.Log(x[i]);
            }
            Debug.Log(y);
        }
    }
}

