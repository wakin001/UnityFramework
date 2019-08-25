using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Framework
{
    public class AssetLoader
    {
        public static bool useAssetBundle = true;

        public static void Load<T>(string name, System.Action<T> onCompleted) where T : Object
        {
            T obj = null;
            if (!useAssetBundle)
            {
                obj = Resources.Load<T>(name);
                if (onCompleted != null)
                {
                    onCompleted.Invoke(obj);
                }
            }
            else if (AssetDownloader.instance.useCaching)
            {
                SingletonMonoBehaviour.instance.StartCoroutine(LoadAsyncCoroutine<T>(name, onCompleted));
            }
            else
            {
                AssetBundle asset = AssetBundle.LoadFromFile(AssetDownloader.instance.savePath + name);
                obj = asset.LoadAsset<T>(name);
                if (onCompleted != null)
                {
                    onCompleted.Invoke(obj);
                }
            }
        }

        public static void LoadAsync<T>(string name, System.Action<T> onCompleted) where T : Object
        {
            SingletonMonoBehaviour.instance.StartCoroutine(LoadAsyncCoroutine<T>(name, onCompleted));
        }

        public static IEnumerator LoadFromCacheCoroutine<T>(string name, System.Action<T> onCompleted) where T : Object
        {

            if (!AssetDownloader.instance.useCaching)
            {
                yield break;
            }
            Coroutine<T> routine = SingletonMonoBehaviour.instance.StartCoroutine<T>(LoadFromCache<T>(name, false));
            yield return routine.coroutine;
            if (onCompleted != null)
            {
                onCompleted.Invoke(routine.Value as T);
            }
        }

        private static IEnumerator LoadFromCache<T>(string name, bool isAsync) where T : Object
        {
            // Wait for caching is ready.
            if (!Caching.ready)
            {
                yield return null;
            }
            Hash128 hash = AssetDownloader.instance.manifest.GetAssetBundleHash(name);
            UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(AssetDownloader.instance.assetUrlPath + name, hash, 0);
            yield return request.SendWebRequest();

            var bundle = DownloadHandlerAssetBundle.GetContent(request);
            if (isAsync)
            {
                var loadRequest = bundle.LoadAssetAsync(name);
                yield return loadRequest.asset as T;
            }
            yield return bundle.LoadAsset(name) as T;
        }

        private static IEnumerator LoadAsyncCoroutine<T>(string name, System.Action<T> onCompleted) where T : Object
        {
            if (!useAssetBundle)
            {
                var request = Resources.LoadAsync<T>(name);
                yield return request;

                if (onCompleted != null)
                {
                    onCompleted.Invoke(request.asset as T);
                }
            }
            else if (AssetDownloader.instance.useCaching)
            {
                Coroutine<T> routine = SingletonMonoBehaviour.instance.StartCoroutine<T>(LoadFromCache<T>(name, true));
                yield return routine.coroutine;
                if (onCompleted != null)
                {
                    onCompleted.Invoke(routine.Value as T);
                }
            }
            else
            {
                var asyncRequest = AssetBundle.LoadFromFileAsync(AssetDownloader.instance.savePath + name);
                yield return asyncRequest;
                if (onCompleted != null)
                {
                    onCompleted.Invoke(asyncRequest.assetBundle.LoadAsset(name) as T);
                }
            }
        }
    }

}
