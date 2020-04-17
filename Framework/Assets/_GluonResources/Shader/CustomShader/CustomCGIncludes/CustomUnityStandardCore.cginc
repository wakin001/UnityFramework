#ifndef UNITY_STANDARD_CORE_INCLUDED
#define UNITY_STANDARD_CORE_INCLUDED

#include "../../CustomCGIncludes/CustomUnityCG.cginc"
#include "../../CustomCGIncludes/CustomUnityShaderVariables.cginc"
#include "../../CustomCGIncludes/CustomUnityInstancing.cginc"
#include "../../CustomCGIncludes/CustomUnityStandardConfig.cginc"
#include "../../CustomCGIncludes/CustomUnityStandardInput.cginc"
#include "../../CustomCGIncludes/CustomUnityPBSLighting.cginc"
#include "../../CustomCGIncludes/CustomUnityStandardUtils.cginc"
#include "../../CustomCGIncludes/CustomUnityStandardBRDF.cginc"

#include "../../CustomCGIncludes/CustomAutoLight.cginc"
#include "../../CustomCGIncludes/CustomFunctions.cginc"
#include "../../CustomShaderCommon.cginc"
#include "../../CustomCGIncludes/FuncRenderTypeFire.cginc"




//-------------------------------------------------------------------------------------
// counterpart for NormalizePerPixelNormal
// skips normalization per-vertex and expects normalization to happen per-pixel
half3 NormalizePerVertexNormal(float3 n) // takes float to avoid overflow
{
#if (APL_SHADER_TARGET < 30) || UNITY_STANDARD_SIMPLE
	return normalize(n);
#else
	return n; // will normalize per-pixel instead
#endif
}

half3 NormalizePerPixelNormal(half3 n)
{
#if (APL_SHADER_TARGET < 30) || UNITY_STANDARD_SIMPLE
	return n;
#else
	return normalize(n);
#endif
}

//-------------------------------------------------------------------------------------
UnityLight MainLight(half3 normalWorld)
{
	UnityLight l;


#ifdef LIGHTMAP_OFF
#if defined(HORIZONTAL_LIGHT_PROBE)
	l.color = 0.0f;
	l.ndotl = 1.f;
	l.dir = half3(0.f, 0.f, 0.f);
#elif !defined(DISABLE_LIGHT_COLOR)		
	l.color = _LightColor0.rgb;
	l.dir = _WorldSpaceLightPos0.xyz;
	l.ndotl = LambertTerm(normalWorld, l.dir);
#else
	l.color = half3(1.f, 1.f, 1.f);
	l.ndotl = 1.f;
	l.dir = half3(0.f, 0.f, 0.f);
#endif
#else
	// no light specified by the engine
	// analytical light might be extracted from Lightmap data later on in the shader depending on the Lightmap type
	l.color = half3(0.f, 0.f, 0.f);
	l.ndotl = 0.f;
	l.dir = half3(0.f, 0.f, 0.f);
#endif

	return l;
}

UnityLight AdditiveLight(half3 normalWorld, half3 lightDir, half atten)
{
	UnityLight l;

#if !defined(DISABLE_LIGHT_COLOR)
	l.color = _LightColor0.rgb;
#else
	l.color = 1.0f;
#endif

	l.dir = lightDir;
#ifndef USING_DIRECTIONAL_LIGHT
	l.dir = NormalizePerPixelNormal(l.dir);
#endif
	l.ndotl = LambertTerm(normalWorld, l.dir);

	// shadow the light
	l.color *= atten;
	return l;
}

UnityLight DummyLight(half3 normalWorld)
{
	UnityLight l;
	l.color = 0;
	l.dir = half3 (0, 1, 0);
	l.ndotl = LambertTerm(normalWorld, l.dir);
	return l;
}

UnityIndirect ZeroIndirect()
{
	UnityIndirect ind;
	ind.diffuse = 0;
	ind.specular = 0;
	return ind;
}




//-------------------------------------------------------------------------------------
// Common fragment setup

// deprecated
half3 WorldNormal(half4 tan2world[3])
{
	return normalize(tan2world[2].xyz);
}

// deprecated
#ifdef _TANGENT_TO_WORLD
half3x3 ExtractTangentToWorldPerPixel(half4 tan2world[3])
{
	half3 t = tan2world[0].xyz;
	half3 b = tan2world[1].xyz;
	half3 n = tan2world[2].xyz;

#if UNITY_TANGENT_ORTHONORMALIZE
	n = NormalizePerPixelNormal(n);

	// ortho-normalize Tangent
	t = normalize(t - n * dot(t, n));

	// recalculate Binormal
	half3 newB = cross(n, t);
	b = newB * sign(dot(newB, b));
#endif

	return half3x3(t, b, n);
}
#else
half3x3 ExtractTangentToWorldPerPixel(half4 tan2world[3])
{
	return half3x3(0, 0, 0, 0, 0, 0, 0, 0, 0);
}
#endif

half3 PerPixelWorldNormal(float4 i_tex, half4 tangentToWorld[3])
{
#ifdef _NORMALMAP
	half3 tangent = tangentToWorld[0].xyz;
	half3 binormal = tangentToWorld[1].xyz;
	half3 normal = tangentToWorld[2].xyz;

#if UNITY_TANGENT_ORTHONORMALIZE
	normal = NormalizePerPixelNormal(normal);

	// ortho-normalize Tangent
	tangent = normalize(tangent - normal * dot(tangent, normal));

	// recalculate Binormal
	half3 newB = cross(normal, tangent);
	binormal = newB * sign(dot(newB, binormal));
#endif

	half3 normalTangent = NormalInTangentSpace(i_tex);
	half3 normalWorld = NormalizePerPixelNormal(tangent * normalTangent.x + binormal * normalTangent.y + normal * normalTangent.z); // @TODO: see if we can squeeze this normalize on SM2.0 as well
#else
	half3 normalWorld = normalize(tangentToWorld[2].xyz);
#endif
	return normalWorld;
}


