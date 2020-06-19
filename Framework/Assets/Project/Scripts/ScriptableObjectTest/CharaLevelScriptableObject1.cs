using System;
using System.Collections.Generic;
using UnityEngine;

public class CharaLevelScriptableObject : ScriptableObject
{
    public List<CharaLevelElementScriptableObject> items;
}

[Serializable]
public class CharaLevelElementScriptableObject
{
    public int id;
    public int necessary_exp;
    public int total_exp;
}