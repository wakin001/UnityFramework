using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using System;
using UnityEngine.Events;

namespace Framework
{
    /// <summary>
    /// アウトゲームの UIAnimation自動分配するクラス
    /// </summary>
    public class UIAnimationPublisher : MonoBehaviour
    {
        [SerializeField, Header("Settings")]
        public UIAnimationSetting[] settings;

        [SerializeField, Header("自動開始")]
        public bool bAutoStartEnterAnimation = true;

        [SerializeField, Header("自動Play状態消す")]
        public bool bAutoClearAnimation = false;

        [SerializeField, Header("Animation Pattern")]
        public AnimationUICanvas.AnimationPattern pattern;

        [SerializeField, Header("Animation Enable Type")]
        public UIAnimationBase.AnimationEnableType enableType;

        [SerializeField, Header("ObjectsToPublish")]
        public GameObject[] objsInSetting;

        public UIAnimationSetting templateSetting;

        public bool firstInFirstOut = true;

        // <summary> 分配の順番 </summary>
        public enum DelayOrderType
        {
            Auto,   // いままでのプレハブも動けるように保留
            Ascending,
            Descending,
        }

        // <summary> 子の実行順番 </summary>
        public DelayOrderType enterDelayType;
        public DelayOrderType exitDelayType;

        [SerializeField, Header("Overrideかどうか")]
        public bool overrideOriginal = true;

        [SerializeField, Header("Activeじゃないノードを無視するか")]
        public bool doNotPublishToInactive = true;

        [SerializeField, Header("Animationがなくても完了するまでまつ")]
        public bool waitEvenIfNoObjectsPublished = false;

        public bool shouldReEnableChildrenAfterExit = true;

        private List<UIAnimationBase> uiAnimationBaseList = new List<UIAnimationBase>();

        // <summary> 最大AnimationFrame数 </summary>
        public const int defaultMaximumCutoffDurationInFrame = 11;
        // <summary> 最大Animation時間 </summary>
        public const float defaultMaximumCutoffDuration = defaultMaximumCutoffDurationInFrame / UIAnimationBase.animationFPS;
        // <summary> 途中で切られるAnimationFrame数 </summary>
        public const int defaultHalfCutoffDurationInFrame = 6;
        // <summary> 途中で切られるAnimation時間 </summary>
        public const float defaultHalfCutoffDuration = defaultHalfCutoffDurationInFrame / UIAnimationBase.animationFPS;

        // <summary> Animation総時間 </summary>
        public float maximumCutoffDuration = defaultMaximumCutoffDuration;
        // <summary> AnimationHalfCutoffのコールバック時間 </summary>
        public float halfCutoffDuration = defaultHalfCutoffDuration;

        public bool onEnterAnimation { get; private set; }
        public bool onExitAnimation { get; private set; }

        public UnityEvent onEnterEvent { get; set; }
        public UnityEvent onExitEvent { get; set; }

        /// <summary>
        /// TimeScaleを無視するかどうか
        /// </summary>
        [System.NonSerialized]
        public bool ignoreTimeScale = false;

        // TODO setting!

        void Awake()
        {
            RePublishAll();
        }

        /// <summary>
        /// AddComponents before release
        /// </summary>
        public void PreheatAll()
        {
            for (int i = 0; i < settings.Length; i++)
            {
                var singleObj = settings[i].targetObject;
                if (singleObj != null)
                {
                    //var uiBaseList = singleObj.GetComponents<AnimationUIBase>();

                    UIAnimationBase uiBase = null;
                    //for (int j = 0; j < uiBaseList.Length; j++)
                    //{
                    //    if (uiBaseList[j].pattern == pattern)
                    //    {
                    //        return;
                    //    }
                    //}

                    if (uiBase == null)
                    {
                        switch (settings[i].animationType)
                        {
                            case UIAnimationSetting.UIAnimationClassType.NotAssigned:
                                break;
                            case UIAnimationSetting.UIAnimationClassType.TopMenu_VanishingPoint:
                                uiBase = singleObj.AddComponent<UIAnimationTopMenu>();
                                break;
                            case UIAnimationSetting.UIAnimationClassType.SecondaryMenu_ParallelDirectional:
                                uiBase = singleObj.AddComponent<UIAnimationSecondaryMenu>();
                                break;
                            case UIAnimationSetting.UIAnimationClassType.InnerMove_InOutToPhotoshopPosition:
                                uiBase = singleObj.AddComponent<UIAnimationInnerMove>();
                                break;
                            case UIAnimationSetting.UIAnimationClassType.Custom:
                                uiBase = singleObj.AddComponent<UIAnimationTopMenu>();
                                break;
                            default:
                                break;
                        }
                    }

                    uiBase.pattern = pattern;
                    uiBase.enableType = UIAnimationBase.AnimationEnableType.Preheated;
                }
            }
        }

