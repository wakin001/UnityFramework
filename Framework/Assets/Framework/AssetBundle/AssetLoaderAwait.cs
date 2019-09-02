using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UniRx.Async;

namespace Framework
{
    public partial class AssetManager
    {
        /// <summary>
        /// Load Object from asset's name. In case asset name equals file name.
        /// </summary>
        public static async Task<T> LoadWithAwait<T>(string assetName) where T : Object
        {
            return await LoadWithAwait<T>(assetName, null);
        }

        /// <summary>
        /// Load Object from asset's name and file name. One asset may contains multiple files.
        /// </summary>
        public static async Task<T> LoadWithAwait<T>(string assetName, string assetFileName) where T : Object
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
            return await AssetManager.instance.LoadFromCacheCoroutineWithAwait<T>(assetName, assetFileName, false);
        }

        public static async Task<T> LoadAsyncWithAwait<T>(string assetName) where T : Object
        {
            return await AssetManager.instance.LoadAsyncCoroutineWithAwait<T>(assetName, null);
        }

        public static async Task<T> LoadAsyncWithAwait<T>(string assetName, string assetFileName) where T : Object
        {
            return await AssetManager.instance.LoadAsyncCoroutineWithAwait<T>(assetName, assetFileName);
        }

        private async Task<T> LoadAsyncCoroutineWithAwait<T>(string assetName, string assetFileName) where T : Object
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
                await request;

                return request.asset as T;
            }
            else    // assetBundle
            {
                return await LoadFromCacheCoroutineWithAwait<T>(assetName, assetFileName, true);
            }
        }

        public async Task<T> LoadFromCacheCoroutineWithAwait<T>(string assetName, string assetFileName, bool isAsync) where T : Object
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
                        await new WaitWhile(() => dependenceCache.isLoading);
                    }
                    await LoadAssetBundleWithAwait(dependenceCache);
                }
            }

            // find in cache.
            AssetCache cache = GetOrCreateCachedAssetBundle(assetName);
            if (cache.bundle == null)
            {
                if (cache.isLoading)
                {
                    await new WaitWhile(() => cache.isLoading);
                }
                await LoadAssetBundleWithAwait(cache);
            }

            string fileName = assetFileName;
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = assetName;
            }
            return await LoadAssetWithAwait<T>(fileName, Hash128.Parse(cache.hash), cache.bundle, isAsync);
        }

        /// <summary>
        /// Load an assetBundle and save it into cache.
        /// </summary>
        private async Task LoadAssetBundleWithAwait(AssetCache cache)
        {
            if (cache == null)
            {
                return;
            }
            cache.isLoading = true;
            UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(AssetDownloader.instance.assetUrlPath + cache.assetFileName, Hash128.Parse(cache.hash), 0);
            await request.SendWebRequest();
            var bundle = DownloadHandlerAssetBundle.GetContent(request);
            cache.bundle = bundle;

            // reset isloading flag.
            cache.isLoading = false;
        }

        /// <summary>
        /// Load an asset file from a assetbundle and cache all files.
        /// Unload the assetbundle after cached all files.
        /// </summary>
        private async Task<T> LoadAssetWithAwait<T>(string fileName, Hash128 hash, AssetBundle bundle, bool isAsync) where T : Object
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
            if (isAsync)
            {
                var loadRequest = bundle.LoadAssetAsync(fullFileName);
                await loadRequest;
                obj = loadRequest.asset;
            }
            else
            {
                obj = bundle.LoadAsset(fullFileName);
            }
            return obj as T;
        }
    }

}
