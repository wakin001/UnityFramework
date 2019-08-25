using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Framework
{
    public class AssetLoader
    {
        public static bool useAssetBundle = true;

        public static T Load<T>(string name) where T : Object
        {
            if (!useAssetBundle)
            {
                return Resources.Load<T>(name);
            }
            else if (AssetDownloader.instance.useCaching)
            {
                Coroutine<T> routine = AssetDownloader.instance.StartCoroutine<T>(LoadFromCache<T>(name, false));
                return routine.Value;
            }
            else
            {
                AssetBundle asset = AssetBundle.LoadFromFile(AssetDownloader.instance.savePath + name);
                return asset.LoadAsset<T>(name);
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

        public static IEnumerator LoadAsync<T>(string name, System.Action<T> onCompleted) where T : Object
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
                // Wait for caching is ready.
                if (!Caching.ready)
                {
                    yield return null;
                }
                Hash128 hash = AssetDownloader.instance.manifest.GetAssetBundleHash(name);
                UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(AssetDownloader.instance.assetUrlPath + name, hash, 0);
                yield return request.SendWebRequest();

                var bundle = DownloadHandlerAssetBundle.GetContent(request);
                var loadRequest = bundle.LoadAssetAsync(name);
                yield return loadRequest;
                if (onCompleted != null)
                {
                    onCompleted.Invoke(loadRequest.asset as T);
                }
            }
            else
            {
                // todo
            }
        }
    }

}
