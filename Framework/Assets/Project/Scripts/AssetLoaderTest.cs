using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Framework;
using UnityEngine.SceneManagement;

public class AssetLoaderTest : MonoBehaviour
{
    private string[] loadFiles = new string[]
    {
        //"Prefabs/Views/View1",
        //"Prefabs/Views/View2",
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

        OnButtonLoadTouched();
        //// async
        //AssetLoader.LoadAsync<GameObject>("view1", (prefab) =>
        //{
        //    if (prefab != null)
        //    {
        //        Instantiate(prefab);
        //    }
        //});


    }

    public void OnButtonLoadTouched()
    {
        for (int i = 0; i < loadFiles.Length; ++i)
        {
            // sync
            AssetManager.Load<GameObject>(loadFiles[i], (prefab) =>
            {
                if (prefab != null)
                {
                    Instantiate(prefab);
                }
            });
        }
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
