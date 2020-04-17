#ifndef UNITY_STANDARD_CORE_FORWARD_SIMPLE_INCLUDED
#define UNITY_STANDARD_CORE_FORWARD_SIMPLE_INCLUDED

#include "../../CustomCGIncludes/CustomUnityStandardCore.cginc"
#include "../../CustomCGIncludes/CustomFunctions.cginc"
#include "../../CustomCGIncludes/FuncEffectTypeAura.cginc"




#if defined(_SPECULARHIGHLIGHTS_OFF) && !defined(_NORMALMAP)
#define	DISABLE_NORMAL
#endif

//  Does not support: _PARALLAXMAP, DIRLIGHTMAP_COMBINED, DIRLIGHTMAP_SEPARATE
#define GLOSSMAP (defined(_SPECGLOSSMAP) || defined(_METALLICGLOSSMAP))

#ifndef SPECULAR_HIGHLIGHTS
#define SPECULAR_HIGHLIGHTS (!defined(_SPECULAR_HIGHLIGHTS_OFF))
#endif




struct VertexOutputBaseSimple
{
	float4 pos							: SV_POSITION;
#ifdef ENABLE_VERTEX_COLOR
	half4	color						: COLOR;
#endif

	float4 tex							: TEXCOORD0;

#if defined(EFFECTTYPE_AURA)
	float4	auraParam					: TEXCOORD1;	//	xy=wpos.xy z=falloff w=eye length
	half4 ambientOrLightmapUV			: TEXCOORD2; // SH or Lightmap UV

	UNITY_FOG_COORDS_PACKED(4, half4) // x: fogCoord, yzw: reflectVec
#if UNITY_SPECCUBE_BOX_PROJECTION || UNITY_LIGHT_PROBE_PROXY_VOLUME
		float3 posWorld					: TEXCOORD8;
#endif
#else
	half4 eyeVec 						: TEXCOORD1; // w: grazingTerm

	half4 ambientOrLightmapUV			: TEXCOORD2; // SH or Lightmap UV
	SHADOW_COORDS(3)
		UNITY_FOG_COORDS_PACKED(4, half4) // x: fogCoord, yzw: reflectVec

#if !defined(DISABLE_NORMAL)
		half4 normalWorld				: TEXCOORD5; // w: fresnelTerm
#endif


/*#ifdef _NORMALMAP
	half3 tangentSpaceLightDir		: TEXCOORD6;
	#if SPECULAR_HIGHLIGHTS
		half3 tangentSpaceEyeVec	: TEXCOORD7;
	#endif
#endif
*/
#if UNITY_SPECCUBE_BOX_PROJECTION || UNITY_LIGHT_PROBE_PROXY_VOLUME
	float3 posWorld					: TEXCOORD8;
#endif

#if defined(RANDERTYPE_FIRE)
	float4 fireUV                   : TEXCOORD9;
	float4 fireDistUV               : TEXCOORD10;
#endif
#ifdef ENABLE_LERP_RIMCOLOR
	half4	lerpRimColor			: TEXCOORD6;
#endif
#if defined(ENABLE_SUB_TEXTURE)
	half2 texcoordSub				: TEXCOORD7;
#endif

#endif
};

// UNIFORM_REFLECTIVITY(): workaround to get (uniform) reflecivity based on UNITY_SETUP_BRDF_INPUT
half MetallicSetup_Reflectivity()
{
	return 1.0h - OneMinusReflectivityFromMetallic(_Metallic);
}

half SpecularSetup_Reflectivity()
{
#if !defined(DISABLE_LIGHT_COLOR)
	return SpecularStrength(_SpecColor.rgb);
#else
	return 0.0f;
#endif
}

#define JOIN2(a, b) a##b
#define JOIN(a, b) JOIN2(a,b)
#define UNIFORM_REFLECTIVITY JOIN(UNITY_SETUP_BRDF_INPUT, _Reflectivity)


#ifdef _NORMALMAP

half3 TransformToTangentSpace(half3 tangent, half3 binormal, half3 normal, half3 v)
{
	// Mali400 shader compiler prefers explicit dot product over using a half3x3 matrix
	return half3(dot(tangent, v), dot(binormal, v), dot(normal, v));
}

