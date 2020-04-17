// Upgrade NOTE: replaced 'UNITY_INSTANCE_ID' with 'UNITY_VERTEX_INPUT_INSTANCE_ID'

#ifndef UNITY_STANDARD_INPUT_INCLUDED
#define UNITY_STANDARD_INPUT_INCLUDED

#include "../../CustomCGIncludes/CustomUnityCG.cginc"
#include "../../CustomCGIncludes/CustomUnityShaderVariables.cginc"
#include "../../CustomCGIncludes/CustomUnityInstancing.cginc"
#include "../../CustomCGIncludes/CustomUnityStandardConfig.cginc"
#include "../../CustomCGIncludes/CustomUnityPBSLighting.cginc" // TBD: remove
#include "../../CustomCGIncludes/CustomUnityStandardUtils.cginc"
#include "../../CustomShaderCommon.cginc"



//---------------------------------------
// Directional lightmaps & Parallax require tangent space too
#if (_NORMALMAP || DIRLIGHTMAP_COMBINED || DIRLIGHTMAP_SEPARATE || _PARALLAXMAP)
	#define _TANGENT_TO_WORLD 1 
#endif

#if (_DETAIL_MULX2 || _DETAIL_MUL || _DETAIL_ADD || _DETAIL_LERP)
	#define _DETAIL 1
#endif

//---------------------------------------
half4		_Color;
half		_ColorPower;
half		_Cutoff;

#ifdef ENABLE_PROGRAMMABLE_COLOR
half4		_ProgrammableColor;
#endif

#ifdef ENABLE_GLOBAL_BG_COLOR
half4		_GlobalBgColor;
#endif

#ifdef USE_SILHOUETTE_COLOR
sampler2D		_SilhouetteTexture;
uniform	half3	_SilhouetteRimColor;
uniform	half3	_SilhouetteBaseColor;
#endif


#ifdef ENABLE_LERP_RIMCOLOR
half3	_LerpRimColor;
half	_LerpRimPower;
half	_LerpRimWidth;
half	_LerpRimValue;
half	_LerpRimEnable;
#endif


#if defined(ENABLE_HEIGHT_COLOR) || defined(ENABLE_HEIGHT_COLOR_DEBUG)
half4	_HeightColorTop;
half4	_HeightColorBottom;
half4	_HeightColorSize;
half4	_HeightColorIntensity;
half4	_HeightColorSoftness;
half3	_HeightColorAddColor;
half3	_HeightColorMulColor;
#endif

#if defined(ENABLE_NORMAL_ADJUSTMENTCOLOR) || defined(ENABLE_NORMAL_ADJUSTMENTCOLOR_DEBUG) || defined(ENABLE_NORMAL_ADJUSTMENTRIM) || defined(ENABLE_NORMAL_ADJUSTMENTRIM_DEBUG)
half _NormalAdjustmentBrightness;
half _NormalAdjustmentSaturation;
half _NormalAdjustmentContrast;
half _NormalAdjustmentColorSize;
half _NormalAdjustmentColorIntensity;
half _NormalAdjustmentColorSoftness;
half3 _NormalAdjustmentRimColor0;
half3 _NormalAdjustmentRimColor1;
half3 _NormalAdjustmentRimColor2;
half4 _NormalAdjustmentRimSize;
half4 _NormalAdjustmentRimIntensity;
half4 _NormalAdjustmentRimSoftness;
half4 _NormalAdjustmentRimPower;
half  _NormalAdjustmentRimValue;
sampler2D _NormalAdjustmentMaskTex;
#endif

#ifdef _COVER_COLOR
half4		_CoverColor;
half		_CoverColorPower;
#endif


sampler2D	_MainTex;
half4		_MainTex_ST;


//	オプションテクスチャを使うか
#if defined(ENABLE_PROGRAMMABLE_COLOR) || !defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A) || defined(_ALPHATEST_ON)
#ifndef USE_OPTION_TEXTURE
#define USE_OPTION_TEXTURE
#endif
#endif



#if defined(USE_OPTION_TEXTURE)
sampler2D	_OptionTex0;
half4		_OptionTex0_ST;
#endif



#if defined(TEXTURECOLOR_PALETTE)
sampler2D	_PaletteTex;
half4		_PaletteTex_ST;
#elif defined(TEXTURECOLOR_YUV411)
sampler2D	_TexY;
sampler2D	_TexCb;
sampler2D	_TexCr;
#endif



