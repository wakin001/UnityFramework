using System;

namespace Framework
{
    /// <summary>
    /// AnimationがあるGameObject(UI用)
    /// </summary>
    public class AnimationUIObject : AnimationUICanvas
    {
        /// <summary>
        /// Enter animationを実行します。
        /// </summary>
        public override void StartEnterAnimation(AnimationPattern pattern = AnimationPattern.Pattern_1, Action onAnimationDone = null, System.Action onCutOff = null)
        {
            ForceResetPlayedState(false, false, pattern);
            base.StartEnterAnimation(pattern, onAnimationDone, onCutOff);
        }

        /// <summary>
        /// Exit Animationを実行します。
        /// </summary>
        public override void StartExitAnimation(AnimationPattern pattern = AnimationPattern.Pattern_1, Action onAnimationDone = null, System.Action onCutOff = null)
        {
            ForceResetPlayedState(false, false, pattern);
            base.StartExitAnimation(pattern, onAnimationDone, onCutOff);
        }
    }
}
