using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HotFix_Project
{
    /// <summary>DOTween の便利なユーティリティ</summary>
    public static class TweenUtil
    {
        private static bool isInitialize = false;

        private static List<Tweener> inGameTweenerList = new List<Tweener>();

        public static void Init(bool isInGame)
        {
            const int tweenersCapacity = 1024;
            const int sequencesCapacity = 256;
            const int instancedTweeners = 32;

            // InGame は再利用するが OutGame は再利用しない
            bool recycle = isInGame;

            if (!isInitialize)
            {
                DOTween.Init(recycle);
                DOTween.defaultEaseType = Ease.Linear;
                DOTween.SetTweensCapacity(tweenersCapacity, sequencesCapacity);
                isInitialize = true;
            }

            SetDefaultTimeScaleIndependent(isInGame);
            DOTween.defaultRecyclable = recycle;

            if (recycle)
            {
                // recycle可の場合はバッファを確保しておく
                for (int i = 0; i < instancedTweeners; i++)
                {
                    DOTween.To(() => 0.0f, x => { }, 0.0f, 0.01f);
                }
            }

#if UNITY_EDITOR
            EditorApplication.pauseStateChanged += OnPauseStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }



#if UNITY_EDITOR
        /// <summary>
        /// 再生時と一時停止時にDOTweenの再生状況を制御する
        /// ※コマ送りには非対応
        /// </summary>
        private static void OnPlayCrrlDOTween()
        {
            if (EditorApplication.isPaused && EditorApplication.isPlaying)
            {
                DOTween.PauseAll();
            }
            else if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
            {
                DOTween.RestartAll();
            }
            else if (EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying)
            {
                DOTween.RestartAll();
            }
            else if (!EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying)
            {
                DOTween.PauseAll();
            }
            else if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
            {
                //  終了時は自分を削除
                EditorApplication.pauseStateChanged -= OnPauseStateChanged;
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            }
        }

        private static void OnPauseStateChanged(PauseState obj)
        {
            OnPlayCrrlDOTween();
        }


        private static void OnPlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            OnPlayCrrlDOTween();
        }
#endif

        /// <summary>
        /// InGame/OutGameの出入り口のみ使います
        /// </summary>
        public static void SetDefaultTimeScaleIndependent(bool timeIndependent)
        {
            DOTween.defaultTimeScaleIndependent = timeIndependent;
        }

        /// <summary>
        /// iTween.ValueTo と同様のことを行う。
        /// OnComplete が必要なら戻り値に対して OnComplete を呼ぶこと
        /// 
        /// (使い方）
        ///   TweenUtil.ValueTo(0.0f, 3.0f, 1.0f, (x) => { Debug.Log(x); });
        ///   TweenUtil.ValueTo(0.0f, 3.0f, 1.0f, (x) => { Debug.Log("value:" + x); }).OnComplete(() => { Debug.Log("OnComplete"); });
        /// 
        /// </summary>
        public static Tweener ValueTo(float from, float to, float duration, System.Action<float> callback = null)
        {
            return DOTween.To(
                () => from,      // settter
                (v) =>
                {
                    if (callback != null) callback(v);
                }, // getter
                to,
                duration
            );
        }

        public static Tweener ValueTo(Vector4 from, Vector4 to, float duration, System.Action<Vector4> callback = null)
        {
            return DOTween.To(
                () => from,      // settter
                (v) =>
                {
                    if (callback != null) callback(v);
                }, // getter
                to,
                duration
            );
        }

        /// <summary>遅延付き ValueTo。戻り値は Sequence</summary>
        public static Sequence ValueTo(float from, float to, float duration, float delay, System.Action<float> callback = null)
        {
            Sequence sequence = DOTween.Sequence();
            if (delay > 0)
            {
                sequence.Append(Wait(delay));
            }

            sequence.Append(ValueTo(from, to, duration, callback));

            return sequence;
        }

        /// <summary>CanvasGroup のアルファを変更する</summary>
        public static Tweener AlphaTo(CanvasGroup canvasgroup, float from, float to, float duration)
        {
            return ValueTo(from, to, duration, (a) =>
            {
                canvasgroup.alpha = a;
            });
        }

        /// <summary>遅延付きで CanvasGroup のアルファを変更する</summary>
        public static Sequence AlphaTo(CanvasGroup canvasgroup, float from, float to, float duration, float delay)
        {
            Sequence sequence = DOTween.Sequence();

            if (delay > 0)
            {
                sequence.Append(Wait(delay));
            }

            sequence.Append(ValueTo(from, to, duration, (a) =>
            {
                canvasgroup.alpha = a;
            }));

            return sequence;
        }

        /// <summary>指定秒数待つだけ。OnComplete や Sequence と組み合わせたりする。</summary>
        public static Tweener Wait(float duration, System.Action<float> callback = null)
        {
            return ValueTo(0.0f, 1.0f, duration, callback);
        }

        /// <summary>
        /// ColorOffsetを設定するもの
        /// </summary>
        /// <param name="image"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="duration"></param>
        /// <param name="useCurrentFromValue"></param>
        /// <param name="isAtlas"></param>
        /// <returns></returns>
        public static Tweener ColorModulusTo(Image image, float from, float to, float duration, bool useCurrentFromValue, bool isAtlas)
        {
            if (useCurrentFromValue)
            {
                from = CommonUtil.GetColorModulus(image.material);
            }

            return ValueTo(from, to, duration, (ev) =>
            {
                CommonUtil.SetColorModulusForAtlasMaterial(image, ev, isAtlas);
            });
        }

        /// <summary>
        /// ColorOffsetをSpriterRenderer + MaterialPropertyBlockを使用して設定する
        /// </summary>
        #region
        public struct ColorModulusArgs
        {
            public SpriteRenderer image;
            public MaterialPropertyBlock propertyBlock;
            public int propertyId;
        }
        public static Tweener DoExposure(ColorModulusArgs args, float fromEvBy255, float toEvBy255, float duration, bool isAtlas)
        {
            return ColorModulusTo(args, fromEvBy255 / 255.0f, toEvBy255 / 255.0f, duration, isAtlas);
        }
        public static Tweener ColorModulusTo(ColorModulusArgs args, float from, float to, float duration, bool isAtlas)
        {
            return ValueTo(from, to, duration, (ev) =>
            {
                SetColorModulus(args, ev);
            });
        }
        public static void SetColorModulus(ColorModulusArgs args, float ev)
        {
            args.propertyBlock.SetVector(args.propertyId, new Vector4(ev, ev, ev, 1f));
            args.image.SetPropertyBlock(args.propertyBlock);
        }

        public static Tweener DoExposure(ColorModulusArgs args, Vector4 fromEvBy255, Vector4 toEvBy255, float duration)
        {
            return ColorModulusTo(args, fromEvBy255, toEvBy255, duration);
        }
        public static Tweener ColorModulusTo(ColorModulusArgs args, Vector4 from, Vector4 to, float duration)
        {
            return ValueTo(from, to, duration, (ev) =>
            {
                SetColorModulus(args, ev);
            });
        }
        public static void SetColorModulus(ColorModulusArgs args, Vector4 ev)
        {
            args.propertyBlock.SetVector(args.propertyId, ev);
            args.image.SetPropertyBlock(args.propertyBlock);
        }
        #endregion

        /// <summary>
        /// テキストのカウント
        /// </summary>
        /// <param name="text"></param>
        /// <param name="initialValue">最初の値</param>
        /// <param name="finalValue">最後の値</param>
        /// <param name="duration">秒数</param>
        /// <param name="digits">桁数</param>
        /// <returns></returns>
        public static Tweener DOTextInt(Text text, int initialValue, int finalValue, float duration, int digits)
        {
            return DOTween.To(
                 () => initialValue,
                 it => text.text = GetFormatText(it, digits),
                 finalValue,
                 duration
             ).SetEase(Ease.OutCubic);
        }


        /// <summary>
        /// ０埋めの文字列を返す
        /// </summary>
        /// <param name="it"></param>
        /// <param name="digits"></param>
        /// <returns></returns>
        public static string GetFormatText(int it, int digits)
        {
            string str = "";
            int valueDigit = 1;
            int zeroDigit = 0;
            for (int i = it; i >= 10; i /= 10)
            {
                valueDigit++;
            }

            zeroDigit = digits - valueDigit;

            for (int i = 0; i < zeroDigit; ++i)
            {
                str += "0";
            }

            str += it.ToString();// Localize.ValueToString(it);

            return str;
        }

        /// <summary>InGame用Tweenerをまとめて破棄します</summary>
        public static void ReleaseInGameTweeners()
        {
            foreach (var t in inGameTweenerList)
            {
                t.Kill();
            }
            inGameTweenerList.Clear();
        }

        /// <summary>
        /// TweenerにInGame用の設定をします
        /// ・自動でKillされない
        /// ・一時停止状態で開始
        /// ・InGame終了時にまとめてKill
        /// </summary>
        public static Tweener ForInGame(this Tweener tweener)
        {
            tweener.SetAutoKill(false).Pause();
            inGameTweenerList.Add(tweener);

            return tweener;
        }
    }
}
