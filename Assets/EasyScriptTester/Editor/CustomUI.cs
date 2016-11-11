///-----------------------------------
/// EasyScriptTester
/// @ 2016 RNGTM(https://github.com/rngtm)
///-----------------------------------
namespace EasyScriptTester
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    public static class CustomUI
    {
        /// <summary>
        /// 入力フィールドのLabelの大きさ (横) 
        /// </summary>
        private const int LabelWidth = 180;

        /// <summary>
        /// 入力フィールドのLabelの大きさ (縦)
        /// </summary>
        private const int LabelHeight = 16;

        /// <summary>
        /// 入力フィールドの大きさ
        /// </summary>
        private const int InputWidth = 300;

        /// <summary>
        /// Indent幅
        /// </summary>
        private const float IndentWidth = 16f;

        /// <summary>
        /// Typeに応じた入力フィールド表示ロジック (Labelあり)
        /// </summary>
        private static Dictionary<Type, Func<string, object, object>> InputFieldActionDictWithLabel { get; set; }

        /// <summary>
        /// Typeに応じた入力フィールド表示ロジック (Labelなし)
        /// </summary>
        private static Dictionary<Type, Func<object, object>> InputFieldActionDict { get; set; }

        /// <summary>
        /// 入力フィールドのGUILayoutOption
        /// </summary>
        private static GUILayoutOption[] GUILayoutOptions = new GUILayoutOption[0];

        /// <summary>
        /// 初期化処理 
        /// </summary>
        public static void Initialize()
        {
            InitializeInputFieldAction();
        }

        /// <summary>
        /// FoldOutをかっこよく表示
        /// </summary>
        public static bool Foldout(string title, bool display)
        {
            var style = new GUIStyle("ShurikenModuleTitle");
            style.font = new GUIStyle(EditorStyles.label).font;
            style.border = new RectOffset(15, 7, 4, 4);
            style.fixedHeight = 22;
            style.contentOffset = new Vector2(20f, -2f);

            var rect = GUILayoutUtility.GetRect(16f, 22f, style);
            GUI.Box(rect, title, style);

            var e = Event.current;

            var toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
            if (e.type == EventType.Repaint)
            {
                EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
            }

            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                display = !display;
                e.Use();
            }

            return display;
        }

        /// <summary>
        /// 入力フィールドの表示
        /// </summary>
        public static object InputField(String name, Type type, object _object)
        {
            object result;

            if (InputFieldActionDictWithLabel.ContainsKey(type))
            {
                result = InputFieldActionDictWithLabel[type].Invoke(name, _object);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                // EditorGUILayout.LabelField(name, GUILayout.Width(LabelWidth));
                EditorGUILayout.SelectableLabel(name, GUILayout.Width(LabelWidth), GUILayout.Height(LabelHeight));

                result = InputFieldInternal(type, _object);
                EditorGUILayout.EndHorizontal();
            }

            return result;
        }

        /// <summary>
        /// 入力フィールドの表示
        /// </summary>
        private static object InputFieldInternal(Type type, object _object)
        {
            if (InputFieldActionDict.ContainsKey(type))
            {
                return InputFieldActionDict[type].Invoke(_object);
            }

            // UnityEngine.Object
            if (type.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                return InputFieldActionDict[typeof(UnityEngine.Object)].Invoke(_object);
            }

            // Enum
            if (type.IsEnum)
            {
                return EditorGUILayout.EnumPopup((Enum)_object, GUILayoutOptions);
            }

            // Unregistered Types
            EditorGUILayout.LabelField("not supported");
            return null;
        }

        /// <summary>
        /// 入力フィールドロジックの登録
        /// </summary>
        private static void InitializeInputFieldAction()
        {
            InputFieldActionDict = new Dictionary<Type, Func<object, object>>();
            InputFieldActionDictWithLabel = new Dictionary<Type, Func<string, object, object>>();

            // Register default actions
            RegisterInputFieldAction(typeof(Int32), (obj) => EditorGUILayout.IntField((Int32)obj, GUILayoutOptions));
            RegisterInputFieldAction(typeof(Double), (obj) => EditorGUILayout.DoubleField((Double)obj, GUILayoutOptions));
            RegisterInputFieldAction(typeof(String), (obj) => EditorGUILayout.TextField((String)obj, GUILayoutOptions));
            RegisterInputFieldAction(typeof(Boolean), (obj) => EditorGUILayout.Toggle((Boolean)obj, GUILayoutOptions));
            RegisterInputFieldAction(typeof(Single), (obj) => EditorGUILayout.FloatField((Single)obj, GUILayoutOptions));
            RegisterInputFieldAction(typeof(Color), (obj) => EditorGUILayout.ColorField((Color)obj, GUILayoutOptions));
            RegisterInputFieldAction(typeof(UnityEngine.Object), (obj) => EditorGUILayout.ObjectField((UnityEngine.Object)obj, typeof(UnityEngine.Object), true, GUILayoutOptions));
            RegisterInputFieldAction(typeof(Int64), (obj) => EditorGUILayout.LongField((Int64)obj, GUILayoutOptions));
            RegisterInputFieldAction(typeof(Char), (obj) => CharField((Char)obj, GUILayoutOptions));

            RegisterInputFieldActionWithLabel(typeof(Vector2), (name, obj) => Vector2Field(name, (Vector2)obj, GUILayoutOptions));
            RegisterInputFieldActionWithLabel(typeof(Vector3), (name, obj) => Vector3Field(name, (Vector3)obj, GUILayoutOptions));
            RegisterInputFieldActionWithLabel(typeof(Vector4), (name, obj) => Vector4Field(name, (Vector4)obj, GUILayoutOptions));

            RegisterInputFieldAction(typeof(System.Type), (obj) => TypeField((String)obj));

            // Register array actions
            RegisterInputFieldAction(typeof(Int32[]), ArrayInputField);
            RegisterInputFieldAction(typeof(String[]), ArrayInputField);
            RegisterInputFieldAction(typeof(Boolean[]), ArrayInputField);
            RegisterInputFieldAction(typeof(Single[]), ArrayInputField);
            RegisterInputFieldAction(typeof(Vector2[]), ArrayInputField);
            RegisterInputFieldAction(typeof(Vector3[]), ArrayInputField);
            RegisterInputFieldAction(typeof(Vector4[]), ArrayInputField);
            RegisterInputFieldAction(typeof(Color[]), ArrayInputField);

            RegisterInputFieldAction(typeof(Int32[][]), ArrayInputField);
            RegisterInputFieldAction(typeof(String[][]), ArrayInputField);
            RegisterInputFieldAction(typeof(Boolean[][]), ArrayInputField);
            RegisterInputFieldAction(typeof(Single[][]), ArrayInputField);
            RegisterInputFieldAction(typeof(Vector2[][]), ArrayInputField);
            RegisterInputFieldAction(typeof(Vector3[][]), ArrayInputField);
            RegisterInputFieldAction(typeof(Vector4[][]), ArrayInputField);
            RegisterInputFieldAction(typeof(Color[][]), ArrayInputField);

            RegisterInputFieldAction(typeof(Int32[,]), ArrayInputField);
            RegisterInputFieldAction(typeof(String[,]), ArrayInputField);
            RegisterInputFieldAction(typeof(Boolean[,]), ArrayInputField);
            RegisterInputFieldAction(typeof(Single[,]), ArrayInputField);
            RegisterInputFieldAction(typeof(Vector2[,]), ArrayInputField);
            RegisterInputFieldAction(typeof(Vector3[,]), ArrayInputField);
            RegisterInputFieldAction(typeof(Vector4[,]), ArrayInputField);
            RegisterInputFieldAction(typeof(Color[,]), ArrayInputField);
        }

        /// <summary>
        /// 入力フィールドロジックの登録 (Labelなし)
        /// </summary>
        private static void RegisterInputFieldAction(Type type, Func<object, object> func)
        {
            InputFieldActionDict.Add(type, func);
        }

        /// <summary>
        /// 入力フィールドロジックの登録 (Labelあり)
        /// </summary>
        private static void RegisterInputFieldActionWithLabel(Type type, Func<string, object, object> func)
        {
            InputFieldActionDictWithLabel.Add(type, func);
        }

        /// <summary>
        /// char型の入力フィールド
        /// </summary>
        /// <returns></returns>
        private static char CharField(char c, params GUILayoutOption[] option)
        {
            var s = EditorGUILayout.TextField(c.ToString(), GUILayoutOptions);
            return c == default(char) ? ' ' : s[0];
        }

        /// <summary>
        /// Vector2の入力フィールド
        /// </summary>
        private static Vector2 Vector2Field(string label, Vector2 v, params GUILayoutOption[] options)
        {
            EditorGUILayout.SelectableLabel(label, GUILayout.Height(LabelHeight));
            EditorGUI.indentLevel++;
            v.x = FloatField("x", v.x);
            v.y = FloatField("y", v.y);
            EditorGUI.indentLevel--;
            return v;
        }

        /// <summary>
        /// Vector3の入力フィールド
        /// </summary>
        private static Vector3 Vector3Field(string label, Vector3 v, params GUILayoutOption[] options)
        {
            EditorGUILayout.SelectableLabel(label, GUILayout.Height(LabelHeight));
            EditorGUI.indentLevel++;
            v.x = FloatField("x", v.x);
            v.y = FloatField("y", v.y);
            v.z = FloatField("z", v.z);
            EditorGUI.indentLevel--;
            return v;
        }

        /// <summary>
        /// Vector4の入力フィールド
        /// </summary>
        private static Vector4 Vector4Field(string label, Vector4 v, params GUILayoutOption[] options)
        {
            EditorGUILayout.SelectableLabel(label, GUILayout.Height(LabelHeight));
            EditorGUI.indentLevel++;
            v.x = FloatField("x", v.x);
            v.y = FloatField("y", v.y);
            v.z = FloatField("z", v.z);
            v.w = FloatField("w", v.w);
            EditorGUI.indentLevel--;
            return v;
        }

        /// <summary>
        /// floatの入力フィールド
        /// </summary>
        private static float FloatField(string label, float value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(LabelWidth - IndentWidth));
            value = EditorGUILayout.FloatField(value);
            EditorGUILayout.EndHorizontal();

            return value;
        }

        /// <summary>
        /// System.Typeの入力フィールド
        /// </summary>
        private static string TypeField(string typeName)
        {
            return EditorGUILayout.TextField(typeName);
        }

        /// <summary>
        /// 配列の入力フィールド
        /// </summary>
        static object ArrayInputField(object obj)
        {
            EditorGUILayout.LabelField("array is not supported");
            return null;

            //if (obj == null) { return null; }

            //var array = (Array)obj;
            //switch (array.Rank)
            //{
            //    case 1:
            //        EditorGUI.indentLevel++;
            //        for (int i = 0; i < array.Length; i++)
            //        {
            //            var value = array.GetValue(i);
            //            return InputField("Element " + i, value.GetType(), value);
            //        }
            //        EditorGUI.indentLevel--;
            //        break;
            //    case 2:
            //        EditorGUI.indentLevel++;
            //        for (int i = 0; i < array.GetLength(0); i++)
            //        {
            //            EditorGUILayout.LabelField("Element " + i);
            //            EditorGUI.indentLevel++;
            //            for (int j = 0; j < array.GetLength(1); j++)
            //            {
            //                var value = array.GetValue(i, j);
            //                return InputField("Element " + j, value.GetType(), value);
            //            }
            //            EditorGUI.indentLevel--;
            //        }
            //        EditorGUI.indentLevel--;
            //        break;
            //}

            //throw new System.NotImplementedException();
        }
    }
}
