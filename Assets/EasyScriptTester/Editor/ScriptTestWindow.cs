///-----------------------------------
/// EasyScriptTester
/// @ 2016 RNGTM(https://github.com/rngtm)
///-----------------------------------
namespace EasyScriptTester
{
    using System;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.Callbacks;
    using UnityEditor.SceneManagement;

    /// <summary>
    /// This is a script testing window.
    /// </summary>
    public class ScriptTestWindow : EditorWindow
    {
        private const string VersionLabel = "EasyScriptTester v1.3";

        /// <summary>
        /// メソッド実行ボタンの大きさ
        /// </summary>
        private const float MethodCallButtonWidth = 32f;

        /// <summary>
        /// メソッド実行ボタンの大きさ
        /// </summary>
        private const float MethodCallButtonHeight = 17f;

        /// <summary>
        /// インデント幅
        /// </summary>
        private const float IndentSize = 18f;

        #region Variables
        /// <summary>
        /// 選択しているObjectのComponenttのデータ
        /// </summary>
        private ComponentData[] componentDatas;

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
        private static bool _needSetCallbacks = true;

        /// <summary>
        /// コンポーネント名をすべて表示するかどうか
        /// </summary>
        private static bool _isShowComponentFullName = true;

        /// <summary>
        /// 引数のタイプをすべて表示するかどうか
        /// </summary>
        private static bool _isShowParameterTypeFullName = false;

        private static IEnumerable<MonoScript> _monoScripts;
        #endregion Variables

        #region Properties
        /// <summary>
        /// プロジェクトに存在する全スクリプトのアセット
        /// </summary>
        private static IEnumerable<MonoScript> MonoScripts { get { return _monoScripts ?? (_monoScripts = Resources.FindObjectsOfTypeAll<MonoScript>()); } }
        #endregion Properties

        /// <summary>
        /// ウィンドウを開く
        /// </summary>
        [MenuItem("Tools/Easy Script Tester", false, 10000)]
        private static void Open()
        {
            _needSetCallbacks = true;
            GetWindow<ScriptTestWindow>();
        }

