using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cute
{
    /// <summary>
    /// プールオブジェクトのベースクラス
    /// 使い終わったら必ずRelease()を呼ぶ。
    /// </summary>
    public interface IPoolableObject
    {
        /// <summary>
        /// PoolからもらったObjectを初期化する
        /// </summary>
        void Init();

        /// <summary>
        /// 解放
        /// </summary>
        void Release();

        /// <summary>
        /// 使われてるかどうか
        /// </summary>
        bool IsUsing();
    }

    /// <summary>
    /// プールオブジェクト
    /// </summary>
    public class PoolableObject<T> where T : IPoolableObject, new()
    {
        private List<T> pool;

        public PoolableObject(int initialCapacity)
        {
            pool = new List<T>(initialCapacity);
            for (int i = 0; i < initialCapacity; ++i)
            {
                // instantiate a pool of N objects
                T obj = new T();
                pool.Add(obj);
            }
        }

        /// <summary>
        /// プールでのオブジェクトの数
        /// </summary>
        public int Count
        {
            get { return pool.Count; }
        }

        /// <summary>
        /// プールからオブジェクトの取得
        /// </summary>
        public T Fetch()
        {
            bool found = false;
            T obj = default(T);
            for (int i = 0; i < pool.Count; ++i)
            {
                if (!pool[i].IsUsing())
                {
                    found = true;
                    obj = pool[i];
                    break;
                }
            }
            if (!found)
            {
                obj = new T();
                pool.Add(obj);
            }
            obj.Init();
            return obj;
        }

        /// <summary>
        /// プールですべてなオブジェクトをリリースする
        /// </summary>
        public void ReleaseAll()
        {
            for (int i = 0; i < pool.Count; ++i)
            {
                pool[i].Release();
            }
            pool.Clear();
        }
    }
}

