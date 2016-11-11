///-----------------------------------
/// EasyScriptTester
/// @ 2016 RNGTM(https://github.com/rngtm)
///-----------------------------------
namespace EasyScriptTester
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Linq;

    /// <summary>
    /// Component data
    /// </summary>
    public class ComponentData
    {
        #region Fields
        /// <summary>
        /// コンポーネント取得に成功した
        /// </summary>
        public bool IsSuccess { get; private set; }

        /// <summary>
        /// コンポーネントのType
        /// </summary>
        public Type ComponentType { get; private set; }

        /// <summary>
        /// スクリプトの種類
        /// </summary>
        public ScriptType ScriptType { get; private set; }

        /// <summary>
        /// オブジェクトの種類
        /// </summary>
        public ObjectType ObjectType { get; private set; }

        /// <summary>
        /// このコンポーネントを持っているオブジェクト
        /// </summary>
        public Object Object { get; private set; }

        /// <summary>
        /// このコンポーネントが持つメソッドの情報
        /// </summary>
        public MethodData[] MethodDatas { get; private set; }
        #endregion Fields

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ComponentData(Object obj, ObjectType objType, Type componentType, ScriptType scriptType)
        {
            if (componentType == null)
            {
                this.IsSuccess = false;
                return;
            }

            this.IsSuccess = true;
            this.Object = obj;
            this.ObjectType = objType;
            this.ScriptType = scriptType;
            this.ComponentType = componentType;
            this.MethodDatas = ExtractMethods(componentType)
              .Select(methodInfo => new MethodData(methodInfo))
              .ToArray();
        }

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
