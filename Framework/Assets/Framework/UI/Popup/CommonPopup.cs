using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using DG.Tweening;
using System.Collections.Generic;
using System;

namespace Framework
{
    /// <summary>
    /// このクラスを使ってシンプルなポップアップを作成することができる
    /// </summary>
    public class CommonPopup : PopupBase
    {
        /// <summary>画面タイプ</summary>
        public enum Menu
        {
            MyPage,
            Growth,
            Party,
            Battle,
            Fort,
            Summon,
        }

        /// <summary>縦のサイズ</summary>
        public enum Size
        {
            S,      // エラーや確認画面でよく使う
            M,      // シンプルなアイコンリストなどで使う
            L,      // WebView、タブ付き、城などで使う
            CUSTOM, // 独自サイズ
        };

        /// <summary>ボタンの種類</summary>
        public enum BottomButtons
        {
            Close,    // 「閉じる」あるいは「OK」ボタンのみ
            CancelOk, // 「キャンセル」と「OK」の２つ
            None,
        }


        [SerializeField, Header("メニュー")]
        public Menu menuTab;

        [SerializeField, Header("サイズタイプ")]
        public Size size;

        [SerializeField, Header("カスタムの場合の高さ")]
        public float customHeight = 600;

        [SerializeField, Header("ボタンの種類")]
        public BottomButtons bottomButtons;

        [SerializeField, TextArea(1, 30)]
        public string text;

        [SerializeField]
        public TextAnchor textAnchor = TextAnchor.MiddleCenter;

        [SerializeField]
        public UnityEvent cancelButtonPressed;

        [SerializeField]
        public UnityEvent okButtonPressed;

        [SerializeField]
        protected Image frame;

        [SerializeField]
        protected Text title;
#if GLUON_DEBUG
        // 自動デバッグ（botverse）用
        public string titleText
        {
            get { return title.text; }
        }
#endif
        [SerializeField]
        protected Text textObj;

        [SerializeField]
        protected Button cancelButton;

        [SerializeField]
        protected Text cancelText;

        [SerializeField]
        protected Button okButton;

        [SerializeField]
        protected Text okText;

        [SerializeField]
        protected Button closeButton;

        [SerializeField]
        protected Text closeText;

        /// <summary>中に表示するテーブルビュー。タブで切り替える場合は全部登録する</summary>
        [SerializeField]
        public RectTransform[] tableViews;

        protected Color whiteColor;
        protected Color blackColor;
        protected Color blackOutlineColor;

        protected float _textWidthMargin = 20;
        public float textWidthMargin
        {
            get { return _textWidthMargin; }
            set { _textWidthMargin = value; }
        }
        protected float _textHeightMargin = 150;
        public float textHeightMargin
        {
            get { return _textHeightMargin; }
            set { _textHeightMargin = value; }
        }
        protected float textPositionOffsetY = 10;

        /// <summary>シンプルなポップアップを作成する</summary>
        public static CommonPopup Create(Size size = Size.S, BottomButtons bottomButtons = BottomButtons.Close,
           bool showBlackLayer = true, bool showStartAnimation = true)
        {
            //if (!SceneController.IsInstanceEmpty())
            //{ // webviewを閉じるコールバックを呼ぶ
            //    var scene = SceneController.instance.currentSceneBase;
            //    if (scene != null && scene.webViewCloseCallback != null)
            //    {
            //        scene.webViewCloseCallback.Invoke();
            //    }
            //}

            PopupCanvas popupCanvas = PopupCanvas.Create(showBlackLayer: showBlackLayer, showStartAnimation: showStartAnimation);

            string path = "UI/Popup/CommonPopup";

            GameObject popupObject;

            // MyPage ポップアップは初回ダウンロード前に使われる可能性があるので _Gluon/Resources に置かれる
            //if (Footer.currentMenuTab == Footer.MenuTab.None)
            //{
            //    popupObject = Instantiate((GameObject)Resources.Load(path + "PreIn"));
            //}
            //else
            {
                popupObject = Instantiate((GameObject)AssetManager.Load(path));
            }

            popupObject.transform.SetParent(popupCanvas.gameObject.transform);
            popupObject.transform.SetAsLastSibling();

            CommonPopup popup = popupObject.GetComponent<CommonPopup>();
            popup.popupCanvas = popupCanvas.GetComponent<Canvas>();
            popup.size = size;
            popup.bottomButtons = bottomButtons;
            popup.showStartAnimation = showStartAnimation;

            return popup;
        }

