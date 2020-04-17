using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Framework;

public class AIScriptTest : MonoBehaviour
{
    AIScript aiScript;

    private void Start()
    {
        aiScript = GetComponent<AIScript>();
    }

    // Start is called before the first frame update
    public void LoadAIScript()
    {
        aiScript.Create("AIScripts/BOS_0030101_02");
    }
}
