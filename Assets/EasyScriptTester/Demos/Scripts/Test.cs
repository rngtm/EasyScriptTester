///-----------------------------------
/// EasyScriptTester
/// @ 2016 RNGTM(https://github.com/rngtm)
///-----------------------------------
namespace EasyScriptTester.Demos
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Demo用のクラス
    /// </summary>
    public class Test
    {
        int GetInt()
        {
            return 10;
        }

        Vector4[] GetVectors()
        {
            return new Vector4[]
            {
                new Vector4(1, 2, 3, 4),
                new Vector4(5, 6, 7, 8),
            };
        }

        Color GetColor()
        {
            return Color.yellow;
        }

        IEnumerable GetIEnumerable1()
        {
            return Enumerable.Range(0, 3);
        }

        IEnumerable<int> GetIEnumerable2()
        {
            return Enumerable.Range(0, 3);
        }

        IEnumerator GetIEnumerator()
        {
            yield return 1;
            yield return 2;
            yield return null;
            yield return "A";
        }
    }
}

