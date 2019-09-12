using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// シーン遷移するためのマネージャー
    /// Unity 標準の ScneeManager と区別するために名前が SceneController にしてある
    /// </summary>
    public class SceneController : SingletonComponent<SceneController>
    {
        /// <summary> フェードインのタイプ   /// </summary>
        public enum FadeInType
        {
            ShowFadeIn,     // 通常のフェード 
            InstantFadeIn,  // 瞬時にフェードが明ける(フェードイン演出をしない)
            WaitStartFadeIn,// フェードイン開始要求が来るまで待つ（それまではフェードアウトしたまま） 
        };


        /// <summary>LoadScene を呼んだ時点で入れ替わるので注意</summary>
        public static string currentSceneName { get; set; }

        /// <summary>LoadScene を呼んだ時点で入れ替わるので注意</summary>
        public static string prevSceneName { get; set; }

        /// <summary>ライトマップなどの追加シーン</summary>
        private List<string> currentOptionalSceneNames = new List<string>();

        /// <summary>ライトマップなどの追加シーン</summary>
        private List<string> nextOptionalSceneNames = new List<string>();

        /// <summary>重複読み込みされている追加シーン</summary>
        private List<string> currentDuplicatedOptionalSceneNames = new List<string>();

        /// <summary>重複読み込みされている追加シーン</summary>
        private List<string> nextDuplicatedOptionalSceneNames = new List<string>();

        /// <summary>現在シーン遷移中か</summary>
        public bool isOnTransition { get; private set; }

        /// <summary>現在シーン遷移アニメーション中か</summary>
        public bool isOnAnimation = false;

        /// <summary>非同期ロード中</summary>
        public bool isOnAsyncLoad = false;

        /// <summary>非同期ロードで次に遷移しようとしてるシーンの名前</summary>
        public string preservedNextSceneName;

        public SceneBase currentSceneBase;
        public List<SceneBase> additionalSceneBases = new List<SceneBase>();

        /// <summary>
        /// 追加シーンのリスト
        /// </summary>
        private List<AddSceneStack> addSceneList = new List<AddSceneStack>();

        public Coroutine loadSceneAsyncCoroutine { get; private set; }

        public SceneModelBase model { get; private set; } = null;

        /// <summary>
        /// 追加シーン情報保持クラス
        /// </summary>
        public class AddSceneStack
        {
            public string prevSceneName;
            public Action loadSceneCallback;
            public SceneBase prevSceneBase;
            public bool entity; // 実体があるかどうか

            public AddSceneStack(string name, Action callback)
            {
                prevSceneName = name;
                loadSceneCallback = callback;
                entity = true;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            isOnTransition = false;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// シーン遷移する。基本はシーン追加、黒色フェードイン・アウト有り。
        /// </summary>
        /// <param name="needRequestEventQuest">EventQuestSceneに行く時、通信を発生しますか？基本的にtrueにする。EventQuestScene.OnGotEventData()でしかfalseにしない。</param>
        /// <returns></returns>
        public bool LoadScene(string nextSceneName, bool isAdditive = true, bool gotoSameScene = false, bool needRequestEventQuest = true, bool isForce = false)
        {
            if (isForce)
            { // 強制シーン遷移の場合はブロック処理に使われるフラグは落としておく
                isOnTransition = false;
                isOnAnimation = false;
                isOnAsyncLoad = false;
            }
            else
            { // 強制シーン遷移でない場合はブロック処理が入る
                if (isOnTransition) return false;
                if (isOnAnimation) return false;
                if (isOnAsyncLoad) return false;
            }

            // 同一シーンへの遷移はしない
            if (!gotoSameScene && currentSceneName == nextSceneName) return false;

            Action continueAction = () =>
            {
                if (nextSceneName == SceneNameDefine.MyPage && !isAdditive)
                {
                    BaseCanvas.showBlackCanvasOnStart = true;
                }

                AddloadSceneClear();

                DoLoadScene(nextSceneName, isAdditive);

            };

            if (nextSceneName == SceneNameDefine.EventQuest && needRequestEventQuest)
            {
                EventQuestScene.GotoEventQuestScene(continueAction);
            }
            else
            {
                continueAction();
            }

            return true;
        }

        /// <summary>
        /// シーン遷移時に経由するシーン名を取得
        /// </summary>
        private string GetThroughSceneName(string nextSceneName)
        {
            if (nextSceneName == SceneNameDefine.QuestResult || nextSceneName == SceneNameDefine.QuestFailedResult)
            {
                return "CapturedBg";
            }
            else
            {
                return "Blank";
            }
        }

        public bool LoadSceneAndReleaseResources(string nextSceneName)
        {
            // シーン遷移中なら遷移しない
            if (isOnTransition) return false;

            if (isOnAnimation) return false;

            if (isOnAsyncLoad) return false;

            // 同一シーンへの遷移はしない
            if (currentSceneName == nextSceneName) return false;

            if (ScreenshotCapture.hasInstance && nextSceneName == "InGame")
            {
                ScreenshotCapture.instance.DestroyImage();
            }

            Action continueAction = () =>
            {
                // どのシーンからきたかを保存
                prevSceneName = currentSceneName;
                currentSceneName = nextSceneName;

                currentOptionalSceneNames.Clear();

                if (nextSceneName == SceneNameDefine.MyPage || nextSceneName == SceneNameDefine.EventQuest)
                {
                    BaseCanvas.showBlackCanvasOnStart = true;
                }

                // 各シーンのOnBeforeLeavingを呼ぶ
                ClearOldScenes(showExitAnimation: false);

                var throughSceneName = GetThroughSceneName(nextSceneName);
                UnityEngine.SceneManagement.SceneManager.LoadScene(throughSceneName);
            };

            if (nextSceneName == SceneNameDefine.EventQuest)
            {
                EventQuestScene.GotoEventQuestScene(continueAction);
            }
            else
            {
                continueAction();
            }
            return true;
        }

        /// <summary>
        /// 非同期シーン読み込みをする。シーン追加のみサポート。フェードは行わない
        /// </summary>
        /// <param name="nextSceneName">行くシーンの名前</param>
        /// <param name="onLoadedCallback">完成callback</param>
        /// <param name="needRequestEventQuest">EventQuestSceneに行く時、通信を発生しますか？基本的にtrueにする。EventQuestScene.OnGotEventData()でしかfalseにしない。</param>
        /// <param name="isAddSceneClearInstantly">追加ロードしていたシーンを即座に破棄
        /// <returns></returns>
        public bool LoadSceneAsync(string nextSceneName, Action onLoadedCallback = null, bool needRequestEventQuest = true, bool isAddSceneClearInstantly = true, SceneModelBase model = null)
        {
            // シーン遷移中なら遷移しない
            if (isOnTransition) return false;

            if (isOnAnimation) return false;

            if (isOnAsyncLoad) return false;

            // 同一シーンへの遷移はしない
            if (currentSceneName == nextSceneName) return false;

            Action continueAction = () =>
            {
                isOnAsyncLoad = true;

                ClearOldScenes(isAddSceneClearInstantly, true);

                preservedNextSceneName = nextSceneName;

                // Modelを渡す
                if (model != null)
                {
                    model.SetSceneName(nextSceneName);
                }
                this.model = model;

                loadSceneAsyncCoroutine = StartCoroutine(LoadSceneAsyncCoroutine(nextSceneName, onLoadedCallback));
            };

            if (nextSceneName == SceneNameDefine.EventQuest && needRequestEventQuest)
            {
                EventQuestScene.GotoEventQuestScene(continueAction);
            }
            else
            {
                continueAction();
            }

            return true;
        }

        /// <summary>非同期シーン読み込みをして、指定されたコールバックを呼ぶ</summary>
        IEnumerator LoadSceneAsyncCoroutine(string nextSceneName, Action onLoadedCallback, SceneModelBase model = null)
        {
            yield return null;
            // fadeOutする時次のSceneを先にロードしない
            // fadeInの時にLoadするとFadeInと同時にLoadとUIAnimation再生できる
            if (!NowLoadingCanvas.IsInstanceEmpty)
            {
                yield return new WaitWhile(NowLoadingCanvas.instance.IsFadingOut);
            }

            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(nextSceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);

            GraphicManager.ResetSceneLightSetting();

            if (onLoadedCallback != null)
            {
                onLoadedCallback.Invoke();
            }

            loadSceneAsyncCoroutine = null;
        }

        /// <summary>
        /// シーンを切り替える前、今のシーンのStartExitAnimationとOnBeforeLeavingを呼びます。
        /// </summary>
        private void ClearOldScenes(bool isAddSceneClearInstantly = true, bool showExitAnimation = true)
        {
            if (currentSceneBase != null)
            {
                currentSceneBase.OnBeforeLeaving();
                if (showExitAnimation)
                {
                    isOnAnimation = true;
                    currentSceneBase.StartExitAnimation();
                }
            }

            if (additionalSceneBases != null)
            {
                for (int i = 0; i < additionalSceneBases.Count; i++)
                {
                    if (additionalSceneBases[i] != null)
                    {
                        additionalSceneBases[i].OnBeforeLeaving();
                    }
                }
                additionalSceneBases.Clear();
            }

            if (isAddSceneClearInstantly)
            {
                AddloadSceneClear();
            }
        }

        public AsyncOperation UnloadPrevScene()
        {
            if (!isOnAsyncLoad) return null;

            AsyncOperation unloadOperation = UnloadScene(currentSceneName);

            for (int i = 0; i < currentDuplicatedOptionalSceneNames.Count; i++)
            {
                UnloadDuplicatedOptionalScene(currentDuplicatedOptionalSceneNames[i]);
            }

            currentDuplicatedOptionalSceneNames.Clear();

            AddloadSceneClear();

            // 前シーンで Additive 読み込みしたシーンを Unload する
            for (int i = 0; i < currentOptionalSceneNames.Count; i++)
            {
                UnloadScene(currentOptionalSceneNames[i]);
            }

            // Unload したシーンを消去して
            currentOptionalSceneNames.Clear();
            currentDuplicatedOptionalSceneNames.Clear();

            // もし次のシーンで既に Additive で追加したものがあれば、それを現在の Additive で追加したシーンとする
            for (int i = 0; i < nextOptionalSceneNames.Count; i++)
            {
                currentOptionalSceneNames.Add(nextOptionalSceneNames[i]);
            }

            for (int i = 0; i < nextDuplicatedOptionalSceneNames.Count; i++)
            {
                currentDuplicatedOptionalSceneNames.Add(nextDuplicatedOptionalSceneNames[i]);
            }


            nextOptionalSceneNames.Clear();
            nextDuplicatedOptionalSceneNames.Clear();

            prevSceneName = currentSceneName;
            currentSceneName = preservedNextSceneName;
            preservedNextSceneName = "";

            if (!string.IsNullOrEmpty(prevSceneName))
            {
                // どのシーンからきたかを保存
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(currentSceneName);
                if (scene.IsValid() && scene.isLoaded)
                {
                    UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
                }
            }

            isOnTransition = false;
            isOnAsyncLoad = false;

            return unloadOperation;
        }

        private void DoLoadScene(string nextSceneName, bool isAdditive)
        {
            ClearOldScenes(showExitAnimation: false);

            if (isAdditive)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
                StartCoroutine(UnloadSceneFunction(currentSceneName));
                isOnTransition = true;
            }
            else
            {
                currentOptionalSceneNames.Clear();
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);

                if (nextSceneName == "MyPage")
                {
                    LoadBase();
                    LoadFooter();
                    LoadHeader();
                    OutGameAssetLoader.LoadResidentAssets();
                    MyPageBgmSyncSettings.PreCalcEventSetting();
                }
            }

            GraphicManager.ResetSceneLightSetting();

            // どのシーンからきたかを保存
            prevSceneName = currentSceneName;

            currentSceneName = nextSceneName;
        }

        /// <summary>シーン遷移した次のフレームで以前のシーンを Unload する</summary>
        IEnumerator UnloadSceneFunction(string sceneName)
        {
            yield return null;

            UnloadScene(sceneName);

            for (int i = 0; i < currentDuplicatedOptionalSceneNames.Count; i++)
            {
                UnloadDuplicatedOptionalScene(currentDuplicatedOptionalSceneNames[i]);
            }

            currentDuplicatedOptionalSceneNames.Clear();

            // 前シーンで Additive 読み込みしたシーンを Unload する
            for (int i = 0; i < currentOptionalSceneNames.Count; i++)
            {
                UnloadScene(currentOptionalSceneNames[i]);
            }

            // Unload したシーンを消去して
            currentOptionalSceneNames.Clear();

            // もし次のシーンで既に Additive で追加したものがあれば、それを現在の Additive で追加したシーンとする
            for (int i = 0; i < nextOptionalSceneNames.Count; i++)
            {
                currentOptionalSceneNames.Add(nextOptionalSceneNames[i]);
            }
            nextOptionalSceneNames.Clear();

            for (int i = 0; i < nextDuplicatedOptionalSceneNames.Count; i++)
            {
                currentDuplicatedOptionalSceneNames.Add(nextDuplicatedOptionalSceneNames[i]);
            }
            nextDuplicatedOptionalSceneNames.Clear();

            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(currentSceneName);
            if (scene.IsValid() && scene.isLoaded)
            {
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            }

            isOnTransition = false;
        }

        /// <summary>Active シーンを本来のものに戻す</summary>
        public void ResetActiveScene()
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(currentSceneName);
            if (scene.IsValid() && scene.isLoaded)
            {
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            }
        }

        /// <summary>ライトマップなどの追加シーンを読み込む。ただし、非同期の LoadOptionalSceneAsyncCoroutine のほうを推奨。</summary>
        public void LoadOptionalScene(string sceneName, System.Action onOptionSceneLoaded = null, bool autoBlockTouchWhileLoading = false)
        {
            if (isOnTransition || isOnAsyncLoad)
            {
                if (nextOptionalSceneNames.Contains(sceneName))
                {
                    if (!nextDuplicatedOptionalSceneNames.Contains(sceneName))
                    {
                        nextDuplicatedOptionalSceneNames.Add(sceneName);
                    }
                }

                // 既にシーン遷移中なら、直後に Unload されないように別のリストに入れておく
                nextOptionalSceneNames.Add(sceneName);
            }
            else
            {
                if (currentOptionalSceneNames.Contains(sceneName))
                {
                    if (!currentDuplicatedOptionalSceneNames.Contains(sceneName))
                    {
                        currentDuplicatedOptionalSceneNames.Add(sceneName);
                    }
                }

                currentOptionalSceneNames.Add(sceneName);
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
            StartCoroutine(CorrectActiveScene());

            if (onOptionSceneLoaded != null || autoBlockTouchWhileLoading)
            {
                StartCoroutine(WaitForOptionSceneLoaded(sceneName, onOptionSceneLoaded, autoBlockTouchWhileLoading));
            }
        }

        /// <summary>非同期でライトマップなどの追加シーンを読み込む</summary>
        public Coroutine StartLoadOptionalSceneAsyncCoroutine(string sceneName, System.Action onOptionSceneLoaded = null, bool autoBlockTouchWhileLoading = false)
        {
            return StartCoroutine(LoadOptionalSceneAsyncCoroutine(sceneName, onOptionSceneLoaded, autoBlockTouchWhileLoading));
        }

        /// <summary>非同期でライトマップなどの追加シーンを読み込む</summary>
        public IEnumerator LoadOptionalSceneAsyncCoroutine(string sceneName, System.Action onOptionSceneLoaded = null, bool autoBlockTouchWhileLoading = false)
        {
            if (isOnTransition || isOnAsyncLoad)
            {
                if (nextOptionalSceneNames.Contains(sceneName))
                {
                    if (!nextDuplicatedOptionalSceneNames.Contains(sceneName))
                    {
                        nextDuplicatedOptionalSceneNames.Add(sceneName);
                    }
                }

                // 既にシーン遷移中なら、直後に Unload されないように別のリストに入れておく
                nextOptionalSceneNames.Add(sceneName);
            }
            else
            {
                if (currentOptionalSceneNames.Contains(sceneName))
                {
                    if (!currentDuplicatedOptionalSceneNames.Contains(sceneName))
                    {
                        currentDuplicatedOptionalSceneNames.Add(sceneName);
                    }
                }

                currentOptionalSceneNames.Add(sceneName);
            }

            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);

            yield return CorrectActiveScene();

            if (onOptionSceneLoaded != null || autoBlockTouchWhileLoading)
            {
                yield return WaitForOptionSceneLoaded(sceneName, onOptionSceneLoaded, autoBlockTouchWhileLoading);
            }
        }

        IEnumerator WaitForOptionSceneLoaded(string sceneName, System.Action onOnptionSceneLoaded, bool autoBlockTouchWhileLoading)
        {
            if (autoBlockTouchWhileLoading)
            {
                currentSceneBase.CreateTouchGuard();
            }

            while (true)
            {
                UnityEngine.SceneManagement.Scene loadingScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
                if (loadingScene.IsValid() && loadingScene.isLoaded) break;

                yield return null;
            }

            if (autoBlockTouchWhileLoading)
            {
                currentSceneBase.RemoveTouchGuard();
            }

            if (onOnptionSceneLoaded != null)
            {
                onOnptionSceneLoaded.Invoke();
            }
        }

        /// <summary>追加シーン読み込みした後に Active シーンを元に戻す</summary>
        IEnumerator CorrectActiveScene()
        {
            yield return null;

            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(currentSceneName);
            if (scene.IsValid() && scene.isLoaded)
            {
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            }

            GraphicManager.ResetSceneLightSetting();
        }

        /// <summary>特定の追加シーンを含んでいるか</summary>
        public bool HasOptionalScene(string sceneName)
        {
            return currentOptionalSceneNames.Contains(sceneName);
        }

        /// <summary>追加シーンを Unload する</summary>
        public AsyncOperation UnloadOptionalScene(string sceneName)
        {
            var asyncOp = UnloadScene(sceneName);
            if (currentOptionalSceneNames.Contains(sceneName))
            {
                if (currentDuplicatedOptionalSceneNames.Contains(sceneName))
                {
                    currentDuplicatedOptionalSceneNames.Remove(sceneName);
                }
            }
            currentOptionalSceneNames.Remove(sceneName);

            return asyncOp;
        }

        /// <summary>追加シーンを Unload する</summary>
        public AsyncOperation UnloadOptionalScene(UnityEngine.SceneManagement.Scene scene)
        {
            if (currentOptionalSceneNames.Contains(scene.name))
            {
                if (currentDuplicatedOptionalSceneNames.Contains(scene.name))
                {
                    currentDuplicatedOptionalSceneNames.Remove(scene.name);
                }
            }
            currentOptionalSceneNames.Remove(scene.name);

            return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
        }

        /// <summary>追加シーンを全て削除する</summary>
        public IEnumerator UnloadAllOptionalScenesWaitCoroutine()
        {
            int count = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (int i = 0; i < count; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                currentOptionalSceneNames.Contains(scene.name);

                if (scene.IsValid())
                {
                    yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
                }
            }
        }

        /// <summary>同名の複数の追加シーンを全て削除する</summary>
        public void UnloadDuplicatedOptionalScene(string sceneName)
        {
            currentOptionalSceneNames.RemoveAll(str => str == sceneName);

            int count = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (int i = 0; i < count; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.name == sceneName)
                {
                    if (scene.IsValid())
                    {
                        UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
                    }
                }
            }
        }

        /// <summary>
        /// 追加シーン呼び出し関数
        /// </summary>
        /// <param name="addSceneName"></param>
        /// <param name="callback"></param>
        /// <param name="lastSceneChange">最後のシーンと入れ替えで追加したい場合trueに</param>
        public void AddLoadScene(string addSceneName, Action callback = null, bool lastSceneChange = false)
        {
            StartCoroutine(AddLoadSceneCoroutine(addSceneName, callback, lastSceneChange));
        }

        /// <summary>
        /// 生成(場合によっては削除してから生成)
        /// </summary>
        /// <param name="addSceneName"></param>
        /// <param name="callback"></param>
        /// <param name="lastSceneChange"></param>
        /// <returns></returns>
        IEnumerator AddLoadSceneCoroutine(string addSceneName, Action callback, bool lastSceneChange)
        {
            // 最初だったら最初のシーンをリストに入れる
            if (addSceneList.Count == 0)
            {
                AddSceneStack currentScene = new AddSceneStack(currentSceneName, null);
                currentScene.prevSceneBase = currentSceneBase;
                addSceneList.Add(currentScene);
            }

            //　ラストを交換したい場合はエンド関連を呼んでから削除
            if (lastSceneChange)
            {
                isOnAnimation = true;
                currentSceneBase.StartExitAnimation();
                currentSceneBase.OnBeforeLeaving();
                yield return new WaitUntil(() => !isOnAnimation);
                UnloadScene(addSceneList[addSceneList.Count - 1].prevSceneName);
                // 実体は削除されたフラグoff
                addSceneList[addSceneList.Count - 1].entity = false;
            }

            // シーンの追加
            AddSceneStack loadScene = new AddSceneStack(addSceneName, callback);
            addSceneList.Add(loadScene);
            UnityEngine.SceneManagement.SceneManager.LoadScene(addSceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
            // 生成されてStart()がよばれたときにcurrentSceneBaseが切り替わるので一旦待つ
            yield return null;
            loadScene.prevSceneBase = currentSceneBase;
            currentSceneName = addSceneName;
            StartCoroutine(CorrectActiveScene());
        }

        /// <summary>
        /// シーン削除
        /// </summary>
        public void UnloadAddLoadScene()
        {
            StartCoroutine(UnloadAddLoadSceneCoroutine());
        }

        /// <summary>
        /// シーン削除のコルーチン処理
        /// </summary>
        /// <returns></returns>
        IEnumerator UnloadAddLoadSceneCoroutine()
        {

            if (currentSceneBase != null)
            {
                isOnAnimation = true;
                currentSceneBase.StartExitAnimation();
                currentSceneBase.OnBeforeLeaving();
            }

            // 画面遷移アニメーション中は待つ
            yield return new WaitUntil(() => !isOnAnimation);

            AddSceneStack loadScene = addSceneList[addSceneList.Count - 1];
            UnloadScene(loadScene.prevSceneName);
            addSceneList.Remove(loadScene);

            if (addSceneList.Count >= 2 && !addSceneList[addSceneList.Count - 1].entity)
            {
                // 前回シーンを読み込む
                AddSceneStack prevloadScene = addSceneList[addSceneList.Count - 1];
                UnityEngine.SceneManagement.SceneManager.LoadScene(prevloadScene.prevSceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
                currentSceneName = addSceneList[addSceneList.Count - 1].prevSceneName;
                currentSceneBase = addSceneList[addSceneList.Count - 1].prevSceneBase;

                if (prevloadScene.loadSceneCallback != null)
                {
                    prevloadScene.loadSceneCallback();
                }

                StartCoroutine(CorrectActiveScene());
            }
            else
            {
                // 一個前をアクティブにする
                AddSceneStack prevloadScene = addSceneList[addSceneList.Count - 1];
                currentSceneName = addSceneList[addSceneList.Count - 1].prevSceneName;
                currentSceneBase = addSceneList[addSceneList.Count - 1].prevSceneBase;

                if (prevloadScene.loadSceneCallback != null)
                {
                    prevloadScene.loadSceneCallback();
                }

                StartCoroutine(CorrectActiveScene());
            }
        }

        /// <summary>
        /// 追加シーンリストをクリアする ただし自分のScene自体は消さない
        /// </summary>
        public void AddloadSceneClear()
        {
            for (int i = 0, count = addSceneList.Count; i < count; ++i)
            {
                if (i == count - 1)
                {
                    break;
                }
                addSceneList[i].prevSceneBase.OnBeforeLeaving();
                UnloadScene(addSceneList[i].prevSceneName);
            }
            addSceneList.Clear();
            additionalSceneBases.Clear();
        }


        /// <summary>
        /// 一個前のシーン名を返す
        /// </summary>
        /// <returns></returns>
        public string PrevAddScene()
        {
            string str = "";

            if (addSceneList.Count >= 2)
            {
                AddSceneStack tempStack = addSceneList[addSceneList.Count - 2];
                str = tempStack.prevSceneName;
            }

            return str;
        }

        /// <summary>
        /// 指定のシーンが追加シーンとして登録されているか判定
        /// </summary>
        public bool IsAddScene(string sceneName)
        {
            for (int i = 0, count = addSceneList.Count; i < count; ++i)
            {
                if (addSceneList[i].prevSceneName == sceneName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 指定のシーンがOptionalシーンとして登録されているか判定
        /// </summary>
        public bool IsOptionalScene(string sceneName)
        {
            for (int i = 0, count = currentOptionalSceneNames.Count; i < count; ++i)
            {
                if (currentOptionalSceneNames[i] == sceneName)
                {
                    return true;
                }
            }

            return false;
        }

        public void SetActiveBg(Footer.MenuTab menuTab)
        {

        }

        public bool IsTransitionReady()
        {
            if (isOnTransition) return false;
            if (isOnAnimation) return false;

            return true;
        }

        // 以下の関数は SceneController オブジェクト自体が生成される前に呼ぶ必要があるので static 関数になっている

        // 各シーンの大元に存在するシーン
        static public void LoadBase()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Base", UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }

        static public void UnloadBase()
        {
            UnloadScene("Base");
        }

        static public void LoadOutGameBase()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("OutGameBase", UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }

        static AsyncOperation UnloadOutGameBase()
        {
            return UnloadScene("BaseOutGameBase");
        }

        // ヘッダー
        static public void LoadHeader()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Header", UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }

        static AsyncOperation UnloadHeader()
        {
            return UnloadScene("Header");
        }

        // フッター
        static public void LoadFooter()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Footer", UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }

        static AsyncOperation UnloadFooter()
        {
            return UnloadScene("Footer");
        }

        public static AsyncOperation UnloadScene(string scene)
        {
            if (UnityEngine.SceneManagement.SceneManager.GetSceneByName(scene).IsValid())
            {
                return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
            }

            return null;
        }

        /// <summary>
        /// 現在のシーンでの日付更新確認
        /// </summary>
        public bool CheckDateChangeOnCurrentScene(bool withCallEvent = false)
        {
            if (currentSceneBase != null)
            {
                return currentSceneBase.CheckDateChange(withCallEvent);
            }
            return false;
        }

        /// <summary>アウトゲームからインゲームに行く時に全ての情報をクリアする</summary>
        public void ClearAllSceneInfo()
        {
            currentSceneBase = null;
            currentOptionalSceneNames.Clear();
            nextOptionalSceneNames.Clear();
            addSceneList.Clear();
            additionalSceneBases.Clear();
            addSceneList.Clear();
        }
    }
}
