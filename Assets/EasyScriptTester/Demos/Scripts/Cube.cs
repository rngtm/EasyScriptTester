///-----------------------------------
/// EasyScriptTester
/// @ 2016 RNGTM(https://github.com/rngtm)
///-----------------------------------
namespace EasyScriptTester.Demos
{
    using UnityEngine;
    using System.Collections;

    /// <summary>
    /// Demo用のクラス
    /// </summary>
    public class Cube : MonoBehaviour
    {
        [SerializeField]
        private bool isRotate = false;

        /// <summary>
        /// 回転開始
        /// </summary>
        void StartRotation(int x)
        {
            this.isRotate = true;
        }

        /// <summary>
        /// 回転終了
        /// </summary>
        void StopRotation()
        {
            this.isRotate = false;
        }

        void Update()
        {
            if (this.isRotate)
            {
                this.transform.Rotate(new Vector3(2f, 3f, 4f));
            }
        }

        IEnumerator TestCoroutine(int x, int y, int z)
        {
            Debug.Log(x);
            yield return new WaitForSeconds(1f);
            Debug.Log(y);
            yield return new WaitForSeconds(1f);
            Debug.Log(z);
        }
    }
}
