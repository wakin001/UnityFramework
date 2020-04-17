// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

/////////////////////////////////////////////////////////////
//
//		OriginalDepthCaster.cginc
//
/////////////////////////////////////////////////////////////

// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
#pragma exclude_renderers gles

//	オプションテクスチャを使うか
#if defined(ENABLE_PROGRAMMABLE_COLOR) || !defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A) || defined(_ALPHATEST_ON)
#ifndef USE_OPTION_TEXTURE
#define USE_OPTION_TEXTURE
#endif
#endif


#ifdef USE_OPTION_TEXTURE
uniform	half		_Cutoff;
uniform sampler2D	_OptionTex0;
uniform half4		_OptionTex0_ST;
#endif


struct v2f
{
	float4 pos		 : SV_POSITION;
	float2 depth	 : TEXCOORD0;
#ifdef USE_OPTION_TEXTURE
	float2 texcoord  : TEXCOORD1;
#endif
};



//-----------------------------------------
//	VertexShader
//-----------------------------------------
v2f VSMain(appdata_base v)
{
	v2f o;

	o.pos = UnityObjectToClipPos(v.vertex);
	o.depth = 0.0f;
	UNITY_TRANSFER_DEPTH(o.depth);

#ifdef USE_OPTION_TEXTURE
	o.texcoord = TRANSFORM_TEX(v.texcoord, _OptionTex0).xy;
#endif

	return o;
}




//-----------------------------------------
//	Fragment Shader
//-----------------------------------------
fixed4	PSMain(v2f i) : COLOR
{
#ifdef _ALPHATEST_ON
	half	a = tex2D(_OptionTex0, i.texcoord).x;

	clip((float)(a - _Cutoff));
#endif

	UNITY_OUTPUT_DEPTH(i.depth);
}