//	テクスチャ座標定義
struct TexcoordData
{
	float4		mainTexcoord;
#ifdef ENABLE_SUB_TEXTURE
	half2		subTexcoord;
#endif
#ifdef ENABLE_DISTORTION_TEXTURE
	//水面ゆがみ
	float2		distortionTexcoord;
#endif
#ifdef RANDERTYPE_FIRE
	float4       fireUV;
	float4       fireDistUV;
#endif
};

#ifdef ENABLE_SUB_TEXTURE
#define	SET_TEXCOORDDATA_SUBTEXTURE( x, sub_tex )	x.subTexcoord = sub_tex;
#else
#define	SET_TEXCOORDDATA_SUBTEXTURE( x, sub_tex )
#endif

#ifdef RANDERTYPE_FIRE
#define	SET_TEXCOORDDATA_FIREPARAM( x, fireuv, firedistuv ) x.fireUV=fireuv; x.fireDistUV=firedistuv;
#else
#define	SET_TEXCOORDDATA_FIREPARAM( x, fireuv, firedistuv )
#endif



#ifdef _PARALLAXMAP
#define IN_VIEWDIR4PARALLAX(i) NormalizePerPixelNormal(half3(i.tangentToWorldAndParallax[0].w,i.tangentToWorldAndParallax[1].w,i.tangentToWorldAndParallax[2].w))
#define IN_VIEWDIR4PARALLAX_FWDADD(i) NormalizePerPixelNormal(i.viewDirForParallax.xyz)
#else
#define IN_VIEWDIR4PARALLAX(i) half3(0,0,0)
#define IN_VIEWDIR4PARALLAX_FWDADD(i) half3(0,0,0)
#endif

#if UNITY_SPECCUBE_BOX_PROJECTION || UNITY_LIGHT_PROBE_PROXY_VOLUME
#define IN_WORLDPOS(i) i.posWorld
#else
#define IN_WORLDPOS(i) half3(0,0,0)
#endif

#define IN_LIGHTDIR_FWDADD(i) half3(i.tangentToWorldAndLightDir[0].w, i.tangentToWorldAndLightDir[1].w, i.tangentToWorldAndLightDir[2].w)

#define TEXCOORDDATA_SETUP \
	TexcoordData	texcoordData;	\
	texcoordData.mainTexcoord = i.tex; \
	SET_TEXCOORDDATA_SUBTEXTURE(texcoordData,i.texcoordSub); \
    SET_TEXCOORDDATA_FIREPARAM(texcoordData, i.fireUV, i.fireDistUV);


#ifdef	ENABLE_TANGENTTOWORLDANDPARALLAX
#define FRAGMENT_SETUP(x) \
	TEXCOORDDATA_SETUP;	\
	FragmentCommonData x = \
	FragmentSetup(texcoordData, i.eyeVec, IN_VIEWDIR4PARALLAX(i), i.tangentToWorldAndParallax, IN_WORLDPOS(i));
#else
#define FRAGMENT_SETUP(x) \
	TEXCOORDDATA_SETUP;	\
	FragmentCommonData x = \
	FragmentSetup(texcoordData, i.eyeVec, IN_VIEWDIR4PARALLAX(i), IN_WORLDPOS(i));
#endif


#ifdef	ENABLE_TANGENTTOWORLDANDPARALLAX
#define FRAGMENT_SETUP_FWDADD(x) \
	TexcoordData	texcoordData;	\
	texcoordData.mainTexcoord = i.tex; \
	FragmentCommonData x = \
	FragmentSetup(texcoordData, i.eyeVec, IN_VIEWDIR4PARALLAX_FWDADD(i), i.tangentToWorldAndLightDir, half3(0,0,0));
#else
#define FRAGMENT_SETUP_FWDADD(x) \
	TexcoordData	texcoordData;	\
	texcoordData.mainTexcoord = i.tex; \
	FragmentCommonData x = \
	FragmentSetup(texcoordData, i.eyeVec, IN_VIEWDIR4PARALLAX_FWDADD(i), half3(0,0,0));
#endif




//
struct FragmentCommonData
{
	half3 diffColor, specColor;
	// Note: oneMinusRoughness & oneMinusReflectivity for optimization purposes, mostly for DX9 SM2.0 level.
	// Most of the math is being done on these (1-x) values, and that saves a few precious ALU slots.
	half oneMinusReflectivity, oneMinusRoughness;
#if !defined(DISABLE_NORMAL)
	half3 normalWorld;
#endif
	half3 eyeVec, posWorld;
	half alpha;

#if UNITY_OPTIMIZE_TEXCUBELOD || UNITY_STANDARD_SIMPLE
	half3 reflUVW;
#endif

#if UNITY_STANDARD_SIMPLE
	half3 tangentSpaceNormal;
#endif
};

#ifndef UNITY_SETUP_BRDF_INPUT
#define UNITY_SETUP_BRDF_INPUT SpecularSetup
#endif

inline FragmentCommonData SpecularSetupCommon(TexcoordData texcoordData, half3 albedo)
{
	half4 specGloss = SpecularGloss(texcoordData.mainTexcoord);
	half3 specColor = specGloss.rgb;
	half oneMinusRoughness = specGloss.a;

	half oneMinusReflectivity;

	half3 diffColor = EnergyConservationBetweenDiffuseAndSpecular(albedo, specColor, /*out*/ oneMinusReflectivity);

	FragmentCommonData o = (FragmentCommonData)0;
	o.diffColor = diffColor;
	o.specColor = specColor;
	o.oneMinusReflectivity = oneMinusReflectivity;
	o.oneMinusRoughness = oneMinusRoughness;
	return o;
}


inline FragmentCommonData SpecularSetup(TexcoordData texcoordData)
{
	half3 albedo;

#ifdef USE_SILHOUETTE_COLOR
#if defined(RANDERTYPE_FIRE)
	float4 fireUV = texcoordData.fireUV;
	float4 fireDistUV = texcoordData.fireDistUV;

	CalcFireFragmentParam(fireUV, fireDistUV, fireUV);
	albedo = Silhouette(texcoordData.mainTexcoord);
#else
	albedo = Silhouette(texcoordData.mainTexcoord);
#endif
#elif defined(RANDERTYPE_FIRE)
	albedo = CalcFireFragmentColor(texcoordData.fireUV, texcoordData.fireDistUV);
#else
	albedo = Albedo(texcoordData.mainTexcoord);

#if defined(ENABLE_SUB_TEXTURE)
	half4	subColor;

	subColor = tex2D(_SubTexture, texcoordData.subTexcoord.xy);

	albedo = lerp(albedo, subColor.rgb, _SubTextureAlpha);
#endif
#endif
	return SpecularSetupCommon(texcoordData, albedo);
}


