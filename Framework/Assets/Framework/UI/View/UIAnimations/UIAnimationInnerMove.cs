using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace Framework
{
    /// <summary>
    /// 新規UIAnimationのBaseクラス
    /// ポイントへの移動（EaseInOut）
    /// </summary>
    public class UIAnimationInnerMove : UIAnimationBase
    {
        protected float moveDistance = 150.0f;
        protected float overrunBounceDistance = 10f;

        protected float postOverrunExtension = 4.0f / animationFPS;

        protected override void JoinEnterCanvasGroupAnimation()
        {
        }

        protected override void JoinExitCanvasGroupAnimation()
        {
        }

        public override void ApplySetting(UIAnimationSetting anotherSetting)
        {
            base.ApplySetting(anotherSetting);

            var specificSetting = setting.innerMoveSetting;

            enterOffset = specificSetting.moveToOffset;
            if (specificSetting.shouldYRelativeToCanvasSize)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    var height = canvas.GetComponent<RectTransform>().rect.height;
                    enterOffset = new Vector3(enterOffset.x, enterOffset.y * height / EdgeToEdgeCanvas.edgeToEdgeBaseHeight, enterOffset.z);
                }
            }
            exitOffset = enterOffset;
        }

        protected override void JoinEnterAnimation()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1;
            }

            var specificSetting = setting.innerMoveSetting;
            if (!specificSetting.applyBounce)
            {
                base.JoinEnterAnimation();
                return;
            }
            // 位置とサイズのアニメーション
            Vector3 initialPosition = rectTransform.anchoredPosition3D;
            Vector3 endPosition = initialPosition;
            Vector3 overrunPosition = endPosition - specificSetting.moveToOffset.normalized * overrunBounceDistance;

            rectTransform.anchoredPosition3D = initialPosition + enterOffset;

            var sequence = DOTween.Sequence();

            sequence.Append(
                TweenUtil.ValueTo(0, 1, enterDuration, (x) =>
                {
                    rectTransform.anchoredPosition3D = Vector3.Lerp(initialPosition + enterOffset, overrunPosition, x);
                }).SetUpdate(UpdateType.Late).SetEase(Ease.OutCubic));

            sequence.Append(
                TweenUtil.ValueTo(0, 1, postOverrunExtension, (x) =>
                {
                    rectTransform.anchoredPosition3D = Vector3.Lerp(overrunPosition, endPosition, x);
                }).SetUpdate(UpdateType.Late).SetEase(Ease.InOutCubic));

            runningSequence.Join(sequence);
        }

        protected override void JoinExitAnimation()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1;
            }
            var specificSetting = setting.innerMoveSetting;
            if (!specificSetting.applyBounce)
            {
                base.JoinExitAnimation();
            }

            Vector3 initialPosition = rectTransform.anchoredPosition3D;
            Vector3 endPosition = initialPosition + specificSetting.moveToOffset;
            Vector3 overrunPosition = endPosition + specificSetting.moveToOffset.normalized * overrunBounceDistance;

            var sequence = DOTween.Sequence();

            sequence.Append(
                TweenUtil.ValueTo(0, 1, exitDuration, (x) =>
                {
                    rectTransform.anchoredPosition3D = Vector3.Lerp(initialPosition, overrunPosition, x);
                }).SetUpdate(UpdateType.Late).SetEase(Ease.OutCubic));

            sequence.Append(
                TweenUtil.ValueTo(0, 1, postOverrunExtension, (x) =>
                {
                    rectTransform.anchoredPosition3D = Vector3.Lerp(overrunPosition, endPosition, x);
                }).SetUpdate(UpdateType.Late).SetEase(Ease.InOutCubic));

            runningSequence.Join(sequence);
        }

        protected override float AppendExtraToEnterAnimation()
        {
            var appendVal = base.AppendExtraToEnterAnimation();

            var specificSetting = setting.innerMoveSetting;
            if (specificSetting.applyBounce)
            {
                appendVal += postOverrunExtension;
            }
            return appendVal;
        }

        protected override float AppendExtraToExitAnimation()
        {
            var appendVal = base.AppendExtraToExitAnimation();

            var specificSetting = setting.innerMoveSetting;
            if (specificSetting.applyBounce)
            {
                appendVal += postOverrunExtension;
            }
            return appendVal;
        }
    }
}