        /// <summary>
        /// 位置、Scaleなどアニメーションの前の値にリセットする。
        /// </summary>
        public void ResetUIAnimations()
        {
            if (uiAnimationBaseList.Count > 0)
            {
                for (int i = 0; i < uiAnimationBaseList.Count; i++)
                {
                    uiAnimationBaseList[i].ResetUIAnimations();
                }
            }
        }

        /// <summary>
        /// PatternとEnableTypeで合ってるものを返す
        /// </summary>
        /// <param name="publishers"></param>
        /// <param name="pattern"></param>
        /// <param name="enableType"></param>
        /// <returns></returns>
        public static UIAnimationPublisher[] GetValidPublisherList(UIAnimationPublisher[] publishers, AnimationUICanvas.AnimationPattern pattern, UIAnimationBase.AnimationEnableType enableType)
        {
            if (publishers == null)
            {
                return null;
            }

            List<UIAnimationPublisher> validPublishers = new List<UIAnimationPublisher>();
            foreach (var publisher in publishers)
            {
                if (publisher.isActiveAndEnabled)
                {
                    if (publisher.IsSamePattern(pattern))
                    {
                        if (publisher.IsEnableTypeMatch(enableType))
                        {
                            validPublishers.Add(publisher);
                        }
                    }
                }
            }

            return validPublishers.ToArray();
        }

        /// <summary>
        /// 主にEditorUse
        /// </summary>
        public void RePublishSetting()
        {
            for (int i = 0; i < settings.Length; i++)
            {
                if (settings[i] != null && settings[i].targetObject != null)
                {
                    var animBase = settings[i].targetObject.GetComponent<UIAnimationBase>();
                    if (animBase != null)
                    {
                        animBase.ApplySetting(settings[i]);
                    }
                }
            }
        }

        public void RePublishAll()
        {
            var enterDelay = enterDelayType;
            var exitDelay = exitDelayType;
            if (enterDelay == DelayOrderType.Auto)
            {
                enterDelay = DelayOrderType.Ascending;
            }
            if (exitDelay == DelayOrderType.Auto)
            {
                exitDelay = firstInFirstOut ? DelayOrderType.Ascending : DelayOrderType.Descending;
            }

            var settingsToPublish = new List<UIAnimationSetting>();
            for (int i = 0; i < settings.Length; i++)
            {
                if (settings[i] != null && settings[i].targetObject != null)
                {
                    if (doNotPublishToInactive && !settings[i].targetObject.activeInHierarchy)
                    {
                        continue;
                    }
                    settingsToPublish.Add(settings[i]);
                }
            }
            uiAnimationBaseList = new List<UIAnimationBase>();

            for (int i = 0; i < settingsToPublish.Count; i++)
            {
                var targetObject = settingsToPublish[i].targetObject;

                var uiBaseList = targetObject.GetComponents<UIAnimationBase>();

                UIAnimationBase uiBase = null;
                for (int j = 0; j < uiBaseList.Length; j++)
                {
                    if (uiBaseList[j].pattern == pattern)
                    {
                        uiBase = uiBaseList[j];
                        break;
                    }
                }

                if (uiBase == null || uiBase.enableType == UIAnimationBase.AnimationEnableType.Preheated)
                {
                    switch (settingsToPublish[i].animationType)
                    {
                        case UIAnimationSetting.UIAnimationClassType.TopMenu_VanishingPoint:
                            {
                                var derivedBase = targetObject.GetComponent<UIAnimationTopMenu>();
                                if (derivedBase == null)
                                {
                                    derivedBase = targetObject.AddComponent<UIAnimationTopMenu>();
                                }

                                uiBase = derivedBase;
                                derivedBase.ApplySetting(settingsToPublish[i]);
                            }
                            break;
                        case UIAnimationSetting.UIAnimationClassType.SecondaryMenu_ParallelDirectional:
                            {
                                var derivedBase = targetObject.GetComponent<UIAnimationSecondaryMenu>();
                                if (derivedBase == null)
                                {
                                    derivedBase = targetObject.AddComponent<UIAnimationSecondaryMenu>();
                                }

                                uiBase = derivedBase;
                                derivedBase.ApplySetting(settingsToPublish[i]);
                            }
                            break;
                        case UIAnimationSetting.UIAnimationClassType.InnerMove_InOutToPhotoshopPosition:
                            {
                                var derivedBase = targetObject.GetComponent<UIAnimationInnerMove>();
                                if (derivedBase == null)
                                {
                                    derivedBase = targetObject.AddComponent<UIAnimationInnerMove>();
                                }

                                uiBase = derivedBase;
                                derivedBase.ApplySetting(settingsToPublish[i]);
                            }
                            break;
                        default:
                            uiBase = targetObject.GetComponent<UIAnimationBase>();
                            if (uiBase == null)
                            {
                                uiBase = targetObject.AddComponent<UIAnimationBase>();
                            }
                            break;
                    }
                    uiBase.pattern = pattern;
                    uiBase.setting = settingsToPublish[i];
                }
                else if (!overrideOriginal)
                {
                    return;
                }

                float ascendingDelay = i / UIAnimationBase.animationFPS;
                float descendingDelay = (settingsToPublish.Count - i - 1) / UIAnimationBase.animationFPS;

                uiBase.enterDelay = enterDelay == DelayOrderType.Ascending ? ascendingDelay : descendingDelay;
                uiBase.exitDelay = exitDelay == DelayOrderType.Ascending ? ascendingDelay : descendingDelay;
                uiBase.shouldReEnableChildrenAfterExit = shouldReEnableChildrenAfterExit;
                uiBase.enableType = enableType;
                uiBase.ignoreTimeScale = ignoreTimeScale;
                uiAnimationBaseList.Add(uiBase);
            }
        }

