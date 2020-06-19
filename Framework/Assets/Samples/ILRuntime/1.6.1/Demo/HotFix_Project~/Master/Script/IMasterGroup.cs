using SerializableCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Cute.Master
{
    public interface IMasterGroupElement
    {
#if UNITY_EDITOR
        void AddData(string[] values, int[] indexes);
#endif
    }

    /// <summary>Masterのデータ設定用基本クラス</summary>
    public abstract class IMasterGroupBase : ScriptableObject
    {
#if UNITY_EDITOR
        /// <summary>各プロパティのcsv内indexを取得する</summary>
        public abstract int[] GetPropertyIndex(string[] values);
        /// <summary>Csvの1行データを追加</summary>
        public abstract void AddData(int id, string[] values, int[] indexes);
        /// <summary>データを消去</summary>
        public abstract void ClearData();
#endif

        /// <summary>
        /// MasterGroup系classのデータを全て破棄する
        /// タイトルへ戻る等のMasterデータのリロード処理の前に呼ぶこと
        /// </summary>
        public static void ClearAll()
        {
            var type = typeof(IMasterGroupBase);
            var types = Assembly.GetAssembly(type).GetTypes()
                .Where(p => p.BaseType != null && p.BaseType.BaseType == type);

            foreach (var t in types)
            {
                t.BaseType.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
            }
        }
    }

    public abstract class IMasterGroup<T, U, E> : IMasterGroupBase where T : IMasterGroupElement, new() where U : IMasterGroupBase, new() where E : SerializableDictionary<int, T>, new()
    {
        public static U Instance { get; private set; }

        [SerializeField]
        E dict = new E();

#if UNITY_EDITOR
        public override void AddData(int id, string[] values, int[] indexes)
        {
            T master;
            if (!dict.TryGetValue(id, out master))
            {
                master = new T();
                dict.Add(id, master);
            }
            master.AddData(values, indexes);
        }

        public override void ClearData()
        {
            dict.Clear();
            dict.TrimExcess();
        }
#endif

        public T Get(int id)
        {
            T value;
            if (dict.TryGetValue(id, out value))
            {
                return value;
            }
            return default(T);
        }

        public List<int> GetIdList()
        {
            return dict.Keys.ToList();
        }

        public List<T> GetAllInstances()
        {
            return dict.Values.ToList();
        }

        protected virtual void OnEnable()
        {
            Instance = this as U;
        }

        public static void Clear()
        {
            Instance = null;
        }
    }
}