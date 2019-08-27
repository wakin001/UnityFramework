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
            // assetBundle's hash
            public Hash128 hash;
            // the asset name to load
            public string assetFileName = "";
            // asset bundle that hold the object.
            public AssetBundle asset = null;
            // loaded object.
            public Object obj = null;
        }

        // Store all assets information. <assetName, AssetCache object>
        // When loading a asset, search this dictionary to get the assetBundle name and hash, then use UnityWebRequestAssetBundle.GetAssetBundle to load.
        private static Dictionary<string, AssetCache> assetCaches = new Dictionary<string, AssetCache>();

        /// <summary>
        /// Load Object from asset's name. In case asset name equals file name.
        /// </summary>
        public static void Load<T>(string assetName, System.Action<T> onCompleted) where T : Object
        {
            Load<T>(assetName, null, onCompleted);
        }

        /// <summary>
        /// Load Object from asset's name and file name. One asset may contains multiple files.
        /// </summary>
        public static void Load<T>(string assetName, string assetFileName, System.Action<T> onCompleted) where T : Object
        {
            assetName = assetName.ToLower();
            if (assetFileName != null)
            {
                assetFileName = assetFileName.ToLower();
            }
            string fileName = assetFileName;
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = assetName;
            }
            // find in cache.
            Object obj = GetCachedObject(fileName);
            if (obj != null)
            {
                if (onCompleted != null)
                {
                    onCompleted.Invoke(obj as T);
                }
                return;
            }

            if (!useAssetBundle)
            {
                obj = Resources.Load(fileName);
                if (onCompleted != null)
                {
                    onCompleted.Invoke(obj as T);
                }
                return;
            }

            if (AssetDownloader.instance.useCaching)
            {
                SingletonMonoBehaviour.instance.StartCoroutine(LoadFromCacheCoroutine<T>(assetName, assetFileName, false, onCompleted));
            }
            else
            {
                AssetBundle asset = AssetBundle.LoadFromFile(AssetDownloader.instance.savePath + assetName);
                SingletonMonoBehaviour.instance.StartCoroutine(LoadAsset<T>(fileName, new Hash128(), asset, true, onCompleted));
            }
        }

        public static void LoadAsync<T>(string assetName, System.Action<T> onCompleted) where T : Object
        {
            SingletonMonoBehaviour.instance.StartCoroutine(LoadAsyncCoroutine<T>(assetName, null, onCompleted));
        }

        public static void LoadAsync<T>(string assetName, string assetFileName, System.Action<T> onCompleted) where T : Object
        {
            SingletonMonoBehaviour.instance.StartCoroutine(LoadAsyncCoroutine<T>(assetName, assetFileName, onCompleted));
        }

        private static IEnumerator LoadAsyncCoroutine<T>(string assetName, string assetFileName, System.Action<T> onCompleted) where T : Object
        {
            assetName = assetName.ToLower();
            if (assetFileName != null)
            {
                assetFileName = assetFileName.ToLower();
            }
            string fileName = assetFileName;
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = assetName;
            }
            // find in cache.
            Object obj = GetCachedObject(fileName);
            if (obj != null)
            {
                if (onCompleted != null)
                {
                    onCompleted.Invoke(obj as T);
                }
                yield break;
            }

            if (!useAssetBundle)
            {
                var request = Resources.LoadAsync<T>(fileName);
                yield return request;

                if (onCompleted != null)
                {
                    onCompleted.Invoke(request.asset as T);
                }
            }
            else if (AssetDownloader.instance.useCaching)
            {
                yield return LoadFromCacheCoroutine<T>(assetName, assetFileName, true, onCompleted);
            }
            else
            {
                var asyncRequest = AssetBundle.LoadFromFileAsync(AssetDownloader.instance.savePath + assetName);
                yield return asyncRequest;

                yield return LoadAsset<T>(fileName, new Hash128(), asyncRequest.assetBundle, true, onCompleted);
            }
        }

        public static IEnumerator LoadFromCacheCoroutine<T>(string assetName, string assetFileName, bool isAsync, System.Action<T> onCompleted) where T : Object
        {
            if (!AssetDownloader.instance.useCaching)
            {
                if (onCompleted != null)
                {
                    onCompleted.Invoke(default(T));
                }
                yield break;
            }
            Hash128 hash = AssetDownloader.instance.scriptableManifest.GetHash(assetName);
            UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(AssetDownloader.instance.assetUrlPath + assetName, hash, 0);
            yield return request.SendWebRequest();

            string fileName = assetFileName;
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = assetName;
            }
            var bundle = DownloadHandlerAssetBundle.GetContent(request);
            yield return LoadAsset<T>(fileName, hash, bundle, isAsync, onCompleted);
        }

        private static IEnumerator LoadAsset<T>(string fileName, Hash128 hash, AssetBundle bundle, bool isAsync, System.Action<T> onCompleted) where T : Object
        {
            if (bundle == null)
            {
                yield break;
            }
            Object obj = null;

            var names = bundle.GetAllAssetNames();
            string fullFileName = "";
            foreach (string name in names)
            {
                string refinedName = GetRefinedAssetFileName(name);
                if (refinedName.Contains(fileName))
                {
                    fullFileName = name;

                    // add to cache.
                    AddToAssetCache(refinedName, hash, bundle, null);
                }
            }
            if (isAsync)
            {
                var loadRequest = bundle.LoadAssetAsync(fullFileName);
                yield return loadRequest;
                obj = loadRequest.asset;
            }
            else
            {
                obj = bundle.LoadAsset(fullFileName);
            }
            // add to cache.
            AddToAssetCache(fileName, hash, bundle, obj);

            // unload assetbundle.
            bundle.Unload(false);
            bundle = null;

            if (onCompleted != null)
            {
                onCompleted.Invoke(obj as T);
            }
        }

        private static void AddToAssetCache(string filename, Hash128 hash, AssetBundle asset, Object obj = null)
        {
            AssetCache cache = null;
            assetCaches.TryGetValue(filename, out cache);
            if (cache != null && cache.hash == hash)
            {
                return;
            }

            var names = asset.GetAllAssetNames();
            foreach (string name in names)
            {
                if (name.Contains(filename))
                {
                    if (cache == null)
                    {
                        cache = new AssetCache() { assetFileName = name, asset = asset, hash = hash, obj = asset.LoadAsset(name) };
                    }
                    else
                    {
                        cache.hash = hash;
                        cache.obj = asset.LoadAsset(name);
                        cache.assetFileName = name;
                    }
                    assetCaches[filename] = cache;
                    break;
                }
            }
        }

        private static Object GetCachedObject(string fileName)
        {
            AssetCache cache = null;
            if (!assetCaches.TryGetValue(fileName, out cache))
            {
                return null;
            }
            if (cache.obj == null)
            {
                if (cache.asset == null)
                {
                    return null;
                }
                string fullFileName = "";
                var names = cache.asset.GetAllAssetNames();
                foreach (string name in names)
                {
                    string refinedName = GetRefinedAssetFileName(name);
                    if (refinedName.Contains(fileName))
                    {
                        fullFileName = name;
                        break;
                    }
                }
                cache.obj = cache.asset.LoadAsset(fullFileName);
            }
            if (cache.obj != null)
            {
                return cache.obj;
            }
            return null;
        }

        public static string GetRefinedAssetFileName(string fullName)
        {
            string refinedName = fullName;
            int index = fullName.LastIndexOf('.');
            if (index >= 0)
            {
                refinedName = fullName.Substring(0, index);
            }
            return refinedName.Replace(AssetBundleDefine.assetRootDirectory.ToLower(), "");
        }

        /// <summary>
        /// UnLoad asset.
        /// </summary>
        public static void UnLoad(string assetName)
        {
            AssetCache cache = null;
            if (!assetCaches.TryGetValue(assetName, out cache))
            {
                return;
            }
            if (cache.asset != null)
            {
                cache.asset.Unload(false);
            }
            assetCaches.Remove(assetName);
        }
    }

}
