#define UNITY_EDITOR

using UnityEngine;

namespace Cute.Master
{
    /// <summary>Masterのデータ設定用基本クラス</summary>
    public abstract class IMasterBase : ScriptableObject
    {
#if UNITY_EDITOR
        /// <summary>各プロパティのcsv内indexを取得する</summary>
        public abstract int[] GetPropertyIndex(string[] values);
        /// <summary>Csvの1行データを追加</summary>
        public abstract void AddData(string[] values, int[] indexes);
        /// <summary>データを消去</summary>
        public abstract void ClearData();
#endif
    }
}