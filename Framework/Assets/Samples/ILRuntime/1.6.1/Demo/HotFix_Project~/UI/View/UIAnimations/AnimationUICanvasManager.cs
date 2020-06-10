using System.Collections.Generic;

namespace HotFix_Project
{
    /// <summary>
    /// 複数AnimationUICanvasがある場合自動管理するもの
    /// SceneのAsycLoad中でも存在するのでSingletonMonobehaviourになれない
    /// </summary>
    public class AnimationUICanvasManager : SingletonComponent<AnimationUICanvasManager>
    {
        private List<AnimationUICanvas> animationUICanvases = new List<AnimationUICanvas>();

        public void Register(AnimationUICanvas item)
        {
            if (!animationUICanvases.Contains(item))
            {
                animationUICanvases.Add(item);
            }
        }

        public void UnRegister(AnimationUICanvas item)
        {
            animationUICanvases.Remove(item);
        }

        /// <summary>
        /// シーン遷移したら削除すべき
        /// </summary>
        public void Clear()
        {
            animationUICanvases.Clear();
        }

        /// <summary>
        /// EnterAnimation
        /// あまり使わないかな
        /// </summary>
        public void StartEnterAnimation()
        {
            for (int i = 0; i < animationUICanvases.Count; i++)
            {
                if (animationUICanvases[i] != null)
                {
                    animationUICanvases[i].StartEnterAnimation();
                }
            }
        }

        /// <summary>
        /// Scene退出時これを実行する、大体Clearしたほうがいい
        /// </summary>
        /// <param name="bClear"></param>
        public void StartExitAnimation(bool bClear = true)
        {
            for (int i = 0; i < animationUICanvases.Count; i++)
            {
                if (animationUICanvases[i] != null)
                {
                    animationUICanvases[i].StartExitAnimation();
                }
            }
            if (bClear)
            {
                Clear();
            }
        }

        /// <summary>
        /// 登録されているかチェック
        /// </summary>
        public bool CheckRegisteredCanvas(AnimationUICanvas item)
        {
            return animationUICanvases.Contains(item);
        }
    }
}
