using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XLua;

public class LuaMonoBehaviour : MonoBehaviour
{
    public TextAsset mainLuaScript;

    public Transform canvas;

    private LuaEnv luaEnv;

    private LuaTable scriptEnv;

    private Action luaStart;
    private Action luaUpdate;
    private Action luaOnDestroy;


    void Awake()
    {
        luaEnv = new LuaEnv();

        scriptEnv = luaEnv.NewTable();

        // 为每个脚本设置一个独立的环境，可一定程度上防止脚本间全局变量、函数冲突
        LuaTable meta = luaEnv.NewTable();
        meta.Set("__index", luaEnv.Global);
        scriptEnv.SetMetaTable(meta);
        meta.Dispose();

        scriptEnv.Set("self", this);

        luaEnv.DoString(mainLuaScript.text, "LuaGlobalEnv", scriptEnv);

        Action luaAwake = scriptEnv.Get<Action>("awake");
        scriptEnv.Get("start", out luaStart);
        scriptEnv.Get("update", out luaUpdate);
        scriptEnv.Get("ondestroy", out luaOnDestroy);
    }

    void OnDestory()
    {
        if (luaOnDestroy != null)
        {
            luaOnDestroy();
        }
        luaEnv.Dispose();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (luaStart != null)
        {
            luaStart();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (luaUpdate != null)
        {
            luaUpdate();
        }
    }

    void LoadLuaFile()
    {
        // method 1
        luaEnv.DoString("print('hello world')");

        // method 2
        luaEnv.DoString("require 'LuaMain'");
    }
}