        public bool IsSamePattern(AnimationUICanvas.AnimationPattern pattern)
        {
            return (pattern == this.pattern || pattern == AnimationUICanvas.AnimationPattern.Pattern_Any);
        }

        public bool IsEnableTypeMatch(UIAnimationBase.AnimationEnableType enableType)
        {
            if (this.enableType != UIAnimationBase.AnimationEnableType.Preheated &&
                (this.enableType == enableType || this.enableType == UIAnimationBase.AnimationEnableType.BothEnterAndExit))
            {
                return true;
            }
            return false;
        }

        public bool IsOnAnimation()
        {
            return onEnterAnimation || onExitAnimation;
        }

        void Start()
        {
            if (bAutoStartEnterAnimation)
            {
                PlayEnterAnimation(null);
            }
        }

        /// <summary>
        /// 入るAnimationをPlay
        /// </summary>
        /// <param name="onFinished"></param>
        /// <returns></returns>
        public void PlayEnterAnimation(Action onFinished, Action onHalfFinished = null)
        {
            if (onEnterEvent != null)
            {
                onEnterEvent.Invoke();
            }
            RePublishAll();
            bool bInstant = true;
            if (uiAnimationBaseList.Count > 0)
            {
                for (int i = 0; i < uiAnimationBaseList.Count; i++)
                {
                    uiAnimationBaseList[i].StartEnterAnimation();
                    bInstant = false;
                }
            }

            if (waitEvenIfNoObjectsPublished)
            {
                bInstant = false;
            }

            if (bInstant)
            {
                if (onFinished != null)
                {
                    onFinished.Invoke();
                }
                CheckAndClearPlayState();
            }
            else
            {
                onEnterAnimation = true;
                StartCoroutine(EnterAnimationCoroutine(onFinished, onHalfFinished));
            }
        }

        private IEnumerator EnterAnimationCoroutine(Action onFinished, Action onHalfFinished)
        {
            if (ignoreTimeScale)
            {
                yield return new WaitForSecondsRealtime(halfCutoffDuration);
            }
            else
            {
                yield return new WaitForSeconds(halfCutoffDuration);
            }
            if (onHalfFinished != null)
            {
                onHalfFinished.Invoke();
            }
            if (ignoreTimeScale)
            {
                yield return new WaitForSecondsRealtime(maximumCutoffDuration - halfCutoffDuration);
            }
            else
            {
                yield return new WaitForSeconds(maximumCutoffDuration - halfCutoffDuration);
            }
            if (onFinished != null)
            {
                onFinished.Invoke();
            }
            CheckAndClearPlayState();
            onEnterAnimation = false;
        }

