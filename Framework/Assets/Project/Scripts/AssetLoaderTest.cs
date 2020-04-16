using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Framework;
using UnityEngine.SceneManagement;
using UnityEngine.Profiling;

public class AssetLoaderTest : MonoBehaviour
{
    private string[] loadFiles = new string[]
    {
        "Prefabs/Views/View1",
        "Prefabs/Views/View2",
        "Prefabs/Views/View3",
        //"Sprite/Tulips",
        //"AnimationCtrls/View1"
    };

    // Start is called before the first frame update
    void Start()
    {
        // set
        AssetDownloader.instance.assetUrlPath = AssetDownloaderTest.assetUrlPath;
        AssetDownloader.instance.savePath = Application.dataPath + "/../AssetBundlesDownload/";

        //OnButtonLoadTouched();
        //// async
        //AssetLoader.LoadAsync<GameObject>("view1", (prefab) =>
        //{
        //    if (prefab != null)
        //    {
        //        Instantiate(prefab);
        //    }
        //});

        //LoadWithAwait();
        StartCoroutine(LoadWithCoroutine());
        //LoadFromLocal();
    }

    public async void LoadWithAwait()
    {
        Profiler.BeginSample("Load asset With Await");
        for (int i = 0; i < loadFiles.Length; ++i)
        {
            // sync
            var prefab = await AssetManager.LoadWithAwait<GameObject>(loadFiles[i]);
            if (prefab != null)
            {
                Instantiate(prefab);
            }
        }
        Profiler.EndSample();
    }

    public IEnumerator LoadWithCoroutine()
    {
        Profiler.BeginSample("Load asset with coroutine");
        for (int i = 0; i < loadFiles.Length; ++i)
        {
            GameObject prefab = null;
            // sync
            yield return (AssetManager.LoadCoroutine<GameObject>(loadFiles[i], (obj) =>
            {
                prefab = obj;
            }));
            if (prefab != null)
            {
                Instantiate(prefab);
            }
        }
        Profiler.EndSample();
    }

    public void LoadFromLocal()
    {
        Profiler.BeginSample("Load asset from local files");
        for (int i = 0; i < loadFiles.Length; ++i)
        {
            // sync
            var prefab = AssetManager.Load<GameObject>(loadFiles[i]);
            if (prefab != null)
            {
                Instantiate(prefab);
            }
        }
        Profiler.EndSample();
    }

    public void OnButtonLoadTouched()
    {
        StartCoroutine(LoadWithCoroutine());
    }

    public void OnButtonUnLoadFalseTouched()
    {
        for (int i = 0; i < loadFiles.Length; ++i)
        {
            // sync
            AssetManager.instance.UnLoad(loadFiles[i]);
        }
    }
    public void OnButtonUnLoadTrueTouched()
    {
        for (int i = 0; i < loadFiles.Length; ++i)
        {
            // sync
            AssetManager.instance.UnLoad(loadFiles[i]);
        }
    }

    public void OnButtonUnLoadAllFalseTouched()
    {
        AssetBundle.UnloadAllAssetBundles(false);
    }

    public void OnButtonUnLoadAllTrueTouched()
    {
        AssetBundle.UnloadAllAssetBundles(true);
    }
}
