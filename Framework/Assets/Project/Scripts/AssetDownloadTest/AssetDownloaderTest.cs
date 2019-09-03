using Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Cannot load dependencies automatically!!!
/// 如果要使用AssetBrowser的话，必须
/// 1. 制定build asset的规则，比如把resource中的每一个文件当成一个asset，还必须保证不重名。（把路径包含到asset名中？）
/// 2. 在assets下载之后，把所有manifest文件都解析一遍，然后把所有的dependencies都保存在一个cache中
/// 3. 当通过文件名load asset时，先load当前asset，然后把所有dependencies的asset都load进来
/// </summary>
public class AssetDownloaderTest : MonoBehaviour
{
    private string _prefabName = "Prefabs/Views/View1";

#if UNITY_EDITOR_OSX
    public const string manifestUrlPath = "http://localhost:1234/manifests/";
    public const string assetUrlPath = "http://localhost:1234/AssetBundles/";
#else
    public const string manifestUrlPath = "http://localhost:4321/manifests/";
    public const string assetUrlPath = "http://localhost:4321/AssetBundles/";
#endif
    public string savePath = "";

    string localUri = "";

    [SerializeField]
    private Text informationText = null;

    [SerializeField]
    private Transform parent = null;

    private string[] loadFiles = new string[]
    {
        "Prefabs/Views/View1",
        "Prefabs/Views/View2",
        "Prefabs/Views/View3",
    };

    private void Awake()
    {
        savePath = Application.dataPath + "/AssetBundlesDownload/";
        localUri = "file:///" + Application.dataPath + "/AssetBundles/";

        AssetDownloader.instance.assetUrlPath = localUri;
        AssetDownloader.instance.savePath = savePath;
    }

    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(Framework.AssetDownloader.instance.DownloadManifestFromStreamAssets(manifestUrlPath, assetUrlPath, savePath));

        Framework.AssetDownloader.instance.onCompleted = () =>
        {
            informationText.text = "downloading completed.";
        };
    }

    public void DownloadToLocal()
    {
        //informationText.text = "downloading...";
        //Framework.AssetDownloader.instance.downloadToLocalPath = true;
        //StartCoroutine(Framework.AssetDownloader.instance.DownloadManifestFromStreamAssets(localUri, localUri, savePath));
    }

    public void DownloadToCache()
    {
        localUri = Application.streamingAssetsPath + "/AssetBundles/";
        informationText.text = "downloading...";
        Framework.AssetDownloader.instance.downloadToLocalPath = false;
        StartCoroutine(Framework.AssetDownloader.instance.DownloadManifestFromStreamAssets(localUri, localUri, savePath));
    }

    public void LoadFromFile()
    {
        DebugTimer timer = new DebugTimer("Load asset from local files");
        AssetDownloader.instance.savePath = Application.streamingAssetsPath + "/AssetBundles/";
        //Profiler.BeginSample("Load asset from local files");
        for (int i = 0; i < loadFiles.Length; ++i)
        {
            // sync
            var prefab = AssetManager.LoadFromFile<GameObject>(loadFiles[i]);
            if (prefab != null)
            {
                Instantiate(prefab, parent);
            }
        }
        //Profiler.EndSample();
        timer.Dispose();
    }

    public void LoadWithCoroutine()
    {
        AssetDownloader.instance.savePath = Application.streamingAssetsPath + "/AssetBundles/";
        var manifest = AssetDownloader.instance.scriptableManifest;
        StartCoroutine(LoadFromCache());
    }

    public IEnumerator LoadFromCache()
    {
        DebugTimer timer = new DebugTimer("Load asset with coroutine");
        //Profiler.BeginSample("Load asset with coroutine");
        int count = 0;
        for (int i = 0; i < loadFiles.Length; ++i)
        {
            GameObject prefab = null;
            // sync
            yield return (AssetManager.LoadCoroutine<GameObject>(loadFiles[i], (obj) =>
            {
                ++count;
                prefab = obj;
            }));
            if (prefab != null)
            {
                Instantiate(prefab, parent);
            }
        }
        yield return new WaitWhile(() => count < loadFiles.Length);
        //Profiler.EndSample();
        timer.Dispose();
    }

    public async void LoadFromCacheWithAwait()
    {
        AssetDownloader.instance.savePath = Application.streamingAssetsPath + "/AssetBundles/";
        var manifest = AssetDownloader.instance.scriptableManifest;
        DebugTimer timer = new DebugTimer("Load asset with coroutine");
        //Profiler.BeginSample("Load asset With Await");
        for (int i = 0; i < loadFiles.Length; ++i)
        {
            // sync
            var prefab = await AssetManager.LoadWithAwait<GameObject>(loadFiles[i]);
            if (prefab != null)
            {
                Instantiate(prefab, parent);
            }
        }
        //Profiler.EndSample();
        timer.Dispose();
    }

    public void ClearAll()
    {
        for (int i = 0; i < parent.childCount; ++i)
        {
            GameObject.Destroy(parent.GetChild(i));
        }


        AssetManager.instance.UnloadAll();
    }
}