sampler2D	_DetailAlbedoMap;
half4		_DetailAlbedoMap_ST;

sampler2D	_BumpMap;
half		_BumpScale;

sampler2D	_DetailMask;
sampler2D	_DetailNormalMap;
half		_DetailNormalMapScale;

sampler2D	_SpecGlossMap;
sampler2D	_MetallicGlossMap;
half		_Metallic;
half		_Glossiness;
half		_GlossMapScale;

#ifdef _OCCLUSION_MAP
sampler2D	_OcclusionMap;
half		_OcclusionStrength;
#endif

sampler2D	_ParallaxMap;
half		_Parallax;
half		_UVSec;

half4 		_EmissionColor;
sampler2D	_EmissionMap;


half4		_BackColor;					//	背景色
half		_VertexColorImpact;			//	頂点カラーの影響度


half        _LightProbeEnable;          //	ライトプロブの有効・無効
half		_LightProbeImpact;			//	ライトプロブの影響度
half		_LightProbeOffset;
half		_LightProbeLuminanceMin;
half		_LightProbeLuminanceMax;
half		_LightProbeSaturationScale;


#if UNITY_OPTIMIZE_TEXCUBELOD
float4x4	_ReflectionRotateMatarix;
#endif


#ifdef ENABLE_SUB_TEXTURE
sampler2D	_SubTexture;
half4		_SubTexture_ST;
fixed		_SubTextureAlpha;
#endif


//	歪みテクスチャ
#ifdef ENABLE_DISTORTION_TEXTURE
sampler2D	_DistortionTex;
half4		_DistortionTex_ST;

half		_DistortionPower;
half		_DistortionSubTexPower;
half		_DistortionSubTexOffset;
#endif


//	揺れのパラメータ
#ifdef	ENABLE_VERTEX_WAVE
half	_WaveFreq;
half	_WaveSpeed;
half	_WaveSize;
#endif


//	草揺れのパラメータ
#ifdef ENABLE_GRASS_WAVE
float4	_GrassWaveStartTime;
half	_GrassWaveSpeed;
half	_GrassWavePower;
half	_GrassWaveOffsetRate;
half3	_GrassWaveDirection;
#endif


#if	defined(ENABLE_DISTORTION_TEXTURE)
//水面ゆれ

//歪みテクスチャ
half		_DistortionScaleX;
half		_DistortionScaleY;

//揺れのパラメータ
half4		_WaveMoveSpeed;
half4		_LightDir;
half4		_SpecularColor;
half4		_ColorAdd;
half		_SpecularCycle;
half		_SpecularMax;
half		_SpecularOffset;
half		_SpecularPower;

//Grab
sampler2D_half		_CapturedTexture;

//水面ゆれ
#endif




#if defined(USE_FOG_LENGTH_RATE)
uniform half _FogLengthRate;
#endif


//	フォグと馴染ませる色の定義
#if defined(SHADER_BLEND_TRANSPARENTADD)
	#define	_FogStatusLerpColor		fixed4(0,0,0,0)//(1.0f-unity_FogColor)//half4(0.0f,0.0f,0.0f,0.0f)
#elif defined(SHADER_BLEND_TRANSPARENTMUL)
	#define	_FogStatusLerpColor		half4(1.0f,1.0f,1.0f,1.0f)
#else
	#define	_FogStatusLerpColor		unity_FogColor
#endif


//-------------------------------------------------------------------------------------
// Input functions

