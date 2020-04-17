// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

////////////////////////////////////////////////////////////////////////////
//
//	シェーダで使う共通関数
//	
////////////////////////////////////////////////////////////////////////////
#ifndef	_CUSTOM_SHADER_COMMON_CGINC_
#define	_CUSTOM_SHADER_COMMON_CGINC_

#pragma exclude_renderers gles

//#define OUTLINE_DISTANCE_SCALE	PLATFORM_SCALE(0.004)
#define OUTLINE_DISTANCE_SCALE	PLATFORM_SCALE(0.0028)
#define	OUTLINE_Z_OFFSET		PLATFORM_SCALE(0.0015)

#define OUTLINE_FOV_BASE	(30.0)	// FOV計算時のデフォルト値
#define OUTLINE_OFFSET		(0.5)	// アウトラインサイズのオフセット

#define COT_FOV_DIV_2		(UNITY_MATRIX_P[1][1])		//	1/cos(fov/2)
#define PI			        (3.14159265359)
#define MAX_CIRCLE_VOLUME	(6.29)


#define TRANSFORM_TEX2(tex,name) (tex.xyzw * name##_ST.xyxy + name##_ST.zwzw)



//-------------------------------------------------------------
//	powの近似値を求める
//-------------------------------------------------------------
#define shlickT(T) inline T shlick(T t, T k){ return t / (k - k * t + t); }

shlickT(half)
shlickT(half2)
shlickT(half3)
shlickT(half4)
#if !defined(SHADER_API_GLES) && !defined(SHADER_API_METAL)
shlickT(float)
shlickT(float2)
shlickT(float3)
shlickT(float4)
#endif



uniform half4x4	_HSVParameter;		//	HSV調整のためのパラメータ




//-------------------------------------------------------------
//	目線ベクトル取得
//-------------------------------------------------------------
inline half3 GetEyeVector( fixed3 worldPosition)
{
    return normalize(_WorldSpaceCameraPos - worldPosition).xyz;
}



//-------------------------------------------------------------
//	スペキュラ計算
//-------------------------------------------------------------
inline fixed GetSpecularValue(fixed3 normal, fixed3 lightVector, fixed3 eyeVector)
{
    fixed3 halfVector = normalize( lightVector + eyeVector );
    fixed specularDot = dot( normal, halfVector );

    return specularDot;
}



//-------------------------------------------------------------
//	フォールオフ計算
//-------------------------------------------------------------
inline fixed GetFallOffValue(fixed3 normal, fixed3 eyeVector)
{
    fixed result = 1.0f - abs( dot( normal, eyeVector ) );

    return result;
}



//-------------------------------------------------------------
//	フォールオフ計算
//-------------------------------------------------------------
inline half3 GetHSVShiftColor(half3 color)
{
    return saturate(mul(half4(color, 0.0f), _HSVParameter).xyz);
}


//-------------------------------------------------------------
//	コントラスト、彩度、明るさを操作する（参考用）
//-------------------------------------------------------------
half3 ContrastSaturationBrightness(half3 color, half brt, half sat, half con)
{ 
    // Increase or decrease theese values to adjust r, g and b color channels seperately 
    const half3 LumCoeff = half3(0.2125, 0.7154, 0.0721);
    
    half3 AvgLumin = half3(0.5, 0.5, 0.5);
    half3 brtColor = color * brt;
    half  intensityf = dot(brtColor, LumCoeff);
    half3 intensity = half3(intensityf, intensityf, intensityf);
    half3 satColor = lerp(intensity, brtColor, sat);
    half3 conColor = lerp(AvgLumin, satColor, con);

    return conColor; 
}



//---------------------------------------------------------------
//	パラメータのスムース調整
//---------------------------------------------------------------
half4 SmoothingParameter(half value, half4 size, half4 softness, half4 intensity)
{
    half4 inverseSize = 1.0f - size;

    return smoothstep(inverseSize - softness, inverseSize + softness, value) * intensity;
}



//----------------------------------------------------------------
//	fixed用のlearp機能、GPUによっては正しく動作しないための機能
//----------------------------------------------------------------
inline fixed4	FixedLerp( fixed4 a, fixed4 b, fixed x )
{
    return	a*(1.0f-x) + b*x;
}
inline fixed3	FixedLerp( fixed3 a, fixed3 b, fixed x )
{
    return	a*(1.0f-x) + b*x;
}



//-------------------------------------------------------------
//	明るさの取得
//-------------------------------------------------------------
inline	fixed	GetBright( fixed3 color )
{
    fixed	bright = max( color.r, color.g );
    
    bright = max( bright, color.b );

    return bright;
}




//-------------------------------------------------------------
//	輝度の取得
//-------------------------------------------------------------
inline	fixed	GetLuminance( fixed3 color )
{
    // Increase or decrease theese values to adjust r, g and b color channels seperately 
    const fixed3 LumCoeff = fixed3(0.2125, 0.7154, 0.0721); 

    return dot( color, LumCoeff ); 
}



//-------------------------------------------------------------
//	彩度の操作を行う
//	※スクリプト側に処理を持っていくことで更に最適化は可能
//-------------------------------------------------------------
inline	fixed3 ColorSaturation(fixed3 color, fixed sat) 
{ 
    fixed intensityf = GetLuminance( color );
    fixed3 satColor = intensityf*(1.0-sat) + color*sat;		//	lerpを使用すると変な色になってしまう	lerp(intensityf, color, sat); 
    return satColor; 
}



