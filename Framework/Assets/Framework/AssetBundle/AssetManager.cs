using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Framework
{
    /// <summary>
    /// yield return StartCoroutineを使うともれなく次の処理が1フレーム遅れる!! ご注意！！
    /// </summary>
    public partial class AssetManager : SingletonComponent<AssetManager>
    {
        public static bool useAssetBundle = true;

        /// <summary>
        /// AssetBundle Cache.
        /// Don't need cache all assets in assetbundle. Seems it is not too slow to load an asset if we have its assetbundle.
        /// And if we use assetbundle.UnLoad(false), all these assets need to be deleted by yourself.
        /// </summary>
        public class AssetCache
        {
            // assetBundle's hash
            public string hash;
            // the refined asset name to load
            public string assetFileName = "";
            // assetbundle.
            public AssetBundle bundle = null;
            // Is loading the assetbundle? Avoid from duplicated loading.
            public bool isLoading = false;
        }

        // Store all assets information. <hash string of the asset, AssetCache object>
        // When loading a asset, search this dictionary to get the assetBundle name and hash, then use UnityWebRequestAssetBundle.GetAssetBundle to load.
        private Dictionary<string, AssetCache> assetCaches = new Dictionary<string, AssetCache>();


        /// <summary>
        /// Load Object from asset's name. In case asset name equals file name.
        /// </summary>
        public static IEnumerator LoadCoroutine<T>(string assetName, System.Action<T> onCompleted) where T : Object
        {
            yield return LoadCoroutine<T>(assetName, null, onCompleted);
        }

        /// <summary>
        /// Load Object from asset's name and file name. One asset may contains multiple files.
        /// </summary>
        public static IEnumerator LoadCoroutine<T>(string assetName, string assetFileName, System.Action<T> onCompleted) where T : Object
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

            // local
            if (!useAssetBundle)
            {
                T obj = Resources.Load<T>(fileName);
                if (onCompleted != null)
                {
                    onCompleted.Invoke(obj);
                }
                yield break;
            }

            // assetbundle
            yield return AssetManager.instance.LoadFromCacheCoroutine<T>(assetName, assetFileName, false, onCompleted);
        }

        /// <summary>
        /// Async load.
        /// Load Object from asset's name. In case asset name equals file name.
        /// </summary>
        public static IEnumerator LoadAsyncCoroutine<T>(string assetName, System.Action<T> onCompleted) where T : Object
        {
            yield return LoadAsyncCoroutine<T>(assetName, null, onCompleted);
        }

        /// <summary>
        /// Async load.
        /// Load Object from asset's name and file name. One asset may contains multiple files.
        /// </summary>
        public static IEnumerator LoadAsyncCoroutine<T>(string assetName, string assetFileName, System.Action<T> onCompleted) where T : Object
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

            // local
            if (!useAssetBundle)
            {
                var request = Resources.LoadAsync<T>(fileName);
                yield return request;

                if (onCompleted != null)
                {
                    onCompleted.Invoke(request.asset as T);
                }
            }
            else    // assetBundle
            {
                yield return AssetManager.instance.LoadFromCacheCoroutine<T>(assetName, assetFileName, true, onCompleted);
            }
        }

        /// <summary>
        /// Load assetBundle from cache if can, or load from server.
        /// </summary>
        public IEnumerator LoadFromCacheCoroutine<T>(string assetName, string assetFileName, bool isAsync, System.Action<T> onCompleted) where T : Object
        {
            // load all dependent assets.
            // load dependent assets.
            var dependencies = AssetDownloader.instance.scriptableManifest.GetAllDependencies(assetName);
            foreach (string dependence in dependencies)
            {
                AssetCache dependenceCache = GetOrCreateCachedAssetBundle(dependence);
                if (dependenceCache.bundle == null)
                {
                    if (dependenceCache.isLoading)
                    {
                        yield return new WaitWhile(() => dependenceCache.isLoading);
                    }
                    yield return LoadAssetBundle(dependenceCache);
                }
            }

            // find in cache.
            AssetCache cache = GetOrCreateCachedAssetBundle(assetName);
            if (cache.bundle == null)
            {
                if (cache.isLoading)
                {
                    yield return new WaitWhile(() => cache.isLoading);
                }
                yield return LoadAssetBundle(cache);
            }

            string fileName = assetFileName;
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = assetName;
            }
            yield return LoadAsset<T>(fileName, cache.bundle, isAsync, onCompleted);
        }

        /// <summary>
        /// Load an assetBundle and save it into cache.
        /// </summary>
        private IEnumerator LoadAssetBundle(AssetCache cache)
        {
            if (cache == null)
            {
                yield break;
            }
            cache.isLoading = true;
            UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(AssetDownloader.instance.assetUrlPath + cache.assetFileName, Hash128.Parse(cache.hash), 0);
            yield return request.SendWebRequest();
            var bundle = DownloadHandlerAssetBundle.GetContent(request);
            cache.bundle = bundle;

            // reset isloading flag.
            cache.isLoading = false;
        }

        /// <summary>
        /// Load an asset file from a assetbundle and cache all files.
        /// Unload the assetbundle after cached all files.
        /// </summary>
        private IEnumerator LoadAsset<T>(string fileName, AssetBundle bundle, bool isAsync, System.Action<T> onCompleted) where T : Object
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
                    break;
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

            if (onCompleted != null)
            {
                onCompleted.Invoke(obj as T);
            }
        }

        /// <summary>
        /// Get a Cached object from its name.
        /// </summary>
        private AssetCache GetOrCreateCachedAssetBundle(string assetName)
        {
            string hash = AssetDownloader.instance.scriptableManifest.GetHash(assetName);
            AssetCache cache = null;
            if (assetCaches.TryGetValue(hash, out cache))
            {
                return cache;
            }
            // create new cache.
            cache = new AssetCache() { hash = hash, assetFileName = assetName, bundle = null, isLoading = false };
            assetCaches.Add(hash, cache);
            return cache;
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
        /// Unload an asset. 
        /// Use AssetBundle.Unload(true) to make sure the objects cannot be copied.
        /// </summary>
        public void UnLoad(string assetName)
        {
            string hash = AssetDownloader.instance.scriptableManifest.GetHash(assetName.ToLower());
            AssetCache cache = null;
            if (!assetCaches.TryGetValue(hash, out cache))
            {
                return;
            }
            if (cache.bundle != null)
            {
                if (cache.isLoading)
                {
                    Debug.Log("Cannot unload a assetbundle which is loading.");
                    return;
                }
                // Use AssetBundle.Unload(true) to make sure the objects cannot be copied.
                cache.bundle.Unload(true);
            }
            assetCaches.Remove(hash);
        }

        /// <summary>
        /// Unload all assetbundles.
        /// </summary>
        public void UnloadAll()
        {
            foreach (var pair in assetCaches)
            {
                AssetCache cache = pair.Value;
                if (cache.bundle != null)
                {
                    if (cache.isLoading)
                    {
                        Debug.Log("Cannot unload a assetbundle which is loading.");
                        continue;
                    }
                    // Use AssetBundle.Unload(true) to make sure the objects cannot be copied.
                    cache.bundle.Unload(false);
                }
            }
            assetCaches.Clear();
        }
    }

}
