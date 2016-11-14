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

    using Object = UnityEngine.Object;

    /// <summary>
    /// Component data
    /// </summary>
    public class ComponentData
    {
        #region Properties
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
        #endregion Properties

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
            this.MethodDatas = ExtractUtility.ExtractMethods(componentType)
              .Select(methodInfo => new MethodData(methodInfo))
              .ToArray();
        }

    }
}
