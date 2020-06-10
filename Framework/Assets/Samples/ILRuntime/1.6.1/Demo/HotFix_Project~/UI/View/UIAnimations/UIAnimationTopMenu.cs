using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

namespace HotFix_Project
{
    /// <summary>
    /// 新規UIAnimationのBaseクラス
    /// 消失点あるいはその逆の方向に固定距離
    /// </summary>
    public class UIAnimationTopMenu : UIAnimationBase
    {
        protected Vector3 startRotation;

        protected Vector3 vanishingPointInCanvas = new Vector3(-370, -232);

        // <summary> EnterT </summary>
        protected float enterVanishingT
        {
            get
            {
                if (setting != null && setting.topMenuSetting != null)
                {
                    switch (setting.topMenuSetting.targetType)
                    {
                        case UIAnimationSetting.UIAnimationSettingTopMenuSetting.TargetType.Default:
                            break;
                        case UIAnimationSetting.UIAnimationSettingTopMenuSetting.TargetType.BGChara:
                            return 3.0f;
                        default:
                            break;
                    }
                }
                return 1.0f;
            }
        }

        protected float exitVanishingT = 0.6f;

        protected float enterScale = 6.5f;
        protected float enterOffsetScale = 3.0f;
        protected float exitScale = 0.6f;

        protected float enterShakePostOverrunExtension = 8.0f / animationFPS;
        protected Vector3 enterShakeRotationStart = new Vector3(0, 180, 0);

#if GLUON_DEBUG
        [HideInInspector]
        public bool drawDebugLine = false;
#endif

        protected override void Awake()
        {
            enterDuration = 10.0f / animationFPS;
            exitDuration = 6.0f / animationFPS;
            exitDelay *= 0.75f;
            base.Awake();
        }

        public override void RecordCurrentStateAsDefault()
        {
            base.RecordCurrentStateAsDefault();

            startRotation = rectTransform.localEulerAngles;
        }

        public override void ResetUIAnimations()
        {
            base.ResetUIAnimations();
            rectTransform.localEulerAngles = startRotation;
        }

#if GLUON_DEBUG

        private List<Image> points = new List<Image>();
        Vector3 lastWorldCenter;

        private void Update()
        {
            if (drawDebugLine)
            {
                var centerPoint = GetWorldRectTransformCenter();

                if (centerPoint != lastWorldCenter)
                {
                    if (points.Count > 0)
                    {
                        foreach (var item in points)
                        {
                            GameObject.Destroy(item.gameObject);
                        }
                    }
                    points.Clear();

                    var vanishingPoint = GetWorldVanishingPoint();
                    var enterPoint = GetWorldEnterPosition(vanishingPoint, centerPoint);
                    var exitPoint = GetWorldExitPosition(vanishingPoint, centerPoint);

                    points.Add(DrawPoint(vanishingPoint, Color.red));
                    points.Add(DrawPoint(exitPoint, Color.yellow));
                    points.Add(DrawPoint(centerPoint, Color.red));
                    points.Add(DrawPoint(enterPoint, Color.yellow));

                    lastWorldCenter = centerPoint;
                }
            }

        }

        private Image DrawPoint(Vector3 p, Color color)
        {
            GameObject go = new GameObject();
            var image = go.AddComponent<Image>();
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            rt.position = p;
            rt.sizeDelta = new Vector2(10, 10);
            image.color = new Color(color.r, color.g, color.b, 0.6f);
            return image;
        }
#endif
        /// <summary>
        /// 消失点のWorld座標
        /// </summary>
        /// <returns></returns>
        public Vector3 GetWorldVanishingPoint()
        {
            var canvas = GetComponentInParent<Canvas>();
            var worldVanishingPoint = canvas.transform.TransformPoint(vanishingPointInCanvas);
            return worldVanishingPoint;
        }