        /// <summary>シンプルなポップアップを作成する</summary>
        public static CommonPopup CreateWithPrefabPath(string prefabPath, bool showBlackLayer = false, bool showStartAnimation = true)
        {
            //if (!SceneController.IsInstanceEmpty())
            //{ // webviewを閉じるコールバックを呼ぶ
            //    var scene = SceneController.instance.currentSceneBase;
            //    if (scene != null && scene.webViewCloseCallback != null)
            //    {
            //        scene.webViewCloseCallback.Invoke();
            //    }
            //}

            PopupCanvas popupCanvas = PopupCanvas.Create(showBlackLayer: showBlackLayer, showStartAnimation: showStartAnimation);

            GameObject popupObject = Instantiate((GameObject)AssetManager.Load(prefabPath));
            popupObject.transform.SetParent(popupCanvas.gameObject.transform);
            popupObject.transform.SetAsLastSibling();

            CommonPopup popup = popupObject.GetComponent<CommonPopup>();
            popup.popupCanvas = popupCanvas.GetComponent<Canvas>();
            popup.showStartAnimation = showStartAnimation;

            return popup;
        }

        protected virtual void Awake()
        {
            menuTab = Menu.MyPage;//  (Footer.MenuTab)Footer.currentMenuTabForIndexing;

            if (textObj != null)
            {
                textObj.text = text;
                textObj.alignment = textAnchor;
            }
        }

        protected override void Start()
        {
            // Canvas が設定されてないなら親を辿って探す
            if (popupCanvas == null)
            {
                for (Transform tr = transform.parent; tr != null; tr = tr.parent)
                {
                    popupCanvas = tr.gameObject.GetComponent<Canvas>();
                    if (popupCanvas != null)
                    {
                        break;
                    }
                }
            }

            var rectTransform = GetComponent<RectTransform>();
            rectTransform.anchoredPosition3D = Vector3.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;

            InitFrameSizeAndPosition();
            InitContentSize();
            InitBottomButtons();

            base.Start();
        }

        /// <summary>
        /// PUのサイズに応じてFrame調整
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="size"></param>
        /// <param name="customHeight"></param>
        /// <returns></returns>
        public static Vector2 AdjustPopupFrameBySize(RectTransform frame, Size size, float customHeight = 0)
        {
            if (frame == null)
            {
                return Vector2.zero;
            }

            float width = frame.sizeDelta.x;
            float height = frame.sizeDelta.y;

            // EdgeToEdge 対応は Canvas でやるのでここでの対応は不要
            float y = 0;
            switch (size)
            {
                case Size.S:
                    height = 456;
                    y = -23;
                    break;

                case Size.M:
                    height = 753;
                    y = -40;
                    break;

                case Size.L:
                    height = 912;
                    y = -50;
                    break;

                default:
                    height = customHeight;
                    break;
            }

            var adjustedSize = new Vector2(width, height);
            frame.sizeDelta = adjustedSize;
            frame.anchoredPosition3D = new Vector3(frame.anchoredPosition3D.x, y, 0);
            return adjustedSize;
        }

        /// <summary>ポップアップの高さと位置を調整する</summary>
        protected virtual void InitFrameSizeAndPosition()
        {
            if (frame == null)
            {
                return;
            }
            var adjustedSize = AdjustPopupFrameBySize(frame.GetComponent<RectTransform>(), size, customHeight);
            InitTextArea(adjustedSize.x, adjustedSize.y);
        }

        /// <summary>コンテンツの高さを調整する</summary>
        protected virtual void InitContentSize()
        {
            if (frame == null)
            {
                return;
            }

            float height = frame.GetComponent<RectTransform>().sizeDelta.y;
            height -= 100;

            for (int i = 0; i < tableViews.Length; i++)
            {
                tableViews[i].sizeDelta = new Vector2(tableViews[i].sizeDelta.x, height);
            }
        }

        /// <summary>テキスト表示領域のサイズを調整する</summary>
        private void InitTextArea(float width, float height)
        {
            if (textObj != null)
            {
                RectTransform textRectTransform = textObj.GetComponent<RectTransform>();
                textRectTransform.sizeDelta = new Vector2(width - textWidthMargin, height - textHeightMargin);
            }
        }

        /// <summary>下部のボタンを設定する</summary>
        protected void InitBottomButtons()
        {
            Button backButton = null;
            switch (bottomButtons)
            {
                case BottomButtons.Close:
                    if (cancelButton != null) cancelButton.gameObject.SetActive(false);
                    if (okButton != null) okButton.gameObject.SetActive(false);
                    if (closeButton != null) closeButton.gameObject.SetActive(true);
                    backButton = closeButton;
                    break;

                case BottomButtons.CancelOk:
                    if (cancelButton != null) cancelButton.gameObject.SetActive(true);
                    if (okButton != null) okButton.gameObject.SetActive(true);
                    if (closeButton != null) closeButton.gameObject.SetActive(false);
                    backButton = cancelButton;
                    break;

                case BottomButtons.None:
                    if (cancelButton != null) cancelButton.gameObject.SetActive(false);
                    if (okButton != null) okButton.gameObject.SetActive(false);
                    if (closeButton != null) closeButton.gameObject.SetActive(false);
                    break;

                default:
                    break;
            }

            if (backKeyEventHandler == null && backButton != null && overrideBackKeyEvent == null)
            {
                // 指定が無い場合、Close/Cancelボタンをバックキーイベント登録・更新
                //BackKeyManager.instance.SetBackKeyEvent(popupCanvas.sortingOrder, backButton.GetComponent<PointerEventHandler>().onClick, false);
            }
        }