void TangentSpaceLightingInput(half3 normalWorld, half4 vTangent, half3 lightDirWorld, half3 eyeVecWorld, out half3 tangentSpaceLightDir, out half3 tangentSpaceEyeVec)
{
	half3 tangentWorld = UnityObjectToWorldDir(vTangent.xyz);
	half sign = half(vTangent.w) * half(unity_WorldTransformParams.w);
	half3 binormalWorld = cross(normalWorld, tangentWorld) * sign;
	tangentSpaceLightDir = TransformToTangentSpace(tangentWorld, binormalWorld, normalWorld, lightDirWorld);
#if SPECULAR_HIGHLIGHTS
	tangentSpaceEyeVec = normalize(TransformToTangentSpace(tangentWorld, binormalWorld, normalWorld, eyeVecWorld));
#else
	tangentSpaceEyeVec = 0;
#endif
}

#endif // _NORMALMAP






VertexOutputBaseSimple vertForwardBaseSimple(VertexInput v)
{
	UNITY_SETUP_INSTANCE_ID(v);


#ifdef ENABLE_VERTEX_WAVE
	//頂点揺れ
	float	wave_time = _Time.z * _WaveSpeed;
	float2	vec2;

	vec2.xy = v.vertex.xz * _WaveFreq - wave_time;

	vec2 = sin(vec2) * _WaveSize;
	v.vertex.y -= vec2.x + vec2.y;
	//頂点揺れ
#endif

#ifdef ENABLE_GRASS_WAVE
	//草揺れ
	v.vertex.xz = v.vertex.xz + _GrassWaveDirection.xz*(sin((_Time.w - _GrassWaveStartTime.w + v.uv1.y*_GrassWaveOffsetRate) * _GrassWaveSpeed) * _GrassWavePower * (v.uv2.x * v.uv2.x));
	//草揺れ
#endif

	VertexOutputBaseSimple o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputBaseSimple, o);
	o.ambientOrLightmapUV = 0;


	//	オーラタイプのとき
#if defined(EFFECTTYPE_AURA)
	CalcAuraParam(v.vertex, v.normal, o.pos, o.auraParam);

#else	
	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);

#if UNITY_SPECCUBE_BOX_PROJECTION || UNITY_LIGHT_PROBE_PROXY_VOLUME	
	o.posWorld = posWorld.xyz;
#endif	
	o.pos = UnityObjectToClipPos(v.vertex);

	half3 eyeVec = normalize(posWorld.xyz - _WorldSpaceCameraPos);
	half3 normalWorld = UnityObjectToWorldNormal(v.normal);

#if !defined(DISABLE_NORMAL)
	//法線
	o.normalWorld.xyz = normalWorld;
	o.eyeVec.xyz = eyeVec;

#ifdef _NORMALMAP
	half3 tangentSpaceEyeVec;
	TangentSpaceLightingInput(normalWorld, v.tangent, _WorldSpaceLightPos0.xyz, eyeVec, o.tangentSpaceLightDir, tangentSpaceEyeVec);
#if SPECULAR_HIGHLIGHTS
	o.tangentSpaceEyeVec = tangentSpaceEyeVec;
#endif
#endif

	//We need this for shadow receiving
	TRANSFER_SHADOW(o);

	o.ambientOrLightmapUV = VertexGIForward(v, posWorld, normalWorld);

	o.fogCoord.yzw = reflect(eyeVec, normalWorld);

	o.normalWorld.w = Pow4(1 - DotClamped(normalWorld, -eyeVec)); // fresnel term
#if !GLOSSMAP
	o.eyeVec.w = saturate(_Glossiness + UNIFORM_REFLECTIVITY()); // grazing term
#endif
//法線
#endif
#endif



#ifdef USE_SILHOUETTE_COLOR
	//シルエットカラー
	o.tex = ComputeScreenPos(o.pos);
	o.tex.xy /= o.tex.w;
#else
	o.tex = TexCoords(v);
	//シルエットカラー
#endif


#if defined(USE_FOG_LENGTH_RATE)
	UNITY_TRANSFER_FOG_RATE(o, o.pos, _FogLengthRate);
#else
	UNITY_TRANSFER_FOG(o, o.pos);
#endif

