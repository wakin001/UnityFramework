using System;
using UnityEngine;
using UnityEngine.UI;

namespace HotFix_Project
{
    /// <summary>
    /// Sceneの中にViewのベースクラス
    /// </summary>
    public class ViewBase : AnimationUIObject
    {
        public ViewModelBase curModel { get; private set; } = null;

        // Viewの名前
        public string viewName { get; set; } = "";

        [SerializeField]
        private Button backButton = null;

        /// <summary>Enter アニメーション完了を待たずに TouchGuard を外す</summary>
        public bool removeTouchGuardOnActive = false;

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        /// <summary>
        /// Viewが表示された後呼ばれる。
        /// </summary>
        public virtual void OnViewBecomeActive(ViewModelBase model)
        {
            curModel = model;
            RegisterBackKey(canvas, backButton);
        }

        /// <summary>
        /// Enter Aniamtionが終わった後呼ばれる。
        /// </summary>
        public virtual void OnViewEnterAnimationEnded()
        {

        }

        /// <summary>
        /// Viewが非表示された前呼ばれる。
        /// </summary>
        public virtual void OnViewBecomeInActive()
        {
            RemoveBackKey(canvas, backButton);
        }

        /// <summary>
        /// Sceneの中にViewを変わります。
        /// </summary>
        public virtual void ChangeView(
            string viewName,
            ViewModelBase model = null,
            AnimationUICanvas.AnimationPattern exitPattern = AnimationUICanvas.AnimationPattern.Pattern_1,
            AnimationUICanvas.AnimationPattern enterPattern = AnimationUICanvas.AnimationPattern.Pattern_1,
            bool delayEnterAnimation = true)
        {
            ViewManager.instance.ChangeView(viewName, model, exitPattern, enterPattern, delayEnterAnimation: delayEnterAnimation);
        }

        /// <summary>
        /// 表示/非表示します。
        /// </summary>
        public virtual void ShowView(bool toShow)
        {
            gameObject.SetActive(toShow);
        }

        /// <summary>
        /// バックキーイベント登録
        /// </summary>
        public void RegisterBackKey(Canvas canvas, Button backButton)
        {
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }
            if (canvas != null && backButton != null)
            {
                //BackKeyManager.instance.AddSortingOrder(canvas.sortingOrder);
                //BackKeyManager.instance.SetBackKeyEvent(canvas.sortingOrder, backButton.GetComponent<PointerEventHandler>().onClick, true);
            }
        }

        /// <summary>
        /// バックキーイベントを解放
        /// </summary>
        public void RemoveBackKey(Canvas canvas, Button backButton)
        {
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }
            if (canvas != null && backButton != null)
            {
                //BackKeyManager.instance.RemoveBackKeyEvent(canvas.sortingOrder, true);
            }
        }
    }
}
