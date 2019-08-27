using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Framework
{
    public class CacheDownloader : IDownloader
    {
        public string error { get; private set; } = null;

        public DownloadErrorType errorType { get; private set; } = DownloadErrorType.None;

        private List<Hash128> listOfCachedVersions = new List<Hash128>();

        IEnumerator IDownloader.Download(string path, string name, Hash128 hash, string toPath)
        {
            // Wait for caching is ready.
            if (!Caching.ready)
            {
                yield return null;
            }
            string uri = path + name;
            if (Caching.IsVersionCached(uri, hash))
            {
                // aleady cached.
                Debug.Log("Find cache for " + name);
                yield break;
            }

            Debug.Log("Cannot find cache for " + name + ", then download..");

            UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(uri, hash, 0);
            yield return request.SendWebRequest();

            if (!request.isHttpError && !request.isNetworkError)
            {
                // remove other caches.
                Caching.ClearOtherCachedVersions(name, hash);
            }
            else
            {
                error = request.error;
                if (request.error.Contains("timeout"))
                {
                    errorType = DownloadErrorType.TimeoutError;
                }
                else
                {
                    errorType = DownloadErrorType.ConnectError;
                }
            }
        }
    }

}