#if defined(ENABLE_VERTEX_COLOR)
#if defined(VERTEX_COLOR_NORMAL) || defined(VERTEX_COLOR_MEDIANHALF)
	o.color = CalcColorImpact(v.color, _VertexColorImpact);
#else
	o.color = v.color;
#endif
#endif

#if defined(ENABLE_SUB_TEXTURE)
	o.texcoordSub = TRANSFORM_TEX(v.uv0, _SubTexture);
#endif


#if defined(RANDERTYPE_FIRE)
	CalcFireVertexParam(v.uv0, v.uv1, v.uv2, o.fireUV, o.fireDistUV);
#endif

	return o;
}





FragmentCommonData FragmentSetupSimple(VertexOutputBaseSimple i)
{
#if defined(EFFECTTYPE_AURA)
	TexcoordData texcoordData;

	texcoordData.mainTexcoord = i.tex;
	SET_TEXCOORDDATA_SUBTEXTURE(texcoordData, i.texcoordSub);
	SET_TEXCOORDDATA_FIREPARAM(texcoordData, 0.0f, 0.0f);
	half FinalValue = CalcAuraCutoutValue(i.auraParam);

	//	カラーテクスチャから色を反映させる
	half NormalizedValue = saturate(FinalValue) / (1.0 - _AuraCutoutThreshold);
	half3 Gradiant = tex2D(_AuraGradiantTex, half2(NormalizedValue, _AuraGradiantLerpValue)).rgb;
	FragmentCommonData s = SpecularSetupCommon(texcoordData, Gradiant);
	half alpha = 1.0f;

#else
	half alpha = Alpha(i.tex);
#if defined(_ALPHATEST_ON)
	clip(alpha - _Cutoff - 0.001f);
#endif

	TexcoordData texcoordData;

	texcoordData.mainTexcoord = i.tex;
	SET_TEXCOORDDATA_SUBTEXTURE(texcoordData, i.texcoordSub);
	SET_TEXCOORDDATA_FIREPARAM(texcoordData, i.fireUV, i.fireDistUV);

	FragmentCommonData s = UNITY_SETUP_BRDF_INPUT(texcoordData);

	s.eyeVec = i.eyeVec.xyz;
#endif

	// NOTE: shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
	s.diffColor = PreMultiplyAlpha(s.diffColor, alpha, s.oneMinusReflectivity, /*out*/ s.alpha);
#if !defined(DISABLE_NORMAL)
	s.normalWorld = i.normalWorld.xyz;
#endif

	s.posWorld = IN_WORLDPOS(i);
	s.reflUVW = i.fogCoord.yzw;

#ifdef _NORMALMAP
	s.tangentSpaceNormal = NormalInTangentSpace(i.tex);
#else
	s.tangentSpaceNormal = 0;
#endif

	return s;
}

UnityLight MainLightSimple(VertexOutputBaseSimple i, FragmentCommonData s)
{
#if !defined(DISABLE_NORMAL)
	UnityLight mainLight = MainLight(s.normalWorld);
#else
	UnityLight mainLight = MainLight(half3(0.0, 0.0, 0.0));
#endif
#if defined(LIGHTMAP_OFF) && defined(_NORMALMAP)
	mainLight.ndotl = LambertTerm(s.tangentSpaceNormal, i.tangentSpaceLightDir);
#endif
	return mainLight;
}

half PerVertexGrazingTerm(VertexOutputBaseSimple i, FragmentCommonData s)
{
#if GLOSSMAP
	return saturate(s.oneMinusRoughness + (1 - s.oneMinusReflectivity));
#else
#if defined(EFFECTTYPE_AURA)
	return 0.0f;
#else
	return i.eyeVec.w;
#endif
#endif
}

half PerVertexFresnelTerm(VertexOutputBaseSimple i)
{
#if !defined(DISABLE_NORMAL)
	return i.normalWorld.w;
#else
	return	0.0f;
#endif
}

#if !SPECULAR_HIGHLIGHTS
#	define REFLECTVEC_FOR_SPECULAR(i, s) half3(0, 0, 0)
#elif defined(_NORMALMAP)
#	define REFLECTVEC_FOR_SPECULAR(i, s) reflect(i.tangentSpaceEyeVec, s.tangentSpaceNormal)
#else
#	define REFLECTVEC_FOR_SPECULAR(i, s) s.reflUVW
#endif

