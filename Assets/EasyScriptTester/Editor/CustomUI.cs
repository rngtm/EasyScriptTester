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
    using UnityEditorInternal;

    public static class CustomUI
    {
        /// <summary>
        /// Labelの大きさ (横) 
        /// </summary>
        private const int LabelWidth = 140;

        /// <summary>
        /// Labelの大きさ (縦)
        /// </summary>
        private const int LabelHeight = 16;

        /// <summary>
        /// ReorderableListのLabelの大きさ (横) 
        /// </summary>
        private const int ReorderableLabelWidth = 130;

        /// <summary>
        /// Indent幅
        /// </summary>
        private const float IndentWidth = 16f;

        /// <summary>
        /// Typeに応じた入力フィールド表示ロジック (Rect指定)
        /// </summary>
        private static Dictionary<Type, Func<Rect, object, object>> InputFieldActionDictOfRect { get; set; }

        /// <summary>
        /// Typeに応じた入力フィールド表示ロジック (Rect指定, ラベルあり)
        /// </summary>
        private static Dictionary<Type, Func<Rect, Rect, string, object, object>> InputFieldActionDictOfRectWithLabel { get; set; }

        /// <summary>
        /// Typeに応じた入力フィールド表示ロジック (1行)
        /// </summary>
        private static Dictionary<Type, Func<object, object>> InputFieldActionDict { get; set; }

        /// <summary>
        /// Typeに応じた入力フィールド表示ロジック (複数行)
        /// </summary>
        private static Dictionary<Type, Func<string, object, object>> MultiLineInputFieldActionDict { get; set; }

        /// <summary>
        /// 入力フィールドのGUILayoutOption
        /// </summary>
        private static GUILayoutOption[] InputFieldOptions = new GUILayoutOption[0];

        private static GUIStyle _versionLabelStyle;

        /// <summary>
        /// バージョン情報ラベルのGUIStyle
        /// </summary>
        private static GUIStyle VersionLabelStyle { get { return _versionLabelStyle ?? (_versionLabelStyle = CreateVersionLabelStyle()); } }

        /// <summary>
        /// 初期化処理 
        /// </summary>
        public static void Initialize()
        {
            InitializeInputFieldAction();
            InitializeRectInputFieldAction();
        }

        /// <summary>
        /// 指定した型のReorderableListの作成
        /// </summary>
        public static ReorderableList CreateReorderableList(string headerLabel, Type arrayType)
        {
            var elementType = arrayType.GetElementType();
            var list = ListUtility.CreateList(elementType);
            var reorderableList = new ReorderableList(list, elementType);

            reorderableList.drawHeaderCallback += (rect) =>
            {
                EditorGUI.LabelField(rect, headerLabel);
            };

            reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                float height = rect.size.y;
                rect.size = new Vector2(rect.size.x, height - 1);
                rect.y += 2;
                rect.yMax -= 4;

                var labelRect = new Rect(rect.position.x, rect.position.y, ReorderableLabelWidth, rect.size.y);
                var fieldRect = new Rect(rect.position.x + ReorderableLabelWidth, rect.position.y, rect.size.x - ReorderableLabelWidth, rect.size.y);
                var element = list[index];
                var elementLabel = elementType.Name + "[" + index + "]";

                EditorGUI.BeginChangeCheck();
                var value = InputField(labelRect, fieldRect, elementLabel, elementType, element);
                if (EditorGUI.EndChangeCheck() && value != null)
                {
                    list[index] = value;
                }
            };

            reorderableList.onAddCallback += (index) =>
            {
                list.Add(GetDefaultValue(elementType));
            };

            return reorderableList;
        }

        /// <summary>
        /// バージョン情報を表示するラベル
        /// </summary>
        public static void VersionLabel(string label)
        {
            int width = Screen.width;
            GUI.Label(new Rect(Screen.width  - width - 2, Screen.height - 72, width, 50), label, VersionLabelStyle);
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

            var clickRect = new Rect(rect);
            clickRect.width -= 180f;

            var e = Event.current;

            var drawRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
            if (e.type == EventType.Repaint)
            {
                EditorStyles.foldout.Draw(drawRect, false, false, display, false);
            }

            if (e.type == EventType.MouseDown && clickRect.Contains(e.mousePosition))
            {
                display = !display;
                e.Use();
            }

            return display;
        }

        /// <summary>
        /// スクリプトを開くボタンの表示
        /// </summary>
        public static void ScriptOpenButton(string scriptName)
        {
            var style = new GUIStyle(GUI.skin.button);
            var text = new GUIContent("Open");
            var rect = GUILayoutUtility.GetRect(text, style, GUILayout.ExpandWidth(false));
            rect.center = new Vector2(EditorGUIUtility.currentViewWidth - rect.width / 2 - 12, rect.center.y - 2);

            if (GUI.Button(rect, text, GUI.skin.button)) { OpenInEditor(scriptName, 0); }

            GUILayout.Space(-GUI.skin.button.lineHeight - 7f);
        }


        /// <summary>
        /// 入力フィールドの表示
        /// </summary>
        public static object InputField(string name, Type type, object _object)
        {
            object result = null;
            if (MultiLineInputFieldActionDict.ContainsKey(type))
            {
                result = MultiLineInputFieldActionDict[type].Invoke(name, _object);
            }
            else
            if (InputFieldActionDict.ContainsKey(type))
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.SelectableLabel(name, GUILayout.Width(LabelWidth), GUILayout.Height(LabelHeight));

                result = InputFieldInternal(type, _object);

                GUILayout.Space(12f);
                EditorGUILayout.EndHorizontal();
            }

            return result;
        }

        /// <summary>
        /// ReorderableListの表示を行う
        /// </summary>
        public static void DoLayoutReorderableList(ReorderableList reorderableList)
        {
            GUILayout.Space(2f);    
            var rect = GUILayoutUtility.GetRect(16f, 22f);
            rect.x += 28f;
            rect.width -= 40f;
            reorderableList.DoList(rect);
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
                return EditorGUILayout.EnumPopup((Enum)_object, InputFieldOptions);
            }

            // Unregistered Types
            EditorGUILayout.LabelField("not supported");
            return null;
        }


        /// <summary>
        /// 入力フィールドの表示 (Rect指定)
        /// </summary>
        private static object InputField(Rect labelRect, Rect fieldRect, string label, Type type, object _object)
        {
            object result = null;
            if (InputFieldActionDictOfRectWithLabel.ContainsKey(type))
            {
                result = InputFieldActionDictOfRectWithLabel[type].Invoke(labelRect, fieldRect, label, _object);
            }
            else
            if (InputFieldActionDictOfRect.ContainsKey(type))
            {
                EditorGUI.LabelField(labelRect, label);
                result = InputFieldInternal(fieldRect, type, _object);
            }
            else
            {
                EditorGUI.LabelField(labelRect, label);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.TextField(fieldRect, "[not supported]");
                EditorGUI.EndDisabledGroup();
            }

            return result;
        }

        /// <summary>
        /// 入力フィールドの表示 (Rect指定)
        /// </summary>
        private static object InputFieldInternal(Rect fieldRect, Type type, object _object)
        {
            if (InputFieldActionDict.ContainsKey(type))
            {
                return InputFieldActionDictOfRect[type].Invoke(fieldRect, _object);
            }

            // UnityEngine.Object
            if (type.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                return InputFieldActionDictOfRect[typeof(UnityEngine.Object)].Invoke(fieldRect, _object);
            }

            // Enum
            if (type.IsEnum)
            {
                return EditorGUI.EnumPopup(fieldRect, (Enum)_object);
            }

            // Unregistered Types
            EditorGUI.LabelField(fieldRect, "not supported");
            return null;
        }


        /// <summary>
        /// 入力フィールドロジックの登録
        /// </summary>
        private static void InitializeInputFieldAction()
        {
            InputFieldActionDict = new Dictionary<Type, Func<object, object>>();
            MultiLineInputFieldActionDict = new Dictionary<Type, Func<string, object, object>>();

            // Register default actions
            RegisterInputFieldAction(typeof(Int32), (obj) => EditorGUILayout.IntField((Int32)obj, InputFieldOptions));
            RegisterInputFieldAction(typeof(Double), (obj) => EditorGUILayout.DoubleField((Double)obj, InputFieldOptions));
            RegisterInputFieldAction(typeof(String), (obj) => EditorGUILayout.TextField((String)obj, InputFieldOptions));
            RegisterInputFieldAction(typeof(Boolean), (obj) => EditorGUILayout.Toggle((Boolean)obj, InputFieldOptions));
            RegisterInputFieldAction(typeof(Single), (obj) => EditorGUILayout.FloatField((Single)obj, InputFieldOptions));
            RegisterInputFieldAction(typeof(Color), (obj) => EditorGUILayout.ColorField((Color)obj, InputFieldOptions));
            RegisterInputFieldAction(typeof(UnityEngine.Object), (obj) => EditorGUILayout.ObjectField((UnityEngine.Object)obj, typeof(UnityEngine.Object), true, InputFieldOptions));
            RegisterInputFieldAction(typeof(Int64), (obj) => EditorGUILayout.LongField((Int64)obj, InputFieldOptions));
            RegisterInputFieldAction(typeof(Char), (obj) => CharField((Char)obj, InputFieldOptions));

            RegisterInputFieldAction(typeof(System.Type), (obj) => TypeField((String)obj));

            RegisterInputFieldActionMultiLine(typeof(Vector2), (name, obj) => Vector2Field(name, (Vector2)obj));
            RegisterInputFieldActionMultiLine(typeof(Vector3), (name, obj) => Vector3Field(name, (Vector3)obj));
            RegisterInputFieldActionMultiLine(typeof(Vector4), (name, obj) => Vector4Field(name, (Vector4)obj));
        }

        /// <summary>
        /// 入力フィールドロジックの登録 (Rect指定)
        /// </summary>
        private static void InitializeRectInputFieldAction()
        {
            InputFieldActionDictOfRect = new Dictionary<Type, Func<Rect, object, object>>();
            InputFieldActionDictOfRectWithLabel = new Dictionary<Type, Func<Rect, Rect, string, object, object>>();

            // Register default actions
            RegisterInputFieldAction(typeof(Int32), (rect, obj) => EditorGUI.IntField(rect, (Int32)obj));
            RegisterInputFieldAction(typeof(Double), (rect, obj) => EditorGUI.DoubleField(rect, (Double)obj));
            RegisterInputFieldAction(typeof(String), (rect, obj) => EditorGUI.TextField(rect, (String)obj));
            RegisterInputFieldAction(typeof(Boolean), (rect, obj) => EditorGUI.Toggle(rect, (Boolean)obj));
            RegisterInputFieldAction(typeof(Single), (rect, obj) => EditorGUI.FloatField(rect, (Single)obj));
            RegisterInputFieldAction(typeof(Color), (rect, obj) => EditorGUI.ColorField(rect, (Color)obj));
            RegisterInputFieldAction(typeof(UnityEngine.Object), (rect, obj) => EditorGUI.ObjectField(rect, (UnityEngine.Object)obj, typeof(UnityEngine.Object), true));
            RegisterInputFieldAction(typeof(Int64), (rect, obj) => EditorGUI.LongField(rect, (Int64)obj));
            RegisterInputFieldAction(typeof(Char), (rect, obj) => CharField(rect, (Char)obj));

            RegisterInputFieldAction(typeof(System.Type), (rect, obj) => TypeField(rect, (String)obj));

            RegisterInputFieldActionWithLabel(typeof(Vector2), (labelRect, fieldRect, name, obj) => Vector2Field(labelRect, fieldRect, name, (Vector2)obj));
            RegisterInputFieldActionWithLabel(typeof(Vector3), (labelRect, fieldRect, name, obj) => Vector3Field(labelRect, fieldRect, name, (Vector3)obj));
            RegisterInputFieldActionWithLabel(typeof(Vector4), (labelRect, fieldRect, name, obj) => Vector4Field(labelRect, fieldRect, name, (Vector4)obj));
        }

        /// <summary>
        /// 入力フィールドロジックの登録 (1行)
        /// </summary>
        private static void RegisterInputFieldAction(Type type, Func<object, object> func)
        {
            InputFieldActionDict.Add(type, func);
        }

        /// <summary>
        /// 入力フィールドロジックの登録 (複数行)
        /// </summary>
        private static void RegisterInputFieldActionMultiLine(Type type, Func<string, object, object> func)
        {
            MultiLineInputFieldActionDict.Add(type, func);
        }

        /// <summary>
        /// 入力フィールドロジックの登録 (Rect指定)
        /// </summary>
        private static void RegisterInputFieldAction(Type type, Func<Rect, object, object> func)
        {
            InputFieldActionDictOfRect.Add(type, func);
        }


        /// <summary>
        /// 入力フィールドロジックの登録 (Rect指定, ラベルあり)
        /// </summary>
        private static void RegisterInputFieldActionWithLabel(Type type, Func<Rect, Rect, string, object, object> func)
        {
            InputFieldActionDictOfRectWithLabel.Add(type, func);
        }

        /// <summary>
        /// char型の入力フィールド
        /// </summary>
        /// <returns></returns>
        private static char CharField(char c, params GUILayoutOption[] option)
        {
            var s = EditorGUILayout.TextField(c.ToString(), InputFieldOptions);
            return c == default(char) ? ' ' : s[0];
        }

        /// <summary>
        /// char型の入力フィールド (Rect指定)
        /// </summary>
        private static char CharField(Rect rect, char c)
        {
            var s = EditorGUI.TextField(rect, c.ToString());
            return c == default(char) ? ' ' : s[0];
        }

        /// <summary>
        /// Vector2の入力フィールド
        /// </summary>
        private static Vector2 Vector2Field(string label, Vector2 v)
        {
            EditorGUILayout.SelectableLabel(label, GUILayout.Height(LabelHeight));
            EditorGUI.indentLevel++;
            v.x = FloatField("x", v.x);
            v.y = FloatField("y", v.y);
            EditorGUI.indentLevel--;
            return v;
        }
        
        /// <summary>
        /// Vector2の入力フィールド (Rect指定)
        /// </summary>
        private static Vector2 Vector2Field(Rect labelRect, Rect fieldRect, string label, Vector2 v)
        {
            EditorGUI.LabelField(labelRect, label);

            fieldRect.x += 20f;
            fieldRect.width /= 2f;
            
            Rect xLabelRect = fieldRect;
            Rect yLabelRect = fieldRect;
            // Rect zLabelRect = fieldRect;

            Rect xFieldRect = fieldRect;
            Rect yFieldRect = fieldRect;
            // Rect zFieldRect = fieldRect;

            float space = 30f;
            yFieldRect.x += (fieldRect.width + 8f) * 1f;
            // zFieldRect.x += (fieldRect.width + 4f) * 2f;

            xFieldRect.width -= space;
            yFieldRect.width -= space;
            // zFieldRect.width -= space;

            float labelSpace = 13f;
            xLabelRect.x = xFieldRect.x - labelSpace;
            yLabelRect.x = yFieldRect.x - labelSpace;
            // zLabelRect.x = zFieldRect.x - labelSpace;
            
            v.x = EditorGUI.FloatField(xFieldRect, v.x);
            v.y = EditorGUI.FloatField(yFieldRect, v.y);
            // v.z = EditorGUI.FloatField(zFieldRect, v.z);

            EditorGUI.LabelField(xLabelRect, "x");
            EditorGUI.LabelField(yLabelRect, "y");
            // EditorGUI.LabelField(zLabelRect, "z");
            return v;
        }

        /// <summary>
        /// Vector3の入力フィールド
        /// </summary>
        private static Vector3 Vector3Field(string label, Vector3 v)
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
        /// Vector3の入力フィールド (Rect指定)
        /// </summary>
        private static Vector3 Vector3Field(Rect labelRect, Rect fieldRect, string label, Vector3 v)
        {
            EditorGUI.LabelField(labelRect, label);

            fieldRect.x += 20f;
            fieldRect.width /= 3f;
            
            Rect xLabelRect = fieldRect;
            Rect yLabelRect = fieldRect;
            Rect zLabelRect = fieldRect;

            Rect xFieldRect = fieldRect;
            Rect yFieldRect = fieldRect;
            Rect zFieldRect = fieldRect;

            float space = 30f;
            yFieldRect.x += (fieldRect.width + 4f) * 1f;
            zFieldRect.x += (fieldRect.width + 4f) * 2f;

            xFieldRect.width -= space;
            yFieldRect.width -= space;
            zFieldRect.width -= space;

            float labelSpace = 12f;
            xLabelRect.x = xFieldRect.x - labelSpace;
            yLabelRect.x = yFieldRect.x - labelSpace;
            zLabelRect.x = zFieldRect.x - labelSpace;
            
            v.x = EditorGUI.FloatField(xFieldRect, v.x);
            v.y = EditorGUI.FloatField(yFieldRect, v.y);
            v.z = EditorGUI.FloatField(zFieldRect, v.z);

            EditorGUI.LabelField(xLabelRect, "x");
            EditorGUI.LabelField(yLabelRect, "y");
            EditorGUI.LabelField(zLabelRect, "z");
            return v;
        }

        /// <summary>
        /// Vector4の入力フィールド
        /// </summary>
        private static Vector4 Vector4Field(string label, Vector4 v)
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
        /// Vector3の入力フィールド (Rect指定)
        /// </summary>
        private static Vector4 Vector4Field(Rect labelRect, Rect fieldRect, string label, Vector4 v)
        {
            EditorGUI.LabelField(labelRect, label);

            fieldRect.x += 20f;
            fieldRect.width /= 4f;

            var labelRects = new Rect[4];
            var fieldRects = new Rect[4];
            for (int i = 0; i < labelRects.Length; i++)
            {
                labelRects[i] = fieldRect;
                fieldRects[i] = fieldRect;
            }

            float space = 30f;
            float labelSpace = 13f;
            for (int i = 0; i < fieldRects.Length; i++)
            {
                fieldRects[i].x += (fieldRect.width + 2.5f) * i;
                fieldRects[i].width -= space;
                labelRects[i].x = fieldRects[i].x - labelSpace;
            }
            
            v.x = EditorGUI.FloatField(fieldRects[0], v.x);
            v.y = EditorGUI.FloatField(fieldRects[1], v.y);
            v.z = EditorGUI.FloatField(fieldRects[2], v.z);
            v.w = EditorGUI.FloatField(fieldRects[3], v.w);

            EditorGUI.LabelField(labelRects[0], "x");
            EditorGUI.LabelField(labelRects[1], "y");
            EditorGUI.LabelField(labelRects[2], "z");
            EditorGUI.LabelField(labelRects[3], "w");
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
        /// System.Typeの入力フィールド (Rect指定)
        /// </summary>
        private static string TypeField(Rect rect, string typeName)
        {
            return EditorGUI.TextField(rect, typeName);
        }

        /// <summary>
        /// スクリプトを外部エディタで開く
        /// </summary>
        private static void OpenInEditor(string scriptName, int scriptLine)
        {
            string[] paths = AssetDatabase.GetAllAssetPaths();

            foreach (string path in paths)
            {
                string scriptPath = System.IO.Path.GetFileNameWithoutExtension(path);
                if (scriptPath.Equals(scriptName))
                {
                    MonoScript script = AssetDatabase.LoadAssetAtPath(path, typeof(MonoScript)) as MonoScript;
                    if (script != null)
                    {
                        if (!AssetDatabase.OpenAsset(script, scriptLine))
                        {
                            Debug.LogWarning("Couldn't open script : " + scriptName);
                        }
                        break;
                    }
                    else
                    {
                        Debug.LogWarning("Couldn't open script : " + scriptName);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Typeの規定値を取得
        /// </summary>
        private static object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            return null;
        }

        /// <summary>
        /// バージョン情報ラベルのGUIStyleを作成する
        /// </summary>
        /// <returns></returns>
        
        private static GUIStyle CreateVersionLabelStyle()
        {
            var style = new GUIStyle(GUI.skin.GetStyle("Label"));
            var color = new Color(style.normal.textColor.r, style.normal.textColor.g, style.normal.textColor.b, 0.4f);

            style.alignment = TextAnchor.LowerRight;
            style.normal.textColor = color;

            return style;
        }
    }
}
