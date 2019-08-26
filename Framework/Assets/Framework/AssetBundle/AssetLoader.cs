using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Framework
{
    public class AssetLoader
    {
        public static bool useAssetBundle = true;

        public class AssetCache
        {
            // assetBundle's name
            public string assetBundleName;
            // assetBundle's hash
            public string hash;
            // the asset name to load
            public string assetName;
        }
        // Store all assets information. <assetName, AssetCache object>
        // When loading a asset, search this dictionary to get the assetBundle name and hash, then use UnityWebRequestAssetBundle.GetAssetBundle to load.
        private static Dictionary<string, AssetCache> assetCaches = new Dictionary<string, AssetCache>();

        /// <summary>
        /// Traversing all assets by manifest file and recreate aasetCaches.
        /// </summary>
        public static void UpdateAssetCache()
        {
            assetCaches.Clear();

            var allAssetBundles = AssetDownloader.instance.manifest.GetAllAssetBundles();
            for (int i = 0; i < allAssetBundles.Length; ++i)
            {
                string assetBundleName = allAssetBundles[i];

                string[] allDependencies = AssetDownloader.instance.manifest.GetAllDependencies(assetBundleName);
                for (int j = 0; j < allDependencies.Length; ++j)
                {

                }
            }


        }

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
                SingletonMonoBehaviour.instance.StartCoroutine(LoadFromCacheCoroutine<T>(name, false, onCompleted));
            }
            else
            {
                AssetBundle asset = AssetBundle.LoadFromFile(AssetDownloader.instance.savePath + name);
                if (asset != null)
                {
                    obj = asset.LoadAsset<T>(name);
                }
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
                yield return LoadFromCacheCoroutine<T>(name, true, onCompleted);
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

        public static IEnumerator LoadFromCacheCoroutine<T>(string name, bool isAsync, System.Action<T> onCompleted) where T : Object
        {
            if (!AssetDownloader.instance.useCaching)
            {
                if (onCompleted != null)
                {
                    onCompleted.Invoke(default(T));
                }
                yield break;
            }
            Hash128 hash = AssetDownloader.instance.manifest.GetAssetBundleHash(name);
            UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(AssetDownloader.instance.assetUrlPath + name, hash, 0);
            yield return request.SendWebRequest();

            T obj = default(T);
            var bundle = DownloadHandlerAssetBundle.GetContent(request);
            if (bundle != null)
            {
                var names = bundle.GetAllAssetNames();
                if (isAsync)
                {
                    var loadRequest = bundle.LoadAssetAsync(name);
                    yield return loadRequest;
                    obj = loadRequest.asset as T;
                }
                else
                {
                    obj = bundle.LoadAsset(name) as T;
                }
            }
            if (onCompleted != null)
            {
                onCompleted.Invoke(obj);
            }
        }

        //private static AssetBundleManifest LoadManifest(string path)
        //{
        //    string manifestName = Path.GetFileNameWithoutExtension(path);
        //    AssetBundle manifestBundle = AssetBundle.LoadFromFile(savePath + manifestName);
        //    if (manifestBundle == null)
        //    {
        //        Debug.LogWarning("Cannot load manifest file");
        //        return null;
        //    }
        //    var manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        //    return manifest;
        //}
    }

}