        /// <summary>閉じるボタンが押された時に uGUI から呼ばれる</summary>
        public virtual void OnCloseButtonPressed()
        {
            StartCloseAnimation(closeButtonPressed);
        }

        /// <summary>キャンセルボタンが押された時に uGUI から呼ばれる</summary>
        public virtual void OnCancelButtonPressed()
        {
            StartCloseAnimation(cancelButtonPressed);
        }

        /// <summary>OK ボタンが押された時に uGUI から呼ばれる</summary>
        public virtual void OnOkButtonPressed()
        {
            StartCloseAnimation(okButtonPressed);
        }

        /// <summary>タイトルの文字を変える</summary>
        public void SetTitleText(string str)
        {
            if (title != null)
            {
                title.text = str;
            }
        }

        /// <summary>タイトルのサイズを変える</summary>
        public void SetTitleSize(Vector2 vec)
        {
            if (title != null)
            {
                RectTransform rect = title.GetComponent<RectTransform>();
                rect.sizeDelta = vec;
            }
        }

        /// <summary>本文の文字を変える</summary>
        public virtual void SetText(string str)
        {
            if (textObj != null)
            {
                textObj.text = CommonUtil.FormatNewLineCode(str);
            }
        }

        /// <summary>本文の文字の表示位置を変える</summary>
        public void SetTextAnchor(TextAnchor anchor)
        {
            if (textObj != null)
            {
                textAnchor = anchor;
                textObj.alignment = textAnchor;
            }
        }

        /// <summary>OK ボタンの有効無効を切り替える</summary>
        public void SetEnableOkButton(bool arg)
        {
            if (okButton != null)
            {
                okButton.interactable = arg;
            }
        }

        /// <summary>キャンセルボタンの有効無効を切り替える</summary>
        public void SetEnableCancelButton(bool arg)
        {
            if (cancelButton != null)
            {
                cancelButton.interactable = arg;
            }
        }

        /// <summary>OKボタンのテキストを変更する</summary>
        public void ChangeOkButtonText(string text)
        {
            if (okButton != null)
            {
                Text okText = okButton.GetComponentInChildren<Text>();
                if (okText != null)
                {
                    okText.text = text;
                }
            }
        }

        /// <summary>キャンセルボタンのテキストを変更する</summary>
        public void ChangeCancelButtonText(string text)
        {
            if (cancelButton != null)
            {
                Text cancelText = cancelButton.GetComponentInChildren<Text>();
                if (cancelText != null)
                {
                    cancelText.text = text;
                }
            }
        }

        /// <summary>
        /// CancelButtonの音を変える
        /// </summary>
        //public void ChangeCancelButtonSound(ButtonSound.SEType seType)
        //{
        //    var buttonSound = cancelButton.GetComponent<ButtonSound>();
        //    buttonSound.seType = seType;
        //}

        /// <summary>閉じるボタンのテキストを変更する</summary>
        public void ChangeCloseButtonText(string text)
        {
            if (closeButton != null)
            {
                Text closeText = closeButton.GetComponentInChildren<Text>();
                if (closeText != null)
                {
                    closeText.text = text;
                }
            }
        }

        ///// <summary>OKボタンのメンテナンス制御を変更する</summary>
        //public void ChangeOkButtonMaintenanceTypes(List<MaintenanceFunctionType> maintenanceTypes)
        //{
        //    if (okButton != null)
        //    {
        //        var maintenanceCtrl = okButton.GetComponent<MaintenanceButtonController>();
        //        if (maintenanceCtrl != null)
        //        {
        //            maintenanceCtrl.enabled = (maintenanceTypes.Count > 0);
        //            maintenanceCtrl.UpdateEnableState(withSwitchFuncTypes: maintenanceTypes);
        //        }
        //    }
        //}

        ///// <summary>
        ///// 現在のメニュータブをインデックス化したものを受け渡し用引数に設定
        ///// </summary>
        //public void SetCurrentMenuTabForIndexing(UiImageExchanger.IndexReceiver indexReceiver)
        //{
        //    if (indexReceiver != null)
        //    {
        //        indexReceiver.index = Footer.currentMenuTabForIndexing;
        //    }
        //}

        /// <summary>
        /// Okボタンを取得
        /// </summary>
        public Button GetOkButton()
        {
            return okButton;
        }
    }
}
