using Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestPopup : PopupBase
{
    [SerializeField]
    private Text title = null;

    // Start is called before the first frame update
    void Start()
    {
        title.text = "This is test title";
    }
}
