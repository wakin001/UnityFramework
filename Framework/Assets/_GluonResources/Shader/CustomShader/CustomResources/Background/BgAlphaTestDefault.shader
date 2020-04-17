Shader "Cygames/Gluon/Background/AlphaTestDefault"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white"{}
        _Color("Color", Color) = (1,1,1,1)
        _ColorPower("Color Power" , Range(0,5)) = 1
        [Enum(None,0,Front,1,Back,2)] _Cull("Culling" , Int) = 2
        _OptionTex0("OpTex0 R=Alpha G=---- B=----", 2D) = "white"{}
        _Cutoff("Alpha Test" , Range(0,1)) = 0.5
        _VertexColorImpact("_Vertex Color Impact" , Range(0,2)) = 1
        [Space][Header(Detail Parameter)] _OffsetFactor("Depth Offset Factor" , Float) = 0
        _OffsetUnits("Depth Offset Units" , Float) = -1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry" 
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }
        
        Pass
        {
            Name "ALPHATESTDEFAULT"
            Tags
            {
                "LightMode" = "ForwardBase"
            }
            
            Blend One Zero
            ZWrite On
            ColorMask RGBA    // chujo 機種端末依存対応のためalphaを書き込む
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

            /*#pragma skip_variants VERTEXLIGHT_ON
            #pragma skip_variants DYNAMICLIGHTMAP_ON
            #pragma skip_variants DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
            #pragma skip_variants SHADOWS_SOFT SHADOWS_NATIVE SHADOWS_SCREEN
            #pragma skip_variants SHADOWS_SOFT DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
            #pragma multi_compile FOG_LINEAR _
            #pragma shader_feature DISABLE_LIGHTMAP _
            #pragma skip_variants LIGHTMAP_OFF
            #pragma multi_compile_fwdbase*/
            
            #pragma vertex vertBase
            #pragma fragment fragBase
            
            #define _GLOSSYREFLECTIONS_OFF
            #define _SPECULARHIGHLIGHTS_OFF
            
            #define SHADER_BLEND_ALHPHATEST
            #define _ALPHATEST_ON           //  アルファテスト
            #define VERTEX_COLOR_BACKFADE

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
            #define _ALPHATEST_ON

            #include "UnityCG.cginc"
            
            #include "../../CustomCGIncludes/OriginalDepthCaster.cginc"
            ENDCG
        }
    }
    
    FallBack Off
}

//  <CustomTags/>
//  BASE
//  ALPHA_TEST
//  VERTEX_COLOR_BACKFADE
//  FOG
//  LIGHTMAP_ON
//  DETAIL
//  </CustomTags>

