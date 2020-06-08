using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestDialog : MonoBehaviour
{
    [SerializeField]
    private Text infomation;

    [SerializeField]
    private Button okButton;

    [SerializeField]
    private Button cancelButton;

    // Start is called before the first frame update
    void Start()
    {
        MyFun(12, "wang");
    }

    // Update is called once per frame
    void Update()
    {

    }

    void MyFun(int a, string n)
    {

    }
}
