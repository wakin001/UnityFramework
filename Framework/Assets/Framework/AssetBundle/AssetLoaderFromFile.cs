using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UniRx.Async;

namespace Framework
{
    /// <summary>
    /// Use AssetBundle.LoadFromFile() to load assetbundle.
    /// </summary>
    public partial class AssetManager
    {
        /// <summary>
        /// Load Object from asset's name. In case asset name equals file name.
        /// </summary>
        public static T LoadFromFile<T>(string assetName) where T : Object
        {
            return LoadFromFile<T>(assetName, null);
        }

        /// <summary>
        /// Load Object from asset's name and file name. One asset may contains multiple files.
        /// </summary>
        public static T LoadFromFile<T>(string assetName, string assetFileName) where T : Object
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
                return obj;
            }

            // assetbundle
            return AssetManager.instance.LoadAssetBundleFromFile<T>(assetName, assetFileName);
        }

        public static void LoadFromFileAsync<T>(string assetName, System.Action<T> onCompleted) where T : Object
        {
            AssetManager.instance.StartCoroutine(AssetManager.instance.LoadAssetBundleFromFileAsync<T>(assetName, null, onCompleted));
        }

        public static void LoadFromFileAsync<T>(string assetName, string assetFileName, System.Action<T> onCompleted) where T : Object
        {
            AssetManager.instance.StartCoroutine(AssetManager.instance.LoadAssetBundleFromFileAsync<T>(assetName, assetFileName, onCompleted));
        }

        public T LoadAssetBundleFromFile<T>(string assetName, string assetFileName) where T : Object
        {
            // load all dependent assets.
            // load dependent assets.
            var dependencies = AssetDownloader.instance.scriptableManifest.GetAllDependencies(assetName);
            foreach (string dependence in dependencies)
            {
                AssetCache dependenceCache = GetOrCreateCachedAssetBundle(dependence);
                if (dependenceCache.bundle == null)
                {
                    LoadAssetBundleFromFile(dependenceCache);
                }
            }

            // find in cache.
            AssetCache cache = GetOrCreateCachedAssetBundle(assetName);
            if (cache.bundle == null)
            {
                LoadAssetBundleFromFile(cache);
            }

            string fileName = assetFileName;
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = assetName;
            }
            return LoadAssetFromFile<T>(fileName, cache.bundle);
        }

        public IEnumerator LoadAssetBundleFromFileAsync<T>(string assetName, string assetFileName, System.Action<T> onCompleted) where T : Object
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
                    yield return LoadAssetBundleFromFileAsync(dependenceCache);
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
                yield return LoadAssetBundleFromFileAsync(cache);
            }

            string fileName = assetFileName;
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = assetName;
            }
            yield return LoadAssetFromFileAsync<T>(fileName, cache.bundle, onCompleted);
        }

        /// <summary>
        /// Load an assetBundle and save it into cache.
        /// </summary>
        private void LoadAssetBundleFromFile(AssetCache cache)
        {
            if (cache == null)
            {
                return;
            }
            cache.isLoading = true;
            cache.bundle = AssetBundle.LoadFromFile(AssetDownloader.instance.savePath + cache.assetFileName);

            // reset isloading flag.
            cache.isLoading = false;
        }

        /// <summary>
        /// Load an assetBundle and save it into cache.
        /// </summary>
        private IEnumerator LoadAssetBundleFromFileAsync(AssetCache cache)
        {
            if (cache == null)
            {
                yield break;
            }
            cache.isLoading = true;
            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(AssetDownloader.instance.savePath + cache.assetFileName);
            yield return request;
            cache.bundle = request.assetBundle;

            // reset isloading flag.
            cache.isLoading = false;
        }

        /// <summary>
        /// Load an asset file from a assetbundle and cache all files.
        /// Unload the assetbundle after cached all files.
        /// </summary>
        private T LoadAssetFromFile<T>(string fileName, AssetBundle bundle) where T : Object
        {
            if (bundle == null)
            {
                return null;
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
            obj = bundle.LoadAsset(fullFileName);
            return obj as T;
        }

        /// <summary>
        /// Load an asset file from a assetbundle and cache all files.
        /// Unload the assetbundle after cached all files.
        /// </summary>
        private IEnumerator LoadAssetFromFileAsync<T>(string fileName, AssetBundle bundle, System.Action<T> onCompleted) where T : Object
        {
            if (bundle == null)
            {
                if (onCompleted != null)
                {
                    onCompleted.Invoke(null);
                }
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
            var loadRequest = bundle.LoadAssetAsync(fullFileName);
            yield return loadRequest;
            obj = loadRequest.asset;

            if (onCompleted != null)
            {
                onCompleted.Invoke(obj as T);
            }
        }
    }

}
