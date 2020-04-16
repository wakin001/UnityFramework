using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framework
{
    /// <summary>
    /// タッチガードの識別オブジェクト
    /// </summary>
    public class TouchGuardObject : IDisposable
    {
        /// <summary> ラベル</summary>
        private string label = "";
        /// <summary> ラベル</summary>
        public string Label { get { return label; } }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public TouchGuardObject(string label)
        {
            this.label = label;
        }

        /// <summary>
        /// IDisposable実装
        /// </summary>
        public void Dispose()
        {
            //if (!BaseCanvas.IsInstanceEmpty())
            {
                BaseCanvas.instance.RemoveTouchGuardObject(this);
            }
        }
    }
}