inline FragmentCommonData MetallicSetup(TexcoordData texcoordData)
{
	half2 metallicGloss = MetallicGloss(texcoordData.mainTexcoord);
	half metallic = metallicGloss.x;
	half oneMinusRoughness = metallicGloss.y;		// this is 1 minus the square root of real roughness m.

	half oneMinusReflectivity;
	half3 specColor;
	half3 albedo;

#ifdef USE_SILHOUETTE_COLOR
#if defined(RANDERTYPE_FIRE)
	float4 fireUV = texcoordData.fireUV;
	float4 fireDistUV = texcoordData.fireDistUV;

	CalcFireFragmentParam(fireUV, fireDistUV, fireUV);
	albedo = Silhouette(texcoordData.mainTexcoord);
#else
	albedo = Silhouette(texcoordData.mainTexcoord);
#endif
#else
	albedo = Albedo(texcoordData.mainTexcoord);

#if defined(ENABLE_SUB_TEXTURE)
	half4	subColor;

	subColor = tex2D(_SubTexture, texcoordData.subTexcoord.xy);

	albedo = lerp(albedo, subColor.rgb, _SubTextureAlpha);
#endif
#endif

	half3 diffColor = DiffuseAndSpecularFromMetallic(albedo, metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

	FragmentCommonData o = (FragmentCommonData)0;
	o.diffColor = diffColor;
	o.specColor = specColor;
	o.oneMinusReflectivity = oneMinusReflectivity;
	o.oneMinusRoughness = oneMinusRoughness;
	return o;
}


#ifdef	ENABLE_TANGENTTOWORLDANDPARALLAX
inline FragmentCommonData FragmentSetup(TexcoordData texcoordData, half3 i_eyeVec, half3 i_viewDirForParallax, half4 tangentToWorld[3], half3 i_posWorld)
#else
inline FragmentCommonData FragmentSetup(TexcoordData texcoordData, half3 i_eyeVec, half3 i_viewDirForParallax, half3 i_posWorld)
#endif
{
	texcoordData.mainTexcoord = Parallax(texcoordData.mainTexcoord, i_viewDirForParallax);

	half alpha = Alpha(texcoordData.mainTexcoord);
#if defined(_ALPHATEST_ON)
	clip(alpha - _Cutoff - 0.001f);
#endif

	FragmentCommonData o = UNITY_SETUP_BRDF_INPUT(texcoordData);
#ifdef	ENABLE_TANGENTTOWORLDANDPARALLAX
#if !defined(DISABLE_NORMAL)
	o.normalWorld = PerPixelWorldNormal(texcoordData.mainTexcoord, tangentToWorld);
#endif
#endif

	o.eyeVec = NormalizePerPixelNormal(i_eyeVec);
	o.posWorld = i_posWorld;

	// NOTE: shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
	o.diffColor = PreMultiplyAlpha(o.diffColor, alpha, o.oneMinusReflectivity, /*out*/ o.alpha);
	return o;
}

inline UnityGI FragmentGI(FragmentCommonData s, half occlusion, half4 i_ambientOrLightmapUV, half atten, UnityLight light, bool reflections)
{
	UnityGIInput d;
	d.light = light;
	d.worldPos = s.posWorld;
	d.worldViewDir = -s.eyeVec;
	d.atten = atten;
#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
	d.ambient = 0;
	d.lightmapUV = i_ambientOrLightmapUV;
#else
	d.ambient = i_ambientOrLightmapUV.rgb;
	d.lightmapUV = 0;
#endif
	d.boxMax[0] = unity_SpecCube0_BoxMax;
	d.boxMin[0] = unity_SpecCube0_BoxMin;
	d.probePosition[0] = unity_SpecCube0_ProbePosition;
	d.probeHDR[0] = unity_SpecCube0_HDR;

	d.boxMax[1] = unity_SpecCube1_BoxMax;
	d.boxMin[1] = unity_SpecCube1_BoxMin;
	d.probePosition[1] = unity_SpecCube1_ProbePosition;
	d.probeHDR[1] = unity_SpecCube1_HDR;

#if defined(_SPECULAR_CUBE)
	if (reflections)
	{
		Unity_GlossyEnvironmentData g;
		g.roughness = 1 - s.oneMinusRoughness;
#if UNITY_OPTIMIZE_TEXCUBELOD || UNITY_STANDARD_SIMPLE
		g.reflUVW = s.reflUVW;
#else
		g.reflUVW = reflect(s.eyeVec, s.normalWorld);
#endif

		return UnityGlobalIllumination(d, occlusion, s.normalWorld, g);
	}
	else
#endif
	{
#if !defined(DISABLE_NORMAL)
		return UnityGlobalIllumination(d, occlusion, s.normalWorld);
#else
		return UnityGlobalIllumination(d, occlusion, half3(0.0, 0.0, 0.0));
#endif
	}
}

inline UnityGI FragmentGI(FragmentCommonData s, half occlusion, half4 i_ambientOrLightmapUV, half atten, UnityLight light)
{
	return FragmentGI(s, occlusion, i_ambientOrLightmapUV, atten, light, true);
}


//-------------------------------------------------------------------------------------
half4 OutputForward(half4 output, half alphaFromSurface)
{
#if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
	output.a = alphaFromSurface;
#else
	UNITY_OPAQUE_ALPHA(output.a);
#endif
	return output;
}





//	SHカラーの調整を行う
inline half3 AdjustmentSHColor(half3 color)
{
	half3	hsl = rgb2hsv(color);

	hsl.z = ((hsl.z * _LightProbeImpact) + _LightProbeOffset) * _LightProbeEnable;
	hsl.z = clamp(hsl.z, _LightProbeLuminanceMin, _LightProbeLuminanceMax);
	hsl.y = hsl.y * _LightProbeSaturationScale;

	color = hsv2rgb(hsl);


	return	color;
}

inline half4 VertexGIForward(VertexInput v, float3 posWorld, half3 normalWorld)
{
	half4 ambientOrLightmapUV = 0;
	// Static lightmaps
#ifndef LIGHTMAP_OFF
#if !defined(DISABLE_LIGHTMAP)
	ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
	ambientOrLightmapUV.zw = 0;
#endif

	// Sample light probe for Dynamic objects only (no static or dynamic lightmaps)
#elif UNITY_SHOULD_SAMPLE_SH
#ifdef VERTEXLIGHT_ON
	// Approximated illumination from non-important point lights
	ambientOrLightmapUV.rgb = Shade4PointLights(
		unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
		unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
		unity_4LightAtten0, posWorld, normalWorld);
#endif

#ifdef HORIZONTAL_LIGHT_PROBE
#if !defined(DISABLE_LIGHTMAP)
	half3 SHColor = (ShadeSHHorizontalPerVertex(ambientOrLightmapUV.rgb) - ambientOrLightmapUV.rgb);

	SHColor = AdjustmentSHColor(SHColor);
	ambientOrLightmapUV.rgb += SHColor;
#endif
#else
	ambientOrLightmapUV.rgb = ShadeSHPerVertex(normalWorld, ambientOrLightmapUV.rgb);
#endif

#endif

#ifdef DYNAMICLIGHTMAP_ON
	ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif

	return ambientOrLightmapUV;
}

// ------------------------------------------------------------------
//  Base forward pass (directional light, emission, lightmaps, ...)

struct VertexOutputForwardBase
{
	float4 pos							: SV_POSITION;
#ifdef ENABLE_VERTEX_COLOR
	half4  color						: COLOR;
#endif
	float4 tex							: TEXCOORD0;
	half3 eyeVec 						: TEXCOORD1;


#if	defined(ENABLE_DISTORTION_TEXTURE)
	//水面ゆれ
	half4	grabPos						: TEXCOORD2;	//GrabTextureをどこからサンプリングするか(スクリーン座標)
	float4	texcoordDis					: TEXCOORD3;	//歪みのUVをどっちにずらすかの情報(xyとzwで2枚ぶんのUVを指定)
	half2	waveOffset					: TEXCOORD4;

#elif defined(ENABLE_TANGENTTOWORLDANDPARALLAX)
	half4 tangentToWorldAndParallax[3]	: TEXCOORD2;	// [3x3:tangentToWorld | 1x3:viewDirForParallax]	TEXCOORD2～TEXCOORD4を使っている
#if defined(ENABLE_LERP_RIMCOLOR)
	half3	lerpRimColor	: TEXCOORD5;
#endif

#else
	//	リムライトを使う
#if defined(ENABLE_LERP_RIMCOLOR)
	half4	lerpRimColor	: TEXCOORD2;
#endif

#if defined(ENABLE_NORMAL_ADJUSTMENTCOLOR) || defined(ENABLE_NORMAL_ADJUSTMENTCOLOR_DEBUG) || defined(ENABLE_NORMAL_ADJUSTMENTRIM) || defined(ENABLE_NORMAL_ADJUSTMENTRIM_DEBUG) || defined(ENABLE_HEIGHT_COLOR) || defined(ENABLE_HEIGHT_COLOR_DEBUG)
	half4	normalWorldAndColorHeight		: TEXCOORD3;		//	x,y,z=normalWorld w=height
#endif

//	サブテクスチャを使う
#if	defined(ENABLE_SUB_TEXTURE)
	half2	texcoordSub		: TEXCOORD4;
#endif
#endif


#if !defined(DISABLE_LIGHT_COLOR)
	half4 ambientOrLightmapUV			: TEXCOORD6;	// SH or Lightmap UV
#endif

	//SHADOW_COORDS(6)
	UNITY_FOG_COORDS(7)

#if UNITY_SPECCUBE_BOX_PROJECTION || UNITY_LIGHT_PROBE_PROXY_VOLUME
		float3 posWorld				: TEXCOORD8;
#endif
	// next ones would not fit into SM2.0 limits, but they are always for SM3.0+
#if defined(RANDERTYPE_FIRE)
	float4 fireUV                    : TEXCOORD9;
	float4 fireDistUV                : TEXCOORD10;
#else
#if UNITY_OPTIMIZE_TEXCUBELOD
#if UNITY_SPECCUBE_BOX_PROJECTION
	half3 reflUVW			: TEXCOORD9;
#else
	half3 reflUVW			: TEXCOORD8;
#endif
#endif
#endif


	UNITY_VERTEX_OUTPUT_STEREO
};


VertexOutputForwardBase vertForwardBase(VertexInput v)
{
	UNITY_SETUP_INSTANCE_ID(v);

	//	頂点揺れ使用
#ifdef ENABLE_VERTEX_WAVE
	float	wave_time = _Time.z * _WaveSpeed;
	float2	vec2;

	vec2.xy = v.vertex.xz * _WaveFreq - wave_time;

	vec2 = sin(vec2) * _WaveSize;
	v.vertex.y -= vec2.x + vec2.y;
#endif

	//	草揺れ
#ifdef ENABLE_GRASS_WAVE
	v.vertex.xz = v.vertex.xz + _GrassWaveDirection.xz*(sin((_Time.w - _GrassWaveStartTime.w + v.uv2.y*_GrassWaveOffsetRate) * _GrassWaveSpeed) * _GrassWavePower * (v.uv2.x * v.uv2.x));
#endif


	VertexOutputForwardBase o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputForwardBase, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
#if UNITY_SPECCUBE_BOX_PROJECTION || UNITY_LIGHT_PROBE_PROXY_VOLUME
	o.posWorld = posWorld.xyz;
#endif
	o.pos = UnityObjectToClipPos(v.vertex);

#ifdef USE_SILHOUETTE_COLOR
	o.tex = ComputeScreenPos(o.pos);
	o.tex.xy /= o.tex.w;
#else
	o.tex = TexCoords(v);
#endif
	v.normal = normalize(v.normal);


#ifdef ENABLE_EYE_VEC
	o.eyeVec = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
#endif // ENABLE_EYE_VEC

	float3 normalWorld = normalize(UnityObjectToWorldNormal(v.normal));

#ifdef	ENABLE_TANGENTTOWORLDANDPARALLAX
#ifdef _TANGENT_TO_WORLD
	float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

	float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
	o.tangentToWorldAndParallax[0].xyz = tangentToWorld[0];
	o.tangentToWorldAndParallax[1].xyz = tangentToWorld[1];
	o.tangentToWorldAndParallax[2].xyz = tangentToWorld[2];
#else
	o.tangentToWorldAndParallax[0].xyz = 0;
	o.tangentToWorldAndParallax[1].xyz = 0;
	o.tangentToWorldAndParallax[2].xyz = normalWorld;
#endif
#endif


#ifdef ENABLE_LERP_RIMCOLOR
	half3 viewDir = normalize(ObjSpaceViewDir(v.vertex));
	half dotProduct = 1 - abs(dot(normalize(v.normal), viewDir));

	o.lerpRimColor.a = dotProduct;
	o.lerpRimColor.rgb = smoothstep(1 - _LerpRimWidth, 1.0, dotProduct) * _LerpRimColor * _LerpRimPower;

#if defined(ENABLE_NORMAL_ADJUSTMENTCOLOR) || defined(ENABLE_NORMAL_ADJUSTMENTCOLOR_DEBUG) || defined(ENABLE_NORMAL_ADJUSTMENTRIM) || defined(ENABLE_NORMAL_ADJUSTMENTRIM_DEBUG)
	o.normalWorldAndColorHeight.xyz = normalWorld;
#endif
#endif

#if defined(ENABLE_HEIGHT_COLOR) || defined(ENABLE_HEIGHT_COLOR_DEBUG)
	o.normalWorldAndColorHeight.w = posWorld.y;
#endif

	//We need this for shadow receving
	TRANSFER_SHADOW(o);

#if !defined(DISABLE_LIGHT_COLOR)
	o.ambientOrLightmapUV = VertexGIForward(v, posWorld, normalWorld);
#endif

#ifdef _PARALLAXMAP
	TANGENT_SPACE_ROTATION;
	half3 viewDirForParallax = mul(rotation, ObjSpaceViewDir(v.vertex));
	o.tangentToWorldAndParallax[0].w = viewDirForParallax.x;
	o.tangentToWorldAndParallax[1].w = viewDirForParallax.y;
	o.tangentToWorldAndParallax[2].w = viewDirForParallax.z;
#endif

#if UNITY_OPTIMIZE_TEXCUBELOD
	o.reflUVW = reflect(o.eyeVec, normalWorld);
	o.reflUVW = mul(_ReflectionRotateMatarix, half4(o.reflUVW, 1.0f));
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

#ifdef	ENABLE_SUB_TEXTURE
	o.texcoordSub = TRANSFORM_TEX(v.uv0, _SubTexture);
#endif

#if defined(RANDERTYPE_FIRE)
	CalcFireVertexParam(v.uv0, v.uv1, v.uv2, o.fireUV, o.fireDistUV);
#endif

#ifdef ENABLE_DISTORTION_TEXTURE
	//水面ゆれ

	//Grab
	o.grabPos = ComputeGrabScreenPos(o.pos);

	//水面疑似スペキュラ
	o.waveOffset = sin(v.vertex.xz - (_Time.y / _SpecularCycle)) * 0.1h;

	//歪み
	float2 distUV = (float2)TRANSFORM_TEX(v.uv0, _DistortionTex);
	o.texcoordDis.xy = distUV + (_Time.x *_WaveMoveSpeed.xy);
	o.texcoordDis.zw = distUV + (_Time.x *_WaveMoveSpeed.zw);

	//水面ゆれ
#endif

	return o;
}

half4 fragForwardBaseInternal(VertexOutputForwardBase i)
{
	return (0.5, 0.5, 1, 1);
	FRAGMENT_SETUP(s)
#if UNITY_OPTIMIZE_TEXCUBELOD
		s.reflUVW = i.reflUVW;
#endif

#if !defined(DISABLE_NORMAL)
	UnityLight mainLight = MainLight(s.normalWorld);
#else
	UnityLight mainLight = MainLight(half3(1.0, 0.0, 0.0));
#endif
	half4 c;


#if defined(DISABLE_LIGHTMAP)
	//	ライトマップが無効なとき
	c.rgb = s.diffColor;
	//	頂点カラーの反映
#ifdef ENABLE_VERTEX_COLOR
#if !defined(VERTEX_COLOR_CHARACOLOR)	//	キャラカラーは別の要素で使う
	c.rgb *= i.color.rgb;
#endif
#endif

#else
	//	ライトマップが有効なとき
	half atten = SHADOW_ATTENUATION(i);
	half occlusion = Occlusion(i.tex.xy);


#if !defined(DISABLE_LIGHT_COLOR)
	UnityGI gi = FragmentGI(s, occlusion, i.ambientOrLightmapUV, atten, mainLight);
#else
	UnityGI gi = FragmentGI(s, occlusion, 0.0f, atten, mainLight);
#endif


#ifdef ENABLE_DISTORTION_TEXTURE
	//水面ゆれ

	//歪み
	half2	screenOffset1 = (tex2D(_DistortionTex, i.texcoordDis.xy) - half2(0.5h, 0.5h));
	half2	screenOffset2 = (tex2D(_DistortionTex, i.texcoordDis.zw) - half2(0.5h, 0.5h));

	//キャプチャーした画面のスクリーン位置
	half2	screenUV = (i.grabPos.xy / i.grabPos.w);
	//歪みテクスチャの分、上下左右にずらして取得すると画面が歪む
	half4	capturedColor = tex2D(_CapturedTexture, screenUV + half2((screenOffset1.x + screenOffset2.x) * _DistortionScaleX, (screenOffset1.y + screenOffset2.y) * _DistortionScaleY));

	//水面疑似スペキュラ
	half3 lightDir = normalize(_LightDir.rgb);
	half3 normalDir = normalize(half3((screenOffset1.x + screenOffset2.x), (i.waveOffset.x + i.waveOffset.y), (screenOffset1.y + screenOffset2.y)));
	half3 reflectVal = reflect(lightDir, normalDir);
	//波の高さ(0.0～1.0)
	half waveTop = shlick(max(0, dot(reflectVal, i.eyeVec)), _SpecularPower);
	//色を取得して乗算色と加算色を乗っける
	c = (capturedColor * _Color) + _ColorAdd + (clamp(waveTop + _SpecularOffset, 0, _SpecularMax) * _SpecularColor);

	//水面ゆれ
#else
#if !defined(DISABLE_NORMAL)
	c = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);
#else
	c = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, half3(0.0, 0.0, 0.0), -s.eyeVec, gi.light, gi.indirect);