struct VertexInput
{
	float4 vertex	: POSITION;
#ifdef ENABLE_VERTEX_COLOR
	fixed4 color	: COLOR;
#endif
	half3 normal	: NORMAL;
	float2 uv0		: TEXCOORD0;
	float2 uv1		: TEXCOORD1;
#if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META) || defined(ENABLE_GRASS_WAVE) || defined(RANDERTYPE_FIRE)
	float2 uv2		: TEXCOORD2;
#endif
#ifdef _TANGENT_TO_WORLD
	half4 tangent	: TANGENT;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

float4 TexCoords(VertexInput v)
{
	float4 texcoord;

#if _DETAIL
	texcoord.xy = TRANSFORM_TEX(v.uv0, _MainTex); // Always source from uv0
	texcoord.zw = TRANSFORM_TEX(((_UVSec == 0) ? v.uv0 : v.uv1), _DetailAlbedoMap);
#else
	#if defined(USE_OPTION_TEXTURE)
		texcoord.xy = TRANSFORM_TEX(v.uv0, _MainTex).xy; // Always source from uv0
		texcoord.zw = TRANSFORM_TEX(v.uv0, _OptionTex0).xy; // Always source from uv0
	#else
		texcoord.xyzw = TRANSFORM_TEX(v.uv0, _MainTex).xyxy; // Always source from uv0
	#endif
#endif

	return texcoord;
}		

half DetailMask(float2 uv)
{
	return tex2D (_DetailMask, uv).a;
}

half3 Albedo(float4 texcoords)
{
	half3 albedo;

#if defined(ENABLE_GLOBAL_BG_COLOR) || defined(ENABLE_PROGRAMMABLE_COLOR)
	half3 mulcol;
#endif


#if defined(TEXTURECOLOR_PALETTE)
	//	パレットテクスチャモード
	albedo = tex2D(_MainTex, texcoords.xy).rgb;
    half    pal = albedo.g;

    pal = pal * _PaletteTex_ST.x + _PaletteTex_ST.z;
    albedo = tex2D(_PaletteTex, half2(pal,0.0f)).rgb;

#elif defined(TEXTURECOLOR_YUV411)
	//	YUV411モード
	albedo.r = tex2D(_TexY, texcoords.xy).a;	//	Y
	albedo.g = tex2D(_TexCb, texcoords.xy);		//	Cb
	albedo.b = tex2D(_TexCr, texcoords.xy);		//	Cr

	albedo = YCbCrtoRGB(albedo);
#else
	albedo = tex2D(_MainTex, texcoords.xy).rgb;
#endif

#if defined(ENABLE_GLOBAL_BG_COLOR) && defined(ENABLE_PROGRAMMABLE_COLOR)
	mulcol = _GlobalBgColor.rgb * _ProgrammableColor.rgb;
#elif defined(ENABLE_GLOBAL_BG_COLOR)
	mulcol = _GlobalBgColor.rgb;
#elif defined(ENABLE_PROGRAMMABLE_COLOR)
	mulcol = _ProgrammableColor.rgb;
#endif

#ifdef SHADER_BLEND_TRANSPARENTMUL
#if defined(ENABLE_GLOBAL_BG_COLOR) || defined(ENABLE_PROGRAMMABLE_COLOR)
	albedo = 1.0f - (1.0f - albedo*_Color.rgb) * _ColorPower * mulcol.rgb;
#else
	albedo = 1.0f - (1.0f - albedo*_Color.rgb) * _ColorPower;
#endif
#else
#if defined(ENABLE_GLOBAL_BG_COLOR) || defined(ENABLE_PROGRAMMABLE_COLOR)
	albedo *= _Color.rgb * _ColorPower * mulcol.rgb;
#else
	albedo *= _Color.rgb * _ColorPower;
#endif
#endif
	
#if _DETAIL
	#if (APL_SHADER_TARGET < 30)
		// SM20: instruction count limitation
		// SM20: no detail mask
		half mask = 1; 
	#else
		half mask = DetailMask(texcoords.xy);
	#endif
	half3 detailAlbedo = tex2D (_DetailAlbedoMap, texcoords.zw).rgb;
	#if _DETAIL_MULX2
		albedo *= LerpWhiteTo (detailAlbedo * unity_ColorSpaceDouble.rgb, mask);
	#elif _DETAIL_MUL
		albedo *= LerpWhiteTo (detailAlbedo, mask);
	#elif _DETAIL_ADD
		albedo += detailAlbedo * mask;
	#elif _DETAIL_LERP
		albedo = lerp (albedo, detailAlbedo, mask);
	#endif
#endif
	return albedo;
}






half Alpha(float4 uv)
{
#if defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
#ifdef ENABLE_PROGRAMMABLE_COLOR
	return _Color.a * _ProgrammableColor.a;
#else
	return _Color.a;
#endif
#else
#ifdef ENABLE_PROGRAMMABLE_COLOR
	return tex2D(_OptionTex0, uv.zw).r * _Color.a * _ProgrammableColor.a;
#else
	return tex2D(_OptionTex0, uv.zw).r * _Color.a;
#endif
#endif
}		

half Occlusion(float2 uv)
{
#ifdef _OCCLUSION_MAP
	#if (APL_SHADER_TARGET < 30)
		// SM20: instruction count limitation
		// SM20: simpler occlusion
		return tex2D(_OcclusionMap, uv).g;
	#else
		half occ = tex2D(_OcclusionMap, uv).g;
		return LerpOneTo (occ, _OcclusionStrength);
	#endif
#else
	return	1.0f;
#endif
}


#ifdef USE_SILHOUETTE_COLOR
half3 Silhouette(float4 texcoords)
{
	half3 silhouette;


	half rim_rate;

	rim_rate = tex2D(_SilhouetteTexture, texcoords).r;


	silhouette = lerp(_SilhouetteBaseColor, _SilhouetteRimColor, rim_rate);

	return	silhouette;
}
#endif



half4 SpecularGloss(float4 uv)
{
	half4 sg;
#ifdef _SPECGLOSSMAP
	#if defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
		sg.rgb = tex2D(_SpecGlossMap, uv.xy).rgb;
		sg.a = tex2D(_OptionTex0, uv.zw).r;
	#else
		sg = tex2D(_SpecGlossMap, uv.xy);
	#endif
	sg.a *= _GlossMapScale;
#else
#if !defined(DISABLE_LIGHT_COLOR) && defined(ENABLE_SPECULAR)
	sg.rgb = _SpecColor.rgb;
#else
	sg.rgb = 0.0f;
#endif
	#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
		sg.a = tex2D(_OptionTex0, uv.xy).r * _GlossMapScale;
	#else
		sg.a = _Glossiness;
	#endif
#endif
	return sg;
}

half2 MetallicGloss(float2 uv)
{
	half2 mg;
	
#ifdef _METALLICGLOSSMAP
	#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
		mg.r = tex2D(_MetallicGlossMap, uv).r;
		mg.g = tex2D(_OptionTex0, uv).r;
	#else
		mg = tex2D(_MetallicGlossMap, uv).ra;
	#endif
	mg.g *= _GlossMapScale;
#else
	mg.r = _Metallic;
	#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
		mg.g = tex2D(_OptionTex0, uv).r * _GlossMapScale;
	#else
		mg.g = _Glossiness;
	#endif
#endif
	return mg;
}

half3 Emission(float2 uv)
{
#ifndef _EMISSION
	return 0;
#else
	return tex2D(_EmissionMap, uv).rgb * _EmissionColor.rgb;
#endif
}

#ifdef _NORMALMAP
half3 NormalInTangentSpace(float4 texcoords)
{
	half3 normalTangent = UnpackScaleNormal(tex2D (_BumpMap, texcoords.xy), _BumpScale);
	// SM20: instruction count limitation
	// SM20: no detail normalmaps
#if _DETAIL && !defined(SHADER_API_MOBILE) && (SHADER_TARGET >= 30) 
	half mask = DetailMask(texcoords.xy);
	half3 detailNormalTangent = UnpackScaleNormal(tex2D (_DetailNormalMap, texcoords.zw), _DetailNormalMapScale);
	#if _DETAIL_LERP
		normalTangent = lerp(
			normalTangent,
			detailNormalTangent,
			mask);
	#else				
		normalTangent = lerp(
			normalTangent,
			BlendNormals(normalTangent, detailNormalTangent),
			mask);
	#endif
#endif
	return normalTangent;
}
#endif

float4 Parallax (float4 texcoords, half3 viewDir)
{
// D3D9/SM30 supports up to 16 samplers, skip the parallax map in case we exceed the limit
#define EXCEEDS_D3D9_SM3_MAX_SAMPLER_COUNT	(defined(LIGHTMAP_ON) && defined(DIRLIGHTMAP_SEPARATE) && defined(SHADOWS_SCREEN) && defined(_NORMALMAP) && \
											 defined(_EMISSION) && defined(_DETAIL) && (defined(_METALLICGLOSSMAP) || defined(_SPECGLOSSMAP)))

#if !defined(_PARALLAXMAP) || (APL_SHADER_TARGET < 30) || (defined(SHADER_API_D3D9) && EXCEEDS_D3D9_SM3_MAX_SAMPLER_COUNT)
	// SM20: instruction count limitation
	// SM20: no parallax
	return texcoords;
#else
	half h = tex2D (_ParallaxMap, texcoords.xy).g;
	float2 offset = ParallaxOffset1Step (h, _Parallax, viewDir);
	return float4(texcoords.xy + offset, texcoords.zw + offset);
#endif

#undef EXCEEDS_D3D9_SM3_MAX_SAMPLER_COUNT
}
			
#endif // UNITY_STANDARD_INPUT_INCLUDED
