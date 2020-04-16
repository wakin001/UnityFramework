using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using DG.Tweening;

namespace Framework
{
    /// <summary>
    /// Popupのベースクラス
    /// </summary>
    public class PopupBase : MonoBehaviour
    {
        public enum AnimationType
        {
            None = -1,      // custom animation
            Normal,    // Show:下からMoveとFadeIn、Close:下にMoveとFadeOut
            Floating,  // Show:中央から上下に拡張、Close:上下から中央に縮小
        }

        [SerializeField, Header("Callbacks")]
        public UnityEvent closeButtonPressed;

        [SerializeField]
        public UnityEvent beforeCloseAnimation = new UnityEvent();

        [SerializeField]
        protected PointerEventHandler backKeyEventHandler = null;

        /// <summary>
        /// バックキーで対応するボタンがあっても指定を別のEventにしたい場合
        /// </summary>        /// 
        protected UnityEvent overrideBackKeyEvent = null;

        protected bool showStartAnimation = true;
        protected bool isWaitingCloseAnimation = false;
        public Canvas popupCanvas { get; set; }

        public AnimationType animationType = AnimationType.Normal;

        /// <summary>アニメーションフレーム数（ver.10 で短くする）</summary>
        public int animationFrame = 6;

        /// <summary>ver.10 より前の標準アニメーションフレーム数</summary>
        private const int slowAnimationFrame = 6;

        /// <summary>ver.10 以降の標準アニメーションフレーム</summary>
        private const int fastAnimationFrame = 5;

        private Vector2 oldMaskSizeForFloating = Vector2.zero;

        static public int popupCount = 0;

        /// <summary>ポップアップを作成する</summary>
        public static T Create<T>(string path, Transform parentTransform = null, bool useAssetLoader = true, bool asErrorPopup = false) where T : PopupBase
        {
            var original = useAssetLoader ? AssetManager.Load(path) : Resources.Load(path);
            if (original == null)
            {
                Debug.Log("not find path : " + path.ToString());
                return null;
            }
            PopupCanvas popupCanvas = PopupCanvas.Create(true, isErrorPopupCanvas: asErrorPopup);
            if (parentTransform != null)
            {
                popupCanvas.transform.SetParent(parentTransform, false);
            }
            GameObject popup = GameObject.Instantiate(original) as GameObject;
            popup.transform.SetParent(popupCanvas.transform, false);
            popup.SetActive(true);

            T popupCtrl = popup.GetComponent<T>();
            popupCtrl.popupCanvas = popupCanvas.GetComponent<Canvas>();

            return popupCtrl;
        }

        /// <summary>
        /// ポップアップが存在するか？
        /// </summary>
        /// <returns></returns>
        static public bool isPopupCreate()
        {
            if (popupCount > 0)
            {
                return true;
            }

            return false;
        }

        protected virtual void Start()
        {
            // アニメーション速度を変更
            if (animationFrame == slowAnimationFrame)
            {
                animationFrame = fastAnimationFrame;
            }

            StartCoroutine(WaitForStartAnimation());

            UnityEvent backKeyEvent = null;
            if (overrideBackKeyEvent != null)
            {
                backKeyEvent = overrideBackKeyEvent;
            }
            else if (backKeyEventHandler != null)
            {
                backKeyEvent = backKeyEventHandler.onClick;
            }
            // バックキーイベント登録
            //BackKeyManager.instance.SetBackKeyEvent(popupCanvas.sortingOrder, backKeyEvent, false);

            popupCount++;
        }

        /// <summary>
        /// 子クラスのStart()が呼ばれた後アニメーションをします。
        /// </summary>
        private IEnumerator WaitForStartAnimation()
        {
            yield return new WaitForEndOfFrame();
            if (showStartAnimation)
            {
                StartShowAnimation();
            }
        }

#if GLUON_DEBUG
        /// <summary> ポップアップ名称取得 </summary>
        private string GetPopupName()
        {
            string popupName = null;
            var type = GetType();
            if (type == typeof(CommonPopup))
            {
                CommonPopup commonPopup = (CommonPopup)this;
                popupName = commonPopup.titleText;
            }
            else
            {
                popupName = type.Name;
            }
            return popupName;
        }
#endif

        /// <summary>
        /// BackKeyがボタンじゃない場合
        /// </summary>
        /// <param name="backKeyEvent"></param>
        public void SetOverrideBackKeyEvent(UnityEvent backKeyEvent)
        {
            overrideBackKeyEvent = backKeyEvent;
        }

