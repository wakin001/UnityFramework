using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Framework
{
    /// <summary>BaseScene のキャンバス</summary>
    public class BaseCanvas : SingletonComponent<BaseCanvas>
    {
        /// <summary>Start() でキャンバスを生成しない</summary>
        public static bool skipCreateOnStart;

        /// <summary>Start() で黒いキャンバスを生成する</summary>
        public static bool showBlackCanvasOnStart;

        /// <summary>メインキャンバス</summary>
        //public BaseCanvasContents mainCanvas;

        /// <summary>EventSystem</summary>
        public EventSystem eventSystem;

        /// <summary>タッチガード</summary>
        public GameObject touchGuardCanvas;

        /// <summary>黒いキャンバス</summary>
        public GameObject blackCanvas;

        /// <summary> タッチガードの有効時間リスト </summary>
        public List<float> touchGuardTimeList = new List<float>();

        /// <summary>ポップアップが開かれているか</summary>
        protected bool isPopupOpened = false;

        /// <summary> タッチガード管理オブジェクトの弱参照リスト </summary>
        [SerializeField]
        private List<WeakReference> touchGuardObjectWrefList = new List<WeakReference>();

        /// <summary>読み込みが完了しているか</summary>
        private bool isAllLoadCompleted = false;

        /// <summary>開いているポップアップの数</summary>
        private int popupCount;

        void Start()
        {
            /// <summary>開始時に黒いキャンバスを表示する</summary>
            if (showBlackCanvasOnStart)
            {
                if (blackCanvas != null)
                {
                    blackCanvas.gameObject.SetActive(true);
                }
            }

            /// <summary>メインキャンバスを作成する</summary>
            if (!skipCreateOnStart)
            {
                //mainCanvas = Instantiate((GameObject)AssetManager.Load("UI/BaseCanvasContents"), transform, false).GetComponent<BaseCanvasContents>();
            }

            skipCreateOnStart = false;

            isAllLoadCompleted = true;

            SetDragThreshold();
        }

        /// <summary>
        /// Drag敏感度を設定
        /// </summary>
        private void SetDragThreshold()
        {
            // =iPhone6 screen width in cm / touchGap = 5.8 / 50 + delta
            /*
             * default pixelDragThreshold = 5 (too low)
             * UnityEngine.EventSystems.EventSystem.current.pixelDragThreshold + " " + Screen.dpi + " " + Screen.currentResolution.width + " " + Screen.width;
             * 14 326 748 748 (iPhone7)
             * 22 480 620 620 (Galaxy S9)
             */

            if (EventSystem.current != null)
            {
                float thresholdInCM = 0.1165f;
                EventSystem.current.pixelDragThreshold = Mathf.Max(EventSystem.current.pixelDragThreshold, (int)(thresholdInCM * Screen.dpi / 2.54f));
            }
        }

        /// <summary>
        /// 更新。
        /// </summary>
        private void Update()
        {
            // 参照されなくなったタッチガード識別オブジェクトを削除するため定期更新する
            UpdateTouchGuardStatus();
        }

        /// <summary>
        /// 黒背景を有効にしているか？
        /// </summary>
        public bool IsBlackCanvasActive()
        {
            if (blackCanvas != null)
            {
                return blackCanvas.activeInHierarchy;
            }
            else
            {
                return false;
            }
        }

        /// <summary>黒いキャンバスを表示する</summary>
        public void ShowBlackCanvas(bool show)
        {
            if (blackCanvas != null)
            {
                blackCanvas.gameObject.SetActive(show);
            }
        }

        /// <summary>初期化が完了しているか</summary>
        public bool IsAllLoadCompleted()
        {
            return isAllLoadCompleted;
        }

        /// <summary>背景設定</summary>
        //public void SetActiveBg(Footer.MenuTab menuTab, bool doExitAnimation = true)
        //{
        //    if (mainCanvas != null)
        //    {
        //        if (!mainCanvas.gameObject.activeSelf)
        //        {
        //            mainCanvas.gameObject.SetActive(true);
        //        }

        //        mainCanvas.SetActiveBg(menuTab, doExitAnimation);
        //    }
        //}

        /// <summary>Canvas を有効/無効にする</summary>
        //public void SetCanvasEnable(bool enable)
        //{
        //    if (mainCanvas != null)
        //    {
        //        if (!mainCanvas.gameObject.activeInHierarchy && enable)
        //        {
        //            mainCanvas.SetAlpha(0.0f);
        //        }
        //        mainCanvas.gameObject.SetActive(enable);
        //    }
        //}

        ///// <summary>Canvas のアルファ値を設定</summary>
        //public void SetCanvasAlpha(float alpha)
        //{
        //    if (mainCanvas != null && mainCanvas.gameObject.activeInHierarchy)
        //    {
        //        mainCanvas.SetAlpha(alpha);
        //    }
        //}

        /// <summary>フェードアウトさせる（非表示にする）</summary>
        //public void FadeOutCanvas(float duration = 1.0f, float startAlpha = 1.0f, float endAlpha = 0.0f)
        //{
        //    if (mainCanvas != null && mainCanvas.gameObject.activeInHierarchy)
        //    {
        //        mainCanvas.FadeOut(duration, startAlpha, endAlpha);
        //    }
        //}

        ///// <summary>フェードインさせる（表示させる）</summary>
        //public void FadeInCanvas(float duration = 1.0f, float startAlpha = 0.0f, float endAlpha = 1.0f)
        //{
        //    if (mainCanvas != null)
        //    {
        //        mainCanvas.FadeIn(duration, startAlpha, endAlpha);
        //    }
        //}

        /// <summary>全面タッチガードを有効/無効にする</summary>
        public void SetTouchGuardEnable(bool enable)
        {
            if (touchGuardCanvas != null)
            {
                touchGuardCanvas.gameObject.SetActive(enable);
            }
        }

        /// <summary>
        /// タッチガード中か
        /// </summary>
        /// <returns></returns>
        public bool IsTouchGurdEnable()
        {
            bool flag = false;
            if (touchGuardCanvas != null)
            {
                flag = touchGuardCanvas.gameObject.activeInHierarchy;
            }

            return flag;
        }

        /// <summary>一定時間タッチを無効にする。負の数が指定されればずっと無効</summary>
        public void CreateTouchGuard(float duration = -1.0f)
        {
            SetTouchGuardEnable(true);

            touchGuardTimeList.Add(duration);

            if (duration >= 0.0f)
            {
                StartCoroutine(TouchGuardCoroutine(duration));
            }
        }

        IEnumerator TouchGuardCoroutine(float duration)
        {
            yield return new WaitForSeconds(duration);

            touchGuardTimeList.Remove(duration);

            UpdateTouchGuardStatus();
        }

        /// <summary>全てのタッチガードを削除してタッチを有効にする。特殊用途。</summary>
        public void RemoveAllTouchGuard()
        {
            touchGuardTimeList.Clear();

            UpdateTouchGuardStatus();
        }

        /// <summary>無制限時間タッチガードを削除する</summary>
        public void RemoveTouchGuard()
        {
            touchGuardTimeList.Remove(-1);

            UpdateTouchGuardStatus();
        }

        /// <summary>
        /// タッチガードの状態を更新する。
        /// </summary>
        private void UpdateTouchGuardStatus()
        {
            // 参照されなくなったタッチガードを外す
            touchGuardObjectWrefList.RemoveAll(wref => !wref.IsAlive);

            // 有効なタッチガードが全部無くなった時点でタッチが有効になる
            if (touchGuardTimeList.Count == 0 && touchGuardObjectWrefList.Count == 0)
            {
                SetTouchGuardEnable(false);
            }
        }

        /// <summary>識別子付きタッチガードを生成する</summary>
        public TouchGuardObject CreateTouchGuardObject(string label, float duration = -1)
        {
            Debug.LogFormat("CreateTouchGuardObject : {0}", label);

            SetTouchGuardEnable(true);

            var touchGuardObject = new TouchGuardObject(label);
            touchGuardObjectWrefList.Add(new WeakReference(touchGuardObject));

            if (duration >= 0.0f)
            {
                StartCoroutine(TouchGuardObjectDelayRemove(duration, touchGuardObject));
            }

            return touchGuardObject;
        }

        /// <summary>識別子付きタッチガードを削除する</summary>
        public void RemoveTouchGuardObject(TouchGuardObject touchGuardObject)
        {
            if (touchGuardObject == null)
            {
                return;
            }

            Debug.LogFormat("RemoveTouchGuardObject : {0}", touchGuardObject.Label);

            touchGuardObjectWrefList.RemoveAll(wref => wref.IsAlive && wref.Target == touchGuardObject);

            UpdateTouchGuardStatus();
        }

        /// <summary>識別子付きタッチガードを少し後に削除する</summary>
        private IEnumerator TouchGuardObjectDelayRemove(float duration, TouchGuardObject touchGuardObject)
        {
            yield return new WaitForSeconds(duration);

            RemoveTouchGuardObject(touchGuardObject);
        }

        /// <summary> Baseシーン上にポップアップが開かれたときに呼ばれる。</summary>
        public void OnPopupOpened()
        {
            popupCount++;
            isPopupOpened = true;
        }

        /// <summary> Baseシーン上のポップアップが閉じたときに呼ばれる。</summary>
        public void OnPopupClosed()
        {
            popupCount--;
            if (popupCount <= 0)
            {
                popupCount = 0;
                isPopupOpened = false;
            }
        }

        /// <summary>ポップアップが開かれているか</summary>
        public bool IsPopupOpened()
        {
            return isPopupOpened;
        }
    }
}