        /// <summary>
        /// 退出AnimationをPlay
        /// </summary>
        /// <param name="onHalfFinished"></param>
        /// <param name="onFinished"></param>
        /// <returns></returns>
        public void PlayExitAnimation(Action onFinished, Action onHalfFinished = null)
        {
            if (onExitEvent != null)
            {
                onExitEvent.Invoke();
            }
            RePublishAll();
            bool bInstant = true;
            if (uiAnimationBaseList.Count > 0)
            {
                for (int i = 0; i < uiAnimationBaseList.Count; i++)
                {
                    uiAnimationBaseList[i].StartExitAnimation();
                    bInstant = false;
                }
            }

            if (waitEvenIfNoObjectsPublished)
            {
                bInstant = false;
            }

            if (bInstant)
            {
                if (onHalfFinished != null)
                {
                    onHalfFinished.Invoke();
                }
                if (onFinished != null)
                {
                    onFinished.Invoke();
                }
                CheckAndClearPlayState();
            }
            else
            {
                onExitAnimation = true;
                StartCoroutine(ExitAnimationCoroutine(onFinished, onHalfFinished));
            }
        }
        private IEnumerator ExitAnimationCoroutine(Action onFinished, Action onHalfFinished = null)
        {
            if (ignoreTimeScale)
            {
                yield return new WaitForSecondsRealtime(halfCutoffDuration);
            }
            else
            {
                yield return new WaitForSeconds(halfCutoffDuration);
            }
            if (onHalfFinished != null)
            {
                onHalfFinished.Invoke();
            }
            if (ignoreTimeScale)
            {
                yield return new WaitForSecondsRealtime(maximumCutoffDuration - halfCutoffDuration);
            }
            else
            {
                yield return new WaitForSeconds(maximumCutoffDuration - halfCutoffDuration);
            }
            if (onFinished != null)
            {
                onFinished.Invoke();
            }
            CheckAndClearPlayState();
            onExitAnimation = false;
        }

        private void CheckAndClearPlayState()
        {
            if (bAutoClearAnimation)
            {
                ForceClearPlayedStatus();
            }
        }

        public void ForceClearPlayedStatus()
        {
            for (int i = 0; i < uiAnimationBaseList.Count; i++)
            {
                uiAnimationBaseList[i].ForceResetPlayedState(false, false);
            }

            // refs #112852 【クエストメニュー】サポート選択画面にてバックキーを押下した際、一回目のタップが反応しない
            //  終了アニメ再生完了前に切り替わるとフラグが立ちっぱなしになるので折る
            onExitAnimation = false;
        }

        public void CompleteRunningAnimation()
        {
            for (int i = 0; i < uiAnimationBaseList.Count; i++)
            {
                uiAnimationBaseList[i].CompleteRunningAnimations();
            }
        }

        /// <summary>
        /// UIAnimationから削除
        /// </summary>
        /// <param name="obj"></param>
        private void ReleaseUIAnimationForObject(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            if (uiAnimationBaseList != null)
            {
                for (int i = uiAnimationBaseList.Count - 1; i >= 0; i--)
                {
                    if (uiAnimationBaseList[i].gameObject == obj)
                    {
                        Destroy(uiAnimationBaseList[i]);
                        uiAnimationBaseList.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// AnimationからObjectを削除
        /// </summary>
        /// <param name="obj"></param>
        public int RemoveObject(GameObject obj)
        {
            ReleaseUIAnimationForObject(obj);
            int removedIndex = -1;
            if (settings != null)
            {
                for (int i = 0; i < settings.Length; i++)
                {
                    if (settings[i].targetObject == obj)
                    {
                        settings[i].targetObject = null;
                        removedIndex = i;
                    }
                }
            }
            if (objsInSetting != null)
            {
                for (int i = 0; i < objsInSetting.Length; i++)
                {
                    if (objsInSetting[i] == obj)
                    {
                        objsInSetting[i] = null;
                        removedIndex = i;
                    }
                }
            }
            return removedIndex;
        }

        /// <summary>
        /// Animationを置き換える
        /// </summary>
        /// <param name="index"></param>
        /// <param name="obj"></param>
        public void OverrideObjectAtIndex(int index, GameObject obj)
        {
            ReleaseUIAnimationForObject(obj);
            if (settings != null && objsInSetting != null
                && settings.Length > index && objsInSetting.Length > index)
            {
                settings[index].targetObject = obj;
                objsInSetting[index] = obj;
            }
        }
    }
}