        /// <summary>開いたときのアニメーション</summary>
        public virtual void StartShowAnimation(UnityEvent onCompleted = null)
        {
            // キャンバスも表示
            PopupCanvas popupCanvasComponent = null;
            if (popupCanvas != null)
            {
                popupCanvasComponent = popupCanvas.GetComponent<PopupCanvas>();
                popupCanvasComponent.StartShowAnimation();
                popupCanvasComponent.ShowFrontTouchGuard(true); // 前面タッチガードON
            }

            TweenCallback completeCallback = () =>
            {
                if (onCompleted != null)
                {
                    onCompleted.Invoke();
                }

                if (popupCanvasComponent != null)
                {
                    popupCanvasComponent.ShowFrontTouchGuard(false); // 前面タッチガードOFF
                }
            };

            switch (animationType)
            {
                case AnimationType.None:
                    TweenUtil.Wait(animationFrame / 30.0f).OnComplete(completeCallback);
                    break;

                case AnimationType.Normal:
                    // 位置
                    var rectTransform = GetComponent<RectTransform>();
                    Vector3 originalPosition = rectTransform.anchoredPosition3D;

                    float moveY = -EdgeToEdgeCanvas.edgeToEdgeBaseHeight / 4;
                    rectTransform.anchoredPosition3D = originalPosition + new Vector3(0, moveY, 0);
                    TweenUtil.ValueTo(moveY, 0.0f, animationFrame / 30.0f, (value) =>
                    {
                        rectTransform.anchoredPosition3D = originalPosition + new Vector3(0, value, 0);
                    }).SetEase(Ease.OutQuad);

                    // アルファ
                    var canvasGroup = GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                    {
                        canvasGroup = gameObject.AddComponent<CanvasGroup>();
                    }

                    canvasGroup.alpha = 0;
                    TweenUtil.ValueTo(0.0f, 1.0f, animationFrame / 30.0f, (value) =>
                    {
                        canvasGroup.alpha = value;
                    }).SetEase(Ease.OutQuad).OnComplete(completeCallback);
                    break;

                case AnimationType.Floating:
                    var rectMask = GetComponentInChildren<RectMask2D>();
                    if (rectMask == null)
                    {
                        Debug.LogWarning("Please add RectMask2D on Bg image.");
                        return;
                    }
                    RectTransform rectTrans = rectMask.GetComponent<RectTransform>();
                    if (oldMaskSizeForFloating == Vector2.zero)
                    {
                        oldMaskSizeForFloating = rectTrans.sizeDelta;
                    }
                    rectTrans.sizeDelta = new Vector2(oldMaskSizeForFloating.x, 0f);
                    rectTrans.DOSizeDelta(oldMaskSizeForFloating, animationFrame / 30f).SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                        {
                            rectMask.enabled = false;
                            if (completeCallback != null)
                            {
                                completeCallback.Invoke();
                            }
                        });
                    break;
            }

        }

        /// <summary>閉じるときのアニメーション</summary>
        public virtual void StartCloseAnimation(UnityEvent onCompleted = null)
        {
            isWaitingCloseAnimation = true;
            StartCoroutine(StartCloseAnimationCoroutine(onCompleted));
        }

