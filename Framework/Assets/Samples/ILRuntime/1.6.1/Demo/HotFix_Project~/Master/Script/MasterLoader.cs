using System;

namespace Cute.Master
{
    /// <summary>
    /// MasterデータをEditor上で読むためのクラス
    /// 実機ではMaster用のScriptableObjectをLoadすること
    /// </summary>
    public class MasterLoader
    {
#if UNITY_EDITOR
        /// <summary>
        /// Editor用Masterロード関数登録用イベント
        /// 
        /// Editorクラスはランタイム側classからはアクセスできないので、Editor側から呼び出し関数を登録してもらう
        /// CsvHelper.dllをランタイム側に移したくないのでこの形をとっている
        /// </summary>
        public static event Action<Func<string, bool>> LoadMasterForEditorEvent;

        /// <summary>全Masterデータを読み込む（Editor用）</summary>
        public static void LoadMasterForEditor(Func<string, bool> fileteringFunction = null)
        {
            if (LoadMasterForEditorEvent != null)
            {
                LoadMasterForEditorEvent(fileteringFunction);
            }
        }
#endif
    }
}