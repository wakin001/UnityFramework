using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Framework
{
    /// <summary>
    /// Asset downloader.
    /// </summary>
    public class AssetDownloader : SingletonComponent<AssetDownloader>
    {
        /// <summary>
        /// Using Caching to cache assetbundles?
        /// </summary>
        private bool _useCaching = true;
        public bool useCaching
        {
            get { return _useCaching; }
            set
            {
                _useCaching = value;
            }
        }

        private AssetBundleManifest _manifest = null;
        public AssetBundleManifest manifest
        {
            get
            {
                if (_manifest == null)
                {
                    string manifestName = Path.GetFileNameWithoutExtension(assetBundleManafestName);
                    AssetBundle manifestBundle = AssetBundle.LoadFromFile(savePath + manifestName);
                    if (manifestBundle == null)
                    {
                        Debug.LogWarning("Cannot load manifest file");
                        return null;
                    }
                    _manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                }
                return _manifest;
            }
            private set
            {
                _manifest = value;
            }
        }

        // downalod from assetUrlPath
        public string assetUrlPath { get; private set; }
        // manifest file path.
        public string savePath { get; private set; }
        private const string assetBundleManafestName = "build.manifest";

        public Action onCompleted = null;

        private void Awake()
        {
            // test.
            savePath = Application.dataPath + "/../AssetBundles/test/";
        }

        public IEnumerator DownloadManifest(string urlPath, string assetUrlPath, string toPath)
        {
            this.assetUrlPath = assetUrlPath;
            this.savePath = toPath;

            string manifestName = Path.GetFileNameWithoutExtension(assetBundleManafestName);

            // remove old files.
            string manifestPath = toPath + manifestName;
            if (File.Exists(manifestPath))
            {
                File.Delete(manifestPath);
            }
            string manifestBundlePath = toPath + assetBundleManafestName;
            if (File.Exists(manifestBundlePath))
            {
                File.Delete(manifestBundlePath);
            }

            // Always use Download to download manifest files.
            var downloader = new Downloader();
            var bundleDownloader = new Downloader();
            yield return downloader.Download(urlPath, assetBundleManafestName, new Hash128(), toPath);
            yield return bundleDownloader.Download(urlPath, manifestName, new Hash128(), toPath);

            if (downloader.errorType == DownloadErrorType.None && bundleDownloader.errorType == DownloadErrorType.None)
            {
                AssetBundle manifestBundle = AssetBundle.LoadFromFile(manifestPath);
                if (manifestBundle == null)
                {
                    Debug.LogWarning("Cannot load manifest file");
                    yield break;
                }
                manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

                StartCoroutine(DownloadAllAssets(assetUrlPath, toPath, manifest));
            }
        }

        public IEnumerator DownloadAllAssets(string urlPath, string toPath, AssetBundleManifest manifest)
        {
            if (manifest == null)
            {
                Debug.LogError("manifest is null in " + toPath);
                yield break;
            }
            string[] allAssets = manifest.GetAllAssetBundles();
            foreach (string assetName in allAssets)
            {
                var downloader = GetDownloader();
                Hash128 hash = manifest.GetAssetBundleHash(assetName);
                yield return downloader.Download(assetUrlPath, assetName, hash, toPath);
            }

            if (onCompleted != null)
            {
                onCompleted.Invoke();
            }
        }

        private IDownloader GetDownloader()
        {
            if (instance.useCaching)
            {
                return new CacheDownloader();
            }
            else
            {
                return new Downloader();
            }
        }
    }
}
