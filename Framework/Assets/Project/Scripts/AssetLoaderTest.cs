using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Framework;

public class AssetLoaderTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(AssetLoader.LoadAsync<GameObject>("view1", (prefab) =>
        {
            if (prefab != null)
            {
                Instantiate(prefab);
            }
        }));
    }

}
