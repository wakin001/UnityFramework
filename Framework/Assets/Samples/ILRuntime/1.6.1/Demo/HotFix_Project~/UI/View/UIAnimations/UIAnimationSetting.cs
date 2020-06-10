using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using System;

namespace HotFix_Project
{
    /// <summary>
    /// 新規UIAnimationのBaseクラスのSetting
    /// </summary>
    [Serializable]
    public class UIAnimationSetting
    {
        public enum UIAnimationClassType
        {
            NotAssigned = -1,

            TopMenu_VanishingPoint,
            SecondaryMenu_ParallelDirectional,
            InnerMove_InOutToPhotoshopPosition,

            Custom = 100000,
        }

        [Serializable]
        public class UIAnimationSettingBase
        {

        }

        [Serializable]
        public class UIAnimationSettingTopMenuSetting : UIAnimationSettingBase
        {
            // <summary> 適応するタイプ </summary>
            public enum TargetType
            {
                Default,
                BGChara,    // Top絵だけ別のScaleを適応
            }

            public bool applyShake = false;
            public TargetType targetType;

            public UIAnimationSettingTopMenuSetting(UIAnimationSettingTopMenuSetting anotherSetting)
            {
                applyShake = anotherSetting.applyShake;
                targetType = anotherSetting.targetType;
            }
        }

        [Serializable]
        public class UIAnimationSettingSecondaryMenu : UIAnimationSettingBase
        {
            public enum DirectionDefine
            {
                Up,
                Down,
                Left,
                Right,
                Custom, // not used
            }

            public Vector3 enterFromDirection = Vector3.down;

            public void ApplyDirection(DirectionDefine direction)
            {
                switch (direction)
                {
                    case DirectionDefine.Up:
                        enterFromDirection = Vector3.up;
                        break;
                    case DirectionDefine.Down:
                        enterFromDirection = Vector3.down;
                        break;
                    case DirectionDefine.Left:
                        enterFromDirection = Vector3.left;
                        break;
                    case DirectionDefine.Right:
                        enterFromDirection = Vector3.right;
                        break;
                    default:
                        enterFromDirection = Vector3.zero;
                        break;
                }
            }

            public DirectionDefine GetDirection()
            {
                if (enterFromDirection == Vector3.up)
                {
                    return DirectionDefine.Up;
                }
                if (enterFromDirection == Vector3.down)
                {
                    return DirectionDefine.Down;
                }
                if (enterFromDirection == Vector3.left)
                {
                    return DirectionDefine.Left;
                }
                if (enterFromDirection == Vector3.right)
                {
                    return DirectionDefine.Right;
                }
                return DirectionDefine.Custom;
            }

            public UIAnimationSettingSecondaryMenu(UIAnimationSettingSecondaryMenu anotherSetting)
            {
                enterFromDirection = anotherSetting.enterFromDirection;
            }
        }

        [Serializable]
        public class UIAnimationSettingInnerMove : UIAnimationSettingBase
        {
            public Vector3 moveToOffset = Vector3.zero;
            public bool applyBounce = false;
            public bool shouldYRelativeToCanvasSize = false;

            public UIAnimationSettingInnerMove(UIAnimationSettingInnerMove anotherSetting)
            {
                moveToOffset = anotherSetting.moveToOffset;
                applyBounce = anotherSetting.applyBounce;
                shouldYRelativeToCanvasSize = anotherSetting.shouldYRelativeToCanvasSize;
            }
        }

        public UIAnimationSettingTopMenuSetting topMenuSetting;
        public UIAnimationSettingSecondaryMenu secondaryMenuSetting;
        public UIAnimationSettingInnerMove innerMoveSetting;

        public GameObject targetObject;
        public UIAnimationClassType animationType = UIAnimationClassType.NotAssigned;

        /// <summary>
        /// Copy from
        /// </summary>
        /// <param name="anotherSetting"></param>
        /// <param name="targetObject"></param>
        public UIAnimationSetting(UIAnimationSetting anotherSetting, GameObject targetObject)
        {
            topMenuSetting = new UIAnimationSettingTopMenuSetting(anotherSetting.topMenuSetting);
            secondaryMenuSetting = new UIAnimationSettingSecondaryMenu(anotherSetting.secondaryMenuSetting);
            innerMoveSetting = new UIAnimationSettingInnerMove(anotherSetting.innerMoveSetting);

            this.targetObject = targetObject;

            animationType = anotherSetting.animationType;
        }
    }
}
