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

    /// <summary>
    /// This is a script testing window.
    /// </summary>
    public class ScriptTestWindow : EditorWindow
    {
        /// <summary>
        /// ボタンの大きさ
        /// </summary>
        private const float ButtonWidth = 32f;

        /// <summary>
        /// ボタンの大きさ
        /// </summary>
        private const float ButtonHeight = 17f;

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
        /// プロジェクトに存在する全スクリプトアセット
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
            EditorGUILayout.LabelField("選択しているオブジェクトのメソッド一覧が表示されます。");
            EditorGUILayout.Space();

            if (this.componentDatas == null) { return; }

            this.scrollPosition = EditorGUILayout.BeginScrollView(this.scrollPosition);

            // コンポーネントとメソッド一覧 表示
            foreach (var item in this.componentDatas.Select((data, i) => new { componentData = data, index = i }))
            {
                var index = item.index;
                if (index >= isOpen.Length) { return; }

                // コンポーネント 表示
                var componentType = item.componentData.ComponentType;
                var componentLabel = this.GetComponentLabel(componentType, item.componentData.ObjectType);
                isOpen[index] = CustomUI.Foldout(componentLabel, isOpen[index]);
                if (isOpen[index] == false) { continue; }

                GUILayout.Space(-5f);

                // メソッド一覧 表示
                this.componentDatas[index].MethodDatas.ToList().ForEach(method =>
                {
                    GUILayout.Space(3f);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(IndentSize);

                    if (GUILayout.Button("実行", GUILayout.Width(ButtonWidth), GUILayout.Height(ButtonHeight)))
                    {
                        var parameters = method.Parameters.Select(p => Convert.ChangeType(p.Value, p.ParameterInfo.ParameterType)).ToArray();
                        var args = parameters.Length == 0 ? "" : parameters.Select(p => (p ?? "null").ToString()).Aggregate((s, next) => s + "," + next);
                        Debug.Log(method.MethodInfo.Name + " (" + args + ")");

                        if (IsEditorScript(componentType)) // Editor script
                        {
                            if (componentType.IsSubclassOf(typeof(Editor)))
                            {
                                var instance = ScriptableObject.CreateInstance(componentType);
                                method.MethodInfo.Invoke(instance, parameters);
                            }
                            else
                            if (componentType.IsSubclassOf(typeof(EditorWindow)))
                            {
                                var instance = ScriptableObject.CreateInstance(componentType);
                                method.MethodInfo.Invoke(instance, parameters);
                            }
                            else
                            {
                                var instance = Activator.CreateInstance(componentType);
                                method.MethodInfo.Invoke(instance, parameters);
                            }
                        }
                        else
                        if (componentType.IsSubclassOf(typeof(MonoBehaviour))) // MonoBehaviour script
                        {
                            var objectType = item.componentData.ObjectType;
                            var selectObject = item.componentData.Object;
                            switch (objectType)
                            {
                                case ObjectType.GameObject:
                                    {
                                        if (method.MethodInfo.ReturnType == typeof(IEnumerator))
                                        {
                                            // コルーチン実行
                                            var component = (selectObject as GameObject).GetComponent(componentType);
                                            var monoBehaviourType = componentType.BaseType;
                                            var argumentTypes = new Type[] { typeof(IEnumerator) };
                                            var coroutine = method.MethodInfo.Invoke(component, parameters);
                                            var startCoroutine = monoBehaviourType.GetMethod("StartCoroutine", argumentTypes);
                                            startCoroutine.Invoke(component, new object[] { coroutine });        

                                            if (EditorApplication.isPlaying == false) { Debug.LogWarning("コルーチンはエディタ停止中には実行できません\nCoroutine can only be called in play mode"); } 
                                        }
                                        else
                                        {
                                            // メソッド実行
                                            var component = (selectObject as GameObject).GetComponent(componentType);
                                            method.MethodInfo.Invoke(component, parameters);
                                        }
                                        
                                        break;
                                    }
                                case ObjectType.MonoScript:
                                    {
                                        // メソッド実行
                                        var component = new GameObject().AddComponent(componentType);
                                        method.MethodInfo.Invoke(component, parameters);
                                        DestroyImmediate(component.gameObject);
                                        
                                        if (method.MethodInfo.ReturnType == typeof(IEnumerator))
                                        {
                                            // スクリプト選択時のコルーチン呼び出しは未実装
                                            Debug.LogWarning("スクリプトファイルのコルーチン実行は非対応です\nScript Coroutine is not supported ");
                                        }
                                        break;
                                    }
                                default:
                                    throw new System.NotImplementedException();

                            }
                        }
                        else // other script
                        {
                            var instance = Activator.CreateInstance(componentType);
                            method.MethodInfo.Invoke(instance, parameters);
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
                  });

                    GUILayout.Space(1f);
                    this.Line(1);
                });

                GUILayout.Space(12f);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// ComponentのFoldOut用のラベルを取得
        /// </summary>
        private string GetComponentLabel(Type componentType, ObjectType objectType)
        {
            var componentName = (_isShowComponentFullName) ? componentType.FullName : componentType.Name;
            switch (objectType)
            {
                case ObjectType.GameObject:
                    return componentName;
                case ObjectType.MonoScript:
                    return componentName + ".cs";
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
                componentDatas.AddRange(componentTypes.Select(t => new ComponentData(obj, ObjectType.GameObject, t)));
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
                componentDatas.Add(new ComponentData(obj, ObjectType.MonoScript, componentType));
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
