using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Framework
{
    public class AssetDownloader : SingletonComponent<AssetDownloader>
    {
        private string _assetUrlPath;
        private const string assetBundleManafestName = "build.manifest";

        public IEnumerator DownloadManifest(string urlPath, string assetUrlPath, string toPath)
        {
            _assetUrlPath = assetUrlPath;
            var downloader = new Downloader();
            yield return downloader.Download(urlPath + assetBundleManafestName, $"{toPath}/{assetBundleManafestName}");

            if (downloader.errorType == DownloadErrorType.None)
            {
                string manifestName = Path.GetFileNameWithoutExtension(assetBundleManafestName);
                AssetBundle manifestBundle = AssetBundle.LoadFromFile($"{toPath}/{manifestName}");
                AssetBundleManifest manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

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
                var downloader = new Downloader();
                yield return downloader.Download(_assetUrlPath + assetName, toPath + "AssetBundles/" + assetName);
            }
        }
    }
}
