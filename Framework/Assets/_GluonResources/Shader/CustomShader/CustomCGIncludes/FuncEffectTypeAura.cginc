///////////////////////////////////////////////////////////////////////////////
//
//      FuncEffectTypeAura.cginc
//
///////////////////////////////////////////////////////////////////////////////
#ifndef _FUNC_EFFECT_TYPE_AURA_CGINC_
#define _FUNC_EFFECT_TYPE_AURA_CGINC_
#if defined(EFFECTTYPE_AURA)
#include "../../CustomShaderCommon.cginc"

uniform float		_AuraSize;
uniform float		_AuraFalloff;
uniform float		_AuraNoiseUvScaleU;
uniform float		_AuraNoiseUvScaleV;
uniform float		_AuraNoiseUvSpeedU;
uniform float		_AuraNoiseUvSpeedV;
uniform float		_AuraNoiseUvSpeedScaleDistortion;
uniform float		_AuraNoiseUvSpeedScaleBase;
uniform float		_AuraNoisePeriod;
uniform float		_AuraDistortionU;
uniform float		_AuraDistortionV;
uniform float		_AuraCutoutThreshold;
uniform float		_AuraGradiantLerpValue;
uniform sampler2D	_AuraNoiseTex;
uniform sampler2D	_AuraGradiantTex;




//--------------------------------------------------------------
//  オーラエフェクト用のパラメータを計算する
//--------------------------------------------------------------
void CalcAuraParam(half3 vertex, half3 normal, out half4 pos, out half4 auraParam)
{
    float3 posLocal = vertex.xyz + (normal.xyz * _AuraSize);
    float4 posWorld = mul(unity_ObjectToWorld, float4(posLocal, 1.0));
    half3 norWorld = UnityObjectToWorldNormal(normal);

    pos = UnityObjectToClipPos(float4(posLocal, 1.0));
    auraParam.xy = pos.xy / pos.w;

#if defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
    auraParam.y = -auraParam.y;
#endif

    half3 normalWorld = normalize(norWorld);
    half3 eye = posWorld.xyz - _WorldSpaceCameraPos;

    half NdotE = abs(dot(normalWorld, normalize(eye)));

    auraParam.z = 1.0 - shlick(1.0 - NdotE, _AuraFalloff);
    auraParam.w = length(eye);
}




//--------------------------------------------------------------
//  オーラエフェクト用のカットアウト値を取得する
//--------------------------------------------------------------
half CalcAuraCutoutValue(half4 auraParam)
{
    half2 uv = auraParam.xy;
    half2 uv_screensize = _ScreenParams.xy / _ScreenParams.yy;
    half2 uv_scale = half2(_AuraNoiseUvScaleU, _AuraNoiseUvScaleV);

    //	ノイズの作成
    float4 NoiseUV_RG;
    float2 NoiseUV_B;
    float4 SinNoize;
    float2 uv_offset = frac(float2(_AuraNoiseUvSpeedU, _AuraNoiseUvSpeedV) * _Time.yy);

    uv *= uv_screensize;
    uv *= auraParam.w / abs(COT_FOV_DIV_2);
    NoiseUV_B = uv * uv_scale;

    NoiseUV_RG = (NoiseUV_B + uv_offset * _AuraNoiseUvSpeedScaleDistortion).xyxy;
    NoiseUV_B += uv_offset * _AuraNoiseUvSpeedScaleBase;

    SinNoize = sin(_Time + uv.y * 5);
    NoiseUV_RG.xz += SinNoize.wy * SinNoize.zx * _AuraNoisePeriod;
        
    float Noise_R = tex2D(_AuraNoiseTex, NoiseUV_RG.xy).r;
    float Noise_G = tex2D(_AuraNoiseTex, NoiseUV_RG.zw).g;
    half noise = tex2D(_AuraNoiseTex, NoiseUV_B + float2(Noise_R, Noise_G) * float2(_AuraDistortionU, _AuraDistortionV)).b;

    //	描画判定を行う
    half Value = auraParam.z * noise * noise;
    half FinalValue = Value - _AuraCutoutThreshold;

    clip((float) FinalValue);

    return FinalValue;
}




#endif
#endif
