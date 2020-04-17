/////////////////////////////////////////////////////////////
//
//		DepthCasterEffectTypeAura.cginc
//
/////////////////////////////////////////////////////////////
#pragma exclude_renderers gles
#include "../../CustomCGIncludes/FuncEffectTypeAura.cginc"
// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices



struct v2f
{
    float4 pos		 : SV_POSITION;
    float2 depth	 : TEXCOORD0;
    half4 auraParam  : TEXCOORD1;
};



//-----------------------------------------
//	VertexShader
//-----------------------------------------
v2f VSMain(appdata_base v)
{
    v2f o;

    CalcAuraParam(v.vertex, v.normal, o.pos, o.auraParam);
    o.depth = 0.0f;
    UNITY_TRANSFER_DEPTH(o.depth);


    return o;
}




//-----------------------------------------
//	Fragment Shader
//-----------------------------------------
fixed4	PSMain(v2f i) : COLOR
{
    CalcAuraCutoutValue(i.auraParam);

    UNITY_OUTPUT_DEPTH(i.depth);
}
