namespace Framework
{
    /// <summary>
    /// アウトゲーム共通のシーンクラスのモデルベース
    /// 継承したものを次のシーンに移す
    /// </summary>
    public class SceneModelBase
    {
        /// <summary>
        /// Matching Scene Name
        /// </summary>
        public string sceneName { get; private set; }

        /// <summary>
        /// !! 継承したクラスなどで使用しないでください
        /// SceneNameを設定（SceneController専用）
        /// </summary>
        /// <param name="sceneName"></param>
        public void SetSceneName(string sceneName)
        {
            this.sceneName = sceneName;
        }
    };
}
