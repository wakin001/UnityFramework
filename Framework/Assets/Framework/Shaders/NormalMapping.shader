// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Framework/NormalMapping"
{
	Properties
	{
		_Color("Color Tint", Color) = (1, 1, 1, 1)
		_MainTex("Main Tex", 2D) = "white" {}
		_Specular("Specular", Color) = (1, 1, 1, 1)
		_Gloss("Gloss", Range(8.0, 256)) = 20

		_BumpMap("Bump Map", 2D) = "bump" {}		// 注意默认值是 "bump"{}
		_BumpScale("Bump Scale", Float) = 1.0

		// 模拟的光照位置
		_CustomLightPos("Custom Light Position", Vector) = (-0.1, -1.0, -0.3)
		// 模拟的光照颜色
		_CustomLightColor("Custom Light Color", Color) = (1, 1, 1, 1)
	}


	SubShader
	{
		Pass
		{
			Tags { "LightMode" = "ForwardBase" }

			CGPROGRAM

			#include "UnityCG.cginc"	

			#pragma vertex vert
			#pragma fragment frag

			sampler2D _MainTex;
			float4 _MainTex_ST;

			fixed4 _Color;
			fixed4 _Specular;
			float _Gloss;

			sampler2D _BumpMap;
			float4 _BumpMap_ST;
			float _BumpScale;
			fixed4 _CustomLightColor;
			float4 _CustomLightPos;

			struct a2v
			{
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;
				float3 normal : NORMAL;			// 注意：这里类型是NORMAL
				float4 tangent : TANGENT;		// 切线
			};

			struct v2f
			{
				float4 pos: SV_POSITION;			// pos是在裁剪空间下，
				float4 uv : TEXCOORD0;				// 注意我们用float4，zw用来存bump的uv
				float3 lightDir : TEXCOORD1;		// 在裁剪空间下
				float3 viewDir : TEXCOORD2;			// 在裁剪空间下
			};

			float3 ObjSpaceLightDirCustom(in float4 v)
			{
				float3 objSpaceLightPos = mul(unity_WorldToObject, _CustomLightPos).xyz;
#ifndef USING_LIGHT_MULTI_COMPILE
				return objSpaceLightPos.xyz - v.xyz * _CustomLightPos.w;
#else
#ifndef USING_DIRECTIONAL_LIGHT
				return objSpaceLightPos.xyz - v.xyz;
#else
				return objSpaceLightPos.xyz;
#endif
#endif
			}

			v2f vert(a2v v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				// o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.uv.xy = v.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				o.uv.zw = v.texcoord.xy * _BumpMap_ST.xy + _BumpMap_ST.zw;

				// construct a matrix to transform vectors from object space to tangent space
				// v.tangent.w is direction.
				float3 binormal = cross(normalize(v.normal), normalize(v.tangent.xyz)) * v.tangent.w;
				float3x3 rotation = float3x3(v.tangent.xyz, binormal, v.normal);
				// Or just use build-in macro
//				TANGENT_SPACE_ROTATION;

				// transform to tangent space
				o.lightDir = mul(rotation, ObjSpaceLightDirCustom(v.vertex)).xyz;
				o.viewDir = mul(rotation, ObjSpaceViewDir(v.vertex)).xyz;

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed3 tangentLightDir = normalize(i.lightDir);
				fixed3 tangentViewDir = normalize(i.viewDir);

				// get the texel in normal map.
				fixed4 packedNormal = tex2D(_BumpMap, i.uv.zw);

				// 如果texture被设成 “Normal Map”，则可以用UnpackNormal函数，否则需要
				// tangentNormal.xy = (packedNormal.xy * 2 - 1);
				fixed3 tangentNormal = UnpackNormal(packedNormal);
				tangentNormal.xy *= _BumpScale;
				tangentNormal.z = sqrt(1.0 - saturate(dot(tangentNormal.xy, tangentNormal.xy)));


				// 在切线空间下计算光照的各个分量
				fixed3 halfDir = normalize(tangentLightDir + tangentViewDir);

				fixed3 specular = _CustomLightColor.rgb * _Specular.rgb * pow(max(0, dot(tangentNormal, halfDir)), _Gloss);

				fixed3 albedo = tex2D(_MainTex, i.uv).rgb * _Color.rgb;
				fixed3 diffuse = _CustomLightColor.rgb * albedo * max(0, dot(tangentNormal, tangentLightDir));

				fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz * albedo;

				return fixed4(specular + diffuse + ambient, 1.0);
			}
			ENDCG
		}
	}
	FallBack "Specular"
}
