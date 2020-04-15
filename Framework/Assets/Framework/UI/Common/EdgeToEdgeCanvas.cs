using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Framework
{
    /// <summary>EdgeToEdge 対応が必要な Canvas に Attach する</summary>
    [RequireComponent(typeof(Canvas))]
    public class EdgeToEdgeCanvas : MonoBehaviour
    {

#if UNITY_IOS
        public const float statusBarHeight = 20.0f * 2.0f;
#else
        public const float statusBarHeight = 0.0f;
#endif

        public const float edgeToEdgeBaseWidth = 640.0f;
        public const float edgeToEdgeBaseHeight = 1136.0f;

        public const float edgeToEdgeBaseMaxBaseWidth = 852.0f; // 3:4 = x : 1136

        void Awake()
        {
            // 横長端末なら左右両端に隙間表示
            if (IsWideDevice())
            {
                GetComponent<CanvasScaler>().matchWidthOrHeight = 1;
            }
            else
            {
                GetComponent<CanvasScaler>().matchWidthOrHeight = 0;
            }
        }

        public static bool IsWideDevice()
        {
            float rate = (float)Screen.width / Screen.height;

            if (rate > edgeToEdgeBaseWidth / (edgeToEdgeBaseHeight - statusBarHeight))
            {
                return true;
            }

            return false;
        }
    }
}
