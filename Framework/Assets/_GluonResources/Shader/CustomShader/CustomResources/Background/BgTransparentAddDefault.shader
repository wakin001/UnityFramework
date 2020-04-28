Shader "Cygames/Gluon/Background/TransparentAddDefault"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white"{}
        _Color("Color", Color) = (1,1,1,1)
        _ColorPower("Color Power" , Range(0,5)) = 1
        [Enum(None,0,Front,1,Back,2)] _Cull("Culling" , Int) = 2
        _VertexColorImpact("Vertex Color Impact" , Range(0,2)) = 1
        _ProgrammableColor("Programmable Color", Color) = (1,1,1,1)
        [Space] [Header(Detail Parameter)] _OffsetFactor("Depth Offset Factor" , Float) = 0
        _OffsetUnits("Depth Offset Units" , Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }
        
        Pass
        {
            Name "TRANSPARENTADDDEFAULT"
            Tags
            {
            }
            
            Blend One One
            ZWrite Off
            ColorMask RGB
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
            #pragma multi_compile FOG_LINEAR FOG_EXP FOG_EXP2 _
            #pragma multi_compile_fwdbase
            
            #pragma vertex vertBase
            #pragma fragment fragBase
            
            #define _GLOSSYREFLECTIONS_OFF
            #define _SPECULARHIGHLIGHTS_OFF
            
            #ifndef UNITY_NO_FULL_STANDARD_SHADER
            #define UNITY_NO_FULL_STANDARD_SHADER
            #endif
            #define SHADER_BLEND_TRANSPARENTADD
            #define DISABLE_LIGHT_COLOR
            #define ENABLE_VERTEX_COLOR
            #define ENABLE_PROGRAMMABLE_COLOR
            
            #define ENABLE_GLOBAL_BG_COLOR

            #include "../../CustomCGIncludes/CustomUnityStandardCoreForward.cginc"
            ENDCG
            
        }
    }
    
    FallBack Off
}

//  <CustomTags/>
//  BASE
//  UNITY_NO_FULL_STANDARD_SHADER
//  DISABLE_LIGHT_COLOR
//  VERTEX_COLOR_NORMAL
//  FOG
//  ProgrammableColor
//  DETAIL
//  </CustomTags>


