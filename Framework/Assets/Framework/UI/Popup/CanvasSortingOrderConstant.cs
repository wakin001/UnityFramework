using UnityEngine;

namespace Framework
{
    /// <summary>キャンバスの Sorting Order</summary>
    public static class CanvasSortingOrderConstant
    {
        /// <summary>Base キャンバス</summary>
        public static int baseScene = -100;

        /// <summary>通常の各画面の UI キャンバス（の最小値）</summary>
        public static int defaultMin = 0;

        /// <summary>通常の各画面の UI キャンバスの最大値</summary>
        public static int defaultMax = 99;

        /// <summary>フッター</summary>
        public static int footer = 100;

        /// <summary>ヘッダー</summary>
        public static int header = 100;

        /// <summary>ポップアップキャンバス</summary>
        public static int popup = 200;

        /// <summary>ポップアップが重なった場合の増加分</summary>
        public static int popupDelta = 10;

        /// <summary>タッチガード</summary>
        public static int touchGuard = 1000;

        /// <summary>通信中</summary>
        public static int onConnect = 1100;

        /// <summary>Now Loading</summary>
        public static int loading = 1100;

        /// <summary>エラーポップアップ（タッチガードよりも上に無いとダメ）</summary>
        public static int errorPopup = 1200;

        /// <summary>タップエフェクト</summary>
        public static int tapEffect = 10000;
    }
}
