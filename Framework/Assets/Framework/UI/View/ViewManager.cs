using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// シーンの中に幾つなViewがあれば、ここでViewを管理します。
    /// </summary>
    public class ViewManager : SingletonComponent<ViewManager>
    {
        [SerializeField]
        private Transform parentTransform = null;

        // View cache.
        private Dictionary<string, ViewBase> views = new Dictionary<string, ViewBase>();

        public ViewBase curView { get; private set; }

        // Viewを切り替え時次のViewが表示する前のDelay時間（Frame）
        private int _delayFrame = UIAnimationPublisher.defaultMaximumCutoffDurationInFrame;
        public int delayFrame
        {
            set { _delayFrame = value; }
            get { return _delayFrame; }
        }

        /// <summary>
        /// 今のScene
        /// </summary>
        public SceneBase curScene { get; set; } = null;

        /// <summary>次に行くViewの名前</summary>
        public string nextViewName { get; set; }

        /// <summary>前のViewの名前</summary>
        public string previousViewName { get; set; }

        private void Awake()
        {
            if (parentTransform == null)
            {
                GameObject obj = new GameObject("Views");
                parentTransform = obj.transform;
            }
        }

        /// <summary>
        /// 解放
        /// </summary>
        private void OnDestroy()
        {
            views.Clear();
        }

        /// <summary>
        /// Viewをロードしたかどうか
        /// </summary>
        public bool IsLoaded(string viewName)
        {
            return views.ContainsKey(viewName) && views[viewName] != null;
        }

        /// <summary>
        /// Viewをロードして、キャッシュに追加します。
        /// [Modified],taneda:2018.10.19:戻り値を追加
        /// <returns>ロードが発生した場合はtrue</returns>
        /// </summary>
        public bool LoadView(string viewName, Action<ViewBase> onLoaded)
        {
            if (!IsLoaded(viewName))
            {
                AssetManager.LoadAsync<GameObject>(viewName, (prefab) =>
                {
                    if (prefab == null)
                    {
                        return;
                    }
                    GameObject go = GameObject.Instantiate(prefab, parentTransform) as GameObject;
                    if (parentTransform != null)
                    {
                        go.SetActive(false);
                    }

                    ViewBase theView = go.GetComponent<ViewBase>();
                    if (theView != null)
                    {
                        views[viewName] = theView;
                    }

                    if (onLoaded != null)
                    {
                        onLoaded(theView);
                    }
                });
                return true;
            }
            else  // Find View from cache.
            {
                // start enter animation.
                if (onLoaded != null)
                {
                    onLoaded(views[viewName]);
                }
                return false;
            }
        }

        /// <summary>
        /// Sceneの中にViewを変わります。
        /// @param exitPattern: 今のViewの退出アニメーション
        /// @param enterPattern: 次のViewの入りアニメーション
        /// </summary>
        public void ChangeView(string viewName,
            ViewModelBase model,
            AnimationUICanvas.AnimationPattern exitPattern,
            AnimationUICanvas.AnimationPattern enterPattern,
            bool allowSameViewReload = true,
            bool delayEnterAnimation = true,
            bool deactivateLastView = true)
        {
            if (!allowSameViewReload && curView != null && curView.viewName == viewName)
            {
                return;
            }

            BaseCanvas.instance.CreateTouchGuard();
            ViewBase lastView = null;
            // start exit animation for current View.
            if (curView != null)
            {
                lastView = curView;
                lastView.OnViewBecomeInActive();
                lastView.StartExitAnimation(exitPattern);
            }

            // load new View.
            LoadView(viewName, (view) =>
            {
                curView = view;
                curView.viewName = viewName;

                // start enter animation.
                // Set delay time in animation if you want to delay.
                float delayTime = 0f;
                if (delayEnterAnimation)
                {
                    delayTime = (float)delayFrame / Common.Parameters.slowFrameRate;
                }
                StartCoroutine(WaitForStartingEnterAnimation(delayTime, enterPattern, exitPattern, lastView, view, model, deactivateLastView));
            });
        }

        /// <summary>
        /// Delay時間を終わったら次のベージのアニメーションを実行します。
        /// 前のページのExitAnimationと今のページのEnterAnimationが両方終わったらBaseCanvas.instance.RemoveTouchGuard()をします。
        /// </summary>
        IEnumerator WaitForStartingEnterAnimation(float delayTime, AnimationUICanvas.AnimationPattern enterPattern, AnimationUICanvas.AnimationPattern exitPattern,
            ViewBase lastView, ViewBase thisView, ViewModelBase model, bool deactivateLastView)
        {
            if (!Mathf.Approximately(delayTime, 0f))
            {
                yield return new WaitForSeconds(delayTime);
            }

            thisView.gameObject.SetActive(true);
            thisView.OnViewBecomeActive(model);

            if (!deactivateLastView)
            {
                yield return new WaitUntil(() => !(lastView.IsOnAnimation(exitPattern) || thisView.IsOnAnimation(enterPattern)));
            }

            // start enter animation.
            // Set delay time in animation if you want to delay.
            thisView.StartEnterAnimation(enterPattern, onAnimationDone: () =>
            {
                nextViewName = "";  // 次に行くViewの更新
            });

            if (thisView.removeTouchGuardOnActive)
            {
                BaseCanvas.instance.RemoveTouchGuard();
            }

            if (lastView != null)
            {
                if (deactivateLastView)
                {

                    yield return new WaitUntil(() => !(lastView.IsOnAnimation(exitPattern) || thisView.IsOnAnimation(enterPattern)));
                    lastView.gameObject.SetActive(false);
                }
            }
            else
            {
                yield return new WaitUntil(() => !thisView.IsOnAnimation(enterPattern));
            }

            thisView.OnViewEnterAnimationEnded();

            if (!thisView.removeTouchGuardOnActive)
            {
                BaseCanvas.instance.RemoveTouchGuard();
            }
        }

        /// <summary>
        /// 今のベージを削除します。
        /// </summary>
        public void RemoveCurView(bool hasAnimation, AnimationUICanvas.AnimationPattern exitPattern = AnimationUICanvas.AnimationPattern.Pattern_1)
        {
            if (curView == null)
            {
                return;
            }
            if (curView.gameObject.activeInHierarchy)
            {
                if (hasAnimation)
                {
                    curView.OnViewBecomeInActive();
                    curView.StartExitAnimation(exitPattern, () =>
                    {
                        Destroy(curView.gameObject);
                        curView = null;
                    });
                }
                else
                {
                    curView.OnViewBecomeInActive();
                    curView.gameObject.SetActive(false);
                    curView = null;
                }
            }
        }

        /// <summary>
        /// View取得
        /// </summary>
        /// <param name="ViewName"></param>
        /// <returns></returns>
        public ViewBase GetView(string viewName)
        {
            return views[viewName];
        }
    }
}
