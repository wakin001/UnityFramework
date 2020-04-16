using UnityEngine.EventSystems;

namespace Framework
{
    /// <summary>
    /// StandaloneInputModuleから継承してます。
    /// GetLastPointerEventData()をアクセスできるようにしました。
    /// </summary>
    public partial class StandaloneInputModuleCustom : StandaloneInputModule
    {
        // the value of ui layer.
        public const int LAYER_UI = 5;

        /// <summary>
        /// public method of StandaloneInputModule.GetLastPointerEventData().
        /// </summary>
        /// <param name="pinterId">-1 if in Editor, otherwise is touch.fingerId</param>
        public PointerEventData GetLastPointerEventDataPublic(int pinterId)
        {
            return GetLastPointerEventData(pinterId);
        }

        /// <summary>
        /// UI partsを押したかどうか
        /// </summary>
        /// <returns><c>true</c> if this instance is selected U; otherwise, <c>false</c>.</returns>
        /// <param name="pinterId">-1 if in Editor, otherwise is touch.fingerId</param>
        public bool IsUITouched(int pointerId)
        {
            PointerEventData eventData = GetLastPointerEventDataPublic(pointerId);
            // Check if current raycast object is UI object.
            if (eventData != null &&
                eventData.pointerCurrentRaycast.gameObject != null &&
                eventData.pointerCurrentRaycast.gameObject.layer == LAYER_UI)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// マルチタッチがあったら、一つ目のpointerIdを取得,
        /// -2ならpointerIdがないです。
        /// </summary>
        public int GetActivePointerId()
        {
            if (m_PointerData == null || m_PointerData.Count == 0)
            {
                // Editorでマウスのpointer idは-1なので、ここ-2を返す.
                return -2;
            }
            // いつもPointerIdは０のタッチを承認します。
            if (m_PointerData.ContainsKey(0))
            {
                return 0;
            }
#if UNITY_EDITOR || UNITY_STANDALONE
            return -1;
#else
            return -2;
#endif
        }
    }
}
