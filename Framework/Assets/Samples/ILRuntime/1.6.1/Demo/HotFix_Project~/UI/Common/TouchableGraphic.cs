using UnityEngine;
using UnityEngine.UI;

namespace HotFix_Project
{
    /// <summary>
    /// 描画しないUI用当たり判定
    /// ダイアログの背景にタッチさせたくないが絵は出したくない場合などに使う
    /// </summary>
    public class TouchableGraphic : Graphic
    {
        public override bool Raycast(Vector2 sp, Camera eventCamera)
        {
            return true;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}