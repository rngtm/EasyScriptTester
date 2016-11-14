///-----------------------------------
/// EasyScriptTester
/// @ 2016 RNGTM(https://github.com/rngtm)
///-----------------------------------
namespace EasyScriptTester
{
    using System;
    using System.Collections;
    using UnityEngine;

    public class TypeUtility
    {
        public static object ToArray(IList list, Type elementType)
        {
            if (elementType == typeof(bool)) { return ToArray<bool>(list); }
            if (elementType == typeof(char)) { return ToArray<char>(list); }
			if (elementType == typeof(sbyte)) { return ToArray<sbyte>(list); }
			if (elementType == typeof(byte)) { return ToArray<byte>(list); }
			if (elementType == typeof(short)) { return ToArray<short>(list); }
			if (elementType == typeof(ushort)) { return ToArray<ushort>(list); }
			if (elementType == typeof(int)) { return ToArray<int>(list); }
			if (elementType == typeof(uint)) { return ToArray<uint>(list); }
			if (elementType == typeof(long)) { return ToArray<long>(list); }
			if (elementType == typeof(ulong)) { return ToArray<ulong>(list); }
			if (elementType == typeof(float)) { return ToArray<float>(list); }
			if (elementType == typeof(double)) { return ToArray<double>(list); }
			if (elementType == typeof(decimal)) { return ToArray<decimal>(list); }
			if (elementType == typeof(object)) { return ToArray<object>(list); }
			if (elementType == typeof(string)) { return ToArray<string>(list); }
            
			if (elementType == typeof(Vector2)) { return ToArray<Vector2>(list); }
			if (elementType == typeof(Vector3)) { return ToArray<Vector3>(list); }
			if (elementType == typeof(Vector4)) { return ToArray<Vector4>(list); }

            // unregistered type
            return ToArray<object>(list);
        }

        public static T[] ToArray<T>(IList list)
        {
            var newArray = new T[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                newArray[i] = (T)Convert.ChangeType(list[i], typeof(T));
            }
            return newArray;
        }

    }
}