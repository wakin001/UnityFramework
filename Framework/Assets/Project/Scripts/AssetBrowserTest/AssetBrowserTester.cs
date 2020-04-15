using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Cannot load dependencies automatically!!!
/// 如果要使用AssetBrowser的话，必须
/// 1. 制定build asset的规则，比如把resource中的每一个文件当成一个asset，还必须保证不重名。（把路径包含到asset名中？）
/// 2. 在assets下载之后，把所有manifest文件都解析一遍，然后把所有的dependencies都保存在一个cache中
/// 3. 当通过文件名load asset时，先load当前asset，然后把所有dependencies的asset都load进来
/// </summary>
public class AssetBrowserTester : MonoBehaviour
{
    private string _bundleName = "view1";
    private string _prefabName = "View1";
#if UNITY_EDITOR_OSX
    private const string manifestUrlPath = "http://localhost:1234/";
    private const string assetUrlPath = "http://localhost:1234/";
#else
    private const string manifestUrlPath = "http://localhost/manifests/";
    private const string assetUrlPath = "http://localhost/AssetBundles/";
#endif
    //private string savePath = Application.dataPath + "/../AssetBundles/build/";
    private string savePath = "";

    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(LoadAssets("window1"));

        //string manifestPath = Application.dataPath + "/../AssetBundles/build/build";
        //string uri = "file:///" + Application.dataPath + "/../AssetBundles/build/" + _bundleName;
        //StartCoroutine(DownloadAndCacheAssetBundle(uri, manifestPath, _bundleName));
        savePath = Application.dataPath + "/../AssetBundles/test/";
        StartCoroutine(Framework.AssetDownloader.instance.DownloadManifest(manifestUrlPath, assetUrlPath, savePath));

        Framework.AssetDownloader.instance.onCompleted = () =>
        {
            StartCoroutine(Framework.AssetLoader.Load<GameObject>(_bundleName, (prefab) =>
            {
                if (prefab != null)
                {
                    Instantiate(prefab);
                }
            }));
        };
    }

    T LoadAsset<T>(string name) where T : UnityEngine.Object
    {
        AssetBundle asset = AssetBundle.LoadFromFile(savePath + name);
        return asset.LoadAsset<T>(name);
    }



    IEnumerator DownloadAndCacheAssetBundle(string uri, string manifestBundlePath, string bundleName)
    {
        //Load the manifest
        AssetBundle manifestBundle = AssetBundle.LoadFromFile(manifestBundlePath);
        AssetBundleManifest manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

        //Create new cache
        string today = DateTime.Today.ToLongDateString();
        Directory.CreateDirectory(today);
        Cache newCache = Caching.AddCache(today);

        //Set current cache for writing to the new cache if the cache is valid
        if (newCache.valid)
            Caching.currentCacheForWriting = newCache;

        //Download the bundle
        Hash128 hash = manifest.GetAssetBundleHash(bundleName);
        UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(uri, hash, 0);
        yield return request.SendWebRequest();
        AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);

        //Get all the cached versions
        List<Hash128> listOfCachedVersions = new List<Hash128>();
        Caching.GetCachedVersions(bundle.name, listOfCachedVersions);

        GameObject assetObj = AssetBundleContainsAssetIWantToLoad(bundle);
        if (assetObj == null)     //Or any conditions you want to check on your new asset bundle
        {
            //If our criteria wasn't met, we can remove the new cache and revert back to the most recent one
            Caching.currentCacheForWriting = Caching.GetCacheAt(Caching.cacheCount);
            Caching.RemoveCache(newCache);

            for (int i = listOfCachedVersions.Count - 1; i > 0; i--)
            {
                //Load a different bundle from a different cache
                request = UnityWebRequestAssetBundle.GetAssetBundle(uri, listOfCachedVersions[i], 0);
                yield return request.SendWebRequest();
                bundle = DownloadHandlerAssetBundle.GetContent(request);

                //Check and see if the newly loaded bundle from the cache meets your criteria
                if (AssetBundleContainsAssetIWantToLoad(bundle))
                    break;
            }
        }
        else
        {
            //This is if we only want to keep 5 local caches at any time
            if (Caching.cacheCount > 5)
                Caching.RemoveCache(Caching.GetCacheAt(1));     //Removes the oldest user created cache

            GameObject.Instantiate(assetObj);
        }
    }

    GameObject AssetBundleContainsAssetIWantToLoad(AssetBundle bundle)
    {
        return bundle.LoadAsset<GameObject>(_prefabName);     //this could be any conditional
    }
}