        /// <summary>
        /// 動かす中心
        /// </summary>
        /// <returns></returns>
        public Vector3 GetWorldRectTransformCenter()
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            var centerStartWorldPos = (corners[0] + corners[2]) / 2;
            return centerStartWorldPos;
        }

        /// <summary>
        /// Enter座標
        /// </summary>
        /// <param name="worldVanishingPoint"></param>
        /// <param name="centerStartWorldPos"></param>
        /// <returns></returns>
        public Vector3 GetWorldEnterPosition(Vector3 worldVanishingPoint, Vector3 centerStartWorldPos)
        {
            var worldTargetEnterPosition = Vector3.LerpUnclamped(worldVanishingPoint, centerStartWorldPos, enterVanishingT * enterOffsetScale);
            return worldTargetEnterPosition;
        }

        public Vector3 GetWorldExitPosition(Vector3 worldVanishingPoint, Vector3 centerStartWorldPos)
        {
            var worldTargetExitPosition = Vector3.LerpUnclamped(worldVanishingPoint, centerStartWorldPos, exitVanishingT * exitScale);
            return worldTargetExitPosition;
        }

        private void ReCalculateVanishingPoint()
        {
            var worldVanishingPoint = GetWorldVanishingPoint();
            var centerStartWorldPos = GetWorldRectTransformCenter();
            var worldTargetEnterPosition = GetWorldEnterPosition(worldVanishingPoint, centerStartWorldPos);
            var worldTargetExitPosition = GetWorldExitPosition(worldVanishingPoint, centerStartWorldPos);

            var localTargetEnterPosition = rectTransform.parent.InverseTransformPoint(worldTargetEnterPosition);
            var localTargetExitPosition = rectTransform.parent.InverseTransformPoint(worldTargetExitPosition);
            var centerStartLocalPos = rectTransform.parent.InverseTransformPoint(centerStartWorldPos);

            enterOffset = localTargetEnterPosition - centerStartLocalPos;
            exitOffset = localTargetExitPosition - centerStartLocalPos;
        }

        /// <summary>
        /// 実行する前に計算しておく
        /// </summary>
        protected override void OnReadyToCalculatePosition()
        {
            ReCalculateVanishingPoint();
        }

        protected override void JoinEnterAnimation()
        {
            base.JoinEnterAnimation();
            rectTransform.localScale = new Vector3(enterScale, enterScale, 1.0f);

            if (setting.topMenuSetting.applyShake)
            {
                runningSequence.Join(rectTransform.DOScale(new Vector3(1.035f, 0.98f), enterDuration).SetUpdate(UpdateType.Late).SetEase(enterEase));
            }
            else
            {
                runningSequence.Join(rectTransform.DOScale(1.0f, enterDuration).SetUpdate(UpdateType.Late).SetEase(enterEase));
            }
        }

        protected override void JoinExitCanvasGroupAnimation()
        {
            base.JoinExitCanvasGroupAnimation();
            runningSequence.Join(rectTransform.DOScale(exitScale, exitDuration).SetUpdate(UpdateType.Late).SetEase(exitEase));
        }

        protected override float AppendExtraToEnterAnimation()
        {
            float appendVal = base.AppendExtraToEnterAnimation();

            var specificSetting = setting.topMenuSetting;
            if (specificSetting.applyShake)
            {
                var sequence = DOTween.Sequence();
                rectTransform.localEulerAngles = enterShakeRotationStart;

                sequence.Append(rectTransform.DOLocalRotate(startRotation, enterDuration).SetUpdate(UpdateType.Late).SetEase(exitEase));
                sequence.Append(rectTransform.DOScale(new Vector3(0.97f, 1.0f), 0.12f).SetEase(exitEase));
                sequence.Append(rectTransform.DOScaleX(1.0f, 0.12f).SetEase(exitEase));

                runningSequence.Join(sequence);

                return enterShakePostOverrunExtension + appendVal;
            }
            return appendVal;
        }
    }
}
