// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Framework/Character"
{
    Properties
    {
		_MainTex("MainTex", 2D) = "white"{}
		_Color("Color", Color) = (1,1,1,1)
		_ColorPower("Color Power" , Range(0,5)) = 1
		[Enum(None,0,Front,1,Back,2)] _Cull("Culling" , Int) = 2
		_VertexColorImpact("_Vertex Color Impact" , Range(0,2)) = 1
		[Space][Header(Detail Parameter)] _OffsetFactor("Depth Offset Factor" , Float) = 0
		_OffsetUnits("Depth Offset Units" , Float) = 0
		_SHLightingScale("Lightprobe scale" , Float) = 1

		_LerpRimColor("Lerp Rim Color", Color) = (1,0,0,1)
		_LerpRimPower("Lerp Rim Power", Range(0.0,5.0)) = 1.0
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
			ZWrite Off
			ZTest Greater
			Blend SrcAlpha One

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 pos : POSITION;
			};

			fixed4 _LerpRimColor;
			half _LerpRimPower;

			v2f vert(appdata v)
			{
				v2f o;

				o.pos = UnityObjectToClipPos(v.vertex);
				return o;
			}

			half4 frag(v2f i) : COLOR
			{
				return _LerpRimColor * _LerpRimPower;
			}
			ENDCG
		}

        Pass
        {
			Name "GEOMETRYDEFAULT"
			Tags
			{
				"LightMode" = "ForwardBase"
			}

			Blend One Zero
			ZWrite On
			ColorMask RGBA
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
			#pragma multi_compile_fwdbase
			#pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON

            #include "UnityCG.cginc"

            struct a2v
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
#ifndef LIGHTMAP_OFF
				half2 uvLM : TEXCOORD2;
#endif 
				// lightprobe
				fixed3  SHLighting : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			fixed4 _Color;
			float _ColorPower;
			float _SHLightingScale;

            v2f vert (a2v v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
#ifndef LIGHTMAP_OFF
				o.uvLM = v.uv.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#endif
				
				float3 worldNormal = mul((float3x3)unity_ObjectToWorld, v.normal);
				o.SHLighting = ShadeSH9(float4(worldNormal, 1));

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
				
#ifndef LIGHTMAP_OFF
				fixed3 lm = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uvLM.xy));
				col.rgb *= lm;
#endif
				col = col * _Color * _ColorPower;
				//col.rgb *= i.SHLighting;
                return col;
            }
            ENDCG
        }

    }
}