//-------------------------------------------------------------
//	コントラストの操作を行う
//-------------------------------------------------------------
inline	fixed3 ColorContrast(fixed3 color, fixed con)
{ 
    fixed3 conColor = lerp(0.5, color, con); 
    return conColor; 
}


    

//------------------------------------------------------------
//	色の明るさと強さから、２色刷りの色を作成する
//	1.グレースケールから色の濃さを取得
//	2.色AとBの色を線形補間
//	3.元のグレースケールの色をかけて不要な箇所の色を削除
//	4.強さをかけて調節
//------------------------------------------------------------
inline fixed3 GetGrayLerpColor( fixed3 colorA, fixed3 colorB, fixed gray, half power )
{
    fixed3 color = lerp( colorA, colorB, gray ) * gray * power;

    return color;
}






//-----------------------------------------------------------
//	周辺減光の処理を行う
//-----------------------------------------------------------
inline	fixed3 GetVignetColor( fixed3 color, half2 uv, half intensity )
{
    half2	coords = (uv - 0.5) * 2.0;
    half	coordDot = dot( coords, coords );
    fixed	mask = 1.0 - coordDot * intensity; 
        
    return color * mask;
}




//---------------------------------------------------------------------
//	頂点でデュフューズとスペキュラーの色の計算をする
//---------------------------------------------------------------------
inline	half3	CalcVertexDiffuseSpecular( float4 position, half3 normal, half3 color, half3 lightDir, half3 lightColor, half3 baseSpecColor, half shininess, float power )
{
    half3		diffuseSpecular;
    float4x4	modelMatrix = unity_ObjectToWorld;
    float4x4	modelMatrixInverse = unity_WorldToObject; 
    float4		posWorld	= mul( modelMatrix, position );

    float3		normalDir = normalize( mul(float4(normal, 0.0), modelMatrixInverse).xyz);
    float3		reflection = reflect(-lightDir, normalDir);
    float3		viewDir = normalize(_WorldSpaceCameraPos - posWorld);
    float4		normalLightDot = dot(normalDir, lightDir);
    half3		diffuseColor = lightColor * color * max(0.0, normalLightDot);
    half3		specularReflection;
    
    half3 specColor = lightColor * baseSpecColor * step(0.0, normalLightDot);
    specularReflection = specColor * shlick(max(0.0, dot(reflection, viewDir)), shininess);
    diffuseSpecular = (diffuseColor + specularReflection) * power;

    return	diffuseSpecular;
}




//----------------------------------------------------------------------
//	フラグメントスペキュラのパラメータの計算を行う
//----------------------------------------------------------------------
inline	void	CalcSpecularParam( float4 position, half3 normal, half3 baseColor,
                                   half3 lightDir, half3 lightColor, half3 baseSpecColor,
                                   out half3 normalDir, out half3 viewDir, out half3 reflection, out half3 outDiffuseColor, out half3 outSpecColor )
{
    half3		diffuseSpecular;
    float4x4	modelMatrix = unity_ObjectToWorld;
    float4x4	modelMatrixInverse = unity_WorldToObject; 
    float4		posWorld	= mul( modelMatrix, position );

    normalDir = normalize( mul(float4(normal, 0.0), modelMatrixInverse).xyz);
    reflection = reflect(-lightDir, normalDir);
    viewDir = normalize(_WorldSpaceCameraPos - posWorld);

    float4		normalLightDot = dot(normalDir, lightDir);

    outDiffuseColor = lightColor * baseColor * max(0.0, normalLightDot);
    outSpecColor	= lightColor * baseSpecColor * step(0.0, normalLightDot);
}



//---------------------------------------------------------------------
//	スペキュラの反射の色を計算する
//---------------------------------------------------------------------
inline	half3	GetSpecularReflection( half3 normalDir, half3 viewDir, half3 reflection, half3 specColor, half shininess )
{
    half3 specularReflection;

    normalDir = normalize(normalDir);
    viewDir = normalize(viewDir);
    
    specularReflection = specColor * shlick(max(0.0, dot(reflection, viewDir)), shininess );

    return	specularReflection;
}



//---------------------------------------------
//  アルファブレンドを行う
//---------------------------------------------
half4 AlphaBlend(half4 srcColor, half4 dstColor)
{
    half4 color;
    color.a = (1.0 - srcColor.a) * dstColor.a + srcColor.a;
    color.rgb = ((srcColor.rgb * srcColor.a) + (dstColor.rgb * dstColor.a) * (1.0 - srcColor.a)) / color.a;

    return color;
}




//-------------------------------------------
//  YUVからRGBに変換
//-------------------------------------------
inline half3 YCbCrtoRGB(half3 YCbCr)
{
    YCbCr.gb -= 0.5f;

    half3	RGB;
    half4	temp = half4(-0.344136, 1.772, 1.402, -0.714136);

    temp *= YCbCr.yyzz;
    temp.x += temp.w;

    RGB = YCbCr.x + temp.zxy;

    return RGB;
}




//----------------------------------------------------
//	ベクトルの角度を計算
//----------------------------------------------------
float VectorToRadian(float2 pos, float size)
{
    pos = normalize(pos); //	計算誤差を回避するために正規化

    float s;
    
    s = acos(pos.y / sqrt(dot(pos, pos))); // 角度θを求める

    s = lerp(0.0f, lerp(s, (2 * PI - s), step(pos.x, 0.0f)), size > 0.0f); //	0～2*PIの間に変換し、sizeで0除算を回避

    return s;
}

#endif


