using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowTest : MonoBehaviour
{
    public Camera m_ShadowCamera;
    private RenderTexture m_RT;
    public Material m_Mat;

    void Start()
    {
        //创建一个纹理，用来捕获照相机的看到的（该纹理既为我们需要的影子的源纹理）
        m_RT = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        m_RT.anisoLevel = 8;
        m_ShadowCamera.targetTexture = m_RT;
        //将上面的纹理传入材质球中，用来将它处理为影子
        m_Mat.SetTexture("_MainTex", m_RT);
    }

    void Update()
    {
        //实时将生成影子需要的纹理的照相机的矩阵传入材质球，这两个矩阵在下面的着色器代码中使用
        m_Mat.SetMatrix("_WorldToCameraMatrix", m_ShadowCamera.worldToCameraMatrix);
        m_Mat.SetMatrix("_ProjectionMatrix", m_ShadowCamera.projectionMatrix);
    }
}