        /// <summary>閉じるときのアニメーション</summary>
        protected virtual IEnumerator StartCloseAnimationCoroutine(UnityEvent onCompleted = null)
        {
            //if (!SceneController.IsInstanceEmpty())
            //{
            //    if (SceneController.instance.currentSceneBase != null)
            //    {
            //        SceneController.instance.currentSceneBase.CreateTouchGuard(16.0f / 30.0f);
            //    }
            //}

            yield return new WaitForSecondsRealtime(3.0f / 30.0f);

            beforeCloseAnimation.Invoke();

            // キャンバスも消す
            PopupCanvas popupCanvasComponent = null;
            if (popupCanvas != null)
            {
                popupCanvasComponent = popupCanvas.GetComponent<PopupCanvas>();
                popupCanvasComponent.StartCloseAnimation();
                popupCanvasComponent.ShowFrontTouchGuard(true); // 前面タッチガードON
            }

            switch (animationType)
            {
                case AnimationType.Normal:
                    // 位置
                    var rectTransform = GetComponent<RectTransform>();
                    Vector3 originalPosition = rectTransform.anchoredPosition3D;

                    float moveY = -EdgeToEdgeCanvas.edgeToEdgeBaseHeight / 4;
                    TweenUtil.ValueTo(0.0f, moveY, animationFrame / 30.0f, (value) =>
                    {
                        rectTransform.anchoredPosition3D = originalPosition + new Vector3(0, value, 0);
                    }).SetEase(Ease.InQuad);

                    // アルファ
                    var canvasGroup = GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                    {
                        canvasGroup = gameObject.AddComponent<CanvasGroup>();
                    }

                    canvasGroup.alpha = 1;
                    TweenUtil.ValueTo(1.0f, 0.0f, animationFrame / 30.0f, (value) =>
                    {
                        canvasGroup.alpha = value;
                    }).SetEase(Ease.InQuad).OnComplete(() =>
                    {
                        // Popupを元の位置に設定します。Hide()して、もう一回Show()するとき正しい位置にするために。
                        rectTransform.anchoredPosition3D = originalPosition;
                        if (onCompleted != null)
                        {
                            onCompleted.Invoke();
                        }
                        if (popupCanvasComponent != null) popupCanvasComponent.ShowFrontTouchGuard(false); // 前面タッチガードOFF
                        isWaitingCloseAnimation = false;
                    });
                    break;

                case AnimationType.Floating:
                    var rectMask = GetComponentInChildren<RectMask2D>(true);
                    if (rectMask == null)
                    {
                        Debug.LogWarning("Please add RectMask2D on Bg image.");
                        yield break;
                    }

                    rectMask.enabled = true;
                    RectTransform rectTrans = rectMask.GetComponent<RectTransform>();
                    if (oldMaskSizeForFloating == Vector2.zero)
                    {
                        oldMaskSizeForFloating = rectTrans.sizeDelta;
                    }
                    rectTrans.DOSizeDelta(new Vector2(oldMaskSizeForFloating.x, 0f), animationFrame / 30f).SetEase(Ease.InQuad).OnComplete(() =>
                    {
                        if (onCompleted != null)
                        {
                            onCompleted.Invoke();
                        }

                        if (popupCanvasComponent != null) popupCanvasComponent.ShowFrontTouchGuard(false); // 前面タッチガードOFF
                        isWaitingCloseAnimation = false;
                    });
                    break;
            }

        }
        /// <summary>
        /// 閉じるアニメーション待機中か判定
        /// </summary>
        public bool IsWaitingCloseAnimation()
        {
            return isWaitingCloseAnimation;
        }


        /// <summary>
        /// ポップアップ用 Canvas を破棄して閉じる
        /// Close Animationはなし。
        /// </summary>
        public void Close()
        {
            if (beforeCloseAnimation != null)
            {
                beforeCloseAnimation.Invoke();
            }

            if (popupCanvas != null)
            {
                // バックキーイベント削除
                //BackKeyManager.instance.RemoveBackKeyEvent(popupCanvas.sortingOrder, true);
                Destroy(popupCanvas.gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            popupCount--;
        }

        /// <summary>
        /// ポップアップ用 Canvas を破棄して閉じる
        /// Close Animationはある。
        /// </summary>
        public void CloseWithAnimation()
        {
            if (popupCanvas != null)
            {
                closeButtonPressed.AddListener(Close);
                StartCloseAnimation(closeButtonPressed);
            }
        }

        /// <summary>
        /// ポップアップを表示する
        /// </summary>
        public void Show(bool withAnimation)
        {
            if (popupCanvas != null)
            {
                popupCanvas.gameObject.SetActive(true);
                if (withAnimation)
                {
                    StartShowAnimation();
                }

                // StartCloseAnimationCoroutineでbutton disableなので、ここenableにする。
                var buttons = GetComponentsInChildren<Button>();
                for (int i = 0; i < buttons.Length; i++)
                {
                    buttons[i].enabled = true;
                }

                var handlers = GetComponentsInChildren<PointerEventHandler>();
                for (int i = 0; i < handlers.Length; i++)
                {
                    handlers[i].enabled = true;
                }
            }
        }

        /// <summary>
        /// ポップアップを不表示する
        /// </summary>
        public void Hide(bool withAnimation)
        {
            if (popupCanvas != null)
            {
                if (withAnimation)
                {
                    closeButtonPressed.AddListener(() =>
                    {
                        popupCanvas.gameObject.SetActive(false);
                    });
                    StartCloseAnimation(closeButtonPressed);
                }
                else
                {
                    popupCanvas.gameObject.SetActive(false);
                    if (closeButtonPressed != null)
                    {
                        closeButtonPressed.Invoke();
                    }
                }
            }
        }
    }
}