#endif
#endif

	//	頂点カラーの反映
#ifdef ENABLE_VERTEX_COLOR
#if !defined(VERTEX_COLOR_CHARACOLOR)	//	キャラカラーは別の要素で使う
	c.rgb *= i.color.rgb;
#endif
#endif

#if !defined(DISABLE_NORMAL)
	c.rgb += UNITY_BRDF_GI(s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, occlusion, gi);
#else
	c.rgb += UNITY_BRDF_GI(s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, half3(0.0, 0.0, 0.0), -s.eyeVec, occlusion, gi);
#endif

#endif

	c.rgb += Emission(i.tex.xy);


	//	頂点アルファで背景色となじませる
#if defined(VERTEX_COLOR_BACKFADE) && defined(ENABLE_GLOBAL_BG_COLOR)
	c.rgb = lerp(_BackColor.rgb * _GlobalBgColor.rgb, c.rgb, i.color.a);
#elif defined(VERTEX_COLOR_BACKFADE)
	c.rgb = lerp(_BackColor.rgb, c.rgb, i.color.a);
#endif


#if  defined(ENABLE_HEIGHT_COLOR) || defined(ENABLE_HEIGHT_COLOR_DEBUG)
	half4 rate;

	rate = saturate((i.normalWorldAndColorHeight.wwww - _HeightColorBottom) / (_HeightColorTop - _HeightColorBottom));
	rate = pow(rate, 1.0f / _HeightColorSoftness);

