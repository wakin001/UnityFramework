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
            listOfCachedVersions.Clear();
            Caching.GetCachedVersions(name, listOfCachedVersions);

            if (listOfCachedVersions.Contains(hash))
            {   // aleady cached.
                yield break;
            }

            string uri = path + name;
            UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(uri, hash, 0);
            yield return request.SendWebRequest();

            if (!request.isHttpError && !request.isNetworkError)
            {
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);
                if (!IsAssetBundleContainsAsset(bundle, name))     //Or any conditions you want to check on your new asset bundle
                {
                    //If our criteria wasn't met, we can remove the new cache and revert back to the most recent one
                    //Caching.currentCacheForWriting = Caching.GetCacheAt(Caching.cacheCount);
                    //Caching.RemoveCache(newCache);

                    for (int i = listOfCachedVersions.Count - 1; i > 0; i--)
                    {
                        //Load a different bundle from a different cache
                        request = UnityWebRequestAssetBundle.GetAssetBundle(uri, listOfCachedVersions[i], 0);
                        yield return request.SendWebRequest();
                        bundle = DownloadHandlerAssetBundle.GetContent(request);

                        //Check and see if the newly loaded bundle from the cache meets your criteria
                        if (IsAssetBundleContainsAsset(bundle, name))
                        {
                            // remove other caches.
                            Caching.ClearOtherCachedVersions(name, listOfCachedVersions[i]);
                            break;
                        }
                    }
                }
                else
                {
                    // remove other caches.
                    Caching.ClearOtherCachedVersions(name, hash);
                }
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

            bool IsAssetBundleContainsAsset(AssetBundle bundle, string assetName)
            {
                return bundle.LoadAsset<GameObject>(assetName) != null;     //this could be any conditional
            }
        }
    }

}
