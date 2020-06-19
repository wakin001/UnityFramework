namespace Cute.Master
{
    /// <summary>Masterのデータ設定用クラス</summary>
    public abstract class IMasterElement
    {
#if UNITY_EDITOR
        public abstract void SetData(string[] values, int[] indexes);
#endif
    }
}