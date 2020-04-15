using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using System;

namespace Framework
{
    /// <summary>
    /// UIを非表示にするアニメーションを制御するクラス
    /// </summary>
    public class UiAnimationHide : MonoBehaviour
    {
        /// <summary> 移動方向のenum </summary>
        private enum MoveDirection
        {
            /// <summary>上</summary>
            UP,
            /// <summary>下</summary>
            Down,
            /// <summary>右</summary>
            Right,
            /// <summary>左</summary>
            Left,
        }

        /// <summary>
        /// UIの状況のenum
        /// </summary>
        private enum UiCondition
        {
            /// <summary>表示中</summary>
            Show,
            /// <summary>移動中</summary>
            Move,
            /// <summary>非表示</summary>
            Hide,
        }

        /// <summary> 一つのUiの制御情報を纏めたクラス</summary>
        [Serializable]
        private class AnimationUi
        {
            /// <summary> UI本体</summary>
            public GameObject obj;
            /// <summary> 移動順</summary>
            public int moveOrder;
            /// <summary> 移動方向</summary>
            public MoveDirection moveDirection;
            /// <summary> 初期位置保存用</summary>
            [NonSerialized]
            public Vector3 defaultPos;
            /// <summary> 表示状況</summary>
            [NonSerialized]
            public UiCondition condition;
        }

        /// <summary> 移動間隔</summary>
        [SerializeField, Header("移動間隔(フレーム)")]
        private int interval = 1;
        /// <summary> 移動時間</summary>
        [SerializeField, Header("移動時間")]
        private float duration = 0.5f;
        /// <summary> 移動距離のマージン</summary>
        [SerializeField, Header("移動距離のマージン")]
        private float moveMargin = 50f;
        /// <summary> 入りのEase</summary>
        [SerializeField, Header("入りのEase")]
        private Ease enterEase = Ease.InCubic;
        /// <summary> はけのEase</summary>
        [SerializeField, Header("はけのEase")]
        private Ease exitEase = Ease.OutCubic;

        /// <summary> ヘッダーの移動順</summary>
        [SerializeField, Header("ヘッダーの移動順")]
        private int headerMoveOrder = 1;
        /// <summary> フッターの移動順</summary>
        [SerializeField, Header("フッターの移動順")]
        private int footerMoveOrder = 1;

        /// <summary> 表示範囲を指定するための親オブジェクトのRectTransform</summary>
        [SerializeField, Header("表示範囲を指定するための親オブジェクトのRectTransform")]
        private RectTransform parentObject = null;

        /// <summary> アニメーションさせるオブジェクト</summary>
        [SerializeField, Header("アニメーションさせるオブジェクト")]
        private AnimationUi[] animationObjects = null;

        /// <summary> UI移動タイミングの個数</summary>
        private int timingCount = 1;

        /// <summary>
        /// 開始時の処理
        /// </summary>
        private void Start()
        {
            for (int i = 0; i < animationObjects.Length; ++i)
            {
                int order = animationObjects[i].moveOrder;
                if (order > timingCount)
                {
                    timingCount = order;
                }
                animationObjects[i].condition = UiCondition.Show;
            }
        }

        /// <summary>
        /// UI非表示ボタンが押された
        /// </summary>
        public void OnUiHideButtonPressed()
        {
            StartCoroutine(StartSlideOut());
        }

        /// <summary>
        /// UI非表示終了
        /// </summary>
        public void OnFinishUiHideButtonPressed()
        {
            StartCoroutine(StartSlideIn());
        }

        /// <summary>
        /// スライドアウト開始
        /// </summary>
        /// <returns></returns>
        private IEnumerator StartSlideOut()
        {
            for (int timing = 1; timing <= timingCount; ++timing)
            {
                if (headerMoveOrder == timing)
                {
                    //HeaderCanvas.instance.SlideOut();
                }
                if (footerMoveOrder == timing)
                {
                    //FooterCanvas.instance.SlideOut();
                }
                for (int i = 0; i < animationObjects.Length; ++i)
                {
                    var ui = animationObjects[i];
                    if (ui.moveOrder == timing)
                    {
                        SlideOut(ui);
                    }
                }
                // 指定フレーム数待機
                for (int i = 0; i < interval; ++i)
                {
                    yield return null;
                }
            }
        }

