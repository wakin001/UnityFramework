// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Framework/GeometryDefault"
{
    Properties
    {
		_MainTex("MainTex", 2D) = "white"{}
		_Color("Color", Color) = (1,1,1,1)
		_ColorPower("Color Power" , Range(0,5)) = 1
		[Enum(None,0,Front,1,Back,2)] _Cull("Culling" , Int) = 2
			/*_VertexColorImpact("_Vertex Color Impact" , Range(0,2)) = 1
		[Space][Header(Detail Parameter)] _OffsetFactor("Depth Offset Factor" , Float) = 0
		_OffsetUnits("Depth Offset Units" , Float) = 0
		_SHLightingScale("Lightprobe scale" , Float) = 1*/
    }
    SubShader
    {
        Tags 
		{ 
			"Queue" = "Geometry"
		}
        LOD 100

        Pass
        {
			Name "GEOMETRYDEFAULT"
			Tags
			{
				"LightMode" = "ForwardBase"
			}

			Blend One Zero
			ZWrite On
			ZTest LEqual
			Cull[_Cull]
			Offset[_OffsetFactor],[_OffsetUnits]
			Fog
			{
				Mode Off
			}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
			#pragma multi_compile_fwdbase
			#pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
			//#pragma skip_variants LIGHTMAP_ON

            #include "UnityCG.cginc"

            struct a2v
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				half2 uvLightmap : TEXCOORD1;
				float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
#ifndef LIGHTMAP_OFF
				float2 uvLM : TEXCOORD1;
#endif 
                UNITY_FOG_COORDS(2)
                float4 vertex : SV_POSITION;

            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			fixed4 _Color;
			float _ColorPower;

            v2f vert (a2v v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
#ifndef LIGHTMAP_OFF
				o.uvLM = v.uvLightmap.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#endif
				UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
				
#ifndef LIGHTMAP_OFF
				fixed3 lightmapColor = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uvLM.xy));
				col.rgb *= lightmapColor;
#endif
				col = col * _Color * _ColorPower;

				UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }

		//Pass
		//{
		//	Name "SHADOWCAST"
		//	/*Tags{ "LightMode" = "ShadowCaster" }

		//	Fog{ Mode Off }
		//	ZWrite On
		//	ZTest LEqual
		//	Cull[_Cull]

		//	CGPROGRAM
		//	#pragma vertex VSMain
		//	#pragma fragment PSMain
		//	#define SHADOWS_DEPTH
		//	#pragma fragmentoption ARB_precision_hint_fastest

		//	#define SHADOW_COLLECTOR_PASS

		//	#include "UnityCG.cginc"

		//	#include "OriginalDepthCaster.cginc"
		//	ENDCG*/

		//	Tags {"LightMode" = "ShadowCaster"}
		//	//Offset 1, 1

		//	CGPROGRAM

		//	#pragma multi_compile_shadowcaster

		//	#pragma vertex vert
		//	#pragma fragment frag

		//	#include "UnityCG.cginc"

		//	struct v2f
		//	{
		//		// 声明一个阴影纹理坐标，需要下一个插值寄存器的索引值
		//		V2F_SHADOW_CASTER;
		//	};

		//	v2f vert(appdata_base v)
		//	{
		//		v2f o;
		//		// 在vert中计算上一步中声明的阴影纹理坐标
		//		TRANSFER_SHADOW_CASTER(o);

		//		return o;
		//	}

		//	fixed4 frag(v2f i) : SV_Target
		//	{
		//		// 计算阴影值
		//		SHADOW_CASTER_FRAGMENT(i);
		//	}
		//	ENDCG
		//}
    }
}