half3 LightDirForSpecular(VertexOutputBaseSimple i, UnityLight mainLight)
{
#if SPECULAR_HIGHLIGHTS && defined(_NORMALMAP)
	return i.tangentSpaceLightDir;
#else
	return mainLight.dir;
#endif
}

half3 BRDF3DirectSimple(half3 diffColor, half3 specColor, half oneMinusRoughness, half rl)
{
#if SPECULAR_HIGHLIGHTS
	return BRDF3_Direct(diffColor, specColor, Pow4(rl), oneMinusRoughness);
#else
	return diffColor;
#endif
}

half4 fragForwardBaseSimpleInternal(VertexOutputBaseSimple i)
{
	//return (1.0, 0, 0, 1);
	FragmentCommonData s = FragmentSetupSimple(i);

	UnityLight mainLight = MainLightSimple(i, s);

	half atten = SHADOW_ATTENUATION(i);

	half occlusion = Occlusion(i.tex.xy);
	half rl = dot(REFLECTVEC_FOR_SPECULAR(i, s), LightDirForSpecular(i, mainLight));

	UnityGI gi = FragmentGI(s, occlusion, i.ambientOrLightmapUV, atten, mainLight);
	half3 attenuatedLightColor = gi.light.color * mainLight.ndotl;

	half3 c = BRDF3_Indirect(s.diffColor, s.specColor, gi.indirect, PerVertexGrazingTerm(i, s), PerVertexFresnelTerm(i));

	c += BRDF3DirectSimple(s.diffColor, s.specColor, s.oneMinusRoughness, rl) * attenuatedLightColor;
#ifdef ENABLE_VERTEX_COLOR
#if defined(SHADER_BLEND_TRANSPARENTMUL)
#if defined(VERTEX_COLOR_NORMAL) || defined(VERTEX_COLOR_MEDIANHALF)
	c = lerp(c, 1.0f, CalcColorImpact(i.color, _VertexColorImpact).rgb);
#else
	c = lerp(c, 1.0f, i.color);
#endif
#else
	c *= i.color.rgb;
#endif
#endif
#if !defined(DISABLE_NORMAL)
	c += UNITY_BRDF_GI(s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, occlusion, gi);
#else
	c += UNITY_BRDF_GI(s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, half3(0.0, 0.0, 0.0), -s.eyeVec, occlusion, gi);
#endif
	c += Emission(i.tex.xy);

#ifdef ENABLE_LERP_RIMCOLOR
	c.rgb = lerp(c.rgb, c.rgb + i.lerpRimColor.rgb, _LerpRimValue*_LerpRimEnable);
#endif

	UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, _FogStatusLerpColor);


#ifdef _COVER_COLOR
	c.rgb = lerp(c.rgb, _CoverColor.rgb, _CoverColorPower);
#endif

	half    alpha = s.alpha;

#if defined(ENABLE_VERTEX_COLOR)
#if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
	alpha *= i.color.a;
#endif
#endif


	return OutputForward(half4(c, 1), alpha);
}

half4 fragForwardBaseSimple(VertexOutputBaseSimple i) : SV_Target	// backward compatibility (this used to be the fragment entry function)
{
	return fragForwardBaseSimpleInternal(i);
}

struct VertexOutputForwardAddSimple
{
	float4 pos							: SV_POSITION;
	float4 tex							: TEXCOORD0;

	LIGHTING_COORDS(1, 2)

#if !defined(_NORMALMAP) && SPECULAR_HIGHLIGHTS
		UNITY_FOG_COORDS_PACKED(3, half4) // x: fogCoord, yzw: reflectVec
#else
		UNITY_FOG_COORDS_PACKED(3, half1)
#endif

		half3 lightDir						: TEXCOORD4;

#if defined(_NORMALMAP)
#if SPECULAR_HIGHLIGHTS
	half3 tangentSpaceEyeVec		: TEXCOORD5;
#endif
#else
	half3 normalWorld					: TEXCOORD5;
#endif
};

