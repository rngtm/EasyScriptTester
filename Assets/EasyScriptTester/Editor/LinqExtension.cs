///-----------------------------------
/// EasyScriptTester
/// @ 2016 RNGTM(https://github.com/rngtm)
///-----------------------------------
namespace EasyScriptTester
{
    using System;
    using System.Collections.Generic;
    public static class LinqExtension
    {
		/// <summary>
        /// ArrayをIEnumerableへ変換 
        /// </summary>
        public static IEnumerable<object> ToEnumerable(this Array array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                yield return array.GetValue(i);
            }
        }
    }

}