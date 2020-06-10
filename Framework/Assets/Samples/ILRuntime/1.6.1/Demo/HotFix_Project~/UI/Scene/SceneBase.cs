using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace HotFix_Project
{
    /// <summary>
    /// アウトゲーム共通のシーンクラスのベース
    /// 各シーンはこのクラスを継承したクラス（例: FooScene）を持っておく
    /// </summary>
    public class SceneBase : MonoBehaviour
    {
        /// <summary>ヘッダーが存在するシーンか</summary>
        [SerializeField]
        public bool hasHeader;

        /// <summary>フッターが存在するシーンか</summary>
        [SerializeField]
        public bool hasFooter;

        /// <summary>日付変更チェック無効フラグ(遷移してきた時)</summary>
        [SerializeField]
        public bool isDateChangeCheckDisableOnTransition = false;
        /// <summary>日付変更チェック無効フラグ(バックグラウンド再開時)</summary>
        [SerializeField]
        public bool isDateChangeCheckDisableOnResume = false;

        [SerializeField]
        public bool hasExitAnimation = false;

        [SerializeField]
        public bool multiTouchEnabled = false;

        [SerializeField, Header("AnimationUIの退出フレーム数")]
        public int sceneExitFrameCount = 24;

        /// <summary>起動直後の最初のシーンか</summary>
        public static bool isInitialSceneForDevelopment = true;

        private int defaultCanvasPlaneDistance = 100;

        [HideInInspector]
        public GameObject backButtonObject;

        private int popupCount;
        protected bool isPopupOpened = false;

        /// <summary>PageController 生成が重いので必要なシーンだけ true にする</summary>
        public bool isPageControlNeeded = false;

        /// <summary>日付更新時処理が呼ばれたか</summary>
        public bool isCalledDateChangeEvent = false;
        /// <summary>メンテナンス時処理が呼ばれたか</summary>
        public bool isCalledMaihtenanceEvent = false;

        private bool isOptionalScene;

        protected bool isPlayTutorial = false;

        /// <summary> WebViewを閉じるためのコールバック </summary>
        public UnityAction webViewCloseCallback = null;

        private static bool _isPromotedItemOnPending = false;

        // 2フレ連続OKの時のみ次に進む
        private const int constPromotedItemOKStatusWaitCount = 2;
        private static int promotedItemOKStatusCount = constPromotedItemOKStatusWaitCount;

        /// <summary>各シーンのこのクラスを継承したオブジェクトの Start() から呼ばれる</summary>
        protected void StartSceneBase(bool isAddedScene = false, bool isInitGraphicParam = false)
        {
            this.isOptionalScene = isAddedScene;


            // 遷移完了
            StartCoroutine(WaitPrevSceneExit());

            // モデル渡し
            //if (SceneController.instance.model != null && SceneController.instance.model.sceneName == SceneController.instance.preservedNextSceneName)
            //{
            //    return SceneController.instance.model;
            //}
            return;
        }

        /// <summary>前のシーンが終わるまで待ってから</summary>
        private IEnumerator WaitPrevSceneExit()
        {
            //yield return new WaitUntil(() => SceneController.instance.IsTransitionReady());
            yield return new WaitWhile(() => BaseCanvas.instance == null);

            if (hasHeader)
            {
                //yield return new WaitWhile(() => HeaderCanvas.IsInstanceEmpty());
                //HeaderCanvas.instance.ResetPresentBg();
                //HeaderCanvas.instance.header.UpdateMissionMessage();
            }

            yield return new WaitUntil(() => !IsTouchGuard());

            if (!isOptionalScene)
            {
                // 日付更新確認
                if (!isDateChangeCheckDisableOnTransition && CheckDateChange(true)) yield return null;

                // メンテナンス確認
                //if (CheckMaintenance(true)) yield return null;
            }

#if GLUON_DEBUG
            // ストーリーデバッグ用
            if (DebugStoryWindow.isOpenDebugStoryWindow)
            {
                yield return new WaitUntil(() => DebugViewController.Instance != null);
                DebugViewController.Instance.OpenDebugStoryWindow();
                DebugStoryWindow.isOpenDebugStoryWindow = false;
            }
#endif
        }

        /// <summary>一定時間タッチを無効にする。負の数が指定されればずっと無効</summary>
        public void CreateTouchGuard(float duration = -1.0f)
        {
            StartCoroutine(TouchGuardWaitCoroutine(duration));
        }

        IEnumerator TouchGuardWaitCoroutine(float duration)
        {
            yield return new WaitWhile(() => BaseCanvas.instance == null);
            BaseCanvas.instance.CreateTouchGuard(duration);
        }

        /// <summary>全てのタッチガードを削除してタッチを有効にする。特殊用途。</summary>
        public void RemoveAllTouchGuard()
        {
            BaseCanvas.instance.RemoveAllTouchGuard();
        }

        /// <summary>無制限時間タッチガードを削除する</summary>
        public void RemoveTouchGuard()
        {
            BaseCanvas.instance.RemoveTouchGuard();
        }

        IEnumerator RemoveTouchGuardWaitCoroutine()
        {
            yield return new WaitWhile(() => BaseCanvas.instance == null);
            BaseCanvas.instance.RemoveTouchGuard();
        }

        /// <summary>
        /// 識別子付きタッチガードを生成する
        /// </summary>
        public TouchGuardObject CreateTouchGuardObject(string label, float duration = -1.0f)
        {
            return BaseCanvas.instance.CreateTouchGuardObject(label, duration);
        }

        /// <summary>
        /// 識別子付きタッチガードを削除する
        /// </summary>
        public void RemoveTouchGuardObject(TouchGuardObject touchGuardObject)
        {
            BaseCanvas.instance.RemoveTouchGuardObject(touchGuardObject);
        }

        /// <summary>
        /// タッチガードが効いてる状態か
        /// </summary>
        /// <returns></returns>
        public bool IsTouchGuard()
        {
            return BaseCanvas.instance.IsTouchGurdEnable();
        }


        /// <summary>シーン終了アニメーションを開始する</summary>
        public virtual void StartExitAnimation()
        {
            CreateTouchGuard();

            AnimationUICanvasManager.instance.StartExitAnimation(true);
            StartCoroutine(ExitAnimationCoroutine());
        }

        private IEnumerator ExitAnimationCoroutine()
        {
            yield return new WaitForSeconds(sceneExitFrameCount / 60.0f);
            OnExitAnimationCompleted();
        }

        /// <summary>シーン終了時アニメーションが完了した際に呼ぶ</summary>
        protected virtual void OnExitAnimationCompleted()
        {
            RemoveTouchGuard();
            //SceneController.instance.isOnAnimation = false;
        }

        /// <summary>
        /// このシーンから退出するとき呼ぶ。
        /// ExitAnimationがあれば、ExitAnimationの前で呼ばれます。
        /// </summary>
        public virtual void OnBeforeLeaving()
        {
            // 全バックキーイベントをリセット
            //BackKeyManager.instance.ClearBackKeyEvent();

            //if (!QuestSelectResourceManager.IsInstanceEmpty())
            //{
            //    QuestSelectResourceManager.instance.ReleaseResources();
            //}
            //if (curPage != null)
            //{
            //    curPage.OnPageBecomeInActive();
            //}

            //CustomMessageManager.instance.PostMessage(CustomMessageType.RemoveCharaVoiceGroup, null);
        }

        /// <summary>
        /// プレゼントを受け取った後の情報更新処理を記述する
        /// </summary>
        public virtual void OnPresentReceived()
        {
        }

        /// <summary>
        /// アイテムを使った後の情報更新処理を記述する
        /// </summary>
        public virtual void OnItemUsed()
        {
        }

        /// <summary>
        /// 日付更新確認
        /// </summary>
        public bool CheckDateChange(bool withCallEvent = false)
        {
            // チュートリアル処理が必要なら日付チェックをしない
            //if (SkipCheckDateChangeForTutorial()) return false;
            bool isDateChanged = false;// CommonUtil.IsDateChanged();

            if (isDateChanged && withCallEvent)
            {
                // 日付更新時処理
                OnDateChanged();
            }
            return isDateChanged;
        }

        /// <summary>
        /// 日付更新時処理
        /// </summary>
        protected virtual void OnDateChanged()
        {
            // 日付更新エラー(日付更新による通信エラー時の処理を行う)
            //HttpHelper.OnError(ErrorType.ResultCode, (int)SystemMessage.COMMON_CHANGE_DATE);
        }

        /// <summary>
        /// バックグラウンド移行・復帰時
        /// </summary>
        void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus && !isDateChangeCheckDisableOnResume)
            {
                // 復帰時に日付更新確認
                CheckDateChange(true);
            }
        }

#if GLUON_DEBUG
        /// <summary>
        /// メモリプロファイルの出力
        /// </summary>
#if UNITY_EDITOR
        [MenuItem("Gluon/System/Output Memory Profier to Console")]
#endif
        public static void OutputMemoryProfiler()
        {
            EffectManager.OutputSparkMemoryProfiler();
            SoundManager.OutputSoundMemoryProfiler();
        }
#endif
    }
}