VertexOutputForwardAddSimple vertForwardAddSimple(VertexInput v)
{
	VertexOutputForwardAddSimple o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputForwardAddSimple, o);

	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
	o.pos = UnityObjectToClipPos(v.vertex);
	o.tex = TexCoords(v);

	//We need this for shadow receiving
	TRANSFER_VERTEX_TO_FRAGMENT(o);

	half3 lightDir = _WorldSpaceLightPos0.xyz - posWorld.xyz * _WorldSpaceLightPos0.w;
#ifndef USING_DIRECTIONAL_LIGHT
	lightDir = NormalizePerVertexNormal(lightDir);
#endif

#if SPECULAR_HIGHLIGHTS
	half3 eyeVec = normalize(posWorld.xyz - _WorldSpaceCameraPos);
#endif

	half3 normalWorld = UnityObjectToWorldNormal(v.normal);

#ifdef _NORMALMAP
#if SPECULAR_HIGHLIGHTS
	TangentSpaceLightingInput(normalWorld, v.tangent, lightDir, eyeVec, o.lightDir, o.tangentSpaceEyeVec);
#else
	half3 ignore;
	TangentSpaceLightingInput(normalWorld, v.tangent, lightDir, 0, o.lightDir, ignore);
#endif
#else
	o.lightDir = lightDir;
	o.normalWorld = normalWorld;
#if SPECULAR_HIGHLIGHTS
	o.fogCoord.yzw = reflect(eyeVec, normalWorld);
#endif
#endif

#if defined(USE_FOG_LENGTH_RATE)
	UNITY_TRANSFER_FOG_RATE(o, o.pos, _FogLengthRate);
#else
	UNITY_TRANSFER_FOG(o, o.pos);
#endif
	return o;
}

FragmentCommonData FragmentSetupSimpleAdd(VertexOutputForwardAddSimple i)
{
	half alpha = Alpha(i.tex);
#if defined(_ALPHATEST_ON)
	clip(alpha - _Cutoff - 0.001f);
#endif

	TexcoordData texcoordData;

	texcoordData.mainTexcoord = i.tex;

	FragmentCommonData s = UNITY_SETUP_BRDF_INPUT(texcoordData);

	// NOTE: shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
	s.diffColor = PreMultiplyAlpha(s.diffColor, alpha, s.oneMinusReflectivity, /*out*/ s.alpha);

	s.eyeVec = 0;
	s.posWorld = 0;

#ifdef _NORMALMAP
	s.tangentSpaceNormal = NormalInTangentSpace(i.tex);
	s.normalWorld = 0;
#else
	s.tangentSpaceNormal = 0;
#if !defined(DISABLE_NORMAL)
	s.normalWorld = i.normalWorld;
#endif
#endif

#if SPECULAR_HIGHLIGHTS && !defined(_NORMALMAP)
	s.reflUVW = i.fogCoord.yzw;
#else
	s.reflUVW = 0;
#endif

	return s;
}

half3 LightSpaceNormal(VertexOutputForwardAddSimple i, FragmentCommonData s)
{
#ifdef _NORMALMAP
	return s.tangentSpaceNormal;
#else
	return i.normalWorld;
#endif
}

half4 fragForwardAddSimpleInternal(VertexOutputForwardAddSimple i)
{
	FragmentCommonData s = FragmentSetupSimpleAdd(i);

	half3 c = BRDF3DirectSimple(s.diffColor, s.specColor, s.oneMinusRoughness, dot(REFLECTVEC_FOR_SPECULAR(i, s), i.lightDir));

#if !defined(DISABLE_LIGHT_COLOR)
#if SPECULAR_HIGHLIGHTS // else diffColor has premultiplied light color
	c *= _LightColor0.rgb;
#endif
#endif

	c *= LIGHT_ATTENUATION(i) * LambertTerm(LightSpaceNormal(i, s), i.lightDir);

	UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, half4(0, 0, 0, 0)); // fog towards black in additive pass
	return OutputForward(half4(c, 1), s.alpha);
}

half4 fragForwardAddSimple(VertexOutputForwardAddSimple i) : SV_Target	// backward compatibility (this used to be the fragment entry function)
{
	return fragForwardAddSimpleInternal(i);
}

#endif // UNITY_STANDARD_CORE_FORWARD_SIMPLE_INCLUDED