#if defined(ENABLE_HEIGHT_COLOR_DEBUG)
	rate.g = 1.0f - rate.g;
	c = 0.0f;
	c.rg = rate.rg;
	return c;
#else
	c.rgb = lerp(c.rgb*_HeightColorMulColor, c.rgb, rate.y);
	//c.rgb = lerp(c.rgb*_HeightColorMulColor, c.rgb, rate.y) +_HeightColorAddColor*rate.x;
#endif


#endif


#ifdef ENABLE_LERP_RIMCOLOR
	c.rgb = lerp(c.rgb, c.rgb + i.lerpRimColor.rgb, _LerpRimValue*_LerpRimEnable);

#if defined(ENABLE_NORMAL_ADJUSTMENTCOLOR) || defined(ENABLE_NORMAL_ADJUSTMENTCOLOR_DEBUG) || defined(ENABLE_NORMAL_ADJUSTMENTRIM) || defined(ENABLE_NORMAL_ADJUSTMENTRIM_DEBUG)
	half3 adj_color = ContrastSaturationBrightness(c.rgb, _NormalAdjustmentBrightness, _NormalAdjustmentSaturation, _NormalAdjustmentContrast);

	half colorAmount = SmoothingParameter((1.0 - i.lerpRimColor.a), _NormalAdjustmentColorSize, _NormalAdjustmentColorSoftness, _NormalAdjustmentColorIntensity);

