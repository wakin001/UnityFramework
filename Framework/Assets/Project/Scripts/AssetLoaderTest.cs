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

        for (int i = 0; i < loadFiles.Length; ++i)
        {
            // sync
            AssetManager.LoadAsync<GameObject>(loadFiles[i], (prefab) =>
            {
                if (prefab != null)
                {
                    Instantiate(prefab);
                }
            });
        }
        //// async
        //AssetLoader.LoadAsync<GameObject>("view1", (prefab) =>
        //{
        //    if (prefab != null)
        //    {
        //        Instantiate(prefab);
        //    }
        //});


    }

}
