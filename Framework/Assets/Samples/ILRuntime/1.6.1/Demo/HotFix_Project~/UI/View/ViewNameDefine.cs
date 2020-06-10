using System.Collections.Generic;

namespace HotFix_Project
{
    /// <summary>
    /// View nameの定義
    /// </summary>
    public class ViewNameDefine
    {
        /// <summary>
        /// Viewの名前の定義
        /// </summary>
        public enum ViewName
        {
            None = 0,
            // view1
            View1,
            View2,
            View3,

        }

        private static Dictionary<int, string> ViewNameDic = new Dictionary<int, string>()
        {
            // quest select
            { (int)ViewName.View1,              "Prefabs/Views/View1" },
            { (int)ViewName.View2,              "Prefabs/Views/View2" },
            { (int)ViewName.View3,              "Prefabs/Views/View3" },
        };

        /// <summary>
        /// ViewNameからPage名前の取得
        /// </summary>
        public static string GetPagePath(ViewName ViewName)
        {
            string path = "";
            ViewNameDic.TryGetValue((int)ViewName, out path);
            return path;
        }
    }
}