#ifdef ENABLE_NORMAL_ADJUSTMENTCOLOR_DEBUG
	c.rgb = colorAmount;
	return c;
#else
	c.rgb = lerp(adj_color, c.rgb, colorAmount);
#endif

#if	defined(ENABLE_NORMAL_ADJUSTMENTRIM) || defined(ENABLE_NORMAL_ADJUSTMENTRIM_DEBUG)

	half2 rimmaskuv = i.normalWorldAndColorHeight.xy*0.5f + 0.5f;
	half4 rimmaskvalue = tex2D(_NormalAdjustmentMaskTex, rimmaskuv);
	half4 rimAmount = SmoothingParameter(0.75 - (1.0 - i.lerpRimColor.a), _NormalAdjustmentRimSize, _NormalAdjustmentRimSoftness, _NormalAdjustmentRimIntensity*rimmaskvalue);
	half3 rimAddColor;

	rimAmount = pow(rimAmount, max(_NormalAdjustmentRimPower, 0.001f)) * _NormalAdjustmentRimValue;
	rimAddColor = _NormalAdjustmentRimColor0 * rimAmount.r + _NormalAdjustmentRimColor1 * rimAmount.g + _NormalAdjustmentRimColor2 * rimAmount.b;

#if defined(VERTEX_COLOR_CHARACOLOR)
	rimAddColor *= i.color.r;			//	Rに法線調整リムカラーの強度を入れておく
#endif

#ifdef ENABLE_NORMAL_ADJUSTMENTRIM_DEBUG
	c.rgb = rimAddColor;
#else
	c.rgb += rimAddColor;
#endif
#endif
#endif
#endif

#if defined(ENABLE_GLOBAL_BG_COLOR)
	UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, _FogStatusLerpColor * _GlobalBgColor.rgb);
#else
	UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, _FogStatusLerpColor);
#endif

#ifdef _COVER_COLOR
	c.rgb = lerp(c.rgb, _CoverColor.rgb, _CoverColorPower);
#endif

	c = OutputForward(c, s.alpha);

#ifdef TRANSPARENT_ALPHA
	c.a = Alpha(i.tex);
#if defined(ENABLE_VERTEX_COLOR) && !defined(VERTEX_COLOR_BACKFADE) && !defined(VERTEX_COLOR_CHARACOLOR)
	c.a *= i.color.a;
#endif
#else
	c.a = 1;    // chujo 機種端末依存対応のためalphaを書き込む
#endif

	return c;
}

half4 fragForwardBase(VertexOutputForwardBase i) : SV_Target	// backward compatibility (this used to be the fragment entry function)
{
	return fragForwardBaseInternal(i);
}

// ------------------------------------------------------------------
//  Additive forward pass (one light per pass)

