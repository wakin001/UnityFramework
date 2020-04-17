/////////////////////////////////////////////////////////////
//
//		DepthCasterRenderTypeFire.cginc
//
/////////////////////////////////////////////////////////////
#pragma exclude_renderers gles
#include "../../CustomCGIncludes/FuncRenderTypeFire.cginc"
// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices



struct appdata
{
    float4 pos : POSITION;
    half3 color : COLOR;
    half2 uv0 : TEXCOORD0;
    half2 uv1 : TEXCOORD1;
    half2 uv2 : TEXCOORD2;
};


struct v2f_shadow
{
    float4 pos : SV_POSITION;
    float2 depth : TEXCOORD0;
    half4 fireuv : TEXCOORD1;
    half4 firedistuv : TEXCOORD2;
};




//-----------------------------------------
//	VertexShader
//-----------------------------------------
v2f_shadow VSMain(appdata v)
{
    v2f_shadow o;

    o.pos = UnityObjectToClipPos(v.pos);
    o.depth = 0.0f;
    UNITY_TRANSFER_DEPTH(o.depth);

    CalcFireVertexParam(v.uv0, v.uv1, v.uv2, o.fireuv, o.firedistuv);

    return o;
}




//-----------------------------------------
//	Fragment Shader
//-----------------------------------------
half4 PSMain(v2f_shadow i) : COLOR
{
    CalcFireFragmentParam(i.fireuv, i.firedistuv, i.fireuv);

    UNITY_OUTPUT_DEPTH(i.depth);
}
