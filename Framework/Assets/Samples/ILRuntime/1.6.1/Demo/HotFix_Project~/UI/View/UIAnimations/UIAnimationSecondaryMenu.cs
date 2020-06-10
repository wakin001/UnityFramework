using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace HotFix_Project
{
    /// <summary>
    /// 新規UIAnimationのBaseクラス
    /// 同じ方向に固定距離
    /// </summary>
    public class UIAnimationSecondaryMenu : UIAnimationBase
    {
        protected float moveDistance = 150.0f;

        public override void ApplySetting(UIAnimationSetting anotherSetting)
        {
            base.ApplySetting(anotherSetting);

            var specificSetting = setting.secondaryMenuSetting;
            enterOffset = specificSetting.enterFromDirection.normalized * moveDistance;
            exitOffset = enterOffset;
        }
    }
}
