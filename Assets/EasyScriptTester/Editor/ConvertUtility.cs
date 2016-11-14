///-----------------------------------
/// EasyScriptTester
/// @ 2016 RNGTM(https://github.com/rngtm)
///-----------------------------------
namespace EasyScriptTester
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    public static class ConvertUtility
    {
        /// <summary>
        /// 変換ロジック (非ジェネリック)
        /// </summary>
        private static Dictionary<Type, Func<object, string>> _convertNonGenericTypeAction = new Dictionary<Type, Func<object, string>>();

        /// <summary>
        /// 変換ロジック (ジェネリックタイプ)
        /// </summary>
        private static Dictionary<Type, Func<object, string>> _convertGenericTypeAction = new Dictionary<Type, Func<object, string>>();

        /// <summary>
        /// 初期化処理 
        /// </summary>
        public static void Initialize()
        {
            InitializeConvertAction();
        }

        /// <summary>
        /// objectをstringに変換する
        /// </summary>
        public static string Convert(object obj, Type objType)
        {
            if (objType.IsGenericType) // generic
            {
                return ConvertGeneric(obj, objType);
            }
            else // non generic
            {
                return ConvertNonGeneric(obj, objType);
            }
        }

        /// <summary>
        /// objectをstringに変換する (ジェネリック)
        /// </summary>
        private static string ConvertGeneric(object obj, Type objType)
        {
            var genericType = objType.GetGenericTypeDefinition();

            if (_convertGenericTypeAction.ContainsKey(genericType))
            {
                return _convertGenericTypeAction[genericType].Invoke(obj);
            }

            return ToString(obj);
        }

        /// <summary>
        /// objectをstringに変換する (非ジェネリック)
        /// </summary>
        public static string ConvertNonGeneric(object obj, Type objType)
        {
            if (_convertNonGenericTypeAction.ContainsKey(objType))
            {
                return _convertNonGenericTypeAction[objType].Invoke(obj);
            }

            // array
            if (objType.IsArray)
            {
                var elementType = objType.GetElementType(); // 配列の型

                if (_convertNonGenericTypeAction.ContainsKey(elementType))
                {
                    var array = (Array)obj;
                    switch (array.Rank)
                    {
                        case 1:
                            return string.Join(", ", Enumerable.Range(0, array.Length)
                                .Select(i => array.GetValue(i))
                                .Select(o => _convertNonGenericTypeAction[elementType].Invoke(o))
                                .ToArray()
                            );
                        default:
                            break;
                    }
                }
            }

            // unregistered type
            return ToString(obj);
        }

        /// <summary>
        /// 変換ロジックの設定 
        /// </summary>
        private static void InitializeConvertAction()
        {
            _convertGenericTypeAction.Clear();
            _convertNonGenericTypeAction.Clear();

            // Register default actions
            RegisterNonGenericConvertAction(typeof(Int32), (obj) => ToString(obj));
            RegisterNonGenericConvertAction(typeof(Double), (obj) => ToString(obj));
            RegisterNonGenericConvertAction(typeof(String), (obj) => ToString(obj));
            RegisterNonGenericConvertAction(typeof(Boolean), (obj) => ToString(obj));
            RegisterNonGenericConvertAction(typeof(Single), (obj) => ToString(obj));
            RegisterNonGenericConvertAction(typeof(Color), (obj) => ToString(obj));
            RegisterNonGenericConvertAction(typeof(Int64), (obj) => ToString(obj));
            RegisterNonGenericConvertAction(typeof(Char), (obj) => ToString(obj));
            RegisterNonGenericConvertAction(typeof(Vector2), (obj) => "Vector2" + ToString(obj));
            RegisterNonGenericConvertAction(typeof(Vector3), (obj) => "Vector3" + ToString(obj));
            RegisterNonGenericConvertAction(typeof(Vector4), (obj) => "Vector4" + ToString(obj));
            RegisterNonGenericConvertAction(typeof(UnityEngine.Object), (obj) => ToString(obj));

            RegisterNonGenericConvertAction(typeof(IEnumerable), (obj) => IEnumerableConvertAction(obj));
            RegisterNonGenericConvertAction(typeof(IEnumerator), (obj) => IEnumeratorConvertAction(obj));
            RegisterNonGenericConvertAction(typeof(IList), (obj) => IEnumerableConvertAction(obj));

            // Register Generic actions
            RegisterGenericConvertAction(typeof(List<>), (obj) => IEnumerableConvertAction(obj));
            RegisterGenericConvertAction(typeof(IEnumerable<>), (obj) => IEnumerableConvertAction(obj));
            RegisterGenericConvertAction(typeof(IEnumerator<>), (obj) => IEnumerableConvertAction(obj));
            RegisterGenericConvertAction(typeof(IList<>), (obj) => IEnumerableConvertAction(obj));
        }

        /// <summary>
        /// 変換ロジックの設定 (ジェネリック)
        /// </summary>
        private static void RegisterGenericConvertAction(Type type, Func<object, string> action)
        {
            _convertGenericTypeAction.Add(type, action);
        }

        /// <summary>
        /// 変換ロジックの設定 (非ジェネリック)
        /// </summary>
        private static void RegisterNonGenericConvertAction(Type type, Func<object, string> action)
        {
            _convertNonGenericTypeAction.Add(type, action);
        }

        /// <summary>
        /// IEnumerable型の変換ロジック 
        /// </summary>
        private static string IEnumerableConvertAction(object obj)
        {
            string result = "";
            foreach (object item in (IEnumerable)obj)
            {
                result += ToString(item) + ", ";
            }
            return result;
        }

        /// <summary>
        /// IEnumerator型の変換ロジック 
        /// </summary>
        private static string IEnumeratorConvertAction(object obj)
        {
            string result = "";
            try
            {
                var iterator = (IEnumerator)obj;
                while (iterator.MoveNext())
                {
                    result += ToString(iterator.Current) + ", ";
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            return result;
        }

        /// <summary>
        /// Array型の変換ロジック 
        /// </summary>
        private static string ArrayConvertAction(object obj)
        {
            string result = "";
            foreach (object item in (Array)obj)
            {
                result += ToString(item) + ", ";
            }
            return result;
        }

        private static string ToString(object obj)
        {
            if (obj == null)
            {
                return "null";
            }
            else
            {
                return obj.ToString();
            }
        }
    }
}