        /// <summary>
        /// スライドイン開始
        /// </summary>
        /// <returns></returns>
        private IEnumerator StartSlideIn()
        {
            for (int timing = timingCount; timing >= 1; --timing)
            {
                if (headerMoveOrder == timing)
                {
                    //HeaderCanvas.instance.SlideIn();
                }
                if (footerMoveOrder == timing)
                {
                    //FooterCanvas.instance.SlideIn();
                }
                for (int i = 0; i < animationObjects.Length; ++i)
                {
                    var ui = animationObjects[i];
                    if (ui.moveOrder == timing)
                    {
                        SlideIn(ui);
                    }
                }
                // 指定フレーム数待機
                for (int i = 0; i < interval; ++i)
                {
                    yield return null;
                }
            }
        }

        /// <summary>
        /// UIをスライドアウト
        /// </summary>
        /// <param name="ui"></param>
        private void SlideOut(AnimationUi ui)
        {
            if (ui.obj != null)
            {
                ui.condition = UiCondition.Move;
                RectTransform rectTransform = ui.obj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    ui.defaultPos = rectTransform.anchoredPosition3D;
                    Vector3 toPos = GetToPosition(ui, rectTransform);

                    TweenUtil.ValueTo(0, 1, duration, (x) =>
                    {
                        rectTransform.anchoredPosition3D = Vector3.Lerp(ui.defaultPos, toPos, x);
                    }).SetUpdate(UpdateType.Late).SetEase(exitEase).OnComplete(() =>
                    {
                        ui.condition = UiCondition.Hide;
                    });
                }
            }
        }

        /// <summary>
        /// UIをスライドイン
        /// </summary>
        /// <param name="ui"></param>
        private void SlideIn(AnimationUi ui)
        {
            if (ui.obj != null)
            {
                ui.condition = UiCondition.Move;
                RectTransform rectTransform = ui.obj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Vector3 fromPos = rectTransform.anchoredPosition3D;

                    TweenUtil.ValueTo(0, 1, duration, (x) =>
                    {
                        rectTransform.anchoredPosition3D = Vector3.Lerp(fromPos, ui.defaultPos, x);
                    }).SetUpdate(UpdateType.Late).SetEase(enterEase).OnComplete(() =>
                    {
                        ui.condition = UiCondition.Show;
                    });
                }
            }
        }

        /// <summary>
        /// UIの移動先座標を求める
        /// </summary>
        /// <param name="ui"></param>
        /// <returns></returns>
        private Vector3 GetToPosition(AnimationUi ui, RectTransform rectTransform)
        {
            float x = rectTransform.anchoredPosition3D.x;
            float y = rectTransform.anchoredPosition3D.y;
            float z = rectTransform.anchoredPosition3D.z;
            switch (ui.moveDirection)
            {
                case MoveDirection.UP:
                    y = (parentObject.rect.height * (1 - rectTransform.anchorMax.y)) + rectTransform.rect.height + moveMargin;
                    break;
                case MoveDirection.Down:
                    y = -(parentObject.rect.height * rectTransform.anchorMax.y) - rectTransform.rect.height - moveMargin;
                    break;
                case MoveDirection.Right:
                    x = (parentObject.rect.width * (1 - rectTransform.anchorMax.x)) + rectTransform.rect.width + moveMargin;
                    break;
                case MoveDirection.Left:
                    x = -(parentObject.rect.width * rectTransform.anchorMax.x) - rectTransform.rect.width - moveMargin;
                    break;
            }
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// UIが移動中か
        /// </summary>
        /// <returns></returns>
        public bool IsMove()
        {
            for (int i = 0; i < animationObjects.Length; ++i)
            {
                if (animationObjects[i].condition == UiCondition.Move)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