        /// <summary>
        /// EditorWindowの描画
        /// </summary>
        private void OnGUI()
        {
            CustomUI.VersionLabel(VersionLabel);
            
            EditorGUILayout.LabelField("選択しているオブジェクトのメソッド一覧が表示されます。");
            EditorGUILayout.Space();

            if (this.componentDatas == null) { return; }

            this.scrollPosition = EditorGUILayout.BeginScrollView(this.scrollPosition);
            this.ShowComponents();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// コンポーネントとメソッドの一覧を表示
        /// </summary>
        private void ShowComponents()
        {
            foreach (var item in this.componentDatas.Select((data, i) => new { componentData = data, index = i }))
            {
                var index = item.index;
                if (index >= isOpen.Length) { return; }

                // コンポーネント 表示
                var componentData = item.componentData;
                var componentType = componentData.ComponentType;
                var objectType = componentData.ObjectType;
                var scriptType = componentData.ScriptType;
                var componentLabel = this.GetComponentLabel(componentType, objectType, scriptType);
                isOpen[index] = CustomUI.Foldout(componentLabel, isOpen[index]);
                GUILayout.Space(-22f);
                CustomUI.ScriptOpenButton(componentType.Name);

                GUILayout.Space(18f);

                if (isOpen[index] == false) { continue; }
                GUILayout.Space(-2f);

                // メソッド一覧 表示
                this.componentDatas[index].MethodDatas.ToList().ForEach(method =>
                {
                    GUILayout.Space(3f);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(IndentSize);
                    
                    if (GUILayout.Button("実行", GUILayout.Width(MethodCallButtonWidth), GUILayout.Height(MethodCallButtonHeight))) // メソッド実行ボタン
                    {
                        try
                        {
                            var parameters = method.Parameters
                                .Select(p => 
                                {
                                    if (p.ReorderableList == null)
                                    {
                                        return Convert.ChangeType(p.Value, p.ParameterInfo.ParameterType);
                                    }
                                    else  // 配列
                                    {
                                        var elementType = p.ParameterInfo.ParameterType.GetElementType();
                                        return (IList)TypeUtility.ToArray(p.ReorderableList.list, elementType);
                                        
                                    }
                                })
                                .ToArray();

                            object result = null;
                            if (IsEditorScript(componentType)) // Editor script
                            {
                                result = InvokeEditorScriptMethod(method, componentType, parameters);
                            }
                            else
                            if (componentType.IsSubclassOf(typeof(MonoBehaviour))) // MonoBehaviour script
                            {
                                result = InvokeMonobehaviourMethod(method, componentType, objectType, parameters, item.componentData.Object);
                            }
                            else // other script
                            {
                                result = InvokeOtherMethod(method, componentType, parameters);
                            }

                            var msg = method.MethodInfo.Name + " ( " + ToString(parameters) + " )";
                            var isMonobehaviour = componentType.IsSubclassOf(typeof(MonoBehaviour));
                            var isCoroutine = method.MethodInfo.ReturnType == typeof(IEnumerator);
                            if (!(isMonobehaviour && isCoroutine)) // コルーチンの場合はメソッド返却値を表示させない
                            {
                                msg += "\n-> " + ConvertUtility.Convert(result, method.MethodInfo.ReturnType);
                            }
                            Debug.Log(msg);

                            if (EditorApplication.isPaused && objectType == ObjectType.GameObject) 
                            {
                                // 全てのシーンにDirtyフラグを入れる
                                Enumerable.Range(0, EditorSceneManager.sceneCount)
                                .Select(i => EditorSceneManager.GetSceneAt(i))
                                .ToList()
                                .ForEach(s =>  EditorSceneManager.MarkSceneDirty(s));
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }

                    // メソッド表示
                    EditorGUILayout.LabelField(method.MethodInfo.Name + " : " + method.MethodInfo.ReturnType.Name);

                    EditorGUILayout.EndHorizontal();

                    // メソッドが引数を持っていた場合は引数入力フィールドを出す
                    method.Parameters.ToList().ForEach(p =>
                    {
                        string label = this.GetParameterLabel(p.ParameterInfo);

                        GUILayout.Space(-1f);
                        EditorGUI.indentLevel += 2;
                        p.Value = CustomUI.InputField(label, p.ParameterInfo.ParameterType, p.Value);
                        EditorGUI.indentLevel -= 2;

                        // 配列入力フィールドの表示
                        var reorderableList = p.ReorderableList;
                        if (reorderableList != null)
                        {
                            CustomUI.DoLayoutReorderableList(reorderableList);
                            GUILayout.Space(reorderableList.GetHeight() - 19f);
                        }
                    });

                    GUILayout.Space(1f);
                    this.Line(1);
                });

                GUILayout.Space(12f);
            }
        }

        /// <summary>
        /// object[]をstringに変換
        /// </summary>
        public static string ToString(object[] objs)
        {
            if (objs == null)
            {
                return "";
            }
            else
            {
                return string.Join(", ", objs
                    .Select(obj => ToString(obj))
                    .ToArray());
            }
        }

        /// <summary>
        /// objectをstringに変換
        /// </summary>
        public static string ToString(object obj)
        {
            if (obj == null) { return "null"; }

            string result = string.Empty;

            var objType = obj.GetType();
            if (objType.IsArray) // array
            {
                result += "[";
                result += string.Join(", ",
                ((Array)obj)
                .ToEnumerable()
                .ToList()
                .Select(o => 
                {
                    if (o == null)
                    {
                        return "null";
                    }
                    else
                    {
                        if (o.GetType().IsArray)
                        {
                            string s = "[";
                            var array = (Array)o;
                            for (int i = 0; i < array.Length; i++)
                            {
                                s += array.GetValue(i);
                                if (i < array.Length - 1) { s += ", "; }
                            }
                            return o.ToString();
                        }
                        else
                        {
                            return o.ToString();
                        }
                    }
                }).ToArray());
                // .Aggregate((s, next) => s + ", " + next);
                result += "]";
            }
            else // not array
            { 
                result += obj.ToString();
            }

            return result;
        }

        /// <summary>
        /// Monobehaviour継承クラスのメソッドを実行する
        /// </summary>
        private static object InvokeMonobehaviourMethod(MethodData method, Type componentType, ObjectType objectType, object[] parameters, object selectObject)
        {
            object result;
            switch (objectType)
            {
                case ObjectType.GameObject:
                    result = InvokeMonobehaviourComponentMethod(method, componentType, parameters, selectObject);
                    break;
                case ObjectType.MonoScript:
                    // メソッド実行
                    result = InvokeMonobehaviourScriptMethod(method, componentType, parameters);
                    break;
                default:
                    throw new System.NotImplementedException();
            }

            return result;
        }

        /// <summary>
        /// Monobehaviourスクリプトアセットのメソッドを実行する
        /// </summary>
        private static object InvokeMonobehaviourScriptMethod(MethodData method, Type componentType, object[] parameters)
        {
            object result;
            var newGameObject = new GameObject();
            var component = newGameObject.AddComponent(componentType);
            result = method.MethodInfo.Invoke(component, parameters);
            DestroyImmediate(newGameObject);

            if (method.MethodInfo.ReturnType == typeof(IEnumerator))
            {
                Debug.LogWarning("Monobehaviour継承スクリプトのファイルからのコルーチン実行は非対応です\nExecuting Coroutine in Monobehaviour script file is not supported");
            }

            return result;
        }

        /// <summary>
        /// Monobehaviourコンポーネントのメソッドを実行する
        /// </summary>
        private static object InvokeMonobehaviourComponentMethod(MethodData method, Type componentType, object[] parameters, object selectObject)
        {
            object result;
            if (method.MethodInfo.ReturnType == typeof(IEnumerator))
            {
                // コルーチン実行
                var component = (selectObject as GameObject).GetComponent(componentType);
                var monoBehaviourType = componentType.BaseType;
                var argumentTypes = new Type[] { typeof(IEnumerator) };
                var coroutine = method.MethodInfo.Invoke(component, parameters);
                var startCoroutine = monoBehaviourType.GetMethod("StartCoroutine", argumentTypes);

                EditorApplication.delayCall += () => // 1フレーム遅らせる
                {
                    startCoroutine.Invoke(component, new object[] { coroutine });
                };

                if (EditorApplication.isPlaying == false) { Debug.LogWarning("コルーチンはエディタ停止中には実行できません\nCoroutine can only be executed in play mode"); }
                result = coroutine;
            }
            else
            {
                // メソッド実行
                var component = (selectObject as GameObject).GetComponent(componentType);
                result = method.MethodInfo.Invoke(component, parameters);
            }

            return result;
        }

        /// <summary>
        /// エディタースクリプトのメソッドを実行する
        /// </summary>
        private static object InvokeEditorScriptMethod(MethodData method, Type componentType, object[] parameters)
        {
            object result;
            if (componentType.IsSubclassOf(typeof(Editor)))
            {
                var instance = ScriptableObject.CreateInstance(componentType);
                result = method.MethodInfo.Invoke(instance, parameters);
            }
            else
            if (componentType.IsSubclassOf(typeof(EditorWindow)))
            {
                var instance = ScriptableObject.CreateInstance(componentType);
                result = method.MethodInfo.Invoke(instance, parameters);
            }
            else
            {
                var instance = Activator.CreateInstance(componentType);
                result = method.MethodInfo.Invoke(instance, parameters);
            }
            return result;
        }

        /// <summary>
        /// その他オブジェクトのメソッドを実行する
        /// </summary>
        private static object InvokeOtherMethod(MethodData method, Type componentType, object[] parameters)
        {
            object result;
            var instance = Activator.CreateInstance(componentType);
            result = method.MethodInfo.Invoke(instance, parameters);
            return result;
        }

        /// <summary>
        /// ComponentのFoldOut用のラベルを取得
        /// </summary>
        private string GetComponentLabel(Type componentType, ObjectType objectType, ScriptType scriptType)
        {
            var componentName = (_isShowComponentFullName) ? componentType.FullName : componentType.Name;
            var extension = string.Empty;

            switch (scriptType)
            {
                case ScriptType.None:
                    extension = "";
                    break;
                case ScriptType.CSharp:
                    extension = ".cs";
                    break;
                case ScriptType.JavaScript:
                    extension = ".js";
                    break;
            }

            switch (objectType)
            {
                case ObjectType.GameObject:
                    return componentName;
                case ObjectType.MonoScript:
                    return componentName + extension;
                default:
                    throw new System.NotImplementedException();
            }
        }

        /// <summary>
        /// 引数入力フィールド用のラベルを取得
        /// </summary>
        private string GetParameterLabel(System.Reflection.ParameterInfo parameterInfo)
        {
            if (_isShowParameterTypeFullName)
            {
                return parameterInfo.Name + "  : " + parameterInfo.ParameterType.ToString();
            }
            else
            {
                return parameterInfo.Name + "  : " + parameterInfo.ParameterType.Name;
            }
        }

        /// <summary>
        /// コンポーネントがEditorスクリプトかどうか
        /// </summary>
        private static bool IsEditorScript(Type componentType)
        {
            return componentType.Module.Name == "Assembly-CSharp-Editor.dll";
        }

        /// <summary>
        /// スクリプトロード時に呼ばれる
        /// </summary>
        [DidReloadScripts]
        private static void OnDidReloadScripts()
        {
            _needSetCallbacks = true;
        }

        /// <summary>
        /// 毎フレーム呼ばれる
        /// </summary>
        private void Update()
        {
            if (_needSetCallbacks)
            {
                CustomUI.Initialize();
                ConvertUtility.Initialize();
                _needSetCallbacks = false;
                Selection.selectionChanged += this.Extract;
                this.Extract();
            }

            this.Repaint();
        }

        /// <summary>
        /// 区切り線を表示
        /// </summary>
        private void Line(float size)
        {
            GUILayout.Box("", GUILayout.Width(this.position.width), GUILayout.Height(size));
        }

        /// <summary>
        /// 選択中のオブジェクトからコンポーネントとメソッド一覧を取り出す
        /// </summary>
        private void Extract()
        {
            if (Resources.FindObjectsOfTypeAll<EditorWindow>().Contains(this) == false) { return; }
            if (EditorApplication.isCompiling) { return; }

            var componentDatas = new List<ComponentData>();

            // Hierarchyビューで選択しているGameObject
            Selection.gameObjects
            .Where(o => o != null)
            .Where(o => IsPrefab(o) == false)
            .ToList()
            .ForEach(obj =>
            {
                // コンポーネント取得
                var componentTypes = GetAllComponents(obj)
                    .Where(type => type.ToString().Split('.')[0] != "UnityEngine")
                    .ToArray();
                componentDatas.AddRange(componentTypes.Select(t => new ComponentData(obj, ObjectType.GameObject, t, ScriptType.None)));
            });

            // Projectビューで選択しているスクリプト
            Selection.objects
            .Where(o => o != null)
            .Where(o => IsScript(o) == true)
            .ToList()
            .ForEach(obj =>
            {
                // コンポーネント取得
                var componentType = GetScriptClass(obj);
                var extension = AssetDatabase.GetAssetPath(obj).Split('.').Last();
                var scriptType = default(ScriptType);
                switch (extension)
                {
                    case "cs":
                        scriptType = ScriptType.CSharp;
                        break;
                    case "js":
                        scriptType = ScriptType.JavaScript;
                        break;
                }
                componentDatas.Add(new ComponentData(obj, ObjectType.MonoScript, componentType, scriptType));
            });

            this.componentDatas = componentDatas.Where(c => c.IsSuccess).ToArray();
            this.isOpen = this.componentDatas.Select(x => true).ToArray();
        }

        /// <summary>
        /// ScriptアセットのクラスTypeを取得する
        /// </summary>
        private Type GetScriptClass(UnityEngine.Object script)
        {
            var path = AssetDatabase.GetAssetPath(script);
            var asset = (MonoScript)AssetDatabase.LoadAssetAtPath(path, typeof(MonoScript));
            var scriptClass = MonoScripts.First(ms => ms == asset).GetClass();

            if (scriptClass == null)
            {
                Debug.LogError("class '" + script.name + "' not found \n");
                return null;
            }
            return scriptClass;
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
        /// Scriptかどうか
        /// </summary>
        private static bool IsScript(UnityEngine.Object obj)
        {
            return obj.GetType() == typeof(MonoScript);
        }

        /// <summary>
        /// GameObjectにアタッチされているすべてのコンポーネントを取得
        /// </summary>
        private static IEnumerable<Type> GetAllComponents(GameObject target)
        {
            foreach (var ms in MonoScripts)
            {
                var cls = ms.GetClass();
                if (cls == null) { continue; }
                if (cls.IsSubclassOf(typeof(MonoBehaviour)) && target.GetComponent(cls) != null)
                {
                    yield return cls;
                }
            }
        }
    }
}
