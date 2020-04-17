Shader "Cygames/Gluon/Background/GeometryDiffuseProbe"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white"{}
        _Color("Color", Color) = (1,1,1,1)
        _ColorPower("Color Power" , Range(0,5)) = 1
        [Enum(None,0,Front,1,Back,2)] _Cull("Culling" , Int) = 2
        _VertexColorImpact("_Vertex Color Impact" , Range(0,2)) = 1
		[Toggle] _LightProbeEnable("Light Probe Enable", Float) = 1.0
        _LightProbeImpact("Light Probe Impact" , Range(0.0,2.0)) = 1
        _LightProbeOffset("Light Probe Offset" , Range(-1,1)) = 0
        _LightProbeLuminanceMin("Light Probe Luminance Min" , Range(0,2)) = 0
        _LightProbeLuminanceMax("Light Probe Luminance Max" , Range(0,2)) = 2
        _LightProbeSaturationScale("Light Probe Saturation Scale", Range(0,1)) = 1
        [Space][Header(Detail Parameter)] _OffsetFactor("Depth Offset Factor" , Float) = 0
        _OffsetUnits("Depth Offset Units" , Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry"
        }
        
        Pass
        {
            Name "GEOMETRYDIFFUSEPROBE"
            Tags
            {
                "LightMode" = "ForwardBase"
            }
            
            Blend One Zero
            ZWrite On
            ColorMask RGBA
            ZTest LEqual
            Cull [_Cull]
            Offset [_OffsetFactor], [_OffsetUnits]
            Fog
            {
                Mode Off
            }
            
            CGPROGRAM
            #pragma target 3.0
            #define APL_SHADER_TARGET (20)

            #pragma skip_variants VERTEXLIGHT_ON
            #pragma skip_variants DYNAMICLIGHTMAP_ON
            #pragma skip_variants DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
            #pragma skip_variants SHADOWS_SOFT SHADOWS_NATIVE SHADOWS_SCREEN
            #pragma skip_variants SHADOWS_SOFT DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
            #pragma multi_compile FOG_LINEAR _
            #pragma shader_feature DISABLE_LIGHTMAP _
            #pragma skip_variants LIGHTMAP_ON
            #pragma multi_compile_fwdbase
            
            #pragma vertex vertBase
            #pragma fragment fragBase
            
            #define _GLOSSYREFLECTIONS_OFF
            #define _SPECULARHIGHLIGHTS_OFF
            
            #define ENABLE_VERTEX_COLOR
            #define	HORIZONTAL_LIGHT_PROBE

            #define ENABLE_GLOBAL_BG_COLOR
            
            #include "../../CustomCGIncludes/CustomUnityStandardCoreForward.cginc"
            ENDCG
            
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{ "LightMode" = "ShadowCaster" }
            
            Fog{ Mode Off }
            ZWrite On
            ZTest LEqual
            Cull[_Cull]
            
            CGPROGRAM
            #pragma vertex VSMain
            #pragma fragment PSMain
            #define SHADOWS_DEPTH
            #pragma fragmentoption ARB_precision_hint_fastest
            
            #define SHADOW_COLLECTOR_PASS

            #include "UnityCG.cginc"
            
            #include "../../CustomCGIncludes/OriginalDepthCaster.cginc"
            ENDCG
        }
    }
    
    FallBack Off
}

//  <CustomTags/>
//  BASE
//  VERTEX_COLOR_NORMAL
//  FOG
//  VERTEXLIGHT
//  LIGHTMAP_OFF
//  DISABLE_LIGHT_COLOR
//  DETAIL
//  </CustomTags>

