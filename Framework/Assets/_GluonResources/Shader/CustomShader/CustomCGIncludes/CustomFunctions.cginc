#ifndef UNITY_STANDARD_CUSTOMFUNCTIONS_INCLUDED
#define UNITY_STANDARD_CUSTOMFUNCTIONS_INCLUDED



#define	mix lerp
#define fract(x)	(x-floor(x))

uniform	half4	_CoefficientRGBtoHSV;	//	(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);






//----------------------------------------------------------
//	カラーの影響度を反映する
//----------------------------------------------------------
half4	CalcColorImpact(half4 color, half colorImpact )
{
#if defined(VERTEX_COLOR_MEDIANHALF)
	color.rgb = (color.rgb - 0.5f) * 2.0f;
#endif
	color.rgb = 1.0f - (1.0f - color.rgb) * colorImpact;

	return	color;
}





//-------------------------------------------------------------
//	RGB -> HSV
//-------------------------------------------------------------
half3 rgb2hsv(half3 rgb)
{
	half3	hsv;

	half4 K = _CoefficientRGBtoHSV;
	half4 p = mix(half4(rgb.bg, K.wz), half4(rgb.gb, K.xy), step(rgb.b, rgb.g));
	half4 q = mix(half4(p.xyw, rgb.r), half4(rgb.r, p.yzx), step(p.x, rgb.r));

	float d = q.x - min(q.w, q.y);
	float e = 1.0e-10;
	
	hsv = half3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);

	return hsv;
}




//-------------------------------------------------------------
//	HSV -> RGB
//-------------------------------------------------------------
half3 hsv2rgb(half3 hsv)
{
	half3	rgb;

	float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	float3 p = abs(fract(hsv.xxx + K.xyz) * 6.0 - K.www);
	rgb = hsv.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), hsv.y);

	return rgb;
}


#endif	//	UNITY_STANDARD_CUSTOMFUNCTIONS_INCLUDED

