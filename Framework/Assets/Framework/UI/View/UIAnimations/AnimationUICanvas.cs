using UnityEngine;
using System.Collections;

namespace Framework
{
    /// <summary>UI に使われるメインキャンバス</summary>
    [RequireComponent(typeof(Canvas))]
    public class AnimationUICanvas : MonoBehaviour
    {
        /// <summary>
        /// Animationのパターンの定義
        /// </summary>
        public enum AnimationPattern
        {
            Pattern_Any = -1,

            Pattern_1,
            Pattern_2,
            Pattern_3,
            Pattern_4,
            Pattern_5,
            Pattern_6,
        }

        /// <summary>
        /// Animation Enable State
        /// </summary>
        public bool animationEnabled { get { return _animationEnabled; } set { _animationEnabled = value; } }
        private bool _animationEnabled = true;

        /// <summary>アニメーションする UI</summary>
        private UIAnimationPublisher[] animationPublishers;

        /// <summary>Canvas</summary>
        public Canvas canvas;

        protected virtual void Awake()
        {
            canvas = GetComponent<Canvas>();
            RegisterToManager();
        }

        public void RegisterToManager()
        {
            //if (AnimationUICanvasManager.IsInstanceEmpty())
            //{
            //    StartCoroutine(RegisterToManagerCoroutine());
            //}
            //else
            {
                AnimationUICanvasManager.instance.Register(this);
            }
        }

        private IEnumerator RegisterToManagerCoroutine()
        {
            //yield return new WaitWhile(() => AnimationUICanvasManager.IsInstanceEmpty());
            yield return null;
            AnimationUICanvasManager.instance.Register(this);
        }

        /// <summary>
        /// 後で追加したものがあるからもう一度計算します
        /// </summary>
        /// <returns></returns>
        public void RecollectAnimationUIs()
        {
            //animationPublishers = GetComponentsInChildren<UIAnimationPublisher>();
        }

        /// <summary>
        /// 開始アニメーションを開始する
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="onAnimationDone">全部のAnimationが完了する時</param>
        /// <param name="onCutOff">AnimationCutOff（11F）が通った時</param>
        public virtual void StartEnterAnimation(AnimationPattern pattern = AnimationPattern.Pattern_1, System.Action onAnimationDone = null, System.Action onCutOff = null)
        {
            if (!animationEnabled)
            {
                if (onAnimationDone != null)
                {
                    onAnimationDone.Invoke();
                }
                return;
            }

            if (gameObject.activeInHierarchy)
            {
                RecollectAnimationUIs();

                bool doneActionScheduled = false;
                for (int i = 0; i < animationPublishers.Length; i++)
                {
                    if (animationPublishers[i].IsSamePattern(pattern) && animationPublishers[i].IsEnableTypeMatch(UIAnimationBase.AnimationEnableType.EnterOnly))
                    {
                        animationPublishers[i].PlayEnterAnimation(doneActionScheduled ? null : onCutOff);
                        doneActionScheduled = true;
                    }
                }

                StartCoroutine(WaitForAnimationComplete(pattern, onAnimationDone));
            }
        }

        /// <summary>
        /// アニメーションが終わるまでに待つ。子クラスはIsOnAnimation()で別の判断条件を追加できます。
        /// </summary>
        private IEnumerator WaitForAnimationComplete(AnimationPattern pattern, System.Action onAnimationDone)
        {
            // 子クラスのoverrideしたStartEnterAnimation()を実行させます。
            yield return new WaitForEndOfFrame();
            yield return new WaitUntil(() => !IsOnAnimation(pattern));

            if (onAnimationDone != null)
            {
                onAnimationDone.Invoke();
            }
        }

        /// <summary>
        /// 終了アニメーションを開始する
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="onAnimationDone">全部のAnimationが完了する時</param>
        /// <param name="onCutOff">AnimationCutOff（11F）が通った時</param>
        public virtual void StartExitAnimation(AnimationPattern pattern = AnimationPattern.Pattern_1, System.Action onAnimationDone = null, System.Action onCutOff = null)
        {
            if (!animationEnabled)
            {
                if (onAnimationDone != null)
                {
                    onAnimationDone.Invoke();
                }
                return;
            }

            if (gameObject.activeInHierarchy)
            {
                RecollectAnimationUIs();

                bool doneActionScheduled = false;
                for (int i = 0; i < animationPublishers.Length; i++)
                {
                    if (animationPublishers[i].IsSamePattern(pattern) && animationPublishers[i].IsEnableTypeMatch(UIAnimationBase.AnimationEnableType.ExitOnly))
                    {
                        animationPublishers[i].PlayExitAnimation(doneActionScheduled ? null : onCutOff);
                        doneActionScheduled = true;
                    }
                }

                StartCoroutine(WaitForAnimationComplete(pattern, onAnimationDone));
            }
        }

        /// <summary>
        /// 同じAnimationUIを繰り返して使おうとするとき強制的にリセットします。
        /// </summary>
        /// <param name="enterState"></param>
        /// <param name="exitState"></param>
        public void ForceResetPlayedState(bool enterState, bool exitState, AnimationPattern pattern = AnimationPattern.Pattern_1)
        {
            if (animationPublishers != null)
            {
                for (int i = 0; i < animationPublishers.Length; i++)
                {
                    if (animationPublishers[i].IsSamePattern(pattern))
                    {
                        animationPublishers[i].ForceClearPlayedStatus();
                    }
                }
            }
        }

        /// <summary>
        /// Enter/Exit Animationをしてるかどうか
        /// pageを切り替える時、exit pageのIsOnAnimation()がfalseなら非表示にします。
        /// </summary>
        public virtual bool IsOnAnimation(AnimationPattern pattern = AnimationPattern.Pattern_1)
        {
            if (animationPublishers != null)
            {
                for (int i = 0; i < animationPublishers.Length; i++)
                {
                    if (animationPublishers[i].IsSamePattern(pattern) && animationPublishers[i].IsOnAnimation())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 任意なアニメーションをしてるかどうか
        /// </summary>
        public bool IsOnAnyAnimation()
        {
            if (animationPublishers != null)
            {
                for (int i = 0; i < animationPublishers.Length; i++)
                {
                    if (animationPublishers[i].IsOnAnimation())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void ResetUIAnimations()
        {
            if (animationPublishers == null)
            {
                RecollectAnimationUIs();
            }
            for (int i = 0; i < animationPublishers.Length; i++)
            {
                animationPublishers[i].ForceClearPlayedStatus();
            }
            if (gameObject.activeInHierarchy)
            {
                for (int i = 0; i < animationPublishers.Length; i++)
                {
                    animationPublishers[i].ResetUIAnimations();
                }
            }
        }

        protected virtual void OnDestroy()
        {
            //if (!AnimationUICanvasManager.IsInstanceEmpty())
            //{
            //    AnimationUICanvasManager.instance.UnRegister(this);
            //}
        }
    }
}
