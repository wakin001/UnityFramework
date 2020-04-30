using Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OcclusionCullingTest : MonoBehaviour
{
    private Renderer rend;
    // Use this for initialization
    void Start()
    {
        rend = this.GetComponent<Renderer>();
    }

    void Update()
    {
        if (rend.isVisible)
        {
            Debug.LogError("red cube is Visible");
        }
        else
        {
            Debug.LogError("red cube not Visible"); ;
        }

        if (Camera.main.useOcclusionCulling)
        {
            Debug.Log("currrent occluson is using");
        }
    }
}
