
namespace Cute.Master
{
    /// <summary>Masterデータ基本クラス</summary>
    public abstract class IMaster<U> : IMasterBase where U : IMasterBase
    {
        public static U Instance { get; private set; }

        protected virtual void OnEnable()
        {
            Instance = this as U;
        }
    }
}