struct VertexOutputForwardAdd
{
	float4 pos							: SV_POSITION;
	float4 tex							: TEXCOORD0;
	half3 eyeVec 						: TEXCOORD1;
	half4 tangentToWorldAndLightDir[3]	: TEXCOORD2;	// [3x3:tangentToWorld | 1x3:lightDir]
	LIGHTING_COORDS(5, 6)
		UNITY_FOG_COORDS(7)


		// next ones would not fit into SM2.0 limits, but they are always for SM3.0+
#if defined(_PARALLAXMAP)
		half3 viewDirForParallax			: TEXCOORD8;
#endif

	UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutputForwardAdd vertForwardAdd(VertexInput v)
{
	VertexOutputForwardAdd o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputForwardAdd, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
	o.pos = UnityObjectToClipPos(v.vertex);

	o.tex = TexCoords(v);
	o.eyeVec = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
	float3 normalWorld = UnityObjectToWorldNormal(v.normal);
#ifdef _TANGENT_TO_WORLD
	float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

	float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
	o.tangentToWorldAndLightDir[0].xyz = tangentToWorld[0];
	o.tangentToWorldAndLightDir[1].xyz = tangentToWorld[1];
	o.tangentToWorldAndLightDir[2].xyz = tangentToWorld[2];
#else
	o.tangentToWorldAndLightDir[0].xyz = 0;
	o.tangentToWorldAndLightDir[1].xyz = 0;
	o.tangentToWorldAndLightDir[2].xyz = normalWorld;
#endif
	//We need this for shadow receiving
	TRANSFER_VERTEX_TO_FRAGMENT(o);

	float3 lightDir = _WorldSpaceLightPos0.xyz - posWorld.xyz * _WorldSpaceLightPos0.w;
#ifndef USING_DIRECTIONAL_LIGHT
	lightDir = NormalizePerVertexNormal(lightDir);
#endif
	o.tangentToWorldAndLightDir[0].w = lightDir.x;
	o.tangentToWorldAndLightDir[1].w = lightDir.y;
	o.tangentToWorldAndLightDir[2].w = lightDir.z;

#ifdef _PARALLAXMAP
	TANGENT_SPACE_ROTATION;
	o.viewDirForParallax = mul(rotation, ObjSpaceViewDir(v.vertex));
#endif

#if defined(USE_FOG_LENGTH_RATE)
	UNITY_TRANSFER_FOG_RATE(o, o.pos, _FogLengthRate);
#else
	UNITY_TRANSFER_FOG(o, o.pos);
#endif

	return o;
}

half4 fragForwardAddInternal(VertexOutputForwardAdd i)
{
	FRAGMENT_SETUP_FWDADD(s)

		UnityIndirect noIndirect = ZeroIndirect();
#if !defined(DISABLE_NORMAL)
	UnityLight light = AdditiveLight(s.normalWorld, IN_LIGHTDIR_FWDADD(i), LIGHT_ATTENUATION(i));
	half4 c = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, light, noIndirect);
#else
	UnityLight light = AdditiveLight(half3(0.0, 0.0, 0.0), IN_LIGHTDIR_FWDADD(i), LIGHT_ATTENUATION(i));
	half4 c = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, half3(0.0, 0.0, 0.0), -s.eyeVec, light, noIndirect);
#endif


	UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, half4(0, 0, 0, 0)); // fog towards black in additive pass
	return OutputForward(c, s.alpha);
}

half4 fragForwardAdd(VertexOutputForwardAdd i) : SV_Target		// backward compatibility (this used to be the fragment entry function)
{
	return fragForwardAddInternal(i);
}

// ------------------------------------------------------------------
//  Deferred pass

struct VertexOutputDeferred
{
	float4 pos							: SV_POSITION;
	float4 color                        : COLOR;
	float4 tex							: TEXCOORD0;
	half3 eyeVec 						: TEXCOORD1;

#if	defined(ENABLE_DISTORTION_TEXTURE)
	//水面ゆれ
	half4	grabPos						: TEXCOORD2;
	float4	texcoordDis					: TEXCOORD3;
	half2	waveOffset					: TEXCOORD4;

#elif defined(ENABLE_TANGENTTOWORLDANDPARALLAX)
	half4 tangentToWorldAndParallax[3]	: TEXCOORD2;	// [3x3:tangentToWorld | 1x3:viewDirForParallax]	TEXCOORD2～TEXCOORD4を使っている

#else
	//	リムライトを使う
#if defined(ENABLE_LERP_RIMCOLOR)
	half4	lerpRimColor	: TEXCOORD2;
#endif

	//	サブテクスチャを使う
#if	defined(ENABLE_SUB_TEXTURE)
	half2	texcoordSub		: TEXCOORD3;
#endif
#endif
	half4 ambientOrLightmapUV			: TEXCOORD5;	// SH or Lightmap UVs			

#if UNITY_SPECCUBE_BOX_PROJECTION || UNITY_LIGHT_PROBE_PROXY_VOLUME
	float3 posWorld						: TEXCOORD6;
#endif

#if defined(RANDERTYPE_FIRE)
	float4 fireUV                   : TEXCOORD7;
	float4 fireDistUV               : TEXCOORD8;
#else
#if UNITY_OPTIMIZE_TEXCUBELOD
#if UNITY_SPECCUBE_BOX_PROJECTION
	half3 reflUVW			: TEXCOORD7;
#else
	half3 reflUVW			: TEXCOORD6;
#endif
#endif
#endif

	UNITY_VERTEX_OUTPUT_STEREO
};


