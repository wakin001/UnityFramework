Shader "Cygames/Gluon/Character/GeometryDefault"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white"{}
        _Color("Color", Color) = (1,1,1,1)
        _ColorPower("Color Power" , Range(0,5)) = 1
        [Enum(None,0,Front,1,Back,2)] _Cull("Culling" , Int) = 2
        [Toggle] _LightProbeEnable("Light Probe Enable", Float) = 1.0
        _LightProbeImpact("Light Probe Impact" , Range(0.0,2.0)) = 1
        _LightProbeOffset("Light Probe Offset" , Range(-1,1)) = 0
        _LightProbeLuminanceMin("Light Probe Luminance Min" , Range(0,2)) = 0
        _LightProbeLuminanceMax("Light Probe Luminance Max" , Range(0,2)) = 2
        _LightProbeSaturationScale("Light Probe Saturation Scale", Range(0,1)) = 1
        _CoverColor("Cover Color", Color) = (1,1,1,1)
        _CoverColorPower("Cover Color Power" , Range(0,1)) = 0
        _NormalAdjustmentRimValue("Normal Adjustment Rim Value", Range(0,1)) = 1.0
        _LerpRimColor("Lerp Rim Color", Color) = (1,0,0,1)
        _LerpRimPower("Lerp Rim Power", Range(0.0,5.0)) = 1.0
        [Toggle] _LerpRimEnable("Lerp Rim Enable", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry"
        }
        
        Pass
        {
            Name "GEOMETRYDEFAULT"
            Tags
            {
                "LightMode" = "ForwardBase"
            }
            
            Blend One Zero
            ZWrite On
            ColorMask RGBA
            ZTest LEqual
            Cull [_Cull]
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
            #pragma multi_compile FOG_LINEAR FOG_EXP FOG_EXP2 _
            #pragma shader_feature DISABLE_LIGHTMAP _
            #pragma skip_variants LIGHTMAP_ON
            #pragma multi_compile_fwdbase

            //  キャラ調整機能
            #pragma shader_feature ENABLE_HEIGHT_COLOR ENABLE_NORMAL_ADJUSTMENTCOLOR ENABLE_NORMAL_ADJUSTMENTRIM ENABLE_HEIGHT_COLOR_DEBUG ENABLE_NORMAL_ADJUSTMENTCOLOR_DEBUG ENABLE_NORMAL_ADJUSTMENTRIM_DEBUG _
            #pragma multi_compile CHARACTER_SHADER_HIGH CHARACTER_SHADER_MIDDLE CHARACTER_SHADER_LOW
            
            #pragma vertex vertBase
            #pragma fragment fragBase
            
            #define _GLOSSYREFLECTIONS_OFF
            #define _SPECULARHIGHLIGHTS_OFF
            
            #define VERTEX_COLOR_CHARACOLOR
            #define HORIZONTAL_LIGHT_PROBE
            #define ENABLE_LERP_RIMCOLOR
            #define _COVER_COLOR

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
//  FOG
//  HorizontalLightProbe
//  LIGHTMAP_OFF
//  COVER_COLOR
//  </CustomTags>

