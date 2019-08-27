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

        private ScriptableManifest _scriptableManifest = null;
        public ScriptableManifest scriptableManifest
        {
            get
            {
                if (_scriptableManifest == null)
                {
                    AssetBundle bundle = AssetBundle.LoadFromFile(savePath + AssetBundleDefine.assetbundleManifestName);
                    if (bundle == null)
                    {
                        Debug.LogWarning("Cannot load manifest file");
                        return null;
                    }
                    _scriptableManifest = bundle.LoadAllAssets<ScriptableManifest>()[0];
                    bundle.Unload(false);
                }
                return _scriptableManifest;
            }
            private set
            {
                _scriptableManifest = value;
            }
        }

        // downalod from assetUrlPath
        public string assetUrlPath { get; set; }
        // manifest file path.
        public string savePath { get; set; }

        public Action onCompleted = null;

        public IEnumerator DownloadManifest(string urlPath, string assetUrlPath, string toPath)
        {
            this.assetUrlPath = assetUrlPath;
            this.savePath = toPath;

            string manifestName = Path.GetFileNameWithoutExtension(AssetBundleDefine.assetbundleManifestName);

            // remove old files.
            string manifestPath = toPath + manifestName;
            if (File.Exists(manifestPath))
            {
                File.Delete(manifestPath);
            }
            string manifestBundlePath = toPath + AssetBundleDefine.assetbundleManifestName;
            if (File.Exists(manifestBundlePath))
            {
                File.Delete(manifestBundlePath);
            }

            // Always use Download to download manifest files.
            var downloader = new Downloader();
            yield return downloader.Download(urlPath, AssetBundleDefine.assetbundleManifestName, new Hash128(), toPath);

            if (downloader.errorType == DownloadErrorType.None)
            {
                StartCoroutine(DownloadAllAssets(assetUrlPath, toPath, scriptableManifest));
            }
        }

        public IEnumerator DownloadAllAssets(string urlPath, string toPath, ScriptableManifest manifest)
        {
            if (manifest == null)
            {
                Debug.LogError("manifest is null in " + toPath);
                yield break;
            }

            // download all categories.
            for (int i = 0; i < manifest.categories.Length; ++i)
            {
                var category = manifest.categories[i];
                // assets.
                foreach (var asset in category.assets)
                {
                    var downloader = GetDownloader();
                    yield return downloader.Download(assetUrlPath, asset.name, Hash128.Parse(asset.hash), toPath);
                }
                // encrpted assets.
                foreach (var asset in category.encryptedAssets)
                {
                    var downloader = GetDownloader();
                    yield return downloader.Download(assetUrlPath, asset.name, Hash128.Parse(asset.hash), toPath);
                }
            }

            // download rawAssets.
            foreach (var asset in manifest.rawAssets)
            {
                var downloader = GetDownloader();
                yield return downloader.Download(assetUrlPath, asset.name, Hash128.Parse(asset.hash), toPath);
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

        /// <summary>
        /// ファイルを1ディレクトリにまとめると性能劣化するので別ディレクトリに分散して保存する
        /// そのディレクトリ名を取得する
        /// </summary>
        public static string GetSubDirectoryPath(string filename)
        {
            return filename.Substring(0, 2);
        }
    }
}
