using Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        CreatePopup();
    }

    void CreatePopup()
    {
        var testPopup = PopupBase.Create<TestPopup>("Prefabs/TestPopup");
        if (testPopup != null)
        {
            Debug.Log("Created test popup.");
        }
    }
}
