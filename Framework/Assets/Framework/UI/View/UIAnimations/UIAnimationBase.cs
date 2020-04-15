using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace Framework
{
    /// <summary>
    /// 新規UIAnimationのBaseクラス
    /// Baseは移動とFadeのみ定義
    /// 全体操作するものを定義
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIAnimationBase : MonoBehaviour
    {
        /// <summary>
        /// アニメーションのEnterExitが有効かどうか
        /// </summary>
        public enum AnimationEnableType
        {
            BothEnterAndExit,
            EnterOnly,
            ExitOnly,
            Preheated,       // for preheat only
        }

        /// <summary>
        /// Flash同様30FPS
        /// </summary>
        public const float animationFPS = 30.0f;

        //////////////////////////////////////////////////////////////////////
        [SerializeField, Header("アニメーションパターン")]
        public AnimationUICanvas.AnimationPattern pattern = AnimationUICanvas.AnimationPattern.Pattern_1;

        [SerializeField, Header("アニメーションフラグ")]
        public AnimationEnableType enableType = AnimationEnableType.BothEnterAndExit;

        [SerializeField, Header("Setting")]
        public UIAnimationSetting setting;

        /// <summary>
        /// TimeScaleを無視するかどうか
        /// </summary>
        [System.NonSerialized]
        public bool ignoreTimeScale = false;

        public const float constDuration = 8.0f / animationFPS;
        /// <summary>
        /// Animationの遷移時間
        /// </summary>
        protected float enterDuration = constDuration;
        protected float exitDuration = constDuration;

        protected Ease enterEase = Ease.OutCubic;
        protected Ease exitEase = Ease.InCubic;

        protected Vector3 enterOffset = Vector3.zero;
        public Vector3 exitOffset { get; set; }

        // in secs
        public float enterDelay { get; set; }
        public float exitDelay { get; set; }

        protected float enterAlpha = 0f;
        protected float exitAlpha = 0f;

        protected bool _shouldReEnableChildrenAfterExit = true;
        public bool shouldReEnableChildrenAfterExit { get { return _shouldReEnableChildrenAfterExit; } set { _shouldReEnableChildrenAfterExit = value; } }

        //////////////////////////////////////////////////////////////////////
        protected RectTransform rectTransform;
        protected CanvasGroup canvasGroup;
        protected bool elementsEnabled = true;
        protected Text[] childTexts;
        protected Image[] childImages;
        protected PointerEventHandler[] childPointEventHandlers;

        // 二重実行禁止
        protected bool isEnterAnimationPlayed;
        protected bool isExitAnimationPlayed;

        // EnableのときはDisableしたものだけをEnableする
        private List<Behaviour> disabledObjectList;

        protected bool isOnEnterAnimation;
        protected bool isOnExitAnimation;

        protected Vector3 startPosition;
        protected Vector3 startNormalAnchorPosition;

        /// <summary>
        /// 実行するSequenceをここに追加
        /// </summary>
        protected Sequence runningSequence = null;

        protected virtual void Awake()
        {
            RecordCurrentStateAsDefault();
        }

        /// <summary>
        /// ApplySetting
        /// </summary>
        /// <param name="setting"></param>
        public virtual void ApplySetting(UIAnimationSetting anotherSetting)
        {
            setting = new UIAnimationSetting(anotherSetting, gameObject);
        }

        /// <summary>
        /// Stableの状態の時再度現状を保存できるように
        /// </summary>
        public virtual void RecordCurrentStateAsDefault()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            startPosition = rectTransform.anchoredPosition3D;
            startNormalAnchorPosition = rectTransform.localPosition;
        }

        /// <summary>
        /// 位置、Scaleなどアニメーションの前の値にリセットする。
        /// </summary>
        public virtual void ResetUIAnimations()
        {
            rectTransform.anchoredPosition3D = startPosition;
            rectTransform.localScale = Vector3.one;
            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// 現在のAnimationを正常終了する
        /// </summary>
        public virtual void CompleteRunningAnimations()
        {
            if (runningSequence != null && runningSequence.IsActive())
            {
                runningSequence.Complete(true);
            }

            runningSequence = null;
        }

        /// <summary>
        /// 現在のAnimationをKillする（Callbackなどよばれないが現状のままの状態でとまる）
        /// </summary>
        public virtual void KillRunningAnimations()
        {
            if (runningSequence != null && runningSequence.IsActive())
            {
                runningSequence.Kill();
            }

            runningSequence = null;
        }

        /// <summary>
        /// なんらかのAnimationが実行されてるかどうか
        /// </summary>
        /// <returns></returns>
        public bool IsAnimationRunning()
        {
            return runningSequence != null;
        }

        /// <summary>
        /// StartAnimationの時位置が初期位置に更新した時
        /// </summary>
        protected virtual void OnReadyToCalculatePosition()
        {

        }

        /// <summary>開始アニメーション開始</summary>
        public virtual void StartEnterAnimation(System.Action onAnimationDone = null)
        {
            if (enableType != AnimationEnableType.BothEnterAndExit && enableType != AnimationEnableType.EnterOnly) return;

            if (isEnterAnimationPlayed) return;
            isEnterAnimationPlayed = true;
            isOnEnterAnimation = true;

            CompleteRunningAnimations();

            var delaySequence = DOTween.Sequence();
            delaySequence.AppendInterval(enterDelay);

            runningSequence = DOTween.Sequence();

            rectTransform.anchoredPosition3D = startPosition;

            // アニメーションを始める前、Touch禁止（enterDelayは０でも）
            DisableTouchEventChildren();

            OnReadyToCalculatePosition();

            runningSequence.AppendInterval(enterDelay);

            JoinEnterCanvasGroupAnimation();
            JoinEnterAnimation();

            var extraDuration = AppendExtraToEnterAnimation();

            // なぜか常にEnableChildrenを呼ばないといけないのでここでやる
            var totalFrame = enterDuration + extraDuration;
            var enterWaitTweener = DOTween.Sequence();
            enterWaitTweener.Append(TweenUtil.Wait(totalFrame / 60.0f, (x) =>
            {
                EnableChildren(false);
            }));

            runningSequence.Join(enterWaitTweener);

            // ここからDelaySequenceを見る
            runningSequence = delaySequence.Append(runningSequence);

            // Enter途中でExitの場合、enterWaitTweener.OnComplete()が呼ばれてない可能性があるので、runningSequence.OnCompleteで
            // "EnableChildren(); isOnEnterAnimation = false;"をします。
            runningSequence.OnComplete(() =>
            {
                EnableChildren();
                isOnEnterAnimation = false;

                runningSequence = null;
                if (onAnimationDone != null)
                {
                    onAnimationDone.Invoke();
                }
            });


            if (ignoreTimeScale)
            {
                runningSequence.SetUpdate(true);
            }
        }

        protected virtual void JoinEnterCanvasGroupAnimation()
        {
            canvasGroup.alpha = 0;
            runningSequence.Join(canvasGroup.DOFade(1.0f, enterDuration).SetUpdate(UpdateType.Late).SetEase(enterEase));
        }

        /// <summary>
        /// EnterAnimation実行
        /// </summary>
        protected virtual void JoinEnterAnimation()
        {
            // 位置とサイズのアニメーション
            Vector3 initialPosition = rectTransform.anchoredPosition3D;
            Vector3 endPosition = initialPosition;

            rectTransform.anchoredPosition3D = initialPosition + enterOffset;
            runningSequence.Join(
                TweenUtil.ValueTo(0, 1, enterDuration, (x) =>
                {
                    rectTransform.anchoredPosition3D = Vector3.Lerp(initialPosition + enterOffset, endPosition, x);
                }).SetUpdate(UpdateType.Late).SetEase(enterEase));
        }

        /// <summary>終了アニメーション開始</summary>
        public virtual void StartExitAnimation(System.Action onAnimationDone = null)
        {
            if (enableType != AnimationEnableType.BothEnterAndExit && enableType != AnimationEnableType.ExitOnly) return;

            if (isExitAnimationPlayed) return;
            isExitAnimationPlayed = true;
            isOnExitAnimation = true;

            // Enter途中でExitの場合一瞬移動してくるのがいやならKillの方を使うが多分そういうことはあまりない
            CompleteRunningAnimations();

            OnReadyToCalculatePosition();

            var delaySequence = DOTween.Sequence();
            delaySequence.AppendInterval(exitDelay);

            runningSequence = DOTween.Sequence();

            DisableTouchEventChildren();

            JoinExitCanvasGroupAnimation();
            JoinExitAnimation();

            AppendExtraToExitAnimation();

            // ここからDelaySequenceを見る
            runningSequence = delaySequence.Append(runningSequence);

            runningSequence.AppendCallback(() =>
            {
                // 害がありそうならデフォ値をfalseにするがちゃんとしてれば大丈夫そうです
                if (shouldReEnableChildrenAfterExit)
                {
                    EnableChildren();
                }
                isOnExitAnimation = false;
                runningSequence = null;
                if (onAnimationDone != null)
                {
                    onAnimationDone.Invoke();
                }
            });

            if (ignoreTimeScale)
            {
                runningSequence.SetUpdate(true);
            }
        }

        /// <summary>
        /// ExitAnimationの実際実行
        /// </summary>
        protected virtual void JoinExitAnimation()
        {
            Vector3 initialPosition = rectTransform.anchoredPosition3D;
            Vector3 endPosition = initialPosition + exitOffset;

            runningSequence.Join(
                TweenUtil.ValueTo(0, 1, exitDuration, (x) =>
                {
                    rectTransform.anchoredPosition3D = Vector3.Lerp(initialPosition, endPosition, x);
                }).SetUpdate(UpdateType.Late).SetEase(exitEase));
        }

        protected virtual void JoinExitCanvasGroupAnimation()
        {
            runningSequence.Join(canvasGroup.DOFade(0.0f, exitDuration).SetUpdate(UpdateType.Late).SetEase(exitEase));
        }

        /// <summary>
        /// 継承すれば処理を追加できる
        /// </summary>
        /// <returns></returns>
        protected virtual float AppendExtraToEnterAnimation()
        {
            return 0f;
        }

        /// <summary>
        /// 継承すれば処理を追加できる
        /// </summary>
        /// <returns></returns>
        protected virtual float AppendExtraToExitAnimation()
        {
            return 0f;
        }

        /// <summary>
        /// 同じAnimationUIを繰り返して使おうとするとき強制的にリセットします。
        /// </summary>
        /// <param name="enterState"></param>
        /// <param name="exitState"></param>
        public void ForceResetPlayedState(bool enterState, bool exitState)
        {
            isEnterAnimationPlayed = enterState;
            isExitAnimationPlayed = exitState;
        }

        protected void DisableChildren()
        {
            disabledObjectList = new List<Behaviour>();
            elementsEnabled = false;

            childTexts = GetComponentsInChildren<Text>();
            for (int i = 0; i < childTexts.Length; i++)
            {
                if (childTexts[i].enabled)
                {
                    childTexts[i].enabled = false;
                    disabledObjectList.Add(childTexts[i]);
                }
            }

            childImages = GetComponentsInChildren<Image>();
            for (int i = 0; i < childImages.Length; i++)
            {
                if (childImages[i].enabled)
                {
                    childImages[i].enabled = false;
                    disabledObjectList.Add(childImages[i]);
                }
            }

            DisableTouchEventChildren();
        }

        protected void EnableChildren(bool includeTouchEvent = true)
        {
            if (!elementsEnabled)
            {
                for (int i = 0; i < disabledObjectList.Count; i++)
                {
                    if (disabledObjectList[i] != null)
                    {
                        disabledObjectList[i].enabled = true;
                    }
                }

                elementsEnabled = true;
            }

            if (includeTouchEvent)
            {
                EnableTouchEventChildren();
            }
        }

        protected void DisableTouchEventChildren()
        {
            // 一回のみGetComponentsInChildren<PointerEventHandler>()をします。
            if (childPointEventHandlers == null || childPointEventHandlers.Length == 0)
            {
                childPointEventHandlers = GetComponentsInChildren<PointerEventHandler>();
            }
            for (int i = 0; i < childPointEventHandlers.Length; i++)
            {
                childPointEventHandlers[i].isBlockingByUIAnimation = true;
            }
        }

        protected void EnableTouchEventChildren()
        {
            if (childPointEventHandlers != null)
            {
                for (int i = 0; i < childPointEventHandlers.Length; i++)
                {
                    childPointEventHandlers[i].isBlockingByUIAnimation = false;
                }
            }
        }

        public bool IsOnAnimation()
        {
            return isOnEnterAnimation || isOnExitAnimation;
        }

        /// <summary>
        /// Animation patternが合ってるかどうか
        /// </summary>
        public bool IsSamePattern(AnimationUICanvas.AnimationPattern animationPattern)
        {
            return (pattern == animationPattern || pattern == AnimationUICanvas.AnimationPattern.Pattern_Any);
        }
    }
}
