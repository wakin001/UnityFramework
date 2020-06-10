using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.Events;

namespace HotFix_Project
{
    /// <summary>
    /// ポップアップを表示するための Canvas 
    /// ポップアップよりも下に表示されているボタン類が反応しないようにする
    /// </summary>
    public class PopupCanvas : MonoBehaviour
    {
        // BlackLayer
        [SerializeField]
        private GameObject blackLayer;

        // touch guard
        [SerializeField]
        private GameObject touchGuard;

        // 前面用touchGuard
        [SerializeField]
        private GameObject frontTouchGuard;

        [SerializeField]
        private GameObject initialTouchGuard;

        private CanvasGroup canvasGroup;

        protected bool showStartAnimation;

        /// <summary>
        /// TimeScaleに引っかかるかどうか
        /// </summary>
        protected bool isIndependentTime = false;

        public bool showOnBaseCanvas;

        private bool _isWaitingAllPopupCanvasClosed = false;
        public bool isWaitingAllPopupCanvasClosed { get { return _isWaitingAllPopupCanvasClosed; } private set { _isWaitingAllPopupCanvasClosed = value; } }

        public bool _isErrorPopupCanvas = false;
        public bool isErrorPopupCanvas
        {
            get { return _isErrorPopupCanvas; }
            set
            {
                _isErrorPopupCanvas = value;
                // sortOrderを再設定します。
                if (showingCanvasList != null)
                {
                    for (int i = 0; i < showingCanvasList.Count; i++)
                    {
                        if (showingCanvasList[i].canvas == this)
                        {
                            if (_isErrorPopupCanvas)
                            {
                                canvas.sortingOrder = CanvasSortingOrderConstant.errorPopup + showingCanvasList[i].sortOrderIndex * CanvasSortingOrderConstant.popupDelta;
                            }
                            else
                            {
                                canvas.sortingOrder = CanvasSortingOrderConstant.popup + showingCanvasList[i].sortOrderIndex * CanvasSortingOrderConstant.popupDelta;
                            }

#if GLUON_DEBUG
                            if (DebugWindow.isOpened)
                            {
                                canvas.sortingOrder += DebugViewController.sortingOrder;
                            }
#endif

                            return;
                        }
                    }
                }
                ApplyAutoSortingOrder();
            }
        }

        [HideInInspector]
        public bool doNotRegisterBackKey = false;

        Canvas canvas = null;

        /// <summary>
        /// SortIndexを記録する
        /// </summary>
        private class CanvasSortOrderInfo
        {
            public int sortOrderIndex;
            public PopupCanvas canvas;
        }

        static private List<CanvasSortOrderInfo> showingCanvasList;


        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvas = GetComponent<Canvas>();

            ApplyAutoSortingOrder();
        }

        /// <summary>
        /// 自動的にSortOrderを増やして設定する
        /// </summary>
        protected virtual void ApplyAutoSortingOrder()
        {
            CanvasSortOrderInfo info = new CanvasSortOrderInfo();
            info.sortOrderIndex = GetCurrentSortOrderMaxIndex() + 1;
            info.canvas = this;
            if (showingCanvasList == null)
            {
                showingCanvasList = new List<CanvasSortOrderInfo>();
            }
            showingCanvasList.Add(info);

            // ポップアップが重なったら、Sorting Order を増やす
            if (isErrorPopupCanvas)
            {
                canvas.sortingOrder = CanvasSortingOrderConstant.errorPopup + info.sortOrderIndex * CanvasSortingOrderConstant.popupDelta;
            }
            else
            {
                canvas.sortingOrder = CanvasSortingOrderConstant.popup + info.sortOrderIndex * CanvasSortingOrderConstant.popupDelta;
            }
        }

