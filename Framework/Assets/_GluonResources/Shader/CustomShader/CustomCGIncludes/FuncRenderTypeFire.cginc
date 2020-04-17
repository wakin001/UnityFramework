///////////////////////////////////////////////////////////////////////////////
//
//      FuncRenderTypeFire.cginc
//
///////////////////////////////////////////////////////////////////////////////
#ifndef _FUNC_RENDER_TYPE_FIRE_CGINC_
#define _FUNC_RENDER_TYPE_FIRE_CGINC_
#if defined(RANDERTYPE_FIRE)


#include "../../CustomShaderCommon.cginc"



sampler2D _FireMaskTex;
float4 _FireMaskTex_ST;
sampler2D _FireRampTex;
float4 _FireRampTex_ST;
sampler2D _FireDistortionTex;
float4 _FireDistortionTex_ST;
sampler2D _FirePalletTex;

uniform float4 _FireScrollMaskColor;     //  xy Mask用　zw Color用
uniform float4 _FireScrollDst;           //  xy Dst01用　zw Dst02用
uniform float2 _FireDistortionPower;
uniform float2 _FireMaskSaturateFlag;


//--------------------------------------------------
//  炎用の頂点シェーダのパラメータを作成する
//--------------------------------------------------
void CalcFireVertexParam(float2 uv0, float2 uv1, float2 uv2, out float4 fireuv, out float4 firedistuv)
{
    fireuv.xy = uv0;
    fireuv.zw = uv1;
    fireuv = fireuv + _Time.y * _FireScrollMaskColor;
    firedistuv = uv2.xyxy + _Time.y * _FireScrollDst;

    float2 saturate_uv;

    fireuv.xy = TRANSFORM_TEX(fireuv.xy, _FireMaskTex);
    saturate_uv = saturate(fireuv.xy); //	ここでclampモードにする
    fireuv.xy = lerp(fireuv.xy, saturate_uv, _FireMaskSaturateFlag.xy); //	ここをオン・オフできる

    fireuv.zw = TRANSFORM_TEX(fireuv.zw, _FireRampTex);
    firedistuv = TRANSFORM_TEX2(firedistuv, _FireDistortionTex);
}




//----------------------------------------------------
//  炎用のフラグメントシェーダのパラメータを作成する
//----------------------------------------------------
half CalcFireFragmentParam(float4 in_fireuv, float4 firedistuv, out float4 out_fireuv)
{
    //歪みテクスチャ取得
    float2 distortion;

    distortion.x = tex2D(_FireDistortionTex, firedistuv.xy).r;
    distortion.y = tex2D(_FireDistortionTex, firedistuv.zw).r;
    distortion = (distortion - 0.5) * _FireDistortionPower;
                
    out_fireuv = in_fireuv.xyzw + distortion.xxyy;
    
    //本体部分黒抜き
    half alpha = tex2D(_FireMaskTex, out_fireuv.xy).r;
    clip((float) (alpha - 0.95f));

    return alpha;
}




//----------------------------------------------------
//  炎用のフラグメントシェーダのパラメータを作成する
//----------------------------------------------------
half3 CalcFireFragmentColor(float4 fireUV, float4 fireDistUV)
{
    half alpha = CalcFireFragmentParam(fireUV, fireDistUV, fireUV);
    
    //  カラーパレットから色を取得
    half grayscale = tex2D(_FireRampTex, fireUV.zw).r;
    grayscale = grayscale + alpha;
    half pallet_uv = float2(grayscale, 0.5);
    half3 albedo = tex2D(_FirePalletTex, pallet_uv).rgb;

    return albedo;
}


#endif
#endif
