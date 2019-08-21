using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cute
{
    public class Coroutine<T>
    {
        public T Value
        {
            get
            {
                if (e != null)
                {
                    throw e;
                }
                return returnVal;
            }
        }
        private T returnVal;
        private Exception e;
        public Coroutine coroutine;

        public IEnumerator InternalRoutine(IEnumerator coroutine)
        {
            while (true)
            {
                try
                {
                    if (!coroutine.MoveNext())
                    {
                        yield break;
                    }
                }
                catch (Exception e)
                {
                    this.e = e;
                    yield break;
                }
                object yielded = coroutine.Current;
                if (yielded != null && yielded.GetType() == typeof(T))
                {
                    returnVal = (T)yielded;
                    yield break;
                }
                else
                {
                    yield return coroutine.Current;
                }
            }
        }
    }

    public static class MonobehaviourExt
    {
        public static Coroutine<T> StartCoroutine<T>(this MonoBehaviour obj, IEnumerator coroutine)
        {
            Coroutine<T> coroutineObject = new Coroutine<T>();
            coroutineObject.coroutine = obj.StartCoroutine(coroutineObject.InternalRoutine(coroutine));
            return coroutineObject;
        }
    }

}