using Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITest : MonoBehaviour
{
    public Image image;

    // Start is called before the first frame update
    void Start()
    {
        CreatePopup();


    }

    void CreatePopup()
    {
        var testPopup = PopupBase.Create<TestPopup>("Prefabs/TestPopup", useAssetLoader: false);
        if (testPopup != null)
        {
            Debug.Log("Created test popup.");
        }
    }
}
