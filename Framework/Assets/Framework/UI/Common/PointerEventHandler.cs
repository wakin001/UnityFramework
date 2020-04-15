using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Framework
{
    /// <summary>
    /// UI controls又はBoxColliderを付いてるGameObjectをclick, long click eventを処理します。
    /// 連打防止もできます。
    /// click途中指が移動したら、onClick() eventを飛ばらないです。
    /// 
    /// ーーーボタンで使ったら、ボタンと同じ階層でアタッチしてください。ーーー
    /// 
    /// </summary>
    [Serializable]
    public class PointerEventHandler :
        MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        /// <summary>
        /// ポインタ（マウス/タッチ）に関するイベント
        /// </summary>
        [Serializable]
        public class TouchEvent : UnityEvent<PointerEventData> { }

        /// <summary>タップ時のイベント</summary>
        public UnityEvent onClick = null;
        /// <summary>長押し時のイベント</summary>
        public UnityEvent onLongClick = null;
        /// <summary>toggle buttonのevents</summary>
        public Toggle.ToggleEvent onToggleClicked = null;

        /// <summary>ポインターが重なった時のイベント</summary>
        private TouchEvent _onPointerEnter = new TouchEvent();
        /// <summary>ポインターが重なった時のイベント</summary>
        public TouchEvent onPointerEnter
        {
            get { return _onPointerEnter; }
            set { _onPointerEnter = value; }
        }

        /// <summary>ポインターが離れた時のイベント</summary>
        private TouchEvent _onPointerExit = new TouchEvent();
        /// <summary>ポインターが離れた時のイベント</summary>
        public TouchEvent onPointerExit
        {
            get { return _onPointerExit; }
            set { _onPointerExit = value; }
        }

        /// <summary>ポインターが押された時のイベント</summary>
        private TouchEvent _onPointerDown = new TouchEvent();
        /// <summary>ポインターが押された時のイベント</summary>
        public TouchEvent onPointerDown
        {
            get { return _onPointerDown; }
            set { _onPointerDown = value; }
        }

        /// <summary>ポインターが上がった時のイベント</summary>
        private TouchEvent _onPointerUp = new TouchEvent();
        /// <summary>ポインターが上がった時のイベント</summary>
        public TouchEvent onPointerUp
        {
            get { return _onPointerUp; }
            set { _onPointerUp = value; }
        }

        /// <summary>
        /// CellなどのバウンスAnimationをOnClickに移動するための専用Animation
        /// </summary>
        [HideInInspector]
        public UnityEvent onDelayPressReleased = null;

        /// <summary>long touch できるかどうか</summary>
        [SerializeField, HeaderAttribute("long touch できるかどうか")]
        public bool canLongTouch = false;

        /// <summary>LongTouch押し続けるに反応するか</summary>
        [SerializeField, HeaderAttribute("LongTouch押し続けるに反応するか")]
        public bool canKeepLongTouch = false;

        /// <summary>multi-touchできるかどうか</summary>
        [SerializeField, HeaderAttribute("マルチタッチイベントを処理しますか")]
        public bool needHandleMultiTouch = false;
        /// <summary>long touch判定時間 </summary>
        public float longTouchThreshold = 0.6f;
        /// <summary>keep long touch 判定時間</summary>
        public float keepLongTouchDuration = 0.1f;
        /// <summary>double touch判定時間（連打必要なボタン）</summary>
        public float doubleTouchThreshold = 0.25f;

        /// <summary>finish checking long touch?</summary>
        private bool isCanceled = false;
        /// <summary>前回click downの時の時間 (needHandleMultiTouchだけでは足りない)</summary>
        public static DateTime lastTouchDownDateTime = DateTime.MinValue;
        /// <summary>click downから経つ時間</summary>
        private float touchTime = 0f;
        /// <summary>The position of touch down.</summary>
        private Vector3 startTouchPos = Vector3.zero;
        /// <summary>前回LongTouchRepeatから経つ時間</summary>
        private float lastKeepLongTouchCalledTime = 0f;

        /// <summary>連打かどうか</summary>
        private bool isRepeatClick = false;
        /// <summary>インプットモジュール</summary>
        private StandaloneInputModuleCustom inputModule = null;
        /// <summary>目標Selectable(UI.Button, Toggleなどを使ったら。)</summary>
        private Selectable targetSelectable = null;
        /// <summary>Toggle buttonしか使わない</summary>
        private bool isOn = false;

        /// <summary>トグルかどうか</summary>
        private bool isToggle = false;
        /// <summary>タッチを監視するコルーチン</summary>
        private Coroutine watchTouchCoroutine = null;

        /// <summary> UIAnimation実行中クリック無視するフラグ </summary>
        [HideInInspector]
        public bool isBlockingByUIAnimation = false;

        /// <summary>大きいタップエフェクトか</summary>
        public static bool suppressBigEffect = false;

        /// <summary>クリック時のアニメーション</summary>
        public Action onClickAnimation;
        /// <summary>フリック時のアクション</summary>
        public UnityAction<float> onFlickVertical;

        /// <summary>縮小するボタンなどで補助的に使う指移動量の判断値</summary>
        public float shrinkTouchGap = 5.0f;

        /// <summary>
        /// 売却画面のように複数のアイコンを素早くタップすることがある際に使用
        /// 連続タップや指移動量を無視する
        /// </summary>
        public bool isMultiSelect = false;

        /// <summary>押されたら縮小するボタンだと Click 判定が厳しくなるので、OnPointerUp で独自のタップ判定をするために使用するフラグ</summary>
        private bool isPointerDownChecked = false;

        /// <summary>指移動量の判断値</summary>
        private float touchGap { get; set; }

        /// <summary>解像度を考慮した shrinkTouchGap</summary>
        private float realShrinkTouchGap { get; set; }

        /// <summary>タップエフェクトを表示するか</summary>
        [SerializeField, Header("タップエフェクトを表示する")]
        public bool isShowTapEffect = true;

        /// <summary>
        /// 生成時の処理
        /// </summary>
        private void Awake()
        {
            // ボタンをコピーしたら、isBlockingByUIAnimationもコピーされちゃうので、もう一度設定します。
            isBlockingByUIAnimation = false;
        }

        /// <summary>
        /// 初期化
        /// </summary>
        void Start()
        {
            if (lastTouchDownDateTime == DateTime.MinValue)
            {
                // タップ判定なのでローカル時間で OK
                lastTouchDownDateTime = DateTime.UtcNow;
            }

            targetSelectable = GetComponent<Selectable>();
            if (targetSelectable != null && targetSelectable is Toggle)
            {
                isOn = ((Toggle)targetSelectable).isOn;
                isToggle = true;
            }

            // 解像度に合わせる
            touchGap = 50f * Screen.width / 640.0f;

            // 解像度に合わせる
            realShrinkTouchGap = shrinkTouchGap * Screen.width / 640.0f;
        }

        /// <summary>
        /// アプリがサスペンド/レジュームしたときの処理
        /// </summary>
        /// <param name="pause"></param>
        private void OnApplicationPause(bool pause)
        {
            var button = GetComponent<Button>();
            if (button != null)
            {
                if (button.interactable)
                {
                    var animator = GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.ResetTrigger("Normal");
                        animator.ResetTrigger("Highlighted");
                        animator.ResetTrigger("Pressed");
                        animator.ResetTrigger("Disabled");
                        animator.SetTrigger("Normal");
                    }
                }
            }

            // Androidでホームボタンを押されたら長押しをキャンセル
            if (pause)
            {
                isCanceled = true;
            }
        }

        /// <summary>
        /// OnPointerClick event.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsTargetValid())
            {
                return;
            }

            if (isBlockingByUIAnimation)
            {
                ResetToggleState();
                return;
            }

            if (!IsValidClick(eventData))
            {
                Debug.Log("PointerEventHandler. click is not valid, : " + gameObject.name);
                ResetToggleState();
                return;
            }
            isCanceled = true;

            if (!isPointerDownChecked)
            {
                return;
            }
            isPointerDownChecked = false;

            // is long touch
            if (canLongTouch && touchTime > longTouchThreshold)
            {
                Debug.Log("PointerEventHandler. is long touch..");
                ResetToggleState();
                return;
            }
            // 連打
            if (isRepeatClick)
            {
                //Debug.Log("PointerEventHandler. Click too fast.");
                ResetToggleState();
                return;
            }
            // 指が移動したら、onClick eventを飛ばない。
            if (IsMoved(eventData.position))
            {
                Debug.Log("PointerEventHandler. Is moved : touchGap: " + touchGap);
                ResetToggleState();
                return;
            }
            if (onClick != null)
            {
                //if (!FooterCanvas.IsInstanceEmpty())
                //{
                //    FooterCanvas.instance.footer.ResetButtonDownStatus();
                //}

                if (targetSelectable != null && targetSelectable is Toggle)
                {
                    isOn = !isOn;
                    ((Toggle)targetSelectable).isOn = isOn;

                    if (onToggleClicked != null)
                    {
                        onToggleClicked.Invoke(isOn);
                    }
                }
                else
                {
                    onClick.Invoke();
                    if (onDelayPressReleased != null)
                    {
                        onDelayPressReleased.Invoke();
                    }

                    // 大きいタップエフェクト
                    if (!suppressBigEffect && isShowTapEffect)
                    {
                        //if (!TapEffectCanvas.IsInstanceEmpty())
                        //{
                        //    TapEffectCanvas.instance.PlayBigTapEffect();
                        //}

                        //if (!TapEffectTitleCanvas.IsInstanceEmpty())
                        //{
                        //    TapEffectTitleCanvas.instance.PlayBigTapEffect();
                        //}

                        //MainGameCtrl mainGameCtrl = MainGameCtrl.instance;
                        //if (mainGameCtrl != null && mainGameCtrl.uiCtrl != null)
                        //{
                        //    mainGameCtrl.uiCtrl.DisplayEffectClickButton();
                        //}
                    }

                    if (this == null)
                    {
                        return;
                    }

                    //if (gameObject != null)
                    //{
                    //    ButtonPressedMark.Decided(gameObject);
                    //}

                    if (onClickAnimation != null)
                    {
                        onClickAnimation.Invoke();
                    }
                }
            }
        }

        /// <summary>
        /// タッチの監視を停止する
        /// </summary>
        private void StopWatchTouchCoroutine()
        {
            if (watchTouchCoroutine != null)
            {
                StopCoroutine(watchTouchCoroutine);
                watchTouchCoroutine = null;
            }
        }

        /// <summary>
        /// OnPointerDown event.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsTargetValid())
            {
                return;
            }

            if (isBlockingByUIAnimation)
            {
                return;
            }

            isPointerDownChecked = true;

            // タップ判定なのでローカル時間で OK
            DateTime curDateTime = DateTime.UtcNow;
            TimeSpan elapseTime = curDateTime - lastTouchDownDateTime;
            float elapseSeconds = (float)elapseTime.TotalMilliseconds / 1000f;
            //Debug.Log("elapseSeconds: " + elapseSeconds);
            if (elapseSeconds < doubleTouchThreshold)
            {
                if (isMultiSelect) return;

                isRepeatClick = true;
                //lastTouchDownDateTime = curDateTime;
                // ものすごく連打してて機能に入れないのが良くなさそうので一旦実験的に外す
                return;
            }

            lastTouchDownDateTime = curDateTime;
            isRepeatClick = false;
            startTouchPos = eventData.position;
            if (canLongTouch)
            {
                lastKeepLongTouchCalledTime = 0;
                StopWatchTouchCoroutine();
                watchTouchCoroutine = StartCoroutine(WatchTouch(eventData));
            }

            if (onPointerDown != null)
            {
                onPointerDown.Invoke(eventData);
            }

            if (isToggle)
            {
                // 大きいタップエフェクト
                if (!suppressBigEffect && isShowTapEffect)
                {
                    //if (!TapEffectCanvas.IsInstanceEmpty())
                    //{
                    //    TapEffectCanvas.instance.PlayBigTapEffect();
                    //}

                    //if (!TapEffectTitleCanvas.IsInstanceEmpty())
                    //{
                    //    TapEffectTitleCanvas.instance.PlayBigTapEffect();
                    //}

                    //MainGameCtrl mainGameCtrl = MainGameCtrl.instance;
                    //if (mainGameCtrl != null)
                    //{
                    //    mainGameCtrl.uiCtrl.DisplayEffectClickButton();
                    //}
                }
            }
        }

        /// <summary>
        /// OnPointerUp event.
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            StopWatchTouchCoroutine();
            if (!IsTargetValid())
            {
                return;
            }

            if (isBlockingByUIAnimation)
            {
                return;
            }

            //Debug.Log("pointer up");
            isCanceled = true;

            if (onPointerUp != null)
            {
                onPointerUp.Invoke(eventData);
            }

            if (onFlickVertical != null)
            {
                float pos = FlickVertical(eventData.position);
                if (pos != 0)
                {
                    onFlickVertical.Invoke(pos);
                }
            }

            // トグルじゃないなら独自の Click 判定も行う
            if (isPointerDownChecked && !isToggle && !isRepeatClick)
            {
                if (isMultiSelect)
                {
                    // アイコンなどの複数選択可能オブジェクトは素早く選択できるように

                    // 縦スクロール時の誤タップを考慮して Y 方向の指移動量だけチェックする
                    if (Math.Abs(eventData.pressPosition.y - eventData.position.y) < realShrinkTouchGap)
                    {
                        // タップ判定なのでローカル時間で OK
                        lastTouchDownDateTime = DateTime.UtcNow;

                        // count as touch down
                        OnPointerClick(eventData);
                    }
                }
                else
                {
                    if (Math.Abs(eventData.pressPosition.x - eventData.position.x) < realShrinkTouchGap &&
                        Math.Abs(eventData.pressPosition.y - eventData.position.y) < realShrinkTouchGap)
                    {
                        // タップ判定なのでローカル時間で OK
                        lastTouchDownDateTime = DateTime.UtcNow;

                        // count as touch down
                        OnPointerClick(eventData);
                    }
                }
            }
        }

        /// <summary>
        /// OnPointerEnter event.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsTargetValid())
            {
                return;
            }

            if (onPointerEnter != null)
            {
                onPointerEnter.Invoke(eventData);
            }
        }

        /// <summary>
        /// OnPointerExit event.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            StopWatchTouchCoroutine();
            if (!IsTargetValid())
            {
                return;
            }
            //Debug.Log("pointer exit");
            isCanceled = true;
            if (onPointerExit != null)
            {
                onPointerExit.Invoke(eventData);
            }
        }

        /// <summary>
        /// Long touchをチェックします。
        /// </summary>
        private IEnumerator WatchTouch(PointerEventData eventData)
        {
            touchTime = 0;
            isCanceled = false;

            while (true)
            {
                touchTime += Time.deltaTime;

                if (isCanceled)
                {
                    break;
                }
                if (IsMoved(eventData.position))
                {
                    break;
                }
                if (touchTime >= longTouchThreshold)
                {
                    if (!canKeepLongTouch)
                    {
                        if (onLongClick != null)
                        {
                            Debug.Log("PointerEventHandler is long touch...");
                            onLongClick.Invoke();
                            if (onDelayPressReleased != null)
                            {
                                onDelayPressReleased.Invoke();
                            }
                        }
                        break;
                    }
                    else
                    {
                        if (lastKeepLongTouchCalledTime == 0f || touchTime >= lastKeepLongTouchCalledTime + keepLongTouchDuration)
                        {
                            lastKeepLongTouchCalledTime = touchTime;

                            if (onLongClick != null)
                            {
                                Debug.Log("PointerEventHandler is long touch...");
                                onLongClick.Invoke();
                                if (onDelayPressReleased != null)
                                {
                                    onDelayPressReleased.Invoke();
                                }
                            }
                        }
                    }
                }
                yield return null;
            }
        }

        /// <summary>
        /// 指を移動したかどうかの判断
        /// </summary>
        private bool IsMoved(Vector3 curTouchPos)
        {
            if (isMultiSelect) return false;

            if (Math.Abs(curTouchPos.x - startTouchPos.x) >= touchGap)
            {
                return true;
            }
            if (Math.Abs(curTouchPos.y - startTouchPos.y) >= touchGap)
            {
                return true;
            }
            return false;
        }



        /// <summary>
        /// 指をどれくらい移動したかどうかの判断
        /// </summary>
        /// <param name="curTouchPos"></param>
        /// <returns></returns>
        private float FlickVertical(Vector3 curTouchPos)
        {
            float dis = 0;
            if (Math.Abs(curTouchPos.x - startTouchPos.x) >= touchGap)
            {
                dis = curTouchPos.x - startTouchPos.x;
            }
            return dis;
        }



        /// <summary>
        /// 今のtouch eventを処理しますか？
        /// needHandleMultiTouchはfalseなら、マルチイベントの処理防止ができます。
        /// </summary>
        private bool IsValidClick(PointerEventData pointEventData)
        {
            if (needHandleMultiTouch)
            {
                return true;
            }
            if (inputModule == null)
            {
                EventSystem eventSystem = EventSystem.current;
                if (eventSystem == null)
                {
                    Debug.Log("PointerEventHandler, inputModule is null");
                    return false;
                }
                inputModule = eventSystem.GetComponent<StandaloneInputModuleCustom>();
            }
            if (inputModule != null)
            {
                int activePointerId = inputModule.GetActivePointerId();
                //Debug.Log("PointerEventHandler, activePointerId: " + activePointerId + " pointEventData.pointerId: " + pointEventData.pointerId);
                if (activePointerId != pointEventData.pointerId)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// ボタンで使ったら、ボタンの有効性を判断します。
        /// 3D GameObjectならいつもtrueを返します。
        /// </summary>
        /// <returns></returns>
        private bool IsTargetValid()
        {
            if (targetSelectable != null && !targetSelectable.interactable)
            {
                Debug.Log("PointerEventHandler. target is not valid, targetSelectable.interactable: " + targetSelectable.interactable);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 非アクティブ時の処理
        /// </summary>
        void OnDisable()
        {
            isCanceled = true;
        }

        /// <summary>
        /// Toggleの自分の処理でisOn flagを変更するので、ここリセット必要があります。s
        /// </summary>
        private void ResetToggleState()
        {
            if (targetSelectable != null && targetSelectable is Toggle)
            {
                Toggle toggle = targetSelectable as Toggle;
                toggle.isOn = isOn;
            }
        }

        /// <summary>
        /// ResetToggleStateの逆の状況
        /// プログラムで設定されたらisOnを切り替える
        /// </summary>
        public void ResetFlagWhenIsOnChangedByScript()
        {
            if (targetSelectable != null && targetSelectable is Toggle)
            {
                Toggle toggle = targetSelectable as Toggle;
                isOn = toggle.isOn;
            }
        }

        /// <summary>
        /// [Added],taneda:2018.10.25
        /// targetSelectable を無効にする
        /// </summary>
        public void ForceReleaseTargetSelectable()
        {
            if (targetSelectable != null)
            {
                targetSelectable = null;
            }
        }

        /// <summary>
        /// 現在のCallbackを削除して新しいCallbackを設定する
        /// ButtonSoundだけ保留
        /// </summary>
        public void ReplaceCallback(UnityAction callback)
        {
            onClick.RemoveAllListeners();
            //var buttonSound = GetComponent<ButtonSound>();
            //if (buttonSound != null)
            //{
            //    buttonSound.SetActions();
            //}
            onClick.AddListener(callback);
        }
    }

}
