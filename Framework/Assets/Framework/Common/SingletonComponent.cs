using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    public class SingletonComponent<T> : MonoBehaviour where T : SingletonComponent<T>
    {
        private static T _instance;

        public static T instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = (T)FindObjectOfType(typeof(T));
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject(typeof(T).Name, typeof(T));
                        _instance = obj.GetComponent<T>();
                        DontDestroyOnLoad(_instance.gameObject);
                    }
                }
                return _instance;
            }

            private set { _instance = value as T; }
        }

        public bool IsInstanceEmpty()
        {
            return _instance == null;
        }

        protected virtual void Awake()
        {

        }
    }
}
