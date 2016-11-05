///-----------------------------------
/// EasyScriptTester
/// @ 2016 RNGTM(https://github.com/rngtm)
///-----------------------------------
namespace EasyScriptTester
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.Callbacks;
    using System;
    using System.Reflection;
    using System.Linq;

    /// <summary>
    /// This is a script testing window.
    /// </summary>
    public class ScriptTestWindow : EditorWindow
    {
        /// <summary>
        /// ボタンの大きさ
        /// </summary>
        private const float ButtonSize = 32f;

        /// <summary>
        /// インデント幅
        /// </summary>
        private const float IndentSize = 18f;

        /// <summary>
        /// The empty options.
        /// </summary>
        static GUILayoutOption[] EmptyOptions = new GUILayoutOption[0];

        /// <summary>
        /// Componentのタイプ
        /// </summary>
        private Type[] componentTypes;

        /// <summary>
        /// Componentに対応するメソッド
        /// </summary>
        private MethodData[][] methodsCollection;

        /// <summary>
        /// Scroll位置
        /// </summary>
        private Vector2 scrollPosition = new Vector2(0, 0);

        /// <summary>
        /// Foldingが開いているかどうか
        /// </summary>
        private bool[] isOpen;

        /// <summary>
        /// EditorApplicationコールバック設定フラグ
        /// </summary>
        private static bool needSetCallbacks = true;

        /// <summary>
        /// Typeに応じた入力フィールド表示ロジック
        /// </summary>
        private static Dictionary<Type, Func<string, object, object>> inputFieldActionDict = new Dictionary<Type, Func<string, object, object>>();

        /// <summary>
        /// ウィンドウを開く
        /// </summary>
        [MenuItem("Tools/Easy Script Tester", false, 10000)]
        static void Open()
        {
            needSetCallbacks = true;
            GetWindow<ScriptTestWindow>();
        }

        /// <summary>
        /// EditorWindowの描画
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField("選択しているGameObjectのメソッド一覧が表示されます。");
            EditorGUILayout.Space();

            if (Selection.activeGameObject == null) { return; }
            if (this.componentTypes == null) { return; }
            if (this.methodsCollection == null) { return; }

            this.scrollPosition = EditorGUILayout.BeginScrollView(this.scrollPosition);

            // コンポーネントとメソッド一覧 表示
            foreach (var item in this.componentTypes.Select((t, i) => new { componentType = t, index = i }))
            {
                var index = item.index;
                var componentType = item.componentType;

                // コンポーネント表示
                isOpen[index] = Foldout(componentType.ToString(), isOpen[index]);
                if (isOpen[index] == false) { continue; }

                GUILayout.Space(-5f);

                // メソッド表示
                this.methodsCollection.ElementAt(index).ToList().ForEach(method =>
                {
                    GUILayout.Space(3f);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(IndentSize);
                    if (GUILayout.Button("実行", GUILayout.Width(ButtonSize), GUILayout.Height(17f)))
                    {
                        // メソッド実行
                        var obj = Selection.activeGameObject.GetComponent(componentType);
                        var parameters = method.Parameters
                            .Select(p => Convert.ChangeType(p.Value, p.ParameterInfo.ParameterType))
                            .ToArray();

                        var arg = parameters.Length == 0 ? "" : parameters.Select(p => (p ?? "null").ToString()).Aggregate((s, next) => s + "," + next);
                        Debug.Log("Invoke : " + method.MethodInfo.Name + " (" + arg + ")");
                        method.MethodInfo.Invoke(obj, parameters);
                    }

                    // メソッドを表示
                    EditorGUILayout.LabelField(method.MethodInfo.Name + " : " + method.MethodInfo.ReturnType.ToString().Split('.', '+').Last());
                    EditorGUILayout.EndHorizontal();

                    // メソッドが引数を持っていた場合は引数入力フィールドを出す
                    method.Parameters.ToList().ForEach(p =>
                    {
                        GUILayout.Space(-1f);
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(IndentSize * 2);

                        // 入力フィールドの表示
                        string label = p.ParameterInfo.Name + "  : " + p.ParameterInfo.ParameterType.ToString().Split('.', '+').Last();
                        p.Value = InputField(label, p.ParameterInfo.ParameterType, p.Value);

                        EditorGUILayout.EndHorizontal();
                    });

                    GUILayout.Space(1f);
                    this.Line(1);
                });

                GUILayout.Space(12f);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// スクリプトロード時に呼ばれる
        /// </summary>
        [DidReloadScripts]
        static void OnDidReloadScripts()
        {
            needSetCallbacks = true;
        }

        /// <summary>
        /// 毎フレーム呼ばれる
        /// </summary>
        private void Update()
        {
            if (needSetCallbacks)
            {
                InitializeInputFieldAction();
                needSetCallbacks = false;
                Selection.selectionChanged += this.Extract;
                this.Extract();
            }
            Repaint();
        }

        /// <summary>
        /// 区切り線を表示
        /// </summary>
        private void Line(float size)
        {
            GUILayout.Box("", GUILayout.Width(this.position.width), GUILayout.Height(size));
        }

        /// <summary>
        /// 選択中のGameObjectからコンポーネントとメソッド一覧を取り出す
        /// </summary>
        private void Extract()
        {
            if (Resources.FindObjectsOfTypeAll<EditorWindow>().Contains(this) == false) { return; }

            if (Selection.activeGameObject != null && IsPrefab(Selection.activeGameObject) == false)
            {
                // コンポーネント取得
                this.componentTypes = GetAllComponents(Selection.activeGameObject)
                    .Where(type => type.ToString().Split('.')[0] != "UnityEngine")
                    .ToArray();

                // メソッド取得
                this.methodsCollection = this.componentTypes
                    .Select(type => ExtractMethods(type))
                    .Select(methodInfos => methodInfos.Select(m => new MethodData(m)).ToArray()).ToArray();

                this.isOpen = this.componentTypes.Select(x => true).ToArray();
            }
            else
            {
                this.isOpen = null;
                this.componentTypes = null;
                this.methodsCollection = null;
            }
        }

        /// <summary>
        /// メソッドを一括取得
        /// </summary>
        private static IEnumerable<MethodInfo> ExtractMethods(System.Type type)
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
        /// Prefabかどうか
        /// </summary>
        private static bool IsPrefab(UnityEngine.Object obj)
        {
            switch (PrefabUtility.GetPrefabType(obj))
            {
                case PrefabType.Prefab:
                case PrefabType.ModelPrefab:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// GameObjectにアタッチされているすべてのコンポーネントを取得
        /// </summary>
        private static IEnumerable<Type> GetAllComponents(GameObject target)
        {
            List<Type> typeList = new List<Type>();
            var mss = Resources.FindObjectsOfTypeAll<MonoScript>();
            foreach (var ms in mss)
            {
                var cls = ms.GetClass();
                if (cls != null)
                {
                    if (cls.IsSubclassOf(typeof(MonoBehaviour)) && target.GetComponent(cls) != null)
                    {
                        typeList.Add(cls);
                    }
                }
            }
            return typeList;
        }

        /// <summary>
        /// FoldOutをかっこよく表示
        /// </summary>
        private static bool Foldout(string title, bool display)
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
        static object InputField(String name, Type type, object _object)
        {
            if (inputFieldActionDict.ContainsKey(type))
            {
                return inputFieldActionDict[type].Invoke(name, _object);
            }

            if (type.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                return inputFieldActionDict[typeof(UnityEngine.Object)].Invoke(name, _object);
            }

            // Enum
            if (type.IsEnum)
            {
                return EditorGUILayout.EnumPopup(name, (Enum)_object, EmptyOptions);
            }

            // null check
            if (_object == null)
            {
                EditorGUILayout.TextField(name, null, EmptyOptions);
            }

            // Unregistered Types
            return EditorGUILayout.TextField(name, _object.ToString(), EmptyOptions);
        }

        /// <summary>
        /// 入力フィールドロジックの登録
        /// </summary>
        private static void InitializeInputFieldAction()
        {
            inputFieldActionDict = new Dictionary<Type, Func<string, object, object>>();

            // Register default actions
            RegisterInputFieldAction(typeof(Int32), (name, obj) => EditorGUILayout.IntField(name, (Int32)obj, EmptyOptions));
            RegisterInputFieldAction(typeof(Double), (name, obj) => EditorGUILayout.DoubleField(name, (Double)obj, EmptyOptions));
            RegisterInputFieldAction(typeof(String), (name, obj) => EditorGUILayout.TextField(name, (String)obj, EmptyOptions));
            RegisterInputFieldAction(typeof(Boolean), (name, obj) => EditorGUILayout.Toggle(name, (Boolean)obj, EmptyOptions));
            RegisterInputFieldAction(typeof(Single), (name, obj) => EditorGUILayout.FloatField(name, (Single)obj, EmptyOptions));
            RegisterInputFieldAction(typeof(Vector2), (name, obj) => EditorGUILayout.Vector2Field(name, (Vector2)obj, EmptyOptions));
            RegisterInputFieldAction(typeof(Vector3), (name, obj) => EditorGUILayout.Vector3Field(name, (Vector3)obj, EmptyOptions));
            RegisterInputFieldAction(typeof(Vector4), (name, obj) => EditorGUILayout.Vector4Field(name, (Vector4)obj, EmptyOptions));
            RegisterInputFieldAction(typeof(Color), (name, obj) => EditorGUILayout.ColorField(name, (Color)obj, EmptyOptions));
            RegisterInputFieldAction(typeof(UnityEngine.Object), (name, obj) => EditorGUILayout.ObjectField(name, (UnityEngine.Object)obj, typeof(UnityEngine.Object), true, EmptyOptions));

            RegisterInputFieldAction(typeof(Int64), (name, obj) => EditorGUILayout.LongField(name, (Int64)obj, EmptyOptions));
            RegisterInputFieldAction(typeof(Char), (name, obj) => { EditorGUILayout.LabelField(name, "", EmptyOptions); return '\0'; });

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
        /// 入力フィールドロジックの登録
        /// </summary>
        private static void RegisterInputFieldAction(Type type, Func<string, object, object> func)
        {
            inputFieldActionDict.Add(type, func);
        }

        /// <summary>
        /// 配列の入力フィールド
        /// </summary>
        static object ArrayInputField(string name, object obj)
        {
            EditorGUILayout.LabelField(name);
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
