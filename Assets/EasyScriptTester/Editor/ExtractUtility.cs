///-----------------------------------
/// EasyScriptTester
/// @ 2016 RNGTM(https://github.com/rngtm)
///-----------------------------------
namespace EasyScriptTester
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEngine;

    public class ExtractUtility : MonoBehaviour
    {
        /// <summary>
        /// メソッドの一括取得
        /// </summary>
        public static IEnumerable<MethodInfo> ExtractMethods(System.Type type)
        {
            var methods = type.GetMethods(
              BindingFlags.Static
              | BindingFlags.Public
              | BindingFlags.NonPublic
              | BindingFlags.Instance
              );

            return methods
              .Where(m => m.DeclaringType == type)
              .Where(m => m.Name[0] != '<')
              .Where(m => IsProperty(m) == false);
        }

        /// <summary>
        /// Propertyかどうか
        /// </summary>
        private static bool IsProperty(MethodInfo methodInfo)
        {
            switch (methodInfo.Name.Split('_')[0])
            {
                case "get":
                case "set":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 変数・プロパティの一括取得
        /// </summary>
        public static IEnumerable<MemberData> ExtractMembers(object obj)
        {
            var type = obj.GetType();
            var members = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

            foreach (var member in members)
            {
                if (member.MemberType == MemberTypes.Field)
                {
                    var field = (FieldInfo)member;
                    yield return new MemberData
                    {
                        Name = field.Name,
                        Value = field.GetValue(obj),
                    };
                    continue;
                }

                if (member.MemberType == MemberTypes.Property)
                {
                    var property = (PropertyInfo)member;
                    yield return new MemberData
                    {
                        Name = property.Name,
                        Value = property.GetValue(obj, null),
                    };
                    continue;
                }
            }
        }
    }
}