        protected virtual void Start()
        {
            canvas.worldCamera = Camera.main;

            // バックキーに登録
            // なぜRemoveはこのクラスにないのがなぞ
            if (!doNotRegisterBackKey)
            {
                //BackKeyManager.instance.AddSortingOrder(canvas.sortingOrder);
            }

            if (showStartAnimation)
            {
                canvasGroup.alpha = 0;
                TweenUtil.ValueTo(0.0f, 1.0f, 8.0f / 30.0f, (value) =>
                {
                    canvasGroup.alpha = value;
                }).OnComplete(() =>
                {
                    ShowInitialTouchGuard(false);
                }).SetUpdate(isIndependentTime);
            }
            else
            {
                ShowFrontTouchGuard(false);
                ShowInitialTouchGuard(false);
                canvasGroup.alpha = 1;
            }

            // 最下に配置
            if (frontTouchGuard != null)
            {
                frontTouchGuard.transform.SetAsLastSibling();
            }

            if (initialTouchGuard != null)
            {
                initialTouchGuard.transform.SetAsLastSibling();
            }
        }

        public static int GetCurrentPopupNum()
        {
            if (showingCanvasList != null)
            {
                return showingCanvasList.Count;
            }

            return 0;
        }

        private static int GetCurrentSortOrderMaxIndex()
        {
            int maxIndex = 0;
            if (showingCanvasList != null)
            {
                for (int i = 0; i < showingCanvasList.Count; i++)
                {
                    if (showingCanvasList[i].sortOrderIndex > maxIndex)
                    {
                        maxIndex = showingCanvasList[i].sortOrderIndex;
                    }
                }
            }
            return maxIndex;
        }

        /// <summary>キャンバスを作る</summary>
        public static PopupCanvas Create(bool showBlackLayer = false, bool showOnBaseCanvas = false, bool showStartAnimation = true, bool isErrorPopupCanvas = false, bool isIndependentTime = false)
        {
            // _GluonResources ではなく _Gluon 以下のリソースから作る
            string path = "Prefabs/Popup/PopupCanvas";
            GameObject popupCanvas = null;

            //if (!showOnBaseCanvas || BaseCanvas.IsInstanceEmpty())
            if (!showOnBaseCanvas)
            {
                popupCanvas = Instantiate((GameObject)Resources.Load(path));
            }
            else
            {
                popupCanvas = (GameObject)Instantiate((GameObject)Resources.Load(path));//, BaseCanvas.instance.transform);
                // ここで既にAwakeが呼ばれてる

                //BaseCanvasがある場合、CanvasScalerで大きさが調整されなくなるのでBaseCanvasと同じ大きさにする
                RectTransform canvasRectTransform = popupCanvas.GetComponent<RectTransform>();

                canvasRectTransform.anchorMin = Vector2.zero;
                canvasRectTransform.anchorMax = Vector2.one;

                canvasRectTransform.offsetMin = Vector2.zero;
                canvasRectTransform.offsetMax = Vector2.zero;
            }

            var popupCanvasComponent = popupCanvas.GetComponent<PopupCanvas>();
            popupCanvasComponent.isIndependentTime = isIndependentTime;
            popupCanvasComponent.GetComponent<PopupCanvas>().showOnBaseCanvas = showOnBaseCanvas;

            if (!showStartAnimation)
            {

            }

            var canvas = popupCanvas.GetComponent<Canvas>();
            canvas.overrideSorting = true;

            RectTransform rectTransform = popupCanvas.GetComponent<RectTransform>();
            rectTransform.anchoredPosition3D = Vector3.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;

            popupCanvas.transform.Find("BlackLayer").gameObject.SetActive(showBlackLayer);

            //if (!SceneController.IsInstanceEmpty() && SceneController.instance.currentSceneBase != null)
            //{
            //    if (showOnBaseCanvas)
            //    {
            //        BaseCanvas.instance.OnPopupOpened();
            //    }
            //    else
            //    {
            //        SceneController.instance.currentSceneBase.OnPopupOpened();
            //    }
            //}

            popupCanvasComponent.showStartAnimation = showStartAnimation;
            popupCanvasComponent.isErrorPopupCanvas = isErrorPopupCanvas;

            return popupCanvasComponent;
        }

        /// <summary>
        /// BlackLayerを表示するか
        /// </summary>
        public void ShowBlackLayer(bool isShown)
        {
            if (blackLayer != null)
            {
                blackLayer.SetActive(isShown);
            }
        }

