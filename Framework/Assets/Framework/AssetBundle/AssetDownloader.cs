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
    /// Use Unity Caching system to cache assetbundles.
    /// </summary>
    public class AssetDownloader : SingletonComponent<AssetDownloader>
    {
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

        // download file's hashs
        private HashSet<ScriptableManifest.RawAsset> downloadList = new HashSet<ScriptableManifest.RawAsset>();

        public IEnumerator DownloadManifest(string urlPath, string assetUrlPath, string toPath)
        {
            this.assetUrlPath = assetUrlPath;
            this.savePath = toPath;

            // remove old files.
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
                yield return DownloadAllAssets(assetUrlPath, toPath, scriptableManifest);
            }
            else
            {
                Debug.LogError("Error in downloading manifest file: " + downloader.error);
            }
        }

        public IEnumerator DownloadAllAssets(string urlPath, string toPath, ScriptableManifest manifest)
        {
            if (manifest == null)
            {
                Debug.LogError("manifest is null in " + toPath);
                yield break;
            }

            if (!Caching.ready)
            {
                yield return null;
            }

            // get all file paths to download.
            downloadList.Clear();

            // download all categories.
            for (int i = 0; i < manifest.categories.Length; ++i)
            {
                var category = manifest.categories[i];
                // assets.
                foreach (var asset in category.assets)
                {
                    Hash128 hash = Hash128.Parse(asset.hash);
                    if (!Caching.IsVersionCached($"{assetUrlPath}{asset.name}", hash))
                    {
                        downloadList.Add(asset);
                    }
                }
                // encrpted assets.
                foreach (var asset in category.encryptedAssets)
                {
                    Hash128 hash = Hash128.Parse(asset.hash);
                    if (!Caching.IsVersionCached($"{assetUrlPath}{asset.name}", hash))
                    {
                        downloadList.Add(asset);
                    }
                }
            }

            // download rawAssets.
            foreach (var asset in manifest.rawAssets)
            {
                Hash128 hash = Hash128.Parse(asset.hash);
                if (!Caching.IsVersionCached($"{assetUrlPath}{asset.name}", hash))
                {
                    downloadList.Add(asset);
                }
            }

            // download
            Debug.Log("download count: " + downloadList.Count);
            foreach (var asset in downloadList)
            {
                var downloader = new CacheDownloader();
                StartCoroutine(downloader.Download(assetUrlPath, asset.name, Hash128.Parse(asset.hash), toPath, (error, errorType, hash) =>
                {
                    if (errorType != DownloadErrorType.None)
                    {
                        Debug.Log("Error in downloading ... " + error);
                        return;
                    }
                    downloadList.RemoveWhere((item) =>
                    {
                        return (item.hash == hash);
                    });
                }));
            }

            yield return new WaitWhile(() => downloadList.Count > 0);

            if (onCompleted != null)
            {
                onCompleted.Invoke();
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
