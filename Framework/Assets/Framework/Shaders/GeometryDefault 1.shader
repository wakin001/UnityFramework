// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: commented out 'float4 unity_LightmapST', a built-in variable  
// Upgrade NOTE: commented out 'sampler2D unity_Lightmap', a built-in variable  

Shader "Framework/TexShader2"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert  
			#pragma fragment frag  
			#include "UnityCG.cginc"  

			sampler2D _MainTex;
			float4 _MainTex_ST;//纹理缩放偏移向量(Unity默认此变量赋值,变量名规则：纹理名_ST)  

			// sampler2D unity_Lightmap;//若开启光照贴图，系统默认填值  
			// float4 unity_LightmapST;//与上unity_Lightmap同理  

			struct v2f {
				float4 pos:POSITION;
				float2 uv:TEXCOORD0;
				float2 uv2:TEXCOORD1;
			};

			v2f vert(appdata_full v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				//第一种方式：  
				//o.uv = v.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;  
				//第二种方式：内建宏，双方和第一种一样，只是对第一种计算的封装  
				o.uv = TRANSFORM_TEX(v.texcoord,_MainTex);

				o.uv2 = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;

				return o;
			}

			fixed4 frag(v2f v) : COLOR
			{
				//解密光照贴图计算公式  
				float3 lightmapColor = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap,v.uv2));
				fixed4 col = tex2D(_MainTex, v.uv);//第一个参数：纹理，第二个参数UV向量  
				col.rgb *= lightmapColor;
				return col;
			}
			ENDCG
		}
	}
}