        /// <summary>
        /// TouchGuardを表示するか
        /// </summary>
        public void ShowTouchGuard(bool isShown)
        {
            if (touchGuard != null)
            {
                touchGuard.SetActive(isShown);
            }
        }

        /// <summary>
        /// 前面TouchGuardを表示するか
        /// </summary>
        public void ShowFrontTouchGuard(bool isShown)
        {
            if (frontTouchGuard != null)
            {
                frontTouchGuard.SetActive(isShown);
            }
        }

        /// <summary>
        /// 開始時 TouchGuardを表示するか
        /// </summary>
        public void ShowInitialTouchGuard(bool isShown)
        {
            if (initialTouchGuard != null)
            {
                initialTouchGuard.SetActive(isShown);
            }
        }

        /// <summary>
        /// ブラックレイヤーの濃さを変更
        /// </summary>
        public void SetBlackLayerAlpha(float alpha)
        {
            Image blackImage = blackLayer.GetComponent<Image>();
            var blackColor = blackImage.color;
            blackImage.color = new Color(blackColor.r, blackColor.g, blackColor.b, alpha);
        }

        /// <summary>Openアニメーション</summary>
        public void StartShowAnimation()
        {
            canvasGroup.alpha = 0;
            TweenUtil.ValueTo(0.0f, 1.0f, 10.0f / 30.0f, (value) =>
            {
                canvasGroup.alpha = value;
            }).OnComplete(() =>
            {
                ShowInitialTouchGuard(false);
            }).SetUpdate(isIndependentTime);
        }

        /// <summary>閉じるアニメーション</summary>
        public void StartCloseAnimation()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1;
                TweenUtil.ValueTo(1.0f, 0.0f, 10.0f / 30.0f, (value) =>
                {
                    if (this != null && gameObject != null && canvasGroup != null)
                    {
                        canvasGroup.alpha = value;
                    }
                }).SetUpdate(isIndependentTime);
            }
        }

        /// <summary>
        /// 現在表示されている全ポップアップの閉じるアニメーションを開始
        /// </summary>
        public void StartCloseAnimationAll(UnityEvent onCompleted = null)
        {
            var isStartClosing = false;
            foreach (var showingCanvas in showingCanvasList)
            {
                var canvas = showingCanvas.canvas;
                if (canvas != null)
                {
                    var popupBase = canvas.GetComponentInChildren<PopupBase>();
                    if (popupBase != null)
                    {
                        popupBase.CloseWithAnimation();
                        isStartClosing = true;
                    }
                }
            }

            if (isStartClosing)
            {
                isWaitingAllPopupCanvasClosed = true;
                StartCoroutine(WaitCloseAnimationAll(onCompleted));
            }
        }

        /// <summary>
        /// 現状の全ポップアップの閉じるアニメを待つ
        /// </summary>
        IEnumerator WaitCloseAnimationAll(UnityEvent onCompleted = null)
        {
            foreach (var showingCanvas in showingCanvasList)
            {
                var canvas = showingCanvas.canvas;
                if (canvas != null)
                {
                    var popupBase = canvas.GetComponentInChildren<PopupBase>();
                    if (popupBase != null)
                    {
                        if (popupBase.IsWaitingCloseAnimation())
                        {
                            yield return null;
                        }
                    }
                }
            }

            if (onCompleted != null)
            {
                onCompleted.Invoke();
            }

            isWaitingAllPopupCanvasClosed = false;
        }

        protected virtual void OnDestroy()
        {
            if (showingCanvasList != null)
            {
                for (int i = 0; i < showingCanvasList.Count; i++)
                {
                    if (showingCanvasList[i].canvas == this)
                    {
                        showingCanvasList.Remove(showingCanvasList[i]);
                        break;
                    }
                }
            }

            //if (!SceneController.IsInstanceEmpty() && SceneController.instance.currentSceneBase != null)
            //{
            //    if (showOnBaseCanvas)
            //    {
            //        BaseCanvas.instance.OnPopupClosed();
            //    }
            //    else
            //    {
            //        SceneController.instance.currentSceneBase.OnPopupClosed();
            //    }
            //}
        }
    }
}

