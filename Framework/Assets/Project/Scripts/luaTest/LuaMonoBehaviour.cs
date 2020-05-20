using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XLua;

public class LuaMonoBehaviour : MonoBehaviour
{
    private LuaEnv luaEnv;

    void Awake()
    {
        luaEnv = new LuaEnv();
    }

    void OnDestory()
    {
        luaEnv.Dispose();
    }

    // Start is called before the first frame update
    void Start()
    {
        LoadLuaFile();
    }

    // Update is called once per frame
    void Update()
    {
    }

    void LoadLuaFile()
    {
        // method 1
        luaEnv.DoString("print('hello world')");

        // method 2
        luaEnv.DoString("require 'LuaScript'");
    }
}
