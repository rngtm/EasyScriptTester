///-----------------------------------
/// EasyScriptTester
/// @ 2016 RNGTM
///-----------------------------------
namespace EasyScriptTester
{
    using UnityEngine;

    public class Cube : MonoBehaviour
    {
        private bool isRotate = false;

        /// <summary>
        /// 回転開始
        /// </summary>
        void StartRotation()
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
    }
}