using System.Diagnostics;
using UnityEngine;
using UnityEngine.Profiling;

namespace HotFix_Project
{
    /// <summary>
    /// プロファイラをラッピングするためのクラス
    /// そのまま使用するとプログラムの可読性が悪くなるのでこちらを使うこと
    /// </summary>
    public class ProfileChecker
    {
        [Conditional("UNITY_EDITOR"), Conditional("PROFILE")]
        public static void BeginSample(string name)
        {
#if !DEEPPROFILE
            Profiler.BeginSample(name);
#endif
        }

        [Conditional("UNITY_EDITOR"), Conditional("PROFILE")]
        public static void BeginSample(string name, Object targetObject)
        {
#if !DEEPPROFILE
            Profiler.BeginSample(name, targetObject);
#endif
        }

        [Conditional("UNITY_EDITOR"), Conditional("PROFILE")]
        public static void EndSample()
        {
#if !DEEPPROFILE
            Profiler.EndSample();
#endif
        }
    }
}