VertexOutputDeferred vertDeferred(VertexInput v)
{
	UNITY_SETUP_INSTANCE_ID(v);
	VertexOutputDeferred o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputDeferred, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
#if UNITY_SPECCUBE_BOX_PROJECTION || UNITY_LIGHT_PROBE_PROXY_VOLUME
	o.posWorld = posWorld;
#endif
	o.pos = UnityObjectToClipPos(v.vertex);

	o.tex = TexCoords(v);
	o.eyeVec = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
	float3 normalWorld = UnityObjectToWorldNormal(v.normal);
#ifdef	ENABLE_TANGENTTOWORLDANDPARALLAX
#ifdef _TANGENT_TO_WORLD
	float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

	float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
	o.tangentToWorldAndParallax[0].xyz = tangentToWorld[0];
	o.tangentToWorldAndParallax[1].xyz = tangentToWorld[1];
	o.tangentToWorldAndParallax[2].xyz = tangentToWorld[2];
#else
	o.tangentToWorldAndParallax[0].xyz = 0;
	o.tangentToWorldAndParallax[1].xyz = 0;
	o.tangentToWorldAndParallax[2].xyz = normalWorld;
#endif
#endif

	o.ambientOrLightmapUV = 0;
#ifndef LIGHTMAP_OFF
#if !defined(DISABLE_LIGHTMAP)
	o.ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#endif
#elif UNITY_SHOULD_SAMPLE_SH
	o.ambientOrLightmapUV.rgb = ShadeSHPerVertex(normalWorld, o.ambientOrLightmapUV.rgb);
#endif
#ifdef DYNAMICLIGHTMAP_ON
	o.ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif

#ifdef _PARALLAXMAP
	TANGENT_SPACE_ROTATION;
	half3 viewDirForParallax = mul(rotation, ObjSpaceViewDir(v.vertex));
	o.tangentToWorldAndParallax[0].w = viewDirForParallax.x;
	o.tangentToWorldAndParallax[1].w = viewDirForParallax.y;
	o.tangentToWorldAndParallax[2].w = viewDirForParallax.z;
#endif

#if UNITY_OPTIMIZE_TEXCUBELOD
	o.reflUVW = reflect(o.eyeVec, normalWorld);
#endif


#ifdef ENABLE_DISTORTION_TEXTURE
	//水面ゆれ

	//Grab
	o.pos = UnityObjectToClipPos(v.vertex);
	o.grabPos = ComputeGrabScreenPos(o.pos);

	//水面疑似スペキュラ
	o.waveOffset = sin(v.vertex.xz - (_Time.y / _SpecularCycle)) * 0.1h;

	//歪み
	float2 distUV = (float2)TRANSFORM_TEX(v.uv0, _DistortionTex);
	o.texcoordDis.xy = distUV + (_Time.x *_WaveMoveSpeed.xy);
	o.texcoordDis.zw = distUV + (_Time.x *_WaveMoveSpeed.zw);

	//水面ゆれ
#endif

	return o;
}

void fragDeferred(
	VertexOutputDeferred i,
	out half4 outDiffuse : SV_Target0,			// RT0: diffuse color (rgb), occlusion (a)
	out half4 outSpecSmoothness : SV_Target1,	// RT1: spec color (rgb), smoothness (a)
	out half4 outNormal : SV_Target2,			// RT2: normal (rgb), --unused, very low precision-- (a) 
	out half4 outEmission : SV_Target3			// RT3: emission (rgb), --unused-- (a)
)
{
#if (APL_SHADER_TARGET < 30)
	outDiffuse = 1;
	outSpecSmoothness = 1;
	outNormal = 0;
	outEmission = 0;
	return;
#endif

	FRAGMENT_SETUP(s)
#if UNITY_OPTIMIZE_TEXCUBELOD
		s.reflUVW = i.reflUVW;
#endif

	// no analytic lights in this pass
#if !defined(DISABLE_NORMAL)
	UnityLight dummyLight = DummyLight(s.normalWorld);
#else
	UnityLight dummyLight = DummyLight(half3(0.0, 0.0, 0.0));
#endif
	half atten = 1;

	// only GI
	half occlusion = Occlusion(i.tex.xy);
#if UNITY_ENABLE_REFLECTION_BUFFERS
	bool sampleReflectionsInDeferred = false;
#else
	bool sampleReflectionsInDeferred = true;
#endif

	UnityGI gi = FragmentGI(s, occlusion, i.ambientOrLightmapUV, atten, dummyLight, sampleReflectionsInDeferred);

#if !defined(DISABLE_NORMAL)
	half3 color = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect).rgb;
	color += UNITY_BRDF_GI(s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, occlusion, gi);
#else
	half3 color = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, half3(0.0, 0.0, 0.0), -s.eyeVec, gi.light, gi.indirect).rgb;
	color += UNITY_BRDF_GI(s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, half3(0.0, 0.0, 0.0), -s.eyeVec, occlusion, gi);
#endif

#ifdef _EMISSION
	color += Emission(i.tex.xy);
#endif

#ifndef UNITY_HDR_ON
	color.rgb = exp2(-color.rgb);
#endif

	outDiffuse = half4(s.diffColor, occlusion);
	outSpecSmoothness = half4(s.specColor, s.oneMinusRoughness);

#if !defined(DISABLE_NORMAL)
	outNormal = half4(s.normalWorld*0.5 + 0.5, 1);
#else
	outNormal = half4(0.0, 0.0, 0.0, 1.0);
#endif

	outEmission = half4(color, 1);
}


//
// Old FragmentGI signature. Kept only for backward compatibility and will be removed soon
//

inline UnityGI FragmentGI(
	float3 posWorld,
	half occlusion, half4 i_ambientOrLightmapUV, half atten, half oneMinusRoughness, half3 normalWorld, half3 eyeVec,
	UnityLight light,
	bool reflections)
{
	// we init only fields actually used
	FragmentCommonData s = (FragmentCommonData)0;
	s.oneMinusRoughness = oneMinusRoughness;
#if !defined(DISABLE_NORMAL)
	s.normalWorld = normalWorld;
#endif
	s.eyeVec = eyeVec;
	s.posWorld = posWorld;
#if UNITY_OPTIMIZE_TEXCUBELOD
	s.reflUVW = reflect(eyeVec, normalWorld);
#endif
	return FragmentGI(s, occlusion, i_ambientOrLightmapUV, atten, light, reflections);
}
inline UnityGI FragmentGI(
	float3 posWorld,
	half occlusion, half4 i_ambientOrLightmapUV, half atten, half oneMinusRoughness, half3 normalWorld, half3 eyeVec,
	UnityLight light)
{
	return FragmentGI(posWorld, occlusion, i_ambientOrLightmapUV, atten, oneMinusRoughness, normalWorld, eyeVec, light, true);
}

#endif // UNITY_STANDARD_CORE_INCLUDED
