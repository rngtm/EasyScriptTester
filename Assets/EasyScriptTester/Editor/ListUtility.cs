///-----------------------------------
/// EasyScriptTester
/// @ 2016 RNGTM(https://github.com/rngtm)
///-----------------------------------
namespace EasyScriptTester
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class ListUtility
    {
        public static IList CreateList(Type elementType)
        {
            Type genericListType = typeof(List<>).MakeGenericType(elementType);
            return (IList)Activator.CreateInstance(genericListType);
        }
    }
}
